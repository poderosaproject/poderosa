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
using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Poderosa {

    public static class UnitTestUtil {
        public static void Trace(string text) {
            Console.Out.WriteLine(text);
            Debug.WriteLine(text);
        }

        public static void Trace(string fmt, params object[] args) {
            Trace(String.Format(fmt, args));
        }

        public static string DumpStructuredText(StructuredText st) {
            StringWriter wr = new StringWriter();
            new TextStructuredTextWriter(wr).Write(st);
            wr.Close();
            return wr.ToString();
        }
    }

    [TestFixture]
    public class RuntimeUtilTests {
        [Test] //ごくふつうのケース
        public void ParseColor1() {
            Color c1 = Color.Red;
            Color c2 = ParseUtil.ParseColor("Red", Color.White);
            Assert.AreEqual(c1, c2);
        }
        [Test] //hex 8ケタのARGB
        public void ParseColor2() {
            Color c1 = Color.FromArgb(10, 20, 30);
            Color c2 = ParseUtil.ParseColor("FF0A141E", Color.White);
            Assert.AreEqual(c1, c2);
        }
        [Test] //hex 6ケタのRGB
        public void ParseColor3() {
            Color c1 = Color.FromArgb(10, 20, 30);
            Color c2 = ParseUtil.ParseColor("0A141E", Color.White);
            Assert.AreEqual(c1, c2);
        }
        [Test] //KnownColorでもOK
        public void ParseColor4() {
            Color c1 = Color.FromKnownColor(KnownColor.WindowText);
            Color c2 = ParseUtil.ParseColor("WindowText", Color.White);
            Assert.AreEqual(c1, c2);
        }
        [Test] //ARGBは一致でもColorの比較としては不一致
        public void ParseColor5() {
            Color c1 = Color.Blue;
            Color c2 = ParseUtil.ParseColor("0000FF", Color.White);
            Assert.AreNotEqual(c1, c2);
            Assert.AreEqual(c1.ToArgb(), c2.ToArgb());
        }
        [Test]　//知らない名前はラストの引数と一致
        public void ParseColor6() {
            Color c1 = Color.White;
            Color c2 = ParseUtil.ParseColor("asdfghj", Color.White); //パースできない場合
            Assert.AreEqual(c1, c2);
        }
        [Test] //ついでなので仕様確認 ToString()ではだめですよ
        public void ColorToString() {
            Color c1 = Color.Red;
            Color c2 = Color.FromName("Red");
            Color c3 = Color.FromKnownColor(KnownColor.WindowFrame);
            Color c4 = Color.FromArgb(255, 0, 0);

            Assert.AreEqual(c1, c2);
            Assert.AreEqual("Red", c1.Name);
            Assert.AreEqual("Red", c2.Name);
            Assert.AreEqual("WindowFrame", c3.Name);
            Assert.AreEqual("ffff0000", c4.Name);
        }
    }

}
#endif
