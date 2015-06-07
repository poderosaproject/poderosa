/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: InputPassphraseDialog.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Granados.SSH2;
using Poderosa.Usability;

namespace Poderosa.Forms {
    internal partial class InputPassphraseDialog : Form {
        private AgentPrivateKey _key;

        public InputPassphraseDialog(AgentPrivateKey key) {
            _key = key;
            InitializeComponent();
            InitText();
        }
        private void InitText() {
            StringResource sr = SSHUtilPlugin.Instance.Strings;

            this.Text = sr.GetString("Form.InputPassphraseDialog.Text");
            _fileNameLabel.Text = sr.GetString("Form.InputPassphraseDialog._fileNameLabel");
            _fileNameBox.Text = _key.FileName;
            _passphraseLabel.Text = sr.GetString("Form.InputPassphraseDialog._passphraseLabel");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            _passphraseBox.Focus();
        }

        private void OnOK(object sender, EventArgs args) {
            this.DialogResult = DialogResult.None;
            try {
                SSH2UserAuthKey key = SSH2UserAuthKey.FromSECSHStyleFile(_key.FileName, _passphraseBox.Text);
                Debug.Assert(key != null); //例外でなければ成功
                _key.SetStatus(PrivateKeyStatus.OK, key);
                this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
            }

        }
    }
}