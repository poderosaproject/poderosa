/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OptionDialog.cs,v 1.4 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;

using Poderosa.UI;
using Poderosa.Util.Collections;
using Poderosa.Util.Drawing;
using Poderosa.Usability;
using Poderosa.Preferences;

namespace Poderosa.Forms {
    internal class OptionDialog : System.Windows.Forms.Form {
        private PanelEntry[] _entries;
        private TypedHashtable<string, WorkPreference> _idToWorkPreference;

        private class PanelEntry {
            private int _index;
            private IOptionPanelExtension _extension;

            public PanelEntry(int index, IOptionPanelExtension extension) {
                _index = index;
                _extension = extension;
            }

            public int Index {
                get {
                    return _index;
                }
            }

            public IOptionPanelExtension Extension {
                get {
                    return _extension;
                }
            }

        }

        private class WorkPreference {
            private IPreferenceFolder _original;
            private IPreferenceFolder _work;

            public WorkPreference(IPreferenceFolder original) {
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


        private static OptionDialog _instance;
        private PanelEntry _currentEntry;

        private System.Windows.Forms.ImageList _imageList;
        private System.Windows.Forms.Panel _categoryItems;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.ComponentModel.IContainer components;

        public static OptionDialog Instance {
            get {
                return _instance;
            }
        }

        public OptionDialog() {
            _instance = this;
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();
            IOptionPanelExtension[] extps = (IOptionPanelExtension[])OptionDialogPlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint(OptionDialogPlugin.OPTION_PANEL_ID).GetExtensions();
            _entries = new PanelEntry[extps.Length];
            for (int i = 0; i < extps.Length; i++)
                _entries[i] = new PanelEntry(i, extps[i]);
            InitItems();
            FillText();
            Debug.Assert(_entries.Length == _categoryItems.Controls.Count); //拡張と同数のパネルがあること
        }

        /// <summary>
        /// 使用されているリソースに後処理を実行します。
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
                foreach (PanelEntry e in _entries)
                    e.Extension.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(OptionDialog));
            this._imageList = new System.Windows.Forms.ImageList(this.components);
            this._categoryItems = new System.Windows.Forms.Panel();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _imageList
            // 
            this._imageList.ImageSize = new System.Drawing.Size(32, 32);
            this._imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_imageList.ImageStream")));
            this._imageList.TransparentColor = System.Drawing.Color.Teal;
            // 
            // _categoryItems
            // 
            this._categoryItems.BackColor = System.Drawing.SystemColors.Window;
            this._categoryItems.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._categoryItems.Location = new System.Drawing.Point(4, 0);
            this._categoryItems.Name = "_categoryItems";
            this._categoryItems.Size = new System.Drawing.Size(72, 376);
            this._categoryItems.TabIndex = 3;
            this._categoryItems.MouseLeave += new System.EventHandler(this.CategoryItemsMouseLeave);
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(336, 384);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 1;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(432, 384);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 2;
            // 
            // OptionDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(528, 414);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._categoryItems);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "OptionDialog";
            this.ResumeLayout(false);

        }

        #endregion

        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._okButton.Text = sr.GetString("Common.OK");
            this._cancelButton.Text = sr.GetString("Common.Cancel");
            this.Text = sr.GetString("Form.OptionDialog.Text");
        }
        private void InitItems() {
            _idToWorkPreference = new TypedHashtable<string, WorkPreference>();
            IPreferences preferences = OptionDialogPlugin.Instance.RootPreferences;
            int y = 8;

            for (int i = 0; i < _entries.Length; i++) {
                IOptionPanelExtension e = _entries[i].Extension;
                foreach (string pref_id in e.PreferenceFolderIDsToEdit) {
                    WorkPreference wp = _idToWorkPreference[pref_id];
                    if (wp == null) { //add entry
                        IPreferenceFolder folder = preferences.FindPreferenceFolder(pref_id);
                        if (folder == null)
                            throw new Exception(pref_id + " not found");
                        _idToWorkPreference.Add(pref_id, new WorkPreference(folder));
                    }
                }
                PanelItem item = new PanelItem(this, i, e.Icon, e.Caption);
                item.Location = new Point(4, y);
                _categoryItems.Controls.Add(item);

                y += 52;
            }
            this.ClientSize = new Size(this.ClientSize.Width, y + 42); //項目が増えたら高さが増える
        }
        private PanelItem PanelItemAt(int index) {
            return (PanelItem)_categoryItems.Controls[index];
        }


        //TODO 数値のindexはナントカしたい
        public Image GetPanelIcon(int index) {
            return _imageList.Images[index];
        }

        public void SelectItem(int index) {
            if (index == _currentEntry.Index)
                return;

            //現在の内容でCommitできた場合のみ選択されたページを表示
            if (ClosePage())
                ShowPage(index);
        }
        public void SetHilightingItemIndex(int index) {
            foreach (PanelItem item in _categoryItems.Controls) {
                item.Hilight = item.Index == index;
            }
        }


        protected override void OnLoad(EventArgs e) {
            base.OnLoad(e);
            ShowPage(_currentEntry == null ? 0 : _currentEntry.Index);
        }

        private bool ClosePage() {
            Debug.Assert(_currentEntry != null);

            if (!_currentEntry.Extension.Commit(GetWorkPreferencesFor(_currentEntry)))
                return false;
            this.Controls.Remove(_currentEntry.Extension.ContentPanel);
            PanelItemAt(_currentEntry.Index).Selected = false;
            _categoryItems.Invalidate(true);
            return true;
        }

        private void ShowPage(int index) {
            _currentEntry = _entries[index];

            _currentEntry.Extension.InitiUI(GetWorkPreferencesFor(_currentEntry));
            Control panel = _currentEntry.Extension.ContentPanel;
            if (panel is Panel)
                ((Panel)panel).BorderStyle = BorderStyle.FixedSingle;
            else if (panel is UserControl)
                ((UserControl)panel).BorderStyle = BorderStyle.FixedSingle;
            panel.Location = new Point(_categoryItems.Right + 4, _categoryItems.Top);
            panel.Size = new Size(this.Width - _categoryItems.Width - 16, _categoryItems.Height);

            this.Controls.Add(panel);
            PanelItemAt(index).Selected = true;
            _categoryItems.Invalidate(true);
        }

        private void OnOK(object sender, EventArgs args) {
            bool ok = ClosePage();
            if (ok) {
                DialogResult = DialogResult.OK;
                //全体のコミット：コミット時の特殊アクションも必要かも
                foreach (WorkPreference wp in _idToWorkPreference.Values) {
                    wp.Original.Import(wp.Work);
                }
                //ここで保存も必要か？

            }
            else {
                DialogResult = DialogResult.None;
            }
        }

        private void CategoryItemsMouseLeave(object sender, EventArgs args) {
            SetHilightingItemIndex(-1);
            _categoryItems.Invalidate(true); //マウスの動かし方によって、MouseLeaveは発生しないこともある
        }

        private IPreferenceFolder[] GetWorkPreferencesFor(PanelEntry entry) {
            string[] ids = entry.Extension.PreferenceFolderIDsToEdit;
            IPreferenceFolder[] t = new IPreferenceFolder[ids.Length];
            for (int i = 0; i < t.Length; i++)
                t[i] = _idToWorkPreference[ids[i]].Work;
            return t;
        }

    }

    internal class PanelItem : UserControl {
        private int _index;
        private Image _image;
        private OptionDialog _parent;
        private string _caption;

        private bool _selected;
        private bool _hilight;

        private static Brush _textBrush = new SolidBrush(SystemColors.WindowText);
        private static Size _defaultSize = new Size(64, 48);
        private static DrawUtil.RoundRectColors _selectedColors;
        private static DrawUtil.RoundRectColors _hilightColors;

        public PanelItem(OptionDialog parent, int index, Image image, string caption) {
            _parent = parent;
            _index = index;
            _image = image;
            _caption = caption;
            this.Size = _defaultSize;
            this.TabStop = true;
            AdjustBackColor();
        }
        public int Index {
            get {
                return _index;
            }
        }
        public bool Selected {
            get {
                return _selected;
            }
            set {
                _selected = value;
                AdjustBackColor();
            }
        }

        public bool Hilight {
            get {
                return _hilight;
            }
            set {
                _hilight = value;
                AdjustBackColor();
            }
        }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            _parent.SetHilightingItemIndex(_selected ? -1 : _index);
        }

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            _parent.SetHilightingItemIndex(_selected ? -1 : _index);
        }
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) {
                _parent.SelectItem(_index);
            }
        }
        protected override void OnClick(EventArgs e) {
            base.OnClick(e);
            _parent.SelectItem(_index);
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            const int image_size = 32; //square image
            if (_selectedColors == null)
                CreateColor();

            Graphics g = e.Graphics;

            if (_selected)
                DrawUtil.DrawRoundRect(g, 0, 0, this.Width - 1, this.Height - 1, _selectedColors);
            else if (_hilight)
                DrawUtil.DrawRoundRect(g, 0, 0, this.Width - 1, this.Height - 1, _hilightColors);
            g.DrawImage(_image, (this.Width - image_size) / 2, 0);
            SizeF sz = g.MeasureString(_caption, this.Font);
            g.DrawString(_caption, this.Font, _textBrush, (int)(this.Width - sz.Width) / 2, image_size);
        }

        private void AdjustBackColor() {
            if (_selected)
                this.BackColor = Color.Orange;
            else if (_hilight)
                this.BackColor = DrawUtil.LightColor(Color.Orange);
            else
                this.BackColor = SystemColors.Window;
        }


        private static void CreateColor() {
            _selectedColors = new DrawUtil.RoundRectColors();
            _selectedColors.border_color = DrawUtil.ToCOLORREF(Color.DarkRed);
            _selectedColors.inner_color = DrawUtil.ToCOLORREF(Color.Orange);
            _selectedColors.outer_color = DrawUtil.ToCOLORREF(SystemColors.Window);
            _selectedColors.lightlight_color = DrawUtil.MergeColor(_selectedColors.border_color, _selectedColors.outer_color);
            _selectedColors.light_color = DrawUtil.MergeColor(_selectedColors.lightlight_color, _selectedColors.border_color);

            _hilightColors = new DrawUtil.RoundRectColors();
            _hilightColors.border_color = DrawUtil.ToCOLORREF(Color.Pink);
            _hilightColors.inner_color = DrawUtil.ToCOLORREF(DrawUtil.LightColor(Color.Orange));
            _hilightColors.outer_color = DrawUtil.ToCOLORREF(SystemColors.Window);
            _hilightColors.lightlight_color = DrawUtil.MergeColor(_hilightColors.border_color, _hilightColors.outer_color);
            _hilightColors.light_color = DrawUtil.MergeColor(_hilightColors.lightlight_color, _hilightColors.border_color);
        }
    }
}
