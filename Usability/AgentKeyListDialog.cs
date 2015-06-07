/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: AgentKeyListDialog.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Granados;
using Poderosa.Usability;
using Poderosa.Terminal;

namespace Poderosa.Forms {
    public partial class AgentKeyListDialog : Form {
        public AgentKeyListDialog() {
            InitializeComponent();
            InitText();
            InitContent();
        }

        private void InitText() {
            StringResource sr = UsabilityPlugin.Strings;
            this.Text = sr.GetString("Form.AgentKeyListDialog.Text");
            _filenameHeader.Text = sr.GetString("Form.AgentKeyListDialog._filenameHeader");
            _statusHeader.Text = sr.GetString("Form.AgentKeyListDialog._statusHeader");
            _commentHeader.Text = sr.GetString("Form.AgentKeyListDialog._commentHeader");

            _addButton.Text = sr.GetString("Form.AgentKeyListDialog._addButton");
            _removeButton.Text = sr.GetString("Form.AgentKeyListDialog._removeButton");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");
        }
        private void InitContent() {
            List<AgentPrivateKey> keys = SSHUtilPlugin.Instance.KeyAgent.GetCurrentKeys();
            foreach (AgentPrivateKey key in keys) {
                AddListItem(key);
            }
        }

        private void OnSelectedIndexChanged(object sender, System.EventArgs e) {
            _removeButton.Enabled = _list.SelectedIndices.Count > 0;
        }
        private void OnListDoubleClick(object sender, System.EventArgs e) {
            ListViewItem li = GetSelectedItem();
            AgentPrivateKey key = li.Tag as AgentPrivateKey;
            Debug.Assert(key != null);

            if (key.Status == PrivateKeyStatus.OK)
                return; //既に確認済み

            if (key.GuessValidKeyFileOrWarn(this)) {
                InputPassphraseDialog dlg = new InputPassphraseDialog(key);
                if (dlg.ShowDialog(this) == DialogResult.OK) {
                    li.SubItems[1].Text = ToStatusString(key);
                    li.SubItems[2].Text = key.Key.Comment;
                    Debug.Assert(key.Status == PrivateKeyStatus.OK);
                }
            }
        }

        private void OnAddButton(object sender, System.EventArgs e) {
            string fn = TerminalUtil.SelectPrivateKeyFileByDialog(this);
            if (fn != null) {
                AgentPrivateKey key = new AgentPrivateKey(fn);
                if (key.GuessValidKeyFileOrWarn(this)) {
                    InputPassphraseDialog dlg = new InputPassphraseDialog(key);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        AddListItem(key);
                }
            }
        }

        private void OnRemoveButton(object sender, System.EventArgs e) {
            _list.Items.Remove(GetSelectedItem());
            _removeButton.Enabled = _list.SelectedIndices.Count > 0;
        }

        private void OnOK(object sender, System.EventArgs e) {
            List<AgentPrivateKey> keys = new List<AgentPrivateKey>();
            foreach (ListViewItem li in _list.Items)
                keys.Add((AgentPrivateKey)li.Tag);

            SSHUtilPlugin.Instance.KeyAgent.SetKeyList(keys);

        }

        private void AddListItem(AgentPrivateKey key) {
            ListViewItem li = new ListViewItem(key.FileName);
            li.SubItems.Add(ToStatusString(key));
            li.SubItems.Add(key.Key == null ? "" : key.Key.Comment);
            li.Tag = key;
            _list.Items.Add(li);
        }

        private string ToStatusString(AgentPrivateKey key) {
            return key.Status == PrivateKeyStatus.OK ? "OK" : "";
        }
        private ListViewItem GetSelectedItem() {
            return _list.SelectedItems.Count == 0 ? null : _list.SelectedItems[0];
        }

    }
}