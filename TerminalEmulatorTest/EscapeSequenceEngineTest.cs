﻿// Copyright 2024 The Poderosa Project.
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
#if UNITTEST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Poderosa.Terminal.EscapeSequence {

    [TestFixture]
    class EscapeSequenceEngineContextTest {

        public static object[] TestGetTextParamPatterns =
            new object[] {
                new object[] {new char[] { }, ""},
                new object[] {new char[] { 'a' }, "a"},
                new object[] {new char[] { 'a', 'b' }, "ab"},
            };

        [TestCaseSource("TestGetTextParamPatterns")]
        public void TestGetTextParam(char[] input, string expected) {
            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();
            context.AppendChar('x');
            context.AppendChar('x');
            foreach (char ch in input) {
                context.AppendParamChar(ch);
            }
            context.AppendChar('x');
            context.AppendChar('x');
            Assert.AreEqual(expected, context.GetTextParam());
        }

        public static object[] TestGetNumericParamsPatterns =
            new object[] {
                new object[] {"", new int?[] { null }, new int[][] { null }},
                new object[] {"1234567890", new int?[] { 1234567890 }, new int[][] { null }},
                new object[] {"0009821", new int?[] { 9821 }, new int[][] { null }},
                new object[] {"abc", new int?[] { null }, new int[][] { null }},
                new object[] {"11;22;33;44", new int?[] { 11, 22, 33, 44 }, new int[][] { null, null, null, null }},
                new object[] {"11;22;33;44;", new int?[] { 11, 22, 33, 44, null }, new int[][] { null, null, null, null, null }},
                new object[] {";11;22;33;44", new int?[] { null, 11, 22, 33, 44 }, new int[][] { null, null, null, null, null }},
                new object[] {"11;22;xx;44", new int?[] { 11, 22, null, 44 }, new int[][] { null, null, null, null }},
                new object[] {";", new int?[] { null, null }, new int[][] { null, null }},
                new object[] {";;79;;", new int?[] { null, null, 79, null, null }, new int[][] { null, null, null, null, null }},
                new object[] {"11:22:33", new int?[] { null }, new int[][] { new int[] { 11, 22, 33 } }},
                new object[] {"11;22:33;44", new int?[] { 11, null, 44 }, new int[][] { null, new int[] { 22, 33 }, null }},
                new object[] {"11;22:", new int?[] { 11, null }, new int[][] { null, new int[] { 22 } }},
                new object[] {"11;22:abc", new int?[] { 11, null }, new int[][] { null, null }},
                new object[] {"11;22::33", new int?[] { 11, null }, new int[][] { null, null }},
                new object[] {"11;:33", new int?[] { 11, null }, new int[][] { null, null }},
                new object[] {"11;:33;44", new int?[] { 11, null, 44 }, new int[][] { null, null, null }},
            };

        [TestCaseSource("TestGetNumericParamsPatterns")]
        public void TestGetNumericParams(string input, int?[] expectedNumericParams, int[][] expectedCombinationParams) {
            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();
            context.AppendChar('x');
            context.AppendChar('x');
            foreach (char ch in input) {
                context.AppendParamChar(ch);
            }
            context.AppendChar('x');
            context.AppendChar('x');

            int?[] numericParams;
            int[][] combinationParams;
            context.GetNumericParams(out numericParams, out combinationParams);

            Assert.AreEqual(expectedNumericParams, numericParams);
            Assert.AreEqual(expectedCombinationParams, combinationParams);
        }

        [Test]
        public void TestReset() {
            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();
            foreach (char ch in "12;34;56") {
                context.AppendParamChar(ch);
            }
            context.Reset();
            Assert.AreEqual("", context.GetTextParam());

            int?[] numericParams;
            int[][] combinationParams;
            context.GetNumericParams(out numericParams, out combinationParams);
            Assert.AreEqual(new int?[] { null }, numericParams);
            Assert.AreEqual(new int[][] { null }, combinationParams);
        }
    }

    [TestFixture]
    class EscapeSequenceEngineStateTest {

        private void DummyAction(object obj, EscapeSequenceEngineBase.Context context) {
        }

        #region Registration

        [Test]
        public void TestRegisterSingleChar() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('A'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [A] --> <FinalState>",
                },
                dump
            );
        }

        [TestCase(ControlCode.IND, "0x84", "0x1b", "D")]
        [TestCase(ControlCode.NEL, "0x85", "0x1b", "E")]
        [TestCase(ControlCode.HTS, "0x88", "0x1b", "H")]
        [TestCase(ControlCode.RI, "0x8d", "0x1b", "M")]
        [TestCase(ControlCode.SS2, "0x8e", "0x1b", "N")]
        [TestCase(ControlCode.SS3, "0x8f", "0x1b", "O")]
        [TestCase(ControlCode.DCS, "0x90", "0x1b", "P")]
        [TestCase(ControlCode.SPA, "0x96", "0x1b", "V")]
        [TestCase(ControlCode.EPA, "0x97", "0x1b", "W")]
        [TestCase(ControlCode.SOS, "0x98", "0x1b", "X")]
        [TestCase(ControlCode.DECID, "0x9a", "0x1b", "Z")]
        [TestCase(ControlCode.CSI, "0x9b", "0x1b", "[")]
        [TestCase(ControlCode.ST, "0x9c", "0x1b", "\\")]
        [TestCase(ControlCode.OSC, "0x9d", "0x1b", "]")]
        [TestCase(ControlCode.PM, "0x9e", "0x1b", "^")]
        [TestCase(ControlCode.APC, "0x9f", "0x1b", "_")]
        public void TestRegisterSpecialControlChar(char ch, string code, string alt1, string alt2) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute(ch), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [" + alt1 + "] --> <CharState>",
                    "               [" + alt2 + "] --> <FinalState>",
                    "  [" + code + "] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterTwoChars() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('A', 'B'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [A] --> <CharState>",
                    "            [B] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterTwoSpecialControlChars() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute(ControlCode.OSC, ControlCode.ST), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [0x1b] --> <CharState>",
                    "               []] --> <CharState>",
                    "                         [0x1b] --> <CharState>",
                    "                                      [\\] --> <FinalState>",
                    "                         [0x9c] --> <FinalState>",
                    "  [0x9d] --> <CharState>",
                    "               [0x1b] --> <CharState>",
                    "                            [\\] --> <FinalState>",
                    "               [0x9c] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterCharAndNumericParams() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('x', EscapeSequenceParamType.Numeric, 'z'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [x] --> <CharState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [z] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterCharAndNumericParamsTerminatedBySpecialControlCharacter() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('x', EscapeSequenceParamType.Numeric, ControlCode.ST), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [x] --> <CharState>",
                    "            [0x1b] --> <CharState>",
                    "                         [\\] --> <FinalState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [0x1b] --> <CharState>",
                    "                                   [\\] --> <FinalState>",
                    "                      [0x9c] --> <FinalState>",
                    "            [0x9c] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterTwoCharsAndNumericParams() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.Numeric, 'y', 'z'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [a] --> <CharState>",
                    "            [b] --> <CharState>",
                    "                      [0] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [1] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [2] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [3] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [4] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [5] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [6] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [7] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [8] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [9] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [:] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [;] --> <NumericParamsState>",
                    "                                [y] --> <CharState>",
                    "                                          [z] --> <FinalState>",
                    "                      [y] --> <CharState>",
                    "                                [z] --> <FinalState>",
                },
                dump
            );
        }

        [TestCase(new int[] { 1, 2, 3, 4 })]
        [TestCase(new int[] { 2, 1, 3, 4 })]
        [TestCase(new int[] { 3, 1, 2, 4 })]
        [TestCase(new int[] { 4, 2, 1, 3 })]
        public void TestRegisterTwoNumericParamsSequences(int[] order) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            foreach (int pattern in order) {
                switch (pattern) {
                    case 1:
                        state.Register(new EscapeSequenceAttribute('a', EscapeSequenceParamType.Numeric, 'z'), DummyAction);
                        break;
                    case 2:
                        state.Register(new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.Numeric, 'z'), DummyAction);
                        break;
                    case 3:
                        state.Register(new EscapeSequenceAttribute('a', 'b', 'c'), DummyAction);
                        break;
                    case 4:
                        state.Register(new EscapeSequenceAttribute('a', 'c'), DummyAction);
                        break;
                }
            }

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [a] --> <CharState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [z] --> <FinalState>",
                    "            [b] --> <CharState>",
                    "                      [0] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [1] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [2] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [3] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [4] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [5] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [6] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [7] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [8] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [9] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [:] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [;] --> <NumericParamsState>",
                    "                                [z] --> <FinalState>",
                    "                      [c] --> <FinalState>",
                    "                      [z] --> <FinalState>",
                    "            [c] --> <FinalState>",
                    "            [z] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterCharAndTextParams() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('x', EscapeSequenceParamType.Text, 'z'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [x] --> <TextParamState>",
                    "            [z] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterTwoCharsAndTextParam() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.Text, 'y', 'z'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [a] --> <CharState>",
                    "            [b] --> <TextParamState>",
                    "                      [y] --> <CharState>",
                    "                                [z] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterMultipleSequences() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('A'), DummyAction);
            state.Register(new EscapeSequenceAttribute('B', 'C'), DummyAction);
            state.Register(new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'E'), DummyAction);
            state.Register(new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'F'), DummyAction);
            state.Register(new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'G', 'H'), DummyAction);
            state.Register(new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'G', 'J'), DummyAction);
            state.Register(new EscapeSequenceAttribute('D', 'S', EscapeSequenceParamType.Numeric, 'E'), DummyAction);
            state.Register(new EscapeSequenceAttribute('D', 'S', EscapeSequenceParamType.Numeric, 'F'), DummyAction);
            state.Register(new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'L'), DummyAction);
            state.Register(new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'M'), DummyAction);
            state.Register(new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'N', 'O'), DummyAction);
            state.Register(new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'N', 'P'), DummyAction);
            state.Register(new EscapeSequenceAttribute(ControlCode.CSI, 'Q'), DummyAction);
            state.Register(new EscapeSequenceAttribute(ControlCode.CSI, 'R'), DummyAction);

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [0x1b] --> <CharState>",
                    "               [[] --> <CharState>",
                    "                         [Q] --> <FinalState>",
                    "                         [R] --> <FinalState>",
                    "  [A] --> <FinalState>",
                    "  [B] --> <CharState>",
                    "            [C] --> <FinalState>",
                    "  [D] --> <CharState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <CharState>",
                    "                                [H] --> <FinalState>",
                    "                                [J] --> <FinalState>",
                    "            [E] --> <FinalState>",
                    "            [F] --> <FinalState>",
                    "            [G] --> <CharState>",
                    "                      [H] --> <FinalState>",
                    "                      [J] --> <FinalState>",
                    "            [S] --> <CharState>",
                    "                      [0] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [1] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [2] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [3] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [4] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [5] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [6] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [7] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [8] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [9] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [:] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [;] --> <NumericParamsState>",
                    "                                [E] --> <FinalState>",
                    "                                [F] --> <FinalState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "  [K] --> <TextParamState>",
                    "            [L] --> <FinalState>",
                    "            [M] --> <FinalState>",
                    "            [N] --> <CharState>",
                    "                      [O] --> <FinalState>",
                    "                      [P] --> <FinalState>",
                    "  [0x9b] --> <CharState>",
                    "               [Q] --> <FinalState>",
                    "               [R] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterConflictFinalState() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('A'), DummyAction);
            Assert.Throws<ArgumentException>(() =>
                state.Register(new EscapeSequenceAttribute('A'), DummyAction)
            );
        }

        [Test]
        public void TestRegisterConflictFinalStateSpecialControlChar() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute(ControlCode.ESC, '['), DummyAction); // equivalent to CSI, but CSI(0x9b) is not registered

            Assert.Throws<ArgumentException>(() =>
                state.Register(new EscapeSequenceAttribute(ControlCode.CSI), DummyAction) // attempt to register sequence ESC+[ but it fails
            );
        }

        [Test]
        public void TestRegisterConflictCharState() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('A', EscapeSequenceParamType.Numeric, 'B'), DummyAction);
            Assert.Throws<ArgumentException>(() =>
                state.Register(new EscapeSequenceAttribute('A', 'B'), DummyAction)
            );
        }

        [Test]
        public void TestRegisterConflictCharStateSpecialControlChar() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute(ControlCode.ESC, '[', EscapeSequenceParamType.Numeric, 'z'), DummyAction); // equivalent to CSI, but CSI(0x9b) is not registered

            Assert.Throws<ArgumentException>(() =>
                state.Register(new EscapeSequenceAttribute(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'x'), DummyAction)
                // attempt to register sequence ESC+[ but it fails becuase another NumericParamsState exists
            );
        }

        [Test]
        public void TestRegisterConflictTextParamState() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            state.Register(new EscapeSequenceAttribute('A', 'B'), DummyAction);
            Assert.Throws<ArgumentException>(() =>
                state.Register(new EscapeSequenceAttribute('A', EscapeSequenceParamType.Text, 'C'), DummyAction)
            );
        }

        #endregion // Registration

        #region Accept

        [Test]
        public void TestCharStateAccept() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            state.Register(new EscapeSequenceAttribute('A', 'B'), DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'A');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.CharState));

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));
        }

        [Test]
        public void TestCharStateAcceptNotAccepted() {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            state.Register(new EscapeSequenceAttribute('A', 'B'), DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'C');
            Assert.Null(s);
        }

        public static object[] TestNumericParamStateAcceptPatterns =
            new object[] {
                new object[] {"", new int?[] { null }, new int[][] { null }},
                new object[] {"12;34;56", new int?[] { 12, 34, 56 }, new int[][] { null, null, null }},
                new object[] {"12:34:56", new int?[] { null }, new int[][] { new int[] { 12, 34, 56 } }},
                new object[] {";", new int?[] { null, null }, new int[][] { null, null }},
            };

        [TestCaseSource("TestNumericParamStateAcceptPatterns")]
        public void TestNumericParamStateAccept(string parameters, int?[] expectedNumericParams, int[][] expectedCombinationParams) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            state.Register(new EscapeSequenceAttribute('A', EscapeSequenceParamType.Numeric, 'B'), DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'A');

            foreach (char ch in parameters) {
                s = s.Accept(context, ch);
            }

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));

            int?[] numericParams;
            int[][] combinationParams;
            context.GetNumericParams(out numericParams, out combinationParams);
            Assert.AreEqual(expectedNumericParams, numericParams);
            Assert.AreEqual(expectedCombinationParams, combinationParams);

            Assert.AreEqual(parameters, context.GetTextParam());
            Assert.AreEqual("A" + parameters + "B", context.GetBufferedText());
        }

        [TestCase("C")]
        [TestCase("12;34X")]
        public void TestNumericParamStateAcceptNotAccepted(string parameters) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            state.Register(new EscapeSequenceAttribute('A', EscapeSequenceParamType.Numeric, 'B'), DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'A');

            for (int i = 0; i < parameters.Length - 1; i++) {
                s = s.Accept(context, parameters[i]);
            }
            s = s.Accept(context, parameters[parameters.Length - 1]);
            Assert.Null(s);
            Assert.AreEqual("A" + parameters.Substring(0, parameters.Length - 1), context.GetBufferedText());
        }

        [TestCase("")]
        [TestCase("a")]
        [TestCase("abc\u0020def")]
        public void TestTextParamStateAccept(string parameters) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            state.Register(new EscapeSequenceAttribute('A', EscapeSequenceParamType.Text, 'B'), DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'A');

            foreach (char ch in parameters) {
                s = s.Accept(context, ch);
            }

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));
            Assert.AreEqual(parameters, context.GetTextParam());
            Assert.AreEqual("A" + parameters + "B", context.GetBufferedText());
        }

        [TestCase("abc\u0000")]
        [TestCase("abc\u001f")]
        public void TestTextParamStateNotAccept(string parameters) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            state.Register(new EscapeSequenceAttribute('A', EscapeSequenceParamType.Text, 'B'), DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'A');

            for (int i = 0; i < parameters.Length - 1; i++) {
                s = s.Accept(context, parameters[i]);
            }
            s = s.Accept(context, parameters[parameters.Length - 1]);
            Assert.Null(s);
            Assert.AreEqual("A" + parameters.Substring(0, parameters.Length - 1), context.GetBufferedText());
        }

        [Test]
        public void TestFinalStateAcceptNotAccepted() {
            EscapeSequenceEngineBase.FinalState state = new EscapeSequenceEngineBase.FinalState(DummyAction);

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.State s = state.Accept(context, 'A');
            Assert.Null(s);
            Assert.AreEqual("", context.GetBufferedText());
            Assert.AreEqual("", context.GetTextParam());
        }

        #endregion

        private string[] RemoveStateId(string[] dump) {
            return dump.Select(s => Regex.Replace(s, @"\(#\d+\)", "")).ToArray();
        }
    }

    [TestFixture]
    class NumericParamsTest {

        [Test]
        public void TestIsSingleInteger() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13 }, new int[][] { null, null, null, null });

            Assert.IsTrue(p.IsSingleInteger(0));
            Assert.IsTrue(p.IsSingleInteger(1));
            Assert.IsFalse(p.IsSingleInteger(2));
            Assert.IsTrue(p.IsSingleInteger(3));
            Assert.IsFalse(p.IsSingleInteger(4));
            Assert.IsFalse(p.IsSingleInteger(-1));
        }

        [Test]
        public void TestIsIntegerCombination() {
            var p = new NumericParams(new int?[] { null, null, null, null }, new int[][] { new int[] { 21, 22 }, null, null, new int[] { 23, 24 } });

            Assert.IsTrue(p.IsIntegerCombination(0));
            Assert.IsFalse(p.IsIntegerCombination(1));
            Assert.IsFalse(p.IsIntegerCombination(2));
            Assert.IsTrue(p.IsIntegerCombination(3));
            Assert.IsFalse(p.IsIntegerCombination(4));
            Assert.IsFalse(p.IsIntegerCombination(-1));
        }

        [Test]
        public void TestGet() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int[][] { null, null, null, null, null });

            Assert.AreEqual(10, p.Get(0, 99));
            Assert.AreEqual(11, p.Get(1, 99));
            Assert.AreEqual(99, p.Get(2, 99));
            Assert.AreEqual(13, p.Get(3, 99));
            Assert.AreEqual(99, p.Get(4, 99));
            Assert.AreEqual(99, p.Get(5, 99));
            Assert.AreEqual(99, p.Get(-1, 99));
        }

        [Test]
        public void TestGetIntegerCombination() {
            var p = new NumericParams(new int?[] { null, null, null, null, null }, new int[][] { new int[] { 21, 22, 23 }, new int[] { 24 }, null, new int[] { 25, 26 }, null });

            Assert.AreEqual(new int[] { 21, 22, 23 }, p.GetIntegerCombination(0));
            Assert.AreEqual(new int[] { 24 }, p.GetIntegerCombination(1));
            Assert.IsNull(p.GetIntegerCombination(2));
            Assert.AreEqual(new int[] { 25, 26 }, p.GetIntegerCombination(3));
            Assert.IsNull(p.GetIntegerCombination(4));
            Assert.IsNull(p.GetIntegerCombination(5));
            Assert.IsNull(p.GetIntegerCombination(-1));
        }

        [Test]
        public void TestEnumerate() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int[][] { null, null, null, null, null });

            Assert.AreEqual(new int?[] { 10, 11, null, 13, null }, p.Enumerate().ToArray());
        }

        [Test]
        public void TestEnumerateWithDefault() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int[][] { null, null, null, null, null });

            Assert.AreEqual(new int?[] { 10, 11, 99, 13, 99 }, p.EnumerateWithDefault(99).ToArray());
        }

        [Test]
        public void TestEnumerateWithoutNull() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int[][] { null, null, null, null, null });

            Assert.AreEqual(new int[] { 10, 11, 13 }, p.EnumerateWithoutNull().ToArray());
        }
    }

    [TestFixture]
    class EscapeSequenceEngineTest {

        [Test]
        public void TestRegisterHandlerValid() {
            var engine = new EscapeSequenceEngine<ValidHandlers>();

            string[] dump = engine.DumpStates();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [A] --> <FinalState>",
                    "  [B] --> <CharState>",
                    "            [C] --> <FinalState>",
                    "  [D] --> <CharState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [E] --> <FinalState>",
                    "            [E] --> <FinalState>",
                    "  [F] --> <CharState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [G] --> <FinalState>",
                    "            [G] --> <FinalState>",
                    "  [H] --> <CharState>",
                    "            [0] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [1] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [2] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [3] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [4] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [5] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [6] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [7] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [8] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [9] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [:] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [;] --> <NumericParamsState>",
                    "                      [I] --> <FinalState>",
                    "            [I] --> <FinalState>",
                    "  [J] --> <TextParamState>",
                    "            [K] --> <FinalState>",
                    "  [L] --> <TextParamState>",
                    "            [M] --> <FinalState>",
                    "  [N] --> <TextParamState>",
                    "            [O] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterHandlerInvalid() {
            Assert.Throws<ArgumentException>(() =>
                new EscapeSequenceEngine<InvalidNoParamsHandler>()
            );

            Assert.Throws<ArgumentException>(() =>
                new EscapeSequenceEngine<InvalidNumericParamsHandlerTooManyArgs>()
            );

            Assert.Throws<ArgumentException>(() =>
                new EscapeSequenceEngine<InvalidNumericParamsHandlerWrongType>()
            );

            Assert.Throws<ArgumentException>(() =>
                new EscapeSequenceEngine<InvalidTextParamHandlerTooManyArgs>()
            );

            Assert.Throws<ArgumentException>(() =>
                new EscapeSequenceEngine<InvalidTextParamHandlerWrongType>()
            );
        }


        public static object[] TestProcessPatterns = new object[] {
            new object[] { "\u009f", new object[] { "Handle_APC" }},
            new object[] { "\u001b_", new object[] { "Handle_APC" }},

            new object[] { "\u009b\u009c", new object[] { "Handle_CSI_ST", null }},
            new object[] { "\u001b[\u009c", new object[] { "Handle_CSI_ST", null }},
            new object[] { "\u009b\u001b\\", new object[] { "Handle_CSI_ST", null }},
            new object[] { "\u001b[\u001b\\", new object[] { "Handle_CSI_ST", null }},
            new object[] { "\u009b11;22;33\u009c", new object[] { "Handle_CSI_ST", 11, 22, 33 }},
            new object[] { "\u001b[11;22;33\u009c", new object[] { "Handle_CSI_ST", 11, 22, 33 }},
            new object[] { "\u009b11;22;33\u001b\\", new object[] { "Handle_CSI_ST", 11, 22, 33 }},
            new object[] { "\u001b[11;22;33\u001b\\", new object[] { "Handle_CSI_ST", 11, 22, 33 }},

            new object[] { "\u009bX\u009c", new object[] { "Handle_CSI_X_ST", null }},
            new object[] { "\u001b[X\u009c", new object[] { "Handle_CSI_X_ST", null }},
            new object[] { "\u009bX\u001b\\", new object[] { "Handle_CSI_X_ST", null }},
            new object[] { "\u001b[X\u001b\\", new object[] { "Handle_CSI_X_ST", null }},
            new object[] { "\u009bX11;22;33\u009c", new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},
            new object[] { "\u001b[X11;22;33\u009c", new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},
            new object[] { "\u009bX11;22;33\u001b\\", new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},
            new object[] { "\u001b[X11;22;33\u001b\\", new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},

            new object[] { "\u009bXZ\u009c", new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { "\u001b[XZ\u009c", new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { "\u009bXZ\u001b\\", new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { "\u001b[XZ\u001b\\", new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { "\u009bX11;22;33Z\u009c", new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},
            new object[] { "\u001b[X11;22;33Z\u009c", new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},
            new object[] { "\u009bX11;22;33Z\u001b\\", new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},
            new object[] { "\u001b[X11;22;33Z\u001b\\", new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},

            new object[] { "\u009bY\u009c", new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { "\u001b[Y\u009c", new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { "\u009bY\u001b\\", new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { "\u001b[Y\u001b\\", new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { "\u009bY11;22;33\u009c", new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},
            new object[] { "\u001b[Y11;22;33\u009c", new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},
            new object[] { "\u009bY11;22;33\u001b\\", new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},
            new object[] { "\u001b[Y11;22;33\u001b\\", new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},

            new object[] { "\u009bYZ\u009c", new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { "\u001b[YZ\u009c", new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { "\u009bYZ\u001b\\", new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { "\u001b[YZ\u001b\\", new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { "\u009bY11;22;33Z\u009c", new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},
            new object[] { "\u001b[Y11;22;33Z\u009c", new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},
            new object[] { "\u009bY11;22;33Z\u001b\\", new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},
            new object[] { "\u001b[Y11;22;33Z\u001b\\", new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},

            new object[] { "\u009d\u009c", new object[] { "Handle_OSC_ST", "" }},
            new object[] { "\u001b]\u009c", new object[] { "Handle_OSC_ST", "" }},
            new object[] { "\u009d\u001b\\", new object[] { "Handle_OSC_ST", "" }},
            new object[] { "\u001b]\u001b\\", new object[] { "Handle_OSC_ST", "" }},
            new object[] { "\u009dfoo\u009c", new object[] { "Handle_OSC_ST", "foo" }},
            new object[] { "\u001b]foo\u009c", new object[] { "Handle_OSC_ST", "foo" }},
            new object[] { "\u009dfoo\u001b\\", new object[] { "Handle_OSC_ST", "foo" }},
            new object[] { "\u001b]foo\u001b\\", new object[] { "Handle_OSC_ST", "foo" }},
        };

        [TestCaseSource("TestProcessPatterns")]
        public void TestProcess(string input, object[] expected) {
            var engine = new EscapeSequenceEngine<ValidHandlersForCheckFinalState>();

            var instance = new ValidHandlersForCheckFinalState();

            for (int i = 0; i < input.Length - 1; i++) {
                Assert.IsTrue(engine.Process(instance, input[i]));
                Assert.AreEqual(0, instance.Calls.Count);
            }

            Assert.IsTrue(engine.Process(instance, input[input.Length - 1]));
            Assert.AreEqual(1, instance.Calls.Count);
            Assert.AreEqual(expected, instance.Calls[0]);
        }

        [TestCase("X")]
        [TestCase("BX")]
        [TestCase("F123X")]
        [TestCase("Labc\0")]
        public void TestProcessAbort(string input) {
            var engine = new EscapeSequenceEngine<ValidHandlers>();

            ValidHandlers instance = new ValidHandlers();

            for (int i = 0; i < input.Length - 1; i++) {
                Assert.IsTrue(engine.Process(instance, input[i]));
                Assert.AreEqual(0, instance.Calls.Count);
            }

            Assert.IsFalse(engine.Process(instance, input[input.Length - 1]));
            Assert.AreEqual(0, instance.Calls.Count);
        }

        [Test]
        public void TestReset() {
            var engine = new EscapeSequenceEngine<ValidHandlers>();

            ValidHandlers instance = new ValidHandlers();

            foreach (char ch in "F123") {
                Assert.IsTrue(engine.Process(instance, ch));
                Assert.AreEqual(0, instance.Calls.Count);
            }

            engine.Reset();

            foreach (char ch in "Labc") {
                Assert.IsTrue(engine.Process(instance, ch));
                Assert.AreEqual(0, instance.Calls.Count);
            }

            Assert.IsTrue(engine.Process(instance, 'M'));
            Assert.AreEqual(1, instance.Calls.Count);
            Assert.AreEqual(new object[] { "HandlerTextParam2", "abc" }, instance.Calls[0]);
        }

        private string[] RemoveStateId(string[] dump) {
            return dump.Select(s => Regex.Replace(s, @"\(#\d+\)", "")).ToArray();
        }
    }

    #region Classes for input

    class NoHandlers {
    }

    class ValidHandlers {

        public readonly List<object[]> Calls = new List<object[]>();

        private void NonHandlerPrivate() {
            Calls.Add(new object[] { "NonHandlerPrivate" });
        }

        public void NonHandlerPublic() {
            Calls.Add(new object[] { "NonHandlerPublic" });
        }

        [EscapeSequence('A')]
        [EscapeSequence('B', 'C')]
        private void HandlerNoParams() {
            Calls.Add(new object[] { "HandlerNoParams" });
        }

        [EscapeSequence('D', EscapeSequenceParamType.Numeric, 'E')]
        private void HandlerNumericParams() {
            Calls.Add(new object[] { "HandlerNumericParams1" });
        }

        [EscapeSequence('F', EscapeSequenceParamType.Numeric, 'G')]
        [EscapeSequence('H', EscapeSequenceParamType.Numeric, 'I')]
        private void HandlerNumericParams(NumericParams parameters) {
            Calls.Add(
                Enumerable.Concat(
                    new object[] { "HandlerNumericParams2" },
                    parameters.Enumerate().Select(v => (object)v)
                )
                .ToArray()
            );
        }

        [EscapeSequence('J', EscapeSequenceParamType.Text, 'K')]
        private void HandlerTextParam() {
            Calls.Add(new object[] { "HandlerTextParam1" });
        }

        [EscapeSequence('L', EscapeSequenceParamType.Text, 'M')]
        [EscapeSequence('N', EscapeSequenceParamType.Text, 'O')]
        private void HandlerTextParam(string parameter) {
            Calls.Add(new object[] { "HandlerTextParam2", parameter });
        }
    }

    class ValidHandlersForCheckFinalState {

        public readonly List<object[]> Calls = new List<object[]>();

        [EscapeSequence(ControlCode.APC)]
        private void Handle_APC() {
            Calls.Add(new object[] { "Handle_APC" });
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, ControlCode.ST)]
        private void Handle_CSI_ST(NumericParams parameters) {
            Calls.Add(
                Enumerable.Concat(
                    new object[] { "Handle_CSI_ST" },
                    parameters.Enumerate().Select(v => (object)v)
                )
                .ToArray()
            );
        }

        [EscapeSequence(ControlCode.CSI, 'X', EscapeSequenceParamType.Numeric, ControlCode.ST)]
        [EscapeSequence(ControlCode.CSI, 'x', EscapeSequenceParamType.Numeric, ControlCode.ST)]
        private void Handle_CSI_X_ST(NumericParams parameters) {
            Calls.Add(
                Enumerable.Concat(
                    new object[] { "Handle_CSI_X_ST" },
                    parameters.Enumerate().Select(v => (object)v)
                )
                .ToArray()
            );
        }

        [EscapeSequence(ControlCode.CSI, 'X', EscapeSequenceParamType.Numeric, 'Z', ControlCode.ST)]
        private void Handle_CSI_X_Z_ST(NumericParams parameters) {
            Calls.Add(
                Enumerable.Concat(
                    new object[] { "Handle_CSI_X_Z_ST" },
                    parameters.Enumerate().Select(v => (object)v)
                )
                .ToArray()
            );
        }

        [EscapeSequence(ControlCode.CSI, 'Y', EscapeSequenceParamType.Numeric, ControlCode.ST)]
        private void Handle_CSI_Y_ST(NumericParams parameters) {
            Calls.Add(
                Enumerable.Concat(
                    new object[] { "Handle_CSI_Y_ST" },
                    parameters.Enumerate().Select(v => (object)v)
                )
                .ToArray()
            );
        }

        [EscapeSequence(ControlCode.CSI, 'Y', EscapeSequenceParamType.Numeric, 'Z', ControlCode.ST)]
        private void Handle_CSI_Y_Z_ST(NumericParams parameters) {
            Calls.Add(
                Enumerable.Concat(
                    new object[] { "Handle_CSI_Y_Z_ST" },
                    parameters.Enumerate().Select(v => (object)v)
                )
                .ToArray()
            );
        }

        [EscapeSequence(ControlCode.OSC, EscapeSequenceParamType.Text, ControlCode.ST)]
        private void Handle_OSC_ST(string parameter) {
            Calls.Add(new object[] { "Handle_OSC_ST", parameter });
        }
    }

    class InvalidNoParamsHandler {
        [EscapeSequence('A')]
        private void InvalidHandler(string parameter) {
        }
    }

    class InvalidNumericParamsHandlerTooManyArgs {
        [EscapeSequence('D', EscapeSequenceParamType.Numeric, 'E')]
        private void InvalidHandler(NumericParams parameters, NumericParams other) {
        }
    }

    class InvalidNumericParamsHandlerWrongType {
        [EscapeSequence('D', EscapeSequenceParamType.Numeric, 'E')]
        private void InvalidHandler(string parameter) {
        }
    }

    class InvalidTextParamHandlerTooManyArgs {
        [EscapeSequence('J', EscapeSequenceParamType.Text, 'K')]
        private void InvalidHandler(string parameter, string other) {
        }
    }

    class InvalidTextParamHandlerWrongType {
        [EscapeSequence('J', EscapeSequenceParamType.Text, 'K')]
        private void InvalidHandler(NumericParams parameter) {
        }
    }

    #endregion

    [TestFixture]
    class OSCParamsTest {

        [TestCase("1234567890", 1234567890, "")]
        [TestCase("1234567890;", 1234567890, "")]
        [TestCase("56;12;34", 56, "12;34")]
        [TestCase("89;abc;def", 89, "abc;def")]
        public void TestParseSuccess(string paramsText, int expectedCode, string expectedText) {
            OSCParams p;
            bool parsed = OSCParams.Parse(paramsText, out p);

            Assert.IsTrue(parsed);

            Assert.AreEqual(expectedCode, p.GetCode());
            Assert.AreEqual(expectedText, p.GetText());
        }

        [TestCase("")]
        [TestCase(";")]
        [TestCase(";12")]
        [TestCase("a12")]
        [TestCase("1a2")]
        [TestCase("12a")]
        public void TestParseFailure(string paramsText) {
            OSCParams p;
            bool parsed = OSCParams.Parse(paramsText, out p);

            Assert.IsFalse(parsed);
        }

        [TestCase("12", false, false)]
        [TestCase("12;", true, false)]
        [TestCase("12;abc", true, false)]
        [TestCase("12;abc;", true, true)]
        public void TestHasNextParam(string paramsText, bool expected1, bool expected2) {
            OSCParams p;
            bool parsed = OSCParams.Parse(paramsText, out p);

            Assert.IsTrue(parsed);

            bool hasNext = p.HasNextParam();
            Assert.AreEqual(expected1, hasNext);

            if (hasNext) {
                string t;
                bool r = p.TryGetNextText(out t);
                Assert.IsTrue(r);
            }

            hasNext = p.HasNextParam();
            Assert.AreEqual(expected2, hasNext);
        }

        [TestCase("12", null, null)]
        [TestCase("12;", null, null)]
        [TestCase("12;abc", null, null)]
        [TestCase("12;1234567890", 1234567890, null)]
        [TestCase("12;123456;7890", 123456, 7890)]
        [TestCase("12;;7890", null, 7890)]
        [TestCase("12;abc;7890", null, 7890)]
        public void TestTryGetNextInteger(string paramsText, int? expected1, int? expected2) {
            OSCParams p;
            bool parsed = OSCParams.Parse(paramsText, out p);
            Assert.IsTrue(parsed);

            {
                int n1;
                bool r1 = p.TryGetNextInteger(out n1);

                if (expected1.HasValue) {
                    Assert.IsTrue(r1);
                    Assert.AreEqual(expected1.Value, n1);
                }
                else {
                    Assert.IsFalse(r1);
                }
            }

            {
                int n2;
                bool r2 = p.TryGetNextInteger(out n2);

                if (expected2.HasValue) {
                    Assert.IsTrue(r2);
                    Assert.AreEqual(expected2.Value, n2);
                }
                else {
                    Assert.IsFalse(r2);
                }
            }
        }

        [TestCase("12", null, null)]
        [TestCase("12;", "", null)]
        [TestCase("12;abc", "abc", null)]
        [TestCase("12;abc;", "abc", "")]
        [TestCase("12;abc;def", "abc", "def")]
        [TestCase("12;;abc", "", "abc")]
        public void TestTryGetNextText(string paramsText, string expected1, string expected2) {
            OSCParams p;
            bool parsed = OSCParams.Parse(paramsText, out p);
            Assert.IsTrue(parsed);

            {
                string s1;
                bool r1 = p.TryGetNextText(out s1);

                if (expected1 != null) {
                    Assert.IsTrue(r1);
                    Assert.AreEqual(expected1, s1);
                }
                else {
                    Assert.IsFalse(r1);
                }
            }

            {
                string s2;
                bool r2 = p.TryGetNextText(out s2);

                if (expected2 != null) {
                    Assert.IsTrue(r2);
                    Assert.AreEqual(expected2, s2);
                }
                else {
                    Assert.IsFalse(r2);
                }
            }
        }
    }

}
#endif