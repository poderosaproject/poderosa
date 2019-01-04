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

namespace Poderosa.Terminal {

    [TestFixture]
    public class ShellSchemeTests {
        [Test]
        public void ParseTests() {
            GenericShellScheme g = new GenericShellScheme("generic", "");
            Confirm(g.ParseCommandInput("a b c"), "a", "b", "c");
            Confirm(g.ParseCommandInput(" a  b  c "), "a", "b", "c");
            Confirm(g.ParseCommandInput("abc \"abc abc\""), "abc", "\"abc abc\"");
            Confirm(g.ParseCommandInput(" abc \"abc abc "), "abc", "\"abc abc ");
        }
        private void Confirm(string[] actual, params string[] expected) {
            Assert.AreEqual(expected.Length, actual.Length);
            for (int i = 0; i < actual.Length; i++) {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [Test]
        public void CommandListTests1() {
            CommandListOne("a;b;c", "a", "b", "c");
            CommandListOne("\\[a;b];b;c", "a;b", "b", "c");
            CommandListOne("\\<a;[]b>;\\[a;b;c];c", "a;[]b", "a;b;c", "c");
        }
        private void CommandListOne(string input, params string[] expected) {
            GenericShellScheme g = new GenericShellScheme("generic", "");
            g.SetCommandList(input);
            IntelliSenseItemCollection col = (IntelliSenseItemCollection)g.CommandHistory;
            Confirm(col.ToStringArray(), expected);
            Assert.AreEqual(input, g.FormatCommandList()); //再フォーマット
        }

    }

}
#endif
