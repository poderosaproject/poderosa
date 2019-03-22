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
    /// <see cref="ICharacterDocument"/> implementation that only supports appending new lines.
    /// </summary>
    public abstract class AppendOnlyCharacterDocument : ICharacterDocument {

        protected readonly object _syncRoot = new object();

        protected readonly InvalidatedRegion _invalidatedRegion = new InvalidatedRegion();

        private readonly GLineBuffer _buffer = new GLineBuffer();


        /// <summary>
        /// Constructor
        /// </summary>
        public AppendOnlyCharacterDocument() {
            _buffer = new GLineBuffer(_syncRoot);    // FIXME: set capacity
        }

        /// <summary>
        /// Append a single line.
        /// </summary>
        /// <param name="line">line object</param>
        protected void Append(GLine line) {
            lock (_syncRoot) {
                int rowID = _buffer.NextRowID;
                _buffer.Append(line);
                _invalidatedRegion.InvalidateRow(rowID);
            }
        }

        /// <summary>
        /// Append single lines.
        /// </summary>
        /// <param name="lines">sequence of line objects</param>
        protected void Append(IEnumerable<GLine> lines) {
            lock (_syncRoot) {
                int rowIDStart = _buffer.NextRowID;
                _buffer.Append(lines);
                int rowIDEnd = _buffer.NextRowID;
                _invalidatedRegion.InvalidateRows(new RowIDSpan(rowIDStart, rowIDEnd - rowIDStart));
            }
        }

        #region ICharacterDocument

        /// <summary>
        /// Object for the synchronization.
        /// </summary>
        public object SyncRoot {
            get {
                return _syncRoot;
            }
        }

        /// <summary>
        /// Invalidated region
        /// </summary>
        public InvalidatedRegion InvalidatedRegion {
            get {
                return _invalidatedRegion;
            }
        }

        /// <summary>
        /// Gets range of the row ID in this document.
        /// </summary>
        /// <returns>span of the row ID</returns>
        public RowIDSpan GetRowIDSpan() {
            lock (_syncRoot) {
                return _buffer.RowIDSpan;
            }
        }

        /// <summary>
        /// Determines which color should be used as the background color of this document.
        /// </summary>
        /// <param name="profile">current profile</param>
        /// <returns>background color</returns>
        public Color DetermineBackgroundColor(RenderProfile profile) {
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
                RowIDSpan buffSpan = _buffer.RowIDSpan;
                RowIDSpan iterSpan = buffSpan.Intersect(new RowIDSpan(startRowID, rows));

                if (iterSpan.Length > 0) {
                    int rowID = startRowID;

                    while (rowID < iterSpan.Start) {
                        action(rowID, null);
                        rowID++;
                    }

                    _buffer.Apply(iterSpan.Start, iterSpan.Length, s => {
                        for (int i = 0; i < s.Length; i++) {
                            action(rowID, s.Array[s.Offset + i]);
                            rowID++;
                        }
                    });

                    int endRowID = startRowID + rows;
                    while (rowID < endRowID) {
                        action(rowID, null);
                        rowID++;
                    }
                }
                else {
                    // all null
                    for (int i = 0; i < rows; i++) {
                        action(startRowID + i, null);
                    }
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
                RowIDSpan buffSpan = _buffer.RowIDSpan;

                if (rowID >= buffSpan.Start && rowID - buffSpan.Start < buffSpan.Length) {
                    _buffer.Apply(rowID, 1, s => {
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
            // do nothing
        }

        #endregion
    }

}
