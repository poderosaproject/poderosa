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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Granados.SSH2;
using Poderosa.Usability;

namespace Poderosa.Forms {
    internal partial class InputPassphraseDialog : Form {
        private AgentPrivateKey _key;

        public InputPassphraseDialog(AgentPrivateKey key) {
            _key = key;
            InitializeComponent();
            InitText();
        }
        private void InitText() {
            StringResource sr = SSHUtilPlugin.Instance.Strings;

            this.Text = sr.GetString("Form.InputPassphraseDialog.Text");
            _fileNameLabel.Text = sr.GetString("Form.InputPassphraseDialog._fileNameLabel");
            _fileNameBox.Text = _key.FileName;
            _passphraseLabel.Text = sr.GetString("Form.InputPassphraseDialog._passphraseLabel");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            _passphraseBox.Focus();
        }

        private void OnOK(object sender, EventArgs args) {
            this.DialogResult = DialogResult.None;
            try {
                SSH2UserAuthKey key = SSH2UserAuthKey.FromSECSHStyleFile(_key.FileName, _passphraseBox.Text);
                Debug.Assert(key != null); //例外でなければ成功
                _key.SetStatus(PrivateKeyStatus.OK, key);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
            }

        }
    }
}