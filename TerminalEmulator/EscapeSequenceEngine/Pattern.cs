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
using System.Globalization;
using System.Linq;

namespace Poderosa.Terminal.EscapeSequenceEngine {
    /// <summary>
    /// Element of a pttern.
    /// </summary>
    internal interface IPatternElement {
    }

    /// <summary>
    /// Represents zero or more numerical parameters. ("{P*}")
    /// </summary>
    internal class ZeroOrMoreNumericalParams : IPatternElement {
        public ZeroOrMoreNumericalParams() {
        }
    }

    /// <summary>
    /// Represents a certain number of repeated parameters. ("{P1}", "{P2}" ...)
    /// </summary>
    internal class NNumericalParams : IPatternElement {
        public readonly int Number;

        public NNumericalParams(int number) {
            this.Number = number;
        }
    }

    /// <summary>
    /// Represents a text parameter. ("{Pt}")
    /// </summary>
    internal class TextParam : IPatternElement {
        public TextParam() {
        }
    }

    /// <summary>
    /// Represents a any character string. ("{Ps}")
    /// </summary>
    internal class AnyCharString : IPatternElement {
        public AnyCharString() {
        }
    }

    /// <summary>
    /// Represents a set of characters.
    /// </summary>
    internal class CharacterSet : IPatternElement {
        public readonly byte[] Characters;

        public CharacterSet(params byte[] characters) {
            this.Characters = characters;
        }
    }

    /// <summary>
    /// Pattern parser
    /// </summary>
    internal class PatternParser {
        public IList<IPatternElement> Parse(string pattern) {
            // \x  character 'x'
            // \\  single backslash ('\')
            // \[  left square bracket ('[')
            // \]  right square bracket (']')
            // \{  left curly bracket ('{')
            // \}  right curly bracket ('}')
            //
            // {...} special component
            //    {ESC} character ESC
            //    {BEL} character BEL
            //     ....
            //    {P*} any number of parameters
            //    {Pt} text parameter (consists of printable chars)
            //    {Ps} any character string (consists of any characters except SOS or ST)
            //    {P1} single numerical parameter
            //    {P2} two numerical parameters
            //     ...
            //
            // [...] character set
            //    [abc]
            //    [A-Z0-9]
            //    [{CR}{LF}]
            //

            var elements = new List<IPatternElement>();

            var charactersInBracket = new List<byte>();
            var rangeIndexInBracket = new List<int>();
            // Note:
            //  range notation (A-Z0-9) is stored like:
            //     charactersInBracket [ 'A' 'Z' '0' '9' ]
            //                              ^       ^
            //     rangeIndexInBracket [    1       3    ]

            int offset = 0;
            bool inBracket = false;
            while (offset < pattern.Length) {
                char ch = pattern[offset++];

                if (ch == '\\') {
                    #region escaped

                    if (offset >= pattern.Length) {
                        throw new ArgumentException("invalid pattern: missing a character trailing backslash");
                    }

                    ch = pattern[offset++];

                    if (inBracket) {
                        charactersInBracket.Add((byte)ch);
                    }
                    else {
                        elements.Add(new CharacterSet((byte)ch));
                    }
                    continue;

                    #endregion
                }

                if (ch == '{') {
                    #region special component

                    int start = offset;
                    int length = 0;
                    for (; ; ) {
                        if (offset >= pattern.Length) {
                            throw new ArgumentException("invalid pattern: missing '}'");
                        }

                        ch = pattern[offset++];
                        if (ch == '}') {
                            break;
                        }
                        length++;
                    }

                    string name = pattern.Substring(start, length);
                    byte? charByte = NameToCharacter(name);
                    if (charByte != null) {
                        if (inBracket) {
                            charactersInBracket.Add(charByte.Value);
                        }
                        else {
                            elements.Add(new CharacterSet(charByte.Value));
                        }
                        continue;
                    }

                    if (name.Length >= 2 && name[0] == 'P') {
                        if (inBracket) {
                            throw new ArgumentException("invalid pattern: parameters cannot be specified in character set ([])");
                        }

                        if (name.Length == 2) {
                            switch (name[1]) {
                                case 't':
                                    elements.Add(new TextParam());
                                    continue;

                                case 's':
                                    elements.Add(new AnyCharString());
                                    continue;

                                case '*':
                                    elements.Add(new ZeroOrMoreNumericalParams());
                                    continue;
                            }
                        }

                        int n;
                        if (Int32.TryParse(name.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out n)) {
                            if (n <= 0) {
                                throw new ArgumentException("invalid pattern: invalid repeat count {" + name + "}");
                            }

                            elements.Add(new NNumericalParams(n));
                            continue;
                        }
                    }

                    throw new ArgumentException("invalid pattern: unknown component {" + name + "}");

                    #endregion
                }

                if (ch == '[') {
                    #region start character-set notation

                    if (inBracket) {
                        throw new ArgumentException("invalid pattern: character set ([]) cannot be nested");
                    }

                    charactersInBracket.Clear();
                    rangeIndexInBracket.Clear();
                    inBracket = true;
                    continue;

                    #endregion
                }

                if (inBracket) {
                    #region in character-set notation

                    if (ch == ']') {
                        if (charactersInBracket.Count == 0) {
                            throw new ArgumentException("invalid pattern: character set ([]) is empty");
                        }

                        var characters = new SortedSet<byte>(charactersInBracket);
                        // expand range notation (-)
                        foreach (int rangeIndex in rangeIndexInBracket) {
                            if (rangeIndex <= 0 || rangeIndex >= charactersInBracket.Count) {
                                // cases like "[-" or "-]".
                                // minus symbol is assumed as a character as it is.
                                characters.Add((byte)'-');
                                continue;
                            }

                            int from = charactersInBracket[rangeIndex - 1];
                            int to = charactersInBracket[rangeIndex];
                            if (from > to) {
                                var t = from;
                                from = to;
                                to = t;
                            }

                            for (int b = from; b <= to; b++) {
                                characters.Add((byte)b);
                            }
                        }

                        elements.Add(new CharacterSet(characters.ToArray()));
                        charactersInBracket.Clear();
                        rangeIndexInBracket.Clear();
                        inBracket = false;
                        continue;
                    }

                    if (ch == '-') {
                        if (charactersInBracket.Count == 0) {
                            // case of "[-..."
                            // minus symbol is assumed as a character as it is.
                            charactersInBracket.Add((byte)'-');
                            continue;
                        }

                        int rangeIndex = charactersInBracket.Count;
                        if (rangeIndexInBracket.Count > 0) {
                            // "rangeIndex" must have 2 characters interval at least from previous index.
                            int prevRangeIndex = rangeIndexInBracket[rangeIndexInBracket.Count - 1];
                            if (rangeIndex < prevRangeIndex + 2) {
                                // minus symbol is assumed as a character as it is.
                                charactersInBracket.Add((byte)'-');
                                continue;
                            }
                        }

                        rangeIndexInBracket.Add(rangeIndex);
                        continue;
                    }

                    charactersInBracket.Add((byte)ch);
                    continue;

                    #endregion
                }

                elements.Add(new CharacterSet((byte)ch));
            }

            if (inBracket) {
                throw new ArgumentException("invalid pattern: character set ([]) is not ended");
            }

            if (elements.Count == 0) {
                throw new ArgumentException("invalid pattern: pattern is empty");
            }

            return elements;
        }

        private byte? NameToCharacter(string keyword) {
            switch (keyword) {
                case "NUL":
                    return (byte)0x00;
                case "SOH":
                    return (byte)0x01;
                case "STX":
                    return (byte)0x02;
                case "ETX":
                    return (byte)0x03;
                case "EOT":
                    return (byte)0x04;
                case "ENQ":
                    return (byte)0x05;
                case "ACK":
                    return (byte)0x06;
                case "BEL":
                    return (byte)0x07;
                case "BS":
                    return (byte)0x08;
                case "HT":
                    return (byte)0x09;
                case "LF":
                    return (byte)0x0a;
                case "VT":
                    return (byte)0x0b;
                case "FF":
                    return (byte)0x0c;
                case "CR":
                    return (byte)0x0d;
                case "LS1":
                    return (byte)0x0e;
                case "SO":
                    return (byte)0x0e;
                case "LS0":
                    return (byte)0x0f;
                case "SI":
                    return (byte)0x0f;
                case "DLE":
                    return (byte)0x10;
                case "DC1":
                    return (byte)0x11;
                case "DC2":
                    return (byte)0x12;
                case "DC3":
                    return (byte)0x13;
                case "DC4":
                    return (byte)0x14;
                case "NAK":
                    return (byte)0x15;
                case "SYN":
                    return (byte)0x16;
                case "ETB":
                    return (byte)0x17;
                case "CAN":
                    return (byte)0x18;
                case "EM":
                    return (byte)0x19;
                case "SUB":
                    return (byte)0x1a;
                case "ESC":
                    return (byte)0x1b;
                case "IS4":
                    return (byte)0x1c;
                case "FS":
                    return (byte)0x1c;
                case "IS3":
                    return (byte)0x1d;
                case "GS":
                    return (byte)0x1d;
                case "IS2":
                    return (byte)0x1e;
                case "RS":
                    return (byte)0x1e;
                case "IS1":
                    return (byte)0x1f;
                case "US":
                    return (byte)0x1f;
                case "SP":
                    return (byte)0x20;
                case "BPH":
                    return (byte)0x82;
                case "NBH":
                    return (byte)0x83;
                case "IND":
                    return (byte)0x84;
                case "NEL":
                    return (byte)0x85;
                case "SSA":
                    return (byte)0x86;
                case "ESA":
                    return (byte)0x87;
                case "HTS":
                    return (byte)0x88;
                case "HTJ":
                    return (byte)0x89;
                case "VTS":
                    return (byte)0x8a;
                case "PLD":
                    return (byte)0x8b;
                case "PLU":
                    return (byte)0x8c;
                case "RI":
                    return (byte)0x8d;
                case "SS2":
                    return (byte)0x8e;
                case "SS3":
                    return (byte)0x8f;
                case "DCS":
                    return (byte)0x90;
                case "PU1":
                    return (byte)0x91;
                case "PU2":
                    return (byte)0x92;
                case "STS":
                    return (byte)0x93;
                case "CCH":
                    return (byte)0x94;
                case "MW":
                    return (byte)0x95;
                case "SPA":
                    return (byte)0x96;
                case "EPA":
                    return (byte)0x97;
                case "SOS":
                    return (byte)0x98;
                case "SCI":
                    return (byte)0x9a;
                case "CSI":
                    return (byte)0x9b;
                case "ST":
                    return (byte)0x9c;
                case "OSC":
                    return (byte)0x9d;
                case "PM":
                    return (byte)0x9e;
                case "APC":
                    return (byte)0x9f;
                default:
                    return null;
            }
        }
    }
}
