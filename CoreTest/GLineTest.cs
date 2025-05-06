// Copyright 2025 The Poderosa Project.
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

using NUnit.Framework;

namespace Poderosa.Document {

    [TestFixture]
    class GLineZOrderTest {

        [Test]
        public void TestCompareTo() {
            uint[] baseValues = new uint[] {
                0u,
                1u,
                UInt32.MaxValue - GLineZOrder.MAX_DIFFERENCE - 1u,
                UInt32.MaxValue - GLineZOrder.MAX_DIFFERENCE,
                UInt32.MaxValue - GLineZOrder.MAX_DIFFERENCE + 1u,
                UInt32.MaxValue - GLineZOrder.MAX_DIFFERENCE + 2u,
            };

            foreach (uint a in baseValues) {
                {
                    GLineZOrder za = GLineZOrder.CreateForTest(a);
                    Assert.AreEqual(0, za.CompareTo(za));
                }

                {
                    GLineZOrder za = GLineZOrder.CreateForTest(a);
                    GLineZOrder zb = GLineZOrder.CreateForTest(a + 1u);
                    Assert.AreEqual(-1, za.CompareTo(zb));
                    Assert.AreEqual(1, zb.CompareTo(za));
                }

                {
                    GLineZOrder za = GLineZOrder.CreateForTest(a);
                    GLineZOrder zb = GLineZOrder.CreateForTest(a + GLineZOrder.MAX_DIFFERENCE);
                    Assert.AreEqual(-1, za.CompareTo(zb));
                    Assert.AreEqual(1, zb.CompareTo(za));
                }
            }
        }


        [Test]
        [TestCase(0u, 0u, true)]
        [TestCase(1u, 2u, false)]
        public void TestEquals(uint a, uint b, bool expected) {
            GLineZOrder za = GLineZOrder.CreateForTest(a);
            GLineZOrder zb = GLineZOrder.CreateForTest(b);
            Assert.AreEqual(expected, za.Equals(zb));
            Assert.AreEqual(expected, za.Equals((object)zb));
            Assert.AreEqual(expected, za == zb);
            Assert.AreEqual(!expected, za != zb);
        }

        [Test]
        public void TestEqualsWithObject() {
            GLineZOrder za = GLineZOrder.CreateForTest(123);
            Assert.IsFalse(za.Equals(null));
            Assert.IsFalse(za.Equals(new object()));
            Assert.IsFalse(za.Equals(System.Tuple.Create(123)));
            Assert.IsFalse(za.Equals(123));
        }
    }

    [TestFixture]
    class GLineZOrderManagerTest {

        [Test]
        [TestCase(0u, 1u)]
        [TestCase(1u, 2u)]
        [TestCase(UInt32.MaxValue, 0u)]
        public void TestIncrement(uint cur, uint expected) {
            var zMan = new GLineZOrder.Manager();
            zMan.SetCurrentForTest(cur);
            Assert.AreEqual(GLineZOrder.CreateForTest(cur), zMan.Current);
            GLineZOrder next = zMan.Increment();
            Assert.AreEqual(GLineZOrder.CreateForTest(expected), next);
            Assert.AreEqual(GLineZOrder.CreateForTest(expected), zMan.Current);
        }
    }

    [TestFixture]
    class GLineColumnSpanTest {

        [Test]
        [TestCase(100u, 10, 20, 100u, 9, 10, 100u, 9, 20)]
        [TestCase(100u, 10, 20, 100u, 9, 20, 100u, 9, 20)]
        [TestCase(100u, 10, 20, 100u, 9, 21, 100u, 9, 21)]
        [TestCase(100u, 10, 20, 100u, 10, 11, 100u, 10, 20)]
        [TestCase(100u, 10, 20, 100u, 10, 20, 100u, 10, 20)]
        [TestCase(100u, 10, 20, 100u, 19, 20, 100u, 10, 20)]
        [TestCase(100u, 10, 20, 100u, 20, 21, 100u, 10, 21)]
        public void TestMerge(uint z1, int start1, int end1, uint z2, int start2, int end2, uint expectedZ, int expectedStart, int expectedEnd) {
            GLineColumnSpan s1 = new GLineColumnSpan(GLineZOrder.CreateForTest(z1), start1, end1);
            GLineColumnSpan s2 = new GLineColumnSpan(GLineZOrder.CreateForTest(z2), start2, end2);
            GLineColumnSpan? merged = s1.Merge(s2);
            Assert.IsTrue(merged.HasValue);
            Assert.AreEqual(GLineZOrder.CreateForTest(expectedZ), merged.Value.Z);
            Assert.AreEqual(expectedStart, merged.Value.Start);
            Assert.AreEqual(expectedEnd, merged.Value.End);
        }

        [Test]
        [TestCase(100u, 10, 20, 100u, 8, 9)]
        [TestCase(100u, 10, 20, 100u, 21, 22)]
        [TestCase(100u, 10, 20, 101u, 9, 21)]
        public void TestMergeFail(uint z1, int start1, int end1, uint z2, int start2, int end2) {
            GLineColumnSpan s1 = new GLineColumnSpan(GLineZOrder.CreateForTest(z1), start1, end1);
            GLineColumnSpan s2 = new GLineColumnSpan(GLineZOrder.CreateForTest(z2), start2, end2);
            GLineColumnSpan? merged = s1.Merge(s2);
            Assert.IsFalse(merged.HasValue);
        }
    }
}
#endif
