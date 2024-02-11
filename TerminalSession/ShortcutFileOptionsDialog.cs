// Copyright 2023 The Poderosa Project.
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

using Poderosa.Serializing;
using System;
using System.Windows.Forms;

namespace Poderosa.Sessions {

    /// <summary>
    /// Dialog to set options for a shortcut file (*.gts)
    /// </summary>
    internal partial class ShortcutFileOptionsDialog : Form {

        /// <summary>
        /// Type of password serialization
        /// </summary>
        public PasswordSerialization PasswordSerialization {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ShortcutFileOptionsDialog() {
            InitializeComponent();
            Localize();        
        }

        private void Localize() {
            this.SuspendLayout();
            this.Text = TEnv.Strings.GetString("Form.ShortcutFileOptionsDialog.Caption");
            this._savePasswordCheckBox.Text = TEnv.Strings.GetString("Form.ShortcutFileOptionsDialog._savePasswordCheckBox");
            this._encryptPasswordRadioButton.Text = TEnv.Strings.GetString("Form.ShortcutFileOptionsDialog._encryptPasswordRadioButton");
            this._plaintextPasswordRadioButton.Text = TEnv.Strings.GetString("Form.ShortcutFileOptionsDialog._plaintextPasswordRadioButton");
            this._saveButton.Text = TEnv.Strings.GetString("Form.ShortcutFileOptionsDialog._saveButton");
            this._cancelButton.Text = TEnv.Strings.GetString("Common.Cancel");
            this.ResumeLayout();
        }

        /// <summary>
        /// Updates the status of controls related to the password
        /// </summary>
        private void updatePasswordSettingControlStatuses() {
            _encryptPasswordRadioButton.Enabled = _savePasswordCheckBox.Checked;
            _plaintextPasswordRadioButton.Enabled = _savePasswordCheckBox.Checked;
        }

        /// <summary>
        /// Updates the PasswordSerialization according to the input state
        /// </summary>
        private void updatePasswordSerialization() {
            if (_savePasswordCheckBox.Checked) {
                if (_encryptPasswordRadioButton.Checked) {
                    this.PasswordSerialization = PasswordSerialization.Encrypted;
                }
                else if (_plaintextPasswordRadioButton.Checked) {
                    this.PasswordSerialization = PasswordSerialization.Plaintext;
                }
                else {
                    this.PasswordSerialization = PasswordSerialization.None;
                }
            }
            else {
                this.PasswordSerialization = PasswordSerialization.None;
            }
        }

        private void SaveShortcutFileDialog_Load(object sender, EventArgs e) {
            if (this.PasswordSerialization == PasswordSerialization.Encrypted) {
                _savePasswordCheckBox.Checked = true;
                _encryptPasswordRadioButton.Checked = true;
            }
            else if (this.PasswordSerialization == PasswordSerialization.Plaintext) {
                _savePasswordCheckBox.Checked = true;
                _plaintextPasswordRadioButton.Checked = true;
            }
            else {
                _savePasswordCheckBox.Checked = false;
                _encryptPasswordRadioButton.Checked = true; // default
            }
            updatePasswordSettingControlStatuses();
        }

        private void _savePasswordCheckBox_CheckedChanged(object sender, EventArgs e) {
            updatePasswordSerialization();
            updatePasswordSettingControlStatuses();
        }

        private void _encryptPasswordRadioButton_CheckedChanged(object sender, EventArgs e) {
            updatePasswordSerialization();
        }

        private void _plaintextPasswordRadioButton_CheckedChanged(object sender, EventArgs e) {
            updatePasswordSerialization();
        }

        private void _cancelButton_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void _saveButton_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.OK;
            Close();
        }

    }
}
