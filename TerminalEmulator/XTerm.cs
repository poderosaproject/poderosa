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

namespace Poderosa.Terminal {
    internal class XTerm : EscapeSequenceTerminal {

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

        private bool _gotEscape;

        private bool _wrapAroundMode;
        private bool _reverseVideo;
        private bool[] _tabStops;
        private readonly List<GLine>[] _savedScreen = new List<GLine>[2];	// { main, alternate } 別のバッファに移行したときにGLineを退避しておく
        private bool _isAlternateBuffer;
        private bool _savedMode_isAlternateBuffer;
        private readonly int[] _xtermSavedRow = new int[2];	// { main, alternate }
        private readonly int[] _xtermSavedCol = new int[2];	// { main, alternate }

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

        private bool _insertMode;
        private bool _scrollRegionRelative;

        private const int MOUSE_POS_LIMIT = 255 - 32;       // mouse position limit
        private const int MOUSE_POS_EXT_LIMIT = 2047 - 32;  // mouse position limit in extended mode
        private const int MOUSE_POS_EXT_START = 127 - 32;   // mouse position to start using extended format

        public XTerm(TerminalInitializeInfo info)
            : base(info) {
            _insertMode = false;
            _scrollRegionRelative = false;
            _wrapAroundMode = true;
            _tabStops = new bool[GetDocument().TerminalWidth];
            _isAlternateBuffer = false;
            _savedMode_isAlternateBuffer = false;
            InitTabStops();
        }

        protected override void ResetInternal() {
            base.ResetInternal();
            _insertMode = false;
            _scrollRegionRelative = false;
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
            if (_gotEscape) {
                _gotEscape = false;
                if (ch == '\\') {
                    // ESC \ --> ST (9C)
                    // Note:
                    //  The conversion of "ESC ch" pair is applied
                    //  only for the "ESC \" case because it may be used
                    //  for terminating the escape sequence.
                    //  After this conversion, we can consider ESC as the start
                    //  of the new escape sequence.
                    base.ProcessChar(ControlCode.ST);
                    return;
                }
                base.ProcessChar(ControlCode.ESC);
            }

            if (ch == ControlCode.ESC) {
                _gotEscape = true;
            }
            else {
                base.ProcessChar(ch);
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
                        new StringBuilder()
                            .Append("\x1b[")
                            .Append(statBits.ToString(NumberFormatInfo.InvariantInfo))
                            .Append(';')
                            .Append((col + 1).ToString(NumberFormatInfo.InvariantInfo))
                            .Append(';')
                            .Append((row + 1).ToString(NumberFormatInfo.InvariantInfo))
                            .Append("M")
                            .ToString());
                    dataLen = data.Length;
                    break;

                case MouseTrackingProtocol.Sgr:
                    data = Encoding.ASCII.GetBytes(
                        new StringBuilder()
                            .Append("\x1b[<")
                            .Append(statBits.ToString(NumberFormatInfo.InvariantInfo))
                            .Append(';')
                            .Append((col + 1).ToString(NumberFormatInfo.InvariantInfo))
                            .Append(';')
                            .Append((row + 1).ToString(NumberFormatInfo.InvariantInfo))
                            .Append(action == TerminalMouseAction.ButtonUp ? 'm' : 'M')
                            .ToString());
                    dataLen = data.Length;
                    break;

                default:
                    return true;    // unknown protocol
            }

            TransmitDirect(data, 0, dataLen);

            return true;
        }

        protected override ProcessCharResult ProcessNormalUnicodeChar(UnicodeChar ch) {
            //WrapAroundがfalseで、キャレットが右端のときは何もしない
            if (!_wrapAroundMode && _manipulator.CaretColumn >= GetDocument().TerminalWidth - 1)
                return ProcessCharResult.Processed;

            if (_insertMode)
                _manipulator.InsertBlanks(_manipulator.CaretColumn, ch.IsWideWidth ? 2 : 1, _currentdecoration);
            return base.ProcessNormalUnicodeChar(ch);
        }
        protected override ProcessCharResult ProcessControlChar(char ch) {
            return base.ProcessControlChar(ch);
            /* 文字コードが誤っているとこのあたりを不意に実行してしまうことがあり、よろしくない。
            switch(ch) {
                //単純な変換なら他にもできるが、サポートしているのはいまのところこれしかない
                case (char)0x8D:
                    base.ProcessChar((char)0x1B);
                    base.ProcessChar('M');
                    return ProcessCharResult.Processed;
                case (char)0x9B:
                    base.ProcessChar((char)0x1B);
                    base.ProcessChar('[');
                    return ProcessCharResult.Processed;
                case (char)0x9D:
                    base.ProcessChar((char)0x1B);
                    base.ProcessChar(']');
                    return ProcessCharResult.Processed;
                default:
                    return base.ProcessControlChar(ch);
            }
            */
        }

        protected override ProcessCharResult ProcessEscapeSequence(char code, char[] seq, int offset) {
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

        private void ProcessDeviceAttributes(string param) {
            if (param.StartsWith(">")) {
                byte[] data = Encoding.ASCII.GetBytes(" [>82;1;0c");
                data[0] = 0x1B; //ESC
                TransmitDirect(data);
            }
            else {
                byte[] data = Encoding.ASCII.GetBytes(" [?1;2c"); //なんかよくわからないがMindTerm等をみるとこれでいいらしい
                data[0] = 0x1B; //ESC
                TransmitDirect(data);
            }
        }

        private void ProcessDeviceStatusReport(string param) {
            string response;
            if (param == "5")
                response = " [0n"; //これでOKの意味らしい
            else if (param == "6")
                response = String.Format(" [{0};{1}R", GetDocument().CurrentLineNumber - GetDocument().TopLineNumber + 1, _manipulator.CaretColumn + 1);
            else
                throw new UnknownEscapeSequenceException("DSR " + param);

            byte[] data = Encoding.ASCII.GetBytes(response);
            data[0] = 0x1B; //ESC
            TransmitDirect(data);
        }

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

        //CSI H
        private void ProcessCursorPosition(string param) {
            IntPair t = ParseIntPair(param, 1, 1);
            int row = t.first, col = t.second;
            if (_scrollRegionRelative && GetDocument().ScrollingTop != -1) {
                row += GetDocument().ScrollingTop;
            }

            if (row < 1)
                row = 1;
            else if (row > GetDocument().TerminalHeight)
                row = GetDocument().TerminalHeight;
            if (col < 1)
                col = 1;
            else if (col > GetDocument().TerminalWidth)
                col = GetDocument().TerminalWidth;
            ProcessCursorPosition(row, col);
        }

        private void ProcessCursorPosition(int row, int col) {
            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = (GetDocument().TopLineNumber + row - 1);
            //int cc = GetDocument().CurrentLine.DisplayPosToCharPos(col-1);
            //Debug.Assert(cc>=0);
            _manipulator.Load(GetDocument().CurrentLine, col - 1);
        }

        //CSI J
        private void ProcessEraseInDisplay(string param) {
            int d = ParseInt(param, 0);

            TerminalDocument doc = GetDocument();
            int cur = doc.CurrentLineNumber;
            int top = doc.TopLineNumber;
            int bottom = top + doc.TerminalHeight;
            int col = _manipulator.CaretColumn;
            switch (d) {
                case 0: //erase below
                    {
                        if (col == 0 && cur == top)
                            goto ERASE_ALL;

                        EraseRight();
                        doc.UpdateCurrentLine(_manipulator);
                        doc.EnsureLine(bottom - 1);
                        doc.RemoveAfter(bottom);
                        doc.ClearRange(cur + 1, bottom, _currentdecoration);
                        _manipulator.Load(doc.CurrentLine, col);
                    }
                    break;
                case 1: //erase above
                    {
                        if (col == doc.TerminalWidth - 1 && cur == bottom - 1)
                            goto ERASE_ALL;

                        EraseLeft();
                        doc.UpdateCurrentLine(_manipulator);
                        doc.ClearRange(top, cur, _currentdecoration);
                        _manipulator.Load(doc.CurrentLine, col);
                    }
                    break;
                case 2: //erase all
                ERASE_ALL: {
                        GetDocument().ApplicationModeBackColor =
                            (_currentdecoration != null) ? _currentdecoration.GetBackColorSpec() : ColorSpec.Default;

                        doc.UpdateCurrentLine(_manipulator);
                        //if(_homePositionOnCSIJ2) { //SFUではこうなる
                        //	ProcessCursorPosition(1,1); 
                        //	col = 0;
                        //}
                        doc.EnsureLine(bottom - 1);
                        doc.RemoveAfter(bottom);
                        doc.ClearRange(top, bottom, _currentdecoration);
                        _manipulator.Load(doc.CurrentLine, col);
                    }
                    break;
                default:
                    throw new UnknownEscapeSequenceException(String.Format("unknown ED option {0}", param));
            }

        }

        //CSI K
        private void ProcessEraseInLine(string param) {
            int d = ParseInt(param, 0);

            switch (d) {
                case 0: //erase right
                    EraseRight();
                    break;
                case 1: //erase left
                    EraseLeft();
                    break;
                case 2: //erase all
                    EraseLine();
                    break;
                default:
                    throw new UnknownEscapeSequenceException(String.Format("unknown EL option {0}", param));
            }
        }

        private void EraseRight() {
            _manipulator.FillSpace(_manipulator.CaretColumn, _manipulator.BufferSize, _currentdecoration);
        }

        private void EraseLeft() {
            _manipulator.FillSpace(0, _manipulator.CaretColumn + 1, _currentdecoration);
        }

        private void EraseLine() {
            _manipulator.FillSpace(0, _manipulator.BufferSize, _currentdecoration);
        }

        private void Index() {
            GetDocument().UpdateCurrentLine(_manipulator);
            int current = GetDocument().CurrentLineNumber;
            if (current == GetDocument().TopLineNumber + GetDocument().TerminalHeight - 1 || current == GetDocument().ScrollingBottom)
                GetDocument().ScrollDown();
            else
                GetDocument().CurrentLineNumber = current + 1;
            _manipulator.Load(GetDocument().CurrentLine, _manipulator.CaretColumn);
        }

        private void ReverseIndex() {
            GetDocument().UpdateCurrentLine(_manipulator);
            int current = GetDocument().CurrentLineNumber;
            if (current == GetDocument().TopLineNumber || current == GetDocument().ScrollingTop)
                GetDocument().ScrollUp();
            else
                GetDocument().CurrentLineNumber = current - 1;
            _manipulator.Load(GetDocument().CurrentLine, _manipulator.CaretColumn);
        }

        private void ProcessSetScrollingRegion(string param) {
            int height = GetDocument().TerminalHeight;
            IntPair v = ParseIntPair(param, 1, height);

            if (v.first < 1)
                v.first = 1;
            else if (v.first > height)
                v.first = height;
            if (v.second < 1)
                v.second = 1;
            else if (v.second > height)
                v.second = height;
            if (v.first > v.second) { //問答無用でエラーが良いようにも思うが
                int t = v.first;
                v.first = v.second;
                v.second = t;
            }

            //指定は1-originだが処理は0-origin
            GetDocument().SetScrollingRegion(v.first - 1, v.second - 1);
        }

        private void ProcessNextLine() {
            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = (GetDocument().CurrentLineNumber + 1);
            _manipulator.Load(GetDocument().CurrentLine, 0);
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

        private ProcessCharResult ProcessDECSETMulti(string param, char code) {
            if (param.Length == 0)
                return ProcessCharResult.Processed;
            bool question = param[0] == '?';
            string[] ps = question ? param.Substring(1).Split(';') : param.Split(';');
            bool unsupported = false;
            foreach (string p in ps) {
                ProcessCharResult r = question ? ProcessDECSET(p, code) : ProcessSetMode(p, code);
                if (r == ProcessCharResult.Unsupported)
                    unsupported = true;
            }
            return unsupported ? ProcessCharResult.Unsupported : ProcessCharResult.Processed;
        }

        private ProcessCharResult ProcessSetMode(string param, char code) {
            bool set = code == 'h';
            switch (param) {
                case "4":
                    _insertMode = set; //hで始まってlで終わる
                    return ProcessCharResult.Processed;
                case "12": {	//local echo
                        ITerminalSettings settings = GetTerminalSettings();
                        bool value = !set;
                        _afterExitLockActions.Add(() => {
                            settings.BeginUpdate();
                            settings.LocalEcho = value;
                            settings.EndUpdate();
                        });
                        return ProcessCharResult.Processed;
                    }
                case "20":
                    return ProcessCharResult.Processed; //!!WinXPのTelnetで確認した
                case "25":
                    return ProcessCharResult.Processed;
                case "34":	//MakeCursorBig, puttyにはある
                    //!setでカーソルを強制的に箱型にし、setで通常に戻すというのが正しい動作だが実害はないので無視
                    return ProcessCharResult.Processed;
                default:
                    return ProcessCharResult.Unsupported;
            }
        }

        //これを送ってくるアプリケーションは viで上方スクロール
        private void ProcessInsertLines(string param) {
            int d = ParseInt(param, 1);

            TerminalDocument doc = GetDocument();
            int caret_pos = _manipulator.CaretColumn;
            int offset = doc.CurrentLineNumber - doc.TopLineNumber;
            doc.UpdateCurrentLine(_manipulator);
            if (doc.ScrollingBottom == -1)
                doc.SetScrollingRegion(0, GetDocument().TerminalHeight - 1);

            for (int i = 0; i < d; i++) {
                doc.ScrollUp(doc.CurrentLineNumber, doc.ScrollingBottom);
                doc.CurrentLineNumber = doc.TopLineNumber + offset;
            }
            _manipulator.Load(doc.CurrentLine, caret_pos);
        }

        //これを送ってくるアプリケーションは viで下方スクロール
        private void ProcessDeleteLines(string param) {
            int d = ParseInt(param, 1);

            /*
            TerminalDocument doc = GetDocument();
            _manipulator.Clear(GetConnection().TerminalWidth);
            GLine target = doc.CurrentLine;
            for(int i=0; i<d; i++) {
                target.Clear();
                target = target.NextLine;
            }
            */

            TerminalDocument doc = GetDocument();
            int caret_col = _manipulator.CaretColumn;
            int offset = doc.CurrentLineNumber - doc.TopLineNumber;
            doc.UpdateCurrentLine(_manipulator);
            if (doc.ScrollingBottom == -1)
                doc.SetScrollingRegion(0, doc.TerminalHeight - 1);

            for (int i = 0; i < d; i++) {
                doc.ScrollDown(doc.CurrentLineNumber, doc.ScrollingBottom);
                doc.CurrentLineNumber = doc.TopLineNumber + offset;
            }
            _manipulator.Load(doc.CurrentLine, caret_col);
        }

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
                //Quick Test
                //_afterExitLockActions.Add(new AfterExitLockDelegate(new CaptionChanger(GetTerminalSettings(), pt).Do));

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

        private void ProcessSGR(string param) {
            int state = 0, target = 0, r = 0, g = 0, b = 0;
            string[] ps = param.Split(';');
            TextDecoration dec = _currentdecoration;
            foreach (string cmd in ps) {
                int code = ParseSGRCode(cmd);
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
            _currentdecoration = dec;
        }

        private int ParseSGRCode(string param) {
            if (param.Length == 0)
                return 0;
            else if (param.Length == 1)
                return param[0] - '0';
            else if (param.Length == 2)
                return (param[0] - '0') * 10 + (param[1] - '0');
            else if (param.Length == 3)
                return (param[0] - '0') * 100 + (param[1] - '0') * 10 + (param[2] - '0');
            else
                throw new UnknownEscapeSequenceException(String.Format("unknown SGR parameter {0}", param));
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

        //CSI ? Pm h, CSI ? Pm l
        private ProcessCharResult ProcessDECSET(string param, char code) {
            //Debug.WriteLine(String.Format("DECSET {0} {1}", param, code));
            bool set = code == 'h';
            switch (param) {
                case "25":
                    return ProcessCharResult.Processed; //!!Show/Hide Cursorだがとりあえず無視
                case "1":
                    ChangeCursorKeyMode(code == 'h' ? TerminalMode.Application : TerminalMode.Normal);
                    return ProcessCharResult.Processed;
                case "1047":	//Alternate Buffer
                    if (set) {
                        SwitchBuffer(true);
                        // XTerm doesn't clear screen.
                    }
                    else {
                        ClearScreen();
                        SwitchBuffer(false);
                    }
                    return ProcessCharResult.Processed;
                case "1048":	//Save/Restore Cursor
                    if (set)
                        SaveCursor();
                    else
                        RestoreCursor();
                    return ProcessCharResult.Processed;
                case "1049":	//Save/Restore Cursor and Alternate Buffer
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
                    return ProcessCharResult.Processed;
                case "1000": // DEC VT200 compatible: Send button press and release event with mouse position.
                    ResetMouseTracking((set) ? MouseTrackingState.Normal : MouseTrackingState.Off);
                    return ProcessCharResult.Processed;
                case "1001": // DEC VT200 highlight tracking
                    // Not supported
                    ResetMouseTracking(MouseTrackingState.Off);
                    return ProcessCharResult.Processed;
                case "1002": // Button-event tracking: Send button press, release, and drag event.
                    ResetMouseTracking((set) ? MouseTrackingState.Drag : MouseTrackingState.Off);
                    return ProcessCharResult.Processed;
                case "1003": // Any-event tracking: Send button press, release, and motion.
                    ResetMouseTracking((set) ? MouseTrackingState.Any : MouseTrackingState.Off);
                    return ProcessCharResult.Processed;
                case "1004": // Send FocusIn/FocusOut events
                    _focusReportingMode = set;
                    return ProcessCharResult.Processed;
                case "1005": // Enable UTF8 Mouse Mode
                    if (set) {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Utf8;
                    }
                    else {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
                    }
                    return ProcessCharResult.Processed;
                case "1006": // Enable SGR Extended Mouse Mode
                    if (set) {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Sgr;
                    }
                    else {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
                    }
                    return ProcessCharResult.Processed;
                case "1015": // Enable UTF8 Extended Mouse Mode
                    if (set) {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Urxvt;
                    }
                    else {
                        _mouseTrackingProtocol = MouseTrackingProtocol.Normal;
                    }
                    return ProcessCharResult.Processed;
                case "1034":	// Input 8 bits
                    return ProcessCharResult.Processed;
                case "2004":    // Set/Reset bracketed paste mode
                    _bracketedPasteMode = set;
                    return ProcessCharResult.Processed;
                case "3":	//132 Column Mode
                    return ProcessCharResult.Processed;
                case "4":	//Smooth Scroll なんのことやら
                    return ProcessCharResult.Processed;
                case "5":
                    SetReverseVideo(set);
                    return ProcessCharResult.Processed;
                case "6":	//Origin Mode
                    _scrollRegionRelative = set;
                    return ProcessCharResult.Processed;
                case "7":
                    _wrapAroundMode = set;
                    return ProcessCharResult.Processed;
                case "12":
                    //一応報告あったので。SETMODEの12ならローカルエコーなんだがな
                    return ProcessCharResult.Processed;
                case "47":
                    if (set)
                        SwitchBuffer(true);
                    else
                        SwitchBuffer(false);
                    return ProcessCharResult.Processed;
                default:
                    return ProcessCharResult.Unsupported;
            }
        }

        private ProcessCharResult ProcessSaveDECSET(string param, char code) {
            switch (param) {
                case "1047":
                case "47":
                    _savedMode_isAlternateBuffer = _isAlternateBuffer;
                    break;
            }
            return ProcessCharResult.Processed;
        }

        private ProcessCharResult ProcessRestoreDECSET(string param, char code) {
            switch (param) {
                case "1047":
                case "47":
                    SwitchBuffer(_savedMode_isAlternateBuffer);
                    break;
            }
            return ProcessCharResult.Processed;
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

        private void ProcessLinePositionAbsolute(string param) {
            foreach (string p in param.Split(';')) {
                int row = ParseInt(p, 1);
                if (row < 1)
                    row = 1;
                if (row > GetDocument().TerminalHeight)
                    row = GetDocument().TerminalHeight;

                int col = _manipulator.CaretColumn;

                //以下はCSI Hとほぼ同じ
                GetDocument().UpdateCurrentLine(_manipulator);
                GetDocument().CurrentLineNumber = (GetDocument().TopLineNumber + row - 1);
                _manipulator.Load(GetDocument().CurrentLine, col);
            }
        }
        private void ProcessLineColumnAbsolute(string param) {
            foreach (string p in param.Split(';')) {
                int n = ParseInt(p, 1);
                if (n < 1)
                    n = 1;
                if (n > GetDocument().TerminalWidth)
                    n = GetDocument().TerminalWidth;
                _manipulator.CaretColumn = n - 1;
            }
        }
        private void ProcessEraseChars(string param) {
            int n = ParseInt(param, 1);
            int s = _manipulator.CaretColumn;
            for (int i = 0; i < n; i++) {
                _manipulator.PutChar(UnicodeChar.ASCII_SPACE, _currentdecoration);
                if (_manipulator.CaretColumn >= _manipulator.BufferSize)
                    break;
            }
            _manipulator.CaretColumn = s;
        }
        private void ProcessScrollUp(string param) {
            int d = ParseInt(param, 1);

            TerminalDocument doc = GetDocument();
            int caret_col = _manipulator.CaretColumn;
            int offset = doc.CurrentLineNumber - doc.TopLineNumber;
            doc.UpdateCurrentLine(_manipulator);
            if (doc.ScrollingBottom == -1)
                doc.SetScrollingRegion(0, GetDocument().TerminalHeight - 1);
            for (int i = 0; i < d; i++) {
                doc.ScrollDown(doc.ScrollingTop, doc.ScrollingBottom); // TerminalDocument's "Scroll-Down" means XTerm's "Scroll-Up"
                doc.CurrentLineNumber = doc.TopLineNumber + offset; // find correct GLine
            }
            _manipulator.Load(doc.CurrentLine, caret_col);
        }
        private void ProcessScrollDown(string param) {
            int d = ParseInt(param, 1);

            TerminalDocument doc = GetDocument();
            int caret_col = _manipulator.CaretColumn;
            int offset = doc.CurrentLineNumber - doc.TopLineNumber;
            doc.UpdateCurrentLine(_manipulator);
            if (doc.ScrollingBottom == -1)
                doc.SetScrollingRegion(0, GetDocument().TerminalHeight - 1);
            for (int i = 0; i < d; i++) {
                doc.ScrollUp(doc.ScrollingTop, doc.ScrollingBottom); // TerminalDocument's "Scroll-Up" means XTerm's "Scroll-Down"
                doc.CurrentLineNumber = doc.TopLineNumber + offset; // find correct GLine
            }
            _manipulator.Load(doc.CurrentLine, caret_col);
        }
        private void ProcessForwardTab(string param) {
            int n = ParseInt(param, 1);

            int t = _manipulator.CaretColumn;
            for (int i = 0; i < n; i++)
                t = GetNextTabStop(t);
            if (t >= GetDocument().TerminalWidth)
                t = GetDocument().TerminalWidth - 1;
            _manipulator.CaretColumn = t;
        }
        private void ProcessBackwardTab(string param) {
            int n = ParseInt(param, 1);

            int t = _manipulator.CaretColumn;
            for (int i = 0; i < n; i++)
                t = GetPrevTabStop(t);
            if (t < 0)
                t = 0;
            _manipulator.CaretColumn = t;
        }
        private void ProcessTabClear(string param) {
            if (param == "0")
                SetTabStop(_manipulator.CaretColumn, false);
            else if (param == "3")
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
        private void SetTabStop(int index, bool value) {
            EnsureTabStops(index + 1);
            _tabStops[index] = value;
        }
        private void ClearAllTabStop() {
            for (int i = 0; i < _tabStops.Length; i++) {
                _tabStops[i] = false;
            }
        }
        protected override int GetNextTabStop(int start) {
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
            ProcessEraseInDisplay("2");
        }

        private void SaveCursor() {
            int sw = _isAlternateBuffer ? 1 : 0;
            _xtermSavedRow[sw] = GetDocument().CurrentLineNumber - GetDocument().TopLineNumber;
            _xtermSavedCol[sw] = _manipulator.CaretColumn;
        }

        private void RestoreCursor() {
            int sw = _isAlternateBuffer ? 1 : 0;
            GetDocument().UpdateCurrentLine(_manipulator);
            GetDocument().CurrentLineNumber = GetDocument().TopLineNumber + _xtermSavedRow[sw];
            _manipulator.Load(GetDocument().CurrentLine, _xtermSavedCol[sw]);
        }

        //画面の反転
        private void SetReverseVideo(bool reverse) {
            if (reverse == _reverseVideo)
                return;

            _reverseVideo = reverse;
            GetDocument().InvalidatedRegion.InvalidatedAll = true; //全体再描画を促す
        }

        private ProcessCharResult SoftTerminalReset(string param) {
            if (param == "!") {
                FullReset();
                return ProcessCharResult.Processed;
            }
            else
                return ProcessCharResult.Unsupported;
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

        public override void FullReset() {
            InitTabStops();
            base.FullReset();
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
