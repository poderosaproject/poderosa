// Copyright 2025 The Poderosa Project.
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
using System;
using System.Drawing;
using System.Linq;

using NUnit.Framework;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Poderosa.Terminal.Sixel {

    [TestFixture]
    class SixelTemporalRowBufferTest {

        private SixelPalette palette;

        [OneTimeSetUp]
        public void SetupPalette() {
            palette = new SixelPalette();
            for (int i = 0; i < 256; i++) {
                // B G R A = (i) (i+10) (i+20) 255
                palette[i] = Color.FromArgb((i + 20) % 256, (i + 10) % 256, i);
            }
        }

        [Test]
        public void TestEmpty() {
            SixelTemporalRowBuffer rb = new SixelTemporalRowBuffer();

            var bm = rb.GetSixelBitmap();
            Assert.GreaterOrEqual(bm.Width, 0);
            Assert.AreEqual(6, bm.Height);
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        public void TestSinglePass(int width) {
            SixelTemporalRowBuffer rb = new SixelTemporalRowBuffer();

            int[] patterns = { 1, 2, 4, 8, 16, 32, 1, 2, 4, 8, 16, 32 };
            int[] colors = { 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211 };

            // expected color placement
            // line 0:  200,   0,   0,   0,   0,   0, 206,   0,   0,   0,   0,   0
            // line 1:    0, 201,   0,   0,   0,   0,   0, 207,   0,   0,   0,   0
            // line 2:    0,   0, 202,   0,   0,   0,   0,   0, 208,   0,   0,   0
            // line 3:    0,   0,   0, 203,   0,   0,   0,   0,   0, 209,   0,   0
            // line 4:    0,   0,   0,   0, 204,   0,   0,   0,   0,   0, 210,   0
            // line 5:    0,   0,   0,   0,   0, 205,   0,   0,   0,   0,   0, 211

            // [ B, G, R, A ... ]
            byte[] expectedBitmap0 = { 200, 210, 220, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 206, 216, 226, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap1 = { 0, 0, 0, 0, 201, 211, 221, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 207, 217, 227, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap2 = { 0, 0, 0, 0, 0, 0, 0, 0, 202, 212, 222, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 208, 218, 228, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap3 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 203, 213, 223, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 209, 219, 229, 255, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap4 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 204, 214, 224, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 210, 220, 230, 255, 0, 0, 0, 0 };
            byte[] expectedBitmap5 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 205, 215, 225, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 211, 221, 231, 255 };

            for (int i = 0; i < width; i++) {
                rb.Put(patterns[i], colors[i], palette);
            }

            var bm = rb.GetSixelBitmap();
            Assert.GreaterOrEqual(bm.Width, width);
            Assert.AreEqual(6, bm.Height);

            BitmapData bmData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            try {
                Assert.AreEqual(expectedBitmap0.Take(width * 4).ToArray(), ToByteArray(bmData, 0));
                Assert.AreEqual(expectedBitmap1.Take(width * 4).ToArray(), ToByteArray(bmData, 1));
                Assert.AreEqual(expectedBitmap2.Take(width * 4).ToArray(), ToByteArray(bmData, 2));
                Assert.AreEqual(expectedBitmap3.Take(width * 4).ToArray(), ToByteArray(bmData, 3));
                Assert.AreEqual(expectedBitmap4.Take(width * 4).ToArray(), ToByteArray(bmData, 4));
                Assert.AreEqual(expectedBitmap5.Take(width * 4).ToArray(), ToByteArray(bmData, 5));
            }
            finally {
                bm.UnlockBits(bmData);
            }
        }

        private byte[] ToByteArray(BitmapData bmData, int y) {
            byte[] newArray = new byte[bmData.Width * 4];
            Marshal.Copy(bmData.Scan0 + bmData.Stride * y, newArray, 0, newArray.Length);
            return newArray;
        }

        [Test]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        public void TestMultiPass(int width) {
            SixelTemporalRowBuffer rb = new SixelTemporalRowBuffer();

            int[] patternsPass1 = { 1, 2, 4, 8, 16, 32, 1, 2, 4, 8, 16, 32 };
            int[] patternsPass2 = { 32, 32, 16, 16, 8, 8, 4, 4, 2, 2, 1, 1 };
            int[] patternsPass3 = { 1, 2, 4, 8, 16, 32, 1, 2, 4, 8, 16, 32 }; // overwrite

            int[] colorsPass1 = { 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100, 100 };
            int[] colorsPass2 = { 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180, 180 };
            int[] colorsPass3 = { 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211 }; // overwrite

            // expected color placement
            // line 0:  200,   0,   0,   0,   0,   0, 206,   0,   0,   0, 180, 180
            // line 1:    0, 201,   0,   0,   0,   0,   0, 207, 180, 180,   0,   0
            // line 2:    0,   0, 202,   0,   0,   0, 180, 180, 208,   0,   0,   0
            // line 3:    0,   0,   0, 203, 180, 180,   0,   0,   0, 209,   0,   0
            // line 4:    0,   0, 180, 180, 204,   0,   0,   0,   0,   0, 210,   0
            // line 5:  180, 180,   0,   0,   0, 205,   0,   0,   0,   0,   0, 211

            // [ B, G, R, A ... ]
            byte[] expectedBitmap0 = { 200, 210, 220, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 206, 216, 226, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 180, 190, 200, 255, 180, 190, 200, 255 };
            byte[] expectedBitmap1 = { 0, 0, 0, 0, 201, 211, 221, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 207, 217, 227, 255, 180, 190, 200, 255, 180, 190, 200, 255, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap2 = { 0, 0, 0, 0, 0, 0, 0, 0, 202, 212, 222, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 180, 190, 200, 255, 180, 190, 200, 255, 208, 218, 228, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap3 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 203, 213, 223, 255, 180, 190, 200, 255, 180, 190, 200, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 209, 219, 229, 255, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] expectedBitmap4 = { 0, 0, 0, 0, 0, 0, 0, 0, 180, 190, 200, 255, 180, 190, 200, 255, 204, 214, 224, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 210, 220, 230, 255, 0, 0, 0, 0 };
            byte[] expectedBitmap5 = { 180, 190, 200, 255, 180, 190, 200, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 205, 215, 225, 255, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 211, 221, 231, 255 };

            for (int i = 0; i < width; i++) {
                rb.Put(patternsPass1[i], colorsPass1[i], palette);
            }
            rb.CarriageReturn();
            for (int i = 0; i < width; i++) {
                rb.Put(patternsPass2[i], colorsPass2[i], palette);
            }
            rb.CarriageReturn();
            for (int i = 0; i < width; i++) {
                rb.Put(patternsPass3[i], colorsPass3[i], palette);
            }

            var bm = rb.GetSixelBitmap();
            Assert.GreaterOrEqual(bm.Width, width);
            Assert.AreEqual(6, bm.Height);

            BitmapData bmData = bm.LockBits(new Rectangle(0, 0, bm.Width, bm.Height), ImageLockMode.ReadOnly, bm.PixelFormat);
            try {
                Assert.AreEqual(expectedBitmap0.Take(width * 4).ToArray(), ToByteArray(bmData, 0));
                Assert.AreEqual(expectedBitmap1.Take(width * 4).ToArray(), ToByteArray(bmData, 1));
                Assert.AreEqual(expectedBitmap2.Take(width * 4).ToArray(), ToByteArray(bmData, 2));
                Assert.AreEqual(expectedBitmap3.Take(width * 4).ToArray(), ToByteArray(bmData, 3));
                Assert.AreEqual(expectedBitmap4.Take(width * 4).ToArray(), ToByteArray(bmData, 4));
                Assert.AreEqual(expectedBitmap5.Take(width * 4).ToArray(), ToByteArray(bmData, 5));
            }
            finally {
                bm.UnlockBits(bmData);
            }
        }

        [Test]
        public void TestWidthAndOffset() {
            SixelTemporalRowBuffer rb = new SixelTemporalRowBuffer();

            Assert.AreEqual(0, rb.Width);
            Assert.AreEqual(0, rb.CurrentX);

            rb.CarriageReturn();

            Assert.AreEqual(0, rb.Width);
            Assert.AreEqual(0, rb.CurrentX);

            for (int i = 0; i < 12; i++) {
                rb.Put(5, 1, palette);
            }

            Assert.AreEqual(12, rb.Width);
            Assert.AreEqual(12, rb.CurrentX);

            rb.CarriageReturn();

            Assert.AreEqual(12, rb.Width);
            Assert.AreEqual(0, rb.CurrentX);

            for (int i = 0; i < 7; i++) {
                rb.Put(4, 2, palette);
            }

            Assert.AreEqual(12, rb.Width);
            Assert.AreEqual(7, rb.CurrentX);

            rb.CarriageReturn();

            Assert.AreEqual(12, rb.Width);
            Assert.AreEqual(0, rb.CurrentX);

            for (int i = 0; i < 15; i++) {
                rb.Put(9, 3, palette);
            }

            Assert.AreEqual(15, rb.Width);
            Assert.AreEqual(15, rb.CurrentX);
        }
    }

    [TestFixture]
    class HlsSupportTest {

        // Note:
        // If optimization is enabled, calculated RGB may differ slightly from the expected value.
        [Test]
        [TestCase(0, 0, 0, 0, 0, 0)]
        [TestCase(0, 0, 25, 0, 0, 0)]
        [TestCase(0, 0, 50, 0, 0, 0)]
        [TestCase(0, 0, 75, 0, 0, 0)]
        [TestCase(0, 0, 100, 0, 0, 0)]
        [TestCase(0, 25, 0, 63, 63, 63)]
        [TestCase(0, 25, 25, 47, 47, 79)]
        [TestCase(0, 25, 50, 31, 31, 95)]
        [TestCase(0, 25, 75, 15, 15, 111)]
        [TestCase(0, 25, 100, 0, 0, 127)]
        [TestCase(0, 50, 0, 127, 127, 127)]
        [TestCase(0, 50, 25, 95, 95, 159)]
        [TestCase(0, 50, 50, 63, 63, 191)]
        [TestCase(0, 50, 75, 31, 31, 223)]
        [TestCase(0, 50, 100, 0, 0, 255)]
        [TestCase(0, 75, 0, 191, 191, 191)]
        [TestCase(0, 75, 25, 175, 175, 207)]
        [TestCase(0, 75, 50, 159, 159, 223)]
        [TestCase(0, 75, 75, 143, 143, 239)]
        [TestCase(0, 75, 100, 127, 127, 255)]
        [TestCase(0, 100, 0, 255, 255, 255)]
        [TestCase(0, 100, 25, 255, 255, 255)]
        [TestCase(0, 100, 50, 255, 255, 255)]
        [TestCase(0, 100, 75, 255, 255, 255)]
        [TestCase(0, 100, 100, 255, 255, 255)]
        [TestCase(30, 0, 0, 0, 0, 0)]
        [TestCase(30, 0, 25, 0, 0, 0)]
        [TestCase(30, 0, 50, 0, 0, 0)]
        [TestCase(30, 0, 75, 0, 0, 0)]
        [TestCase(30, 0, 100, 0, 0, 0)]
        [TestCase(30, 25, 0, 63, 63, 63)]
        [TestCase(30, 25, 25, 63, 47, 79)]
        [TestCase(30, 25, 50, 63, 31, 95)]
        [TestCase(30, 25, 75, 63, 15, 111)]
        [TestCase(30, 25, 100, 63, 0, 127)]
        [TestCase(30, 50, 0, 127, 127, 127)]
        [TestCase(30, 50, 25, 127, 95, 159)]
        [TestCase(30, 50, 50, 127, 63, 191)]
        [TestCase(30, 50, 75, 127, 31, 223)]
        [TestCase(30, 50, 100, 127, 0, 255)]
        [TestCase(30, 75, 0, 191, 191, 191)]
        [TestCase(30, 75, 25, 191, 175, 207)]
        [TestCase(30, 75, 50, 191, 159, 223)]
        [TestCase(30, 75, 75, 191, 143, 239)]
        [TestCase(30, 75, 100, 191, 127, 255)]
        [TestCase(30, 100, 0, 255, 255, 255)]
        [TestCase(30, 100, 25, 255, 255, 255)]
        [TestCase(30, 100, 50, 255, 255, 255)]
        [TestCase(30, 100, 75, 255, 255, 255)]
        [TestCase(30, 100, 100, 255, 255, 255)]
        [TestCase(60, 0, 0, 0, 0, 0)]
        [TestCase(60, 0, 25, 0, 0, 0)]
        [TestCase(60, 0, 50, 0, 0, 0)]
        [TestCase(60, 0, 75, 0, 0, 0)]
        [TestCase(60, 0, 100, 0, 0, 0)]
        [TestCase(60, 25, 0, 63, 63, 63)]
        [TestCase(60, 25, 25, 79, 47, 79)]
        [TestCase(60, 25, 50, 95, 31, 95)]
        [TestCase(60, 25, 75, 111, 15, 111)]
        [TestCase(60, 25, 100, 127, 0, 127)]
        [TestCase(60, 50, 0, 127, 127, 127)]
        [TestCase(60, 50, 25, 159, 95, 159)]
        [TestCase(60, 50, 50, 191, 63, 191)]
        [TestCase(60, 50, 75, 223, 31, 223)]
        [TestCase(60, 50, 100, 255, 0, 255)]
        [TestCase(60, 75, 0, 191, 191, 191)]
        [TestCase(60, 75, 25, 207, 175, 207)]
        [TestCase(60, 75, 50, 223, 159, 223)]
        [TestCase(60, 75, 75, 239, 143, 239)]
        [TestCase(60, 75, 100, 255, 127, 255)]
        [TestCase(60, 100, 0, 255, 255, 255)]
        [TestCase(60, 100, 25, 255, 255, 255)]
        [TestCase(60, 100, 50, 255, 255, 255)]
        [TestCase(60, 100, 75, 255, 255, 255)]
        [TestCase(60, 100, 100, 255, 255, 255)]
        [TestCase(90, 0, 0, 0, 0, 0)]
        [TestCase(90, 0, 25, 0, 0, 0)]
        [TestCase(90, 0, 50, 0, 0, 0)]
        [TestCase(90, 0, 75, 0, 0, 0)]
        [TestCase(90, 0, 100, 0, 0, 0)]
        [TestCase(90, 25, 0, 63, 63, 63)]
        [TestCase(90, 25, 25, 79, 47, 63)]
        [TestCase(90, 25, 50, 95, 31, 63)]
        [TestCase(90, 25, 75, 111, 15, 63)]
        [TestCase(90, 25, 100, 127, 0, 63)]
        [TestCase(90, 50, 0, 127, 127, 127)]
        [TestCase(90, 50, 25, 159, 95, 127)]
        [TestCase(90, 50, 50, 191, 63, 127)]
        [TestCase(90, 50, 75, 223, 31, 127)]
        [TestCase(90, 50, 100, 255, 0, 127)]
        [TestCase(90, 75, 0, 191, 191, 191)]
        [TestCase(90, 75, 25, 207, 175, 191)]
        [TestCase(90, 75, 50, 223, 159, 191)]
        [TestCase(90, 75, 75, 239, 143, 191)]
        [TestCase(90, 75, 100, 255, 127, 191)]
        [TestCase(90, 100, 0, 255, 255, 255)]
        [TestCase(90, 100, 25, 255, 255, 255)]
        [TestCase(90, 100, 50, 255, 255, 255)]
        [TestCase(90, 100, 75, 255, 255, 255)]
        [TestCase(90, 100, 100, 255, 255, 255)]
        [TestCase(120, 0, 0, 0, 0, 0)]
        [TestCase(120, 0, 25, 0, 0, 0)]
        [TestCase(120, 0, 50, 0, 0, 0)]
        [TestCase(120, 0, 75, 0, 0, 0)]
        [TestCase(120, 0, 100, 0, 0, 0)]
        [TestCase(120, 25, 0, 63, 63, 63)]
        [TestCase(120, 25, 25, 79, 47, 47)]
        [TestCase(120, 25, 50, 95, 31, 31)]
        [TestCase(120, 25, 75, 111, 15, 15)]
        [TestCase(120, 25, 100, 127, 0, 0)]
        [TestCase(120, 50, 0, 127, 127, 127)]
        [TestCase(120, 50, 25, 159, 95, 95)]
        [TestCase(120, 50, 50, 191, 63, 63)]
        [TestCase(120, 50, 75, 223, 31, 31)]
        [TestCase(120, 50, 100, 255, 0, 0)]
        [TestCase(120, 75, 0, 191, 191, 191)]
        [TestCase(120, 75, 25, 207, 175, 175)]
        [TestCase(120, 75, 50, 223, 159, 159)]
        [TestCase(120, 75, 75, 239, 143, 143)]
        [TestCase(120, 75, 100, 255, 127, 127)]
        [TestCase(120, 100, 0, 255, 255, 255)]
        [TestCase(120, 100, 25, 255, 255, 255)]
        [TestCase(120, 100, 50, 255, 255, 255)]
        [TestCase(120, 100, 75, 255, 255, 255)]
        [TestCase(120, 100, 100, 255, 255, 255)]
        [TestCase(150, 0, 0, 0, 0, 0)]
        [TestCase(150, 0, 25, 0, 0, 0)]
        [TestCase(150, 0, 50, 0, 0, 0)]
        [TestCase(150, 0, 75, 0, 0, 0)]
        [TestCase(150, 0, 100, 0, 0, 0)]
        [TestCase(150, 25, 0, 63, 63, 63)]
        [TestCase(150, 25, 25, 79, 63, 47)]
        [TestCase(150, 25, 50, 95, 63, 31)]
        [TestCase(150, 25, 75, 111, 63, 15)]
        [TestCase(150, 25, 100, 127, 63, 0)]
        [TestCase(150, 50, 0, 127, 127, 127)]
        [TestCase(150, 50, 25, 159, 127, 95)]
        [TestCase(150, 50, 50, 191, 127, 63)]
        [TestCase(150, 50, 75, 223, 127, 31)]
        [TestCase(150, 50, 100, 255, 127, 0)]
        [TestCase(150, 75, 0, 191, 191, 191)]
        [TestCase(150, 75, 25, 207, 191, 175)]
        [TestCase(150, 75, 50, 223, 191, 159)]
        [TestCase(150, 75, 75, 239, 191, 143)]
        [TestCase(150, 75, 100, 255, 191, 127)]
        [TestCase(150, 100, 0, 255, 255, 255)]
        [TestCase(150, 100, 25, 255, 255, 255)]
        [TestCase(150, 100, 50, 255, 255, 255)]
        [TestCase(150, 100, 75, 255, 255, 255)]
        [TestCase(150, 100, 100, 255, 255, 255)]
        [TestCase(180, 0, 0, 0, 0, 0)]
        [TestCase(180, 0, 25, 0, 0, 0)]
        [TestCase(180, 0, 50, 0, 0, 0)]
        [TestCase(180, 0, 75, 0, 0, 0)]
        [TestCase(180, 0, 100, 0, 0, 0)]
        [TestCase(180, 25, 0, 63, 63, 63)]
        [TestCase(180, 25, 25, 79, 79, 47)]
        [TestCase(180, 25, 50, 95, 95, 31)]
        [TestCase(180, 25, 75, 111, 111, 15)]
        [TestCase(180, 25, 100, 127, 127, 0)]
        [TestCase(180, 50, 0, 127, 127, 127)]
        [TestCase(180, 50, 25, 159, 159, 95)]
        [TestCase(180, 50, 50, 191, 191, 63)]
        [TestCase(180, 50, 75, 223, 223, 31)]
        [TestCase(180, 50, 100, 255, 255, 0)]
        [TestCase(180, 75, 0, 191, 191, 191)]
        [TestCase(180, 75, 25, 207, 207, 175)]
        [TestCase(180, 75, 50, 223, 223, 159)]
        [TestCase(180, 75, 75, 239, 239, 143)]
        [TestCase(180, 75, 100, 255, 255, 127)]
        [TestCase(180, 100, 0, 255, 255, 255)]
        [TestCase(180, 100, 25, 255, 255, 255)]
        [TestCase(180, 100, 50, 255, 255, 255)]
        [TestCase(180, 100, 75, 255, 255, 255)]
        [TestCase(180, 100, 100, 255, 255, 255)]
        [TestCase(210, 0, 0, 0, 0, 0)]
        [TestCase(210, 0, 25, 0, 0, 0)]
        [TestCase(210, 0, 50, 0, 0, 0)]
        [TestCase(210, 0, 75, 0, 0, 0)]
        [TestCase(210, 0, 100, 0, 0, 0)]
        [TestCase(210, 25, 0, 63, 63, 63)]
        [TestCase(210, 25, 25, 63, 79, 47)]
        [TestCase(210, 25, 50, 63, 95, 31)]
        [TestCase(210, 25, 75, 63, 111, 15)]
        [TestCase(210, 25, 100, 63, 127, 0)]
        [TestCase(210, 50, 0, 127, 127, 127)]
        [TestCase(210, 50, 25, 127, 159, 95)]
        [TestCase(210, 50, 50, 127, 191, 63)]
        [TestCase(210, 50, 75, 127, 223, 31)]
        [TestCase(210, 50, 100, 127, 255, 0)]
        [TestCase(210, 75, 0, 191, 191, 191)]
        [TestCase(210, 75, 25, 191, 207, 175)]
        [TestCase(210, 75, 50, 191, 223, 159)]
        [TestCase(210, 75, 75, 191, 239, 143)]
        [TestCase(210, 75, 100, 191, 255, 127)]
        [TestCase(210, 100, 0, 255, 255, 255)]
        [TestCase(210, 100, 25, 255, 255, 255)]
        [TestCase(210, 100, 50, 255, 255, 255)]
        [TestCase(210, 100, 75, 255, 255, 255)]
        [TestCase(210, 100, 100, 255, 255, 255)]
        [TestCase(240, 0, 0, 0, 0, 0)]
        [TestCase(240, 0, 25, 0, 0, 0)]
        [TestCase(240, 0, 50, 0, 0, 0)]
        [TestCase(240, 0, 75, 0, 0, 0)]
        [TestCase(240, 0, 100, 0, 0, 0)]
        [TestCase(240, 25, 0, 63, 63, 63)]
        [TestCase(240, 25, 25, 47, 79, 47)]
        [TestCase(240, 25, 50, 31, 95, 31)]
        [TestCase(240, 25, 75, 15, 111, 15)]
        [TestCase(240, 25, 100, 0, 127, 0)]
        [TestCase(240, 50, 0, 127, 127, 127)]
        [TestCase(240, 50, 25, 95, 159, 95)]
        [TestCase(240, 50, 50, 63, 191, 63)]
        [TestCase(240, 50, 75, 31, 223, 31)]
        [TestCase(240, 50, 100, 0, 255, 0)]
        [TestCase(240, 75, 0, 191, 191, 191)]
        [TestCase(240, 75, 25, 175, 207, 175)]
        [TestCase(240, 75, 50, 159, 223, 159)]
        [TestCase(240, 75, 75, 143, 239, 143)]
        [TestCase(240, 75, 100, 127, 255, 127)]
        [TestCase(240, 100, 0, 255, 255, 255)]
        [TestCase(240, 100, 25, 255, 255, 255)]
        [TestCase(240, 100, 50, 255, 255, 255)]
        [TestCase(240, 100, 75, 255, 255, 255)]
        [TestCase(240, 100, 100, 255, 255, 255)]
        [TestCase(270, 0, 0, 0, 0, 0)]
        [TestCase(270, 0, 25, 0, 0, 0)]
        [TestCase(270, 0, 50, 0, 0, 0)]
        [TestCase(270, 0, 75, 0, 0, 0)]
        [TestCase(270, 0, 100, 0, 0, 0)]
        [TestCase(270, 25, 0, 63, 63, 63)]
        [TestCase(270, 25, 25, 47, 79, 63)]
        [TestCase(270, 25, 50, 31, 95, 63)]
        [TestCase(270, 25, 75, 15, 111, 63)]
        [TestCase(270, 25, 100, 0, 127, 63)]
        [TestCase(270, 50, 0, 127, 127, 127)]
        [TestCase(270, 50, 25, 95, 159, 127)]
        [TestCase(270, 50, 50, 63, 191, 127)]
        [TestCase(270, 50, 75, 31, 223, 127)]
        [TestCase(270, 50, 100, 0, 255, 127)]
        [TestCase(270, 75, 0, 191, 191, 191)]
        [TestCase(270, 75, 25, 175, 207, 191)]
        [TestCase(270, 75, 50, 159, 223, 191)]
        [TestCase(270, 75, 75, 143, 239, 191)]
        [TestCase(270, 75, 100, 127, 255, 191)]
        [TestCase(270, 100, 0, 255, 255, 255)]
        [TestCase(270, 100, 25, 255, 255, 255)]
        [TestCase(270, 100, 50, 255, 255, 255)]
        [TestCase(270, 100, 75, 255, 255, 255)]
        [TestCase(270, 100, 100, 255, 255, 255)]
        [TestCase(300, 0, 0, 0, 0, 0)]
        [TestCase(300, 0, 25, 0, 0, 0)]
        [TestCase(300, 0, 50, 0, 0, 0)]
        [TestCase(300, 0, 75, 0, 0, 0)]
        [TestCase(300, 0, 100, 0, 0, 0)]
        [TestCase(300, 25, 0, 63, 63, 63)]
        [TestCase(300, 25, 25, 47, 79, 79)]
        [TestCase(300, 25, 50, 31, 95, 95)]
        [TestCase(300, 25, 75, 15, 111, 111)]
        [TestCase(300, 25, 100, 0, 127, 127)]
        [TestCase(300, 50, 0, 127, 127, 127)]
        [TestCase(300, 50, 25, 95, 159, 159)]
        [TestCase(300, 50, 50, 63, 191, 191)]
        [TestCase(300, 50, 75, 31, 223, 223)]
        [TestCase(300, 50, 100, 0, 255, 255)]
        [TestCase(300, 75, 0, 191, 191, 191)]
        [TestCase(300, 75, 25, 175, 207, 207)]
        [TestCase(300, 75, 50, 159, 223, 223)]
        [TestCase(300, 75, 75, 143, 239, 239)]
        [TestCase(300, 75, 100, 127, 255, 255)]
        [TestCase(300, 100, 0, 255, 255, 255)]
        [TestCase(300, 100, 25, 255, 255, 255)]
        [TestCase(300, 100, 50, 255, 255, 255)]
        [TestCase(300, 100, 75, 255, 255, 255)]
        [TestCase(300, 100, 100, 255, 255, 255)]
        [TestCase(330, 0, 0, 0, 0, 0)]
        [TestCase(330, 0, 25, 0, 0, 0)]
        [TestCase(330, 0, 50, 0, 0, 0)]
        [TestCase(330, 0, 75, 0, 0, 0)]
        [TestCase(330, 0, 100, 0, 0, 0)]
        [TestCase(330, 25, 0, 63, 63, 63)]
        [TestCase(330, 25, 25, 47, 63, 79)]
        [TestCase(330, 25, 50, 31, 63, 95)]
        [TestCase(330, 25, 75, 15, 63, 111)]
        [TestCase(330, 25, 100, 0, 63, 127)]
        [TestCase(330, 50, 0, 127, 127, 127)]
        [TestCase(330, 50, 25, 95, 127, 159)]
        [TestCase(330, 50, 50, 63, 127, 191)]
        [TestCase(330, 50, 75, 31, 127, 223)]
        [TestCase(330, 50, 100, 0, 127, 255)]
        [TestCase(330, 75, 0, 191, 191, 191)]
        [TestCase(330, 75, 25, 175, 191, 207)]
        [TestCase(330, 75, 50, 159, 191, 223)]
        [TestCase(330, 75, 75, 143, 191, 239)]
        [TestCase(330, 75, 100, 127, 191, 255)]
        [TestCase(330, 100, 0, 255, 255, 255)]
        [TestCase(330, 100, 25, 255, 255, 255)]
        [TestCase(330, 100, 50, 255, 255, 255)]
        [TestCase(330, 100, 75, 255, 255, 255)]
        [TestCase(330, 100, 100, 255, 255, 255)]
        [TestCase(360, 0, 0, 0, 0, 0)]
        [TestCase(360, 0, 25, 0, 0, 0)]
        [TestCase(360, 0, 50, 0, 0, 0)]
        [TestCase(360, 0, 75, 0, 0, 0)]
        [TestCase(360, 0, 100, 0, 0, 0)]
        [TestCase(360, 25, 0, 63, 63, 63)]
        [TestCase(360, 25, 25, 47, 47, 79)]
        [TestCase(360, 25, 50, 31, 31, 95)]
        [TestCase(360, 25, 75, 15, 15, 111)]
        [TestCase(360, 25, 100, 0, 0, 127)]
        [TestCase(360, 50, 0, 127, 127, 127)]
        [TestCase(360, 50, 25, 95, 95, 159)]
        [TestCase(360, 50, 50, 63, 63, 191)]
        [TestCase(360, 50, 75, 31, 31, 223)]
        [TestCase(360, 50, 100, 0, 0, 255)]
        [TestCase(360, 75, 0, 191, 191, 191)]
        [TestCase(360, 75, 25, 175, 175, 207)]
        [TestCase(360, 75, 50, 159, 159, 223)]
        [TestCase(360, 75, 75, 143, 143, 239)]
        [TestCase(360, 75, 100, 127, 127, 255)]
        [TestCase(360, 100, 0, 255, 255, 255)]
        [TestCase(360, 100, 25, 255, 255, 255)]
        [TestCase(360, 100, 50, 255, 255, 255)]
        [TestCase(360, 100, 75, 255, 255, 255)]
        [TestCase(360, 100, 100, 255, 255, 255)]
        public void Test(int hue, int lightness, int saturarion, int expectedR, int expectedG, int expectedB) {
            Color color = HlsSupport.HlsToRgb(hue, lightness, saturarion);
            Assert.AreEqual(Color.FromArgb(expectedR, expectedG, expectedB), color);
        }
    }
}
#endif
