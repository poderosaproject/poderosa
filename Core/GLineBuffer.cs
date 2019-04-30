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
using System.Collections.Generic;

namespace Poderosa.Document {

    /// <summary>
    /// GLine buffer.
    /// </summary>
    /// <remarks>
    /// This buffer is designed for containing "out-of-screen" lines.
    /// When the number of lines exceeds the capacity, oldest lines are removed from this buffer automatically.
    /// Removing / inserting lines between two lines are not supported.
    /// </remarks>
    public class GLineBuffer {

#if UNITTEST
        internal
#else
        private
#endif
 const int ROWS_PER_PAGE = 2048;

        public const int DEFAULT_CAPACITY = ROWS_PER_PAGE;

        // This buffer consists of a list of `page`s.
        //
        // The page contains contiguious rows.
        // Each page have `ROWS_PER_PAGE` rows, but the first page and the last page of the list may have less rows.
        //
        // When a new row is added to this buffer, a new page will be added to the list as its necessary.
        // When the total number of rows in this buffer exceeds the capacity of this buffer, oldest rows are removed from the oldest page.
        // If the oldest page becomes empty, the page is removed from the list.

        #region GLinePage

        /// <summary>
        /// A group of the GLines.
        /// </summary>
#if UNITTEST
        internal
#else
        private
#endif
 class GLinePage {
            private readonly GLine[] _glines = new GLine[ROWS_PER_PAGE];
            private int _startIndex = 0;    // start index of the range (inclusive)
            private int _endIndex = 0;  // end index of the range (exclusive)

            public int Size {
                get {
                    return _endIndex - _startIndex;
                }
            }

            public int Available {
                get {
                    return ROWS_PER_PAGE - _endIndex;
                }
            }

            public bool IsEmpty {
                get {
                    return _endIndex <= _startIndex;
                }
            }

            public void Apply(int index, int length, Action<GLineChunkSpan> action) {
                if (index < 0) {
                    throw new ArgumentException("invalid index");
                }

                int srcIndex = _startIndex + index;
                int endIndex = srcIndex + length;
                if (srcIndex >= _endIndex || endIndex > _endIndex) {
                    throw new IndexOutOfRangeException();
                }

                action(new GLineChunkSpan(_glines, srcIndex, length));
            }

            public IEnumerable<GLine> Peek(int offset, int length) {
                for (int i = 0; i < length; i++) {
                    yield return _glines[offset + i];
                }
            }

            public bool Append(GLine line) {
                if (_endIndex >= ROWS_PER_PAGE) {
                    return false;
                }
                _glines[_endIndex] = line;
                _endIndex++;
                return true;
            }

            public void RemoveFromHead(int rows) {
                if (rows < 0) {
                    throw new ArgumentException("invalid value", "rows");
                }
                if (rows > this.Size) {
                    throw new ArgumentException("too many rows", "rows");
                }
                Array.Clear(_glines, _startIndex, rows);
                _startIndex += rows;
            }

            public void RemoveFromTail(GLineChunkSpan span) {
                if (span.Length > this.Size) {
                    throw new ArgumentException("too many rows", "rows");
                }
                int newEndIndex = _endIndex - span.Length;
                Array.Copy(_glines, newEndIndex, span.Array, span.Offset, span.Length);
                Array.Clear(_glines, newEndIndex, span.Length);
                _endIndex = newEndIndex;
            }
        }

        #endregion

        #region GLinePageList

        /// <summary>
        /// List of the GLinePages which is maintained with the circular buffer.
        /// </summary>
#if UNITTEST
        internal
#else
        private
#endif
 class GLinePageList {
            // circular buffer of the GLinePage
            private readonly int _capacity;
            private readonly GLinePage[] _pages;
            private int _startIndex = 0;
            private int _size = 0;

            public GLinePage this[int index] {
                get {
                    if (index < 0 || index >= _size) {
                        throw new IndexOutOfRangeException();
                    }
                    return _pages[(_startIndex + index) % _capacity];
                }
            }

            public GLinePage Head {
                get {
                    return this[0];
                }
            }

            public GLinePage Tail {
                get {
                    return this[_size - 1];
                }
            }

            public int Size {
                get {
                    return _size;
                }
            }

            public int Capacity {
                get {
                    return _capacity;
                }
            }

            public GLinePageList(int maxRows) {
                // Required number of pages for retainig maxRows is:
                //     ceil(maxRows / ROWS_PER_PAGE) + 1
                // And more 1 page is required because new rows are added before remove oldest rows.
                _capacity = (maxRows + ROWS_PER_PAGE - 1) / ROWS_PER_PAGE + 2;
                _pages = new GLinePage[_capacity];
            }

            public GLinePageList(int maxRows, GLinePageList source)
                : this(maxRows) {
                // copy pages from `source`
                if (source._size > _capacity) {
                    throw new InvalidOperationException("too many pages in the source list");
                }
                int sourceIndex = source._startIndex;
                for (int i = 0; i < source._size; i++) {
                    _pages[_size] = source._pages[sourceIndex];
                    _size++;
                    sourceIndex = (sourceIndex + 1) % source._capacity;
                }
            }

            public void Append(GLinePage page) {
                if (_size >= _capacity) {
                    throw new InvalidOperationException("GLinePageList full");
                }
                _pages[(_startIndex + _size) % _capacity] = page;
                _size++;
            }

            public void RemoveHead() {
                if (_size > 0) {
                    _pages[_startIndex] = null;
                    _startIndex = (_startIndex + 1) % _capacity;
                    _size--;
                }
            }

            public void RemoveTail() {
                if (_size > 0) {
                    _pages[(_startIndex + _size - 1) % _capacity] = null;
                    _size--;
                }
            }

            public GLinePage Peek(int internalIndex) {
                return _pages[internalIndex % _capacity];
            }
        }

        #endregion

        private GLinePageList _pageList;
        private int _capacity;

        private int _rowCount = 0;
        private int _firstRowID = 1;  // Row ID of the first row

        private readonly object _syncRoot;

        /// <summary>
        /// Object for synchronization of the buffer operations.
        /// </summary>
        public object SyncRoot {
            get {
                return _syncRoot;
            }
        }

        /// <summary>
        /// Row ID span of this document.
        /// </summary>
        public RowIDSpan RowIDSpan {
            get {
                lock (_syncRoot) {
                    return new RowIDSpan(_firstRowID, _rowCount);
                }
            }
        }

        /// <summary>
        /// Next row ID
        /// </summary>
        public int NextRowID {
            get {
                lock (_syncRoot) {
                    return _firstRowID + _rowCount;
                }
            }
        }

        /// <summary>
        /// Constructs an instance with the default capacity.
        /// </summary>
        public GLineBuffer()
            : this(null, DEFAULT_CAPACITY) {
        }

        /// <summary>
        /// Constructs an instance with the default capacity.
        /// </summary>
        public GLineBuffer(object sync)
            : this(sync, DEFAULT_CAPACITY) {
        }

        /// <summary>
        /// Constructs an instance with the specified capacity.
        /// </summary>
        /// <param name="capacity">capacity of this buffer in number of rows.</param>
        public GLineBuffer(int capacity)
            : this(null, capacity) {
        }

        /// <summary>
        /// Constructs an instance with the specified capacity.
        /// </summary>
        /// <param name="sync">an object to be used for the synchronization</param>
        /// <param name="capacity">capacity of this buffer in number of rows.</param>
        public GLineBuffer(object sync, int capacity) {
            _syncRoot = sync ?? new object();
            capacity = Math.Max(capacity, 1);
            _capacity = capacity;
            _pageList = new GLinePageList(capacity);
        }

        /// <summary>
        /// <para>Set capacity of this buffer.</para>
        /// <para>If new capacity was smaller than the current content size, oldest rows are removed.</para>
        /// </summary>
        /// <param name="newCapacity">new capacity of this buffer in number of rows.</param>
        public void SetCapacity(int newCapacity) {
            lock (_syncRoot) {
                newCapacity = Math.Max(newCapacity, 1);
                // if the capacity was shrinked, oldest rows have to be removed before the page list is re-created.
                _capacity = newCapacity;
                TrimHead(); // note that this call does nothing if the capacity was enlarged.
                _pageList = new GLinePageList(newCapacity, _pageList);
            }
        }

#if UNITTEST
        /// <summary>
        /// Gets the capacity of the page list for testing purpose.
        /// </summary>
        /// <returns>the capacity of the page list in the number of pages</returns>
        internal int GetPageCapacity() {
            return _pageList.Capacity;
        }
#endif
        /// <summary>
        /// <para>Append a line.</para>
        /// <para>If this buffer was full, an oldest row is removed.</para>
        /// </summary>
        /// <param name="line">line to add</param>
        public void Append(GLine line) {
            lock (_syncRoot) {
                if (_pageList.Size == 0) {
                    _pageList.Append(new GLinePage());
                }
                if (!_pageList.Tail.Append(line)) { // page-full
                    _pageList.Append(new GLinePage());
                    _pageList.Tail.Append(line);
                }
                _rowCount++;
                TrimHead();
            }
        }

        /// <summary>
        /// <para>Append lines.</para>
        /// <para>If this buffer was full, oldest rows are removed.</para>
        /// </summary>
        /// <param name="lines">lines to add</param>
        public void Append(IEnumerable<GLine> lines) {
            lock (_syncRoot) {
                foreach (GLine line in lines) {
                    if (_pageList.Size == 0) {
                        _pageList.Append(new GLinePage());
                    }
                    if (!_pageList.Tail.Append(line)) { // page-full
                        TrimHead(); // reduce active pages for avoiding list-full
                        _pageList.Append(new GLinePage());
                        _pageList.Tail.Append(line);
                    }
                    _rowCount++;
                }
                TrimHead();
            }
        }

        /// <summary>
        /// Remove tail (newest) rows.
        /// </summary>
        /// <param name="span">
        /// a span of the GLine array to store the removed rows.
        /// the chunk length dictates the number of rows to remove.
        /// </param>
        /// <returns>array of the removed rows.</returns>
        public void RemoveFromTail(GLineChunkSpan span) {
            lock (_syncRoot) {
                if (span.Length > _rowCount) {
                    throw new ArgumentException("too many rows", "rows");
                }

                int removedLinesCount = 0;
                int subSpanOffset = span.Length;
                while (_pageList.Size > 0 && removedLinesCount < span.Length) {
                    GLinePage tailPage = _pageList.Tail;
                    int rowsToRemove = Math.Min(span.Length - removedLinesCount, tailPage.Size);
                    subSpanOffset -= rowsToRemove;
                    tailPage.RemoveFromTail(span.Span(subSpanOffset, rowsToRemove));
                    removedLinesCount += rowsToRemove;
                    _rowCount -= rowsToRemove;
                    if (tailPage.IsEmpty) {
                        _pageList.RemoveTail();
                    }
                }
            }
        }

        /// <summary>
        /// Gets lines starting with the specified row ID.
        /// </summary>
        /// <param name="startRowID">row ID of the first row</param>
        /// <param name="span">
        /// a span of the GLine array to store the copied rows.
        /// the chunk length dictates the number of rows to copy.
        /// </param>
        /// <exception cref="InvalidOperationException">buffer is empty</exception>
        /// <exception cref="ArgumentException">length is smaller than zero</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// the range specified by <paramref name="startRowID"/> and <paramref name="span"/> doesn't match with this buffer
        /// </exception>
        public void GetLinesByID(int startRowID, GLineChunkSpan span) {
            int spanOffset = 0;
            Apply(startRowID, span.Length, s => {
                Array.Copy(s.Array, s.Offset, span.Array, span.Offset + spanOffset, s.Length);
                spanOffset += s.Length;
            });
        }

        /// <summary>
        /// Calls action for the specified range.
        /// </summary>
        /// <param name="startRowID">row ID of the first row</param>
        /// <param name="length">number of rows</param>
        /// <param name="action">
        /// action to be called for each span.
        /// </param>
        /// <exception cref="InvalidOperationException">buffer is empty</exception>
        /// <exception cref="ArgumentException">length is smaller than zero</exception>
        /// <exception cref="IndexOutOfRangeException">
        /// the range specified by <paramref name="startRowID"/> and <paramref name="length"/> doesn't match with this buffer
        /// </exception>
        public void Apply(int startRowID, int length, Action<GLineChunkSpan> action) {
            lock (_syncRoot) {
                if (_rowCount <= 0) {
                    if (length == 0) {
                        return;
                    }
                    throw new InvalidOperationException("no lines");
                }

                // determine row index in this buffer
                int rowIndex = startRowID - _firstRowID;

                // check range
                if (rowIndex < 0 || rowIndex + length > _rowCount) {
                    throw new IndexOutOfRangeException();
                }

                // determine page index
                int firstPageRows = _pageList.Head.Size;
                int pageIndex;
                int pageRowIndex;
                if (rowIndex < firstPageRows) {
                    pageIndex = 0;
                    pageRowIndex = rowIndex;
                }
                else {
                    int r = rowIndex - firstPageRows;
                    pageIndex = r / ROWS_PER_PAGE + 1;
                    pageRowIndex = r % ROWS_PER_PAGE;
                }

                // get lines
                if (length > 0) {
                    int lineCount = 0;
                    for (; ; ) {
                        GLinePage page = _pageList[pageIndex];
                        int rowsToRead = Math.Min(page.Size - pageRowIndex, length - lineCount);
                        page.Apply(pageRowIndex, rowsToRead, action);
                        lineCount += rowsToRead;
                        if (lineCount >= length) {
                            break;
                        }
                        pageIndex++;
                        pageRowIndex = 0;
                    }
                }
            }
        }

#if UNITTEST
        /// <summary>
        /// Copy internal <see cref="GLinePage"/>s for the testing purpose. 
        /// </summary>
        /// <returns></returns>
        internal GLinePage[] GetAllPages() {
            lock (_syncRoot) {
                int size = _pageList.Size;
                GLinePage[] pages = new GLinePage[size];
                for (int i = 0; i < size; i++) {
                    pages[i] = _pageList[i];
                }
                return pages;
            }
        }
#endif

        /// <summary>
        /// Removes oldest rows that were pushed out from this buffer.
        /// </summary>
        private void TrimHead() {
            int rowsToRemove = _rowCount - _capacity;
            while (_pageList.Size > 0 && rowsToRemove > 0) {
                GLinePage headPage = _pageList.Head;
                int pageSize = headPage.Size;

                int rowsRemoved;
                if (rowsToRemove >= pageSize) {
                    _pageList.RemoveHead();
                    rowsRemoved = pageSize;
                }
                else {
                    headPage.RemoveFromHead(rowsToRemove);
                    rowsRemoved = rowsToRemove;
                }

                rowsToRemove -= rowsRemoved;
                _rowCount -= rowsRemoved;
                _firstRowID += rowsRemoved;
            }
        }

    }



}
