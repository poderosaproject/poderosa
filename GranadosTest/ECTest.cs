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
using System.IO;

using NUnit.Framework;

using Granados.Mono.Math;
using Granados.Crypto;
using Granados.IO.SSH2;

namespace Granados.PKI {

    public class EllipticCurveTest {

        private const string NISTTV_FILE_PATH = @"../testvectors/ec/nisttv";

        private const string ECDSATESTVECTORS_BASE_PATH = @"../testvectors/ec/186-4ecdsatestvectors";

        // Tests point-multiplication using test vectors
        // http://point-at-infinity.org/ecc/nisttv
        [Test]
        public void TestPointMultiplication() {
            using (var reader = new System.IO.StreamReader(NISTTV_FILE_PATH)) {
                EllipticCurve curve = null;
                string ks = null;
                BigInteger k = null;
                BigInteger x = null;
                BigInteger y = null;
                int testCount = 0;

                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"Curve:\s+(\w+)");
                    if (match.Success) {
                        string curveName = "nist" + match.Groups[1].Value.ToLowerInvariant();
                        curve = EllipticCurve.FindByName(curveName);
                        if (curve != null) {
                            Console.WriteLine("Test {0}", curve.CurveName);
                        }
                        ks = null;
                        k = x = y = null;
                        testCount = 0;
                        continue;
                    }

                    if (line.StartsWith("k = ") && curve != null) {
                        ks = line.Substring(4).Trim();
                        k = BigInteger.Parse(ks);
                        continue;
                    }

                    if (line.StartsWith("x = ") && curve != null) {
                        x = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(4).Trim()));
                        continue;
                    }

                    if (line.StartsWith("y = ") && curve != null) {
                        y = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(4).Trim()));

                        if (k != null && x != null) {
                            ECPoint p = curve.PointMul(k, curve.BasePoint, true);

                            Assert.NotNull(p);
                            Assert.IsNotInstanceOf(typeof(ECPointAtInfinity), p);
                            Assert.AreEqual(x, p.X);
                            Assert.AreEqual(y, p.Y);

                            ++testCount;

                            Console.WriteLine("Pass #{0} : {1}", testCount, ks);
                        }

                        k = x = y = null;
                    }
                }
            }
        }

        // Tests public key validation using test vectors from NIST CAVP.
        // http://csrc.nist.gov/groups/STM/cavp/digital-signatures.html
        [Test]
        public void TestPKV() {
            using (var reader = new System.IO.StreamReader(Path.Combine(ECDSATESTVECTORS_BASE_PATH, "PKV.rsp"))) {
                EllipticCurve curve = null;
                string qxs = null;
                BigInteger qx = null;
                BigInteger qy = null;
                string result = null;
                int testCount = 0;

                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\[([-\w]+)\]");
                    if (match.Success) {
                        string curveName = "nist" + match.Groups[1].Value.ToLowerInvariant().Replace("-", "");
                        curve = EllipticCurve.FindByName(curveName);
                        if (curve != null) {
                            Console.WriteLine("Test {0}", curve.CurveName);
                        }
                        qx = qy = null;
                        qxs = null;
                        result = null;
                        testCount = 0;
                        continue;
                    }

                    if (line.StartsWith("Qx = ") && curve != null) {
                        qxs = line.Substring(5).Trim();
                        qx = new BigInteger(BigIntegerConverter.ParseHex(qxs));
                        continue;
                    }

                    if (line.StartsWith("Qy = ") && curve != null) {
                        qy = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(5).Trim()));
                        continue;
                    }

                    if (line.StartsWith("Result = ") && curve != null) {
                        result = line.Substring(9, 1);

                        if (qx != null && qy != null) {
                            var pk = new ECDSAPublicKey(curve, new ECPoint(qx, qy));
                            string r = pk.IsValid() ? "P" : "F";

                            Assert.AreEqual(result, r);

                            ++testCount;

                            Console.WriteLine("Pass #{0} : {1}", testCount, qxs);
                        }

                        qx = qy = null;
                        qxs = null;
                        result = null;
                    }
                }
            }
        }

        // Tests key pair validation using test vectors from NIST CAVP.
        // http://csrc.nist.gov/groups/STM/cavp/digital-signatures.html
        [Test]
        public void TestKeyPair() {
            using (var reader = new System.IO.StreamReader(Path.Combine(ECDSATESTVECTORS_BASE_PATH, "KeyPair.rsp"))) {
                EllipticCurve curve = null;
                string ds = null;
                BigInteger d = null;
                BigInteger qx = null;
                BigInteger qy = null;
                int testCount = 0;

                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\[([-\w]+)\]");
                    if (match.Success) {
                        string curveName = "nist" + match.Groups[1].Value.ToLowerInvariant().Replace("-", "");
                        curve = EllipticCurve.FindByName(curveName);
                        if (curve != null) {
                            Console.WriteLine("Test {0}", curve.CurveName);
                        }
                        d = qx = qy = null;
                        ds = null;
                        testCount = 0;
                        continue;
                    }

                    if (line.StartsWith("d = ") && curve != null) {
                        ds = line.Substring(4).Trim();
                        d = new BigInteger(BigIntegerConverter.ParseHex(ds));
                        continue;
                    }

                    if (line.StartsWith("Qx = ") && curve != null) {
                        qx = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(5).Trim()));
                        continue;
                    }

                    if (line.StartsWith("Qy = ") && curve != null) {
                        qy = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(5).Trim()));

                        if (d != null && qx != null) {
                            var pk = new ECDSAPublicKey(curve, new ECPoint(qx, qy));
                            var kp = new ECDSAKeyPair(curve, pk, d);

                            Assert.True(kp.CheckKeyConsistency());

                            ++testCount;

                            Console.WriteLine("Pass #{0} : {1}", testCount, ds);
                        }

                        d = qx = qy = null;
                        ds = null;
                    }
                }
            }
        }

        // Tests signature verification using test vectors from NIST CAVP.
        // http://csrc.nist.gov/groups/STM/cavp/digital-signatures.html
        [Test]
        public void TestSignatureVerification() {
            using (var reader = new System.IO.StreamReader(Path.Combine(ECDSATESTVECTORS_BASE_PATH, "SigVer.rsp"))) {
                EllipticCurve curve = null;
                byte[] msg = null;
                BigInteger qx = null;
                BigInteger qy = null;
                BigInteger r = null;
                BigInteger s = null;
                string result = null;
                int testCount = 0;

                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"\[([-\w]+),(SHA-\d+)\]");
                    if (match.Success) {
                        string curveName = "nist" + match.Groups[1].Value.ToLowerInvariant().Replace("-", "");
                        curve = EllipticCurve.FindByName(curveName);
                        if (curve != null) {
                            using (var hashFunc = ECDSAHashAlgorithmChooser.Choose(curve)) {
                                var hashName = "SHA-" + hashFunc.HashSize.ToString();
                                if (hashName == match.Groups[2].Value) {
                                    Console.WriteLine("Test {0}", curve.CurveName);
                                }
                                else {
                                    // hash function doesn't match
                                    curve = null;
                                }
                            }
                        }
                        msg = null;
                        qx = qy = r = s = null;
                        result = null;
                        testCount = 0;
                        continue;
                    }

                    if (line.StartsWith("Msg = ") && curve != null) {
                        msg = BigIntegerConverter.ParseHex(line.Substring(6).Trim());
                        continue;
                    }

                    if (line.StartsWith("Qx = ") && curve != null) {
                        qx = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(5).Trim()));
                        continue;
                    }

                    if (line.StartsWith("Qy = ") && curve != null) {
                        qy = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(5).Trim()));
                        continue;
                    }

                    if (line.StartsWith("R = ") && curve != null) {
                        r = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(4).Trim()));
                        continue;
                    }

                    if (line.StartsWith("S = ") && curve != null) {
                        s = new BigInteger(BigIntegerConverter.ParseHex(line.Substring(4).Trim()));
                        continue;
                    }

                    if (line.StartsWith("Result = ") && curve != null) {
                        result = line.Substring(9, 1);

                        if (msg != null && qx != null && qy != null && r != null && s != null) {
                            var pk = new ECDSAPublicKey(curve, new ECPoint(qx, qy));
                            var buf = new SSH2DataWriter();
                            buf.WriteBigInteger(r);
                            buf.WriteBigInteger(s);
                            var sig = buf.ToByteArray();
                            string verRes;
                            try {
                                pk.Verify(sig, msg);
                                verRes = "P";
                            }
                            catch (VerifyException) {
                                verRes = "F";
                            }

                            Assert.AreEqual(result, verRes);

                            ++testCount;

                            Console.WriteLine("Pass #{0}", testCount);
                        }

                        msg = null;
                        qx = qy = r = s = null;
                        result = null;
                    }
                }
            }
        }
    }

}
#endif
