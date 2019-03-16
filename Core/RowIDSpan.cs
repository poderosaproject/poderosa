using System;
using System.Collections.Generic;
using System.Linq;
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

using System.Text;
using System.Threading.Tasks;

namespace Poderosa.Document {

    /// <summary>
    /// A struct to represent a span of the row IDs.
    /// </summary>
    public struct RowIDSpan {

        private readonly int _start;
        private readonly int _length;

        /// <summary>
        /// Start row ID
        /// </summary>
        public int Start {
            get {
                return _start;
            }
        }

        /// <summary>
        /// Number of rows in this range
        /// </summary>
        public int Length {
            get {
                return _length;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="start">start row ID</param>
        /// <param name="length">number of rows</param>
        public RowIDSpan(int start, int length) {
            this._start = start;
            this._length = Math.Max(length, 0);
        }

        /// <summary>
        /// Gets an intersection of this span and another span.
        /// </summary>
        /// <param name="other">another span</param>
        /// <returns>intersection span</returns>
        public RowIDSpan Intersect(RowIDSpan other) {
            int otherEnd = other._start + other._length;
            if (otherEnd <= this._start) {
                return new RowIDSpan(this._start, 0);
            }
            int thisEnd = this._start + this._length;
            if (thisEnd <= other._start) {
                return new RowIDSpan(this._start, 0);
            }
            int intersectStart = Math.Max(this._start, other._start);
            int intersectEnd = Math.Min(thisEnd, otherEnd);
            return new RowIDSpan(intersectStart, intersectEnd - intersectStart);
        }
    }
}
