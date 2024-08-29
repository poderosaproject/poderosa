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

}
