// Copyright (c) 2005-2017 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using Granados.Mono.Math;

using System;
using System.Reflection;

namespace Granados.PKI {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ISigner {
        byte[] Sign(byte[] data);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IVerifier {
        void Verify(byte[] data, byte[] expected);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IKeyWriter {
        void WriteString(string s);
        void WriteByteString(byte[] data);
        void WriteBigInteger(BigInteger bi);
    }

    /// <summary>
    /// Public key algorithm
    /// </summary>
    public enum PublicKeyAlgorithm {
        [AlgorithmSpec(AlgorithmName = "ssh-dss", DefaultPriority = 1)]
        DSA,
        [AlgorithmSpec(AlgorithmName = "ssh-rsa", DefaultPriority = 2)]
        RSA,
        [AlgorithmSpec(AlgorithmName = "ecdsa-sha2-nistp256", DefaultPriority = 3)]
        ECDSA_SHA2_NISTP256,
        [AlgorithmSpec(AlgorithmName = "ecdsa-sha2-nistp384", DefaultPriority = 4)]
        ECDSA_SHA2_NISTP384,
        [AlgorithmSpec(AlgorithmName = "ecdsa-sha2-nistp521", DefaultPriority = 5)]
        ECDSA_SHA2_NISTP521,
        [AlgorithmSpec(AlgorithmName = "ssh-ed25519", DefaultPriority = 6)]
        ED25519,
    }

    /// <summary>
    /// Extension methods for <see cref="PublicKeyAlgorithm"/>
    /// </summary>
    public static class PublicKeyAlgorithmMixin {

        public static string GetAlgorithmName(this PublicKeyAlgorithm value) {
            return AlgorithmSpecUtil<PublicKeyAlgorithm>.GetAlgorithmName(value);
        }

        public static int GetDefaultPriority(this PublicKeyAlgorithm value) {
            return AlgorithmSpecUtil<PublicKeyAlgorithm>.GetDefaultPriority(value);
        }
    }

    /// <summary>
    /// Attribute to define the algorithm name and a related public key algorithm.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SignatureAlgorithmSpecAttribute : AlgorithmSpecAttribute {
        /// <summary>
        /// Public key algorithm to which this signature algorithm is related
        /// </summary>
        public PublicKeyAlgorithm PublicKeyAlgorithm {
            get;
            set;
        }
    }

    /// <summary>
    /// Signature algorithm variant
    /// </summary>
    public enum SignatureAlgorithmVariant {
        /// <summary>
        /// Use the default signature algorithm.
        /// </summary>
        [AlgorithmSpec(AlgorithmName = "", DefaultPriority = 1)]
        Default,

        [SignatureAlgorithmSpecAttribute(PublicKeyAlgorithm = PublicKeyAlgorithm.RSA, AlgorithmName = "rsa-sha2-256", DefaultPriority = 2)]
        RSA_SHA2_256,
        [SignatureAlgorithmSpecAttribute(PublicKeyAlgorithm = PublicKeyAlgorithm.RSA, AlgorithmName = "rsa-sha2-512", DefaultPriority = 3)]
        RSA_SHA2_512,
    }

    /// <summary>
    /// Extension methods for <see cref="SignatureAlgorithmVariant"/>
    /// </summary>
    public static class SignatureAlgorithmVariantMixin {

        public static string GetSignatureAlgorithmName(this SignatureAlgorithmVariant value) {
            return AlgorithmSpecUtil<SignatureAlgorithmVariant>.GetAlgorithmName(value);
        }

        public static int GetDefaultPriority(this SignatureAlgorithmVariant value) {
            return AlgorithmSpecUtil<SignatureAlgorithmVariant>.GetDefaultPriority(value);
        }

        public static string GetActualSignatureAlgorithmName(this SignatureAlgorithmVariant value, PublicKeyAlgorithm publicKeyAlgorithm) {
            return (value != SignatureAlgorithmVariant.Default)
                    ? AlgorithmSpecUtil<SignatureAlgorithmVariant>.GetAlgorithmName(value)
                    : publicKeyAlgorithm.GetAlgorithmName();
        }

        public static bool IsRelatedTo(this SignatureAlgorithmVariant value, PublicKeyAlgorithm algorithm) {
            AlgorithmSpecAttribute spec =
                AlgorithmSpecUtil<SignatureAlgorithmVariant>.GetAlgorithmSpec(value);
            SignatureAlgorithmSpecAttribute sigSpec = spec as SignatureAlgorithmSpecAttribute;
            return sigSpec != null && sigSpec.PublicKeyAlgorithm == algorithm;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public abstract class PublicKey {
        public abstract void WriteTo(IKeyWriter writer);
        public abstract PublicKeyAlgorithm Algorithm {
            get;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public abstract class KeyPair {

        public abstract PublicKey PublicKey {
            get;
        }
        public abstract PublicKeyAlgorithm Algorithm {
            get;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class PKIUtil {
        // OID { 1.3.14.3.2.26 }
        // iso(1) identified-org(3) OIW(14) secsig(3) alg(2) sha1(26)
        public static readonly byte[] SHA1_ASN_ID = new byte[] { 0x30, 0x21, 0x30, 0x09, 0x06, 0x05, 0x2b, 0x0e, 0x03, 0x02, 0x1a, 0x05, 0x00, 0x04, 0x14 };
        public static readonly byte[] SHA256_ASN_ID = new byte[] { 0x30, 0x31, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x01, 0x05, 0x00, 0x04, 0x20 };
        public static readonly byte[] SHA512_ASN_ID = new byte[] { 0x30, 0x51, 0x30, 0x0d, 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x02, 0x03, 0x05, 0x00, 0x04, 0x40 };
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class VerifyException : Exception {
        public VerifyException(string msg)
            : base(msg) {
        }
    }


}
