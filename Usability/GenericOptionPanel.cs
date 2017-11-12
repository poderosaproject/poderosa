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

using Poderosa.Util;
using Poderosa.Usability;
using Poderosa.Preferences;
using Poderosa.UI;
using Poderosa.Terminal;
using Poderosa.Sessions;
using Microsoft.Win32;
using System.IO;

namespace Poderosa.Forms {
    internal class GenericOptionPanel : UserControl {
        private System.Windows.Forms.Label _languageLabel;
        private ComboBox _languageBox;
        private System.Windows.Forms.Label _MRUSizeLabel;
        private TextBox _MRUSize;
        private CheckBox _askCloseOnExit;
        private CheckBox _showToolBar;
        private Label _startupOptionLabel;
        private ComboBox _startupOptionBox;
        private GroupBox _fileAssociationGroup;
        private Button _makeFileAssociationGts;

        public GenericOptionPanel() {
            InitializeComponent();
            FillText();
        }
        private void InitializeComponent() {
            this._MRUSizeLabel = new System.Windows.Forms.Label();
            this._MRUSize = new TextBox();
            this._askCloseOnExit = new System.Windows.Forms.CheckBox();
            this._showToolBar = new CheckBox();
            this._languageLabel = new Label();
            this._languageBox = new ComboBox();
            this._startupOptionLabel = new Label();
            this._startupOptionBox = new ComboBox();
            this._fileAssociationGroup = new GroupBox();
            this._makeFileAssociationGts = new Button();

            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._MRUSizeLabel,
                this._MRUSize,
                this._askCloseOnExit,
                this._showToolBar,
                this._languageLabel,
                this._languageBox,
                this._startupOptionLabel,
                this._startupOptionBox,
                this._fileAssociationGroup,
            });
            // 
            // _languageLabel
            // 
            this._languageLabel.Location = new System.Drawing.Point(16, 8);
            this._languageLabel.Name = "_languageLabel";
            this._languageLabel.Size = new System.Drawing.Size(168, 24);
            this._languageLabel.TabIndex = 0;
            this._languageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _languageBox
            // 
            this._languageBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._languageBox.Location = new System.Drawing.Point(208, 8);
            this._languageBox.Name = "_languageBox";
            this._languageBox.Size = new System.Drawing.Size(216, 20);
            this._languageBox.TabIndex = 1;
            // 
            // _startupLabel
            // 
            this._startupOptionLabel.Location = new System.Drawing.Point(16, 32);
            this._startupOptionLabel.Name = "_startupLabel";
            this._startupOptionLabel.Size = new System.Drawing.Size(168, 24);
            this._startupOptionLabel.TabIndex = 2;
            this._startupOptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _startupBox
            // 
            this._startupOptionBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._startupOptionBox.Location = new System.Drawing.Point(208, 32);
            this._startupOptionBox.Name = "_startupBox";
            this._startupOptionBox.Size = new System.Drawing.Size(216, 20);
            this._startupOptionBox.TabIndex = 3;
            // 
            // _MRUSizeLabel
            // 
            this._MRUSizeLabel.Location = new System.Drawing.Point(16, 56);
            this._MRUSizeLabel.Name = "_MRUSizeLabel";
            this._MRUSizeLabel.Size = new System.Drawing.Size(272, 23);
            this._MRUSizeLabel.TabIndex = 4;
            this._MRUSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _MRUSize
            // 
            this._MRUSize.Location = new System.Drawing.Point(304, 56);
            this._MRUSize.MaxLength = 2;
            this._MRUSize.Name = "_MRUSize";
            this._MRUSize.Size = new System.Drawing.Size(120, 19);
            this._MRUSize.TabIndex = 5;
            this._MRUSize.Text = "";
            // 
            // _askCloseOnExit
            // 
            this._askCloseOnExit.Location = new System.Drawing.Point(24, 80);
            this._askCloseOnExit.Name = "_askCloseOnExit";
            this._askCloseOnExit.FlatStyle = FlatStyle.System;
            this._askCloseOnExit.Size = new System.Drawing.Size(296, 23);
            this._askCloseOnExit.TabIndex = 6;
            // 
            // _showToolBar
            // 
            this._showToolBar.Location = new System.Drawing.Point(24, 104);
            this._showToolBar.Name = "_showToolBar";
            this._showToolBar.FlatStyle = FlatStyle.System;
            this._showToolBar.Size = new System.Drawing.Size(296, 23);
            this._showToolBar.TabIndex = 7;

            this._fileAssociationGroup.Location = new System.Drawing.Point(16, 160);
            this._fileAssociationGroup.Size = new System.Drawing.Size(408, 54);
            this._fileAssociationGroup.FlatStyle = FlatStyle.System;
            this._fileAssociationGroup.Name = "_fileAssociationGroup";
            this._fileAssociationGroup.TabIndex = 8;

            this._makeFileAssociationGts.Location = new System.Drawing.Point(8, 20);
            this._makeFileAssociationGts.AutoSize = true;
            this._makeFileAssociationGts.FlatStyle = FlatStyle.System;
            this._makeFileAssociationGts.Name = "_makeFileAssociationGts";
            this._makeFileAssociationGts.TabIndex = 0;
            this._makeFileAssociationGts.Click += _makeFileAssociationGts_Click;

            this._fileAssociationGroup.Controls.Add(this._makeFileAssociationGts);

            this.BackColor = SystemColors.Window;
        }
        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._MRUSizeLabel.Text = sr.GetString("Form.OptionDialog._MRUSizeLabel");
            this._askCloseOnExit.Text = sr.GetString("Form.OptionDialog._askCloseOnExit");
            this._languageLabel.Text = sr.GetString("Form.OptionDialog._languageLabel");
            this._showToolBar.Text = sr.GetString("Form.OptionDialog._showToolBar");
            this._startupOptionLabel.Text = sr.GetString("Form.OptionDialog._startupOptionLabel");
            this._fileAssociationGroup.Text = sr.GetString("Form.OptionDialog._fileAssociationGroup");
            this._makeFileAssociationGts.Text = sr.GetString("Form.OptionDialog._makeFileAssociationGts");

            _languageBox.Items.AddRange(EnumListItem<Language>.GetListItems());
            _startupOptionBox.Items.AddRange(EnumListItem<StartupAction>.GetListItems());
        }
        public void InitUI(ITerminalSessionOptions terminalsession, IMRUOptions mru, ICoreServicePreference window, IStartupActionOptions startup) {
            _MRUSize.Text = mru.LimitCount.ToString();
            _askCloseOnExit.Checked = terminalsession.AskCloseOnExit;
            _showToolBar.Checked = window.ShowsToolBar;
            _languageBox.SelectedItem = window.Language;            // select EnumListItem<T> by T
            _startupOptionBox.SelectedItem = startup.StartupAction; // select EnumListItem<T> by T
        }
        public bool Commit(ITerminalSessionOptions terminalsession, IMRUOptions mru, ICoreServicePreference window, IStartupActionOptions startup) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            string itemname = null;
            bool successful = false;
            try {
                itemname = sr.GetString("Caption.OptionDialog.MRUCount");
                mru.LimitCount = Int32.Parse(_MRUSize.Text);
                terminalsession.AskCloseOnExit = _askCloseOnExit.Checked;
                window.ShowsToolBar = _showToolBar.Checked;

                window.Language = ((EnumListItem<Language>)_languageBox.SelectedItem).Value;
                startup.StartupAction = ((EnumListItem<StartupAction>)_startupOptionBox.SelectedItem).Value;
                successful = true;
            }
            catch (FormatException) {
                GUtil.Warning(this, String.Format(sr.GetString("Message.OptionDialog.InvalidItem"), itemname));
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
            }

            return successful;
        }

        private void _makeFileAssociationGts_Click(object sender, EventArgs e) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            if (GUtil.AskUserYesNo(this.ParentForm, sr.GetString("Form.OptionDialog.MakeFileAssociationGtsConfirmation")) != DialogResult.Yes) {
                return;
            }

            try {
                string exePath = Path.GetFullPath(Application.ExecutablePath);
                if (exePath.ToLower().EndsWith(".vshost.exe")) {
                    exePath = exePath.Substring(0, exePath.Length - 11) + ".exe";
                }
                if (!File.Exists(exePath)) {
                    throw new Exception("Cannot determine the path of an executable file.");
                }

                const string APPID = "Poderosa";

                // [HKEY_CURRENT_USER\Software\Classes\.gts]
                Registry.CurrentUser
                    .OpenSubKey(@"Software\Classes", true)
                    .CreateSubKey(".gts")
                    .SetValue("", APPID, RegistryValueKind.String);

                // [HKEY_CURRENT_USER\Software\Classes\<APPID>\shell\open\command]
                string command = "\"" + exePath + "\" -open \"%1\"";
                Registry.CurrentUser
                    .OpenSubKey(@"Software\Classes", true)
                    .CreateSubKey(APPID + @"\shell\open\command")
                    .SetValue("", command, RegistryValueKind.String);

                MessageBox.Show(this.ParentForm,
                    sr.GetString("Form.OptionDialog.FileAssociationChanged"),
                    "Poderosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) {
                RuntimeUtil.SilentReportException(ex);
                MessageBox.Show(this.ParentForm,
                    sr.GetString("Form.OptionDialog.FileAssociationFailed"),
                    "Poderosa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }


    internal class GenericOptionPanelExtension : OptionPanelExtensionBase {
        private GenericOptionPanel _panel;
        public GenericOptionPanelExtension()
            : base("Form.OptionDialog._genericPanel", 6) {
        }

        public override string[] PreferenceFolderIDsToEdit {
            get {
                return new string[] { "org.poderosa.terminalsessions", MRUPlugin.PLUGIN_ID, "org.poderosa.core.window", StartupActionPlugin.PLUGIN_ID };
            }
        }
        public override Control ContentPanel {
            get {
                return _panel;
            }
        }

        public override void InitiUI(IPreferenceFolder[] values) {
            if (_panel == null)
                _panel = new GenericOptionPanel();
            _panel.InitUI(
                (ITerminalSessionOptions)values[0].QueryAdapter(typeof(ITerminalSessionOptions)),
                (IMRUOptions)values[1].QueryAdapter(typeof(IMRUOptions)),
                (ICoreServicePreference)values[2].QueryAdapter(typeof(ICoreServicePreference)),
                (IStartupActionOptions)values[3].QueryAdapter(typeof(IStartupActionOptions)));
        }

        public override bool Commit(IPreferenceFolder[] values) {
            Debug.Assert(_panel != null);
            return _panel.Commit(
                (ITerminalSessionOptions)values[0].QueryAdapter(typeof(ITerminalSessionOptions)),
                (IMRUOptions)values[1].QueryAdapter(typeof(IMRUOptions)),
                (ICoreServicePreference)values[2].QueryAdapter(typeof(ICoreServicePreference)),
                (IStartupActionOptions)values[3].QueryAdapter(typeof(IStartupActionOptions)));
        }

        public override void Dispose() {
            if (_panel != null) {
                _panel.Dispose();
                _panel = null;
            }
        }
    }
}
