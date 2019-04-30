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
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Poderosa.Document {

    [TestFixture]
    public class RowIDSpanTest {
        [TestCase(10, 10, 5, 4, 10, 0)] // [10..19] x [5..8] => []
        [TestCase(10, 10, 5, 5, 10, 0)] // [10..19] x [5..9] => []
        [TestCase(10, 10, 5, 6, 10, 1)] // [10..19] x [5..10] => [10]
        [TestCase(10, 10, 5, 15, 10, 10)] // [10..19] x [5..19] => [10..19]
        [TestCase(10, 10, 5, 16, 10, 10)] // [10..19] x [5..20] => [10..19]
        [TestCase(10, 10, 10, 0, 10, 0)] // [10..19] x [] => []
        [TestCase(10, 10, 10, 10, 10, 10)] // [10..19] x [10..19] => [10..19]
        [TestCase(10, 10, 11, 1, 11, 1)] // [10..19] x [11] => [11]
        [TestCase(10, 10, 11, 9, 11, 9)] // [10..19] x [11..19] => [11..19]
        [TestCase(10, 10, 11, 10, 11, 9)] // [10..19] x [11..20] => [11..19]
        [TestCase(10, 10, 19, 1, 19, 1)] // [10..19] x [19] => [19]
        [TestCase(10, 10, 19, 2, 19, 1)] // [10..19] x [19..20] => [19]
        [TestCase(10, 10, 20, 5, 10, 0)] // [10..19] x [20..24] => []
        [TestCase(10, 10, 21, 5, 10, 0)] // [10..19] x [21..25] => []
        [TestCase(10, 0, 5, 6, 10, 0)] // [] x [5..10] => []
        [TestCase(10, 0, 10, 0, 10, 0)] // [] x [] => []
        [TestCase(10, 0, 10, 10, 10, 0)] // [] x [10..19] => []
        [TestCase(10, 0, 11, 1, 10, 0)] // [] x [11] => []
        public void TestIntersect(int start1, int len1, int start2, int len2, int expectedStart, int expectedLength) {

            var s1 = new RowIDSpan(start1, len1);
            var s2 = new RowIDSpan(start2, len2);

            var r = s1.Intersect(s2);

            Assert.AreEqual(expectedStart, r.Start);
            Assert.AreEqual(expectedLength, r.Length);
        }

        [TestCase(10, 5, 9, false)]
        [TestCase(10, 5, 10, true)]
        [TestCase(10, 5, 11, true)]
        [TestCase(10, 5, 12, true)]
        [TestCase(10, 5, 13, true)]
        [TestCase(10, 5, 14, true)]
        [TestCase(10, 5, 15, false)]
        public void TestIncludes(int start, int len, int testRowId, bool expected) {
            Assert.AreEqual(expected, new RowIDSpan(start, len).Includes(testRowId));
        }
    }
}

#endif
