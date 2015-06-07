/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: ShellSchemeEditor.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Poderosa.Util.Collections;
using Poderosa.Terminal;

namespace Poderosa.Usability {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public partial class ShellSchemeEditor : Form {
        private IShellSchemeCollection _schemeCollection;
        private ItemTag _current;
        private List<ItemTag> _tags;
        private List<ItemTag> _removing; //編集中に削除したやつはこのコレクションに放り込んでおいて、あとでマップを引かせる
        private bool _blockUIEvent;

        private class ItemTag {
            private IShellScheme _scheme;
            private IShellScheme _original;

            public ItemTag(IShellScheme scheme, IShellScheme original) {
                _original = original;
                _scheme = scheme;
            }
            public IShellScheme ShellScheme {
                get {
                    return _scheme;
                }
            }
            public IShellScheme Original {
                get {
                    return _original;
                }
            }
        }

        public ShellSchemeEditor(IShellScheme current) {
            InitializeComponent();
            InitializeText();

            _deleteCharBox.Items.AddRange(new string[] { "BackSpace", "Delete" });

            _schemeCollection = TerminalUIPlugin.Instance.TerminalEmulatorPlugin.ShellSchemeCollection;
            _tags = new List<ItemTag>();
            _removing = new List<ItemTag>();

            _current = null;
            int index = 0;
            int current_index = 0;
            _blockUIEvent = true;
            foreach (IShellScheme ss in _schemeCollection.Items) {
                ItemTag tag = new ItemTag(ss.Clone(), ss);
                _tags.Add(tag);
                _schemeComboBox.Items.Add(ss.Name);
                if (current == ss)
                    current_index = index;
                index++;
            }
            _blockUIEvent = false;

            //これで初期化
            _schemeComboBox.SelectedIndex = current_index;
        }
        private void InitializeText() {
            StringResource sr = TerminalUIPlugin.Instance.Strings;

            this.Text = sr.GetString("Form.ShellSchemeEditor.Text");
            _schemeCollectionGroup.Text = sr.GetString("Form.ShellSchemeEditor._schemeCollectionGroup");
            _deleteSchemeButton.Text = sr.GetString("Form.ShellSchemeEditor._deleteSchemeButton");
            _newSchemeButton.Text = sr.GetString("Form.ShellSchemeEditor._newSchemeButton");
            _schemeLabel.Text = sr.GetString("Form.ShellSchemeEditor._schemeLabel");
            _deleteCharLabel.Text = sr.GetString("Form.ShellSchemeEditor._deleteCharLabel");
            _nameLabel.Text = sr.GetString("Form.ShellSchemeEditor._nameLabel");
            _promptLabel.Text = sr.GetString("Form.ShellSchemeEditor._promptLabel");
            _deleteCommandsButton.Text = sr.GetString("Form.ShellSchemeEditor._deleteButton");
            _alphabeticalSort.Text = sr.GetString("Form.ShellSchemeEditor._alphabeticalSort");
            _itemLabel.Text = sr.GetString("Form.ShellSchemeEditor._itemLabel");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");
        }
        private void OnSchemeChanged(object sender, EventArgs args) {
            if (_blockUIEvent)
                return;
            ItemTag t = _tags[_schemeComboBox.SelectedIndex];
            if (t != _current) {
                SelectScheme(t);
            }
        }
        private void OnNewScheme(object sender, EventArgs args) {
            int count = 0;
            string name;
            do {
                name = count == 0 ? "New Shell" : String.Format("New Shell ({0})", count);
                count++;
            } while (FindTag(name) != null);

            ItemTag tag = new ItemTag(_schemeCollection.CreateEmptyScheme(name), null);
            _tags.Add(tag);
            _schemeComboBox.Items.Add(name);
            Debug.Assert(_tags.Count == _schemeComboBox.Items.Count);
            _schemeComboBox.SelectedIndex = _tags.Count - 1; //これで選択
        }
        private void OnDeleteScheme(object sender, EventArgs args) {
            int index = _schemeComboBox.SelectedIndex;
            int next = index == 0 ? 0 : index - 1;
            ItemTag tag = _tags[index];
            _tags.Remove(tag);
            _removing.Add(tag);
            _schemeComboBox.Items.RemoveAt(index);
            Debug.Assert(_tags.Count == _schemeComboBox.Items.Count);

            _schemeComboBox.SelectedIndex = next;
        }
        private void OnSortChange(object sender, EventArgs args) {
            if (_blockUIEvent)
                return;
            InitCommandListBox();
        }
        private void OnDeleteCommands(object sender, EventArgs args) {
            ListBox.SelectedIndexCollection indices_ = _commandListBox.SelectedIndices;
            int[] indices = new int[indices_.Count];
            for (int i = 0; i < indices.Length; i++)
                indices[i] = indices_[i];

            IIntelliSenseItemCollection col = _current.ShellScheme.CommandHistory;
            for (int i = indices.Length - 1; i >= 0; i--) {
                col.RemoveAt(indices[i]);
                _commandListBox.Items.RemoveAt(indices[i]);
            }

            _deleteCommandsButton.Enabled = false;
        }
        private void OnValidateSchemeName(object sender, CancelEventArgs e) {
            if (_blockUIEvent)
                return;
            StringResource sr = TerminalUIPlugin.Instance.Strings;
            if (_nameBox.Text.Length == 0) {
                GUtil.Warning(this, sr.GetString("Message.ShellSchemeEditor.EmptyName"));
                e.Cancel = true;
            }

            ItemTag t = FindTag(_nameBox.Text);
            if (t != null && t != _current) {
                GUtil.Warning(this, sr.GetString("Message.ShellSchemeEditor.DuplicatedName"));
                e.Cancel = true;
            }
            else {
                _current.ShellScheme.Name = _nameBox.Text;
                _currentSchemeGroup.Text = String.Format(sr.GetString("Form.ShellSchemeEditor._currentSchemeGroup"), _nameBox.Text);
                _schemeComboBox.Items[_schemeComboBox.SelectedIndex] = _nameBox.Text;
            }
        }
        private void OnValidatePrompt(object sender, CancelEventArgs e) {
            if (_blockUIEvent)
                return;
            StringResource sr = TerminalUIPlugin.Instance.Strings;
            bool ok = false;
            try {
                Regex re = new Regex(_promptBox.Text);
                Match m = re.Match("");
                ok = !m.Success;
            }
            catch (Exception) {
            }

            if (ok) {
                _current.ShellScheme.PromptExpression = _promptBox.Text;
            }
            else {
                GUtil.Warning(this, sr.GetString("Message.ShellSchemeEditor.PromptError"));
                e.Cancel = true;
            }
        }
        private void OnDeleteCharBoxChanged(object sender, EventArgs args) {
            _current.ShellScheme.BackSpaceChar = _deleteCharBox.SelectedIndex == 0 ? (char)0x08 : (char)0x7F;
        }
        private void OnSelectedIndicesChanged(object sender, EventArgs args) {
            _deleteCommandsButton.Enabled = _commandListBox.SelectedIndices.Count > 0;
        }
        private void OnOK(object sender, EventArgs args) {
            try {
                TypedHashtable<IShellScheme, IShellScheme> table = new TypedHashtable<IShellScheme, IShellScheme>();
                List<IShellScheme> newscheme = new List<IShellScheme>();
                IShellScheme newdefault = null;
                foreach (ItemTag tag in _tags) {
                    if (tag.Original != null)
                        table.Add(tag.Original, tag.ShellScheme);
                    newscheme.Add(tag.ShellScheme);
                    if (tag.ShellScheme.IsGeneric)
                        newdefault = tag.ShellScheme;
                }

                foreach (ItemTag tag in _removing) {
                    table.Add(tag.Original, newdefault);
                }

                _schemeCollection.UpdateAll(newscheme.ToArray(), table);
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }

        //UIの調整
        private void SelectScheme(ItemTag tag) {
            _current = tag;
            IShellScheme ss = tag.ShellScheme;
            _blockUIEvent = true;
            StringResource sr = TerminalUIPlugin.Instance.Strings;
            _deleteSchemeButton.Enabled = !ss.IsGeneric;
            _currentSchemeGroup.Text = String.Format(sr.GetString("Form.ShellSchemeEditor._currentSchemeGroup"), ss.Name);
            _nameBox.Enabled = !ss.IsGeneric;
            _nameBox.Text = ss.Name;
            _promptBox.Text = ss.PromptExpression;
            _deleteCharBox.SelectedIndex = ss.BackSpaceChar == (char)0x7F ? 1 : 0;
            _alphabeticalSort.Checked = false;
            InitCommandListBox();

            _blockUIEvent = false;
            _deleteCommandsButton.Enabled = false;
        }
        private void InitCommandListBox() {
            _commandListBox.Items.Clear();
            _commandListBox.Sorted = false;
            foreach (IIntelliSenseItem item in _current.ShellScheme.CommandHistory.Items) {
                _commandListBox.Items.Add(item.Format(' '));
            }
            if (_alphabeticalSort.Checked)
                _commandListBox.Sorted = true;
        }

        private ItemTag FindTag(string name) {
            foreach (ItemTag tag in _tags)
                if (tag.ShellScheme.Name == name)
                    return tag;
            return null;
        }
    }

}