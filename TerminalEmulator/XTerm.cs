// Copyright 2004-2025 The Poderosa Project.
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

        private class SavedCursor {
            // cursor position (0-based index)
            public readonly int RowIndex;
            public readonly int ColIndex;
            // character attributes / selective erase attribute
            public readonly TextDecoration Decoration;
            // wrap mode
            public readonly bool WrapAroundMode;
            // origin mode
            public readonly bool ScrollRegionRelative;
            // character set mapping
            public readonly CharacterSetMapping CharacterSetMapping;

            public SavedCursor(
                int rowIndex,
                int colIndex,
                TextDecoration decoration,
                bool wrapAroundMode,
                bool scrollRegionRelative,
                CharacterSetMapping characterSetMapping
            ) {
                this.RowIndex = rowIndex;
                this.ColIndex = colIndex;
                this.Decoration = decoration;
                this.WrapAroundMode = wrapAroundMode;
                this.ScrollRegionRelative = scrollRegionRelative;
                this.CharacterSetMapping = characterSetMapping;
            }
        }

        private struct RectArea {
            public readonly Row Top;
            public readonly Col Left;
            public readonly Row Bottom;
            public readonly Col Right;

            public RectArea(
                Row top,
                Col left,
                Row bottom,
                Col right
            ) {
                this.Top = top;
                this.Left = left;
                this.Bottom = bottom;
                this.Right = right;
            }
        }

        /// <summary>
        /// Position on the screen or view-port
        /// </summary>
        private struct RowCol {
            /// <summary>
            /// Vertical position. 1-based.
            /// </summary>
            public readonly Row Row;
            /// <summary>
            /// Horizontal position. 1-based.
            /// </summary>
            public readonly Col Col;

            public RowCol(Row row, Col col) {
                Row = row;
                Col = col;
            }
        }

        /// <summary>
        /// View port
        /// </summary>
        private struct ViewPort {
            /// <summary>
            /// View port width
            /// </summary>
            public readonly int Width;
            /// <summary>
            /// View port height
            /// </summary>
            public readonly int Height;

            private readonly int _topOffset; // 0-based
            private readonly int _leftOffset; // 0-based

            private readonly int _baseLineNumber;

            public ViewPort(TerminalDocument doc, bool relative) {
                if (relative) {
                    _topOffset = doc.TopMarginOffset;
                    _leftOffset = doc.LeftMarginOffset;
                    Height = doc.BottomMarginOffset - _topOffset + 1;
                    Width = doc.RightMarginOffset - _leftOffset + 1;
                    _baseLineNumber = doc.TopLineNumber + _topOffset;
                }
                else {
                    _topOffset = 0;
                    _leftOffset = 0;
                    Height = doc.TerminalHeight;
                    Width = doc.TerminalWidth;
                    _baseLineNumber = doc.TopLineNumber;
                }
            }

            public int ToLineNumber(Row row) {
                return _baseLineNumber + row.Value - 1;
            }

            public int ToCaretColumn(Col col) {
                return _leftOffset + col.Value - 1;
            }

            public Row FromLineNumber(int n) {
                return (n - _baseLineNumber + 1).AsRow();
            }

            public Col FromCaretColumn(int c) {
                return (c - _leftOffset + 1).AsCol();
            }

            /// <summary>
            /// Get origin as the absolute position on the screen
            /// </summary>
            public RowCol GetOrigin() {
                return new RowCol(
                    row: (_topOffset + 1).AsRow(),
                    col: (_leftOffset + 1).AsCol()
                );
            }
        }

        [Flags]
        private enum SGRStackMask : ushort {
            None = 0,
            Bold = 1,
            Underline = 8,
            Blink = 16,
            Inverted = 32,
            Hidden = 64,
            ForegroundColor = 128,
            BackgroundColor = 256,
        }

        private class SGRStackItem {
            public readonly TextDecoration Decoration;
            public readonly SGRStackMask Mask;

            public SGRStackItem(TextDecoration decoration, SGRStackMask mask) {
                Decoration = decoration;
                Mask = mask;
            }
        }

        private class TransmissionMode {
            public readonly bool IsEightBit;
            public readonly byte[] BRACKETED_PASTE_MODE_LEADING_BYTES;
            public readonly byte[] BRACKETED_PASTE_MODE_TRAILING_BYTES;

            private TransmissionMode(bool eightBit) {
                IsEightBit = eightBit;
                BRACKETED_PASTE_MODE_LEADING_BYTES = eightBit
                    ? new byte[] { (byte)ControlCode.CSI, (byte)'2', (byte)'0', (byte)'0', (byte)'~' }
                    : new byte[] { (byte)ControlCode.ESC, (byte)'[', (byte)'2', (byte)'0', (byte)'0', (byte)'~' };

                BRACKETED_PASTE_MODE_TRAILING_BYTES = eightBit
                    ? new byte[] { (byte)ControlCode.CSI, (byte)'2', (byte)'0', (byte)'1', (byte)'~' }
                    : new byte[] { (byte)ControlCode.ESC, (byte)'[', (byte)'2', (byte)'0', (byte)'1', (byte)'~' };
            }

            public static readonly TransmissionMode EightBit = new TransmissionMode(true);
            public static readonly TransmissionMode SevenBit = new TransmissionMode(false);
        }


        private readonly EscapeSequenceEngine<XTerm> _escapeSequenceEngine;

        private IModalCharacterTask _currentCharacterTask = null;

        private UnicodeChar? _prevNormalChar = null;
        private bool _wrapAroundMode = true;
        private bool _reverseVideo = false;
        private TabStops _tabStops = new TabStops();
        private readonly List<GLine>[] _savedScreen = new List<GLine>[2];	// { main, alternate } 別のバッファに移行したときにGLineを退避しておく
        private bool _isAlternateBuffer = false;
        private readonly SavedCursor[] _savedCursor = new SavedCursor[2];	// { main, alternate }
        private SavedCursor _savedCursorSCO = null;
        private readonly Dictionary<int, bool> _savedDecModes = new Dictionary<int, bool>();
        private readonly LinkedList<SGRStackItem> _sgrStack = new LinkedList<SGRStackItem>();

        private bool _bracketedPasteMode = false;
        private static readonly byte[] BRACKETED_PASTE_MODE_EMPTY_BYTES = new byte[0];

        private MouseTrackingState _mouseTrackingState = MouseTrackingState.Off;
        private MouseTrackingProtocol _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
        private bool _focusReportingMode = false;
        private int _prevMouseRow = -1;
        private int _prevMouseCol = -1;
        private MouseButtons _mouseButton = MouseButtons.None;

        private TransmissionMode _transmissionMode = TransmissionMode.SevenBit;
        private bool _insertMode = false;
        private bool _originRelative = false;
        private bool _enableHorizontalMargins = false; // DECLRMM
        private bool _rectangularAttributeChange = false; // mode for DECCARA and DECRARA
        private bool _sixelDisplayMode = false; // DECSDM
        private bool _sixelScrollingCursorRight = false;
        private bool _usePrivateColorRegisters = true;

        private const int MOUSE_POS_LIMIT = 255 - 32;       // mouse position limit
        private const int MOUSE_POS_EXT_LIMIT = 2047 - 32;  // mouse position limit in extended mode
        private const int MOUSE_POS_EXT_START = 127 - 32;   // mouse position to start using extended format

        private const int MAX_SGR_STACK = 10;

        public XTerm(TerminalInitializeInfo info)
            : base(info) {
            _escapeSequenceEngine = new EscapeSequenceEngine<XTerm>(
                completedHandler: HandleEscapeSequenceCompleted,
                incompleteHandler: HandleIncompleteEscapeSequence,
                exceptionHandler: HandleException
            );
            _tabStops.Extend(Document.TerminalWidth);
        }

        private void HandleEscapeSequenceCompleted(IEscapeSequenceContext context) {
            if (this.LogService.HasXmlLogger) {
                this.LogService.XmlLogger.EscapeSequence(context.GetSequence());
            }
        }

        private void HandleIncompleteEscapeSequence(IEscapeSequenceContext context) {
            if (this.LogService.HasXmlLogger) {
                this.LogService.XmlLogger.EscapeSequence(context.GetSequence());
            }
#if DEBUG
            Debug.WriteLine(EscapeSequenceErrorUtil.FormatMessage("Incomplete escape sequence", context.GetSequence()));
#endif
        }

        private void HandleException(Exception ex, IEscapeSequenceContext context) {
            RuntimeUtil.SilentReportException(ex);
        }

        public override bool IsEscapeSequenceReading {
            get {
                return _escapeSequenceEngine.IsEscapeSequenceReading;
            }
        }

        protected override void FullResetInternal() { // called from the base class
            _tabStops.Initialize();
            _savedDecModes.Clear();
            _prevNormalChar = null;
            _wrapAroundMode = true;
            _reverseVideo = false;
            _bracketedPasteMode = false;
            _mouseTrackingState = MouseTrackingState.Off;
            _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
            _focusReportingMode = false;
            _transmissionMode = TransmissionMode.SevenBit;
            _insertMode = false;
            _originRelative = false;
            _enableHorizontalMargins = false;
            _rectangularAttributeChange = false;
            _sixelDisplayMode = false;
            _sixelScrollingCursorRight = false;
            _usePrivateColorRegisters = true;
            _escapeSequenceEngine.Reset();
            DoEraseInDisplay(2 /* all */, false, true);
            MoveCursorTo(new Row(1), new Col(1));
        }

        protected override void SoftResetInternal() { // called from the base class
            _prevNormalChar = null;
            _wrapAroundMode = true;
            _insertMode = false;
            _originRelative = false;
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
            return _bracketedPasteMode ? _transmissionMode.BRACKETED_PASTE_MODE_LEADING_BYTES : BRACKETED_PASTE_MODE_EMPTY_BYTES;
        }

        internal override byte[] GetPasteTrailingBytes() {
            return _bracketedPasteMode ? _transmissionMode.BRACKETED_PASTE_MODE_TRAILING_BYTES : BRACKETED_PASTE_MODE_EMPTY_BYTES;
        }

        public override void ProcessChar(char ch) {
            if (ch == ControlCode.NUL) {
                _prevNormalChar = null;
                return;
            }

            if (!_escapeSequenceEngine.Process(this, ch)) {
                IModalCharacterTask characterTask = _currentCharacterTask;
                if (characterTask != null) { // macro etc.
                    characterTask.ProcessChar(ch);
                }

                ProcessNormalChar(ch);
            }
            else if (!_escapeSequenceEngine.IsEscapeSequenceReading) {
                // escape sequence has been processed
                _prevNormalChar = null;
            }
        }

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
                    data = MakeCSI(
                        (byte)77, // M
                        (byte)statBits,
                        (col == posLimit) ?
                            (byte)0 :                   // emulate xterm's bug
                            (byte)(col + (1 + 0x20)),   // column 0 --> send as 1
                        (row == posLimit) ?
                            (byte)0 :                   // emulate xterm's bug
                            (byte)(row + (1 + 0x20))    // row 0 --> send as 1
                    );
                    dataLen = data.Length;
                    break;

                case MouseTrackingProtocol.Utf8:
                    data = MakeCSI(
                        (byte)77, // M
                        (byte)statBits,
                        0, 0, 0, 0
                    );

                    dataLen = data.Length - 4;

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
                    data = MakeCSI(
                            statBits.ToInvariantString()
                            + ";"
                            + (col + 1).ToInvariantString()
                            + ";"
                            + (row + 1).ToInvariantString()
                            + "M"
                        );
                    dataLen = data.Length;
                    break;

                case MouseTrackingProtocol.Sgr:
                    data = MakeCSI(
                            "<"
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
            this.LogService.XmlLogger.Write(ch);

            UnicodeChar unicodeChar;
            if (!base.UnicodeCharConverter.Feed(ch, out unicodeChar)) {
                _prevNormalChar = null;
                return;
            }
            if (unicodeChar.IsZeroWidth) {
                _prevNormalChar = null;
                return; // drop
            }

            ProcessNormalUnicodeChar(unicodeChar);

            _prevNormalChar = (unicodeChar.CodePoint >= 0x20) ? unicodeChar : (UnicodeChar?)null;
        }

        private void ProcessNormalUnicodeChar(UnicodeChar unicodeChar) {
            if (Document.TerminalWidth < 1) {
                return;
            }

            if (Document.WrapPending && _wrapAroundMode) {
                // do pending line wrap
                ContinueToNextLine();
                ProcessNormalUnicodeChar2(unicodeChar, false);
            }
            else {
                ProcessNormalUnicodeChar2(unicodeChar, _wrapAroundMode);
            }
        }

        private void ProcessNormalUnicodeChar2(UnicodeChar unicodeChar, bool canWrap) {
            int charWidth = unicodeChar.IsWideWidth ? 2 : 1;
            int nextColumn = Document.CaretColumn + charWidth;
            int rightEnd = GetCaretColumnRightLimit() + 1;

            if (nextColumn <= rightEnd) { // many cases
                if (_insertMode) {
                    _manipulator.InsertBlanks(Document.CaretColumn, charWidth, rightEnd, Document.CurrentDecoration);
                }

                _manipulator.PutChar(Document.CaretColumn, unicodeChar, Document.CurrentDecoration);

                if (nextColumn == rightEnd) {
                    Document.CaretColumn = rightEnd - 1;
                    if (_wrapAroundMode) {
                        Document.WrapPending = true;
                    }
                }
                else {
                    Document.CaretColumn = nextColumn;
                }
            }
            else {
                // overflow
                if (canWrap) {
                    ContinueToNextLine();
                    ProcessNormalUnicodeChar2(unicodeChar, false);
                }
                else {
                    _manipulator.PutChar(rightEnd - 1, (charWidth == 2) ? UnicodeChar.ASCII_NUL : unicodeChar, Document.CurrentDecoration);
                    Document.CaretColumn = rightEnd - 1;
                }
            }
        }

        private int GetCaretColumnLeftLimit() {
            int leftMargin = Document.LeftMarginOffset;
            return (Document.CaretColumn >= leftMargin) ? leftMargin : 0;
        }

        private int GetCaretColumnRightLimit() {
            int rightMargin = Document.RightMarginOffset;
            return (Document.CaretColumn <= rightMargin) ? rightMargin : (Document.TerminalWidth - 1);
        }

        private void ContinueToNextLine() {
            _manipulator.EOLType = EOLType.Continue;
            Document.UpdateCurrentLine(_manipulator);
            Document.LineFeed();
            _manipulator.Load(Document.CurrentLine);
            Document.CaretColumn = Document.LeftMarginOffset;
        }

        [EscapeSequence(ControlCode.ST)] // ST (orphan string terminator)
        [EscapeSequence(ControlCode.ENQ)] // ENQ (answerback message is not supported)
        [EscapeSequence(ControlCode.DEL)] // DEL (if DEL is a printable character in the selected graphics set, CharDecoder convert it)
        [EscapeSequence(ControlCode.ESC, ' ', 'L')] // dpANS X3.134.1 - ANSI conformance level 1
        [EscapeSequence(ControlCode.ESC, ' ', 'M')] // dpANS X3.134.1 - ANSI conformance level 2
        [EscapeSequence(ControlCode.ESC, ' ', 'N')] // dpANS X3.134.1 - ANSI conformance level 3
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
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'q')] // Load LEDs
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 'q')] // Set Cursor Style
        // [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 't')] // Title Mode (xterm)
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 't')] // Set Warning Bell Volume
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 'u')] // Set Margin Bell Volume
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '\'', 'w')] // Enable Filter Rectangle
        // [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '#', 'y')] // Select checksum extension (xterm)
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

        // APC, DCS and PM are handled by EscapeSequenceEngine (just ignored)
        // [EscapeSequence(ControlCode.APC, EscapeSequenceParamType.Text, ControlCode.ST)] // Application Program Command
        // [EscapeSequence(ControlCode.PM, EscapeSequenceParamType.Text, ControlCode.ST)] // Privacy message
        // [EscapeSequence(ControlCode.DCS, EscapeSequenceParamType.Text, ControlCode.ST)] // DCS
        // private void Ignore(string p) {
        // }

        [EscapeSequence(ControlCode.ESC, ' ', 'F')] // S7C1T
        private void Select7bitC1() {
            if (_transmissionMode.IsEightBit) {
                _transmissionMode = TransmissionMode.SevenBit;
            }
        }

        [EscapeSequence(ControlCode.ESC, ' ', 'G')] // S8C1T
        private void Select8bitC1() {
            if (!_transmissionMode.IsEightBit) {
                _transmissionMode = TransmissionMode.EightBit;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '"', 'p')] // DECSCL
        private void SelectConformanceLevel(NumericParams p) {
            int level = p.Get(0, 0);
            if (level < 61 || level > 64) {
                return;
            }

            if (level <= 61) {
                Select7bitC1();
            }
            else {
                int c1Mode = p.Get(1, 0);
                switch (c1Mode) {
                    case 0:
                    case 2:
                        Select8bitC1();
                        break;
                    case 1:
                        Select7bitC1();
                        break;
                }
            }
        }

        [EscapeSequence(ControlCode.ESC, 'c')] // RIS
        private void ProcessRIS() {
            FullReset();
        }

        [EscapeSequence(ControlCode.LF)]
        [EscapeSequence(ControlCode.VT)]
        [EscapeSequence(ControlCode.FF)]
        private void LineFeed() {
            if (Document.ForceNewLine) {
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
            if (_lineContinuationMode == LineContinuationMode.Poderosa
                && (Document.CaretColumn == Document.LeftMarginOffset || Document.CaretColumn == 0)) {
                int prevLineNumber = Document.CurrentLineNumber - 1;
                GLine prevLine = Document.FindLineOrNull(prevLineNumber);
                if (prevLine != null && prevLine.EOLType == EOLType.Continue) {
                    Document.UpdateCurrentLine(_manipulator);
                    Document.CurrentLineNumber = prevLineNumber;
                    if (Document.TopLineNumber > Document.CurrentLineNumber) {
                        Document.SetTopLineNumber(Document.CurrentLineNumber);
                    }
                    _manipulator.Load(Document.CurrentLine);
                    Document.CaretColumn = Document.CurrentLine.DisplayLength - 1;
                }
                return;
            }

            Document.CaretColumn = Math.Max(Document.CaretColumn - 1, GetCaretColumnLeftLimit());
        }

        [EscapeSequence(ControlCode.HT)]
        private void HorizontalTab() {
            DoForwardTab(1);
        }

        private void DoLineFeed() {
            _manipulator.EOLType = (_manipulator.EOLType == EOLType.CR || _manipulator.EOLType == EOLType.CRLF) ? EOLType.CRLF : EOLType.LF;
            Document.UpdateCurrentLine(_manipulator);
            Document.LineFeed();

            _manipulator.Load(Document.CurrentLine);
        }

        private void DoCarriageReturn() {
            _manipulator.EOLType = EOLType.CR;  // will be changed to CRLF in DoLineFeed()
            Document.CaretColumn = GetCaretColumnLeftLimit();
        }

        [EscapeSequence(ControlCode.ESC, '6')] // DECBI
        private void BackIndex() {
            if (Document.CaretColumn == Document.LeftMarginOffset
                && Document.IsCurrentLineInScrollingRegion
                && Document.IsCaretColumnInScrollingRegion) {

                ShiftScrollRegion(1);
            }
            else {
                Document.CaretColumn = Math.Max(Document.CaretColumn - 1, GetCaretColumnLeftLimit());
            }
        }

        [EscapeSequence(ControlCode.ESC, '9')] // DECFI
        private void ForwardIndex() {
            if (Document.CaretColumn == Document.RightMarginOffset
                && Document.IsCurrentLineInScrollingRegion
                && Document.IsCaretColumnInScrollingRegion) {

                ShiftScrollRegion(-1);
            }
            else {
                Document.CaretColumn = Math.Min(Document.CaretColumn + 1, GetCaretColumnRightLimit());
            }
        }

        /// <summary>
        /// Shift left / shift right scroll region
        /// </summary>
        /// <param name="columns">shift right if positive value, shift left if negative value.</param>
        private void ShiftScrollRegion(int columns) {
            ShiftScrollRegionFrom(Document.LeftMarginOffset, columns);
        }

        /// <summary>
        /// Shift left / shift right scroll region
        /// </summary>
        /// <param name="from">column index to start shifting.</param>
        /// <param name="columns">shift right if positive value, shift left if negative value.</param>
        private void ShiftScrollRegionFrom(int from, int columns) {
            Document.UpdateCurrentLine(_manipulator);

            int scrollingBottom = Document.ScrollingBottomLineNumber;
            for (GLine l = Document.FindLineOrEdge(Document.ScrollingTopLineNumber); l != null && l.ID <= scrollingBottom; l = l.NextLine) {
                _manipulator.Load(l);
                if (columns > 0) {
                    _manipulator.InsertBlanks(from, columns, Document.RightMarginOffset + 1, Document.CurrentDecoration);
                }
                else if (columns < 0) {
                    _manipulator.DeleteChars(from, -columns, Document.RightMarginOffset + 1, Document.CurrentDecoration);
                }
                _manipulator.ExportTo(l);
                Document.InvalidatedRegion.InvalidateLine(l.ID);
            }

            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.ESC, '#', '8')] // DECALN
        private void ProcessScreenAlignment() {
            Document.ClearMargins();

            Document.UpdateCurrentLine(_manipulator);

            // fill test pattern as xterm does
            UnicodeChar testChar = new UnicodeChar('E', false);
            _manipulator.Reset(Document.TerminalWidth);
            for (int i = 0; i < Document.TerminalWidth; i++) {
                _manipulator.PutChar(i, testChar, Document.CurrentDecoration);
            }

            int bottomLineNumber = Document.TopLineNumber + Document.TerminalHeight - 1;
            Document.EnsureLine(Document.TopLineNumber + Document.TerminalHeight - 1);
            GLine l = Document.TopLine;
            while (l != null && l.ID <= bottomLineNumber) {
                _manipulator.ExportTo(l);
                l = l.NextLine;
            }

            _manipulator.Load(Document.CurrentLine);

            MoveCursorTo(new Row(1), new Col(1));
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'c')] // DA
        private void ProcessPrimaryDeviceAttributes(NumericParams p) {
            if (p.Get(0, 0) == 0) {
                int[] features =
                {
                    1, // 132 columns
                    // 2, // Printer port
                    4, // Sixel
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

                byte[] data = MakeCSI("?64;" + featuresText + "c");
                TransmitDirect(data);
            }
        }

        [EscapeSequence(ControlCode.CSI, '>', EscapeSequenceParamType.Numeric, 'c')] // Secondary DA
        private void ProcessSecondaryDeviceAttributes(NumericParams p) {
            if (p.Get(0, 0) == 0) {
                byte[] data = MakeCSI(">41;1;0c");
                TransmitDirect(data);
                return;
            }
        }

        [EscapeSequence(ControlCode.CSI, '=', EscapeSequenceParamType.Numeric, 'c')] // Tertiary DA
        private void ProcessTertiaryDeviceAttributes(NumericParams p) {
            if (p.Get(0, 0) == 0) {
                // DECRPTUI: DCS ! | 00 00 00 00 ST
                byte[] data = MakeDCS_ST("!|00000000");
                TransmitDirect(data);
                return;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'n')] // DSR
        private void ProcessDeviceStatusReport(NumericParams p) {
            int param = p.Get(0, 0);

            byte[] response;
            switch (param) {
                case 5: // Operating Status Report
                    response = MakeCSI("0n"); // good
                    break;
                case 6: // Cursor Position Report
                    {
                        var rc = GetCursorPosition();
                        response = MakeCSI(
                            rc.Row.ToInvariantString()
                            + ";"
                            + rc.Col.ToInvariantString()
                            + "R"
                        );
                        break;
                    }
                default:
                    return;
            }

            TransmitDirect(response);
        }

        private RowCol GetCursorPosition() {
            ViewPort vp = GetViewPort();
            return new RowCol(
                row: vp.FromLineNumber(Document.CurrentLineNumber).ClipLower(1),
                col: vp.FromCaretColumn(Document.CaretColumn).ClipLower(1)
            );
        }

        private RowCol GetScreenCursorPosition() {
            return new RowCol(
                row: (Document.CurrentLineNumber - Document.TopLineNumber + 1).AsRow(),
                col: (Document.CaretColumn + 1).AsCol()
            );
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'n')] // DEC DSR
        private void ProcessDeviceStatusReportDEC(NumericParams p) {
            int param = p.Get(0, 0);

            byte[] response;
            switch (param) {
                case 5:
                    // "CSI ? 5 n" doesn't appear in the DEC documentations, but xterm returns this
                    response = MakeCSI("?0n");
                    break;
                case 6: // Extended Cursor Position Report
                    {
                        var rc = GetCursorPosition();
                        response = MakeCSI(
                            "?"
                            + rc.Row.ToInvariantString()
                            + ";"
                            + rc.Col.ToInvariantString()
                            + ";1R"
                        );
                        break;
                    }
                case 15: // Printer Status Report
                    response = MakeCSI("?13n"); // no printer
                    break;
                case 25: // User-Defined Keys Status
                    response = MakeCSI("?20n"); // unlocked
                    break;
                case 26: // Keyboard Status Report
                    response = MakeCSI("?27;1;0;0n"); // North American, Ready
                    break;
                case 55: // Locator Status
                    // according to "Locator Input Model for ANSI Terminals (sixth revision)", response below means "no locator"
                    response = MakeCSI("?53n"); // no locator
                    break;
                case 56: // Locator Type
                    response = MakeCSI("?57;0n"); // unknown
                    break;
                case 62: // Macro Space Report
                    // xterm returns 4 digits
                    response = MakeCSI("0000*{");
                    break;
                case 63: // Memory Checksum
                    int pid = p.Get(1, 0);
                    response = MakeDCS_ST(pid.ToInvariantString() + "!~0000");
                    break;
                case 75: // Data Integrity Report
                    response = MakeCSI("?70n"); // OK
                    break;
                case 85: // Multi-session Configuration
                    response = MakeCSI("?83n"); // not configured
                    break;
                default:
                    return;
            }

            TransmitDirect(response);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'w')] // DECRQPSR
        private void ProcessPresentationStateReport(NumericParams p) {
            int n = p.Get(0, 0);

            byte[] response;
            switch (n) {
                case 1:
                    // DECCIR
                    {
                        var rc = GetScreenCursorPosition();
                        int page = 1;
                        char srend = GetDECCIRRenditions();
                        char satt = GetDECCIRAttributes();
                        char sflag = GetDECCIRFlags();
                        char scss = GetDECCIRCharacterSetSize();
                        int pgl;
                        int pgr;
                        string sdesig;
                        GetDECCIRCharacterSetMapping(out sdesig, out pgl, out pgr);

                        response = MakeDCS_ST(
                            "1$u"
                            + rc.Row.ToInvariantString() + ";"
                            + rc.Col.ToInvariantString() + ";"
                            + page.ToInvariantString() + ";"
                            + new String(new char[] { srend, ';', satt, ';', sflag, ';' })
                            + pgl.ToInvariantString() + ";"
                            + pgr.ToInvariantString() + ";"
                            + new String(new char[] { scss, ';' })
                            + sdesig
                        );
                        break;
                    }
                case 2:
                    // DECTABSR
                    {
                        _tabStops.Extend(Document.TerminalWidth);
                        var tabColumns =
                            String.Join("/",
                                _tabStops.GetIndices()
                                    .Where(index => index > 0)
                                    .Select((index) => index + 1) // to column position (1-based)
                                    .Select((column) => column.ToInvariantString())
                            );
                        response = MakeDCS_ST("2$u" + tabColumns);
                        break;
                    }
                default:
                    return; // do nothing
            }

            TransmitDirect(response);
        }

        private char GetDECCIRRenditions() {
            // bit 7: always 0
            // bit 6: always 1
            // bit 5: extension indicator
            // bit 4: always 0
            // bit 3: reverse video
            // bit 2: blink
            // bit 1: underline
            // bit 0: bold
            TextDecoration dec = Document.CurrentDecoration;
            int r = 0x40
                + (dec.Bold ? 1 : 0)
                + (dec.Underline ? 2 : 0)
                + (dec.Blink ? 4 : 0)
                + (dec.Inverted ? 8 : 0);
            return (char)r;
        }

        private void RestoreDECCIRRenditions(char ch) {
            if ((ch & 0xf0) == 0x40) {
                TextDecoration dec = Document.CurrentDecoration;
                dec = dec.GetCopyWithBold((ch & 1) != 0);
                dec = dec.GetCopyWithUnderline((ch & 2) != 0);
                dec = dec.GetCopyWithBlink((ch & 4) != 0);
                dec = dec.GetCopyWithInverted((ch & 8) != 0);
                Document.CurrentDecoration = dec;
            }
        }

        private char GetDECCIRAttributes() {
            // bit 7: always 0
            // bit 6: always 1
            // bit 5: extension indicator
            // bit 4: reserved
            // bit 3: reserved
            // bit 2: reserved
            // bit 1: reserved
            // bit 0: selectively erasable
            TextDecoration dec = Document.CurrentDecoration;
            int r = 0x40
                + (dec.Protected ? 1 : 0);
            return (char)r;
        }

        private void RestoreDECCIRAttributes(char ch) {
            if ((ch & 0xe0) == 0x40) {
                TextDecoration dec = Document.CurrentDecoration;
                dec = dec.GetCopyWithProtected((ch & 1) != 0);
                Document.CurrentDecoration = dec;
            }
        }

        private char GetDECCIRFlags() {
            // bit 7: always 0
            // bit 6: always 1
            // bit 5: extension indicator
            // bit 4: reserved
            // bit 3: auto-wrap pending (1=pending)
            // bit 2: SS3 pending (1=SS3 received)
            // bit 1: SS2 pending (1=SS2 received)
            // bit 0: origin mode (1=set 0=reset)
            bool autoWrapPending = Document.WrapPending && _wrapAroundMode;
            int r = 0x40
                + (_originRelative ? 1 : 0)
                + (autoWrapPending ? 8 : 0);
            return (char)r;
        }

        private void RestoreDECCIRFlags(char ch) {
            if ((ch & 0xe0) == 0x40) {
                _originRelative = (ch & 1) != 0;
                Document.WrapPending = (ch & 8) != 0 && _wrapAroundMode;
            }
        }

        private char GetDECCIRCharacterSetSize() {
            // bit 7: always 0
            // bit 6: always 1
            // bit 5: extension indicator
            // bit 4: reserved
            // bit 3: size of G3 set (1=96-character, 0=94-character)
            // bit 2: size of G2 set (1=96-character, 0=94-character)
            // bit 1: size of G1 set (1=96-character, 0=94-character)
            // bit 0: size of GO set (1=96-character, 0=94-character)
            int r = 0x40
                + ((CharacterSetManager.GetCharacterSetSizeType(0) == CharacterSetSizeType.CS96) ? 1 : 0)
                + ((CharacterSetManager.GetCharacterSetSizeType(1) == CharacterSetSizeType.CS96) ? 2 : 0)
                + ((CharacterSetManager.GetCharacterSetSizeType(2) == CharacterSetSizeType.CS96) ? 4 : 0)
                + ((CharacterSetManager.GetCharacterSetSizeType(3) == CharacterSetSizeType.CS96) ? 8 : 0);
            return (char)r;
        }

        private void GetDECCIRCharacterSetMapping(out string mapping, out int gl, out int gr) {
            string g0, g1, g2, g3;
            CharacterSetManager.GetCharacterSetMapping(out g0, out g1, out g2, out g3, out gl, out gr);

            const string DEFAULT_DESIGNATOR = "B";

            mapping =
                ((g0 != null) ? g0 : DEFAULT_DESIGNATOR)
                + ((g1 != null) ? g1 : DEFAULT_DESIGNATOR)
                + ((g2 != null) ? g2 : DEFAULT_DESIGNATOR)
                + ((g3 != null) ? g3 : DEFAULT_DESIGNATOR);
        }

        private void RestoreDECCIRCharacterSetMapping(string mapping, int gl, int gr) {
            List<string> desigs = new List<string>();
            char prefixChar = '\u0000';

            foreach (char ch in mapping) {
                switch (prefixChar) {
                    case '%':
                    case '"':
                    case '&':
                        desigs.Add(new String(new char[] { prefixChar, ch }));
                        prefixChar = '\u0000';
                        break;
                    default:
                        switch (ch) {
                            case '%':
                            case '"':
                            case '&':
                                prefixChar = ch;
                                break;
                            default:
                                desigs.Add(new String(new char[] { ch }));
                                break;
                        }
                        break;
                }
            }

            if (desigs.Count >= 4 && gl >= 0 && gl <= 3 && gr >= 0 && gr <= 3) {
                CharacterSetManager.RestoreCharacterSetMapping(desigs[0], desigs[1], desigs[2], desigs[3], gl, gr);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'x')] // DECREQTPARM
        private void ProcessRequestTerminalParameters(NumericParams p) {
            int s = p.Get(0, 0);

            string r; // <sol> of response
            if (s == 0) {
                r = "2";
            }
            else if (s == 1) {
                r = "3";
            }
            else {
                return;
            }

            // <parity> = 1 / <nbits> = 1 / <xspeed> = 128 / <rspeed> = 128 / <clkmul> = 1 / <flags> = 0
            byte[] response = MakeCSI(r + ";1;1;128;128;1;0x");

            TransmitDirect(response);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'A')] // CUU
        private void ProcessCursorUp(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            Document.UpdateCurrentLine(_manipulator);
            int scrollingTop = Document.ScrollingTopLineNumber;
            if (Document.CurrentLineNumber >= scrollingTop) {
                Document.CurrentLineNumber = Math.Max(Document.CurrentLineNumber - count, scrollingTop);
            }
            else {
                Document.CurrentLineNumber = Math.Max(Document.CurrentLineNumber - count, Document.TopLineNumber);
            }
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'B')] // CUD
        private void ProcessCursorDown(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            Document.UpdateCurrentLine(_manipulator);
            int scrollingBottom = Document.ScrollingBottomLineNumber;
            if (Document.CurrentLineNumber <= scrollingBottom) {
                Document.CurrentLineNumber = Math.Min(Document.CurrentLineNumber + count, scrollingBottom);
            }
            else {
                Document.CurrentLineNumber = Math.Min(Document.CurrentLineNumber + count, Document.TopLineNumber + Document.TerminalHeight - 1);
            }
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'C')] // CUF
        private void ProcessCursorForward(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            Document.CaretColumn = Math.Min(Document.CaretColumn + count, GetCaretColumnRightLimit());
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'D')] // CUB
        private void ProcessCursorBackward(NumericParams p) {
            int count = p.GetNonZero(0, 1);
            Document.CaretColumn = Math.Max(Document.CaretColumn - count, GetCaretColumnLeftLimit());
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'E')] // CNL
        private void ProcessCursorNextLine(NumericParams p) {
            ProcessCursorDown(p);
            Document.CaretColumn = GetCaretColumnLeftLimit();
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'F')] // CPL
        private void ProcessCursorPrecedingLine(NumericParams p) {
            ProcessCursorUp(p);
            Document.CaretColumn = GetCaretColumnLeftLimit();
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'G')] // CHA
        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '`')] // HPA
        private void ProcessLineColumnAbsolute(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            ViewPort vp = GetViewPort();
            Document.CaretColumn = vp.ToCaretColumn(Math.Min(n, vp.Width).AsCol());
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'H')] // CUP
        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'f')] // HVP
        private void ProcessCursorPosition(NumericParams p) {
            int row = p.GetNonZero(0, 1);
            int col = p.GetNonZero(1, 1);

            ViewPort vp = GetViewPort();
            RowCol origin = vp.GetOrigin();
            MoveCursorTo(
                (origin.Row.Value + Math.Min(row, vp.Height) - 1).AsRow(),
                (origin.Col.Value + Math.Min(col, vp.Width) - 1).AsCol()
            );
        }

        [EscapeSequence(ControlCode.CSI, 'U')] // NP
        private void ProcessMovePagesForward() {
            MoveCursorToOrigin();
        }

        [EscapeSequence(ControlCode.CSI, 'V')] // PP
        private void ProcessMovePagesBackward() {
            MoveCursorToOrigin();
        }

        private void MoveCursorTo(Row row, Col col) {
            int nextLineNumber = Document.TopLineNumber + row.Value - 1;
            if (Document.CurrentLineNumber != nextLineNumber) {
                Document.UpdateCurrentLine(_manipulator);
                Document.CurrentLineNumber = nextLineNumber;
                _manipulator.Load(Document.CurrentLine);
            }
            Document.CaretColumn = col.Value - 1;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'a')] // HPR
        private void ProcessCursorPositionRelative(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            int max = Document.TerminalWidth - 1;
            if (Document.CaretColumn < max) {
                Document.CaretColumn = Math.Min(Document.CaretColumn + n, max);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'J')] // ED
        private void ProcessEraseInDisplay(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInDisplay(param, false, true);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'J')] // DECSED
        private void ProcessSelectiveEraseInDisplay(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInDisplay(param, true, false);
        }

        private void DoEraseInDisplay(int param, bool selective, bool resetLineRenderingType) {
            int bottomLineNumber = Document.TopLineNumber + Document.TerminalHeight - 1;
            switch (param) {
                case 0: //erase below
                    {
                        if (Document.CaretColumn == 0 && Document.CurrentLineNumber == Document.TopLineNumber) {
                            goto ERASE_ALL;
                        }

                        if (Document.CaretColumn == 0 && resetLineRenderingType) {
                            _manipulator.LineRenderingType = LineRenderingType.Normal;
                        }

                        if (selective) {
                            SelectiveEraseRight();
                        }
                        else {
                            EraseRight();
                        }

                        Document.UpdateCurrentLine(_manipulator);
                        Document.EnsureLine(bottomLineNumber);
                        Document.RemoveAfter(bottomLineNumber + 1);
                        Document.ClearRange(Document.CurrentLineNumber + 1, bottomLineNumber + 1, Document.CurrentDecoration, selective, resetLineRenderingType);
                        _manipulator.Load(Document.CurrentLine);

                        // clear wide enough to avoid residual pixels at the screen edge.
                        // FIXME: all sixels on the current line are cleared.
                        bool cleared = Document.SixelImageManager.ClearLineRange(Document.CurrentLineNumber, bottomLineNumber + 1);
                        if (cleared) {
                            Document.InvalidateAll();
                        }
                    }
                    break;
                case 1: //erase above
                    {
                        if (Document.CaretColumn >= Document.TerminalWidth - 1 && Document.CurrentLineNumber == bottomLineNumber) {
                            goto ERASE_ALL;
                        }

                        if (Document.CaretColumn >= Document.TerminalWidth - 1 && resetLineRenderingType) {
                            _manipulator.LineRenderingType = LineRenderingType.Normal;
                        }

                        if (selective) {
                            SelectiveEraseLeft();
                        }
                        else {
                            EraseLeft();
                        }
                        Document.UpdateCurrentLine(_manipulator);
                        Document.ClearRange(Document.TopLineNumber, Document.CurrentLineNumber, Document.CurrentDecoration, selective, resetLineRenderingType);
                        _manipulator.Load(Document.CurrentLine);

                        // clear wide enough to avoid residual pixels at the screen edge.
                        // FIXME: all sixels on the current line are cleared.
                        bool cleared = Document.SixelImageManager.ClearLineRange(Document.TopLineNumber - 1, Document.CurrentLineNumber);
                        if (cleared) {
                            Document.InvalidateAll();
                        }
                    }
                    break;
                case 2: //erase all
                ERASE_ALL: {
                        Document.ApplicationModeBackColor =
                                (Document.CurrentDecoration != null) ? Document.CurrentDecoration.GetBackColorSpec() : ColorSpec.Default;

                        Document.UpdateCurrentLine(_manipulator);
                        Document.EnsureLine(bottomLineNumber);
                        Document.RemoveAfter(bottomLineNumber + 1);
                        Document.ClearRange(Document.TopLineNumber, bottomLineNumber + 1, Document.CurrentDecoration, selective, resetLineRenderingType);
                        _manipulator.Load(Document.CurrentLine);

                        // clear wide enough to avoid residual pixels at the screen edge.
                        bool cleared = Document.SixelImageManager.ClearLineRange(Document.TopLineNumber - 1, bottomLineNumber + 1);
                        if (cleared) {
                            Document.InvalidateAll();
                        }
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

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'z')] // DECERA
        private void ProcessEraseRectangle(NumericParams p) {
            DoEraseRectangle(p, false);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', '{')] // DECSERA
        private void ProcessSelectiveEraseRectangle(NumericParams p) {
            DoEraseRectangle(p, true);
        }

        private void DoEraseRectangle(NumericParams p, bool selective) {
            ViewPort vp = GetViewPort();
            RectArea rect;
            if (!ReadRectAreaFromParameters(p, 0, vp, out rect)) {
                return;
            }

            Document.UpdateCurrentLine(_manipulator);

            int rectTopLineNumber = vp.ToLineNumber(rect.Top);
            int rectBottomLineNumber = vp.ToLineNumber(rect.Bottom);

            GLine l = Document.FindLineOrEdge(rectTopLineNumber);
            while (l != null && l.ID <= rectBottomLineNumber) {
                _manipulator.Load(l);
                if (selective) {
                    // DECSERA preserves attributes
                    _manipulator.FillSpaceUnprotectedPreservingAttributes(vp.ToCaretColumn(rect.Left), vp.ToCaretColumn(rect.Right + 1));
                }
                else {
                    _manipulator.FillSpace(vp.ToCaretColumn(rect.Left), vp.ToCaretColumn(rect.Right + 1), Document.CurrentDecoration);
                }
                _manipulator.ExportTo(l);
                Document.InvalidatedRegion.InvalidateLine(l.ID);
                l = l.NextLine;
            }

            _manipulator.Load(Document.CurrentLine);
        }

        private bool ReadRectAreaFromParameters(NumericParams p, int index, ViewPort vp, out RectArea rect) {
            int top = p.GetNonZero(index, 1);
            int left = p.GetNonZero(index + 1, 1);
            int bottom = p.GetNonZero(index + 2, vp.Height);
            int right = p.GetNonZero(index + 3, vp.Width);

            if (top > vp.Height || left > vp.Width || bottom < top || right < left) {
                rect = new RectArea();
                return false;
            }

            bottom = Math.Min(bottom, vp.Height);
            right = Math.Min(right, vp.Width);

            rect = new RectArea(
                    top: top.AsRow(),
                    left: left.AsCol(),
                    bottom: bottom.AsRow(),
                    right: right.AsCol()
                );
            return true;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'K')] // EL
        private void ProcessEraseInLine(NumericParams p) {
            int param = p.Get(0, 0);
            DoEraseInLine(param, false);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'K')] // DECSEL
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
            _manipulator.FillSpace(Document.CaretColumn, _manipulator.BufferSize, Document.CurrentDecoration);
        }

        private void SelectiveEraseRight() {
            _manipulator.FillSpaceUnprotected(Document.CaretColumn, _manipulator.BufferSize, Document.CurrentDecoration);
        }

        private void EraseLeft() {
            _manipulator.FillSpace(0, Document.CaretColumn + 1, Document.CurrentDecoration);
        }

        private void SelectiveEraseLeft() {
            _manipulator.FillSpaceUnprotected(0, Document.CaretColumn + 1, Document.CurrentDecoration);
        }

        private void EraseLine() {
            _manipulator.FillSpace(0, _manipulator.BufferSize, Document.CurrentDecoration);
        }

        private void SelectiveEraseLine() {
            _manipulator.FillSpaceUnprotected(0, _manipulator.BufferSize, Document.CurrentDecoration);
        }

        [EscapeSequence(ControlCode.IND)]
        private void Index() {
            Document.UpdateCurrentLine(_manipulator);
            // do LF without considering new-line mode
            Document.LineFeed();
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.NEL)]
        private void ProcessNextLine() {
            Document.UpdateCurrentLine(_manipulator);
            Document.LineFeed();
            _manipulator.Load(Document.CurrentLine);
            Document.CaretColumn = 0;
        }

        [EscapeSequence(ControlCode.RI)]
        private void ReverseIndex() {
            Document.UpdateCurrentLine(_manipulator);
            Document.ReverseLineFeed();
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'r')] // DECSTBM
        private void ProcessSetScrollingRegion(NumericParams p) {
            int height = Document.TerminalHeight;
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

            // top must be smaller than bottom
            if (((topOffset < 0) ? 0 : topOffset) >= ((bottomOffset < 0) ? height - 1 : bottomOffset)) {
                return;
            }

            Document.SetVerticalMargins(topOffset, bottomOffset);
            MoveCursorToOrigin();
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 's')] // SCOSC or DECSLRM
        private void ProcessSCOSCOrDECSLRM(NumericParams p) {
            if (!_enableHorizontalMargins) {
                if (p.Length == 0) {
                    ProcessSCOSC();
                }
                return;
            }

            int width = Document.TerminalWidth;
            int left = p.Get(0, -1);
            int right = p.Get(1, -1);

            int leftOffset;
            if (left <= 0) {
                leftOffset = -1; // left-most of the display area
            }
            else if (left > width) {
                leftOffset = width - 1;
            }
            else {
                leftOffset = left - 1;
            }

            int rightOffset;
            if (right <= 0) {
                rightOffset = -1;  // right-most of the display area
            }
            else if (right > width) {
                rightOffset = width - 1;
            }
            else {
                rightOffset = right - 1;
            }

            // left must be smaller than right
            if (((leftOffset < 0) ? 0 : leftOffset) >= ((rightOffset < 0) ? width - 1 : rightOffset)) {
                return;
            }

            Document.SetHorizontalMargins(leftOffset, rightOffset);
            MoveCursorToOrigin();
        }

        private void MoveCursorToOrigin() {
            ViewPort vp = GetViewPort();
            RowCol rc = vp.GetOrigin();
            MoveCursorTo(rc.Row, rc.Col);
        }

        private void ProcessSCOSC() {
            _savedCursorSCO = CreateSavedCursor();
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'b')] // REP
        private void ProcessRepeatPrecedingChar(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            if (_prevNormalChar.HasValue) {
                for (int i = 0; i < n; i++) {
                    ProcessNormalUnicodeChar(_prevNormalChar.Value);
                }
            }
        }

        [EscapeSequence(ControlCode.ESC, '=')] // DECKPAM
        private void ProcessEnterAlternateKeypadMode() {
            ChangeMode(TerminalMode.Application);
        }

        [EscapeSequence(ControlCode.ESC, '>')] // DECKPNM
        private void ProcessExitAlternateKeypadMode() {
            ChangeMode(TerminalMode.Normal);
        }

        protected override void ChangeMode(TerminalMode mode) {
            if (_terminalMode == mode)
                return;

            if (mode == TerminalMode.Normal) {
                Document.ClearMargins();
                GetConnection().TerminalOutput.Resize(Document.TerminalWidth, Document.TerminalHeight); //たとえばemacs起動中にリサイズし、シェルへ戻るとシェルは新しいサイズを認識していない
                //RMBoxで確認されたことだが、無用に後方にドキュメントを広げてくる奴がいる。カーソルを123回後方へ、など。
                //場当たり的だが、ノーマルモードに戻る際に後ろの空行を削除することで対応する。
                GLine l = Document.LastLine;
                while (l != null && l.DisplayLength == 0 && l.ID > Document.CurrentLineNumber)
                    l = l.PrevLine;

                if (l != null)
                    l = l.NextLine;
                if (l != null)
                    Document.RemoveAfter(l.ID);

                Document.IsApplicationMode = false;
            }
            else {
                Document.ApplicationModeBackColor = ColorSpec.Default;
                Document.IsApplicationMode = true;
            }

            Document.InvalidateAll();

            _terminalMode = mode;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'h')] // SM
        private void SetMode(NumericParams p) {
            DoSetMode(p, true);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'l')] // RM
        private void ResetMode(NumericParams p) {
            DoSetMode(p, false);
        }

        private void DoSetMode(NumericParams p, bool set) {
            foreach (int param in p.EnumerateWithoutNull()) {
                switch (param) {
                    case 2: // Keyboard Action Mode
                        Document.KeySendLocked = set;
                        break;
                    case 4: // Insert Mode
                        _insertMode = set;
                        break;
                    case 12: {	//local echo
                            ITerminalSettings settings = GetTerminalSettings();
                            bool value = !set;
                            _afterExitLockActions.Enqueue(() => {
                                settings.BeginUpdate();
                                settings.LocalEcho = value;
                                settings.EndUpdate();
                            });
                        }
                        break;
                    case 20: // New Line Mode
                        Document.ForceNewLine = set;
                        break;
                }
            }
        }

        [EscapeSequence(ControlCode.ESC, '#', '3')] // DECDHL – Double Height Line / top half
        private void ProcessDoubleHeightLineTopHalf() {
            if (!_enableHorizontalMargins) {
                _manipulator.LineRenderingType = LineRenderingType.QuadUpperHalf;
                Document.UpdateCurrentLine(_manipulator);
            }
        }

        [EscapeSequence(ControlCode.ESC, '#', '4')] // DECDHL – Double Height Line / bottom half
        private void ProcessDoubleHeightLineBottomHalf() {
            if (!_enableHorizontalMargins) {
                _manipulator.LineRenderingType = LineRenderingType.QuadLowerHalf;
                Document.UpdateCurrentLine(_manipulator);
            }
        }

        [EscapeSequence(ControlCode.ESC, '#', '5')] // DECSWL – Single-width Line
        private void ProcessSingleWidthLine() {
            _manipulator.LineRenderingType = LineRenderingType.Normal;
            Document.UpdateCurrentLine(_manipulator);
        }

        [EscapeSequence(ControlCode.ESC, '#', '6')] // DECDWL – Double-Width Line
        private void ProcessDoubleWidthLine() {
            if (!_enableHorizontalMargins) {
                _manipulator.LineRenderingType = LineRenderingType.DoubleWidth;
                Document.UpdateCurrentLine(_manipulator);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'p')] // DECRQM (ANSI mode)
        private void RequestMode(NumericParams p) {
            int param = p.Get(0, 65535);
            bool? status = GetANSIMode(param);
            string value = status.HasValue
                ? (status.Value
                    ? "1" // set
                    : "2" // reset
                )
                : "0"; // not recognized
            byte[] data = MakeCSI(param.ToInvariantString() + ";" + value + "$y");
            TransmitDirect(data);
        }

        private bool? GetANSIMode(int param) {
            switch (param) {
                case 2: // Keyboard Action Mode
                    return Document.KeySendLocked;
                case 4: // Insert Mode
                    return _insertMode;
                case 12: //local echo
                    return !GetTerminalSettings().LocalEcho;
                case 20: // New Line Mode
                    return Document.ForceNewLine;
                default:
                    return null;
            }
        }

        [EscapeSequence(ControlCode.OSC, EscapeSequenceParamType.ControlString, ControlCode.ST)] // OSC
        [EscapeSequence(ControlCode.OSC, EscapeSequenceParamType.ControlString, ControlCode.BEL)]
        private void ProcessOSC(string paramText) {
            OSCParams p;
            if (!OSCParams.Parse(paramText, out p)) {
                Debug.WriteLine("invalid OSC format: {0}", new object[] { paramText });
                return;
            }

            switch (p.GetCode()) {
                case 0:
                case 2:
                    OSCChangeWindowTitle(p);
                    return;

                case 1:
                    return;

                case 4:
                    OSCChangeColorPalette(p, paramText);
                    return;
            }
        }

        private void OSCChangeWindowTitle(OSCParams p) {
            string title = p.GetText();
            title = ParseUtf8String(title);

            IDynamicCaptionFormatter[] formatters = TerminalEmulatorPlugin.Instance.DynamicCaptionFormatter;
            IDynamicCaptionFormatter formatter = (formatters != null && formatters.Length > 0) ? formatters[0] : new DefaultDynamicCaptionFormatter();

            ITerminalSettings settings = GetTerminalSettings();
            string formatted = formatter.FormatCaptionUsingWindowTitle(GetConnection().Destination, settings, title);

            Document.SetSubCaption(formatted);
        }

        private string ParseUtf8String(string s) {
            List<byte> utf8data = new List<byte>();

            foreach (char ch in s) {
                if (ch > 0xf4) {
                    continue;
                }
                if (ch < 0x20) {
                    utf8data.Add((byte)0x20); // space
                }
                else {
                    utf8data.Add((byte)ch);
                }
            }

            Encoding utf8 = Encoding.GetEncoding("UTF-8", EncoderFallback.ReplacementFallback, DecoderFallback.ReplacementFallback);
            return utf8.GetString(utf8data.ToArray());
        }

        private void OSCChangeColorPalette(OSCParams p, string source) {
            // Xterm parses color spec with XParseColor in Xlib.
            // The "Color Strings" section of "Xlib - C Language X Interface" explains details of the color spec.
            //
            // 1. RGB Device String
            //
            //   rgb:<red>/<green>/<blue>
            //
            //   <red>, <green>, <blue> := h | hh | hhh | hhhh
            //   (see XcmsLRGB_RGB_ParseString in libX11)
            //
            // 2. Old RGB Device String
            //
            //   #RGB           (4 bits each)
            //   #RRGGBB        (8 bits each)
            //   #RRRGGGBBB     (12 bits each)
            //   #RRRRGGGGBBBB  (16 bits each)
            //
            //   (see XcmsLRGB_RGB_ParseString in libX11)
            //
            // 3. RGB Intensity String
            //
            //   rgbi:<red>/<green>/<blue>
            //
            //   <red>, <green>, <blue> := floating point value between 0.0 and 1.0, inclusive
            //   That floating point values are those that sscanf can parse.
            //   The decimal point can be either '.' or ','.
            //   (see XcmsLRGB_RGBi_ParseString in libX11)
            //
            // 4. Device-Independent String
            //
            //   CIEXYZ:<X>/<Y>/<Z>
            //   CIEuvY:<u>/<v>/<Y>
            //   CIExyY:<x>/<y>/<Y>
            //   CIELab:<L>/<a>/<b>
            //   CIELuv:<L>/<u>/<v>
            //   TekHVC:<H>/<V>/<C>
            //
            //   All of the values (C, H, V, X, Y, Z, a, b, u, v, y, x) are floating-point values.
            //   That floating point values are those that sscanf can parse.
            //   The decimal point can be either '.' or ','.
            //   (see CIEXYZ_ParseString, or similar for other color spaces, in libX11)
            //

            bool needRepaint = false;

            try {
                while (p.HasNextParam()) {
                    int colorNumber;
                    if (!p.TryGetNextInteger(out colorNumber)) {
                        Debug.WriteLine("(OSC) invalid color number : {0}", new object[] { source });
                        return;
                    }
                    if (colorNumber < 0 || colorNumber > 255) {
                        Debug.WriteLine("(OSC) unsupported color number : {0}", new object[] { source });
                        return;
                    }

                    string spec;
                    if (!p.TryGetNextText(out spec)) {
                        Debug.WriteLine("(OSC) missing color spec : {0}", new object[] { source });
                        return;
                    }

                    if (spec == "?") {
                        OSCReportColor(colorNumber);
                        continue;
                    }

                    int r, g, b;

                    if (spec.StartsWith("#")) {
                        if (!OSCParseOldRGB(spec, out r, out g, out b)) {
                            Debug.WriteLine("(OSC) invalid old RGB color spec : {0}", new object[] { spec });
                            return;
                        }
                    }
                    else if (!OSCParseColorSpec(spec, out r, out g, out b)) {
                        return;
                    }

                    // Debug.WriteLine("color: {0} / {1} / {2}", r, g, b);

                    GetRenderProfile().ESColorSet[colorNumber] = new ESColor(Color.FromArgb(r, g, b), true);
                    needRepaint = true;
                }
            }
            finally {
                if (needRepaint) {
                    Document.InvalidateAll();
                }
            }
        }

        private void OSCReportColor(int colorNumber) {
            Color color = GetRenderProfile().ESColorSet[colorNumber].Color;
            byte[] response = MakeOSC_ST(
                "4;"
                + colorNumber.ToInvariantString()
                + ";rgb:"
                + OSCReportColorFormatComponent(color.R) + "/"
                + OSCReportColorFormatComponent(color.G) + "/"
                + OSCReportColorFormatComponent(color.B)
            );
            TransmitDirect(response);
        }

        private string OSCReportColorFormatComponent(byte v) {
            uint c = ((uint)v << 8) + v;
            return String.Format(NumberFormatInfo.InvariantInfo, "{0:x4}", c);
        }

        private bool OSCParseOldRGB(string spec, out int r, out int g, out int b) {
            string hex = spec.Substring(1); // drop '#'

            if (hex.Length != 3 && hex.Length != 6 && hex.Length != 9 && hex.Length != 12) {
                r = g = b = 0;
                return false;
            }

            ulong rgb;
            if (!UInt64.TryParse(hex, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out rgb)) {
                r = g = b = 0;
                return false;
            }

            int bits = hex.Length * 4 / 3;
            ulong mask = (1ul << bits) - 1;

            if (bits < 8) {
                int shift = 8 - bits;
                b = (int)(rgb & mask) << shift;
                rgb >>= bits;
                g = (int)(rgb & mask) << shift;
                rgb >>= bits;
                r = (int)(rgb & mask) << shift;
            }
            else {
                int shift = bits - 8;
                b = (int)(rgb & mask) >> shift;
                rgb >>= bits;
                g = (int)(rgb & mask) >> shift;
                rgb >>= bits;
                r = (int)(rgb & mask) >> shift;
            }
            return true;
        }

        private bool OSCParseColorSpec(string spec, out int r, out int g, out int b) {
            int c = spec.IndexOf(':');
            if (c < 0) {
                Debug.WriteLine("(OSC) invalid color spec : {0}", new object[] { spec });
                r = g = b = 0;
                return false;
            }

            string name = spec.Substring(0, c);
            string[] para = spec.Substring(c + 1).Split('/');

            switch (name) {
                case "rgb":
                    if (!OSCParseRGB(spec, para, out r, out g, out b)) {
                        Debug.WriteLine("(OSC) invalid RGB color spec : {0}", new object[] { spec });
                        return false;
                    }
                    return true;
                case "rgbi":
                    if (!OSCParseRGBi(spec, para, out r, out g, out b)) {
                        Debug.WriteLine("(OSC) invalid RGBi color spec : {0}", new object[] { spec });
                        return false;
                    }
                    return true;
                default:
                    Debug.WriteLine("(OSC) unsupported color space : {0}", new object[] { spec });
                    r = g = b = 0;
                    return false;
            }
        }

        private bool OSCParseRGB(string spec, string[] para, out int r, out int g, out int b) {
            if (para.Length != 3) {
                r = g = b = 0;
                return false;
            }

            int[] v = new int[3];
            for (int i = 0; i < 3; i++) {
                string hex = para[i];
                if (hex.Length < 1 || hex.Length > 4) {
                    r = g = b = 0;
                    return false;
                }

                uint c;
                if (!UInt32.TryParse(hex, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out c)) {
                    r = g = b = 0;
                    return false;
                }

                int bits = hex.Length * 4;
                if (bits == 8) {
                    v[i] = (int)c;
                }
                else {
                    v[i] = (int)((c * 0xffu) / ((1u << bits) - 1)); // Xlib do this
                }
            }

            r = v[0];
            g = v[1];
            b = v[2];
            return true;
        }

        private bool OSCParseRGBi(string spec, string[] para, out int r, out int g, out int b) {
            if (para.Length != 3) {
                r = g = b = 0;
                return false;
            }

            int[] v = new int[3];
            for (int i = 0; i < 3; i++) {
                string f = para[i];

                f = f.Replace(',', '.');

                double c;
                if (!Double.TryParse(f, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent, NumberFormatInfo.InvariantInfo, out c)) {
                    r = g = b = 0;
                    return false;
                }

                // use linear transformation
                if (c < 0.0) {
                    v[i] = 0;
                }
                else if (c > 1.0) {
                    v[i] = 255;
                }
                else {
                    v[i] = Math.Min((int)(c * 255.5), 255);
                }
            }

            r = v[0];
            g = v[1];
            b = v[2];
            return true;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'm')] // SGR
        private void ProcessSGR(NumericParams p) {
            TextDecoration dec = Document.CurrentDecoration;

            int paramIndex = 0;
            while (paramIndex < p.Length) {

                if (p.IsIntegerCombination(paramIndex)) {
                    int[] subParams = p.GetIntegerCombination(paramIndex++)
                                        .Select(v => v.GetValueOrDefault(0))
                                        .ToArray();
                    if (subParams.Length >= 1 && (subParams[0] == 38 || subParams[0] == 48)) {
                        // 38/48 : 2/5 : ... (colon-separated)
                        bool isForeColor = subParams[0] == 38;
                        subParams = subParams.Skip(1).ToArray();
                        dec = SetColorBySGRSubParams(dec, isForeColor, subParams);
                    }
                    continue;
                }

                int code = p.Get(paramIndex++, 0);
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
                        dec = dec.GetCopyWithHidden(true);
                        break;
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
                        dec = dec.GetCopyWithHidden(false);
                        break;
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
                    case 38: // Set foreground color (XTERM,ISO-8613-3)
                    case 48: // Set background color (XTERM,ISO-8613-3)
                        if (p.IsIntegerCombination(paramIndex)) {
                            // 38/48 ; 2/5 : ... (colon-separated)
                            int[] subParams = p.GetIntegerCombination(paramIndex++)
                                                .Select(v => v.GetValueOrDefault(0))
                                                .ToArray();
                            bool isForeColor = code == 38;
                            dec = SetColorBySGRSubParams(dec, isForeColor, subParams);
                        }
                        else {
                            int type = p.Get(paramIndex++, 0);
                            if (type == 2) { // RGB
                                // sub parameters are semicolon-separated: supports only KDE konsole format
                                // 38/48 ; 2 ; R ; G ; B
                                int r = p.Get(paramIndex++, 0);
                                int g = p.Get(paramIndex++, 0);
                                int b = p.Get(paramIndex++, 0);
                                if (r >= 0 && r <= 255 && g >= 0 && g <= 255 && b >= 0 && b <= 255) {
                                    if (code == 38) {
                                        dec = SetForeColorByRGB(dec, r, g, b);
                                    }
                                    else {
                                        dec = SetBackColorByRGB(dec, r, g, b);
                                    }
                                }
                            }
                            else if (type == 5) { // indexed
                                // sub parameters are semicolon-separated
                                // 38/48 ; 5 ; N
                                int n = p.Get(paramIndex++, 0);
                                if (n >= 0 && n <= 255) {
                                    if (code == 38) {
                                        dec = SelectForeColor(dec, n);
                                    }
                                    else {
                                        dec = SelectBackgroundColor(dec, n);
                                    }
                                }
                            }
                        }
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
                        Debug.WriteLine("unknown SGR code : {0}", code);
                        break;
                }
            }

            Document.CurrentDecoration = dec;
        }

        private TextDecoration SetColorBySGRSubParams(TextDecoration dec, bool isForeColor, int[] p) {
            if (p.Length == 0) {
                return dec;
            }
            int type = p[0];
            if (type == 2) { // RGB
                if (p.Length == 4) {
                    // konsole, xterm
                    // { 2, <R>, <G>, <B> }
                    if (p[1] >= 0 && p[1] <= 255 && p[2] >= 0 && p[2] <= 255 && p[3] >= 0 && p[3] <= 255) {
                        if (isForeColor) {
                            return SetForeColorByRGB(dec, p[1], p[2], p[3]);
                        }
                        else {
                            return SetBackColorByRGB(dec, p[1], p[2], p[3]);
                        }
                    }
                }
                else if (p.Length >= 5) {
                    // ITU-T T.416 / ISO-8613-6
                    // { 2, <colour-space-identifier>, <R>, <G>, <B>, <none>, <tolerance>, <tolerance-color-space> }
                    if (p[2] >= 0 && p[2] <= 255 && p[3] >= 0 && p[3] <= 255 && p[4] >= 0 && p[4] <= 255) {
                        if (isForeColor) {
                            return SetForeColorByRGB(dec, p[2], p[3], p[4]);
                        }
                        else {
                            return SetBackColorByRGB(dec, p[2], p[3], p[4]);
                        }
                    }
                }
            }
            else if (type == 5) { // indexed
                // { 5, <index> }
                if (p.Length >= 2) {
                    if (p[1] >= 0 && p[1] <= 255) {
                        if (isForeColor) {
                            return SelectForeColor(dec, p[1]);
                        }
                        else {
                            return SelectBackgroundColor(dec, p[1]);
                        }
                    }
                }
            }
            return dec;
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

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '#', '{')] // XTPUSHSGR
        private void ProcessPushSGR(NumericParams p) {
            if (_sgrStack.Count >= MAX_SGR_STACK) {
                return;
            }

            SGRStackMask mask;
            if (p.Length == 0 || (p.Length == 1 && p.Get(0, 0) == 0)) {
                mask = (SGRStackMask)0xffffu; // all
            }
            else {
                mask = SGRStackMask.None;
                foreach (int n in p.EnumerateWithoutNull()) {
                    switch (n) {
                        case 1:
                            mask |= SGRStackMask.Bold;
                            break;
                        case 2:
                            // Faint: not supported
                            break;
                        case 3:
                            // Italic: not supported
                            break;
                        case 4:
                            mask |= SGRStackMask.Underline;
                            break;
                        case 5:
                            mask |= SGRStackMask.Blink;
                            break;
                        case 7:
                            mask |= SGRStackMask.Inverted;
                            break;
                        case 8:
                            mask |= SGRStackMask.Hidden;
                            break;
                        case 9:
                            // Strikeout: not supported
                            break;
                        case 21:
                            // Doubly underline: not supported
                            break;
                        case 30:
                            mask |= SGRStackMask.ForegroundColor;
                            break;
                        case 31:
                            mask |= SGRStackMask.BackgroundColor;
                            break;
                    }
                }
            }

            _sgrStack.AddLast(new SGRStackItem(Document.CurrentDecoration, mask));
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '#', '}')] // XTPOPSGR
        private void ProcessPopSGR() {
            if (_sgrStack.Last == null) {
                return;
            }

            SGRStackItem item = _sgrStack.Last.Value;
            _sgrStack.RemoveLast();

            TextDecoration dec = Document.CurrentDecoration;
            SGRStackMask mask = item.Mask;
            if ((mask & SGRStackMask.Bold) != 0) {
                dec = dec.GetCopyWithBold(item.Decoration.Bold);
            }
            if ((mask & SGRStackMask.Underline) != 0) {
                dec = dec.GetCopyWithUnderline(item.Decoration.Underline);
            }
            if ((mask & SGRStackMask.Blink) != 0) {
                dec = dec.GetCopyWithBlink(item.Decoration.Blink);
            }
            if ((mask & SGRStackMask.Inverted) != 0) {
                dec = dec.GetCopyWithInverted(item.Decoration.Inverted);
            }
            if ((mask & SGRStackMask.Hidden) != 0) {
                dec = dec.GetCopyWithHidden(item.Decoration.Hidden);
            }
            if ((mask & SGRStackMask.ForegroundColor) != 0) {
                dec = dec.GetCopyWithForeColor(item.Decoration.GetForeColorSpec());
            }
            if ((mask & SGRStackMask.BackgroundColor) != 0) {
                dec = dec.GetCopyWithBackColor(item.Decoration.GetBackColorSpec());
            }

            Document.CurrentDecoration = dec;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '#', '|')] // XTREPORTSGR
        private void ProcessReportSGR(NumericParams p) {
            ViewPort vp = GetViewPort();
            RectArea rect;
            if (!ReadRectAreaFromParameters(p, 0, vp, out rect)) {
                return;
            }

            Document.UpdateCurrentLine(_manipulator);

            TextDecoration commonDec = null;
            int bottomLineNumber = vp.ToLineNumber(rect.Bottom);
            int columnStart = vp.ToCaretColumn(rect.Left);
            int columnEnd = vp.ToCaretColumn(rect.Right) + 1;

            GLine l = Document.FindLineOrEdge(vp.ToLineNumber(rect.Top));
            while (l != null && l.ID <= bottomLineNumber) {
                _manipulator.Load(l);
                for (int index = columnStart; index < columnEnd; index++) {
                    TextDecoration d;
                    if (_manipulator.GetAttributes(index, out d)) {
                        if (commonDec != null) {
                            commonDec = commonDec.GetCommon(d);
                        }
                        else {
                            commonDec = d;
                        }
                    }
                }
                l = l.NextLine;
            }

            _manipulator.Load(Document.CurrentLine);

            string response = "0";
            if (commonDec != null) {
                if (commonDec.Bold) {
                    response += ";1";
                }
                if (commonDec.Underline) {
                    response += ";4";
                }
                if (commonDec.Blink) {
                    response += ";5";
                }
                if (commonDec.Inverted) {
                    response += ";7";
                }
                if (commonDec.Hidden) {
                    response += ";8";
                }

                {
                    ColorSpec foreColor = commonDec.GetForeColorSpec();
                    if (foreColor.ColorType == ColorType.Custom8bit) {
                        if (foreColor.ColorCode >= 16) {
                            response = response + ";38:5:" + foreColor.ColorCode.ToInvariantString();
                        }
                        else if (foreColor.ColorCode >= 8) {
                            response = response + ";" + (foreColor.ColorCode + 82).ToInvariantString();
                        }
                        else if (foreColor.ColorCode >= 0) {
                            response = response + ";" + (foreColor.ColorCode + 30).ToInvariantString();
                        }
                    }
                    else if (foreColor.ColorType == ColorType.Custom24bit) {
                        response = response + ";38:2::"
                            + ((int)foreColor.Color.R).ToInvariantString() + ":"
                            + ((int)foreColor.Color.G).ToInvariantString() + ":"
                            + ((int)foreColor.Color.B).ToInvariantString();
                    }
                }
                {
                    ColorSpec backColor = commonDec.GetBackColorSpec();
                    if (backColor.ColorType == ColorType.Custom8bit) {
                        if (backColor.ColorCode >= 16) {
                            response = response + ";48:5:" + backColor.ColorCode.ToInvariantString();
                        }
                        else if (backColor.ColorCode >= 8) {
                            response = response + ";" + (backColor.ColorCode + 92).ToInvariantString();
                        }
                        else if (backColor.ColorCode >= 0) {
                            response = response + ";" + (backColor.ColorCode + 40).ToInvariantString();
                        }
                    }
                    else if (backColor.ColorType == ColorType.Custom24bit) {
                        response = response + ";48:2::"
                            + ((int)backColor.Color.R).ToInvariantString() + ":"
                            + ((int)backColor.Color.G).ToInvariantString() + ":"
                            + ((int)backColor.Color.B).ToInvariantString();
                    }
                }
            }
            response += "m";

            byte[] data = MakeCSI(response);
            TransmitDirect(data);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '"', 'q')] // DECSCA
        private void ProcessProtectCharacter(NumericParams p) {
            int param = p.Get(0, 0);

            switch (param) {
                case 0:
                case 2:
                    Document.CurrentDecoration = Document.CurrentDecoration.GetCopyWithProtected(false);
                    break;
                case 1:
                    Document.CurrentDecoration = Document.CurrentDecoration.GetCopyWithProtected(true);
                    break;
                default:
                    break;
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'h')] // DECSET
        private void ProcessDECSET(NumericParams p) {
            DoDECSETMultiple(p, true);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'l')] // DECRST
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
                    Document.ClearMargins();
                    ClearScreen();
                    MoveCursorTo(new Row(1), new Col(1));
                    break;
                case 4:	//Smooth Scroll
                    break;
                case 5:
                    SetReverseVideo(set);
                    break;
                case 6:	//Origin Mode
                    _originRelative = set;
                    MoveCursorToOrigin();
                    break;
                case 7:
                    _wrapAroundMode = set;
                    break;
                case 25: // Show/Hide cursor
                    Document.ShowCaret = set;
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
                case 69: // Enable left and right margin mode (DECLRMM)
                    _enableHorizontalMargins = set;
                    Document.ClearHorizontalMargins();
                    if (set) {
                        ResetLineRenderingType();
                    }
                    break;
                case 80: // Sixel display mode (DECSDM)
                    _sixelDisplayMode = set;
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
                case 1070:  // Private color registers
                    _usePrivateColorRegisters = set;
                    break;
                case 2004:    // Set/Reset bracketed paste mode
                    _bracketedPasteMode = set;
                    break;
                case 8452:  // Sixel scrolling leaves cursor to right of graphic
                    _sixelScrollingCursorRight = set;
                    break;
            }
        }

        private void ResetLineRenderingType() {
            int bottomLineNumber = Document.TopLineNumber + Document.TerminalHeight - 1;
            GLine l = Document.TopLine;
            while (l != null && l.ID <= bottomLineNumber) {
                bool updated = l.SetLineRenderingType(LineRenderingType.Normal);
                if (updated) {
                    Document.InvalidatedRegion.InvalidateLine(l.ID);
                }
                l = l.NextLine;
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, '$', 'p')] // DECRQM (DEC private mode)
        private void RequestDECMode(NumericParams p) {
            int param = p.Get(0, 65535);
            bool? status = GetDECMode(param);
            string value = status.HasValue
                ? (status.Value
                    ? "1" // set
                    : "2" // reset
                )
                : "0"; // not recognized
            byte[] data = MakeCSI("?" + param.ToInvariantString() + ";" + value + "$y");
            TransmitDirect(data);
        }

        private bool? GetDECMode(int param) {
            switch (param) {
                case 1:
                    return CursorKeyMode == TerminalMode.Application;
                case 5:
                    return ReverseVideo;
                case 6:
                    return _originRelative;
                case 7:
                    return _wrapAroundMode;
                case 25:
                    return Document.ShowCaret;
                case 47:
                case 1047:
                case 1049:
                    return _isAlternateBuffer;
                case 66:
                    return TerminalMode == TerminalMode.Application;
                case 69:
                    return _enableHorizontalMargins;
                case 80:
                    return _sixelDisplayMode;
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
                case 1070:
                    return _usePrivateColorRegisters;
                case 2004:
                    return _bracketedPasteMode;
                case 8452:
                    return _sixelScrollingCursorRight;
                default:
                    return null;
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 's')] // Save DEC private mode values
        private void ProcessSaveDECSET(NumericParams p) {
            foreach (int param in p.EnumerateWithoutNull()) {
                bool? status = GetDECMode(param);
                if (status.HasValue) {
                    _savedDecModes[param] = status.Value;
                }
            }
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'r')] // Restore DEC private mode values
        private void ProcessRestoreDECSET(NumericParams p) {
            foreach (int param in p.EnumerateWithoutNull()) {
                bool status;
                if (_savedDecModes.TryGetValue(param, out status)) {
                    DoDECSET(param, status);
                }
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'r')] // DECCARA
        private void ProcessChangeAttributesRect(NumericParams p) {
            ViewPort vp = GetViewPort();
            RectArea rect;
            if (!ReadRectAreaFromParameters(p, 0, vp, out rect)) {
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

            ChangeAttribute(ref vp, ref rect, (manip, from, to) => manip.ModifyAttributes(from, to, mod));
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 't')] // DECRARA
        private void ProcessReverseAttributesRect(NumericParams p) {
            ViewPort vp = GetViewPort();
            RectArea rect;
            if (!ReadRectAreaFromParameters(p, 0, vp, out rect)) {
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

            ChangeAttribute(ref vp, ref rect, (manip, from, to) => manip.ReverseAttributes(from, to, mod));
        }

        private void ChangeAttribute(ref ViewPort vp, ref RectArea rect, Action<GLineManipulator, int, int> changeAttributes) {
            Document.UpdateCurrentLine(_manipulator);

            int rectTopLineNumber = vp.ToLineNumber(rect.Top);
            int rectBottomLineNumber = vp.ToLineNumber(rect.Bottom);

            Document.EnsureLine(rectBottomLineNumber);

            if (_rectangularAttributeChange) {
                GLine l = Document.FindLineOrEdge(rectTopLineNumber);
                while (l != null && l.ID <= rectBottomLineNumber) {
                    _manipulator.Load(l);
                    changeAttributes(_manipulator, vp.ToCaretColumn(rect.Left), vp.ToCaretColumn(rect.Right + 1));
                    _manipulator.ExportTo(l);
                    Document.InvalidatedRegion.InvalidateLine(l.ID);
                    l = l.NextLine;
                }
            }
            else {
                GLine l = Document.FindLineOrEdge(rectTopLineNumber);
                while (l != null && l.ID <= rectBottomLineNumber) {
                    _manipulator.Load(l);
                    changeAttributes(
                        _manipulator,
                        vp.ToCaretColumn((l.ID == rectTopLineNumber) ? rect.Left : new Col(1)),
                        vp.ToCaretColumn((l.ID == rectBottomLineNumber) ? rect.Right + 1 : (vp.Width + 1).AsCol())
                    );
                    _manipulator.ExportTo(l);
                    Document.InvalidatedRegion.InvalidateLine(l.ID);
                    l = l.NextLine;
                }
            }

            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '*', 'x')] // DECSACE
        private void ProcessSelectAttributeChangeExtent(NumericParams p) {
            int mode = p.GetNonZero(0, 0);
            switch (mode) {
                case 0:
                case 1:
                    _rectangularAttributeChange = false;
                    break;
                case 2:
                    _rectangularAttributeChange = true;
                    break;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'v')] // DECCRA
        private void ProcessCopyRect(NumericParams p) {
            ViewPort vp = GetViewPort();
            RectArea srcRect;
            if (!ReadRectAreaFromParameters(p, 0, vp, out srcRect)) {
                return;
            }

            // int srcPage = p.Get(4, 1); // ignored
            int destTop = p.GetNonZero(5, 1);
            int destLeft = p.GetNonZero(6, 1);
            // int destPage = p.GetNonZero(7, 1); // ignored

            if (destTop > vp.Height || destLeft > vp.Width || (destTop == srcRect.Top.Value && destLeft == srcRect.Left.Value)) {
                return;
            }

            Document.UpdateCurrentLine(_manipulator);

            Document.EnsureLine(vp.ToLineNumber(srcRect.Bottom));

            GLine[] copy = new GLine[srcRect.Bottom.Value - srcRect.Top.Value + 1];
            {
                int rectTopLineNumber = vp.ToLineNumber(srcRect.Top);
                int rectBottomLineNumber = vp.ToLineNumber(srcRect.Bottom);
                GLine l = Document.FindLineOrEdge(rectTopLineNumber);
                while (l != null && l.ID <= rectBottomLineNumber) {
                    int offset = l.ID - rectTopLineNumber;
                    if (offset >= 0 && offset < copy.Length) {
                        copy[offset] = l.CloneWithoutUpdateSpans();
                    }
                    l = l.NextLine;
                }
            }

            int destTopLineNumber = vp.ToLineNumber(destTop.AsRow());
            int destBottomLimit = vp.ToLineNumber(Math.Min(destTop + srcRect.Bottom.Value - srcRect.Top.Value, vp.Height).AsRow());

            Document.EnsureLine(destBottomLimit);

            GLine destLine = Document.FindLineOrEdge(destTopLineNumber);
            GLineManipulator srcManipurator = new GLineManipulator(Document.GLineZOrderManager);
            while (destLine != null && destLine.ID <= destBottomLimit) {
                int offset = destLine.ID - destTopLineNumber;
                if (offset >= 0 && offset < copy.Length) {
                    if (copy[offset] != null) {
                        srcManipurator.Load(copy[offset]);
                        _manipulator.Load(destLine);
                        _manipulator.ExpandBuffer(Document.TerminalWidth);
                        _manipulator.CopyFrom(srcManipurator, srcRect.Left.Value - 1, srcRect.Right.Value, destLeft - 1);
                        _manipulator.ExportTo(destLine);
                        Document.InvalidatedRegion.InvalidateLine(destLine.ID);
                    }
                }
                destLine = destLine.NextLine;
            }

            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '$', 'x')] // DECFRA
        private void ProcessFillRect(NumericParams p) {
            int charCode = p.Get(0, 0);
            if (!((charCode >= 32 && charCode <= 126) || (charCode >= 160 && charCode <= 255))) {
                return;
            }

            ViewPort vp = GetViewPort();
            RectArea rect;
            if (!ReadRectAreaFromParameters(p, 1, vp, out rect)) {
                return;
            }

            char charVal = CharacterSetManager.GetCharacer((byte)charCode).GetValueOrDefault(' ');

            UnicodeChar fillChar = new UnicodeChar(charVal, false);

            Document.UpdateCurrentLine(_manipulator);

            int bottomLineNumber = vp.ToLineNumber(rect.Bottom);
            int fillStart = vp.ToCaretColumn(rect.Left);
            int fillEnd = vp.ToCaretColumn(rect.Right) + 1;

            Document.EnsureLine(bottomLineNumber);

            GLine l = Document.FindLineOrEdge(vp.ToLineNumber(rect.Top));
            while (l != null && l.ID <= bottomLineNumber) {
                _manipulator.Load(l);
                _manipulator.FillCharacter(fillStart, fillEnd, fillChar, Document.CurrentDecoration);
                _manipulator.ExportTo(l);
                Document.InvalidatedRegion.InvalidateLine(l.ID);
                l = l.NextLine;
            }

            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '*', 'y')] // DECRQCRA
        private void ProcessRequestChecksumRect(NumericParams p) {
            int pid = p.Get(0, 65535);
            // int page = p.Get(0, 0); // ignored

            ViewPort vp = GetViewPort();
            RectArea rect;
            ushort sum = 0;

            if (ReadRectAreaFromParameters(p, 2, vp, out rect)) {
                Document.UpdateCurrentLine(_manipulator);

                int bottomLineNumber = vp.ToLineNumber(rect.Bottom);
                int sumStart = vp.ToCaretColumn(rect.Left);
                int sumEnd = vp.ToCaretColumn(rect.Right) + 1;

                Document.EnsureLine(bottomLineNumber);

                GLine l = Document.FindLineOrEdge(vp.ToLineNumber(rect.Top));
                while (l != null && l.ID <= bottomLineNumber) {
                    _manipulator.Load(l);
                    for (int i = sumStart; i < sumEnd; i++) {
                        // Note: this implementation is not compatible with xterm
                        uint c;
                        UnicodeChar ch;
                        bool isRightHalf;
                        if (_manipulator.GetChar(i, out ch, out isRightHalf)) {
                            c = ch.CodePoint;
                            if (c >= 0x100u) {
                                c = 0x1bu;
                            }
                            else if (c >= 0x80u) {
                                c &= 0x7fu;
                            }
                        }
                        else {
                            c = 0x20;
                        }

                        TextDecoration dec;
                        if (_manipulator.GetAttributes(i, out dec)) {
                            if (dec.Underline) {
                                c += 0x10u;
                            }
                            if (dec.Inverted) {
                                c += 0x20u;
                            }
                            if (dec.Blink) {
                                c += 0x40u;
                            }
                            if (dec.Bold) {
                                c += 0x80u;
                            }
                        }

                        sum += (ushort)c;
                    }
                    l = l.NextLine;
                }

                _manipulator.Load(Document.CurrentLine);
            }

            sum = (ushort)(-((int)sum));

            // DECCKSR
            byte[] response = MakeDCS_ST(pid.ToInvariantString() + "!~" + sum.ToString("X4", NumberFormatInfo.InvariantInfo));
            TransmitDirect(response);
        }

        [EscapeSequence(ControlCode.CSI, '?', EscapeSequenceParamType.Numeric, 'S')] // Query Graphics (xterm)
        private void ProcessQueryGraphics(NumericParams p) {
            if (p.Length < 2) {
                return;
            }

            int item = p.Get(0, 0);
            int action = p.Get(1, 0);

            // TODO

            int result = 1; // item error

            byte[] data = MakeCSI("?" + item.ToInvariantString() + ";" + result.ToInvariantString() + "S");
            TransmitDirect(data);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 't')] // Window Manipulation (xterm)
        private void ProcessWindowManipulation(NumericParams p) {
            int cmd = p.Get(0, -1);

            switch (cmd) {
                case 11: // Report xterm window state
                    TransmitDirect(MakeCSI("1t"));
                    break;
                case 13: {
                        int sub = p.Get(1, -1);
                        if (sub == -1) {
                            // Report xterm window position
                            TransmitDirect(MakeCSI("3;0;0t"));
                        }
                        else if (sub == 2) {
                            // Report xterm text-area position
                            TransmitDirect(MakeCSI("3;0;0t"));
                        }
                        break;
                    }
                case 14:
                case 15: {
                        RenderProfile renderProfile = GetRenderProfile();
                        int sw = (int)(Document.TerminalWidth * renderProfile.Pitch.Width);
                        int sh = (int)(Document.TerminalHeight * (renderProfile.Pitch.Height + renderProfile.LineSpacing) - renderProfile.LineSpacing);

                        if (cmd == 14) {
                            int sub = p.Get(1, -1);
                            if (sub == -1) {
                                // Report xterm text area size in pixels
                                TransmitDirect(MakeCSI("4;" + sh.ToInvariantString() + ";" + sw.ToInvariantString() + "t"));
                            }
                            else if (sub == 2) {
                                // Report xterm window size in pixels
                                TransmitDirect(MakeCSI("4;" + sh.ToInvariantString() + ";" + sw.ToInvariantString() + "t"));
                            }
                        }
                        else {
                            // Report size of the screen in pixels
                            TransmitDirect(MakeCSI("5;" + sh.ToInvariantString() + ";" + sw.ToInvariantString() + "t"));
                        }
                        break;
                    }
                case 16: {
                        // Report xterm character size in pixels
                        RenderProfile renderProfile = GetRenderProfile();
                        int cw = (int)renderProfile.Pitch.Width;
                        int ch = (int)renderProfile.Pitch.Height;
                        TransmitDirect(MakeCSI("6;" + ch.ToInvariantString() + ";" + cw.ToInvariantString() + "t"));
                        break;
                    }
                case 18:
                case 19: {
                        int w = Document.TerminalWidth;
                        int h = Document.TerminalHeight;
                        if (cmd == 18) {
                            // Report the size of the text area in characters
                            TransmitDirect(MakeCSI("8;" + h.ToInvariantString() + ";" + w.ToInvariantString() + "t"));
                        }
                        else {
                            // Report the size of the screen in characters
                            TransmitDirect(MakeCSI("9;" + h.ToInvariantString() + ";" + w.ToInvariantString() + "t"));
                        }
                        break;
                    }
                case 20:
                case 21: {
                        string label = Document.SubCaption;
                        string ascii_label = new String(label.Select(ch => (ch >= 0x20 && ch <= 0x7e) ? ch : '_').ToArray());
                        if (cmd == 20) {
                            // Report xterm window's icon label
                            TransmitDirect(MakeOSC_ST("L" + ascii_label));
                        }
                        else {
                            // Report xterm window's icon label
                            TransmitDirect(MakeOSC_ST("l" + ascii_label));
                        }
                        break;
                    }
            }
        }

        private void ResetMouseTracking(MouseTrackingState newState) {
            if (newState != MouseTrackingState.Off) {
                if (_mouseTrackingState == MouseTrackingState.Off) {
                    ChangeUICursor(Cursors.Arrow);
                }
            }
            else {
                if (_mouseTrackingState != MouseTrackingState.Off) {
                    ChangeUICursor(null);
                }
            }
            _mouseTrackingState = newState;
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'd')] // VPA
        private void ProcessLinePositionAbsolute(NumericParams p) {
            int row = p.GetNonZero(0, 1);
            Document.UpdateCurrentLine(_manipulator);
            if (_originRelative) {
                Document.CurrentLineNumber = Math.Min(Document.ScrollingTopLineNumber + row - 1, Document.ScrollingBottomLineNumber);
            }
            else {
                Document.CurrentLineNumber = Document.TopLineNumber + Math.Min(row, Document.TerminalHeight) - 1;
            }
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'e')] // VPR
        private void ProcessLinePositionRelative(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            Document.UpdateCurrentLine(_manipulator);
            Document.CurrentLineNumber = Math.Min(Document.CurrentLineNumber + n, Document.TopLineNumber + Document.TerminalHeight - 1);
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'X')] // ECH
        private void ProcessEraseChars(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            _manipulator.FillSpace(Document.CaretColumn, Math.Min(Document.CaretColumn + n, Document.TerminalWidth), Document.CurrentDecoration);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'P')] // DCH
        private void ProcessDeleteChars(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            if (Document.IsCaretColumnInScrollingRegion) {
                _manipulator.DeleteChars(Document.CaretColumn, n, Document.RightMarginOffset + 1, Document.CurrentDecoration);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '@')] // ICH
        private void ProcessInsertBlankCharacters(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            if (Document.IsCaretColumnInScrollingRegion) {
                _manipulator.InsertBlanks(Document.CaretColumn, n, Document.RightMarginOffset + 1, Document.CurrentDecoration);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', '@')] // SL
        private void ProcessShiftLeft(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            if (Document.IsCurrentLineInScrollingRegion && Document.IsCaretColumnInScrollingRegion) {
                ShiftScrollRegion(-n);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ' ', 'A')] // SR
        private void ProcessShiftRight(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            if (Document.IsCurrentLineInScrollingRegion && Document.IsCaretColumnInScrollingRegion) {
                ShiftScrollRegion(n);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'S')] // SU
        private void ProcessScrollUp(NumericParams p) {
            int d = p.GetNonZero(0, 1);
            DoScrollUp(d);
        }

        private void DoScrollUp(int d) {
            Document.UpdateCurrentLine(_manipulator);
            if (!Document.HasTopMargin && !Document.HasBottomMargin && !Document.HasLeftMargin && !Document.HasRightMargin) {
                Document.CurrentLineNumber += d;
                Document.SetTopLineNumber(Document.TopLineNumber + d);
                Document.InvalidateAll();
            }
            else {
                // DEC document describes the SU scrolls the entire screen,
                // but the xterm scrolls the area bounded with margins.

                // TerminalDocument's "Scroll-Down" means that the view port is moved down and the content is scrolled up.
                Document.ScrollDownRegion(d);
            }
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'T')] // SD or Initiate highlight mouse tracking
        private void ProcessScrollDown(NumericParams p) {
            if (p.Length > 1) {
                // ignore highlight tracking information
                // CSI <func> ; <startx> ; <starty> ; <firstrow> ; <lastrow> T
                return;
            }

            int d = p.GetNonZero(0, 1);

            Document.UpdateCurrentLine(_manipulator);
            // DEC document describes the SD scrolls the entire screen,
            // but the xterm scrolls the area bounded with margins.

            // TerminalDocument's "Scroll-Up" means that the view port is moved up and the content is scrolled down.
            Document.ScrollUpRegion(d);
            _manipulator.Load(Document.CurrentLine);
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'L')] // IL
        private void ProcessInsertLines(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            if (Document.IsCurrentLineInScrollingRegion && Document.IsCaretColumnInScrollingRegion) {
                Document.UpdateCurrentLine(_manipulator);
                Document.ScrollUpRegionFrom(Document.CurrentLineNumber, d);
                _manipulator.Load(Document.CurrentLine);
                Document.CaretColumn = Document.LeftMarginOffset;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'M')] // DL
        private void ProcessDeleteLines(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            if (Document.IsCurrentLineInScrollingRegion && Document.IsCaretColumnInScrollingRegion) {
                Document.UpdateCurrentLine(_manipulator);
                Document.ScrollDownRegionFrom(Document.CurrentLineNumber, d);
                _manipulator.Load(Document.CurrentLine);
                Document.CaretColumn = Document.LeftMarginOffset;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '\'', '}')] // DECIC
        private void ProcessInsertColumns(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            if (Document.IsCurrentLineInScrollingRegion && Document.IsCaretColumnInScrollingRegion) {
                Document.UpdateCurrentLine(_manipulator);
                ShiftScrollRegionFrom(Document.CaretColumn, d);
                _manipulator.Load(Document.CurrentLine);
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, '\'', '~')] // DECDC
        private void ProcessDeleteColumns(NumericParams p) {
            int d = p.GetNonZero(0, 1);

            if (Document.IsCurrentLineInScrollingRegion && Document.IsCaretColumnInScrollingRegion) {
                Document.UpdateCurrentLine(_manipulator);
                ShiftScrollRegionFrom(Document.CaretColumn, -d);
                _manipulator.Load(Document.CurrentLine);
            }
        }


        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'I')] // CHT
        private void ProcessForwardTab(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            DoForwardTab(n);
        }

        private void DoForwardTab(int count) {
            if (count <= 0) {
                return;
            }

            bool wrapPending = Document.WrapPending;

            _tabStops.Extend(Document.TerminalWidth);

            int rightLimit = GetCaretColumnRightLimit();

            int col = Document.CaretColumn;

            for (int i = 0; i < count; i++) {
                int? nextCol = _tabStops.GetNextTabStop(col);
                if (nextCol.HasValue && nextCol.Value < rightLimit) {
                    col = nextCol.Value;
                }
                else {
                    col = rightLimit;
                    break;
                }
            }

            Document.CaretColumn = col;
            if (wrapPending) {
                Document.WrapPending = true;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'Z')] // CBT
        private void ProcessBackwardTab(NumericParams p) {
            int n = p.GetNonZero(0, 1);
            DoBackwardTab(n);
        }

        private void DoBackwardTab(int count) {
            if (count <= 0) {
                return;
            }

            bool wrapPending = Document.WrapPending;

            _tabStops.Extend(Document.TerminalWidth);

            // Note:
            // left-limit is different from Xterm.
            // Xterm does not limit movement to the left from the left margin.
            // But such behavior is inconsistent.
            int leftLimit = GetCaretColumnLeftLimit();

            int col = Document.CaretColumn;

            for (int i = 0; i < count; i++) {
                int? nextCol = _tabStops.GetPrevTabStop(col);
                if (nextCol.HasValue && nextCol.Value > leftLimit) {
                    col = nextCol.Value;
                }
                else {
                    col = leftLimit;
                    break;
                }
            }

            Document.CaretColumn = col;
            if (wrapPending) {
                Document.WrapPending = true;
            }
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'g')] // TBC
        private void ProcessTabClear(NumericParams p) {
            int param = p.Get(0, 0);
            if (param == 0) {
                _tabStops.Unset(Document.CaretColumn);
            }
            else if (param == 3) {
                _tabStops.Clear();
            }
        }

        [EscapeSequence(ControlCode.HTS)]
        private void ProcessHTS() {
            _tabStops.Set(Document.CaretColumn);
        }

        private void SwitchBuffer(bool toAlternate) {
            if (_isAlternateBuffer == toAlternate) {
                return;
            }

            Document.UpdateCurrentLine(_manipulator);
            SaveScreen(toAlternate ? 0 : 1);
            RestoreScreen(toAlternate ? 1 : 0);
            _manipulator.Load(Document.CurrentLine);
            _isAlternateBuffer = toAlternate;
        }

        private void SaveScreen(int sw) {
            List<GLine> lines = new List<GLine>();
            GLine l = Document.TopLine;
            int m = l.ID + Document.TerminalHeight;
            while (l != null && l.ID < m) {
                lines.Add(l.CloneWithoutUpdateSpans());
                l = l.NextLine;
            }
            _savedScreen[sw] = lines;
        }

        private void RestoreScreen(int sw) {
            if (_savedScreen[sw] == null) {
                ClearScreen();	// emulate new buffer
                return;
            }
            int w = Document.TerminalWidth;
            int m = Document.TerminalHeight;
            GLine t = Document.TopLine;
            foreach (GLine l in _savedScreen[sw]) {
                this.LogService.TextLogger.ForceWriteLine(l);
                l.ExpandBuffer(w);
                if (t == null)
                    Document.AddLine(l);
                else {
                    Document.Replace(t, l);
                    t = l.NextLine;
                }
                if (--m == 0)
                    break;
            }
        }

        private void ClearScreen() {
            DoEraseInDisplay(2, false, true);
        }

        [EscapeSequence(ControlCode.ESC, '7')] // DECSC
        private void ProcessDECSC() {
            SaveCursor();
        }

        private void SaveCursor() {
            int sw = _isAlternateBuffer ? 1 : 0;
            _savedCursor[sw] = CreateSavedCursor();
        }

        private SavedCursor CreateSavedCursor() {
            int rowIndex = Document.CurrentLineNumber - Document.TopLineNumber;
            int colIndex = Document.CaretColumn;
            CharacterSetMapping csMap = CharacterSetManager.GetCharacterSetMapping();
            return new SavedCursor(
                    rowIndex: rowIndex,
                    colIndex: colIndex,
                    decoration: Document.CurrentDecoration,
                    wrapAroundMode: _wrapAroundMode,
                    scrollRegionRelative: _originRelative,
                    characterSetMapping: csMap
                );
        }

        [EscapeSequence(ControlCode.ESC, '8')] // DECRC
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
                    rowIndex: 0,
                    colIndex: 0,
                    decoration: TextDecoration.Default,
                    wrapAroundMode: true,
                    scrollRegionRelative: false,
                    characterSetMapping: CharacterSetMapping.GetDefault()
                );
            }

            Document.UpdateCurrentLine(_manipulator);
            Document.CurrentLineNumber = Document.TopLineNumber + saved.RowIndex;
            _manipulator.Load(Document.CurrentLine);
            Document.CaretColumn = saved.ColIndex;

            Document.CurrentDecoration = saved.Decoration;

            _wrapAroundMode = saved.WrapAroundMode;

            _originRelative = saved.ScrollRegionRelative;

            CharacterSetManager.RestoreCharacterSetMapping(saved.CharacterSetMapping);
        }

        [EscapeSequence(ControlCode.CSI, 'u')] // SCORC
        private void ProcessSCORC() {
            RestoreCursorInternal(_savedCursorSCO);
        }

        //画面の反転
        private void SetReverseVideo(bool reverse) {
            if (reverse == _reverseVideo)
                return;

            _reverseVideo = reverse;
            Document.InvalidateAll();
        }

        [EscapeSequence(ControlCode.CSI, '!', 'p')] // DECSTR
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
                byte b;
                switch (key) {
                    case Keys.Insert:
                        b = (byte)'2';
                        break;
                    case Keys.Home:
                        b = (byte)'7';
                        break;
                    case Keys.PageUp:
                        b = (byte)'5';
                        break;
                    case Keys.Delete:
                        b = (byte)'3';
                        break;
                    case Keys.End:
                        b = (byte)'8';
                        break;
                    case Keys.PageDown:
                        b = (byte)'6';
                        break;
                    default:
                        throw new ArgumentException(String.Format("unknown key: {0}", key));
                }
                return MakeCSI(b, (byte)'~');
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
                return MakeCSI((byte)'1', (byte)';', (byte)('0' + m), c);
            }
            else {
                return MakeSS3(c);
            }
        }

        private byte[] XtermFunctionKeyF5ToF12(int m, byte c1, byte c2) {
            if (m > 1) {
                return MakeCSI(c1, c2, (byte)';', (byte)('0' + m), (byte)'~');
            }
            else {
                return MakeCSI(c1, c2, (byte)'~');
            }
        }

        // emulate Xterm's modifyCursorKeys
        private byte[] ModifyCursorKey(Keys modifier, Keys key) {
            byte c;
            switch (key) {
                case Keys.Up:
                    c = (byte)'A';
                    break;
                case Keys.Down:
                    c = (byte)'B';
                    break;
                case Keys.Right:
                    c = (byte)'C';
                    break;
                case Keys.Left:
                    c = (byte)'D';
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
                case 2:
                    return MakeCSI((byte)'1', (byte)';', (byte)('0' + m), c);
                case 3:
                    return MakeCSI((byte)'>', (byte)'1', (byte)';', (byte)('0' + m), c);
            }

            return null;
        }

        private byte[] VT100CursorKey(Keys modifier, Keys key) {
            byte b;
            switch (key) {
                case Keys.Up:
                    b = (byte)'A';
                    break;
                case Keys.Down:
                    b = (byte)'B';
                    break;
                case Keys.Right:
                    b = (byte)'C';
                    break;
                case Keys.Left:
                    b = (byte)'D';
                    break;
                default:
                    throw new ArgumentException(String.Format("unknown cursor key code: {0}", key));
            }

            if (_cursorKeyMode == TerminalMode.Normal)
                return MakeCSI(b);
            else
                return MakeSS3(b);
        }

        [EscapeSequence(ControlCode.DCS, EscapeSequenceParamType.Numeric, '$', 't')] // DECRSPS
        private void RestorePresentationStatus(NumericParams p) {
            int param = p.Get(0, 0);
            if (param == 1) {
                // DECCIR
                _escapeSequenceEngine.StartDCSProcessor(new BufferedDCSProcessor(ProcessRestoreDECCIR));
            }
            else if (param == 2) {
                // DECTABSR
                _escapeSequenceEngine.StartDCSProcessor(new BufferedDCSProcessor(ProcessRestoreDECTABSR));
            }
        }

        private void ProcessRestoreDECCIR(string s) {
            string[] p = s.Split(';');
            if (p.Length < 10) {
                return;
            }

            int rowVal;
            if (!ParseNonNegativeInteger(p[0], out rowVal)) {
                return;
            }

            int colVal;
            if (!ParseNonNegativeInteger(p[1], out colVal)) {
                return;
            }

            int page;
            if (!ParseNonNegativeInteger(p[2], out page)) {
                return;
            }

            char srend;
            if (!GetSingleChar(p[3], out srend)) {
                return;
            }

            char satt;
            if (!GetSingleChar(p[4], out satt)) {
                return;
            }

            char sflag;
            if (!GetSingleChar(p[5], out sflag)) {
                return;
            }

            int pgl;
            if (!ParseNonNegativeInteger(p[6], out pgl)) {
                return;
            }

            int pgr;
            if (!ParseNonNegativeInteger(p[7], out pgr)) {
                return;
            }

            char scss;
            if (!GetSingleChar(p[8], out scss)) {
                return;
            }

            string sdesig = p[9];

            Row row = rowVal.AsRow().Clamp(1, Document.TerminalHeight);
            Col col = colVal.AsCol().Clamp(1, Document.TerminalWidth);
            MoveCursorTo(row, col);

            RestoreDECCIRRenditions(srend);

            RestoreDECCIRAttributes(satt);

            RestoreDECCIRFlags(sflag);

            RestoreDECCIRCharacterSetMapping(sdesig, pgl, pgr);
        }

        private void ProcessRestoreDECTABSR(string s) {
            string[] p = s.Split('/');

            List<int> tabStops = new List<int>();
            foreach (string ps in p) {
                int n;
                if (!ParseNonNegativeInteger(ps, out n)) {
                    return;
                }
                tabStops.Add(n);
            }

            _tabStops.Clear();
            foreach (int pos in tabStops) {
                if (pos >= 1) {
                    _tabStops.Set(pos - 1);
                }
            }
        }

        [EscapeSequence(ControlCode.DCS, EscapeSequenceParamType.Numeric, 'q')] // SIXEL
        private void ProcessSixel(NumericParams p) {
            RenderProfile renderProfile = GetRenderProfile();

            // "Sixel Scrolling" is enabled when "Sixel Display Mode" is off
            bool sixelScrolling = !_sixelDisplayMode;

            int lineId;
            int columnIndex;
            if (sixelScrolling) {
                lineId = Document.CurrentLineNumber;
                columnIndex = Document.CaretColumn;
            }
            else {
                lineId = Document.TopLineNumber;
                columnIndex = 0;
            }

            Sixel.SixelDCSProcessor.CompletedCallback onCompleted = (startLineId, startColumnIndex, imageSize, lastSixelDataPosition) => {
                if (sixelScrolling) {
                    float linePitch = renderProfile.Pitch.Height + renderProfile.LineSpacing;
                    int imageBottomLineId = startLineId + (int)Math.Ceiling(lastSixelDataPosition.Y / linePitch) - 1;

                    float columnPitch = renderProfile.Pitch.Width;
                    int cursorColumnIndex;
                    if (_sixelScrollingCursorRight) {
                        cursorColumnIndex = startColumnIndex + (int)Math.Ceiling(imageSize.Width / columnPitch);
                    }
                    else {
                        cursorColumnIndex = startColumnIndex;
                    }

                    int rightEnd = GetCaretColumnRightLimit();
                    bool wrapPending;
                    if (cursorColumnIndex > rightEnd) {
                        cursorColumnIndex = rightEnd;
                        wrapPending = _wrapAroundMode;
                    }
                    else {
                        wrapPending = false;
                    }

                    ViewPort vp = new ViewPort(Document, false); // without origin mode
                    int scrollingBottom = Document.ScrollingBottomLineNumber;
                    if (imageBottomLineId > scrollingBottom) {
                        int scrollLines = imageBottomLineId - scrollingBottom;
                        Row row = vp.FromLineNumber(scrollingBottom);
                        Col col = vp.FromCaretColumn(cursorColumnIndex);
                        MoveCursorTo(row, col);
                        DoScrollUp(scrollLines);
                    }
                    else {
                        MoveCursorTo(vp.FromLineNumber(imageBottomLineId), vp.FromCaretColumn(cursorColumnIndex));
                    }

                    if (wrapPending) {
                        Document.WrapPending = true;
                    }
                }

                Document.UpdateCurrentLine(_manipulator); // flush update spans with the current z-order
                return Document.GLineZOrderManager.Increment();
            };

            _escapeSequenceEngine.StartDCSProcessor(
                new Sixel.SixelDCSProcessor(
                    document: Document,
                    renderProfile: renderProfile,
                    manager: Document.SixelImageManager,
                    lineId: lineId,
                    columnIndex: columnIndex,
                    para: p,
                    usePrivateColorRegisters: _usePrivateColorRegisters,
                    completed: onCompleted
                )
            );
        }

        private bool ParseNonNegativeInteger(string s, out int n) {
            return Int32.TryParse(s, NumberStyles.None, NumberFormatInfo.InvariantInfo, out n) && n >= 0;
        }

        private bool GetSingleChar(string s, out char c) {
            if (s.Length == 1) {
                c = s[0];
                return true;
            }
            c = '\u0000';
            return false;
        }

        private byte[] MakeCSI(string data) {
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);
            if (_transmissionMode.IsEightBit) {
                byte[] buff = new byte[dataBytes.Length + 1];
                buff[0] = (byte)ControlCode.CSI;
                Array.Copy(dataBytes, 0, buff, 1, dataBytes.Length);
                return buff;
            }
            else {
                byte[] buff = new byte[dataBytes.Length + 2];
                buff[0] = (byte)ControlCode.ESC;
                buff[1] = (byte)'[';
                Array.Copy(dataBytes, 0, buff, 2, dataBytes.Length);
                return buff;
            }
        }

        private byte[] MakeCSI(byte b1) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.CSI, b1 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'[', b1 };
        }

        private byte[] MakeCSI(byte b1, byte b2) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.CSI, b1, b2 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'[', b1, b2 };
        }

        private byte[] MakeCSI(byte b1, byte b2, byte b3) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.CSI, b1, b2, b3 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'[', b1, b2, b3 };
        }

        private byte[] MakeCSI(byte b1, byte b2, byte b3, byte b4) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.CSI, b1, b2, b3, b4 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'[', b1, b2, b3, b4 };
        }

        private byte[] MakeCSI(byte b1, byte b2, byte b3, byte b4, byte b5) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.CSI, b1, b2, b3, b4, b5 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'[', b1, b2, b3, b4, b5 };
        }

        private byte[] MakeCSI(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.CSI, b1, b2, b3, b4, b5, b6 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'[', b1, b2, b3, b4, b5, b6 };
        }

        private byte[] MakeSS3(byte b1) {
            return _transmissionMode.IsEightBit
                ? new byte[] { (byte)ControlCode.SS3, b1 }
                : new byte[] { (byte)ControlCode.ESC, (byte)'O', b1 };
        }

        private byte[] MakeDCS_ST(string data) {
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);
            if (_transmissionMode.IsEightBit) {
                byte[] buff = new byte[dataBytes.Length + 2];
                buff[0] = (byte)ControlCode.DCS;
                Array.Copy(dataBytes, 0, buff, 1, dataBytes.Length);
                buff[dataBytes.Length + 1] = (byte)ControlCode.ST;
                return buff;
            }
            else {
                byte[] buff = new byte[dataBytes.Length + 4];
                buff[0] = (byte)ControlCode.ESC;
                buff[1] = (byte)'P';
                Array.Copy(dataBytes, 0, buff, 2, dataBytes.Length);
                buff[dataBytes.Length + 2] = (byte)ControlCode.ESC;
                buff[dataBytes.Length + 3] = (byte)'\\';
                return buff;
            }
        }

        private byte[] MakeOSC_ST(string data) {
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);
            if (_transmissionMode.IsEightBit) {
                byte[] buff = new byte[dataBytes.Length + 2];
                buff[0] = (byte)ControlCode.OSC;
                Array.Copy(dataBytes, 0, buff, 1, dataBytes.Length);
                buff[dataBytes.Length + 1] = (byte)ControlCode.ST;
                return buff;
            }
            else {
                byte[] buff = new byte[dataBytes.Length + 4];
                buff[0] = (byte)ControlCode.ESC;
                buff[1] = (byte)']';
                Array.Copy(dataBytes, 0, buff, 2, dataBytes.Length);
                buff[dataBytes.Length + 2] = (byte)ControlCode.ESC;
                buff[dataBytes.Length + 3] = (byte)'\\';
                return buff;
            }
        }

        private ViewPort GetViewPort() {
            return new ViewPort(Document, _originRelative);
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

    /// <summary>
    /// Default IDynamicCaptionFormatter implementation.
    /// </summary>
    /// <remarks>
    /// This behavior can be overridden using custom plugin.
    /// </remarks>
    internal class DefaultDynamicCaptionFormatter : IDynamicCaptionFormatter {
        public string FormatCaptionUsingWindowTitle(Protocols.ITerminalParameter param, ITerminalSettings settings, string windowTitle) {
            return windowTitle;
        }
    }
}
