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
using System.Linq;

namespace Poderosa.Document {

    [TestFixture]
    class GLineScreenBufferTest {

        private readonly GLine[] _glines = Enumerable.Range(0, 200).Select(_ => new GLine(1)).ToArray();

        private IEnumerable<GLine> GLines(int start, int count) {
            for (int i = 0; i < count; i++) {
                yield return _glines[start + i];
            }
        }

        private IEnumerable<GLine> Nulls(int count) {
            for (int i = 0; i < count; i++) {
                yield return null;
            }
        }

        [TestCase(1, 64)]
        [TestCase(64, 64)]
        [TestCase(65, 96)]
        [TestCase(96, 96)]
        [TestCase(97, 128)]
        public void Constructor_NormalSize(int size, int expectedBufferSize) {
            var buff = new GLineScreenBuffer(size, (index) => _glines[index]);

            Assert.AreEqual(size, buff.Size);
            Assert.AreEqual(0, buff.StartIndex);

            CheckNullRows(buff);

            var expectedScreenRows = GLines(0, size);
            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(-1)]
        [TestCase(0)]
        public void Constructor_InvalidSize(int size) {
            Assert.Throws<ArgumentException>(() => new GLineScreenBuffer(size, (index) => _glines[index]));
        }

        [TestCase(40, 23, 64)]
        [TestCase(40, 24, 64)]
        public void ConstructorWithSartIndex_NoWrapAround(int startIndex, int size, int expectedBufferSize) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            Assert.AreEqual(size, buff.Size);
            Assert.AreEqual(startIndex, buff.StartIndex);

            CheckNullRows(buff);

            var expectedScreenRows = GLines(0, size);
            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(40, 25, 64)]
        [TestCase(40, 64, 64)]
        public void ConstructorWithSartIndex_WrapAround(int startIndex, int size, int expectedBufferSize) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            Assert.AreEqual(size, buff.Size);
            Assert.AreEqual(startIndex, buff.StartIndex);

            CheckNullRows(buff);

            int expectedNonWrapAroundRows = expectedBufferSize - startIndex;
            int expectedWrapAroundRows = size - expectedNonWrapAroundRows;

            var expectedScreenRows = GLines(0, size);
            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        // BufferSize = 64
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 64)]   // to end
        [TestCase(30, 0)]
        [TestCase(30, 1)]
        [TestCase(30, 34)]  // to end
        [TestCase(63, 0)]
        [TestCase(63, 1)]   // to end
        public void CopyToBuffer_NoWrapAround(int buffIndex, int length) {
            var buff = new GLineScreenBuffer(1, (index) => (GLine)null);
            // BufferSize = 64, filled by null.
            buff.InternalCopyToBuffer(_glines, 3, buffIndex, length);

            var expectedBuffContent =
                GLineSequenceUtil.Concat(
                    Nulls(buffIndex),
                    GLines(3, length),
                    Nulls(64 - buffIndex - length)
                );

            CollectionAssert.AreEqual(expectedBuffContent, buff.GetRawBuff());
        }

        // BufferSize = 64
        [TestCase(30, 35)]
        [TestCase(30, 36)]
        [TestCase(30, 64)]
        [TestCase(63, 2)]
        [TestCase(63, 3)]
        [TestCase(63, 64)]
        public void CopyToBuffer_WrapAround(int buffIndex, int length) {
            var buff = new GLineScreenBuffer(1, (index) => (GLine)null);
            // BufferSize = 64, filled by null.
            buff.InternalCopyToBuffer(_glines, 3, buffIndex, length);

            int expectedNonWrapAroundRows = 64 - buffIndex;
            int expectedWrapAroundRows = length - expectedNonWrapAroundRows;

            var expectedBuffContent =
                GLineSequenceUtil.Concat(
                    GLines(3 + expectedNonWrapAroundRows, expectedWrapAroundRows),
                    Nulls(64 - expectedWrapAroundRows - expectedNonWrapAroundRows),
                    GLines(3, expectedNonWrapAroundRows)
                );

            CollectionAssert.AreEqual(expectedBuffContent, buff.GetRawBuff());
        }

        // BufferSize = 64
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 64)]   // to end
        [TestCase(30, 0)]
        [TestCase(30, 1)]
        [TestCase(30, 34)]  // to end
        [TestCase(63, 0)]
        [TestCase(63, 1)]   // to end
        public void CopyFromBuffer_NoWrapAround(int buffIndex, int length) {
            var buff = new GLineScreenBuffer(64, (index) => _glines[index]);
            // BufferSize = 64, filled by GLines.
            GLine[] copiedRows = new GLine[length + 3];
            buff.InternalCopyFromBuffer(buffIndex, copiedRows, 3, length);

            var expectedCopiedRows =
                GLineSequenceUtil.Concat(
                    Nulls(3),
                    GLines(buffIndex, length)
                );

            CollectionAssert.AreEqual(expectedCopiedRows, copiedRows);
        }

        // BufferSize = 64
        [TestCase(30, 35)]
        [TestCase(30, 36)]
        [TestCase(30, 64)]
        [TestCase(63, 2)]
        [TestCase(63, 3)]
        [TestCase(63, 64)]
        public void CopyFromBuffer_WrapAround(int buffIndex, int length) {
            var buff = new GLineScreenBuffer(64, (index) => _glines[index]);
            // BufferSize = 64, filled by GLines.
            GLine[] copiedRows = new GLine[length + 3];
            buff.InternalCopyFromBuffer(buffIndex, copiedRows, 3, length);

            int expectedNonWrapAroundRows = 64 - buffIndex;
            int expectedWrapAroundRows = length - expectedNonWrapAroundRows;

            var expectedCopiedRows =
                GLineSequenceUtil.Concat(
                    Nulls(3),
                    GLines(buffIndex, expectedNonWrapAroundRows),
                    GLines(0, expectedWrapAroundRows)
                );

            CollectionAssert.AreEqual(expectedCopiedRows, copiedRows);
        }

        // BufferSize = 64
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(0, 64)]   // to end
        [TestCase(30, 0)]
        [TestCase(30, 1)]
        [TestCase(30, 34)]  // to end
        [TestCase(63, 0)]
        [TestCase(63, 1)]   // to end
        public void ClearBuffer_NoWrapAround(int buffIndex, int length) {
            var buff = new GLineScreenBuffer(64, (index) => _glines[index]);
            // BufferSize = 64, filled by GLines.
            buff.InternalClearBuffer(buffIndex, length);

            var expectedBuffContent =
                GLineSequenceUtil.Concat(
                    GLines(0, buffIndex),
                    Nulls(length),
                    GLines(buffIndex + length, 64 - buffIndex - length)
                );

            CollectionAssert.AreEqual(expectedBuffContent, buff.GetRawBuff());
        }

        // BufferSize = 64
        [TestCase(30, 35)]
        [TestCase(30, 36)]
        [TestCase(30, 64)]
        [TestCase(63, 2)]
        [TestCase(63, 3)]
        [TestCase(63, 64)]
        public void ClearBuffer_WrapAround(int buffIndex, int length) {
            var buff = new GLineScreenBuffer(64, (index) => _glines[index]);
            // BufferSize = 64, filled by GLines.
            buff.InternalClearBuffer(buffIndex, length);

            int expectedNonWrapAroundRows = 64 - buffIndex;
            int expectedWrapAroundRows = length - expectedNonWrapAroundRows;

            var expectedBuffContent =
                GLineSequenceUtil.Concat(
                    Nulls(expectedWrapAroundRows),
                    GLines(expectedWrapAroundRows, 64 - expectedWrapAroundRows - expectedNonWrapAroundRows),
                    Nulls(expectedNonWrapAroundRows)
                );

            CollectionAssert.AreEqual(expectedBuffContent, buff.GetRawBuff());
        }

        [TestCase(10, 20, 0)]
        [TestCase(10, 20, 1)]
        [TestCase(10, 20, 10)]
        public void ExtendHead_NoWrapAround(int initialStartIndex, int initialSize, int extendSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);
            GLineChunk chunk = GetFilledGLineChunk(3 + extendSize + 3);

            buff.ExtendHead(chunk.Span(3, extendSize));

            int expectedStartIndex = initialStartIndex - extendSize;

            Assert.AreEqual(expectedStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize + extendSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    chunk.Array.Skip(3).Take(extendSize),
                    GLines(0, initialSize)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(10, 20, 11)]
        [TestCase(0, 20, 1)]
        [TestCase(10, 20, 44)]
        public void ExtendHead_WrapAround(int initialStartIndex, int initialSize, int extendSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);
            GLineChunk chunk = GetFilledGLineChunk(3 + extendSize + 3);

            buff.ExtendHead(chunk.Span(3, extendSize));

            int expectedStartIndex = 64 - (extendSize - initialStartIndex);

            Assert.AreEqual(expectedStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize + extendSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    chunk.Array.Skip(3).Take(extendSize),
                    GLines(0, initialSize)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(10, 20, 45, 128)]
        [TestCase(54, 20, 45, 128)] // wrap-arounded
        public void ExtendHead_Reallocate(int initialStartIndex, int initialSize, int extendSize, int expectedBufferSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + extendSize + 3);

            buff.ExtendHead(chunk.Span(3, extendSize));

            Assert.AreEqual(0, buff.StartIndex);
            Assert.AreEqual(extendSize + initialSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    chunk.Array.Skip(3).Take(extendSize),
                    GLines(0, initialSize)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(34, 20, 0)]
        [TestCase(34, 20, 1)]
        [TestCase(34, 20, 10)]
        public void ExtendTail_NoWrapAround(int initialStartIndex, int initialSize, int extendSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + extendSize + 3);

            buff.ExtendTail(chunk.Span(3, extendSize));

            Assert.AreEqual(initialStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize + extendSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, initialSize),
                    chunk.Array.Skip(3).Take(extendSize)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(34, 20, 11)]
        [TestCase(44, 20, 1)]
        [TestCase(34, 20, 44)]
        public void ExtendTail_WrapAround(int initialStartIndex, int initialSize, int extendSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + extendSize + 3);

            buff.ExtendTail(chunk.Span(3, extendSize));

            Assert.AreEqual(initialStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize + extendSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, initialSize),
                    chunk.Array.Skip(3).Take(extendSize)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(34, 20, 45, 128)]
        [TestCase(54, 20, 45, 128)] // wrap-arounded
        public void ExtendTail_Reallocate(int initialStartIndex, int initialSize, int extendSize, int expectedBufferSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + extendSize + 3);

            buff.ExtendTail(chunk.Span(3, extendSize));

            Assert.AreEqual(0, buff.StartIndex);
            Assert.AreEqual(initialSize + extendSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, initialSize),
                    chunk.Array.Skip(3).Take(extendSize)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(10, 20, 5)]
        [TestCase(44, 20, 19)]
        public void ShrinkHead_NoWrapAround(int initialStartIndex, int initialSize, int shrinkSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            buff.ShrinkHead(shrinkSize);

            Assert.AreEqual(initialStartIndex + shrinkSize, buff.StartIndex);
            Assert.AreEqual(initialSize - shrinkSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows = GLines(shrinkSize, initialSize - shrinkSize);

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(54, 20, 10)]
        [TestCase(54, 20, 11)]
        public void ShrinkHead_WrapAround(int initialStartIndex, int initialSize, int shrinkSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            buff.ShrinkHead(shrinkSize);

            int expectedStartIndex = shrinkSize - (64 - initialStartIndex);

            Assert.AreEqual(expectedStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize - shrinkSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows = GLines(shrinkSize, initialSize - shrinkSize);

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(20, 20)]
        [TestCase(20, 21)]
        public void ShrinkHead_InvalidParam(int initialSize, int shrinkSize) {
            var buff = new GLineScreenBuffer(0, initialSize, (index) => _glines[index]);

            Assert.Throws<ArgumentException>(() => buff.ShrinkHead(shrinkSize));
        }

        [TestCase(10, 20, 5)]
        [TestCase(0, 20, 19)]
        public void ShrinkTail_NoWrapAround(int initialStartIndex, int initialSize, int shrinkSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            buff.ShrinkTail(shrinkSize);

            Assert.AreEqual(initialStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize - shrinkSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows = GLines(0, initialSize - shrinkSize);

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(54, 20, 10)]
        [TestCase(54, 20, 11)]
        [TestCase(54, 20, 19)]
        public void ShrinkTail_WrapAround(int initialStartIndex, int initialSize, int shrinkSize) {
            var buff = new GLineScreenBuffer(initialStartIndex, initialSize, (index) => _glines[index]);

            buff.ShrinkTail(shrinkSize);

            Assert.AreEqual(initialStartIndex, buff.StartIndex);
            Assert.AreEqual(initialSize - shrinkSize, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows = GLines(0, initialSize - shrinkSize);

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(20, 20)]
        [TestCase(20, 21)]
        public void ShrinkTail_InvalidParam(int initialSize, int shrinkSize) {
            var buff = new GLineScreenBuffer(0, initialSize, (index) => _glines[index]);

            Assert.Throws<ArgumentException>(() => buff.ShrinkTail(shrinkSize));
        }

        [TestCase(0, 20, 0, 0)]
        [TestCase(0, 20, 0, 20)]
        [TestCase(0, 20, 3, 10)]
        [TestCase(0, 20, 20, 0)]
        [TestCase(54, 20, 0, 0)]   // splitted in 2 regions
        [TestCase(54, 20, 0, 20)]   // splitted in 2 regions
        [TestCase(54, 20, 3, 10)]   // splitted in 2 regions
        [TestCase(54, 20, 20, 0)]   // splitted in 2 regions
        public void GetRows_Success(int startIndex, int size, int rowIndex, int length) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = new GLineChunk(30);
            buff.GetRows(rowIndex, chunk.Span(3, length));

            var expectedDest =
                GLineSequenceUtil.Concat(
                    Nulls(3),
                    GLines(rowIndex, length),
                    Nulls(30 - 3 - length)
                );

            CollectionAssert.AreEqual(expectedDest, chunk.Array);
        }

        [TestCase(0, 20, -1, 1)]
        [TestCase(0, 20, 3, -1)]
        [TestCase(0, 20, 0, 21)]
        [TestCase(0, 20, 3, 18)]
        public void GetRows_Error(int startIndex, int size, int rowIndex, int length) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = new GLineChunk(30);

            Assert.Throws<ArgumentException>(() => buff.GetRows(rowIndex, chunk.Span(3, length)));
        }

        [TestCase(0, 20, 0, 0)]
        [TestCase(0, 20, 0, 20)]
        [TestCase(0, 20, 3, 10)]
        [TestCase(0, 20, 20, 0)]
        [TestCase(54, 20, 0, 0)]   // splitted in 2 regions
        [TestCase(54, 20, 0, 20)]   // splitted in 2 regions
        [TestCase(54, 20, 3, 10)]   // splitted in 2 regions
        [TestCase(54, 20, 20, 0)]   // splitted in 2 regions
        public void SetRows_Success(int startIndex, int size, int rowIndex, int length) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + length + 3);

            buff.SetRows(rowIndex, chunk.Span(3, length));

            Assert.AreEqual(startIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, rowIndex),
                    chunk.Array.Skip(3).Take(length),
                    GLines(rowIndex + length, size - rowIndex - length)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(0, 20, -1, 1)]
        [TestCase(0, 20, 3, -1)]
        [TestCase(0, 20, 0, 21)]
        [TestCase(0, 20, 3, 18)]
        public void SetRows_Error(int startIndex, int size, int rowIndex, int length) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + length + 3);

            Assert.Throws<ArgumentException>(() => buff.SetRows(rowIndex, chunk.Span(3, length)));

            Assert.AreEqual(startIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);
        }

        [TestCase(10, 20, 0, 10)]
        [TestCase(10, 20, 1, 11)]
        [TestCase(10, 20, 10, 20)]
        [TestCase(10, 20, 20, 30)]  // screen size
        [TestCase(54, 20, 10, 0)]
        [TestCase(54, 20, 11, 1)]
        [TestCase(54, 20, 20, 10)]  // screen size
        public void ScrollUp_NotExceedScreenSize(int startIndex, int size, int scrollRows, int expectedNextStartIndex) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollUp(chunk.Span(3, scrollRows));

            Assert.AreEqual(expectedNextStartIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(scrollRows, size - scrollRows),
                    chunk.Array.Skip(3).Take(scrollRows)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(10, 20, 21, 30)]  // exceeds screen size
        [TestCase(10, 20, 200, 30)]  // exceeds screen size
        [TestCase(54, 20, 21, 10)]  // exceeds screen size
        [TestCase(54, 20, 200, 10)]  // exceeds screen size
        public void ScrollUp_ExceedScreenSize(int startIndex, int size, int scrollRows, int expectedNextStartIndex) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollUp(chunk.Span(3, scrollRows));

            Assert.AreEqual(expectedNextStartIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows = chunk.Array.Skip(3 + scrollRows - size).Take(size);

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(10, 20, 0, 10)]
        [TestCase(10, 20, 1, 9)]
        [TestCase(10, 20, 10, 0)]
        [TestCase(10, 20, 20, 54)]  // screen size
        [TestCase(54, 20, 10, 44)]
        [TestCase(54, 20, 11, 43)]
        [TestCase(54, 20, 20, 34)]  // screen size
        public void ScrollDown_NotExceedScreenSize(int startIndex, int size, int scrollRows, int expectedNextStartIndex) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollDown(chunk.Span(3, scrollRows));

            Assert.AreEqual(expectedNextStartIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    chunk.Array.Skip(3).Take(scrollRows),
                    GLines(0, size - scrollRows)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(10, 20, 21, 54)]  // exceeds screen size
        [TestCase(10, 20, 200, 54)]  // exceeds screen size
        [TestCase(54, 20, 21, 34)]  // exceeds screen size
        [TestCase(54, 20, 200, 34)]  // exceeds screen size
        public void ScrollDown_ExceedScreenSize(int startIndex, int size, int scrollRows, int expectedNextStartIndex) {
            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollDown(chunk.Span(3, scrollRows));

            Assert.AreEqual(expectedNextStartIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows = chunk.Array.Skip(3).Take(size);

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(54, 20, 5, 15, 5, 15, 0)]
        [TestCase(54, 20, 5, 15, 5, 15, 1)]
        [TestCase(54, 20, 5, 15, 5, 15, 10)]    // scroll region size
        [TestCase(54, 20, 0, 15, 0, 15, 1)]
        [TestCase(54, 20, -1, 15, 0, 15, 1)]
        [TestCase(54, 20, 5, 20, 5, 20, 1)]
        [TestCase(54, 20, 5, 21, 5, 20, 1)]
        [TestCase(54, 20, 0, 20, 0, 20, 1)]    // scroll entire of the screen
        [TestCase(54, 20, 0, 20, 0, 20, 20)]    // scroll entire of the screen
        public void ScrollUpRegion_NotExceedRegionSize(
            int startIndex, int size,
            int startRowIndex, int endRowIndex,
            int actualStartRowIndex, int actualEndRowIndex,
            int scrollRows) {

            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollUpRegion(startRowIndex, endRowIndex, chunk.Span(3, scrollRows));

            Assert.AreEqual(startIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, actualStartRowIndex),
                    GLines(actualStartRowIndex + scrollRows, (actualEndRowIndex - actualStartRowIndex) - scrollRows),
                    chunk.Array.Skip(3).Take(scrollRows),
                    GLines(actualEndRowIndex, size - actualEndRowIndex)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(54, 20, 5, 15, 5, 15, 11)]
        [TestCase(54, 20, 5, 15, 5, 15, 200)]
        public void ScrollUpRegion_ExceedRegionSize(
            int startIndex, int size,
            int startRowIndex, int endRowIndex,
            int actualStartRowIndex, int actualEndRowIndex,
            int scrollRows) {

            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollUpRegion(startRowIndex, endRowIndex, chunk.Span(3, scrollRows));

            Assert.AreEqual(startIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            int regionSize = actualEndRowIndex - actualStartRowIndex;

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, actualStartRowIndex),
                    chunk.Array.Skip(3 + scrollRows - regionSize).Take(regionSize),
                    GLines(actualEndRowIndex, size - actualEndRowIndex)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(54, 20, 5, 15, 5, 15, 0)]
        [TestCase(54, 20, 5, 15, 5, 15, 1)]
        [TestCase(54, 20, 5, 15, 5, 15, 10)]    // scroll region size
        [TestCase(54, 20, 0, 15, 0, 15, 1)]
        [TestCase(54, 20, -1, 15, 0, 15, 1)]
        [TestCase(54, 20, 5, 20, 5, 20, 1)]
        [TestCase(54, 20, 5, 21, 5, 20, 1)]
        [TestCase(54, 20, 0, 20, 0, 20, 1)]    // scroll entire of the screen
        [TestCase(54, 20, 0, 20, 0, 20, 20)]    // scroll entire of the screen
        public void ScrollDownRegion_NotExceedRegionSize(
            int startIndex, int size,
            int startRowIndex, int endRowIndex,
            int actualStartRowIndex, int actualEndRowIndex,
            int scrollRows) {

            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollDownRegion(startRowIndex, endRowIndex, chunk.Span(3, scrollRows));

            Assert.AreEqual(startIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, actualStartRowIndex),
                    chunk.Array.Skip(3).Take(scrollRows),
                    GLines(actualStartRowIndex, (actualEndRowIndex - actualStartRowIndex) - scrollRows),
                    GLines(actualEndRowIndex, size - actualEndRowIndex)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        [TestCase(54, 20, 5, 15, 5, 15, 11)]
        [TestCase(54, 20, 5, 15, 5, 15, 200)]
        public void ScrollDownRegion_ExceedRegionSize(
            int startIndex, int size,
            int startRowIndex, int endRowIndex,
            int actualStartRowIndex, int actualEndRowIndex,
            int scrollRows) {

            var buff = new GLineScreenBuffer(startIndex, size, (index) => _glines[index]);

            GLineChunk chunk = GetFilledGLineChunk(3 + scrollRows + 3);

            buff.ScrollDownRegion(startRowIndex, endRowIndex, chunk.Span(3, scrollRows));

            Assert.AreEqual(startIndex, buff.StartIndex);
            Assert.AreEqual(size, buff.Size);

            CheckNullRows(buff);

            int regionSize = actualEndRowIndex - actualStartRowIndex;

            var expectedScreenRows =
                GLineSequenceUtil.Concat(
                    GLines(0, actualStartRowIndex),
                    chunk.Array.Skip(3).Take(regionSize),
                    GLines(actualEndRowIndex, size - actualEndRowIndex)
                );

            CollectionAssert.AreEqual(expectedScreenRows, GetScreenRows(buff));
        }

        private GLineChunk GetFilledGLineChunk(int rows) {
            GLineChunk chunk = new GLineChunk(rows);
            for (int i = 0; i < chunk.Array.Length; i++) {
                chunk.Array[i] = new GLine(1);
            }
            return chunk;
        }

        private IEnumerable<GLine> GetScreenRows(GLineScreenBuffer buff) {
            for (int i = 0; i < buff.Size; i++) {
                yield return buff[i];
            }
        }

        // checks whether the row is null or not.
        // an active row must have a GLine object.
        // an inactive row must be null.
        private void CheckNullRows(GLineScreenBuffer buff) {
            int startIndex = buff.StartIndex;
            int size = buff.Size;
            var rawBuff = buff.GetRawBuff();

            int endIndex = (startIndex + size) % rawBuff.Length;

            for (int i = 0; i < rawBuff.Length; i++) {
                int offset = (i < startIndex) ? (i + rawBuff.Length - startIndex) : (i - startIndex);
                if (offset < size) {
                    Assert.IsNotNull(rawBuff[i]);
                }
                else {
                    Assert.IsNull(rawBuff[i]);
                }
            }
        }

    }
}

#endif
