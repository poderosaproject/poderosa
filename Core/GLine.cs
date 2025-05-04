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
using System.Drawing;
using System.Diagnostics;
using System.Text;

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
        RightHalf = 1u << 29,
        CJK = UnicodeCharFlags.CJK,
        WideWidth = UnicodeCharFlags.WideWidth,
    }

    /// <summary>
    /// Character information in <see cref="GLine"/>.
    /// </summary>
    internal struct GChar {
        // bit 0..20 : Unicode Code Point (copied from UnicodeChar)
        //
        // bit 29 : Right half of a wide-width character
        // bit 30 : CJK (copied from UnicodeChar)
        // bit 31 : wide width (copied from UnicodeChar)

        private readonly uint _bits;

        private const uint CodePointMask = 0x1fffffu;
        private const uint UnicodeCharMask = CodePointMask | (uint)(UnicodeCharFlags.WideWidth | UnicodeCharFlags.CJK);

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
        /// Unicode code point
        /// </summary>
        public uint CodePoint {
            get {
                return _bits & CodePointMask;
            }
        }

        /// <summary>
        /// Whether this object represents a CJK character.
        /// </summary>
        public bool IsCJK {
            get {
                return (_bits & (uint)GCharFlags.CJK) != 0u;
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
        /// <param name="bits">raw bits</param>
        private GChar(uint bits) {
            this._bits = bits;
        }

        /// <summary>
        /// Convert to UnicodeChar
        /// </summary>
        /// <returns>UnicodeChar</returns>
        public UnicodeChar ToUnicodeChar() {
            UnicodeCharFlags flags = UnicodeCharFlags.None;
            if (IsCJK) {
                flags |= UnicodeCharFlags.CJK;
            }
            if (IsWideWidth) {
                flags |= UnicodeCharFlags.WideWidth;
            }
            return new UnicodeChar(CodePoint, flags);
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

        /// <summary>
        /// Removes flags.
        /// </summary>
        /// <param name="ch">object to be based on</param>
        /// <param name="flags">flags to remove</param>
        /// <returns>new object</returns>
        public static GChar operator -(GChar ch, GCharFlags flags) {
            return new GChar(ch._bits & ~(uint)flags);
        }
    }

    /// <summary>
    /// Flag bits for <see cref="GAttr"/>.
    /// </summary>
    [Flags]
    internal enum GAttrFlags : uint {
        None = 0u,
        Protected = 1u << 18,
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
        // bit 8..15 : 8 bit back color code
        //
        // bit 18 : protected (DECSED or DECSEL cannot erase)
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
        /// Get a new instance retaining only the common attributes of this instance and another instance.
        /// </summary>
        /// <remarks>
        /// Different attributes between two instances are set to default values.
        /// </remarks>
        /// <param name="another">another instance to be compared</param>
        /// <returns>new instance</returns>
        public GAttr GetCommon(GAttr another) {
            GAttr common = new GAttr((this.CoreBits & ~0xffffu) & (another.CoreBits & ~0xffffu)); // except colors

            if (common.Has(GAttrFlags.Use8bitForeColor)) {
                if (this.ForeColor == another.ForeColor) {
                    common = common.CopyWith8bitForeColor(this.ForeColor);
                }
                else {
                    // reset to default
                    common = common.CopyWithDefaultForeColor();
                }
            }

            if (common.Has(GAttrFlags.Use8bitBackColor)) {
                if (this.BackColor == another.BackColor) {
                    common = common.CopyWith8bitBackColor(this.BackColor);
                }
                else {
                    // reset to default
                    common = common.CopyWithDefaultBackColor();
                }
            }

            return common;
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
        /// Reverse flags.
        /// </summary>
        /// <param name="attr">object to be based on</param>
        /// <param name="flags">flags to reverse</param>
        /// <returns>new object</returns>
        public static GAttr operator ^(GAttr attr, GAttrFlags flags) {
            return new GAttr(attr._bits ^ (uint)flags);
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
    /// Z-Order for GLine update spans.
    /// </summary>
    public struct GLineZOrder : IComparable<GLineZOrder>, IEquatable<GLineZOrder> {

        internal const uint MAX_DIFFERENCE = 2147483647u;

        private readonly uint _value;

        private GLineZOrder(uint value) {
            _value = value;
        }

#if UNITTEST
        public static GLineZOrder CreateForTest(uint value) {
            return new GLineZOrder(value);
        }
#endif

        /// <summary>
        /// Compares with another value.
        /// </summary>
        /// <remarks>
        /// This method determines correctly when the difference between the two values in mod 2^32 is within 2147483647.
        /// Currently, z-order is incremented when a new sixel image is created, and used to determine the overlap between text and sixel images.
        /// The older sixel images will be deleted due to the image quantity limitation, so the range of 2147483647 is enough.
        /// </remarks>
        /// <param name="another">another Z-order</param>
        /// <returns></returns>
        public int CompareTo(GLineZOrder another) {
            if (this._value == another._value) {
                return 0;
            }
            uint d = unchecked(this._value - another._value);
            return (d <= MAX_DIFFERENCE) ? 1 : -1;
        }

        public bool Equals(GLineZOrder another) {
            return this._value == another._value;
        }

        public override bool Equals(object obj) {
            if (obj is GLineZOrder) {
                return Equals((GLineZOrder)obj);
            }
            return false;
        }

        public static bool operator ==(GLineZOrder a, GLineZOrder b) {
            return a.Equals(b);
        }

        public static bool operator !=(GLineZOrder a, GLineZOrder b) {
            return !a.Equals(b);
        }

        public override int GetHashCode() {
            return this._value.GetHashCode();
        }

        /// <summary>
        /// Manage <see cref="GLineZOrder"/> for each terminal.
        /// </summary>
        public class Manager {

            private bool _initial = true;
            private uint _current = 0;

            public Manager() {
            }

            /// <summary>
            /// Whether the current z-order is initial value or not.
            /// </summary>
            public bool IsInitial {
                get {
                    return _initial;
                }
            }

            /// <summary>
            /// Current z-order.
            /// </summary>
            public GLineZOrder Current {
                get {
                    return new GLineZOrder(_current);
                }
            }

            /// <summary>
            /// Increment z-order.
            /// </summary>
            /// <returns>new z-order.</returns>
            public GLineZOrder Increment() {
                _current++;
                _initial = false;
                return new GLineZOrder(_current);
            }

#if UNITTEST
            public void SetCurrentForTest(uint value) {
                _current = value;
            }
#endif
        }
    }

    /// <summary>
    /// Column span on <see cref="GLine"/>
    /// </summary>
    public struct GLineColumnSpan {
        /// <summary>
        /// Z-Order
        /// </summary>
        /// <seealso cref="GLineZOrder"/>
        public readonly GLineZOrder Z;

        /// <summary>
        /// Start column index (inclusive)
        /// </summary>
        public readonly int Start;

        /// <summary>
        /// End column index (exclusive)
        /// </summary>
        public readonly int End;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="z">z-order</param>
        /// <param name="start">start column index (inclusive)</param>
        /// <param name="end">end column index (exclusive)</param>
        public GLineColumnSpan(GLineZOrder z, int start, int end) {
            this.Z = z;
            this.Start = start;
            this.End = end;
        }

        /// <summary>
        /// Merge this span and another span.
        /// </summary>
        /// <param name="another">another span</param>
        /// <returns>new span if merging succeeded, otherwise null.</returns>
        public GLineColumnSpan? Merge(GLineColumnSpan another) {
            if (another.Z != this.Z || another.End < this.Start || another.Start > this.End) {
                return null;
            }

            return new GLineColumnSpan(
                        this.Z,
                        Math.Min(this.Start, another.Start),
                        Math.Max(this.End, another.End)
            );
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

        // core data
        private GCell[] _cell;
        private GColor24[] _color24;    // can be null if 24 bit colors are not used
        private int _displayLength;
        private EOLType _eolType;
        private LineRenderingType _lineRenderingType;
        private int _id;
        private GLine _nextLine;
        private GLine _prevLine;

        // the list of the update column spans.
        // when GLine is cloned for drawing, this list is **moved** to the instance for drawing.
        private List<GLineColumnSpan> _updatedSpans = null;

        [ThreadStatic]
        private static char[] _copyTempBuff;
        [ThreadStatic]
        private static int[] _copyDxBuff;

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

        // Returns thread-local temporary buffer for internal use.
        private int[] GetInternalTemporaryDxBuffer(int minLen) {
            int[] buff = _copyDxBuff;
            if (buff == null || buff.Length < minLen) {
                buff = _copyDxBuff = new int[minLen];
            }
            return buff;
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

        public LineRenderingType LineRenderingType {
            get {
                return _lineRenderingType;
            }
        }

        public IEnumerable<GLineColumnSpan> UpdatedSpans {
            get {
                return _updatedSpans;
            }
        }

        /// <summary>
        /// Whether this line is rendered double width.
        /// </summary>
        public bool IsDoubleWidth {
            get {
                return (_lineRenderingType & LineRenderingType.DoubleWidth) != 0;
            }
        }

        /// <summary>
        /// Whether this line is rendered double height.
        /// </summary>
        public bool IsDoubleHeight {
            get {
                return (_lineRenderingType & LineRenderingType.DoubleHeight) != 0;
            }
        }

        /// <summary>
        /// Whether this line is rendered as lower-half of double height.
        /// </summary>
        public bool IsLowerHalf {
            get {
                return (_lineRenderingType & LineRenderingType.LowerHalf) != 0;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="length"></param>
        /// <param name="dec">text decoration (optional)</param>
        public GLine(int length, TextDecoration dec = null) {
            if (length <= 0) {
                throw new ArgumentException("invalid length : " + length);
            }

            _cell = new GCell[length];

            if (dec != null) {
                GAttr attr = dec.Attr;

                if (length > 0) {
                    _cell[0] = new GCell(GChar.ASCII_NUL, attr + GAttrFlags.SameToPrevious - GAttrFlags.Protected);
                    int copied = 1;
                    while (copied < length) {
                        int copyLen = Math.Min(length - copied, copied);
                        Array.Copy(_cell, 0, _cell, copied, copyLen);
                        copied += copyLen;
                    }
                    _cell[0].Attr -= GAttrFlags.SameToPrevious;
                }

                if (attr.Uses24bitColor) {
                    _color24 = new GColor24[length];
                    _color24[0] = dec.Color24;
                    int copied = 1;
                    while (copied < length) {
                        int copyLen = Math.Min(length - copied, copied);
                        Array.Copy(_color24, 0, _color24, copied, copyLen);
                        copied += copyLen;
                    }
                }
                else {
                    _color24 = null;
                }
            }
            else {
                _color24 = null;
            }

            _displayLength = 0;
            _eolType = EOLType.Continue;
            _lineRenderingType = LineRenderingType.Normal;
            _id = -1;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cell">cell data</param>
        /// <param name="color24">24 bit colors</param>
        /// <param name="displayLength">length of the content</param>
        /// <param name="eolType">type of the line ending</param>
        /// <param name="lineRenderingType">type of the line rendering</param>
        /// <param name="updatedSpans">updated column spans to set, or null if no spans</param>
        internal GLine(GCell[] cell, GColor24[] color24, int displayLength, EOLType eolType, LineRenderingType lineRenderingType, List<GLineColumnSpan> updatedSpans) {
            _cell = cell;
            _color24 = color24;
            _displayLength = displayLength;
            _eolType = eolType;
            _lineRenderingType = lineRenderingType;
            _id = -1;
            _updatedSpans = updatedSpans;
        }

        /// <summary>
        /// Updates content in this line.
        /// </summary>
        /// <param name="cell">cell data to be copied</param>
        /// <param name="color24">24 bit colors to be copied, or null</param>
        /// <param name="displayLength">length of the content</param>
        /// <param name="eolType">type of the line ending</param>
        /// <param name="lineRenderingType">type of the line rendering</param>
        /// <param name="updatedSpans">updated column spans to set, or null if no spans.</param>
        /// <param name="mergeUpdatedSpans">if true, <paramref name="updatedSpans"/> is merged into the existing spans.</param>
        internal void UpdateContent(GCell[] cell, GColor24[] color24, int displayLength, EOLType eolType, LineRenderingType lineRenderingType, List<GLineColumnSpan> updatedSpans, bool mergeUpdatedSpans) {
            lock (this) {
                if (_cell.Length == cell.Length) {
                    cell.CopyTo(_cell);
                }
                else {
                    _cell = (GCell[])cell.Clone();
                }

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
                _lineRenderingType = lineRenderingType;

                if (!mergeUpdatedSpans) {
                    _updatedSpans = updatedSpans;
                }
                else if (updatedSpans != null) {
                    if (_updatedSpans == null) {
                        _updatedSpans = updatedSpans;
                    }
                    else {
                        GLineColumnSpan? merged;
                        if (_updatedSpans.Count > 0 && updatedSpans.Count > 0) {
                            // try to merge with previous one
                            merged = _updatedSpans[_updatedSpans.Count - 1].Merge(updatedSpans[0]);
                        }
                        else {
                            merged = null;
                        }

                        if (merged.HasValue) {
                            updatedSpans[0] = merged.Value;
                            _updatedSpans.RemoveAt(_updatedSpans.Count - 1);
                            _updatedSpans.AddRange(updatedSpans);
                        }
                        else {
                            _updatedSpans.AddRange(updatedSpans);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copys content and ID from the specified instance.
        /// The updated column spans are moved to the new instance from the this instance.
        /// </summary>
        /// <param name="line">another instance</param>
        public void CopyAndMoveUpdateSpansFrom(GLine line) {
            lock (this) {
                List<GLineColumnSpan> updatedSpans = line._updatedSpans;
                line._updatedSpans = null;
                this.UpdateContent(line._cell, line._color24, line._displayLength, line._eolType, line._lineRenderingType, updatedSpans, false /*replace*/);
                this._id = line._id;
            }
        }

        /// <summary>
        /// Creates cloned instance.
        /// The updated column spans are moved to the new instance from the this instance.
        /// </summary>
        /// <returns>cloned instance</returns>
        public GLine CloneAndMoveUpdateSpans() {
            lock (this) {
                GLine nl = CloneInternal(_updatedSpans);
                _updatedSpans = null;
                return nl;
            }
        }

        /// <summary>
        /// Creates cloned instance.
        /// The updated column spans in the new instance are reset.
        /// </summary>
        /// <returns>cloned instance</returns>
        public GLine CloneWithoutUpdateSpans() {
            lock (this) {
                GLine nl = CloneInternal(null);
                return nl;
            }
        }

        /// <summary>
        /// Creates cloned instance.
        /// </summary>
        /// <param name="updatedSpans">updated column spans for the new instance</param>
        /// <returns>cloned instance</returns>
        private GLine CloneInternal(List<GLineColumnSpan> updatedSpans) {
            GLine nl = new GLine(
                        (GCell[])_cell.Clone(),
                        (_color24 != null) ? (GColor24[])_color24.Clone() : null,
                        _displayLength,
                        _eolType,
                        _lineRenderingType,
                        updatedSpans
                    );
            nl._id = _id;
            return nl;
        }

        /// <summary>
        /// Duplicates internal buffer.
        /// </summary>
        /// <param name="reusableCellArray">reusable array, or null</param>
        /// <param name="reusableColorArray">reusable array, or null</param>
        /// <param name="cellArray">
        /// if <paramref name="reusableCellArray"/> is available for storing cells, <paramref name="reusableCellArray"/> with copied cell data will be returned.
        /// otherwise, cloned cell data array will be returned.
        /// </param>
        /// <param name="colorArray">
        /// if <paramref name="reusableColorArray"/> is available for storing cells, <paramref name="reusableColorArray"/> with copied color data will be returned.
        /// otherwise, cloned color data array will be returned.
        /// </param>
        internal void DuplicateBuffers(GCell[] reusableCellArray, GColor24[] reusableColorArray, out GCell[] cellArray, out GColor24[] colorArray) {
            lock (this) {
                cellArray = DuplicateCells(reusableCellArray);
                colorArray = Duplicate24bitColors(reusableColorArray);
            }
        }

        /// <summary>
        /// Duplicates internal cell data.
        /// </summary>
        /// <param name="reusable">reusable array, or null</param>
        /// <returns>
        /// if the reusable array is available for storing data, the reusable array with copied cell data will be returned.
        /// otherwise, cloned cell data array will be returned.
        /// </returns>
        private GCell[] DuplicateCells(GCell[] reusable) {
            if (reusable != null && reusable.Length == _cell.Length) {
                _cell.CopyTo(reusable);
                return reusable;
            }
            else {
                return (GCell[])_cell.Clone();
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
        /// Clears content with the specified background color.
        /// </summary>
        /// <param name="dec">text decoration for specifying the background color, or null for using default attributes.</param>
        /// <param name="selective">if true, protected characters are retained.</param>
        /// <param name="resetLineRenderingType">if true, LineRenderingType is reset to Normal.</param>
        public void Clear(TextDecoration dec = null, bool selective = false, bool resetLineRenderingType = true) {
            TextDecoration d = dec ?? TextDecoration.Default;
            GAttr attr = d.Attr;
            GColor24 color = d.Color24;

            lock (this) {
                if (resetLineRenderingType) {
                    _lineRenderingType = LineRenderingType.Normal;
                }
                if (selective) {
                    FillSelective(0, _cell.Length, GChar.ASCII_NUL, attr, color);
                }
                else {
                    Fill(0, _cell.Length, GChar.ASCII_NUL, attr, color);
                    _displayLength = attr.IsDefault ? 0 : _cell.Length;
                }
                _eolType = EOLType.CRLF;
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

            GCell fillCell = new GCell(ch, attr + GAttrFlags.SameToPrevious - GAttrFlags.Protected);

            if (start < end) {
                _cell[start] = fillCell;
                if (uses24bitColor) {
                    _color24[start] = color;
                }
                UpdateSameToPrevious(start);
            }

            for (int i = start + 1; i < end; i++) {
                _cell[i] = fillCell;
                if (uses24bitColor) {
                    _color24[i] = color;
                }
            }

            UpdateSameToPrevious(end);
        }

        /// <summary>
        /// Fill range with the specified character and attributes, but skips protected characters.
        /// </summary>
        /// <param name="start">start index of the range (inclusive)</param>
        /// <param name="end">end index of the range (exclusive)</param>
        /// <param name="ch">character to fill cells</param>
        /// <param name="attr">attributes to fill cells</param>
        /// <param name="color">24 bit colors to fill cells</param>
        private void FillSelective(int start, int end, GChar ch, GAttr attr, GColor24 color) {
            bool uses24bitColor = attr.Uses24bitColor;
            if (uses24bitColor && _color24 == null) {
                _color24 = new GColor24[_cell.Length];
            }

            GCell fillCell = new GCell(ch, attr + GAttrFlags.SameToPrevious - GAttrFlags.Protected);

            for (int i = start; i < end; i++) {
                if (!_cell[i].Attr.Has(GAttrFlags.Protected)) {
                    _cell[i] = fillCell;
                    if (uses24bitColor) {
                        _color24[i] = color;
                    }
                }
                UpdateSameToPrevious(i);
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
                    if (_cell[index - 1].Attr == _cell[index].Attr && (_color24 == null || _color24[index - 1] == _color24[index])) {
                        _cell[index].Attr += GAttrFlags.SameToPrevious;
                    }
                    else {
                        _cell[index].Attr -= GAttrFlags.SameToPrevious;
                    }
                }

                return;
            }

            if (index == 0 && _cell.Length > 0) {
                _cell[index].Attr -= GAttrFlags.SameToPrevious;
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

                byte v = ToCharTypeForWordBreak(_cell[pos].Char);

                int index = pos - 1;
                while (index >= 0 && ToCharTypeForWordBreak(_cell[index].Char) == v) {
                    index--;
                }
                start = index + 1;

                index = pos + 1;
                while (index < _cell.Length && ToCharTypeForWordBreak(_cell[index].Char) == v) {
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
        /// Set rendering type
        /// </summary>
        /// <param name="t">new value</param>
        /// <returns>true if rendering type has been changed</returns>
        public bool SetLineRenderingType(LineRenderingType t) {
            if (_lineRenderingType != t) {
                _lineRenderingType = t;
                return true;
            }
            else {
                return false;
            }
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

                GCell[] oldBuff = _cell;
                GCell[] newBuff = new GCell[length];
                oldBuff.CopyTo(newBuff);
                _cell = newBuff;

                if (_color24 != null) {
                    GColor24[] newColors = new GColor24[length];
                    _color24.CopyTo(newColors);
                    _color24 = newColors;
                }

                Fill(oldBuff.Length, newBuff.Length, GChar.ASCII_NUL, GAttr.Default, new GColor24());
                // Note: _displayLength is not changed.
            }
        }

        /// <summary>
        /// Returns whether this line requires periodic redraw.
        /// </summary>
        public bool IsPeriodicRedrawRequired() {
            lock (this) {
                for (int i = 0; i < _cell.Length; i++) {
                    if (_cell[i].Attr.Has(GAttrFlags.Blink)) {
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
            int y1 = y;
            int y2 = y1 + (int)prof.Pitch.Height;

            float pitch = prof.Pitch.Width;

            Win32.SetBkMode(hdc, Win32.TRANSPARENT);

            int cellStart = 0;
            float fx1 = fx0;

            int textYOffset = 0;
            int etoCommonOpts = 0;
            GAttrFlags underlineFlag = GAttrFlags.Underlined;

            LineRenderingType lineRenderingType = _lineRenderingType;

            if (IsDoubleWidth) {
                pitch *= 2;

                // supports only the case where both double-height and double-width are specified.
                if (IsDoubleHeight) {
                    etoCommonOpts += Win32.ETO_CLIPPED;
                    if (IsLowerHalf) {
                        textYOffset = (prof.LineSpacing / 2) - prof.LineSpacing - (int)prof.Pitch.Height;
                    }
                    else {
                        y2 += prof.LineSpacing;
                        textYOffset = prof.LineSpacing / 2;
                        underlineFlag = GAttrFlags.None;
                    }
                }
            }

            lock (this) {
                // Note:
                //  Currently exclusive execution is not required here
                //  because Render() is called on the temporary instances
                //  copied from the GLine list.
                //  Lock is still here for keeping internal design consistency.

                while (cellStart < _displayLength) {
                    int cellEnd = cellStart + 1;
                    while (cellEnd < _displayLength && _cell[cellEnd].Attr.Has(GAttrFlags.SameToPrevious)) {
                        cellEnd++;
                    }

                    GAttr attr = _cell[cellStart].Attr;
                    GColor24 color24 = (_color24 != null) ? _color24[cellStart] : new GColor24();

                    IntPtr hFont = prof.CalcHFONT(attr, lineRenderingType);
                    Win32.SelectObject(hdc, hFont);

                    Color foreColor;
                    Color backColor;
                    prof.DetermineColors(attr, color24, caret, baseBackColor, out backColor, out foreColor);

                    // If background fill is required, we call ExtTextOut() with ETO_OPAQUE to draw the first character.
                    float fx2 = fx0 + pitch * cellEnd;
                    Win32.RECT rect = new Win32.RECT((int)fx1, y1, (int)fx2, y2);

                    char[] tmpCharBuf = GetInternalTemporaryCharBuffer((cellEnd - cellStart) * 2);
                    int tmpCharBufLen = 0;

                    if (foreColor.A != 0) { // text is opaque
                        int[] dxBuf = GetInternalTemporaryDxBuffer((cellEnd - cellStart) * 2);
                        int dxBufIndex = 0;

                        float curPosF = fx1;
                        int prevPos = (int)curPosF;
                        int cellIndex = cellStart;
                        while (cellIndex < cellEnd) {
                            GChar ch = NulToSpace(_cell[cellIndex].Char);
                            int addedChars = ch.WriteTo(tmpCharBuf, tmpCharBufLen);
                            tmpCharBufLen += addedChars;
                            if (ch.IsWideWidth) {
                                if (ch.IsLeftHalf) {
                                    cellIndex += 2;
                                    curPosF = curPosF + pitch + pitch;
                                }
                                else {
                                    cellIndex++;
                                    curPosF += pitch;
                                }
                            }
                            else {
                                cellIndex++;
                                curPosF += pitch;
                            }
                            int nextPos = (int)curPosF;
                            dxBuf[dxBufIndex++] = nextPos - prevPos;
                            while (--addedChars > 0) {
                                dxBuf[dxBufIndex++] = 0;
                            }
                            prevPos = nextPos;
                        }

                        uint foreColorRef = DrawUtil.ToCOLORREF(foreColor);
                        Win32.SetTextColor(hdc, foreColorRef);

                        int etoOpts;
                        if (backColor.A != 0) {
                            uint bkColorRef = DrawUtil.ToCOLORREF(backColor);
                            Win32.SetBkColor(hdc, bkColorRef);
                            etoOpts = etoCommonOpts + Win32.ETO_OPAQUE;
                        }
                        else {
                            etoOpts = etoCommonOpts;
                        }

                        unsafe {
                            fixed (int* pDx = dxBuf)
                            fixed (char* p = tmpCharBuf) {
                                Win32.ExtTextOut(hdc, rect.left, rect.top + textYOffset, etoOpts, &rect, p, tmpCharBufLen, pDx);
                            }
                        }

                        if (attr.Has(underlineFlag)) {
                            DrawUnderline(hdc, foreColorRef, (int)fx1, y2 - 1, (int)fx2 - (int)fx1);
                        }
                    }
                    else if (backColor.A != 0) { // text is not visible, but background is opaque
                        uint bkColorRef = DrawUtil.ToCOLORREF(backColor);
                        Win32.SetBkColor(hdc, bkColorRef);
                        int etoOpts = etoCommonOpts + Win32.ETO_OPAQUE;
                        unsafe {
                            fixed (char* p = tmpCharBuf) {
                                Win32.ExtTextOut(hdc, rect.left, rect.top + textYOffset, etoOpts, &rect, p, 0, null);
                            }
                        }
                    }

                    cellStart = cellEnd;
                    fx1 = fx2;
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
        /// <param name="adjustedIndex">returns adjusted cell index</param>
        /// <param name="width">returns cursor width (1 or 2)</param>
        internal void SetCursor(int index, out int adjustedIndex, out int width) {
            lock (this) {
                if (index >= _displayLength) {
                    int prevLength = _displayLength;
                    ExpandBuffer(index + 1);
                    _displayLength = index + 1;
                }

                _cell[index].Attr += GAttrFlags.Cursor;

                if (_cell[index].Char.IsRightHalf) {
                    if (index > 0 && _cell[index - 1].Char.IsLeftHalf) {
                        _cell[index - 1].Attr += GAttrFlags.Cursor;
                        UpdateSameToPreviousForCellsChanged(index - 1, index + 1);
                        adjustedIndex = index - 1;
                        width = 2;
                    }
                    else {
                        UpdateSameToPreviousForCellsChanged(index, index + 1);
                        adjustedIndex = index;
                        width = 1;
                    }
                }
                else if (_cell[index].Char.IsLeftHalf) {
                    if (index + 1 < _cell.Length && _cell[index + 1].Char.IsRightHalf) {
                        _cell[index + 1].Attr += GAttrFlags.Cursor;
                        UpdateSameToPreviousForCellsChanged(index, index + 2);
                        adjustedIndex = index;
                        width = 2;
                    }
                    else {
                        UpdateSameToPreviousForCellsChanged(index, index + 1);
                        adjustedIndex = index;
                        width = 1;
                    }
                }
                else {
                    UpdateSameToPreviousForCellsChanged(index, index + 1);
                    adjustedIndex = index;
                    width = 1;
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

                if (from >= 0 && from < _cell.Length && _cell[from].Char.IsRightHalf) {
                    from--;
                }

                if (to >= 0 && to < _cell.Length && _cell[to].Char.IsRightHalf) {
                    to++;
                }

                if (from < 0) {
                    from = 0;
                }

                if (to > _displayLength) {
                    to = _displayLength;
                }

                for (int i = from; i < to; i++) {
                    _cell[i].Attr += GAttrFlags.Selected;
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
                return (index >= 0 && index < _cell.Length) ? _cell[index].Char.IsRightHalf : false;
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
            while (lastNonNulIndex >= 0 && _cell[lastNonNulIndex].Char.CodePoint == 0) {
                lastNonNulIndex--;
            }

            start = Math.Max(0, start);
            end = Math.Min(Math.Min(_cell.Length, lastNonNulIndex + 1), end);

            char[] temp = GetInternalTemporaryBufferForCopy();
            int tempIndex = 0;
            for (int i = start; i < end; i++) {
                GChar ch = _cell[i].Char;
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
        /// <param name="z">z-order</param>
        /// <returns></returns>
        public static GLine CreateSimpleGLine(string text, TextDecoration dec, GLineZOrder z) {
            GCell[] buff = new GCell[text.Length * 2];
            GColor24[] colorBuff = null;
            int offset = 0;
            GAttr attr = dec.Attr;
            GColor24 colors = dec.Color24;

            if (attr.Uses24bitColor) {
                colorBuff = new GColor24[buff.Length];
            }

            UnicodeCharConverter conv = new UnicodeCharConverter(true);

            foreach (char originalChar in text) {
                UnicodeChar unicodeChar;
                if (!conv.Feed(originalChar, out unicodeChar)) {
                    continue;
                }
                if (unicodeChar.IsZeroWidth) {
                    continue; // drop
                }

                GChar gchar = new GChar(unicodeChar);
                buff[offset].Set(gchar, attr);
                if (colorBuff != null) {
                    colorBuff[offset] = colors;
                }

                if (offset == 0) {
                    // next cell has "SameToPrevious" flag
                    attr += GAttrFlags.SameToPrevious;
                }
                offset++;

                if (gchar.IsWideWidth) {
                    buff[offset].Set(gchar + GCharFlags.RightHalf, attr);
                    if (colorBuff != null) {
                        colorBuff[offset] = colors;
                    }
                    offset++;
                }
            }

            List<GLineColumnSpan> updatedSpans =
                (offset > 0) ? new List<GLineColumnSpan> { new GLineColumnSpan(z, 0, offset) } : null;

            return new GLine(buff, null, offset, EOLType.CRLF, LineRenderingType.Normal, updatedSpans);
        }
    }

    /// <summary>
    /// Manipulator for editing line buffer copied from <see cref="GLine"/>.
    /// </summary>
    public class GLineManipulator {

        private readonly GLineZOrder.Manager _zMan;

        // Note:
        //  GColor24 array is always non-null even if the 24 bit colors are not used, and
        //  its length is always same as the length of GCell array.
        //
        // Note:
        //  GAttrFlags.SameToPrevious of each cell is not updated during the manipulation.
        //  The flag will be updated in Export(). 

        private GCell[] _cell = new GCell[1];
        private GColor24[] _color24 = new GColor24[1];  // always non-null
        private byte[] _dirty = new byte[1];
        private EOLType _eolType = EOLType.Continue;
        private LineRenderingType _lineRenderingType = LineRenderingType.Normal;

        /// <summary>
        /// Current buffer size.
        /// </summary>
        public int BufferSize {
            get {
                return _cell.Length;
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
        /// Type of the line rendering.
        /// </summary>
        public LineRenderingType LineRenderingType {
            get {
                return _lineRenderingType;
            }
            set {
                _lineRenderingType = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="zMan">GLine Z-Order manager</param>
        public GLineManipulator(GLineZOrder.Manager zMan) {
            _zMan = zMan;
        }

        /// <summary>
        /// Reset line buffer.
        /// </summary>
        /// <param name="length">length of the internal buffer</param>
        public void Reset(int length) {
            if (_cell == null || length != _cell.Length) {
                _cell = new GCell[length];
            }

            for (int i = 0; i < _cell.Length; i++) {
                _cell[i].Set(GChar.ASCII_NUL, GAttr.Default);
            }

            if (_color24 == null || length != _color24.Length) {
                _color24 = new GColor24[length];
            }

            for (int i = 0; i < _color24.Length; i++) {
                _color24[i] = new GColor24();
            }

            if (_dirty == null || length != _dirty.Length) {
                _dirty = new byte[length];
            }

            ClearDirtyFlagsInternal();

            _eolType = EOLType.Continue;
            _lineRenderingType = LineRenderingType.Normal;
        }

        /// <summary>
        /// Load content from a <see cref="GLine"/>.
        /// </summary>
        /// <param name="line">a line object that the content are copied from.</param>
        /// <remarks>
        /// If <paramref name="line"/> was null, internal buffer will be cleared.
        /// </remarks>
        public void Load(GLine line) {
            if (line == null) {
                Reset(80);
            }
            else {
                line.DuplicateBuffers(_cell, _color24, out _cell, out _color24);
                _eolType = line.EOLType;
                _lineRenderingType = line.LineRenderingType;
                ClearDirtyFlagsInternal();
            }
        }

        /// <summary>
        /// Expand the buffer.
        /// </summary>
        /// <param name="length">minimum buffer size.</param>
        public void ExpandBuffer(int length) {
            if (length > _cell.Length) {
#if DEBUG
                Debug.WriteLine("GLineManipulator.ExpandBuffer: {0}", length);
#endif

                GCell[] oldBuff = _cell;
                GCell[] newBuff = new GCell[length];
                oldBuff.CopyTo(newBuff);
                newBuff.Fill(oldBuff.Length, newBuff.Length, GChar.ASCII_NUL, GAttr.Default);
                _cell = newBuff;
            }

            if (length > _dirty.Length) {
                byte[] newDirty = new byte[length];
                _dirty.CopyTo(newDirty, 0);
                _dirty = newDirty;
            }

            if (length > _color24.Length) {
                GColor24[] newColors = new GColor24[length];
                _color24.CopyTo(newColors);
                _color24 = newColors;
            }
        }

        /// <summary>
        /// Write one character to the specified position.
        /// </summary>
        /// <param name="index">index to write a character.</param>
        /// <param name="ch">character to write.</param>
        /// <param name="dec">text decoration of the character.</param>
        /// <returns>the next index</returns>
        public int PutChar(int index, UnicodeChar ch, TextDecoration dec) {
            if (index < 0) {
                return index;
            }

            GChar newChar = new GChar(ch);
            GAttr newAttr = dec.Attr;
            GColor24 newColor = dec.Color24;

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(index + (newChar.IsWideWidth ? 2 : 1));

            if (newChar.IsCJK) {
                newAttr += GAttrFlags.UseCjkFont;
            }

            FixLeftHalfOfWideWidthCharacter(index - 1);

            _cell[index].Set(newChar, newAttr);
            _color24[index] = newColor;
            _dirty[index] = 1;

            index++;

            if (newChar.IsWideWidth) {
                _cell[index].Set(newChar + GCharFlags.RightHalf, newAttr);
                _color24[index] = newColor;
                _dirty[index] = 1;
                index++;
            }

            FixRightHalfOfWideWidthCharacter(index);

            return index;
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
                if (_cell[index].Char.IsLeftHalf) {
                    _cell[index].SetNul();
                    // dirty flag is not set, because this change is for cosmetic improvement
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
                if (_cell[index].Char.IsRightHalf) {
                    _cell[index].SetNul();
                    // dirty flag is not set, because this change is for cosmetic improvement
                }
            }
        }

        /// <summary>
        /// Get character.
        /// </summary>
        /// <param name="index">column index</param>
        /// <param name="ch">character</param>
        /// <param name="isRightHalf">true if right-half of the character is stored in the column</param>
        /// <returns>true if character exists.</returns>
        public bool GetChar(int index, out UnicodeChar ch, out bool isRightHalf) {
            if (index >= 0 && index < _cell.Length) {
                ch = _cell[index].Char.ToUnicodeChar();
                isRightHalf = _cell[index].Char.IsRightHalf;
                return true;
            }

            ch = UnicodeChar.ASCII_NUL;
            isRightHalf = false;
            return false;
        }

        /// <summary>
        /// Get character attributes.
        /// </summary>
        /// <param name="index">column index</param>
        /// <param name="dec">attributes are stored as TextDecoration</param>
        /// <returns>true if attributes exist.</returns>
        public bool GetAttributes(int index, out TextDecoration dec) {
            if (index >= 0 && index < _cell.Length) {
                GAttr attr = _cell[index].Attr;
                dec = TextDecoration.Default
                        .GetCopyWithBlink(attr.Has(GAttrFlags.Blink))
                        .GetCopyWithBold(attr.Has(GAttrFlags.Bold))
                        .GetCopyWithHidden(attr.Has(GAttrFlags.Hidden))
                        .GetCopyWithInverted(attr.Has(GAttrFlags.Inverted))
                        .GetCopyWithProtected(attr.Has(GAttrFlags.Protected))
                        .GetCopyWithUnderline(attr.Has(GAttrFlags.Underlined));
                if (attr.Has(GAttrFlags.Use24bitForeColor)) {
                    dec = dec.GetCopyWithForeColor(new ColorSpec(_color24[index].ForeColor));
                }
                if (attr.Has(GAttrFlags.Use8bitForeColor)) {
                    dec = dec.GetCopyWithForeColor(new ColorSpec(attr.ForeColor));
                }
                if (attr.Has(GAttrFlags.Use24bitBackColor)) {
                    dec = dec.GetCopyWithBackColor(new ColorSpec(_color24[index].BackColor));
                }
                if (attr.Has(GAttrFlags.Use8bitBackColor)) {
                    dec = dec.GetCopyWithBackColor(new ColorSpec(attr.BackColor));
                }
                return true;
            }

            dec = TextDecoration.Default;
            return false;
        }

        /// <summary>
        /// Fills specified range with spaces (NULs). 
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        /// <param name="dec">text decoration of the blanks</param>
        public void FillSpace(int from, int to, TextDecoration dec) {
            from = Math.Max(from, 0);

            if (to <= from) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(to);

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            FixLeftHalfOfWideWidthCharacter(from - 1);

            for (int i = from; i < to; i++) {
                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell[i].Set(GChar.ASCII_NUL, fillAttr);
                _color24[i] = fillColor;
                _dirty[i] = 1;
            }

            FixRightHalfOfWideWidthCharacter(to);
        }

        /// <summary>
        /// Fills specified range with spaces but not protected characters.
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        /// <param name="dec">text decoration of the blanks</param>
        public void FillSpaceUnprotected(int from, int to, TextDecoration dec) {
            from = Math.Max(from, 0);

            if (to <= from) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(to);

            dec = dec.GetCopyWithProtected(false);

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            FixLeftHalfOfWideWidthCharacter(from - 1);

            for (int i = from; i < to; i++) {
                if (_cell[i].Attr.Has(GAttrFlags.Protected)) {
                    continue;
                }
                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell[i].Set(GChar.ASCII_NUL, fillAttr);
                _color24[i] = fillColor;
                _dirty[i] = 1;
            }

            FixRightHalfOfWideWidthCharacter(to);
        }

        /// <summary>
        /// Fills specified range with spaces but not protected characters.
        /// Character attributes are preserved.
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        public void FillSpaceUnprotectedPreservingAttributes(int from, int to) {
            from = Math.Max(from, 0);

            if (to <= from) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(to);

            FixLeftHalfOfWideWidthCharacter(from - 1);

            for (int i = from; i < to; i++) {
                GAttr attr = _cell[i].Attr;

                if (attr.Has(GAttrFlags.Protected)) {
                    continue;
                }

                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell[i].Set(GChar.ASCII_NUL, attr - GAttrFlags.UseCjkFont);
                _dirty[i] = 1;
            }

            FixRightHalfOfWideWidthCharacter(to);
        }

        /// <summary>
        /// Fills specified range with the specified character.
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        /// <param name="ch">a character to fill with</param>
        /// <param name="dec">attributes to fill with</param>
        public void FillCharacter(int from, int to, UnicodeChar ch, TextDecoration dec) {
            from = Math.Max(from, 0);

            if (to <= from) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(to);

            GChar fillChar = new GChar(ch);
            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            if (fillChar.IsCJK) {
                fillAttr += GAttrFlags.UseCjkFont;
            }

            FixLeftHalfOfWideWidthCharacter(from - 1);

            if (ch.IsWideWidth) {
                int index = from;
                while (index + 1 < to) {
                    _cell[index].Set(fillChar, fillAttr);
                    _cell[index + 1].Set(fillChar + GCharFlags.RightHalf, fillAttr);
                    _color24[index] = fillColor;
                    _color24[index + 1] = fillColor;
                    _dirty[index] = 1;
                    _dirty[index + 1] = 1;
                    index += 2;
                }
                if (index < to) {
                    _cell[index].Set(GChar.ASCII_NUL, fillAttr - GAttrFlags.UseCjkFont);
                    _color24[index] = fillColor;
                    _dirty[index] = 1;
                }
            }
            else {
                for (int index = from; index < to; index++) {
                    _cell[index].Set(fillChar, fillAttr);
                    _color24[index] = fillColor;
                    _dirty[index] = 1;
                }
            }

            FixRightHalfOfWideWidthCharacter(to);
        }

        /// <summary>
        /// Deletes characters in the specified range and moves trailing characters to the left.
        /// </summary>
        /// <param name="start">start columns index</param>
        /// <param name="count">count of columns to delete</param>
        /// <param name="end">end position for left shift of characters (column index, exclusive)</param>
        /// <param name="dec">text decoration of the blanks</param>
        public void DeleteChars(int start, int count, int? end, TextDecoration dec) {
            if (count <= 0) {
                return;
            }

            start = Math.Max(start, 0);

            int actualEnd;
            if (end.HasValue) {
                actualEnd = end.Value;
                if (start >= actualEnd) {
                    return;
                }
                // expand as needed to avoid errors in unexpected status.
                ExpandBuffer(actualEnd);
            }
            else {
                actualEnd = _cell.Length;
            }

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            int dstIndex = start;
            int srcIndex = dstIndex + count;
            while (dstIndex < actualEnd && srcIndex < actualEnd) {
                _cell[dstIndex] = _cell[srcIndex];
                _color24[dstIndex] = _color24[srcIndex];
                _dirty[dstIndex] = 1;
                dstIndex++;
                srcIndex++;
            }

            while (dstIndex < actualEnd) {
                // Note: uses ASCII_NUL instead of ASCII_SPACE for detecting correct length of the content
                _cell[dstIndex].Set(GChar.ASCII_NUL, fillAttr);
                _color24[dstIndex] = fillColor;
                _dirty[dstIndex] = 1;
                dstIndex++;
            }

            FixLeftHalfOfWideWidthCharacter(start - 1);
            FixRightHalfOfWideWidthCharacter(start);
            FixLeftHalfOfWideWidthCharacter(actualEnd - 1);
            FixRightHalfOfWideWidthCharacter(actualEnd);
        }

        /// <summary>
        /// Insert blank characters in the specified range and moves trailing characters to the right.
        /// </summary>
        /// <param name="start">start column index</param>
        /// <param name="count">count of columns to insert blanks</param>
        /// <param name="end">end position for right shift of characters (column index, exclusive)</param>
        /// <param name="dec">text decoration of the blanks</param>
        public void InsertBlanks(int start, int count, int end, TextDecoration dec) {
            if (count <= 0) {
                return;
            }

            start = Math.Max(start, 0);

            if (start >= end) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(end);

            count = Math.Min(count, end - start);

            GAttr fillAttr = dec.Attr;
            GColor24 fillColor = dec.Color24;

            int dstIndex = end - 1;
            int srcIndex = dstIndex - count;
            while (srcIndex >= start) {
                _cell[dstIndex] = _cell[srcIndex];
                _color24[dstIndex] = _color24[srcIndex];
                _dirty[dstIndex] = 1;
                dstIndex--;
                srcIndex--;
            }

            while (dstIndex >= start) {
                _cell[dstIndex].Set(GChar.ASCII_NUL, fillAttr);
                _color24[dstIndex] = fillColor;
                _dirty[dstIndex] = 1;
                dstIndex--;
            }

            FixLeftHalfOfWideWidthCharacter(start - 1);
            FixRightHalfOfWideWidthCharacter(start + count);
            FixLeftHalfOfWideWidthCharacter(end - 1);
            FixRightHalfOfWideWidthCharacter(end);
        }

        /// <summary>
        /// Copy from other manipurator.
        /// </summary>
        /// <param name="source">source manipurator</param>
        /// <param name="srcFrom">start index of the source range (inclusive)</param>
        /// <param name="srcTo">end index of the source range (exclusive)</param>
        /// <param name="destFrom">start index of the destination range (inclusive)</param>
        public void CopyFrom(GLineManipulator source, int srcFrom, int srcTo, int destFrom) {
            int copyLen = srcTo - srcFrom;
            if (copyLen <= 0) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(destFrom + copyLen);

            int? lastCopiedCol = null;
            for (int srcCol = srcFrom, destCol = destFrom; srcCol < srcTo; srcCol++, destCol++) {
                if (srcCol >= 0 && srcCol < source._cell.Length) {
                    this._cell[destCol] = source._cell[srcCol];
                    this._color24[destCol] = source._color24[srcCol];
                    this._dirty[destCol] = 1;
                }
                else {
                    this._cell[destCol].Set(GChar.ASCII_SPACE, GAttr.Default);
                    this._dirty[destCol] = 1;
                }

                if (!lastCopiedCol.HasValue) { // first character
                    FixLeftHalfOfWideWidthCharacter(destCol - 1);
                    FixRightHalfOfWideWidthCharacter(destCol);
                }

                lastCopiedCol = destCol;
            }

            if (lastCopiedCol.HasValue) {
                FixLeftHalfOfWideWidthCharacter(lastCopiedCol.Value);
                FixRightHalfOfWideWidthCharacter(lastCopiedCol.Value + 1);
            }
        }

        /// <summary>
        /// Modify attributes. (for DECCARA)
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        /// <param name="mod">modifications</param>
        public void ModifyAttributes(int from, int to, AttributeModifications mod) {
            from = Math.Max(from, 0);
            if (to <= from) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(to);

            for (int i = from; i < to; i++) {
                GAttr attr = _cell[i].Attr;

                if (mod.Bold.HasValue) {
                    if (mod.Bold.Value) {
                        attr += GAttrFlags.Bold;
                    }
                    else {
                        attr -= GAttrFlags.Bold;
                    }
                }

                if (mod.Underline.HasValue) {
                    if (mod.Underline.Value) {
                        attr += GAttrFlags.Underlined;
                    }
                    else {
                        attr -= GAttrFlags.Underlined;
                    }
                }

                if (mod.Blink.HasValue) {
                    if (mod.Blink.Value) {
                        attr += GAttrFlags.Blink;
                    }
                    else {
                        attr -= GAttrFlags.Blink;
                    }
                }

                if (mod.Inverted.HasValue) {
                    if (mod.Inverted.Value) {
                        attr += GAttrFlags.Inverted;
                    }
                    else {
                        attr -= GAttrFlags.Inverted;
                    }
                }

                _cell[i].Attr = attr;
                _dirty[i] = 1;
            }
        }

        /// <summary>
        /// Reverse attributes. (for DECRARA)
        /// </summary>
        /// <param name="from">start index of the range (inclusive)</param>
        /// <param name="to">end index of the range (exclusive)</param>
        /// <param name="mod">modifications</param>
        public void ReverseAttributes(int from, int to, AttributeModifications mod) {
            from = Math.Max(from, 0);
            if (to <= from) {
                return;
            }

            // expand as needed to avoid errors in unexpected status.
            ExpandBuffer(to);

            for (int i = from; i < to; i++) {
                GAttr attr = _cell[i].Attr;

                if (mod.Bold.HasValue && mod.Bold.Value) {
                    attr ^= GAttrFlags.Bold;
                }

                if (mod.Underline.HasValue && mod.Underline.Value) {
                    attr ^= GAttrFlags.Underlined;
                }

                if (mod.Blink.HasValue && mod.Blink.Value) {
                    attr ^= GAttrFlags.Blink;
                }

                if (mod.Inverted.HasValue && mod.Inverted.Value) {
                    attr ^= GAttrFlags.Inverted;
                }

                _cell[i].Attr = attr;
                _dirty[i] = 1;
            }
        }

        /// <summary>
        /// Clear dirty flags
        /// </summary>
        public void ClearDirtyFlags() {
            ClearDirtyFlagsInternal();
        }

        private void ClearDirtyFlagsInternal() {
            Array.Clear(_dirty, 0, _dirty.Length);
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
                            (GCell[])_cell.Clone(),
                            uses24bitColor ? (GColor24[])_color24.Clone() : null,
                            displayLength,
                            _eolType,
                            _lineRenderingType,
                            GetUpdatedSpans());
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

            line.UpdateContent(_cell, uses24bitColor ? _color24 : null, displayLength, _eolType, _lineRenderingType, GetUpdatedSpans(), true /*merge*/);
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
                tempUses24bitColor |= _cell[i].Attr.Uses24bitColor;
                if (!_cell[i].Attr.IsDefault || _cell[i].Char.CodePoint != 0u) {
                    lastCharIndex = i;
                }
            }

            // update "SameToPrevious" flags

            _cell[0].Attr -= GAttrFlags.SameToPrevious;

            for (int i = 1; i < _cell.Length; i++) {
                if (_cell[i - 1].Attr == _cell[i].Attr && (_color24 == null || _color24[i - 1] == _color24[i])) {
                    _cell[i].Attr += GAttrFlags.SameToPrevious;
                }
                else {
                    _cell[i].Attr -= GAttrFlags.SameToPrevious;
                }
            }

            uses24bitColor = tempUses24bitColor;
            displayLength = lastCharIndex + 1;
        }

        private List<GLineColumnSpan> GetUpdatedSpans() {
            // Optimization until the first increment of the Z-order.
            // Currently, GLineColumnSpans with the initial z-order are not used.
            if (_zMan.IsInitial) {
                return null;
            }

            GLineZOrder z = _zMan.Current;

            List<GLineColumnSpan> newList = null;

            int i = 0;
            while (i < _dirty.Length) {
                while (i < _dirty.Length && _dirty[i] == 0) {
                    i++;
                }

                if (i >= _dirty.Length) {
                    break;
                }

                int start = i;
                i++;

                while (i < _dirty.Length && _dirty[i] != 0) {
                    i++;
                }

                int end = i;
                i++;

                if (newList == null) {
                    newList = new List<GLineColumnSpan>(4);
                }
                newList.Add(new GLineColumnSpan(z, start, end));
            }

            if (newList != null) {
                ClearDirtyFlagsInternal();
            }

            return newList;
        }
    }

    /// <summary>
    /// Specifies attribute modification for DECCARA and DECRARA.
    /// </summary>
    public struct AttributeModifications {
        public bool? Bold {
            get;
            set;
        }

        public bool? Underline {
            get;
            set;
        }

        public bool? Blink {
            get;
            set;
        }

        public bool? Inverted {
            get;
            set;
        }

        public bool IsEmpty {
            get {
                return !Bold.HasValue && !Underline.HasValue && !Blink.HasValue && !Inverted.HasValue;
            }
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
    public enum EOLType : byte {
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

    [Flags]
    public enum LineRenderingType : byte {
        /// <summary>
        /// Normal (single width, single height)
        /// </summary>
        Normal = 0,
        /// <summary>
        /// Double width
        /// </summary>
        DoubleWidth = 1,
        /// <summary>
        /// Double height
        /// </summary>
        DoubleHeight = 2,
        /// <summary>
        /// Lower half of double height
        /// </summary>
        LowerHalf = 4,
        /// <summary>
        /// Upper half of quadrupted size
        /// </summary>
        QuadUpperHalf = DoubleWidth | DoubleHeight,
        /// <summary>
        /// Lower half of quadrupted size
        /// </summary>
        QuadLowerHalf = DoubleWidth | DoubleHeight | LowerHalf,
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
