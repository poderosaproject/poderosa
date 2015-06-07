/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: SSHShortcutLoginDialog.cs,v 1.3 2012/03/18 12:05:53 kzmi Exp $
*/
using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

using Granados;
using Poderosa.Toolkit;

namespace Poderosa.PortForwarding {
    /// <summary>
    /// SSHShortcutLoginDialog の概要の説明です。
    /// </summary>
    internal class SSHShortcutLoginDialog : System.Windows.Forms.Form, ISocketWithTimeoutClient {
        private ChannelProfile _profile;
        private SocketWithTimeout _connector;
        private ChannelFactory _result;
        private IntPtr _savedHWND;

        private Label _privateKeyBox;
        private TextBox _passphraseBox;
        private System.Windows.Forms.Label _hostLabel;
        private System.Windows.Forms.Label _hostBox;
        private System.Windows.Forms.Label _methodLabel;
        private System.Windows.Forms.Label _methodBox;
        private System.Windows.Forms.Label _accountLabel;
        private System.Windows.Forms.Label _accountBox;
        private System.Windows.Forms.Label _authTypeLabel;
        private System.Windows.Forms.Label _authTypeBox;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _loginButton;
        private System.Windows.Forms.Label _privateKeyLabel;
        private System.Windows.Forms.Label _passphraseLabel;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public SSHShortcutLoginDialog(ChannelProfile profile) {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();
            InitializeText();

            //
            // TODO: InitializeComponent 呼び出しの後に、コンストラクタ コードを追加してください。
            //
            _profile = profile;
            InitUI();
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

        public ChannelFactory Result {
            get {
                return _result;
            }
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._privateKeyBox = new Label();
            this._privateKeyLabel = new System.Windows.Forms.Label();
            this._passphraseBox = new TextBox();
            this._passphraseLabel = new System.Windows.Forms.Label();
            this._cancelButton = new System.Windows.Forms.Button();
            this._loginButton = new System.Windows.Forms.Button();
            this._hostLabel = new System.Windows.Forms.Label();
            this._hostBox = new System.Windows.Forms.Label();
            this._methodLabel = new System.Windows.Forms.Label();
            this._methodBox = new System.Windows.Forms.Label();
            this._accountLabel = new System.Windows.Forms.Label();
            this._accountBox = new System.Windows.Forms.Label();
            this._authTypeLabel = new System.Windows.Forms.Label();
            this._authTypeBox = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _privateKeyBox
            // 
            this._privateKeyBox.Location = new System.Drawing.Point(104, 72);
            this._privateKeyBox.Name = "_privateKeyBox";
            this._privateKeyBox.Size = new System.Drawing.Size(160, 19);
            this._privateKeyBox.TabIndex = 3;
            this._privateKeyBox.Text = "";
            // 
            // _privateKeyLabel
            // 
            this._privateKeyLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._privateKeyLabel.Location = new System.Drawing.Point(8, 72);
            this._privateKeyLabel.Name = "_privateKeyLabel";
            this._privateKeyLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._privateKeyLabel.Size = new System.Drawing.Size(72, 16);
            this._privateKeyLabel.TabIndex = 2;
            this._privateKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _passphraseBox
            // 
            this._passphraseBox.Location = new System.Drawing.Point(104, 96);
            this._passphraseBox.Name = "_passphraseBox";
            this._passphraseBox.PasswordChar = '*';
            this._passphraseBox.Size = new System.Drawing.Size(184, 19);
            this._passphraseBox.TabIndex = 1;
            this._passphraseBox.Text = "";
            // 
            // _passphraseLabel
            // 
            this._passphraseLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._passphraseLabel.Location = new System.Drawing.Point(8, 96);
            this._passphraseLabel.Name = "_passphraseLabel";
            this._passphraseLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._passphraseLabel.Size = new System.Drawing.Size(80, 16);
            this._passphraseLabel.TabIndex = 0;
            this._passphraseLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.ImageIndex = 0;
            this._cancelButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._cancelButton.Location = new System.Drawing.Point(216, 120);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.FlatStyle = FlatStyle.System;
            this._cancelButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._cancelButton.Size = new System.Drawing.Size(72, 25);
            this._cancelButton.TabIndex = 11;
            // 
            // _loginButton
            // 
            this._loginButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._loginButton.ImageIndex = 0;
            this._loginButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._loginButton.Location = new System.Drawing.Point(128, 120);
            this._loginButton.Name = "_loginButton";
            this._loginButton.FlatStyle = FlatStyle.System;
            this._loginButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._loginButton.Size = new System.Drawing.Size(72, 25);
            this._loginButton.TabIndex = 10;
            this._loginButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _hostLabel
            // 
            this._hostLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._hostLabel.Location = new System.Drawing.Point(8, 8);
            this._hostLabel.Name = "_hostLabel";
            this._hostLabel.Size = new System.Drawing.Size(80, 16);
            this._hostLabel.TabIndex = 0;
            this._hostLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _hostBox
            // 
            this._hostBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._hostBox.Location = new System.Drawing.Point(104, 8);
            this._hostBox.Name = "_hostBox";
            this._hostBox.Size = new System.Drawing.Size(144, 16);
            this._hostBox.TabIndex = 35;
            this._hostBox.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _methodLabel
            // 
            this._methodLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._methodLabel.Location = new System.Drawing.Point(8, 24);
            this._methodLabel.Name = "_methodLabel";
            this._methodLabel.Size = new System.Drawing.Size(80, 16);
            this._methodLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _methodBox
            // 
            this._methodBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._methodBox.Location = new System.Drawing.Point(104, 24);
            this._methodBox.Name = "_methodBox";
            this._methodBox.Size = new System.Drawing.Size(144, 16);
            this._methodBox.TabIndex = 0;
            this._methodBox.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _accountLabel
            // 
            this._accountLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._accountLabel.Location = new System.Drawing.Point(8, 40);
            this._accountLabel.Name = "_accountLabel";
            this._accountLabel.Size = new System.Drawing.Size(80, 16);
            this._accountLabel.TabIndex = 0;
            this._methodLabel.TabIndex = 0;
            this._accountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _accountBox
            // 
            this._accountBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._accountBox.Location = new System.Drawing.Point(104, 40);
            this._accountBox.Name = "_accountBox";
            this._accountBox.Size = new System.Drawing.Size(144, 16);
            this._accountBox.TabIndex = 0;
            this._accountBox.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _authTypeLabel
            // 
            this._authTypeLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._authTypeLabel.Location = new System.Drawing.Point(8, 56);
            this._authTypeLabel.Name = "_authTypeLabel";
            this._authTypeLabel.Size = new System.Drawing.Size(80, 16);
            this._authTypeLabel.TabIndex = 0;
            this._authTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _authTypeBox
            // 
            this._authTypeBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._authTypeBox.Location = new System.Drawing.Point(104, 56);
            this._authTypeBox.Name = "_authTypeBox";
            this._authTypeBox.Size = new System.Drawing.Size(144, 16);
            this._authTypeBox.TabIndex = 0;
            this._authTypeBox.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // SSHShortcutLoginDialog
            // 
            this.AcceptButton = this._loginButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(298, 152);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._cancelButton,
                this._loginButton,
                this._hostLabel,
                this._hostBox,
                this._methodLabel,
                this._methodBox,
                this._accountLabel,
                this._accountBox,
                this._authTypeLabel,
                this._authTypeBox,
                this._privateKeyBox,
                this._privateKeyLabel,
                this._passphraseBox,
                this._passphraseLabel});
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SSHShortcutLoginDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }
        #endregion

        private void InitializeText() {
            this._privateKeyLabel.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog._privateKeyLabel");
            this._passphraseLabel.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog._passphraseLabel");
            this._hostLabel.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog._hostLabel");
            this._methodLabel.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog._methodLabel");
            this._accountLabel.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog._accountLabel");
            this._authTypeLabel.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog._authTypeLabel");
            this.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog.Text");
            this._cancelButton.Text = Env.Strings.GetString("Common.Cancel");
            this._loginButton.Text = Env.Strings.GetString("Common.OK");
        }

        private void InitUI() {
            _hostBox.Text = _profile.SSHHost;
            _methodBox.Text = "SSH2";
            if (_profile.SSHPort != 22)
                _methodBox.Text += String.Format(Env.Strings.GetString("Caption.SSHShortcutLoginDialog.NotRegularPort"), _profile.SSHPort);
            _accountBox.Text = _profile.SSHAccount;
            _authTypeBox.Text = Util.AuthTypeDescription(_profile.AuthType);
            _passphraseBox.Text = _profile.Passphrase;

            if (_profile.AuthType == AuthenticationType.Password)
                _privateKeyBox.Text = "-";
            else
                _privateKeyBox.Text = _profile.PrivateKeyFile;

            AdjustUI();
        }
        private void AdjustUI() {
        }

        private void OnOK(object sender, System.EventArgs e) {
            this.DialogResult = DialogResult.None;
            if (ValidateContent() == null)
                return;  //パラメータに誤りがあれば即脱出

            _loginButton.Enabled = false;
            _cancelButton.Enabled = false;
            this.Cursor = Cursors.WaitCursor;
            this.Text = Env.Strings.GetString("Caption.SSHShortcutLoginDialog.Connecting");

            //HostKeyChecker checker = new HostKeyChecker(this, param);
            _savedHWND = this.Handle;
            _connector = ConnectionManager.StartNewConnection(this, _profile, _passphraseBox.Text, null/*new HostKeyCheckCallback(checker.CheckHostKeyCallback)*/);
            if (_connector == null)
                ClearConnectingState();
        }
        //入力内容に誤りがあればそれを警告してnullを返す。なければ必要なところを埋めたTCPTerminalParamを返す
        private ChannelProfile ValidateContent() {
            string msg = null;

            try {
                if (_profile.AuthType == AuthenticationType.PublicKey) {
                    if (!File.Exists(_privateKeyBox.Text))
                        msg = Env.Strings.GetString("Message.SSHShortcutLoginDialog.KeyFileNotExist");
                    else
                        _profile.PrivateKeyFile = _privateKeyBox.Text;
                }

                if (msg != null) {
                    Util.Warning(this, msg);
                    return null;
                }
                else
                    return _profile;
            }
            catch (Exception ex) {
                Util.Warning(this, ex.Message);
                return null;
            }
        }
        private void OnOpenPrivateKey(object sender, System.EventArgs e) {
            string fn = Util.SelectPrivateKeyFileByDialog(this);
            if (fn != null)
                _privateKeyBox.Text = fn;
        }

        private void ShowError(string msg) {
            Util.Warning(this, msg, Env.Strings.GetString("Message.SSHShortcutLoginDialog.ConnectionError"));
        }

        protected override bool ProcessDialogKey(Keys key) {
            if (_connector != null && key == (Keys.Control | Keys.C)) {
                _connector.Interrupt();
                ClearConnectingState();
                return true;
            }
            else
                return base.ProcessDialogKey(key);
        }
        private void ClearConnectingState() {
            _loginButton.Enabled = true;
            _cancelButton.Enabled = true;
            this.Cursor = Cursors.Default;
            this.Text = Env.Strings.GetString("Form.SSHShortcutLoginDialog.Text");
            _connector = null;
        }

        //Invokeで来るもの
        private void SuccessfullyExitX() {
            this.DialogResult = DialogResult.OK;
            Close();
        }
        private void ShowErrorX(string msg) {
            ClearConnectingState();
            ShowError(msg);
        }

        private delegate void ExitDelegate();
        private delegate void ShowErrorDelegate(string msg);

        //ISocketWithTimeoutClient これらはこのウィンドウとは別のスレッドで実行されるので慎重に
        public void SuccessfullyExit(object result) {
            _result = (ChannelFactory)result;
            Debug.Assert(InvokeRequired);
            this.Invoke(new ExitDelegate(SuccessfullyExitX));
        }
        public void ConnectionFailed(string message) {
            Debug.Assert(InvokeRequired);
            this.Invoke(new ShowErrorDelegate(ShowErrorX), message);
        }
        public void CancelTimer() {
        }

    }
}
