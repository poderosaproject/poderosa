// Copyright 2004-2017 The Poderosa Project.
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
using System.Text;

namespace Poderosa.Util.Collections {

    [TestFixture]
    public class ConvertingCollectionTests {

        public class V {
            public int _value;
            public V(int v) {
                _value = v;
            }
            public static implicit operator V(int v) {
                return new V(v);
            }
        }

        [Test]
        public void Test1() {
            V[] t = new V[] { 10, 20, 30 };
            StringBuilder bld = new StringBuilder();
            //delegateが効いていることを確認すべく2倍にしてみる
            foreach (string x in new ConvertingEnumerable<V, string>(t, delegate(V v) {
                return (v._value * 2).ToString();
            })) {
                bld.Append(x);
            }
            Assert.AreEqual("204060", bld.ToString());
        }
        [Test]
        public void Test2() {
            int[] t = new int[] { 10, 20, 30 };
            StringBuilder bld = new StringBuilder();
            //単なるIEnumerableはint[]等にも適用可能
            foreach (string x in new ConvertingEnumerable<string>(t, delegate(object v) {
                return v.ToString();
            })) {
                bld.Append(x);
            }
            Assert.AreEqual("102030", bld.ToString());
        }
    }

}
#endif
