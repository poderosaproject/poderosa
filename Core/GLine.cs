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
using System.Drawing;
using System.Diagnostics;
using System.Text;

#if UNITTEST
using NUnit.Framework;
#endif

using Poderosa.Util.Drawing;
using Poderosa.Forms;
using Poderosa.View;

namespace Poderosa.Document {

    /// <summary>
    /// Flag bits for <see cref="GChar"/>.
    /// </summary>
    [Flags]
    internal enum GCharFlags : uint {
        None = 0u,
        RightHalf = 1u << 30,
        WideWidth = UnicodeCharFlags.WideWidth,
        Mask = RightHalf | WideWidth,
    }

    /// <summary>
    /// Character information in <see cref="GLine"/>.
    /// </summary>
    internal struct GChar {
        // bit 0..20 : Unicode Code Point (copied from UnicodeChar)
        //
        // bit 30 : Right half of a wide-width character
        // bit 31 : wide width (copied from UnicodeChar)

        private readonly uint _bits;

        private const uint CodePointMask = 0x1fffffu;
        private const uint UnicodeCharMask = CodePointMask | (uint)UnicodeCharFlags.WideWidth;

        /// <summary>
        /// An instance of SPACE (U+0020)
        /// </summary>
        public static GChar ASCII_SPACE {
            get {
                // The cost of the constructor would be zero with JIT compiler enabling optimization.
                return new GChar(UnicodeChar.ASCII_SPACE);
            }
        }

        /// <summary>
        /// An instance of NUL (U+0000)
        /// </summary>
        public static GChar ASCII_NUL {
            get {
                // The cost of the constructor would be zero with JIT compiler enabling optimization.
                return new GChar(UnicodeChar.ASCII_NUL);
            }
        }

        /// <summary>
        /// Flag bits
        /// </summary>
        internal GCharFlags Flags {
            get {
                return (GCharFlags)(_bits & (uint)GCharFlags.Mask);
            }
        }

        /// <summary>
        /// Unicode code point
        /// </summary>
        public uint CodePoint {
            get {
                return _bits & CodePointMask;
            }
        }

        /// <summary>
        /// Whether this object represents a wide-width character.
        /// </summary>
        public bool IsWideWidth {
            get {
                return (_bits & (uint)GCharFlags.WideWidth) != 0u;
            }
        }

        /// <summary>
        /// Whether this object represents right-half of a wide-width character.
        /// </summary>
        public bool IsRightHalf {
            get {
                const uint mask = (uint)(GCharFlags.WideWidth | GCharFlags.RightHalf);
                return (_bits & mask) == mask;
            }
        }

        /// <summary>
        /// Whether this object represents left-half of a wide-width character.
        /// </summary>
        public bool IsLeftHalf {
            get {
                const uint mask = (uint)(GCharFlags.WideWidth | GCharFlags.RightHalf);
                return (_bits & mask) == (uint)GCharFlags.WideWidth;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ch">Unicode character</param>
        public GChar(UnicodeChar ch) {
            this._bits = ch.RawData & UnicodeCharMask;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="codePoint">Unicode code point</param>
        /// <param name="flags">flags</param>
        public GChar(uint codePoint, GCharFlags flags) {
            this._bits = codePoint | (uint)flags;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bits">raw bits</param>
        private GChar(uint bits) {
            this._bits = bits;
        }

        /// <summary>
        /// Copy character into the char array in UTF-16.
        /// </summary>
        /// <param name="seq">destination array</param>
        /// <param name="index">start index of the array</param>
        /// <returns>char count written</returns>
        public int WriteTo(char[] seq, int index) {
            return Unicode.WriteCodePointTo(this._bits & CodePointMask, seq, index);
        }

        /// <summary>
        /// Adds flags.
        /// </summary>
        /// <param name="ch">object to be based on</param>
        /// <param name="flags">flags to add</param>
        /// <returns>new object</returns>
        public static GChar operator +(GChar ch, GCharFlags flags) {
            return new GChar(ch._bits | (uint)flags);
        }
    }

    /// <summary>
    /// Flag bits for <see cref="GAttr"/>.
    /// </summary>
    [Flags]
    internal enum GAttrFlags : uint {
        None = 0u,
        Blink = 1u << 19,
        Hidden = 1u << 20,
        Underlined = 1u << 21,
        Bold = 1u << 22,
        UseCjkFont = 1u << 23,
        Cursor = 1u << 24,
        Selected = 1u << 25,
        Inverted = 1u << 26,
        Use24bitForeColor = 1u << 27,
        Use24bitBackColor = 1u << 28,
        Use8bitForeColor = 1u << 29,
        Use8bitBackColor = 1u << 30,
        SameToPrevious = 1u << 31,
    }

    /// <summary>
    /// Attribute information in <see cref="GLine"/>.
    /// </summary>
    /// <remarks>
    /// This object doesn't contain 24 bit colors.<br/>
    /// 24 bit colors are maintained by array of <see cref="GColor24"/>.
    /// </remarks>
    internal struct GAttr {
        // bit 0..7  : 8 bit fore color code
        // bit 8..16 : 8 bit back color code
        //
        // bit 19 : blink
        // bit 20 : hidden
        // bit 21 : underlined
        // bit 22 : bold
        // bit 23 : use cjk font
        // bit 24 : cursor
        // bit 25 : selected
        // bit 26 : inverted
        // bit 27 : use 24 bit fore color
        // bit 28 : use 24 bit back color
        // bit 29 : use 8 bit fore color
        // bit 30 : use 8 bit back color
        // bit 31 : marker to tell that this cell has the same flags/colors to previous cell.

        private readonly uint _bits;

        /// <summary>
        /// Default value
        /// </summary>
        public static GAttr Default {
            get {
                return new GAttr(); // all bits must be zero
            }
        }

        /// <summary>
        /// Back color (8 bit color code)
        /// </summary>
        public int BackColor {
            get {
                return (int)((this._bits >> 8) & 0xffu);
            }
        }

        /// <summary>
        /// Fore color (8 bit color code)
        /// </summary>
        public int ForeColor {
            get {
                return (int)(this._bits & 0xffu);
            }
        }

        /// <summary>
        /// Whether this GAttr uses 24 bit colors.
        /// </summary>
        public bool Uses24bitColor {
            get {
                return (this._bits & (uint)(GAttrFlags.Use24bitBackColor | GAttrFlags.Use24bitForeColor)) != 0u;
            }
        }

        /// <summary>
        /// Whether this GAttr represents the default settings.
        /// </summary>
        public bool IsDefault {
            get {
                return this.CoreBits == GAttr.Default.CoreBits;
            }
        }

        /// <summary>
        /// Intenal bits without "SameToPrevious" bit.
        /// </summary>
        private uint CoreBits {
            get {
                return this._bits & ~(uint)GAttrFlags.SameToPrevious;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="backColor">back color (8 bit color code)</param>
        /// <param name="foreColor">fore color (8 bit color code)</param>
        /// <param name="flags">flags</param>
        public GAttr(int backColor, int foreColor, GAttrFlags flags) {
            uint flagBits = (uint)flags;
            uint bits = flagBits & ~0xffffu;
            if ((flagBits & (uint)GAttrFlags.Use8bitForeColor) != 0u) {
                bits |= (uint)(foreColor & 0xff);
            }
            if ((flagBits & (uint)GAttrFlags.Use8bitBackColor) != 0u) {
                bits |= (uint)((backColor & 0xff) << 8);
            }
            this._bits = bits;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bits">raw bits</param>
        private GAttr(uint bits) {
            this._bits = bits;
        }

        /// <summary>
        /// Gets a new value that is specified to use default fore color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWithDefaultForeColor() {
            return new GAttr(this._bits & ~(0xffu | (uint)GAttrFlags.Use8bitForeColor | (uint)GAttrFlags.Use24bitForeColor));
        }

        /// <summary>
        /// Gets a new value that is specified to use 8 bit fore color.
        /// </summary>
        /// <param name="color">8 bit color code</param>
        /// <returns>a new value</returns>
        public GAttr CopyWith8bitForeColor(int color) {
            return new GAttr(
                        (this._bits & ~(0xffu | (uint)GAttrFlags.Use8bitForeColor | (uint)GAttrFlags.Use24bitForeColor))
                        | (uint)(color & 0xff)
                        | (uint)GAttrFlags.Use8bitForeColor);
        }

        /// <summary>
        /// Gets a new value that is specified to use 24 bit fore color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWith24bitForeColor() {
            return new GAttr(
                        (this._bits & ~(0xffu | (uint)GAttrFlags.Use8bitForeColor | (uint)GAttrFlags.Use24bitForeColor))
                        | (uint)GAttrFlags.Use24bitForeColor);
        }

        /// <summary>
        /// Gets a new value that is specified to use default back color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWithDefaultBackColor() {
            return new GAttr(this._bits & ~(0xff00u | (uint)GAttrFlags.Use8bitBackColor | (uint)GAttrFlags.Use24bitBackColor));
        }

        /// <summary>
        /// Gets a new value that is specified to use 8 bit back color.
        /// </summary>
        /// <param name="color">8 bit color code</param>
        /// <returns>a new value</returns>
        public GAttr CopyWith8bitBackColor(int color) {
            return new GAttr(
                        (this._bits & ~(0xff00u | (uint)GAttrFlags.Use8bitBackColor | (uint)GAttrFlags.Use24bitBackColor))
                        | ((uint)(color & 0xff) << 8)
                        | (uint)GAttrFlags.Use8bitBackColor);
        }

        /// <summary>
        /// Gets a new value that is specified to use 24 bit back color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWith24bitBackColor() {
            return new GAttr(
                        (this._bits & ~(0xff00u | (uint)GAttrFlags.Use8bitBackColor | (uint)GAttrFlags.Use24bitBackColor))
                        | (uint)GAttrFlags.Use24bitBackColor);
        }

        /// <summary>
        /// Checks if one or more of the specified flags were set.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns>true if one or more of the specified flags were set.</returns>
        public bool Has(GAttrFlags flags) {
            return (this._bits & (uint)flags) != 0u;
        }

        /// <summary>
        /// Adds flags.
        /// </summary>
        /// <param name="attr">object to be based on</param>
        /// <param name="flags">flags to set</param>
        /// <returns>new object</returns>
        public static GAttr operator +(GAttr attr, GAttrFlags flags) {
            return new GAttr(attr._bits | (uint)flags);
        }

        /// <summary>
        /// Removes flags.
        /// </summary>
        /// <param name="attr">object to be based on</param>
        /// <param name="flags">flags to remove</param>
        /// <returns>new object</returns>
        public static GAttr operator -(GAttr attr, GAttrFlags flags) {
            return new GAttr(attr._bits & ~(uint)flags);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="attr1"></param>
        /// <param name="attr2"></param>
        /// <returns></returns>
        public static bool operator ==(GAttr attr1, GAttr attr2) {
            return attr1.CoreBits == attr2.CoreBits;
        }

        /// <summary>
        /// Non-equality operator
        /// </summary>
        /// <param name="attr1"></param>
        /// <param name="attr2"></param>
        /// <returns></returns>
        public static bool operator !=(GAttr attr1, GAttr attr2) {
            return !(attr1 == attr2);
        }

        public override bool Equals(object obj) {
            if (obj is GAttr) {
                return this == (GAttr)obj;
            }
            return false;
        }

        public override int GetHashCode() {
            return (int)this.CoreBits;
        }
    }

    /// <summary>
    /// Cell of <see cref="GLine"/>.
    /// </summary>
    internal struct GCell {
        public GChar Char;
        public GAttr Attr;

        public GCell(GChar ch, GAttr attr) {
            Char = ch;
            Attr = attr;
        }

        public void Set(GChar ch, GAttr attr) {
            // this method can assign members faster comparing to the following methods.
            // 1:
            //   array[i].Char = ch;
            //   array[i].Attr = attr;  // need realoding of the address of array[i]
            // 2:
            //   array[i] = new GCell(ch, attr); // need copying temporary 8 bytes.

            Char = ch;
            Attr = attr;
        }

        public void SetNul() {
            Char = GChar.ASCII_NUL;
            Attr -= GAttrFlags.UseCjkFont;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="GCell"/> array.
    /// </summary>
    internal static class GCellArrayMixin {

        /// <summary>
        /// Copy <see cref="GCell"/>s to another array.
        /// </summary>
        /// <param name="srcArray">source array</param>
        /// <param name="dstArray">destination array</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyTo(this GCell[] srcArray, GCell[] dstArray) {
            for (int i = 0; i < srcArray.Length; i++) {
                dstArray[i] = srcArray[i];
            }
        }

        /// <summary>
        /// Fill <see cref="GCell"/>s with the specified value.
        /// </summary>
        /// <param name="dstArray">destination array</param>
        /// <param name="offsetStart">start offset of the range (inclusive)</param>
        /// <param name="offsetEnd">end offset of the range (exclusive)</param>
        /// <param name="fillChar"><see cref="GChar"/> value to fill</param>
        /// <param name="fillAttr"><see cref="GAttr"/> value to fill</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Fill(this GCell[] dstArray, int offsetStart, int offsetEnd, GChar fillChar, GAttr fillAttr) {
            for (int i = offsetStart; i < offsetEnd; i++) {
                dstArray[i].Set(fillChar, fillAttr);
            }
        }
    }

    /// <summary>
    /// 24 bit color information in <see cref="GLine"/>.
    /// </summary>
    internal struct GColor24 {
        // bit 0 ..7  : B
        // bit 8 ..15 : G
        // bit 16..23 : R
        private uint _foreColor;
        private uint _backColor;

        /// <summary>
        /// 24 bit fore color
        /// </summary>
        public Color ForeColor {
            get {
                return Color.FromArgb((int)(_foreColor | 0xff000000u));
            }
            set {
                _foreColor = (uint)(value.ToArgb() & 0xffffff);
            }
        }

        /// <summary>
        /// 24 bit back color
        /// </summary>
        public Color BackColor {
            get {
                return Color.FromArgb((int)(_backColor | 0xff000000u));
            }
            set {
                _backColor = (uint)(value.ToArgb() & 0xffffff);
            }
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <returns></returns>
        public static bool operator ==(GColor24 col1, GColor24 col2) {
            return col1._foreColor == col2._foreColor && col1._backColor == col2._backColor;
        }

        /// <summary>
        /// Non-equality operator
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <returns></returns>
        public static bool operator !=(GColor24 col1, GColor24 col2) {
            return !(col1 == col2);
        }

        public override bool Equals(object obj) {
            if (obj is GColor24) {
                return this == ((GColor24)obj);
            }
            return false;
        }

        public override int GetHashCode() {
            return (int)(this._foreColor + this._backColor);
        }
    }

    /// <summary>
    /// Extension methods for <see cref="GColor24"/> array.
    /// </summary>
    internal static class GColor24ArrayMixin {

        /// <summary>
        /// Copy <see cref="GColor24"/>s to another array.
        /// </summary>
        /// <param name="srcArray">source array</param>
        /// <param name="dstArray">destination array</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyTo(this GColor24[] srcArray, GColor24[] dstArray) {
            for (int i = 0; i < srcArray.Length; i++) {
                dstArray[i] = srcArray[i];
            }
        }

        /// <summary>
        /// Fill <see cref="GColor24"/>s with the specified value.
        /// </summary>
        /// <param name="dstArray">destination array</param>
        /// <param name="offsetStart">start offset of the range (inclusive)</param>
        /// <param name="offsetEnd">end offset of the range (exclusive)</param>
        /// <param name="fillValue"><see cref="GColor24"/> value to fill</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void Fill(this GColor24[] dstArray, int offsetStart, int offsetEnd, GColor24 fillValue) {
            for (int i = offsetStart; i < offsetEnd; i++) {
                dstArray[i] = fillValue;
            }
        }
    }

    /// <summary>
    /// Represents a single line.
    /// </summary>
    public sealed class GLine {

        // Note:
        //  If 24 bit colors are not used in this line, GColor24 array can be null for reducing memory usage.
        //
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
        private GColor24[] _color24;    // can be null if 24 bit colors are not used
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
            _cell = new CompactGCellArray(length);
            _color24 = null;    // 24 bit colors are not used
            _displayLength = 0;
            _id = -1;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cell">cell data</param>
        /// <param name="color24">24 bit colors</param>
        /// <param name="displayLength">length of the content</param>
        /// <param name="eolType">type of the line ending</param>
        internal GLine(CompactGCellArray cell, GColor24[] color24, int displayLength, EOLType eolType) {
            _cell = cell;
            _color24 = color24;
            _displayLength = displayLength;
            _eolType = eolType;
            _id = -1;
        }

        /// <summary>
        /// Updates content in this line.
        /// </summary>
        /// <param name="cells">cell data to be copied</param>
        /// <param name="color24">24 bit colors to be copied, or null</param>
        /// <param name="displayLength">length of the content</param>
        /// <param name="eolType">type of the line ending</param>
        internal void UpdateContent(IGCellArray cells, GColor24[] color24, int displayLength, EOLType eolType) {
            lock (this) {
                _cell.Reset(cells);

                if (color24 == null) {
                    _color24 = null;
                }
                else if (_color24 != null && _color24.Length == color24.Length) {
                    color24.CopyTo(_color24);
                }
                else {
                    _color24 = (GColor24[])color24.Clone();
                }

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
                this.UpdateContent(line._cell, line._color24, line._displayLength, line._eolType);
                this._id = line._id;
            }
        }

        /// <summary>
        /// Creates cloned instance.
        /// </summary>
        /// <returns>cloned instance</returns>
        public GLine Clone() {
            lock (this) {
                GLine nl = new GLine(
                            _cell.Clone(),
                            (_color24 != null) ? (GColor24[])_color24.Clone() : null,
                            _displayLength,
                            _eolType);
                nl._id = _id;
                return nl;
            }
        }

        /// <summary>
        /// Duplicates internal buffer.
        /// </summary>
        /// <param name="reusableColorArray">reusable array, or null</param>
        /// <param name="cells">
        /// if <paramref name="reusableCellArray"/> is available for storing cells, <paramref name="reusableCellArray"/> with copied cell data will be returned.
        /// otherwise, cloned cell data array will be returned.
        /// </param>
        /// <param name="colorArray">
        /// if <paramref name="reusableColorArray"/> is available for storing cells, <paramref name="reusableColorArray"/> with copied color data will be returned.
        /// otherwise, cloned color data array will be returned.
        /// </param>
        internal void DuplicateBuffers(IGCellArray cells, GColor24[] reusableColorArray, out GColor24[] colorArray) {
            lock (this) {
                cells.Reset(_cell);
                colorArray = Duplicate24bitColors(reusableColorArray);
            }
        }

        /// <summary>
        /// Duplicates internal 24 bit color data.
        /// </summary>
        /// <param name="reusable">reusable array, or null</param>
        /// <returns>
        /// if the reusable array is available for storing data, the reusable array with copied color data will be returned.
        /// otherwise, cloned color data array will be returned.
        /// </returns>
        private GColor24[] Duplicate24bitColors(GColor24[] reusable) {
            if (_color24 == null) {
                if (reusable != null && reusable.Length == _cell.Length) {
                    // clear
                    reusable.Fill(0, reusable.Length, new GColor24());
                    return reusable;
                }
                else {
                    return new GColor24[_cell.Length];
                }
            }
            else {
                if (reusable != null && reusable.Length == _color24.Length) {
                    _color24.CopyTo(reusable);
                    return reusable;
                }
                else {
                    return (GColor24[])_color24.Clone();
                }
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
                Fill(0, _cell.Length, GChar.ASCII_NUL, attr, color);
                _displayLength = attr.IsDefault ? 0 : _cell.Length;
            }
        }

        /// <summary>
        /// Fill range with the specified character and attributes.
        /// </summary>
        /// <param name="start">start index of the range (inclusive)</param>
        /// <param name="end">end index of the range (exclusive)</param>
        /// <param name="ch">character to fill cells</param>
        /// <param name="attr">attributes to fill cells</param>
        /// <param name="color">24 bit colors to fill cells</param>
        private void Fill(int start, int end, GChar ch, GAttr attr, GColor24 color) {
            bool uses24bitColor = attr.Uses24bitColor;
            if (uses24bitColor && _color24 == null) {
                _color24 = new GColor24[_cell.Length];
            }

            GCell fillCell = new GCell(ch, attr + GAttrFlags.SameToPrevious);

            if (start < end) {
                _cell.Set(start, fillCell);
                if (uses24bitColor) {
                    _color24[start] = color;
                }
                UpdateSameToPrevious(start);
            }

            for (int i = start + 1; i < end; i++) {
                _cell.Set(i, fillCell);
                if (uses24bitColor) {
                    _color24[i] = color;
                }
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
                    if (_cell.AttrAt(index - 1) == _cell.AttrAt(index) && (_color24 == null || _color24[index - 1] == _color24[index])) {
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

                if (_color24 != null) {
                    GColor24[] newColors = new GColor24[length];
                    _color24.CopyTo(newColors);
                    _color24 = newColors;
                }

                Fill(oldLength, length, GChar.ASCII_NUL, GAttr.Default, new GColor24());
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
                    GColor24 color24 = (_color24 != null) ? _color24[cellStart] : new GColor24();

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
            CompactGCellArray cells = new CompactGCellArray(columns);
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
                cells.Set(offset, gchar, attr);
                if (colorBuff != null) {
                    colorBuff[offset] = colors;
                }

                if (offset == 0) {
                    // next cell has "SameToPrevious" flag
                    attr += GAttrFlags.SameToPrevious;
                }
                offset++;

                if (gchar.IsWideWidth) {
                    cells.Set(offset, gchar + GCharFlags.RightHalf, attr);
                    if (colorBuff != null) {
                        colorBuff[offset] = colors;
                    }
                    offset++;
                }
            }

            return new GLine(cells, null, offset, EOLType.CRLF);
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

        private readonly GCellArray _cell = new GCellArray(1);
        private GColor24[] _color24 = new GColor24[1];  // always non-null
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

            if (_color24 == null || length != _color24.Length) {
                _color24 = new GColor24[length];
            }

            for (int i = 0; i < _color24.Length; i++) {
                _color24[i] = new GColor24();
            }

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
                line.DuplicateBuffers(_cell, _color24, out _color24);
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

            if (length > _color24.Length) {
                GColor24[] newColors = new GColor24[length];
                _color24.CopyTo(newColors);
                _color24 = newColors;
            }
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
                _cell.Set(_caretColumn, newChar, newAttr);
                _color24[_caretColumn] = newColor;
            }

            _caretColumn++;

            if (newChar.IsWideWidth) {
                if (_caretColumn >= 0 && _caretColumn < _cell.Length) {
                    _cell.Set(_caretColumn, newChar + GCharFlags.RightHalf, newAttr);
                    _color24[_caretColumn] = newColor;
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
                _cell.Set(i, GChar.ASCII_NUL, fillAttr);
                _color24[i] = fillColor;
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
                _color24[dstIndex] = _color24[srcIndex];
                dstIndex++;
                srcIndex++;
            }

            while (dstIndex < _cell.Length) {
                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell.Set(dstIndex, GChar.ASCII_NUL, fillAttr);
                _color24[dstIndex] = fillColor;
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
                _color24[dstIndex] = _color24[srcIndex];
                dstIndex--;
                srcIndex--;
            }

            while (dstIndex >= limit) {
                _cell.Set(dstIndex, GChar.ASCII_NUL, fillAttr);
                _color24[dstIndex] = fillColor;
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
            bool uses24bitColor;
            int displayLength;
            PrepareExport(out uses24bitColor, out displayLength);

            GLine line = new GLine(
                            new CompactGCellArray(_cell),
                            uses24bitColor ? (GColor24[])_color24.Clone() : null,
                            displayLength,
                            _eolType);
            return line;
        }

        /// <summary>
        /// Export to an existing <see cref="GLine"/>.
        /// </summary>
        /// <param name="line"><see cref="GLine"/> to export to</param>
        public void ExportTo(GLine line) {
            bool uses24bitColor;
            int displayLength;
            PrepareExport(out uses24bitColor, out displayLength);

            line.UpdateContent(_cell, uses24bitColor ? _color24 : null, displayLength, _eolType);
        }

        /// <summary>
        /// Prepare export.
        /// </summary>
        /// <param name="uses24bitColor">whether 24 bit colors are used</param>
        /// <param name="displayLength">displayLength of the <see cref="GLine"/></param>
        private void PrepareExport(out bool uses24bitColor, out int displayLength) {
            bool tempUses24bitColor = false;
            int lastCharIndex = -1;
            for (int i = 0; i < _cell.Length; i++) {
                var cell = _cell.At(i);
                tempUses24bitColor |= cell.Attr.Uses24bitColor;
                if (!cell.Attr.IsDefault || cell.Char.CodePoint != 0u) {
                    lastCharIndex = i;
                }
            }

            // update "SameToPrevious" flags

            _cell.ClearFlags(0, GAttrFlags.SameToPrevious);

            var prevAttr = _cell.AttrAt(0);
            for (int i = 1; i < _cell.Length; i++) {
                var curAttr = _cell.AttrAt(i);
                if (prevAttr == curAttr && (_color24 == null || _color24[i - 1] == _color24[i])) {
                    _cell.SetFlags(i, GAttrFlags.SameToPrevious);
                }
                else {
                    _cell.ClearFlags(i, GAttrFlags.SameToPrevious);
                }
                prevAttr = curAttr;
            }

            uses24bitColor = tempUses24bitColor;
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
