// Copyright 2004-2017 The Poderosa Project.
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
using NUnit.Framework;
using Poderosa.Serializing;
using System.Diagnostics;
using System.Drawing;
using System.IO;

namespace Poderosa.View {

    [TestFixture]
    public class RenderProfileSerializeTests {

        private RenderProfileSerializer _renderProfileSerializer;

        [OneTimeSetUp]
        public void Init() {
            _renderProfileSerializer = new RenderProfileSerializer();
        }

        [Test]
        public void Test1() {
            RenderProfile prof1 = new RenderProfile();
            prof1.FontName = "console";
            prof1.CJKFontName = "ＭＳ ゴシック";
            prof1.UseClearType = true;
            prof1.FontSize = 12;
            prof1.BackColor = Color.FromKnownColor(KnownColor.Yellow);
            prof1.ForeColor = Color.FromKnownColor(KnownColor.White);
            prof1.BackgroundImageFileName = "image-file";
            prof1.ImageStyle = ImageStyle.Scaled;
            prof1.ESColorSet = new EscapesequenceColorSet();
            prof1.ESColorSet[1] = new ESColor(Color.Pink, true);

            SerializationOptions opt = new SerializationOptions();
            StructuredText storage = _renderProfileSerializer.Serialize(prof1, opt);
            //確認
            StringWriter wr = new StringWriter();
            new TextStructuredTextWriter(wr).Write(storage);
            wr.Close();
            Debug.WriteLine(wr.ToString());

            RenderProfile prof2 = (RenderProfile)_renderProfileSerializer.Deserialize(storage);

            Assert.AreEqual(prof1.FontName, prof2.FontName);
            Assert.AreEqual(prof1.CJKFontName, prof2.CJKFontName);
            Assert.AreEqual(prof1.UseClearType, prof2.UseClearType);
            Assert.AreEqual(prof1.FontSize, prof2.FontSize);
            Assert.AreEqual(prof1.BackColor.Name, prof2.BackColor.Name);
            Assert.AreEqual(prof1.ForeColor.Name, prof2.ForeColor.Name);
            Assert.AreEqual(prof1.BackgroundImageFileName, prof2.BackgroundImageFileName);
            Assert.AreEqual(prof1.ImageStyle, prof2.ImageStyle);
            Assert.AreEqual(prof1.ESColorSet.Format(), prof2.ESColorSet.Format());

        }

    }
}
#endif
