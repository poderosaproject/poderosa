/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: SSHUtil.cs,v 1.5 2011/10/27 23:21:56 kzmi Exp $
*/
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text;
//using SHA1CryptoServiceProvider = System.Security.Cryptography.SHA1CryptoServiceProvider;
using HMACSHA1 = System.Security.Cryptography.HMACSHA1;

using Granados.PKI;
using Granados.Crypto;
using System.Reflection;

namespace Granados {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class SSHException : Exception {
        private byte[] _data;

        public SSHException(string msg, byte[] data)
            : base(msg) {
            _data = data;
        }

        public SSHException(string msg)
            : base(msg) {
        }
    }

    /// <summary>
    /// <ja>SSHプロトコルの種類を示します</ja>
    /// <en>Kind of SSH protocol</en>
    /// </summary>
    public enum SSHProtocol {
        /// <summary>
        /// <ja>SSH1</ja>
        /// <en>SSH1</en>
        /// </summary>
        SSH1,
        /// <summary>
        /// <ja>SSH2</ja>
        /// <en>SSH2</en>
        /// </summary>
        SSH2
    }

    /// <summary>
    /// <ja>
    /// アルゴリズムの種類を示します。
    /// </ja>
    /// <en>
    /// Kind of algorithm
    /// </en>
    /// </summary>
    public enum CipherAlgorithm {
        /// <summary>
        /// TripleDES
        /// </summary>
        TripleDES = 3,
        /// <summary>
        /// BlowFish
        /// </summary>
        Blowfish = 6,
        /// <summary>
        /// <ja>AES128（SSH2のみ有効）</ja>
        /// <en>AES128（SSH2 only）</en>
        /// </summary>
        AES128 = 10,
        /// <summary>
        /// <ja>AES192（SSH2のみ有効）</ja>
        /// <en>AES192（SSH2 only）</en>
        /// </summary>
        AES192 = 11,
        /// <summary>
        /// <ja>AES256（SSH2のみ有効）</ja>
        /// <en>AES256（SSH2 only）</en>
        /// </summary>
        AES256 = 12,
        /// <summary>
        /// <ja>AES128-CTR（SSH2のみ有効）</ja>
        /// <en>AES128-CTR（SSH2 only）</en>
        /// </summary>
        AES128CTR = 13,
        /// <summary>
        /// <ja>AES192-CTR（SSH2のみ有効）</ja>
        /// <en>AES192-CTR（SSH2 only）</en>
        /// </summary>
        AES192CTR = 14,
        /// <summary>
        /// <ja>AES256-CTR（SSH2のみ有効）</ja>
        /// <en>AES256-CTR（SSH2 only）</en>
        /// </summary>
        AES256CTR = 15,
    }

    /// <summary>
    /// <ja>認証方式を示します。</ja>
    /// <en>Kind of authentification method</en>
    /// </summary>
    public enum AuthenticationType {
        /// <summary>
        /// <ja>公開鍵方式</ja>
        /// <en>Public key cryptosystem</en>
        /// </summary>
        PublicKey = 2, //uses identity file
        /// <summary>
        /// <ja>パスワード方式</ja>
        /// <en>Password Authentication</en>
        /// </summary>
        Password = 3,
        /// <summary>
        /// <ja>キーボードインタラクティブ</ja>
        /// <en>KeyboardInteractive</en>
        /// </summary>
        KeyboardInteractive = 4
    }

    /// <summary>
    /// <ja>
    /// 認証結果を示します。
    /// </ja>
    /// <en>
    /// Result of authentication.
    /// </en>
    /// </summary>
    public enum AuthenticationResult {
        /// <summary>
        /// <ja>成功</ja>
        /// <en>Succeed</en>
        /// </summary>
        Success,
        /// <summary>
        /// <ja>失敗</ja>
        /// <en>Failed</en>
        /// </summary>
        Failure,
        /// <summary>
        /// <ja>プロンプト</ja>
        /// <en>Prompt</en>
        /// </summary>
        Prompt
    }


    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum MACAlgorithm {
        HMACSHA1
    }

    /// <summary>
    /// <ja>鍵交換アルゴリズム</ja>
    /// <en>key exchange algorighm</en>
    /// </summary>
    /// <exclude/>
    public enum KexAlgorithm {
        /// <summary>diffie-hellman-group1-sha1 described in RFC4253</summary>
        DH_G1_SHA1,
        /// <summary>diffie-hellman-group14-sha1 described in RFC4253</summary>
        DH_G14_SHA1,
        /// <summary>diffie-hellman-group14-sha256 described in draft-ietf-curdle-ssh-kex-sha2</summary>
        DH_G14_SHA256,
        /// <summary>diffie-hellman-group16-sha512 described in draft-ietf-curdle-ssh-kex-sha2</summary>
        DH_G16_SHA512,
        /// <summary>diffie-hellman-group18-sha512 described in draft-ietf-curdle-ssh-kex-sha2</summary>
        DH_G18_SHA512,
    }
}

namespace Granados.Util {

    internal static class SSHUtil {

        /// <summary>
        /// Get version string of the Granados.
        /// </summary>
        /// <param name="p">SSH protocol type</param>
        /// <returns>a version string</returns>
        public static string ClientVersionString(SSHProtocol p) {
            Assembly assy = Assembly.GetAssembly(typeof(SSHUtil));
            Version ver = assy.GetName().Version;
            string s = String.Format("{0}-{1}.{2}",
                            (p == SSHProtocol.SSH1) ? "SSH-1.5-Granados" : "SSH-2.0-Granados",
                            ver.Major, ver.Minor);
            return s;
        }

        /// <summary>
        /// Read Int32 value in network byte order.
        /// </summary>
        /// <param name="data">source byte array</param>
        /// <param name="offset">index to start reading</param>
        /// <returns>Int32 value</returns>
        public static int ReadInt32(byte[] data, int offset) {
            return (int)ReadUInt32(data, offset);
        }

        /// <summary>
        /// Read UInt32 value in network byte order.
        /// </summary>
        /// <param name="data">source byte array</param>
        /// <param name="offset">index to start reading</param>
        /// <returns>UInt32 value</returns>
        public static uint ReadUInt32(byte[] data, int offset) {
            uint ret = 0;
            ret |= data[offset];
            ret <<= 8;
            ret |= data[offset + 1];
            ret <<= 8;
            ret |= data[offset + 2];
            ret <<= 8;
            ret |= data[offset + 3];
            return ret;
        }

        /// <summary>
        /// Write Int32 value in network byte order.
        /// </summary>
        /// <param name="dst">byte array to be written</param>
        /// <param name="pos">index to start writing</param>
        /// <param name="data">Int32 value</param>
        public static void WriteIntToByteArray(byte[] dst, int pos, int data) {
            WriteUIntToByteArray(dst, pos, (uint)data);
        }

        /// <summary>
        /// Write UInt32 value in network byte order.
        /// </summary>
        /// <param name="dst">byte array to be written</param>
        /// <param name="pos">index to start writing</param>
        /// <param name="data">UInt32 value</param>
        public static void WriteUIntToByteArray(byte[] dst, int pos, uint data) {
            dst[pos] = (byte)(data >> 24);
            dst[pos + 1] = (byte)(data >> 16);
            dst[pos + 2] = (byte)(data >> 8);
            dst[pos + 3] = (byte)(data);
        }

        /// <summary>
        /// Check if a string array contains a particular string.
        /// </summary>
        /// <param name="s">a string array</param>
        /// <param name="v">a string</param>
        /// <returns>true if <paramref name="s"/> contains <paramref name="v"/>.</returns>
        public static bool ContainsString(string[] s, string v) {
            foreach (string x in s) {
                if (x == v) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if the contents of two byte arrays are identical.
        /// </summary>
        /// <param name="d1">a byte array</param>
        /// <param name="d2">a byte array</param>
        /// <returns>true if the contents of two byte arrays are identical.</returns>
        public static bool ByteArrayEqual(byte[] d1, byte[] d2) {
            if (d1.Length != d2.Length) {
                return false;
            }
            return ByteArrayEqual(d1, 0, d2, 0, d1.Length);
        }

        /// <summary>
        /// Check if the partial contents of two byte arrays are identical.
        /// </summary>
        /// <param name="d1">a byte array</param>
        /// <param name="o1">index of <paramref name="d1"/> to start comparison</param>
        /// <param name="d2">a byte array</param>
        /// <param name="o2">index of <paramref name="d2"/> to start comparison</param>
        /// <param name="len">number of bytes to compare</param>
        /// <returns>true if the partial contents of two byte arrays are identical.</returns>
        public static bool ByteArrayEqual(byte[] d1, int o1, byte[] d2, int o2, int len) {
            for (int i = 0; i < len; ++i) {
                if (d1[o1++] != d2[o2++]) {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class Strings {
        private static StringResources _strings;
        public static string GetString(string id) {
            if (_strings == null)
                Reload();
            return _strings.GetString(id);
        }

        //load resource corresponding to current culture
        public static void Reload() {
            _strings = new StringResources("Granados.strings", typeof(Strings).Assembly);
        }
    }

    internal class DebugUtil {
        public static string DumpByteArray(byte[] data) {
            return DumpByteArray(data, 0, data.Length);
        }
        public static string DumpByteArray(byte[] data, int offset, int length) {
            StringBuilder bld = new StringBuilder();
            for (int i = 0; i < length; i++) {
                bld.Append(data[offset + i].ToString("X2"));
                if ((i % 4) == 3)
                    bld.Append(' ');
            }
            return bld.ToString();
        }

        public static string CurrentThread() {
            Thread t = Thread.CurrentThread;
            return t.GetHashCode().ToString();
        }
    }
}
