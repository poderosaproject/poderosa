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
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.UI;
using Poderosa.Usability;
using Poderosa.Preferences;
using Poderosa.Protocols;

using Granados;
using Granados.PKI;
using System.Collections.Generic;

namespace Poderosa.Forms {
    internal class SSHOptionPanel : UserControl {
        private System.Windows.Forms.GroupBox _cipherOrderGroup;
        private System.Windows.Forms.ListBox _cipherOrderList;
        private System.Windows.Forms.Button _algorithmOrderUp;
        private System.Windows.Forms.Button _algorithmOrderDown;
        private System.Windows.Forms.GroupBox _ssh2OptionGroup;
        private System.Windows.Forms.Label _hostKeyLabel;
        private System.Windows.Forms.Label _windowSizeLabel;
        private TextBox _windowSizeBox;
        private System.Windows.Forms.GroupBox _sshMiscGroup;
        private CheckBox _sshCheckMAC;
        private ListBox _hostKeyAlgorithmOrderList;
        private Button _hostKeyAlgorithmOrderUp;
        private Button _hostKeyAlgorithmOrderDown;
        private CheckBox _sshEventLog;

        public SSHOptionPanel() {
            InitializeComponent();
            FillText();
        }
        private void InitializeComponent() {
            this._cipherOrderGroup = new System.Windows.Forms.GroupBox();
            this._cipherOrderList = new System.Windows.Forms.ListBox();
            this._algorithmOrderUp = new System.Windows.Forms.Button();
            this._algorithmOrderDown = new System.Windows.Forms.Button();
            this._ssh2OptionGroup = new System.Windows.Forms.GroupBox();
            this._hostKeyAlgorithmOrderList = new System.Windows.Forms.ListBox();
            this._hostKeyAlgorithmOrderUp = new System.Windows.Forms.Button();
            this._hostKeyAlgorithmOrderDown = new System.Windows.Forms.Button();
            this._hostKeyLabel = new System.Windows.Forms.Label();
            this._windowSizeLabel = new System.Windows.Forms.Label();
            this._windowSizeBox = new System.Windows.Forms.TextBox();
            this._sshMiscGroup = new System.Windows.Forms.GroupBox();
            this._sshCheckMAC = new System.Windows.Forms.CheckBox();
            this._sshEventLog = new System.Windows.Forms.CheckBox();
            this._cipherOrderGroup.SuspendLayout();
            this._ssh2OptionGroup.SuspendLayout();
            this._sshMiscGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _cipherOrderGroup
            // 
            this._cipherOrderGroup.Controls.Add(this._cipherOrderList);
            this._cipherOrderGroup.Controls.Add(this._algorithmOrderUp);
            this._cipherOrderGroup.Controls.Add(this._algorithmOrderDown);
            this._cipherOrderGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cipherOrderGroup.Location = new System.Drawing.Point(8, 8);
            this._cipherOrderGroup.Name = "_cipherOrderGroup";
            this._cipherOrderGroup.Size = new System.Drawing.Size(416, 80);
            this._cipherOrderGroup.TabIndex = 0;
            this._cipherOrderGroup.TabStop = false;
            // 
            // _cipherOrderList
            // 
            this._cipherOrderList.ItemHeight = 12;
            this._cipherOrderList.Location = new System.Drawing.Point(8, 16);
            this._cipherOrderList.Name = "_cipherOrderList";
            this._cipherOrderList.Size = new System.Drawing.Size(208, 52);
            this._cipherOrderList.TabIndex = 1;
            // 
            // _algorithmOrderUp
            // 
            this._algorithmOrderUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._algorithmOrderUp.Location = new System.Drawing.Point(232, 16);
            this._algorithmOrderUp.Name = "_algorithmOrderUp";
            this._algorithmOrderUp.Size = new System.Drawing.Size(75, 23);
            this._algorithmOrderUp.TabIndex = 2;
            this._algorithmOrderUp.Click += new System.EventHandler(this.OnCipherAlgorithmOrderUp);
            // 
            // _algorithmOrderDown
            // 
            this._algorithmOrderDown.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._algorithmOrderDown.Location = new System.Drawing.Point(232, 48);
            this._algorithmOrderDown.Name = "_algorithmOrderDown";
            this._algorithmOrderDown.Size = new System.Drawing.Size(75, 23);
            this._algorithmOrderDown.TabIndex = 3;
            this._algorithmOrderDown.Click += new System.EventHandler(this.OnCipherAlgorithmOrderDown);
            // 
            // _ssh2OptionGroup
            // 
            this._ssh2OptionGroup.Controls.Add(this._hostKeyAlgorithmOrderList);
            this._ssh2OptionGroup.Controls.Add(this._hostKeyAlgorithmOrderUp);
            this._ssh2OptionGroup.Controls.Add(this._hostKeyAlgorithmOrderDown);
            this._ssh2OptionGroup.Controls.Add(this._hostKeyLabel);
            this._ssh2OptionGroup.Controls.Add(this._windowSizeLabel);
            this._ssh2OptionGroup.Controls.Add(this._windowSizeBox);
            this._ssh2OptionGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._ssh2OptionGroup.Location = new System.Drawing.Point(8, 96);
            this._ssh2OptionGroup.Name = "_ssh2OptionGroup";
            this._ssh2OptionGroup.Size = new System.Drawing.Size(416, 139);
            this._ssh2OptionGroup.TabIndex = 4;
            this._ssh2OptionGroup.TabStop = false;
            // 
            // _hostKeyAlgorithmOrderList
            // 
            this._hostKeyAlgorithmOrderList.ItemHeight = 12;
            this._hostKeyAlgorithmOrderList.Location = new System.Drawing.Point(8, 40);
            this._hostKeyAlgorithmOrderList.Name = "_hostKeyAlgorithmOrderList";
            this._hostKeyAlgorithmOrderList.Size = new System.Drawing.Size(208, 52);
            this._hostKeyAlgorithmOrderList.TabIndex = 2;
            // 
            // _hostKeyAlgorithmOrderUp
            // 
            this._hostKeyAlgorithmOrderUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._hostKeyAlgorithmOrderUp.Location = new System.Drawing.Point(232, 40);
            this._hostKeyAlgorithmOrderUp.Name = "_hostKeyAlgorithmOrderUp";
            this._hostKeyAlgorithmOrderUp.Size = new System.Drawing.Size(75, 23);
            this._hostKeyAlgorithmOrderUp.TabIndex = 3;
            this._hostKeyAlgorithmOrderUp.Click += new System.EventHandler(this.OnHostKeyAlgorithmOrderUp);
            // 
            // _hostKeyAlgorithmOrderDown
            // 
            this._hostKeyAlgorithmOrderDown.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._hostKeyAlgorithmOrderDown.Location = new System.Drawing.Point(232, 72);
            this._hostKeyAlgorithmOrderDown.Name = "_hostKeyAlgorithmOrderDown";
            this._hostKeyAlgorithmOrderDown.Size = new System.Drawing.Size(75, 23);
            this._hostKeyAlgorithmOrderDown.TabIndex = 4;
            this._hostKeyAlgorithmOrderDown.Click += new System.EventHandler(this.OnHostKeyAlgorithmOrderDown);
            // 
            // _hostKeyLabel
            // 
            this._hostKeyLabel.Location = new System.Drawing.Point(8, 16);
            this._hostKeyLabel.Name = "_hostKeyLabel";
            this._hostKeyLabel.Size = new System.Drawing.Size(200, 23);
            this._hostKeyLabel.TabIndex = 1;
            this._hostKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _windowSizeLabel
            // 
            this._windowSizeLabel.Location = new System.Drawing.Point(8, 110);
            this._windowSizeLabel.Name = "_windowSizeLabel";
            this._windowSizeLabel.Size = new System.Drawing.Size(192, 23);
            this._windowSizeLabel.TabIndex = 7;
            this._windowSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _windowSizeBox
            // 
            this._windowSizeBox.Location = new System.Drawing.Point(206, 110);
            this._windowSizeBox.MaxLength = 10;
            this._windowSizeBox.Name = "_windowSizeBox";
            this._windowSizeBox.Size = new System.Drawing.Size(120, 19);
            this._windowSizeBox.TabIndex = 8;
            this._windowSizeBox.Text = "0";
            // 
            // _sshMiscGroup
            // 
            this._sshMiscGroup.Controls.Add(this._sshCheckMAC);
            this._sshMiscGroup.Controls.Add(this._sshEventLog);
            this._sshMiscGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._sshMiscGroup.Location = new System.Drawing.Point(8, 243);
            this._sshMiscGroup.Name = "_sshMiscGroup";
            this._sshMiscGroup.Size = new System.Drawing.Size(416, 83);
            this._sshMiscGroup.TabIndex = 9;
            this._sshMiscGroup.TabStop = false;
            // 
            // _sshCheckMAC
            // 
            this._sshCheckMAC.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._sshCheckMAC.Location = new System.Drawing.Point(8, 12);
            this._sshCheckMAC.Name = "_sshCheckMAC";
            this._sshCheckMAC.Size = new System.Drawing.Size(400, 37);
            this._sshCheckMAC.TabIndex = 12;
            // 
            // _sshEventLog
            // 
            this._sshEventLog.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._sshEventLog.Location = new System.Drawing.Point(8, 48);
            this._sshEventLog.Name = "_sshEventLog";
            this._sshEventLog.Size = new System.Drawing.Size(400, 23);
            this._sshEventLog.TabIndex = 13;
            // 
            // SSHOptionPanel
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this._cipherOrderGroup);
            this.Controls.Add(this._ssh2OptionGroup);
            this.Controls.Add(this._sshMiscGroup);
            this.Name = "SSHOptionPanel";
            this.Size = new System.Drawing.Size(381, 350);
            this._cipherOrderGroup.ResumeLayout(false);
            this._ssh2OptionGroup.ResumeLayout(false);
            this._ssh2OptionGroup.PerformLayout();
            this._sshMiscGroup.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// List item object for displaying the algorithm name instead of the enum name.
        /// </summary>
        private class PublicKeyAlgorithmListItem {
            public readonly string AlgorithmName;
            public readonly PublicKeyAlgorithm Value;

            public PublicKeyAlgorithmListItem(PublicKeyAlgorithm value) {
                this.Value = value;
                this.AlgorithmName = value.GetAlgorithmName();
            }

            public override string ToString() {
                return AlgorithmName;
            }
        }

        /// <summary>
        /// List item object for displaying the algorithm name instead of the enum name.
        /// </summary>
        private class CipherAlgorithmListItem {
            public readonly string AlgorithmName;
            public readonly CipherAlgorithm Value;

            public CipherAlgorithmListItem(CipherAlgorithm value) {
                this.Value = value;
                this.AlgorithmName = value.GetAlgorithmName();
            }

            public override string ToString() {
                return AlgorithmName;
            }
        }

        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._cipherOrderGroup.Text = sr.GetString("Form.OptionDialog._cipherOrderGroup");
            this._algorithmOrderUp.Text = sr.GetString("Form.OptionDialog._algorithmOrderUp");
            this._algorithmOrderDown.Text = sr.GetString("Form.OptionDialog._algorithmOrderDown");
            this._ssh2OptionGroup.Text = sr.GetString("Form.OptionDialog._ssh2OptionGroup");
            this._hostKeyLabel.Text = sr.GetString("Form.OptionDialog._hostKeyLabel");
            this._hostKeyAlgorithmOrderUp.Text = sr.GetString("Form.OptionDialog._hostKeyAlgorithmOrderUp");
            this._hostKeyAlgorithmOrderDown.Text = sr.GetString("Form.OptionDialog._hostKeyAlgorithmOrderDown");
            this._windowSizeLabel.Text = sr.GetString("Form.OptionDialog._windowSizeLabel");
            this._sshMiscGroup.Text = sr.GetString("Form.OptionDialog._sshMiscGroup");
            this._sshCheckMAC.Text = sr.GetString("Form.OptionDialog._sshCheckMAC");
            this._sshEventLog.Text = sr.GetString("Form.OptionDialog._sshEventLog");
        }
        public void InitUI(IProtocolOptions options, IKeyAgentOptions agent) {
            _cipherOrderList.Items.Clear();
            _cipherOrderList.Items.AddRange(ToCipherAlgorithmListItems(options.CipherAlgorithmOrder));
            _hostKeyAlgorithmOrderList.Items.Clear();
            _hostKeyAlgorithmOrderList.Items.AddRange(ToPublicKeyAlgorithmListItems(options.HostKeyAlgorithmOrder));
            _windowSizeBox.Text = options.SSHWindowSize.ToString();
            _sshCheckMAC.Checked = options.SSHCheckMAC;
            _sshEventLog.Checked = options.LogSSHEvents;
        }

        private PublicKeyAlgorithmListItem[] ToPublicKeyAlgorithmListItems(string[] algorithms) {
            var list = new List<PublicKeyAlgorithmListItem>(algorithms.Length);
            foreach (var a in algorithms) {
                PublicKeyAlgorithm val;
                if (Enum.TryParse(a, out val) && Enum.IsDefined(typeof(PublicKeyAlgorithm), val)) {
                    list.Add(new PublicKeyAlgorithmListItem(val));
                }
            }
            return list.ToArray();
        }

        private CipherAlgorithmListItem[] ToCipherAlgorithmListItems(string[] algorithms) {
            var list = new List<CipherAlgorithmListItem>(algorithms.Length);
            foreach (var a in algorithms) {
                CipherAlgorithm val;
                if (Enum.TryParse(a, out val) && Enum.IsDefined(typeof(CipherAlgorithm), val)) {
                    list.Add(new CipherAlgorithmListItem(val));
                }
            }
            return list.ToArray();
        }

        public bool Commit(IProtocolOptions options, IKeyAgentOptions agent) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            //暗号アルゴリズム順序はoptionsを直接いじっているのでここでは何もしなくてよい
            try {
                options.HostKeyAlgorithmOrder = GetHostKeyAlgorithmOrder();

                try {
                    options.SSHWindowSize = Int32.Parse(_windowSizeBox.Text);
                }
                catch (FormatException) {
                    GUtil.Warning(this, sr.GetString("Message.OptionDialog.InvalidWindowSize"));
                    return false;
                }

                options.SSHCheckMAC = _sshCheckMAC.Checked;
                options.CipherAlgorithmOrder = GetCipherAlgorithmOrder();
                options.LogSSHEvents = _sshEventLog.Checked;

                return true;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                return false;
            }
        }

        private string[] GetHostKeyAlgorithmOrder() {
            var algorithms = new List<string>();
            foreach (PublicKeyAlgorithmListItem item in _hostKeyAlgorithmOrderList.Items) {
                algorithms.Add(item.Value.ToString());
            }
            return algorithms.ToArray();
        }

        private string[] GetCipherAlgorithmOrder() {
            var algorithms = new List<string>();
            foreach (CipherAlgorithmListItem item in _cipherOrderList.Items) {
                algorithms.Add(item.Value.ToString());
            }
            return algorithms.ToArray();
        }

        //SSHオプション関係
        private void OnCipherAlgorithmOrderUp(object sender, EventArgs args) {
            int i = _cipherOrderList.SelectedIndex;
            if (i == -1 || i == 0) {
                return;
            }

            object item = _cipherOrderList.SelectedItem;
            _cipherOrderList.Items.RemoveAt(i);
            _cipherOrderList.Items.Insert(i - 1, item);

            _cipherOrderList.SelectedIndex = i - 1;
        }

        private void OnCipherAlgorithmOrderDown(object sender, EventArgs args) {
            int i = _cipherOrderList.SelectedIndex;
            if (i == -1 || i == _cipherOrderList.Items.Count - 1) {
                return;
            }

            object item = _cipherOrderList.SelectedItem;
            _cipherOrderList.Items.RemoveAt(i);
            _cipherOrderList.Items.Insert(i + 1, item);

            _cipherOrderList.SelectedIndex = i + 1;
        }

        private void OnHostKeyAlgorithmOrderUp(object sender, EventArgs args) {
            int i = _hostKeyAlgorithmOrderList.SelectedIndex;
            if (i == -1 || i == 0) {
                return;
            }

            object item = _hostKeyAlgorithmOrderList.SelectedItem;
            _hostKeyAlgorithmOrderList.Items.RemoveAt(i);
            _hostKeyAlgorithmOrderList.Items.Insert(i - 1, item);

            _hostKeyAlgorithmOrderList.SelectedIndex = i - 1;
        }

        private void OnHostKeyAlgorithmOrderDown(object sender, EventArgs args) {
            int i = _hostKeyAlgorithmOrderList.SelectedIndex;
            if (i == -1 || i == _hostKeyAlgorithmOrderList.Items.Count - 1) {
                return;
            }

            object item = _hostKeyAlgorithmOrderList.SelectedItem;
            _hostKeyAlgorithmOrderList.Items.RemoveAt(i);
            _hostKeyAlgorithmOrderList.Items.Insert(i + 1, item);

            _hostKeyAlgorithmOrderList.SelectedIndex = i + 1;
        }
    }


    internal class SSHOptionPanelExtension : OptionPanelExtensionBase {
        private SSHOptionPanel _panel;

        public SSHOptionPanelExtension()
            : base("Form.OptionDialog._sshPanel", 4) {
        }


        public override string[] PreferenceFolderIDsToEdit {
            get {
                return new string[] { "org.poderosa.protocols", "org.poderosa.usability.ssh-keyagent" };
            }
        }

        public override Control ContentPanel {
            get {
                return _panel;
            }
        }


        public override void InitiUI(IPreferenceFolder[] values) {
            if (_panel == null)
                _panel = new SSHOptionPanel();
            _panel.InitUI((IProtocolOptions)values[0].QueryAdapter(typeof(IProtocolOptions)), (IKeyAgentOptions)values[1].QueryAdapter(typeof(IKeyAgentOptions)));
        }

        public override bool Commit(IPreferenceFolder[] values) {
            Debug.Assert(_panel != null);
            return _panel.Commit((IProtocolOptions)values[0].QueryAdapter(typeof(IProtocolOptions)), (IKeyAgentOptions)values[1].QueryAdapter(typeof(IKeyAgentOptions)));
        }

        public override void Dispose() {
            if (_panel != null) {
                _panel.Dispose();
                _panel = null;
            }
        }
    }
}
