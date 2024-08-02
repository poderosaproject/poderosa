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
using Granados.Mono.Math;
using Granados.Crypto;
using NUnit.Framework;

namespace Granados.ECDH {

    [TestFixture]
    public class MontgomeryCurveTest {

        private BigInteger FromLittleEndianHex(string hex) {
            byte[] bytes = BigIntegerConverter.ParseHex(hex);
            Array.Reverse(bytes);
            return new BigInteger(bytes);
        }

        [TestCaseSource("X25519_ScalarMultiplicationPatterns")]
        [TestCaseSource("X448_ScalarMultiplicationPatterns")]
        public void TestScalarMultiplication(string curveTypeName, string kHex, string expectedFixedK, string uHex, string expectedFixedU, string expectedResultHex) {
            MontgomeryCurveType type =
                ((MontgomeryCurveType[])Enum.GetValues(typeof(MontgomeryCurveType)))
                    .Where((t) => t.ToString() == curveTypeName)
                    .First();

            MontgomeryCurve curve = new MontgomeryCurve(type);

            BigInteger kVal = FromLittleEndianHex(kHex);
            BigInteger kFixed = new BigInteger(curve.FixScalar(kVal));
            BigInteger expectedFixedKVal = BigInteger.Parse(expectedFixedK);

            Assert.AreEqual(expectedFixedKVal, kFixed);

            BigInteger uVal = FromLittleEndianHex(uHex);
            BigInteger uFixed = new BigInteger(curve.FixUCord(uVal));
            BigInteger expectedFixedUVal = BigInteger.Parse(expectedFixedU);

            Assert.AreEqual(expectedFixedUVal, uFixed);

            BigInteger r = curve.ScalarMultiplication(kVal, uVal);
            BigInteger expectedResult = FromLittleEndianHex(expectedResultHex);

            Assert.AreEqual(expectedResult, r);
        }

        [Ignore("This test will take a very long time.")]
        [TestCaseSource("X25519_ScalarMultiplicationIterationPatterns")]
        [TestCaseSource("X448_ScalarMultiplicationIterationPatterns")]
        public void TestScalarMultiplicationIteration(string curveTypeName, string initialHex, string expected1Hex, string expected1000Hex, string expected1000000Hex) {
            MontgomeryCurveType type =
                ((MontgomeryCurveType[])Enum.GetValues(typeof(MontgomeryCurveType)))
                    .Where((t) => t.ToString() == curveTypeName)
                    .First();

            MontgomeryCurve curve = new MontgomeryCurve(type);
            BigInteger k = FromLittleEndianHex(initialHex);
            BigInteger u = k;

            for (int n = 1; n <= 1000000; n++) {
                BigInteger r = curve.ScalarMultiplication(k, u);
                BigInteger expected;
                switch (n) {
                    case 1:
                        expected = FromLittleEndianHex(expected1Hex);
                        break;
                    case 1000:
                        expected = FromLittleEndianHex(expected1000Hex);
                        break;
                    case 1000000:
                        expected = FromLittleEndianHex(expected1000000Hex);
                        break;
                    default:
                        expected = null;
                        break;
                }

                if (expected != null) {
                    Assert.AreEqual(expected, r);
                }

                u = k;
                k = r;
            }
        }

        public static object[] X25519_ScalarMultiplicationPatterns =
        {
            // test vectors from RFC7748

            new object[] {
                MontgomeryCurveType.Curve25519.ToString(),
                // Input scalar:
                "a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4",
                // Input scalar as a number (base 10):
                "31029842492115040904895560451863089656472772604678260265531221036453811406496",
                // Input u-coordinate:
                "e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c",
                // Input u-coordinate as a number (base 10):
                "34426434033919594451155107781188821651316167215306631574996226621102155684838",
                // Output u-coordinate:
                "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552",
            },

            new object[] {
                MontgomeryCurveType.Curve25519.ToString(),
                // Input scalar:
                "4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d",
                // Input scalar as a number (base 10):
                "35156891815674817266734212754503633747128614016119564763269015315466259359304",
                // Input u-coordinate:
                "e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493",
                // Input u-coordinate as a number (base 10):
                "8883857351183929894090759386610649319417338800022198945255395922347792736741",
                // Output u-coordinate:
                "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957",
            },
        };

        public static object[] X25519_ScalarMultiplicationIterationPatterns =
        {
            new object[] {
                MontgomeryCurveType.Curve25519.ToString(),
                // Initial value
                "0900000000000000000000000000000000000000000000000000000000000000",
                // After one iteration:
                "422c8e7a6227d7bca1350b3e2bb7279f7897b87bb6854b783c60e80311ae3079",
                // After 1,000 iterations:
                "684cf59ba83309552800ef566f2f4d3c1c3887c49360e3875f2eb94d99532c51",
                // After 1,000,000 iterations:
                "7c3911e0ab2586fd864497297e575e6f3bc601c0883c30df5f4dd2d24f665424",
            },
        };

        public static object[] X448_ScalarMultiplicationPatterns =
        {
            // test vectors from RFC7748

            new object[] {
                MontgomeryCurveType.Curve448.ToString(),
                // Input scalar:
                "3d262fddf9ec8e88495266fea19a34d28882acef045104d0d1aae121700a779c984c24f8cdd78fbff44943eba368f54b29259a4f1c600ad3",
                // Input scalar as a number (base 10):
                "599189175373896402783756016145213256157230856085026129926891459468622403380588640249457727683869421921443004045221642549886377526240828",
                // Input u-coordinate:
                "06fce640fa3487bfda5f6cf2d5263f8aad88334cbd07437f020f08f9814dc031ddbdc38c19c6da2583fa5429db94ada18aa7a7fb4ef8a086",
                // Input u-coordinate as a number (base 10):
                "382239910814107330116229961234899377031416365240571325148346555922438025162094455820962429142971339584360034337310079791515452463053830",
                // Output u-coordinate:
                "ce3e4ff95a60dc6697da1db1d85e6afbdf79b50a2412d7546d5f239fe14fbaadeb445fc66a01b0779d98223961111e21766282f73dd96b6f",
            },

            new object[] {
                MontgomeryCurveType.Curve448.ToString(),
                // Input scalar:
                "203d494428b8399352665ddca42f9de8fef600908e0d461cb021f8c538345dd77c3e4806e25f46d3315c44e0a5b4371282dd2c8d5be3095f",
                // Input scalar as a number (base 10):
                "633254335906970592779259481534862372382525155252028961056404001332122152890562527156973881968934311400345568203929409663925541994577184",
                // Input u-coordinate:
                "0fbcc2f993cd56d3305b0b7d9e55d4c1a8fb5dbb52f8e9a1e9b6201b165d015894e56c4d3570bee52fe205e28a78b91cdfbde71ce8d157db",
                // Input u-coordinate as a number (base 10):
                "622761797758325444462922068431234180649590390024811299761625153767228042600197997696167956134770744996690267634159427999832340166786063",
                // Output u-coordinate:
                "884a02576239ff7a2f2f63b2db6a9ff37047ac13568e1e30fe63c4a7ad1b3ee3a5700df34321d62077e63633c575c1c954514e99da7c179d",
            },
        };

        public static object[] X448_ScalarMultiplicationIterationPatterns =
        {
            new object[] {
                MontgomeryCurveType.Curve448.ToString(),
                // Initial value
                "0500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
                // After one iteration:
                "3f482c8a9f19b01e6c46ee9711d9dc14fd4bf67af30765c2ae2b846a4d23a8cd0db897086239492caf350b51f833868b9bc2b3bca9cf4113",
                // After 1,000 iterations:
                "aa3b4749d55b9daf1e5b00288826c467274ce3ebbdd5c17b975e09d4af6c67cf10d087202db88286e2b79fceea3ec353ef54faa26e219f38",
                // After 1,000,000 iterations:
                "077f453681caca3693198420bbe515cae0002472519b3e67661a7e89cab94695c8f4bcd66e61b9b9c946da8d524de3d69bd9d9d66b997e37",
            },
        };
    }
}
#endif