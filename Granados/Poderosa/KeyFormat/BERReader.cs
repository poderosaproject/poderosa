/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: BERReader.cs,v 1.1 2011/11/03 16:27:38 kzmi Exp $
 */
using System;
using System.IO;

#if UNITTEST
using NUnit.Framework;
#endif

namespace Granados.Poderosa.KeyFormat {

    /// <summary>
    /// Reads elements which are encoded by ASN.1 Basic Encoding Rules
    /// </summary>
    /// <remarks>
    /// Only SEQUENCE and INTEGER are supported.
    /// </remarks>
    internal class BERReader {

        private readonly Stream strm;

        private const int LENGTH_INDEFINITE = -1;
        private const int TAG_INTEGER = 2;
        private const int TAG_SEQUENCE = 16;

        internal struct BERTagInfo {
            public int ClassBits;
            public bool IsConstructed;
            public int TagNumber;
            public int Length;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s">stream to input</param>
        public BERReader(Stream s) {
            this.strm = s;
        }

        /// <summary>
        /// Read sequnce. (only check the value type)
        /// </summary>
        /// <returns>true if succeeded.</returns>
        public bool ReadSequence() {
            BERTagInfo tagInfo = new BERTagInfo();
            if (ReadTagInfo(ref tagInfo)
                && tagInfo.IsConstructed == true
                && tagInfo.TagNumber == TAG_SEQUENCE
                && (tagInfo.Length == LENGTH_INDEFINITE || tagInfo.Length > 0)) {

                return true;
            }
            return false;
        }

        /// <summary>
        /// Read integer.
        /// </summary>
        /// <param name="bigint">BigInteger instance will be stored if succeeded.</param>
        /// <returns>true if succeeded.</returns>
        public bool ReadInteger(out BigInteger bigint) {
            BERTagInfo tagInfo = new BERTagInfo();
            if (ReadTagInfo(ref tagInfo)
                && tagInfo.IsConstructed == false
                && tagInfo.TagNumber == TAG_INTEGER
                && tagInfo.Length != LENGTH_INDEFINITE
                && tagInfo.Length > 0) {

                byte[] buff = new byte[tagInfo.Length];
                int len = strm.Read(buff, 0, tagInfo.Length);
                if (len == tagInfo.Length) {
                    bigint = new BigInteger(buff);
                    return true;
                }
            }

            bigint = null;
            return false;
        }

        internal bool ReadTagInfo(ref BERTagInfo tagInfo) {
            return ReadTag(ref tagInfo.ClassBits, ref tagInfo.IsConstructed, ref tagInfo.TagNumber) && ReadLength(ref tagInfo.Length);
        }

        private bool ReadTag(ref int cls, ref bool constructed, ref int tagnum) {
            int n = strm.ReadByte();
            if (n == -1)
                return false;
            cls = (n >> 6) & 0x3;
            constructed = ((n & 0x20) != 0);
            if ((n & 0x1f) != 0x1f) {
                tagnum = n & 0x1f;
                return true;
            }

            int num = 0;
            int bits = 0;
            while (true) {
                n = strm.ReadByte();
                if (n == -1)
                    return false;
                num = (num << 7) | (n & 0x7f);
                if (bits == 0) {
                    bits = 7;
                    for (int mask = 0x40; bits != 0; mask >>= 1, bits--) {
                        if ((n & mask) != 0)
                            break;
                    }
                }
                else {
                    bits += 7;
                    if (bits > 31)
                        return false;
                }
                if ((n & 0x80) == 0)
                    break;
            }
            tagnum = num;
            return true;
        }

        private bool ReadLength(ref int length) {
            int n = strm.ReadByte();
            if (n == -1)
                return false;
            if (n == 0x80) {
                length = LENGTH_INDEFINITE;
                return true;
            }
            if ((n & 0x80) == 0) {
                length = n & 0x7f;
                return true;
            }

            int octets = n & 0x7f;
            int num = 0;
            int bits = 0;
            for (int i = 0; i < octets; i++) {
                n = strm.ReadByte();
                if (n == -1)
                    return false;
                num = (num << 8) | (n & 0xff);
                bits += 8;
                if (bits > 31)
                    return false;
            }
            length = num;
            return true;
        }

    }

#if UNITTEST

    [TestFixture]
    public class BERReaderTest {

        [Test]
        public void TestLargeTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x7f, 0x87, 0xef, 0xab, 0xb7, 0x6e, 0x03, 0x02, 0x01, 0x01 }
            )) {
                BERReader reader = new BERReader(mem);
                BERReader.BERTagInfo tagInfo = new BERReader.BERTagInfo();
                Assert.True(reader.ReadTagInfo(ref tagInfo));
                Assert.AreEqual(1, tagInfo.ClassBits);
                Assert.AreEqual(true, tagInfo.IsConstructed);
                Assert.AreEqual(0x7deadbee, tagInfo.TagNumber);
                Assert.AreEqual(3, tagInfo.Length);
            }
        }

        [Test]
        public void TestIncompleteTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x7f, 0x87, 0xef, 0xab, 0xb7, 0x6e }
            )) {
                BERReader reader = new BERReader(mem);
                BERReader.BERTagInfo tagInfo = new BERReader.BERTagInfo();
                Assert.False(reader.ReadTagInfo(ref tagInfo));
            }
        }

        [Test]
        public void TestIncompleteSequenceTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x30 }
            )) {
                BERReader reader = new BERReader(mem);
                Assert.False(reader.ReadSequence());
            }
        }

        [Test]
        public void TestNonSequenceTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x02, 0x01, 0x01 }
            )) {
                BERReader reader = new BERReader(mem);
                Assert.False(reader.ReadSequence());
            }
        }

        [Test]
        public void TestSequenceTag1() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x30, 0x03, 0x02, 0x01, 0x01 }
            )) {
                BERReader reader = new BERReader(mem);
                Assert.True(reader.ReadSequence());
            }
        }

        [Test]
        public void TestIndefiniteSequenceTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x30, 0x80, 0x02, 0x01, 0x01, 0x00, 0x00 }
            )) {
                BERReader reader = new BERReader(mem);
                Assert.True(reader.ReadSequence());
            }
        }

        [Test]
        public void TestIncompleteIntegerTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x02, 0x04, 0x12, 0x34, 0x56 }
            )) {
                BERReader reader = new BERReader(mem);
                BigInteger n;
                Assert.False(reader.ReadInteger(out n));
            }
        }

        [Test]
        public void TestNonIntegerTag() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x30, 0x03, 0x02, 0x01, 0x01 }
            )) {
                BERReader reader = new BERReader(mem);
                BigInteger n;
                Assert.False(reader.ReadInteger(out n));
            }
        }

        [Test]
        public void TestIntegerTag1() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x02, 0x01, 0xa3 }
            )) {
                BERReader reader = new BERReader(mem);
                BigInteger n;
                Assert.True(reader.ReadInteger(out n));
                Assert.AreEqual("163", n.ToString());
            }
        }

        [Test]
        public void TestIntegerTag3() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x02, 0x01, 0x00 }
            )) {
                BERReader reader = new BERReader(mem);
                BigInteger n;
                Assert.True(reader.ReadInteger(out n));
                Assert.AreEqual("0", n.ToString());
            }
        }

        [Test]
        public void TestIntegerTag4() {
            using (MemoryStream mem = new MemoryStream(
                new byte[] { 0x02, 0x09, 0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0, 0x01 }
            )) {
                BERReader reader = new BERReader(mem);
                BigInteger n;
                Assert.True(reader.ReadInteger(out n));
                Assert.AreEqual("123456789ABCDEF001", n.ToString(16));
            }
        }

    }

#endif

}
