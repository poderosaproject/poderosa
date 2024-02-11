// Copyright (c) 2023 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using System;
using System.Runtime.CompilerServices;

using Granados.Crypto;
using Granados.Mono.Math;
using Granados.PKI;

namespace Granados.ECDH {

    /// <summary>
    /// Interface of a class implementing Elliptic Curve Diffie-Hellman
    /// </summary>
    public interface EllipticCurveDiffieHellman {
        /// <summary>
        /// Returns ephemeral public key octet string
        /// </summary>
        /// <returns>ephemeral public key octet string</returns>
        byte[] GetEphemeralPublicKey();

        /// <summary>
        /// Returns the curve size in bits
        /// </summary>
        /// <returns>the curve size in bits</returns>
        int GetCurveSize();

        /// <summary>
        /// Calcurates a shared secret
        /// </summary>
        /// <param name="serverEphemeralPublicKey">server's ephemeral public key</param>
        /// <returns>a shared secret</returns>
        /// <exception cref="EllipticCurveDiffieHellmanException">calcuration / validation error</exception>
        BigInteger CalcSharedSecret(byte[] serverEphemeralPublicKey);
    }

    /// <summary>
    /// Error in <see cref="EllipticCurveDiffieHellman"/>.
    /// </summary>
    public class EllipticCurveDiffieHellmanException : Exception {
        internal EllipticCurveDiffieHellmanException(string message)
            : base(message) {
        }
    }

    /// <summary>
    /// Creates an EllipticCurveDiffieHellman instance.
    /// </summary>
    public static class EllipticCurveDiffieHellmanFactory {
        /// <summary>
        /// Creates an EllipticCurveDiffieHellman instance from <see cref="KexAlgorithm"/>.
        /// </summary>
        /// <param name="kexAlgorithm">Key exchange algorithm</param>
        /// <returns>A new instance</returns>
        /// <exception cref="EllipticCurveDiffieHellmanException">failed to determine the implementation</exception>
        public static EllipticCurveDiffieHellman GetInstance(KexAlgorithm kexAlgorithm) {
            switch (kexAlgorithm) {
                case KexAlgorithm.ECDH_SHA2_NISTP256:
                    return GetEllipticCurveDiffieHellmanImpl("nistp256");
                case KexAlgorithm.ECDH_SHA2_NISTP384:
                    return GetEllipticCurveDiffieHellmanImpl("nistp384");
                case KexAlgorithm.ECDH_SHA2_NISTP521:
                    return GetEllipticCurveDiffieHellmanImpl("nistp521");
                case KexAlgorithm.CURVE25519_SHA256:
                case KexAlgorithm.CURVE25519_SHA256_LIBSSH:
                    return new MontgomeryCurveDiffieHellman(new MontgomeryCurve(MontgomeryCurveType.Curve25519));
                case KexAlgorithm.CURVE448_SHA512:
                    return new MontgomeryCurveDiffieHellman(new MontgomeryCurve(MontgomeryCurveType.Curve448));
                default:
                    throw new EllipticCurveDiffieHellmanException("Cannot determine ECDH processor : " + kexAlgorithm.ToString());
            }
        }

        private static EllipticCurveDiffieHellmanImpl GetEllipticCurveDiffieHellmanImpl(string curveName) {
            EllipticCurve curve = EllipticCurve.FindByName(curveName);
            if (curve == null) {
                throw new EllipticCurveDiffieHellmanException("Cannot determine elliptic curve : " + curveName);
            }
            return new EllipticCurveDiffieHellmanImpl(curve);
        }
    }

    /// <summary>
    /// Elliptic Curve Diffie-Hellman in RFC5656
    /// </summary>
    internal class EllipticCurveDiffieHellmanImpl : EllipticCurveDiffieHellman {

        private readonly EllipticCurve _curve;
        private readonly ECDSAKeyPair _keyPair;
        private readonly byte[] _publicKey;

        internal EllipticCurveDiffieHellmanImpl(EllipticCurve curve) {
            _curve = curve;
            _keyPair = curve.GenerateKeyPair();
            _publicKey = _curve.ConvertPointToOctetString(_keyPair.PublicKeyPoint);
        }

        /// <summary>
        /// Returns ephemeral public key octet string
        /// </summary>
        /// <returns></returns>
        public byte[] GetEphemeralPublicKey() {
            return _publicKey;
        }

        /// <summary>
        /// Returns the curve size in bits
        /// </summary>
        /// <returns></returns>
        public int GetCurveSize() {
            return _curve.Order.BitCount();
        }

        /// <summary>
        /// Calcurates a shared secret
        /// </summary>
        /// <param name="serverEphemeralPublicKey">server's ephemeral public key</param>
        /// <returns>a shared secret</returns>
        public BigInteger CalcSharedSecret(byte[] serverEphemeralPublicKey) {
            ECPoint serverPublicKeyPoint;
            if (!ECPoint.Parse(serverEphemeralPublicKey, _curve, out serverPublicKeyPoint)
                    || !_curve.ValidatePoint(serverPublicKeyPoint)) {
                throw new EllipticCurveDiffieHellmanException("Server's ephemeral public key is invalid");
            }

            ECPoint p = _curve.PointMul(_curve.Cofactor, _keyPair.PrivateKey, serverPublicKeyPoint, true);
            if (p == null) {
                throw new EllipticCurveDiffieHellmanException("Failed to get a shared secret");
            }

            return p.X;
        }
    }

    /// <summary>
    /// Curve25519 / Curve448 Diffie-Hellman in RFC8731 and RFC7748
    /// </summary>
    internal class MontgomeryCurveDiffieHellman : EllipticCurveDiffieHellman {

        private readonly MontgomeryCurve _curve;
        private readonly BigInteger _a;
        private readonly byte[] _k;

        internal MontgomeryCurveDiffieHellman(MontgomeryCurve curve) {
            _curve = curve;
            _a = curve.GetRandomValue();
            BigInteger k = curve.ScalarMultiplication(_a, curve.BaseU);
            _k = GetFixedLengthLittleEndianBytes(k, _curve.SizeInBytes);
        }

        /// <summary>
        /// Returns ephemeral public key octet string
        /// </summary>
        /// <returns></returns>
        public byte[] GetEphemeralPublicKey() {
            return _k;
        }

        /// <summary>
        /// Returns the curve size in bits
        /// </summary>
        /// <returns></returns>
        public int GetCurveSize() {
            return _curve.SizeInBits;
        }

        /// <summary>
        /// Calcurates a shared secret
        /// </summary>
        /// <param name="serverEphemeralPublicKey">server's ephemeral public key</param>
        /// <returns>a shared secret</returns>
        public BigInteger CalcSharedSecret(byte[] serverEphemeralPublicKey) {
            if (serverEphemeralPublicKey.Length != _curve.SizeInBytes) {
                throw new EllipticCurveDiffieHellmanException("Server's ephemeral public key is invalid");
            }

            byte[] s = (byte[])serverEphemeralPublicKey.Clone();
            Array.Reverse(s); // to the big endian
            BigInteger secret = _curve.ScalarMultiplication(_a, new BigInteger(s));

            if (secret == 0) {
                throw new EllipticCurveDiffieHellmanException("Failed to get a shared secret");
            }

            // In RFC7748, the result of the scalar multiplication is encoded in a fixed-length little-endian bytes.
            // In addition, RFC8731 says that the result bytes is reinterpreted as a fixed-length big-endian integer value.
            return ReverseBytes(secret);
        }

        private static byte[] GetFixedLengthLittleEndianBytes(BigInteger n, int size) {
            byte[] nBytes = n.GetBytes();
            if (nBytes.Length > size) {
                throw new ArgumentException("too large value");
            }
            byte[] data = new byte[size];
            Buffer.BlockCopy(nBytes, 0, data, data.Length - nBytes.Length, nBytes.Length);
            Array.Reverse(data);
            return data;
        }

        private static BigInteger ReverseBytes(BigInteger n) {
            // encode as little-endian
            byte[] data = n.GetBytes();
            Array.Reverse(data);
            // reinterpret as big-endian
            return new BigInteger(data);
        }
    }


    internal enum MontgomeryCurveType {
        Curve25519,
        Curve448,
    }

    internal class MontgomeryCurve {

        private readonly BigInteger _p;
        private readonly uint _a;
        private readonly uint _cofactor;
        private readonly int _bits;
        private readonly BigInteger _bu;
        // private readonly BigInteger _bv;

        internal MontgomeryCurve(MontgomeryCurveType type) {
            switch (type) {
                case MontgomeryCurveType.Curve25519:
                    _p = new BigInteger(new uint[] {
                            // 2^255 - 19
                            0x7fffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu,
                            0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffedu,
                    });
                    _a = 486662;
                    _cofactor = 8;
                    _bits = 255;
                    _bu = 9;
                    //_bv = new BigInteger(new uint[] {
                    //        // 14781619447589544791020593568409986887264606134616475288964881837755586237401
                    //        0x20ae19a1u, 0xb8a086b4u, 0xe01edd2cu, 0x7748d14cu,
                    //        0x923d4d7eu, 0x6d7c61b2u, 0x29e9c5a2u, 0x7eced3d9u,
                    //});
                    break;

                case MontgomeryCurveType.Curve448:
                    _p = new BigInteger(new uint[] {
                            // 2^448 - 2^224 - 1
                            0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu,
                            0xffffffffu, 0xffffffffu, 0xfffffffeu, 0xffffffffu,
                            0xffffffffu, 0xffffffffu, 0xffffffffu, 0xffffffffu,
                            0xffffffffu, 0xffffffffu,
                    });
                    _a = 156326;
                    _cofactor = 4;
                    _bits = 448;
                    _bu = 5;
                    //_bv = new BigInteger(new uint[] {
                    //        // 35529392678556817526412750206378333480897639938771427183188089843516908878696741
                    //        // 0002932673765864550910142774147268105838985595290606362
                    //        0x7d235d12u, 0x95f5b1f6u, 0x6c98ab6eu, 0x58326fceu,
                    //        0xcbae5d34u, 0xf55545d0u, 0x60f75dc2u, 0x8df3f6edu,
                    //        0xb8027e23u, 0x46430d21u, 0x1312c4b1u, 0x50677af7u,
                    //        0x6fd7223du, 0x457b5b1au,
                    //});
                    break;

                default:
                    throw new EllipticCurveDiffieHellmanException("unknown curve : " + type.ToString());
            }
        }

        internal int SizeInBits {
            get {
                return _bits;
            }
        }

        internal int SizeInBytes {
            get {
                return (_bits + 7) / 8;
            }
        }

        internal BigInteger BaseU {
            get {
                return _bu;
            }
        }

        internal BigInteger GetRandomValue() {
            byte[] random = new byte[SizeInBytes];
            RngManager.GetSecureRng().GetBytes(random);
            return new BigInteger(random);
        }

        /// <summary>
        /// The scalar multiplication specified in RFC7748.
        /// </summary>
        /// <param name="k">a scalar value</param>
        /// <param name="u">a u-coordinate value</param>
        /// <returns>result u-coordinate value</returns>
        internal BigInteger ScalarMultiplication(BigInteger k, BigInteger u) {
            u = new BigInteger(FixUCord(u));
            byte[] kBytes = FixScalar(k);

            BigInteger x1 = u;
            BigInteger x2 = 1;
            BigInteger z2 = 0;
            BigInteger x3 = u;
            BigInteger z3 = 1;
            bool swap = false;

            uint a24 = (_a - 2) / 4;

            byte bitMask = (byte)(1 << ((_bits - 1) % 8));

            foreach (byte kBt in kBytes) {
                while (bitMask != 0) {
                    bool kt = (kBt & bitMask) != 0;
                    swap ^= kt;
                    ConditionalSwap(swap, x2, x3, out x2, out x3);
                    ConditionalSwap(swap, z2, z3, out z2, out z3);
                    swap = kt;

                    BigInteger a = ModAdd(x2, z2);
                    BigInteger aa = ModSquare(a);
                    BigInteger b = ModSub(x2, z2);
                    BigInteger bb = ModSquare(b);
                    BigInteger e = ModSub(aa, bb);
                    BigInteger c = ModAdd(x3, z3);
                    BigInteger d = ModSub(x3, z3);
                    BigInteger da = ModMul(d, a);
                    BigInteger cb = ModMul(c, b);
                    x3 = ModSquare(ModAdd(da, cb));
                    z3 = ModMul(x1, ModSquare(ModSub(da, cb)));
                    x2 = ModMul(aa, bb);
                    z2 = ModMul(e, ModAdd(aa, a24 * e));

                    bitMask >>= 1;
                }
                bitMask = 0x80;
            }

            ConditionalSwap(swap, x2, x3, out x2, out x3);
            ConditionalSwap(swap, z2, z3, out z2, out z3);

            return ModMul(x2, z2.ModPow(_p - 2, _p));
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void ConditionalSwap(bool swap, BigInteger a, BigInteger b, out BigInteger c, out BigInteger d) {
            c = swap ? b : a;
            d = swap ? a : b;
        }

        // Fixes u-coordinate value into the valid range as described in RFC7748.
        // Returns fixed-length bytes encoded in big-endian.
        internal byte[] FixUCord(BigInteger u) {
            int size = SizeInBytes;
            byte[] uBytes = u.GetBytes(); // big endian
            if (uBytes.Length > size) {
                throw new EllipticCurveDiffieHellmanException("too large u value");
            }
            byte[] newBytes = new byte[size]; // big endian
            Buffer.BlockCopy(uBytes, 0, newBytes, size - uBytes.Length, uBytes.Length);
            int maskBits = _bits % 8;
            if (maskBits != 0) {
                byte mask = (byte)((1 << maskBits) - 1);
                newBytes[0] &= mask;
            }
            return newBytes;
        }

        // Fixes scalar value into the valid range as described in RFC7748.
        // Returns fixed-length bytes encoded in big-endian.
        internal byte[] FixScalar(BigInteger k) {
            int size = SizeInBytes;
            byte[] kBytes = k.GetBytes(); // big endian
            if (kBytes.Length > size) {
                throw new EllipticCurveDiffieHellmanException("too large scalar value");
            }
            byte[] newBytes = new byte[size]; // big endian
            Buffer.BlockCopy(kBytes, 0, newBytes, size - kBytes.Length, kBytes.Length);
            newBytes[newBytes.Length - 1] &= (byte)(0xff - (_cofactor - 1));
            int maskBits = _bits % 8;
            if (maskBits != 0) {
                byte mask = (byte)((1 << maskBits) - 1);
                newBytes[0] &= mask;
            }
            byte msb = (byte)(1 << ((_bits - 1) % 8));
            newBytes[0] |= msb;
            return newBytes;
        }

        private BigInteger ModSquare(BigInteger n) {
            return (n * n) % _p;
        }

        private BigInteger ModAdd(BigInteger a, BigInteger b) {
            return (a + b) % _p;
        }

        private BigInteger ModSub(BigInteger a, BigInteger b) {
            return (_p + a - b) % _p;
        }

        private BigInteger ModMul(BigInteger a, BigInteger b) {
            return (a * b) % _p;
        }
    }

#if DEBUG
    internal static class MontgomeryCurveTest {

        private static BigInteger FromLittleEndianHex(string hex) {
            byte[] bytes = BigIntegerConverter.ParseHex(hex);
            Array.Reverse(bytes);
            return new BigInteger(bytes);
        }

        private static void testScalarMultiplication(MontgomeryCurveType type, string kHex, string expectedFixedK, string uHex, string expectedFixedU, string expectedResultHex) {
            MontgomeryCurve curve = new MontgomeryCurve(type);

            BigInteger kVal = FromLittleEndianHex(kHex);
            BigInteger kFixed = new BigInteger(curve.FixScalar(kVal));
            BigInteger expectedFixedKVal = BigInteger.Parse(expectedFixedK);
            if (kFixed != expectedFixedKVal) {
                throw new Exception("wrong FixScalar() result");
            }

            BigInteger uVal = FromLittleEndianHex(uHex);
            BigInteger uFixed = new BigInteger(curve.FixUCord(uVal));
            BigInteger expectedFixedUVal = BigInteger.Parse(expectedFixedU);
            if (uFixed != expectedFixedUVal) {
                throw new Exception("wrong FixUCord() result");
            }

            BigInteger r = curve.ScalarMultiplication(kVal, uVal);
            BigInteger expectedResult = FromLittleEndianHex(expectedResultHex);
            if (r != expectedResult) {
                throw new Exception("wrong ScalarMultiplication() result");
            }
        }

        private static void testScalarMultiplicationIteration(MontgomeryCurveType type, string initialHex, string expected1Hex, string expected1000Hex, string expected1000000Hex) {
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
                if (expected != null && r != expected) {
                    throw new Exception("wrong ScalarMultiplication() result (iteration " + n.ToString() + ")");
                }

                u = k;
                k = r;
            }
        }

        internal static void TestX25519() {
            // test vectors from RFC7748

            testScalarMultiplication(
                MontgomeryCurveType.Curve25519,
                // Input scalar:
                "a546e36bf0527c9d3b16154b82465edd62144c0ac1fc5a18506a2244ba449ac4",
                // Input scalar as a number (base 10):
                "31029842492115040904895560451863089656472772604678260265531221036453811406496",
                // Input u-coordinate:
                "e6db6867583030db3594c1a424b15f7c726624ec26b3353b10a903a6d0ab1c4c",
                // Input u-coordinate as a number (base 10):
                "34426434033919594451155107781188821651316167215306631574996226621102155684838",
                // Output u-coordinate:
                "c3da55379de9c6908e94ea4df28d084f32eccf03491c71f754b4075577a28552"
            );

            testScalarMultiplication(
                MontgomeryCurveType.Curve25519,
                // Input scalar:
                "4b66e9d4d1b4673c5ad22691957d6af5c11b6421e0ea01d42ca4169e7918ba0d",
                // Input scalar as a number (base 10):
                "35156891815674817266734212754503633747128614016119564763269015315466259359304",
                // Input u-coordinate:
                "e5210f12786811d3f4b7959d0538ae2c31dbe7106fc03c3efc4cd549c715a493",
                // Input u-coordinate as a number (base 10):
                "8883857351183929894090759386610649319417338800022198945255395922347792736741",
                // Output u-coordinate:
                "95cbde9476e8907d7aade45cb4b873f88b595a68799fa152e6f8f7647aac7957"
            );

            testScalarMultiplicationIteration(
                MontgomeryCurveType.Curve25519,
                // Initial value
                "0900000000000000000000000000000000000000000000000000000000000000",
                // After one iteration:
                "422c8e7a6227d7bca1350b3e2bb7279f7897b87bb6854b783c60e80311ae3079",
                // After 1,000 iterations:
                "684cf59ba83309552800ef566f2f4d3c1c3887c49360e3875f2eb94d99532c51",
                // After 1,000,000 iterations:
                "7c3911e0ab2586fd864497297e575e6f3bc601c0883c30df5f4dd2d24f665424"
            );
        }

        internal static void TestX448() {
            // test vectors from RFC7748

            testScalarMultiplication(
                MontgomeryCurveType.Curve448,
                // Input scalar:
                "3d262fddf9ec8e88495266fea19a34d28882acef045104d0d1aae121700a779c984c24f8cdd78fbff44943eba368f54b29259a4f1c600ad3",
                // Input scalar as a number (base 10):
                "599189175373896402783756016145213256157230856085026129926891459468622403380588640249457727683869421921443004045221642549886377526240828",
                // Input u-coordinate:
                "06fce640fa3487bfda5f6cf2d5263f8aad88334cbd07437f020f08f9814dc031ddbdc38c19c6da2583fa5429db94ada18aa7a7fb4ef8a086",
                // Input u-coordinate as a number (base 10):
                "382239910814107330116229961234899377031416365240571325148346555922438025162094455820962429142971339584360034337310079791515452463053830",
                // Output u-coordinate:
                "ce3e4ff95a60dc6697da1db1d85e6afbdf79b50a2412d7546d5f239fe14fbaadeb445fc66a01b0779d98223961111e21766282f73dd96b6f"
            );

            testScalarMultiplication(
                MontgomeryCurveType.Curve448,
                // Input scalar:
                "203d494428b8399352665ddca42f9de8fef600908e0d461cb021f8c538345dd77c3e4806e25f46d3315c44e0a5b4371282dd2c8d5be3095f",
                // Input scalar as a number (base 10):
                "633254335906970592779259481534862372382525155252028961056404001332122152890562527156973881968934311400345568203929409663925541994577184",
                // Input u-coordinate:
                "0fbcc2f993cd56d3305b0b7d9e55d4c1a8fb5dbb52f8e9a1e9b6201b165d015894e56c4d3570bee52fe205e28a78b91cdfbde71ce8d157db",
                // Input u-coordinate as a number (base 10):
                "622761797758325444462922068431234180649590390024811299761625153767228042600197997696167956134770744996690267634159427999832340166786063",
                // Output u-coordinate:
                "884a02576239ff7a2f2f63b2db6a9ff37047ac13568e1e30fe63c4a7ad1b3ee3a5700df34321d62077e63633c575c1c954514e99da7c179d"
            );

            testScalarMultiplicationIteration(
                MontgomeryCurveType.Curve448,
                // Initial value
                "0500000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
                // After one iteration:
                "3f482c8a9f19b01e6c46ee9711d9dc14fd4bf67af30765c2ae2b846a4d23a8cd0db897086239492caf350b51f833868b9bc2b3bca9cf4113",
                // After 1,000 iterations:
                "aa3b4749d55b9daf1e5b00288826c467274ce3ebbdd5c17b975e09d4af6c67cf10d087202db88286e2b79fceea3ec353ef54faa26e219f38",
                // After 1,000,000 iterations:
                "077f453681caca3693198420bbe515cae0002472519b3e67661a7e89cab94695c8f4bcd66e61b9b9c946da8d524de3d69bd9d9d66b997e37"
            );
        }
    }
#endif

}
