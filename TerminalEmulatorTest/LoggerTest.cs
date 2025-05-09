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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Poderosa.Document;
using Poderosa.Protocols;
using Poderosa.Preferences;

using NUnit.Framework;

namespace Poderosa.Terminal {

    internal class MockBinaryLogger : IBinaryLogger {

        public readonly List<byte> Written = new List<byte>();
        public int CloseCount = 0;
        public int FlushCount = 0;
        public int AutoFlushCount = 0;

        public void Write(ByteDataFragment frag) {
            Written.AddRange(frag.Buffer.Skip(frag.Offset).Take(frag.Length));
        }

        public void Close() {
            CloseCount++;
        }

        public void Flush() {
            FlushCount++;
        }

        public void AutoFlush() {
            AutoFlushCount++;
        }
    }

    internal class MockTextLogger : ITextLogger {

        public string Written = "";
        public int CloseCount = 0;
        public int FlushCount = 0;
        public int AutoFlushCount = 0;

        public void WriteLine(GLine line) {
            Written += line.ToNormalString() + "\r\n";
        }

        public void Comment(string comment) {
            Written += "[[" + comment + "]]\r\n";
        }

        public void Close() {
            CloseCount++;
        }

        public void Flush() {
            FlushCount++;
        }

        public void AutoFlush() {
            AutoFlushCount++;
        }

        public void ForceWriteLine(GLine line) {
            Written += "<" + line.ToNormalString() + ">\r\n";
        }
    }

    internal class MockXmlLogger : IXmlLogger {

        public string Written = "";
        public int CloseCount = 0;
        public int FlushCount = 0;
        public int AutoFlushCount = 0;

        public void Write(char ch) {
            Written += ch;
        }

        public void EscapeSequence(char[] body) {
            Written += "<esc>" + new string(body) + "</esc>";
        }

        public void Comment(string comment) {
            Written += "<comment>" + comment + "</comment>";
        }

        public void Close() {
            CloseCount++;
        }

        public void Flush() {
            FlushCount++;
        }

        public void AutoFlush() {
            AutoFlushCount++;
        }
    }

    [TestFixture]
    class LogServiceTest {

        private void WriteAll(LogService logService, string s) {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            logService.BinaryLogger.Write(new ByteDataFragment(bytes, 0, bytes.Length));

            GLine gline = GLine.CreateSimpleGLine(s, TextDecoration.Default, GLineZOrder.CreateForTest(0));
            logService.TextLogger.WriteLine(gline);

            foreach (char c in s) {
                logService.XmlLogger.Write(c);
            }
        }

        [Test]
        public void TestAddRemoveLogger() {
            LogService logService = new LogService();

            Assert.AreEqual(typeof(NullBinaryLogger), logService.BinaryLogger.GetType());
            Assert.AreEqual(typeof(NullTextLogger), logService.TextLogger.GetType());
            Assert.AreEqual(typeof(NullXmlLogger), logService.XmlLogger.GetType());

            WriteAll(logService, "A"); // not sent to any logger

            MockBinaryLogger b1 = new MockBinaryLogger();
            logService.AddBinaryLogger(b1);
            Assert.AreEqual(typeof(MockBinaryLogger), logService.BinaryLogger.GetType());

            WriteAll(logService, "B"); // send to: b1

            MockTextLogger t1 = new MockTextLogger();
            logService.AddTextLogger(t1);
            Assert.AreEqual(typeof(MockTextLogger), logService.TextLogger.GetType());

            WriteAll(logService, "C"); // send to: b1, t1

            MockXmlLogger x1 = new MockXmlLogger();
            logService.AddXmlLogger(x1);
            Assert.AreEqual(typeof(MockXmlLogger), logService.XmlLogger.GetType());

            WriteAll(logService, "D"); // send to: b1, t1, x1

            MockBinaryLogger b2 = new MockBinaryLogger();
            logService.AddBinaryLogger(b2);
            Assert.AreEqual(typeof(BinaryLoggerList), logService.BinaryLogger.GetType());

            WriteAll(logService, "E"); // send to: b1, t1, x1, b2

            MockTextLogger t2 = new MockTextLogger();
            logService.AddTextLogger(t2);
            Assert.AreEqual(typeof(TextLoggerList), logService.TextLogger.GetType());

            WriteAll(logService, "F"); // send to: b1, t1, x1, b2, t2

            MockXmlLogger x2 = new MockXmlLogger();
            logService.AddXmlLogger(x2);
            Assert.AreEqual(typeof(XmlLoggerList), logService.XmlLogger.GetType());

            WriteAll(logService, "G"); // send to: b1, t1, x1, b2, t2, x2

            MockBinaryLogger b3 = new MockBinaryLogger();
            logService.AddBinaryLogger(b3);
            Assert.AreEqual(typeof(BinaryLoggerList), logService.BinaryLogger.GetType());

            WriteAll(logService, "H"); // send to: b1, t1, x1, b2, t2, x2, b3

            MockTextLogger t3 = new MockTextLogger();
            logService.AddTextLogger(t3);
            Assert.AreEqual(typeof(TextLoggerList), logService.TextLogger.GetType());

            WriteAll(logService, "I"); // send to: b1, t1, x1, b2, t2, x2, b3, t3

            MockXmlLogger x3 = new MockXmlLogger();
            logService.AddXmlLogger(x3);
            Assert.AreEqual(typeof(XmlLoggerList), logService.XmlLogger.GetType());

            WriteAll(logService, "J"); // send to: b1, t1, x1, b2, t2, x2, b3, t3, x3

            logService.RemoveBinaryLogger(b2);
            Assert.AreEqual(typeof(BinaryLoggerList), logService.BinaryLogger.GetType());

            WriteAll(logService, "K"); // send to: b1, t1, x1, t2, x2, b3, t3, x3

            logService.RemoveTextLogger(t2);
            Assert.AreEqual(typeof(TextLoggerList), logService.TextLogger.GetType());

            WriteAll(logService, "L"); // send to: b1, t1, x1, x2, b3, t3, x3

            logService.RemoveXmlLogger(x2);
            Assert.AreEqual(typeof(XmlLoggerList), logService.XmlLogger.GetType());

            WriteAll(logService, "M"); // send to: b1, t1, x1, b3, t3, x3

            logService.RemoveBinaryLogger(b1);
            Assert.AreEqual(typeof(MockBinaryLogger), logService.BinaryLogger.GetType());

            WriteAll(logService, "N"); // send to: t1, x1, b3, t3, x3

            logService.RemoveTextLogger(t1);
            Assert.AreEqual(typeof(MockTextLogger), logService.TextLogger.GetType());

            WriteAll(logService, "O"); // send to: x1, b3, t3, x3

            logService.RemoveXmlLogger(x1);
            Assert.AreEqual(typeof(MockXmlLogger), logService.XmlLogger.GetType());

            WriteAll(logService, "P"); // send to: b3, t3, x3

            logService.RemoveBinaryLogger(b3);
            Assert.AreEqual(typeof(NullBinaryLogger), logService.BinaryLogger.GetType());

            WriteAll(logService, "Q"); // send to: t3, x3

            logService.RemoveTextLogger(t3);
            Assert.AreEqual(typeof(NullTextLogger), logService.TextLogger.GetType());

            WriteAll(logService, "R"); // send to: x3

            logService.RemoveXmlLogger(x3);
            Assert.AreEqual(typeof(NullXmlLogger), logService.XmlLogger.GetType());

            WriteAll(logService, "S"); // not sent to any logger

            Assert.AreEqual(Encoding.UTF8.GetBytes("BCDEFGHIJKLM"), b1.Written.ToArray());
            Assert.AreEqual(Encoding.UTF8.GetBytes("EFGHIJ"), b2.Written.ToArray());
            Assert.AreEqual(Encoding.UTF8.GetBytes("HIJKLMNOP"), b3.Written.ToArray());

            Assert.AreEqual("C\r\nD\r\nE\r\nF\r\nG\r\nH\r\nI\r\nJ\r\nK\r\nL\r\nM\r\nN\r\n", t1.Written);
            Assert.AreEqual("F\r\nG\r\nH\r\nI\r\nJ\r\nK\r\n", t2.Written);
            Assert.AreEqual("I\r\nJ\r\nK\r\nL\r\nM\r\nN\r\nO\r\nP\r\nQ\r\n", t3.Written);

            Assert.AreEqual("DEFGHIJKLMNO", x1.Written);
            Assert.AreEqual("GHIJKL", x2.Written);
            Assert.AreEqual("JKLMNOPQR", x3.Written);

            Assert.AreEqual(1, b1.FlushCount);
            Assert.AreEqual(1, b2.FlushCount);
            Assert.AreEqual(1, b3.FlushCount);

            Assert.AreEqual(1, t1.FlushCount);
            Assert.AreEqual(1, t2.FlushCount);
            Assert.AreEqual(1, t3.FlushCount);

            Assert.AreEqual(1, x1.FlushCount);
            Assert.AreEqual(1, x2.FlushCount);
            Assert.AreEqual(1, x3.FlushCount);
        }

        [Test]
        public void TestBinaryLoggerList() {
            MockBinaryLogger l1 = new MockBinaryLogger();
            MockBinaryLogger l2 = new MockBinaryLogger();
            BinaryLoggerList list = new BinaryLoggerList(new IBinaryLogger[] { l1, l2 });

            byte[] bytes = new byte[100];
            Random random = new Random();
            random.NextBytes(bytes);
            list.Write(new ByteDataFragment(bytes, 0, bytes.Length));

            list.AutoFlush();

            list.AutoFlush();

            list.AutoFlush();

            list.Flush();

            list.Flush();

            list.Close();

            Assert.AreEqual(bytes, l1.Written.ToArray());
            Assert.AreEqual(bytes, l2.Written.ToArray());

            Assert.AreEqual(3, l1.AutoFlushCount);
            Assert.AreEqual(3, l2.AutoFlushCount);

            Assert.AreEqual(2, l1.FlushCount);
            Assert.AreEqual(2, l2.FlushCount);

            Assert.AreEqual(1, l1.CloseCount);
            Assert.AreEqual(1, l2.CloseCount);
        }

        [Test]
        public void TestTextLoggerList() {
            MockTextLogger l1 = new MockTextLogger();
            MockTextLogger l2 = new MockTextLogger();
            TextLoggerList list = new TextLoggerList(new ITextLogger[] { l1, l2 });

            byte[] bytes = new byte[100];
            Random random = new Random();
            random.NextBytes(bytes);
            list.WriteLine(GLine.CreateSimpleGLine("abc", TextDecoration.Default, GLineZOrder.CreateForTest(0)));
            list.ForceWriteLine(GLine.CreateSimpleGLine("def", TextDecoration.Default, GLineZOrder.CreateForTest(0)));
            list.Comment("xyz");

            list.AutoFlush();

            list.AutoFlush();

            list.AutoFlush();

            list.Flush();

            list.Flush();

            list.Close();

            Assert.AreEqual("abc\r\n<def>\r\n[[xyz]]\r\n", l1.Written);
            Assert.AreEqual("abc\r\n<def>\r\n[[xyz]]\r\n", l2.Written);

            Assert.AreEqual(3, l1.AutoFlushCount);
            Assert.AreEqual(3, l2.AutoFlushCount);

            Assert.AreEqual(2, l1.FlushCount);
            Assert.AreEqual(2, l2.FlushCount);

            Assert.AreEqual(1, l1.CloseCount);
            Assert.AreEqual(1, l2.CloseCount);
        }

        [Test]
        public void TestXmlLoggerList() {
            MockXmlLogger l1 = new MockXmlLogger();
            MockXmlLogger l2 = new MockXmlLogger();
            XmlLoggerList list = new XmlLoggerList(new IXmlLogger[] { l1, l2 });

            byte[] bytes = new byte[100];
            Random random = new Random();
            random.NextBytes(bytes);
            list.Write('a');
            list.EscapeSequence(new char[] { '\u001b', '[', 'H' });
            list.Comment("xyz");

            list.AutoFlush();

            list.AutoFlush();

            list.AutoFlush();

            list.Flush();

            list.Flush();

            list.Close();

            Assert.AreEqual("a<esc>\u001b[H</esc><comment>xyz</comment>", l1.Written);
            Assert.AreEqual("a<esc>\u001b[H</esc><comment>xyz</comment>", l2.Written);

            Assert.AreEqual(3, l1.AutoFlushCount);
            Assert.AreEqual(3, l2.AutoFlushCount);

            Assert.AreEqual(2, l1.FlushCount);
            Assert.AreEqual(2, l2.FlushCount);

            Assert.AreEqual(1, l1.CloseCount);
            Assert.AreEqual(1, l2.CloseCount);
        }
    }

}

#endif
