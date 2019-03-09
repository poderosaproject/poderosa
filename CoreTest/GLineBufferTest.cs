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

using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Poderosa.Document {

    static class GLineSequenceUtil {
        public static IEnumerable<GLine> Concat(params IEnumerable<GLine>[] sources) {
            return sources.SelectMany(_ => _);
        }
    }

    [TestFixture]
    public class GLinePageTest {

        [Test]
        public void GLinePage_Initial() {
            GLineBuffer.GLinePage page = new GLineBuffer.GLinePage();
            Assert.AreEqual(0, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE, page.Available);
            Assert.AreEqual(true, page.IsEmpty);

            Assert.Throws<IndexOutOfRangeException>(() => {
                page.Apply(0, 10, s => {
                });
            });
        }

        [Test]
        public void GLinePage_Append() {
            GLineBuffer.GLinePage page = new GLineBuffer.GLinePage();
            GLine l1 = new GLine(1);
            GLine l2 = new GLine(1);
            GLine l3 = new GLine(1);

            bool appended;
            // row 1
            appended = page.Append(l1);
            Assert.AreEqual(true, appended);
            Assert.AreEqual(1, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 1, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // row 2
            appended = page.Append(l2);
            Assert.AreEqual(true, appended);
            Assert.AreEqual(2, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 2, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // row 3
            appended = page.Append(l3);
            Assert.AreEqual(true, appended);
            Assert.AreEqual(3, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 3, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // check content
            CheckContent(page, l1, l2, l3);
            CheckInternalContent(page, l1, l2, l3, null);
        }

        [Test]
        public void GLinePage_Append_Full() {
            GLineBuffer.GLinePage page = new GLineBuffer.GLinePage();

            // append rows until buffer-full
            for (int i = 0; i < GLineBuffer.ROWS_PER_PAGE; i++) {
                GLine l = new GLine(1);
                bool appended = page.Append(l);
                Assert.AreEqual(true, appended);
            }

            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE, page.Size);
            Assert.AreEqual(0, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // extra appending will fail
            {
                GLine l = new GLine(1);
                bool appended = page.Append(l);
                Assert.AreEqual(false, appended);   // fails to append
            }

            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE, page.Size);
            Assert.AreEqual(0, page.Available);
            Assert.AreEqual(false, page.IsEmpty);
        }

        [Test]
        public void GLinePage_RemoveFromHead_RemoveSpecifiedRows([Range(0, 4)]int rows) {
            // page with 5 rows
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);
            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            page.RemoveFromHead(rows);

            Assert.AreEqual(5 - rows, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);   // does not change
            Assert.AreEqual(false, page.IsEmpty);

            // check content
            CheckContent(page, lines.Skip(rows).Take(5 - rows).ToArray());
            CheckInternalContent(page,
                (rows >= 1) ? null : lines[0],
                (rows >= 2) ? null : lines[1],
                (rows >= 3) ? null : lines[2],
                (rows >= 4) ? null : lines[3],
                lines[4],
                null
            );
        }

        [Test]
        public void GLinePage_RemoveFromHead_RemoveAll() {
            // page with 5 rows
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            page.RemoveFromHead(5);

            Assert.AreEqual(0, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);   // does not change
            Assert.AreEqual(true, page.IsEmpty);

            // check content
            CheckInternalContent(page, null, null, null, null, null, null);
        }

        [Test]
        public void GLinePage_RemoveFromHead_TooManyRows() {
            // page with 5 rows
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            Assert.Throws<ArgumentException>(() => page.RemoveFromHead(6));

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // check content
            CheckInternalContent(page, lines);
        }

        [Test]
        public void GLinePage_RemoveFromHead_Then_AppendFull() {
            // page with 5 rows
            GLine[] lines = CreateLines(GLineBuffer.ROWS_PER_PAGE);
            GLineBuffer.GLinePage page = new GLineBuffer.GLinePage();
            page.Append(lines[0]);
            page.Append(lines[1]);
            page.Append(lines[2]);
            page.Append(lines[3]);
            page.Append(lines[4]);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            page.RemoveFromHead(4);

            Assert.AreEqual(1, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);   // does not change
            Assert.AreEqual(false, page.IsEmpty);

            // append lines until buffer-full
            for (int i = 0; i < GLineBuffer.ROWS_PER_PAGE - 5; i++) {
                bool appended = page.Append(lines[i + 5]);
                Assert.AreEqual(true, appended, "append {0}", i);
            }

            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 4, page.Size);
            Assert.AreEqual(0, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // check content
            CheckContent(page, lines.Skip(4).Take(GLineBuffer.ROWS_PER_PAGE - 4).ToArray());
            CheckInternalContent(page, null, null, null, null, lines[4], lines[5], lines[6]);

            // extra appending will fail
            {
                GLine l = new GLine(1);
                bool appended = page.Append(l);
                Assert.AreEqual(false, appended);
            }

            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 4, page.Size);
            Assert.AreEqual(0, page.Available);
            Assert.AreEqual(false, page.IsEmpty);
        }

        [Test]
        public void GLinePage_RemoveFromTail_RemoveSpecifiedRows([Values(0, 3)] int extra, [Range(0, 4)] int rows) {
            // page with 5 rows
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            GLineChunk chunk = new GLineChunk(extra + rows + extra);
            page.RemoveFromTail(chunk.Span(extra, rows));

            CollectionAssert.AreEqual(
                GLineSequenceUtil.Concat(
                    Enumerable.Repeat<GLine>(null, extra),
                    lines.Skip(5 - rows),
                    Enumerable.Repeat<GLine>(null, extra)
                ),
                chunk.Array);

            Assert.AreEqual(5 - rows, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5 + rows, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // check content
            CheckContent(page, lines.Take(5 - rows).ToArray());
            CheckInternalContent(page,
                    lines[0],
                    (rows >= 4) ? null : lines[1],
                    (rows >= 3) ? null : lines[2],
                    (rows >= 2) ? null : lines[3],
                    (rows >= 1) ? null : lines[4],
                    null
            );
        }

        [Test]
        public void GLinePage_RemoveFromTail_RemoveAll() {
            // page with 5 rows
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            GLineChunk chunk = new GLineChunk(5);
            page.RemoveFromTail(chunk.Span(0, 5));

            CollectionAssert.AreEqual(lines, chunk.Array);

            Assert.AreEqual(0, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE, page.Available);
            Assert.AreEqual(true, page.IsEmpty);

            // check content
            CollectionAssert.AreEqual(
                new GLine[] {
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                },
                page.Peek(0, 6)
            );
        }

        [Test]
        public void GLinePage_RemoveFromTail_TooManyRows() {
            // page with 5 rows
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            GLineChunk chunk = new GLineChunk(6);
            Assert.Throws<ArgumentException>(() => page.RemoveFromTail(chunk.Span(0, 6)));

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);
        }

        [Test]
        public void GLinePage_RemoveFromTail_Then_AppendFull() {
            // page with 5 rows
            GLine[] lines = CreateLines(GLineBuffer.ROWS_PER_PAGE + 10);
            GLineBuffer.GLinePage page = new GLineBuffer.GLinePage();
            page.Append(lines[0]);
            page.Append(lines[1]);
            page.Append(lines[2]);
            page.Append(lines[3]);
            page.Append(lines[4]);

            Assert.AreEqual(5, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 5, page.Available);

            // remove rows
            GLineChunk chunk = new GLineChunk(4);
            page.RemoveFromTail(chunk.Span(0, 4));

            Assert.AreEqual(1, page.Size);
            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE - 1, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // append lines until buffer-full
            for (int i = 0; i < GLineBuffer.ROWS_PER_PAGE - 1; i++) {
                bool appended = page.Append(lines[i + 5]);
                Assert.AreEqual(true, appended, "append {0}", i);
            }

            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE, page.Size);
            Assert.AreEqual(0, page.Available);
            Assert.AreEqual(false, page.IsEmpty);

            // check content
            CheckContent(page, lines[0], lines[5], lines[6], lines[7], lines[8]);
            CheckInternalContent(page, lines[0], lines[5], lines[6], lines[7], lines[8]);

            // extra appending will fail
            {
                GLine l = new GLine(1);
                bool appended = page.Append(l);
                Assert.AreEqual(false, appended);
            }

            Assert.AreEqual(GLineBuffer.ROWS_PER_PAGE, page.Size);
            Assert.AreEqual(0, page.Available);
            Assert.AreEqual(false, page.IsEmpty);
        }

        [Test]
        public void GLinePage_Apply() {
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            // note: "offset" and "length" are valid, so no exception should be thrown.
            {
                page.Apply(0, 0, s => {
                });
            }

            // note: "offset" and "length" are valid, so no exception should be thrown.
            {
                page.Apply(1, 0, s => {
                });
            }

            {
                List<GLine> copied = new List<GLine>();
                page.Apply(0, 1, s => AddToList(copied, s));
                CollectionAssert.AreEqual(new GLine[] {
                    lines[0],
                }, copied);
            }

            {
                List<GLine> copied = new List<GLine>();
                page.Apply(1, 2, s => AddToList(copied, s));
                CollectionAssert.AreEqual(new GLine[] {
                    lines[1], lines[2],
                }, copied);
            }

            {
                List<GLine> copied = new List<GLine>();
                page.Apply(2, 3, s => AddToList(copied, s));
                CollectionAssert.AreEqual(new GLine[] {
                    lines[2], lines[3], lines[4],
                }, copied);
            }

            {
                List<GLine> copied = new List<GLine>();
                page.Apply(4, 1, s => AddToList(copied, s));
                CollectionAssert.AreEqual(new GLine[] {
                    lines[4],
                }, copied);
            }
        }

        [Test]
        public void GLinePage_Apply_InvalidOffset() {
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.Throws<ArgumentException>(() => {
                page.Apply(-1, 1, s => {
                });
            });
        }

        [Test]
        public void GLinePage_Read_OffsetOutOfRange() {
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.Throws<IndexOutOfRangeException>(() => {
                page.Apply(5, 1, s => {
                });
            });
        }

        [Test]
        public void GLinePage_Read_LengthOutOfRange() {
            GLine[] lines = CreateLines(5);
            GLineBuffer.GLinePage page = SetupPage(lines);

            Assert.Throws<IndexOutOfRangeException>(() => {
                page.Apply(2, 4, s => {
                });
            });
        }

        private GLine[] CreateLines(int num) {
            return Enumerable.Range(0, num).Select(_ => new GLine(1)).ToArray();
        }

        private GLineBuffer.GLinePage SetupPage(GLine[] lines) {
            GLineBuffer.GLinePage page = new GLineBuffer.GLinePage();
            foreach (GLine line in lines) {
                page.Append(line);
            }
            return page;
        }

        private void CheckContent(GLineBuffer.GLinePage page, params GLine[] expectedStaringValues) {
            List<GLine> list = new List<GLine>();
            page.Apply(0, expectedStaringValues.Length, s => AddToList(list, s));
            CollectionAssert.AreEqual(expectedStaringValues, list);
        }

        private void CheckInternalContent(GLineBuffer.GLinePage page, params GLine[] expectedStaringValues) {
            CollectionAssert.AreEqual(expectedStaringValues, page.Peek(0, expectedStaringValues.Length));
        }

        private void AddToList(List<GLine> list, GLineChunkSpan span) {
            for (int i = 0; i < span.Length; i++) {
                list.Add(span.Array[span.Offset + i]);
            }
        }
    }

    [TestFixture]
    public class GLinePageListTest {

        private const int DEFAULT_CAPACITY = 99999;
        private const int MAX_PAGES = (DEFAULT_CAPACITY + GLineBuffer.ROWS_PER_PAGE - 1) / GLineBuffer.ROWS_PER_PAGE + 2;

        [Test]
        public void GLinePageList_Initial() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            Assert.AreEqual(0, list.Size);

            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list.Head;
            });
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list.Tail;
            });
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[0];
            });
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[1];
            });
        }

        [Test]
        public void GLinePageList_Append() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            GLineBuffer.GLinePage page1 = new GLineBuffer.GLinePage();
            GLineBuffer.GLinePage page2 = new GLineBuffer.GLinePage();
            GLineBuffer.GLinePage page3 = new GLineBuffer.GLinePage();

            // page 1
            list.Append(page1);

            Assert.AreEqual(1, list.Size);

            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(page1, list[0]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[1];
            });
            Assert.AreSame(page1, list.Head);
            Assert.AreSame(page1, list.Tail);

            // page 2
            list.Append(page2);

            Assert.AreEqual(2, list.Size);

            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(page1, list[0]);
            Assert.AreSame(page2, list[1]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[2];
            });
            Assert.AreSame(page1, list.Head);
            Assert.AreSame(page2, list.Tail);

            // page 3
            list.Append(page3);

            Assert.AreEqual(3, list.Size);

            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(page1, list[0]);
            Assert.AreSame(page2, list[1]);
            Assert.AreSame(page3, list[2]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[4];
            });
            Assert.AreSame(page1, list.Head);
            Assert.AreSame(page3, list.Tail);
        }

        [Test]
        public void GLinePageList_AppendNewPage_Full() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            // append pages until list-full
            GLineBuffer.GLinePage[] pages = CreatePages(MAX_PAGES);
            foreach (GLineBuffer.GLinePage page in pages) {
                list.Append(page);
            }

            Assert.AreEqual(MAX_PAGES, list.Size);

            // check internal
            for (int i = 0; i < MAX_PAGES; i++) {
                Assert.AreSame(pages[i], list.Peek(i));
            }

            // extra appending page will fail
            Assert.Throws<InvalidOperationException>(() => {
                list.Append(new GLineBuffer.GLinePage());
            });

            Assert.AreEqual(MAX_PAGES, list.Size);
        }

        [Test]
        public void GLinePageList_RemoveHead() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            // append 5 pages
            GLineBuffer.GLinePage[] pages = CreatePages(5);
            foreach (GLineBuffer.GLinePage page in pages) {
                list.Append(page);
            }

            Assert.AreEqual(5, list.Size);

            //-------------------------------------------

            // remove a page
            list.RemoveHead();

            Assert.AreEqual(4, list.Size);

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[1], list[0]);
            Assert.AreSame(pages[2], list[1]);
            Assert.AreSame(pages[3], list[2]);
            Assert.AreSame(pages[4], list[3]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[4];
            });

            // check internal
            Assert.IsNull(list.Peek(0));

            //-------------------------------------------

            // remove a page
            list.RemoveHead();

            Assert.AreEqual(3, list.Size);

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[2], list[0]);
            Assert.AreSame(pages[3], list[1]);
            Assert.AreSame(pages[4], list[2]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[3];
            });

            // check internal
            Assert.IsNull(list.Peek(0));
            Assert.IsNull(list.Peek(1));
        }

        [Test]
        public void GLinePageList_RemoveHead_FromEmpty() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            Assert.AreEqual(0, list.Size);

            list.RemoveHead();

            Assert.AreEqual(0, list.Size);
        }

        [Test]
        public void GLinePageList_RemoveHead_And_Append() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            GLineBuffer.GLinePage[] pages = CreatePages(MAX_PAGES + 10);

            // append 3 pages
            list.Append(pages[0]);
            list.Append(pages[1]);
            list.Append(pages[2]);

            Assert.AreEqual(3, list.Size);

            // [ p0, p1, p2, null, null, ... ]

            //-------------------------------------------------

            // remove a page
            list.RemoveHead();

            Assert.AreEqual(2, list.Size);

            // [ null, p1, p2, null, null, ... ]

            // append a page
            list.Append(pages[3]);

            Assert.AreEqual(3, list.Size);

            // [ null, p1, p2, p3, null, ... ]

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[1], list[0]);
            Assert.AreSame(pages[2], list[1]);
            Assert.AreSame(pages[3], list[2]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[3];
            });

            // check internal
            Assert.IsNull(list.Peek(0));

            //-------------------------------------------------

            // remove a page
            list.RemoveHead();

            Assert.AreEqual(2, list.Size);

            // [ null, null, p2, p3, null, ... ]

            // append a page
            list.Append(pages[4]);

            Assert.AreEqual(3, list.Size);

            // [ null, null, p2, p3, p4, null, ... ]

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[2], list[0]);
            Assert.AreSame(pages[3], list[1]);
            Assert.AreSame(pages[4], list[2]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[3];
            });

            // check internal
            Assert.IsNull(list.Peek(0));
            Assert.IsNull(list.Peek(1));

            //-------------------------------------------------

            // remove a page
            list.RemoveHead();

            Assert.AreEqual(2, list.Size);

            // [ null, null, null, p3, p4, null, ... ]

            // append pages until list-full
            for (int i = 0; i < MAX_PAGES - 2; i++) {
                list.Append(pages[5 + i]);
            }

            Assert.AreEqual(MAX_PAGES, list.Size);

            // [ p(MAX), p(MAX+1), p(MAX+2), p3, p4, p5, ... p(MAX-1) ]

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            for (int i = 0; i < MAX_PAGES; i++) {
                Assert.AreSame(pages[3 + i], list[i]);
            }
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[MAX_PAGES];
            });

            // check internal
            for (int i = 0; i <= 2; i++) {
                Assert.AreSame(pages[MAX_PAGES + i], list.Peek(i));
            }
            for (int i = 3; i < MAX_PAGES; i++) {
                Assert.AreSame(pages[i], list.Peek(i));
            }
        }

        [Test]
        public void GLinePageList_RemoveTail() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            GLineBuffer.GLinePage[] pages = CreatePages(MAX_PAGES + 10);

            // append pages until list-full
            for (int i = 0; i < 2; i++) {
                list.Append(pages[i]);
            }
            for (int i = 0; i < 2; i++) {
                list.RemoveHead();
            }
            for (int i = 0; i < MAX_PAGES; i++) {
                list.Append(pages[2 + i]);
            }

            Assert.AreEqual(MAX_PAGES, list.Size);

            // [ p(MAX), p(MAX+1), p2, p3, p4, p5, ... p(MAX-1) ]

            // check internal
            Assert.AreSame(pages[MAX_PAGES], list.Peek(0));
            Assert.AreSame(pages[MAX_PAGES + 1], list.Peek(1));
            Assert.AreSame(pages[2], list.Peek(2));
            Assert.AreSame(pages[3], list.Peek(3));

            //------------------------------------------------------

            // remove a page
            list.RemoveTail();

            Assert.AreEqual(MAX_PAGES - 1, list.Size);

            // [ p(MAX), null, p2, p3, p4, p5, ... p(MAX-1) ]

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[2], list[0]);
            Assert.AreSame(pages[MAX_PAGES], list[MAX_PAGES - 2]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[MAX_PAGES - 1];
            });

            // check internal
            Assert.AreSame(pages[MAX_PAGES], list.Peek(0));
            Assert.IsNull(list.Peek(1));
            Assert.AreSame(pages[2], list.Peek(2));
            Assert.AreSame(pages[3], list.Peek(3));

            //------------------------------------------------------

            // remove a page
            list.RemoveTail();

            Assert.AreEqual(MAX_PAGES - 2, list.Size);

            // [ null, null, p2, p3, p4, p5, ... p(MAX-1) ]

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[2], list[0]);
            Assert.AreSame(pages[MAX_PAGES - 1], list[MAX_PAGES - 3]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[MAX_PAGES - 2];
            });

            // check internal
            Assert.IsNull(list.Peek(0));
            Assert.IsNull(list.Peek(1));
            Assert.AreSame(pages[2], list.Peek(2));
            Assert.AreSame(pages[3], list.Peek(3));

            //------------------------------------------------------

            // remove a page
            list.RemoveTail();

            Assert.AreEqual(MAX_PAGES - 3, list.Size);

            // [ null, null, p2, p3, p4, p5, ... p(MAX-2), null ]

            // check accessor
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[-1];
            });
            Assert.AreSame(pages[2], list[0]);
            Assert.AreSame(pages[MAX_PAGES - 2], list[MAX_PAGES - 4]);
            Assert.Throws<IndexOutOfRangeException>(() => {
                var page = list[MAX_PAGES - 3];
            });

            // check internal
            Assert.IsNull(list.Peek(0));
            Assert.IsNull(list.Peek(1));
            Assert.AreSame(pages[2], list.Peek(2));
            Assert.AreSame(pages[3], list.Peek(3));
            Assert.IsNull(list.Peek(MAX_PAGES - 1));
            Assert.AreSame(pages[MAX_PAGES - 2], list.Peek(MAX_PAGES - 2));
        }

        [Test]
        public void GLinePageList_RemoveTail_FromEmpty() {
            GLineBuffer.GLinePageList list = new GLineBuffer.GLinePageList(DEFAULT_CAPACITY);

            Assert.AreEqual(0, list.Size);

            list.RemoveTail();

            Assert.AreEqual(0, list.Size);
        }

        private GLineBuffer.GLinePage[] CreatePages(int num) {
            return Enumerable.Range(0, num).Select(_ => new GLineBuffer.GLinePage()).ToArray();
        }
    }

    [TestFixture]
    public class GLineBufferTest {

        private interface ILineAppender {
            void Append(GLineBuffer buff, int lines);
            IEnumerable<GLine> Last(int lines);
            IEnumerable<GLine> Rows();
        }

        // appender using GLineBuffer.Append(GLine)
        private class LineAppender1 : ILineAppender {
            private readonly List<GLine> _lines = new List<GLine>();

            public void Append(GLineBuffer buff, int lines) {
                for (int i = 0; i < lines; i++) {
                    GLine l = GLine.CreateSimpleGLine(i.ToString(), TextDecoration.Default);
                    _lines.Add(l);
                    buff.Append(l);
                }
            }

            public IEnumerable<GLine> Last(int lines) {
                for (int i = _lines.Count - lines; i < _lines.Count; i++) {
                    yield return _lines[i];
                }
            }

            public IEnumerable<GLine> Rows() {
                return _lines;
            }
        }

        // appender using GLineBuffer.Append(IEnumerable<GLine>)
        private class LineAppender2 : ILineAppender {
            private readonly List<GLine> _lines = new List<GLine>();

            public void Append(GLineBuffer buff, int lines) {
                GLine[] newLines = new GLine[lines];
                for (int i = 0; i < lines; i++) {
                    newLines[i] = GLine.CreateSimpleGLine(i.ToString(), TextDecoration.Default);
                }
                _lines.AddRange(newLines);
                buff.Append(newLines);
            }

            public IEnumerable<GLine> Last(int lines) {
                for (int i = _lines.Count - lines; i < _lines.Count; i++) {
                    yield return _lines[i];
                }
            }

            public IEnumerable<GLine> Rows() {
                return _lines;
            }
        }

        [Test]
        public void GLineBuffer_Initial() {
            GLineBuffer buff = new GLineBuffer(99999);

            Assert.IsNull(buff.FirstRowID);
            Assert.IsNull(buff.LastRowID);

            CollectionAssert.IsEmpty(buff.GetAllPages());
        }

        [Test]
        public void GLineBuffer_SmallCapcity_AppendLines1() {
            GLineBuffer_SmallCapcity_AppendLines(new LineAppender1());
        }

        [Test]
        public void GLineBuffer_SmallCapcity_AppendLines2() {
            GLineBuffer_SmallCapcity_AppendLines(new LineAppender2());
        }

        private void GLineBuffer_SmallCapcity_AppendLines(ILineAppender appender) {
            const int PAGE_SIZE = GLineBuffer.ROWS_PER_PAGE;

            GLineBuffer buff = new GLineBuffer(100);

            Assert.IsNull(buff.FirstRowID);
            Assert.IsNull(buff.LastRowID);

            //------------------------------------------

            appender.Append(buff, 100);

            Assert.AreEqual(1, buff.FirstRowID);
            Assert.AreEqual(100, buff.LastRowID);

            CheckBuffContent(buff, appender, 100);
            CheckPages(buff, appender, new int[] { 100 });

            //------------------------------------------

            int repeatNum = buff.GetPageCapacity() + 2;   // enough number to make the internal circular buffer back to the head

            for (int repeat = 0; repeat < repeatNum; repeat++) {

                int expectedRowIDOffset = repeat * PAGE_SIZE;

                // 100 lines already exist in the head of the current page

                appender.Append(buff, 1);

                // one oldest line was removed

                Assert.AreEqual(expectedRowIDOffset + 2, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + 101, buff.LastRowID);

                CheckBuffContent(buff, appender, 100);
                CheckPages(buff, appender, new int[] { 100 });

                //------------------------------------------

                // fill the current page
                appender.Append(buff, PAGE_SIZE - 101);

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE - 99, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE, buff.LastRowID);

                CheckBuffContent(buff, appender, 100);
                CheckPages(buff, appender, new int[] { 100 });

                //------------------------------------------

                appender.Append(buff, 1);

                // a new page was added

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE - 98, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 1, buff.LastRowID);

                CheckBuffContent(buff, appender, 100);
                CheckPages(buff, appender, new int[] { 99, 1 });

                //------------------------------------------

                appender.Append(buff, 98);

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 99, buff.LastRowID);

                CheckBuffContent(buff, appender, 100);
                CheckPages(buff, appender, new int[] { 1, 99 });

                //------------------------------------------

                appender.Append(buff, 1);

                // oldest page was removed

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 1, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 100, buff.LastRowID);

                CheckBuffContent(buff, appender, 100);
                CheckPages(buff, appender, new int[] { 100 });
            }
        }

        [Test]
        public void GLineBuffer_PageSizeCapcity_AppendLines1() {
            GLineBuffer_PageSizeCapcity_AppendLines(new LineAppender1());
        }

        [Test]
        public void GLineBuffer_PageSizeCapcity_AppendLines2() {
            GLineBuffer_PageSizeCapcity_AppendLines(new LineAppender2());
        }

        private void GLineBuffer_PageSizeCapcity_AppendLines(ILineAppender appender) {
            const int PAGE_SIZE = GLineBuffer.ROWS_PER_PAGE;

            GLineBuffer buff = new GLineBuffer(PAGE_SIZE);

            Assert.IsNull(buff.FirstRowID);
            Assert.IsNull(buff.LastRowID);

            //------------------------------------------

            appender.Append(buff, PAGE_SIZE);

            Assert.AreEqual(1, buff.FirstRowID);
            Assert.AreEqual(PAGE_SIZE, buff.LastRowID);

            CheckBuffContent(buff, appender, PAGE_SIZE);
            CheckPages(buff, appender, new int[] { PAGE_SIZE });

            //------------------------------------------

            int repeatNum = buff.GetPageCapacity() + 2;   // enough number to make the internal circular buffer back to the head

            for (int repeat = 0; repeat < repeatNum; repeat++) {

                int expectedRowIDOffset = repeat * PAGE_SIZE;

                // the current page is already filled

                appender.Append(buff, 1);

                // a new page was added

                Assert.AreEqual(expectedRowIDOffset + 2, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 1, buff.LastRowID);

                CheckBuffContent(buff, appender, PAGE_SIZE);
                CheckPages(buff, appender, new int[] { PAGE_SIZE - 1, 1 });

                //------------------------------------------

                appender.Append(buff, PAGE_SIZE - 2);

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + PAGE_SIZE - 1, buff.LastRowID);

                CheckBuffContent(buff, appender, PAGE_SIZE);
                CheckPages(buff, appender, new int[] { 1, PAGE_SIZE - 1 });

                //------------------------------------------

                appender.Append(buff, 1);

                // oldest page was removed

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 1, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + PAGE_SIZE, buff.LastRowID);

                CheckBuffContent(buff, appender, PAGE_SIZE);
                CheckPages(buff, appender, new int[] { PAGE_SIZE });
            }
        }

        [Test]
        public void GLineBuffer_LargeCapcity_AppendLines1() {
            GLineBuffer_LargeCapcity_AppendLines(new LineAppender1());
        }

        [Test]
        public void GLineBuffer_LargeCapcity_AppendLines2() {
            GLineBuffer_LargeCapcity_AppendLines(new LineAppender2());
        }

        private void GLineBuffer_LargeCapcity_AppendLines(ILineAppender appender) {
            const int PAGE_SIZE = GLineBuffer.ROWS_PER_PAGE;
            const int CAPACITY = PAGE_SIZE * 2 + 500;

            GLineBuffer buff = new GLineBuffer(CAPACITY);

            Assert.IsNull(buff.FirstRowID);
            Assert.IsNull(buff.LastRowID);

            //------------------------------------------

            appender.Append(buff, CAPACITY);

            Assert.AreEqual(1, buff.FirstRowID);
            Assert.AreEqual(CAPACITY, buff.LastRowID);

            CheckBuffContent(buff, appender, CAPACITY);
            CheckPages(buff, appender, new int[] { PAGE_SIZE, PAGE_SIZE, 500 });

            //------------------------------------------

            int repeatNum = buff.GetPageCapacity() + 2;   // enough number to make the internal circular buffer back to the head

            for (int repeat = 0; repeat < repeatNum; repeat++) {

                int expectedRowIDOffset = repeat * PAGE_SIZE;

                // (PAGE_SIZE * 2 + 500) lines already exist in the last three pages.

                appender.Append(buff, 1);

                // one oldest line was removed

                Assert.AreEqual(expectedRowIDOffset + 2, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + CAPACITY + 1, buff.LastRowID);

                CheckBuffContent(buff, appender, CAPACITY);
                CheckPages(buff, appender, new int[] { PAGE_SIZE - 1, PAGE_SIZE, 501 });

                //------------------------------------------

                // fill the last page
                appender.Append(buff, PAGE_SIZE - 501);

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE - 499, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + CAPACITY + PAGE_SIZE - 500, buff.LastRowID);

                CheckBuffContent(buff, appender, CAPACITY);
                CheckPages(buff, appender, new int[] { 500, PAGE_SIZE, PAGE_SIZE });

                //------------------------------------------

                appender.Append(buff, 1);

                // a new page was added

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE - 498, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + CAPACITY + PAGE_SIZE - 499, buff.LastRowID);

                CheckBuffContent(buff, appender, CAPACITY);
                CheckPages(buff, appender, new int[] { 499, PAGE_SIZE, PAGE_SIZE, 1 });

                //------------------------------------------

                appender.Append(buff, 498);

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + CAPACITY + PAGE_SIZE - 1, buff.LastRowID);

                CheckBuffContent(buff, appender, CAPACITY);
                CheckPages(buff, appender, new int[] { 1, PAGE_SIZE, PAGE_SIZE, 499 });

                //------------------------------------------

                appender.Append(buff, 1);

                // oldest page was removed

                Assert.AreEqual(expectedRowIDOffset + PAGE_SIZE + 1, buff.FirstRowID);
                Assert.AreEqual(expectedRowIDOffset + CAPACITY + PAGE_SIZE, buff.LastRowID);

                CheckBuffContent(buff, appender, CAPACITY);
                CheckPages(buff, appender, new int[] { PAGE_SIZE, PAGE_SIZE, 500 });
            }
        }

        [Test]
        public void GLineBuffer_AppendManyLinesInSingleCall() {
            GLineBuffer buff = new GLineBuffer(20000);

            ILineAppender appender = new LineAppender2();   // use GLineBuffer.Append(IEnumerable<GLine>)

            appender.Append(buff, 30000); // call GLineBuffer.Append(IEnumerable<GLine>) once

            Assert.AreEqual(10001, buff.FirstRowID);
            Assert.AreEqual(30000, buff.LastRowID);

            CheckBuffContent(buff, appender, 20000);
        }

        [Test]
        public void GLineBuffer_AppendZeroLinesInSingleCall() {
            GLineBuffer buff = new GLineBuffer(100);

            // prepare initial state
            ILineAppender appender = new LineAppender1();
            appender.Append(buff, 300);

            Assert.AreEqual(201, buff.FirstRowID);
            Assert.AreEqual(300, buff.LastRowID);
            CheckBuffContent(buff, appender, 100);

            // add zero lines
            buff.Append(new GLine[0]);

            Assert.AreEqual(201, buff.FirstRowID);
            Assert.AreEqual(300, buff.LastRowID);
            CheckBuffContent(buff, appender, 100);
        }

        [Test]
        public void GLineBuffer_RemoveFromTail() {
            const int PAGE_SIZE = GLineBuffer.ROWS_PER_PAGE;

            GLineBuffer buff = new GLineBuffer(20000);

            ILineAppender appender = new LineAppender1();
            appender.Append(buff, PAGE_SIZE * 3 + 100);

            Assert.AreEqual(1, buff.FirstRowID);
            Assert.AreEqual(PAGE_SIZE * 3 + 100, buff.LastRowID);

            CheckBuffContent(buff, appender, PAGE_SIZE * 3 + 100);
            CheckPages(buff, appender, new int[] { PAGE_SIZE, PAGE_SIZE, PAGE_SIZE, 100 });

            //-------------------------------------------------

            {
                GLineChunk chunk = new GLineChunk(3 + 99 + 3);
                buff.RemoveFromTail(chunk.Span(3, 99));

                CollectionAssert.AreEqual(
                    GLineSequenceUtil.Concat(
                        Enumerable.Repeat<GLine>(null, 3),
                        appender.Last(99),
                        Enumerable.Repeat<GLine>(null, 3)
                    ),
                    chunk.Array);

                Assert.AreEqual(1, buff.FirstRowID);
                Assert.AreEqual(PAGE_SIZE * 3 + 1, buff.LastRowID);

                CheckBuffContent(buff, appender.Rows(), PAGE_SIZE * 3 + 1);
                CheckPages(buff, appender.Rows(), new int[] { PAGE_SIZE, PAGE_SIZE, PAGE_SIZE, 1 });
            }

            //-------------------------------------------------

            {
                GLineChunk chunk = new GLineChunk(3 + 1 + 3);
                buff.RemoveFromTail(chunk.Span(3, 1));

                // the last page was removed

                CollectionAssert.AreEqual(
                    GLineSequenceUtil.Concat(
                        Enumerable.Repeat<GLine>(null, 3),
                       appender.Last(100).Take(1),
                        Enumerable.Repeat<GLine>(null, 3)
                    ),
                    chunk.Array);

                Assert.AreEqual(1, buff.FirstRowID);
                Assert.AreEqual(PAGE_SIZE * 3, buff.LastRowID);

                CheckBuffContent(buff, appender.Rows(), PAGE_SIZE * 3);
                CheckPages(buff, appender.Rows(), new int[] { PAGE_SIZE, PAGE_SIZE, PAGE_SIZE });
            }

            //-------------------------------------------------

            {
                GLineChunk chunk = new GLineChunk(3 + PAGE_SIZE + 3);
                buff.RemoveFromTail(chunk.Span(3, PAGE_SIZE));

                // the last page was removed

                CollectionAssert.AreEqual(
                    GLineSequenceUtil.Concat(
                        Enumerable.Repeat<GLine>(null, 3),
                        appender.Last(PAGE_SIZE + 100).Take(PAGE_SIZE),
                        Enumerable.Repeat<GLine>(null, 3)
                    ),
                    chunk.Array);

                Assert.AreEqual(1, buff.FirstRowID);
                Assert.AreEqual(PAGE_SIZE * 2, buff.LastRowID);

                CheckBuffContent(buff, appender.Rows(), PAGE_SIZE * 2);
                CheckPages(buff, appender.Rows(), new int[] { PAGE_SIZE, PAGE_SIZE });
            }

            //-------------------------------------------------

            {
                GLineChunk chunk = new GLineChunk(3 + PAGE_SIZE + 1 + 3);
                buff.RemoveFromTail(chunk.Span(3, PAGE_SIZE + 1));

                // the last page was removed

                CollectionAssert.AreEqual(
                    GLineSequenceUtil.Concat(
                        Enumerable.Repeat<GLine>(null, 3),
                        appender.Last(PAGE_SIZE * 2 + 101).Take(PAGE_SIZE + 1),
                        Enumerable.Repeat<GLine>(null, 3)
                    ),
                    chunk.Array);

                Assert.AreEqual(1, buff.FirstRowID);
                Assert.AreEqual(PAGE_SIZE - 1, buff.LastRowID);

                CheckBuffContent(buff, appender.Rows(), PAGE_SIZE - 1);
                CheckPages(buff, appender.Rows(), new int[] { PAGE_SIZE - 1 });
            }

            //-------------------------------------------------

            {
                // the last page has (PAGE_SIZE - 1) rows

                appender.Append(buff, 2);

                Assert.AreEqual(1, buff.FirstRowID);
                Assert.AreEqual(PAGE_SIZE + 1, buff.LastRowID);

                CheckBuffContent(buff,
                    GLineSequenceUtil.Concat(
                        appender.Rows().Take(PAGE_SIZE - 1),
                        appender.Last(2)
                    ),
                    PAGE_SIZE + 1);
                CheckPages(buff,
                    GLineSequenceUtil.Concat(
                        appender.Rows().Take(PAGE_SIZE - 1),
                        appender.Last(2)
                    ),
                    new int[] { PAGE_SIZE, 1 });
            }

            //-------------------------------------------------

            {
                GLineChunk chunk = new GLineChunk(3 + PAGE_SIZE + 2 + 3);
                Assert.Throws<ArgumentException>(() => buff.RemoveFromTail(chunk.Span(3, PAGE_SIZE + 2)));

                // no change

                Assert.AreEqual(1, buff.FirstRowID);
                Assert.AreEqual(PAGE_SIZE + 1, buff.LastRowID);

                CheckBuffContent(buff,
                    GLineSequenceUtil.Concat(
                        appender.Rows().Take(PAGE_SIZE - 1),
                        appender.Last(2)
                    ),
                    PAGE_SIZE + 1);
                CheckPages(buff,
                    GLineSequenceUtil.Concat(
                        appender.Rows().Take(PAGE_SIZE - 1),
                        appender.Last(2)
                    ),
                    new int[] { PAGE_SIZE, 1 });
            }

            //-------------------------------------------------

            {
                GLineChunk chunk = new GLineChunk(3 + PAGE_SIZE + 1 + 3);
                buff.RemoveFromTail(chunk.Span(3, PAGE_SIZE + 1));

                CollectionAssert.AreEqual(
                    GLineSequenceUtil.Concat(
                        Enumerable.Repeat<GLine>(null, 3),
                        appender.Rows().Take(PAGE_SIZE - 1),
                        appender.Last(2),
                        Enumerable.Repeat<GLine>(null, 3)
                    ),
                    chunk.Array);

                Assert.IsNull(buff.FirstRowID);
                Assert.IsNull(buff.LastRowID);

                CollectionAssert.IsEmpty(buff.GetAllPages());
            }
        }

        [Test]
        public void GLineBuffer_SetCapacity_Expand() {
            // prepare

            const int BUFFER_CAPACITY = GLineBuffer.ROWS_PER_PAGE * 2 + 200;
            const int LINES_TO_ADD = GLineBuffer.ROWS_PER_PAGE * 5 + 100;

            const int NEW_BUFFER_CAPACITY = GLineBuffer.ROWS_PER_PAGE * 3 + 500;

            GLineBuffer buff = new GLineBuffer(BUFFER_CAPACITY);
            Assert.AreEqual(5, buff.GetPageCapacity());    // confirm capacity of the page list

            ILineAppender appender = new LineAppender1();
            appender.Append(buff, LINES_TO_ADD);

            GLineBuffer.GLinePage[] origPages = buff.GetAllPages();
            Assert.AreEqual(4, origPages.Length);    // confirm that the page list is full 

            Assert.AreEqual(LINES_TO_ADD - BUFFER_CAPACITY + 1, buff.FirstRowID);
            Assert.AreEqual(LINES_TO_ADD, buff.LastRowID);

            CheckBuffContent(buff, appender, BUFFER_CAPACITY);

            //-------------------------------------------------
            // change capacity

            buff.SetCapacity(NEW_BUFFER_CAPACITY);
            Assert.AreEqual(6, buff.GetPageCapacity());    // confirm capacity of the page list

            // all rows have been retained

            Assert.AreEqual(LINES_TO_ADD - BUFFER_CAPACITY + 1, buff.FirstRowID);
            Assert.AreEqual(LINES_TO_ADD, buff.LastRowID);

            CheckBuffContent(buff, appender, BUFFER_CAPACITY);

            CollectionAssert.AreEqual(origPages, buff.GetAllPages());   // pages are not changed

            //-------------------------------------------------
            // check that the new rows can be added

            appender.Append(buff, 10);

            Assert.AreEqual(LINES_TO_ADD - BUFFER_CAPACITY + 1, buff.FirstRowID); // not change
            Assert.AreEqual(LINES_TO_ADD + 10, buff.LastRowID);

            CheckBuffContent(buff, appender, BUFFER_CAPACITY + 10);
        }

        [Test]
        public void GLineBuffer_SetCapacity_Shrink() {
            // prepare

            const int BUFFER_CAPACITY = GLineBuffer.ROWS_PER_PAGE * 2 + 200;
            const int LINES_TO_ADD = GLineBuffer.ROWS_PER_PAGE * 5 + 100;

            const int NEW_BUFFER_CAPACITY = GLineBuffer.ROWS_PER_PAGE * 1 + 500;

            GLineBuffer buff = new GLineBuffer(BUFFER_CAPACITY);
            Assert.AreEqual(5, buff.GetPageCapacity());    // confirm capacity of the page list

            ILineAppender appender = new LineAppender1();
            appender.Append(buff, LINES_TO_ADD);

            GLineBuffer.GLinePage[] origPages = buff.GetAllPages();
            Assert.AreEqual(4, origPages.Length);    // confirm that the page list is full 

            Assert.AreEqual(LINES_TO_ADD - BUFFER_CAPACITY + 1, buff.FirstRowID);
            Assert.AreEqual(LINES_TO_ADD, buff.LastRowID);

            CheckBuffContent(buff, appender, BUFFER_CAPACITY);

            //-------------------------------------------------
            // change capacity

            buff.SetCapacity(NEW_BUFFER_CAPACITY);
            Assert.AreEqual(4, buff.GetPageCapacity());    // confirm capacity of the page list

            // only latest rows have been retained

            Assert.AreEqual(LINES_TO_ADD - NEW_BUFFER_CAPACITY + 1, buff.FirstRowID);
            Assert.AreEqual(LINES_TO_ADD, buff.LastRowID);

            CheckBuffContent(buff, appender, NEW_BUFFER_CAPACITY);

            CollectionAssert.AreEqual(origPages.Skip(1), buff.GetAllPages());   // oldest page has removed

            //-------------------------------------------------
            // check that the new rows can be added

            appender.Append(buff, 10);

            Assert.AreEqual(LINES_TO_ADD - NEW_BUFFER_CAPACITY + 11, buff.FirstRowID);
            Assert.AreEqual(LINES_TO_ADD + 10, buff.LastRowID);

            CheckBuffContent(buff, appender, NEW_BUFFER_CAPACITY);
        }

        [Test]
        public void GLineBuffer_GetLinesByID() {
            const int PAGE_SIZE = GLineBuffer.ROWS_PER_PAGE;

            GLineBuffer buff = new GLineBuffer(PAGE_SIZE + 6);

            ILineAppender appender = new LineAppender1();
            appender.Append(buff, PAGE_SIZE * 2 + 3);

            // Page status:
            //
            //   page[0]
            //     null    x (PAGE_SIZE - 3)
            //     GLine   x 3
            //
            //   page[1]
            //     GLine   x (PAGE_SIZE)
            //
            //   page[2]
            //     GLine   x 3
            //     null    x (PAGE_SIZE - 3)

            CheckPages(buff, appender, new int[] { 3, PAGE_SIZE, 3 });

            // call GetLinesByID with some range patterns:
            //   - starts before the page boundary
            //   - starts at the page boundary
            //   - starts after the page boundary
            //   - ends before the page boundary
            //   - ends at the page boundary
            //   - ends after the page boundary

            for (int start = 0; start < 6; start++) {
                // start = 0 --> page[0][PAGE_SIZE - 3]  Row ID: PAGE_SIZE - 2
                // start = 1 --> page[0][PAGE_SIZE - 2]  Row ID: PAGE_SIZE - 1
                // start = 2 --> page[0][PAGE_SIZE - 1]  Row ID: PAGE_SIZE
                // start = 3 --> page[1][0]              Row ID: PAGE_SIZE + 1
                // start = 4 --> page[1][1]              Row ID: PAGE_SIZE + 2
                // start = 5 --> page[1][2]              Row ID: PAGE_SIZE + 3

                int startRowID = start + PAGE_SIZE - 2;

                for (int end = 0; end < 6; end++) {
                    // end = 0 --> page[1][PAGE_SIZE - 3]  Row ID: PAGE_SIZE * 2 - 2
                    // end = 1 --> page[1][PAGE_SIZE - 2]  Row ID: PAGE_SIZE * 2 - 1
                    // end = 2 --> page[1][PAGE_SIZE - 1]  Row ID: PAGE_SIZE * 2
                    // end = 3 --> page[2][0]              Row ID: PAGE_SIZE * 2 + 1
                    // end = 4 --> page[2][1]              Row ID: PAGE_SIZE * 2 + 2
                    // end = 5 --> page[2][2]              Row ID: PAGE_SIZE * 2 + 3

                    int endRowID = end + PAGE_SIZE * 2 - 2;

                    int rowCount = endRowID - startRowID + 1;

                    var chunk = new GLineChunk(rowCount + 5);
                    buff.GetLinesByID(startRowID, chunk.Span(5, rowCount));

                    CollectionAssert.AreEqual(
                        GLineSequenceUtil.Concat(
                            new GLine[] { null, null, null, null, null },
                            appender.Last(PAGE_SIZE + 6).Skip(start).Take(rowCount)
                        ),
                        chunk.Array
                    );
                }
            }
        }

        [Test]
        public void GLineBuffer_CloneLinesByID() {
            const int PAGE_SIZE = GLineBuffer.ROWS_PER_PAGE;

            GLineBuffer buff = new GLineBuffer(PAGE_SIZE + 6);

            ILineAppender appender = new LineAppender1();
            appender.Append(buff, PAGE_SIZE * 2 + 3);

            // Page status:
            //
            //   page[0]
            //     null    x (PAGE_SIZE - 3)
            //     GLine   x 3
            //
            //   page[1]
            //     GLine   x (PAGE_SIZE)
            //
            //   page[2]
            //     GLine   x 3
            //     null    x (PAGE_SIZE - 3)

            CheckPages(buff, appender, new int[] { 3, PAGE_SIZE, 3 });

            // call GetLinesByID with some range patterns:
            //   - starts before the page boundary
            //   - starts at the page boundary
            //   - starts after the page boundary
            //   - ends before the page boundary
            //   - ends at the page boundary
            //   - ends after the page boundary

            for (int start = 0; start < 6; start++) {
                // start = 0 --> page[0][PAGE_SIZE - 3]  Row ID: PAGE_SIZE - 2
                // start = 1 --> page[0][PAGE_SIZE - 2]  Row ID: PAGE_SIZE - 1
                // start = 2 --> page[0][PAGE_SIZE - 1]  Row ID: PAGE_SIZE
                // start = 3 --> page[1][0]              Row ID: PAGE_SIZE + 1
                // start = 4 --> page[1][1]              Row ID: PAGE_SIZE + 2
                // start = 5 --> page[1][2]              Row ID: PAGE_SIZE + 3

                int startRowID = start + PAGE_SIZE - 2;

                for (int end = 0; end < 6; end++) {
                    // end = 0 --> page[1][PAGE_SIZE - 3]  Row ID: PAGE_SIZE * 2 - 2
                    // end = 1 --> page[1][PAGE_SIZE - 2]  Row ID: PAGE_SIZE * 2 - 1
                    // end = 2 --> page[1][PAGE_SIZE - 1]  Row ID: PAGE_SIZE * 2
                    // end = 3 --> page[2][0]              Row ID: PAGE_SIZE * 2 + 1
                    // end = 4 --> page[2][1]              Row ID: PAGE_SIZE * 2 + 2
                    // end = 5 --> page[2][2]              Row ID: PAGE_SIZE * 2 + 3

                    int endRowID = end + PAGE_SIZE * 2 - 2;

                    int rowCount = endRowID - startRowID + 1;

                    var chunk = new GLineChunk(rowCount + 5);
                    GLine resusable = new GLine(1);
                    if (chunk.Array.Length >= 8) {
                        chunk.Array[7] = resusable;
                    }
                    buff.CloneLinesByID(startRowID, chunk.Span(5, rowCount), true);

                    for (int i = 0; i < 5; i++) {
                        Assert.IsNull(chunk.Array[i]);
                    }
                    String[] expectedContents = appender.Last(PAGE_SIZE + 6).Skip(start).Take(rowCount).Select(l => l.ToNormalString()).ToArray();
                    String[] actualContents = chunk.Array.Skip(5).Select(l => l.ToNormalString()).ToArray();
                    CollectionAssert.AreEqual(expectedContents, actualContents);

                    if (chunk.Array.Length >= 8) {
                        Assert.AreSame(resusable, chunk.Array[7]);
                    }
                }
            }
        }

        private GLine[] CreateLines(int num) {
            return Enumerable.Range(0, num).Select(_ => new GLine(1)).ToArray();
        }

        private void CheckPages(GLineBuffer buff, ILineAppender appender, int[] pageSize) {
            CheckPages(buff, appender.Last(pageSize.Sum()), pageSize);
        }

        private void CheckPages(GLineBuffer buff, IEnumerable<GLine> lineSource, int[] pageSize) {
            GLineBuffer.GLinePage[] pages = buff.GetAllPages();
            Assert.AreEqual(pageSize.Length, pages.Length);

            int skipLines = 0;
            for (int i = 0; i < pageSize.Length; i++) {
                int expectedSize = pageSize[i];
                Assert.AreEqual(expectedSize, pages[i].Size);

                List<GLine> list = new List<GLine>();
                pages[i].Apply(0, expectedSize, s => {
                    for (int k = 0; k < s.Length; k++) {
                        list.Add(s.Array[s.Offset + k]);
                    }
                });
                CollectionAssert.AreEqual(lineSource.Skip(skipLines).Take(expectedSize), list);
                skipLines += expectedSize;
            }
        }

        private void CheckBuffContent(GLineBuffer buff, ILineAppender appender, int expectedSize) {
            CheckBuffContent(buff, appender.Last(expectedSize), expectedSize);
        }

        private void CheckBuffContent(GLineBuffer buff, IEnumerable<GLine> lineSource, int expectedSize) {
            var chunk = new GLineChunk(expectedSize);
            buff.GetLinesByID(buff.FirstRowID.Value, chunk.Span(0, expectedSize));
            CollectionAssert.AreEqual(lineSource.Take(expectedSize), chunk.Array);
        }
    }
}

#endif // UNITTEST
