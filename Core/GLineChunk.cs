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

namespace Poderosa.Document {

    /// <summary>
    /// Manages reusable <see cref="GLine"/> array.
    /// </summary>
    public class GLineChunk {

        private GLine[] _array;

        /// <summary>
        /// Underlying array
        /// </summary>
        public GLine[] Array {
            get {
                return _array;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="capacity">initial capacity</param>
        public GLineChunk(int capacity) {
            _array = new GLine[capacity];
        }

        /// <summary>
        /// Clear array
        /// </summary>
        public void Clear() {
            System.Array.Clear(_array, 0, _array.Length);
        }

        /// <summary>
        /// Ensures that the capacity of this instance is at least the specified value.
        /// </summary>
        /// <param name="capacity">minimum capacity</param>
        public void EnsureCapacity(int capacity) {
            GLine[] oldArray = _array;
            if (oldArray.Length < capacity) {
                GLine[] newArray = new GLine[capacity];
                System.Array.Copy(oldArray, 0, newArray, 0, oldArray.Length);
                _array = newArray;
            }
        }

        /// <summary>
        /// Creates <see cref="GLineChunkSpan"/> which is based on this instance.
        /// </summary>
        /// <param name="offset">offset of the underlying array</param>
        /// <param name="length">span length in the number of rows</param>
        /// <returns>new span</returns>
        /// <exception cref="ArgumentException"><paramref name="offset"/> and <paramref name="length"/> doesn't fit the underlying array.</exception>
        public GLineChunkSpan Span(int offset, int length) {
            return new GLineChunkSpan(_array, offset, length);
        }
    }

    /// <summary>
    /// A struct that specifies a range on the <see cref="GLine"/> array.
    /// </summary>
    public struct GLineChunkSpan {

        /// <summary>
        /// Underlying array
        /// </summary>
        public readonly GLine[] Array;

        /// <summary>
        /// Offset from the head of the array
        /// </summary>
        public readonly int Offset;

        /// <summary>
        /// Length of this span
        /// </summary>
        public readonly int Length;

        internal GLineChunkSpan(GLine[] array, int offset, int length) {
#if DEBUG || UNITTEST
            if (offset < 0) {
                throw new ArgumentException("invalid offset", "offset");
            }
            if (length < 0) {
                throw new ArgumentException("invalid length", "length");
            }
            if (offset + length > array.Length) {
                throw new ArgumentException("size of the underlying array is not enough");
            }
#endif
            this.Array = array;
            this.Offset = offset;
            this.Length = length;
        }

        /// <summary>
        /// Gets a sub span of this span.
        /// </summary>
        /// <param name="offset">offset in this span. 0 indicates that the new offset of the underlying array is same with this span.</param>
        /// <param name="length">length of the new span.</param>
        /// <returns>new span</returns>
        public GLineChunkSpan Span(int offset, int length) {
#if DEBUG || UNITTEST
            if (offset < 0) {
                throw new ArgumentException("invalid offset", "offset");
            }
            if (length < 0) {
                throw new ArgumentException("invalid length", "length");
            }
            if (offset + length > this.Length) {
                throw new ArgumentException("offset and length exceed length of the based span.");
            }
#endif
            return new GLineChunkSpan(this.Array, this.Offset + offset, length);
        }
    }

}
