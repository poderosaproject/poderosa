// Copyright 2004-2025 The Poderosa Project.
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
using Poderosa.ConnectionParam;
using Poderosa.Plugins;
using Poderosa.Serializing;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Poderosa.Terminal {

    [TestFixture]
    public class TerminalSettingsSerializeTests {

        private TerminalSettingsSerializer _terminalSettingsSerializer;

        [OneTimeSetUp]
        public void Init() {
            _terminalSettingsSerializer = new TerminalSettingsSerializer();
            new TerminalEmulatorPlugin().InitializePluginForTest(new PoderosaWorldForTest());
        }

        [Test]
        public void Test0() {
            SerializationOptions opt = new SerializationOptions();
            TerminalSettings ts1 = new TerminalSettings();
            StructuredText storage = _terminalSettingsSerializer.Serialize(ts1, opt);
            TerminalSettings ts2 = (TerminalSettings)_terminalSettingsSerializer.Deserialize(storage);

            Assert.AreEqual(EncodingType.ISO8859_1, ts1.Encoding);
            Assert.AreEqual(false, ts1.LocalEcho);
            Assert.AreEqual(NewLine.CR, ts1.TransmitNL);
            Assert.AreEqual(LineFeedRule.Normal, ts1.LineFeedRule);
            Assert.AreEqual(TerminalType.XTerm256Color, ts1.TerminalType);

            Assert.AreEqual(EncodingType.ISO8859_1, ts2.Encoding);
            Assert.AreEqual(false, ts2.LocalEcho);
            Assert.AreEqual(NewLine.CR, ts2.TransmitNL);
            Assert.AreEqual(LineFeedRule.Normal, ts2.LineFeedRule);
            Assert.AreEqual(TerminalType.XTerm256Color, ts2.TerminalType);
        }

        [Test]
        public void Test0_oldDefaultTerminalType() {
            SerializationOptions opt = new SerializationOptions();
            TerminalSettings ts1 = new TerminalSettings();
            ts1.BeginUpdate();
            ts1.TerminalType = TerminalType.XTerm; // old default typeminal type
            ts1.EndUpdate();
            StructuredText storage = _terminalSettingsSerializer.Serialize(ts1, opt);
            TerminalSettings ts2 = (TerminalSettings)_terminalSettingsSerializer.Deserialize(storage);

            Assert.AreEqual(EncodingType.ISO8859_1, ts1.Encoding);
            Assert.AreEqual(false, ts1.LocalEcho);
            Assert.AreEqual(NewLine.CR, ts1.TransmitNL);
            Assert.AreEqual(LineFeedRule.Normal, ts1.LineFeedRule);
            Assert.AreEqual(TerminalType.XTerm, ts1.TerminalType);

            Assert.AreEqual(EncodingType.ISO8859_1, ts2.Encoding);
            Assert.AreEqual(false, ts2.LocalEcho);
            Assert.AreEqual(NewLine.CR, ts2.TransmitNL);
            Assert.AreEqual(LineFeedRule.Normal, ts2.LineFeedRule);
            Assert.AreEqual(TerminalType.XTerm, ts2.TerminalType);
        }

        [Test]
        public void Test1() {
            SerializationOptions opt = new SerializationOptions();
            TerminalSettings ts1 = new TerminalSettings();
            ts1.BeginUpdate();
            ts1.Encoding = EncodingType.SHIFT_JIS;
            ts1.LocalEcho = true;
            ts1.TransmitNL = NewLine.CRLF;
            ts1.TerminalType = TerminalType.VT100;
            ts1.EndUpdate();

            StructuredText storage = _terminalSettingsSerializer.Serialize(ts1, opt);
            //確認
            StringWriter wr = new StringWriter();
            new TextStructuredTextWriter(wr).Write(storage);
            wr.Close();
            Debug.WriteLine(wr.ToString());

            TerminalSettings ts2 = (TerminalSettings)_terminalSettingsSerializer.Deserialize(storage);

            Assert.AreEqual(ts1.Encoding, ts2.Encoding);
            Assert.AreEqual(ts1.LocalEcho, ts2.LocalEcho);
            Assert.AreEqual(ts1.TransmitNL, ts2.TransmitNL);
            Assert.AreEqual(ts1.LineFeedRule, ts2.LineFeedRule);
            Assert.AreEqual(ts1.TerminalType, ts2.TerminalType);
        }

        [Test]
        public void Test2() {
            StringReader reader = new StringReader("Poderosa.Terminal.TerminalSettings {\r\n encoding=xxx\r\n localecho=xxx\r\n transmit-nl=xxx}");
            StructuredText storage = new TextStructuredTextReader(reader).Read();

            TerminalSettings ts = (TerminalSettings)_terminalSettingsSerializer.Deserialize(storage);
            Assert.AreEqual(EncodingType.UTF8_Latin, ts.Encoding);
            Assert.AreEqual(false, ts.LocalEcho);
            Assert.AreEqual(NewLine.CR, ts.TransmitNL);
            Assert.AreEqual(LineFeedRule.Normal, ts.LineFeedRule);
            Assert.AreEqual(TerminalType.XTerm, ts.TerminalType);
        }

        #region PoderosaWorldForTest

        private class PoderosaWorldForTest : IPoderosaWorld {

            public IAdapterManager AdapterManager {
                get {
                    throw new NotImplementedException();
                }
            }

            public IPluginManager PluginManager {
                get {
                    throw new NotImplementedException();
                }
            }

            public IPoderosaCulture Culture {
                get;
                private set;
            }

            public IAdaptable GetAdapter(System.Type adapter) {
                throw new NotImplementedException();
            }

            public PoderosaWorldForTest() {
                this.Culture = new PoderosaCultureForTest();
            }
        }

        private class PoderosaCultureForTest : IPoderosaCulture {

            public CultureInfo InitialCulture {
                get {
                    return CultureInfo.InvariantCulture;
                }
            }

            public CultureInfo CurrentCulture {
                get {
                    return CultureInfo.InvariantCulture;
                }
            }

            public void SetCulture(CultureInfo culture) {
            }

            public void AddChangeListener(ICultureChangeListener listener) {
            }

            public void RemoveChangeListener(ICultureChangeListener listener) {
            }

            public bool IsJapaneseOS {
                get {
                    return false;
                }
            }

            public bool IsSimplifiedChineseOS {
                get {
                    return false;
                }
            }

            public bool IsTraditionalChineseOS {
                get {
                    return false;
                }
            }

            public bool IsKoreanOS {
                get {
                    return false;
                }
            }
        }

        #endregion
    }
}
#endif
