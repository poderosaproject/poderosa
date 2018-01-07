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

using System;

namespace Poderosa.Document.Internal {

    /// <summary>
    /// Flag bits for <see cref="GChar"/>.
    /// </summary>
    [Flags]
    internal enum GCharFlags : uint {
        None = 0u,
        RightHalf = 1u << 30,
        WideWidth = UnicodeCharFlags.WideWidth,
        Mask = RightHalf | WideWidth,
    }

    /// <summary>
    /// Character information in <see cref="GLine"/>.
    /// </summary>
    internal struct GChar {
        // bit 0..20 : Unicode Code Point (copied from UnicodeChar)
        //
        // bit 30 : Right half of a wide-width character
        // bit 31 : wide width (copied from UnicodeChar)

        private readonly uint _bits;

        private const uint CodePointMask = 0x1fffffu;
        private const uint UnicodeCharMask = CodePointMask | (uint)UnicodeCharFlags.WideWidth;

        /// <summary>
        /// An instance of SPACE (U+0020)
        /// </summary>
        public static GChar ASCII_SPACE {
            get {
                // The cost of the constructor would be zero with JIT compiler enabling optimization.
                return new GChar(UnicodeChar.ASCII_SPACE);
            }
        }

        /// <summary>
        /// An instance of NUL (U+0000)
        /// </summary>
        public static GChar ASCII_NUL {
            get {
                // The cost of the constructor would be zero with JIT compiler enabling optimization.
                return new GChar(UnicodeChar.ASCII_NUL);
            }
        }

        /// <summary>
        /// Flag bits
        /// </summary>
        internal GCharFlags Flags {
            get {
                return (GCharFlags)(_bits & (uint)GCharFlags.Mask);
            }
        }

        /// <summary>
        /// Unicode code point
        /// </summary>
        public uint CodePoint {
            get {
                return _bits & CodePointMask;
            }
        }

        /// <summary>
        /// Whether this object represents a wide-width character.
        /// </summary>
        public bool IsWideWidth {
            get {
                return (_bits & (uint)GCharFlags.WideWidth) != 0u;
            }
        }

        /// <summary>
        /// Whether this object represents right-half of a wide-width character.
        /// </summary>
        public bool IsRightHalf {
            get {
                const uint mask = (uint)(GCharFlags.WideWidth | GCharFlags.RightHalf);
                return (_bits & mask) == mask;
            }
        }

        /// <summary>
        /// Whether this object represents left-half of a wide-width character.
        /// </summary>
        public bool IsLeftHalf {
            get {
                const uint mask = (uint)(GCharFlags.WideWidth | GCharFlags.RightHalf);
                return (_bits & mask) == (uint)GCharFlags.WideWidth;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ch">Unicode character</param>
        public GChar(UnicodeChar ch) {
            this._bits = ch.RawData & UnicodeCharMask;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="codePoint">Unicode code point</param>
        /// <param name="flags">flags</param>
        public GChar(uint codePoint, GCharFlags flags) {
            this._bits = codePoint | (uint)flags;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bits">raw bits</param>
        private GChar(uint bits) {
            this._bits = bits;
        }

        /// <summary>
        /// Copy character into the char array in UTF-16.
        /// </summary>
        /// <param name="seq">destination array</param>
        /// <param name="index">start index of the array</param>
        /// <returns>char count written</returns>
        public int WriteTo(char[] seq, int index) {
            return Unicode.WriteCodePointTo(this._bits & CodePointMask, seq, index);
        }

        /// <summary>
        /// Adds flags.
        /// </summary>
        /// <param name="ch">object to be based on</param>
        /// <param name="flags">flags to add</param>
        /// <returns>new object</returns>
        public static GChar operator +(GChar ch, GCharFlags flags) {
            return new GChar(ch._bits | (uint)flags);
        }
    }

}
