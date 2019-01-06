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
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Poderosa.Boot {

    [TestFixture]
    public class PluginManifestTests {

        private StringResource _stringResource;

        [OneTimeSetUp]
        public void Init() {
            _stringResource = new StringResource("Plugin.strings", typeof(PluginManifest).Assembly);
        }

        [Test]
        public void Test2_NormalLoad() {
            ITracer tracer = CreateDefaultTracer();
            Poderosa.Boot.PluginManifest pm = PluginManifest.CreateByText(String.Format("manifest {{\r\n  {0}\\Core.dll {{\r\n plugin=Poderosa.Preferences.PreferencePlugin\r\n}}\r\n}}\r\n", PoderosaAppDir()));
            int count = 0;
            foreach (PluginManifest.AssemblyEntry ent in pm.Entries) {
                Assert.AreEqual(1, ent.PluginTypeCount);
                Assert.AreEqual(1, ent.PluginTypes.Count());
                Assert.AreEqual("Poderosa.Preferences.PreferencePlugin", ent.PluginTypes.First());
                count++;
            }
            Assert.AreEqual(1, count); //アセンブリ指定は１個しかないので
        }

        private string PoderosaAppDir() {
            return Path.GetDirectoryName(Assembly.GetAssembly(typeof(Poderosa.Plugins.PluginBase)).Location);
        }

        //PoderosaWorldを経由しないテストなのでこれで凌ぐ
        private ITracer CreateDefaultTracer() {
            return new DefaultTracer(_stringResource);
        }

        private void CheckOneErrorMessage(TraceDocument doc, string msg) {
            string actual = doc.GetDataAt(0);
            if (actual != msg) {
                //しばしば長くなる。Debugに出さないとわかりづらい
                Debug.WriteLine("actual=" + actual);
            }
            Assert.AreEqual(msg, actual);
        }
    }

}
#endif
