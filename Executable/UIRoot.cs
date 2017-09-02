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

#if UIUNITTEST
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using Poderosa.Preferences;
using Poderosa.Document;
using Poderosa.View;
using Poderosa.Terminal;

namespace Poderosa.Executable {
    internal class UIRoot {

        private const string HOMEDIR = "C:\\P4\\src\\";

        public static void Main(string[] args) {
            GEnv.ReloadStringResource();
            Init();

            if (args.Length == 0)
                Debug.WriteLine("Argument required");
            string t = args[0];

            if (t == "CharacterDocument")
                TestCharacterDocument();
            else
                Debug.WriteLine("Invalid Argument");
        }

        private static void Init() {
            RenderProfile rp = GEnv.Options.CreateRenderProfile();
            rp.FontName = "ＭＳ ゴシック";
            rp.JapaneseFontName = "ＭＳ ゴシック";
            rp.BackColor = Color.White;
            rp.ForeColor = Color.Black;
            GEnv.DefaultRenderProfile = rp;
        }

        private static void TestCharacterDocument() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            CharacterDocument doc = CharacterDocument.LoadForTest(HOMEDIR + "Executable\\characterdocumenttest1.txt");
            CharacterDocumentViewerContainer f = new CharacterDocumentViewerContainer(doc);
            Application.Run(f);
        }

        private class CharacterDocumentViewerContainer : Form {
            private CharacterDocumentViewer _viewer;
            private Timer _caretTimer;
            public CharacterDocumentViewerContainer(CharacterDocument doc) {
                _viewer = new CharacterDocumentViewer();
                _viewer.SetContent(doc, GEnv.DefaultRenderProfile);
                _viewer.Dock = DockStyle.Fill;

                _caretTimer = new System.Windows.Forms.Timer();
                _caretTimer.Interval = Win32.GetCaretBlinkTime();
                _caretTimer.Tick += new EventHandler(this.OnCaretTimer);
                _caretTimer.Start();

                this.Controls.Add(_viewer);
                this.Size = new Size(300, 300);
            }
            private void OnCaretTimer(object sender, EventArgs args) {
                _viewer.CaretTick();
            }
        }
    }
}
#endif