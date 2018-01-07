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

        private GCell[] _cells;

        public int Length {
            get {
                return _cells.Length;
            }
        }

        public CompactGCellArray(int initialLength) {
            _cells = new GCell[initialLength];
        }

        public CompactGCellArray(IGCellArray source) {
            _cells = new GCell[source.Length];
            int i = 0;
            foreach (var cell in source.AsEnumerable()) {
                _cells[i++] = cell;
            }
        }

        private CompactGCellArray(GCell[] cells) {
            _cells = cells;
        }

        public GAttr AttrAt(int index) {
            return _cells[index].Attr;
        }

        public GChar CharAt(int index) {
            return _cells[index].Char;
        }

        public uint CodePointAt(int index) {
            return _cells[index].Char.CodePoint;
        }

        public void Set(int index, GChar ch, GAttr attr) {
            _cells[index].Set(ch, attr);
        }

        public void Set(int index, GCell cell) {
            _cells[index] = cell;
        }

        public void SetFlags(int index, GAttrFlags flags) {
            _cells[index].Attr += flags;
        }

        public void ClearFlags(int index, GAttrFlags flags) {
            _cells[index].Attr -= flags;
        }

        public void Expand(int newLength) {
            if (newLength > _cells.Length) {
                GCell[] oldBuff = _cells;
                GCell[] newBuff = new GCell[newLength];
                oldBuff.CopyTo(newBuff);
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

        public CompactGCellArray Clone() {
            return new CompactGCellArray((GCell[])_cells.Clone());
        }

    }



}
