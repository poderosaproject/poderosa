/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: ChangeLog.cs,v 1.6 2012/03/18 01:06:30 kzmi Exp $
 */
using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Poderosa.Util;
using Poderosa.Terminal;
using Poderosa.Sessions;
using Poderosa.ConnectionParam;

namespace Poderosa.Usability {
    internal class ChangeLog : System.Windows.Forms.Form {
        private ITerminalSession _session;

        private ComboBox _logTypeBox;
        private System.Windows.Forms.Label _logTypeLabel;
        private ComboBox _fileNameBox;
        private System.Windows.Forms.Label _fileNameLabel;
        private Button _selectlogButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public ChangeLog(ITerminalSession session) {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();
            StringResource sr = TerminalUIPlugin.Instance.Strings;
            this._logTypeLabel.Text = sr.GetString("Form.ChangeLog._logTypeLabel");
            this._fileNameLabel.Text = sr.GetString("Form.ChangeLog._fileNameLabel");
            this._cancelButton.Text = sr.GetString("Common.Cancel");
            this._okButton.Text = sr.GetString("Common.OK");
            this.Text = sr.GetString("Form.ChangeLog.Text");

            this._logTypeBox.Items.AddRange(EnumListItem<LogType>.GetListItems());

            _session = session;
            ISimpleLogSettings ls = GetSimpleLogSettings();
            if (ls != null) {
                _logTypeBox.SelectedItem = ls.LogType;  // select EnumListItem<T> by T
                if (ls.LogType != LogType.None) {
                    _fileNameBox.Items.Add(ls.LogPath);
                    _fileNameBox.SelectedIndex = 0;
                }
            }
            else
                _logTypeBox.SelectedItem = LogType.None;    // select EnumListItem<T> by T

            AdjustUI();
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
            this._logTypeBox = new System.Windows.Forms.ComboBox();
            this._logTypeLabel = new System.Windows.Forms.Label();
            this._fileNameBox = new System.Windows.Forms.ComboBox();
            this._fileNameLabel = new System.Windows.Forms.Label();
            this._selectlogButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _logTypeBox
            // 
            this._logTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._logTypeBox.Location = new System.Drawing.Point(104, 8);
            this._logTypeBox.Name = "_logTypeBox";
            this._logTypeBox.Size = new System.Drawing.Size(160, 20);
            this._logTypeBox.TabIndex = 1;
            this._logTypeBox.SelectedIndexChanged += new System.EventHandler(this.OnLogTypeChanged);
            // 
            // _logTypeLabel
            // 
            this._logTypeLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._logTypeLabel.Location = new System.Drawing.Point(5, 8);
            this._logTypeLabel.Name = "_logTypeLabel";
            this._logTypeLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._logTypeLabel.Size = new System.Drawing.Size(80, 16);
            this._logTypeLabel.TabIndex = 0;
            this._logTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _fileNameBox
            // 
            this._fileNameBox.Location = new System.Drawing.Point(104, 32);
            this._fileNameBox.Name = "_fileNameBox";
            this._fileNameBox.Size = new System.Drawing.Size(160, 20);
            this._fileNameBox.TabIndex = 3;
            // 
            // _fileNameLabel
            // 
            this._fileNameLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._fileNameLabel.Location = new System.Drawing.Point(5, 32);
            this._fileNameLabel.Name = "_fileNameLabel";
            this._fileNameLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._fileNameLabel.Size = new System.Drawing.Size(88, 16);
            this._fileNameLabel.TabIndex = 2;
            this._fileNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _selectlogButton
            // 
            this._selectlogButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectlogButton.ImageIndex = 0;
            this._selectlogButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._selectlogButton.Location = new System.Drawing.Point(269, 32);
            this._selectlogButton.Name = "_selectlogButton";
            this._selectlogButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._selectlogButton.Size = new System.Drawing.Size(19, 19);
            this._selectlogButton.TabIndex = 4;
            this._selectlogButton.Text = "...";
            this._selectlogButton.Click += new System.EventHandler(this.OnSelectLogFile);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.ImageIndex = 0;
            this._cancelButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._cancelButton.Location = new System.Drawing.Point(216, 56);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._cancelButton.Size = new System.Drawing.Size(72, 25);
            this._cancelButton.TabIndex = 6;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.ImageIndex = 0;
            this._okButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._okButton.Location = new System.Drawing.Point(128, 56);
            this._okButton.Name = "_okButton";
            this._okButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._okButton.Size = new System.Drawing.Size(72, 25);
            this._okButton.TabIndex = 5;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // ChangeLog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(292, 85);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._logTypeBox);
            this.Controls.Add(this._logTypeLabel);
            this.Controls.Add(this._fileNameBox);
            this.Controls.Add(this._fileNameLabel);
            this.Controls.Add(this._selectlogButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChangeLog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }
        #endregion

        private void AdjustUI() {
            bool e = ((EnumListItem<LogType>)_logTypeBox.SelectedItem).Value != LogType.None;
            _fileNameBox.Enabled = e;
            _selectlogButton.Enabled = e;
        }
        private void OnLogTypeChanged(object sender, EventArgs args) {
            AdjustUI();
        }
        private void OnSelectLogFile(object sender, EventArgs args) {
            string fn = LogUtil.SelectLogFileByDialog(this);
            if (fn != null)
                _fileNameBox.Text = fn;
        }
        private void OnOK(object sender, EventArgs args) {
            this.DialogResult = DialogResult.None;
            ISimpleLogSettings ls = TerminalUIPlugin.Instance.TerminalEmulatorPlugin.CreateDefaultSimpleLogSettings();
            LogType t = ((EnumListItem<LogType>)_logTypeBox.SelectedItem).Value;
            string path = null;

            bool append = false;
            if (t != LogType.None) {
                path = _fileNameBox.Text;
                LogFileCheckResult r = LogUtil.CheckLogFileName(path, this);
                if (r == LogFileCheckResult.Cancel || r == LogFileCheckResult.Error)
                    return;
                append = (r == LogFileCheckResult.Append);
            }

            ls.LogType = t;
            ls.LogPath = path;
            ls.LogAppend = append;
            _session.Terminal.ILogService.ApplyLogSettings(ls, true);

            this.DialogResult = DialogResult.OK;
        }

        private ISimpleLogSettings GetSimpleLogSettings() {
            IMultiLogSettings ml = _session.TerminalSettings.LogSettings;
            foreach (ILogSettings ls in ml) {
                ISimpleLogSettings sl = ls as ISimpleLogSettings;
                if (sl != null)
                    return sl;
            }
            return null;
        }
    }
}
