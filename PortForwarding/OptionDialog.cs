/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: OptionDialog.cs,v 1.4 2012/03/18 12:05:53 kzmi Exp $
*/
using System;
using System.Drawing;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Poderosa.Toolkit;
using Granados;
using Granados.PKI;

namespace Poderosa.PortForwarding {
    /// <summary>
    /// OptionDialog の概要の説明です。
    /// </summary>
    internal class OptionDialog : System.Windows.Forms.Form {
        //再度開いたときに開くタブを記憶
        private static int _FIRSTTABPAGE = 0;

        private Options _options;

        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.TabControl _tabControl;

        private System.Windows.Forms.TabPage _sshPage;
        private System.Windows.Forms.GroupBox _cipherOrderGroup;
        private System.Windows.Forms.ListBox _cipherOrderList;
        private System.Windows.Forms.Button _algorithmOrderUp;
        private System.Windows.Forms.Button _algorithmOrderDown;
        private System.Windows.Forms.GroupBox _ssh2OptionGroup;
        private System.Windows.Forms.Label _hostKeyLabel;
        private ComboBox _hostKeyBox;
        private System.Windows.Forms.Label _windowSizeLabel;
        private TextBox _windowSizeBox;
        private System.Windows.Forms.GroupBox _sshMiscGroup;
        private CheckBox _retainsPassphrase;
        private CheckBox _sshCheckMAC;

        private System.Windows.Forms.TabPage _connectionPage;
        private System.Windows.Forms.GroupBox _socksGroup;
        private CheckBox _useSocks;
        private System.Windows.Forms.Label _socksServerLabel;
        private TextBox _socksServerBox;
        private System.Windows.Forms.Label _socksPortLabel;
        private TextBox _socksPortBox;
        private System.Windows.Forms.Label _socksAccountLabel;
        private TextBox _socksAccountBox;
        private System.Windows.Forms.Label _socksPasswordLabel;
        private TextBox _socksPasswordBox;
        private System.Windows.Forms.Label _socksNANetworksLabel;
        private TextBox _socksNANetworksBox;

        private System.Windows.Forms.TabPage _genericPage;
        private CheckBox _showInTaskBarOption;
        private CheckBox _warningOnExit;
        private System.Windows.Forms.Label _optionPreservePlaceLabel;
        private ComboBox _optionPreservePlace;
        private System.Windows.Forms.Label _optionPreservePlacePath;
        private System.Windows.Forms.Label _languageLabel;
        private ComboBox _languageBox;


        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public OptionDialog() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();

            //
            // TODO: InitializeComponent 呼び出しの後に、コンストラクタ コードを追加してください。
            //
            _tabControl.SelectedIndex = _FIRSTTABPAGE;
            _optionPreservePlace.Items.AddRange(EnumListItem<OptionPreservePlace>.GetListItems());
            _languageBox.Items.AddRange(EnumListItem<Language>.GetListItems());
            InitializeText();

            //TODO SOCKSとOptionPreservePlaceまわりは未サポート
            _useSocks.Enabled = false;
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

        #region Windows Form Designer generated code
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._tabControl = new System.Windows.Forms.TabControl();

            this._sshPage = new System.Windows.Forms.TabPage();
            this._cipherOrderGroup = new System.Windows.Forms.GroupBox();
            this._cipherOrderList = new System.Windows.Forms.ListBox();
            this._algorithmOrderUp = new System.Windows.Forms.Button();
            this._algorithmOrderDown = new System.Windows.Forms.Button();
            this._ssh2OptionGroup = new System.Windows.Forms.GroupBox();
            this._hostKeyLabel = new System.Windows.Forms.Label();
            this._hostKeyBox = new ComboBox();
            this._windowSizeLabel = new System.Windows.Forms.Label();
            this._windowSizeBox = new TextBox();
            this._sshMiscGroup = new System.Windows.Forms.GroupBox();
            this._retainsPassphrase = new System.Windows.Forms.CheckBox();
            this._sshCheckMAC = new System.Windows.Forms.CheckBox();

            this._connectionPage = new System.Windows.Forms.TabPage();
            this._socksGroup = new System.Windows.Forms.GroupBox();
            this._useSocks = new CheckBox();
            this._socksServerLabel = new System.Windows.Forms.Label();
            this._socksServerBox = new TextBox();
            this._socksPortLabel = new System.Windows.Forms.Label();
            this._socksPortBox = new TextBox();
            this._socksAccountLabel = new System.Windows.Forms.Label();
            this._socksAccountBox = new TextBox();
            this._socksPasswordLabel = new System.Windows.Forms.Label();
            this._socksPasswordBox = new TextBox();
            this._socksNANetworksLabel = new System.Windows.Forms.Label();
            this._socksNANetworksBox = new TextBox();

            this._genericPage = new System.Windows.Forms.TabPage();
            this._showInTaskBarOption = new System.Windows.Forms.CheckBox();
            this._optionPreservePlaceLabel = new System.Windows.Forms.Label();
            this._optionPreservePlace = new ComboBox();
            this._optionPreservePlacePath = new Label();
            this._languageLabel = new Label();
            this._languageBox = new ComboBox();
            this._warningOnExit = new CheckBox();

            this._tabControl.SuspendLayout();
            this._sshPage.SuspendLayout();
            this._cipherOrderGroup.SuspendLayout();
            this._ssh2OptionGroup.SuspendLayout();
            this._sshMiscGroup.SuspendLayout();
            this._connectionPage.SuspendLayout();
            this._socksGroup.SuspendLayout();
            this._genericPage.SuspendLayout();
            this.SuspendLayout();
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(248, 312);
            this._okButton.Name = "_okButton";
            this._okButton.FlatStyle = FlatStyle.System;
            this._okButton.TabIndex = 0;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(328, 312);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.FlatStyle = FlatStyle.System;
            this._cancelButton.TabIndex = 1;
            // 
            // _tabControl
            // 
            this._tabControl.Anchor = (System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right);
            this._tabControl.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._sshPage,
                this._connectionPage,
                this._genericPage});
            this._tabControl.Location = new System.Drawing.Point(8, 0);
            this._tabControl.Name = "_tabControl";
            this._tabControl.SelectedIndex = 0;
            this._tabControl.Size = new System.Drawing.Size(394, 304);
            this._tabControl.TabIndex = 2;
            this._tabControl.Validating += new System.ComponentModel.CancelEventHandler(this.OnVerifyTab);
            this._tabControl.SelectedIndexChanged += new System.EventHandler(this.OnTabChanged);
            // 
            // _sshPage
            // 
            this._sshPage.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._cipherOrderGroup,
                this._ssh2OptionGroup,
                this._sshMiscGroup});
            this._sshPage.Location = new System.Drawing.Point(4, 21);
            this._sshPage.Name = "_sshPage";
            this._sshPage.Size = new System.Drawing.Size(386, 279);
            this._sshPage.Text = "SSH";
            this._sshPage.Validating += new System.ComponentModel.CancelEventHandler(this.OnVerifyTab);
            // 
            // _cipherOrderGroup
            // 
            this._cipherOrderGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._cipherOrderList,
                this._algorithmOrderUp,
                this._algorithmOrderDown});
            this._cipherOrderGroup.Location = new System.Drawing.Point(8, 8);
            this._cipherOrderGroup.Name = "_cipherOrderGroup";
            this._cipherOrderGroup.Size = new System.Drawing.Size(368, 88);
            this._cipherOrderGroup.TabIndex = 0;
            this._cipherOrderGroup.FlatStyle = FlatStyle.System;
            this._cipherOrderGroup.TabStop = false;
            // 
            // _cipherOrderList
            // 
            this._cipherOrderList.ItemHeight = 12;
            this._cipherOrderList.Location = new System.Drawing.Point(8, 16);
            this._cipherOrderList.Name = "_cipherOrderList";
            this._cipherOrderList.Size = new System.Drawing.Size(208, 64);
            this._cipherOrderList.TabIndex = 1;
            // 
            // _algorithmOrderUp
            // 
            this._algorithmOrderUp.Location = new System.Drawing.Point(232, 16);
            this._algorithmOrderUp.Name = "_algorithmOrderUp";
            this._algorithmOrderUp.FlatStyle = FlatStyle.System;
            this._algorithmOrderUp.TabIndex = 2;
            this._algorithmOrderUp.Click += new System.EventHandler(this.OnCipherAlgorithmOrderUp);
            // 
            // _algorithmOrderDown
            // 
            this._algorithmOrderDown.Location = new System.Drawing.Point(232, 56);
            this._algorithmOrderDown.Name = "_algorithmOrderDown";
            this._algorithmOrderDown.FlatStyle = FlatStyle.System;
            this._algorithmOrderDown.TabIndex = 3;
            this._algorithmOrderDown.Click += new System.EventHandler(this.OnCipherAlgorithmOrderDown);
            // 
            // _ssh2OptionGroup
            // 
            this._ssh2OptionGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._hostKeyLabel,
                this._hostKeyBox,
                this._windowSizeLabel,
                this._windowSizeBox});
            this._ssh2OptionGroup.Location = new System.Drawing.Point(8, 104);
            this._ssh2OptionGroup.Name = "_ssh2OptionGroup";
            this._ssh2OptionGroup.Size = new System.Drawing.Size(368, 80);
            this._ssh2OptionGroup.TabIndex = 4;
            this._ssh2OptionGroup.FlatStyle = FlatStyle.System;
            this._ssh2OptionGroup.TabStop = false;
            // 
            // _hostKeyLabel
            // 
            this._hostKeyLabel.Location = new System.Drawing.Point(8, 16);
            this._hostKeyLabel.Name = "_hostKeyLabel";
            this._hostKeyLabel.Size = new System.Drawing.Size(200, 23);
            this._hostKeyLabel.TabIndex = 5;
            this._hostKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _hostKeyBox
            // 
            this._hostKeyBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._hostKeyBox.Items.AddRange(new object[] {
                "DSA",
                "RSA"});
            this._hostKeyBox.Location = new System.Drawing.Point(224, 16);
            this._hostKeyBox.Name = "_hostKeyBox";
            this._hostKeyBox.Size = new System.Drawing.Size(121, 20);
            this._hostKeyBox.TabIndex = 6;
            // 
            // _windowSizeLabel
            // 
            this._windowSizeLabel.Location = new System.Drawing.Point(8, 48);
            this._windowSizeLabel.Name = "_windowSizeLabel";
            this._windowSizeLabel.Size = new System.Drawing.Size(192, 23);
            this._windowSizeLabel.TabIndex = 7;
            this._windowSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _windowSizeBox
            // 
            this._windowSizeBox.Location = new System.Drawing.Point(224, 48);
            this._windowSizeBox.MaxLength = 5;
            this._windowSizeBox.Name = "_windowSizeBox";
            this._windowSizeBox.Size = new System.Drawing.Size(120, 19);
            this._windowSizeBox.TabIndex = 8;
            this._windowSizeBox.Text = "0";
            // 
            // _sshMiscGroup
            // 
            this._sshMiscGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._sshCheckMAC,
                this._retainsPassphrase});
            this._sshMiscGroup.Location = new System.Drawing.Point(8, 196);
            this._sshMiscGroup.Name = "_sshMiscGroup";
            this._sshMiscGroup.Size = new System.Drawing.Size(368, 72);
            this._sshMiscGroup.TabIndex = 9;
            this._sshMiscGroup.FlatStyle = FlatStyle.System;
            this._sshMiscGroup.TabStop = false;
            // 
            // _retainsPassphrase
            // 
            this._retainsPassphrase.Location = new System.Drawing.Point(8, 14);
            this._retainsPassphrase.Name = "_retainsPassphrase";
            this._retainsPassphrase.FlatStyle = FlatStyle.System;
            this._retainsPassphrase.Size = new System.Drawing.Size(352, 23);
            this._retainsPassphrase.TabIndex = 10;
            // 
            // _sshCheckMAC
            // 
            this._sshCheckMAC.Location = new System.Drawing.Point(8, 33);
            this._sshCheckMAC.Name = "_ssh1CheckMAC";
            this._sshCheckMAC.FlatStyle = FlatStyle.System;
            this._sshCheckMAC.Size = new System.Drawing.Size(352, 37);
            this._sshCheckMAC.TabIndex = 11;

            //
            //_connectionPage
            //
            this._connectionPage.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._useSocks,
                this._socksGroup});
            this._connectionPage.Location = new System.Drawing.Point(4, 21);
            this._connectionPage.Name = "_connectionPage";
            //this._connectionPage.BackColor = ThemeUtil.TabPaneBackColor;
            this._connectionPage.Size = new System.Drawing.Size(386, 279);
            this._connectionPage.Validating += new System.ComponentModel.CancelEventHandler(this.OnVerifyTab);
            //
            //_useSocks
            //
            this._useSocks.Location = new System.Drawing.Point(16, 3);
            this._useSocks.Name = "_useSocksAuthentication";
            this._useSocks.FlatStyle = FlatStyle.System;
            this._useSocks.Size = new System.Drawing.Size(160, 23);
            this._useSocks.TabIndex = 1;
            this._useSocks.CheckedChanged += new EventHandler(OnUseSocksOptionChanged);
            this._useSocks.FlatStyle = FlatStyle.System;
            //
            //_socksGroup
            //
            this._socksGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._socksServerLabel,
                this._socksServerBox,
                this._socksPortLabel,
                this._socksPortBox,
                this._socksAccountLabel,
                this._socksAccountBox,
                this._socksPasswordLabel,
                this._socksPasswordBox,
                this._socksNANetworksLabel,
                this._socksNANetworksBox});
            this._socksGroup.Location = new System.Drawing.Point(8, 9);
            this._socksGroup.Name = "_socksGroup";
            this._socksGroup.Size = new System.Drawing.Size(368, 128);
            this._socksGroup.TabIndex = 2;
            this._socksGroup.FlatStyle = FlatStyle.System;
            this._socksGroup.TabStop = false;
            this._socksGroup.Text = "";
            //
            //_socksServerLabel
            //
            this._socksServerLabel.Location = new System.Drawing.Point(8, 18);
            this._socksServerLabel.Name = "_socksServerLabel";
            this._socksServerLabel.Size = new System.Drawing.Size(80, 23);
            this._socksServerLabel.TabIndex = 0;
            this._socksServerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            //_socksServerBox
            //
            this._socksServerBox.Location = new System.Drawing.Point(96, 18);
            this._socksServerBox.Name = "_socksServerBox";
            this._socksServerBox.Size = new System.Drawing.Size(80, 19);
            this._socksServerBox.Enabled = false;
            this._socksServerBox.TabIndex = 1;
            //
            //_socksPortLabel
            //
            this._socksPortLabel.Location = new System.Drawing.Point(192, 18);
            this._socksPortLabel.Name = "_socksPortLabel";
            this._socksPortLabel.Size = new System.Drawing.Size(80, 23);
            this._socksPortLabel.TabIndex = 2;
            this._socksPortLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            //_socksPortBox
            //
            this._socksPortBox.Location = new System.Drawing.Point(280, 18);
            this._socksPortBox.Name = "_socksPortBox";
            this._socksPortBox.Size = new System.Drawing.Size(80, 19);
            this._socksPortBox.Enabled = false;
            this._socksPortBox.TabIndex = 3;
            //
            //_socksAccountLabel
            //
            this._socksAccountLabel.Location = new System.Drawing.Point(8, 40);
            this._socksAccountLabel.Name = "_socksAccountLabel";
            this._socksAccountLabel.Size = new System.Drawing.Size(80, 23);
            this._socksAccountLabel.TabIndex = 4;
            this._socksAccountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            //_socksAccountBox
            //
            this._socksAccountBox.Location = new System.Drawing.Point(96, 40);
            this._socksAccountBox.Name = "_socksAccountBox";
            this._socksAccountBox.Size = new System.Drawing.Size(80, 19);
            this._socksAccountBox.Enabled = false;
            this._socksAccountBox.TabIndex = 5;
            //
            //_socksPasswordLabel
            //
            this._socksPasswordLabel.Location = new System.Drawing.Point(192, 40);
            this._socksPasswordLabel.Name = "_socksPasswordLabel";
            this._socksPasswordLabel.Size = new System.Drawing.Size(80, 23);
            this._socksPasswordLabel.TabIndex = 6;
            this._socksPasswordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            //_socksPasswordBox
            //
            this._socksPasswordBox.Location = new System.Drawing.Point(280, 40);
            this._socksPasswordBox.Name = "_socksPasswordBox";
            this._socksPasswordBox.PasswordChar = '*';
            this._socksPasswordBox.Enabled = false;
            this._socksPasswordBox.Size = new System.Drawing.Size(80, 19);
            this._socksPasswordBox.TabIndex = 7;
            //
            //_socksNANetworksLabel
            //
            this._socksNANetworksLabel.Location = new System.Drawing.Point(8, 68);
            this._socksNANetworksLabel.Name = "_socksNANetworksLabel";
            this._socksNANetworksLabel.Size = new System.Drawing.Size(352, 28);
            this._socksNANetworksLabel.TabIndex = 8;
            this._socksNANetworksLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            //_socksNANetworksBox
            //
            this._socksNANetworksBox.Location = new System.Drawing.Point(8, 98);
            this._socksNANetworksBox.Name = "_socksNANetworksBox";
            this._socksNANetworksBox.Enabled = false;
            this._socksNANetworksBox.Size = new System.Drawing.Size(352, 19);
            this._socksNANetworksBox.TabIndex = 9;
            // 
            // _genericPage
            // 
            this._genericPage.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._showInTaskBarOption,
                this._warningOnExit,
                this._optionPreservePlaceLabel,
                this._optionPreservePlacePath,
                this._optionPreservePlace,
                this._languageLabel,
                this._languageBox});
            this._genericPage.Location = new System.Drawing.Point(4, 21);
            this._genericPage.Name = "_genericPage";
            //this._genericPage.BackColor = ThemeUtil.TabPaneBackColor;
            this._genericPage.Size = new System.Drawing.Size(386, 279);
            this._genericPage.Validating += new System.ComponentModel.CancelEventHandler(this.OnVerifyTab);
            // 
            // _languageLabel
            // 
            this._languageLabel.Location = new System.Drawing.Point(8, 8);
            this._languageLabel.Name = "_languageLabel";
            this._languageLabel.Size = new System.Drawing.Size(168, 24);
            this._languageLabel.TabIndex = 0;
            this._languageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _languageBox
            // 
            this._languageBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._languageBox.Location = new System.Drawing.Point(184, 8);
            this._languageBox.Name = "_languageBox";
            this._languageBox.Size = new System.Drawing.Size(176, 20);
            this._languageBox.TabIndex = 1;
            // 
            // _showInTaskBarOption
            // 
            this._showInTaskBarOption.Location = new System.Drawing.Point(16, 32);
            this._showInTaskBarOption.Name = "_showInTaskBarOption";
            this._showInTaskBarOption.FlatStyle = FlatStyle.System;
            this._showInTaskBarOption.Size = new System.Drawing.Size(264, 23);
            this._showInTaskBarOption.TabIndex = 2;
            // 
            // _warningOnExit
            // 
            this._warningOnExit.Location = new System.Drawing.Point(16, 56);
            this._warningOnExit.Name = "_warningOnExit";
            this._warningOnExit.FlatStyle = FlatStyle.System;
            this._warningOnExit.Size = new System.Drawing.Size(264, 23);
            this._warningOnExit.TabIndex = 3;
            // 
            // _optionPreservePlaceLabel
            // 
            this._optionPreservePlaceLabel.Location = new System.Drawing.Point(8, 80);
            this._optionPreservePlaceLabel.Name = "_optionPreservePlaceLabel";
            this._optionPreservePlaceLabel.Size = new System.Drawing.Size(168, 24);
            this._optionPreservePlaceLabel.TabIndex = 4;
            this._optionPreservePlaceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _optionPreservePlaceBox
            // 
            this._optionPreservePlace.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._optionPreservePlace.Location = new System.Drawing.Point(184, 80);
            this._optionPreservePlace.Name = "_optionPreservePlaceBox";
            this._optionPreservePlace.Size = new System.Drawing.Size(176, 20);
            this._optionPreservePlace.TabIndex = 5;
            this._optionPreservePlace.SelectedIndexChanged += new System.EventHandler(this.OnOptionPreservePlaceChanged);
            // 
            // _optionPreservePlacePath
            // 
            this._optionPreservePlacePath.Location = new System.Drawing.Point(8, 104);
            this._optionPreservePlacePath.BorderStyle = BorderStyle.FixedSingle;
            this._optionPreservePlacePath.Name = "_optionPreservePlacePath";
            this._optionPreservePlacePath.Size = new System.Drawing.Size(352, 36);
            this._optionPreservePlacePath.TabIndex = 6;
            this._optionPreservePlacePath.TextAlign = System.Drawing.ContentAlignment.TopLeft;
            // 
            // OptionDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(410, 343);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._tabControl,
                this._okButton,
                this._cancelButton});
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.OptionDialog_Load);
            this._tabControl.ResumeLayout(false);
            this._sshPage.ResumeLayout(false);
            this._cipherOrderGroup.ResumeLayout(false);
            this._ssh2OptionGroup.ResumeLayout(false);
            this._sshMiscGroup.ResumeLayout(false);
            this._connectionPage.ResumeLayout(false);
            this._socksGroup.ResumeLayout(false);
            this._genericPage.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void InitializeText() {
            this._okButton.Text = Env.Strings.GetString("Common.OK");
            this._cancelButton.Text = Env.Strings.GetString("Common.Cancel");
            this._cipherOrderGroup.Text = Env.Strings.GetString("Form.OptionDialog._cipherOrderGroup");
            this._algorithmOrderUp.Text = Env.Strings.GetString("Form.OptionDialog._algorithmOrderUp");
            this._algorithmOrderDown.Text = Env.Strings.GetString("Form.OptionDialog._algorithmOrderDown");
            this._ssh2OptionGroup.Text = Env.Strings.GetString("Form.OptionDialog._ssh2OptionGroup");
            this._hostKeyLabel.Text = Env.Strings.GetString("Form.OptionDialog._hostKeyLabel");
            this._windowSizeLabel.Text = Env.Strings.GetString("Form.OptionDialog._windowSizeLabel");
            this._sshMiscGroup.Text = Env.Strings.GetString("Form.OptionDialog._sshMiscGroup");
            this._retainsPassphrase.Text = Env.Strings.GetString("Form.OptionDialog._retainsPassphrase");
            this._sshCheckMAC.Text = Env.Strings.GetString("Form.OptionDialog._sshCheckMAC");
            this._connectionPage.Text = Env.Strings.GetString("Form.OptionDialog._connectionPage");
            this._useSocks.Text = Env.Strings.GetString("Form.OptionDialog._useSocks");
            this._socksServerLabel.Text = Env.Strings.GetString("Form.OptionDialog._socksServerLabel");
            this._socksPortLabel.Text = Env.Strings.GetString("Form.OptionDialog._socksPortLabel");
            this._socksAccountLabel.Text = Env.Strings.GetString("Form.OptionDialog._socksAccountLabel");
            this._socksPasswordLabel.Text = Env.Strings.GetString("Form.OptionDialog._socksPasswordLabel");
            this._socksNANetworksLabel.Text = Env.Strings.GetString("Form.OptionDialog._socksNANetworksLabel");
            this._genericPage.Text = Env.Strings.GetString("Form.OptionDialog._genericPage");
            this._showInTaskBarOption.Text = Env.Strings.GetString("Form.OptionDialog._showInTaskBarOption");
            this._warningOnExit.Text = Env.Strings.GetString("Form.OptionDialog._warningOnExit");
            this._optionPreservePlaceLabel.Text = Env.Strings.GetString("Form.OptionDialog._optionPreservePlaceLabel");
            this._languageLabel.Text = Env.Strings.GetString("Form.OptionDialog._languageLabel");
            this.Text = Env.Strings.GetString("Form.OptionDialog.Text");
        }

        private void OptionDialog_Load(object sender, System.EventArgs args) {
            _options = (Options)Env.Options.Clone();

            //SSH
            string[] co = _options.CipherAlgorithmOrder;
            foreach (string c in co)
                _cipherOrderList.Items.Add(c);
            _hostKeyBox.SelectedIndex = SSHUtil.ParsePublicKeyAlgorithm(_options.HostKeyAlgorithmOrder[0]) == PublicKeyAlgorithm.DSA ? 0 : 1; //これはDSA/RSAのどちらかしかない
            _windowSizeBox.Text = _options.SSHWindowSize.ToString();
            _retainsPassphrase.Checked = _options.RetainsPassphrase;
            _sshCheckMAC.Checked = _options.SSHCheckMAC;

            //接続
            _useSocks.Checked = _options.UseSocks;
            _socksServerBox.Text = _options.SocksServer;
            _socksPortBox.Text = _options.SocksPort.ToString();
            _socksAccountBox.Text = _options.SocksAccount;
            _socksPasswordBox.Text = _options.SocksPassword;
            _socksNANetworksBox.Text = _options.SocksNANetworks;

            //一般
            _showInTaskBarOption.Checked = _options.ShowInTaskBar;
            _warningOnExit.Checked = _options.WarningOnExit;
            _optionPreservePlace.SelectedItem = _options.OptionPreservePlace;   // select EnumListItem<T> by T
            _languageBox.SelectedItem = _options.Language;      // select EnumListItem<T> by T
        }
        private void OnTabChanged(object sender, System.EventArgs args) {
        }
        private void OnVerifyTab(object sender, CancelEventArgs args) {
        }

        private bool CommitCurrentTab() {
            bool ok;
            switch (_tabControl.SelectedIndex) {
                case 0:
                    ok = CommitSSHOptions();
                    break;
                case 1:
                    ok = CommitConnectionOptions();
                    break;
                case 2:
                    ok = CommitGenericOptions();
                    break;
                default:
                    ok = true;
                    break;
            }
            return ok;
        }

        private void OnOK(object sender, EventArgs args) {
            bool ok = CommitCurrentTab();
            if (ok) {
                DialogResult = DialogResult.OK;

                Env.UpdateOptions(_options);
            }
            else {
                DialogResult = DialogResult.None;
            }
        }
        protected override void OnClosed(EventArgs args) {
            _FIRSTTABPAGE = _tabControl.SelectedIndex;
        }

        private bool CommitSSHOptions() {
            //暗号アルゴリズム順序は_optionsを直接いじっているのでここでは何もしなくてよい
            try {
                PublicKeyAlgorithm[] pa = new PublicKeyAlgorithm[2];
                if (_hostKeyBox.SelectedIndex == 0) {
                    pa[0] = PublicKeyAlgorithm.DSA;
                    pa[1] = PublicKeyAlgorithm.RSA;
                }
                else {
                    pa[0] = PublicKeyAlgorithm.RSA;
                    pa[1] = PublicKeyAlgorithm.DSA;
                }
                _options.HostKeyAlgorithmOrder = SSHUtil.FormatPublicKeyAlgorithmList(pa);

                try {
                    _options.SSHWindowSize = Int32.Parse(_windowSizeBox.Text);
                }
                catch (FormatException) {
                    Util.Warning(this, Env.Strings.GetString("Message.OptionDialog.InvalidWindowSize"));
                    return false;
                }

                _options.RetainsPassphrase = _retainsPassphrase.Checked;
                _options.SSHCheckMAC = _sshCheckMAC.Checked;

                return true;
            }
            catch (Exception ex) {
                Util.Warning(this, ex.Message);
                return false;
            }
        }
        private bool CommitConnectionOptions() {
            string itemname = "";
            try {
                _options.UseSocks = _useSocks.Checked;
                if (_options.UseSocks && _socksServerBox.Text.Length == 0)
                    throw new Exception(Env.Strings.GetString("Message.OptionDialog.EmptySOCKSServer"));
                _options.SocksServer = _socksServerBox.Text;
                itemname = Env.Strings.GetString("Caption.OptionDialog.SOCKSPort");
                _options.SocksPort = Int32.Parse(_socksPortBox.Text);
                _options.SocksAccount = _socksAccountBox.Text;
                _options.SocksPassword = _socksPasswordBox.Text;
                itemname = Env.Strings.GetString("Caption.OptionDialog.NetworkAddress");
                foreach (string c in _socksNANetworksBox.Text.Split(';')) {
                    if (!NetUtil.IsNetworkAddress(c))
                        throw new FormatException();
                }
                _options.SocksNANetworks = _socksNANetworksBox.Text;

                return true;
            }
            catch (FormatException) {
                Util.Warning(this, String.Format(Env.Strings.GetString("Message.OptionDialog.InvalidItem"), itemname));
                return false;
            }
            catch (Exception ex) {
                Util.Warning(this, ex.Message);
                return false;
            }
        }
        private bool CommitGenericOptions() {
            string itemname = null;
            bool successful = false;
            try {
                _options.ShowInTaskBar = _showInTaskBarOption.Checked;
                _options.WarningOnExit = _warningOnExit.Checked;

                OptionPreservePlace optionPreservePlace = ((EnumListItem<OptionPreservePlace>)_optionPreservePlace.SelectedItem).Value;
                if (Env.Options.OptionPreservePlace != optionPreservePlace && !Env.IsRegistryWritable) {
                    Util.Warning(this, Env.Strings.GetString("Message.OptionDialog.RegistryAuthRequired"));
                    return false;
                }
                _options.OptionPreservePlace = optionPreservePlace;

                _options.Language = ((EnumListItem<Language>)_languageBox.SelectedItem).Value;
                if (_options.Language == Language.Japanese && Env.Options.EnvLanguage == Language.English) {
                    if (Util.AskUserYesNo(this, Env.Strings.GetString("Message.OptionDialog.AskJapaneseFont")) == DialogResult.No)
                        return false;
                }
                successful = true;
            }
            catch (FormatException) {
                Util.Warning(this, String.Format(Env.Strings.GetString("Message.OptionDialog.InvalidItem"), itemname));
            }

            return successful;
        }


        //SSHオプション関係
        private void OnCipherAlgorithmOrderUp(object sender, EventArgs args) {
            int i = _cipherOrderList.SelectedIndex;
            if (i == -1 || i == 0)
                return; //選択されていないか既にトップなら何もしない

            string temp1 = _options.CipherAlgorithmOrder[i];
            _options.CipherAlgorithmOrder[i] = _options.CipherAlgorithmOrder[i - 1];
            _options.CipherAlgorithmOrder[i - 1] = temp1;

            object temp2 = _cipherOrderList.SelectedItem;
            _cipherOrderList.Items.RemoveAt(i);
            _cipherOrderList.Items.Insert(i - 1, temp2);

            _cipherOrderList.SelectedIndex = i - 1;
        }
        private void OnCipherAlgorithmOrderDown(object sender, EventArgs args) {
            int i = _cipherOrderList.SelectedIndex;
            if (i == -1 || i == _cipherOrderList.Items.Count - 1)
                return; //選択されていなければ何もしない

            string temp1 = _options.CipherAlgorithmOrder[i];
            _options.CipherAlgorithmOrder[i] = _options.CipherAlgorithmOrder[i + 1];
            _options.CipherAlgorithmOrder[i + 1] = temp1;

            object temp2 = _cipherOrderList.SelectedItem;
            _cipherOrderList.Items.RemoveAt(i);
            _cipherOrderList.Items.Insert(i + 1, temp2);

            _cipherOrderList.SelectedIndex = i + 1;
        }

        private void OnOptionPreservePlaceChanged(object sender, EventArgs e) {
            AdjustOptionFileLocation(((EnumListItem<OptionPreservePlace>)_optionPreservePlace.SelectedItem).Value);
        }
        private void AdjustOptionFileLocation(OptionPreservePlace p) {
            _optionPreservePlacePath.Text = Env.GetOptionDirectory(p);
        }

        //SOCKSのUI
        private void OnUseSocksOptionChanged(object sender, EventArgs args) {
            bool e = _useSocks.Checked;
            _socksServerBox.Enabled = e;
            _socksPortBox.Enabled = e;
            _socksAccountBox.Enabled = e;
            _socksPasswordBox.Enabled = e;
            _socksNANetworksBox.Enabled = e;
        }
    }
}
