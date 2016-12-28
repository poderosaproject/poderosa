/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.


 $Id: PKIBase.cs,v 1.5 2011/11/08 12:24:05 kzmi Exp $
*/
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
        [AlgorithmSpec(AlgorithmName = "ssh-dss", Order = 1)]
        DSA,
        [AlgorithmSpec(AlgorithmName = "ssh-rsa", Order = 2)]
        RSA,
        [AlgorithmSpec(AlgorithmName = "ecdsa-sha2-nistp256", Order = 3)]
        ECDSA_SHA2_NISTP256,
        [AlgorithmSpec(AlgorithmName = "ecdsa-sha2-nistp384", Order = 4)]
        ECDSA_SHA2_NISTP384,
        [AlgorithmSpec(AlgorithmName = "ecdsa-sha2-nistp521", Order = 5)]
        ECDSA_SHA2_NISTP521,
    }

    /// <summary>
    /// Attribute to define algorithm name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class AlgorithmSpecAttribute : Attribute {
        /// <summary>
        /// Algorithm name
        /// </summary>
        public string AlgorithmName {
            get;
            set;
        }

        /// <summary>
        /// Default priority order
        /// </summary>
        public int Order {
            get;
            set;
        }
    }

    /// <summary>
    /// Extension methods for <see cref="PublicKeyAlgorithm"/>
    /// </summary>
    public static class PublicKeyAlgorithmMixin {

        public static string GetAlgorithmName(this PublicKeyAlgorithm value) {
            var spec = GetAlgorithmSpec(value);
            return (spec != null) ? spec.AlgorithmName : null;
        }

        public static string GetOrder(this PublicKeyAlgorithm value) {
            var spec = GetAlgorithmSpec(value);
            return (spec != null) ? spec.AlgorithmName : null;
        }

        private static AlgorithmSpecAttribute GetAlgorithmSpec(PublicKeyAlgorithm value) {
            Type enumType = typeof(PublicKeyAlgorithm);
            FieldInfo field = enumType.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);
            if (field == null) {
                throw new ArgumentException("Not a member of enum " + enumType.Name, "value");
            }

            object[] attrs = field.GetCustomAttributes(typeof(AlgorithmSpecAttribute), false);
            if (attrs.Length > 0) {
                return (AlgorithmSpecAttribute)attrs[0];
            }
            return null;
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
