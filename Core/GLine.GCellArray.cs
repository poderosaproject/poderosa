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

using Poderosa.Document.Internal.Mixins;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Poderosa.Document.Internal {

    /// <summary>
    /// Interface that provides contents of <see cref="GCell"/> array.
    /// </summary>
    internal interface IGCellArraySource {
        /// <summary>
        /// Length of GCell array.
        /// </summary>
        int Length {
            get;
        }

        /// <summary>
        /// Whether 24 bit colors are used.
        /// </summary>
        bool IsColor24Used {
            get;
        }

        /// <summary>
        /// Gets Enumerable of GCells.
        /// </summary>
        /// <returns>Enumerable of GCells</returns>
        IEnumerable<GCell> AsEnumerable();

        /// <summary>
        /// Creates a new array that contains same values with internal array.
        /// </summary>
        /// <returns>new array</returns>
        GCell[] ToArray();
    }

    /// <summary>
    /// Common interface of the wrapper class that represents <see cref="GCell"/> array.
    /// </summary>
    internal interface IGCellArray {
        /// <summary>
        /// Resets internal array so that it contains same values with the specified array.
        /// </summary>
        /// <param name="source">source GCell array</param>
        void Reset(IGCellArraySource source);
    }

    /// <summary>
    /// Simple <see cref="GCell"/> array.
    /// </summary>
    internal class SimpleGCellArray : IGCellArray, IGCellArraySource {

        /// <summary>
        /// Internal array
        /// </summary>
        private GCell[] _cells;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialLength">initial array length</param>
        public SimpleGCellArray(int initialLength) {
            _cells = new GCell[initialLength];
        }

        /// <summary>
        /// Gets <see cref="GAttr"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <remarks>
        /// This method provides optimal access to At(index).Attr.
        /// </remarks>
        /// <param name="index">index of GCell array</param>
        /// <returns>GAttr value</returns>
        public GAttr AttrAt(int index) {
            return _cells[index].Attr;
        }

        /// <summary>
        /// Gets <see cref="GChar"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <remarks>
        /// This method provides optimal access to At(index).Char.
        /// </remarks>
        /// <param name="index">index of GCell array</param>
        /// <returns>GChar value</returns>
        public GChar CharAt(int index) {
            return _cells[index].Char;
        }

        /// <summary>
        /// Gets <see cref="GColor24"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <returns>GColor24 value</returns>
        public GColor24 Color24At(int index) {
            return _cells[index].Color24;
        }

        /// <summary>
        /// Sets <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <remarks>
        /// We use <c>Set(int, GChar, GAttr)</c> rather than <c>Set(int, GCell)</c>
        /// because the first one will be optimized more efficiently by JIT.
        /// </remarks>
        /// <param name="index">index of GCell array</param>
        /// <param name="ch">character data of GCell</param>
        /// <param name="attr">attribute data of GCell</param>
        /// <param name="color24">24 bit color data of GCell</param>
        public void Set(int index, GChar ch, GAttr attr, GColor24 color24) {
            _cells[index].Set(ch, attr, color24);
        }

        /// <summary>
        /// Sets ASCII_NULL at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        public void SetNul(int index) {
            _cells[index].SetNul();
        }

        /// <summary>
        /// Copy GCell in the internal array.
        /// </summary>
        /// <param name="srcIndex">source index</param>
        /// <param name="dstIndex">destination index</param>
        public void Copy(int srcIndex, int dstIndex) {
            _cells[dstIndex] = _cells[srcIndex];
        }

        /// <summary>
        /// Sets attribute flags at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <param name="flags">flags to set</param>
        public void SetFlags(int index, GAttrFlags flags) {
            _cells[index].Attr += flags;
        }

        /// <summary>
        /// Clear attribute flags at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <param name="flags">flags to clear</param>
        public void ClearFlags(int index, GAttrFlags flags) {
            _cells[index].Attr -= flags;
        }

        /// <summary>
        /// Re-initialize internal array.
        /// </summary>
        /// <param name="newLength">new length of GCell array</param>
        public void Clear(int newLength) {
            if (_cells.Length != newLength) {
                _cells = new GCell[newLength];
            }

            GCell fill = new GCell(GChar.ASCII_NUL, GAttr.Default, new GColor24());
            for (int i = 0; i < _cells.Length; i++) {
                _cells[i] = fill;
            }
        }

        /// <summary>
        /// Expands array.
        /// </summary>
        /// <param name="newLength">new length of GCell array</param>
        public void Expand(int newLength) {
            if (newLength > _cells.Length) {
                int oldLength = _cells.Length;
                GCell[] oldBuff = _cells;
                GCell[] newBuff = new GCell[newLength];
                oldBuff.CopyTo(newBuff);
                GCell fill = new GCell(GChar.ASCII_NUL, GAttr.Default, new GColor24());
                for (int i = oldLength; i < newLength; i++) {
                    newBuff[i] = fill;
                }
                _cells = newBuff;
            }
        }

        #region IGCellArraySource

        /// <summary>
        /// Length of GCell array.
        /// </summary>
        public int Length {
            get {
                return _cells.Length;
            }
        }

        /// <summary>
        /// Whether 24 bit colors are used.
        /// </summary>
        public bool IsColor24Used {
            get;
            set;
        }

        /// <summary>
        /// Gets Enumerable of GCells.
        /// </summary>
        /// <returns>Enumerable of GCells</returns>
        public IEnumerable<GCell> AsEnumerable() {
            return (IEnumerable<GCell>)_cells;
        }

        /// <summary>
        /// Creates a new array that contains same values with internal array.
        /// </summary>
        /// <returns>new array</returns>
        public GCell[] ToArray() {
            return (GCell[])_cells.Clone();
        }

        #endregion

        #region IGCellArray

        /// <summary>
        /// Resets internal array so that it contains same values with the specified array.
        /// </summary>
        /// <param name="source">source GCell array</param>
        public void Reset(IGCellArraySource source) {
            if (_cells.Length == source.Length) {
                int i = 0;
                foreach (var cell in source.AsEnumerable()) {
                    _cells[i++] = cell;
                }
                return;
            }

            _cells = source.ToArray();
        }

        #endregion
    }

    /// <summary>
    /// Compact <see cref="GCell"/> array.
    /// <para>
    /// This class retains <see cref="GAttr"/> array and <see cref="GChar"/> array sepalately.
    /// <see cref="GChar"/> array is retained as the compact form.
    /// </para>
    /// </summary>
    internal class CompactGCellArray : IGCellArray, IGCellArraySource {

        #region GChar arrays

        /// <summary>
        /// <see cref="GChar"/> array type
        /// </summary>
        private enum GCharArrayType {
            /// <summary>Uses <see cref="HalfWidthSingleByteGCharArray"/></summary>
            HalfWidthSingleByteGCharArray,
            /// <summary>Uses <see cref="DoubleByteGCharArray"/></summary>
            DoubleByteGCharArray,
            /// <summary>Uses <see cref="TripleByteGCharArray"/></summary>
            TripleByteGCharArray,
        }

        /// <summary>
        /// Common interface of <see cref="GChar"/> array class.
        /// </summary>
        private interface IGCharArray {
            /// <summary>
            /// Array length
            /// </summary>
            int Length {
                get;
            }

            /// <summary>
            /// <see cref="GChar"/> array type
            /// </summary>
            GCharArrayType Type {
                get;
            }

            /// <summary>
            /// Gets <see cref="GChar"/> at the specified index.
            /// </summary>
            /// <param name="index">index of array</param>
            /// <returns><see cref="GChar"/> value</returns>
            GChar CharAt(int index);

            /// <summary>
            /// Gets Unicode code point of <see cref="GChar"/> at the specified index.
            /// </summary>
            /// <remarks>
            /// This method provides optimal access to CharAt(index).CodePoint.
            /// </remarks>
            /// <param name="index">index of array</param>
            /// <returns>Unicode code point</returns>
            uint CodePointAt(int index);

            /// <summary>
            /// Sets <see cref="GChar"/> at the specified index.
            /// </summary>
            /// <param name="index">index of array</param>
            /// <param name="ch">value to set</param>
            void Set(int index, GChar ch);

            /// <summary>
            /// Checks whether the <see cref="GChar"/> array implementation can contains the specified <see cref="GChar"/>.
            /// </summary>
            /// <param name="ch">value</param>
            /// <returns>true if the <see cref="GChar"/> array implementation can contains the specified <see cref="GChar"/>.</returns>
            bool CanContain(GChar ch);

            /// <summary>
            /// Expands array.
            /// </summary>
            /// <param name="newLength">new length</param>
            void Expand(int newLength);

            /// <summary>
            /// Create a cloned <see cref="GChar"/> array.
            /// </summary>
            /// <returns></returns>
            IGCharArray Clone();
        }

        /// <summary>
        /// <see cref="IGCharArray"/> implementation that retains only half-width characters in U+0000..U+00FF.
        /// <para>This implementation uses 1 byte per character.</para>
        /// </summary>
        private class HalfWidthSingleByteGCharArray : IGCharArray {
            // bit 0..7 : Unicode Code Point (U+0000 - U+00FF)
            // all characters are half-width character implicitly.

            private byte[] _chars;

            private const uint MaxCodePoint = 0xffu;

            public int Length {
                get {
                    return _chars.Length;
                }
            }

            public GCharArrayType Type {
                get {
                    return GCharArrayType.HalfWidthSingleByteGCharArray;
                }
            }

            public HalfWidthSingleByteGCharArray(int length) {
                _chars = new byte[length];  // no need to fill
            }

            public HalfWidthSingleByteGCharArray(IGCharArray source)
                : this(source.Length) {

                for (int i = 0; i < source.Length; i++) {
                    Set(i, source.CharAt(i));
                }
            }

            // constructor for Clone()
            private HalfWidthSingleByteGCharArray(HalfWidthSingleByteGCharArray orig) {
                _chars = (byte[])orig._chars.Clone();
            }

            public GChar CharAt(int index) {
                uint cp = _chars[index];
                return new GChar(cp, GCharFlags.None);
            }

            public uint CodePointAt(int index) {
                uint cp = _chars[index];
                return cp;
            }

            public void Set(int index, GChar ch) {
                _chars[index] = (byte)ch.CodePoint;
            }

            public bool CanContain(GChar ch) {
                return IsSuitableFor(ch);
            }

            public static bool IsSuitableFor(GChar ch) {
                return ch.CodePoint <= MaxCodePoint && !ch.IsWideWidth;
            }

            public void Expand(int newLength) {
                byte[] oldBuff = _chars;
                byte[] newBuff = new byte[newLength];
                Buffer.BlockCopy(oldBuff, 0, newBuff, 0, oldBuff.Length);
                _chars = newBuff;
            }

            public IGCharArray Clone() {
                return new HalfWidthSingleByteGCharArray(this);
            }
        }

        /// <summary>
        /// <see cref="IGCharArray"/> implementation that retains only characters in U+0000..U+3FFF.
        /// <para>This implementation uses 2 bytes per character.</para>
        /// </summary>
        private class DoubleByteGCharArray : IGCharArray {
            // bit 0..13 : Unicode Code Point (U+0000 - U+3FFF)
            // bit 14 : Right half of a wide-width character
            // bit 15 : wide width

            private ushort[] _chars;

            private const uint MaxCodePoint = 0x3fffu;
            private const uint CodePointMask = 0x3fffu;
            private const uint FlagsMask = 0xc000u;
            private const int FlagsShift = 16;

            public int Length {
                get {
                    return _chars.Length;
                }
            }

            public GCharArrayType Type {
                get {
                    return GCharArrayType.DoubleByteGCharArray;
                }
            }

            public DoubleByteGCharArray(int length) {
                _chars = new ushort[length];  // no need to fill
            }

            public DoubleByteGCharArray(IGCharArray source)
                : this(source.Length) {

                for (int i = 0; i < source.Length; i++) {
                    Set(i, source.CharAt(i));
                }
            }

            // constructor for Clone()
            private DoubleByteGCharArray(DoubleByteGCharArray orig) {
                _chars = (ushort[])orig._chars.Clone();
            }

            public GChar CharAt(int index) {
                uint d = _chars[index];
                uint cp = d & CodePointMask;
                GCharFlags flags = (GCharFlags)((d & FlagsMask) << FlagsShift);
                return new GChar(cp, flags);
            }

            public uint CodePointAt(int index) {
                uint d = _chars[index];
                uint cp = d & CodePointMask;
                return cp;
            }

            public void Set(int index, GChar ch) {
                ushort d = (ushort)((ch.CodePoint & CodePointMask) | ((uint)ch.Flags >> FlagsShift));
                _chars[index] = d;
            }

            public bool CanContain(GChar ch) {
                return IsSuitableFor(ch);
            }

            public static bool IsSuitableFor(GChar ch) {
                return ch.CodePoint <= MaxCodePoint;
            }

            public void Expand(int newLength) {
                ushort[] oldBuff = _chars;
                ushort[] newBuff = new ushort[newLength];
                Buffer.BlockCopy(oldBuff, 0, newBuff, 0, oldBuff.Length * sizeof(ushort));
                _chars = newBuff;
            }

            public IGCharArray Clone() {
                return new DoubleByteGCharArray(this);
            }
        }

        /// <summary>
        /// <see cref="IGCharArray"/> implementation that retains any characters.
        /// <para>This implementation uses 3 bytes per character.</para>
        /// </summary>
        private class TripleByteGCharArray : IGCharArray {
            // bit 0..20 : Unicode Code Point (U+0000 - U+1FFFFF)
            //
            // bit 22 : Right half of a wide-width character
            // bit 23 : wide width

            [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 3)]
            private struct UInt24 {
                private readonly byte _b1;
                private readonly byte _b2;
                private readonly byte _b3;

                public UInt24(uint val) {
                    _b1 = (byte)val;
                    _b2 = (byte)(val >> 8);
                    _b3 = (byte)(val >> 16);
                }

                public uint Value {
                    get {
                        return (uint)(_b1 | (_b2 << 8) | (_b3 << 16));
                    }
                }
            }

            private int _length;
            private UInt24[] _chars;

            private const uint CodePointMask = 0x1fffffu;
            private const uint FlagsMask = 0xc00000u;
            private const int FlagsShift = 8;

            public int Length {
                get {
                    return _length;
                }
            }

            public GCharArrayType Type {
                get {
                    return GCharArrayType.TripleByteGCharArray;
                }
            }

            public TripleByteGCharArray(int length) {
                _length = length;
                _chars = new UInt24[length];  // no need to fill
            }

            public TripleByteGCharArray(IGCharArray source)
                : this(source.Length) {

                for (int i = 0; i < source.Length; i++) {
                    Set(i, source.CharAt(i));
                }
            }

            // constructor for Clone()
            private TripleByteGCharArray(TripleByteGCharArray orig) {
                _length = orig._length;
                _chars = (UInt24[])orig._chars.Clone();
            }

            public GChar CharAt(int index) {
                uint d = _chars[index].Value;
                uint cp = d & CodePointMask;
                GCharFlags flags = (GCharFlags)((d & FlagsMask) << FlagsShift);
                return new GChar(cp, flags);
            }

            public uint CodePointAt(int index) {
                int dataIndex = index * 3;
                uint d = _chars[index].Value;
                uint cp = d & CodePointMask;
                return cp;
            }

            public void Set(int index, GChar ch) {
                uint d = (ch.CodePoint & CodePointMask) | ((uint)ch.Flags >> FlagsShift);
                _chars[index] = new UInt24(d);
            }

            public bool CanContain(GChar ch) {
                return true;
            }

            public void Expand(int newLength) {
                UInt24[] oldBuff = _chars;
                UInt24[] newBuff = new UInt24[newLength];
                oldBuff.CopyTo(newBuff);
                _chars = newBuff;
                _length = newLength;
            }

            public IGCharArray Clone() {
                return new TripleByteGCharArray(this);
            }
        }

        #endregion

        /// <summary>
        /// Internal array
        /// </summary>
        private GAttr[] _attrs;

        /// <summary>
        /// Internal array
        /// </summary>
        private IGCharArray _chars;

        /// <summary>
        /// Internal array
        /// </summary>
        private GColor24[] _color24s; // this may be null

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="initialLength">initial array length</param>
        /// <param name="useColor24">true if 24 bit colors are used</param>
        public CompactGCellArray(int initialLength, bool useColor24) {
            _attrs = new GAttr[initialLength];
            _chars = new HalfWidthSingleByteGCharArray(initialLength);
            _color24s = useColor24 ? new GColor24[initialLength] : null;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="source">source that contens are copied from</param>
        public CompactGCellArray(IGCellArraySource source) {
            GCharArrayType type = DetermineSuitableGCharArrayType(source.AsEnumerable());
            _chars = CreateGCharArray(type, source.Length);
            _attrs = new GAttr[source.Length];
            _color24s = source.IsColor24Used ? new GColor24[source.Length] : null;

            int i = 0;
            foreach (var cell in source.AsEnumerable()) {
                _chars.Set(i, cell.Char);
                _attrs[i] = cell.Attr;
                if (_color24s != null) {
                    _color24s[i] = cell.Color24;
                }
                i++;
            }
        }

        /// <summary>
        /// Constructor (for Clone())
        /// </summary>
        /// <param name="orig">original instance</param>
        private CompactGCellArray(CompactGCellArray orig) {
            _attrs = (GAttr[])orig._attrs.Clone();
            _chars = orig._chars.Clone();
            _color24s = (orig._color24s != null) ? (GColor24[])orig._color24s.Clone() : null;
        }

        /// <summary>
        /// Gets <see cref="GAttr"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <returns>GAttr value</returns>
        public GAttr AttrAt(int index) {
            return _attrs[index];
        }

        /// <summary>
        /// Gets <see cref="GChar"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <returns>GChar value</returns>
        public GChar CharAt(int index) {
            return _chars.CharAt(index);
        }

        /// <summary>
        /// Gets Unicode code point of <see cref="GChar"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <remarks>
        /// This method provides optimal access to CharAt(index).CodePoint.
        /// </remarks>
        /// <param name="index">index of GCell array</param>
        /// <returns>Unicode code point</returns>
        public uint CodePointAt(int index) {
            return _chars.CodePointAt(index);
        }

        /// <summary>
        /// Gets <see cref="GColor24"/> of <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <returns>GColor24 value</returns>
        public GColor24 Color24At(int index) {
            return (_color24s != null) ? _color24s[index] : new GColor24();
        }

        /// <summary>
        /// Sets <see cref="GCell"/> at the specified index.
        /// </summary>
        /// <remarks>
        /// We use <c>Set(int, GChar, GAttr)</c> rather than <c>Set(int, GCell)</c>
        /// because the first one will be optimized more efficiently by JIT.
        /// </remarks>
        /// <param name="index">index of GCell array</param>
        /// <param name="ch">character data of GCell</param>
        /// <param name="attr">attribute data of GCell</param>
        /// <param name="color24">24 bit color data of GCell (used if attr.Uses24bitColor was true)</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void Set(int index, GChar ch, GAttr attr, GColor24 color24) {
            if (!_chars.CanContain(ch)) {
                // convert to the suitable GChar array.
                GCharArrayType newArrayType = DetermineSuitableGCharArrayType(ch, _chars.Type);
                _chars = CreateGCharArray(newArrayType, _chars);
            }
            _chars.Set(index, ch);
            _attrs[index] = attr;

            SetColor24(attr.Uses24bitColor, index, color24);
        }

        private void SetColor24(bool useColor24, int index, GColor24 color24) {
            if (useColor24) {
                if (_color24s == null) {
                    _color24s = new GColor24[_attrs.Length];
                }
                _color24s[index] = color24;
            }
            else {
                if (_color24s != null) {
                    _color24s[index] = new GColor24();
                }
            }
        }

        /// <summary>
        /// Sets ASCII_NULL at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <param name="attr">attribute to set</param>
        /// <param name="color24">24 bit color data (used if attr.Uses24bitColor was true)</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public void SetNul(int index, GAttr attr, GColor24 color24) {
            _chars.Set(index, GChar.ASCII_NUL);
            _attrs[index] = attr - GAttrFlags.UseCjkFont;

            SetColor24(attr.Uses24bitColor, index, color24);
        }

        /// <summary>
        /// Sets attribute flags at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <param name="flags">flags to set</param>
        public void SetFlags(int index, GAttrFlags flags) {
            _attrs[index] += flags;
        }

        /// <summary>
        /// Clear attribute flags at the specified index.
        /// </summary>
        /// <param name="index">index of GCell array</param>
        /// <param name="flags">flags to clear</param>
        public void ClearFlags(int index, GAttrFlags flags) {
            _attrs[index] -= flags;
        }

        /// <summary>
        /// Expands array.
        /// </summary>
        /// <param name="newLength">new length of GCell array</param>
        /// <param name="fillWithNul">if true, expanded range is filled with ASCII_NUL and the default attribute</param>
        public void Expand(int newLength, bool fillWithNul) {
            if (newLength > _attrs.Length) {
                int oldLength = _attrs.Length;

                _chars.Expand(newLength);

                GAttr[] oldAttrBuff = _attrs;
                GAttr[] newAttrBuff = new GAttr[newLength];
                oldAttrBuff.CopyTo(newAttrBuff);
                _attrs = newAttrBuff;

                if (fillWithNul) {
                    for (int i = oldLength; i < newLength; i++) {
                        _chars.Set(i, GChar.ASCII_NUL);
                        _attrs[i] = GAttr.Default;
                    }
                }

                if (_color24s != null) {
                    GColor24[] oldColorBuff = _color24s;
                    GColor24[] newColorBuff = new GColor24[newLength];
                    oldColorBuff.CopyTo(newColorBuff);
                    _color24s = newColorBuff;
                }
            }
        }

        /// <summary>
        /// Creates a cloned instance.
        /// </summary>
        /// <returns></returns>
        public CompactGCellArray Clone() {
            return new CompactGCellArray(this);
        }

        #region IGCellArraySource

        /// <summary>
        /// Length of GCell array.
        /// </summary>
        public int Length {
            get {
                return _attrs.Length;
            }
        }

        /// <summary>
        /// Whether 24 bit colors are used.
        /// </summary>
        public bool IsColor24Used {
            get {
                return _color24s != null;
            }
        }

        /// <summary>
        /// Gets Enumerable of GCells.
        /// </summary>
        /// <returns>Enumerable of GCells</returns>
        public IEnumerable<GCell> AsEnumerable() {
            return (_color24s != null) ? AsEnumerableWithColor24() : AsEnumerableWithoutColor24();
        }

        private IEnumerable<GCell> AsEnumerableWithColor24() {
            for (int i = 0; i < _attrs.Length; i++) {
                yield return new GCell(_chars.CharAt(i), _attrs[i], _color24s[i]);
            }
        }

        private IEnumerable<GCell> AsEnumerableWithoutColor24() {
            for (int i = 0; i < _attrs.Length; i++) {
                yield return new GCell(_chars.CharAt(i), _attrs[i], new GColor24());
            }
        }

        /// <summary>
        /// Creates a new array that contains same values with internal array.
        /// </summary>
        /// <returns>new array</returns>
        public GCell[] ToArray() {
            GCell[] cells = new GCell[_attrs.Length];

            if (_color24s != null) {
                for (int i = 0; i < _attrs.Length; i++) {
                    cells[i].Set(_chars.CharAt(i), _attrs[i], _color24s[i]);
                }
            }
            else {
                for (int i = 0; i < _attrs.Length; i++) {
                    cells[i].Set(_chars.CharAt(i), _attrs[i], new GColor24());
                }
            }
            return cells;
        }

        #endregion

        #region IGCellArray

        /// <summary>
        /// Resets internal array so that it contains same values with the specified array.
        /// </summary>
        /// <param name="source">source GCell array</param>
        public void Reset(IGCellArraySource source) {
            GCharArrayType type = DetermineSuitableGCharArrayType(source.AsEnumerable());
            if (_chars.Type != type || _chars.Length != source.Length) {
                _chars = CreateGCharArray(type, source.Length);
            }

            if (_attrs.Length != source.Length) {
                _attrs = new GAttr[source.Length];
            }

            if (source.IsColor24Used) {
                if (_color24s == null || _color24s.Length != source.Length) {
                    _color24s = new GColor24[source.Length];
                }
            }
            else {
                _color24s = null;
            }

            int i = 0;
            foreach (var cell in source.AsEnumerable()) {
                _chars.Set(i, cell.Char);
                _attrs[i] = cell.Attr;
                if (_color24s != null) {
                    _color24s[i] = cell.Color24;
                }
                i++;
            }
        }

        #endregion

        private static GCharArrayType DetermineSuitableGCharArrayType(IEnumerable<GCell> source) {
            var cellIter = source.GetEnumerator();
            if (!cellIter.MoveNext()) {
                return GCharArrayType.HalfWidthSingleByteGCharArray;
            }
            var ch = cellIter.Current.Char;

            while (HalfWidthSingleByteGCharArray.IsSuitableFor(ch)) {
                if (!cellIter.MoveNext()) {
                    return GCharArrayType.HalfWidthSingleByteGCharArray;
                }
                ch = cellIter.Current.Char;
            }

            while (DoubleByteGCharArray.IsSuitableFor(ch)) {
                if (!cellIter.MoveNext()) {
                    return GCharArrayType.DoubleByteGCharArray;
                }
                ch = cellIter.Current.Char;
            }

            return GCharArrayType.TripleByteGCharArray;
        }

        private static GCharArrayType DetermineSuitableGCharArrayType(GChar ch, GCharArrayType oldType) {
            switch (oldType) {
                case GCharArrayType.HalfWidthSingleByteGCharArray:
                    if (HalfWidthSingleByteGCharArray.IsSuitableFor(ch)) {
                        return GCharArrayType.HalfWidthSingleByteGCharArray;
                    }
                    goto case GCharArrayType.DoubleByteGCharArray;

                case GCharArrayType.DoubleByteGCharArray:
                    if (DoubleByteGCharArray.IsSuitableFor(ch)) {
                        return GCharArrayType.DoubleByteGCharArray;
                    }
                    goto case GCharArrayType.TripleByteGCharArray;

                case GCharArrayType.TripleByteGCharArray:
                    return GCharArrayType.TripleByteGCharArray;

                default:
                    return oldType;
            }
        }

        private static IGCharArray CreateGCharArray(GCharArrayType type, int length) {
            switch (type) {
                case GCharArrayType.HalfWidthSingleByteGCharArray:
                    return new HalfWidthSingleByteGCharArray(length);

                case GCharArrayType.DoubleByteGCharArray:
                    return new DoubleByteGCharArray(length);

                case GCharArrayType.TripleByteGCharArray:
                default:
                    return new TripleByteGCharArray(length);
            }
        }

        private static IGCharArray CreateGCharArray(GCharArrayType type, IGCharArray source) {
            switch (type) {
                case GCharArrayType.HalfWidthSingleByteGCharArray:
                    return new HalfWidthSingleByteGCharArray(source);

                case GCharArrayType.DoubleByteGCharArray:
                    return new DoubleByteGCharArray(source);

                case GCharArrayType.TripleByteGCharArray:
                default:
                    return new TripleByteGCharArray(source);
            }
        }
    }

}
