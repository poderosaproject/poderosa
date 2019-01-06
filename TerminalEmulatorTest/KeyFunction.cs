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
using System.Windows.Forms;

namespace Poderosa.Terminal {

    [TestFixture]
    public class KeyFunctionTests {
        [Test]
        public void SingleChar1() {
            KeyFunction f = KeyFunction.Parse("C=0x03");
            FixedStyleKeyFunction fs = f.ToFixedStyle();
            Assert.AreEqual(1, fs._keys.Length);
            Assert.AreEqual(1, fs._datas.Length);
            Assert.AreEqual(Keys.C, fs._keys[0]);
            Assert.AreEqual(1, fs._datas[0].Length);
            Assert.AreEqual(3, (int)fs._datas[0][0]);
        }

        [Test]
        public void SingleChar2() {
            KeyFunction f = KeyFunction.Parse("Ctrl+6=0x1F");
            FixedStyleKeyFunction fs = f.ToFixedStyle();
            Assert.AreEqual(1, fs._keys.Length);
            Assert.AreEqual(1, fs._datas.Length);
            Assert.AreEqual(Keys.Control | Keys.D6, fs._keys[0]);
            Assert.AreEqual(1, fs._datas[0].Length);
            Assert.AreEqual(31, (int)fs._datas[0][0]);
        }

        [Test]
        public void String1() {
            KeyFunction f = KeyFunction.Parse("Ctrl+Question=0x010x020x1F0x7F");
            FixedStyleKeyFunction fs = f.ToFixedStyle();
            Assert.AreEqual(1, fs._keys.Length);
            Assert.AreEqual(1, fs._datas.Length);
            Assert.AreEqual(Keys.Control | Keys.OemQuestion, fs._keys[0]);
            Assert.AreEqual(4, fs._datas[0].Length);
            Assert.AreEqual(2, (int)fs._datas[0][1]);
            Assert.AreEqual(127, (int)fs._datas[0][3]);

            Assert.AreEqual("Ctrl+OemQuestion=0x010x020x1F0x7F", f.Format());
        }
        [Test]
        public void String2() {
            KeyFunction f = KeyFunction.Parse("Ctrl+Shift+L=ls -la");
            FixedStyleKeyFunction fs = f.ToFixedStyle();
            Assert.AreEqual(1, fs._keys.Length);
            Assert.AreEqual(1, fs._datas.Length);
            Assert.AreEqual(Keys.Control | Keys.Shift | Keys.L, fs._keys[0]);
            Assert.AreEqual("ls -la", fs._datas[0]);

            Assert.AreEqual("Ctrl+Shift+L=ls -la", f.Format());
        }
        [Test]
        public void Multi1() {
            KeyFunction f = KeyFunction.Parse("Ctrl+Shift+L=ls -la, Ctrl+Shift+F=find -name");
            FixedStyleKeyFunction fs = f.ToFixedStyle();
            Assert.AreEqual(2, fs._keys.Length);
            Assert.AreEqual(2, fs._datas.Length);
            Assert.AreEqual(Keys.Control | Keys.Shift | Keys.L, fs._keys[0]);
            Assert.AreEqual(Keys.Control | Keys.Shift | Keys.F, fs._keys[1]);
            Assert.AreEqual("ls -la", fs._datas[0]);
            Assert.AreEqual("find -name", fs._datas[1]);

            Assert.AreEqual("Ctrl+Shift+L=ls -la, Ctrl+Shift+F=find -name", f.Format());
        }
    }
}
#endif
