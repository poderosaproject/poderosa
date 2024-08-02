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
using NUnit.Framework;

using Granados.Crypto;

namespace Granados.Algorithms {

    [TestFixture]
    public class BlowfishTest {

        private const string VECTORS_TXT_PATH = @"..\testvectors\blowfish\vectors.txt";

        // Test using test vector
        // https://www.schneier.com/code/vectors.txt
        [Test]
        public void Test() {

            // Test ECB
            using (var reader = new System.IO.StreamReader(VECTORS_TXT_PATH)) {
                string line;
                do {
                    line = reader.ReadLine();
                } while (line != null && !line.StartsWith("key bytes"));

                var blowfish = new Blowfish();
                int count = 0;
                while (true) {
                    line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }
                    string[] w = System.Text.RegularExpressions.Regex.Split(line, @"\s\s+");
                    if (w.Length < 3 || w[0].Length != 16 || w[1].Length != 16 || w[2].Length != 16) {
                        break;
                    }
                    byte[] key = BigIntegerConverter.ParseHex(w[0]);
                    byte[] clear = BigIntegerConverter.ParseHex(w[1]);
                    byte[] cipher = BigIntegerConverter.ParseHex(w[2]);

                    ++count;
                    System.Diagnostics.Debug.WriteLine("Test ECB #{0}", count);

                    blowfish.InitializeKey(key);
                    for (int tries = 1; tries <= 3; ++tries) {
                        byte[] encrypted = new byte[cipher.Length];
                        blowfish.BlockEncrypt(clear, 0, encrypted, 0);

                        Assert.AreEqual(cipher, encrypted, "BlockEncrypt failed (tries = {0})", tries);
                    }
                    for (int tries = 1; tries <= 3; ++tries) {
                        byte[] decrypted = new byte[clear.Length];
                        blowfish.BlockDecrypt(cipher, 0, decrypted, 0);

                        Assert.AreEqual(clear, decrypted, "BlockDecrypt failed (tries = {0})", tries);
                    }
                }
            }

            // Test CBC
            {
                byte[] key = BigIntegerConverter.ParseHex("0123456789ABCDEFF0E1D2C3B4A59687");
                byte[] iv = BigIntegerConverter.ParseHex("FEDCBA9876543210");
                // data: 37363534333231204E6F77206973207468652074696D6520666F722000 (29 bytes) + padding bytes (3 bytes)
                byte[] data = BigIntegerConverter.ParseHex("37363534333231204E6F77206973207468652074696D6520666F722000000000");
                byte[] cipher = BigIntegerConverter.ParseHex("6B77B4D63006DEE605B156E27403979358DEB9E7154616D959F1652BD5FF92CC");
                System.Diagnostics.Debug.WriteLine("Test CBC");

                {
                    Blowfish blowfish = new Blowfish();
                    blowfish.InitializeKey(key);
                    blowfish.SetIV(iv);

                    byte[] encrypted = new byte[cipher.Length];
                    blowfish.EncryptCBC(data, 0, data.Length, encrypted, 0);

                    Assert.AreEqual(cipher, encrypted);
                }

                {
                    Blowfish blowfish = new Blowfish();
                    blowfish.InitializeKey(key);
                    blowfish.SetIV(iv);

                    byte[] decrypted = new byte[data.Length];
                    for (int i = 0; i < decrypted.Length; ++i) {
                        decrypted[i] = 0xff;
                    }
                    blowfish.DecryptCBC(cipher, 0, cipher.Length, decrypted, 0);

                    Assert.AreEqual(data, decrypted);
                }
            }
        }
    }
}
#endif