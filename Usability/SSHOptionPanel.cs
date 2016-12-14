/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SSHOptionPanel.cs,v 1.5 2011/10/27 23:21:59 kzmi Exp $
 */
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

namespace Poderosa.Forms {
    internal class SSHOptionPanel : UserControl {
        private string[] _cipherAlgorithmOrder;

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
            this._hostKeyLabel = new System.Windows.Forms.Label();
            this._hostKeyBox = new System.Windows.Forms.ComboBox();
            this._windowSizeLabel = new System.Windows.Forms.Label();
            this._windowSizeBox = new System.Windows.Forms.TextBox();
            this._sshMiscGroup = new System.Windows.Forms.GroupBox();
            this._sshCheckMAC = new System.Windows.Forms.CheckBox();
            this._retainsPassphrase = new System.Windows.Forms.CheckBox();
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
            this._ssh2OptionGroup.Controls.Add(this._hostKeyLabel);
            this._ssh2OptionGroup.Controls.Add(this._hostKeyBox);
            this._ssh2OptionGroup.Controls.Add(this._windowSizeLabel);
            this._ssh2OptionGroup.Controls.Add(this._windowSizeBox);
            this._ssh2OptionGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._ssh2OptionGroup.Location = new System.Drawing.Point(8, 96);
            this._ssh2OptionGroup.Name = "_ssh2OptionGroup";
            this._ssh2OptionGroup.Size = new System.Drawing.Size(416, 80);
            this._ssh2OptionGroup.TabIndex = 4;
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
            this._sshMiscGroup.Controls.Add(this._sshCheckMAC);
            this._sshMiscGroup.Controls.Add(this._retainsPassphrase);
            this._sshMiscGroup.Controls.Add(this._sshEventLog);
            this._sshMiscGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._sshMiscGroup.Location = new System.Drawing.Point(8, 180);
            this._sshMiscGroup.Name = "_sshMiscGroup";
            this._sshMiscGroup.Size = new System.Drawing.Size(416, 105);
            this._sshMiscGroup.TabIndex = 9;
            this._sshMiscGroup.TabStop = false;
            // 
            // _sshCheckMAC
            // 
            this._sshCheckMAC.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._sshCheckMAC.Location = new System.Drawing.Point(8, 34);
            this._sshCheckMAC.Name = "_sshCheckMAC";
            this._sshCheckMAC.Size = new System.Drawing.Size(400, 37);
            this._sshCheckMAC.TabIndex = 12;
            // 
            // _retainsPassphrase
            // 
            this._retainsPassphrase.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._retainsPassphrase.Location = new System.Drawing.Point(8, 12);
            this._retainsPassphrase.Name = "_retainsPassphrase";
            this._retainsPassphrase.Size = new System.Drawing.Size(400, 23);
            this._retainsPassphrase.TabIndex = 11;
            // 
            // _sshEventLog
            // 
            this._sshEventLog.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._sshEventLog.Location = new System.Drawing.Point(8, 70);
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
            this.Size = new System.Drawing.Size(381, 279);
            this._cipherOrderGroup.ResumeLayout(false);
            this._ssh2OptionGroup.ResumeLayout(false);
            this._ssh2OptionGroup.PerformLayout();
            this._sshMiscGroup.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._cipherOrderGroup.Text = sr.GetString("Form.OptionDialog._cipherOrderGroup");
            this._algorithmOrderUp.Text = sr.GetString("Form.OptionDialog._algorithmOrderUp");
            this._algorithmOrderDown.Text = sr.GetString("Form.OptionDialog._algorithmOrderDown");
            this._ssh2OptionGroup.Text = sr.GetString("Form.OptionDialog._ssh2OptionGroup");
            this._hostKeyLabel.Text = sr.GetString("Form.OptionDialog._hostKeyLabel");
            this._windowSizeLabel.Text = sr.GetString("Form.OptionDialog._windowSizeLabel");
            this._sshMiscGroup.Text = sr.GetString("Form.OptionDialog._sshMiscGroup");
            this._retainsPassphrase.Text = sr.GetString("Form.OptionDialog._retainsPassphrase");
            this._sshCheckMAC.Text = sr.GetString("Form.OptionDialog._sshCheckMAC");
            this._sshEventLog.Text = sr.GetString("Form.OptionDialog._sshEventLog");
        }
        public void InitUI(IProtocolOptions options, IKeyAgentOptions agent) {
            _cipherOrderList.Items.Clear();
            string[] co = options.CipherAlgorithmOrder;
            foreach (string c in co)
                _cipherOrderList.Items.Add(c);
            _hostKeyBox.SelectedIndex = ParsePublicKeyAlgorithm(options.HostKeyAlgorithmOrder[0]) == PublicKeyAlgorithm.DSA ? 0 : 1; //これはDSA/RSAのどちらかしかない
            _windowSizeBox.Text = options.SSHWindowSize.ToString();
            _retainsPassphrase.Checked = options.RetainsPassphrase;
            _sshCheckMAC.Checked = options.SSHCheckMAC;
            _sshEventLog.Checked = options.LogSSHEvents;
            _cipherAlgorithmOrder = options.CipherAlgorithmOrder;
        }
        public bool Commit(IProtocolOptions options, IKeyAgentOptions agent) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            //暗号アルゴリズム順序はoptionsを直接いじっているのでここでは何もしなくてよい
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
                options.HostKeyAlgorithmOrder = FormatPublicKeyAlgorithmList(pa);

                try {
                    options.SSHWindowSize = Int32.Parse(_windowSizeBox.Text);
                }
                catch (FormatException) {
                    GUtil.Warning(this, sr.GetString("Message.OptionDialog.InvalidWindowSize"));
                    return false;
                }

                options.RetainsPassphrase = _retainsPassphrase.Checked;
                options.SSHCheckMAC = _sshCheckMAC.Checked;
                options.CipherAlgorithmOrder = _cipherAlgorithmOrder;
                options.LogSSHEvents = _sshEventLog.Checked;

                return true;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                return false;
            }
        }

        //SSHオプション関係
        private void OnCipherAlgorithmOrderUp(object sender, EventArgs args) {
            int i = _cipherOrderList.SelectedIndex;
            if (i == -1 || i == 0)
                return; //選択されていないか既にトップなら何もしない

            string temp1 = _cipherAlgorithmOrder[i];
            _cipherAlgorithmOrder[i] = _cipherAlgorithmOrder[i - 1];
            _cipherAlgorithmOrder[i - 1] = temp1;

            object temp2 = _cipherOrderList.SelectedItem;
            _cipherOrderList.Items.RemoveAt(i);
            _cipherOrderList.Items.Insert(i - 1, temp2);

            _cipherOrderList.SelectedIndex = i - 1;
        }
        private void OnCipherAlgorithmOrderDown(object sender, EventArgs args) {
            int i = _cipherOrderList.SelectedIndex;
            if (i == -1 || i == _cipherOrderList.Items.Count - 1)
                return; //選択されていなければ何もしない

            string temp1 = _cipherAlgorithmOrder[i];
            _cipherAlgorithmOrder[i] = _cipherAlgorithmOrder[i + 1];
            _cipherAlgorithmOrder[i + 1] = temp1;

            object temp2 = _cipherOrderList.SelectedItem;
            _cipherOrderList.Items.RemoveAt(i);
            _cipherOrderList.Items.Insert(i + 1, temp2);

            _cipherOrderList.SelectedIndex = i + 1;
        }
        private static string[] FormatPublicKeyAlgorithmList(PublicKeyAlgorithm[] value) {
            string[] ret = new string[value.Length];
            int i = 0;
            foreach (PublicKeyAlgorithm a in value)
                ret[i++] = a.ToString();
            return ret;
        }


        private static PublicKeyAlgorithm ParsePublicKeyAlgorithm(string t) {
            if (t == "DSA")
                return PublicKeyAlgorithm.DSA;
            else if (t == "RSA")
                return PublicKeyAlgorithm.RSA;
            else
                throw new Exception("Unknown PublicKeyAlgorithm " + t);
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
