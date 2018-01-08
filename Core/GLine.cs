// Copyright 2004-2018 The Poderosa Project.
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
using System.Drawing;
using System.Diagnostics;


#if UNITTEST
using NUnit.Framework;
#endif

using Poderosa.Document.Internal;
using Poderosa.Util.Drawing;
using Poderosa.View;

namespace Poderosa.Document {

    /// <summary>
    /// Represents a single line.
    /// </summary>
    public sealed class GLine {

        // Note:
        //  GCell array may contains GChar.ASCII_NUL.
        //  In rendering or conversion to the text, trailing GChar.ASCII_NULs are ignored, and
        //  other GChar.ASCII_NULs are treated as the GChar.ASCII_SPACE.

        /// <summary>
        /// Delegate for copying characters in GLine.
        /// </summary>
        /// <param name="buff">An array of char which contains characters to copy.</param>
        /// <param name="length">Number of characters to copy from buff.</param>
        public delegate void BufferWriter(char[] buff, int length);

        private readonly CompactGCellArray _cell;
        private int _displayLength;
        private EOLType _eolType;
        private int _id;
        private GLine _nextLine;
        private GLine _prevLine;

        [ThreadStatic]
        private static char[] _copyTempBuff;

        // Returns thread-local temporary buffer for internal use.
        private char[] GetInternalTemporaryCharBuffer(int minLen) {
            char[] buff = _copyTempBuff;
            if (buff == null || buff.Length < minLen) {
                buff = _copyTempBuff = new char[minLen];
            }
            return buff;
        }

        // Returns thread-local temporary buffer
        // for copying characters in _text.
        private char[] GetInternalTemporaryBufferForCopy() {
            return GetInternalTemporaryCharBuffer(_cell.Length * 2);
        }

        /// <summary>
        /// Length of the content in this line.
        /// </summary>
        public int DisplayLength {
            get {
                return _displayLength;
            }
        }

        /// <summary>
        /// Line ID
        /// </summary>
        public int ID {
            get {
                return _id;
            }
            set {
                _id = value;
            }
        }

        /// <summary>
        /// Next node of the doubly linked list.
        /// </summary>
        public GLine NextLine {
            get {
                return _nextLine;
            }
            set {
                _nextLine = value;
            }
        }

        /// <summary>
        /// Previous node of the doubly linked list.
        /// </summary>
        public GLine PrevLine {
            get {
                return _prevLine;
            }
            set {
                _prevLine = value;
            }
        }

        /// <summary>
        /// Type of the line ending.
        /// </summary>
        public EOLType EOLType {
            get {
                return _eolType;
            }
            set {
                _eolType = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="length"></param>
        public GLine(int length) {
            Debug.Assert(length > 0);
            _cell = new CompactGCellArray(length, false);
            _displayLength = 0;
            _id = -1;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cell">cell data</param>
        /// <param name="displayLength">length of the content</param>
        /// <param name="eolType">type of the line ending</param>
        internal GLine(CompactGCellArray cell, int displayLength, EOLType eolType) {
            _cell = cell;
            _displayLength = displayLength;
            _eolType = eolType;
            _id = -1;
        }

        /// <summary>
        /// Constructor (for Clone())
        /// </summary>
        /// <param name="orig"></param>
        private GLine(GLine orig) {
            _cell = orig._cell.Clone();
            _displayLength = orig._displayLength;
            _eolType = orig._eolType;
            _id = orig._id;
        }

        /// <summary>
        /// Updates content in this line.
        /// </summary>
        /// <param name="cells">cell data to be copied</param>
        /// <param name="displayLength">length of the content</param>
        /// <param name="eolType">type of the line ending</param>
        internal void UpdateContent(IGCellArraySource cells, int displayLength, EOLType eolType) {
            lock (this) {
                _cell.Reset(cells);
                _displayLength = displayLength;
                _eolType = eolType;
            }
        }

        /// <summary>
        /// Copys content and ID from the specified instance.
        /// </summary>
        /// <param name="line">another instance</param>
        public void CopyFrom(GLine line) {
            lock (this) {
                this.UpdateContent(line._cell, line._displayLength, line._eolType);
                this._id = line._id;
            }
        }

        /// <summary>
        /// Creates cloned instance.
        /// </summary>
        /// <returns>cloned instance</returns>
        public GLine Clone() {
            lock (this) {
                return new GLine(this);
            }
        }

        /// <summary>
        /// Reset the specified cell array with the content in this instance.
        /// </summary>
        internal void ResetGCellArray(IGCellArray cells) {
            lock (this) {
                cells.Reset(_cell);
            }
        }

        /// <summary>
        /// Clears content with the default attributes.
        /// </summary>
        public void Clear() {
            Clear(null);
        }

        /// <summary>
        /// Clears content with the specified background color.
        /// </summary>
        /// <param name="dec">text decoration for specifying the background color, or null for using default attributes.</param>
        public void Clear(TextDecoration dec) {
            GAttr attr = dec.Attr;
            GColor24 color = dec.Color24;

            lock (this) {
                FillWithNul(0, _cell.Length, attr, color);
                _displayLength = attr.IsDefault ? 0 : _cell.Length;
            }
        }

        /// <summary>
        /// Fill range with ASCII_NUL and the specified attributes.
        /// </summary>
        /// <param name="start">start index of the range (inclusive)</param>
        /// <param name="end">end index of the range (exclusive)</param>
        /// <param name="attr">attributes to fill cells</param>
        /// <param name="color">24 bit colors to fill cells</param>
        private void FillWithNul(int start, int end, GAttr attr, GColor24 color) {
            GAttr fillAttr = attr + GAttrFlags.SameToPrevious;

            if (start < end) {
                _cell.SetNul(start, fillAttr, color);
                UpdateSameToPrevious(start);
            }

            for (int i = start + 1; i < end; i++) {
                _cell.SetNul(i, fillAttr, color);
            }

            UpdateSameToPrevious(end);
        }

        /// <summary>
        /// Updates "SameToPrevious" flag of the cell.
        /// </summary>
        /// <param name="index">cell index</param>
        private void UpdateSameToPrevious(int index) {
            if (index > 0) {    // most common case
                if (index < _cell.Length) {
                    if (_cell.AttrAt(index - 1) == _cell.AttrAt(index) && (_cell.Color24At(index - 1) == _cell.Color24At(index))) {
                        _cell.SetFlags(index, GAttrFlags.SameToPrevious);
                    }
                    else {
                        _cell.ClearFlags(index, GAttrFlags.SameToPrevious);
                    }
                }

                return;
            }

            if (index == 0 && _cell.Length > 0) {
                _cell.ClearFlags(index, GAttrFlags.SameToPrevious);
            }
        }

        /// <summary>
        /// Updates "SameToPrevious" flag of the cells.
        /// </summary>
        /// <param name="start">start index of the cells changed (inclusive)</param>
        /// <param name="end">end index of the cells changed (exclusive)</param>
        private void UpdateSameToPreviousForCellsChanged(int start, int end) {
            // note: the flag of the cell at "end" also needs to be updated.
            for (int i = start; i <= end; i++) {
                UpdateSameToPrevious(i);
            }
        }

        /// <summary>
        /// Finds word boundary.
        /// </summary>
        /// <param name="pos">cell index</param>
        /// <param name="start">start index of the current word (inclusive)</param>
        /// <param name="end">end index of the current word (exclusive)</param>
        public void FindWordBreakPoint(int pos, out int start, out int end) {
            lock (this) {
                if (pos < 0 || pos >= _cell.Length) {
                    start = pos;
                    end = pos + 1;
                    return;
                }

                byte v = ToCharTypeForWordBreak(_cell.CharAt(pos));

                int index = pos - 1;
                while (index >= 0 && ToCharTypeForWordBreak(_cell.CharAt(index)) == v) {
                    index--;
                }
                start = index + 1;

                index = pos + 1;
                while (index < _cell.Length && ToCharTypeForWordBreak(_cell.CharAt(index)) == v) {
                    index++;
                }
                end = index;
            }
        }

        /// <summary>
        /// Determine type of a character.
        /// </summary>
        /// <param name="ch">a character</param>
        /// <returns>type code</returns>
        private static byte ToCharTypeForWordBreak(GChar ch) {
            uint cp = ch.CodePoint;
            if (cp < 0x80u) {
                return ASCIIWordBreakTable.Default.GetAt((char)cp);
            }
            else if (cp == '\u3000') // full-width space
                return ASCIIWordBreakTable.SPACE;
            else
                return ASCIIWordBreakTable.NOT_ASCII;
            // TODO: consider unicode character class
        }

        /// <summary>
        /// Expand internal buffer.
        /// </summary>
        /// <param name="length">minimum length</param>
        public void ExpandBuffer(int length) {
            lock (this) {
                if (length <= _cell.Length) {
                    return;
                }

                int oldLength = _cell.Length;

                _cell.Expand(length, false);

                FillWithNul(oldLength, length, GAttr.Default, new GColor24());
                // Note: _displayLength is not changed.
            }
        }

        /// <summary>
        /// Returns whether this line requires periodic redraw.
        /// </summary>
        public bool IsPeriodicRedrawRequired() {
            lock (this) {
                for (int i = 0; i < _cell.Length; i++) {
                    if (_cell.AttrAt(i).Has(GAttrFlags.Blink)) {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Render this line.
        /// </summary>
        /// <param name="hdc">handle of the device context</param>
        /// <param name="prof">profile settings</param>
        /// <param name="caret">caret settings</param>
        /// <param name="baseBackColor">base background color</param>
        /// <param name="x">left coordinate to paint the line</param>
        /// <param name="y">top coordinate to paint the line</param>
        internal void Render(IntPtr hdc, RenderProfile prof, Caret caret, Color baseBackColor, int x, int y) {
            float fx0 = (float)x;
            float fx1 = fx0;
            int y1 = y;
            int y2 = y1 + (int)prof.Pitch.Height;

            float pitch = prof.Pitch.Width;

            Win32.SetBkMode(hdc, Win32.TRANSPARENT);

            int cellStart = 0;

            lock (this) {
                // Note:
                //  Currently exclusive execution is not required here
                //  because Render() is called on the temporary instances
                //  copied from the GLine list.
                //  Lock is still here for keeping internal design consistency.

                while (cellStart < _displayLength) {
                    int cellEnd = cellStart + 1;
                    while (cellEnd < _displayLength && _cell.AttrAt(cellEnd).Has(GAttrFlags.SameToPrevious)) {
                        cellEnd++;
                    }

                    GAttr attr = _cell.AttrAt(cellStart);
                    GColor24 color24 = _cell.Color24At(cellStart);

                    IntPtr hFont = prof.CalcHFONT_NoUnderline(attr);
                    Win32.SelectObject(hdc, hFont);

                    Color foreColor;
                    Color backColor;
                    prof.DetermineColors(attr, color24, caret, baseBackColor, out backColor, out foreColor);

                    bool isTextOpaque = foreColor.A != 0;
                    uint foreColorRef = DrawUtil.ToCOLORREF(foreColor);
                    if (isTextOpaque) {
                        Win32.SetTextColor(hdc, foreColorRef);
                    }

                    bool isBackgroundOpaque = backColor.A != 0;
                    if (isBackgroundOpaque) {
                        uint bkColorRef = DrawUtil.ToCOLORREF(backColor);
                        Win32.SetBkColor(hdc, bkColorRef);
                    }

                    float fx2 = fx0 + pitch * cellEnd;

                    if (prof.DetermineBold(attr) || attr.Has(GAttrFlags.UseCjkFont)) {
                        // It is not always true that width of a character in the CJK font is twice of a character in the ASCII font.
                        // Characters are drawn one by one to adjust pitch.

                        char[] tmpCharBuf = GetInternalTemporaryCharBuffer(2);

                        int cellIndex = cellStart;
                        float fx = fx1;

                        if (isBackgroundOpaque) {
                            // If background fill is required, we call ExtTextOut() with ETO_OPAQUE to draw the first character.
                            Win32.RECT rect = new Win32.RECT((int)fx1, y1, (int)fx2, y2);
                            if (isTextOpaque) {
                                GChar ch = NulToSpace(_cell.CharAt(cellIndex));
                                int len = ch.WriteTo(tmpCharBuf, 0);
                                unsafe {
                                    fixed (char* p = tmpCharBuf) {
                                        Win32.ExtTextOut(hdc, rect.left, rect.top, Win32.ETO_OPAQUE, &rect, p, len, null);
                                    }
                                }

                                if (ch.IsLeftHalf) {
                                    cellIndex += 2;
                                    fx = fx + pitch + pitch;
                                }
                                else {
                                    cellIndex += 1;
                                    fx = fx + pitch;
                                }
                            }
                            else {
                                unsafe {
                                    fixed (char* p = tmpCharBuf) {
                                        Win32.ExtTextOut(hdc, rect.left, rect.top, Win32.ETO_OPAQUE, &rect, p, 0, null);
                                    }
                                }
                            }
                        }

                        if (isTextOpaque) {
                            while (cellIndex < cellEnd) {
                                GChar ch = NulToSpace(_cell.CharAt(cellIndex));
                                int len = ch.WriteTo(tmpCharBuf, 0);
                                unsafe {
                                    fixed (char* p = tmpCharBuf) {
                                        Win32.ExtTextOut(hdc, (int)fx, y1, 0, null, p, len, null);
                                    }
                                }

                                if (ch.IsLeftHalf) {
                                    cellIndex += 2;
                                    fx = fx + pitch + pitch;
                                }
                                else {
                                    cellIndex += 1;
                                    fx = fx + pitch;
                                }
                            }
                        }
                    }
                    else {
                        char[] tmpCharBuf = GetInternalTemporaryCharBuffer((cellEnd - cellStart) * 2);
                        int bufLen = 0;
                        for (int i = cellStart; i < cellEnd; i++) {
                            bufLen += NulToSpace(_cell.CharAt(i)).WriteTo(tmpCharBuf, bufLen);
                        }

                        if (isBackgroundOpaque) {
                            Win32.RECT rect = new Win32.RECT((int)fx1, y1, (int)fx2, y2);
                            if (isTextOpaque) {
                                unsafe {
                                    fixed (char* p = tmpCharBuf) {
                                        Win32.ExtTextOut(hdc, rect.left, rect.top, Win32.ETO_OPAQUE, &rect, p, bufLen, null);
                                    }
                                }
                            }
                            else {
                                unsafe {
                                    fixed (char* p = tmpCharBuf) {
                                        Win32.ExtTextOut(hdc, rect.left, rect.top, Win32.ETO_OPAQUE, &rect, p, 0, null);
                                    }
                                }
                            }
                        }
                        else {
                            if (isTextOpaque) {
                                unsafe {
                                    fixed (char* p = tmpCharBuf) {
                                        Win32.ExtTextOut(hdc, (int)fx1, y1, 0, null, p, bufLen, null);
                                    }
                                }
                            }
                        }
                    }

                    if (attr.Has(GAttrFlags.Underlined) && isTextOpaque) {
                        DrawUnderline(hdc, foreColorRef, (int)fx1, y2 - 1, (int)fx2 - (int)fx1);
                    }

                    fx1 = fx2;

                    cellStart = cellEnd;
                }
            }
        }

        /// <summary>
        /// Converts a character to SPACE(U+0020) if it was a NUL(U+0000)
        /// </summary>
        /// <param name="ch">a character</param>
        /// <returns>a character</returns>
        private GChar NulToSpace(GChar ch) {
            return (ch.CodePoint == 0) ? GChar.ASCII_SPACE : ch;
        }

        /// <summary>
        /// Draws underline.
        /// </summary>
        /// <param name="hdc">handle of the device context</param>
        /// <param name="col">line color (in COLORREF)</param>
        /// <param name="x">left coordinate to start drawing</param>
        /// <param name="y">top coordinate to start drawing</param>
        /// <param name="length">length of the line</param>
        private void DrawUnderline(IntPtr hdc, uint col, int x, int y, int length) {
            IntPtr pen = Win32.CreatePen(0, 1, col);
            IntPtr prev = Win32.SelectObject(hdc, pen);
            Win32.MoveToEx(hdc, x, y, IntPtr.Zero);
            Win32.LineTo(hdc, x + length, y);
            Win32.SelectObject(hdc, prev);
            Win32.DeleteObject(pen);
        }

        /// <summary>
        /// Set cursor at the specified cell.
        /// </summary>
        /// <param name="index">cell index</param>
        internal void SetCursor(int index) {
            lock (this) {
                if (index >= _displayLength) {
                    int prevLength = _displayLength;
                    ExpandBuffer(index + 1);
                    _displayLength = index + 1;
                }

                _cell.SetFlags(index, GAttrFlags.Cursor);

                if (_cell.CharAt(index).IsRightHalf) {
                    if (index > 0 && _cell.CharAt(index - 1).IsLeftHalf) {
                        _cell.SetFlags(index - 1, GAttrFlags.Cursor);
                        UpdateSameToPreviousForCellsChanged(index - 1, index + 1);
                    }
                    else {
                        UpdateSameToPreviousForCellsChanged(index, index + 1);
                    }
                }
                else if (_cell.CharAt(index).IsLeftHalf) {
                    if (index + 1 < _cell.Length && _cell.CharAt(index + 1).IsRightHalf) {
                        _cell.SetFlags(index + 1, GAttrFlags.Cursor);
                        UpdateSameToPreviousForCellsChanged(index, index + 2);
                    }
                    else {
                        UpdateSameToPreviousForCellsChanged(index, index + 1);
                    }
                }
                else {
                    UpdateSameToPreviousForCellsChanged(index, index + 1);
                }
            }
        }

        /// <summary>
        /// Set selection.
        /// </summary>
        /// <param name="from">start column index of the range. (inclusive)</param>
        /// <param name="to">end column index of the range. (exclusive)</param>
        internal void SetSelection(int from, int to) {
            lock (this) {
                ExpandBuffer(Math.Max(from + 1, to));

                if (from >= 0 && from < _cell.Length && _cell.CharAt(from).IsRightHalf) {
                    from--;
                }

                if (to >= 0 && to < _cell.Length && _cell.CharAt(to).IsRightHalf) {
                    to++;
                }

                if (from < 0) {
                    from = 0;
                }

                if (to > _displayLength) {
                    to = _displayLength;
                }

                for (int i = from; i < to; i++) {
                    _cell.SetFlags(i, GAttrFlags.Selected);
                }

                UpdateSameToPreviousForCellsChanged(from, to);
            }
        }

        /// <summary>
        /// Returns whether the specified cell is a right-half of a wide-width character.
        /// </summary>
        /// <param name="index">cell index</param>
        /// <returns>true if the specified cell is a right-half of a wide-width character.</returns>
        public bool IsRightSideOfZenkaku(int index) {
            lock (this) {
                return (index >= 0 && index < _cell.Length) ? _cell.CharAt(index).IsRightHalf : false;
            }
        }

        /// <summary>
        /// Writes content with the specified writer.
        /// </summary>
        /// <param name="writer">writer</param>
        public void WriteTo(BufferWriter writer) {
            lock (this) {
                WriteToInternal(writer, 0, _cell.Length);
            }
        }

        /// <summary>
        /// Writes content with the specified writer.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="start">start cell index (inclusive)</param>
        /// <param name="end">end cell index (exclusive)</param>
        public void WriteTo(BufferWriter writer, int start, int end) {
            lock (this) {
                WriteToInternal(writer, start, end);
            }
        }

        /// <summary>
        /// Writes content with the specified writer.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="start">start cell index (inclusive)</param>
        public void WriteTo(BufferWriter writer, int start) {
            lock (this) {
                WriteToInternal(writer, start, _cell.Length);
            }
        }

        /// <summary>
        /// Writes content with the specified writer.
        /// </summary>
        /// <param name="writer">writer</param>
        /// <param name="start">start cell index (inclusive)</param>
        /// <param name="end">end cell index (exclusive)</param>
        private void WriteToInternal(BufferWriter writer, int start, int end) {
            if (writer == null) {
                return;
            }

            // determine the length of contens
            int lastNonNulIndex = _displayLength - 1;
            while (lastNonNulIndex >= 0 && _cell.CodePointAt(lastNonNulIndex) == 0) {
                lastNonNulIndex--;
            }

            start = Math.Max(0, start);
            end = Math.Min(Math.Min(_cell.Length, lastNonNulIndex + 1), end);

            char[] temp = GetInternalTemporaryBufferForCopy();
            int tempIndex = 0;
            for (int i = start; i < end; i++) {
                GChar ch = _cell.CharAt(i);
                if (ch.IsRightHalf) {
                    continue;
                }
                ch = NulToSpace(ch);
                tempIndex += ch.WriteTo(temp, tempIndex);
            }

            writer(temp, tempIndex);
        }

        /// <summary>
        /// Convert content to a text string. 
        /// </summary>
        /// <returns>a text string</returns>
        public string ToNormalString() {
            lock (this) {
                string s = null;
                WriteToInternal(
                    (buff, length) => s = new string(buff, 0, length),
                    0, _cell.Length);
                return s;
            }
        }

        /// <summary>
        /// Create an instance from a text string.
        /// </summary>
        /// <param name="text">a text string</param>
        /// <param name="dec"></param>
        /// <returns></returns>
        public static GLine CreateSimpleGLine(string text, TextDecoration dec) {
            int columns = text.Length * 2;
            CompactGCellArray cells = new CompactGCellArray(columns, false);
            GColor24[] colorBuff = null;
            int offset = 0;
            GAttr attr = dec.Attr;
            GColor24 colors = dec.Color24;

            if (attr.Uses24bitColor) {
                colorBuff = new GColor24[columns];
            }

            UnicodeCharConverter conv = new UnicodeCharConverter(true);

            foreach (char originalChar in text) {
                UnicodeChar unicodeChar;
                if (!conv.Feed(originalChar, out unicodeChar)) {
                    continue;
                }

                GChar gchar = new GChar(unicodeChar);
                cells.Set(offset, gchar, attr, colors);

                if (offset == 0) {
                    // next cell has "SameToPrevious" flag
                    attr += GAttrFlags.SameToPrevious;
                }
                offset++;

                if (gchar.IsWideWidth) {
                    cells.Set(offset, gchar + GCharFlags.RightHalf, attr, colors);
                    offset++;
                }
            }

            return new GLine(cells, offset, EOLType.CRLF);
        }
    }

    /// <summary>
    /// Manipulator for editing line buffer copied from <see cref="GLine"/>.
    /// </summary>
    public class GLineManipulator {

        // Note:
        //  GColor24 array is always non-null even if the 24 bit colors are not used, and
        //  its length is always same as the length of GCell array.
        //
        // Note:
        //  GAttrFlags.SameToPrevious of each cell is not updated during the manipulation.
        //  The flag will be updated in Export(). 

        private readonly SimpleGCellArray _cell = new SimpleGCellArray(1);
        private int _caretColumn = 0;
        private EOLType _eolType = EOLType.Continue;

        /// <summary>
        /// Current buffer size.
        /// </summary>
        public int BufferSize {
            get {
                return _cell.Length;
            }
        }

        /// <summary>
        /// Position of the caret.
        /// </summary>
        public int CaretColumn {
            get {
                return _caretColumn;
            }
            set {
                ExpandBuffer(value + 1);
                _caretColumn = value;
            }
        }

        /// <summary>
        /// Type of the line ending.
        /// </summary>
        public EOLType EOLType {
            get {
                return _eolType;
            }
            set {
                _eolType = value;
            }
        }

        /// <summary>
        /// Insert the carriage return.
        /// </summary>
        public void CarriageReturn() {
            this.CaretColumn = 0;
        }

        /// <summary>
        /// Reset line buffer.
        /// </summary>
        /// <param name="length">length of the internal buffer</param>
        public void Reset(int length) {
            _cell.Clear(length);
            _caretColumn = 0;
            _eolType = EOLType.Continue;
        }

        /// <summary>
        /// Load content from a <see cref="GLine"/>.
        /// </summary>
        /// <param name="line">a line object that the content are copied from.</param>
        /// <param name="caretColumn">the caret position</param>
        /// <remarks>
        /// If <paramref name="line"/> was null, internal buffer will be cleared.
        /// </remarks>
        public void Load(GLine line, int caretColumn) {
            if (line == null) {
                Reset(80);
            }
            else {
                line.ResetGCellArray(_cell);
                _eolType = line.EOLType;
            }
            this.CaretColumn = caretColumn;  // buffer may be expanded
        }

        /// <summary>
        /// Expand the buffer.
        /// </summary>
        /// <param name="length">minimum buffer size.</param>
        public void ExpandBuffer(int length) {
            _cell.Expand(length);
        }

        /// <summary>
        /// Write one character to the specified position.
        /// </summary>
        /// <param name="ch">character to write.</param>
        /// <param name="dec">text decoration of the character. (null indicates default setting)</param>
        public void PutChar(UnicodeChar ch, TextDecoration dec) {
            GChar newChar = new GChar(ch);
            GAttr newAttr = dec.Attr;
            GColor24 newColor = dec.Color24;

            if (ch.IsCJK) {
                newAttr += GAttrFlags.UseCjkFont;
            }

            FixLeftHalfOfWideWidthCharacter(_caretColumn - 1);

            if (_caretColumn >= 0 && _caretColumn < _cell.Length) {
                _cell.Set(_caretColumn, newChar, newAttr, newColor);
            }

            _caretColumn++;

            if (newChar.IsWideWidth) {
                if (_caretColumn >= 0 && _caretColumn < _cell.Length) {
                    _cell.Set(_caretColumn, newChar + GCharFlags.RightHalf, newAttr, newColor);
                }
                _caretColumn++;
            }

            FixRightHalfOfWideWidthCharacter(_caretColumn);
        }

        /// <summary>
        /// Fixes orphan left-half of a wide width character.
        /// <para>
        /// If the character at the specified position was a left-half of a wide width character,
        /// this method puts SPACE there.
        /// </para>
        /// </summary>
        /// <param name="index"></param>
        private void FixLeftHalfOfWideWidthCharacter(int index) {
            if (index >= 0 && index < _cell.Length) {
                if (_cell.CharAt(index).IsLeftHalf) {
                    _cell.SetNul(index);
                }
            }
        }

        /// <summary>
        /// Fixes orphan right-half of a wide width character.
        /// <para>
        /// If the character at the specified position was a right-half of a wide width character,
        /// this method puts SPACE there.
        /// </para>
        /// </summary>
        /// <param name="index"></param>
        private void FixRightHalfOfWideWidthCharacter(int index) {
            if (index >= 0 && index < _cell.Length) {
                if (_cell.CharAt(index).IsRightHalf) {
                    _cell.SetNul(index);
                }
            }
        }

        /// <summary>
        /// Moves the caret position to the left. 
        /// </summary>
        /// <remarks>
        /// If current caret position was the top of the line, caret position will not be changed.
        /// </remarks>
        public void BackCaret() {
            if (_caretColumn > 0) {
                _caretColumn--;
            }
        }

        /// <summary>
        /// Fills specified range with spaces. 
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        /// <param name="dec">text decoration of the blanks (null indicates default setting)</param>
        public void FillSpace(int from, int to, TextDecoration dec) {
            from = Math.Max(0, from);
            to = Math.Min(_cell.Length, to);

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            FixLeftHalfOfWideWidthCharacter(from - 1);

            for (int i = from; i < to; i++) {
                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell.Set(i, GChar.ASCII_NUL, fillAttr, fillColor);
            }

            FixRightHalfOfWideWidthCharacter(to);
        }

        /// <summary>
        /// Deletes characters in the specified range and moves trailing characters to the left.
        /// </summary>
        /// <param name="start">start columns index</param>
        /// <param name="count">count of columns to delete</param>
        /// <param name="dec">text decoration of the blanks (null indicates default setting)</param>
        public void DeleteChars(int start, int count, TextDecoration dec) {
            if (count <= 0) {
                return;
            }

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            int dstIndex = (start >= 0) ? start : 0;
            int srcIndex = dstIndex + count;
            while (dstIndex < _cell.Length && srcIndex < _cell.Length) {
                _cell.Copy(srcIndex, dstIndex);
                dstIndex++;
                srcIndex++;
            }

            while (dstIndex < _cell.Length) {
                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell.Set(dstIndex, GChar.ASCII_NUL, fillAttr, fillColor);
                dstIndex++;
            }

            FixLeftHalfOfWideWidthCharacter(start - 1);
            FixRightHalfOfWideWidthCharacter(start);
        }

        /// <summary>
        /// Insert blank characters in the specified range and moves trailing characters to the right.
        /// </summary>
        /// <param name="start">start columns index</param>
        /// <param name="count">count of columns to insert blanks</param>
        /// <param name="dec">text decoration of the blanks (null indicates default setting)</param>
        public void InsertBlanks(int start, int count, TextDecoration dec) {
            if (count <= 0) {
                return;
            }

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            int limit = Math.Max(0, start);
            int dstIndex = _cell.Length - 1;
            int srcIndex = dstIndex - count;
            while (srcIndex >= limit) {
                _cell.Copy(srcIndex, dstIndex);
                dstIndex--;
                srcIndex--;
            }

            while (dstIndex >= limit) {
                _cell.Set(dstIndex, GChar.ASCII_NUL, fillAttr, fillColor);
                dstIndex--;
            }

            FixLeftHalfOfWideWidthCharacter(start - 1);
            FixRightHalfOfWideWidthCharacter(start + count);
            FixLeftHalfOfWideWidthCharacter(_cell.Length - 1);
        }

        /// <summary>
        /// Export as the new <see cref="GLine"/>.
        /// </summary>
        /// <returns>new <see cref="GLine"/> instance</returns>
        public GLine Export() {
            int displayLength;
            PrepareExport(out displayLength);

            GLine line = new GLine(new CompactGCellArray(_cell), displayLength, _eolType);
            return line;
        }

        /// <summary>
        /// Export to an existing <see cref="GLine"/>.
        /// </summary>
        /// <param name="line"><see cref="GLine"/> to export to</param>
        public void ExportTo(GLine line) {
            int displayLength;
            PrepareExport(out displayLength);

            line.UpdateContent(_cell, displayLength, _eolType);
        }

        /// <summary>
        /// Prepare export.
        /// </summary>
        /// <param name="displayLength">displayLength of the <see cref="GLine"/></param>
        private void PrepareExport(out int displayLength) {
            bool tempUses24bitColor = false;
            int lastCharIndex = -1;
            for (int i = 0; i < _cell.Length; i++) {
                var curAttr = _cell.AttrAt(i);
                var curChar = _cell.CharAt(i);
                tempUses24bitColor |= curAttr.Uses24bitColor;
                if (!curAttr.IsDefault || curChar.CodePoint != 0u) {
                    lastCharIndex = i;
                }
            }

            // update "IsColor24Used"
            _cell.IsColor24Used = tempUses24bitColor;

            // update "SameToPrevious" flags

            _cell.ClearFlags(0, GAttrFlags.SameToPrevious);

            var prevAttr = _cell.AttrAt(0);
            var prevColor24 = _cell.Color24At(0);
            for (int i = 1; i < _cell.Length; i++) {
                var curAttr = _cell.AttrAt(i);
                var curColor24 = _cell.Color24At(i);
                if (prevAttr == curAttr && prevColor24 == curColor24) {
                    _cell.SetFlags(i, GAttrFlags.SameToPrevious);
                }
                else {
                    _cell.ClearFlags(i, GAttrFlags.SameToPrevious);
                }
                prevAttr = curAttr;
                prevColor24 = curColor24;
            }

            displayLength = lastCharIndex + 1;
        }
    }

    /// <summary>
    /// <ja>
    /// 改行コードの種類を示します。
    /// </ja>
    /// <en>
    /// Kind of Line feed code
    /// </en>
    /// </summary>
    public enum EOLType {
        /// <summary>
        /// <ja>改行せずに継続します。</ja><en>It continues without changing line.</en>
        /// </summary>
        Continue,
        /// <summary>
        /// <ja>CRLFで改行します。</ja><en>It changes line with CRLF. </en>
        /// </summary>
        CRLF,
        /// <summary>
        /// <ja>CRで改行します。</ja><en>It changes line with CR. </en>
        /// </summary>
        CR,
        /// <summary>
        /// <ja>LFで改行します。</ja><en>It changes line with LF. </en>
        /// </summary>
        LF
    }

    /// <summary>
    /// Word-break table for ASCII characters.
    /// </summary>
    public class ASCIIWordBreakTable {

        public const byte LETTER = 0;
        public const byte SYMBOL = 1;
        public const byte SPACE = 2;
        public const byte NOT_ASCII = 3;

        private byte[] _data;

        public ASCIIWordBreakTable() {
            _data = new byte[0x80];
            Reset();
        }

        public void Reset() {
            // control characters and spaces
            for (int i = 0; i <= 0x20; i++) {
                _data[i] = SPACE;
            }
            _data[0x7F] = SPACE; //DEL

            // other
            for (int i = 0x21; i <= 0x7E; i++) {
                char c = (char)i;
                if (('0' <= c && c <= '9') || ('a' <= c && c <= 'z') || ('A' <= c && c <= 'Z') || c == '_') {
                    _data[i] = LETTER;
                }
                else {
                    _data[i] = SYMBOL;
                }
            }
        }

        public byte GetAt(char ch) {
            if ((uint)ch < (uint)_data.Length) {
                return _data[(uint)ch];
            }
            return LETTER;
        }

        public void Set(char ch, byte type) {
            if ((uint)ch < (uint)_data.Length) {
                _data[(uint)ch] = type;
            }
        }

        private static ASCIIWordBreakTable _instance = new ASCIIWordBreakTable();

        public static ASCIIWordBreakTable Default {
            get {
                return _instance;
            }
        }
    }

}
