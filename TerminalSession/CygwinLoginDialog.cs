// Copyright 2004-2025 The Poderosa Project.
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
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

using Poderosa.Plugins;
using Poderosa.Util;
using Poderosa.Terminal;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Forms;
using Poderosa.View;

namespace Poderosa.Sessions {

    internal class LocalShellLoginDialog : LoginDialogBase {
        private ICygwinParameter _param;

        private Label _logTypeLabel;
        private ComboBox _logTypeBox;
        private Label _logFileLabel;
        private ComboBox _logFileBox;
        private Label _terminalTypeLabel;
        private ComboBox _terminalTypeBox;
        private Label _cygwinDirLabel;
        private TextBox _cygwinDirBox;
        private Button _selectCygwinDirButton;
        private Button _selectlogButton;
        private GroupBox _advancedOptionGroup;
        private Label _homeDirectoryLabel;
        private Label _shellLabel;
        private TextBox _homeDirectoryBox;
        private TextBox _shellBox;
        private Label _encodingLabel;
        private ComboBox _encodingBox;
        private Label _autoExecMacroPathLabel;
        private TextBox _autoExecMacroPathBox;
        private Button _selectAutoExecMacroButton;
        private CheckBox _cygwin32Check;
        private Label _homeDirectoryExampleLabel;
        private Label _shellExampleLabel;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public LocalShellLoginDialog(IPoderosaMainWindow parentWindow)
            : base(parentWindow) {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();

            //
            // TODO: InitializeComponent 呼び出しの後に、コンストラクタ コードを追加してください。
            //
            this._loginButton.Text = TEnv.Strings.GetString("Common.OK");
            this._cancelButton.Text = TEnv.Strings.GetString("Common.Cancel");
            this._homeDirectoryLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._homeDirectoryLabel");
            this._advancedOptionGroup.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._advancedOptionGroup");
            this._shellLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._shellLabel");
            this._logFileLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._logFileLabel");
            this._logTypeLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._logTypeLabel");
            this._encodingLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._encodingLabel");
            this._terminalTypeLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._terminalTypeLabel");
            this._cygwinDirLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._cygwinDirLabel");
            this._autoExecMacroPathLabel.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._autoExecMacroPathLabel");
            this._cygwin32Check.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog._cygwin32Check");

            this._logTypeBox.Items.AddRange(EnumListItem<LogType>.GetListItems());
            this._encodingBox.Items.AddRange(EnumListItem<EncodingType>.GetListItems());
            this._terminalTypeBox.Items.AddRange(EnumListItem<TerminalType>.GetListItems());

            this._homeDirectoryExampleLabel.Font = new Font(this._homeDirectoryExampleLabel.Font.Name, 8, this._homeDirectoryExampleLabel.Font.Style);
            this._homeDirectoryExampleLabel.Text = String.Format(TEnv.Strings.GetString("Form.CygwinLoginDialog._homeDirectoryExampleLabel"), "/home/" + Environment.UserName);
            this._shellExampleLabel.Font = new Font(this._shellExampleLabel.Font.Name, 8, this._shellExampleLabel.Font.Style);
            this._shellExampleLabel.Text = String.Format(TEnv.Strings.GetString("Form.CygwinLoginDialog._shellExampleLabel"), "/bin/bash -i -l");

            //作っておく
            AdjustLoginDialogUISupport("org.poderosa.terminalsessions.loginDialogUISupport", "cygwinLoginDialogUISupport");
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._logTypeBox = new System.Windows.Forms.ComboBox();
            this._logTypeLabel = new System.Windows.Forms.Label();
            this._logFileBox = new System.Windows.Forms.ComboBox();
            this._logFileLabel = new System.Windows.Forms.Label();
            this._selectlogButton = new System.Windows.Forms.Button();
            this._terminalTypeLabel = new System.Windows.Forms.Label();
            this._terminalTypeBox = new System.Windows.Forms.ComboBox();
            this._cygwinDirLabel = new System.Windows.Forms.Label();
            this._cygwinDirBox = new System.Windows.Forms.TextBox();
            this._selectCygwinDirButton = new System.Windows.Forms.Button();
            this._advancedOptionGroup = new System.Windows.Forms.GroupBox();
            this._homeDirectoryLabel = new System.Windows.Forms.Label();
            this._homeDirectoryExampleLabel = new System.Windows.Forms.Label();
            this._homeDirectoryBox = new System.Windows.Forms.TextBox();
            this._shellLabel = new System.Windows.Forms.Label();
            this._shellExampleLabel = new System.Windows.Forms.Label();
            this._shellBox = new System.Windows.Forms.TextBox();
            this._encodingLabel = new System.Windows.Forms.Label();
            this._encodingBox = new System.Windows.Forms.ComboBox();
            this._autoExecMacroPathLabel = new System.Windows.Forms.Label();
            this._autoExecMacroPathBox = new System.Windows.Forms.TextBox();
            this._selectAutoExecMacroButton = new System.Windows.Forms.Button();
            this._cygwin32Check = new System.Windows.Forms.CheckBox();
            this._advancedOptionGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _loginButton
            // 
            this._loginButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._loginButton.Location = new System.Drawing.Point(144, 335);
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.Location = new System.Drawing.Point(232, 335);
            // 
            // _logTypeBox
            // 
            this._logTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._logTypeBox.Location = new System.Drawing.Point(114, 8);
            this._logTypeBox.Name = "_logTypeBox";
            this._logTypeBox.Size = new System.Drawing.Size(160, 20);
            this._logTypeBox.TabIndex = 3;
            this._logTypeBox.SelectionChangeCommitted += new System.EventHandler(this.OnLogTypeChanged);
            // 
            // _logTypeLabel
            // 
            this._logTypeLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._logTypeLabel.Location = new System.Drawing.Point(8, 8);
            this._logTypeLabel.Name = "_logTypeLabel";
            this._logTypeLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._logTypeLabel.Size = new System.Drawing.Size(100, 19);
            this._logTypeLabel.TabIndex = 2;
            this._logTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _logFileBox
            // 
            this._logFileBox.Location = new System.Drawing.Point(114, 32);
            this._logFileBox.Name = "_logFileBox";
            this._logFileBox.Size = new System.Drawing.Size(160, 20);
            this._logFileBox.TabIndex = 5;
            // 
            // _logFileLabel
            // 
            this._logFileLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._logFileLabel.Location = new System.Drawing.Point(8, 32);
            this._logFileLabel.Name = "_logFileLabel";
            this._logFileLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._logFileLabel.Size = new System.Drawing.Size(100, 19);
            this._logFileLabel.TabIndex = 4;
            this._logFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _selectlogButton
            // 
            this._selectlogButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectlogButton.ImageIndex = 0;
            this._selectlogButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._selectlogButton.Location = new System.Drawing.Point(282, 32);
            this._selectlogButton.Name = "_selectlogButton";
            this._selectlogButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._selectlogButton.Size = new System.Drawing.Size(19, 19);
            this._selectlogButton.TabIndex = 6;
            this._selectlogButton.Text = "...";
            this._selectlogButton.Click += new System.EventHandler(this.SelectLog);
            // 
            // _terminalTypeLabel
            // 
            this._terminalTypeLabel.Location = new System.Drawing.Point(8, 83);
            this._terminalTypeLabel.Name = "_terminalTypeLabel";
            this._terminalTypeLabel.Size = new System.Drawing.Size(100, 19);
            this._terminalTypeLabel.TabIndex = 9;
            // 
            // _terminalTypeBox
            // 
            this._terminalTypeBox.Location = new System.Drawing.Point(114, 80);
            this._terminalTypeBox.Name = "_terminalTypeBox";
            this._terminalTypeBox.Size = new System.Drawing.Size(96, 20);
            this._terminalTypeBox.TabIndex = 10;
            // 
            // _cygwinDirLabel
            // 
            this._cygwinDirLabel.Location = new System.Drawing.Point(8, 136);
            this._cygwinDirLabel.Name = "_cygwinDirLabel";
            this._cygwinDirLabel.Size = new System.Drawing.Size(256, 16);
            this._cygwinDirLabel.TabIndex = 12;
            // 
            // _cygwinDirBox
            // 
            this._cygwinDirBox.Location = new System.Drawing.Point(8, 155);
            this._cygwinDirBox.Name = "_cygwinDirBox";
            this._cygwinDirBox.Size = new System.Drawing.Size(273, 19);
            this._cygwinDirBox.TabIndex = 13;
            // 
            // _selectCygwinDirButton
            // 
            this._selectCygwinDirButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectCygwinDirButton.ImageIndex = 0;
            this._selectCygwinDirButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._selectCygwinDirButton.Location = new System.Drawing.Point(287, 155);
            this._selectCygwinDirButton.Name = "_selectCygwinDirButton";
            this._selectCygwinDirButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._selectCygwinDirButton.Size = new System.Drawing.Size(19, 19);
            this._selectCygwinDirButton.TabIndex = 14;
            this._selectCygwinDirButton.Text = "...";
            this._selectCygwinDirButton.Click += new System.EventHandler(this.SelectCygwinDir);
            // 
            // _advancedOptionGroup
            // 
            this._advancedOptionGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._advancedOptionGroup.Controls.Add(this._homeDirectoryLabel);
            this._advancedOptionGroup.Controls.Add(this._homeDirectoryExampleLabel);
            this._advancedOptionGroup.Controls.Add(this._homeDirectoryBox);
            this._advancedOptionGroup.Controls.Add(this._shellLabel);
            this._advancedOptionGroup.Controls.Add(this._shellExampleLabel);
            this._advancedOptionGroup.Controls.Add(this._shellBox);
            this._advancedOptionGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._advancedOptionGroup.Location = new System.Drawing.Point(8, 222);
            this._advancedOptionGroup.Name = "_advancedOptionGroup";
            this._advancedOptionGroup.Size = new System.Drawing.Size(300, 105);
            this._advancedOptionGroup.TabIndex = 19;
            this._advancedOptionGroup.TabStop = false;
            // 
            // _homeDirectoryLabel
            // 
            this._homeDirectoryLabel.Location = new System.Drawing.Point(8, 20);
            this._homeDirectoryLabel.Name = "_homeDirectoryLabel";
            this._homeDirectoryLabel.Size = new System.Drawing.Size(112, 23);
            this._homeDirectoryLabel.TabIndex = 0;
            this._homeDirectoryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _homeDirectoryExampleLabel
            // 
            this._homeDirectoryExampleLabel.Location = new System.Drawing.Point(120, 40);
            this._homeDirectoryExampleLabel.Name = "_homeDirectoryExampleLabel";
            this._homeDirectoryExampleLabel.Size = new System.Drawing.Size(172, 16);
            this._homeDirectoryExampleLabel.TabIndex = 2;
            this._homeDirectoryExampleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _homeDirectoryBox
            // 
            this._homeDirectoryBox.Location = new System.Drawing.Point(120, 20);
            this._homeDirectoryBox.Name = "_homeDirectoryBox";
            this._homeDirectoryBox.Size = new System.Drawing.Size(172, 19);
            this._homeDirectoryBox.TabIndex = 1;
            // 
            // _shellLabel
            // 
            this._shellLabel.Location = new System.Drawing.Point(8, 62);
            this._shellLabel.Name = "_shellLabel";
            this._shellLabel.Size = new System.Drawing.Size(112, 23);
            this._shellLabel.TabIndex = 3;
            this._shellLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _shellExample
            // 
            this._shellExampleLabel.Location = new System.Drawing.Point(120, 82);
            this._shellExampleLabel.Name = "_shellExample";
            this._shellExampleLabel.Size = new System.Drawing.Size(172, 16);
            this._shellExampleLabel.TabIndex = 5;
            this._shellExampleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _shellBox
            // 
            this._shellBox.Location = new System.Drawing.Point(120, 62);
            this._shellBox.Name = "_shellBox";
            this._shellBox.Size = new System.Drawing.Size(172, 19);
            this._shellBox.TabIndex = 4;
            // 
            // _encodingLabel
            // 
            this._encodingLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._encodingLabel.Location = new System.Drawing.Point(8, 56);
            this._encodingLabel.Name = "_encodingLabel";
            this._encodingLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._encodingLabel.Size = new System.Drawing.Size(100, 19);
            this._encodingLabel.TabIndex = 7;
            this._encodingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _encodingBox
            // 
            this._encodingBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._encodingBox.Location = new System.Drawing.Point(114, 56);
            this._encodingBox.Name = "_encodingBox";
            this._encodingBox.Size = new System.Drawing.Size(96, 20);
            this._encodingBox.TabIndex = 8;
            // 
            // _autoExecMacroPathLabel
            // 
            this._autoExecMacroPathLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._autoExecMacroPathLabel.Location = new System.Drawing.Point(8, 186);
            this._autoExecMacroPathLabel.Name = "_autoExecMacroPathLabel";
            this._autoExecMacroPathLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._autoExecMacroPathLabel.Size = new System.Drawing.Size(100, 16);
            this._autoExecMacroPathLabel.TabIndex = 15;
            this._autoExecMacroPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _autoExecMacroPathBox
            // 
            this._autoExecMacroPathBox.Location = new System.Drawing.Point(112, 185);
            this._autoExecMacroPathBox.Name = "_autoExecMacroPathBox";
            this._autoExecMacroPathBox.Size = new System.Drawing.Size(169, 19);
            this._autoExecMacroPathBox.TabIndex = 16;
            // 
            // _selectAutoExecMacroButton
            // 
            this._selectAutoExecMacroButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectAutoExecMacroButton.ImageIndex = 0;
            this._selectAutoExecMacroButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._selectAutoExecMacroButton.Location = new System.Drawing.Point(287, 185);
            this._selectAutoExecMacroButton.Name = "_selectAutoExecMacroButton";
            this._selectAutoExecMacroButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._selectAutoExecMacroButton.Size = new System.Drawing.Size(19, 19);
            this._selectAutoExecMacroButton.TabIndex = 17;
            this._selectAutoExecMacroButton.Text = "...";
            this._selectAutoExecMacroButton.Click += new System.EventHandler(this._selectAutoExecMacroButton_Click);
            // 
            // _cygwin32Check
            // 
            this._cygwin32Check.AutoSize = true;
            this._cygwin32Check.Location = new System.Drawing.Point(9, 111);
            this._cygwin32Check.Name = "_cygwin32Check";
            this._cygwin32Check.Size = new System.Drawing.Size(15, 14);
            this._cygwin32Check.TabIndex = 11;
            this._cygwin32Check.UseVisualStyleBackColor = true;
            // 
            // LocalShellLoginDialog
            // 
            this.AcceptButton = this._loginButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(314, 367);
            this.Controls.Add(this._cygwin32Check);
            this.Controls.Add(this._autoExecMacroPathLabel);
            this.Controls.Add(this._autoExecMacroPathBox);
            this.Controls.Add(this._selectAutoExecMacroButton);
            this.Controls.Add(this._encodingLabel);
            this.Controls.Add(this._encodingBox);
            this.Controls.Add(this._logTypeLabel);
            this.Controls.Add(this._logTypeBox);
            this.Controls.Add(this._logFileLabel);
            this.Controls.Add(this._logFileBox);
            this.Controls.Add(this._selectlogButton);
            this.Controls.Add(this._advancedOptionGroup);
            this.Controls.Add(this._cygwinDirLabel);
            this.Controls.Add(this._terminalTypeLabel);
            this.Controls.Add(this._selectCygwinDirButton);
            this.Controls.Add(this._cygwinDirBox);
            this.Controls.Add(this._terminalTypeBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LocalShellLoginDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Controls.SetChildIndex(this._terminalTypeBox, 0);
            this.Controls.SetChildIndex(this._cygwinDirBox, 0);
            this.Controls.SetChildIndex(this._selectCygwinDirButton, 0);
            this.Controls.SetChildIndex(this._terminalTypeLabel, 0);
            this.Controls.SetChildIndex(this._cygwinDirLabel, 0);
            this.Controls.SetChildIndex(this._advancedOptionGroup, 0);
            this.Controls.SetChildIndex(this._selectlogButton, 0);
            this.Controls.SetChildIndex(this._logFileBox, 0);
            this.Controls.SetChildIndex(this._logFileLabel, 0);
            this.Controls.SetChildIndex(this._logTypeBox, 0);
            this.Controls.SetChildIndex(this._logTypeLabel, 0);
            this.Controls.SetChildIndex(this._loginButton, 0);
            this.Controls.SetChildIndex(this._cancelButton, 0);
            this.Controls.SetChildIndex(this._encodingBox, 0);
            this.Controls.SetChildIndex(this._encodingLabel, 0);
            this.Controls.SetChildIndex(this._selectAutoExecMacroButton, 0);
            this.Controls.SetChildIndex(this._autoExecMacroPathBox, 0);
            this.Controls.SetChildIndex(this._autoExecMacroPathLabel, 0);
            this.Controls.SetChildIndex(this._cygwin32Check, 0);
            this._advancedOptionGroup.ResumeLayout(false);
            this._advancedOptionGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        //EXTP使用
        public void ApplyParam() {
            ICygwinParameter parameter = null;
            ITerminalSettings settings = null;
            if (_loginDialogUISupport != null) {
                ITerminalParameter tp = null;
                _loginDialogUISupport.FillTopDestination(typeof(ICygwinParameter), out tp, out settings);
                parameter = tp == null ? null : (ICygwinParameter)tp.GetAdapter(typeof(ICygwinParameter));
            }
            if (parameter == null)
                parameter = TerminalSessionsPlugin.Instance.ProtocolService.CreateDefaultCygwinParameter();
            if (settings == null)
                settings = CygwinPlugin.Instance.CreateDefaultCygwinTerminalSettings();

            IAutoExecMacroParameter autoExecParams = parameter.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
            if (autoExecParams != null && CygwinPlugin.Instance.MacroEngine != null) {
                _autoExecMacroPathBox.Text = (autoExecParams.AutoExecMacroPath != null) ? autoExecParams.AutoExecMacroPath : String.Empty;
            }
            else {
                _autoExecMacroPathLabel.Enabled = false;
                _autoExecMacroPathBox.Enabled = false;
                _selectAutoExecMacroButton.Enabled = false;
            }

            ApplyParam(parameter, settings);
        }

        private void ApplyParam(ICygwinParameter shellparam, ITerminalSettings terminalSettings) {
            _param = (ICygwinParameter)shellparam.Clone();
            this.TerminalSettings = terminalSettings.Clone();

            this.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog.TextCygwin");

        }

        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            if (_param == null)
                _param = TerminalSessionsPlugin.Instance.ProtocolService.CreateDefaultCygwinParameter();

            _homeDirectoryBox.Text = _param.Home;
            _shellBox.Text = _param.ShellName;

            _logTypeBox.SelectedItem = LogType.None;    // select EnumListItem<T> by T

            _encodingBox.SelectedItem = this.TerminalSettings.Encoding;         // select EnumListItem<T> by T
            _terminalTypeBox.SelectedItem = this.TerminalSettings.TerminalType; // select EnumListItem<T> by T
            _cygwinDirBox.Text = _param.CygwinDir;
            _cygwin32Check.Checked = _param.CygwinArchitecture == CygwinArchitecture.X86;

            AdjustUI();
        }

        protected override ITerminalParameter PrepareTerminalParameter() {
            _param.Home = _homeDirectoryBox.Text;
            _param.ShellName = _shellBox.Text;

            //ログ設定
            LogType logtype = ((EnumListItem<LogType>)_logTypeBox.SelectedItem).Value;
            ISimpleLogSettings logsettings = null;
            if (logtype != LogType.None) {
                logsettings = CreateSimpleLogSettings(logtype, _logFileBox.Text);
                if (logsettings == null)
                    return null; //動作キャンセル
            }
            ITerminalSettings settings = this.TerminalSettings;
            settings.BeginUpdate();
            if (logsettings != null)
                settings.LogSettings.Reset(logsettings);
            if (!String.IsNullOrEmpty(_param.Home)) {
                settings.Caption = _param.Home;
            }
            else {
                string shell = _param.ShellBody;
                if (!String.IsNullOrEmpty(shell)) {
                    settings.Caption = shell;
                }
                else {
                    settings.Caption = "Cygwin";
                }
            }
            settings.Icon = Poderosa.TerminalSession.Properties.Resources.Cygwin16x16;
            settings.Encoding = ((EnumListItem<EncodingType>)_encodingBox.SelectedItem).Value;
            settings.TerminalType = (_terminalTypeBox.SelectedItem == null)
                ? TerminalType.XTerm256Color
                : ((EnumListItem<TerminalType>)_terminalTypeBox.SelectedItem).Value;
            settings.EndUpdate();

            ITerminalParameter termParam = (ITerminalParameter)_param.GetAdapter(typeof(ITerminalParameter));
            termParam.SetTerminalName(_terminalTypeBox.Text);	// Used for TERM environment variable (LocalShellUtil)

            _param.CygwinDir = _cygwinDirBox.Text;
            if (_param.CygwinDir.Length > 0 && _param.CygwinDir[_param.CygwinDir.Length - 1] == Path.PathSeparator)
                _param.CygwinDir = _param.CygwinDir.Substring(0, _param.CygwinDir.Length - 1);

            string autoExecMacroPath = null;
            if (_autoExecMacroPathBox.Text.Length != 0)
                autoExecMacroPath = _autoExecMacroPathBox.Text;

            _param.CygwinArchitecture = _cygwin32Check.Checked ? CygwinArchitecture.X86 : CygwinArchitecture.X86_64;
            _param.UseUTF8 = settings.Encoding == EncodingType.UTF8_Latin || settings.Encoding == EncodingType.UTF8;

            IAutoExecMacroParameter autoExecParams = _param.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
            if (autoExecParams != null)
                autoExecParams.AutoExecMacroPath = autoExecMacroPath;

            return (ITerminalParameter)_param.GetAdapter(typeof(ITerminalParameter));
        }
        protected override void StartConnection() {
            _connector = TerminalSessionsPlugin.Instance.ProtocolService.AsyncCygwinConnect(this, _param);
            if (_connector == null)
                ClearConnectingState();
        }

        private void OnLogTypeChanged(object sender, System.EventArgs args) {
            AdjustUI();
        }

        private void AdjustUI() {
            bool e = ((EnumListItem<LogType>)_logTypeBox.SelectedItem).Value != LogType.None;
            _logFileBox.Enabled = e;
            _selectlogButton.Enabled = e;
        }
        private void SelectLog(object sender, System.EventArgs e) {
            string fn = LogUtil.SelectLogFileByDialog(this);
            if (fn != null)
                _logFileBox.Text = fn;
        }

        private void SelectCygwinDir(object sender, EventArgs e) {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                dialog.Description = TEnv.Strings.GetString("Form.CygwinLoginDialog.SelectCygwinDir");
                dialog.ShowNewFolderButton = false;
                dialog.SelectedPath = _cygwinDirBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    this._cygwinDirBox.Text = dialog.SelectedPath;
            }
        }

        protected override void ShowError(string msg) {
            GUtil.Warning(this, msg, TEnv.Strings.GetString("Message.CygwinLoginDialog.ConnectionError"));
        }

        protected override void ClearConnectingState() {
            base.ClearConnectingState();
            this.Text = TEnv.Strings.GetString("Form.CygwinLoginDialog.TextCygwin");
        }

        private void _selectAutoExecMacroButton_Click(object sender, EventArgs e) {
            if (CygwinPlugin.Instance.MacroEngine != null) {
                string path = CygwinPlugin.Instance.MacroEngine.SelectMacro(this);
                if (path != null)
                    _autoExecMacroPathBox.Text = path;
            }
        }

    }
}
