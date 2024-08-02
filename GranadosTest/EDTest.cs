// Copyright 2024 The Poderosa Project.
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
using System.Linq;

using NUnit.Framework;

using Granados.Crypto;

namespace Granados.PKI {

    [TestFixture]
    public class CurveEd25519Test {

        private const string SIGN_INPUT_PATH = @"..\testvectors\ec\sign.input";

        // Test of signing and verifying using test vectors
        // http://ed25519.cr.yp.to/python/sign.input
        [Test]
        public void Test() {
            using (var reader = new System.IO.StreamReader(SIGN_INPUT_PATH)) {
                int skip = 0;
                int count = 0;
                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }
                    count++;
                    if (count <= skip) {
                        continue;
                    }
                    Console.WriteLine("Line {0}", count);

                    string[] w = line.Split(':');
                    byte[][] b = w.Select(s => BigIntegerConverter.ParseHex(s)).ToArray();
                    byte[] privateKey = new byte[32];
                    Buffer.BlockCopy(b[0], 0, privateKey, 0, 32);
                    byte[] publicKey = b[1];
                    byte[] message = b[2];
                    byte[] signature = new byte[64];
                    Buffer.BlockCopy(b[3], 0, signature, 0, 64);

                    CurveEd25519 curve = new CurveEd25519();

                    byte[] sig;
                    bool signSucceeded = curve.Sign(privateKey, message, out sig);
                    Assert.True(signSucceeded);
                    Assert.AreEqual(signature, sig);

                    bool verifySucceeded = curve.Verify(publicKey, signature, message);
                    Assert.True(verifySucceeded);
                }
            }
        }
    }

}
#endif
