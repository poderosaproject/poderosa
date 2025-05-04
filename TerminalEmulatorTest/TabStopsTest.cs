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
using System.Linq;

using NUnit.Framework;

namespace Poderosa.Terminal {

    [TestFixture]
    public class TabStopsTest {

        [TestCase(0, new uint[] { })]
        [TestCase(1, new uint[] { 0x01010101u })]
        [TestCase(32, new uint[] { 0x01010101u })]
        [TestCase(33, new uint[] { 0x01010101u, 0x01010101u })]
        public void TestExtendNotCleard(int width, uint[] expected) {
            TabStops t = new TabStops();
            t.Extend(width);
            Assert.AreEqual(expected, t.GetRawBitsForTest());
        }

        [TestCase(0, new uint[] { 0x12345678u })]
        [TestCase(1, new uint[] { 0x12345678u })]
        [TestCase(32, new uint[] { 0x12345678u })]
        [TestCase(33, new uint[] { 0x12345678u, 0x01010101u })]
        public void TestExtendAfterSet(int width, uint[] expected) {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x12345678u });
            t.Extend(width);
            Assert.AreEqual(expected, t.GetRawBitsForTest());
        }

        [TestCase(0, new uint[] { })]
        [TestCase(1, new uint[] { 0u })]
        [TestCase(32, new uint[] { 0u })]
        [TestCase(33, new uint[] { 0u, 0u })]
        public void TestExtendAfterClear(int width, uint[] expected) {
            TabStops t = new TabStops();
            t.Clear();
            t.Extend(width);
            Assert.AreEqual(expected, t.GetRawBitsForTest());
        }

        [Test]
        public void TestInitialize() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0xffffffffu, 0xffffffffu, 0xffffffffu });
            t.Initialize();
            Assert.AreEqual(new uint[] { 0x01010101u, 0x01010101u, 0x01010101u }, t.GetRawBitsForTest());
        }

        [TestCase(0, new uint[] { 0x00000001u, 0x00000000u })]
        [TestCase(1, new uint[] { 0x00000002u, 0x00000000u })]
        [TestCase(31, new uint[] { 0x80000000u, 0x00000000u })]
        [TestCase(32, new uint[] { 0x00000000u, 0x00000001u })]
        [TestCase(63, new uint[] { 0x00000000u, 0x80000000u })]
        [TestCase(64, new uint[] { 0x00000000u, 0x00000000u, 0x01010101u })] // extended automatically
        [TestCase(65, new uint[] { 0x00000000u, 0x00000000u, 0x01010103u })] // extended automatically
        public void TestSet(int index, uint[] expected) {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x00000000u, 0x00000000u });
            t.Set(index);
            Assert.AreEqual(expected, t.GetRawBitsForTest());
        }

        [TestCase(0, new uint[] { 0xfffffffeu, 0xffffffffu })]
        [TestCase(1, new uint[] { 0xfffffffdu, 0xffffffffu })]
        [TestCase(31, new uint[] { 0x7fffffffu, 0xffffffffu })]
        [TestCase(32, new uint[] { 0xffffffffu, 0xfffffffeu })]
        [TestCase(63, new uint[] { 0xffffffffu, 0x7fffffffu })]
        [TestCase(64, new uint[] { 0xffffffffu, 0xffffffffu, 0x01010100u })] // extended automatically
        [TestCase(65, new uint[] { 0xffffffffu, 0xffffffffu, 0x01010101u })] // extended automatically
        public void TestUnset(int index, uint[] expected) {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0xffffffffu, 0xffffffffu });
            t.Unset(index);
            Assert.AreEqual(expected, t.GetRawBitsForTest());
        }

        [Test]
        public void TestClear() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0xffffffffu, 0xffffffffu });
            t.Clear();
            Assert.AreEqual(new uint[] { 0x00000000u, 0x00000000u }, t.GetRawBitsForTest());
        }

        [TestCase(new uint[] { }, new int[] { })]
        [TestCase(new uint[] { 0x00000001u }, new int[] { 0 })]
        [TestCase(new uint[] { 0x00000002u }, new int[] { 1 })]
        [TestCase(new uint[] { 0x90000016u }, new int[] { 1, 2, 4, 28, 31 })]
        [TestCase(new uint[] { 0x00000000u, 0x00000000u, 0x00000100u }, new int[] { 72 })]
        public void TestGetIndices(uint[] initial, int[] expected) {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(initial);
            int[] result = t.GetIndices().ToArray();
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestGetNextTabStop_1() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0xffffffffu, 0xffffffffu });
            for (int p = 0; p <= 62; p++) {
                int? expected = p + 1;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 63; p <= 64; p++) {
                int? expected = null;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetNextTabStop_2() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x55555555u, 0x55555555u });
            for (int p = 0; p <= 61; p++) {
                int? expected = (p - p % 2) + 2;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 62; p <= 64; p++) {
                int? expected = null;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetNextTabStop_3() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x49249249u, 0x92492492u });
            for (int p = 0; p <= 62; p++) {
                int? expected = (p - p % 3) + 3;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 63; p <= 64; p++) {
                int? expected = null;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetNextTabStop_63() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x00000000u, 0x80000000u });
            for (int p = 0; p <= 62; p++) {
                int? expected = 63;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 63; p <= 64; p++) {
                int? expected = null;
                int? actual = t.GetNextTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetPrevTabStop_1() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0xffffffffu, 0xffffffffu });

            for (int p = 0; p <= 0; p++) {
                int? expected = null;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }
            
            for (int p = 1; p <= 64; p++) {
                int? expected = p - 1;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }
            
            for (int p = 65; p <= 127; p++) {
                int? expected = 63;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetPrevTabStop_2() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x55555555u, 0x55555555u });

            for (int p = 0; p <= 0; p++) {
                int? expected = null;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 1; p <= 64; p++) {
                int? expected = p + p % 2 - 2;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 65; p <= 127; p++) {
                int? expected = 62;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetPrevTabStop_3() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x49249249u, 0x92492492u });

            for (int p = 0; p <= 0; p++) {
                int? expected = null;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 1; p <= 63; p++) {
                int? expected = (p - 1) - (p - 1) % 3;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 64; p <= 127; p++) {
                int? expected = 63;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }

        [Test]
        public void TestGetPrevTabStop_63() {
            TabStops t = new TabStops();
            t.SetRawBitsForTest(new uint[] { 0x00000001u, 0x00000000u });

            for (int p = 0; p <= 0; p++) {
                int? expected = null;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }

            for (int p = 1; p <= 64; p++) {
                int? expected = 0;
                int? actual = t.GetPrevTabStop(p);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
#endif
