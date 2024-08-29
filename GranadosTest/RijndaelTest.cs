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
using System.Diagnostics;

using NUnit.Framework;

using Granados.Algorithms;
using Granados.Crypto;

namespace Granados.Algorithms {

    [TestFixture]
    public class RijndaelTest {

        // Test vectors from NIST 800-38A
        // Recommendation for Block Cipher Modes of Operation: Methods and Techniques
        private static readonly byte[] original =
            BigIntegerConverter.ParseHex(
              "6bc1bee22e409f96e93d7e117393172a"
            + "ae2d8a571e03ac9c9eb76fac45af8e51"
            + "30c81c46a35ce411e5fbc1191a0a52ef"
            + "f69f2445df4f9b17ad2b417be66c3710");

        #region ECB

        [TestCaseSource("AES_ECB_Patterns")]
        public void Test_AES_ECB_Encrypt_OutOfPlace(string name, byte[] key, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = new byte[5 + plaintext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(ciphertext, 5, 7);

            var aes = new Rijndael();
            aes.InitializeKey(key);
            for (int i = 0; i < inputDataLength; i += aes.GetBlockSize()) {
                aes.blockEncrypt(inputData, inputDataOffset + i, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_ECB_Patterns")]
        public void Test_AES_ECB_Encrypt_InPlace(string name, byte[] key, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = inputData;
            int outputDataOffset = inputDataOffset;
            byte[] expectedData = ExtendCopy(ciphertext, 3, 5);

            var aes = new Rijndael();
            aes.InitializeKey(key);
            for (int i = 0; i < inputDataLength; i += aes.GetBlockSize()) {
                aes.blockEncrypt(inputData, inputDataOffset + i, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
        }

        [TestCaseSource("AES_ECB_Patterns")]
        public void Test_AES_ECB_Decrypt_OutOfPlace(string name, byte[] key, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = new byte[5 + ciphertext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(plaintext, 5, 7);

            var aes = new Rijndael();
            aes.InitializeKey(key);
            for (int i = 0; i < inputDataLength; i += aes.GetBlockSize()) {
                aes.blockDecrypt(inputData, inputDataOffset + i, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_ECB_Patterns")]
        public void Test_AES_ECB_Decrypt_InPlace(string name, byte[] key, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = inputData;
            int outputDataOffset = inputDataOffset;
            byte[] expectedData = ExtendCopy(plaintext, 3, 5);

            var aes = new Rijndael();
            aes.InitializeKey(key);
            for (int i = 0; i < inputDataLength; i += aes.GetBlockSize()) {
                aes.blockDecrypt(inputData, inputDataOffset + i, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
        }

        // Test vectors from NIST 800-38A
        // Recommendation for Block Cipher Modes of Operation: Methods and Techniques
        public static object[] AES_ECB_Patterns =
        {
            new object[] {
                // name
                "128 bit key",
                // key
                BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "3ad77bb40d7a3660a89ecaf32466ef97"
                    + "f5d3d58503b9699de785895a96fdbaaf"
                    + "43b1cd7f598ece23881b00e3ed030688"
                    + "7b0c785e27e8ad3f8223207104725dd4"
                ),
            },
            new object[] {
                // name
                "192 bit key",
                // key
                BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "bd334f1d6e45f25ff712a214571fa5cc"
                    + "974104846d0ad3ad7734ecb3ecee4eef"
                    + "ef7afd2270e2e60adce0ba2face6444e"
                    + "9a4b41ba738d6c72fb16691603c18e0e"
                ),
            },
            new object[] {
                // name
                "256 bit key",
                // key
                BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "f3eed1bdb5d2a03c064b5a7e3db181f8"
                    + "591ccb10d410ed26dc5ba74a31362870"
                    + "b6ed21b99ca6f4f9f153e7b1beafed1d"
                    + "23304b7a39f9f3ff067d8d8f9e24ecc7"
                ),
            },
        };

        #endregion

        #region CBC

        [TestCaseSource("AES_CBC_Patterns")]
        public void Test_AES_CBC_Encrypt_OutOfPlace(string name, byte[] key, byte[] iv, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = new byte[5 + plaintext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(ciphertext, 5, 7);

            var aes = new AESBlockCipherCBC(key, iv);
            aes.Encrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_CBC_Patterns")]
        public void Test_AES_CBC_Encrypt_InPlace(string name, byte[] key, byte[] iv, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = inputData;
            int outputDataOffset = inputDataOffset;
            byte[] expectedData = ExtendCopy(ciphertext, 3, 5);

            var aes = new AESBlockCipherCBC(key, iv);
            aes.Encrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
        }

        [TestCaseSource("AES_CBC_Patterns")]
        public void Test_AES_CBC_Encrypt_BlockByBlock(string name, byte[] key, byte[] iv, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = new byte[5 + plaintext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(ciphertext, 5, 7);

            var aes = new AESBlockCipherCBC(key, iv);
            int blockSize = aes.GetBlockSize();
            for (int i = 0; i < inputDataLength; i += blockSize) {
                aes.Encrypt(inputData, inputDataOffset + i, blockSize, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_CBC_Patterns")]
        public void Test_AES_CBC_Decrypt_OutOfPlace(string name, byte[] key, byte[] iv, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = new byte[5 + ciphertext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(plaintext, 5, 7);

            var aes = new AESBlockCipherCBC(key, iv);
            aes.Decrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_CBC_Patterns")]
        public void Test_AES_CBC_Decrypt_InPlace(string name, byte[] key, byte[] iv, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = inputData;
            int outputDataOffset = inputDataOffset;
            byte[] expectedData = ExtendCopy(plaintext, 3, 5);

            var aes = new AESBlockCipherCBC(key, iv);
            aes.Decrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
        }

        [TestCaseSource("AES_CBC_Patterns")]
        public void Test_AES_CBC_Decrypt_BlockByBlock(string name, byte[] key, byte[] iv, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = new byte[5 + ciphertext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(plaintext, 5, 7);

            var aes = new AESBlockCipherCBC(key, iv);
            int blockSize = aes.GetBlockSize();
            for (int i = 0; i < inputDataLength; i += blockSize) {
                aes.Decrypt(inputData, inputDataOffset + i, blockSize, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        // Test vectors from NIST 800-38A
        // Recommendation for Block Cipher Modes of Operation: Methods and Techniques
        public static object[] AES_CBC_Patterns =
        {
            new object[] {
                // name
                "128 bit key",
                // key
                BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                // iv
                BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "7649abac8119b246cee98e9b12e9197d"
                    + "5086cb9b507219ee95db113a917678b2"
                    + "73bed6b8e3c1743b7116e69e22229516"
                    + "3ff1caa1681fac09120eca307586e1a7"
                ),
            },
            new object[] {
                // name
                "192 bit key",
                // key
                BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                // iv
                BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "4f021db243bc633d7178183a9fa071e8"
                    + "b4d9ada9ad7dedf4e5e738763f69145a"
                    + "571b242012fb7ae07fa9baac3df102e0"
                    + "08b0e27988598881d920a9e64f5615cd"
                ),
            },
            new object[] {
                // name
                "256 bit key",
                // key
                BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                // iv
                BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "f58c4c04d6e5f1ba779eabfb5f7bfbd6"
                    + "9cfc4e967edb808d679f777bc6702c7d"
                    + "39f23369a9d9bacfa530e26304231461"
                    + "b2eb05e2c39be9fcda6c19078c6a9d1b"
                ),
            },
        };
        #endregion

        #region CTR

        [TestCase("00000000000000000000000000000000", "00000000000000000000000000000001")]
        [TestCase("000000000000000000000000000000ff", "00000000000000000000000000000100")]
        [TestCase("00000000000000000000000000000100", "00000000000000000000000000000101")]
        [TestCase("00ffffffffffffffffffffffffffffff", "01000000000000000000000000000000")]
        [TestCase("01000000000000000000000000000000", "01000000000000000000000000000001")]
        [TestCase("ffffffffffffffffffffffffffffffff", "00000000000000000000000000000000")]
        public void Test_AES_CTR_IncrementCounterBlock(String cb, String expected) {
            byte[] cbBytes = BigIntegerConverter.ParseHex(cb);
            byte[] expectedBytes = BigIntegerConverter.ParseHex(expected);
            var aes = new AESBlockCipherCTR(BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"), cbBytes);

            Assert.AreEqual(cbBytes, aes.CopyCounterBlock());
            aes.IncrementCounterBlock();
            Assert.AreEqual(expectedBytes, aes.CopyCounterBlock());
        }

        [TestCaseSource("AES_CTR_Patterns")]
        public void Test_AES_CTR_Encrypt_OutOfPlace(string name, byte[] key, byte[] icb, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = new byte[5 + plaintext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(ciphertext, 5, 7);

            var aes = new AESBlockCipherCTR(key, icb);
            aes.Encrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_CTR_Patterns")]
        public void Test_AES_CTR_Encrypt_InPlace(string name, byte[] key, byte[] icb, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = inputData;
            int outputDataOffset = inputDataOffset;
            byte[] expectedData = ExtendCopy(ciphertext, 3, 5);

            var aes = new AESBlockCipherCTR(key, icb);
            aes.Encrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
        }

        [TestCaseSource("AES_CTR_Patterns")]
        public void Test_AES_CTR_Encrypt_BlockByBlock(string name, byte[] key, byte[] icb, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(plaintext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = plaintext.Length;
            byte[] outputData = new byte[5 + plaintext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(ciphertext, 5, 7);

            var aes = new AESBlockCipherCTR(key, icb);
            int blockSize = aes.GetBlockSize();
            for (int i = 0; i < inputDataLength; i += blockSize) {
                aes.Encrypt(inputData, inputDataOffset + i, blockSize, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_CTR_Patterns")]
        public void Test_AES_CTR_Decrypt_OutOfPlace(string name, byte[] key, byte[] icb, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = new byte[5 + ciphertext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(plaintext, 5, 7);

            var aes = new AESBlockCipherCTR(key, icb);
            aes.Decrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        [TestCaseSource("AES_CTR_Patterns")]
        public void Test_AES_CTR_Decrypt_InPlace(string name, byte[] key, byte[] icb, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = inputData;
            int outputDataOffset = inputDataOffset;
            byte[] expectedData = ExtendCopy(plaintext, 3, 5);

            var aes = new AESBlockCipherCTR(key, icb);
            aes.Decrypt(inputData, inputDataOffset, inputDataLength, outputData, outputDataOffset);

            Assert.AreEqual(expectedData, outputData, "wrong output");
        }

        [TestCaseSource("AES_CTR_Patterns")]
        public void Test_AES_CTR_Decrypt_BlockByBlock(string name, byte[] key, byte[] icb, byte[] plaintext, byte[] ciphertext) {
            byte[] inputData = ExtendCopy(ciphertext, 3, 5);
            byte[] inputDataOrig = (byte[])inputData.Clone();
            int inputDataOffset = 3;
            int inputDataLength = ciphertext.Length;
            byte[] outputData = new byte[5 + ciphertext.Length + 7];
            int outputDataOffset = 5;
            byte[] expectedData = ExtendCopy(plaintext, 5, 7);

            var aes = new AESBlockCipherCTR(key, icb);
            int blockSize = aes.GetBlockSize();
            for (int i = 0; i < inputDataLength; i += blockSize) {
                aes.Decrypt(inputData, inputDataOffset + i, blockSize, outputData, outputDataOffset + i);
            }

            Assert.AreEqual(expectedData, outputData, "wrong output");
            Assert.AreEqual(inputDataOrig, inputData, "input data were corrupted");
        }

        // Test vectors from NIST 800-38A
        // Recommendation for Block Cipher Modes of Operation: Methods and Techniques
        public static object[] AES_CTR_Patterns =
        {
            new object[] {
                // name
                "128 bit key",
                // key
                BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                // icb
                BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "874d6191b620e3261bef6864990db6ce"
                    + "9806f66b7970fdff8617187bb9fffdff"
                    + "5ae4df3edbd5d35e5b4f09020db03eab"
                    + "1e031dda2fbe03d1792170a0f3009cee"
                ),
            },
            new object[] {
                // name
                "192 bit key",
                // key
                BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                // icb
                BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "1abc932417521ca24f2b0459fe7e6e0b"
                    + "090339ec0aa6faefd5ccc2c6f4ce8e94"
                    + "1e36b26bd1ebc670d1bd1d665620abf7"
                    + "4f78a7f6d29809585a97daec58c6b050"
                ),
            },
            new object[] {
                // name
                "256 bit key",
                // key
                BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                // icb
                BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                // plaintext
                (byte[])original.Clone(),
                // ciphertext
                BigIntegerConverter.ParseHex(
                      "601ec313775789a5b7a7f504bbf3d228"
                    + "f443e3ca4d62b59aca84e990cacaf5c5"
                    + "2b0930daa23de94ce87017ba2d84988d"
                    + "dfc9c58db67aada613c2dd08457941a6"
                ),
            },
        };
        #endregion

        #region GCM

        // Use test vectors from NIST
        // https://csrc.nist.gov/Projects/cryptographic-algorithm-validation-program/cavp-testing-block-cipher-modes
        // gcmtestvectors.zip

        private const string GCMTESTVECTORS_BASE_PATH = @"..\testvectors\gcmtestvectors";

        [TestCase("gcmEncryptExtIV128.rsp")]
        [TestCase("gcmEncryptExtIV192.rsp")]
        [TestCase("gcmEncryptExtIV256.rsp")]
        public void Test_AES_GCM_Encrypt(string testVectorFile) {
            using (var reader = new System.IO.StreamReader(System.IO.Path.Combine(GCMTESTVECTORS_BASE_PATH, testVectorFile))) {
                int keyLen = -1;
                int ivLen = -1;
                int ptLen = -1;
                int aadLen = -1;
                int tagLen = -1;

                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    string paramName;
                    int paramValue;
                    if (ReadGcmTestVectorParam(line, out paramName, out paramValue)) {
                        switch (paramName) {
                            case "Keylen":
                                keyLen = paramValue;
                                break;
                            case "IVlen":
                                ivLen = paramValue;
                                break;
                            case "PTlen":
                                ptLen = paramValue;
                                break;
                            case "AADlen":
                                aadLen = paramValue;
                                break;
                            case "Taglen":
                                tagLen = paramValue;
                                break;
                        }
                        continue;
                    }

                    if (line.StartsWith("Count =")) {
                        byte[] key;
                        byte[] iv;
                        byte[] pt;
                        byte[] aad;
                        byte[] ct;
                        byte[] tag;
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "Key", keyLen, out key)) {
                            throw new Exception("missing Key");
                        }
                        string keyLine = line;
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "IV", ivLen, out iv)) {
                            throw new Exception("missing IV");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "PT", ptLen, out pt)) {
                            throw new Exception("missing PT");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "AAD", aadLen, out aad)) {
                            throw new Exception("missing AAD");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "CT", ptLen, out ct)) {
                            throw new Exception("missing CT");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "Tag", tagLen, out tag)) {
                            throw new Exception("missing Tag");
                        }

                        Test_AES_GCM_Encrypt_OutOfPlace(key, iv, pt, aad, ct, tag, testVectorFile, keyLine);
                        Test_AES_GCM_Encrypt_InPlace(key, iv, pt, aad, ct, tag, testVectorFile, keyLine);
                    }
                }
            }
        }

        private void Test_AES_GCM_Encrypt_OutOfPlace(byte[] key, byte[] iv, byte[] pt, byte[] aad, byte[] ct, byte[] tag, string testVectorFile, string keyLine) {
            byte[] input = ExtendCopy(pt, 11, 13);
            byte[] inputOrig = (byte[])input.Clone();
            int inputOffset = 11;
            int inputLength = pt.Length;
            byte[] inputAAD = ExtendCopy(aad, 13, 15);
            byte[] inputAADOrig = (byte[])inputAAD.Clone();
            int inputAADOffset = 13;
            int inputAADLength = aad.Length;
            int inputTagLength = tag.Length;
            byte[] output = new byte[9 + pt.Length + 3];
            int outputOffset = 9;
            byte[] expected = ExtendCopy(ct, 9, 3);
            byte[] outputTag = new byte[7 + tag.Length + 5];
            int outputTagOffset = 7;
            int outputTagLength = tag.Length;
            byte[] expectedTag = ExtendCopy(tag, 7, 5);

            var aes = new AESBlockCipherGCM(key, iv);

            aes.Encrypt(
                input: input,
                inputOffset: inputOffset,
                inputLength: inputLength,
                aad: inputAAD,
                aadOffset: inputAADOffset,
                aadLength: inputAADLength,
                output: output,
                outputOffset: outputOffset,
                outputTag: outputTag,
                outputTagOffset: outputTagOffset,
                outputTagLength: outputTagLength
            );

            Assert.AreEqual(expected, output, "{0} {1}: wrong output", testVectorFile, keyLine);
            Assert.AreEqual(expectedTag, outputTag, "{0} {1}: wrong tag", testVectorFile, keyLine);
            Assert.AreEqual(inputOrig, input, "{0} {1}: input data were corrupted", testVectorFile, keyLine);
            Assert.AreEqual(inputAADOrig, inputAAD, "{0} {1}: AAD were corrupted", testVectorFile, keyLine);

            Console.WriteLine("{0} {1}: PASS", testVectorFile, keyLine);
        }

        private void Test_AES_GCM_Encrypt_InPlace(byte[] key, byte[] iv, byte[] pt, byte[] aad, byte[] ct, byte[] tag, string testVectorFile, string keyLine) {
            byte[] input = ExtendCopy(pt, 11, 13);
            int inputOffset = 11;
            int inputLength = pt.Length;
            byte[] inputAAD = ExtendCopy(aad, 13, 15);
            byte[] inputAADOrig = (byte[])inputAAD.Clone();
            int inputAADOffset = 13;
            int inputAADLength = aad.Length;
            int inputTagLength = tag.Length;
            byte[] output = input;
            int outputOffset = inputOffset;
            byte[] expected = ExtendCopy(ct, 11, 13);
            byte[] outputTag = new byte[7 + tag.Length + 5];
            int outputTagOffset = 7;
            int outputTagLength = tag.Length;
            byte[] expectedTag = ExtendCopy(tag, 7, 5);

            var aes = new AESBlockCipherGCM(key, iv);

            aes.Encrypt(
                input: input,
                inputOffset: inputOffset,
                inputLength: inputLength,
                aad: inputAAD,
                aadOffset: inputAADOffset,
                aadLength: inputAADLength,
                output: output,
                outputOffset: outputOffset,
                outputTag: outputTag,
                outputTagOffset: outputTagOffset,
                outputTagLength: outputTagLength
            );

            Assert.AreEqual(expected, output, "{0} {1}: wrong output", testVectorFile, keyLine);
            Assert.AreEqual(expectedTag, outputTag, "{0} {1}: wrong tag", testVectorFile, keyLine);
            Assert.AreEqual(inputAADOrig, inputAAD, "{0} {1}: AAD were corrupted", testVectorFile, keyLine);

            Console.WriteLine("{0} {1}: PASS", testVectorFile, keyLine);
        }

        [TestCase("gcmDecrypt128.rsp")]
        [TestCase("gcmDecrypt192.rsp")]
        [TestCase("gcmDecrypt256.rsp")]
        public void Test_AES_GCM_Decrypt(string testVectorFile) {
            using (var reader = new System.IO.StreamReader(System.IO.Path.Combine(GCMTESTVECTORS_BASE_PATH, testVectorFile))) {
                int keyLen = -1;
                int ivLen = -1;
                int ptLen = -1;
                int aadLen = -1;
                int tagLen = -1;

                while (true) {
                    string line = reader.ReadLine();
                    if (line == null) {
                        break;
                    }

                    string paramName;
                    int paramValue;
                    if (ReadGcmTestVectorParam(line, out paramName, out paramValue)) {
                        switch (paramName) {
                            case "Keylen":
                                keyLen = paramValue;
                                break;
                            case "IVlen":
                                ivLen = paramValue;
                                break;
                            case "PTlen":
                                ptLen = paramValue;
                                break;
                            case "AADlen":
                                aadLen = paramValue;
                                break;
                            case "Taglen":
                                tagLen = paramValue;
                                break;
                        }
                        continue;
                    }

                    if (line.StartsWith("Count =")) {
                        byte[] key;
                        byte[] iv;
                        byte[] ct;
                        byte[] aad;
                        byte[] tag;
                        byte[] pt;
                        bool failureCase;
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "Key", keyLen, out key)) {
                            throw new Exception("missing Key");
                        }
                        string keyLine = line;
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "IV", ivLen, out iv)) {
                            throw new Exception("missing IV");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "CT", ptLen, out ct)) {
                            throw new Exception("missing CT");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "AAD", aadLen, out aad)) {
                            throw new Exception("missing AAD");
                        }
                        line = reader.ReadLine();
                        if (!ReadGcmTestVectorValue(line, "Tag", tagLen, out tag)) {
                            throw new Exception("missing Tag");
                        }
                        line = reader.ReadLine();
                        if (line.StartsWith("FAIL")) {
                            failureCase = true;
                            pt = null;
                        }
                        else {
                            failureCase = false;
                            if (!ReadGcmTestVectorValue(line, "PT", ptLen, out pt)) {
                                throw new Exception("missing PT");
                            }
                        }

                        Test_AES_GCM_Decrypt_OutOfPlace(key, iv, ct, aad, tag, pt, failureCase, testVectorFile, keyLine);
                        Test_AES_GCM_Decrypt_InPlace(key, iv, ct, aad, tag, pt, failureCase, testVectorFile, keyLine);
                    }
                }
            }
        }

        private void Test_AES_GCM_Decrypt_OutOfPlace(byte[] key, byte[] iv, byte[] ct, byte[] aad, byte[] tag, byte[] pt, bool failureCase, string testVectorFile, string keyLine) {
            byte[] input = ExtendCopy(ct, 11, 13);
            byte[] inputOrig = (byte[])input.Clone();
            int inputOffset = 11;
            int inputLength = ct.Length;
            byte[] inputAAD = ExtendCopy(aad, 13, 15);
            byte[] inputAADOrig = (byte[])inputAAD.Clone();
            int inputAADOffset = 13;
            int inputAADLength = aad.Length;
            byte[] inputTag = ExtendCopy(tag, 7, 5);
            byte[] inputTagOrig = (byte[])inputTag.Clone();
            int inputTagOffset = 7;
            int inputTagLength = tag.Length;
            byte[] output = new byte[9 + ct.Length + 3];
            int outputOffset = 9;
            byte[] expected = failureCase ? null : ExtendCopy(pt, 9, 3);

            var aes = new AESBlockCipherGCM(key, iv);

            bool succeeded = aes.Decrypt(
                input: input,
                inputOffset: inputOffset,
                inputLength: inputLength,
                aad: inputAAD,
                aadOffset: inputAADOffset,
                aadLength: inputAADLength,
                tag: inputTag,
                tagOffset: inputTagOffset,
                tagLength: inputTagLength,
                output: output,
                outputOffset: outputOffset
            );

            Assert.AreEqual(failureCase ? false : true, succeeded, "{0} {1}: wrong result", testVectorFile, keyLine);

            if (!failureCase) {
                Assert.AreEqual(expected, output, "{0} {1}: wrong output", testVectorFile, keyLine);
            }

            Assert.AreEqual(inputOrig, input, "{0} {1}: input data were corrupted", testVectorFile, keyLine);
            Assert.AreEqual(inputAADOrig, inputAAD, "{0} {1}: AAD were corrupted", testVectorFile, keyLine);
            Assert.AreEqual(inputTagOrig, inputTag, "{0} {1}: Tag data were corrupted", testVectorFile, keyLine);

            Console.WriteLine("{0} {1}: PASS", testVectorFile, keyLine);
        }

        private void Test_AES_GCM_Decrypt_InPlace(byte[] key, byte[] iv, byte[] ct, byte[] aad, byte[] tag, byte[] pt, bool failureCase, string testVectorFile, string keyLine) {
            byte[] input = ExtendCopy(ct, 11, 13);
            int inputOffset = 11;
            int inputLength = ct.Length;
            byte[] inputAAD = ExtendCopy(aad, 13, 15);
            byte[] inputAADOrig = (byte[])inputAAD.Clone();
            int inputAADOffset = 13;
            int inputAADLength = aad.Length;
            byte[] inputTag = ExtendCopy(tag, 7, 5);
            byte[] inputTagOrig = (byte[])inputTag.Clone();
            int inputTagOffset = 7;
            int inputTagLength = tag.Length;
            byte[] output = input;
            int outputOffset = inputOffset;
            byte[] expected = failureCase ? null : ExtendCopy(pt, 11, 13);

            var aes = new AESBlockCipherGCM(key, iv);

            bool succeeded = aes.Decrypt(
                input: input,
                inputOffset: inputOffset,
                inputLength: inputLength,
                aad: inputAAD,
                aadOffset: inputAADOffset,
                aadLength: inputAADLength,
                tag: inputTag,
                tagOffset: inputTagOffset,
                tagLength: inputTagLength,
                output: output,
                outputOffset: outputOffset
            );

            Assert.AreEqual(failureCase ? false : true, succeeded, "{0} {1}: wrong result", testVectorFile, keyLine);

            if (!failureCase) {
                Assert.AreEqual(expected, output, "{0} {1}: wrong output", testVectorFile, keyLine);
            }

            Assert.AreEqual(inputAADOrig, inputAAD, "{0} {1}: AAD were corrupted", testVectorFile, keyLine);
            Assert.AreEqual(inputTagOrig, inputTag, "{0} {1}: Tag data were corrupted", testVectorFile, keyLine);

            Console.WriteLine("{0} {1}: PASS", testVectorFile, keyLine);
        }

        private static bool ReadGcmTestVectorParam(string line, out string name, out int value) {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"\[(\w+)\s*=\s*(\d+)\]");
            if (match.Success) {
                name = match.Groups[1].Value;
                value = Int32.Parse(match.Groups[2].Value, System.Globalization.NumberFormatInfo.InvariantInfo);
                return true;
            }
            else {
                name = null;
                value = -1;
                return false;
            }
        }

        private static bool ReadGcmTestVectorValue(string line, string key, int valueLen, out byte[] value) {
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(\w+)\s*=\s*([0-9a-f]+)?");
            if (match.Success) {
                if (match.Groups[1].Value != key) {
                    throw new Exception(String.Format("key name mismatch: actual={0} expected={1}", match.Groups[1].Value, key));
                }
                value = BigIntegerConverter.ParseHex(match.Groups[2].Value);
                if (value.Length * 8 != valueLen) {
                    throw new Exception(String.Format("value length mismatch: actual={0} expected={1}", value.Length, valueLen));
                }
                return true;
            }
            else {
                value = null;
                return false;
            }
        }

        //[Test]
        public void AES_GCM_Encrypt_Benchmark() {
            //Key = c0ff351317a08ac04b1f925e416b5c0f
            //IV = b1c8cc5d64c9199a34da1db6
            //PT = be1e3ad747afa026a37fdcffea185cd3aa6b6cc55c6bb4542155af1ac03fd94425573902914426f2979217d513369e2ea97347
            //AAD = b7537509c762449b29e589947b2be7c1
            //CT = 53ab8587aac7fa4d2b0d9c2ed09c644b2b90accf8aa4c478161c364dda9d0924bf78b40e9d072b41830bd529441d9a82cb2150
            //Tag = 192275948364b24c436901402a05a8

            byte[] key = BigIntegerConverter.ParseHex("c0ff351317a08ac04b1f925e416b5c0f");
            byte[] iv = BigIntegerConverter.ParseHex("b1c8cc5d64c9199a34da1db6");
            byte[] input = BigIntegerConverter.ParseHex("be1e3ad747afa026a37fdcffea185cd3aa6b6cc55c6bb4542155af1ac03fd94425573902914426f2979217d513369e2ea97347");
            byte[] inputAAD = BigIntegerConverter.ParseHex("b7537509c762449b29e589947b2be7c1");
            byte[] expectedOutput = BigIntegerConverter.ParseHex("53ab8587aac7fa4d2b0d9c2ed09c644b2b90accf8aa4c478161c364dda9d0924bf78b40e9d072b41830bd529441d9a82cb2150");
            byte[] expectedTag = BigIntegerConverter.ParseHex("192275948364b24c436901402a05a8");
            byte[] output = new byte[expectedOutput.Length];
            byte[] outputTag = new byte[expectedTag.Length];

            int count1 = 0;

            Stopwatch s1 = Stopwatch.StartNew();

            for (int n = 0; n < 1000; n++) {
                new AESBlockCipherGCM(key, iv);
                count1++;
            }

            TimeSpan t1 = s1.Elapsed;
            long perCall1 = (long)(t1.TotalMilliseconds * 1000000 / count1);

            var aes = new AESBlockCipherGCM(key, iv);

            int count2 = 0;

            Stopwatch s2 = Stopwatch.StartNew();

            for (int n = 0; n < 100000; n++) {
                aes.Encrypt(
                    input: input,
                    inputOffset: 0,
                    inputLength: input.Length,
                    aad: inputAAD,
                    aadOffset: 0,
                    aadLength: inputAAD.Length,
                    output: output,
                    outputOffset: 0,
                    outputTag: outputTag,
                    outputTagOffset: 0,
                    outputTagLength: outputTag.Length
                );

                //Assert.AreEqual(expectedOutput, output);
                //Assert.AreEqual(expectedTag, outputTag);

                count2++;
            }

            TimeSpan t2 = s2.Elapsed;
            long perCall2 = (long)(t2.TotalMilliseconds * 1000000 / count2);

            System.IO.File.WriteAllLines(
                "aes-gcm-benchmark-" + DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".txt",
                new string[] {
                    String.Format("Constructor: count={0} time={1} per call={2}[ns]", count1, t1, perCall1),
                    String.Format("Encryption:  count={0} time={1} per call={2}[ns]", count2, t2, perCall2),
                },
                System.Text.Encoding.UTF8
            );
        }

        [TestCase("deadbeefdeadbeefdeadbeef00000000", "deadbeefdeadbeefdeadbeef00000001", "deadbeefdeadbeefdeadbeef00000002")]
        [TestCase("deadbeefdeadbeefdeadbeef0000000f", "deadbeefdeadbeefdeadbeef00000010", "deadbeefdeadbeefdeadbeef00000011")]
        [TestCase("deadbeefdeadbeefdeadbeef000000ff", "deadbeefdeadbeefdeadbeef00000100", "deadbeefdeadbeefdeadbeef00000101")]
        [TestCase("deadbeefdeadbeefdeadbeef00000fff", "deadbeefdeadbeefdeadbeef00001000", "deadbeefdeadbeefdeadbeef00001001")]
        [TestCase("deadbeefdeadbeefdeadbeef0000ffff", "deadbeefdeadbeefdeadbeef00010000", "deadbeefdeadbeefdeadbeef00010001")]
        [TestCase("deadbeefdeadbeefdeadbeef000fffff", "deadbeefdeadbeefdeadbeef00100000", "deadbeefdeadbeefdeadbeef00100001")]
        [TestCase("deadbeefdeadbeefdeadbeef00ffffff", "deadbeefdeadbeefdeadbeef01000000", "deadbeefdeadbeefdeadbeef01000001")]
        [TestCase("deadbeefdeadbeefdeadbeef0fffffff", "deadbeefdeadbeefdeadbeef10000000", "deadbeefdeadbeefdeadbeef10000001")]
        [TestCase("deadbeefdeadbeefdeadbeeffffffffe", "deadbeefdeadbeefdeadbeefffffffff", "deadbeefdeadbeefdeadbeef00000000")]
        public void Test_AES_GCM_IncrementCounterBlock(string data, string expected1, string expected2) {
            byte[] key = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            byte[] iv = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            var aes = new AESBlockCipherGCM(key, iv);

            byte[] dataBytes = BigIntegerConverter.ParseHex(data);
            byte[] expected1Bytes = BigIntegerConverter.ParseHex(expected1);
            byte[] expected2Bytes = BigIntegerConverter.ParseHex(expected2);

            Debug.Assert(dataBytes.Length == 16 && expected1Bytes.Length == 16 && expected2Bytes.Length == 16);

            aes.IncrementCounter(dataBytes);

            Assert.AreEqual(expected1Bytes, dataBytes);

            aes.IncrementCounter(dataBytes);

            Assert.AreEqual(expected2Bytes, dataBytes);
        }

        #endregion

        #region UI128

        [Test]
        public void Test_UI128_From() {
            byte[] source =
            {
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
            };

            var ui128 = AESBlockCipherGCM.UI128.From(source, 7);

            Assert.AreEqual(0x1718191a1b1c1d1eUL, ui128.hi);
            Assert.AreEqual(0x1f20212223242526UL, ui128.lo);
        }

        [TestCase(3, 0, 0x0000000000000000UL, 0x0000000000000000UL)]
        [TestCase(3, 1, 0x1300000000000000UL, 0x0000000000000000UL)]
        [TestCase(3, 2, 0x1314000000000000UL, 0x0000000000000000UL)]
        [TestCase(3, 3, 0x1314150000000000UL, 0x0000000000000000UL)]
        [TestCase(3, 4, 0x1314151600000000UL, 0x0000000000000000UL)]
        [TestCase(3, 5, 0x1314151617000000UL, 0x0000000000000000UL)]
        [TestCase(3, 6, 0x1314151617180000UL, 0x0000000000000000UL)]
        [TestCase(3, 7, 0x1314151617181900UL, 0x0000000000000000UL)]
        [TestCase(3, 8, 0x131415161718191aUL, 0x0000000000000000UL)]
        [TestCase(3, 9, 0x131415161718191aUL, 0x1b00000000000000UL)]
        [TestCase(3, 10, 0x131415161718191aUL, 0x1b1c000000000000UL)]
        [TestCase(3, 11, 0x131415161718191aUL, 0x1b1c1d0000000000UL)]
        [TestCase(3, 12, 0x131415161718191aUL, 0x1b1c1d1e00000000UL)]
        [TestCase(3, 13, 0x131415161718191aUL, 0x1b1c1d1e1f000000UL)]
        [TestCase(3, 14, 0x131415161718191aUL, 0x1b1c1d1e1f200000UL)]
        [TestCase(3, 15, 0x131415161718191aUL, 0x1b1c1d1e1f202100UL)]
        [TestCase(3, 16, 0x131415161718191aUL, 0x1b1c1d1e1f202122UL)]
        public void Test_UI128_From_WithLength(int offset, int length, ulong expectedHi, ulong expectedLo) {
            byte[] source =
            {
                0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18, 0x19, 0x1a, 0x1b, 0x1c, 0x1d, 0x1e, 0x1f,
                0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
            };

            var ui128 = AESBlockCipherGCM.UI128.From(source, offset, length);

            Assert.AreEqual(expectedHi, ui128.hi);
            Assert.AreEqual(expectedLo, ui128.lo);
        }
        #endregion

        #region Utilities

        private static byte[] ExtendCopy(byte[] source, int prefixLength, int suffixLength) {
            byte[] b = new byte[prefixLength + source.Length + suffixLength];
            Array.Copy(source, 0, b, prefixLength, source.Length);
            return b;
        }

        #endregion
    }
}
#endif
