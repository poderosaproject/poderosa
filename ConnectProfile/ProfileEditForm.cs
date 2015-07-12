using System;
using System.IO;
using System.Windows.Forms;

using Poderosa.ConnectionParam;
using Poderosa.Terminal;
using Poderosa.UI;
using Poderosa.Util;


namespace Poderosa.ConnectProfile {
    /// <summary>
    /// プロファイルの追加/編集フォームクラス
    /// </summary>
    public partial class ProfileEditForm : Form {
        private ConnectProfileStruct _result;
        private bool _Initialized = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="prof">プロファイル</param>
        public ProfileEditForm(ConnectProfileStruct prof) {
            InitializeComponent();
            InitializeComponentValue();

            // オブジェクト初期値設定
            if (prof == null) {
                // 新規作成時はデフォルト値を設定(フォント色/背景色はターミナルオプション値を反映)
                ITerminalEmulatorOptions terminalOptions = ConnectProfilePlugin.Instance.TerminalEmulatorService.TerminalEmulatorOptions;
                _protocolBox.SelectedItem = ConnectionMethod.SSH2;
                _portBox.Value = ConnectProfileStruct.DEFAULT_SSH_PORT;
                _authTypeBox.SelectedItem = AuthType.Password;
                _charCodeBox.SelectedItem = EncodingType.UTF8;
                _newLineTypeBox.SelectedItem = NewLine.CR;
                _telnetNewLineCheck.Checked = true;
                _terminalTypeBox.SelectedItem = TerminalType.XTerm;
                _terminalFontColorButton.SelectedColor = terminalOptions.TextColor;
                _terminalBGColorButton.SelectedColor = terminalOptions.BGColor;
                _commandSendIntBox.Value = ConnectProfileStruct.DEFAULT_CMD_SEND_INTERVAL;
                _promptRecvTimeoutBox.Value = ConnectProfileStruct.DEFAULT_PROMPT_RECV_TIMEOUT;
            } else {
                // 編集時は対象プロファイル値を設定
                _hostNameBox.Text = prof.HostName;
                _protocolBox.SelectedItem = prof.Protocol;
                _portBox.Value = prof.Port;
                _authTypeBox.SelectedItem = prof.AuthType;
                _keyFileBox.Text = prof.KeyFile;
                _userNameBox.Text = prof.UserName;
                _passwordBox.Text = prof.Password;
                _autoLoginCheck.Checked = prof.AutoLogin;
                _loginPromptBox.Text = prof.LoginPrompt;
                _passwordPromptBox.Text = prof.PasswordPrompt;
                _execCommandBox.Text = prof.ExecCommand;
                _suUserNameBox.Text = prof.SUUserName;
                _suPasswordBox.Text = prof.SUPassword;
                if (prof.SUType == _suTypeRadio1.Text) _suTypeRadio1.Checked = true;
                else if (prof.SUType == _suTypeRadio2.Text) _suTypeRadio2.Checked = true;
                _charCodeBox.SelectedItem = prof.CharCode;
                _newLineTypeBox.SelectedItem = prof.NewLine;
                _telnetNewLineCheck.Checked = prof.TelnetNewLine;
                _terminalTypeBox.SelectedItem = prof.TerminalType;
                _terminalFontColorButton.SelectedColor = prof.TerminalFontColor;
                _terminalBGColorButton.SelectedColor = prof.TerminalBGColor;
                _commandSendIntBox.Value = prof.CommandSendInterval;
                _promptRecvTimeoutBox.Value = prof.PromptRecvTimeout;
                _profileItemColorButton.SelectedColor = prof.ProfileItemColor;
                _descriptionBox.Text = prof.Description;
            }

            _Initialized = true;
            EnableValidControls(this, EventArgs.Empty);
        }

        /// <summary>
        /// オブジェクトの各値を設定
        /// </summary>
        private void InitializeComponentValue() {
            // テキスト
            this.Text = ConnectProfilePlugin.Strings.GetString("Caption.AddProfile");
            this._accountGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._accountGroup");
            this._authTypeLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._authTypeLabel");
            this._autoLoginCheck.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._autoLoginCheck");
            this._autoLoginGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._autoLoginGroup");
            this._basicGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._basicGroup");
            this._cancelButton.Text = ConnectProfilePlugin.Strings.GetString("Form.Common._cancelButton");
            this._charCodeLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._charCodeLabel");
            this._commandSendIntLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._commandSendIntLabel");
            this._descriptionLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._descriptionLabel");
            this._etcGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._etcGroup");
            this._execCommandLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._execCommandLabel");
            this._hostNameLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._hostNameLabel");
            this._keyFileLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._keyFileLabel");
            this._loginPromptLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._loginPromptLabel");
            this._newLineTypeLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._newLineTypeLabel");
            this._okButton.Text = ConnectProfilePlugin.Strings.GetString("Form.Common._okButton");
            this._openKeyFileButton.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._openKeyFileButton");
            this._passwordLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._passwordLabel");
            this._passwordPromptLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._passwordPromptLabel");
            this._portLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._portLabel");
            this._profileItemColorLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._profileItemColorLabel");
            this._promptRecvTimeoutLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._promptRecvTimeoutLabel");
            this._protocolLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._protocolLabel");
            this._sshGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._sshGroup");
            this._suGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._suGroup");
            this._suPasswordLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._suPasswordLabel");
            this._suTypeLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._suTypeLabel");
            this._suTypeRadio1.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._suTypeRadio1");
            this._suTypeRadio2.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._suTypeRadio2");
            this._suUserNameLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._suUserNameLabel");
            this._telnetNewLineCheck.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._telnetNewLineCheck");
            this._terminalBGColorLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._terminalBGColorLabel");
            this._terminalFontColorLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._terminalFontColorLabel");
            this._terminalGroup.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._terminalGroup");
            this._terminalTypeLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._terminalTypeLabel");
            this._userNameLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile._userNameLabel");

            // コンボボックス
            this._charCodeBox.Items.AddRange(EnumListItem<EncodingType>.GetListItems());
            this._authTypeBox.Items.AddRange(EnumListItem<AuthType>.GetListItems());
            this._newLineTypeBox.Items.AddRange(EnumListItem<NewLine>.GetListItems());
            this._terminalTypeBox.Items.AddRange(EnumListItem<TerminalType>.GetListItems());
            this._protocolBox.Items.AddRange(new object[] {
                new ListItem<ConnectionMethod>(ConnectionMethod.Telnet,ConnectionMethod.Telnet.ToString()),
                new ListItem<ConnectionMethod>(ConnectionMethod.SSH1, ConnectionMethod.SSH1.ToString()),
                new ListItem<ConnectionMethod>(ConnectionMethod.SSH2, ConnectionMethod.SSH2.ToString()),
            });
        }

        /// <summary>
        /// オブジェクトを有効/無効化(オブジェクト共通)
        /// </summary>
        private void EnableValidControls(object sender, EventArgs e) {
            if (_Initialized == true) {
                ConnectionMethod protocol = ((ListItem<ConnectionMethod>)_protocolBox.SelectedItem).Value;
                EnumListItem<AuthType> authTypeItem = (EnumListItem<AuthType>)_authTypeBox.SelectedItem;
                EnumListItem<NewLine> newLineItem = (EnumListItem<NewLine>)_newLineTypeBox.SelectedItem;
                bool autologin = (_autoLoginCheck.Checked);
                bool ssh = (protocol == ConnectionMethod.SSH1 || protocol == ConnectionMethod.SSH2);
                bool pubkey = (authTypeItem != null && authTypeItem.Value == AuthType.PublicKey);
                bool kbd = (authTypeItem != null && authTypeItem.Value == AuthType.KeyboardInteractive);
                bool newline = (newLineItem != null && newLineItem.Value == NewLine.CRLF);
                bool su = (_suUserNameBox.Text != "");
                string watermarktext = ConnectProfilePlugin.Strings.GetString("Form.Common.WaterMark.Required");

                // ユーザ名/パスワード
                _userNameBox.Enabled = (ssh || autologin);
                _passwordBox.Enabled = autologin;

                // 自動ログイン用プロンプト
                _loginPromptBox.Enabled = (autologin && !ssh);
                _passwordPromptBox.Enabled = ((autologin && !ssh) || (autologin && su));

                // 秘密鍵ファイル
                _authTypeBox.Enabled = ssh;
                _keyFileBox.Enabled = (ssh && pubkey);
                _openKeyFileButton.Enabled = (ssh && pubkey);

                // 実行コマンド
                _execCommandBox.Enabled = autologin;

                // SU
                _suUserNameBox.Enabled = autologin;
                _suPasswordBox.Enabled = (su && autologin);
                _suTypeRadio1.Enabled = (su && autologin);
                _suTypeRadio2.Enabled = (su && autologin);

                // TelnetNewLine
                _telnetNewLineCheck.Enabled = (!ssh && newline);

                // コマンド発行間隔/プロンプト受信タイムアウト
                _commandSendIntBox.Enabled = autologin;
                _promptRecvTimeoutBox.Enabled = autologin;

                // ポート番号
                _portBox.Value = ssh ? ConnectProfileStruct.DEFAULT_SSH_PORT : ConnectProfileStruct.DEFAULT_TELNET_PORT;

                // ウォーターマーク
                _hostNameBox.WaterMarkText = watermarktext;
                _userNameBox.WaterMarkText = (_userNameBox.Enabled) ? watermarktext : "";
                _keyFileBox.WaterMarkText = (_keyFileBox.Enabled) ? watermarktext : "";
                _loginPromptBox.WaterMarkText = (_loginPromptBox.Enabled) ? watermarktext : "";
                _passwordPromptBox.WaterMarkText = (_passwordPromptBox.Enabled) ? watermarktext : "";
            }
        }

        /// <summary>
        /// 説明文を表示(オブジェクト共通)
        /// </summary>
        private void ShowHint(object sender, EventArgs e) {
            if (sender == _hostNameBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._hostNameBox");
            } else if (sender == _protocolBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._protocolBox");
            } else if (sender == _portBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._portBox");
            } else if (sender == _authTypeBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._authTypeBox");
            } else if (sender == _keyFileBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._keyFileBox");
            } else if (sender == _userNameBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._userNameBox");
            } else if (sender == _passwordBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._passwordBox");
            } else if (sender == _autoLoginCheck) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._autoLoginCheck");
            } else if (sender == _loginPromptBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._loginPromptBox");
            } else if (sender == _passwordPromptBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._passwordPromptBox");
            } else if (sender == _execCommandBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._execCommandBox");
            } else if (sender == _suUserNameBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._suUserNameBox");
            } else if (sender == _suPasswordBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._suPasswordBox");
            } else if ((sender == _suTypeRadio1) || sender == _suTypeRadio2) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._suTypeRadio");
            } else if (sender == _charCodeBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._charCodeBox");
            } else if (sender == _newLineTypeBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._newLineTypeBox");
            } else if (sender == _telnetNewLineCheck) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._telnetNewLineCheck");
            } else if (sender == _terminalTypeBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._terminalTypeBox");
            } else if (sender == _terminalFontColorButton) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._terminalFontColorButton");
            } else if (sender == _terminalBGColorButton) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._terminalBGColorButton");
            } else if (sender == _commandSendIntBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._commandSendIntBox");
            } else if (sender == _promptRecvTimeoutBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._promptRecvTimeoutBox");
            } else if (sender == _profileItemColorButton) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._profileItemColorButton");
            } else if (sender == _descriptionBox) {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint._descriptionBox");
            } else {
                this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.AddProfile.Hint.None");
            }
        }

        /// <summary>
        /// OKボタンクリックイベント
        /// </summary>
        private void _okButton_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.None;

            // 入力チェック
            try {
                // 初期化
                _result = new ConnectProfileStruct();
                _result.HostName = _hostNameBox.Text;
                _result.Protocol = ((ListItem<ConnectionMethod>)_protocolBox.SelectedItem).Value;
                _result.Port = (int)_portBox.Value;
                _result.AuthType = ((EnumListItem<AuthType>)_authTypeBox.SelectedItem).Value;
                _result.KeyFile = "";
                _result.UserName = "";
                _result.Password = _passwordBox.Text;
                _result.AutoLogin = _autoLoginCheck.Checked;
                _result.LoginPrompt = "";
                _result.PasswordPrompt = "";
                _result.ExecCommand = "";
                _result.SUUserName = "";
                _result.SUPassword = "";
                _result.SUType = "";
                _result.CharCode = ((EnumListItem<EncodingType>)_charCodeBox.SelectedItem).Value;
                _result.NewLine = ((EnumListItem<NewLine>)_newLineTypeBox.SelectedItem).Value;
                _result.TelnetNewLine = _telnetNewLineCheck.Checked;
                _result.TerminalType = ((EnumListItem<TerminalType>)_terminalTypeBox.SelectedItem).Value;
                _result.TerminalFontColor = _terminalFontColorButton.SelectedColor;
                _result.TerminalBGColor = _terminalBGColorButton.SelectedColor;
                _result.CommandSendInterval = (int)_commandSendIntBox.Value;
                _result.PromptRecvTimeout = (int)_promptRecvTimeoutBox.Value;
                _result.ProfileItemColor = _profileItemColorButton.SelectedColor;
                _result.Description = _descriptionBox.Text;

                // ホスト名
                if (_hostNameBox.Text == "") throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.EmptyHostName"));

                // 秘密鍵ファイル
                if (_keyFileBox.Enabled == true) {
                    if (_keyFileBox.Text != "") {
                        if (File.Exists(_keyFileBox.Text)) _result.KeyFile = _keyFileBox.Text;
                        else throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.KeyFileNotExist"));
                    } else {
                        throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.EmptyKeyFile"));
                    }
                }

                // ユーザ名
                if (_userNameBox.Enabled == true) {
                    if (_userNameBox.Text != "") _result.UserName = _userNameBox.Text;
                    else throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.EmptyUserName"));
                }

                // ログインプロンプト
                if (_loginPromptBox.Enabled == true) {
                    if (_loginPromptBox.Text != "") _result.LoginPrompt = _loginPromptBox.Text;
                    else throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.EmptyLoginPrompt"));
                }

                // パスワードプロンプト
                if (_passwordPromptBox.Enabled == true) {
                    if (_passwordPromptBox.Text != "") _result.PasswordPrompt = _passwordPromptBox.Text;
                    else throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.EmptyPasswordPrompt"));
                }

                // 実行コマンド
                if ((_execCommandBox.Enabled == true) && _execCommandBox.Text != "") _result.ExecCommand = _execCommandBox.Text;

                // SU
                if ((_suUserNameBox.Enabled == true) && _suUserNameBox.Text != "") {
                    _result.SUUserName = _suUserNameBox.Text;
                    _result.SUPassword = _suPasswordBox.Text;
                    if (_suTypeRadio1.Checked == true) _result.SUType = _suTypeRadio1.Text;
                    else if (_suTypeRadio2.Checked == true) _result.SUType = _suTypeRadio2.Text;
                    else throw new Exception(ConnectProfilePlugin.Strings.GetString("Message.AddProfile.SUTypeNotSelect"));
                }

                this.DialogResult = DialogResult.OK;
            } catch (Exception ex) {
                ConnectProfilePlugin.MessageBoxInvoke(ex.Message, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 秘密鍵ファイル選択ボタンクリックイベント
        /// </summary>
        private void _openKeyFileButton_Click(object sender, EventArgs e) {
            string fn = TerminalUtil.SelectPrivateKeyFileByDialog(this);
            if (fn != null) _keyFileBox.Text = fn;
            _keyFileBox.Focus();
        }

        /// <summary>
        /// 設定済みプロファイル
        /// </summary>
        public ConnectProfileStruct ResultProfile {
            get { return _result; }
        }
    }
}
