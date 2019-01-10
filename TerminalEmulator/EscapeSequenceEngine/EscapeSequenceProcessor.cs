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
using System.Linq;

namespace Poderosa.Terminal.EscapeSequenceEngine {

    /// <summary>
    /// Base class of escape-sequence processor
    /// </summary>
    internal abstract class AbstractEscapeSequenceProcessor {

        private readonly DfaEngine _dfaEngine;
        private readonly Action<char> _onTextChar;
        private readonly Action<char[]> _onUnknownSequence;

        private bool _escape = false;

        private const char CHAR_ESC = '\u001b';
        private const byte BYTE_ESC = 0x1b;

        /// <summary>
        /// Unescape C1 code in the 2-character escape sequence form.
        /// </summary>
        /// <param name="secondByte">a byte after ESC</param>
        /// <returns>equvalent code or null</returns>
        protected abstract byte? UnescapeC1(byte secondByte);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dfaEngine">DFA engine</param>
        /// <param name="onTextChar">callback to process a character in the normal text</param>
        /// <param name="onUnknownSequence">callback to process unknown escape sequence</param>
        protected AbstractEscapeSequenceProcessor(DfaEngine dfaEngine, Action<char> onTextChar, Action<char[]> onUnknownSequence) {
            _dfaEngine = dfaEngine;
            _onTextChar = onTextChar;
            _onUnknownSequence = onUnknownSequence;
        }

        /// <summary>
        /// Reset status
        /// </summary>
        public void Reset() {
            _escape = false;
            _dfaEngine.Abort();
        }

        /// <summary>
        /// Process a single char.
        /// </summary>
        /// <param name="c">char data</param>
        public void Process(char c) {
            if (_escape) {
                AfterEscape(c);
                _escape = false;
                return;
            }

            if (c == CHAR_ESC) {
                _escape = true;
                return;
            }

            ProcessChar(c);
        }

        private void AfterEscape(char c) {
            if (c <= 0xff) {
                byte b = (byte)c;
                byte? c1 = UnescapeC1(b);
                if (c1.HasValue) {
                    ProcessByte(c1.Value, BYTE_ESC, b);
                    return;
                }
            }

            ProcessByte(BYTE_ESC, BYTE_ESC);
            ProcessChar(c);
        }

        private void ProcessChar(char c) {
            if (c <= 0xff) {
                byte b = (byte)c;
                ProcessByte(b, b);
            }
            else {
                _dfaEngine.Abort();
                EscapeSequenceContext context = _dfaEngine.Context;
                if (context.Matched.Count > 0) {
                    _onUnknownSequence(ToCharArray(context.Matched, c));
                }
                else {
                    _onTextChar(c);
                }
            }
        }

        private readonly byte[] _processByteOrigBytes1 = new byte[1];
        private readonly byte[] _processByteOrigBytes2 = new byte[2];

        private void ProcessByte(byte b, byte origByte) {
            _processByteOrigBytes1[0] = origByte;
            ProcessByteCore(b, _processByteOrigBytes1);
        }

        private void ProcessByte(byte b, byte origByte1, byte origByte2) {
            _processByteOrigBytes2[0] = origByte1;
            _processByteOrigBytes2[1] = origByte2;
            ProcessByteCore(b, _processByteOrigBytes2);
        }

        private void ProcessByteCore(byte b, byte[] origBytes) {
            bool accepetd = _dfaEngine.Process(b, origBytes);
            if (!accepetd) {
                EscapeSequenceContext context = _dfaEngine.Context;
                if (context.Matched.Count > 0) {
                    _onUnknownSequence(ToCharArray(context.Matched, origBytes));
                }
                else {
                    foreach (byte ob in origBytes) {
                        _onTextChar((char)ob);
                    }
                }
            }
        }

        private char[] ToCharArray(IEnumerable<byte> list1, IEnumerable<byte> list2) {
            return list1.Concat(list2).Select(b => (char)b).ToArray();
        }

        private char[] ToCharArray(IEnumerable<byte> list1, char c) {
            return list1.Select(b => (char)b).Concat(new char[] { c }).ToArray();
        }
    }

    /// <summary>
    /// Escape-sequence processor for XTERM emulation
    /// </summary>
    internal class XTermEscapeSequenceProcessor : AbstractEscapeSequenceProcessor {

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dfaEngine">DFA engine</param>
        /// <param name="onTextChar">callback to process a character in the normal text</param>
        /// <param name="onUnknownSequence">callback to process unknown escape sequence</param>
        public XTermEscapeSequenceProcessor(DfaEngine dfaEngine, Action<char> onTextChar, Action<char[]> onUnknownSequence)
            : base(dfaEngine, onTextChar, onUnknownSequence) {
        }

        protected override byte? UnescapeC1(byte secondByte) {
            switch (secondByte) {
                case (byte)'D': // ESC D: Index (IND)
                    return 0x84;
                case (byte)'E': // ESC E: Next Line (NEL)
                    return 0x85;
                case (byte)'H': // ESC H: Tab Set (HTS)
                    return 0x88;
                case (byte)'M': // ESC M: Reverse Index (RI)
                    return 0x8d;
                case (byte)'N': // ESC N: Single Shift Select of G2 Character Set (SS2)
                    return 0x8e;
                case (byte)'O': // ESC O: Single Shift Select of G3 Character Set (SS3)
                    return 0x8f;
                case (byte)'P': // ESC P: Device Control String (DCS)
                    return 0x90;
                case (byte)'V': // ESC V: Start of Guarded Area (SPA)
                    return 0x96;
                case (byte)'W': // ESC W: End of Guarded Area (EPA)
                    return 0x97;
                case (byte)'X': // ESC X: Start of String (SOS)
                    return 0x98;
                case (byte)'Z': // ESC Z: Return Terminal ID (DECID), or Single Character Introducer (SCI)
                    return 0x9a;
                case (byte)'[': // ESC [: Control Sequence Introducer (CSI)
                    return 0x9b;
                case (byte)'\\': // ESC \: String Terminator (ST)
                    return 0x9c;
                case (byte)']': // ESC ]: Operating System Command (OSC)
                    return 0x9d;
                case (byte)'^': // ESC ^: Privacy Message (PM)
                    return 0x9e;
                case (byte)'_': // ESC _: Application Program Command (APC)
                    return 0x9f;
                default:
                    return null;
            }
        }
    }
}
