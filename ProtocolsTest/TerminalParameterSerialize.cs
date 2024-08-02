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
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using System.IO;
using NUnit.Framework;

using System.Globalization;
using Granados;
using Poderosa.Serializing;

namespace Poderosa.Protocols {

    [TestFixture]
    public class TerminalParameterTests {

        private TelnetParameterSerializer _telnetSerializer;
        private SSHParameterSerializer _sshSerializer;
        private LocalShellParameterSerializer _localShellSerializer;

        [OneTimeSetUp]
        public void Init() {
            _telnetSerializer = new TelnetParameterSerializer();
            _sshSerializer = new SSHParameterSerializer();
            _localShellSerializer = new LocalShellParameterSerializer();
        }

        [Test]
        public void Telnet0() {
            SerializationOptions opt = new SerializationOptions();
            TelnetParameter p1 = new TelnetParameter();
            StructuredText t = _telnetSerializer.Serialize(p1, opt);
            Assert.IsNull(t.Parent);
            Assert.IsNull(t.Get("port"));
            TelnetParameter p2 = (TelnetParameter)_telnetSerializer.Deserialize(t);
            Assert.AreEqual(23, p2.Port);
            Assert.AreEqual(TerminalParameter.DEFAULT_TERMINAL_TYPE, p2.TerminalType);
        }
        [Test]
        public void Telnet1() {
            SerializationOptions opt = new SerializationOptions();
            TelnetParameter p1 = new TelnetParameter();
            p1.SetTerminalName("TERMINAL");
            p1.Port = 80;
            p1.Destination = "DESTINATION";
            StructuredText t = _telnetSerializer.Serialize(p1, opt);
            TelnetParameter p2 = (TelnetParameter)_telnetSerializer.Deserialize(t);
            Assert.AreEqual(80, p2.Port);
            Assert.AreEqual("TERMINAL", p2.TerminalType);
            Assert.AreEqual("DESTINATION", p2.Destination);
        }

        /* These tests doesn't pass due to the deep dependencies to many core components...

        [Test]
        public void SSH0() {
            SSHLoginParameter p1 = new SSHLoginParameter();
            StructuredText t = _sshSerializer.Serialize(p1);
            //確認
            StringWriter wr = new StringWriter();
            new TextStructuredTextWriter(wr).Write(t);
            wr.Close();
            Debug.WriteLine(wr.ToString());

            Assert.IsNull(t.Get("port"));
            Assert.IsNull(t.Get("method"));
            Assert.IsNull(t.Get("authentication"));
            Assert.IsNull(t.Get("identityFileName"));
            SSHLoginParameter p2 = (SSHLoginParameter)_sshSerializer.Deserialize(t);
            Assert.AreEqual(22, p2.Port);
            Assert.AreEqual(SSHProtocol.SSH2, p2.Method);
            Assert.AreEqual(AuthenticationType.Password, p2.AuthenticationType);
            Assert.AreEqual("", p2.IdentityFileName);
        }
        [Test]
        public void SSH1() {
            SSHLoginParameter p1 = new SSHLoginParameter();
            p1.Method = SSHProtocol.SSH1;
            p1.Account = "account";
            p1.IdentityFileName = "identity-file";
            p1.AuthenticationType = AuthenticationType.PublicKey;

            StructuredText t = _sshSerializer.Serialize(p1);
            UnitTestUtil.DumpStructuredText(t);
            //確認
            Debug.WriteLine(UnitTestUtil.DumpStructuredText(t));

            SSHLoginParameter p2 = (SSHLoginParameter)_sshSerializer.Deserialize(t);
            Assert.AreEqual(SSHProtocol.SSH1, p2.Method);
            Assert.AreEqual(AuthenticationType.PublicKey, p2.AuthenticationType);
            Assert.AreEqual("identity-file", p2.IdentityFileName);
            Assert.AreEqual("account", p2.Account);
        }
         */
        //TODO CYGWIN
        //TODO StructuredTextを手で作成し、本来ありえないデータが入っていてもちゃんと読めることをテスト
    }
}
#endif
