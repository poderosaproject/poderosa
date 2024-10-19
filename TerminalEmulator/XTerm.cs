// Copyright 2004-2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;

using Poderosa.Document;
using Poderosa.ConnectionParam;
using Poderosa.View;
using Poderosa.Preferences;
using Poderosa.Terminal.EscapeSequence;

namespace Poderosa.Terminal {
    internal class XTerm : AbstractTerminal {

        private enum MouseTrackingState {
            Off,
            Normal,
            Drag,
            Any,
        }

        private enum MouseTrackingProtocol {
            Normal,
            Utf8,
            Urxvt,
            Sgr,
        }

        private enum ProcessCharResult {
            Processed,
            Unsupported,
        }

        private class SavedCursor {
            // cursor position (0-based index)
            public readonly int Row;
            public readonly int Col;
            // character attributes / selective erase attribute
            public readonly TextDecoration Decoration;
            // wrap mode
            public readonly bool WrapAroundMode;
            // origin mode
            public readonly bool ScrollRegionRelative;

            public SavedCursor(
                int row,
                int col,
                TextDecoration decoration,
                bool wrapAroundMode,
                bool scrollRegionRelative
            ) {
                this.Row = row;
                this.Col = col;
                this.Decoration = decoration;
                this.WrapAroundMode = wrapAroundMode;
                this.ScrollRegionRelative = scrollRegionRelative;
            }
        }

        private class RectArea {
            public readonly int Top;
            public readonly int Left;
            public readonly int Bottom;
            public readonly int Right;

            public RectArea(
                int top,
                int left,
                int bottom,
                int right
            ) {
                this.Top = top;
                this.Left = left;
                this.Bottom = bottom;
                this.Right = right;
            }
        }

        private readonly EscapeSequenceEngine<XTerm> _escapeSequenceEngine;

        private IModalCharacterTask _currentCharacterTask = null;

        private bool _wrapAroundMode = true;
        private bool _reverseVideo = false;
        private bool[] _tabStops;
        private readonly List<GLine>[] _savedScreen = new List<GLine>[2];	// { main, alternate } 別のバッファに移行したときにGLineを退避しておく
        private bool _isAlternateBuffer = false;
        private readonly SavedCursor[] _savedCursor = new SavedCursor[2];	// { main, alternate }
        private SavedCursor _savedCursorSCO = null;
        private readonly Dictionary<int, bool> _savedDecModes = new Dictionary<int, bool>();

        private bool _bracketedPasteMode = false;
        private readonly byte[] _bracketedPasteModeLeadingBytes = new byte[] { 0x1b, (byte)'[', (byte)'2', (byte)'0', (byte)'0', (byte)'~' };
        private readonly byte[] _bracketedPasteModeTrailingBytes = new byte[] { 0x1b, (byte)'[', (byte)'2', (byte)'0', (byte)'1', (byte)'~' };
        private readonly byte[] _bracketedPasteModeEmptyBytes = new byte[0];

        private MouseTrackingState _mouseTrackingState = MouseTrackingState.Off;
        private MouseTrackingProtocol _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
        private bool _focusReportingMode = false;
        private int _prevMouseRow = -1;
        private int _prevMouseCol = -1;
        private MouseButtons _mouseButton = MouseButtons.None;

        private bool _forceNewLine = false; // controls behavior of LF/FF/VT
        private bool _insertMode = false;
        private bool _scrollRegionRelative = false;

        private const int MOUSE_POS_LIMIT = 255 - 32;       // mouse position limit
        private const int MOUSE_POS_EXT_LIMIT = 2047 - 32;  // mouse position limit in extended mode
        private const int MOUSE_POS_EXT_START = 127 - 32;   // mouse position to start using extended format

        public XTerm(TerminalInitializeInfo info)
            : base(info) {
            _escapeSequenceEngine = new EscapeSequenceEngine<XTerm>(HandleException, HandleIncompleteEscapeSequence);
            _tabStops = new bool[GetDocument().TerminalWidth];
            InitTabStops();
        }

        private void HandleIncompleteEscapeSequence(string sequence) {
            RuntimeUtil.SilentReportException(new IncompleteEscapeSequenceException("Incomplete escape sequence", sequence));
        }

        private void HandleException(Exception ex, string sequence) {
            if (ex is UnknownEscapeSequenceException) {
                CharDecodeError(
                    String.Format("{0}: {1}",
                        GEnv.Strings.GetString("Message.EscapesequenceTerminal.UnsupportedSequence"),
                        ex.Message
                    )
                );
            }
            RuntimeUtil.SilentReportException(ex);
        }

        public override bool IsEscapeSequenceReading {
            get {
                return _escapeSequenceEngine.IsEscapeSequenceReading;
            }
        }

        protected override void FullResetInternal() { // called from the base class
            ProcessCursorPosition(1, 1);
            DoEraseInDisplay(2 /* all */, false);
            InitTabStops();
            _savedDecModes.Clear();
            _wrapAroundMode = true;
            _reverseVideo = false;
            _bracketedPasteMode = false;
            _mouseTrackingState = MouseTrackingState.Off;
            _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
            _focusReportingMode = false;
            _forceNewLine = false;
            _insertMode = false;
            _scrollRegionRelative = false;
            _escapeSequenceEngine.Reset();
        }

        protected override void SoftResetInternal() { // called from the base class
            _wrapAroundMode = true;
            _insertMode = false;
            _scrollRegionRelative = false;
        }

        public override void StartModalTerminalTask(IModalTerminalTask task) {
            base.StartModalTerminalTask(task);
            _currentCharacterTask = (IModalCharacterTask)task.GetAdapter(typeof(IModalCharacterTask));
        }

        public override void EndModalTerminalTask() {
            base.EndModalTerminalTask();
            _currentCharacterTask = null;
        }

        public override bool GetFocusReportingMode() {
            return _focusReportingMode;
        }

        internal override byte[] GetPasteLeadingBytes() {
            return _bracketedPasteMode ? _bracketedPasteModeLeadingBytes : _bracketedPasteModeEmptyBytes;
        }

        internal override byte[] GetPasteTrailingBytes() {
            return _bracketedPasteMode ? _bracketedPasteModeTrailingBytes : _bracketedPasteModeEmptyBytes;
        }

        public override void ProcessChar(char ch) {
            if (ch == ControlCode.NUL) {
                return;
            }

            this.LogService.XmlLogger.Write(ch);

            if (!_escapeSequenceEngine.Process(this, ch)) {
                IModalCharacterTask characterTask = _currentCharacterTask;
                if (characterTask != null) { // macro etc.
                    characterTask.ProcessChar(ch);
                }

                ProcessNormalChar(ch);
            }
        }

#if NOTUSED
        private void ProcessCharInternal(char ch) {
            if (!_isEscapeSequenceReading) {
                if (ch == ControlCode.ESC) {
                    _isEscapeSequenceReading = true;
                }
                else {
                    IModalCharacterTask characterTask = _currentCharacterTask;
                    if (characterTask != null) { //マクロなど、charを取るタイプ
                        characterTask.ProcessChar(ch);
                    }

                    this.LogService.XmlLogger.Write(ch);

                    if (Unicode.IsControlCharacter(ch)) {
                        ProcessControlChar(ch);
                        // ignore result
                    }
                    else {
                        ProcessNormalChar(ch);
                    }
                }
            }
            else {
                if (ch == ControlCode.NUL)
                    return; //シーケンス中にNULL文字が入っているケースが確認された なお今はXmlLoggerにもこのデータは行かない。

                if (ch == ControlCode.ESC) {
                    // escape sequence restarted ?
                    // save log silently
                    RuntimeUtil.SilentReportException(new UnknownEscapeSequenceException("Incomplete escape sequence: ESC " + _escapeSequence.ToString()));
                    _escapeSequence.Remove(0, _escapeSequence.Length);
                    return;
                }

                _escapeSequence.Append(ch);
                bool end_flag = false; //escape sequenceの終わりかどうかを示すフラグ
                if (_escapeSequence.Length == 1) { //ESC+１文字である場合
                    end_flag = ('0' <= ch && ch <= '9') || ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z' && ch != 'P') || ch == '>' || ch == '=' || ch == '|' || ch == '}' || ch == '~';
                }
                else if (_escapeSequence[0] == ']') { //OSCの終端はBELかST(String Terminator)
                    end_flag = (ch == ControlCode.BEL) || (ch == ControlCode.ST);
                    // Note: The conversion from "ESC \" to ST would be done in Preprocessor.
                }
                else if (this._escapeSequence[0] == '@') {
                    end_flag = (ch == '0') || (ch == '1');
                }
                else if (this._escapeSequence[0] == 'P') {  // DCS
                    end_flag = (ch == ControlCode.ST);
                }
                else {
                    end_flag = ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ch == '@' || ch == '~' || ch == '|' || ch == '{';
                }

                if (end_flag) { //シーケンスのおわり
                    _isEscapeSequenceReading = false;

                    char[] seq = _escapeSequence.ToString().ToCharArray();
                    _escapeSequence.Remove(0, _escapeSequence.Length);

                    this.LogService.XmlLogger.EscapeSequence(seq);

                    try {
                        char code = seq[0];
                        ProcessCharResult result = ProcessEscapeSequence(code, seq, 1);
                        if (result == ProcessCharResult.Unsupported)
                            throw new UnknownEscapeSequenceException("Unknown escape sequence: ESC " + new string(seq));
                    }
                    catch (UnknownEscapeSequenceException ex) {
                        CharDecodeError(GEnv.Strings.GetString("Message.EscapesequenceTerminal.UnsupportedSequence") + ex.Message);
                        RuntimeUtil.SilentReportException(ex);
                    }
                }
                else {
                    _isEscapeSequenceReading = true;
                }
            }
        }
#endif

        public bool ReverseVideo {
            get {
                return _reverseVideo;
            }
        }

        public override bool ProcessMouse(TerminalMouseAction action, MouseButtons button, Keys modKeys, int row, int col) {
            MouseTrackingState currentState = _mouseTrackingState;  // copy value because _mouseTrackingState may be changed in another thread.

            if (currentState == MouseTrackingState.Off) {
                _prevMouseRow = -1;
                _prevMouseCol = -1;
                switch (action) {
                    case TerminalMouseAction.ButtonUp:
                    case TerminalMouseAction.ButtonDown:
                        _mouseButton = MouseButtons.None;
                        break;
                }
                return false;
            }

            // Note: from here, return value must be true even if nothing has been processed actually.

            MouseTrackingProtocol protocol = _mouseTrackingProtocol; // copy value because _mouseTrackingProtocol may be changed in another thread.

            int posLimit = protocol == MouseTrackingProtocol.Normal ? MOUSE_POS_LIMIT : MOUSE_POS_EXT_LIMIT;

            if (row < 0)
                row = 0;
            else if (row > posLimit)
                row = posLimit;

            if (col < 0)
                col = 0;
            else if (col > posLimit)
                col = posLimit;

            int statBits;
            switch (action) {
                case TerminalMouseAction.ButtonDown:
                    if (_mouseButton != MouseButtons.None)
                        return true;    // another button is already pressed

                    switch (button) {
                        case MouseButtons.Left:
                            statBits = 0x00;
                            break;
                        case MouseButtons.Middle:
                            statBits = 0x01;
                            break;
                        case MouseButtons.Right:
                            statBits = 0x02;
                            break;
                        default:
                            return true;    // unsupported button
                    }

                    _mouseButton = button;
                    break;

                case TerminalMouseAction.ButtonUp:
                    if (button != _mouseButton)
                        return true;    // ignore

                    if (protocol == MouseTrackingProtocol.Sgr) {
                        switch (button) {
                            case MouseButtons.Left:
                                statBits = 0x00;
                                break;
                            case MouseButtons.Middle:
                                statBits = 0x01;
                                break;
                            case MouseButtons.Right:
                                statBits = 0x02;
                                break;
                            default:
                                return true;    // unsupported button
                        }
                    }
                    else {
                        statBits = 0x03;
                    }

                    _mouseButton = MouseButtons.None;
                    break;

                case TerminalMouseAction.WheelUp:
                    statBits = 0x40;
                    break;

                case TerminalMouseAction.WheelDown:
                    statBits = 0x41;
                    break;

                case TerminalMouseAction.MouseMove:
                    if (currentState != MouseTrackingState.Any && currentState != MouseTrackingState.Drag)
                        return true;    // no need to send

                    if (currentState == MouseTrackingState.Drag && _mouseButton == MouseButtons.None)
                        return true;    // no need to send

                    if (row == _prevMouseRow && col == _prevMouseCol)
                        return true;    // no need to send

                    switch (_mouseButton) {
                        case MouseButtons.Left:
                            statBits = 0x20;
                            break;
                        case MouseButtons.Middle:
                            statBits = 0x21;
                            break;
                        case MouseButtons.Right:
                            statBits = 0x22;
                            break;
                        default:
                            statBits = 0x20;
                            break;
                    }
                    break;

                default:
                    return true;    // unknown action
            }

            if ((modKeys & Keys.Shift) != Keys.None)
                statBits |= 0x04;

            if ((modKeys & Keys.Alt) != Keys.None)
                statBits |= 0x08;   // Meta key

            if ((modKeys & Keys.Control) != Keys.None)
                statBits |= 0x10;

            if (protocol != MouseTrackingProtocol.Sgr)
                statBits += 0x20;

            _prevMouseRow = row;
            _prevMouseCol = col;

            byte[] data;
            int dataLen;

            switch (protocol) {

                case MouseTrackingProtocol.Normal:
                    data = new byte[] {
                        (byte)27, // ESCAPE
                        (byte)91, // [
                        (byte)77, // M
                        (byte)statBits,
                        (col == posLimit) ?
                            (byte)0 :                   // emulate xterm's bug
                            (byte)(col + (1 + 0x20)),   // column 0 --> send as 1
                        (row == posLimit) ?
                            (byte)0 :                   // emulate xterm's bug
                            (byte)(row + (1 + 0x20)),   // row 0 --> send as 1
                    };
                    dataLen = 6;
                    break;

                case MouseTrackingProtocol.Utf8:
                    data = new byte[8] {
                        (byte)27, // ESCAPE
                        (byte)91, // [
                        (byte)77, // M
                        (byte)statBits,
                        0,0,0,0,
                    };

                    dataLen = 4;

                    if (col < MOUSE_POS_EXT_START)
                        data[dataLen++] = (byte)(col + (1 + 0x20));     // column 0 --> send as 1
                    else { // encode in UTF-8
                        int val = col + 1 + 0x20;
                        data[dataLen++] = (byte)(0xc0 + (val >> 6));
                        data[dataLen++] = (byte)(0x80 + (val & 0x3f));
                    }

                    if (row < MOUSE_POS_EXT_START)
                        data[dataLen++] = (byte)(row + (1 + 0x20));     // row 0 --> send as 1
                    else { // encode in UTF-8
                        int val = row + (1 + 0x20);
                        data[dataLen++] = (byte)(0xc0 + (val >> 6));
                        data[dataLen++] = (byte)(0x80 + (val & 0x3f));
                    }
                    break;

                case MouseTrackingProtocol.Urxvt:
                    data = Encoding.ASCII.GetBytes(
                            "\u001b["
                            + statBits.ToInvariantString()
                            + ";"
                            + (col + 1).ToInvariantString()
                            + ";"
                            + (row + 1).ToInvariantString()
                            + "M"
                        );
                    dataLen = data.Length;
                    break;

                case MouseTrackingProtocol.Sgr:
                    data = Encoding.ASCII.GetBytes(
                            "\u001b[<"
                            + statBits.ToInvariantString()
                            + ";"
                            + (col + 1).ToInvariantString()
                            + ";"
                            + (row + 1).ToInvariantString()
                            + ((action == TerminalMouseAction.ButtonUp) ? "m" : "M")
                        );
                    dataLen = data.Length;
                    break;

                default:
                    return true;    // unknown protocol
            }

            TransmitDirect(data, 0, dataLen);

            return true;
        }

        private void ProcessNormalChar(char ch) {
            UnicodeChar unicodeChar;
            if (!base.UnicodeCharConverter.Feed(ch, out unicodeChar)) {
                return;
            }
            if (unicodeChar.IsZeroWidth) {
                return; // drop
            }

            ProcessNormalUnicodeChar(unicodeChar);
        }

        private void ProcessNormalUnicodeChar(UnicodeChar unicodeChar) {
            //WrapAroundがfalseで、キャレットが右端のときは何もしない
            if (!_wrapAroundMode && _manipulator.CaretColumn >= GetDocument().TerminalWidth - 1)
                return;

            if (_insertMode)
                _manipulator.InsertBlanks(_manipulator.CaretColumn, unicodeChar.IsWideWidth ? 2 : 1, GetDocument().CurrentDecoration);

            //既に画面右端にキャレットがあるのに文字が来たら改行をする
            int tw = GetDocument().TerminalWidth;
            if (_manipulator.CaretColumn + (unicodeChar.IsWideWidth ? 2 : 1) > tw) {
                _manipulator.EOLType = EOLType.Continue;
                GLine lineUpdated = GetDocument().UpdateCurrentLine(_manipulator);
                if (lineUpdated != null) {
                    this.LogService.TextLogger.WriteLine(lineUpdated);
                }
                GetDocument().LineFeed();
                _manipulator.Load(GetDocument().CurrentLine, 0);
            }

            //画面のリサイズがあったときは、_manipulatorのバッファサイズが不足の可能性がある
            if (tw > _manipulator.BufferSize)
                _manipulator.ExpandBuffer(tw);

            //通常文字の処理
            _manipulator.PutChar(unicodeChar, GetDocument().CurrentDecoration);
        }

        [EscapeSequence(ControlCode.ESC, ' ', 'F')] // S7C1T
        [EscapeSequence(ControlCode.ESC, ' ', 'G')] // S8C1T
        [EscapeSequence(ControlCode.ESC, ' ', 'L')] // dpANS X3.134.1 - ANSI conformance level 1
        [EscapeSequence(ControlCode.ESC, ' ', 'M')] // dpANS X3.134.1 - ANSI conformance level 2
        [EscapeSequence(ControlCode.ESC, ' ', 'N')] // dpANS X3.134.1 - ANSI conformance level 3
        [EscapeSequence(ControlCode.ESC, '#', '3')] // DECDHL – Double Height Line / top half
        [EscapeSequence(ControlCode.ESC, '#', '4')] // DECDHL – Double Height Line / bottom half
        [EscapeSequence(ControlCode.ESC, '#', '5')] // DECSWL – Single-width Line
        [EscapeSequence(ControlCode.ESC, '#', '6')] // DECDWL – Double-Width Line
        [EscapeSequence(ControlCode.ESC, '#', '8')] // DECALN – Screen Alignment Display
        [EscapeSequence(ControlCode.ESC, '%', '@')] // Select default character set
        [EscapeSequence(ControlCode.ESC, '%', 'G')] // Select UTF-8 character set
        [EscapeSequence(ControlCode.ESC, 'F')] // Cursor to lower left corner of screen
        [EscapeSequence(ControlCode.ESC, 'l')] // Memory Lock
        [EscapeSequence(ControlCode.ESC, 'm')] // Memory Unlock
        [EscapeSequence(ControlCode.SI)] // LS0 - Map G0 into GL (SI should be already processed by CharDecoder)
        [EscapeSequence(ControlCode.SO)] // LS1 - Map G1 into GL (SO should be already processed by CharDecoder)
        [EscapeSequence(ControlCode.ESC, 'n')] // LS2 - Map G2 into GL
        [EscapeSequence(ControlCode.ESC, '}')] // LS2R - Map G2 into GR
        [EscapeSequence(ControlCode.ESC, 'o')] // LS3 - Map G3 into GL
        [EscapeSequence(ControlCode.ESC, '|')] // LS3R - Map G3 into GR
        [EscapeSequence(ControlCode.ESC, '~')] // LS1R - Map G1 into GR
        // unsupported CSI is handled by EscapeSequenceEngine
        // [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 'T')] // Reset title modes
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'i')] // Media Copy
        // [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'i')] // Media Copy (DEC)
        // [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 'm')] // set key modifiers mode (xterm)
        // [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 'n')] // disable key modifiers mode (xterm)
        // [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 'p')] // set pointerMode (xterm)
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '"', 'p')] // Select Conformance Level
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'q')] // Load LEDs
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 'q')] // Set Cursor Style
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 't')] // Window Manipulation (xterm)
        // [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 't')] // Title Mode (xterm)
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 't')] // Set Warning Bell Volume
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 'u')] // Set Margin Bell Volume
        private void Ignore() {
        }

        [EscapeSequence(ControlCode.ESC, '(', EscapeSequenceParamType.SinglePrintable)] // Designate G0 Character Set
        [EscapeSequence(ControlCode.ESC, '(', '%', EscapeSequenceParamType.SinglePrintable)] // Designate G0 Character Set
        [EscapeSequence(ControlCode.ESC, '(', '&', EscapeSequenceParamType.SinglePrintable)] // Designate G0 Character Set
        [EscapeSequence(ControlCode.ESC, '(', '"', EscapeSequenceParamType.SinglePrintable)] // Designate G0 Character Set
        [EscapeSequence(ControlCode.ESC, ')', EscapeSequenceParamType.SinglePrintable)] // Designate G1 Character Set
        [EscapeSequence(ControlCode.ESC, '*', EscapeSequenceParamType.SinglePrintable)] // Designate G2 Character Set
        [EscapeSequence(ControlCode.ESC, '+', EscapeSequenceParamType.SinglePrintable)] // Designate G3 Character Set
        [EscapeSequence(ControlCode.ESC, '-', EscapeSequenceParamType.SinglePrintable)] // Designate G1 Character Set
        [EscapeSequence(ControlCode.ESC, '.', EscapeSequenceParamType.SinglePrintable)] // Designate G2 Character Set
        [EscapeSequence(ControlCode.ESC, '/', EscapeSequenceParamType.SinglePrintable)] // Designate G3 Character Set
        private void Ignore(char ch) {
        }

        [EscapeSequence(ControlCode.APC, EscapeSequenceParamType.Text, ControlCode.ST)] // Application Program Command
        private void Ignore(string p) {
        }

        [EscapeSequence(ControlCode.LF)]
        [EscapeSequence(ControlCode.VT)]
        [EscapeSequence(ControlCode.FF)]
        private void LineFeed() {
            if (_forceNewLine) {
                DoCarriageReturn();
                DoLineFeed();
                return;
            }

            LineFeedRule rule = GetTerminalSettings().LineFeedRule;
            if (rule == LineFeedRule.Normal) {
                DoLineFeed();
            }
            else if (rule == LineFeedRule.LFOnly) {
                DoCarriageReturn();
                DoLineFeed();
            }
        }

        [EscapeSequence(ControlCode.CR)]
        private void CarriageReturn() {
            LineFeedRule rule = GetTerminalSettings().LineFeedRule;
            if (rule == LineFeedRule.Normal) {
                DoCarriageReturn();
            }
            else if (rule == LineFeedRule.CROnly) {
                DoCarriageReturn();
                DoLineFeed();
            }
        }

        [EscapeSequence(ControlCode.BEL)]
        private void Bell() {
            IndicateBell();
        }

        [EscapeSequence(ControlCode.BS)]
        private void BackSpace() {
            //行頭で、直前行の末尾が継続であった場合行を戻す
            if (_manipulator.CaretColumn == 0) {
                TerminalDocument doc = GetDocument();
                int line = doc.CurrentLineNumber - 1;
                if (line >= 0 && doc.FindLineOrEdge(line).EOLType == EOLType.Continue) {
                    doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
                    doc.CurrentLineNumber = line;
                    if (doc.CurrentLine == null)
                        _manipulator.Reset(doc.TerminalWidth);
                    else
                        _manipulator.Load(doc.CurrentLine, doc.CurrentLine.DisplayLength - 1); //NOTE ここはCharLengthだったが同じだと思って改名した
                    doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
                }
            }
            else {
                _manipulator.BackCaret();
            }
        }

        [EscapeSequence(ControlCode.HT)]
        private void HorizontalTab() {
            _manipulator.CaretColumn = GetNextTabStop(_manipulator.CaretColumn);
        }

        private void DoLineFeed() {
            _manipulator.EOLType = (_manipulator.EOLType == EOLType.CR || _manipulator.EOLType == EOLType.CRLF) ? EOLType.CRLF : EOLType.LF;
            GLine lineUpdated = GetDocument().UpdateCurrentLine(_manipulator);
            if (lineUpdated != null) {
                this.LogService.TextLogger.WriteLine(lineUpdated);
            }
            GetDocument().LineFeed();

            //カラム保持は必要。サンプル:linuxconf.log
            int col = _manipulator.CaretColumn;
            _manipulator.Load(GetDocument().CurrentLine, col);
        }

        private void DoCarriageReturn() {
            _manipulator.CarriageReturn();
            _manipulator.EOLType = EOLType.CR;  // will be changed to CRLF in DoLineFeed()
        }

        [EscapeSequence(ControlCode.ESC, '6')]
        private void BackIndex() {
            if (_manipulator.CaretColumn > 0) {
                _manipulator.CaretColumn--;
            }
            else {
                ShiftScreen(1);
            }
        }

        [EscapeSequence(ControlCode.ESC, '9')]
        private void ForwardIndex() {
            if (_manipulator.CaretColumn < GetDocument().TerminalWidth - 1) {
                _manipulator.CaretColumn++;
            }
            else {
                ShiftScreen(-1);
            }
        }

        private void ShiftScreen(int columns) {
            TerminalDocument doc = GetDocument();

            int caretColumn = _manipulator.CaretColumn;
            doc.UpdateCurrentLine(_manipulator);

            int w = doc.TerminalWidth;
            int m = doc.TerminalHeight;
            for (GLine l = doc.TopLine; l != null; l = l.NextLine) {
                _manipulator.Load(l, 0);
                if (columns > 0) {
                    _manipulator.InsertBlanks(0, columns, doc.CurrentDecoration);
                }
                else if (columns < 0) {
                    _manipulator.DeleteChars(0, -columns, doc.CurrentDecoration);
                }
                _manipulator.ExportTo(l);
            }

            _manipulator.Load(doc.CurrentLine, caretColumn);

            doc.InvalidateAll();
        }

#if NOTUSED
        private ProcessCharResult ProcessEscapeSequence(char code, char[] seq, int offset) {
            switch (code) {
                case '[':
                    if (seq.Length - offset - 1 >= 0) {
                        string param = new string(seq, offset, seq.Length - offset - 1);
                        return ProcessAfterCSI(param, seq[seq.Length - 1]);
                    }
                    break;
                case ']':
                    if (seq.Length - offset - 1 >= 0) {
                        string param = new string(seq, offset, seq.Length - offset - 1);
                        return ProcessAfterOSC(param, seq[seq.Length - 1]);
                    }
                    break;
                case '=':
                    ChangeMode(TerminalMode.Application);
                    return ProcessCharResult.Processed;
                case '>':
                    ChangeMode(TerminalMode.Normal);
                    return ProcessCharResult.Processed;
                case 'E':
                    ProcessNextLine();
                    return ProcessCharResult.Processed;
                case 'M':
                    ReverseIndex();
                    return ProcessCharResult.Processed;
                case 'D':
                    Index();
                    return ProcessCharResult.Processed;
                case '7':
                    SaveCursor();
                    return ProcessCharResult.Processed;
                case '8':
                    RestoreCursor();
                    return ProcessCharResult.Processed;
                case 'c':
                    FullReset();
                    return ProcessCharResult.Processed;
                case 'F':
                    if (seq.Length == offset) { //パラメータなしの場合
                        ProcessCursorPosition(1, 1);
                        return ProcessCharResult.Processed;
                    }
                    else if (seq.Length > offset && seq[offset] == ' ')
                        return ProcessCharResult.Processed; //7/8ビットコントロールは常に両方をサポート
                    break;
                case 'G':
                    if (seq.Length > offset && seq[offset] == ' ')
                        return ProcessCharResult.Processed; //7/8ビットコントロールは常に両方をサポート
                    break;
                case 'L':
                    if (seq.Length > offset && seq[offset] == ' ')
                        return ProcessCharResult.Processed; //VT100は最初からOK
                    break;
                case 'H':
                    SetTabStop(_manipulator.CaretColumn, true);
                    return ProcessCharResult.Processed;
            }

            return ProcessCharResult.Unsupported;
        }

        private ProcessCharResult ProcessAfterCSI(string param, char code) {
            switch (code) {
                case 'c':
                    ProcessDeviceAttributes(param);
                    return ProcessCharResult.Processed;
                case 'm': //SGR
                    ProcessSGR(param);
                    return ProcessCharResult.Processed;
                case 'h':
                case 'l':
                    return ProcessDECSETMulti(param, code);
                case 'r':
                    if (param.Length > 0 && param[0] == '?')
                        return ProcessRestoreDECSET(param.Substring(1), code);
                    ProcessSetScrollingRegion(param);
                    return ProcessCharResult.Processed;
                case 's':
                    if (param.Length > 0 && param[0] == '?')
                        return ProcessSaveDECSET(param.Substring(1), code);
                    break;
                case 'n':
                    ProcessDeviceStatusReport(param);
                    return ProcessCharResult.Processed;
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                    ProcessCursorMove(param, code);
                    return ProcessCharResult.Processed;
                case 'H':
                case 'f': //fは本当はxterm固有
                    ProcessCursorPosition(param);
                    return ProcessCharResult.Processed;
                case 'J':
                    ProcessEraseInDisplay(param);
                    return ProcessCharResult.Processed;
                case 'K':
                    ProcessEraseInLine(param);
                    return ProcessCharResult.Processed;
                case 'L':
                    ProcessInsertLines(param);
                    return ProcessCharResult.Processed;
                case 'M':
                    ProcessDeleteLines(param);
                    return ProcessCharResult.Processed;
                case 'd':
                    ProcessLinePositionAbsolute(param);
                    return ProcessCharResult.Processed;
                case 'G':
                case '`': //CSI Gは実際に来たことがあるが、これは来たことがない。いいのか？
                    ProcessLineColumnAbsolute(param);
                    return ProcessCharResult.Processed;
                case 'X':
                    ProcessEraseChars(param);
                    return ProcessCharResult.Processed;
                case 'P':
                    _manipulator.DeleteChars(_manipulator.CaretColumn, ParseInt(param, 1), _currentdecoration);
                    return ProcessCharResult.Processed;
                case 'p':
                    return SoftTerminalReset(param);
                case '@':
                    _manipulator.InsertBlanks(_manipulator.CaretColumn, ParseInt(param, 1), _currentdecoration);
                    return ProcessCharResult.Processed;
                case 'I':
                    ProcessForwardTab(param);
                    return ProcessCharResult.Processed;
                case 'Z':
                    ProcessBackwardTab(param);
                    return ProcessCharResult.Processed;
                case 'S':
                    ProcessScrollUp(param);
                    return ProcessCharResult.Processed;
                case 'T':
                    ProcessScrollDown(param);
                    return ProcessCharResult.Processed;
                case 'g':
                    ProcessTabClear(param);
                    return ProcessCharResult.Processed;
                case 't':
                    //!!パラメータによって無視してよい場合と、応答を返すべき場合がある。応答の返し方がよくわからないので保留中
                    return ProcessCharResult.Processed;
                case 'U': //これはSFUでしか確認できてない
                    ProcessCursorPosition(GetDocument().TerminalHeight, 1);
                    return ProcessCharResult.Processed;
                case 'u': //SFUでのみ確認。特にbは続く文字を繰り返すらしいが、意味のある動作になっているところを見ていない
                case 'b':
                    return ProcessCharResult.Processed;
            }
            return ProcessCharResult.Unsupported;
        }
#endif

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'c')]
        private void ProcessPrimaryDeviceAttributes(NumericParams p) {
            if (p.Get(0, 0) == 0) {
                int[] features =
                {
                    1, // 132 columns
                    // 2, // Printer port
                    // 4, // Sixel
                    6, // Selective erase
                    // 7, // Soft character set (DRCS)
                    // 8, // User-defined keys (UDKs)
                    // 9, // National replacement character sets (NRCS)
                    // 12, // Yugoslavian (SCS)
                    // 15, // Technical character set
                    // 18, // Windowing capability
                    // 21, // Horizontal scrolling
                    22, // ANSI color
                    // 23, // Greek
                    // 24, // Turkish
                    // 29, // ANSI text locator
                    // 42, // ISO Latin-2 character set
                    // 44, // PCTerm
                    // 45, // Soft key map
                    // 46, // ASCII emulation
                };
                string featuresText = String.Join(";", features.Select(v => v.ToInvariantString()));

                byte[] data = Encoding.ASCII.GetBytes("\u001b[?64;" + featuresText + "c");
                TransmitDirect(data);
            }
        }

        [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 'c')]
        private void ProcessSecondaryDeviceAttributes(NumericParams p) {
            if (p.Get(0, 0) == 0) {
                byte[] data = Encoding.ASCII.GetBytes("\u001b[>41;1;0c");
                TransmitDirect(data);
                return;
            }
        }

        [EscapeSequence(ControlCode.CSI, '=', EscapeSequenceParamType.Numeric, 'c')]
        private void ProcessTertiaryDeviceAttributes(NumericParams p) {
            if (p.Get(0, 0) == 0) {
                // DECRPTUI: DCS ! | 00 00 00 00 ST
                byte[] data = Encoding.ASCII.GetBytes("\u001bP!|00000000\u001b\\");
                TransmitDirect(data);
                return;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'n')]
        private void ProcessDeviceStatusReport(NumericParams p) {
            int param = p.Get(0, 0);

            string response;
            switch (param) {
                case 5: // Operating Status Report
                    response = "\u001b[0n"; // good
                    break;
                case 6: // Cursor Position Report
                    response =
                        "\u001b["
                        + (GetDocument().CurrentLineNumber - GetDocument().TopLineNumber + 1).ToInvariantString()
                        + ";"
                        + (_manipulator.CaretColumn + 1).ToInvariantString()
                        + "R";
                    break;
                default:
                    return;
            }

            byte[] data = Encoding.ASCII.GetBytes(response);
            TransmitDirect(data);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'n')]
        private void ProcessDeviceStatusReportDEC(NumericParams p) {
            int param = p.Get(0, 0);

            string response;
            switch (param) {
                case 5:
                    // "CSI ? 5 n" doesn't appear in the DEC documentations, but xterm returns this
                    response = "\u001b[?0n";
                    break;
                case 6: // Extended Cursor Position Report
                    response =
                        "\u001b[?"
                        + (GetDocument().CurrentLineNumber - GetDocument().TopLineNumber + 1).ToInvariantString()
                        + ";"
                        + (_manipulator.CaretColumn + 1).ToInvariantString()
                        + ";1R";
                    break;
                case 15: // Printer Status Report
                    response = "\u001b[?13n"; // no printer
                    break;
                case 25: // User-Defined Keys Status
                    response = "\u001b[?20n"; // unlocked
                    break;
                case 26: // Keyboard Status Report
                    response = "\u001b[?27;1;0;0n"; // North American, Ready
                    break;
                case 55: // Locator Status
                    // according to "Locator Input Model for ANSI Terminals (sixth revision)", response below means "no locator"
                    response = "\u001b[?53n"; // no locator
                    break;
                case 56: // Locator Type
                    response = "\u001b[?57;0n"; // unknown
                    break;
                case 62: // Macro Space Report
                    // xterm returns 4 digits
                    response = "\u001b[0000*{";
                    break;
                case 63: // Memory Checksum
                    int pid = p.Get(1, 0);
                    response = "\u001bP" + pid.ToInvariantString() + "!~0000\u001b\\";
                    break;
                case 75: // Data Integrity Report
                    response = "\u001b[?70n"; // OK
                    break;
                case 85: // Multi-session Configuration
                    response = "\u001b[?83n"; // not configured
                    break;
                default:
                    return;
            }

            byte[] data = Encoding.ASCII.GetBytes(response);
            TransmitDirect(data);
        }

#if UNUSED        
        private void ProcessCursorMove(string param, char method) {
            int count = ParseInt(param, 1); //パラメータが省略されたときの移動量は１

            int column = _manipulator.CaretColumn;
            switch (method) {
                case 'A':
                    GetDocument().UpdateCurrentLine(_manipulator);
                    GetDocument().CurrentLineNumber = (GetDocument().CurrentLineNumber - count);
                    _manipulator.Load(GetDocument().CurrentLine, column);
                    break;
                case 'B':
                    GetDocument().UpdateCurrentLine(_manipulator);
                    GetDocument().CurrentLineNumber = (GetDocument().CurrentLineNumber + count);
                    _manipulator.Load(GetDocument().CurrentLine, column);
                    break;
                case 'C': {
                        int newvalue = column + count;
                        if (newvalue >= GetDocument().TerminalWidth)
                            newvalue = GetDocument().TerminalWidth - 1;
                        _manipulator.CaretColumn = newvalue;
                    }
                    break;
                case 'D': {
                        int newvalue = column - count;
                        if (newvalue < 0)
                            newvalue = 0;
                        _manipulator.CaretColumn = newvalue;
                    }
                    break;
            }
        }
#endif

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'A')]
        private void ProcessCursorUp(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            TerminalDocument doc = GetDocument();
            int column = _manipulator.CaretColumn;
            GLine lineUpdated = doc.UpdateCurrentLine(_manipulator);
            if (lineUpdated != null) {
                this.LogService.TextLogger.WriteLine(lineUpdated);
            }
            doc.CurrentLineNumber = Math.Max(Math.Max(doc.CurrentLineNumber - count, doc.TopLineNumber), doc.ScrollingTop);
            _manipulator.Load(doc.CurrentLine, column);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'B')]
        private void ProcessCursorDown(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            int column = _manipulator.CaretColumn;
            GLine lineUpdated = GetDocument().UpdateCurrentLine(_manipulator);
            if (lineUpdated != null) {
                this.LogService.TextLogger.WriteLine(lineUpdated);
            }
            GetDocument().CurrentLineNumber = (GetDocument().CurrentLineNumber + count);
            _manipulator.Load(GetDocument().CurrentLine, column);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'C')]
        private void ProcessCursorForward(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            int column = _manipulator.CaretColumn;
            int newvalue = column + count;
            if (newvalue >= GetDocument().TerminalWidth)
                newvalue = GetDocument().TerminalWidth - 1;
            _manipulator.CaretColumn = newvalue;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'D')]
        private void ProcessCursorBackward(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            int column = _manipulator.CaretColumn;
            int newvalue = column - count;
            if (newvalue < 0)
                newvalue = 0;
            _manipulator.CaretColumn = newvalue;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'E')]
        private void ProcessCursorNextLine(NumericParams p) {
            TerminalDocument doc = GetDocument();
            int count = p.GetNonZero(0, 1);
            int bottomLineNumber = doc.TopLineNumber + doc.TerminalHeight - 1;
            GLine lineUpdated = doc.UpdateCurrentLine(_manipulator);
            if (lineUpdated != null) {
                this.LogService.TextLogger.WriteLine(lineUpdated);
            }
            doc.CurrentLineNumber = Math.Min(doc.CurrentLineNumber + count, bottomLineNumber);
            _manipulator.Load(doc.CurrentLine, 0);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'F')]
        private void ProcessCursorPrecedingLine(NumericParams p) {
            TerminalDocument doc = GetDocument();
            int count = p.GetNonZero(0, 1);
            int topLineNumber = doc.TopLineNumber;
            GLine lineUpdated = doc.UpdateCurrentLine(_manipulator);
            if (lineUpdated != null) {
                this.LogService.TextLogger.WriteLine(lineUpdated);
            }
            doc.CurrentLineNumber = Math.Max(doc.CurrentLineNumber - count, topLineNumber);
            _manipulator.Load(doc.CurrentLine, 0);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'H')] // CUP
        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'f')] // HVP
        private void ProcessCursorPosition(NumericParams p) {
            int row = p.GetNonZero(0, 1);
            int col = p.GetNonZero(1, 1);

            if (_scrollRegionRelative && GetDocument().HasScrollingRegionTop) {
                row += GetDocument().ScrollingTopOffset;
            }

            row = Math.Min(row, GetDocument().TerminalHeight);
            col = Math.Min(col, GetDocument().TerminalWidth);

            ProcessCursorPosition(row, col);
        }

        [EscapeSequence(ControlCode.CSI, 'U')] // FIXME: undocumented?
        private void ProcessCursorPositionToBottomLeft() {
            ProcessCursorPosition(GetDocument().TerminalHeight, 1);
        }

        private void ProcessCursorPosition(int row, int col) {
            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = (GetDocument().TopLineNumber + row - 1);
            _manipulator.Load(GetDocument().CurrentLine, col - 1);
        }

        private void MoveCursorToHome() {
            int row = (_scrollRegionRelative && GetDocument().HasScrollingRegionTop) ? GetDocument().ScrollingTopOffset + 1 : 1;
            ProcessCursorPosition(row, 1);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'a')]
        private void ProcessCursorPositionRelative(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            int max = GetDocument().TerminalWidth - 1;
            if (_manipulator.CaretColumn < max) {
                _manipulator.CaretColumn = Math.Min(_manipulator.CaretColumn + n, max);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'J')]
        private void ProcessEraseInDisplay(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInDisplay(param, false);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'J')]
        private void ProcessSelectiveEraseInDisplay(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInDisplay(param, true);
        }

        private void DoEraseInDisplay(int param, bool selective) {
            TerminalDocument doc = GetDocument();
            int cur = doc.CurrentLineNumber;
            int top = doc.TopLineNumber;
            int bottom = top + doc.TerminalHeight;
            int col = _manipulator.CaretColumn;
            switch (param) {
                case 0: //erase below
                    {
                        if (col == 0 && cur == top)
                            goto ERASE_ALL;

                        if (selective) {
                            SelectiveEraseRight();
                        }
                        else {
                            EraseRight();
                        }
                        doc.UpdateCurrentLine(_manipulator);
                        doc.EnsureLine(bottom - 1);
                        doc.RemoveAfter(bottom);
                        doc.ClearRange(cur + 1, bottom, doc.CurrentDecoration, selective);
                        _manipulator.Load(doc.CurrentLine, col);
                    }
                    break;
                case 1: //erase above
                    {
                        if (col == doc.TerminalWidth - 1 && cur == bottom - 1)
                            goto ERASE_ALL;

                        if (selective) {
                            SelectiveEraseLeft();
                        }
                        else {
                            EraseLeft();
                        }
                        doc.UpdateCurrentLine(_manipulator);
                        doc.ClearRange(top, cur, doc.CurrentDecoration, selective);
                        _manipulator.Load(doc.CurrentLine, col);
                    }
                    break;
                case 2: //erase all
                ERASE_ALL: {
                        doc.ApplicationModeBackColor =
                            (doc.CurrentDecoration != null) ? doc.CurrentDecoration.GetBackColorSpec() : ColorSpec.Default;

                        doc.UpdateCurrentLine(_manipulator);
                        //if(_homePositionOnCSIJ2) { //SFUではこうなる
                        //	ProcessCursorPosition(1,1); 
                        //	col = 0;
                        //}
                        doc.EnsureLine(bottom - 1);
                        doc.RemoveAfter(bottom);
                        doc.ClearRange(top, bottom, doc.CurrentDecoration, selective);
                        _manipulator.Load(doc.CurrentLine, col);
                    }
                    break;
                case 3: //saved lines
                    // not implemented
                    break;
                default:
                    // ignore
                    break;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'z')]
        private void ProcessEraseRectangle(NumericParams p) {
            DoEraseRectangle(p, false);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', '{')]
        private void ProcessSelectiveEraseRectangle(NumericParams p) {
            DoEraseRectangle(p, true);
        }

        private void DoEraseRectangle(NumericParams p, bool selective) {
            RectArea rect = ReadRectAreaFromParameters(p, 0);
            if (rect == null) {
                return;
            }

            TerminalDocument doc = GetDocument();

            int caretColumn = _manipulator.CaretColumn;
            doc.UpdateCurrentLine(_manipulator);

            GLine line = doc.FindLineOrNull(doc.TopLineNumber + rect.Top - 1);

            for (int r = rect.Top; line != null && r <= rect.Bottom; r++, line = line.NextLine) {
                _manipulator.Load(line, 0);
                if (selective) {
                    _manipulator.FillSpaceSkipProtected(rect.Left - 1, rect.Right, doc.CurrentDecoration);
                }
                else {
                    _manipulator.FillSpace(rect.Left - 1, rect.Right, doc.CurrentDecoration);
                }
                _manipulator.ExportTo(line);
                doc.InvalidatedRegion.InvalidateLine(line.ID);
            }

            _manipulator.Load(doc.CurrentLine, caretColumn);
        }

        private RectArea ReadRectAreaFromParameters(NumericParams p, int index) {
            TerminalDocument doc = GetDocument();
            int top = p.GetNonZero(index, 1);
            int left = p.GetNonZero(index + 1, 1);
            int bottom = p.GetNonZero(index + 2, doc.TerminalHeight);
            int right = p.GetNonZero(index + 3, doc.TerminalWidth);

            if (top > doc.TerminalHeight || left > doc.TerminalWidth || bottom < top || right < left) {
                return null;
            }

            bottom = Math.Min(bottom, doc.TerminalHeight);
            right = Math.Min(right, doc.TerminalWidth);

            return new RectArea(
                    top: top,
                    left: left,
                    bottom: bottom,
                    right: right
                );
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'K')]
        private void ProcessEraseInLine(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInLine(param, false);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'K')]
        private void ProcessSelectiveEraseInLine(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInLine(param, true);
        }

        private void DoEraseInLine(int param, bool selective) {
            switch (param) {
                case 0: //erase right
                    if (selective) {
                        SelectiveEraseRight();
                    }
                    else {
                        EraseRight();
                    }
                    break;
                case 1: //erase left
                    if (selective) {
                        SelectiveEraseLeft();
                    }
                    else {
                        EraseLeft();
                    }
                    break;
                case 2: //erase all
                    if (selective) {
                        SelectiveEraseLine();
                    }
                    else {
                        EraseLine();
                    }
                    break;
                default:
                    break;
            }
        }

        private void EraseRight() {
            _manipulator.FillSpace(_manipulator.CaretColumn, _manipulator.BufferSize, GetDocument().CurrentDecoration);
        }

        private void SelectiveEraseRight() {
            _manipulator.FillSpaceSkipProtected(_manipulator.CaretColumn, _manipulator.BufferSize, GetDocument().CurrentDecoration);
        }

        private void EraseLeft() {
            _manipulator.FillSpace(0, _manipulator.CaretColumn + 1, GetDocument().CurrentDecoration);
        }

        private void SelectiveEraseLeft() {
            _manipulator.FillSpaceSkipProtected(0, _manipulator.CaretColumn + 1, GetDocument().CurrentDecoration);
        }

        private void EraseLine() {
            _manipulator.FillSpace(0, _manipulator.BufferSize, GetDocument().CurrentDecoration);
        }

        private void SelectiveEraseLine() {
            _manipulator.FillSpaceSkipProtected(0, _manipulator.BufferSize, GetDocument().CurrentDecoration);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '"', 'q')]
        private void ProcessProtectCharacter(NumericParams p) {
            int param = p.Get(0, 0);

            switch (param) {
                case 0:
                case 2:
                    GetDocument().CurrentDecoration = GetDocument().CurrentDecoration.GetCopyWithProtected(false);
                    break;
                case 1:
                    GetDocument().CurrentDecoration = GetDocument().CurrentDecoration.GetCopyWithProtected(true);
                    break;
                default:
                    break;
            }
        }

        [EscapeSequence(ControlCode.IND)]
        private void Index() {
            GetDocument().UpdateCurrentLine(_manipulator);
            int current = GetDocument().CurrentLineNumber;
            if (GetDocument().HasScrollingRegionBottom && current == GetDocument().ScrollingBottom)
                GetDocument().ScrollDown();
            else
                GetDocument().CurrentLineNumber = current + 1;
            _manipulator.Load(GetDocument().CurrentLine, _manipulator.CaretColumn);
        }

        [EscapeSequence(ControlCode.RI)]
        private void ReverseIndex() {
            GetDocument().UpdateCurrentLine(_manipulator);
            int current = GetDocument().CurrentLineNumber;
            if (current == GetDocument().ScrollingTop)
                GetDocument().ScrollUp();
            else
                GetDocument().CurrentLineNumber = current - 1;
            _manipulator.Load(GetDocument().CurrentLine, _manipulator.CaretColumn);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'r')]
        private void ProcessSetScrollingRegion(NumericParams p) {
            int height = GetDocument().TerminalHeight;
            int top = p.Get(0, -1);
            int bottom = p.Get(1, -1);

            int topOffset;
            if (top <= 0) {
                topOffset = -1; // top of the display area
            }
            else if (top > height) {
                topOffset = height - 1;
            }
            else {
                topOffset = top - 1;
            }

            int bottomOffset;
            if (bottom <= 0) {
                bottomOffset = -1;  // bottom of the display area
            }
            else if (bottom > height) {
                bottomOffset = height - 1;
            }
            else {
                bottomOffset = bottom - 1;
            }

            if (((topOffset == -1) ? 0 : topOffset) < ((bottomOffset == -1) ? height - 1 : bottomOffset)) {
                GetDocument().SetScrollingRegion(topOffset, bottomOffset);
            }

            MoveCursorToHome();
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'b')]
        private void ProcessRepeatPrecedingChar(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            UnicodeChar? ch = _manipulator.GetPreviousChar();
            if (ch.HasValue) {
                for (int i = 0; i < n; i++) {
                    ProcessNormalUnicodeChar(ch.Value);
                }
            }
        }

        [EscapeSequence(ControlCode.NEL)]
        private void ProcessNextLine() {
            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = (GetDocument().CurrentLineNumber + 1);
            _manipulator.Load(GetDocument().CurrentLine, 0);
        }

        [EscapeSequence(ControlCode.ESC, '=')]
        private void ProcessEnterAlternateKeypadMode() {
            ChangeMode(TerminalMode.Application);
        }

        [EscapeSequence(ControlCode.ESC, '>')]
        private void ProcessExitAlternateKeypadMode() {
            ChangeMode(TerminalMode.Normal);
        }

        protected override void ChangeMode(TerminalMode mode) {
            if (_terminalMode == mode)
                return;

            if (mode == TerminalMode.Normal) {
                GetDocument().ClearScrollingRegion();
                GetConnection().TerminalOutput.Resize(GetDocument().TerminalWidth, GetDocument().TerminalHeight); //たとえばemacs起動中にリサイズし、シェルへ戻るとシェルは新しいサイズを認識していない
                //RMBoxで確認されたことだが、無用に後方にドキュメントを広げてくる奴がいる。カーソルを123回後方へ、など。
                //場当たり的だが、ノーマルモードに戻る際に後ろの空行を削除することで対応する。
                GLine l = GetDocument().LastLine;
                while (l != null && l.DisplayLength == 0 && l.ID > GetDocument().CurrentLineNumber)
                    l = l.PrevLine;

                if (l != null)
                    l = l.NextLine;
                if (l != null)
                    GetDocument().RemoveAfter(l.ID);

                GetDocument().IsApplicationMode = false;
            }
            else {
                GetDocument().ApplicationModeBackColor = ColorSpec.Default;
                GetDocument().SetScrollingRegion(0, GetDocument().TerminalHeight - 1);
                GetDocument().IsApplicationMode = true;
            }

            GetDocument().InvalidateAll();

            _terminalMode = mode;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'h')]
        private void SetMode(NumericParams p) {
            DoSetMode(p, true);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'l')]
        private void ResetMode(NumericParams p) {
            DoSetMode(p, false);
        }

        private void DoSetMode(NumericParams p, bool set) {
            foreach (int param in p.EnumerateWithoutNull()) {
                switch (param) {
                    case 2: // Keyboard Action Mode
                        SetKeySendLocked(set);
                        break;
                    case 4: // Insert Mode
                        _insertMode = set;
                        break;
                    case 12: {	//local echo
                            ITerminalSettings settings = GetTerminalSettings();
                            bool value = !set;
                            _afterExitLockActions.Add(() => {
                                settings.BeginUpdate();
                                settings.LocalEcho = value;
                                settings.EndUpdate();
                            });
                        }
                        break;
                    case 20: // New Line Mode
                        _forceNewLine = set; // controls behavior of LF/FF/VT
                        SetNewLineOnEnterKey(set); // controls behavior of Enter key
                        break;
                }
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'p')]
        private void RequestMode(NumericParams p) {
            int param = p.Get(0, 65535);
            bool? status = GetANSIMode(param);
            string value = status.HasValue
                ? (status.Value
                    ? "1" // set
                    : "2" // reset
                )
                : "0"; // not recognized
            byte[] data = Encoding.ASCII.GetBytes("\u001b[" + param.ToInvariantString() + ";" + value + "$y");
            TransmitDirect(data);
        }

        private bool? GetANSIMode(int param) {
            switch (param) {
                case 2: // Keyboard Action Mode
                    return IsKeySendLocked();
                case 4: // Insert Mode
                    return _insertMode;
                case 12: //local echo
                    return !GetTerminalSettings().LocalEcho;
                case 20: // New Line Mode
                    return _forceNewLine;
                default:
                    return null;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'L')]
        private void ProcessInsertLines(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            TerminalDocument doc = GetDocument();
            if (!doc.IsCurrentLineInScrollingRegion) {
                return;
            }

            int bottom = doc.ScrollingBottom;

            doc.UpdateCurrentLine(_manipulator);
            int currentLineNumber = doc.CurrentLineNumber;
            doc.ScrollUp(currentLineNumber, bottom, d);
            doc.CurrentLineNumber = currentLineNumber;
            _manipulator.Load(doc.CurrentLine, 0);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'M')]
        private void ProcessDeleteLines(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            TerminalDocument doc = GetDocument();
            if (!doc.IsCurrentLineInScrollingRegion) {
                return;
            }

            int bottom = doc.ScrollingBottom;

            doc.UpdateCurrentLine(_manipulator);
            int currentLineNumber = doc.CurrentLineNumber;
            doc.ScrollDown(currentLineNumber, bottom, d);
            doc.CurrentLineNumber = currentLineNumber;
            _manipulator.Load(doc.CurrentLine, 0);
        }

#if NOTUSED
        private ProcessCharResult ProcessAfterOSC(string param, char code) {
            int semicolon = param.IndexOf(';');
            if (semicolon == -1)
                return ProcessCharResult.Unsupported;

            string ps = param.Substring(0, semicolon);
            string pt = param.Substring(semicolon + 1);
            if (ps == "0" || ps == "2") {
                IDynamicCaptionFormatter[] fmts = TerminalEmulatorPlugin.Instance.DynamicCaptionFormatter;
                TerminalDocument doc = GetDocument();

                if (fmts.Length > 0) {
                    ITerminalSettings settings = GetTerminalSettings();
                    string title = fmts[0].FormatCaptionUsingWindowTitle(GetConnection().Destination, settings, pt);
                    _afterExitLockActions.Add(new AfterExitLockDelegate(new CaptionChanger(GetTerminalSettings(), title).Do));
                }

                return ProcessCharResult.Processed;
            }
            else if (ps == "1")
                return ProcessCharResult.Processed; //Set Icon Nameというやつだが無視でよさそう
            else if (ps == "4") {
                // パレット変更
                //   形式: OSC 4 ; 色番号 ; 色指定 ST
                //     色番号: 0～255
                //     色指定: 以下の形式のどれか
                //       #rgb
                //       #rrggbb
                //       #rrrgggbbb
                //       #rrrrggggbbbb
                //       rgb:r/g/b
                //       rgb:rr/gg/bb
                //       rgb:rrr/ggg/bbb
                //       rgb:rrrr/gggg/bbbb
                //       他にも幾つか形式があるけれど、通常はこれで十分と思われる。
                //       他の形式は XParseColor(1) を参照
                //
                // 参考: http://ttssh2.sourceforge.jp/manual/ja/about/ctrlseq.html#OSC
                //
                while ((semicolon = pt.IndexOf(';')) != -1) {
                    string pv = pt.Substring(semicolon + 1);
                    int pn;
                    if (Int32.TryParse(pt.Substring(0, semicolon), out pn) && pn >= 0 && pn <= 255) {
                        if ((semicolon = pv.IndexOf(';')) != -1) {
                            pt = pv.Substring(semicolon + 1);
                            pv = pv.Substring(0, semicolon);
                        }
                        else {
                            pt = pv;
                        }
                        int r, g, b;
                        if (pv.StartsWith("#")) {
                            switch (pv.Length) {
                                case 4: // #rgb
                                    if (Int32.TryParse(pv.Substring(1, 1), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                        Int32.TryParse(pv.Substring(2, 1), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                        Int32.TryParse(pv.Substring(3, 1), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                        r <<= 4;
                                        g <<= 4;
                                        b <<= 4;
                                    }
                                    else {
                                        return ProcessCharResult.Unsupported;
                                    }
                                    break;
                                case 7: // #rrggbb
                                    if (Int32.TryParse(pv.Substring(1, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                        Int32.TryParse(pv.Substring(3, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                        Int32.TryParse(pv.Substring(5, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                    }
                                    else {
                                        return ProcessCharResult.Unsupported;
                                    }
                                    break;
                                case 10: // #rrrgggbbb
                                    if (Int32.TryParse(pv.Substring(1, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                        Int32.TryParse(pv.Substring(4, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                        Int32.TryParse(pv.Substring(7, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                        r >>= 4;
                                        g >>= 4;
                                        b >>= 4;
                                    }
                                    else {
                                        return ProcessCharResult.Unsupported;
                                    }
                                    break;
                                case 13: // #rrrrggggbbbb
                                    if (Int32.TryParse(pv.Substring(1, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                        Int32.TryParse(pv.Substring(5, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                        Int32.TryParse(pv.Substring(9, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                        r >>= 8;
                                        g >>= 8;
                                        b >>= 8;
                                    }
                                    else {
                                        return ProcessCharResult.Unsupported;
                                    }
                                    break;
                                default:
                                    return ProcessCharResult.Unsupported;
                            }
                        }
                        else if (pv.StartsWith("rgb:")) { // rgb:rr/gg/bb
                            string[] vals = pv.Substring(4).Split(new Char[] { '/' });
                            if (vals.Length == 3
                                && vals[0].Length == vals[1].Length
                                && vals[0].Length == vals[2].Length
                                && vals[0].Length > 0
                                && vals[0].Length <= 4
                                && Int32.TryParse(vals[0], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r)
                                && Int32.TryParse(vals[1], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g)
                                && Int32.TryParse(vals[2], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                switch (vals[0].Length) {
                                    case 1:
                                        r <<= 4;
                                        g <<= 4;
                                        b <<= 4;
                                        break;
                                    case 3:
                                        r >>= 4;
                                        g >>= 4;
                                        b >>= 4;
                                        break;
                                    case 4:
                                        r >>= 8;
                                        g >>= 8;
                                        b >>= 8;
                                        break;
                                }
                            }
                            else {
                                return ProcessCharResult.Unsupported;
                            }
                        }
                        else {
                            return ProcessCharResult.Unsupported;
                        }
                        GetRenderProfile().ESColorSet[pn] = new ESColor(Color.FromArgb(r, g, b), true);
                    }
                    else {
                        return ProcessCharResult.Unsupported;
                    }
                }
                return ProcessCharResult.Processed;
            }
            else
                return ProcessCharResult.Unsupported;
        }
#endif


        [EscapeSequence(ControlCode.OSC, EscapeSequenceParamType.Text, ControlCode.BEL)]
        [EscapeSequence(ControlCode.OSC, EscapeSequenceParamType.Text, ControlCode.ST)]
        private void ProcessOSC(string paramText) {
            OSCParams p;
            if (!OSCParams.Parse(paramText, out p)) {
                throw new UnknownEscapeSequenceException("invalid OSC format");
            }

            switch (p.GetCode()) {
                case 0:
                case 2:
                    OSCChangeWindowTitle(p);
                    return;

                case 1:
                    return;

                case 4:
                    OSCChangeColorPalette(p);
                    return;

                default:
                    throw new UnknownEscapeSequenceException("unsupported OSC code");
            }
        }

        private void OSCChangeWindowTitle(OSCParams p) {
            IDynamicCaptionFormatter[] fmts = TerminalEmulatorPlugin.Instance.DynamicCaptionFormatter;
            TerminalDocument doc = GetDocument();

            if (fmts.Length > 0) {
                ITerminalSettings settings = GetTerminalSettings();
                string title = fmts[0].FormatCaptionUsingWindowTitle(GetConnection().Destination, settings, p.GetText());
                _afterExitLockActions.Add(new AfterExitLockDelegate(new CaptionChanger(GetTerminalSettings(), title).Do));
            }
        }

        private void OSCChangeColorPalette(OSCParams p) {
            // パレット変更
            //   形式: OSC 4 ; 色番号 ; 色指定 ST
            //     色番号: 0～255
            //     色指定: 以下の形式のどれか
            //       #rgb
            //       #rrggbb
            //       #rrrgggbbb
            //       #rrrrggggbbbb
            //       rgb:r/g/b
            //       rgb:rr/gg/bb
            //       rgb:rrr/ggg/bbb
            //       rgb:rrrr/gggg/bbbb
            //       他にも幾つか形式があるけれど、通常はこれで十分と思われる。
            //       他の形式は XParseColor(1) を参照
            //
            // 参考: http://ttssh2.sourceforge.jp/manual/ja/about/ctrlseq.html#OSC
            //
            while (p.HasNextParam()) {
                int pn;
                if (!p.TryGetNextInteger(out pn)) {
                    throw new UnknownEscapeSequenceException("(OSC) invalid color number");
                }
                string pv;
                if (!p.TryGetNextText(out pv)) {
                    throw new UnknownEscapeSequenceException("(OSC) missing color spec");
                }
                if (pn < 0 || pn > 255) {
                    throw new UnknownEscapeSequenceException("(OSC) unsupported color number");
                }

                int r, g, b;
                if (pv.StartsWith("#")) {
                    switch (pv.Length) {
                        case 4: // #rgb
                            if (Int32.TryParse(pv.Substring(1, 1), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                Int32.TryParse(pv.Substring(2, 1), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                Int32.TryParse(pv.Substring(3, 1), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                r <<= 4;
                                g <<= 4;
                                b <<= 4;
                            }
                            else {
                                throw new UnknownEscapeSequenceException("(OSC) invalid color spec");
                            }
                            break;
                        case 7: // #rrggbb
                            if (Int32.TryParse(pv.Substring(1, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                Int32.TryParse(pv.Substring(3, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                Int32.TryParse(pv.Substring(5, 2), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                            }
                            else {
                                throw new UnknownEscapeSequenceException("(OSC) invalid color spec");
                            }
                            break;
                        case 10: // #rrrgggbbb
                            if (Int32.TryParse(pv.Substring(1, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                Int32.TryParse(pv.Substring(4, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                Int32.TryParse(pv.Substring(7, 3), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                r >>= 4;
                                g >>= 4;
                                b >>= 4;
                            }
                            else {
                                throw new UnknownEscapeSequenceException("(OSC) invalid color spec");
                            }
                            break;
                        case 13: // #rrrrggggbbbb
                            if (Int32.TryParse(pv.Substring(1, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r) &&
                                Int32.TryParse(pv.Substring(5, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g) &&
                                Int32.TryParse(pv.Substring(9, 4), NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                                r >>= 8;
                                g >>= 8;
                                b >>= 8;
                            }
                            else {
                                throw new UnknownEscapeSequenceException("(OSC) invalid color spec");
                            }
                            break;
                        default:
                            throw new UnknownEscapeSequenceException("(OSC) unsupported color spec format");
                    }
                }
                else if (pv.StartsWith("rgb:")) { // rgb:rr/gg/bb
                    string[] vals = pv.Substring(4).Split(new Char[] { '/' });
                    if (vals.Length == 3
                        && vals[0].Length == vals[1].Length
                        && vals[0].Length == vals[2].Length
                        && vals[0].Length > 0
                        && vals[0].Length <= 4
                        && Int32.TryParse(vals[0], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out r)
                        && Int32.TryParse(vals[1], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out g)
                        && Int32.TryParse(vals[2], NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out b)) {
                        switch (vals[0].Length) {
                            case 1:
                                r <<= 4;
                                g <<= 4;
                                b <<= 4;
                                break;
                            case 3:
                                r >>= 4;
                                g >>= 4;
                                b >>= 4;
                                break;
                            case 4:
                                r >>= 8;
                                g >>= 8;
                                b >>= 8;
                                break;
                        }
                    }
                    else {
                        throw new UnknownEscapeSequenceException("(OSC) invalid color spec");
                    }
                }
                else {
                    throw new UnknownEscapeSequenceException("(OSC) unsupported color spec format");
                }

                GetRenderProfile().ESColorSet[pn] = new ESColor(Color.FromArgb(r, g, b), true);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'm')]
        private void ProcessSGR(NumericParams p) {
            int state = 0, target = 0, r = 0, g = 0, b = 0;
            TextDecoration dec = GetDocument().CurrentDecoration;
            foreach (int code in p.EnumerateWithDefault(0)) {
                if (state != 0) {
                    switch (state) {
                        case 1:
                            if (code == 5) { // select indexed color
                                state = 2;
                            }
                            else if (code == 2) { // select RGB color
                                state = 3;  // read R value
                            }
                            else {
                                Debug.WriteLine("Invalid SGR code : {0}", code);
                                goto Apply;
                            }
                            break;
                        case 2:
                            if (code < 256) {
                                if (target == 3) {
                                    dec = SelectForeColor(dec, code);
                                }
                                else if (target == 4) {
                                    dec = SelectBackgroundColor(dec, code);
                                }
                            }
                            state = 0;
                            target = 0;
                            break;
                        case 3:
                            if (code < 256) {
                                r = code;
                                state = 4;  // read G value
                            }
                            else {
                                Debug.WriteLine("Invalid SGR R value : {0}", code);
                                goto Apply;
                            }
                            break;
                        case 4:
                            if (code < 256) {
                                g = code;
                                state = 5;  // read B value
                            }
                            else {
                                Debug.WriteLine("Invalid SGR G value : {0}", code);
                                goto Apply;
                            }
                            break;
                        case 5:
                            if (code < 256) {
                                b = code;
                                if (target == 3) {
                                    dec = SetForeColorByRGB(dec, r, g, b);
                                }
                                else if (target == 4) {
                                    dec = SetBackColorByRGB(dec, r, g, b);
                                }
                                state = 0;
                                target = 0;
                            }
                            else {
                                Debug.WriteLine("Invalid SGR B value : {0}", code);
                                goto Apply;
                            }
                            break;
                    }
                }
                else {
                    switch (code) {
                        case 8: // concealed characters (ECMA-48,VT300)
                            dec = dec.GetCopyWithHidden(true);
                            break;
                        case 28: // revealed characters (ECMA-48)
                            dec = dec.GetCopyWithHidden(false);
                            break;
                        case 38: // Set foreground color (XTERM,ISO-8613-3)
                            state = 1;  // start reading subsequent values
                            target = 3; // set foreground color
                            break;
                        case 48: // Set background color (XTERM,ISO-8613-3)
                            state = 1;  // start reading subsequent values
                            target = 4; // set background color
                            break;
                        case 90: // Set foreground color to Black (XTERM)
                        case 91: // Set foreground color to Red (XTERM)
                        case 92: // Set foreground color to Green (XTERM)
                        case 93: // Set foreground color to Yellow (XTERM)
                        case 94: // Set foreground color to Blue (XTERM)
                        case 95: // Set foreground color to Magenta (XTERM)
                        case 96: // Set foreground color to Cyan (XTERM)
                        case 97: // Set foreground color to White (XTERM)
                            dec = SelectForeColor(dec, code - 90 + 8);
                            break;
                        case 100: // Set background color to Black (XTERM)
                        case 101: // Set background color to Red (XTERM)
                        case 102: // Set background color to Green (XTERM)
                        case 103: // Set background color to Yellow (XTERM)
                        case 104: // Set background color to Blue (XTERM)
                        case 105: // Set background color to Magenta (XTERM)
                        case 106: // Set background color to Cyan (XTERM)
                        case 107: // Set background color to White (XTERM)
                            dec = SelectBackgroundColor(dec, code - 100 + 8);
                            break;
                        default:
                            ProcessSGRParameterANSI(code, ref dec);
                            break;
                    }
                }
            }
        Apply:
            GetDocument().CurrentDecoration = dec;
        }

        private TextDecoration SetForeColorByRGB(TextDecoration dec, int r, int g, int b) {
            return dec.GetCopyWithForeColor(new ColorSpec(Color.FromArgb(r, g, b)));
        }

        private TextDecoration SetBackColorByRGB(TextDecoration dec, int r, int g, int b) {
            return dec.GetCopyWithBackColor(new ColorSpec(Color.FromArgb(r, g, b)));
        }

        private TextDecoration SelectForeColor(TextDecoration dec, int index) {
            return dec.GetCopyWithForeColor(new ColorSpec(index));
        }

        private TextDecoration SelectBackgroundColor(TextDecoration dec, int index) {
            return dec.GetCopyWithBackColor(new ColorSpec(index));
        }

        private void ProcessSGRParameterANSI(int code, ref TextDecoration dec) {
            switch (code) {
                case 0: // default rendition (implementation-defined) (ECMA-48,VT100,VT220)
                    dec = TextDecoration.Default;
                    break;
                case 1: // bold or increased intensity (ECMA-48,VT100,VT220)
                    dec = dec.GetCopyWithBold(true);
                    break;
                case 2: // faint, decreased intensity or second colour (ECMA-48)
                    break;
                case 3: // italicized (ECMA-48)
                    break;
                case 4: // singly underlined (ECMA-48,VT100,VT220)
                    dec = dec.GetCopyWithUnderline(true);
                    break;
                case 5: // slowly blinking (ECMA-48,VT100,VT220)
                case 6: // rapidly blinking (ECMA-48)
                    dec = dec.GetCopyWithBlink(true);
                    break;
                case 7: // negative image (ECMA-48,VT100,VT220)
                    dec = dec.GetCopyWithInverted(true);
                    break;
                case 8: // concealed characters (ECMA-48,VT300)
                case 9: // crossed-out (ECMA-48)
                case 10: // primary (default) font (ECMA-48)
                case 11: // first alternative font (ECMA-48)
                case 12: // second alternative font (ECMA-48)
                case 13: // third alternative font (ECMA-48)
                case 14: // fourth alternative font (ECMA-48)
                case 15: // fifth alternative font (ECMA-48)
                case 16: // sixth alternative font (ECMA-48)
                case 17: // seventh alternative font (ECMA-48)
                case 18: // eighth alternative font (ECMA-48)
                case 19: // ninth alternative font (ECMA-48)
                case 20: // Fraktur (Gothic) (ECMA-48)
                case 21: // doubly underlined (ECMA-48)
                    break;
                case 22: // normal colour or normal intensity (neither bold nor faint) (ECMA-48,VT220,VT300)
                    dec = TextDecoration.Default;
                    break;
                case 23: // not italicized, not fraktur (ECMA-48)
                    break;
                case 24: // not underlined (neither singly nor doubly) (ECMA-48,VT220,VT300)
                    dec = dec.GetCopyWithUnderline(false);
                    break;
                case 25: // steady (not blinking) (ECMA-48,VT220,VT300)
                    dec = dec.GetCopyWithBlink(false);
                    break;
                case 26: // reserved (ECMA-48)
                    break;
                case 27: // positive image (ECMA-48,VT220,VT300)
                    dec = dec.GetCopyWithInverted(false);
                    break;
                case 28: // revealed characters (ECMA-48)
                case 29: // not crossed out (ECMA-48)
                    break;
                case 30: // black display (ECMA-48)
                case 31: // red display (ECMA-48)
                case 32: // green display (ECMA-48)
                case 33: // yellow display (ECMA-48)
                case 34: // blue display (ECMA-48)
                case 35: // magenta display (ECMA-48)
                case 36: // cyan display (ECMA-48)
                case 37: // white display (ECMA-48)
                    dec = SelectForeColor(dec, code - 30);
                    break;
                case 38: // reserved (ECMA-48)
                    break;
                case 39: // default display colour (implementation-defined) (ECMA-48)
                    dec = dec.GetCopyWithForeColor(ColorSpec.Default);
                    break;
                case 40: // black background (ECMA-48)
                case 41: // red background (ECMA-48)
                case 42: // green background (ECMA-48)
                case 43: // yellow background (ECMA-48)
                case 44: // blue background (ECMA-48)
                case 45: // magenta background (ECMA-48)
                case 46: // cyan background (ECMA-48)
                case 47: // white background (ECMA-48)
                    dec = SelectBackgroundColor(dec, code - 40);
                    break;
                case 48: // reserved (ECMA-48)
                    break;
                case 49: // default background colour (implementation-defined) (ECMA-48)
                    dec = dec.GetCopyWithBackColor(ColorSpec.Default);
                    break;
                case 50: // reserved (ECMA-48)
                case 51: // framed (ECMA-48)
                case 52: // encircled (ECMA-48)
                case 53: // overlined (ECMA-48)
                case 54: // not framed, not encircled (ECMA-48)
                case 55: // not overlined (ECMA-48)
                case 56: // reserved (ECMA-48)
                case 57: // reserved (ECMA-48)
                case 58: // reserved (ECMA-48)
                case 59: // reserved (ECMA-48)
                case 60: // ideogram underline or right side line (ECMA-48)
                case 61: // ideogram double underline or double line on the right side (ECMA-48)
                case 62: // ideogram overline or left side line (ECMA-48)
                case 63: // ideogram double overline or double line on the left side (ECMA-48)
                case 64: // ideogram stress marking (ECMA-48)
                case 65: // cancels the effect of the rendition aspects established by parameter values 60 to 64 (ECMA-48)
                    break;
                default:
                    // other values are ignored without notification to the user
                    Debug.WriteLine("unknown SGR code (ANSI) : {0}", code);
                    break;
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'h')]
        private void ProcessDECSET(NumericParams p) {
            DoDECSETMultiple(p, true);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'l')]
        private void ProcessDECRST(NumericParams p) {
            DoDECSETMultiple(p, false);
        }

        private void DoDECSETMultiple(NumericParams p, bool set) {
            foreach (int param in p.EnumerateWithoutNull()) {
                DoDECSET(param, set);
            }
        }

        private void DoDECSET(int param, bool set) {
            switch (param) {
                case 1:
                    ChangeCursorKeyMode(set ? TerminalMode.Application : TerminalMode.Normal);
                    break;
                case 3:	//132 Column Mode
                    break;
                case 4:	//Smooth Scroll
                    break;
                case 5:
                    SetReverseVideo(set);
                    break;
                case 6:	//Origin Mode
                    _scrollRegionRelative = set;
                    break;
                case 7:
                    _wrapAroundMode = set;
                    break;
                case 25: // Show/Hide cursor
                    SetHideCaret(!set);
                    break;
                case 47:
                    if (set)
                        SwitchBuffer(true);
                    else
                        SwitchBuffer(false);
                    break;
                case 66: // Numeric keypad mode
                    ChangeMode(set ? TerminalMode.Application : TerminalMode.Normal);
                    break;
                case 1000: // DEC VT200 compatible: Send button press and release event with mouse position.
                    ResetMouseTracking((set) ? MouseTrackingState.Normal : MouseTrackingState.Off);
                    break;
                case 1001: // DEC VT200 highlight tracking
                    // Not supported
                    ResetMouseTracking(MouseTrackingState.Off);
                    break;
                case 1002: // Button-event tracking: Send button press, release, and drag event.
                    ResetMouseTracking((set) ? MouseTrackingState.Drag : MouseTrackingState.Off);
                    break;
                case 1003: // Any-event tracking: Send button press, release, and motion.
                    ResetMouseTracking((set) ? MouseTrackingState.Any : MouseTrackingState.Off);
                    break;
                case 1004: // Send FocusIn/FocusOut events
                    _focusReportingMode = set;
                    break;
                case 1005: // Enable UTF8 Mouse Mode
                    if (set) {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Utf8;
                    }
                    else {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
                    }
                    break;
                case 1006: // Enable SGR Extended Mouse Mode
                    if (set) {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Sgr;
                    }
                    else {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
                    }
                    break;
                case 1015: // Enable UTF8 Extended Mouse Mode
                    if (set) {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Urxvt;
                    }
                    else {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
                    }
                    break;
                case 1034:	// Input 8 bits
                    break;
                case 1047:	//Alternate Buffer
                    if (set) {
                        SwitchBuffer(true);
                        // XTerm doesn't clear screen.
                    }
                    else {
                        ClearScreen();
                        SwitchBuffer(false);
                    }
                    break;
                case 1048:	//Save/Restore Cursor
                    if (set)
                        SaveCursor();
                    else
                        RestoreCursor();
                    break;
                case 1049:	//Save/Restore Cursor and Alternate Buffer
                    if (set) {
                        SaveCursor();
                        SwitchBuffer(true);
                        ClearScreen();
                    }
                    else {
                        // XTerm doesn't clear screen for enabling copy/paste from the alternate buffer.
                        // But we need ClearScreen for emulating the buffer-switch.
                        ClearScreen();
                        SwitchBuffer(false);
                        RestoreCursor();
                    }
                    break;
                case 2004:    // Set/Reset bracketed paste mode
                    _bracketedPasteMode = set;
                    break;
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, '$', 'p')]
        private void RequestDECMode(NumericParams p) {
            int param = p.Get(0, 65535);
            bool? status = GetDECMode(param);
            string value = status.HasValue
                ? (status.Value
                    ? "1" // set
                    : "2" // reset
                )
                : "0"; // not recognized
            byte[] data = Encoding.ASCII.GetBytes("\u001b[?" + param.ToInvariantString() + ";" + value + "$y");
            TransmitDirect(data);
        }

        private bool? GetDECMode(int param) {
            switch (param) {
                case 1:
                    return CursorKeyMode == TerminalMode.Application;
                case 5:
                    return ReverseVideo;
                case 6:
                    return _scrollRegionRelative;
                case 7:
                    return _wrapAroundMode;
                case 25:
                    return !IsCaretHidden();
                case 47:
                case 1047:
                case 1049:
                    return _isAlternateBuffer;
                case 66:
                    return TerminalMode == TerminalMode.Application;
                case 1000:
                    return _mouseTrackingState == MouseTrackingState.Normal;
                case 1001:
                    return false;
                case 1002:
                    return _mouseTrackingState == MouseTrackingState.Drag;
                case 1003:
                    return _mouseTrackingState == MouseTrackingState.Any;
                case 1004:
                    return _focusReportingMode;
                case 1005:
                    return _mouseTrackingProtocol == MouseTrackingProtocol.Utf8;
                case 1006:
                    return _mouseTrackingProtocol == MouseTrackingProtocol.Sgr;
                case 1015:
                    return _mouseTrackingProtocol == MouseTrackingProtocol.Urxvt;
                case 1048:
                    return true;
                case 2004:
                    return _bracketedPasteMode;
                default:
                    return null;
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 's')]
        private void ProcessSaveDECSET(NumericParams p) {
            foreach (int param in p.EnumerateWithoutNull()) {
                bool? status = GetDECMode(param);
                if (status.HasValue) {
                    _savedDecModes[param] = status.Value;
                }
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'r')]
        private void ProcessRestoreDECSET(NumericParams p) {
            foreach (int param in p.EnumerateWithoutNull()) {
                bool status;
                if (_savedDecModes.TryGetValue(param, out status)) {
                    DoDECSET(param, status);
                }
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'r')]
        private void ProcessChangeAttributesRect(NumericParams p) {
            RectArea rect = ReadRectAreaFromParameters(p, 0);
            if (rect == null) {
                return;
            }

            AttributeModifications mod = new AttributeModifications();

            for (int paramIndex = 4; paramIndex < p.Length; paramIndex++) {
                int attr = p.Get(paramIndex, -1);
                switch (attr) {
                    case 0:
                        mod.Bold = false;
                        mod.Underline = false;
                        mod.Blink = false;
                        mod.Inverted = false;
                        break;
                    case 1:
                        mod.Bold = true;
                        break;
                    case 4:
                        mod.Underline = true;
                        break;
                    case 5:
                        mod.Blink = true;
                        break;
                    case 7:
                        mod.Inverted = true;
                        break;
                    case 22:
                        mod.Bold = false;
                        break;
                    case 24:
                        mod.Underline = false;
                        break;
                    case 25:
                        mod.Blink = false;
                        break;
                    case 27:
                        mod.Inverted = false;
                        break;
                }
            }

            if (mod.IsEmpty) {
                return;
            }

            TerminalDocument doc = GetDocument();

            int saveLineNumber = doc.CurrentLineNumber;
            int saveCaretColumn = _manipulator.CaretColumn;
            doc.UpdateCurrentLine(_manipulator);

            int rectTopLineNumber = doc.TopLineNumber + rect.Top - 1;
            int rectBottomLineNumber = doc.TopLineNumber + rect.Bottom - 1;

            doc.EnsureLine(rectBottomLineNumber);
            GLine l = doc.FindLineOrEdge(rectTopLineNumber);
            while (l != null && l.ID <= rectBottomLineNumber) {
                _manipulator.Load(l, 0);
                _manipulator.ModifyAttributes(rect.Left - 1, rect.Right, mod);
                _manipulator.ExportTo(l);
                doc.InvalidatedRegion.InvalidateLine(l.ID);
                l = l.NextLine;
            }
            doc.CurrentLineNumber = saveLineNumber;
            _manipulator.Load(doc.CurrentLine, saveCaretColumn);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 't')]
        private void ProcessReverseAttributesRect(NumericParams p) {
            RectArea rect = ReadRectAreaFromParameters(p, 0);
            if (rect == null) {
                return;
            }

            AttributeModifications mod = new AttributeModifications();

            for (int paramIndex = 4; paramIndex < p.Length; paramIndex++) {
                int attr = p.Get(paramIndex, -1);
                switch (attr) {
                    case 0:
                        mod.Bold = true;
                        mod.Underline = true;
                        mod.Blink = true;
                        mod.Inverted = true;
                        break;
                    case 1:
                        mod.Bold = true;
                        break;
                    case 4:
                        mod.Underline = true;
                        break;
                    case 5:
                        mod.Blink = true;
                        break;
                    case 7:
                        mod.Inverted = true;
                        break;
                }
            }

            if (mod.IsEmpty) {
                return;
            }

            TerminalDocument doc = GetDocument();

            int saveLineNumber = doc.CurrentLineNumber;
            int saveCaretColumn = _manipulator.CaretColumn;
            doc.UpdateCurrentLine(_manipulator);

            int rectTopLineNumber = doc.TopLineNumber + rect.Top - 1;
            int rectBottomLineNumber = doc.TopLineNumber + rect.Bottom - 1;

            doc.EnsureLine(rectBottomLineNumber);
            GLine l = doc.FindLineOrEdge(rectTopLineNumber);
            while (l != null && l.ID <= rectBottomLineNumber) {
                _manipulator.Load(l, 0);
                _manipulator.ReverseAttributes(rect.Left - 1, rect.Right, mod);
                _manipulator.ExportTo(l);
                doc.InvalidatedRegion.InvalidateLine(l.ID);
                l = l.NextLine;
            }
            doc.CurrentLineNumber = saveLineNumber;
            _manipulator.Load(doc.CurrentLine, saveCaretColumn);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'v')]
        private void ProcessCopyRect(NumericParams p) {
            RectArea srcRect = ReadRectAreaFromParameters(p, 0);
            if (srcRect == null) {
                return;
            }

            // int srcPage = p.Get(4, 1); // ignored
            int destTop = p.GetNonZero(5, 1);
            int destLeft = p.GetNonZero(6, 1);
            // int destPage = p.GetNonZero(7, 1); // ignored

            TerminalDocument doc = GetDocument();
            if (destTop > doc.TerminalHeight || destLeft > doc.TerminalWidth || (destTop == srcRect.Top && destLeft == srcRect.Left)) {
                return;
            }

            int saveLineNumber = doc.CurrentLineNumber;
            int saveCaretColumn = _manipulator.CaretColumn;
            doc.UpdateCurrentLine(_manipulator);

            doc.EnsureLine(doc.TopLineNumber + srcRect.Bottom - 1);

            GLine[] copy = new GLine[srcRect.Bottom - srcRect.Top + 1];
            {
                int rectTopLineNumber = doc.TopLineNumber + srcRect.Top - 1;
                GLine l = doc.FindLineOrEdge(rectTopLineNumber);
                while (l != null) {
                    int offset = l.ID - rectTopLineNumber;
                    if (offset >= 0 && offset < copy.Length) {
                        copy[offset] = l.Clone();
                    }
                    l = l.NextLine;
                }
            }

            int destTopLineNumber = doc.TopLineNumber + destTop - 1;
            int destBottomLimit = doc.TopLineNumber + Math.Min(destTop + srcRect.Bottom - srcRect.Top, doc.TerminalHeight) - 1;

            doc.EnsureLine(destBottomLimit);

            GLine destLine = doc.FindLineOrEdge(destTopLineNumber);
            GLineManipulator srcManipurator = new GLineManipulator();
            while (destLine != null && destLine.ID <= destBottomLimit) {
                int offset = destLine.ID - destTopLineNumber;
                if (offset >= 0 && offset < copy.Length) {
                    if (copy[offset] != null) {
                        srcManipurator.Load(copy[offset], 0);
                        _manipulator.Load(destLine, 0);
                        _manipulator.ExpandBuffer(doc.TerminalWidth);
                        _manipulator.CopyFrom(srcManipurator, srcRect.Left - 1, srcRect.Right, destLeft - 1);
                        _manipulator.ExportTo(destLine);
                        doc.InvalidatedRegion.InvalidateLine(destLine.ID);
                    }
                }
                destLine = destLine.NextLine;
            }

            doc.CurrentLineNumber = saveLineNumber;
            _manipulator.Load(doc.CurrentLine, saveCaretColumn);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'S')]
        private void ProcessQueryGraphics(NumericParams p) {
            if (p.Length < 2) {
                return;
            }

            int item = p.Get(0, 0);
            int action = p.Get(1, 0);

            // TODO

            int result = 1; // item error

            byte[] data = Encoding.ASCII.GetBytes("\u001b[?" + item.ToInvariantString() + ";" + result.ToInvariantString() + "S");
            TransmitDirect(data);
        }

        private void ResetMouseTracking(MouseTrackingState newState) {
            if (newState != MouseTrackingState.Off) {
                if (_mouseTrackingState == MouseTrackingState.Off) {
                    SetDocumentCursor(Cursors.Arrow);
                }
            }
            else {
                if (_mouseTrackingState != MouseTrackingState.Off) {
                    ResetDocumentCursor();
                }
            }
            _mouseTrackingState = newState;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'd')]
        private void ProcessLinePositionAbsolute(NumericParams p) {
            int row = p.GetNonZero(0, 1);
            row = Math.Min(row, GetDocument().TerminalHeight);
            int col = _manipulator.CaretColumn;

            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = GetDocument().TopLineNumber + row - 1;
            _manipulator.Load(GetDocument().CurrentLine, col);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'e')]
        private void ProcessLinePositionRelative(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            int col = _manipulator.CaretColumn;

            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = Math.Min(GetDocument().CurrentLineNumber + n, GetDocument().TopLineNumber + GetDocument().TerminalHeight - 1);
            _manipulator.Load(GetDocument().CurrentLine, col);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'G')]
        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '`')]
        private void ProcessLineColumnAbsolute(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            n = Math.Min(n, GetDocument().TerminalWidth);
            _manipulator.CaretColumn = n - 1;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'X')]
        private void ProcessEraseChars(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            int s = _manipulator.CaretColumn;
            for (int i = 0; i < n; i++) {
                _manipulator.PutChar(UnicodeChar.ASCII_SPACE, GetDocument().CurrentDecoration);
                if (_manipulator.CaretColumn >= _manipulator.BufferSize)
                    break;
            }
            _manipulator.CaretColumn = s;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'P')]
        private void ProcessDeleteChars(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            _manipulator.DeleteChars(_manipulator.CaretColumn, n, GetDocument().CurrentDecoration);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '@')]
        private void ProcessInsertBlankCharacters(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            _manipulator.InsertBlanks(_manipulator.CaretColumn, n, GetDocument().CurrentDecoration);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', '@')]
        private void ProcessShiftLeft(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            ShiftScreen(-n);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 'A')]
        private void ProcessShiftRight(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            ShiftScreen(n);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'S')]
        private void ProcessScrollUp(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            TerminalDocument doc = GetDocument();

            int caretColumn = _manipulator.CaretColumn;
            int currentLilneNumber = doc.CurrentLineNumber;
            doc.UpdateCurrentLine(_manipulator);

            if (!doc.HasScrollingRegionTop && !doc.HasScrollingRegionBottom) {
                doc.CurrentLineNumber += d;
                doc.SetTopLineNumber(doc.TopLineNumber + d);
            }
            else {
                doc.ScrollDown(doc.ScrollingTop, doc.ScrollingBottom, d); // TerminalDocument's "Scroll-Down" means XTerm's "Scroll-Up"
                doc.CurrentLineNumber = currentLilneNumber;
            }

            _manipulator.Load(doc.CurrentLine, caretColumn);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'T')]
        private void ProcessScrollDown(NumericParams p) {
            if (p.Length > 1) {
                // ignore highlight tracking information
                // CSI <func> ; <startx> ; <starty> ; <firstrow> ; <lastrow> T
                return;
            }

            int d = p.GetNonZero(0, 1);

            TerminalDocument doc = GetDocument();
            int caret_col = _manipulator.CaretColumn;
            int currentLilneNumber = doc.CurrentLineNumber;
            doc.UpdateCurrentLine(_manipulator);
            doc.ScrollUp(doc.ScrollingTop, doc.ScrollingBottom, d); // TerminalDocument's "Scroll-Down" means XTerm's "Scroll-Up"
            doc.CurrentLineNumber = currentLilneNumber;
            _manipulator.Load(doc.CurrentLine, caret_col);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'I')]
        private void ProcessForwardTab(NumericParams p) {
            int n = p.GetNonZero(0, 1);

            int t = _manipulator.CaretColumn;
            for (int i = 0; i < n; i++)
                t = GetNextTabStop(t);
            if (t >= GetDocument().TerminalWidth)
                t = GetDocument().TerminalWidth - 1;
            _manipulator.CaretColumn = t;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'Z')]
        private void ProcessBackwardTab(NumericParams p) {
            int n = p.GetNonZero(0, 1);

            int t = _manipulator.CaretColumn;
            for (int i = 0; i < n; i++)
                t = GetPrevTabStop(t);
            if (t < 0)
                t = 0;
            _manipulator.CaretColumn = t;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'g')]
        private void ProcessTabClear(NumericParams p) {
            int param = p.Get(0, 0);
            if (param == 0)
                SetTabStop(_manipulator.CaretColumn, false);
            else if (param == 3)
                ClearAllTabStop();
        }

        private void InitTabStops() {
            for (int i = 0; i < _tabStops.Length; i++) {
                _tabStops[i] = (i % 8) == 0;
            }
        }
        private void EnsureTabStops(int length) {
            if (length >= _tabStops.Length) {
                bool[] newarray = new bool[Math.Max(length, _tabStops.Length * 2)];
                Array.Copy(_tabStops, 0, newarray, 0, _tabStops.Length);
                for (int i = _tabStops.Length; i < newarray.Length; i++) {
                    newarray[i] = (i % 8) == 0;
                }
                _tabStops = newarray;
            }
        }

        [EscapeSequence(ControlCode.HTS)]
        private void ProcessHTS() {
            SetTabStop(_manipulator.CaretColumn, true);
        }

        private void SetTabStop(int index, bool value) {
            EnsureTabStops(index + 1);
            _tabStops[index] = value;
        }
        private void ClearAllTabStop() {
            for (int i = 0; i < _tabStops.Length; i++) {
                _tabStops[i] = false;
            }
        }

        private int GetNextTabStop(int start) {
            EnsureTabStops(Math.Max(start + 1, GetDocument().TerminalWidth));

            int index = start + 1;
            while (index < GetDocument().TerminalWidth) {
                if (_tabStops[index])
                    return index;
                index++;
            }
            return GetDocument().TerminalWidth - 1;
        }

        //これはvt100にはないのでoverrideしない
        private int GetPrevTabStop(int start) {
            EnsureTabStops(start + 1);

            int index = start - 1;
            while (index > 0) {
                if (_tabStops[index])
                    return index;
                index--;
            }
            return 0;
        }

        private void SwitchBuffer(bool toAlternate) {
            if (_isAlternateBuffer != toAlternate) {
                SaveScreen(toAlternate ? 0 : 1);
                RestoreScreen(toAlternate ? 1 : 0);
                _isAlternateBuffer = toAlternate;
            }
        }

        private void SaveScreen(int sw) {
            List<GLine> lines = new List<GLine>();
            GLine l = GetDocument().TopLine;
            int m = l.ID + GetDocument().TerminalHeight;
            while (l != null && l.ID < m) {
                lines.Add(l.Clone());
                l = l.NextLine;
            }
            _savedScreen[sw] = lines;
        }

        private void RestoreScreen(int sw) {
            if (_savedScreen[sw] == null) {
                ClearScreen();	// emulate new buffer
                return;
            }
            TerminalDocument doc = GetDocument();
            int w = doc.TerminalWidth;
            int m = doc.TerminalHeight;
            GLine t = doc.TopLine;
            foreach (GLine l in _savedScreen[sw]) {
                l.ExpandBuffer(w);
                if (t == null)
                    doc.AddLine(l);
                else {
                    doc.Replace(t, l);
                    t = l.NextLine;
                }
                if (--m == 0)
                    break;
            }
        }

        private void ClearScreen() {
            DoEraseInDisplay(2, false);
        }

        [EscapeSequence(ControlCode.ESC, '7')]
        private void ProcessDECSC() {
            SaveCursor();
        }

        private void SaveCursor() {
            int sw = _isAlternateBuffer ? 1 : 0;
            _savedCursor[sw] = CreateSavedCursor();
        }

        private SavedCursor CreateSavedCursor() {
            int row = GetDocument().CurrentLineNumber - GetDocument().TopLineNumber;
            int col = _manipulator.CaretColumn;
            return new SavedCursor(
                    row: row,
                    col: col,
                    decoration: GetDocument().CurrentDecoration,
                    wrapAroundMode: _wrapAroundMode,
                    scrollRegionRelative: _scrollRegionRelative
                );
        }

        [EscapeSequence(ControlCode.ESC, '8')]
        private void ProcessDECRC() {
            RestoreCursor();
        }

        private void RestoreCursor() {
            int sw = _isAlternateBuffer ? 1 : 0;
            RestoreCursorInternal(_savedCursor[sw]);
        }

        private void RestoreCursorInternal(SavedCursor saved) {
            if (saved == null) {
                saved = new SavedCursor(
                    row: 0,
                    col: 0,
                    decoration: TextDecoration.Default,
                    wrapAroundMode: true,
                    scrollRegionRelative: false
                );
            }

            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = GetDocument().TopLineNumber + saved.Row;
            _manipulator.Load(GetDocument().CurrentLine, saved.Col);

            GetDocument().CurrentDecoration = saved.Decoration;

            _wrapAroundMode = saved.WrapAroundMode;

            _scrollRegionRelative = saved.ScrollRegionRelative;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 's')]
        private void ProcessSCOSCOrDECSLRM(NumericParams p) {
            if (p.Length == 0) {
                ProcessSCOSC();
            }
            else {
                // DECSLRM (not supported)
                Ignore();
            }
        }

        private void ProcessSCOSC() {
            // Note: DECLRMM is currently unsupported, but if DECLRMM was enabled, this method should do nothing.
            _savedCursorSCO = CreateSavedCursor();
        }

        [EscapeSequence(ControlCode.CSI, 'u')]
        private void ProcessSCORC() {
            RestoreCursorInternal(_savedCursorSCO);
        }

        //画面の反転
        private void SetReverseVideo(bool reverse) {
            if (reverse == _reverseVideo)
                return;

            _reverseVideo = reverse;
            GetDocument().InvalidateAll();
        }

        [EscapeSequence(ControlCode.CSI, '!', 'p')]
        private void SoftTerminalReset() {
            SoftReset();
        }

        internal override byte[] SequenceKeyData(Keys modifier, Keys key) {
            if ((int)Keys.F1 <= (int)key && (int)key <= (int)Keys.F12) {
                return XtermFunctionKey(modifier, key);
            }
            else if (GUtil.IsCursorKey(key)) {
                byte[] data = ModifyCursorKey(modifier, key);
                if (data != null)
                    return data;
                return VT100CursorKey(modifier, key);
            }
            else {
                byte[] r = new byte[4];
                r[0] = 0x1B;
                r[1] = (byte)'[';
                r[3] = (byte)'~';
                //このあたりはxtermでは割と違うようだ
                if (key == Keys.Insert)
                    r[2] = (byte)'2';
                else if (key == Keys.Home)
                    r[2] = (byte)'7';
                else if (key == Keys.PageUp)
                    r[2] = (byte)'5';
                else if (key == Keys.Delete)
                    r[2] = (byte)'3';
                else if (key == Keys.End)
                    r[2] = (byte)'8';
                else if (key == Keys.PageDown)
                    r[2] = (byte)'6';
                else
                    throw new ArgumentException(String.Format("unknown key: {0}", key));
                return r;
            }
        }

        private byte[] XtermFunctionKey(Keys modifier, Keys key) {
            int m = 1;
            if ((modifier & Keys.Shift) != Keys.None) {
                m += 1;
            }
            if ((modifier & Keys.Alt) != Keys.None) {
                m += 2;
            }
            if ((modifier & Keys.Control) != Keys.None) {
                m += 4;
            }
            switch (key) {
                case Keys.F1:
                    return XtermFunctionKeyF1ToF4(m, (byte)'P');
                case Keys.F2:
                    return XtermFunctionKeyF1ToF4(m, (byte)'Q');
                case Keys.F3:
                    return XtermFunctionKeyF1ToF4(m, (byte)'R');
                case Keys.F4:
                    return XtermFunctionKeyF1ToF4(m, (byte)'S');
                case Keys.F5:
                    return XtermFunctionKeyF5ToF12(m, (byte)'1', (byte)'5');
                case Keys.F6:
                    return XtermFunctionKeyF5ToF12(m, (byte)'1', (byte)'7');
                case Keys.F7:
                    return XtermFunctionKeyF5ToF12(m, (byte)'1', (byte)'8');
                case Keys.F8:
                    return XtermFunctionKeyF5ToF12(m, (byte)'1', (byte)'9');
                case Keys.F9:
                    return XtermFunctionKeyF5ToF12(m, (byte)'2', (byte)'0');
                case Keys.F10:
                    return XtermFunctionKeyF5ToF12(m, (byte)'2', (byte)'1');
                case Keys.F11:
                    return XtermFunctionKeyF5ToF12(m, (byte)'2', (byte)'3');
                case Keys.F12:
                    return XtermFunctionKeyF5ToF12(m, (byte)'2', (byte)'4');
                default:
                    throw new ArgumentException("unexpected key value : " + key.ToString(), "key");
            }
        }

        private byte[] XtermFunctionKeyF1ToF4(int m, byte c) {
            if (m > 1) {
                return new byte[] { 0x1b, (byte)'[', (byte)'1', (byte)';', (byte)('0' + m), c };
            }
            else {
                return new byte[] { 0x1b, (byte)'O', c };
            }
        }

        private byte[] XtermFunctionKeyF5ToF12(int m, byte c1, byte c2) {
            if (m > 1) {
                return new byte[] { 0x1b, (byte)'[', c1, c2, (byte)';', (byte)('0' + m), (byte)'~' };
            }
            else {
                return new byte[] { 0x1b, (byte)'[', c1, c2, (byte)'~' };
            }
        }

        // emulate Xterm's modifyCursorKeys
        private byte[] ModifyCursorKey(Keys modifier, Keys key) {
            char c;
            switch (key) {
                case Keys.Up:
                    c = 'A';
                    break;
                case Keys.Down:
                    c = 'B';
                    break;
                case Keys.Right:
                    c = 'C';
                    break;
                case Keys.Left:
                    c = 'D';
                    break;
                default:
                    return null;
            }

            int m = 1;
            if ((modifier & Keys.Shift) != Keys.None) {
                m += 1;
            }
            if ((modifier & Keys.Alt) != Keys.None) {
                m += 2;
            }
            if ((modifier & Keys.Control) != Keys.None) {
                m += 4;
            }
            if (m == 1 || m == 8) {
                return null;
            }

            switch (XTermPreferences.Instance.modifyCursorKeys) {
                // only modifyCursorKeys=2 and modifyCursorKeys=3 are supported
                case 2: {
                        byte[] data = new byte[] {
                            0x1b, (byte)'[', (byte)'1', (byte)';', (byte)('0' + m), (byte)c
                        };
                        return data;
                    }
                case 3: {
                        byte[] data = new byte[] {
                            0x1b, (byte)'[', (byte)'>', (byte)'1', (byte)';', (byte)('0' + m), (byte)c
                        };
                        return data;
                    }
            }

            return null;
        }

        private byte[] VT100CursorKey(Keys modifier, Keys key) {
            byte[] r = new byte[3];
            r[0] = 0x1B;
            if (_cursorKeyMode == TerminalMode.Normal)
                r[1] = (byte)'[';
            else
                r[1] = (byte)'O';

            switch (key) {
                case Keys.Up:
                    r[2] = (byte)'A';
                    break;
                case Keys.Down:
                    r[2] = (byte)'B';
                    break;
                case Keys.Right:
                    r[2] = (byte)'C';
                    break;
                case Keys.Left:
                    r[2] = (byte)'D';
                    break;
                default:
                    throw new ArgumentException(String.Format("unknown cursor key code: {0}", key));
            }
            return r;
        }

#if NOTUSED
        private static string[] FUNCTIONKEY_MAP_VT100 = { 
        //     F1    F2    F3    F4    F5    F6    F7    F8    F9    F10   F11  F12
              "11", "12", "13", "14", "15", "17", "18", "19", "20", "21", "23", "24",
        //     F13   F14   F15   F16   F17  F18   F19   F20   F21   F22
              "25", "26", "28", "29", "31", "32", "33", "34", "23", "24"
        };
        //特定のデータを流すタイプ。現在、カーソルキーとファンクションキーが該当する         
        private byte[] SequenceKeyDataVT100(Keys modifier, Keys body) {
            if ((int)Keys.F1 <= (int)body && (int)body <= (int)Keys.F12) {
                byte[] r = new byte[5];
                r[0] = 0x1B;
                r[1] = (byte)'[';
                int n = (int)body - (int)Keys.F1;
                if ((modifier & Keys.Shift) != Keys.None)
                    n += 10; //shiftは値を10ずらす
                char tail;
                if (n >= 20)
                    tail = (modifier & Keys.Control) != Keys.None ? '@' : '$';
                else
                    tail = (modifier & Keys.Control) != Keys.None ? '^' : '~';
                string f = FUNCTIONKEY_MAP_VT100[n];
                r[2] = (byte)f[0];
                r[3] = (byte)f[1];
                r[4] = (byte)tail;
                return r;
            }
            else if (GUtil.IsCursorKey(body)) {
                byte[] r = new byte[3];
                r[0] = 0x1B;
                if (_cursorKeyMode == TerminalMode.Normal)
                    r[1] = (byte)'[';
                else
                    r[1] = (byte)'O';

                switch (body) {
                    case Keys.Up:
                        r[2] = (byte)'A';
                        break;
                    case Keys.Down:
                        r[2] = (byte)'B';
                        break;
                    case Keys.Right:
                        r[2] = (byte)'C';
                        break;
                    case Keys.Left:
                        r[2] = (byte)'D';
                        break;
                    default:
                        throw new ArgumentException("unknown cursor key code", "key");
                }
                return r;
            }
            else {
                byte[] r = new byte[4];
                r[0] = 0x1B;
                r[1] = (byte)'[';
                r[3] = (byte)'~';
                if (body == Keys.Insert)
                    r[2] = (byte)'1';
                else if (body == Keys.Home)
                    r[2] = (byte)'2';
                else if (body == Keys.PageUp)
                    r[2] = (byte)'3';
                else if (body == Keys.Delete)
                    r[2] = (byte)'4';
                else if (body == Keys.End)
                    r[2] = (byte)'5';
                else if (body == Keys.PageDown)
                    r[2] = (byte)'6';
                else
                    throw new ArgumentException("unknown key " + body.ToString());
                return r;
            }
        }
#endif

        [EscapeSequence(ControlCode.ESC, 'c')]
        private void ProcessRIS() {
            FullReset();
        }

        [EscapeSequence(ControlCode.DCS, EscapeSequenceParamType.Text, ControlCode.ST)]
        private void ProcessDCS(string p) {
            // TODO
        }

        //FormatExceptionのほかにOverflowExceptionの可能性もあるので
        private static int ParseInt(string param, int default_value) {
            try {
                if (param.Length > 0)
                    return Int32.Parse(param);
                else
                    return default_value;
            }
            catch (Exception ex) {
                throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", param, ex.Message));
            }
        }

        //動的変更用
        private class CaptionChanger {
            private ITerminalSettings _settings;
            private string _title;
            public CaptionChanger(ITerminalSettings settings, string title) {
                _settings = settings;
                _title = title;
            }
            public void Do() {
                _settings.BeginUpdate();
                _settings.Caption = _title;
                _settings.EndUpdate();
            }
        }
    }

    /// <summary>
    /// Preferences for XTerm
    /// </summary>
    internal class XTermPreferences : IPreferenceSupplier {

        private static XTermPreferences _instance = new XTermPreferences();

        public static XTermPreferences Instance {
            get {
                return _instance;
            }
        }

        private const int DEFAULT_MODIFY_CURSOR_KEYS = 2;

        private IIntPreferenceItem _modifyCursorKeys;

        /// <summary>
        /// Xterm's modifyCursorKeys feature
        /// </summary>
        public int modifyCursorKeys {
            get {
                if (_modifyCursorKeys != null)
                    return _modifyCursorKeys.Value;
                else
                    return DEFAULT_MODIFY_CURSOR_KEYS;
            }
        }

        #region IPreferenceSupplier

        public string PreferenceID {
            get {
                return TerminalEmulatorPlugin.PLUGIN_ID + ".xterm";
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _modifyCursorKeys = builder.DefineIntValue(folder, "modifyCursorKeys", DEFAULT_MODIFY_CURSOR_KEYS, PreferenceValidatorUtil.PositiveIntegerValidator);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }

        #endregion
    }
}
