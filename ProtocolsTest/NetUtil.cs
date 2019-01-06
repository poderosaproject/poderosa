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
using Poderosa.Plugins;
using Poderosa.Preferences;
using System;
using System.Net;
using System.Net.Sockets;

namespace Poderosa.Protocols {

    [TestFixture]
    public class NetUtilTests {

        /* These tests doesn't pass due to the deep dependencies to many core components...

        [Test]
        public void TestTimeout() {
            Exception e = null;
            try {
                Socket s = NetUtil.ConnectTCPSocket(new IPAddressList(IPAddress.Parse("1.1.1.1")), 10); //接続できないはずのものをテスト
            }
            catch (Exception ex) {
                e = ex;
                Console.Out.WriteLine(ex.StackTrace);
            }

            Assert.IsTrue(e != null);
            Assert.AreEqual("TIMEOUT", e.Message);
        }

        [Test]
        public void TestSuccessful() {
            Socket s = NetUtil.ConnectTCPSocket(new IPAddressList(IPAddress.Loopback), 8888); //listenしてる適当なポートにつなぐ
            Assert.IsTrue(s.Connected);
        }
         */
    }
}
#endif
