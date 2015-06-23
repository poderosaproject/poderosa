/*
 Copyright (c) 2015 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.
*/
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Granados {

    /// <summary>
    /// Random number generator
    /// </summary>
    public interface Rng {
        /// <summary>
        /// Fills an array of bytes with random values.
        /// </summary>
        /// <param name="data">array to fill with random values.</param>
        void GetBytes(byte[] data);

        /// <summary>
        /// Returns a random number between 0 and maxValue-1
        /// </summary>
        int GetInt(int maxValue);
    }

    /// <summary>
    /// Random number generation utility
    /// </summary>
    public static class RngManager {

#if true
        // for .Net 2.0
        [ThreadStatic]
#endif
        private static RNGCryptoServiceProvider _coreRng;

        /// <summary>
        /// Get a new instance which implements Rng using <see cref="System.Random"/>
        /// </summary>
        /// <remarks>An instance returned should be used in the same thread.</remarks>
        /// <returns>a new instance</returns>
        public static Rng GetSystemRandomRng() {
            return new SystemRandomRng();
        }

        /// <summary>
        /// Get a new instance which implements Rng using <see cref="System.Random"/>
        /// </summary>
        /// <remarks>An instance returned should be used in the same thread.</remarks>
        /// <returns>a new instance</returns>
        public static Rng GetSecureRng() {
            if (_coreRng == null) {
                _coreRng = new RNGCryptoServiceProvider();
            }
            return new SecureRng(_coreRng);
        }

        /// <summary>
        /// implementation of Rng
        /// </summary>
        /// <remarks>An instance returned should be used in the same thread.</remarks>
        private class SecureRng : Rng {

            private readonly RNGCryptoServiceProvider _rng;

            public SecureRng(RNGCryptoServiceProvider rng) {
                _rng = rng;
            }

            public void GetBytes(byte[] data) {
                _rng.GetBytes(data);
            }

            public int GetInt(int maxValue) {
                if (maxValue < 0) {
                    throw new ArgumentOutOfRangeException("maxValue");
                }
                byte[] rbits = new byte[4];
                GetBytes(rbits);
                uint r = BitConverter.ToUInt32(rbits, 0);
                return (int)((((long)r) * maxValue) >> 32);
            }
        }

        /// <summary>
        /// implementation of Rng
        /// </summary>
        private class SystemRandomRng : Rng {

            private readonly Random _random = new Random();

            public void GetBytes(byte[] data) {
                _random.NextBytes(data);
            }

            public int GetInt(int maxValue) {
                return _random.Next(maxValue);
            }
        }
    }
}


