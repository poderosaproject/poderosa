/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: CommandOptionPanel.cs,v 1.4 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;

using Poderosa.Util.Collections;
using Poderosa.UI;
using Poderosa.Usability;
using Poderosa.Preferences;
using Poderosa.Commands;

namespace Poderosa.Forms {
    internal class CommandOptionPanel : UserControl {
        private IKeyBinds _keybinds;

        private System.Windows.Forms.ListView _keyConfigList;
        private System.Windows.Forms.ColumnHeader _commandCategoryHeader;
        private System.Windows.Forms.ColumnHeader _commandNameHeader;
        private System.Windows.Forms.ColumnHeader _commandConfigHeader;
        private System.Windows.Forms.Button _resetKeyConfigButton;
        private System.Windows.Forms.Button _clearKeyConfigButton;
        private System.Windows.Forms.GroupBox _commandConfigGroup;
        private System.Windows.Forms.Label _commandNameLabel;
        private System.Windows.Forms.Label _commandName;
        private System.Windows.Forms.Label _currentConfigLabel;
        private System.Windows.Forms.Label _currentCommand;
        private System.Windows.Forms.Label _newAllocationLabel;
        private HotKey _hotKey;
        private System.Windows.Forms.Button _allocateKeyButton;

        public CommandOptionPanel() {
            InitializeComponent();
            FillText();
        }
        private void InitializeComponent() {
            this._keyConfigList = new System.Windows.Forms.ListView();
            this._commandCategoryHeader = new System.Windows.Forms.ColumnHeader();
            this._commandNameHeader = new System.Windows.Forms.ColumnHeader();
            this._commandConfigHeader = new System.Windows.Forms.ColumnHeader();
            this._resetKeyConfigButton = new System.Windows.Forms.Button();
            this._clearKeyConfigButton = new System.Windows.Forms.Button();
            this._commandConfigGroup = new System.Windows.Forms.GroupBox();
            this._commandNameLabel = new System.Windows.Forms.Label();
            this._commandName = new System.Windows.Forms.Label();
            this._currentConfigLabel = new System.Windows.Forms.Label();
            this._currentCommand = new System.Windows.Forms.Label();
            this._hotKey = new Poderosa.UI.HotKey();
            this._newAllocationLabel = new System.Windows.Forms.Label();
            this._allocateKeyButton = new System.Windows.Forms.Button();

            this._commandConfigGroup.SuspendLayout();

            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._keyConfigList,
                this._resetKeyConfigButton,
                this._clearKeyConfigButton,
                this._commandConfigGroup});
            // 
            // _keyConfigList
            // 
            this._keyConfigList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this._commandCategoryHeader,
                this._commandNameHeader,
                this._commandConfigHeader});
            this._keyConfigList.FullRowSelect = true;
            this._keyConfigList.GridLines = true;
            this._keyConfigList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._keyConfigList.MultiSelect = false;
            this._keyConfigList.Name = "_keyConfigList";
            this._keyConfigList.Size = new System.Drawing.Size(432, 172);
            this._keyConfigList.TabIndex = 0;
            this._keyConfigList.View = System.Windows.Forms.View.Details;
            this._keyConfigList.SelectedIndexChanged += new System.EventHandler(this.OnKeyMapItemActivated);
            // 
            // _commandCategoryHeader
            // 
            this._commandCategoryHeader.Width = 80;
            // 
            // _commandNameHeader
            // 
            this._commandNameHeader.Width = 188;
            // 
            // _commandConfigHeader
            // 
            this._commandConfigHeader.Width = 136;
            // 
            // _resetKeyConfigButton
            // 
            this._resetKeyConfigButton.Location = new System.Drawing.Point(216, 172);
            this._resetKeyConfigButton.Name = "_resetKeyConfigButton";
            this._resetKeyConfigButton.FlatStyle = FlatStyle.System;
            this._resetKeyConfigButton.Size = new System.Drawing.Size(104, 23);
            this._resetKeyConfigButton.TabIndex = 1;
            this._resetKeyConfigButton.Click += new System.EventHandler(this.OnResetKeyConfig);
            // 
            // _clearKeyConfigButton
            // 
            this._clearKeyConfigButton.Location = new System.Drawing.Point(336, 172);
            this._clearKeyConfigButton.Name = "_clearKeyConfigButton";
            this._clearKeyConfigButton.FlatStyle = FlatStyle.System;
            this._clearKeyConfigButton.Size = new System.Drawing.Size(88, 23);
            this._clearKeyConfigButton.TabIndex = 2;
            this._clearKeyConfigButton.Click += new System.EventHandler(this.OnClearKeyConfig);
            // 
            // _commandConfigGroup
            // 
            this._commandConfigGroup.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._commandNameLabel,
                this._commandName,
                this._currentConfigLabel,
                this._currentCommand,
                this._hotKey,
                this._newAllocationLabel,
                this._allocateKeyButton});
            this._commandConfigGroup.Location = new System.Drawing.Point(8, 196);
            this._commandConfigGroup.Name = "_commandConfigGroup";
            this._commandConfigGroup.FlatStyle = FlatStyle.System;
            this._commandConfigGroup.Size = new System.Drawing.Size(416, 96);
            this._commandConfigGroup.TabIndex = 3;
            this._commandConfigGroup.TabStop = false;
            // 
            // _commandNameLabel
            // 
            this._commandNameLabel.Location = new System.Drawing.Point(8, 16);
            this._commandNameLabel.Name = "_commandNameLabel";
            this._commandNameLabel.Size = new System.Drawing.Size(88, 23);
            this._commandNameLabel.TabIndex = 4;
            this._commandNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _commandName
            // 
            this._commandName.Location = new System.Drawing.Point(112, 16);
            this._commandName.Name = "_commandName";
            this._commandName.Size = new System.Drawing.Size(248, 23);
            this._commandName.TabIndex = 5;
            this._commandName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _currentConfigLabel
            // 
            this._currentConfigLabel.Location = new System.Drawing.Point(8, 40);
            this._currentConfigLabel.Name = "_currentConfigLabel";
            this._currentConfigLabel.Size = new System.Drawing.Size(88, 23);
            this._currentConfigLabel.TabIndex = 6;
            this._currentConfigLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _currentCommand
            // 
            this._currentCommand.Location = new System.Drawing.Point(112, 40);
            this._currentCommand.Name = "_currentCommand";
            this._currentCommand.Size = new System.Drawing.Size(248, 23);
            this._currentCommand.TabIndex = 7;
            this._currentCommand.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _hotKey
            // 
            this._hotKey.DebugTextBox = null;
            this._hotKey.ImeMode = System.Windows.Forms.ImeMode.Disable;
            this._hotKey.Key = System.Windows.Forms.Keys.None;
            this._hotKey.Location = new System.Drawing.Point(112, 64);
            this._hotKey.Name = "_hotKey";
            this._hotKey.Size = new System.Drawing.Size(168, 19);
            this._hotKey.TabIndex = 8;
            this._hotKey.Text = "";
            // 
            // _newAllocationLabel
            // 
            this._newAllocationLabel.Location = new System.Drawing.Point(8, 64);
            this._newAllocationLabel.Name = "_newAllocationLabel";
            this._newAllocationLabel.Size = new System.Drawing.Size(88, 23);
            this._newAllocationLabel.TabIndex = 9;
            this._newAllocationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _allocateKeyButton
            // 
            this._allocateKeyButton.Enabled = false;
            this._allocateKeyButton.Location = new System.Drawing.Point(288, 64);
            this._allocateKeyButton.Name = "_allocateKeyButton";
            this._allocateKeyButton.FlatStyle = FlatStyle.System;
            this._allocateKeyButton.Size = new System.Drawing.Size(75, 24);
            this._allocateKeyButton.TabIndex = 10;
            this._allocateKeyButton.Click += new System.EventHandler(this.OnAllocateKey);

            this.BackColor = SystemColors.Window;
            this._commandConfigGroup.ResumeLayout();
        }
        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._commandCategoryHeader.Text = sr.GetString("Form.OptionDialog._commandCategoryHeader");
            this._commandNameHeader.Text = sr.GetString("Form.OptionDialog._commandNameHeader");
            this._commandConfigHeader.Text = sr.GetString("Form.OptionDialog._commandConfigHeader");
            this._resetKeyConfigButton.Text = sr.GetString("Form.OptionDialog._resetKeyConfigButton");
            this._clearKeyConfigButton.Text = sr.GetString("Form.OptionDialog._clearKeyConfigButton");
            this._commandConfigGroup.Text = sr.GetString("Form.OptionDialog._commandConfigGroup");
            this._commandNameLabel.Text = sr.GetString("Form.OptionDialog._commandNameLabel");
            this._currentConfigLabel.Text = sr.GetString("Form.OptionDialog._currentConfigLabel");
            this._newAllocationLabel.Text = sr.GetString("Form.OptionDialog._newAllocationLabel");
            this._allocateKeyButton.Text = sr.GetString("Form.OptionDialog._allocateKeyButton");
        }
        public void InitUI(IKeyBinds keybinds) {
            _keybinds = keybinds;
            InitKeyConfigUI();
        }
        public bool Commit(IKeyBinds keybinds) {
            //逐次_keybindsを変更しているので同一インスタンスなら何もしなくていい
            if (keybinds != _keybinds)
                keybinds.Import(_keybinds);
            return true;
        }

        private void InitKeyConfigUI() {
            _keyConfigList.Items.Clear();

            //列挙してソート
            TypedHashtable<ICommandCategory, List<IGeneralCommand>> category_list = new TypedHashtable<ICommandCategory, List<IGeneralCommand>>();
            foreach (IGeneralCommand cmd in _keybinds.Commands) {
                ICommandCategory cat = cmd.CommandCategory;
                if (cat != null && cat.IsKeybindCustomizable) {
                    if (category_list.Contains(cat))
                        category_list[cat].Add(cmd);
                    else {
                        List<IGeneralCommand> l = new List<IGeneralCommand>();
                        l.Add(cmd);
                        category_list.Add(cat, l);
                    }
                }
            }

            ICollection result = PositionDesignationSorter.SortItems(category_list.Keys);
            foreach (ICommandCategory cat in result) {
                foreach (IGeneralCommand cmd in category_list[cat]) {
                    ListViewItem li = new ListViewItem(cat.Name);
                    li = _keyConfigList.Items.Add(li);
                    li.SubItems.Add(cmd.Description);
                    li.SubItems.Add(FormatKey(_keybinds.GetKey(cmd)));
                    li.Tag = cmd;
                }
            }

        }
        private void OnKeyMapItemActivated(object sender, EventArgs args) {
            if (_keyConfigList.SelectedItems.Count == 0)
                return;

            ListViewItem li = _keyConfigList.SelectedItems[0];
            IGeneralCommand cmd = li.Tag as IGeneralCommand;
            Debug.Assert(cmd != null);
            _hotKey.Key = _keybinds.GetKey(cmd);

            _commandName.Text = String.Format("{0} - {1}", cmd.CommandCategory.Name, cmd.Description);
            _currentCommand.Text = FormatKey(_keybinds.GetKey(cmd));
            _allocateKeyButton.Enabled = true;
        }
        private void OnAllocateKey(object sender, EventArgs args) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            if (_keyConfigList.SelectedItems.Count == 0)
                return;

            IGeneralCommand cmd = _keyConfigList.SelectedItems[0].Tag as IGeneralCommand;
            Debug.Assert(cmd != null);
            Keys key = _hotKey.Key;

            IGeneralCommand existing = _keybinds.FindCommand(key);
            if (existing != null && existing != cmd) { //別コマンドへの割当があったら
                if (GUtil.AskUserYesNo(this, String.Format(sr.GetString("Message.OptionDialog.AskOverwriteCommand"), existing.Description)) == DialogResult.No)
                    return;

                _keybinds.SetKey(existing, Keys.None); //既存のやつに割当をクリア
                FindItemFromTag(existing).SubItems[2].Text = "";
            }

            //設定を書き換え
            _keybinds.SetKey(cmd, key);
            _keyConfigList.SelectedItems[0].SubItems[2].Text = FormatKey(key);
        }

        private void OnResetKeyConfig(object sender, EventArgs args) {
            _keybinds.ResetToDefault();
            InitKeyConfigUI();
        }
        private void OnClearKeyConfig(object sender, EventArgs args) {
            _keybinds.ClearAll();
            InitKeyConfigUI();
        }

        private ListViewItem FindItemFromTag(object tag) {
            foreach (ListViewItem item in _keyConfigList.Items) {
                if (item.Tag == tag)
                    return item;
            }
            return null;
        }

        private static string FormatKey(Keys key) {
            return Poderosa.UI.GMenuItem.FormatShortcut(key);
        }
    }

    internal class CommandOptionPanelExtension : OptionPanelExtensionBase {
        private CommandOptionPanel _panel;

        public CommandOptionPanelExtension()
            : base("Form.OptionDialog._commandPanel", 3) {
        }

        public override string[] PreferenceFolderIDsToEdit {
            get {
                return new string[] { "org.poderosa.core.commands" };
            }
        }
        public override Control ContentPanel {
            get {
                return _panel;
            }
        }

        public override void InitiUI(IPreferenceFolder[] values) {
            if (_panel == null)
                _panel = new CommandOptionPanel();
            _panel.InitUI(OptionDialogPlugin.Instance.CommandManager.GetKeyBinds(values[0]));
        }

        public override bool Commit(IPreferenceFolder[] values) {
            Debug.Assert(_panel != null);
            return _panel.Commit(OptionDialogPlugin.Instance.CommandManager.GetKeyBinds(values[0]));

        }

        public override void Dispose() {
            if (_panel != null) {
                _panel.Dispose();
                _panel = null;
            }
        }
    }
}
