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
#if UNITTEST
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace Poderosa.Terminal.EscapeSequenceEngine {

    [TestFixture]
    class EscapeSequenceProcessorTest {

        private TestEscapeSequenceExecutor executor;
        private XTermEscapeSequenceProcessor processor;
        private StringBuilder normalText;
        private List<string> unknownSequences;

        [SetUp]
        public void SetUp() {
            normalText = new StringBuilder();
            unknownSequences = new List<string>();
            executor = new TestEscapeSequenceExecutor();

            var dfaEngine = DfaEngineFactory<TestEscapeSequenceExecutor>.CreateDfaEngine(executor);

            processor = new XTermEscapeSequenceProcessor(
                dfaEngine,
                (ch) => normalText.Append(ch),
                (seq) => unknownSequences.Add(new String(seq)));
        }

        private void ClearResults() {
            normalText.Clear();
            unknownSequences.Clear();
            executor.History.Clear();
        }

        private void Test_NormalAsciiText() {
            processor.Process('A');
            processor.Process('B');
            processor.Process('C');

            CollectionAssert.AreEqual(new string[0], executor.History);
            Assert.AreEqual("ABC", normalText.ToString());
            CollectionAssert.AreEqual(new string[0], unknownSequences);
        }

        private void Test_NormalNonAsciiText() {
            processor.Process('\u677e');
            processor.Process('\u7af9');
            processor.Process('\u6885');

            CollectionAssert.AreEqual(new string[0], executor.History);
            Assert.AreEqual("\u677e\u7af9\u6885", normalText.ToString());
            CollectionAssert.AreEqual(new string[0], unknownSequences);
        }

        private void Test_MatchSingleChar() {
            processor.Process('\u0007');    // BEL

            CollectionAssert.AreEqual(new string[] { "BEL" }, executor.History);
            Assert.AreEqual("", normalText.ToString());
            CollectionAssert.AreEqual(new string[0], unknownSequences);
        }

        private void Test_NoC1Conversion_MatchSequence() {
            processor.Process('\u001b');
            processor.Process('1');         // ESC 1 --> ESC 1

            CollectionAssert.AreEqual(new string[] { "ESC_1" }, executor.History);
            Assert.AreEqual("", normalText.ToString());
            CollectionAssert.AreEqual(new string[0], unknownSequences);
        }

        private void Test_C1Code_MatchSequence() {
            processor.Process('\u009b');    // CSI
            processor.Process('S');

            CollectionAssert.AreEqual(new string[] { "CSI_S" }, executor.History);
            Assert.AreEqual("", normalText.ToString());
            CollectionAssert.AreEqual(new string[0], unknownSequences);
        }

        private void Test_C1Conversion_MatchSequence() {
            processor.Process('\u001b');
            processor.Process('[');         // ESC [ --> CSI
            processor.Process('S');

            CollectionAssert.AreEqual(new string[] { "CSI_S" }, executor.History);
            Assert.AreEqual("", normalText.ToString());
            CollectionAssert.AreEqual(new string[0], unknownSequences);
        }

        private void Test_UnknownSequence() {
            processor.Process('\u001b');
            processor.Process('2');         // ESC 2 --> unknwon

            CollectionAssert.AreEqual(new string[0], executor.History);
            Assert.AreEqual("", normalText.ToString());
            CollectionAssert.AreEqual(new string[] { "\u001b2" }, unknownSequences);
        }

        private void Test_UnknownSequence_NonAscii() {
            processor.Process('\u001b');
            processor.Process('\u5b8c');

            CollectionAssert.AreEqual(new string[0], executor.History);
            Assert.AreEqual("", normalText.ToString());
            CollectionAssert.AreEqual(new string[] { "\u001b\u5b8c" }, unknownSequences);
        }

        //--------------------------------------------------------------
        // Test combination of patterns
        //--------------------------------------------------------------

        [Test, Combinatorial]
        public void TestCombinations([Range(1, 8)] int first, [Range(1, 8)] int second) {
            DoTest(first);
            ClearResults();
            DoTest(second);
        }

        private void DoTest(int n) {
            switch (n) {
                case 1:
                    Test_NormalAsciiText();
                    break;
                case 2:
                    Test_NormalNonAsciiText();
                    break;
                case 3:
                    Test_MatchSingleChar();
                    break;
                case 4:
                    Test_NoC1Conversion_MatchSequence();
                    break;
                case 5:
                    Test_C1Code_MatchSequence();
                    break;
                case 6:
                    Test_C1Conversion_MatchSequence();
                    break;
                case 7:
                    Test_UnknownSequence();
                    break;
                case 8:
                    Test_UnknownSequence_NonAscii();
                    break;
                default:
                    Assert.Fail("unknown test number : {0}", n);
                    break;
            }
        }

        private class TestEscapeSequenceExecutor : IEscapeSequenceExecutor {

            public readonly List<string> History = new List<string>();

            [ESPattern("{BEL}")]
            public void BEL(EscapeSequenceContext context) {
                History.Add("BEL");
            }

            [ESPattern("{ESC}1")]
            public void ESC_1(EscapeSequenceContext context) {
                History.Add("ESC_1");
            }

            [ESPattern("{CSI}S")]
            public void CSI_S(EscapeSequenceContext context) {
                History.Add("CSI_S");
            }
        }
    }
}
#endif