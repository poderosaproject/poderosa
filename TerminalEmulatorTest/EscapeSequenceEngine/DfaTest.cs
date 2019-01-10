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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poderosa.Terminal.EscapeSequenceEngine {
    /// <summary>
    /// DFA test
    /// </summary>
    [TestFixture]
    class EscapeSequenceDFATest {
        private readonly string[] XTERM_ESCAPE_SEQUENCE_PATTERNS = new string[] {
                @"{ESC}{SP}F",
                @"{ESC}{SP}G",
                @"{ESC}{SP}L",
                @"{ESC}{SP}M",
                @"{ESC}{SP}N",
                @"{ESC}#3",
                @"{ESC}#4",
                @"{ESC}#5",
                @"{ESC}#6",
                @"{ESC}#8",
                @"{ESC}%@",
                @"{ESC}%G",
                @"{ESC}[()*+][AB4C5RfQ9KY6ZH7=0<>]",
                @"{ESC}[()*+]"">",
                @"{ESC}[()*+]%=",
                @"{ESC}[()*+]`,E",
                @"{ESC}[()*+]%6",
                @"{ESC}[()*+]%2",
                @"{ESC}[()*+]%5",
                @"{ESC}[()*+]""?",
                @"{ESC}[()*+]""4",
                @"{ESC}[()*+]%0",
                @"{ESC}[()*+]&4",
                @"{ESC}[()*+]&5",
                @"{ESC}[()*+]%3",
                @"{ESC}[-\./][AFHLM]",
                @"{ESC}6",
                @"{ESC}7",
                @"{ESC}8",
                @"{ESC}9",
                @"{ESC}=",
                @"{ESC}>",
                @"{ESC}F",
                @"{ESC}c",
                @"{ESC}l",
                @"{ESC}m",
                @"{ESC}n",
                @"{ESC}o",
                @"{ESC}|",
                @"{ESC}\}",
                @"{ESC}~",
                @"{APC}{Pt}{ST}",
                @"{DCS}{P2}|{Pt}{ST}",
                @"{DCS}$q{Pt}{ST}",
                @"{DCS}{P1}$t{Pt}{ST}",
                @"{DCS}+p{Pt}{ST}",
                @"{DCS}+q{Pt}{ST}",
                @"{CSI}{P1}@",
                @"{CSI}{P1}{SP}@",
                @"{CSI}{P1}A",
                @"{CSI}{P1}{SP}A",
                @"{CSI}{P1}B",
                @"{CSI}{P1}C",
                @"{CSI}{P1}D",
                @"{CSI}{P1}E",
                @"{CSI}{P1}F",
                @"{CSI}{P1}G",
                @"{CSI}{P2}H",
                @"{CSI}{P1}I",
                @"{CSI}{P1}J",
                @"{CSI}?{P1}J",
                @"{CSI}{P1}K",
                @"{CSI}?{P1}K",
                @"{CSI}{P1}L",
                @"{CSI}{P1}M",
                @"{CSI}{P1}P",
                @"{CSI}{P1}S",
                @"{CSI}?{P3}S",
                @"{CSI}{P1}T",
                @"{CSI}{P5}T",
                @"{CSI}>{P2}T",
                @"{CSI}{P1}X",
                @"{CSI}{P1}Z",
                @"{CSI}{P1}^",
                @"{CSI}{P*}`",
                @"{CSI}{P*}a",
                @"{CSI}{P1}b",
                @"{CSI}{P1}c",
                @"{CSI}={P1}c",
                @"{CSI}>{P1}c",
                @"{CSI}{P*}d",
                @"{CSI}{P*}e",
                @"{CSI}{P2}f",
                @"{CSI}{P1}g",
                @"{CSI}{P*}h",
                @"{CSI}?{P*}h",
                @"{CSI}{P*}i",
                @"{CSI}?{P*}i",
                @"{CSI}{P*}l",
                @"{CSI}?{P*}l",
                @"{CSI}{P*}m",
                @"{CSI}>{P2}m",
                @"{CSI}{P1}n",
                @"{CSI}>{P1}n",
                @"{CSI}?{P1}n",
                @"{CSI}>{P1}p",
                @"{CSI}!p",
                @"{CSI}{P2}""p",
                @"{CSI}{P1}$p",
                @"{CSI}?{P1}$p",
                @"{CSI}{P1}q",
                @"{CSI}{P1}{SP}q",
                @"{CSI}{P1}""q",
                @"{CSI}{P2}r",
                @"{CSI}?{P*}r",
                @"{CSI}{P5}$r",
                @"{CSI}s",
                @"{CSI}{P2}s",
                @"{CSI}?{P*}s",
                @"{CSI}{P3}t",
                @"{CSI}>{P2}t",
                @"{CSI}{P1}{SP}t",
                @"{CSI}{P5}$t",
                @"{CSI}u",
                @"{CSI}{P1}{SP}u",
                @"{CSI}{P8}$v",
                @"{CSI}{P1}$w",
                @"{CSI}{P4}'w",
                @"{CSI}{P1}x",
                @"{CSI}{P1}*x",
                @"{CSI}{P5}$x",
                @"{CSI}{P1}#y",
                @"{CSI}{P6}*y",
                @"{CSI}{P2}'z",
                @"{CSI}{P4}$z",
                @"{CSI}{P*}'\{",
                @"{CSI}#\{",
                @"{CSI}{P2}#\{",
                @"{CSI}{P4}$\{",
                @"{CSI}{P4}#|",
                @"{CSI}{P1}$|",
                @"{CSI}{P1}'|",
                @"{CSI}{P1}*|",
                @"{CSI}#\}",
                @"{CSI}{P*}'\}",
                @"{CSI}{P*}'~",
                @"{OSC}{P1};{Pt}{BEL}",
                @"{OSC}{P1};{Pt}{ST}",
                @"{PM}{Pt}{ST}",
                @"{SOS}{Ps}{ST}",
            };

        [Test]
        public void TestDfa() {
            NfaManager nfa = new NfaManager();

            EscapeSequenceContext lastCompletedContext = null;

            foreach (string pattern in XTERM_ESCAPE_SEQUENCE_PATTERNS) {
                nfa.AddPattern(pattern, context => {
                    lastCompletedContext = context;
                });
            }

            var sw = Stopwatch.StartNew();
            var dfaStateManager = nfa.CreateDfa();
            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine("CreateDfa : {0} ms", elapsed);

            DfaEngine dfa = new DfaEngine(dfaStateManager, new DummyEscapeSequenceExecutor());

            byte[] origBytes = new byte[1];

            foreach (string pattern in XTERM_ESCAPE_SEQUENCE_PATTERNS) {
                foreach (DfaTestData testData in GenerateTestPatterns(pattern)) {
#if false
                    Debug.WriteLine("\"{0}\" => [{1}]",
                        pattern,
                        String.Join(" ",
                            testData.Bytes.Select(b =>
                                (b >= 0x21 && b <= 0x7e) ? Char.ToString((Char)b) : ("<" + b.ToString("x2") + ">"))));
#endif
                    lastCompletedContext = null;
                    int index = 0;
                    try {
                        for (index = 0; index < testData.Bytes.Length; index++) {
                            byte b = testData.Bytes[index];
                            origBytes[0] = b;
                            bool accepted = dfa.Process(b, origBytes);

                            if (testData.IsFailurePattern && index == testData.Bytes.Length - 1) {
                                Assert.AreEqual(false, accepted);
                            }
                            else {
                                Assert.AreEqual(true, accepted);
                            }

                            if (index < testData.Bytes.Length - 1 || testData.IsFailurePattern) {
                                Assert.IsNull(lastCompletedContext);
                            }
                            else {
                                Assert.AreEqual(pattern, lastCompletedContext.Pattern);
                            }
                        }

                        if (!testData.IsFailurePattern) {
                            CollectionAssert.AreEqual(
                                testData.ExpectedNumericalParams,
                                lastCompletedContext.NumericalParams.Select(n => n.Value).ToArray());

                            Assert.AreEqual(
                                testData.ExpectedTextParam,
                                (lastCompletedContext.TextParam != null) ? lastCompletedContext.TextParam.Value : null);
                        }
                    }
                    catch (Exception e) {
                        Console.Out.WriteLine("pattern = \"{0}\"", pattern);
                        Console.Out.WriteLine("input = [{0}]",
                            String.Concat(
                                testData.Bytes.Select(b =>
                                    (b >= 0x21 && b <= 0x7e) ? Char.ToString((Char)b) : ("\\x" + b.ToString("x2")))));
                        Console.Out.WriteLine("index = {0}", index);
                        Console.Out.WriteLine(e.StackTrace);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Test case data
        /// </summary>
        /// <remarks>
        /// Immutable object which retains conditions of a test case.
        /// </remarks>
        private class DfaTestData {
            /// <summary>
            /// Bytes to input
            /// </summary>
            public readonly byte[] Bytes;

            /// <summary>
            /// Whether this data must fail
            /// </summary>
            public readonly bool IsFailurePattern;

            /// <summary>
            /// Expected numerical parameters
            /// </summary>
            public readonly int?[] ExpectedNumericalParams;

            /// <summary>
            /// Expected text parameter
            /// </summary>
            public readonly string ExpectedTextParam;

            public DfaTestData()
                : this(new byte[0], false, new int?[0], null) {
            }

            private DfaTestData(byte[] bytes, bool isFailurePattern, int?[] expectedNumericalParams, string expectedTextParam) {
                this.Bytes = bytes;
                this.IsFailurePattern = isFailurePattern;
                this.ExpectedNumericalParams = expectedNumericalParams;
                this.ExpectedTextParam = expectedTextParam;
            }

            public DfaTestData AppendBytes(params byte[] bytes) {
                return new DfaTestData(
                    ConcatArray(this.Bytes, bytes),
                    this.IsFailurePattern,
                    this.ExpectedNumericalParams,
                    this.ExpectedTextParam);
            }

            public DfaTestData AppendBytesForFailure(params byte[] bytes) {
                return new DfaTestData(
                    ConcatArray(this.Bytes, bytes),
                    true,
                    this.ExpectedNumericalParams,
                    this.ExpectedTextParam);
            }

            public DfaTestData AppendNumericalParams(int?[] paramValues, params byte[] bytes) {
                return new DfaTestData(
                    ConcatArray(this.Bytes, bytes),
                    this.IsFailurePattern,
                    ConcatArray(this.ExpectedNumericalParams, paramValues),
                    this.ExpectedTextParam);
            }

            public DfaTestData AppendTextParam(string paramValue, params byte[] bytes) {
                return new DfaTestData(
                    ConcatArray(this.Bytes, bytes),
                    this.IsFailurePattern,
                    this.ExpectedNumericalParams,
                    paramValue);
            }

            private T[] ConcatArray<T>(T[] preceding, params T[] newItems) {
                T[] newArray = new T[preceding.Length + newItems.Length];
                preceding.CopyTo(newArray, 0);
                newItems.CopyTo(newArray, preceding.Length);
                return newArray;
            }
        }

        private IEnumerable<DfaTestData> GenerateTestPatterns(string pattern) {
            // Build linked generators.
            // Each generator generates various test pattern for the pattern-element,
            // and calls the next generator for each generated pattern.
            // As a result, the first generator returns all combination of generated patterns.

            Func<DfaTestData, IEnumerable<DfaTestData>> generator = d => new DfaTestData[] { d };   // the last generator

            foreach (IPatternElement elem in new PatternParser().Parse(pattern).Reverse()) {
                if (elem is CharacterSet) {
                    CharacterSet characterSet = elem as CharacterSet;
                    Func<DfaTestData, IEnumerable<DfaTestData>> next = generator;
                    generator = d => GenerateTestPatterns(d, characterSet, next);
                }
                else if (elem is ZeroOrMoreNumericalParams) {
                    ZeroOrMoreNumericalParams mparams = elem as ZeroOrMoreNumericalParams;
                    Func<DfaTestData, IEnumerable<DfaTestData>> next = generator;
                    generator = d => GenerateTestPatterns(d, mparams, next);
                }
                else if (elem is NNumericalParams) {
                    NNumericalParams nparams = elem as NNumericalParams;
                    Func<DfaTestData, IEnumerable<DfaTestData>> next = generator;
                    generator = d => GenerateTestPatterns(d, nparams, next);
                }
                else if (elem is TextParam) {
                    TextParam tparam = elem as TextParam;
                    Func<DfaTestData, IEnumerable<DfaTestData>> next = generator;
                    generator = d => GenerateTestPatterns(d, tparam, next);
                }
                else if (elem is AnyCharString) {
                    AnyCharString anyCharStr = elem as AnyCharString;
                    Func<DfaTestData, IEnumerable<DfaTestData>> next = generator;
                    generator = d => GenerateTestPatterns(d, anyCharStr, next);
                }
                else {
                    throw new Exception("unknown element type");
                }
            }

            return generator(new DfaTestData());
        }

        /// <summary>
        /// Generator for <see cref="CharacterSet"/>
        /// </summary>
        /// <param name="testData"></param>
        /// <param name="characterSet"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        private IEnumerable<DfaTestData> GenerateTestPatterns(
            DfaTestData testData, CharacterSet characterSet, Func<DfaTestData, IEnumerable<DfaTestData>> next) {
            foreach (byte b in characterSet.Characters) {
                var newTestData = testData.AppendBytes(b);
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // failure cases
            if (characterSet.Characters.Length == 1 && characterSet.Characters[0] == 0x9c /*ST*/) {
                yield return testData.AppendBytesForFailure(0x98/*SOS*/);
            }
            else {
                yield return testData.AppendBytesForFailure(0x00);
                yield return testData.AppendBytesForFailure(0xff);
            }
        }

        private readonly string[] validNumericalParamValues = new string[] {
            "",
            "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
            "100", "101", "102", "103", "104", "105", "106", "107", "108", "109",
            "000", "001", "002", "003", "004", "005", "006", "007", "008", "009",
        };

        /// <summary>
        /// Generator for <see cref="ZeroOrMoreNumericalParams"/>
        /// </summary>
        private IEnumerable<DfaTestData> GenerateTestPatterns(
            DfaTestData testData, ZeroOrMoreNumericalParams p, Func<DfaTestData, IEnumerable<DfaTestData>> next) {
            for (int testParamNum = 1; testParamNum < 10; testParamNum++) {
                // generates parameter strings like:
                //   <paramValue>;1;1
                //   1;<paramValue>;1
                //   1;1;<paramValue>
                foreach (string paramValue in validNumericalParamValues) {
                    int? paramIntValue = (paramValue.Length == 0) ? (int?)null : Int32.Parse(paramValue);

                    for (int i = 0; i < testParamNum; i++) {
                        string paramStr =
                        String.Join(";", Enumerable.Range(0, testParamNum).Select(index => (index == i) ? paramValue : "1"));

                        int?[] expectedParamValues =
                            Enumerable.Range(0, testParamNum).Select(index => (index == i) ? paramIntValue : 1).ToArray();

                        if (!expectedParamValues[expectedParamValues.Length - 1].HasValue) {
                            // last parameter will not be stored
                            expectedParamValues = expectedParamValues.Take(expectedParamValues.Length - 1).ToArray();
                        }

                        var newTestData = testData.AppendNumericalParams(expectedParamValues, Encoding.ASCII.GetBytes(paramStr));
                        foreach (var r in next(newTestData)) {
                            yield return r;
                        }
                    }
                }

                // generates ";;;...;;;" (all parameters are empty)
                {
                    string paramStr = String.Concat(Enumerable.Repeat(";", testParamNum - 1));

                    // last parameter will not be stored
                    int?[] expectedParamValues = Enumerable.Repeat((int?)null, testParamNum - 1).ToArray();

                    var newTestData = testData.AppendNumericalParams(expectedParamValues, Encoding.ASCII.GetBytes(paramStr));
                    foreach (var r in next(newTestData)) {
                        yield return r;
                    }
                }
            }

            // failure cases
            for (int testParamNum = 1; testParamNum < 10; testParamNum++) {
                string paramStr;

                paramStr = String.Concat(Enumerable.Repeat("1;", testParamNum - 1)) + "\u0000";
                yield return testData.AppendBytesForFailure(Encoding.ASCII.GetBytes(paramStr));

                paramStr = String.Concat(Enumerable.Repeat("1;", testParamNum - 1)) + "1\u0000";
                yield return testData.AppendBytesForFailure(Encoding.ASCII.GetBytes(paramStr));
            }
        }

        /// <summary>
        /// Generator for <see cref="NNumericalParams"/>
        /// </summary>
        private IEnumerable<DfaTestData> GenerateTestPatterns(
            DfaTestData testData, NNumericalParams p, Func<DfaTestData, IEnumerable<DfaTestData>> next) {
            foreach (int testParamNum in new int[] { p.Number, p.Number - 1 /* the last parameter was omitted */ }) {
                if (testParamNum < 1) {
                    continue;
                }

                // generates parameter strings like:
                //   <paramValue>;1;1
                //   1;<paramValue>;1
                //   1;1;<paramValue>
                foreach (string paramValue in validNumericalParamValues) {
                    if (paramValue.Length == 0 && testParamNum == p.Number - 1) {
                        // not include the case that the last two parameters were omitted
                        continue;
                    }

                    int? paramIntValue = (paramValue.Length == 0) ? (int?)null : Int32.Parse(paramValue);

                    for (int i = 0; i < testParamNum; i++) {

                        string paramStr =
                            String.Join(";", Enumerable.Range(0, testParamNum).Select(index => (index == i) ? paramValue : "1"));

                        int?[] expectedParamValues =
                            Enumerable.Range(0, testParamNum).Select(index => (index == i) ? paramIntValue : 1).ToArray();

                        if (!expectedParamValues[expectedParamValues.Length - 1].HasValue) {
                            // last parameter will not be stored
                            expectedParamValues = expectedParamValues.Take(expectedParamValues.Length - 1).ToArray();
                        }

                        var newTestData = testData.AppendNumericalParams(expectedParamValues, Encoding.ASCII.GetBytes(paramStr));
                        foreach (var r in next(newTestData)) {
                            yield return r;
                        }
                    }
                }
            }

            // generates ";;;...;;;" (all parameters are empty)
            {
                string paramStr = String.Concat(Enumerable.Repeat(";", p.Number - 1));

                // last parameter will not be stored
                int?[] expectedParamValues = Enumerable.Repeat((int?)null, p.Number - 1).ToArray();

                var newTestData = testData.AppendNumericalParams(expectedParamValues, Encoding.ASCII.GetBytes(paramStr));
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // failure cases
            for (int testParamNum = 1; testParamNum < p.Number; testParamNum++) {
                string paramStr;

                paramStr = String.Concat(Enumerable.Repeat("1;", testParamNum - 1)) + "\u0000";
                yield return testData.AppendBytesForFailure(Encoding.ASCII.GetBytes(paramStr));

                paramStr = String.Concat(Enumerable.Repeat("1;", testParamNum - 1)) + "1\u0000";
                yield return testData.AppendBytesForFailure(Encoding.ASCII.GetBytes(paramStr));
            }
        }

        /// <summary>
        /// Generator for <see cref="TextParam"/>
        /// </summary>
        private IEnumerable<DfaTestData> GenerateTestPatterns(DfaTestData testData, TextParam p, Func<DfaTestData, IEnumerable<DfaTestData>> next) {
            // empty
            {
                string expectedParamValue = null;
                var newTestData = testData.AppendTextParam(expectedParamValue, new byte[] { });
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // one character
            foreach (byte b in GeneratePrintable()) {
                string expectedParamValue = Char.ToString((char)b);
                var newTestData = testData.AppendTextParam(expectedParamValue, b);
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // three characters
            foreach (byte b in GeneratePrintable()) {
                string s = Char.ToString((char)b);
                string expectedParamValue = s + s + s;
                var newTestData = testData.AppendTextParam(expectedParamValue, b, b, b);
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // failure cases
            {
                yield return testData.AppendBytesForFailure(0x00);
                yield return testData.AppendBytesForFailure(0x41/*A*/, 0x00);
            }
        }

        private IEnumerable<byte> GeneratePrintable() {
            for (int c = 0x08; c <= 0x0d; c++) {
                yield return (byte)c;
            }

            for (int c = 0x20; c <= 0x7e; c++) {
                yield return (byte)c;
            }
        }

        /// <summary>
        /// Generator for <see cref="AnyCharString"/>
        /// </summary>
        private IEnumerable<DfaTestData> GenerateTestPatterns(DfaTestData testData, AnyCharString p, Func<DfaTestData, IEnumerable<DfaTestData>> next) {
            // empty
            {
                string expectedParamValue = null;
                var newTestData = testData.AppendTextParam(expectedParamValue, new byte[] { });
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // one character
            foreach (byte b in GenerateStringChar()) {
                string expectedParamValue = Char.ToString((char)b);
                var newTestData = testData.AppendTextParam(expectedParamValue, b);
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // three characters
            foreach (byte b in GenerateStringChar()) {
                string s = Char.ToString((char)b);
                string expectedParamValue = s + s + s;
                var newTestData = testData.AppendTextParam(expectedParamValue, b, b, b);
                foreach (var r in next(newTestData)) {
                    yield return r;
                }
            }

            // failure cases
            {
                yield return testData.AppendBytesForFailure(0x98/*SOS*/);
                yield return testData.AppendBytesForFailure(0x41, 0x98/*SOS*/);
            }
        }

        private IEnumerable<byte> GenerateStringChar() {
            for (int c = 0; c <= 0xff; c++) {
                if (c == 0x98 || c == 0x9c) {
                    continue;
                }
                yield return (byte)c;
            }
        }
    }

    class DummyEscapeSequenceExecutor : IEscapeSequenceExecutor {
    }

    [TestFixture]
    class TestDfaLimits {

        private readonly string[] ESCAPE_SEQUENCE_PATTERNS =
        {
            "X{Pt}{ST}"
        };

        private bool finished;
        private DfaEngine dfa;

        private readonly byte[] origBytes = new byte[1];

        [SetUp]
        public void SetUp() {
            NfaManager nfa = new NfaManager();

            foreach (string pattern in ESCAPE_SEQUENCE_PATTERNS) {
                nfa.AddPattern(pattern, context => {
                    finished = true;
                });
            }

            var dfaStateManager = nfa.CreateDfa();
            dfa = new DfaEngine(dfaStateManager, new DummyEscapeSequenceExecutor());
            finished = false;
        }

        private bool Put(byte b) {
            origBytes[0] = b;
            return dfa.Process(b, origBytes);
        }

        [Test]
        public void FinishesBeforeLimit() {
            bool accepted;

            accepted = Put((byte)'X');
            Assert.AreEqual(true, accepted);
            Assert.AreEqual(false, finished);

            for (int i = 0; i < DfaEngine.MAX_SEQUENCE_LENGTH - 3; i++) {
                accepted = Put((byte)'a');
                Assert.AreEqual(true, accepted);
                Assert.AreEqual(false, finished);
            }

            accepted = Put((byte)0x9c);
            Assert.AreEqual(true, accepted);
            Assert.AreEqual(true, finished);
        }

        [Test]
        public void FinishesJustAtLimit() {
            bool accepted;

            accepted = Put((byte)'X');
            Assert.AreEqual(true, accepted);
            Assert.AreEqual(false, finished);

            for (int i = 0; i < DfaEngine.MAX_SEQUENCE_LENGTH - 2; i++) {
                accepted = Put((byte)'a');
                Assert.AreEqual(true, accepted);
                Assert.AreEqual(false, finished);
            }

            accepted = Put((byte)0x9c);
            Assert.AreEqual(true, accepted);
            Assert.AreEqual(true, finished);
        }

        [Test]
        public void NoFinish() {
            bool accepted;

            accepted = Put((byte)'X');
            Assert.AreEqual(true, accepted);
            Assert.AreEqual(false, finished);

            for (int i = 0; i < DfaEngine.MAX_SEQUENCE_LENGTH - 2; i++) {
                accepted = Put((byte)'a');
                Assert.AreEqual(true, accepted);
                Assert.AreEqual(false, finished);
            }

            accepted = Put((byte)'a');
            Assert.AreEqual(false, accepted);   // too long
            Assert.AreEqual(false, finished);
        }
    }

    [TestFixture]
    class DfaEngineFactoryTest {

        private class ExecResult {
            public readonly string Method;
            public readonly string Pattern;
            public readonly byte[] Matched;
            public ExecResult(string method, EscapeSequenceContext context) {
                this.Method = method;
                this.Pattern = context.Pattern;
                this.Matched = context.Matched.ToArray();
            }
        }

        private class EscapeSequenceExecutorBase : IEscapeSequenceExecutor {


            public readonly List<ExecResult> Results = new List<ExecResult>();

            [ESPattern("BA")]
            public void BA(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Base:BA", context));
            }

            [ESPattern("BB")]
            [ESPattern("BX")]
            public virtual void BB(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Base:BB", context));
            }

            [ESPattern("BC")]
            internal void BC(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Base:BC", context));
            }

            [ESPattern("BD")]
            internal virtual void BD(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Base:BD", context));
            }

            [ESPattern("BE")]
            private void BE(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Base:BE", context));
            }
        }

        private class EscapeSequenceExecutor1 : EscapeSequenceExecutorBase {

            // [ESPattern("BB")] inherited
            [ESPattern("BX")]   // this pattern is also specified in the base method
            [ESPattern("BY")]
            public override void BB(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Override:BB", context));
            }

            [ESPattern("C1")]
            [ESPattern("C2")]
            public void C(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Exec:C", context));
            }

            [ESPattern("D1")]
            [ESPattern("D2")]
            internal void D(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Exec:D", context));
            }

            [ESPattern("E1")]
            [ESPattern("E2")]
            private void E(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Exec:E", context));
            }
        }

        private class EscapeSequenceExecutor2 : EscapeSequenceExecutor1 {

            [ESPattern("F1")]
            [ESPattern("F2")]
            public void F(EscapeSequenceContext context) {
                Results.Add(new ExecResult("Exec:F", context));
            }

        }

        [Test]
        public void TestDfaEngineFactory_WithPreparation() {
            DfaEngineFactory<EscapeSequenceExecutor1>.Prepare();

            var executor = new EscapeSequenceExecutor1();

            DfaEngine engine = DfaEngineFactory<EscapeSequenceExecutor1>.CreateDfaEngine(executor);

            foreach (string s in new string[] {
                "BA", "BB", "BC", "BD", "BE", "BX", "BY",
                "C1", "C2", "D1", "D2", "E1", "E2", "F1", "F2",
            }) {
                foreach (byte b in Encoding.ASCII.GetBytes(s)) {
                    engine.Process(b, new byte[] { b });
                }
            }

            Assert.AreEqual(10, executor.Results.Count);
            Check(executor.Results[0], "Base:BA", "BA", "BA");
            Check(executor.Results[1], "Override:BB", "BB", "BB");
            Check(executor.Results[2], "Base:BC", "BC", "BC");
            Check(executor.Results[3], "Base:BD", "BD", "BD");
            Check(executor.Results[4], "Override:BB", "BX", "BX");
            Check(executor.Results[5], "Override:BB", "BY", "BY");
            Check(executor.Results[6], "Exec:C", "C1", "C1");
            Check(executor.Results[7], "Exec:C", "C2", "C2");
            Check(executor.Results[8], "Exec:D", "D1", "D1");
            Check(executor.Results[9], "Exec:D", "D2", "D2");
        }

        private void Check(ExecResult result, string expectedMethod, string expectedPattern, string expectedMatched) {
            Assert.AreEqual(expectedMethod, result.Method);
            Assert.AreEqual(expectedPattern, result.Pattern);
            CollectionAssert.AreEqual(Encoding.ASCII.GetBytes(expectedMatched), result.Matched);
        }

        [Test]
        public void CreateDfaEngine_CheckSharedDfaStateManager() {
            var bag1 = new ConcurrentBag<DfaEngine>();
            var bag2 = new ConcurrentBag<DfaEngine>();
            Action action1 = () => {
                var executor = new EscapeSequenceExecutor1();
                var dfaEngine = DfaEngineFactory<EscapeSequenceExecutor1>.CreateDfaEngine(executor);
                bag1.Add(dfaEngine);
            };
            Action action2 = () => {
                var executor = new EscapeSequenceExecutor2();
                var dfaEngine = DfaEngineFactory<EscapeSequenceExecutor2>.CreateDfaEngine(executor);
                bag2.Add(dfaEngine);
            };
            var tasks = Enumerable.Range(0, 100).Select((n) => Task.Run((n % 2 == 0) ? action1 : action2)).ToArray();
            Task.WaitAll(tasks);

            Assert.AreEqual(50, bag1.Count);
            Assert.AreEqual(50, bag2.Count);

            DfaStateManager stateManager1 = bag1.First().DfaStateManager;
            DfaStateManager stateManager2 = bag2.First().DfaStateManager;
            Assert.AreNotSame(stateManager1, stateManager2);
            foreach (var dfaEngine in bag1) {
                Assert.AreSame(stateManager1, dfaEngine.DfaStateManager);
            }
            foreach (var dfaEngine in bag2) {
                Assert.AreSame(stateManager2, dfaEngine.DfaStateManager);
            }
        }
    }
}
#endif
