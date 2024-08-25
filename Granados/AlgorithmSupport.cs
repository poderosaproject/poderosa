// Copyright (c) 2005-2017 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using System;
using System.Diagnostics;
using HMACSHA1 = System.Security.Cryptography.HMACSHA1;
using Granados.Crypto;
using Granados.Algorithms;
using System.Security.Cryptography;
using System.Text;

namespace Granados.Crypto {

    /// <summary>
    /// SSH Cipher interface
    /// </summary>
    internal interface Cipher {
        /// <summary>
        /// Encrypt plaintext.
        /// </summary>
        /// <param name="data">plaintext data buffer</param>
        /// <param name="offset">offset position on <paramref name="data"/> to read plaintext</param>
        /// <param name="length">plaintext length</param>
        /// <param name="result">output buffer</param>
        /// <param name="resultOffset">offset position on <paramref name="result"/> to write ciphertext</param>
        void Encrypt(byte[] data, int offset, int length, byte[] result, int resultOffset);

        /// <summary>
        /// Decrypt ciphertext.
        /// </summary>
        /// <param name="data">ciphertext data buffer</param>
        /// <param name="offset">offset position on <paramref name="data"/> to read ciphertext</param>
        /// <param name="length">ciphertext length</param>
        /// <param name="result">output buffer</param>
        /// <param name="resultOffset">offset position on <paramref name="result"/> to write plaintext</param>
        void Decrypt(byte[] data, int offset, int length, byte[] result, int resultOffset);

        /// <summary>
        /// Block size of the cipher algorithm (in bytes).
        /// </summary>
        int BlockSize {
            get;
        }
    }

    /// <summary>
    /// Creates a cipher from given parameters
    /// </summary>
    internal class CipherFactory {
        public static Cipher CreateCipher(SSHProtocol protocol, CipherAlgorithm algorithm, byte[] key) {
            if (protocol == SSHProtocol.SSH1) {
                switch (algorithm) {
                    case CipherAlgorithm.TripleDES:
                        return new SSH1.TripleDESCipher1(key);
                    case CipherAlgorithm.Blowfish:
                        return new SSH1.BlowfishCipher1(key);
                    default:
                        throw new SSHException("unknown algorithm " + algorithm);
                }
            }
            else {
                switch (algorithm) {
                    case CipherAlgorithm.TripleDES:
                        return new SSH2.TripleDESCipher2(key);
                    case CipherAlgorithm.Blowfish:
                        return new SSH2.BlowfishCipher2(key);
                    default:
                        throw new SSHException("unknown algorithm " + algorithm);
                }
            }
        }
        public static Cipher CreateCipher(SSHProtocol protocol, CipherAlgorithm algorithm, byte[] key, byte[] iv) {
            if (protocol == SSHProtocol.SSH1) { //ignoring iv
                return CreateCipher(protocol, algorithm, key);
            }
            else {
                switch (algorithm) {
                    case CipherAlgorithm.TripleDES:
                        return new SSH2.TripleDESCipher2(key, iv);
                    case CipherAlgorithm.Blowfish:
                        return new SSH2.BlowfishCipher2(key, iv);
                    case CipherAlgorithm.AES128:
                    case CipherAlgorithm.AES192:
                    case CipherAlgorithm.AES256:
                    case CipherAlgorithm.AES128CTR:
                    case CipherAlgorithm.AES192CTR:
                    case CipherAlgorithm.AES256CTR:
                        return new SSH2.AESCipher2(key, iv, algorithm);
                    case CipherAlgorithm.AES128GCM:
                    case CipherAlgorithm.AES256GCM:
                        return new SSH2.AESGCMCipher2(key, iv);
                    default:
                        throw new SSHException("unknown algorithm " + algorithm);
                }
            }
        }

        /// <summary>
        /// returns necessary key size from Algorithm in bytes
        /// </summary>
        public static int GetKeySize(CipherAlgorithm algorithm) {
            switch (algorithm) {
                case CipherAlgorithm.TripleDES:
                    return 24;
                case CipherAlgorithm.Blowfish:
                case CipherAlgorithm.AES128:
                case CipherAlgorithm.AES128CTR:
                case CipherAlgorithm.AES128GCM:
                    return 16;
                case CipherAlgorithm.AES192:
                case CipherAlgorithm.AES192CTR:
                    return 24;
                case CipherAlgorithm.AES256:
                case CipherAlgorithm.AES256CTR:
                case CipherAlgorithm.AES256GCM:
                    return 32;
                default:
                    throw new SSHException("unknown algorithm " + algorithm);
            }
        }

        /// <summary>
        /// returns the IV size from Algorithm in bytes
        /// </summary>
        public static int GetIVSize(CipherAlgorithm algorithm) {
            switch (algorithm) {
                case CipherAlgorithm.TripleDES:
                case CipherAlgorithm.Blowfish:
                    return 8;
                case CipherAlgorithm.AES128:
                case CipherAlgorithm.AES192:
                case CipherAlgorithm.AES256:
                case CipherAlgorithm.AES128CTR:
                case CipherAlgorithm.AES192CTR:
                case CipherAlgorithm.AES256CTR:
                    return 16;
                case CipherAlgorithm.AES128GCM:
                case CipherAlgorithm.AES256GCM:
                    return 12;
                default:
                    throw new SSHException("unknown algorithm " + algorithm);
            }
        }

        public static string[] AlgorithmToSSH2Name(CipherAlgorithm algorithm) {
            switch (algorithm) {
                case CipherAlgorithm.TripleDES:
                    return new string[] { "3des-cbc" };
                case CipherAlgorithm.Blowfish:
                    return new string[] { "blowfish-cbc" };
                case CipherAlgorithm.AES128:
                    return new string[] { "aes128-cbc" };
                case CipherAlgorithm.AES192:
                    return new string[] { "aes192-cbc" };
                case CipherAlgorithm.AES256:
                    return new string[] { "aes256-cbc" };
                case CipherAlgorithm.AES128CTR:
                    return new string[] { "aes128-ctr" };
                case CipherAlgorithm.AES192CTR:
                    return new string[] { "aes192-ctr" };
                case CipherAlgorithm.AES256CTR:
                    return new string[] { "aes256-ctr" };
                case CipherAlgorithm.AES128GCM:
                    // "aes128-gcm@openssh.com" is more preferred over AEAD_AES_128_GCM
                    // because it provides how to select correct MAC algorithm.
                    return new string[] { "aes128-gcm@openssh.com", "AEAD_AES_128_GCM" };
                case CipherAlgorithm.AES256GCM:
                    // "aes256-gcm@openssh.com" is more preferred over AEAD_AES_256_GCM
                    // because it provides how to select correct MAC algorithm.
                    return new string[] { "aes256-gcm@openssh.com", "AEAD_AES_256_GCM" };
                default:
                    throw new SSHException("unknown algorithm " + algorithm);
            }
        }
        public static CipherAlgorithm SSH2NameToAlgorithm(string name) {
            switch (name) {
                case "3des-cbc":
                    return CipherAlgorithm.TripleDES;
                case "blowfish-cbc":
                    return CipherAlgorithm.Blowfish;
                case "aes128-cbc":
                    return CipherAlgorithm.AES128;
                case "aes192-cbc":
                    return CipherAlgorithm.AES192;
                case "aes256-cbc":
                    return CipherAlgorithm.AES256;
                case "aes128-ctr":
                    return CipherAlgorithm.AES128CTR;
                case "aes192-ctr":
                    return CipherAlgorithm.AES192CTR;
                case "aes256-ctr":
                    return CipherAlgorithm.AES256CTR;
                case "AEAD_AES_128_GCM":
                case "aes128-gcm@openssh.com":
                    return CipherAlgorithm.AES128GCM;
                case "AEAD_AES_256_GCM":
                case "aes256-gcm@openssh.com":
                    return CipherAlgorithm.AES256GCM;
                default:
                    throw new SSHException("Unknown algorithm " + name);
            }
        }
    }



    /**********        MAC        ***********/

    /// <summary>
    /// MAC calculator
    /// </summary>
    interface MAC {
        byte[] ComputeHash(byte[] data, int offset, int length);
        int Size {
            get;
        }
    }

    /// <summary>
    /// Factory class creating a MAC calculator.
    /// </summary>
    internal class MACFactory {

        /// <summary>
        /// implementation class of the interface <see cref="MAC"/>.
        /// </summary>
        private class MACImpl : MAC {
            private readonly HMAC _algorithm;

            public MACImpl(HMAC algorithm) {
                _algorithm = algorithm;
            }

            public byte[] ComputeHash(byte[] data, int offset, int length) {
                _algorithm.Initialize();
                return _algorithm.ComputeHash(data, offset, length);
            }

            public int Size {
                get {
                    return _algorithm.HashSize / 8;
                }
            }
        }

        /// <summary>
        /// NULL implementation class of the interface <see cref="MAC"/>.
        /// This object is used as a fake MAC algorithm object if the cipher algorithm was AEAD_AES_128_GCM or AEAD_AES_256_GCM.
        /// </summary>
        private class MACNullImpl : MAC {
            private readonly int _size;

            public MACNullImpl(int size) {
                _size = size;
            }

            public byte[] ComputeHash(byte[] data, int offset, int length) {
                return new byte[_size];
            }

            public int Size {
                get {
                    return _size;
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="MAC"/> object.
        /// </summary>
        /// <param name="algorithm">MAC algorithm</param>
        /// <param name="key">key data</param>
        /// <returns>new MAC object.</returns>
        public static MAC CreateMAC(MACAlgorithm algorithm, byte[] key) {
            switch (algorithm) {
                case MACAlgorithm.HMACSHA1:
                    return new MACImpl(new HMACSHA1(key));
                case MACAlgorithm.HMACSHA256:
                    return new MACImpl(new HMACSHA256(key));
                case MACAlgorithm.HMACSHA512:
                    return new MACImpl(new HMACSHA512(key));
                case MACAlgorithm.AEAD_AES_128_GCM:
                case MACAlgorithm.AEAD_AES_256_GCM:
                    return new MACNullImpl(16);
                default:
                    throw new SSHException("unknown algorithm " + algorithm);
            }
        }

        /// <summary>
        /// Gets length of the MAC in bytes.
        /// </summary>
        /// <param name="algorithm">MAC algorithm</param>
        /// <returns>length of the MAC</returns>
        public static int GetSize(MACAlgorithm algorithm) {
            switch (algorithm) {
                case MACAlgorithm.HMACSHA1:
                    return 20;
                case MACAlgorithm.HMACSHA256:
                    return 32;
                case MACAlgorithm.HMACSHA512:
                    return 64;
                case MACAlgorithm.AEAD_AES_128_GCM:
                case MACAlgorithm.AEAD_AES_256_GCM:
                    return 16;
                default:
                    throw new SSHException("unknown algorithm " + algorithm);
            }
        }

        /// <summary>
        /// Gets name of a MAC algorithm for SSH2.
        /// </summary>
        /// <param name="algorithm">MAC algorithm</param>
        /// <returns></returns>
        public static string[] AlgorithmToSSH2Name(MACAlgorithm algorithm) {
            switch (algorithm) {
                case MACAlgorithm.HMACSHA1:
                    return new string[] { "hmac-sha1" };
                case MACAlgorithm.HMACSHA256:
                    return new string[] { "hmac-sha2-256" };
                case MACAlgorithm.HMACSHA512:
                    return new string[] { "hmac-sha2-512" };
                case MACAlgorithm.AEAD_AES_128_GCM:
                    return new string[] { "AEAD_AES_128_GCM" };
                case MACAlgorithm.AEAD_AES_256_GCM:
                    return new string[] { "AEAD_AES_256_GCM" };
                default:
                    throw new SSHException("unknown algorithm " + algorithm);
            }
        }
    }

    /// <summary>
    /// Partial implementation of bcrypt
    /// </summary>
    internal class Bcrypt {

        private static readonly byte[] _bcryptCipherText = Encoding.ASCII.GetBytes("OxychromaticBlowfishSwatDynamite");

        /// <summary>
        /// bcrypt_hash
        /// </summary>
        /// <param name="blowfish">blowfish object to use</param>
        /// <param name="sha2pass">SHA512 of password</param>
        /// <param name="sha2salt">SHA512 of salt</param>
        /// <returns></returns>
        private byte[] BcryptHash(Blowfish blowfish, byte[] sha2pass, byte[] sha2salt) {
            // this code is based on OpenBSD's bcrypt_pbkdf.c
            const int BLOCKSIZE = 8;

            // key expansion
            blowfish.InitializeState();
            blowfish.ExpandState(sha2pass, sha2salt);
            for (int i = 0; i < 64; ++i) {
                blowfish.ExpandState(sha2salt);
                blowfish.ExpandState(sha2pass);
            }

            // encryption
            byte[] cdata = (byte[])_bcryptCipherText.Clone();
            for (int i = 0; i < 64; ++i) {
                for (int j = 0; j < 32; j += BLOCKSIZE) {
                    blowfish.BlockEncrypt(cdata, j, cdata, j);
                }
            }

            // copy out
            for (int i = 0; i < 32; i += 4) {
                byte b0 = cdata[i + 0];
                byte b1 = cdata[i + 1];
                byte b2 = cdata[i + 2];
                byte b3 = cdata[i + 3];
                cdata[i + 3] = b0;
                cdata[i + 2] = b1;
                cdata[i + 1] = b2;
                cdata[i + 0] = b3;
            }

            return cdata;
        }

        /// <summary>
        /// bcrypt_pbkdf (pkcs #5 pbkdf2 implementation using the "bcrypt" hash)
        /// </summary>
        /// <param name="pass">password</param>
        /// <param name="salt">salt</param>
        /// <param name="rounds">rounds</param>
        /// <param name="keylen">key length</param>
        /// <returns>key</returns>
        public byte[] BcryptPbkdf(string pass, byte[] salt, uint rounds, int keylen) {
            // this code is based on OpenBSD's bcrypt_pbkdf.c

            if (rounds < 1) {
                return null;
            }
            if (pass.Length == 0 || salt.Length == 0 || keylen <= 0 || keylen > 1024) {
                return null;
            }

            byte[] key = new byte[keylen];

            int stride = (keylen + 32 - 1) / 32;
            int amt = (keylen + stride - 1) / stride;

            var blowfish = new Blowfish();
            using (var sha512 = new SHA512CryptoServiceProvider()) {

                // collapse password
                byte[] passData = Encoding.UTF8.GetBytes(pass);
                byte[] sha2pass = sha512.ComputeHash(passData);

                // generate key, sizeof(out) at a time
                byte[] countsalt = new byte[4];
                for (int count = 1; keylen > 0; ++count) {
                    countsalt[0] = (byte)(count >> 24);
                    countsalt[1] = (byte)(count >> 16);
                    countsalt[2] = (byte)(count >> 8);
                    countsalt[3] = (byte)(count);

                    // first round, salt is salt
                    sha512.Initialize();
                    sha512.TransformBlock(salt, 0, salt.Length, null, 0);
                    sha512.TransformFinalBlock(countsalt, 0, countsalt.Length);
                    byte[] sha2salt = sha512.Hash;
                    byte[] tmpout = BcryptHash(blowfish, sha2pass, sha2salt);
                    byte[] output = (byte[])tmpout.Clone();

                    for (uint r = rounds; r > 1; --r) {
                        // subsequent rounds, salt is previous output
                        sha512.Initialize();
                        sha2salt = sha512.ComputeHash(tmpout);
                        tmpout = BcryptHash(blowfish, sha2pass, sha2salt);
                        for (int i = 0; i < output.Length; ++i) {
                            output[i] ^= tmpout[i];
                        }
                    }

                    // pbkdf2 deviation: output the key material non-linearly.
                    amt = Math.Min(amt, keylen);
                    int k;
                    for (k = 0; k < amt; ++k) {
                        int dest = k * stride + (count - 1);
                        if (dest >= key.Length) {
                            break;
                        }
                        key[dest] = output[k];
                    }
                    keylen -= k;
                }
            }

            return key;
        }
    }
}

namespace Granados.Crypto.SSH1 {

    using Granados.Algorithms;

    /// <summary>
    /// Blowfish for SSH1
    /// </summary>
    internal class BlowfishCipher1 : Cipher {
        private Blowfish _bf;

        public BlowfishCipher1(byte[] key) {
            Debug.Assert(key.Length == 32);
            _bf = new Blowfish();
            _bf.InitializeKey(key);
        }
        public void Encrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _bf.EncryptSSH1Style(data, offset, len, result, ro);
        }
        public void Decrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _bf.DecryptSSH1Style(data, offset, len, result, ro);
        }
        public int BlockSize {
            get {
                return 8;
            }
        }
    }

    /// <summary>
    /// TripleDES for SSH1
    /// </summary>
    internal class TripleDESCipher1 : Cipher {
        private DES _DESCipher1;
        private DES _DESCipher2;
        private DES _DESCipher3;

        public TripleDESCipher1(byte[] key) {
            Debug.Assert(key.Length == 24);
            _DESCipher1 = new DES();
            _DESCipher2 = new DES();
            _DESCipher3 = new DES();

            _DESCipher1.InitializeKey(key, 0);
            _DESCipher2.InitializeKey(key, 8);
            _DESCipher3.InitializeKey(key, 16);
        }
        public void Encrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _DESCipher1.EncryptCBC(data, offset, len, result, ro);
            _DESCipher2.DecryptCBC(result, ro, len, result, ro);
            _DESCipher3.EncryptCBC(result, ro, len, result, ro);
        }
        public void Decrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _DESCipher3.DecryptCBC(data, offset, len, result, ro);
            _DESCipher2.EncryptCBC(result, ro, len, result, ro);
            _DESCipher1.DecryptCBC(result, ro, len, result, ro);
        }
        public int BlockSize {
            get {
                return 8;
            }
        }
    }

}

namespace Granados.Crypto.SSH2 {

    using Granados.Algorithms;

    /// <summary>
    /// Blowfish for SSH2
    /// </summary>
    internal class BlowfishCipher2 : Cipher {
        private Blowfish _bf;

        public BlowfishCipher2(byte[] key) {
            Debug.Assert(key.Length == 32);
            _bf = new Blowfish();
            _bf.InitializeKey(key);
        }
        public BlowfishCipher2(byte[] key, byte[] iv) {
            _bf = new Blowfish();
            _bf.SetIV(iv);
            _bf.InitializeKey(key);
        }
        public void Encrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _bf.EncryptCBC(data, offset, len, result, ro);
        }
        public void Decrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _bf.DecryptCBC(data, offset, len, result, ro);
        }
        public int BlockSize {
            get {
                return 8;
            }
        }
    }

    /// <summary>
    /// TripleDES for SSH2
    /// </summary>
    internal class TripleDESCipher2 : Cipher {
        private DES _DESCipher1;
        private DES _DESCipher2;
        private DES _DESCipher3;

        private byte[] _buffer;
        private byte[] _ivSave;

        public TripleDESCipher2(byte[] key) {
            Init(key);
        }
        public TripleDESCipher2(byte[] key, byte[] iv) {
            Init(key);
            _DESCipher1.SetIV(iv);
            _DESCipher2.SetIV(iv);
            _DESCipher3.SetIV(iv);

            _DESCipher1.InitializeKey(key, 0);
            _DESCipher2.InitializeKey(key, 8);
            _DESCipher3.InitializeKey(key, 16);
        }
        private void Init(byte[] key) {
            Debug.Assert(key.Length == 24);
            _buffer = new byte[8];
            _ivSave = new byte[8];
            _DESCipher1 = new DES();
            _DESCipher2 = new DES();
            _DESCipher3 = new DES();

            _DESCipher1.InitializeKey(key, 0);
            _DESCipher2.InitializeKey(key, 8);
            _DESCipher3.InitializeKey(key, 16);
        }

        public void Encrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            int n = 0;
            while (n < len) {
                _DESCipher1.EncryptCBC(data, offset + n, 8, result, ro + n);
                _DESCipher2.DecryptCBC(result, ro + n, 8, _buffer, 0);
                _DESCipher3.EncryptCBC(_buffer, 0, 8, result, ro + n);
                _DESCipher1.SetIV(result, ro + n);
                _DESCipher2.SetIV(result, ro + n);
                _DESCipher3.SetIV(result, ro + n);
                n += 8;
            }
        }
        public void Decrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            int n = 0;
            while (n < len) {
                Buffer.BlockCopy(data, offset + n, _ivSave, 0, 8);
                _DESCipher3.DecryptCBC(data, offset + n, 8, result, ro + n);
                _DESCipher2.EncryptCBC(result, ro + n, 8, _buffer, 0);
                _DESCipher1.DecryptCBC(_buffer, 0, 8, result, ro + n);
                _DESCipher3.SetIV(_ivSave);
                _DESCipher2.SetIV(_ivSave);
                _DESCipher1.SetIV(_ivSave);
                n += 8;
            }
        }
        public int BlockSize {
            get {
                return 8;
            }
        }
    }

    /// <summary>
    /// AES (SSH2 only)
    /// </summary>
    internal class AESCipher2 : Cipher {

        private readonly IAESBlockCipherMode _aes;

        public AESCipher2(byte[] key, byte[] iv, CipherAlgorithm algorithm) {
            if (algorithm == CipherAlgorithm.AES256CTR ||
                algorithm == CipherAlgorithm.AES192CTR ||
                algorithm == CipherAlgorithm.AES128CTR)
                _aes = new AESBlockCipherCTR(key, iv);
            else
                _aes = new AESBlockCipherCBC(key, iv);
        }
        public void Encrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _aes.Encrypt(data, offset, len, result, ro);
        }
        public void Decrypt(byte[] data, int offset, int len, byte[] result, int ro) {
            _aes.Decrypt(data, offset, len, result, ro);
        }
        public int BlockSize {
            get {
                return _aes.GetBlockSize();
            }
        }
    }

    /// <summary>
    /// AES-GCM (SSH2 only)
    /// </summary>
    internal class AESGCMCipher2 : Cipher {

        private readonly AESBlockCipherGCM _aesGcm;
        private readonly byte[] _iv = new byte[12];

        public AESGCMCipher2(byte[] key, byte[] iv) {
            _aesGcm = new AESBlockCipherGCM(key);
            Array.Copy(iv, 0, _iv, 0, _iv.Length);
        }
        public void Encrypt(byte[] data, int offset, int len, byte[] result, int resultOffset) {
            throw new NotImplementedException("Use EncryptGCM for AES-GCM");
        }
        public void Decrypt(byte[] data, int offset, int len, byte[] result, int resultOffset) {
            throw new NotImplementedException("Use DecryptGCM for AES-GCM");
        }
        public int BlockSize {
            get {
                return _aesGcm.GetBlockSize();
            }
        }

        public void EncryptGCM(byte[] input, int inputOffset, int inputLength, byte[] aad, int aadOffset, int aadLength, byte[] output, int outputOffset, byte[] outputTag, int outputTagOffset, int outputTagLength) {
            _aesGcm.SetIV(_iv);
            _aesGcm.Encrypt(
                input: input,
                inputOffset: inputOffset,
                inputLength: inputLength,
                aad: aad,
                aadOffset: aadOffset,
                aadLength: aadLength,
                output: output,
                outputOffset: outputOffset,
                outputTag: outputTag,
                outputTagOffset: outputTagOffset,
                outputTagLength: outputTagLength
            );
            IncrementIV();
        }

        public bool DecryptGCM(byte[] input, int inputOffset, int inputLength, byte[] aad, int aadOffset, int aadLength, byte[] tag, int tagOffset, int tagLength, byte[] output, int outputOffset) {
            _aesGcm.SetIV(_iv);
            bool result = _aesGcm.Decrypt(
                input: input,
                inputOffset: inputOffset,
                inputLength: inputLength,
                aad: aad,
                aadOffset: aadOffset,
                aadLength: aadLength,
                tag: tag,
                tagOffset: tagOffset,
                tagLength: tagLength,
                output: output,
                outputOffset: outputOffset
            );
            IncrementIV();
            return result;
        }

        private void IncrementIV() {
            // increment lower 64 bit
            if (++_iv[11] == 0) {
                if (++_iv[10] == 0) {
                    if (++_iv[9] == 0) {
                        if (++_iv[8] == 0) {
                            if (++_iv[7] == 0) {
                                if (++_iv[6] == 0) {
                                    if (++_iv[5] == 0) {
                                        ++_iv[4];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
