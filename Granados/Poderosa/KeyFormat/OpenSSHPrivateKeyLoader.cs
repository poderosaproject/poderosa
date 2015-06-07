/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OpenSSHPrivateKeyLoader.cs,v 1.1 2011/11/03 16:27:38 kzmi Exp $
 */
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Security.Cryptography;

using Granados.Crypto;
using Granados.PKI;
using Granados.Util;

namespace Granados.Poderosa.KeyFormat {

    /// <summary>
    /// OpenSSH SSH2 private key loader
    /// </summary>
    internal class OpenSSHPrivateKeyLoader : ISSH2PrivateKeyLoader {

        private readonly string keyFilePath;
        private readonly byte[] keyFile;

        private enum KeyType {
            RSA,
            DSA,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="keyFile">key file data</param>
        /// <param name="keyFilePath">Path of a key file</param>
        public OpenSSHPrivateKeyLoader(byte[] keyFile, string keyFilePath) {
            this.keyFilePath = keyFilePath;
            this.keyFile = keyFile;
        }


        /// <summary>
        /// Read OpenSSH SSH2 private key parameters.
        /// </summary>
        /// <param name="passphrase">passphrase for decrypt the key file</param>
        /// <param name="keyPair">key pair</param>
        /// <param name="comment">comment or empty if it didn't exist</param>
        public void Load(string passphrase, out KeyPair keyPair, out string comment) {
            if (keyFile == null)
                throw new SSHException("A key file is not loaded yet");

            KeyType keyType;
            String base64Text;
            bool encrypted = false;
            CipherAlgorithm? encryption = null;
            byte[] iv = null;
            int keySize = 0;
            int ivSize = 0;
            using (StreamReader sreader = GetStreamReader()) {
                string line = sreader.ReadLine();
                if (line == null)
                    throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (unexpected eof)");

                if (line == PrivateKeyFileHeader.SSH2_OPENSSH_HEADER_RSA)
                    keyType = KeyType.RSA;
                else if (line == PrivateKeyFileHeader.SSH2_OPENSSH_HEADER_DSA)
                    keyType = KeyType.DSA;
                else
                    throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (unexpected key type)");

                string footer = line.Replace("BEGIN", "END");

                StringBuilder buf = new StringBuilder();
                comment = String.Empty;
                while (true) {
                    line = sreader.ReadLine();
                    if (line == null)
                        throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (unexpected eof)");
                    if (line == footer)
                        break;
                    if (line.IndexOf(':') >= 0) {
                        if (line.StartsWith("Proc-Type:")) {
                            string[] w = line.Substring("Proc-Type:".Length).Trim().Split(',');
                            if (w.Length < 1)
                                throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (invalid Proc-Type)");
                            if (w[0] != "4")
                                throw new SSHException(Strings.GetString("UnsupportedPrivateKeyFormat")
                                            + " (" + Strings.GetString("Reason_UnsupportedProcType") + ")");
                            if (w.Length >= 2 && w[1] == "ENCRYPTED")
                                encrypted = true;
                        }
                        else if (line.StartsWith("DEK-Info:")) {
                            string[] w = line.Substring("DEK-Info:".Length).Trim().Split(',');
                            if (w.Length < 2)
                                throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (invalid DEK-Info)");
                            switch (w[0]) {
                                case "DES-EDE3-CBC":
                                    encryption = CipherAlgorithm.TripleDES;
                                    ivSize = 8;
                                    keySize = 24;
                                    break;
                                case "AES-128-CBC":
                                    encryption = CipherAlgorithm.AES128;
                                    ivSize = 16;
                                    keySize = 16;
                                    break;
                                default:
                                    throw new SSHException(Strings.GetString("UnsupportedPrivateKeyFormat")
                                            + " (" + Strings.GetString("Reason_UnsupportedEncryptionType") + ")");
                            }
                            iv = HexToByteArray(w[1]);
                            if (iv == null || iv.Length != ivSize)
                                throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (invalid IV)");
                        }
                    }
                    else
                        buf.Append(line);
                }
                base64Text = buf.ToString();
            }

            byte[] keydata = Base64.Decode(Encoding.ASCII.GetBytes(base64Text));

            if (encrypted) {
                if (!encryption.HasValue || iv == null)
                    throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (missing encryption type or IV)");
                byte[] key = OpenSSHPassphraseToKey(passphrase, iv, keySize);
                Cipher cipher = CipherFactory.CreateCipher(SSHProtocol.SSH2, encryption.Value, key, iv);
                if (keydata.Length % cipher.BlockSize != 0)
                    throw new SSHException(Strings.GetString("NotValidPrivateKeyFile") + " (invalid key data size)");
                cipher.Decrypt(keydata, 0, keydata.Length, keydata, 0);
            }

            using (MemoryStream keyDataStream = new MemoryStream(keydata, false)) {
                BERReader reader = new BERReader(keyDataStream);
                if (!reader.ReadSequence())
                    throw new SSHException(Strings.GetString("WrongPassphrase"));
                if (keyType == KeyType.RSA) {
                    /* from OpenSSL rsa_asn1.c
                     * 
                     * ASN1_SIMPLE(RSA, version, LONG),
                     * ASN1_SIMPLE(RSA, n, BIGNUM),
                     * ASN1_SIMPLE(RSA, e, BIGNUM),
                     * ASN1_SIMPLE(RSA, d, BIGNUM),
                     * ASN1_SIMPLE(RSA, p, BIGNUM),
                     * ASN1_SIMPLE(RSA, q, BIGNUM),
                     * ASN1_SIMPLE(RSA, dmp1, BIGNUM),
                     * ASN1_SIMPLE(RSA, dmq1, BIGNUM),
                     * ASN1_SIMPLE(RSA, iqmp, BIGNUM)
                     */
                    BigInteger v, n, e, d, p, q, dmp1, dmq1, iqmp;
                    if (!reader.ReadInteger(out v) ||
                        !reader.ReadInteger(out n) ||
                        !reader.ReadInteger(out e) ||
                        !reader.ReadInteger(out d) ||
                        !reader.ReadInteger(out p) ||
                        !reader.ReadInteger(out q) ||
                        !reader.ReadInteger(out dmp1) ||
                        !reader.ReadInteger(out dmq1) ||
                        !reader.ReadInteger(out iqmp)) {

                        throw new SSHException(Strings.GetString("WrongPassphrase"));
                    }

                    BigInteger u = p.modInverse(q);	// inverse of p mod q
                    keyPair = new RSAKeyPair(e, d, n, u, p, q);
                }
                else if (keyType == KeyType.DSA) {
                    /* from OpenSSL dsa_asn1.c
                     * 
                     * ASN1_SIMPLE(DSA, version, LONG),
                     * ASN1_SIMPLE(DSA, p, BIGNUM),
                     * ASN1_SIMPLE(DSA, q, BIGNUM),
                     * ASN1_SIMPLE(DSA, g, BIGNUM),
                     * ASN1_SIMPLE(DSA, pub_key, BIGNUM),
                     * ASN1_SIMPLE(DSA, priv_key, BIGNUM)
                     */
                    BigInteger v, p, q, g, y, x;
                    if (!reader.ReadInteger(out v) ||
                        !reader.ReadInteger(out p) ||
                        !reader.ReadInteger(out q) ||
                        !reader.ReadInteger(out g) ||
                        !reader.ReadInteger(out y) ||
                        !reader.ReadInteger(out x)) {

                        throw new SSHException(Strings.GetString("WrongPassphrase"));
                    }
                    keyPair = new DSAKeyPair(p, g, q, y, x);
                }
                else {
                    throw new SSHException("Unknown file type. This should not happen.");
                }
            }
        }

        private static byte[] OpenSSHPassphraseToKey(string passphrase, byte[] iv, int length) {
            const int HASH_SIZE = 16;
            const int SALT_SIZE = 8;
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] pp = Encoding.UTF8.GetBytes(passphrase);
            byte[] buf = new byte[((length + HASH_SIZE - 1) / HASH_SIZE) * HASH_SIZE];
            int offset = 0;

            while (offset < length) {
                if (offset > 0)
                    md5.TransformBlock(buf, 0, offset, null, 0);
                md5.TransformBlock(pp, 0, pp.Length, null, 0);
                md5.TransformFinalBlock(iv, 0, SALT_SIZE);
                Buffer.BlockCopy(md5.Hash, 0, buf, offset, HASH_SIZE);
                offset += HASH_SIZE;
                md5.Initialize();
            }
            md5.Clear();

            byte[] key = new byte[length];
            Buffer.BlockCopy(buf, 0, key, 0, length);
            return key;
        }

        private static byte[] HexToByteArray(string text) {
            if (text.Length % 2 != 0)
                return null;
            int len = text.Length / 2;
            byte[] buf = new byte[len];
            for (int i = 0; i < len; i++) {
                byte b;
                if (!Byte.TryParse(text.Substring(i * 2, 2),
                        NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out b)) {
                    return null;
                }
                buf[i] = b;
            }
            return buf;
        }

        private StreamReader GetStreamReader() {
            MemoryStream mem = new MemoryStream(keyFile, false);
            return new StreamReader(mem, Encoding.ASCII);
        }
    }


}
