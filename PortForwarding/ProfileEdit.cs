/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.

 $Id: ProfileEdit.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net.Sockets;

using Granados;

namespace Poderosa.PortForwarding {
    /// <summary>
    /// </summary>
    internal class ProfileEdit : System.Windows.Forms.Form {
        private ChannelProfile _result;

        private System.Windows.Forms.Label _sshHostLabel;
        private TextBox _sshHostBox;
        private TextBox _portBox;
        private System.Windows.Forms.Label _portLabel;
        private TextBox _accountBox;
        private System.Windows.Forms.Label _accountLabel;
        private GroupBox _authTypeGroup;
        private Label _authTypeLabel;
        private RadioButton _passwordOption;
        private RadioButton _publicKeyOption;
        private Label _privateKeyLabel;
        private TextBox _privateKeyBox;
        private Button _privateKeySelectButton;
        private System.Windows.Forms.GroupBox _localToRemoteGroup;
        private System.Windows.Forms.RadioButton _localToRemoteOption;
        private System.Windows.Forms.Label _LRLocalPortLabel;
        private TextBox _LRLocalPortBox;
        private CheckBox _LRIPv6;
        private System.Windows.Forms.Label _LRRemoteHostLabel;
        private TextBox _LRRemoteHostBox;
        private TextBox _LRRemotePortBox;
        private System.Windows.Forms.Label _LRRemotePortLabel;
        private System.Windows.Forms.GroupBox _remoteToLocalGroup;
        private System.Windows.Forms.RadioButton _remoteToLocalOption;
        private System.Windows.Forms.Label _RLRemotePortLabel;
        private TextBox _RLRemotePortBox;
        private System.Windows.Forms.Label _RLLocalHostLabel;
        private TextBox _RLLocalHostBox;
        private TextBox _RLLocalPortBox;
        private System.Windows.Forms.Label _RLLocalPortLabel;
        private System.Windows.Forms.GroupBox _optionGroup;
        private CheckBox _loopbackOnly;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public ProfileEdit(ChannelProfile prof) {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();
            InitializeText();

            //
            // TODO: InitializeComponent 呼び出しの後に、コンストラクタ コードを追加してください。
            //
            InitUI(prof);
        }

        /// <summary>
        /// 使用されているリソースに後処理を実行します。
        /// </summary>
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
            this._sshHostLabel = new System.Windows.Forms.Label();
            this._sshHostBox = new TextBox();
            this._portLabel = new System.Windows.Forms.Label();
            this._portBox = new TextBox();
            this._accountLabel = new System.Windows.Forms.Label();
            this._accountBox = new TextBox();
            this._authTypeGroup = new GroupBox();
            this._authTypeLabel = new Label();
            this._passwordOption = new RadioButton();
            this._publicKeyOption = new RadioButton();
            this._privateKeyLabel = new Label();
            this._privateKeyBox = new TextBox();
            this._privateKeySelectButton = new Button();
            this._localToRemoteOption = new System.Windows.Forms.RadioButton();
            this._localToRemoteGroup = new System.Windows.Forms.GroupBox();
            this._LRLocalPortLabel = new System.Windows.Forms.Label();
            this._LRLocalPortBox = new TextBox();
            this._LRIPv6 = new CheckBox();
            this._LRRemoteHostLabel = new System.Windows.Forms.Label();
            this._LRRemoteHostBox = new TextBox();
            this._LRRemotePortLabel = new System.Windows.Forms.Label();
            this._LRRemotePortBox = new TextBox();
            this._remoteToLocalOption = new System.Windows.Forms.RadioButton();
            this._remoteToLocalGroup = new System.Windows.Forms.GroupBox();
            this._RLRemotePortLabel = new System.Windows.Forms.Label();
            this._RLRemotePortBox = new TextBox();
            this._RLLocalHostLabel = new System.Windows.Forms.Label();
            this._RLLocalHostBox = new TextBox();
            this._RLLocalPortLabel = new System.Windows.Forms.Label();
            this._RLLocalPortBox = new TextBox();
            this._optionGroup = new GroupBox();
            this._loopbackOnly = new CheckBox();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._localToRemoteGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _sshHostLabel
            // 
            this._sshHostLabel.Location = new System.Drawing.Point(8, 8);
            this._sshHostLabel.Name = "_sshHostLabel";
            this._sshHostLabel.Size = new System.Drawing.Size(80, 23);
            this._sshHostLabel.TabIndex = 0;
            this._sshHostLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _sshHostBox
            // 
            this._sshHostBox.Location = new System.Drawing.Point(104, 8);
            this._sshHostBox.Name = "_sshHostBox";
            this._sshHostBox.Size = new System.Drawing.Size(136, 19);
            this._sshHostBox.TabIndex = 1;
            this._sshHostBox.Text = "";
            // 
            // _portLabel
            // 
            this._portLabel.Location = new System.Drawing.Point(248, 8);
            this._portLabel.Name = "_portLabel";
            this._portLabel.Size = new System.Drawing.Size(80, 23);
            this._portLabel.TabIndex = 2;
            this._portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _portBox
            // 
            this._portBox.Location = new System.Drawing.Point(336, 8);
            this._portBox.Name = "_portBox";
            this._portBox.Size = new System.Drawing.Size(40, 19);
            this._portBox.TabIndex = 3;
            this._portBox.Text = "22";
            // 
            // _accountLabel
            // 
            this._accountLabel.Location = new System.Drawing.Point(8, 32);
            this._accountLabel.Name = "_accountLabel";
            this._accountLabel.Size = new System.Drawing.Size(80, 23);
            this._accountLabel.TabIndex = 4;
            this._accountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _accountBox
            // 
            this._accountBox.Location = new System.Drawing.Point(104, 32);
            this._accountBox.Name = "_accountBox";
            this._accountBox.Size = new System.Drawing.Size(136, 19);
            this._accountBox.TabIndex = 5;
            this._accountBox.Text = "";
            //
            // _authTypeGroup
            //
            this._authTypeGroup.Controls.Add(_passwordOption);
            this._authTypeGroup.Controls.Add(_publicKeyOption);
            this._authTypeGroup.Controls.Add(_privateKeyLabel);
            this._authTypeGroup.Controls.Add(_privateKeyBox);
            this._authTypeGroup.Controls.Add(_privateKeySelectButton);
            this._authTypeGroup.Location = new System.Drawing.Point(8, 56);
            this._authTypeGroup.Name = "_authTypeGroup";
            this._authTypeGroup.Size = new System.Drawing.Size(384, 72);
            this._authTypeGroup.TabIndex = 7;
            this._authTypeGroup.FlatStyle = FlatStyle.System;
            this._authTypeGroup.TabStop = false;
            //
            // _passwordOption
            //
            this._passwordOption.Location = new System.Drawing.Point(8, 16);
            this._passwordOption.Name = "_passwordOption";
            this._passwordOption.FlatStyle = FlatStyle.System;
            this._passwordOption.Size = new System.Drawing.Size(120, 24);
            this._passwordOption.TabIndex = 0;
            this._passwordOption.CheckedChanged += new EventHandler(OnAuthTypeChanged);
            //
            // _publicKeyOption
            //
            this._publicKeyOption.Location = new System.Drawing.Point(128, 16);
            this._publicKeyOption.Name = "_publicKeyOption";
            this._publicKeyOption.FlatStyle = FlatStyle.System;
            this._publicKeyOption.Size = new System.Drawing.Size(120, 24);
            this._publicKeyOption.TabIndex = 1;
            this._publicKeyOption.CheckedChanged += new EventHandler(OnAuthTypeChanged);
            //
            // _privateKeyLabel
            //
            this._privateKeyLabel.Location = new System.Drawing.Point(8, 40);
            this._privateKeyLabel.Name = "_privateKeyLabel";
            this._privateKeyLabel.Size = new System.Drawing.Size(112, 23);
            this._privateKeyLabel.TabIndex = 2;
            this._privateKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _privateKeyBox
            //
            this._privateKeyBox.Location = new System.Drawing.Point(120, 40);
            this._privateKeyBox.Name = "_privateKeyBox";
            this._privateKeyBox.Size = new System.Drawing.Size(240, 19);
            this._privateKeyBox.TabIndex = 3;
            this._privateKeyBox.Text = "";
            //
            // _privateKeySelectButton
            //
            this._privateKeySelectButton.Location = new System.Drawing.Point(360, 40);
            this._privateKeySelectButton.Name = "_privateKeySelectButton";
            this._privateKeySelectButton.FlatStyle = FlatStyle.System;
            this._privateKeySelectButton.Size = new System.Drawing.Size(19, 19);
            this._privateKeySelectButton.TabIndex = 4;
            this._privateKeySelectButton.Text = "...";
            this._privateKeySelectButton.Click += new EventHandler(OnSelectPrivateKey);
            // 
            // _localToRemoteOption
            // 
            this._localToRemoteOption.Location = new System.Drawing.Point(16, 132);
            this._localToRemoteOption.Name = "_localToRemoteOption";
            this._localToRemoteOption.FlatStyle = FlatStyle.System;
            this._localToRemoteOption.Size = new System.Drawing.Size(264, 24);
            this._localToRemoteOption.TabIndex = 8;
            this._localToRemoteOption.CheckedChanged += new EventHandler(OnGroupCheckChanged);
            // 
            // _localToRemoteGroup
            // 
            this._localToRemoteGroup.Controls.Add(this._LRLocalPortLabel);
            this._localToRemoteGroup.Controls.Add(this._LRLocalPortBox);
            this._localToRemoteGroup.Controls.Add(this._LRIPv6);
            this._localToRemoteGroup.Controls.Add(this._LRRemoteHostLabel);
            this._localToRemoteGroup.Controls.Add(this._LRRemoteHostBox);
            this._localToRemoteGroup.Controls.Add(this._LRRemotePortLabel);
            this._localToRemoteGroup.Controls.Add(this._LRRemotePortBox);
            this._localToRemoteGroup.Location = new System.Drawing.Point(8, 136);
            this._localToRemoteGroup.Name = "_localToRemoteGroup";
            this._localToRemoteGroup.Size = new System.Drawing.Size(384, 104);
            this._localToRemoteGroup.TabIndex = 9;
            this._localToRemoteGroup.FlatStyle = FlatStyle.System;
            this._localToRemoteGroup.TabStop = false;
            // 
            // _LRLocalPortLabel
            // 
            this._LRLocalPortLabel.Location = new System.Drawing.Point(8, 24);
            this._LRLocalPortLabel.Name = "_LRLocalPortLabel";
            this._LRLocalPortLabel.Size = new System.Drawing.Size(144, 23);
            this._LRLocalPortLabel.TabIndex = 0;
            this._LRLocalPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _LRLocalPortBox
            // 
            this._LRLocalPortBox.Location = new System.Drawing.Point(160, 24);
            this._LRLocalPortBox.Name = "_LRLocalPortBox";
            this._LRLocalPortBox.Size = new System.Drawing.Size(40, 19);
            this._LRLocalPortBox.TabIndex = 1;
            this._LRLocalPortBox.Text = "";
            // 
            // _LRIPv6
            // 
            this._LRIPv6.Location = new System.Drawing.Point(208, 24);
            this._LRIPv6.Name = "_LRIPv6";
            this._LRIPv6.FlatStyle = FlatStyle.System;
            this._LRIPv6.Size = new System.Drawing.Size(120, 19);
            this._LRIPv6.TabIndex = 2;
            // 
            // _LRRemoteHostLabel
            // 
            this._LRRemoteHostLabel.Location = new System.Drawing.Point(8, 48);
            this._LRRemoteHostLabel.Name = "_LRRemoteHostLabel";
            this._LRRemoteHostLabel.Size = new System.Drawing.Size(144, 23);
            this._LRRemoteHostLabel.TabIndex = 3;
            this._LRRemoteHostLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _LRRemoteHostBox
            // 
            this._LRRemoteHostBox.Location = new System.Drawing.Point(160, 48);
            this._LRRemoteHostBox.Name = "_LRRemoteHostBox";
            this._LRRemoteHostBox.Size = new System.Drawing.Size(144, 19);
            this._LRRemoteHostBox.TabIndex = 4;
            this._LRRemoteHostBox.Text = "";
            // 
            // _LRRemotePortLabel
            // 
            this._LRRemotePortLabel.Location = new System.Drawing.Point(8, 72);
            this._LRRemotePortLabel.Name = "_LRRemotePortLabel";
            this._LRRemotePortLabel.Size = new System.Drawing.Size(144, 23);
            this._LRRemotePortLabel.TabIndex = 5;
            this._LRRemotePortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _LRRemotePortBox
            // 
            this._LRRemotePortBox.Location = new System.Drawing.Point(160, 72);
            this._LRRemotePortBox.Name = "_LRRemotePortBox";
            this._LRRemotePortBox.Size = new System.Drawing.Size(40, 19);
            this._LRRemotePortBox.TabIndex = 6;
            this._LRRemotePortBox.Text = "";
            // 
            // _remoteToLocalOption
            // 
            this._remoteToLocalOption.Location = new System.Drawing.Point(16, 244);
            this._remoteToLocalOption.Name = "_remoteToLocalOption";
            this._remoteToLocalOption.FlatStyle = FlatStyle.System;
            this._remoteToLocalOption.Size = new System.Drawing.Size(264, 24);
            this._remoteToLocalOption.TabIndex = 10;
            this._remoteToLocalOption.CheckedChanged += new EventHandler(OnGroupCheckChanged);
            // 
            // _remoteToLocalGroup
            // 
            this._remoteToLocalGroup.Controls.Add(this._RLLocalPortLabel);
            this._remoteToLocalGroup.Controls.Add(this._RLLocalPortBox);
            this._remoteToLocalGroup.Controls.Add(this._RLLocalHostLabel);
            this._remoteToLocalGroup.Controls.Add(this._RLLocalHostBox);
            this._remoteToLocalGroup.Controls.Add(this._RLRemotePortLabel);
            this._remoteToLocalGroup.Controls.Add(this._RLRemotePortBox);
            this._remoteToLocalGroup.Location = new System.Drawing.Point(8, 248);
            this._remoteToLocalGroup.Name = "_remoteToLocalGroup";
            this._remoteToLocalGroup.Size = new System.Drawing.Size(384, 104);
            this._remoteToLocalGroup.TabIndex = 11;
            this._remoteToLocalGroup.FlatStyle = FlatStyle.System;
            this._remoteToLocalGroup.TabStop = false;
            // 
            // _RLRemotePortLabel
            // 
            this._RLRemotePortLabel.Location = new System.Drawing.Point(8, 24);
            this._RLRemotePortLabel.Name = "_RLRemotePortLabel";
            this._RLRemotePortLabel.Size = new System.Drawing.Size(144, 23);
            this._RLRemotePortLabel.TabIndex = 0;
            this._RLRemotePortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _RLRemotePortBox
            // 
            this._RLRemotePortBox.Location = new System.Drawing.Point(160, 24);
            this._RLRemotePortBox.Name = "_RLRemotePortBox";
            this._RLRemotePortBox.Size = new System.Drawing.Size(40, 19);
            this._RLRemotePortBox.TabIndex = 1;
            this._RLRemotePortBox.Text = "";
            // 
            // _RLLocalHostLabel
            // 
            this._RLLocalHostLabel.Location = new System.Drawing.Point(8, 48);
            this._RLLocalHostLabel.Name = "_RLLocalHostLabel";
            this._RLLocalHostLabel.Size = new System.Drawing.Size(144, 23);
            this._RLLocalHostLabel.TabIndex = 3;
            this._RLLocalHostLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _RLLocalHostBox
            // 
            this._RLLocalHostBox.Location = new System.Drawing.Point(160, 48);
            this._RLLocalHostBox.Name = "_RLLocalHostBox";
            this._RLLocalHostBox.Size = new System.Drawing.Size(144, 19);
            this._RLLocalHostBox.TabIndex = 4;
            this._RLLocalHostBox.Text = "localhost";
            // 
            // _RLLocalPortLabel
            // 
            this._RLLocalPortLabel.Location = new System.Drawing.Point(8, 72);
            this._RLLocalPortLabel.Name = "_RLLocalPortLabel";
            this._RLLocalPortLabel.Size = new System.Drawing.Size(144, 23);
            this._RLLocalPortLabel.TabIndex = 5;
            this._RLLocalPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _RLLocalPortBox
            // 
            this._RLLocalPortBox.Location = new System.Drawing.Point(160, 72);
            this._RLLocalPortBox.Name = "_RLLocalPortBox";
            this._RLLocalPortBox.Size = new System.Drawing.Size(40, 19);
            this._RLLocalPortBox.TabIndex = 6;
            this._RLLocalPortBox.Text = "";
            // 
            // _optionGroup
            // 
            this._optionGroup.Controls.Add(this._loopbackOnly);
            this._optionGroup.Location = new System.Drawing.Point(8, 360);
            this._optionGroup.Name = "_optionGroup";
            this._optionGroup.Size = new System.Drawing.Size(384, 64);
            this._optionGroup.TabIndex = 12;
            this._optionGroup.FlatStyle = FlatStyle.System;
            this._optionGroup.TabStop = false;
            // 
            // _loopbackOnly
            // 
            this._loopbackOnly.Location = new System.Drawing.Point(8, 16);
            this._loopbackOnly.Name = "_loopbackOnly";
            this._loopbackOnly.FlatStyle = FlatStyle.System;
            this._loopbackOnly.Size = new System.Drawing.Size(288, 19);
            this._loopbackOnly.TabIndex = 1;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(224, 440);
            this._okButton.Name = "_okButton";
            this._okButton.FlatStyle = FlatStyle.System;
            this._okButton.Size = new System.Drawing.Size(72, 24);
            this._okButton.TabIndex = 13;
            this._okButton.Click += new EventHandler(OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(312, 440);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.FlatStyle = FlatStyle.System;
            this._cancelButton.Size = new System.Drawing.Size(72, 24);
            this._cancelButton.TabIndex = 14;
            // 
            // ProfileEdit
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(394, 471);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._optionGroup);
            this.Controls.Add(this._localToRemoteOption);
            this.Controls.Add(this._localToRemoteGroup);
            this.Controls.Add(this._remoteToLocalOption);
            this.Controls.Add(this._remoteToLocalGroup);
            this.Controls.Add(this._authTypeGroup);
            this.Controls.Add(this._accountBox);
            this.Controls.Add(this._portBox);
            this.Controls.Add(this._sshHostBox);
            this.Controls.Add(this._accountLabel);
            this.Controls.Add(this._portLabel);
            this.Controls.Add(this._sshHostLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileEdit";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this._localToRemoteGroup.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void InitializeText() {
            this._sshHostLabel.Text = Env.Strings.GetString("Form.ProfileEdit._sshHostLabel");
            this._portLabel.Text = Env.Strings.GetString("Form.ProfileEdit._portLabel");
            this._accountLabel.Text = Env.Strings.GetString("Form.ProfileEdit._accountLabel");
            this._authTypeGroup.Text = Env.Strings.GetString("Form.ProfileEdit._authTypeGroup");
            this._passwordOption.Text = Env.Strings.GetString("Form.ProfileEdit._passwordOption");
            this._publicKeyOption.Text = Env.Strings.GetString("Form.ProfileEdit._publicKeyOption");
            this._privateKeyLabel.Text = Env.Strings.GetString("Form.ProfileEdit._privateKeyLabel");
            this._localToRemoteOption.Text = Env.Strings.GetString("Form.ProfileEdit._localToRemoteOption");
            this._LRLocalPortLabel.Text = Env.Strings.GetString("Form.ProfileEdit._LRLocalPortLabel");
            this._LRIPv6.Text = Env.Strings.GetString("Form.ProfileEdit._LRIPv6");
            this._LRRemoteHostLabel.Text = Env.Strings.GetString("Form.ProfileEdit._LRRemoteHostLabel");
            this._LRRemotePortLabel.Text = Env.Strings.GetString("Form.ProfileEdit._LRRemotePortLabel");
            this._remoteToLocalOption.Text = Env.Strings.GetString("Form.ProfileEdit._remoteToLocalOption");
            this._RLRemotePortLabel.Text = Env.Strings.GetString("Form.ProfileEdit._RLRemotePortLabel");
            this._RLLocalHostLabel.Text = Env.Strings.GetString("Form.ProfileEdit._RLLocalHostLabel");
            this._RLLocalPortLabel.Text = Env.Strings.GetString("Form.ProfileEdit._RLLocalPortLabel");
            this._optionGroup.Text = Env.Strings.GetString("Form.ProfileEdit._optionGroup");
            this._loopbackOnly.Text = Env.Strings.GetString("Form.ProfileEdit._loopbackOnly");
            this.Text = Env.Strings.GetString("Form.ProfileEdit.Text");
            this._okButton.Text = Env.Strings.GetString("Common.OK");
            this._cancelButton.Text = Env.Strings.GetString("Common.Cancel");
        }

        private void InitUI(ChannelProfile prof) {
            if (prof == null) {
                _localToRemoteOption.Checked = true;
                _passwordOption.Checked = true;
            }
            else {
                _sshHostBox.Text = prof.SSHHost;
                _portBox.Text = prof.SSHPort.ToString();
                _accountBox.Text = prof.SSHAccount;
                //_udpOption.Checked = prof.ProtocolType==ProtocolType.Udp;
                if (prof.AuthType == AuthenticationType.Password)
                    _passwordOption.Checked = true;
                else {
                    _publicKeyOption.Checked = true;
                    _privateKeyBox.Text = prof.PrivateKeyFile;
                }
                _loopbackOnly.Checked = !prof.AllowsForeignConnection;

                if (prof is LocalToRemoteChannelProfile) {
                    LocalToRemoteChannelProfile p = (LocalToRemoteChannelProfile)prof;
                    _localToRemoteOption.Checked = true;
                    _LRLocalPortBox.Text = p.ListenPort.ToString();
                    _LRIPv6.Checked = p.UseIPv6;
                    _LRRemoteHostBox.Text = p.DestinationHost;
                    _LRRemotePortBox.Text = p.DestinationPort.ToString();
                }
                else {
                    RemoteToLocalChannelProfile p = (RemoteToLocalChannelProfile)prof;
                    _remoteToLocalOption.Checked = true;
                    _RLLocalPortBox.Text = p.DestinationPort.ToString();
                    _RLLocalHostBox.Text = p.DestinationHost;
                    _RLRemotePortBox.Text = p.ListenPort.ToString();
                }
            }
        }

        private void OnAuthTypeChanged(object sender, EventArgs args) {
            _privateKeyBox.Enabled = _publicKeyOption.Checked;
            _privateKeyLabel.Enabled = _publicKeyOption.Checked;
            _privateKeySelectButton.Enabled = _publicKeyOption.Checked;
        }

        private void OnGroupCheckChanged(object sender, EventArgs args) {
            if (_localToRemoteOption.Checked) {
                _localToRemoteGroup.Enabled = true;
                _remoteToLocalGroup.Enabled = false;
            }
            else {
                _localToRemoteGroup.Enabled = false;
                _remoteToLocalGroup.Enabled = true;
            }
        }
        private void OnSelectPrivateKey(object sender, EventArgs args) {
            string fn = Util.SelectPrivateKeyFileByDialog(this);
            if (fn != null)
                _privateKeyBox.Text = fn;
        }

        private void OnOK(object sender, EventArgs args) {
            this.DialogResult = DialogResult.None;
            string itemname = null;
            try {
                if (_localToRemoteOption.Checked)
                    _result = new LocalToRemoteChannelProfile();
                else
                    _result = new RemoteToLocalChannelProfile();

                if (_sshHostBox.Text.Length == 0)
                    throw new Exception(Env.Strings.GetString("Message.ProfileEdit.EmptySSHServer"));
                _result.SSHHost = _sshHostBox.Text;
                if (_accountBox.Text.Length == 0)
                    throw new Exception(Env.Strings.GetString("Message.ProfileEdit.EmptyAccount"));
                _result.SSHAccount = _accountBox.Text;
                _result.AuthType = _publicKeyOption.Checked ? AuthenticationType.PublicKey : AuthenticationType.Password;
                if (_result.AuthType == AuthenticationType.PublicKey)
                    _result.PrivateKeyFile = _privateKeyBox.Text;

                itemname = Env.Strings.GetString("Caption.ProfileEdit.SSHPortNumber");
                _result.SSHPort = Util.ParsePort(_portBox.Text);

                _result.ProtocolType = ProtocolType.Tcp;

                _result.AllowsForeignConnection = !_loopbackOnly.Checked;
                if (_localToRemoteOption.Checked) {
                    LocalToRemoteChannelProfile p = (LocalToRemoteChannelProfile)_result;
                    itemname = Env.Strings.GetString("Caption.ProfileEdit.LocalPort");
                    p.ListenPort = Util.ParsePort(_LRLocalPortBox.Text);
                    if (_LRRemoteHostBox.Text.Length == 0)
                        throw new Exception(Env.Strings.GetString("Message.ProfileEdit.EmptyRemoveHost"));
                    p.DestinationHost = _LRRemoteHostBox.Text;
                    itemname = Env.Strings.GetString("Caption.ProfileEdit.DestinationPort");
                    p.DestinationPort = Util.ParsePort(_LRRemotePortBox.Text);
                    p.UseIPv6 = _LRIPv6.Checked;
                }
                else {
                    RemoteToLocalChannelProfile p = (RemoteToLocalChannelProfile)_result;
                    itemname = Env.Strings.GetString("Caption.ProfileEdit.RemotePort");
                    p.ListenPort = Util.ParsePort(_RLRemotePortBox.Text);
                    itemname = Env.Strings.GetString("Caption.ProfileEdit.DestinationHost");
                    p.DestinationHost = _RLLocalHostBox.Text;
                    itemname = Env.Strings.GetString("Caption.ProfileEdit.DestinationPort");
                    p.DestinationPort = Util.ParsePort(_RLLocalPortBox.Text);
                    p.AllowsForeignConnection = !_loopbackOnly.Checked;
                    /*
                    if(_udpOption.Checked && _loopbackOnly.Checked) {
                        throw new Exception("リモートからローカルへのUDPの転送をするときはLoopback以外からの接続を許可する必要があります。");
                    }
                    */
                }

                this.DialogResult = DialogResult.OK;
            }
            catch (FormatException) {
                Util.Warning(this, String.Format(Env.Strings.GetString("Message.OptionDialog.InvalidItem"), itemname));
            }
            catch (Exception ex) {
                Util.Warning(this, ex.Message);
            }

        }

        public ChannelProfile ResultProfile {
            get {
                return _result;
            }
        }

    }
}
