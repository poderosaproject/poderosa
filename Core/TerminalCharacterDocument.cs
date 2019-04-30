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

using Poderosa.View;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Poderosa.Document {

    /// <summary>
    /// <see cref="ICharacterDocument"/> implementation which consists of a screen buffer and a log buffer.
    /// </summary>
    public abstract class TerminalCharacterDocument : ICharacterDocument {

        protected readonly object _syncRoot = new object();

        protected readonly InvalidatedRegion _invalidatedRegion = new InvalidatedRegion();

        private readonly GLineBuffer _logBuffer;

        private readonly GLineScreenBuffer _screenBuffer;

        private readonly GLineChunk _workGLineBuff = new GLineChunk(10);

        private readonly GLine[] _workSingleGLine = new GLine[1];

        /// <summary>
        /// Gets whether the screen buffer is isolated from the log buffer.
        /// </summary>
        /// <returns>if true, the rows scroll-out from the screen are not moved to the log buffer.</returns>
        protected abstract bool IsScreenIsolated();

        /// <summary>
        /// Notifies document implementation from the document viewer
        /// that the size of the visible area was changed.
        /// </summary>
        /// <param name="rows">number of visible rows</param>
        /// <param name="cols">number of visible columns</param>
        protected abstract void OnVisibleAreaSizeChanged(int rows, int cols);

        /// <summary>
        /// Creates a new line object.
        /// </summary>
        /// <returns>new line object</returns>
        protected abstract GLine CreateEmptyLine();

        #region ICharacterDocument

        public object SyncRoot {
            get {
                return _syncRoot;
            }
        }

        public InvalidatedRegion InvalidatedRegion {
            get {
                return _invalidatedRegion;
            }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="width">initial screen width</param>
        /// <param name="height">initial screen height</param>
        protected TerminalCharacterDocument(int width, int height) {
            if (width <= 0) {
                throw new ArgumentException("invalid width", "width");
            }
            if (height <= 0) {
                throw new ArgumentException("invalid height", "height");
            }

            _logBuffer = new GLineBuffer(_syncRoot);
            _screenBuffer = new GLineScreenBuffer(_syncRoot, height, (n) => new GLine(width));
        }

        #region ICharacterDocument

        /// <summary>
        /// Gets range of the row ID in this document.
        /// </summary>
        /// <returns>span of the row ID</returns>
        public RowIDSpan GetRowIDSpan() {
            lock (_syncRoot) {
                var logSpan = _logBuffer.RowIDSpan;
                return new RowIDSpan(logSpan.Start, logSpan.Length + _screenBuffer.Size);
            }
        }

        /// <summary>
        /// Determines which color should be used as the background color of this document.
        /// </summary>
        /// <param name="profile">current profile</param>
        /// <returns>background color</returns>
        public virtual Color DetermineBackgroundColor(RenderProfile profile) {
            return profile.BackColor;
        }

        /// <summary>
        /// Determines which image should be painted (or should not be painted) in the background of this document.
        /// </summary>
        /// <param name="profile">current profile</param>
        /// <returns>an image object to paint, or null.</returns>
        public Image DetermineBackgroundImage(RenderProfile profile) {
            return profile.GetImage();
        }

        /// <summary>
        /// Apply action to each row in the specified range.
        /// </summary>
        /// <remarks>
        /// This method must guarantee that the specified action is called for all rows in the specified range.
        /// If a row was missing in this document, null is passed to the action.
        /// </remarks>
        /// <param name="startRowID">start Row ID</param>
        /// <param name="rows">number of rows</param>
        /// <param name="action">
        /// a delegate function to apply. the first argument is a row ID. the second argument is a target GLine object.
        /// </param>
        public void ForEach(int startRowID, int rows, Action<int, GLine> action) {
            if (rows < 0) {
                throw new ArgumentException("invalid value", "rows");
            }
            if (action == null) {
                throw new ArgumentException("action is null", "action");
            }

            lock (_syncRoot) {
                RowIDSpan logBuffSpan;
                RowIDSpan screenBuffSpan;
                GetRowIDSpans(out logBuffSpan, out screenBuffSpan);

                int rowID = startRowID;

                {
                    RowIDSpan logIterSpan = logBuffSpan.Intersect(new RowIDSpan(startRowID, rows));

                    if (logIterSpan.Length > 0) {
                        while (rowID < logIterSpan.Start) {
                            action(rowID, null);
                            rowID++;
                        }

                        _logBuffer.Apply(logIterSpan.Start, logIterSpan.Length, s => {
                            foreach (var line in s.GLines()) {
                                action(rowID, line);
                                rowID++;
                            }
                        });
                    }
                }

                {
                    RowIDSpan screenIterSpan = screenBuffSpan.Intersect(new RowIDSpan(startRowID, rows));

                    if (screenIterSpan.Length > 0) {
                        while (rowID < screenIterSpan.Start) {
                            action(rowID, null);
                            rowID++;
                        }

                        _screenBuffer.Apply(screenIterSpan.Start - screenBuffSpan.Start, screenIterSpan.Length, s => {
                            foreach (var line in s.GLines()) {
                                action(rowID, line);
                                rowID++;
                            }
                        });
                    }
                }

                int endRowID = startRowID + rows;
                while (rowID < endRowID) {
                    action(rowID, null);
                    rowID++;
                }
            }
        }

        /// <summary>
        /// Apply action to the specified row.
        /// </summary>
        /// <remarks>
        /// If a row was missing in this document, null is passed to the action.
        /// </remarks>
        /// <param name="rowID">Row ID</param>
        /// <param name="action">
        /// a delegate function to apply. the first argument may be null.
        /// </param>
        public void Apply(int rowID, Action<GLine> action) {
            if (action == null) {
                throw new ArgumentException("action is null", "action");
            }

            lock (_syncRoot) {
                RowIDSpan logBuffSpan;
                RowIDSpan screenBuffSpan;
                GetRowIDSpans(out logBuffSpan, out screenBuffSpan);

                if (logBuffSpan.Includes(rowID)) {
                    _logBuffer.Apply(rowID, 1, s => {
                        action(s.Array[s.Offset]);
                    });
                }
                else if (screenBuffSpan.Includes(rowID)) {
                    _screenBuffer.Apply(rowID - screenBuffSpan.Start, 1, s => {
                        action(s.Array[s.Offset]);
                    });
                }
                else {
                    action(null);
                }
            }
        }

        /// <summary>
        /// Notifies document implementation from the document viewer
        /// that the size of the visible area was changed.
        /// </summary>
        /// <param name="rows">number of visible rows</param>
        /// <param name="cols">number of visible columns</param>
        public void VisibleAreaSizeChanged(int rows, int cols) {
            int newRows = Math.Max(rows, 1);
            int newCols = Math.Max(cols, 1);

            lock (_syncRoot) {
                int curRows = _screenBuffer.Size;
                if (newRows < curRows) {
                    int shrinkRows = curRows - newRows;
                    if (IsScreenIsolated()) {
                        // the first row must not be moved.
                        _screenBuffer.ShrinkTail(shrinkRows);
                    }
                    else {
                        // copy rows from the screen buffer to the log buffer
                        _workGLineBuff.EnsureCapacity(shrinkRows);
                        var buffSpan = _workGLineBuff.Span(0, shrinkRows);
                        _screenBuffer.GetRows(0, buffSpan);
                        _logBuffer.Append(buffSpan.GLines());
                        _workGLineBuff.Clear();
                        // shrink the screen buffer
                        _screenBuffer.ShrinkHead(shrinkRows);
                    }
                }
                else if (newRows > curRows) {
                    int extendRows = newRows - curRows;
                    if (IsScreenIsolated()) {
                        // the first row must not be moved.
                        _screenBuffer.ExtendTail(FillNewLines(_workGLineBuff, extendRows));
                        _workGLineBuff.Clear();
                    }
                    else {
                        int logRows = _logBuffer.RowIDSpan.Length;
                        // move rows from the log buffer to the screen buffer (only available rows)
                        int moveRows = Math.Min(extendRows, logRows);
                        if (moveRows > 0) {
                            _workGLineBuff.EnsureCapacity(moveRows);
                            var buffSpan = _workGLineBuff.Span(0, moveRows);
                            _logBuffer.RemoveFromTail(buffSpan);
                            _screenBuffer.ExtendHead(buffSpan);
                            _workGLineBuff.Clear();
                        }
                        // extend screen if the moved rows were not enough
                        int extendTailRows = extendRows - moveRows;
                        if (extendTailRows > 0) {
                            _screenBuffer.ExtendTail(FillNewLines(_workGLineBuff, extendTailRows));
                            _workGLineBuff.Clear();
                        }
                    }
                }

                OnVisibleAreaSizeChanged(rows, cols);
            }
        }

        #endregion

        //-------------------------------------------------------------
        // Internal screen operations
        //-------------------------------------------------------------

        /// <summary>
        /// Append a single line.
        /// </summary>
        /// <param name="line">line object</param>
        protected void ScreenAppend(GLine line) {
            lock (_syncRoot) {
                _workSingleGLine[0] = line;
                ScreenScrollUp(new GLineChunkSpan(_workSingleGLine, 0, 1));
                _workSingleGLine[0] = null;
            }
        }

        /// <summary>
        /// <para>
        /// Gets a <see cref="GLine"/> object at the specified row.
        /// </para>
        /// <para>
        /// Note that each access to this property does internal synchronization.
        /// Consider to use <see cref="ScreenGetRows(int, GLineChunkSpan)"/> or <see cref="ScreenSetRows(int, GLineChunkSpan)"/>.
        /// </para>
        /// </summary>
        /// <param name="rowIndex">row index of the screen</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="rowIndex"/> was out-of-range.</exception>
        protected GLine ScreenGetRow(int rowIndex) {
            return _screenBuffer[rowIndex];
        }

        /// <summary>
        /// <para>
        /// Gets or sets a <see cref="GLine"/> object at the specified row.
        /// </para>
        /// <para>
        /// Note that each access to this property does internal synchronization.
        /// Consider to use <see cref="ScreenGetRows(int, GLineChunkSpan)"/> or <see cref="ScreenSetRows(int, GLineChunkSpan)"/>.
        /// </para>
        /// </summary>
        /// <param name="rowIndex">row index of the screen</param>
        /// <param name="line">line object to set</param>
        /// <exception cref="IndexOutOfRangeException"><paramref name="rowIndex"/> was out-of-range.</exception>
        protected void ScreenSetRow(int rowIndex, GLine line) {
            _screenBuffer[rowIndex] = line;
        }

        /// <summary>
        /// Gets rows starting at the specified index.
        /// </summary>
        /// <param name="rowIndex">row index. 0 indicates the first row at the top of the screen.</param>
        /// <param name="span">
        /// array buffer to store the copied object.
        /// the length of the span is used as the number of rows to get.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="rowIndex"/> and the length of the span was out-of-range.</exception>
        protected void ScreenGetRows(int rowIndex, GLineChunkSpan span) {
            _screenBuffer.GetRows(rowIndex, span);
        }

        /// <summary>
        /// Sets rows starting at the specified index.
        /// </summary>
        /// <param name="rowIndex">row index. 0 indicates the first row at the top of the screen.</param>
        /// <param name="span">
        /// array buffer to get objects to be copied.
        /// the length of the span is used as the number of rows to set.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="rowIndex"/> and the length of the span was out-of-range.</exception>
        protected void ScreenSetRows(int rowIndex, GLineChunkSpan span) {
            _screenBuffer.SetRows(rowIndex, span);
        }

        /// <summary>
        /// Scroll-up entire of the screen.
        /// </summary>
        /// <param name="scrollRows">number of rows to scroll</param>
        protected void ScreenScrollUp(int scrollRows) {
            lock (_syncRoot) {
                int screenRows = _screenBuffer.Size;
                int scrollOutRows = Math.Min(scrollRows, screenRows);

                if (!IsScreenIsolated()) {
                    // copy rows from the screen buffer to the log buffer
                    _workGLineBuff.EnsureCapacity(scrollOutRows);
                    var buffSpan = _workGLineBuff.Span(0, scrollOutRows);
                    _screenBuffer.GetRows(0, buffSpan);
                    _logBuffer.Append(buffSpan.GLines());
                    _workGLineBuff.Clear();
                    // append empty rows if the copied rows were not enough
                    if (scrollOutRows < scrollRows) {
                        _logBuffer.Append(GenerateNewLines(scrollRows - scrollOutRows));
                    }
                }
                _screenBuffer.ScrollUp(FillNewLines(_workGLineBuff, scrollOutRows));
                _workGLineBuff.Clear();
            }
        }

        /// <summary>
        /// Scroll-up entire of the screen.
        /// </summary>
        /// <param name="newRows">rows to append at the bottom of the screen.</param>
        protected void ScreenScrollUp(GLineChunkSpan newRows) {
            lock (_syncRoot) {
                int scrollRows = newRows.Length;
                int screenRows = _screenBuffer.Size;
                int scrollOutRows = Math.Min(scrollRows, screenRows);

                if (!IsScreenIsolated()) {
                    // copy rows from the screen buffer to the log buffer
                    _workGLineBuff.EnsureCapacity(scrollOutRows);
                    var buffSpan = _workGLineBuff.Span(0, scrollOutRows);
                    _screenBuffer.GetRows(0, buffSpan);
                    _logBuffer.Append(buffSpan.GLines());
                    _workGLineBuff.Clear();
                    // copy rows from newRows if the copied rows were not enough
                    if (scrollOutRows < scrollRows) {
                        _logBuffer.Append(newRows.Span(0, scrollRows - scrollOutRows).GLines());
                    }
                }

                _screenBuffer.ScrollUp(newRows.Span(scrollRows - scrollOutRows, scrollOutRows));
            }
        }

        /// <summary>
        /// Scroll-down entire of the screen.
        /// </summary>
        /// <param name="scrollRows">number of rows to scroll</param>
        protected void ScreenScrollDown(int scrollRows) {
            lock (_syncRoot) {
                if (!IsScreenIsolated()) {
                    var logRowIDSpan = _logBuffer.RowIDSpan;
                    // copy rows from the log buffer to the screen buffer
                    _workGLineBuff.EnsureCapacity(scrollRows);
                    int rowsToRemove = Math.Min(scrollRows, logRowIDSpan.Length);
                    int emptyRows = scrollRows - rowsToRemove;
                    if (emptyRows > 0) {
                        FillNewLines(_workGLineBuff.Span(0, emptyRows));
                    }
                    _logBuffer.RemoveFromTail(_workGLineBuff.Span(emptyRows, rowsToRemove));
                    _screenBuffer.ScrollDown(_workGLineBuff.Span(0, scrollRows));
                    _workGLineBuff.Clear();
                }
                else {
                    _screenBuffer.ScrollDown(FillNewLines(_workGLineBuff, scrollRows));
                }
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
        protected void ScreenScrollUpRegion(int startRowIndex, int endRowIndex, GLineChunkSpan newRows) {
            _screenBuffer.ScrollUpRegion(startRowIndex, endRowIndex, newRows);
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
        protected void ScreenScrollDownRegion(int startRowIndex, int endRowIndex, GLineChunkSpan newRows) {
            _screenBuffer.ScrollDownRegion(startRowIndex, endRowIndex, newRows);
        }

#if UNITTEST
        internal int ScreenBufferSize {
            get {
                return _screenBuffer.Size;
            }
        }

        internal int LogBufferSize {
            get {
                return _logBuffer.RowIDSpan.Length;
            }
        }

        internal void StoreGLines(GLine[] screenBuff) {
            for (int i = 0; i < screenBuff.Length; i++) {
                _screenBuffer[i] = screenBuff[i];
            }
        }
        
        internal void PeekGLines(out GLine[] screenBuff, out GLine[] logBuff) {
            lock (_syncRoot) {
                int screenSize = _screenBuffer.Size;
                screenBuff = new GLine[screenSize];
                _screenBuffer.GetRows(0, new GLineChunkSpan(screenBuff, 0, screenSize));

                var logSpan = _logBuffer.RowIDSpan;
                logBuff = new GLine[logSpan.Length];
                _logBuffer.GetLinesByID(logSpan.Start, new GLineChunkSpan(logBuff, 0, logSpan.Length));
            }
        }
#endif

        private void GetRowIDSpans(out RowIDSpan logBuffSpan, out RowIDSpan screenBuffSpan) {
            logBuffSpan = _logBuffer.RowIDSpan;
            screenBuffSpan = new RowIDSpan(logBuffSpan.Start + logBuffSpan.Length, _screenBuffer.Size);
        }

        private GLineChunkSpan FillNewLines(GLineChunk chunk, int rows) {
            chunk.EnsureCapacity(rows);
            var span = chunk.Span(0, rows);
            FillNewLines(span);
            return span;
        }

        private void FillNewLines(GLineChunkSpan span) {
            for (int i = 0; i < span.Length; i++) {
                span.Array[span.Offset + i] = CreateEmptyLine();
            }
        }

        private IEnumerable<GLine> GenerateNewLines(int rows) {
            for (int i = 0; i < rows; i++) {
                yield return CreateEmptyLine();
            }
        }
    }
}
