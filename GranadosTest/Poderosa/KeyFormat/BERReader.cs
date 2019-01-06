// Copyright 2011-2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#if UNITTEST
using Granados.Mono.Math;
using NUnit.Framework;
using System.IO;

namespace Granados.Poderosa.KeyFormat {

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

}
#endif
