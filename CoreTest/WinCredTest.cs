// Copyright 2023 The Poderosa Project.
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
using System.Globalization;
using System.IO;

using NUnit.Framework;

namespace Poderosa.Util {

    [TestFixture]
    public class WinCredTest {

        private readonly Random random = new Random();

        [Test]
        public void SaveAndReadUserPasswordWithoutPort() {
            SaveAndReadUserPassword(false);
        }

        [Test]
        public void SaveAndReadUserPasswordWithPort() {
            SaveAndReadUserPassword(true);
        }

        private void SaveAndReadUserPassword(bool withPort) {
            string time = DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
            string rand = random.Next(0x10000).ToString("x4", NumberFormatInfo.InvariantInfo);
            string protocol = "aaa";
            string host = "192.168.1.2";
            int? port = withPort ? (int?)123 : null;
            string user = "user" + time + rand;
            string password = "password " + time + rand;

            //
            // add new entry
            //

            bool saved = WinCred.SaveUserPassword(protocol, host, port, user, password);
            Assert.IsTrue(saved);

            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host, port, user, out passwordRead);
                Assert.IsTrue(read);
                Assert.AreEqual(password, passwordRead);
            }

            // protocol doesn't match
            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol + "x", host, port, user, out passwordRead);
                Assert.IsFalse(read);
                Assert.IsNull(passwordRead);
            }

            // host doesn't match
            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host + "x", port, user, out passwordRead);
                Assert.IsFalse(read);
                Assert.IsNull(passwordRead);
            }

            // port doesn't match
            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host, 999, user, out passwordRead);
                Assert.IsFalse(read);
                Assert.IsNull(passwordRead);
            }

            // port doesn't match
            if (withPort) {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host, null, user, out passwordRead);
                Assert.IsFalse(read);
                Assert.IsNull(passwordRead);
            }

            // user doesn't match
            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host, port, user + "x", out passwordRead);
                Assert.IsFalse(read);
                Assert.IsNull(passwordRead);
            }

            //
            // overwrite entry
            //

            string newPassword = "new password " + time + rand;

            saved = WinCred.SaveUserPassword(protocol, host, port, user, newPassword);
            Assert.IsTrue(saved);

            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host, port, user, out passwordRead);
                Assert.IsTrue(read);
                Assert.AreEqual(newPassword, passwordRead);
            }

            //
            // delete entry
            //

            WinCred.DeleteUserPassword(protocol, host, port, user);

            {
                string passwordRead;
                bool read = WinCred.ReadUserPassword(protocol, host, port, user, out passwordRead);
                Assert.IsFalse(read);
                Assert.IsNull(passwordRead);
            }
        }

        [Test]
        public void SaveAndReadKeyFilePassword() {
            string keyFilePath = Path.GetTempFileName();
            string keyFilePath2 = Path.GetTempFileName();
            try {
                byte[] keyFileContent = new byte[500];
                random.NextBytes(keyFileContent);
                File.WriteAllBytes(keyFilePath, keyFileContent);

                random.NextBytes(keyFileContent);
                File.WriteAllBytes(keyFilePath2, keyFileContent);

                string time = DateTime.Now.ToString("yyyyMMddHHmmss", DateTimeFormatInfo.InvariantInfo);
                string rand = new Random().Next(0x10000).ToString("x4", NumberFormatInfo.InvariantInfo);
                string protocol = "aaa";
                string password = "password " + time + rand;

                //
                // add new entry
                //

                string keyFileHash;
                bool saved = WinCred.SaveKeyFilePassword(protocol, keyFilePath, password, out keyFileHash);
                Assert.IsTrue(saved);
                Assert.IsNotNull(keyFilePath);

                {
                    string passwordRead;
                    bool read = WinCred.ReadKeyFilePassword(protocol, keyFilePath, out passwordRead);
                    Assert.IsTrue(read);
                    Assert.AreEqual(password, passwordRead);
                }

                // hash doesn't match
                {
                    string passwordRead;
                    bool read = WinCred.ReadKeyFilePassword(protocol, keyFilePath2 /* another existing key file */, out passwordRead);
                    Assert.IsFalse(read);
                    Assert.IsNull(passwordRead);
                }

                // key file doesn't exist
                {
                    string passwordRead;
                    bool read = WinCred.ReadKeyFilePassword(protocol, keyFilePath + ".nonexistent" /* nonexistent key file */, out passwordRead);
                    Assert.IsFalse(read);
                    Assert.IsNull(passwordRead);
                }

                //
                // overwrite entry
                //

                string newPassword = "new password " + time + rand;

                string newKeyFileHash;
                saved = WinCred.SaveKeyFilePassword(protocol, keyFilePath, newPassword, out newKeyFileHash);
                Assert.IsTrue(saved);
                Assert.AreEqual(keyFileHash, newKeyFileHash);

                {
                    string passwordRead;
                    bool read = WinCred.ReadKeyFilePassword(protocol, keyFilePath, out passwordRead);
                    Assert.IsTrue(read);
                    Assert.AreEqual(newPassword, passwordRead);
                }

                //
                // delete entry
                //

                WinCred.DeleteKeyFilePassword(protocol, keyFileHash);

                {
                    string passwordRead;
                    bool read = WinCred.ReadKeyFilePassword(protocol, keyFilePath, out passwordRead);
                    Assert.IsFalse(read);
                    Assert.IsNull(passwordRead);
                }
            }
            finally {
                File.Delete(keyFilePath);
                File.Delete(keyFilePath2);
            }
        }
    }
}

#endif
