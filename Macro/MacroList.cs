/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MacroList.cs,v 1.3 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Forms;

namespace Poderosa.MacroInternal {
    /// <summary>
    /// MacroList の概要の説明です。
    /// </summary>
    internal class MacroList : System.Windows.Forms.Form, IMacroEventListener {
        private System.Windows.Forms.Button _runButton;
        private System.Windows.Forms.Button _stopButton;
        private System.Windows.Forms.Button _propButton;
        private System.Windows.Forms.Button _addButton;
        private System.Windows.Forms.Button _deleteButton;
        private System.Windows.Forms.ListView _list;
        private System.Windows.Forms.ColumnHeader _titleHeader;
        private System.Windows.Forms.ColumnHeader _pathHeader;
        //private System.Windows.Forms.ColumnHeader _shortCutHeader;
        private System.Windows.Forms.ColumnHeader _infoHeader;
        private System.Windows.Forms.Button _environmentButton;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _downButton;
        private System.Windows.Forms.Button _upButton;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        //順序がかわったら初期実行マクロは除去
        //private bool _macroOrderUpdated;


        public MacroList() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();

            //廃止
            _environmentButton.Visible = false;

            StringResource sr = MacroPlugin.Instance.Strings;
            this._titleHeader.Text = sr.GetString("Form.MacroList._titleHeader");
            this._pathHeader.Text = sr.GetString("Form.MacroList._pathHeader");
            //this._shortCutHeader.Text = sr.GetString("Form.MacroList._shortCutHeader");
            this._infoHeader.Text = sr.GetString("Form.MacroList._infoHeader");
            this._runButton.Text = sr.GetString("Form.MacroList._runButton");
            this._stopButton.Text = sr.GetString("Form.MacroList._stopButton");
            this._propButton.Text = sr.GetString("Form.MacroList._propButton");
            this._downButton.Text = sr.GetString("Form.MacroList._downButton");
            this._upButton.Text = sr.GetString("Form.MacroList._upButton");
            this._addButton.Text = sr.GetString("Form.MacroList._addButton");
            this._deleteButton.Text = sr.GetString("Form.MacroList._deleteButton");
            this._environmentButton.Text = sr.GetString("Form.MacroList._environmentButton");
            this._okButton.Text = sr.GetString("Common.OK");
            this.Text = sr.GetString("Form.MacroList.Text");

            InitUI();

            //_macroOrderUpdated = false;
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

        /*
        //マクロ編集フォームから呼ばれる。keyに割り当て済みのコマンド名があるならそれを返し、なければnullを返す。
        public string FindCommandDescription(Keys key) {
            MacroModule mod = (MacroModule)_keyToModule[key];
            if (mod != null)
                return mod.Title;
            else {
                Commands.Entry e = GApp.Options.Commands.FindEntry(key);
                if (e != null && e.Category != Commands.Category.Macro)
                    return e.Description;
                else
                    return null;
            }
        }
         */

        #region Windows Form Designer generated code
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._list = new System.Windows.Forms.ListView();
            this._titleHeader = new System.Windows.Forms.ColumnHeader();
            this._pathHeader = new System.Windows.Forms.ColumnHeader();
            this._infoHeader = new System.Windows.Forms.ColumnHeader();
            this._runButton = new System.Windows.Forms.Button();
            this._stopButton = new System.Windows.Forms.Button();
            this._propButton = new System.Windows.Forms.Button();
            this._downButton = new System.Windows.Forms.Button();
            this._upButton = new System.Windows.Forms.Button();
            this._addButton = new System.Windows.Forms.Button();
            this._deleteButton = new System.Windows.Forms.Button();
            this._environmentButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _list
            // 
            this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._titleHeader,
            this._pathHeader,
            this._infoHeader});
            this._list.FullRowSelect = true;
            this._list.GridLines = true;
            this._list.Location = new System.Drawing.Point(0, 0);
            this._list.MultiSelect = false;
            this._list.Name = "_list";
            this._list.Size = new System.Drawing.Size(408, 280);
            this._list.TabIndex = 0;
            this._list.UseCompatibleStateImageBehavior = false;
            this._list.View = System.Windows.Forms.View.Details;
            this._list.SelectedIndexChanged += new System.EventHandler(this.OnSelectedIndexChanged);
            this._list.DoubleClick += new System.EventHandler(this.OnListDoubleClicked);
            // 
            // _titleHeader
            // 
            this._titleHeader.Width = 130;
            // 
            // _pathHeader
            // 
            this._pathHeader.Width = 190;
            // 
            // _runButton
            // 
            this._runButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._runButton.Location = new System.Drawing.Point(416, 8);
            this._runButton.Name = "_runButton";
            this._runButton.Size = new System.Drawing.Size(88, 23);
            this._runButton.TabIndex = 1;
            this._runButton.Click += new System.EventHandler(this.OnRunButtonClicked);
            // 
            // _stopButton
            // 
            this._stopButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._stopButton.Location = new System.Drawing.Point(416, 40);
            this._stopButton.Name = "_stopButton";
            this._stopButton.Size = new System.Drawing.Size(88, 23);
            this._stopButton.TabIndex = 2;
            this._stopButton.Click += new System.EventHandler(this.OnStopButtonClicked);
            // 
            // _propButton
            // 
            this._propButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._propButton.Location = new System.Drawing.Point(416, 72);
            this._propButton.Name = "_propButton";
            this._propButton.Size = new System.Drawing.Size(88, 23);
            this._propButton.TabIndex = 3;
            this._propButton.Click += new System.EventHandler(this.OnPropButtonClicked);
            // 
            // _downButton
            // 
            this._downButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._downButton.Location = new System.Drawing.Point(416, 104);
            this._downButton.Name = "_downButton";
            this._downButton.Size = new System.Drawing.Size(40, 23);
            this._downButton.TabIndex = 4;
            this._downButton.Click += new System.EventHandler(this.OnDownButtonClicked);
            // 
            // _upButton
            // 
            this._upButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._upButton.Location = new System.Drawing.Point(464, 104);
            this._upButton.Name = "_upButton";
            this._upButton.Size = new System.Drawing.Size(40, 23);
            this._upButton.TabIndex = 5;
            this._upButton.Click += new System.EventHandler(this.OnUpButtonClicked);
            // 
            // _addButton
            // 
            this._addButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._addButton.Location = new System.Drawing.Point(416, 152);
            this._addButton.Name = "_addButton";
            this._addButton.Size = new System.Drawing.Size(88, 23);
            this._addButton.TabIndex = 6;
            this._addButton.Click += new System.EventHandler(this.OnAddButtonClicked);
            // 
            // _deleteButton
            // 
            this._deleteButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deleteButton.Location = new System.Drawing.Point(416, 184);
            this._deleteButton.Name = "_deleteButton";
            this._deleteButton.Size = new System.Drawing.Size(88, 23);
            this._deleteButton.TabIndex = 7;
            this._deleteButton.Click += new System.EventHandler(this.OnDeleteButtonClicked);
            // 
            // _environmentButton
            // 
            this._environmentButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._environmentButton.Location = new System.Drawing.Point(416, 216);
            this._environmentButton.Name = "_environmentButton";
            this._environmentButton.Size = new System.Drawing.Size(88, 23);
            this._environmentButton.TabIndex = 8;
            this._environmentButton.Click += new System.EventHandler(this.OnEnvironmentButtonClicked);
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(416, 248);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(88, 23);
            this._okButton.TabIndex = 9;
            // 
            // MacroList
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._okButton;
            this.ClientSize = new System.Drawing.Size(506, 279);
            this.Controls.Add(this._upButton);
            this.Controls.Add(this._downButton);
            this.Controls.Add(this._environmentButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._deleteButton);
            this.Controls.Add(this._addButton);
            this.Controls.Add(this._propButton);
            this.Controls.Add(this._stopButton);
            this.Controls.Add(this._runButton);
            this.Controls.Add(this._list);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MacroList";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }
        #endregion

        private void InitUI() {
            foreach (MacroModule mod in MacroPlugin.Instance.MacroManager.Modules) {
                AddListItem(mod);
            }
            AdjustUI();
        }
        private void AddListItem(MacroModule mod) {
            ListViewItem li = new ListViewItem(mod.Title);
            li = _list.Items.Add(li);
            li.SubItems.Add(mod.Path);
            //li.SubItems.Add(UILibUtil.KeyString(shortcut & Keys.Modifiers, shortcut & Keys.KeyCode, '+'));
            li.SubItems.Add(GetInfoString(mod));
        }

        private void OnSelectedIndexChanged(object sender, EventArgs args) {
            AdjustUI();
        }
        private void OnListDoubleClicked(object sender, EventArgs args) {
            ShowProp(_list.SelectedItems[0].Index);
        }
        private void OnRunButtonClicked(object sender, EventArgs args) {
            MacroManager mm = MacroPlugin.Instance.MacroManager;
            mm.SetMacroEventListener(this);
            mm.Execute(this, mm.GetModule(_list.SelectedItems[0].Index));
        }
        private void OnStopButtonClicked(object sender, EventArgs args) {
            MacroManager mm = MacroPlugin.Instance.MacroManager;
            mm.StopMacro();
        }

        private void OnPropButtonClicked(object sender, EventArgs args) {
            ShowProp(_list.SelectedItems[0].Index);
        }
        private void OnAddButtonClicked(object sender, EventArgs args) {
            ModuleProperty dlg = new ModuleProperty(this, null);
            MacroManager mm = MacroPlugin.Instance.MacroManager;
            if (dlg.ShowDialog(this) == DialogResult.OK) {
                AddListItem(dlg.Module);
                mm.AddModule(dlg.Module);
                //if(dlg.ShortCut!=Keys.None) _keyToModule.Add(dlg.ShortCut, dlg.Module);
                AdjustUI();
            }
        }
        private void OnDeleteButtonClicked(object sender, EventArgs args) {
            MacroManager mm = MacroPlugin.Instance.MacroManager;
            MacroModule mod = mm.GetModule(_list.SelectedItems[0].Index);
            mm.RemoveModule(mod);
            _list.Items.Remove(_list.SelectedItems[0]);
            //_macroOrderUpdated = true;
            AdjustUI();
        }
        private void OnEnvironmentButtonClicked(object sender, EventArgs args) {
            //EnvVariableList dlg = new EnvVariableList();
        }
        private void OnDownButtonClicked(object sender, EventArgs args) {
            int n = _list.SelectedItems[0].Index;
            if (n == _list.Items.Count - 1)
                return;

            MacroManager mm = MacroPlugin.Instance.MacroManager;
            ListViewItem li = _list.Items[n];
            _list.Items.RemoveAt(n);
            _list.Items.Insert(n + 1, li);
            MacroModule mod = mm.GetModule(n);
            mm.RemoveAt(n);
            mm.InsertModule(n + 1, mod);
            //_macroOrderUpdated = true;
        }
        private void OnUpButtonClicked(object sender, EventArgs args) {
            int n = _list.SelectedItems[0].Index;
            if (n == 0)
                return;

            MacroManager mm = MacroPlugin.Instance.MacroManager;
            ListViewItem li = _list.Items[n];
            _list.Items.RemoveAt(n);
            _list.Items.Insert(n - 1, li);
            MacroModule mod = mm.GetModule(n);
            mm.RemoveAt(n);
            mm.InsertModule(n - 1, mod);
            //_macroOrderUpdated = true;
        }

        private void AdjustUI() {
            bool e = _list.SelectedItems.Count > 0;
            _runButton.Enabled = e;
            _stopButton.Enabled = false;
            _propButton.Enabled = e;
            _addButton.Enabled = true;
            _deleteButton.Enabled = e;
            _downButton.Enabled = e;
            _upButton.Enabled = e;
        }
        private void ShowProp(int index) {
            MacroManager mm = MacroPlugin.Instance.MacroManager;
            MacroModule mod = mm.GetModule(index);
            /*
            Keys key = Keys.None;
            IDictionaryEnumerator ie = _keyToModule.GetEnumerator();
            while (ie.MoveNext()) {
                if (ie.Value == mod) {
                    key = (Keys)(ie.Key);
                    break;
                }
            }
             */
            ModuleProperty dlg = new ModuleProperty(this, mod);
            if (dlg.ShowDialog(this) == DialogResult.OK) {
                mm.ReplaceModule(mm.GetModule(index), dlg.Module);
                ListViewItem li = _list.Items[index];
                li.Text = dlg.Module.Title;
                li.SubItems[1].Text = dlg.Module.Path;
                //li.SubItems[2].Text = UILibUtil.KeyString(dlg.ShortCut);
                li.SubItems[2].Text = GetInfoString(dlg.Module);

                AdjustUI();
            }
        }
        protected override void OnClosing(CancelEventArgs args) { //これを閉じるとき無条件で更新するが、いいのか？ OK/Cancel方式にすべき？
            base.OnClosed(args);

            MacroPlugin.Instance.MacroManager.SetMacroEventListener(null);
        }

        //これらはマクロスレッドで実行されることもあるので
        public void IndicateMacroStarted() {
            if (this.InvokeRequired)
                this.Invoke(new IndicateMacro(IndicateMacroStarted_));
            else
                IndicateMacroStarted_();
        }
        public void IndicateMacroFinished() {
            if (this.InvokeRequired)
                this.Invoke(new IndicateMacro(IndicateMacroFinished_));
            else
                IndicateMacroFinished_();
        }
        private delegate void IndicateMacro();
        private void IndicateMacroStarted_() {
            _runButton.Enabled = false;
            _stopButton.Enabled = true;
        }
        private void IndicateMacroFinished_() {
            _runButton.Enabled = true;
            _stopButton.Enabled = false;
        }



        private string GetInfoString(MacroModule mod) {
            return mod.DebugMode ? MacroPlugin.Instance.Strings.GetString("Caption.MacroList.Trace") : ""; //とりあえずはデバッグかどうかだけ
        }

    }
}
