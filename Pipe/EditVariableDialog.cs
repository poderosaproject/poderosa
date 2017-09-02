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

namespace Poderosa.Pipe {

    /// <summary>
    /// Edit an environment variable
    /// </summary>
    internal partial class EditVariableDialog : Form {

        private string _name = null;
        private string _value = null;

        public string VariableName {
            get {
                return _name;
            }
        }

        public string VariableValue {
            get {
                return _value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EditVariableDialog() {
            InitializeComponent();

            SetupControls();
        }

        /// <summary>
        /// Set parameters
        /// </summary>
        /// <param name="name">Variable's name</param>
        /// <param name="value">Variable's value</param>
        public void ApplyParams(string name, string value) {
            _textBoxName.Text = name;
            _textBoxValue.Text = value;
        }

        private void SetupControls() {
            StringResource res = PipePlugin.Instance.Strings;

            this.Text = res.GetString("Form.EditVariableDialog.Title");

            _labelName.Text = res.GetString("Form.EditVariableDialog._labelName");
            _labelValue.Text = res.GetString("Form.EditVariableDialog._labelValue");

            _buttonOK.Text = res.GetString("Common.OK");
            _buttonCancel.Text = res.GetString("Common.Cancel");
        }

        private bool ValidateParams() {
            StringResource res = PipePlugin.Instance.Strings;

            _name = _textBoxName.Text;

            if (_name.Length == 0) {
                GUtil.Warning(this, res.GetString("Form.EditVariableDialog.Error.EnterName"));
                return false;
            }

            if (_name.IndexOf('=') != -1) {
                GUtil.Warning(this, res.GetString("Form.EditVariableDialog.Error.NameHasIllegalCharacter"));
                return false;
            }

            _value = _textBoxValue.Text;

            if (_value.Length == 0) {
                GUtil.Warning(this, res.GetString("Form.EditVariableDialog.Error.EnterValue"));
                return false;
            }

            return true;
        }

        private void _buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void _buttonOK_Click(object sender, EventArgs e) {
            bool isValid = ValidateParams();
            if (isValid) {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}