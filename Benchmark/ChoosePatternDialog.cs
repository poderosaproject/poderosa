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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Poderosa.Benchmark {

    internal partial class ChoosePatternDialog : Form {

        private XTermBenchmarkPattern _pattern;

        public XTermBenchmarkPattern Pattern {
            get {
                return _pattern;
            }
        }

        public ChoosePatternDialog() {
            InitializeComponent();

            this.comboBoxPattern.Items.Clear();
            foreach (XTermBenchmarkPattern pattern in Enum.GetValues(typeof(XTermBenchmarkPattern))) {
                this.comboBoxPattern.Items.Add(pattern);
            }
            this.comboBoxPattern.SelectedIndex = 0;
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this._pattern = (XTermBenchmarkPattern)this.comboBoxPattern.SelectedItem;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}