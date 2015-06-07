/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SSH.cs,v 1.3 2011/10/27 23:21:57 kzmi Exp $
 */
using System;
using System.Text;

using Granados;
using Granados.PKI;


namespace Poderosa.Protocols {
    //Granadosを使うやつはこちら　起動時にはロードしないようにするため
    internal class LocalSSHUtil {
        public static CipherAlgorithm ParseCipherAlgorithm(string t) {
            if (t == "AES128")
                return CipherAlgorithm.AES128;
            else if (t == "AES192")
                return CipherAlgorithm.AES192;
            else if (t == "AES256")
                return CipherAlgorithm.AES256;
            else if (t == "AES128CTR")
                return CipherAlgorithm.AES128CTR;
            else if (t == "AES192CTR")
                return CipherAlgorithm.AES192CTR;
            else if (t == "AES256CTR")
                return CipherAlgorithm.AES256CTR;
            else if (t == "Blowfish")
                return CipherAlgorithm.Blowfish;
            else if (t == "TripleDES")
                return CipherAlgorithm.TripleDES;
            else
                throw new Exception("Unknown CipherAlgorithm " + t);
        }
        public static CipherAlgorithm[] ParseCipherAlgorithm(string[] t) {
            CipherAlgorithm[] ret = new CipherAlgorithm[t.Length];
            int i = 0;
            foreach (string a in t) {
                ret[i++] = ParseCipherAlgorithm(a);
            }
            return ret;
        }
        public static string[] FormatPublicKeyAlgorithmList(PublicKeyAlgorithm[] value) {
            string[] ret = new string[value.Length];
            int i = 0;
            foreach (PublicKeyAlgorithm a in value)
                ret[i++] = a.ToString();
            return ret;
        }

        public static CipherAlgorithm[] ParseCipherAlgorithmList(string value) {
            return ParseCipherAlgorithm(value.Split(','));
        }


        public static PublicKeyAlgorithm ParsePublicKeyAlgorithm(string t) {
            if (t == "DSA")
                return PublicKeyAlgorithm.DSA;
            else if (t == "RSA")
                return PublicKeyAlgorithm.RSA;
            else
                throw new Exception("Unknown PublicKeyAlgorithm " + t);
        }
        public static PublicKeyAlgorithm[] ParsePublicKeyAlgorithm(string[] t) {
            PublicKeyAlgorithm[] ret = new PublicKeyAlgorithm[t.Length];
            int i = 0;
            foreach (string a in t) {
                ret[i++] = ParsePublicKeyAlgorithm(a);
            }
            return ret;
        }
        public static PublicKeyAlgorithm[] ParsePublicKeyAlgorithmList(string value) {
            return ParsePublicKeyAlgorithm(value.Split(','));
        }

        public static string SimpleEncrypt(string plain) {
            byte[] t = Encoding.ASCII.GetBytes(plain);
            if ((t.Length % 16) != 0) {
                byte[] t2 = new byte[t.Length + (16 - (t.Length % 16))];
                Array.Copy(t, 0, t2, 0, t.Length);
                for (int i = t.Length + 1; i < t2.Length; i++) //残りはダミー
                    t2[i] = t[i % t.Length];
                t = t2;
            }

            byte[] key = Encoding.ASCII.GetBytes("- BOBO VIERI 32-");
            Granados.Algorithms.Rijndael rijndael = new Granados.Algorithms.Rijndael();
            rijndael.InitializeKey(key);

            byte[] e = new byte[t.Length];
            rijndael.encryptCBC(t, 0, t.Length, e, 0);

            return Encoding.ASCII.GetString(Granados.Util.Base64.Encode(e));
        }
        public static string SimpleDecrypt(string enc) {
            byte[] t = Granados.Util.Base64.Decode(Encoding.ASCII.GetBytes(enc));
            byte[] key = Encoding.ASCII.GetBytes("- BOBO VIERI 32-");
            Granados.Algorithms.Rijndael rijndael = new Granados.Algorithms.Rijndael();
            rijndael.InitializeKey(key);

            byte[] d = new byte[t.Length];
            rijndael.decryptCBC(t, 0, t.Length, d, 0);

            return Encoding.ASCII.GetString(d); //パディングがあってもNULL文字になるので除去されるはず
        }
    }

    //鍵のチェック関係
    /// <summary>
    /// <ja>
    /// 鍵のチェック状況を示します。
    /// </ja>
    /// </summary>
    internal enum KeyCheckResult {
        /// <summary>
        /// <ja>正しい鍵</ja>
        /// <en>Correct key.</en>
        /// </summary>
        OK,
        /// <summary>
        /// <ja>保持している内容と異なる鍵</ja>
        /// <en>Key is different from held one.</en>
        /// </summary>
        Different,
        /// <summary>
        /// <ja>鍵情報が見つからない</ja>
        /// <en>Key is not exist.</en>
        /// </summary>
        NotExists
    }
}
