// Copyright (c) 2005-2017 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using System;
using Granados.Util;
using Granados.Crypto;

namespace Granados.Algorithms {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class Rijndael {
        private int[][] _Ke;			// encryption round keys
        private int[][] _Kd;			// decryption round keys
        private int _rounds;

        public Rijndael() {
        }

        public int GetBlockSize() {
            return BLOCK_SIZE;
        }

        ///////////////////////////////////////////////
        // set KEY
        ///////////////////////////////////////////////
        public void InitializeKey(byte[] key) {
            if (key == null)
                throw new ArgumentException("Empty key", "key");
            //128bit or 192bit or 256bit
            if (!(key.Length == 16 || key.Length == 24 || key.Length == 32))
                throw new ArgumentException("Invalid key length", "key");

            _rounds = getRounds(key.Length, GetBlockSize());
            _Ke = new int[_rounds + 1][];
            _Kd = new int[_rounds + 1][];
            int i, j;
            for (i = 0; i < _rounds + 1; i++) {
                _Ke[i] = new int[BC];
                _Kd[i] = new int[BC];
            }

            int ROUND_KEY_COUNT = (_rounds + 1) * BC;
            int KC = key.Length / 4;
            int[] tk = new int[KC];

            for (i = 0, j = 0; i < KC; ) {
                tk[i++] = (key[j++] & 0xFF) << 24 |
                          (key[j++] & 0xFF) << 16 |
                          (key[j++] & 0xFF) << 8 |
                          (key[j++] & 0xFF);
            }

            int t = 0;
            for (j = 0; (j < KC) && (t < ROUND_KEY_COUNT); j++, t++) {
                _Ke[t / BC][t % BC] = tk[j];
                _Kd[_rounds - (t / BC)][t % BC] = tk[j];
            }
            int tt, rconpointer = 0;
            while (t < ROUND_KEY_COUNT) {
                tt = tk[KC - 1];
                tk[0] ^= (S[(tt >> 16) & 0xFF] & 0xFF) << 24 ^
                         (S[(tt >> 8) & 0xFF] & 0xFF) << 16 ^
                         (S[tt & 0xFF] & 0xFF) << 8 ^
                         (S[(tt >> 24) & 0xFF] & 0xFF) ^
                         (rcon[rconpointer++] & 0xFF) << 24;

                if (KC != 8) {
                    for (i = 1, j = 0; i < KC; )
                        tk[i++] ^= tk[j++];
                }
                else {
                    for (i = 1, j = 0; i < KC / 2; )
                        tk[i++] ^= tk[j++];
                    tt = tk[KC / 2 - 1];
                    tk[KC / 2] ^= (S[tt & 0xFF] & 0xFF) ^
                                  (S[(tt >> 8) & 0xFF] & 0xFF) << 8 ^
                                  (S[(tt >> 16) & 0xFF] & 0xFF) << 16 ^
                                  (S[(tt >> 24) & 0xFF] & 0xFF) << 24;
                    for (j = KC / 2, i = j + 1; i < KC; )
                        tk[i++] ^= tk[j++];
                }
                for (j = 0; (j < KC) && (t < ROUND_KEY_COUNT); j++, t++) {
                    _Ke[t / BC][t % BC] = tk[j];
                    _Kd[_rounds - (t / BC)][t % BC] = tk[j];
                }
            }
            for (int r = 1; r < _rounds; r++) {
                for (j = 0; j < BC; j++) {
                    tt = _Kd[r][j];
                    _Kd[r][j] = U1[(tt >> 24) & 0xFF] ^
                               U2[(tt >> 16) & 0xFF] ^
                               U3[(tt >> 8) & 0xFF] ^
                               U4[tt & 0xFF];
                }
            }
        }

        public static int getRounds(int keySize, int blockSize) {
            switch (keySize) {
                case 16:
                    return blockSize == 16 ? 10 : (blockSize == 24 ? 12 : 14);
                case 24:
                    return blockSize != 32 ? 12 : 14;
                default:
                    return 14;
            }
        }

        public void blockEncrypt(byte[] src, int inOffset, byte[] dst, int outOffset) {
            int[] Ker = _Ke[0];

            int t0 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Ker[0];
            int t1 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Ker[1];
            int t2 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Ker[2];
            int t3 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Ker[3];

            int a0, a1, a2, a3;
            for (int r = 1; r < _rounds; r++) {
                Ker = _Ke[r];
                a0 = (T1[(t0 >> 24) & 0xFF] ^
                      T2[(t1 >> 16) & 0xFF] ^
                      T3[(t2 >> 8) & 0xFF] ^
                      T4[t3 & 0xFF]) ^ Ker[0];
                a1 = (T1[(t1 >> 24) & 0xFF] ^
                      T2[(t2 >> 16) & 0xFF] ^
                      T3[(t3 >> 8) & 0xFF] ^
                      T4[t0 & 0xFF]) ^ Ker[1];
                a2 = (T1[(t2 >> 24) & 0xFF] ^
                      T2[(t3 >> 16) & 0xFF] ^
                      T3[(t0 >> 8) & 0xFF] ^
                      T4[t1 & 0xFF]) ^ Ker[2];
                a3 = (T1[(t3 >> 24) & 0xFF] ^
                      T2[(t0 >> 16) & 0xFF] ^
                      T3[(t1 >> 8) & 0xFF] ^
                      T4[t2 & 0xFF]) ^ Ker[3];
                t0 = a0;
                t1 = a1;
                t2 = a2;
                t3 = a3;
            }

            Ker = _Ke[_rounds];
            int tt = Ker[0];
            dst[outOffset + 0] = (byte)(S[(t0 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 1] = (byte)(S[(t1 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 2] = (byte)(S[(t2 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 3] = (byte)(S[t3 & 0xFF] ^ tt);
            tt = Ker[1];
            dst[outOffset + 4] = (byte)(S[(t1 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 5] = (byte)(S[(t2 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 6] = (byte)(S[(t3 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 7] = (byte)(S[t0 & 0xFF] ^ tt);
            tt = Ker[2];
            dst[outOffset + 8] = (byte)(S[(t2 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 9] = (byte)(S[(t3 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 10] = (byte)(S[(t0 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 11] = (byte)(S[t1 & 0xFF] ^ tt);
            tt = Ker[3];
            dst[outOffset + 12] = (byte)(S[(t3 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 13] = (byte)(S[(t0 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 14] = (byte)(S[(t1 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 15] = (byte)(S[t2 & 0xFF] ^ tt);
        }

        public void blockDecrypt(byte[] src, int inOffset, byte[] dst, int outOffset) {
            int[] Kdr = _Kd[0];

            int t0 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Kdr[0];
            int t1 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Kdr[1];
            int t2 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Kdr[2];
            int t3 = ((src[inOffset++] & 0xFF) << 24 |
                      (src[inOffset++] & 0xFF) << 16 |
                      (src[inOffset++] & 0xFF) << 8 |
                      (src[inOffset++] & 0xFF)) ^ Kdr[3];

            int a0, a1, a2, a3;
            for (int r = 1; r < _rounds; r++) {
                Kdr = _Kd[r];
                a0 = (T5[(t0 >> 24) & 0xFF] ^
                      T6[(t3 >> 16) & 0xFF] ^
                      T7[(t2 >> 8) & 0xFF] ^
                      T8[t1 & 0xFF]) ^ Kdr[0];
                a1 = (T5[(t1 >> 24) & 0xFF] ^
                      T6[(t0 >> 16) & 0xFF] ^
                      T7[(t3 >> 8) & 0xFF] ^
                      T8[t2 & 0xFF]) ^ Kdr[1];
                a2 = (T5[(t2 >> 24) & 0xFF] ^
                      T6[(t1 >> 16) & 0xFF] ^
                      T7[(t0 >> 8) & 0xFF] ^
                      T8[t3 & 0xFF]) ^ Kdr[2];
                a3 = (T5[(t3 >> 24) & 0xFF] ^
                      T6[(t2 >> 16) & 0xFF] ^
                      T7[(t1 >> 8) & 0xFF] ^
                      T8[t0 & 0xFF]) ^ Kdr[3];
                t0 = a0;
                t1 = a1;
                t2 = a2;
                t3 = a3;
            }

            Kdr = _Kd[_rounds];
            int tt = Kdr[0];
            dst[outOffset + 0] = (byte)(Si[(t0 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 1] = (byte)(Si[(t3 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 2] = (byte)(Si[(t2 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 3] = (byte)(Si[t1 & 0xFF] ^ tt);
            tt = Kdr[1];
            dst[outOffset + 4] = (byte)(Si[(t1 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 5] = (byte)(Si[(t0 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 6] = (byte)(Si[(t3 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 7] = (byte)(Si[t2 & 0xFF] ^ tt);
            tt = Kdr[2];
            dst[outOffset + 8] = (byte)(Si[(t2 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 9] = (byte)(Si[(t1 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 10] = (byte)(Si[(t0 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 11] = (byte)(Si[t3 & 0xFF] ^ tt);
            tt = Kdr[3];
            dst[outOffset + 12] = (byte)(Si[(t3 >> 24) & 0xFF] ^ (tt >> 24));
            dst[outOffset + 13] = (byte)(Si[(t2 >> 16) & 0xFF] ^ (tt >> 16));
            dst[outOffset + 14] = (byte)(Si[(t1 >> 8) & 0xFF] ^ (tt >> 8));
            dst[outOffset + 15] = (byte)(Si[t0 & 0xFF] ^ tt);
        }

        /// <summary>
        /// constants
        /// </summary>
        private const int BLOCK_SIZE = 16;
        private const int BC = 4;

        private static readonly int[] alog = new int[256];
        private static readonly int[] log = new int[256];

        private static readonly byte[] S = new byte[256];
        private static readonly byte[] Si = new byte[256];
        private static readonly int[] T1 = new int[256];
        private static readonly int[] T2 = new int[256];
        private static readonly int[] T3 = new int[256];
        private static readonly int[] T4 = new int[256];
        private static readonly int[] T5 = new int[256];
        private static readonly int[] T6 = new int[256];
        private static readonly int[] T7 = new int[256];
        private static readonly int[] T8 = new int[256];
        private static readonly int[] U1 = new int[256];
        private static readonly int[] U2 = new int[256];
        private static readonly int[] U3 = new int[256];
        private static readonly int[] U4 = new int[256];
        private static readonly byte[] rcon = new byte[30];

        private static readonly int[, ,] shifts = new int[,,] {
            { {0, 0}, {1, 3}, {2, 2}, {3, 1} },
            { {0, 0}, {1, 5}, {2, 4}, {3, 3} },
            { {0, 0}, {1, 7}, {3, 5}, {4, 4} }};

        ///////////////////////////////////
        //class initialization
        ///////////////////////////////////
        static Rijndael() {
            int ROOT = 0x11B;
            int i, j = 0;

            alog[0] = 1;
            for (i = 1; i < 256; i++) {
                j = (alog[i - 1] << 1) ^ alog[i - 1];
                if ((j & 0x100) != 0)
                    j ^= ROOT;
                alog[i] = j;
            }
            for (i = 1; i < 255; i++)
                log[alog[i]] = i;
            byte[,] A = new byte[,] {
                {1, 1, 1, 1, 1, 0, 0, 0},
                {0, 1, 1, 1, 1, 1, 0, 0},
                {0, 0, 1, 1, 1, 1, 1, 0},
                {0, 0, 0, 1, 1, 1, 1, 1},
                {1, 0, 0, 0, 1, 1, 1, 1},
                {1, 1, 0, 0, 0, 1, 1, 1},
                {1, 1, 1, 0, 0, 0, 1, 1},
                {1, 1, 1, 1, 0, 0, 0, 1}};
            byte[] B = new byte[] { 0, 1, 1, 0, 0, 0, 1, 1 };

            int t;
            byte[,] box = new byte[256, 8];
            box[1, 7] = 1;
            for (i = 2; i < 256; i++) {
                j = alog[255 - log[i]];
                for (t = 0; t < 8; t++) {
                    box[i, t] = (byte)((j >> (7 - t)) & 0x01);
                }
            }

            byte[,] cox = new byte[256, 8];
            for (i = 0; i < 256; i++) {
                for (t = 0; t < 8; t++) {
                    cox[i, t] = B[t];
                    for (j = 0; j < 8; j++)
                        cox[i, t] ^= (byte)(A[t, j] * box[i, j]);
                }
            }

            for (i = 0; i < 256; i++) {
                S[i] = (byte)(cox[i, 0] << 7);
                for (t = 1; t < 8; t++)
                    S[i] ^= (byte)(cox[i, t] << (7 - t));
                Si[S[i] & 0xFF] = (byte)i;
            }
            byte[][] G = new byte[4][];
            G[0] = new byte[] { 2, 1, 1, 3 };
            G[1] = new byte[] { 3, 2, 1, 1 };
            G[2] = new byte[] { 1, 3, 2, 1 };
            G[3] = new byte[] { 1, 1, 3, 2 };

            byte[,] AA = new byte[4, 8];
            for (i = 0; i < 4; i++) {
                for (j = 0; j < 4; j++)
                    AA[i, j] = G[i][j];
                AA[i, i + 4] = 1;
            }
            byte pivot, tmp;
            byte[][] iG = new byte[4][];
            for (i = 0; i < 4; i++)
                iG[i] = new byte[4];

            for (i = 0; i < 4; i++) {
                pivot = AA[i, i];
                if (pivot == 0) {
                    t = i + 1;
                    while ((AA[t, i] == 0) && (t < 4))
                        t++;
                    if (t != 4) {
                        for (j = 0; j < 8; j++) {
                            tmp = AA[i, j];
                            AA[i, j] = AA[t, j];
                            AA[t, j] = (byte)tmp;
                        }
                        pivot = AA[i, i];
                    }
                }
                for (j = 0; j < 8; j++)
                    if (AA[i, j] != 0)
                        AA[i, j] = (byte)
                            alog[(255 + log[AA[i, j] & 0xFF] - log[pivot & 0xFF]) % 255];
                for (t = 0; t < 4; t++)
                    if (i != t) {
                        for (j = i + 1; j < 8; j++)
                            AA[t, j] ^= (byte)mul(AA[i, j], AA[t, i]);
                        AA[t, i] = 0;
                    }
            }

            for (i = 0; i < 4; i++)
                for (j = 0; j < 4; j++)
                    iG[i][j] = AA[i, j + 4];

            int s;
            for (t = 0; t < 256; t++) {
                s = S[t];
                T1[t] = mul4(s, G[0]);
                T2[t] = mul4(s, G[1]);
                T3[t] = mul4(s, G[2]);
                T4[t] = mul4(s, G[3]);

                s = Si[t];
                T5[t] = mul4(s, iG[0]);
                T6[t] = mul4(s, iG[1]);
                T7[t] = mul4(s, iG[2]);
                T8[t] = mul4(s, iG[3]);

                U1[t] = mul4(t, iG[0]);
                U2[t] = mul4(t, iG[1]);
                U3[t] = mul4(t, iG[2]);
                U4[t] = mul4(t, iG[3]);
            }

            rcon[0] = 1;
            int r = 1;
            for (t = 1; t < 30; )
                rcon[t++] = (byte)(r = mul(2, r));
        }

        private static int mul(int a, int b) {
            return (a != 0 && b != 0) ?
                    alog[(log[a & 0xFF] + log[b & 0xFF]) % 255] :
                    0;
        }

        private static int mul4(int a, byte[] b) {
            if (a == 0)
                return 0;
            a = log[a & 0xFF];
            int a0 = (b[0] != 0) ? alog[(a + log[b[0] & 0xFF]) % 255] & 0xFF : 0;
            int a1 = (b[1] != 0) ? alog[(a + log[b[1] & 0xFF]) % 255] & 0xFF : 0;
            int a2 = (b[2] != 0) ? alog[(a + log[b[2] & 0xFF]) % 255] & 0xFF : 0;
            int a3 = (b[3] != 0) ? alog[(a + log[b[3] & 0xFF]) % 255] & 0xFF : 0;
            return a0 << 24 | a1 << 16 | a2 << 8 | a3;
        }
    }

    /// <summary>
    /// AES Block Cipher Mode interface
    /// </summary>
    public interface IAESBlockCipherMode {
        void Encrypt(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset);
        void Decrypt(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset);
        int GetBlockSize();
    }

    /// <summary>
    /// AES CBC mode encryption / decryption
    /// </summary>
    public class AESBlockCipherCBC : Rijndael, IAESBlockCipherMode {

        private readonly byte[] _chainBlock;
        private readonly byte[] _tmpBlock;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">AES key (128 / 192 / 256 bit)</param>
        /// <param name="iv">initial vector. if omitted, zero-filled block is used.</param>
        public AESBlockCipherCBC(byte[] key, byte[] iv = null) {
            if (iv != null && iv.Length != GetBlockSize()) {
                throw new ArgumentException("Invald IV size", "iv");
            }

            _chainBlock = new byte[GetBlockSize()];
            if (iv != null) {
                Array.Copy(iv, _chainBlock, iv.Length);
            }

            _tmpBlock = new byte[GetBlockSize()];

            InitializeKey(key);
        }

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="input">input byte array</param>
        /// <param name="inputOffset">input offset</param>
        /// <param name="inputLen">input length</param>
        /// <param name="output">output byte array. this can be the same array as <paramref name="input"/>.</param>
        /// <param name="outputOffset">output offset</param>
        public void Encrypt(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset) {
            int blockSize = GetBlockSize();
            int nBlocks = inputLen / blockSize;
            for (int bc = 0; bc < nBlocks; bc++) {
                CipherUtil.BlockXor2(input, inputOffset, _chainBlock, 0, blockSize, _tmpBlock, 0);
                blockEncrypt(_tmpBlock, 0, _chainBlock, 0);
                Array.Copy(_chainBlock, 0, output, outputOffset, blockSize);
                inputOffset += blockSize;
                outputOffset += blockSize;
            }
        }

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="input">input byte array</param>
        /// <param name="inputOffset">input offset</param>
        /// <param name="inputLen">input length</param>
        /// <param name="output">output byte array. this can be the same array as <paramref name="input"/>.</param>
        /// <param name="outputOffset">output offset</param>
        public void Decrypt(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset) {
            int blockSize = GetBlockSize();
            int nBlocks = inputLen / blockSize;
            for (int bc = 0; bc < nBlocks; bc++) {
                blockDecrypt(input, inputOffset, _tmpBlock, 0);
                CipherUtil.BlockXor(_chainBlock, 0, blockSize, _tmpBlock, 0);
                Array.Copy(input, inputOffset, _chainBlock, 0, blockSize);
                Array.Copy(_tmpBlock, 0, output, outputOffset, blockSize);
                inputOffset += blockSize;
                outputOffset += blockSize;
            }
        }
    }

    /// <summary>
    /// AES CTR mode encryption / decryption
    /// </summary>
    public class AESBlockCipherCTR : Rijndael, IAESBlockCipherMode {

        private readonly byte[] _counterBlock;
        private readonly byte[] _tmpBlock;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">AES key (128 / 192 / 256 bit)</param>
        /// <param name="icb">initial counter block</param>
        public AESBlockCipherCTR(byte[] key, byte[] icb) {
            if (icb.Length != GetBlockSize()) {
                throw new ArgumentException("Invald ICB size", "icb");
            }

            _counterBlock = new byte[GetBlockSize()];
            Array.Copy(icb, _counterBlock, icb.Length);

            _tmpBlock = new byte[GetBlockSize()];

            InitializeKey(key);
        }

        /// <summary>
        /// Encrypt
        /// </summary>
        /// <param name="input">input byte array</param>
        /// <param name="inputOffset">input offset</param>
        /// <param name="inputLen">input length</param>
        /// <param name="output">output byte array. this can be the same array as <paramref name="input"/>.</param>
        /// <param name="outputOffset">output offset</param>
        public void Encrypt(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset) {
            int blockSize = GetBlockSize();
            int nBlocks = inputLen / blockSize;
            for (int bc = 0; bc < nBlocks; bc++) {
                blockEncrypt(_counterBlock, 0, _tmpBlock, 0);
                CipherUtil.BlockXor2(input, inputOffset, _tmpBlock, 0, blockSize, output, outputOffset);
                IncrementCounterBlock();
                inputOffset += blockSize;
                outputOffset += blockSize;
            }
        }

        /// <summary>
        /// Decrypt
        /// </summary>
        /// <param name="input">input byte array</param>
        /// <param name="inputOffset">input offset</param>
        /// <param name="inputLen">input length</param>
        /// <param name="output">output byte array. this can be the same array as <paramref name="input"/>.</param>
        /// <param name="outputOffset">output offset</param>
        public void Decrypt(byte[] input, int inputOffset, int inputLen, byte[] output, int outputOffset) {
            Encrypt(input, inputOffset, inputLen, output, outputOffset);
        }

        internal void IncrementCounterBlock() {
            for (int i = _counterBlock.Length - 1; i >= 0; i--) {
                if (++_counterBlock[i] != 0) {
                    return;
                }
            }
        }

        // for testing
        internal byte[] CopyCounterBlock() {
            return (byte[])_counterBlock.Clone();
        }
    }


#if DEBUG
    // Test
    internal static class RijndaelTest {
        // Test vectors from NIST 800-38A
        // Recommendation for Block Cipher Modes of Operation: Methods and Techniques

        private static readonly byte[] original =
            BigIntegerConverter.ParseHex(
              "6bc1bee22e409f96e93d7e117393172a"
            + "ae2d8a571e03ac9c9eb76fac45af8e51"
            + "30c81c46a35ce411e5fbc1191a0a52ef"
            + "f69f2445df4f9b17ad2b417be66c3710");

        internal static void Test() {
            Test_AES128_ECB_Encrypt();
            Test_AES128_ECB_Decrypt();
            Test_AES192_ECB_Encrypt();
            Test_AES192_ECB_Decrypt();
            Test_AES256_ECB_Encrypt();
            Test_AES256_ECB_Decrypt();

            Test_AES128_CBC_Encrypt();
            Test_AES128_CBC_Decrypt();
            Test_AES192_CBC_Encrypt();
            Test_AES192_CBC_Decrypt();
            Test_AES256_CBC_Encrypt();
            Test_AES256_CBC_Decrypt();

            Test_AES_CTR_IncrementCounterBlock();

            Test_AES128_CTR_Encrypt();
            Test_AES128_CTR_Decrypt();
            Test_AES192_CTR_Encrypt();
            Test_AES192_CTR_Decrypt();
            Test_AES256_CTR_Encrypt();
            Test_AES256_CTR_Decrypt();
        }

        #region ECB

        private static void test_AES_ECB_Encrypt(byte[] key, byte[] input, byte[] expected, [System.Runtime.CompilerServices.CallerMemberName] string testName = "") {
            byte[] inputOrig = (byte[])input.Clone();
            byte[] output = new byte[input.Length];

            var aes = new Rijndael();
            aes.InitializeKey(key);
            int offset = 0;
            while (offset < input.Length) {
                aes.blockEncrypt(input, offset, output, offset);
                offset += aes.GetBlockSize();
            }

            if (!System.Linq.Enumerable.SequenceEqual(output, expected)) {
                throw new Exception(String.Format("{0}: Encrypt failed: wrong output", testName));
            }
            if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                throw new Exception(String.Format("{0}: Encrypt failed: input data were corrupted", testName));
            }
        }

        private static void test_AES_ECB_Decrypt(byte[] key, byte[] input, byte[] expected, [System.Runtime.CompilerServices.CallerMemberName] string testName = "") {
            byte[] inputOrig = (byte[])input.Clone();
            byte[] output = new byte[input.Length];

            var aes = new Rijndael();
            aes.InitializeKey(key);
            int offset = 0;
            while (offset < input.Length) {
                aes.blockDecrypt(input, offset, output, offset);
                offset += aes.GetBlockSize();
            }

            if (!System.Linq.Enumerable.SequenceEqual(output, expected)) {
                throw new Exception(String.Format("{0}: Decrypt failed: wrong output", testName));
            }
            if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                throw new Exception(String.Format("{0}: Decrypt failed: input data were corrupted", testName));
            }
        }

        private static void Test_AES128_ECB_Encrypt() {
            test_AES_ECB_Encrypt(
                key: BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "3ad77bb40d7a3660a89ecaf32466ef97"
                    + "f5d3d58503b9699de785895a96fdbaaf"
                    + "43b1cd7f598ece23881b00e3ed030688"
                    + "7b0c785e27e8ad3f8223207104725dd4"
                )
            );
        }

        private static void Test_AES128_ECB_Decrypt() {
            test_AES_ECB_Decrypt(
                key: BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                input: BigIntegerConverter.ParseHex(
                      "3ad77bb40d7a3660a89ecaf32466ef97"
                    + "f5d3d58503b9699de785895a96fdbaaf"
                    + "43b1cd7f598ece23881b00e3ed030688"
                    + "7b0c785e27e8ad3f8223207104725dd4"
                ),
                expected: (byte[])original.Clone()
            );
        }

        private static void Test_AES192_ECB_Encrypt() {
            test_AES_ECB_Encrypt(
                key: BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "bd334f1d6e45f25ff712a214571fa5cc"
                    + "974104846d0ad3ad7734ecb3ecee4eef"
                    + "ef7afd2270e2e60adce0ba2face6444e"
                    + "9a4b41ba738d6c72fb16691603c18e0e"
                )
            );
        }

        private static void Test_AES192_ECB_Decrypt() {
            test_AES_ECB_Decrypt(
                key: BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                input: BigIntegerConverter.ParseHex(
                      "bd334f1d6e45f25ff712a214571fa5cc"
                    + "974104846d0ad3ad7734ecb3ecee4eef"
                    + "ef7afd2270e2e60adce0ba2face6444e"
                    + "9a4b41ba738d6c72fb16691603c18e0e"
                ),
                expected: (byte[])original.Clone()
            );
        }

        private static void Test_AES256_ECB_Encrypt() {
            test_AES_ECB_Encrypt(
                key: BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "f3eed1bdb5d2a03c064b5a7e3db181f8"
                    + "591ccb10d410ed26dc5ba74a31362870"
                    + "b6ed21b99ca6f4f9f153e7b1beafed1d"
                    + "23304b7a39f9f3ff067d8d8f9e24ecc7"
                )
            );
        }

        private static void Test_AES256_ECB_Decrypt() {
            test_AES_ECB_Decrypt(
                key: BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                input: BigIntegerConverter.ParseHex(
                      "f3eed1bdb5d2a03c064b5a7e3db181f8"
                    + "591ccb10d410ed26dc5ba74a31362870"
                    + "b6ed21b99ca6f4f9f153e7b1beafed1d"
                    + "23304b7a39f9f3ff067d8d8f9e24ecc7"
                ),
                expected: (byte[])original.Clone()
            );
        }
        #endregion

        #region CBC

        private static void test_AES_CBC_Encrypt(byte[] key, byte[] iv, byte[] input, byte[] expected, [System.Runtime.CompilerServices.CallerMemberName] string testName = "") {
            byte[] inputOrig = (byte[])input.Clone();

            // encrypt all blocks
            {
                byte[] output = new byte[input.Length];

                var aes = new AESBlockCipherCBC(key, iv);
                aes.Encrypt(input, 0, input.Length, output, 0);

                if (!System.Linq.Enumerable.SequenceEqual(output, expected)) {
                    throw new Exception(String.Format("{0} Encrypt failed: wrong output", testName));
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Encrypt failed: input data were corrupted", testName));
                }
            }

            // encrypt each blocks
            {

                var aes = new AESBlockCipherCBC(key, iv);
                int blockSize = aes.GetBlockSize();
                byte[] outputBlock = new byte[blockSize];
                byte[] expectedBlock = new byte[blockSize];
                for (int inputOffset = 0; inputOffset < input.Length; inputOffset += blockSize) {
                    aes.Encrypt(input, inputOffset, blockSize, outputBlock, 0);

                    Array.Copy(expected, inputOffset, expectedBlock, 0, blockSize);

                    if (!System.Linq.Enumerable.SequenceEqual(outputBlock, expectedBlock)) {
                        throw new Exception(String.Format("{0} Encrypt failed (inputOffset = {1}): wrong output", testName, inputOffset));
                    }
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Encrypt failed: input data were corrupted", testName));
                }
            }

            // in-place encrypt all blocks
            {
                var aes = new AESBlockCipherCBC(key, iv);
                aes.Encrypt(input, 0, input.Length, input, 0);

                if (!System.Linq.Enumerable.SequenceEqual(input, expected)) {
                    throw new Exception(String.Format("{0} Encrypt In-Place failed: wrong output", testName));
                }
            }
        }

        private static void test_AES_CBC_Decrypt(byte[] key, byte[] iv, byte[] input, byte[] expected, [System.Runtime.CompilerServices.CallerMemberName] string testName = "") {
            byte[] inputOrig = (byte[])input.Clone();

            // decrypt all blocks
            {
                byte[] output = new byte[input.Length];

                var aes = new AESBlockCipherCBC(key, iv);
                aes.Decrypt(input, 0, input.Length, output, 0);

                if (!System.Linq.Enumerable.SequenceEqual(output, expected)) {
                    throw new Exception(String.Format("{0} Decrypt failed: wrong output", testName));
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Decrypt failed: input data were corrupted", testName));
                }
            }

            // decrypt each blocks
            {
                var aes = new AESBlockCipherCBC(key, iv);
                int blockSize = aes.GetBlockSize();
                byte[] outputBlock = new byte[blockSize];
                byte[] expectedBlock = new byte[blockSize];
                for (int inputOffset = 0; inputOffset < input.Length; inputOffset += blockSize) {
                    aes.Decrypt(input, inputOffset, blockSize, outputBlock, 0);

                    Array.Copy(expected, inputOffset, expectedBlock, 0, blockSize);

                    if (!System.Linq.Enumerable.SequenceEqual(outputBlock, expectedBlock)) {
                        throw new Exception(String.Format("{0} Decrypt failed (inputOffset = {1}): wrong output", testName, inputOffset));
                    }
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Encrypt failed: input data were corrupted", testName));
                }
            }

            // in-place decrypt all blocks
            {
                var aes = new AESBlockCipherCBC(key, iv);
                aes.Decrypt(input, 0, input.Length, input, 0);

                if (!System.Linq.Enumerable.SequenceEqual(input, expected)) {
                    throw new Exception(String.Format("{0} Decrypt In-Place failed: wrong output", testName));
                }
            }
        }

        private static void Test_AES128_CBC_Encrypt() {
            test_AES_CBC_Encrypt(
                key: BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                iv: BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "7649abac8119b246cee98e9b12e9197d"
                    + "5086cb9b507219ee95db113a917678b2"
                    + "73bed6b8e3c1743b7116e69e22229516"
                    + "3ff1caa1681fac09120eca307586e1a7"
                )
            );
        }

        private static void Test_AES128_CBC_Decrypt() {
            test_AES_CBC_Decrypt(
                key: BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                iv: BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                input: BigIntegerConverter.ParseHex(
                      "7649abac8119b246cee98e9b12e9197d"
                    + "5086cb9b507219ee95db113a917678b2"
                    + "73bed6b8e3c1743b7116e69e22229516"
                    + "3ff1caa1681fac09120eca307586e1a7"
                ),
                expected: (byte[])original.Clone()
            );
        }

        private static void Test_AES192_CBC_Encrypt() {
            test_AES_CBC_Encrypt(
                key: BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                iv: BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "4f021db243bc633d7178183a9fa071e8"
                    + "b4d9ada9ad7dedf4e5e738763f69145a"
                    + "571b242012fb7ae07fa9baac3df102e0"
                    + "08b0e27988598881d920a9e64f5615cd"
                )
            );
        }

        private static void Test_AES192_CBC_Decrypt() {
            test_AES_CBC_Decrypt(
                key: BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                iv: BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                input: BigIntegerConverter.ParseHex(
                      "4f021db243bc633d7178183a9fa071e8"
                    + "b4d9ada9ad7dedf4e5e738763f69145a"
                    + "571b242012fb7ae07fa9baac3df102e0"
                    + "08b0e27988598881d920a9e64f5615cd"
                ),
                expected: (byte[])original.Clone()
            );
        }

        private static void Test_AES256_CBC_Encrypt() {
            test_AES_CBC_Encrypt(
                key: BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                iv: BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "f58c4c04d6e5f1ba779eabfb5f7bfbd6"
                    + "9cfc4e967edb808d679f777bc6702c7d"
                    + "39f23369a9d9bacfa530e26304231461"
                    + "b2eb05e2c39be9fcda6c19078c6a9d1b"
                )
            );
        }

        private static void Test_AES256_CBC_Decrypt() {
            test_AES_CBC_Decrypt(
                key: BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                iv: BigIntegerConverter.ParseHex("000102030405060708090a0b0c0d0e0f"),
                input: BigIntegerConverter.ParseHex(
                      "f58c4c04d6e5f1ba779eabfb5f7bfbd6"
                    + "9cfc4e967edb808d679f777bc6702c7d"
                    + "39f23369a9d9bacfa530e26304231461"
                    + "b2eb05e2c39be9fcda6c19078c6a9d1b"
                ),
                expected: (byte[])original.Clone()
            );
        }
        #endregion

        #region CTR

        private static void Test_AES_CTR_IncrementCounterBlock() {
            Tuple<string, string>[] patterns =
            {
                Tuple.Create("00000000000000000000000000000000", "00000000000000000000000000000001"),
                Tuple.Create("000000000000000000000000000000ff", "00000000000000000000000000000100"),
                Tuple.Create("00000000000000000000000000000100", "00000000000000000000000000000101"),
                Tuple.Create("00ffffffffffffffffffffffffffffff", "01000000000000000000000000000000"),
                Tuple.Create("01000000000000000000000000000000", "01000000000000000000000000000001"),
                Tuple.Create("ffffffffffffffffffffffffffffffff", "00000000000000000000000000000000"),
            };

            foreach (var p in patterns) {
                byte[] icb = BigIntegerConverter.ParseHex(p.Item1);
                byte[] expected = BigIntegerConverter.ParseHex(p.Item2);
                var aes = new AESBlockCipherCTR(BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"), icb);
                if (!System.Linq.Enumerable.SequenceEqual(aes.CopyCounterBlock(), icb)) {
                    throw new Exception("icb does not match");
                }
                aes.IncrementCounterBlock();
                if (!System.Linq.Enumerable.SequenceEqual(aes.CopyCounterBlock(), expected)) {
                    throw new Exception("incremented counter block has wrong value");
                }
            }
        }

        private static void test_AES_CTR_Encrypt(byte[] key, byte[] icb, byte[] input, byte[] expected, [System.Runtime.CompilerServices.CallerMemberName] string testName = "") {
            byte[] inputOrig = (byte[])input.Clone();

            // encrypt all blocks
            {
                byte[] output = new byte[input.Length];

                var aes = new AESBlockCipherCTR(key, icb);
                aes.Encrypt(input, 0, input.Length, output, 0);

                if (!System.Linq.Enumerable.SequenceEqual(output, expected)) {
                    throw new Exception(String.Format("{0} Encrypt failed: wrong output", testName));
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Encrypt failed: input data were corrupted", testName));
                }
            }

            // encrypt each blocks
            {
                var aes = new AESBlockCipherCTR(key, icb);
                int blockSize = aes.GetBlockSize();
                byte[] outputBlock = new byte[blockSize];
                byte[] expectedBlock = new byte[blockSize];
                for (int inputOffset = 0; inputOffset < input.Length; inputOffset += blockSize) {
                    aes.Encrypt(input, inputOffset, blockSize, outputBlock, 0);

                    Array.Copy(expected, inputOffset, expectedBlock, 0, blockSize);

                    if (!System.Linq.Enumerable.SequenceEqual(outputBlock, expectedBlock)) {
                        throw new Exception(String.Format("{0} Encrypt failed (inputOffset = {1}): wrong output", testName, inputOffset));
                    }
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Encrypt failed: input data were corrupted", testName));
                }
            }

            // in-place encrypt all blocks
            {
                var aes = new AESBlockCipherCTR(key, icb);
                aes.Encrypt(input, 0, input.Length, input, 0);

                if (!System.Linq.Enumerable.SequenceEqual(input, expected)) {
                    throw new Exception(String.Format("{0} Encrypt In-Place failed: wrong output", testName));
                }
            }
        }

        private static void test_AES_CTR_Decrypt(byte[] key, byte[] icb, byte[] input, byte[] expected, [System.Runtime.CompilerServices.CallerMemberName] string testName = "") {
            byte[] inputOrig = (byte[])input.Clone();

            // decrypt all blocks
            {
                byte[] output = new byte[input.Length];

                var aes = new AESBlockCipherCTR(key, icb);
                aes.Decrypt(input, 0, input.Length, output, 0);

                if (!System.Linq.Enumerable.SequenceEqual(output, expected)) {
                    throw new Exception(String.Format("{0} Decrypt failed: wrong output", testName));
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Decrypt failed: input data were corrupted", testName));
                }
            }

            // decrypt each blocks
            {
                var aes = new AESBlockCipherCTR(key, icb);
                int blockSize = aes.GetBlockSize();
                byte[] outputBlock = new byte[blockSize];
                byte[] expectedBlock = new byte[blockSize];
                for (int inputOffset = 0; inputOffset < input.Length; inputOffset += blockSize) {
                    aes.Decrypt(input, inputOffset, blockSize, outputBlock, 0);

                    Array.Copy(expected, inputOffset, expectedBlock, 0, blockSize);

                    if (!System.Linq.Enumerable.SequenceEqual(outputBlock, expectedBlock)) {
                        throw new Exception(String.Format("{0} Decrypt failed (inputOffset = {1}): wrong output", testName, inputOffset));
                    }
                }
                if (!System.Linq.Enumerable.SequenceEqual(input, inputOrig)) {
                    throw new Exception(String.Format("{0} Decrypt failed: input data were corrupted", testName));
                }
            }

            // in-place decrypt all blocks
            {
                var aes = new AESBlockCipherCTR(key, icb);
                aes.Decrypt(input, 0, input.Length, input, 0);

                if (!System.Linq.Enumerable.SequenceEqual(input, expected)) {
                    throw new Exception(String.Format("{0} Decrypt In-Place failed: wrong output", testName));
                }
            }
        }

        private static void Test_AES128_CTR_Encrypt() {
            test_AES_CTR_Encrypt(
                key: BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                icb: BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "874d6191b620e3261bef6864990db6ce"
                    + "9806f66b7970fdff8617187bb9fffdff"
                    + "5ae4df3edbd5d35e5b4f09020db03eab"
                    + "1e031dda2fbe03d1792170a0f3009cee"
                )
            );
        }

        private static void Test_AES128_CTR_Decrypt() {
            test_AES_CTR_Decrypt(
                key: BigIntegerConverter.ParseHex("2b7e151628aed2a6abf7158809cf4f3c"),
                icb: BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                input: BigIntegerConverter.ParseHex(
                      "874d6191b620e3261bef6864990db6ce"
                    + "9806f66b7970fdff8617187bb9fffdff"
                    + "5ae4df3edbd5d35e5b4f09020db03eab"
                    + "1e031dda2fbe03d1792170a0f3009cee"
                ),
                expected: (byte[])original.Clone()
            );
        }

        private static void Test_AES192_CTR_Encrypt() {
            test_AES_CTR_Encrypt(
                key: BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                icb: BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "1abc932417521ca24f2b0459fe7e6e0b"
                    + "090339ec0aa6faefd5ccc2c6f4ce8e94"
                    + "1e36b26bd1ebc670d1bd1d665620abf7"
                    + "4f78a7f6d29809585a97daec58c6b050"
                )
            );
        }

        private static void Test_AES192_CTR_Decrypt() {
            test_AES_CTR_Decrypt(
                key: BigIntegerConverter.ParseHex("8e73b0f7da0e6452c810f32b809079e562f8ead2522c6b7b"),
                icb: BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                input: BigIntegerConverter.ParseHex(
                      "1abc932417521ca24f2b0459fe7e6e0b"
                    + "090339ec0aa6faefd5ccc2c6f4ce8e94"
                    + "1e36b26bd1ebc670d1bd1d665620abf7"
                    + "4f78a7f6d29809585a97daec58c6b050"
                ),
                expected: (byte[])original.Clone()
            );
        }

        private static void Test_AES256_CTR_Encrypt() {
            test_AES_CTR_Encrypt(
                key: BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                icb: BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                input: (byte[])original.Clone(),
                expected: BigIntegerConverter.ParseHex(
                      "601ec313775789a5b7a7f504bbf3d228"
                    + "f443e3ca4d62b59aca84e990cacaf5c5"
                    + "2b0930daa23de94ce87017ba2d84988d"
                    + "dfc9c58db67aada613c2dd08457941a6"
                )
            );
        }

        private static void Test_AES256_CTR_Decrypt() {
            test_AES_CTR_Decrypt(
                key: BigIntegerConverter.ParseHex(
                      "603deb1015ca71be2b73aef0857d7781"
                    + "1f352c073b6108d72d9810a30914dff4"
                ),
                icb: BigIntegerConverter.ParseHex("f0f1f2f3f4f5f6f7f8f9fafbfcfdfeff"),
                input: BigIntegerConverter.ParseHex(
                      "601ec313775789a5b7a7f504bbf3d228"
                    + "f443e3ca4d62b59aca84e990cacaf5c5"
                    + "2b0930daa23de94ce87017ba2d84988d"
                    + "dfc9c58db67aada613c2dd08457941a6"
                ),
                expected: (byte[])original.Clone()
            );
        }
        #endregion

    }
#endif

}
