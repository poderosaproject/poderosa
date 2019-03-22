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


namespace Poderosa.Document {

    /// <summary>
    /// A row region that need to be redrawn.
    /// </summary>
    public class InvalidatedRegion {

        private enum Status {
            Empty,
            All,
            Range,
        }

        private int _startRowID = 0;    // inclusive
        private int _endRowID = 0;      // exclusive
        private Status _status = Status.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        public InvalidatedRegion() {
        }

        /// <summary>
        /// Starting row ID (inclusive)
        /// </summary>
        public int StartRowID {
            get {
                return _startRowID;
            }
        }

        /// <summary>
        /// Ending row ID (exclusive)
        /// </summary>
        public int EndRowID {
            get {
                return _endRowID;
            }
        }

        /// <summary>
        /// Whether this range is empty
        /// </summary>
        public bool IsEmpty {
            get {
                lock (this) {
                    return _endRowID <= _startRowID;
                }
            }
        }

        /// <summary>
        /// Whether all rows need to be redrawn
        /// </summary>
        public bool InvalidatedAll {
            get {
                return _status == Status.All;
            }
            set {
                lock (this) {
                    _startRowID = _endRowID = 0;
                    _status = Status.All;
                }
            }
        }

        /// <summary>
        /// Invalidate a single row
        /// </summary>
        /// <param name="rowID"></param>
        public void InvalidateRow(int rowID) {
            lock (this) {
                if (_status == Status.Range) {
                    if (rowID < _startRowID) {
                        _startRowID = rowID;
                    }
                    if (rowID >= _endRowID) {
                        _endRowID = rowID + 1;
                    }
                }
                else if (_status == Status.Empty) {
                    _startRowID = rowID;
                    _endRowID = rowID + 1;
                    _status = Status.Range;
                }
            }
        }

        /// <summary>
        /// Invalidate a span of rows
        /// </summary>
        /// <param name="span"></param>
        public void InvalidateRows(RowIDSpan span) {
            lock (this) {
                if (_status == Status.Range) {
                    if (span.Start < _startRowID) {
                        _startRowID = span.Start;
                    }
                    int endSpan = span.Start + span.Length;
                    if (endSpan > _endRowID) {
                        _endRowID = endSpan;
                    }
                }
                else if (_status == Status.Empty) {
                    _startRowID = span.Start;
                    _endRowID = span.Start + span.Length;
                    _status = Status.Range;
                }
                // if the status was Status.All, no need to update.
            }
        }

        /// <summary>
        /// Clear
        /// </summary>
        public void Clear() {
            lock (this) {
                _startRowID = _endRowID = 0;
                _status = Status.Empty;
            }
        }

        /// <summary>
        /// Copy region information then clear
        /// </summary>
        /// <returns>copied information</returns>
        public InvalidatedRegion GetCopyAndClear() {
            lock (this) {
                InvalidatedRegion copy = (InvalidatedRegion)MemberwiseClone();
                Clear();
                return copy;
            }
        }
    }
}
