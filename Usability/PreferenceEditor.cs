/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PreferenceEditor.cs,v 1.3 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Forms;
using Poderosa.Preferences;

namespace Poderosa.Usability {
    internal class PreferenceEditor : Form {

        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
                _boldFont.Dispose();
                _filterChangeTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._resetButton = new Button();
            this._filterLabel = new System.Windows.Forms.Label();
            this._filterBox = new System.Windows.Forms.TextBox();
            this._listView = new System.Windows.Forms.ListView();
            this._nameHeader = new System.Windows.Forms.ColumnHeader();
            this._typeHeader = new System.Windows.Forms.ColumnHeader();
            this._valueHeader = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(449, 369);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 0;
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new EventHandler(OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(530, 369);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 1;
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _filterLabel
            // 
            this._filterLabel.AutoSize = true;
            this._filterLabel.Location = new System.Drawing.Point(13, 13);
            this._filterLabel.Name = "_filterLabel";
            this._filterLabel.Size = new System.Drawing.Size(0, 12);
            this._filterLabel.TabIndex = 2;
            this._filterLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _filterBox
            // 
            this._filterBox.Location = new System.Drawing.Point(73, 10);
            this._filterBox.Name = "_filterBox";
            this._filterBox.Size = new System.Drawing.Size(532, 19);
            this._filterBox.TabIndex = 3;
            this._filterBox.TextChanged += new System.EventHandler(this.OnFilterTextChanged);
            // 
            // _resetButton
            // 
            this._resetButton.Location = new System.Drawing.Point(15, 369);
            this._resetButton.Name = "_resetButton";
            this._resetButton.Size = new System.Drawing.Size(140, 23);
            this._resetButton.TabIndex = 1;
            this._resetButton.UseVisualStyleBackColor = true;
            this._resetButton.Click += new System.EventHandler(this.OnResetAll);
            // 
            // _listView
            // 
            this._listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._nameHeader,
            this._typeHeader,
            this._valueHeader});
            this._listView.FullRowSelect = true;
            this._listView.GridLines = true;
            this._listView.Location = new System.Drawing.Point(15, 41);
            this._listView.MultiSelect = false;
            this._listView.Name = "_listView";
            this._listView.Size = new System.Drawing.Size(590, 322);
            this._listView.TabIndex = 4;
            this._listView.UseCompatibleStateImageBehavior = false;
            this._listView.View = System.Windows.Forms.View.Details;
            this._listView.DoubleClick += new System.EventHandler(this.OnListViewDoubleClick);
            // 
            // _nameHeader
            // 
            this._nameHeader.Width = 230;
            // 
            // _valueHeader
            // 
            this._valueHeader.Width = 229;
            // 
            // PreferenceEditor
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(617, 404);
            this.Controls.Add(this._listView);
            this.Controls.Add(this._filterBox);
            this.Controls.Add(this._filterLabel);
            this.Controls.Add(this._resetButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PreferenceEditor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PreferenceEditor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }



        #endregion

        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private Button _resetButton;
        private System.Windows.Forms.Label _filterLabel;
        private System.Windows.Forms.TextBox _filterBox;
        private System.Windows.Forms.ListView _listView;
        private System.Windows.Forms.ColumnHeader _nameHeader;
        private System.Windows.Forms.ColumnHeader _typeHeader;
        private System.Windows.Forms.ColumnHeader _valueHeader;
        private Font _boldFont;
        private Timer _filterChangeTimer;

        private class FolderTag {
            private IPreferenceFolder _original;
            private IPreferenceFolder _work;

            public FolderTag(IPreferenceFolder original) {
                _original = original;
                _work = original.Clone();
            }
            public IPreferenceFolder Original {
                get {
                    return _original;
                }
            }
            public IPreferenceFolder Work {
                get {
                    return _work;
                }
            }
        }

        private class ItemTag : IComparable<ItemTag> {
            private IPreferenceItem _item;

            public ItemTag(IPreferenceItem item) {
                _item = item;
            }
            public IPreferenceItem Item {
                get {
                    return _item;
                }
            }
            public string TypeStringID {
                get {
                    if (_item.AsBool() != null)
                        return "Caption.PreferenceEditor.Bool";
                    else if (_item.AsInt() != null)
                        return "Caption.PreferenceEditor.Int";
                    else
                        return "Caption.PreferenceEditor.String";
                }
            }
            public string ValueString {
                get {
                    if (_item.AsBool() != null)
                        return _item.AsBool().Value.ToString();
                    else if (_item.AsInt() != null)
                        return _item.AsInt().Value.ToString();
                    else
                        return _item.AsString().Value;
                }
            }
            public bool IsChanged {
                get {
                    if (_item.AsBool() != null)
                        return _item.AsBool().Value != _item.AsBool().InitialValue;
                    else if (_item.AsInt() != null)
                        return _item.AsInt().Value != _item.AsInt().InitialValue;
                    else
                        return _item.AsString().Value != _item.AsString().InitialValue;
                }
            }

            public int CompareTo(ItemTag other) {
                return _item.FullQualifiedId.CompareTo(other._item.FullQualifiedId);
            }
        }

        private IPreferences _preferences;
        private List<FolderTag> _folderTags;
        private List<ItemTag> _itemTags;

        public PreferenceEditor(IPreferences pref) {
            InitializeComponent();
            _boldFont = new Font(_listView.Font, _listView.Font.Style | FontStyle.Bold);
            _preferences = pref;

            _filterChangeTimer = new Timer();
            _filterChangeTimer.Interval = 500;
            _filterChangeTimer.Tick += new EventHandler(OnFilterChangeTimer);

            StringResource sr = UsabilityPlugin.Strings;
            _filterLabel.Text = sr.GetString("Form.PreferenceEditor._filterLabel");
            _nameHeader.Text = sr.GetString("Form.PreferenceEditor._nameHeader");
            _typeHeader.Text = sr.GetString("Form.PreferenceEditor._typeHeader");
            _valueHeader.Text = sr.GetString("Form.PreferenceEditor._valueHeader");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");
            _resetButton.Text = sr.GetString("Form.PreferenceEditor._resetButton");
            this.Text = sr.GetString("Form.PreferenceEditor.Text");

            //洗い出し
            _folderTags = new List<FolderTag>();
            _itemTags = new List<ItemTag>();
            foreach (IPreferenceFolder folder in _preferences.GetAllFolders()) {
                FolderTag ft = new FolderTag(folder);
                _folderTags.Add(ft);
                int count = ft.Work.ChildCount;
                for (int i = 0; i < count; i++) {
                    IPreferenceItem item = ft.Work.ChildAt(i).AsItem();
                    if (item != null) {
                        ItemTag it = new ItemTag(item);
                        _itemTags.Add(it);
                    }
                }
            }
            //ソート
            _itemTags.Sort();

            InitList();
        }

        private void InitList() {
            _listView.Items.Clear();
            StringResource sr = UsabilityPlugin.Strings;

            //ListItem
            _listView.BeginUpdate();
            foreach (ItemTag it in _itemTags) {
                if (IsVisibleItem(it.Item)) {
                    ListViewItem lvi = new ListViewItem(it.Item.FullQualifiedId);
                    lvi.SubItems.Add(sr.GetString(it.TypeStringID));
                    lvi.SubItems.Add(it.ValueString);
                    if (it.IsChanged)
                        lvi.Font = _boldFont;

                    lvi.Tag = it;
                    _listView.Items.Add(lvi);
                }
            }
            _listView.EndUpdate();
        }
        private void UpdateItemStatus(ListViewItem item, ItemTag tag) {
            item.SubItems[2].Text = tag.ValueString;
            item.Font = tag.IsChanged ? _boldFont : _listView.Font;
        }

        private bool IsVisibleItem(IPreferenceItem item) {
            string filter = _filterBox.Text;
            return filter.Length == 0 ? true : item.FullQualifiedId.Contains(filter); //テキストの絞込み
        }

        private void OnListViewDoubleClick(object sender, EventArgs args) {

            ListView.SelectedListViewItemCollection col = _listView.SelectedItems;
            if (col.Count == 0)
                return;
            ListViewItem item = col[0];
            ItemTag tag = (ItemTag)item.Tag;

            //型によって編集
            if (tag.Item.AsBool() != null) {
                IBoolPreferenceItem boolitem = tag.Item.AsBool();
                boolitem.Value = !boolitem.Value;
                UpdateItemStatus(item, tag);
            }
            else {
                PreferenceItemEditor dlg = new PreferenceItemEditor(tag.Item);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    UpdateItemStatus(item, tag);
            }
        }

        //Filterを更新するとリストが限定されてくるが、
        private void OnFilterTextChanged(object sender, EventArgs e) {
            _filterChangeTimer.Stop();
            _filterChangeTimer.Start();
        }
        private void OnFilterChangeTimer(object sender, EventArgs args) {
            _filterChangeTimer.Stop();
            InitList();
        }
        private void OnOK(object sender, EventArgs e) {
            try {
                foreach (FolderTag ft in _folderTags) {
                    ft.Original.Import(ft.Work);
                }
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                this.DialogResult = DialogResult.None;
            }

        }
        private void OnResetAll(object sender, EventArgs args) {
            foreach (ItemTag item in _itemTags) {
                item.Item.ResetValue();
            }
            InitList();
        }
    }

}