// Copyright 2019 The Poderosa Project.
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
using System.Runtime.CompilerServices;

namespace Poderosa.Document {

    /// <summary>
    /// GLine screen buffer.
    /// </summary>
    /// <remarks>
    /// This buffer is designed for containing visible lines on the screen.
    /// </remarks>
    public class GLineScreenBuffer {

        // Internal buffer is a circular buffer.

        private GLine[] _buff;
        private int _startIndex;
        private int _size;

        private readonly object _syncRoot = new object();

        /// <summary>
        /// Synchronization object
        /// </summary>
        public object SyncRoot {
            get {
                return _syncRoot;
            }
        }

        /// <summary>
        /// Screen size in the number of rows.
        /// </summary>
        public int Size {
            get {
                return _size;
            }
        }

        /// <summary>
        /// <para>
        /// Gets or sets a <see cref="GLine"/> object at the specified row.
        /// </para>
        /// <para>
        /// Note that each access to this property does internal synchronization.
        /// Consider to use <see cref="GetRows(int, GLineChunkSpan)"/> or <see cref="SetRows(int, GLineChunkSpan)"/>.
        /// </para>
        /// </summary>
        /// <param name="rowIndex">row index of the screen</param>
        public GLine this[int rowIndex] {
            get {
                lock (_syncRoot) {
                    if (rowIndex < 0 || rowIndex >= _size) {
                        throw new IndexOutOfRangeException("invalid index");
                    }

                    return _buff[RowIndexToBuffIndex(rowIndex)];
                }
            }
            set {
                lock (_syncRoot) {
                    if (rowIndex < 0 || rowIndex >= _size) {
                        throw new IndexOutOfRangeException("invalid index");
                    }

                    _buff[RowIndexToBuffIndex(rowIndex)] = value;
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rows">number of visible rows</param>
        /// <param name="createLine">a function to get the initial GLine objects</param>
        public GLineScreenBuffer(int rows, Func<int, GLine> createLine) {
            if (rows <= 0) {
                throw new ArgumentException("invalid value", "rows");
            }

            _buff = new GLine[CalcBufferSize(rows)];
            _startIndex = 0;
            _size = rows;

            for (int i = 0; i < rows; i++) {
                _buff[i] = createLine(i);
            }
        }

        /// <summary>
        /// Constructor (for testing)
        /// </summary>
        /// <param name="startIndex">index of the internal buffer</param>
        /// <param name="rows">number of visible rows</param>
        /// <param name="createLine">a function to get the initial GLine objects</param>
        internal GLineScreenBuffer(int startIndex, int rows, Func<int, GLine> createLine) {
            if (rows <= 0) {
                throw new ArgumentException("invalid value", "rows");
            }

            _buff = new GLine[CalcBufferSize(rows)];
            _startIndex = startIndex;
            _size = rows;

            for (int i = 0; i < rows; i++) {
                _buff[RowIndexToBuffIndex(i)] = createLine(i);
            }
        }

        /// <summary>
        /// Determine new buffer size
        /// </summary>
        /// <param name="rows">number of visible rows</param>
        /// <returns>buffer size</returns>
        private static int CalcBufferSize(int rows) {
            // round-up to power of 2
            int bits = rows - 1;
            bits |= bits >> 1;
            bits |= bits >> 2;
            bits |= bits >> 4;
            bits |= bits >> 8;
            bits |= bits >> 16;
            bits |= 63; // allocate 64 rows at least
            return bits + 1;
        }

        /// <summary>
        /// Gets index of the internal buffer from the row index.
        /// </summary>
        /// <param name="rowIndex">row index</param>
        /// <returns>index of the internal buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int RowIndexToBuffIndex(int rowIndex) {
            return (_startIndex + rowIndex) % _buff.Length;
        }

        /// <summary>
        /// Gets index of the internal buffer from the row index.
        /// </summary>
        /// <param name="offset">
        /// row index towards the negative direction.
        /// a value "1" indicatess the previous row of the top row of the screen.
        /// </param>
        /// <returns>index of the internal buffer</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int RowIndexToBuffIndexNegative(int offset) {
            int buffSize = _buff.Length;
            return (_startIndex + buffSize - offset) % buffSize;
        }

        /// <summary>
        /// Increases the buffer size with extending the top of the screen.
        /// </summary>
        /// <param name="rows">
        /// rows to insert at the top of the screen.
        /// the length of the span dictates the new screen size.
        /// </param>
        public void ExtendHead(GLineChunkSpan rows) {
            lock (_syncRoot) {
                int newSize = _size + rows.Length;
                int buffSize = CalcBufferSize(newSize);

                if (buffSize <= _buff.Length) {
                    // buffer size is already enough.
                    // insert rows.
                    int newStartIndex = RowIndexToBuffIndexNegative(rows.Length);
                    CopyToBuffer(rows.Array, rows.Offset, newStartIndex, rows.Length);
                    _startIndex = newStartIndex;
                    _size = newSize;
                }
                else {
                    // buffer size is not enough.
                    // allocate new buffer.
                    GLine[] newBuff = new GLine[buffSize];
                    // copy rows.
                    Array.Copy(rows.Array, rows.Offset, newBuff, 0, rows.Length);
                    CopyFromBuffer(_startIndex, newBuff, rows.Length, _size);
                    _buff = newBuff;
                    _startIndex = 0;
                    _size = newSize;
                }
            }
        }

        /// <summary>
        /// Increases the buffer size with extending the bottom of the screen.
        /// </summary>
        /// <param name="rows">
        /// rows to insert at the top of the screen.
        /// the length of the span dictates the new screen size.
        /// </param>
        public void ExtendTail(GLineChunkSpan rows) {
            lock (_syncRoot) {
                int newSize = _size + rows.Length;
                int buffSize = CalcBufferSize(newSize);

                if (buffSize <= _buff.Length) {
                    // buffer size is already enough.
                    // insert rows.
                    CopyToBuffer(rows.Array, rows.Offset, RowIndexToBuffIndex(_size), rows.Length);
                    _size = newSize;
                }
                else {
                    // buffer size is not enough.
                    // allocate new buffer.
                    GLine[] newBuff = new GLine[buffSize];
                    // copy rows.
                    CopyFromBuffer(_startIndex, newBuff, 0, _size);
                    Array.Copy(rows.Array, rows.Offset, newBuff, _size, rows.Length);
                    _buff = newBuff;
                    _startIndex = 0;
                    _size = newSize;
                }
            }
        }

        /// <summary>
        /// Decreases the buffer size with shrinking the top of the screen.
        /// </summary>
        /// <param name="rows">number of rows to decrease</param>
        public void ShrinkHead(int rows) {
            if (rows < 0) {
                throw new ArgumentException("invalid value", "rows");
            }

            lock (_syncRoot) {
                if (rows >= _size) {
                    throw new ArgumentException("too large shrink size", "rows");
                }

                ClearBuffer(_startIndex, rows);
                _startIndex = RowIndexToBuffIndex(rows);
                _size -= rows;
            }
        }

        /// <summary>
        /// Decreases the buffer size with shrinking the bottom of the screen.
        /// </summary>
        /// <param name="rows">number of rows to decrease</param>
        public void ShrinkTail(int rows) {
            if (rows < 0) {
                throw new ArgumentException("invalid value", "rows");
            }

            lock (_syncRoot) {
                if (rows >= _size) {
                    throw new ArgumentException("too large shrink size", "rows");
                }

                _size -= rows;
                ClearBuffer(RowIndexToBuffIndex(_size), rows);
            }
        }

        /// <summary>
        /// Gets rows starting at the specified index.
        /// </summary>
        /// <param name="rowIndex">row index. 0 indicates the first row at the top of the screen.</param>
        /// <param name="span">
        /// array buffer to store the copied object.
        /// the length of the span is used as the number of rows to get.
        /// </param>
        public void GetRows(int rowIndex, GLineChunkSpan span) {
            if (rowIndex < 0) {
                throw new ArgumentException("invalid value", "rowIndex");
            }

            lock (_syncRoot) {
                if (rowIndex + span.Length > _size) {
                    throw new ArgumentException("invalid range");
                }

                CopyFromBuffer(RowIndexToBuffIndex(rowIndex), span.Array, span.Offset, span.Length);
            }
        }

        /// <summary>
        /// Sets rows starting at the specified index.
        /// </summary>
        /// <param name="rowIndex">row index. 0 indicates the first row at the top of the screen.</param>
        /// <param name="span">
        /// array buffer to get objects to be copied.
        /// the length of the span is used as the number of rows to set.
        /// </param>
        public void SetRows(int rowIndex, GLineChunkSpan span) {
            if (rowIndex < 0) {
                throw new ArgumentException("invalid value", "rowIndex");
            }

            lock (_syncRoot) {
                if (rowIndex + span.Length > _size) {
                    throw new ArgumentException("invalid range");
                }

                CopyToBuffer(span.Array, span.Offset, RowIndexToBuffIndex(rowIndex), span.Length);
            }
        }

        /// <summary>
        /// Scroll-up entire of the screen.
        /// </summary>
        /// <param name="newRows">
        /// rows to append at the bottom of the screen.
        /// the length of the span is used as the number of rows to scroll.
        /// </param>
        public void ScrollUp(GLineChunkSpan newRows) {
            lock (_syncRoot) {
                int oldEndIndex = RowIndexToBuffIndex(_size);
                // scroll-up
                int scrollSize = Math.Min(newRows.Length, _size);
                ClearBuffer(_startIndex, scrollSize);
                _startIndex = RowIndexToBuffIndex(scrollSize);
                // append new rows
                CopyToBuffer(newRows.Array, newRows.Offset + newRows.Length - scrollSize, oldEndIndex, scrollSize);
            }
        }

        /// <summary>
        /// Scroll-down entire of the screen.
        /// </summary>
        /// <param name="newRows">
        /// rows to insert at the top of the screen.
        /// the length of the span is used as the number of rows to scroll.
        /// </param>
        public void ScrollDown(GLineChunkSpan newRows) {
            lock (_syncRoot) {
                // scroll-down
                int scrollSize = Math.Min(newRows.Length, _size);
                _startIndex = RowIndexToBuffIndexNegative(scrollSize);
                ClearBuffer(RowIndexToBuffIndex(_size), scrollSize);
                // insert new rows
                CopyToBuffer(newRows.Array, newRows.Offset, _startIndex, scrollSize);
            }
        }

        /// <summary>
        /// Scroll-up rows in the specified region.
        /// </summary>
        /// <param name="startRowIndex">start row indedx of the scroll region (inclusive)</param>
        /// <param name="endRowIndex">end row indedx of the scroll region (exclusive)</param>
        /// <param name="newRows">
        /// rows to insert at the bottom of the region.
        /// the length of the span is used as the number of rows to scroll.
        /// </param>
        public void ScrollUpRegion(int startRowIndex, int endRowIndex, GLineChunkSpan newRows) {
            lock (_syncRoot) {
                // adjust range
                startRowIndex = Math.Max(startRowIndex, 0);
                endRowIndex = Math.Min(endRowIndex, _size);
                if (startRowIndex >= endRowIndex) {
                    return;
                }
                // scroll-up
                int scrollSize = Math.Min(newRows.Length, endRowIndex - startRowIndex);
                int destRowIndex = startRowIndex;
                int srcRowIndex = destRowIndex + scrollSize;
                while (srcRowIndex < endRowIndex) {
                    _buff[RowIndexToBuffIndex(destRowIndex)] = _buff[RowIndexToBuffIndex(srcRowIndex)];
                    destRowIndex++;
                    srcRowIndex++;
                }
                CopyToBuffer(newRows.Array, newRows.Offset + newRows.Length - scrollSize, RowIndexToBuffIndex(destRowIndex), scrollSize);
            }
        }

        /// <summary>
        /// Scroll-down rows in the specified region.
        /// </summary>
        /// <param name="startRowIndex">start row indedx of the scroll region (inclusive)</param>
        /// <param name="endRowIndex">end row indedx of the scroll region (exclusive)</param>
        /// <param name="newRows">
        /// rows to insert at the top of the region.
        /// the length of the span is used as the number of rows to scroll.
        /// </param>
        public void ScrollDownRegion(int startRowIndex, int endRowIndex, GLineChunkSpan newRows) {
            lock (_syncRoot) {
                // adjust range
                startRowIndex = Math.Max(startRowIndex, 0);
                endRowIndex = Math.Min(endRowIndex, _size);
                if (startRowIndex >= endRowIndex) {
                    return;
                }
                // scroll-down
                int scrollSize = Math.Min(newRows.Length, endRowIndex - startRowIndex);
                int destRowIndex = endRowIndex - 1;
                int srcRowIndex = destRowIndex - scrollSize;
                while (srcRowIndex >= startRowIndex) {
                    _buff[RowIndexToBuffIndex(destRowIndex)] = _buff[RowIndexToBuffIndex(srcRowIndex)];
                    destRowIndex--;
                    srcRowIndex--;
                }
                CopyToBuffer(newRows.Array, newRows.Offset, RowIndexToBuffIndex(startRowIndex), scrollSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyToBuffer(GLine[] src, int srcIndex, int buffIndex, int length) {
            int sizeToEnd = _buff.Length - buffIndex;
            if (sizeToEnd >= length) {
                Array.Copy(src, srcIndex, _buff, buffIndex, length);
            }
            else {
                Array.Copy(src, srcIndex, _buff, buffIndex, sizeToEnd);
                Array.Copy(src, srcIndex + sizeToEnd, _buff, 0, length - sizeToEnd);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CopyFromBuffer(int buffIndex, GLine[] dest, int destIndex, int length) {
            int sizeToEnd = _buff.Length - buffIndex;
            if (sizeToEnd >= length) {
                Array.Copy(_buff, buffIndex, dest, destIndex, length);
            }
            else {
                Array.Copy(_buff, buffIndex, dest, destIndex, sizeToEnd);
                Array.Copy(_buff, 0, dest, destIndex + sizeToEnd, length - sizeToEnd);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBuffer(int buffIndex, int length) {
            int sizeToEnd = _buff.Length - buffIndex;
            if (sizeToEnd >= length) {
                Array.Clear(_buff, buffIndex, length);
            }
            else {
                Array.Clear(_buff, buffIndex, sizeToEnd);
                Array.Clear(_buff, 0, length - sizeToEnd);
            }
        }

#if UNITTEST
        internal void InternalCopyToBuffer(GLine[] src, int srcIndex, int buffIndex, int length) {
            CopyToBuffer(src, srcIndex, buffIndex, length);
        }

        internal void InternalCopyFromBuffer(int buffIndex, GLine[] dest, int destIndex, int length) {
            CopyFromBuffer(buffIndex, dest, destIndex, length);
        }

        internal void InternalClearBuffer(int buffIndex, int length) {
            ClearBuffer(buffIndex, length);
        }

        internal int StartIndex {
            get {
                return _startIndex;
            }
        }

        internal GLine[] GetRawBuff() {
            return (GLine[])_buff.Clone();
        }
#endif
    }

}
