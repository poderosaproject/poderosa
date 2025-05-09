// Copyright 2024-2025 The Poderosa Project.
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using NUnit.Framework;

namespace Poderosa.Terminal.EscapeSequence {

    [TestFixture]
    class NumericParamsParserTest {

        public static object[] TestParsePatterns =
            new object[] {
                new object[] {"", new int?[] { null }, new int?[][] { null }},
                new object[] {"1234567890", new int?[] { 1234567890 }, new int?[][] { null }},
                new object[] {"0009821", new int?[] { 9821 }, new int?[][] { null }},
                new object[] {"abc", new int?[] { null }, new int?[][] { null }},
                new object[] {"11;22;33;44", new int?[] { 11, 22, 33, 44 }, new int?[][] { null, null, null, null }},
                new object[] {"11;22;33;44;", new int?[] { 11, 22, 33, 44, null }, new int?[][] { null, null, null, null, null }},
                new object[] {";11;22;33;44", new int?[] { null, 11, 22, 33, 44 }, new int?[][] { null, null, null, null, null }},
                new object[] {"11;22;xx;44", new int?[] { 11, 22, null, 44 }, new int?[][] { null, null, null, null }},
                new object[] {";", new int?[] { null, null }, new int?[][] { null, null }},
                new object[] {";;79;;", new int?[] { null, null, 79, null, null }, new int?[][] { null, null, null, null, null }},
                new object[] {"11:22:33", new int?[] { null }, new int?[][] { new int?[] { 11, 22, 33 } }},
                new object[] {"11;22:33;44", new int?[] { 11, null, 44 }, new int?[][] { null, new int?[] { 22, 33 }, null }},
                new object[] {"11;22:", new int?[] { 11, null }, new int?[][] { null, new int?[] { 22, null } }},
                new object[] {"11;22:abc", new int?[] { 11, null }, new int?[][] { null, new int?[] { 22, null } }},
                new object[] {"11;22::33", new int?[] { 11, null }, new int?[][] { null, new int?[] { 22, null, 33 }}},
                new object[] {"11;:33", new int?[] { 11, null }, new int?[][] { null, new int?[] {null, 33} }},
                new object[] {"11;:33;44", new int?[] { 11, null, 44 }, new int?[][] { null, new int?[] {null, 33}, null }},
            };

        [TestCaseSource("TestParsePatterns")]
        public void TestParse(string input, int?[] expectedNumericParams, int?[][] expectedCombinationParams) {
            NumericParams p = NumericParamsParser.Parse(input);

            int?[] numericParams = p.GetNumericParametersForTesting();
            int?[][] combinationParams = p.GetIntegerCombinationsForTesting();

            Assert.AreEqual(expectedNumericParams, numericParams);
            Assert.AreEqual(expectedCombinationParams, combinationParams);
        }
    }

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

        [Test]
        public void TestGetLastChar() {
            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();
            context.AppendChar('a');
            context.AppendChar('b');
            context.AppendChar('c');
            context.AppendChar('d');
            Assert.AreEqual('d', context.GetLastChar());
        }

        public static object[] TestGetNumericParamsPatterns =
            new object[] {
                new object[] {"", new int?[] { null }, new int?[][] { null }},
                new object[] {"11;22;33;44", new int?[] { 11, 22, 33, 44 }, new int?[][] { null, null, null, null }},
            };

        [TestCaseSource("TestGetNumericParamsPatterns")]
        public void TestGetNumericParams(string input, int?[] expectedNumericParams, int?[][] expectedCombinationParams) {
            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();
            context.AppendChar('x');
            context.AppendChar('x');
            foreach (char ch in input) {
                context.AppendParamChar(ch);
            }
            context.AppendChar('x');
            context.AppendChar('x');

            NumericParams p = context.GetNumericParams();
            int?[] numericParams = p.GetNumericParametersForTesting();
            int?[][] combinationParams = p.GetIntegerCombinationsForTesting();

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

            NumericParams p = context.GetNumericParams();
            int?[] numericParams = p.GetNumericParametersForTesting();
            int?[][] combinationParams = p.GetIntegerCombinationsForTesting();
            Assert.AreEqual(new int?[] { null }, numericParams);
            Assert.AreEqual(new int[][] { null }, combinationParams);
        }
    }

    [TestFixture]
    class EscapeSequenceEngineStateMachineBuilderTest {

        class DummyClass {
            private void DummyAction() {
            }
        }

        class DummyClass4 {
            private void DummyAction1() {
            }
            private void DummyAction2() {
            }
            private void DummyAction3() {
            }
            private void DummyAction4() {
            }
        }

        private EscapeSequenceEngineBase.CharState Build(params EscapeSequenceAttribute[] attrs) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();
            new EscapeSequenceEngineBase.StateMachineBuilder(state).RegisterHandlers(
                typeof(DummyClass),
                (method) => {
                    if (method.Name == "DummyAction") {
                        return attrs;
                    }
                    else {
                        return new EscapeSequenceAttribute[0];
                    }
                }
            );
            return state;
        }

        #region Registration

        [Test]
        public void TestRegisterSingleChar() {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A')
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute(ch)
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', 'B')
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute(ControlCode.OSC, ControlCode.ST)
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('x', EscapeSequenceParamType.Numeric, 'z')
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('x', EscapeSequenceParamType.Numeric, ControlCode.ST)
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.Numeric, 'y', 'z')
            );

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

        [TestCase(new int[] { 0, 1, 2, 3 })]
        [TestCase(new int[] { 1, 0, 2, 3 })]
        [TestCase(new int[] { 2, 0, 1, 3 })]
        [TestCase(new int[] { 3, 1, 0, 2 })]
        public void TestRegisterTwoNumericParamsSequences(int[] order) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            EscapeSequenceAttribute[] attrs = {
                new EscapeSequenceAttribute('a', EscapeSequenceParamType.Numeric, 'z'),
                new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.Numeric, 'z'),
                new EscapeSequenceAttribute('a', 'b', 'c'),
                new EscapeSequenceAttribute('a', 'c'),
            };

            int orderIndex = 0;
            new EscapeSequenceEngineBase.StateMachineBuilder(state).RegisterHandlers(
                typeof(DummyClass4),
                (method) => {
                    if (method.Name.StartsWith("DummyAction") && orderIndex < order.Length) {
                        return new List<EscapeSequenceAttribute>() { attrs[order[orderIndex++]] };
                    }
                    else {
                        return new List<EscapeSequenceAttribute>();
                    }
                }
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('x', EscapeSequenceParamType.Text, 'z')
            );

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
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.Text, 'y', 'z')
            );

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
        public void TestRegisterNextPrintableParams() {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('a', 'b', EscapeSequenceParamType.SinglePrintable),
                new EscapeSequenceAttribute('a', 'c', EscapeSequenceParamType.SinglePrintable),
                new EscapeSequenceAttribute('a', EscapeSequenceParamType.SinglePrintable)
            );

            string[] dump = state.Dump();
            foreach (string s in dump) {
                Console.WriteLine(s);
            }
            dump = RemoveStateId(dump);

            Assert.AreEqual(
                new string[] {
                    "<CharState>",
                    "  [a] --> <CharState>",
                    "            [!] --> <FinalState>",
                    "            [\"] --> <FinalState>",
                    "            [#] --> <FinalState>",
                    "            [$] --> <FinalState>",
                    "            [%] --> <FinalState>",
                    "            [&] --> <FinalState>",
                    "            ['] --> <FinalState>",
                    "            [(] --> <FinalState>",
                    "            [)] --> <FinalState>",
                    "            [*] --> <FinalState>",
                    "            [+] --> <FinalState>",
                    "            [,] --> <FinalState>",
                    "            [-] --> <FinalState>",
                    "            [.] --> <FinalState>",
                    "            [/] --> <FinalState>",
                    "            [0] --> <FinalState>",
                    "            [1] --> <FinalState>",
                    "            [2] --> <FinalState>",
                    "            [3] --> <FinalState>",
                    "            [4] --> <FinalState>",
                    "            [5] --> <FinalState>",
                    "            [6] --> <FinalState>",
                    "            [7] --> <FinalState>",
                    "            [8] --> <FinalState>",
                    "            [9] --> <FinalState>",
                    "            [:] --> <FinalState>",
                    "            [;] --> <FinalState>",
                    "            [<] --> <FinalState>",
                    "            [=] --> <FinalState>",
                    "            [>] --> <FinalState>",
                    "            [?] --> <FinalState>",
                    "            [@] --> <FinalState>",
                    "            [A] --> <FinalState>",
                    "            [B] --> <FinalState>",
                    "            [C] --> <FinalState>",
                    "            [D] --> <FinalState>",
                    "            [E] --> <FinalState>",
                    "            [F] --> <FinalState>",
                    "            [G] --> <FinalState>",
                    "            [H] --> <FinalState>",
                    "            [I] --> <FinalState>",
                    "            [J] --> <FinalState>",
                    "            [K] --> <FinalState>",
                    "            [L] --> <FinalState>",
                    "            [M] --> <FinalState>",
                    "            [N] --> <FinalState>",
                    "            [O] --> <FinalState>",
                    "            [P] --> <FinalState>",
                    "            [Q] --> <FinalState>",
                    "            [R] --> <FinalState>",
                    "            [S] --> <FinalState>",
                    "            [T] --> <FinalState>",
                    "            [U] --> <FinalState>",
                    "            [V] --> <FinalState>",
                    "            [W] --> <FinalState>",
                    "            [X] --> <FinalState>",
                    "            [Y] --> <FinalState>",
                    "            [Z] --> <FinalState>",
                    "            [[] --> <FinalState>",
                    "            [\\] --> <FinalState>",
                    "            []] --> <FinalState>",
                    "            [^] --> <FinalState>",
                    "            [_] --> <FinalState>",
                    "            [`] --> <FinalState>",
                    "            [a] --> <FinalState>",
                    "            [b] --> <CharState>",
                    "                      [!] --> <FinalState>",
                    "                      [\"] --> <FinalState>",
                    "                      [#] --> <FinalState>",
                    "                      [$] --> <FinalState>",
                    "                      [%] --> <FinalState>",
                    "                      [&] --> <FinalState>",
                    "                      ['] --> <FinalState>",
                    "                      [(] --> <FinalState>",
                    "                      [)] --> <FinalState>",
                    "                      [*] --> <FinalState>",
                    "                      [+] --> <FinalState>",
                    "                      [,] --> <FinalState>",
                    "                      [-] --> <FinalState>",
                    "                      [.] --> <FinalState>",
                    "                      [/] --> <FinalState>",
                    "                      [0] --> <FinalState>",
                    "                      [1] --> <FinalState>",
                    "                      [2] --> <FinalState>",
                    "                      [3] --> <FinalState>",
                    "                      [4] --> <FinalState>",
                    "                      [5] --> <FinalState>",
                    "                      [6] --> <FinalState>",
                    "                      [7] --> <FinalState>",
                    "                      [8] --> <FinalState>",
                    "                      [9] --> <FinalState>",
                    "                      [:] --> <FinalState>",
                    "                      [;] --> <FinalState>",
                    "                      [<] --> <FinalState>",
                    "                      [=] --> <FinalState>",
                    "                      [>] --> <FinalState>",
                    "                      [?] --> <FinalState>",
                    "                      [@] --> <FinalState>",
                    "                      [A] --> <FinalState>",
                    "                      [B] --> <FinalState>",
                    "                      [C] --> <FinalState>",
                    "                      [D] --> <FinalState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <FinalState>",
                    "                      [H] --> <FinalState>",
                    "                      [I] --> <FinalState>",
                    "                      [J] --> <FinalState>",
                    "                      [K] --> <FinalState>",
                    "                      [L] --> <FinalState>",
                    "                      [M] --> <FinalState>",
                    "                      [N] --> <FinalState>",
                    "                      [O] --> <FinalState>",
                    "                      [P] --> <FinalState>",
                    "                      [Q] --> <FinalState>",
                    "                      [R] --> <FinalState>",
                    "                      [S] --> <FinalState>",
                    "                      [T] --> <FinalState>",
                    "                      [U] --> <FinalState>",
                    "                      [V] --> <FinalState>",
                    "                      [W] --> <FinalState>",
                    "                      [X] --> <FinalState>",
                    "                      [Y] --> <FinalState>",
                    "                      [Z] --> <FinalState>",
                    "                      [[] --> <FinalState>",
                    "                      [\\] --> <FinalState>",
                    "                      []] --> <FinalState>",
                    "                      [^] --> <FinalState>",
                    "                      [_] --> <FinalState>",
                    "                      [`] --> <FinalState>",
                    "                      [a] --> <FinalState>",
                    "                      [b] --> <FinalState>",
                    "                      [c] --> <FinalState>",
                    "                      [d] --> <FinalState>",
                    "                      [e] --> <FinalState>",
                    "                      [f] --> <FinalState>",
                    "                      [g] --> <FinalState>",
                    "                      [h] --> <FinalState>",
                    "                      [i] --> <FinalState>",
                    "                      [j] --> <FinalState>",
                    "                      [k] --> <FinalState>",
                    "                      [l] --> <FinalState>",
                    "                      [m] --> <FinalState>",
                    "                      [n] --> <FinalState>",
                    "                      [o] --> <FinalState>",
                    "                      [p] --> <FinalState>",
                    "                      [q] --> <FinalState>",
                    "                      [r] --> <FinalState>",
                    "                      [s] --> <FinalState>",
                    "                      [t] --> <FinalState>",
                    "                      [u] --> <FinalState>",
                    "                      [v] --> <FinalState>",
                    "                      [w] --> <FinalState>",
                    "                      [x] --> <FinalState>",
                    "                      [y] --> <FinalState>",
                    "                      [z] --> <FinalState>",
                    "                      [{] --> <FinalState>",
                    "                      [|] --> <FinalState>",
                    "                      [}] --> <FinalState>",
                    "                      [~] --> <FinalState>",
                    "            [c] --> <CharState>",
                    "                      [!] --> <FinalState>",
                    "                      [\"] --> <FinalState>",
                    "                      [#] --> <FinalState>",
                    "                      [$] --> <FinalState>",
                    "                      [%] --> <FinalState>",
                    "                      [&] --> <FinalState>",
                    "                      ['] --> <FinalState>",
                    "                      [(] --> <FinalState>",
                    "                      [)] --> <FinalState>",
                    "                      [*] --> <FinalState>",
                    "                      [+] --> <FinalState>",
                    "                      [,] --> <FinalState>",
                    "                      [-] --> <FinalState>",
                    "                      [.] --> <FinalState>",
                    "                      [/] --> <FinalState>",
                    "                      [0] --> <FinalState>",
                    "                      [1] --> <FinalState>",
                    "                      [2] --> <FinalState>",
                    "                      [3] --> <FinalState>",
                    "                      [4] --> <FinalState>",
                    "                      [5] --> <FinalState>",
                    "                      [6] --> <FinalState>",
                    "                      [7] --> <FinalState>",
                    "                      [8] --> <FinalState>",
                    "                      [9] --> <FinalState>",
                    "                      [:] --> <FinalState>",
                    "                      [;] --> <FinalState>",
                    "                      [<] --> <FinalState>",
                    "                      [=] --> <FinalState>",
                    "                      [>] --> <FinalState>",
                    "                      [?] --> <FinalState>",
                    "                      [@] --> <FinalState>",
                    "                      [A] --> <FinalState>",
                    "                      [B] --> <FinalState>",
                    "                      [C] --> <FinalState>",
                    "                      [D] --> <FinalState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <FinalState>",
                    "                      [H] --> <FinalState>",
                    "                      [I] --> <FinalState>",
                    "                      [J] --> <FinalState>",
                    "                      [K] --> <FinalState>",
                    "                      [L] --> <FinalState>",
                    "                      [M] --> <FinalState>",
                    "                      [N] --> <FinalState>",
                    "                      [O] --> <FinalState>",
                    "                      [P] --> <FinalState>",
                    "                      [Q] --> <FinalState>",
                    "                      [R] --> <FinalState>",
                    "                      [S] --> <FinalState>",
                    "                      [T] --> <FinalState>",
                    "                      [U] --> <FinalState>",
                    "                      [V] --> <FinalState>",
                    "                      [W] --> <FinalState>",
                    "                      [X] --> <FinalState>",
                    "                      [Y] --> <FinalState>",
                    "                      [Z] --> <FinalState>",
                    "                      [[] --> <FinalState>",
                    "                      [\\] --> <FinalState>",
                    "                      []] --> <FinalState>",
                    "                      [^] --> <FinalState>",
                    "                      [_] --> <FinalState>",
                    "                      [`] --> <FinalState>",
                    "                      [a] --> <FinalState>",
                    "                      [b] --> <FinalState>",
                    "                      [c] --> <FinalState>",
                    "                      [d] --> <FinalState>",
                    "                      [e] --> <FinalState>",
                    "                      [f] --> <FinalState>",
                    "                      [g] --> <FinalState>",
                    "                      [h] --> <FinalState>",
                    "                      [i] --> <FinalState>",
                    "                      [j] --> <FinalState>",
                    "                      [k] --> <FinalState>",
                    "                      [l] --> <FinalState>",
                    "                      [m] --> <FinalState>",
                    "                      [n] --> <FinalState>",
                    "                      [o] --> <FinalState>",
                    "                      [p] --> <FinalState>",
                    "                      [q] --> <FinalState>",
                    "                      [r] --> <FinalState>",
                    "                      [s] --> <FinalState>",
                    "                      [t] --> <FinalState>",
                    "                      [u] --> <FinalState>",
                    "                      [v] --> <FinalState>",
                    "                      [w] --> <FinalState>",
                    "                      [x] --> <FinalState>",
                    "                      [y] --> <FinalState>",
                    "                      [z] --> <FinalState>",
                    "                      [{] --> <FinalState>",
                    "                      [|] --> <FinalState>",
                    "                      [}] --> <FinalState>",
                    "                      [~] --> <FinalState>",
                    "            [d] --> <FinalState>",
                    "            [e] --> <FinalState>",
                    "            [f] --> <FinalState>",
                    "            [g] --> <FinalState>",
                    "            [h] --> <FinalState>",
                    "            [i] --> <FinalState>",
                    "            [j] --> <FinalState>",
                    "            [k] --> <FinalState>",
                    "            [l] --> <FinalState>",
                    "            [m] --> <FinalState>",
                    "            [n] --> <FinalState>",
                    "            [o] --> <FinalState>",
                    "            [p] --> <FinalState>",
                    "            [q] --> <FinalState>",
                    "            [r] --> <FinalState>",
                    "            [s] --> <FinalState>",
                    "            [t] --> <FinalState>",
                    "            [u] --> <FinalState>",
                    "            [v] --> <FinalState>",
                    "            [w] --> <FinalState>",
                    "            [x] --> <FinalState>",
                    "            [y] --> <FinalState>",
                    "            [z] --> <FinalState>",
                    "            [{] --> <FinalState>",
                    "            [|] --> <FinalState>",
                    "            [}] --> <FinalState>",
                    "            [~] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterMultipleSequences() {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A'),
                new EscapeSequenceAttribute('B', 'C'),
                new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'E'),
                new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'F'),
                new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'G', 'H'),
                new EscapeSequenceAttribute('D', EscapeSequenceParamType.Numeric, 'G', 'J'),
                new EscapeSequenceAttribute('D', 'S', EscapeSequenceParamType.Numeric, 'E'),
                new EscapeSequenceAttribute('D', 'S', EscapeSequenceParamType.Numeric, 'F'),
                new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'L'),
                new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'M'),
                new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'N', 'O'),
                new EscapeSequenceAttribute('K', EscapeSequenceParamType.Text, 'N', 'P'),
                new EscapeSequenceAttribute('Q', EscapeSequenceParamType.SinglePrintable),
                new EscapeSequenceAttribute('Q', 'R', EscapeSequenceParamType.SinglePrintable),
                new EscapeSequenceAttribute(ControlCode.CSI, 'Q'),
                new EscapeSequenceAttribute(ControlCode.CSI, 'R')
            );

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
                    "  [Q] --> <CharState>",
                    "            [!] --> <FinalState>",
                    "            [\"] --> <FinalState>",
                    "            [#] --> <FinalState>",
                    "            [$] --> <FinalState>",
                    "            [%] --> <FinalState>",
                    "            [&] --> <FinalState>",
                    "            ['] --> <FinalState>",
                    "            [(] --> <FinalState>",
                    "            [)] --> <FinalState>",
                    "            [*] --> <FinalState>",
                    "            [+] --> <FinalState>",
                    "            [,] --> <FinalState>",
                    "            [-] --> <FinalState>",
                    "            [.] --> <FinalState>",
                    "            [/] --> <FinalState>",
                    "            [0] --> <FinalState>",
                    "            [1] --> <FinalState>",
                    "            [2] --> <FinalState>",
                    "            [3] --> <FinalState>",
                    "            [4] --> <FinalState>",
                    "            [5] --> <FinalState>",
                    "            [6] --> <FinalState>",
                    "            [7] --> <FinalState>",
                    "            [8] --> <FinalState>",
                    "            [9] --> <FinalState>",
                    "            [:] --> <FinalState>",
                    "            [;] --> <FinalState>",
                    "            [<] --> <FinalState>",
                    "            [=] --> <FinalState>",
                    "            [>] --> <FinalState>",
                    "            [?] --> <FinalState>",
                    "            [@] --> <FinalState>",
                    "            [A] --> <FinalState>",
                    "            [B] --> <FinalState>",
                    "            [C] --> <FinalState>",
                    "            [D] --> <FinalState>",
                    "            [E] --> <FinalState>",
                    "            [F] --> <FinalState>",
                    "            [G] --> <FinalState>",
                    "            [H] --> <FinalState>",
                    "            [I] --> <FinalState>",
                    "            [J] --> <FinalState>",
                    "            [K] --> <FinalState>",
                    "            [L] --> <FinalState>",
                    "            [M] --> <FinalState>",
                    "            [N] --> <FinalState>",
                    "            [O] --> <FinalState>",
                    "            [P] --> <FinalState>",
                    "            [Q] --> <FinalState>",
                    "            [R] --> <CharState>",
                    "                      [!] --> <FinalState>",
                    "                      [\"] --> <FinalState>",
                    "                      [#] --> <FinalState>",
                    "                      [$] --> <FinalState>",
                    "                      [%] --> <FinalState>",
                    "                      [&] --> <FinalState>",
                    "                      ['] --> <FinalState>",
                    "                      [(] --> <FinalState>",
                    "                      [)] --> <FinalState>",
                    "                      [*] --> <FinalState>",
                    "                      [+] --> <FinalState>",
                    "                      [,] --> <FinalState>",
                    "                      [-] --> <FinalState>",
                    "                      [.] --> <FinalState>",
                    "                      [/] --> <FinalState>",
                    "                      [0] --> <FinalState>",
                    "                      [1] --> <FinalState>",
                    "                      [2] --> <FinalState>",
                    "                      [3] --> <FinalState>",
                    "                      [4] --> <FinalState>",
                    "                      [5] --> <FinalState>",
                    "                      [6] --> <FinalState>",
                    "                      [7] --> <FinalState>",
                    "                      [8] --> <FinalState>",
                    "                      [9] --> <FinalState>",
                    "                      [:] --> <FinalState>",
                    "                      [;] --> <FinalState>",
                    "                      [<] --> <FinalState>",
                    "                      [=] --> <FinalState>",
                    "                      [>] --> <FinalState>",
                    "                      [?] --> <FinalState>",
                    "                      [@] --> <FinalState>",
                    "                      [A] --> <FinalState>",
                    "                      [B] --> <FinalState>",
                    "                      [C] --> <FinalState>",
                    "                      [D] --> <FinalState>",
                    "                      [E] --> <FinalState>",
                    "                      [F] --> <FinalState>",
                    "                      [G] --> <FinalState>",
                    "                      [H] --> <FinalState>",
                    "                      [I] --> <FinalState>",
                    "                      [J] --> <FinalState>",
                    "                      [K] --> <FinalState>",
                    "                      [L] --> <FinalState>",
                    "                      [M] --> <FinalState>",
                    "                      [N] --> <FinalState>",
                    "                      [O] --> <FinalState>",
                    "                      [P] --> <FinalState>",
                    "                      [Q] --> <FinalState>",
                    "                      [R] --> <FinalState>",
                    "                      [S] --> <FinalState>",
                    "                      [T] --> <FinalState>",
                    "                      [U] --> <FinalState>",
                    "                      [V] --> <FinalState>",
                    "                      [W] --> <FinalState>",
                    "                      [X] --> <FinalState>",
                    "                      [Y] --> <FinalState>",
                    "                      [Z] --> <FinalState>",
                    "                      [[] --> <FinalState>",
                    "                      [\\] --> <FinalState>",
                    "                      []] --> <FinalState>",
                    "                      [^] --> <FinalState>",
                    "                      [_] --> <FinalState>",
                    "                      [`] --> <FinalState>",
                    "                      [a] --> <FinalState>",
                    "                      [b] --> <FinalState>",
                    "                      [c] --> <FinalState>",
                    "                      [d] --> <FinalState>",
                    "                      [e] --> <FinalState>",
                    "                      [f] --> <FinalState>",
                    "                      [g] --> <FinalState>",
                    "                      [h] --> <FinalState>",
                    "                      [i] --> <FinalState>",
                    "                      [j] --> <FinalState>",
                    "                      [k] --> <FinalState>",
                    "                      [l] --> <FinalState>",
                    "                      [m] --> <FinalState>",
                    "                      [n] --> <FinalState>",
                    "                      [o] --> <FinalState>",
                    "                      [p] --> <FinalState>",
                    "                      [q] --> <FinalState>",
                    "                      [r] --> <FinalState>",
                    "                      [s] --> <FinalState>",
                    "                      [t] --> <FinalState>",
                    "                      [u] --> <FinalState>",
                    "                      [v] --> <FinalState>",
                    "                      [w] --> <FinalState>",
                    "                      [x] --> <FinalState>",
                    "                      [y] --> <FinalState>",
                    "                      [z] --> <FinalState>",
                    "                      [{] --> <FinalState>",
                    "                      [|] --> <FinalState>",
                    "                      [}] --> <FinalState>",
                    "                      [~] --> <FinalState>",
                    "            [S] --> <FinalState>",
                    "            [T] --> <FinalState>",
                    "            [U] --> <FinalState>",
                    "            [V] --> <FinalState>",
                    "            [W] --> <FinalState>",
                    "            [X] --> <FinalState>",
                    "            [Y] --> <FinalState>",
                    "            [Z] --> <FinalState>",
                    "            [[] --> <FinalState>",
                    "            [\\] --> <FinalState>",
                    "            []] --> <FinalState>",
                    "            [^] --> <FinalState>",
                    "            [_] --> <FinalState>",
                    "            [`] --> <FinalState>",
                    "            [a] --> <FinalState>",
                    "            [b] --> <FinalState>",
                    "            [c] --> <FinalState>",
                    "            [d] --> <FinalState>",
                    "            [e] --> <FinalState>",
                    "            [f] --> <FinalState>",
                    "            [g] --> <FinalState>",
                    "            [h] --> <FinalState>",
                    "            [i] --> <FinalState>",
                    "            [j] --> <FinalState>",
                    "            [k] --> <FinalState>",
                    "            [l] --> <FinalState>",
                    "            [m] --> <FinalState>",
                    "            [n] --> <FinalState>",
                    "            [o] --> <FinalState>",
                    "            [p] --> <FinalState>",
                    "            [q] --> <FinalState>",
                    "            [r] --> <FinalState>",
                    "            [s] --> <FinalState>",
                    "            [t] --> <FinalState>",
                    "            [u] --> <FinalState>",
                    "            [v] --> <FinalState>",
                    "            [w] --> <FinalState>",
                    "            [x] --> <FinalState>",
                    "            [y] --> <FinalState>",
                    "            [z] --> <FinalState>",
                    "            [{] --> <FinalState>",
                    "            [|] --> <FinalState>",
                    "            [}] --> <FinalState>",
                    "            [~] --> <FinalState>",
                    "  [0x9b] --> <CharState>",
                    "               [Q] --> <FinalState>",
                    "               [R] --> <FinalState>",
                },
                dump
            );
        }

        [Test]
        public void TestRegisterConflictFinalState() {
            TestRegisterConflictCore(
                new EscapeSequenceAttribute[] {
                    new EscapeSequenceAttribute('A'),
                    new EscapeSequenceAttribute('A'),
                }
            );
        }

        private void TestRegisterConflictCore(EscapeSequenceAttribute[] attrs) {
            EscapeSequenceEngineBase.CharState state = new EscapeSequenceEngineBase.CharState();

            int index = 0;
            Assert.Throws<ArgumentException>(() => {
                new EscapeSequenceEngineBase.StateMachineBuilder(state).RegisterHandlers(
                    typeof(DummyClass4),
                    (method) => {
                        if (method.Name.StartsWith("DummyAction") && index < attrs.Length) {
                            return new List<EscapeSequenceAttribute>() { attrs[index++] };
                        }
                        else {
                            return new List<EscapeSequenceAttribute>();
                        }
                    }
                );
            });

            // exception should be raised for the last EscapeSequenceAttribute
            Assert.AreEqual(attrs.Length, index);
        }

        [Test]
        public void TestRegisterConflictFinalStateSpecialControlChar() {
            TestRegisterConflictCore(
                new EscapeSequenceAttribute[] {
                    new EscapeSequenceAttribute(ControlCode.ESC, '['), // equivalent to CSI, but CSI(0x9b) is not registered
                    new EscapeSequenceAttribute(ControlCode.CSI), // attempt to register sequence ESC+[ but it fails
                }
            );
        }

        [Test]
        public void TestRegisterConflictCharState() {
            TestRegisterConflictCore(
                new EscapeSequenceAttribute[] {
                    new EscapeSequenceAttribute('A', EscapeSequenceParamType.Numeric, 'B'),
                    new EscapeSequenceAttribute('A', 'B'),
                }
            );
        }

        [Test]
        public void TestRegisterConflictCharStateSpecialControlChar() {
            TestRegisterConflictCore(
                new EscapeSequenceAttribute[] {
                    new EscapeSequenceAttribute(ControlCode.ESC, '[', EscapeSequenceParamType.Numeric, 'z'), // equivalent to CSI, but CSI(0x9b) is not registered
                    new EscapeSequenceAttribute(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'x'),
                        // attempt to register sequence ESC+[ but it fails becuase another NumericParamsState exists
                }
            );
        }

        [Test]
        public void TestRegisterConflictTextParamState() {
            TestRegisterConflictCore(
                new EscapeSequenceAttribute[] {
                    new EscapeSequenceAttribute('A', 'B'),
                    new EscapeSequenceAttribute('A', EscapeSequenceParamType.Text, 'C'),
                }
            );
        }

        [Test]
        public void TestRegisterConflictSinglePrintable() {
            TestRegisterConflictCore(
                new EscapeSequenceAttribute[] {
                    new EscapeSequenceAttribute('A', 'B', EscapeSequenceParamType.SinglePrintable),
                    new EscapeSequenceAttribute('A', 'B', EscapeSequenceParamType.SinglePrintable),
                }
            );
        }

        #endregion // Registration

        #region Accept

        [Test]
        public void TestCharStateAccept() {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.CharState));

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));
        }

        [Test]
        public void TestCharStateAcceptNotAccepted() {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'C');
            Assert.Null(s);
        }

        public static object[] TestNumericParamStateAcceptPatterns =
            new object[] {
                new object[] {"", new int?[] { null }, new int?[][] { null }},
                new object[] {"12;34;56", new int?[] { 12, 34, 56 }, new int?[][] { null, null, null }},
                new object[] {"12:34:56", new int?[] { null }, new int?[][] { new int?[] { 12, 34, 56 } }},
                new object[] {";", new int?[] { null, null }, new int?[][] { null, null }},
            };

        [TestCaseSource("TestNumericParamStateAcceptPatterns")]
        public void TestNumericParamStateAccept(string parameters, int?[] expectedNumericParams, int?[][] expectedCombinationParams) {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.Numeric, 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            foreach (char ch in parameters) {
                s = s.Accept(context, ch);
            }

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));

            NumericParams p = context.GetNumericParams();
            int?[] numericParams = p.GetNumericParametersForTesting();
            int?[][] combinationParams = p.GetIntegerCombinationsForTesting();
            Assert.AreEqual(expectedNumericParams, numericParams);
            Assert.AreEqual(expectedCombinationParams, combinationParams);

            Assert.AreEqual(parameters, context.GetTextParam());
            Assert.AreEqual(("A" + parameters + "B").ToCharArray(), context.GetSequence());
        }

        [TestCase("C")]
        [TestCase("12;34X")]
        public void TestNumericParamStateAcceptNotAccepted(string parameters) {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.Numeric, 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            for (int i = 0; i < parameters.Length - 1; i++) {
                s = s.Accept(context, parameters[i]);
            }
            s = s.Accept(context, parameters[parameters.Length - 1]);
            Assert.Null(s);
            Assert.AreEqual(("A" + parameters.Substring(0, parameters.Length - 1)).ToCharArray(), context.GetSequence());
        }

        [TestCase("")]
        [TestCase("a")]
        [TestCase("abc\r\n\tdef")]
        public void TestTextParamStateAccept(string parameters) {
            parameters = TestUtil.ConvertArg(parameters);

            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.Text, 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            foreach (char ch in parameters) {
                s = s.Accept(context, ch);
            }

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));
            Assert.AreEqual(parameters, context.GetTextParam());
            Assert.AreEqual(("A" + parameters + "B").ToCharArray(), context.GetSequence());
        }

        [TestCase("abc\\u0000")]
        [TestCase("abc\\u001f")]
        [TestCase("abc\\u0007")]
        [TestCase("abc\\u000e")]
        [TestCase("abc\\u0080")]
        [TestCase("abc\\u009c")]
        public void TestTextParamStateNotAccept(string parameters) {
            parameters = TestUtil.ConvertArg(parameters);

            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.Text, 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            for (int i = 0; i < parameters.Length - 1; i++) {
                s = s.Accept(context, parameters[i]);
            }
            s = s.Accept(context, parameters[parameters.Length - 1]);
            Assert.Null(s);
            Assert.AreEqual(("A" + parameters.Substring(0, parameters.Length - 1)).ToCharArray(), context.GetSequence());
        }

        [TestCase("")]
        [TestCase("a")]
        [TestCase("abc\r\n\tdef")]
        [TestCase("abc\\u0080")]
        [TestCase("abc\\u009c")]
        [TestCase("\\u00e2\\u0098\\u0095\\u00f0\\u009f\\u008d\\u00b0")]
        public void TestControlStringTextParamStateAccept(string parameters) {
            parameters = TestUtil.ConvertArg(parameters);

            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.ControlString, 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            foreach (char ch in parameters) {
                s = s.Accept(context, ch);
            }

            s = s.Accept(context, 'B');
            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));
            Assert.AreEqual(parameters, context.GetTextParam());
            Assert.AreEqual(("A" + parameters + "B").ToCharArray(), context.GetSequence());
        }

        [TestCase("abc\\u0000")]
        [TestCase("abc\\u001f")]
        [TestCase("abc\\u0007")]
        [TestCase("abc\\u000e")]
        public void TestControlStringTextParamStateNotAccept(string parameters) {
            parameters = TestUtil.ConvertArg(parameters);

            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.ControlString, 'B')
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            for (int i = 0; i < parameters.Length - 1; i++) {
                s = s.Accept(context, parameters[i]);
            }
            s = s.Accept(context, parameters[parameters.Length - 1]);
            Assert.Null(s);
            Assert.AreEqual(("A" + parameters.Substring(0, parameters.Length - 1)).ToCharArray(), context.GetSequence());
        }

        [TestCase('!')]
        [TestCase('~')]
        [TestCase('0')]
        [TestCase('9')]
        [TestCase('a')]
        [TestCase('z')]
        [TestCase('A')]
        [TestCase('Z')]
        public void TestNextPrintableParamStateAccept(char paramChar) {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.SinglePrintable)
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            s = s.Accept(context, paramChar);

            Assert.That(s.GetType() == typeof(EscapeSequenceEngineBase.FinalState));
            Assert.AreEqual(paramChar, context.GetLastChar());
            Assert.AreEqual(new char[] { 'A', paramChar }, context.GetSequence());
        }

        [TestCase('\u0000')]
        [TestCase(' ')]
        [TestCase('\u007f')]
        [TestCase('\u03b1')]
        public void TestNextPrintableParamStateNotAccept(char paramChar) {
            EscapeSequenceEngineBase.CharState state = Build(
                new EscapeSequenceAttribute('A', EscapeSequenceParamType.SinglePrintable)
            );

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');

            s = s.Accept(context, paramChar);

            Assert.Null(s);
            Assert.AreEqual(new char[] { 'A' }, context.GetSequence());
        }

        [Test]
        public void TestFinalStateAcceptNotAccepted() {
            EscapeSequenceEngineBase.FinalState state = new EscapeSequenceEngineBase.FinalState((obj, ctx) => {
            });

            EscapeSequenceEngineBase.Context context = new EscapeSequenceEngineBase.Context();

            EscapeSequenceEngineBase.IState s = state.Accept(context, 'A');
            Assert.Null(s);
            Assert.AreEqual(new char[0], context.GetSequence());
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
            var p = new NumericParams(new int?[] { 10, 11, null, 13 }, new int?[][] { null, null, null, null });

            Assert.IsTrue(p.IsSingleInteger(0));
            Assert.IsTrue(p.IsSingleInteger(1));
            Assert.IsFalse(p.IsSingleInteger(2));
            Assert.IsTrue(p.IsSingleInteger(3));
            Assert.IsFalse(p.IsSingleInteger(4));
            Assert.IsFalse(p.IsSingleInteger(-1));
        }

        [Test]
        public void TestIsIntegerCombination() {
            var p = new NumericParams(new int?[] { null, null, null, null }, new int?[][] { new int?[] { 21, 22 }, null, null, new int?[] { 23, 24 } });

            Assert.IsTrue(p.IsIntegerCombination(0));
            Assert.IsFalse(p.IsIntegerCombination(1));
            Assert.IsFalse(p.IsIntegerCombination(2));
            Assert.IsTrue(p.IsIntegerCombination(3));
            Assert.IsFalse(p.IsIntegerCombination(4));
            Assert.IsFalse(p.IsIntegerCombination(-1));
        }

        [Test]
        public void TestGet() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int?[][] { null, null, null, null, null });

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
            var p = new NumericParams(new int?[] { null, null, null, null, null }, new int?[][] { new int?[] { 21, 22, null, 23 }, new int?[] { 24 }, null, new int?[] { 25, 26 }, null });

            Assert.AreEqual(new int?[] { 21, 22, null, 23 }, p.GetIntegerCombination(0));
            Assert.AreEqual(new int?[] { 24 }, p.GetIntegerCombination(1));
            Assert.AreEqual(new int?[] { }, p.GetIntegerCombination(2));
            Assert.AreEqual(new int?[] { }, p.GetIntegerCombination(-1));
        }

        [Test]
        public void TestEnumerate() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int?[][] { null, null, null, null, null });

            Assert.AreEqual(new int?[] { 10, 11, null, 13, null }, p.Enumerate().ToArray());
        }

        [Test]
        public void TestEnumerateWithDefault() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int?[][] { null, null, null, null, null });

            Assert.AreEqual(new int?[] { 10, 11, 99, 13, 99 }, p.EnumerateWithDefault(99).ToArray());
        }

        [Test]
        public void TestEnumerateWithoutNull() {
            var p = new NumericParams(new int?[] { 10, 11, null, 13, null }, new int?[][] { null, null, null, null, null });

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
                    // added by RegisterMissingHandlers()
                    "  [0x1b] --> <CharState>",
                    "               [P] --> <CharState>",
                    "                         [0x1b] --> <NonConsumingFinalState>", // DCS ESC
                    "               [X] --> <CharState>",
                    "                         [0x1b] --> <NonConsumingFinalState>", // SOS ESC
                    "               []] --> <CharState>",
                    "                         [0x1b] --> <NonConsumingFinalState>", // OSC ESC
                    "               [^] --> <CharState>",
                    "                         [0x1b] --> <NonConsumingFinalState>", // PM ESC
                    "               [_] --> <CharState>",
                    "                         [0x1b] --> <NonConsumingFinalState>", // APC ESC
                    //
                    // from ValidHandlers
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
                    //
                    // added by RegisterMissingHandlers()
                    "  [0x90] --> <CharState>",
                    "               [0x1b] --> <NonConsumingFinalState>", // DCS ESC
                    "  [0x98] --> <CharState>",
                    "               [0x1b] --> <NonConsumingFinalState>", // SOS ESC
                    "  [0x9d] --> <CharState>",
                    "               [0x1b] --> <NonConsumingFinalState>", // OSC ESC
                    "  [0x9e] --> <CharState>",
                    "               [0x1b] --> <NonConsumingFinalState>", // PM ESC
                    "  [0x9f] --> <CharState>",
                    "               [0x1b] --> <NonConsumingFinalState>", // APC ESC
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

        const string APC8 = "\\u009f";
        const string APC7 = "\\u001b_";
        const string CSI8 = "\\u009b";
        const string CSI7 = "\\u001b[";
        const string DCS8 = "\\u0090";
        const string DCS7 = "\\u001bP";
        const string OSC8 = "\\u009d";
        const string OSC7 = "\\u001b]";
        const string PM8 = "\\u009e";
        const string PM7 = "\\u001b^";
        const string SOS8 = "\\u0098";
        const string SOS7 = "\\u001bX";
        const string ST8 = "\\u009c";
        const string ST7 = "\\u001b\\";
        const string ESC = "\\u001b";
        const string CAN = "\\u0018";
        const string SUB = "\\u001a";
        const string BEL = "\\u0007";
        const string BS = "\\u0008";

        public static object[] TestProcessPatterns = new object[] {
            new object[] { APC8, new object[] { "Handle_APC" }},
            new object[] { APC7, new object[] { "Handle_APC" }},

            new object[] { CSI8 + ST8, new object[] { "Handle_CSI_ST", null }},
            new object[] { CSI7 + ST8, new object[] { "Handle_CSI_ST", null }},
            new object[] { CSI8 + ST7, new object[] { "Handle_CSI_ST", null }},
            new object[] { CSI7 + ST7, new object[] { "Handle_CSI_ST", null }},
            new object[] { CSI8 + "11;22;33" + ST8, new object[] { "Handle_CSI_ST", 11, 22, 33 }},
            new object[] { CSI7 + "11;22;33" + ST8, new object[] { "Handle_CSI_ST", 11, 22, 33 }},
            new object[] { CSI8 + "11;22;33" + ST7, new object[] { "Handle_CSI_ST", 11, 22, 33 }},
            new object[] { CSI7 + "11;22;33" + ST7, new object[] { "Handle_CSI_ST", 11, 22, 33 }},

            new object[] { CSI8 + "X" + ST8, new object[] { "Handle_CSI_X_ST", null }},
            new object[] { CSI7 + "X" + ST8, new object[] { "Handle_CSI_X_ST", null }},
            new object[] { CSI8 + "X" + ST7, new object[] { "Handle_CSI_X_ST", null }},
            new object[] { CSI7 + "X" + ST7, new object[] { "Handle_CSI_X_ST", null }},
            new object[] { CSI8 + "X11;22;33" + ST8, new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},
            new object[] { CSI7 + "X11;22;33" + ST8, new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},
            new object[] { CSI8 + "X11;22;33" + ST7, new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},
            new object[] { CSI7 + "X11;22;33" + ST7, new object[] { "Handle_CSI_X_ST", 11, 22, 33 }},

            new object[] { CSI8 + "XZ" + ST8, new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { CSI7 + "XZ" + ST8, new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { CSI8 + "XZ" + ST7, new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { CSI7 + "XZ" + ST7, new object[] { "Handle_CSI_X_Z_ST", null }},
            new object[] { CSI8 + "X11;22;33Z" + ST8, new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},
            new object[] { CSI7 + "X11;22;33Z" + ST8, new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},
            new object[] { CSI8 + "X11;22;33Z" + ST7, new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},
            new object[] { CSI7 + "X11;22;33Z" + ST7, new object[] { "Handle_CSI_X_Z_ST", 11, 22, 33 }},

            new object[] { CSI8 + "Y" + ST8, new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { CSI7 + "Y" + ST8, new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { CSI8 + "Y" + ST7, new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { CSI7 + "Y" + ST7, new object[] { "Handle_CSI_Y_ST", null }},
            new object[] { CSI8 + "Y11;22;33" + ST8, new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},
            new object[] { CSI7 + "Y11;22;33" + ST8, new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},
            new object[] { CSI8 + "Y11;22;33" + ST7, new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},
            new object[] { CSI7 + "Y11;22;33" + ST7, new object[] { "Handle_CSI_Y_ST", 11, 22, 33 }},

            new object[] { CSI8 + "YZ" + ST8, new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { CSI7 + "YZ" + ST8, new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { CSI8 + "YZ" + ST7, new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { CSI7 + "YZ" + ST7, new object[] { "Handle_CSI_Y_Z_ST", null }},
            new object[] { CSI8 + "Y11;22;33Z" + ST8, new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},
            new object[] { CSI7 + "Y11;22;33Z" + ST8, new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},
            new object[] { CSI8 + "Y11;22;33Z" + ST7, new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},
            new object[] { CSI7 + "Y11;22;33Z" + ST7, new object[] { "Handle_CSI_Y_Z_ST", 11, 22, 33 }},

            new object[] { OSC8 + ST8, new object[] { "Handle_OSC_ST", "" }}, // Handle_OSC_ST() use EscapeSequenceParamType.Text, so 8bit ST is used as terminal character
            new object[] { OSC7 + ST8, new object[] { "Handle_OSC_ST", "" }},
            new object[] { OSC8 + ST7, new object[] { "Handle_OSC_ST", "" }},
            new object[] { OSC7 + ST7, new object[] { "Handle_OSC_ST", "" }},
            new object[] { OSC8 + "foo" + ST8, new object[] { "Handle_OSC_ST", "foo" }},
            new object[] { OSC7 + "foo" + ST8, new object[] { "Handle_OSC_ST", "foo" }},
            new object[] { OSC8 + "foo" + ST7, new object[] { "Handle_OSC_ST", "foo" }},
            new object[] { OSC7 + "foo" + ST7, new object[] { "Handle_OSC_ST", "foo" }},
        };

        [TestCaseSource("TestProcessPatterns")]
        public void TestProcess(string input, object[] expected) {
            input = TestUtil.ConvertArg(input);

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

        [TestCase(CSI7 + "1;2;3XYZ", "YZ", new string[] { })] // final byte is 'X'
        [TestCase(CSI8 + "1;2;3XYZ", "YZ", new string[] { })] // final byte is 'X'
        [TestCase(CSI7 + "XYZ", "YZ", new string[] { })] // final byte is 'X'
        [TestCase(CSI7 + "!XYZ", "YZ", new string[] { })] // also ignore intermediate bytes
        [TestCase(CSI7 + "1;2;3\\u000aXYZ", "\\u000aXYZ", new string[] { })] // U+000a is not a final byte or intermediate byte
        [TestCase(CSI7 + "1;2;3X\\u001b[1;2;3A\\u001b[1;2;3Ya", "a", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        [TestCase(CSI7 + "1;2;3\\u001b[1;2;3A", "", new string[] { "Handle CSI A" })] // restart escape sequence by ESC
        public void TestIgnoreCSI(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            TestIgnoreControlString(input, expectedNotAcceptedText, expectedCalled);
        }

        [TestCase(APC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(APC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(APC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(APC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(APC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(APC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(APC7 + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit)
        [TestCase(APC7 + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN
        [TestCase(APC7 + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB
        [TestCase(APC7 + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(APC7 + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(APC7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        [TestCase(APC7 + "ABC" + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(APC7 + "ABC" + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(APC7 + "ABC" + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        public void TestIgnoreAPC(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            TestIgnoreControlString(input, expectedNotAcceptedText, expectedCalled);
        }

        [TestCase(DCS7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(DCS7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(DCS7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(DCS8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(DCS8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(DCS8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(DCS7 + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit)
        [TestCase(DCS7 + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN
        [TestCase(DCS7 + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB
        [TestCase(DCS7 + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(DCS7 + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(DCS7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        [TestCase(DCS7 + "ABC" + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(DCS7 + "ABC" + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(DCS7 + "ABC" + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        public void TestIgnoreDCS(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            TestIgnoreControlString(input, expectedNotAcceptedText, expectedCalled);
        }

        [TestCase(OSC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(OSC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(OSC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(OSC7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + BEL + "XYZ", "XYZ", new string[] { })] // terminated by BEL, 8bit ST is ignored as UTF-8 data
        [TestCase(OSC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(OSC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(OSC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(OSC8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + BEL + "XYZ", "XYZ", new string[] { })] // terminated by BEL, 8bit ST is ignored as UTF-8 data
        [TestCase(OSC7 + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit)
        [TestCase(OSC7 + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN
        [TestCase(OSC7 + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB
        [TestCase(OSC7 + BEL + "XYZ", "XYZ", new string[] { })] // terminated by BEL
        [TestCase(OSC7 + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(OSC7 + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(OSC7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        [TestCase(OSC7 + "ABC" + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(OSC7 + "ABC" + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(OSC7 + "ABC" + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        public void TestIgnoreOSC(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            TestIgnoreControlString(input, expectedNotAcceptedText, expectedCalled);
        }

        [TestCase(PM7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(PM7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(PM7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(PM8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(PM8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(PM8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(PM7 + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit)
        [TestCase(PM7 + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN
        [TestCase(PM7 + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB
        [TestCase(PM7 + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(PM7 + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(PM7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        [TestCase(PM7 + "ABC" + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(PM7 + "ABC" + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(PM7 + "ABC" + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        public void TestIgnorePM(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            TestIgnoreControlString(input, expectedNotAcceptedText, expectedCalled);
        }

        [TestCase(SOS7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(SOS7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(SOS7 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(SOS8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ESC of ST (7bit), 8bit ST is ignored as UTF-8 data
        [TestCase(SOS8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN, 8bit ST is ignored as UTF-8 data
        [TestCase(SOS8 + "ABC\\u0008\\u000d\\u0080\\u00f4DEF" + ST8 + "GHI" + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB, 8bit ST is ignored as UTF-8 data
        [TestCase(SOS7 + ST7 + "XYZ", "XYZ", new string[] { "Handle ST" })] // terminated by ST (7bit)
        [TestCase(SOS7 + CAN + "XYZ", "XYZ", new string[] { })] // terminated by CAN
        [TestCase(SOS7 + SUB + "XYZ", "XYZ", new string[] { })] // terminated by SUB
        [TestCase(SOS7 + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(SOS7 + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(SOS7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        [TestCase(SOS7 + "ABC" + ST7 + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle ST", "Handle CSI A" })] // ST (7bit) and the subsequent escape sequence must be handled
        [TestCase(SOS7 + "ABC" + ESC + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // first sequence is aborted by ESC after ESC, and restart sequence
        [TestCase(SOS7 + "ABC" + CSI7 + "1;2;3AXYZ", "XYZ", new string[] { "Handle CSI A" })] // the subsequent escape sequence must be handled
        public void TestIgnoreSOS(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            TestIgnoreControlString(input, expectedNotAcceptedText, expectedCalled);
        }

        private void TestIgnoreControlString(string input, string expectedNotAcceptedText, string[] expectedCalled) {
            input = TestUtil.ConvertArg(input);
            expectedNotAcceptedText = TestUtil.ConvertArg(expectedNotAcceptedText);

            var engine = new EscapeSequenceEngine<ControlStringHandlers>();

            ControlStringHandlers instance = new ControlStringHandlers();

            List<char> notAccepted = new List<char>();

            var enumerator = input.GetEnumerator();

            while (enumerator.MoveNext()) {
                char ch = enumerator.Current;
                if (!engine.Process(instance, ch)) {
                    notAccepted.Add(ch);
                    break;
                }
            }

            while (enumerator.MoveNext()) {
                char ch = enumerator.Current;
                Assert.IsFalse(engine.Process(instance, ch));
                notAccepted.Add(ch);
            }

            string notAcceptedText = new String(notAccepted.ToArray());

            Assert.AreEqual(expectedNotAcceptedText, notAcceptedText);
            Assert.AreEqual(expectedCalled, instance.Called.ToArray());
        }


        public static object[] TestControlCharacterInterruptPatterns = new object[] {
            new object[] {
                ESC + BS + "[" + "12;34;56" + ST7,
                new object[][] {
                    new object[] { "Handle_BS" },
                    new object[] { "Handle_CSI_ST", 12, 34, 56 },
                },
            },
            new object[] {
                CSI7 + BS + "12;34;56" + ST7,
                new object[][] {
                    new object[] { "Handle_BS" },
                    new object[] { "Handle_CSI_ST", 12, 34, 56 },
                },
            },
            new object[] {
                CSI7 + "1" + BS + "2;34;56" + ST7,
                new object[][] {
                    new object[] { "Handle_BS" },
                    new object[] { "Handle_CSI_ST", 12, 34, 56 },
                },
            },
            new object[] {
                CSI7 + "12;" + BS + "34;56" + ST7,
                new object[][] {
                    new object[] { "Handle_BS" },
                    new object[] { "Handle_CSI_ST", 12, 34, 56 },
                },
            },
            new object[] {
                CSI7 + "12;34;56" + BS + ST7,
                new object[][] {
                    new object[] { "Handle_BS" },
                    new object[] { "Handle_CSI_ST", 12, 34, 56 },
                },
            },
            new object[] {
                CSI7 + "12;34;56" + ESC + BS + "\\",
                new object[][] {
                    new object[] { "Handle_BS" },
                    new object[] { "Handle_CSI_ST", 12, 34, 56 },
                },
            },
        };

        [TestCaseSource("TestControlCharacterInterruptPatterns")]
        public void TestControlCharacterInterrupt(string input, object[][] expectedCalls) {
            input = TestUtil.ConvertArg(input);

            var engine = new EscapeSequenceEngine<ValidHandlersForCheckFinalState>();

            var instance = new ValidHandlersForCheckFinalState();

            for (int i = 0; i < input.Length; i++) {
                Assert.IsTrue(engine.Process(instance, input[i]));
            }

            Assert.AreEqual(expectedCalls, instance.Calls.ToArray());
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

        [EscapeSequence(ControlCode.BS)]
        private void Handle_BS() {
            Calls.Add(new object[] { "Handle_BS" });
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

    class ControlStringHandlers {

        public readonly List<string> Called = new List<string>();

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'A')]
        private void CSIA() {
            Called.Add("Handle CSI A");
        }

        [EscapeSequence(ControlCode.CSI, EscapeSequenceParamType.Numeric, 'B')]
        private void CSIB() {
            Called.Add("Handle CSI B");
        }

        [EscapeSequence(ControlCode.ST)] // orphan ST
        private void ST() {
            Called.Add("Handle ST");
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

    internal class TestUtil {
        // Visual Studio or NUnit VS Test Adapter fail to recognize parameterized tests whose parameters contain non-printable characters.
        // To avoid this issue, we pass the non-printable characters in the parameter as \uXXXX notation, then convert them to characters with this utility function.

        public static string ConvertArg(string arg) {
            while (true) {
                int prefixIndex = arg.IndexOf("\\u");
                if (prefixIndex < 0) {
                    break;
                }

                string hex = arg.Substring(prefixIndex + 2, 4);
                uint code = UInt32.Parse(hex, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo);
                string ch = new String(new char[] { (char)code });
                arg = arg.Substring(0, prefixIndex) + ch + arg.Substring(prefixIndex + 6);
            }
            return arg;
        }
    }
}
#endif
