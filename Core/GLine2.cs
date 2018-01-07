// Copyright 2017 The Poderosa Project.
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
using System.Collections.Generic;

using System.Threading;

namespace Poderosa.Document {

    internal interface IGCellArray {
        int Length {
            get;
        }

        IEnumerable<GCell> AsEnumerable();

        GCell[] ToArray();

        void Reset(IGCellArray cellArray);
    }

    internal class GCellArray : IGCellArray {

        private GCell[] _cells;

        public int Length {
            get {
                return _cells.Length;
            }
        }

        public GCellArray(int initialLength) {
            _cells = new GCell[initialLength];
        }

        public GCell At(int index) {
            return _cells[index];
        }

        public GAttr AttrAt(int index) {
            return _cells[index].Attr;
        }

        public GChar CharAt(int index) {
            return _cells[index].Char;
        }

        public void Set(int index, GChar ch, GAttr attr) {
            _cells[index].Set(ch, attr);
        }

        public void SetNul(int index) {
            _cells[index].SetNul();
        }

        public void Copy(int srcIndex, int dstIndex) {
            _cells[dstIndex] = _cells[srcIndex];
        }

        public void SetFlags(int index, GAttrFlags flags) {
            _cells[index].Attr += flags;
        }

        public void ClearFlags(int index, GAttrFlags flags) {
            _cells[index].Attr -= flags;
        }

        public void Clear(int newLength) {
            if (_cells.Length != newLength) {
                _cells = new GCell[newLength];
            }

            for (int i = 0; i < _cells.Length; i++) {
                _cells[i].Set(GChar.ASCII_NUL, GAttr.Default);
            }
        }

        public void Expand(int newLength) {
            if (newLength > _cells.Length) {
                GCell[] oldBuff = _cells;
                GCell[] newBuff = new GCell[newLength];
                oldBuff.CopyTo(newBuff);
                newBuff.Fill(oldBuff.Length, newBuff.Length, GChar.ASCII_NUL, GAttr.Default);
                _cells = newBuff;
            }
        }

        public IEnumerable<GCell> AsEnumerable() {
            return (IEnumerable<GCell>)_cells;
        }

        public GCell[] ToArray() {
            return (GCell[])_cells.Clone();
        }

        public void Reset(IGCellArray cellArray) {
            if (_cells.Length == cellArray.Length) {
                int i = 0;
                foreach (var cell in cellArray.AsEnumerable()) {
                    _cells[i++] = cell;
                }
                return;
            }

            _cells = cellArray.ToArray();
        }
    }

    internal class CompactGCellArray : IGCellArray {

        private enum GCharArrayType {
            HalfWidthSingleByteGCharArray,
            DoubleByteGCharArray,
            TripleByteGCharArray,
        }

        private interface IGCharArray {
            int Length {
                get;
            }

            GCharArrayType Type {
                get;
            }

            GChar CharAt(int index);

            uint CodePointAt(int index);

            void Set(int index, GChar ch);

            bool CanContain(GChar ch);

            void Expand(int newLength);

            IGCharArray Clone();
        }

        private class HalfWidthSingleByteGCharArray : IGCharArray {

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

        private class DoubleByteGCharArray : IGCharArray {
            // bit 0..13 : Unicode Code Point (U+0000 - U+3FFF)
            //
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

        private class TripleByteGCharArray : IGCharArray {
            // bit 0..20 : Unicode Code Point (U+0000 - U+1FFFFF)
            //
            // bit 22 : Right half of a wide-width character
            // bit 23 : wide width

            private int _length;

            private byte[] _data;

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
                _data = new byte[length * 3];  // no need to fill
            }

            public TripleByteGCharArray(IGCharArray source)
                : this(source.Length) {

                for (int i = 0; i < source.Length; i++) {
                    Set(i, source.CharAt(i));
                }
            }

            private TripleByteGCharArray(TripleByteGCharArray orig) {
                _length = orig._length;
                _data = (byte[])orig._data.Clone();
            }

            public GChar CharAt(int index) {
                int dataIndex = index * 3;
                uint d = ((uint)_data[dataIndex]) | ((uint)_data[dataIndex + 1] << 8) | ((uint)_data[dataIndex + 2] << 16);
                uint cp = d & CodePointMask;
                GCharFlags flags = (GCharFlags)((d & FlagsMask) << FlagsShift);
                return new GChar(cp, flags);
            }

            public uint CodePointAt(int index) {
                int dataIndex = index * 3;
                uint d = ((uint)_data[dataIndex]) | ((uint)_data[dataIndex + 1] << 8) | ((uint)_data[dataIndex + 2] << 16);
                uint cp = d & CodePointMask;
                return cp;
            }

            public void Set(int index, GChar ch) {
                uint d = (ch.CodePoint & CodePointMask) | ((uint)ch.Flags >> FlagsShift);
                int dataIndex = index * 3;
                _data[dataIndex] = (byte)d;
                _data[dataIndex + 1] = (byte)(d >> 8);
                _data[dataIndex + 2] = (byte)(d >> 16);
            }

            public bool CanContain(GChar ch) {
                return true;
            }

            public void Expand(int newLength) {
                byte[] oldBuff = _data;
                byte[] newBuff = new byte[newLength * 3];
                Buffer.BlockCopy(oldBuff, 0, newBuff, 0, oldBuff.Length);
                _data = newBuff;
                _length = newLength;
            }

            public IGCharArray Clone() {
                return new TripleByteGCharArray(this);
            }
        }

        private GAttr[] _attrs;
        private IGCharArray _chars;

        public int Length {
            get {
                return _attrs.Length;
            }
        }

        public CompactGCellArray(int initialLength) {
            _attrs = new GAttr[initialLength];
            _chars = new HalfWidthSingleByteGCharArray(initialLength);
        }

        public CompactGCellArray(IGCellArray source) {
            GCharArrayType type = DetermineSuitableGCharArrayType(source.AsEnumerable());
            _chars = CreateGCharArray(type, source.Length);

            _attrs = new GAttr[source.Length];

            int i = 0;
            foreach (var cell in source.AsEnumerable()) {
                _chars.Set(i, cell.Char);
                _attrs[i] = cell.Attr;
                i++;
            }
        }

        private CompactGCellArray(CompactGCellArray orig) {
            _attrs = (GAttr[])orig._attrs.Clone();
            _chars = orig._chars.Clone();
        }

        public GAttr AttrAt(int index) {
            return _attrs[index];
        }

        public GChar CharAt(int index) {
            return _chars.CharAt(index);
        }

        public uint CodePointAt(int index) {
            return _chars.CodePointAt(index);
        }

        public void Set(int index, GChar ch, GAttr attr) {
            if (!_chars.CanContain(ch)) {
                GCharArrayType newArrayType = DetermineSuitableGCharArrayType(ch, _chars.Type);
                _chars = CreateGCharArray(newArrayType, _chars);
            }
            _chars.Set(index, ch);
            _attrs[index] = attr;
        }

        public void SetNul(int index, GAttr attr) {
            _chars.Set(index, GChar.ASCII_NUL);
            _attrs[index] = attr;
        }

        public void SetFlags(int index, GAttrFlags flags) {
            _attrs[index] += flags;
        }

        public void ClearFlags(int index, GAttrFlags flags) {
            _attrs[index] -= flags;
        }

        public void Expand(int newLength, bool fillWithNul) {
            if (newLength > _attrs.Length) {
                int oldLength = _attrs.Length;
                _chars.Expand(newLength);
                GAttr[] oldAttrBuff = _attrs;
                GAttr[] newAttrBuff = new GAttr[newLength];
                oldAttrBuff.CopyTo(newAttrBuff, 0);
                _attrs = newAttrBuff;
                if (fillWithNul) {
                    for (int i = oldLength; i < newLength; i++) {
                        _chars.Set(i, GChar.ASCII_NUL);
                        _attrs[i] = GAttr.Default;
                    }
                }
            }
        }

        public IEnumerable<GCell> AsEnumerable() {
            for (int i = 0; i < _attrs.Length; i++) {
                yield return new GCell(_chars.CharAt(i), _attrs[i]);
            }
        }

        public GCell[] ToArray() {
            GCell[] cells = new GCell[_attrs.Length];
            for (int i = 0; i < _attrs.Length; i++) {
                cells[i] = new GCell(_chars.CharAt(i), _attrs[i]);
            }
            return cells;
        }

        public void Reset(IGCellArray cellArray) {
            GCharArrayType type = DetermineSuitableGCharArrayType(cellArray.AsEnumerable());
            if (_chars.Type != type || _chars.Length != cellArray.Length) {
                _chars = CreateGCharArray(type, cellArray.Length);
            }

            if (_attrs.Length != cellArray.Length) {
                _attrs = new GAttr[cellArray.Length];
            }

            int i = 0;
            foreach (var cell in cellArray.AsEnumerable()) {
                _chars.Set(i, cell.Char);
                _attrs[i] = cell.Attr;
                i++;
            }
        }

        public CompactGCellArray Clone() {
            return new CompactGCellArray(this);
        }

        private static GCharArrayType DetermineSuitableGCharArrayType(IEnumerable<GCell> source) {
            GCharArrayType type = GCharArrayType.HalfWidthSingleByteGCharArray;
            foreach (var cell in source) {
                type = DetermineSuitableGCharArrayType(cell.Char, type);
            }
            return type;
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
