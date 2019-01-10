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
using System.Linq;

namespace Poderosa.Terminal.EscapeSequenceEngine {
    [TestFixture]
    class PatternParserTest {
        private PatternParser parser;

        [SetUp]
        public void SetUp() {
            parser = new PatternParser();
        }

        [Test]
        public void EmptyPattern() {
            Assert.Catch<ArgumentException>(() => parser.Parse(""));
        }

        [Test]
        public void NonEscapedCharacter() {
            var str = "!\"#$%&'()*+,-./azAZ09:;<=>?@]^_`|}~\u0020";
            var elements = parser.Parse(str);
            Assert.AreEqual(str.Length, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(CharacterSet));
            CollectionAssert.AreEqual(
                str.Select(c => new byte[] { (byte)c }),
                elements.Select(s => ((CharacterSet)s).Characters));
        }

        [Test]
        public void EscapedCharacter() {
            var str = "!\"#$%&'()*+,-./azAZ09:;<=>?@[\\]^_`{|}~\u0020";
            var testStr = String.Join("", str.Select(c => new String(new char[] { '\\', c })));
            var elements = parser.Parse(testStr);
            Assert.AreEqual(str.Length, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(CharacterSet));
            CollectionAssert.AreEqual(
                str.Select(c => new byte[] { (byte)c }),
                elements.Select(s => ((CharacterSet)s).Characters));
        }

        [Test]
        public void IncompleteEscape() {
            Assert.Catch<ArgumentException>(() => parser.Parse("a\\"));
        }

        [Test]
        public void CharacterSet() {
            var str = "[(\\\\)][\\[abcabc\\]][-a-c][a-c-][01c-a-e-gh][{CR}-{LF}{NUL}{ESC}][\\{CR}]";
            var elements = parser.Parse(str);
            Assert.AreEqual(7, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(CharacterSet));
            CollectionAssert.AreEqual(
                new byte[] { 0x28, 0x29, 0x5c },  // ( ) \
                ((CharacterSet)elements[0]).Characters);
            CollectionAssert.AreEqual(
                new byte[] { 0x5b, 0x5d, 0x61, 0x62, 0x63 },    // [ ] a b c
                ((CharacterSet)elements[1]).Characters);
            CollectionAssert.AreEqual(
                new byte[] { 0x2d, 0x61, 0x62, 0x63 },  // - a b c
                ((CharacterSet)elements[2]).Characters);
            CollectionAssert.AreEqual(
                new byte[] { 0x2d, 0x61, 0x62, 0x63 },  // - a b c
                ((CharacterSet)elements[3]).Characters);
            CollectionAssert.AreEqual(
                new byte[] { 0x2d, 0x30, 0x31, 0x61, 0x62, 0x63, 0x65, 0x66, 0x67, 0x68 },  // - 0 1 a b c e f g h
                ((CharacterSet)elements[4]).Characters);
            CollectionAssert.AreEqual(
                new byte[] { 0x00, 0x0a, 0x0b, 0x0c, 0x0d, 0x1b },  // NUL LF VT FF CR ESC
                ((CharacterSet)elements[5]).Characters);
            CollectionAssert.AreEqual(
                new byte[] { 0x43, 0x52, 0x7b, 0x7d },  // C R { }
                ((CharacterSet)elements[6]).Characters);
        }

        [Test]
        public void EmptyCharacterSet() {
            Assert.Catch<ArgumentException>(() => parser.Parse("a[]"));
        }

        [Test]
        public void IncompleteCharacterSet() {
            Assert.Catch<ArgumentException>(() => parser.Parse("a[abc"));
        }

        [Test]
        public void NamedCharacter() {
            var str = "{NUL}{ESC}{CR}{LF}{CSI}";
            var elements = parser.Parse(str);
            Assert.AreEqual(5, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(CharacterSet));
            CollectionAssert.AreEqual(new byte[] { 0x00 }, ((CharacterSet)elements[0]).Characters);
            CollectionAssert.AreEqual(new byte[] { 0x1b }, ((CharacterSet)elements[1]).Characters);
            CollectionAssert.AreEqual(new byte[] { 0x0d }, ((CharacterSet)elements[2]).Characters);
            CollectionAssert.AreEqual(new byte[] { 0x0a }, ((CharacterSet)elements[3]).Characters);
            CollectionAssert.AreEqual(new byte[] { 0x9b }, ((CharacterSet)elements[4]).Characters);
        }

        [Test]
        public void NumericalParameter() {
            var str = "{P1}{P2}{P3}{P04}{P100}";
            var elements = parser.Parse(str);
            Assert.AreEqual(5, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(NNumericalParams));
            Assert.AreEqual(1, ((NNumericalParams)elements[0]).Number);
            Assert.AreEqual(2, ((NNumericalParams)elements[1]).Number);
            Assert.AreEqual(3, ((NNumericalParams)elements[2]).Number);
            Assert.AreEqual(4, ((NNumericalParams)elements[3]).Number);
            Assert.AreEqual(100, ((NNumericalParams)elements[4]).Number);
        }

        [Test]
        public void InvalidNumericalParameter() {
            Assert.Catch<ArgumentException>(() => parser.Parse("{P-1}"));
            Assert.Catch<ArgumentException>(() => parser.Parse("{P0}"));
            Assert.Catch<ArgumentException>(() => parser.Parse("{P 1}"));
            Assert.Catch<ArgumentException>(() => parser.Parse("{P1 }"));
            Assert.Catch<ArgumentException>(() => parser.Parse("{P+1}"));
        }

        [Test]
        public void ZeroOrMoreNumericalParameters() {
            var str = "{P*}";
            var elements = parser.Parse(str);
            Assert.AreEqual(1, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(ZeroOrMoreNumericalParams));
        }

        [Test]
        public void TextParameter() {
            var str = "{Pt}";
            var elements = parser.Parse(str);
            Assert.AreEqual(1, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(TextParam));
        }

        [Test]
        public void StringParameter() {
            var str = "{Ps}";
            var elements = parser.Parse(str);
            Assert.AreEqual(1, elements.Count);
            CollectionAssert.AllItemsAreInstancesOfType(elements, typeof(AnyCharString));
        }

        [Test]
        public void UnknownName() {
            Assert.Catch<ArgumentException>(() => parser.Parse("{Px}"));
            Assert.Catch<ArgumentException>(() => parser.Parse("{aaa}"));
            Assert.Catch<ArgumentException>(() => parser.Parse("{}"));
        }

        [Test]
        public void IncompleteNamedParam() {
            Assert.Catch<ArgumentException>(() => parser.Parse("a{P"));
            Assert.Catch<ArgumentException>(() => parser.Parse("a{ESC"));
        }
    }
}
#endif
