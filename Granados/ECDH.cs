// Copyright (c) 2023 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using System;

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
    /// Elliptic curve Diffie-Hellman in RFC5656
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

}
