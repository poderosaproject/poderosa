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
using System.Globalization;
using System.Linq;

namespace Poderosa.Document {

    [TestFixture]
    class TerminalCharacterDocumentTest {

        private readonly GLine[] newLines = new GLine[] {
                null, null, null, null, null,
                GLine.CreateSimpleGLine("A", TextDecoration.Default),
                GLine.CreateSimpleGLine("B", TextDecoration.Default),
                GLine.CreateSimpleGLine("C", TextDecoration.Default),
                GLine.CreateSimpleGLine("D", TextDecoration.Default),
                GLine.CreateSimpleGLine("E", TextDecoration.Default),
                GLine.CreateSimpleGLine("F", TextDecoration.Default),
                GLine.CreateSimpleGLine("G", TextDecoration.Default),
                GLine.CreateSimpleGLine("H", TextDecoration.Default),
                GLine.CreateSimpleGLine("I", TextDecoration.Default),
                GLine.CreateSimpleGLine("J", TextDecoration.Default),
                GLine.CreateSimpleGLine("K", TextDecoration.Default),
                GLine.CreateSimpleGLine("L", TextDecoration.Default),
                GLine.CreateSimpleGLine("M", TextDecoration.Default),
                GLine.CreateSimpleGLine("N", TextDecoration.Default),
                GLine.CreateSimpleGLine("O", TextDecoration.Default),
                GLine.CreateSimpleGLine("P", TextDecoration.Default),
                GLine.CreateSimpleGLine("Q", TextDecoration.Default),
                GLine.CreateSimpleGLine("R", TextDecoration.Default),
                GLine.CreateSimpleGLine("S", TextDecoration.Default),
                GLine.CreateSimpleGLine("T", TextDecoration.Default),
                GLine.CreateSimpleGLine("U", TextDecoration.Default),
                GLine.CreateSimpleGLine("V", TextDecoration.Default),
                GLine.CreateSimpleGLine("W", TextDecoration.Default),
                GLine.CreateSimpleGLine("X", TextDecoration.Default),
                GLine.CreateSimpleGLine("Y", TextDecoration.Default),
                GLine.CreateSimpleGLine("Z", TextDecoration.Default),
                null, null, null, null, null,
            };

        [Test]
        public void Test_InitialState() {
            TestTerminalDoc doc = new TestTerminalDoc();
            CheckLines(doc,
                new string[] { },
                new string[] { "", "", "", "", "", "", "", "", "", "", }
            );
        }

        [Test]
        public void Test_VisibleAreaSizeChanged_ScreenIsNotIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", }
            );

            doc.ScreenIsolated = false;

            // 80x10 -> 60x6
            doc.VisibleAreaSizeChanged(6, 60);

            CheckLines(doc,
                new string[] { "1", "2", "3", "4", },
                new string[] { "5", "6", "7", "8", "9", "10", }
            );

            // 60x6 -> 70x6
            doc.VisibleAreaSizeChanged(6, 70);

            CheckLines(doc,
                new string[] { "1", "2", "3", "4", },
                new string[] { "5", "6", "7", "8", "9", "10", }
            );

            // 70x6 -> 70x8
            doc.VisibleAreaSizeChanged(8, 70);

            CheckLines(doc,
                new string[] { "1", "2", },
                new string[] { "3", "4", "5", "6", "7", "8", "9", "10", }
            );

            // 70x8 -> 70x12
            doc.VisibleAreaSizeChanged(12, 70);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "", "", }
            );

            // 70x12 -> 70x13
            doc.VisibleAreaSizeChanged(15, 70);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "", "", "", "", "", }
            );
        }

        [Test]
        public void Test_VisibleAreaSizeChanged_ScreenIsIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", }
            );

            doc.ScreenIsolated = true;

            // 80x10 -> 60x6
            doc.VisibleAreaSizeChanged(6, 60);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", }
            );

            // 60x6 -> 70x6
            doc.VisibleAreaSizeChanged(6, 70);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", }
            );

            // 70x6 -> 70x8
            doc.VisibleAreaSizeChanged(8, 70);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "", "", }
            );

            // 70x8 -> 70x12
            doc.VisibleAreaSizeChanged(12, 70);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "", "", "", "", "", "", }
            );
        }

        [Test]
        public void Test_ScreenAppend() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", }
            );

            doc.ScreenAppend_(GLine.CreateSimpleGLine("X", TextDecoration.Default));

            CheckLines(doc,
                new string[] { "1", },
                new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "X", }
            );

            doc.ScreenAppend_(GLine.CreateSimpleGLine("Y", TextDecoration.Default));

            CheckLines(doc,
                new string[] { "1", "2", },
                new string[] { "3", "4", "5", "6", "7", "8", "9", "10", "X", "Y", }
            );
        }

        [Test]
        public void Test_ScreenGetRow() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            Assert.AreEqual("1", doc.ScreenGetRow_(0).ToNormalString());
            Assert.AreEqual("10", doc.ScreenGetRow_(9).ToNormalString());
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void Test_ScreenGetRow_Error(int rowIndex) {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            Assert.Throws<IndexOutOfRangeException>(() => doc.ScreenGetRow_(rowIndex));
        }

        [Test]
        public void Test_ScreenSetRow() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            doc.ScreenSetRow_(0, GLine.CreateSimpleGLine("X", TextDecoration.Default));
            doc.ScreenSetRow_(9, GLine.CreateSimpleGLine("Y", TextDecoration.Default));

            CheckLines(doc,
                new string[] { },
                new string[] { "X", "2", "3", "4", "5", "6", "7", "8", "9", "Y", }
            );
        }

        [TestCase(-1)]
        [TestCase(10)]
        public void Test_ScreenSetRow_Error(int rowIndex) {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            Assert.Throws<IndexOutOfRangeException>(
                () => doc.ScreenSetRow_(rowIndex, GLine.CreateSimpleGLine("Z", TextDecoration.Default)));
        }

        [TestCase(0, 10, new string[] { null, null, null, null, null, "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", null, null, null, null, null, })]
        [TestCase(9, 1, new string[] { null, null, null, null, null, "10", null, null, null, null, null, null, null, null, null, null, null, null, null, null, })]
        public void Test_ScreenGetRows(int rowIndex, int length, string[] expected) {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            GLineChunk chunk = new GLineChunk(20);
            doc.ScreenGetRows_(rowIndex, chunk.Span(5, length));
            string[] chunkContent = chunk.Array.Select(r => (r != null) ? r.ToNormalString() : null).ToArray();
            CollectionAssert.AreEqual(expected, chunkContent);
        }

        [TestCase(-1, 2)]
        [TestCase(0, 11)]
        [TestCase(9, 2)]
        [TestCase(10, 1)]
        public void Test_ScreenGetRows_Error(int rowIndex, int length) {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            GLineChunk chunk = new GLineChunk(20);
            Assert.Throws<ArgumentException>(() => doc.ScreenGetRows_(rowIndex, chunk.Span(5, length)));
        }

        [TestCase(0, 10, new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", })]
        [TestCase(9, 1, new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "A", })]
        public void Test_ScreenSetRows(int rowIndex, int length, string[] expected) {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            doc.ScreenSetRows_(rowIndex, new GLineChunkSpan(newLines, 5, length));

            CheckLines(doc,
                new string[] { },
                expected);
        }

        [TestCase(-1, 2)]
        [TestCase(0, 11)]
        [TestCase(9, 2)]
        [TestCase(10, 1)]
        public void Test_ScreenSetRows_Error(int rowIndex, int length) {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);
            Assert.Throws<ArgumentException>(
                () => doc.ScreenSetRows_(rowIndex, new GLineChunkSpan(newLines, 5, length)));
        }

        [Test]
        public void Test_ScreenScrollUp_AppendEmptyLines_ScreenIsNotIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            doc.ScreenIsolated = false;

            doc.ScreenScrollUp_(1);

            CheckLines(doc,
                new string[] { "1", },
                new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "", }
            );

            doc.ScreenScrollUp_(2);

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "", "", "", }
            );

            doc.ScreenScrollUp_(15);

            CheckLines(doc,
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "", "", "", "", "", "", "", "", },
                new string[] { "", "", "", "", "", "", "", "", "", "", }
            );
        }

        [Test]
        public void Test_ScreenScrollUp_AppendEmptyLines_ScreenIsIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            doc.ScreenIsolated = true;

            doc.ScreenScrollUp_(1);

            CheckLines(doc,
                new string[] { },
                new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "", }
            );

            doc.ScreenScrollUp_(2);

            CheckLines(doc,
                new string[] { },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "", "", "", }
            );

            doc.ScreenScrollUp_(15);

            CheckLines(doc,
                new string[] { },
                new string[] { "", "", "", "", "", "", "", "", "", "", }
            );
        }

        [Test]
        public void Test_ScreenScrollUp_AppendLines_ScreenIsNotIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            doc.ScreenIsolated = false;

            doc.ScreenScrollUp_(new GLineChunkSpan(newLines, 5, 1));

            CheckLines(doc,
                new string[] { "1", },
                new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "A", }
            );

            doc.ScreenScrollUp_(new GLineChunkSpan(newLines, 6, 2));

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "A", "B", "C", }
            );

            doc.ScreenScrollUp_(new GLineChunkSpan(newLines, 8, 15));

            CheckLines(doc,
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "A", "B", "C", "D", "E", "F", "G", "H", },
                new string[] { "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", }
            );
        }

        [Test]
        public void Test_ScreenScrollUp_AppendLines_ScreenIsIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            doc.ScreenIsolated = true;

            doc.ScreenScrollUp_(new GLineChunkSpan(newLines, 5, 1));

            CheckLines(doc,
                new string[] { },
                new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "10", "A", }
            );

            doc.ScreenScrollUp_(new GLineChunkSpan(newLines, 6, 2));

            CheckLines(doc,
                new string[] { },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "A", "B", "C", }
            );

            doc.ScreenScrollUp_(new GLineChunkSpan(newLines, 8, 15));

            CheckLines(doc,
                new string[] { },
                new string[] { "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", }
            );
        }

        [Test]
        public void Test_ScreenScrollDown_ScreenIsNotIsolated_1() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);
            doc.ScreenAppend_(GLine.CreateSimpleGLine("11", TextDecoration.Default));
            doc.ScreenAppend_(GLine.CreateSimpleGLine("12", TextDecoration.Default));
            doc.ScreenAppend_(GLine.CreateSimpleGLine("13", TextDecoration.Default));

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", }
            );

            doc.ScreenIsolated = false;

            doc.ScreenScrollDown_(1);

            CheckLines(doc,
                new string[] { "1", "2", },
                new string[] { "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", }
            );

            doc.ScreenScrollDown_(2);

            CheckLines(doc,
                new string[] { },
                new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", }
            );

            doc.ScreenScrollDown_(3);

            CheckLines(doc,
                new string[] { },
                new string[] { "", "", "", "1", "2", "3", "4", "5", "6", "7", }
            );
        }

        [Test]
        public void Test_ScreenScrollDown_ScreenIsNotIsolated_2() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);
            doc.ScreenAppend_(GLine.CreateSimpleGLine("11", TextDecoration.Default));
            doc.ScreenAppend_(GLine.CreateSimpleGLine("12", TextDecoration.Default));
            doc.ScreenAppend_(GLine.CreateSimpleGLine("13", TextDecoration.Default));

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", }
            );

            doc.ScreenIsolated = false;

            doc.ScreenScrollDown_(12);

            CheckLines(doc,
                new string[] { },
                new string[] { "", "", "", "", "", "", "", "", "", "1", }
            );
        }

        [Test]
        public void Test_ScreenScrollDown_ScreenIsIsolated() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);
            doc.ScreenAppend_(GLine.CreateSimpleGLine("11", TextDecoration.Default));
            doc.ScreenAppend_(GLine.CreateSimpleGLine("12", TextDecoration.Default));
            doc.ScreenAppend_(GLine.CreateSimpleGLine("13", TextDecoration.Default));

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", }
            );

            doc.ScreenIsolated = true;

            doc.ScreenScrollDown_(1);

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "", "4", "5", "6", "7", "8", "9", "10", "11", "12", }
            );

            doc.ScreenScrollDown_(2);

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "", "", "", "4", "5", "6", "7", "8", "9", "10", }
            );

            doc.ScreenScrollDown_(15);

            CheckLines(doc,
                new string[] { "1", "2", "3", },
                new string[] { "", "", "", "", "", "", "", "", "", "", }
            );
        }

        [Test]
        public void Test_GetRowIDSpan() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            {
                var span = doc.GetRowIDSpan();
                Assert.AreEqual(1, span.Start);
                Assert.AreEqual(10, span.Length);
            }

            foreach (var line in
                Enumerable.Range(11, 10)
                    .Select(n => n.ToString(NumberFormatInfo.InvariantInfo))
                    .Select(s => GLine.CreateSimpleGLine(s, TextDecoration.Default))) {

                doc.ScreenAppend_(line);
            }

            {
                var span = doc.GetRowIDSpan();
                Assert.AreEqual(1, span.Start);
                Assert.AreEqual(20, span.Length);
            }

            foreach (var line in
                Enumerable.Range(21, 3980)
                    .Select(n => n.ToString(NumberFormatInfo.InvariantInfo))
                    .Select(s => GLine.CreateSimpleGLine(s, TextDecoration.Default))) {

                doc.ScreenAppend_(line);
            }

            {
                var span = doc.GetRowIDSpan();
                Assert.AreEqual(4000 - (GLineBuffer.DEFAULT_CAPACITY + 10) + 1, span.Start);
                Assert.AreEqual(GLineBuffer.DEFAULT_CAPACITY + 10, span.Length);
            }
        }

        [Test]
        public void Test_Apply() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);
            foreach (var line in
                Enumerable.Range(11, 3990)
                    .Select(n => n.ToString(NumberFormatInfo.InvariantInfo))
                    .Select(s => GLine.CreateSimpleGLine(s, TextDecoration.Default))) {

                doc.ScreenAppend_(line);
            }

            var span = doc.GetRowIDSpan();
            Assert.AreEqual(4000 - (GLineBuffer.DEFAULT_CAPACITY + 10) + 1, span.Start);
            Assert.AreEqual(GLineBuffer.DEFAULT_CAPACITY + 10, span.Length);

            // note: the content of each row equal to its RowID

            foreach (var testCase in new[] {
                new { RowID= span.Start, ExpectNotNull = true },
                new { RowID= span.Start - 1, ExpectNotNull = false },
                new { RowID= span.Start + GLineBuffer.DEFAULT_CAPACITY - 1, ExpectNotNull = true },
                new { RowID= span.Start + GLineBuffer.DEFAULT_CAPACITY, ExpectNotNull = true },
                new { RowID= span.Start + GLineBuffer.DEFAULT_CAPACITY + 9, ExpectNotNull = true },
                new { RowID= span.Start + GLineBuffer.DEFAULT_CAPACITY + 10, ExpectNotNull = false },
            }) {
                string actualContent = "xxxxx";
                doc.Apply(testCase.RowID, line => {
                    actualContent = (line != null) ? line.ToNormalString() : null;
                });
                if (testCase.ExpectNotNull) {
                    string expectedContent = testCase.RowID.ToString(NumberFormatInfo.InvariantInfo);
                    Assert.AreEqual(expectedContent, actualContent);
                }
                else {
                    Assert.IsNull(actualContent);
                }
            }
        }

        [Test]
        public void Test_ForEach_1() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);
            foreach (var line in
                Enumerable.Range(11, 3990)
                    .Select(n => n.ToString(NumberFormatInfo.InvariantInfo))
                    .Select(s => GLine.CreateSimpleGLine(s, TextDecoration.Default))) {

                doc.ScreenAppend_(line);
            }

            var span = doc.GetRowIDSpan();
            Assert.AreEqual(4000 - (GLineBuffer.DEFAULT_CAPACITY + 10) + 1, span.Start);
            Assert.AreEqual(GLineBuffer.DEFAULT_CAPACITY + 10, span.Length);

            // note: the content of each row equal to its RowID

            foreach (var testCase in new[] {
                new { StartRowID= span.Start - 10, Length = GLineBuffer.DEFAULT_CAPACITY + 30 },
                new { StartRowID= span.Start, Length = GLineBuffer.DEFAULT_CAPACITY },
                new { StartRowID= span.Start + GLineBuffer.DEFAULT_CAPACITY, Length = 10 },
            }) {
                int nextExpectedRowID = testCase.StartRowID;

                doc.ForEach(testCase.StartRowID, testCase.Length, (rowID, line) => {
                    Assert.AreEqual(nextExpectedRowID, rowID);
                    if (span.Includes(rowID)) {
                        string expectedContent = rowID.ToString(NumberFormatInfo.InvariantInfo);
                        string actualContent = line.ToNormalString();
                        Assert.AreEqual(expectedContent, actualContent);
                    }
                    else {
                        Assert.IsNull(line);
                    }
                    nextExpectedRowID++;
                });

                Assert.AreEqual(testCase.StartRowID + testCase.Length, nextExpectedRowID);
            }
        }

        [Test]
        public void Test_ForEach_2() {
            TestTerminalDoc doc = new TestTerminalDoc();
            SetupScreenLines(doc, 1, 10);

            var span = doc.GetRowIDSpan();
            Assert.AreEqual(1, span.Start);
            Assert.AreEqual(10, span.Length);

            // note: the content of each row equal to its RowID

            foreach (var testCase in new[] {
                new { StartRowID= -10, Length = 30 },
                new { StartRowID= 1, Length = 10 },
            }) {
                int nextExpectedRowID = testCase.StartRowID;

                doc.ForEach(testCase.StartRowID, testCase.Length, (rowID, line) => {
                    Assert.AreEqual(nextExpectedRowID, rowID);
                    if (span.Includes(rowID)) {
                        string expectedContent = rowID.ToString(NumberFormatInfo.InvariantInfo);
                        string actualContent = line.ToNormalString();
                        Assert.AreEqual(expectedContent, actualContent);
                    }
                    else {
                        Assert.IsNull(line);
                    }
                    nextExpectedRowID++;
                });

                Assert.AreEqual(testCase.StartRowID + testCase.Length, nextExpectedRowID);
            }
        }

        private void SetupScreenLines(TestTerminalDoc doc, int start, int length) {
            bool screenIsolated = doc.ScreenIsolated;
            doc.ScreenIsolated = false;
            doc.StoreGLines(
                Enumerable.Range(start, length)
                    .Select(n => n.ToString(NumberFormatInfo.InvariantInfo))
                    .Select(s => GLine.CreateSimpleGLine(s, TextDecoration.Default))
                    .ToArray());
            doc.ScreenIsolated = screenIsolated;
        }

        private void CheckLines(TerminalCharacterDocument doc, IEnumerable<string> expectedLogRows, IEnumerable<string> expectedScreenRows) {
            GLine[] screenRows;
            GLine[] logRows;
            doc.PeekGLines(out screenRows, out logRows);
            var actualScreenRows = screenRows.Select(r => r.ToNormalString()).ToArray();
            var actualLogRows = logRows.Select(r => r.ToNormalString()).ToArray();
            CollectionAssert.AreEqual(expectedScreenRows, actualScreenRows);
            CollectionAssert.AreEqual(expectedLogRows, actualLogRows);
        }
    }

    // A class for exposing protected methods of the TerminalCharacterDocument
    class TestTerminalDoc : TerminalCharacterDocument {

        public TestTerminalDoc()
            : base(80, 10) {
        }

        public bool ScreenIsolated {
            get;
            set;
        }

        protected override bool IsScreenIsolated() {
            return this.ScreenIsolated;
        }

        protected override void OnVisibleAreaSizeChanged(int rows, int cols) {
            // do nothing
        }

        protected override GLine CreateEmptyLine() {
            return GLine.CreateSimpleGLine("", TextDecoration.Default);
        }

        public void ScreenAppend_(GLine line) {
            ScreenAppend(line);
        }

        public GLine ScreenGetRow_(int rowIndex) {
            return ScreenGetRow(rowIndex);
        }

        public void ScreenSetRow_(int rowIndex, GLine line) {
            ScreenSetRow(rowIndex, line);
        }

        public void ScreenGetRows_(int rowIndex, GLineChunkSpan span) {
            ScreenGetRows(rowIndex, span);
        }

        public void ScreenSetRows_(int rowIndex, GLineChunkSpan span) {
            ScreenSetRows(rowIndex, span);
        }

        public void ScreenScrollUp_(int scrollRows) {
            ScreenScrollUp(scrollRows);
        }

        public void ScreenScrollUp_(GLineChunkSpan newRows) {
            ScreenScrollUp(newRows);
        }

        public void ScreenScrollDown_(int scrollRows) {
            ScreenScrollDown(scrollRows);
        }

        public void ScreenScrollUpRegion_(int startRowIndex, int endRowIndex, GLineChunkSpan newRows) {
            ScreenScrollUpRegion(startRowIndex, endRowIndex, newRows);
        }

        public void ScreenScrollDownRegion_(int startRowIndex, int endRowIndex, GLineChunkSpan newRows) {
            ScreenScrollDownRegion(startRowIndex, endRowIndex, newRows);
        }
    }
}

#endif
