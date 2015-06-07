/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: XZModemDialog.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Forms;
using Poderosa.Terminal;

namespace Poderosa.XZModem {
    internal class XZModemDialog : System.Windows.Forms.Form {
        private bool _closed;
        private bool _executing;
        private AbstractTerminal _terminal;
        private ModemBase _modemTask;

        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private Label _directionLabel;
        private ComboBox _directionBox;
        private Label _protocolLabel;
        private ComboBox _protocolBox;
        private System.Windows.Forms.Label _fileNameLabel;
        private System.Windows.Forms.TextBox _fileNameBox;
        private System.Windows.Forms.Button _selectButton;
        private System.Windows.Forms.Label _progressText;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public XZModemDialog() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();

            StringResource sr = XZModemPlugin.Instance.Strings;
            this._okButton.Text = sr.GetString("Form.XZModemDialog._okButton");
            this._cancelButton.Text = sr.GetString("Common.Cancel");
            this._protocolLabel.Text = sr.GetString("Form.XZModemDialog._protocolLabel");
            this._protocolBox.Items.AddRange(new string[] { "XModem", "ZModem" });
            this._directionLabel.Text = sr.GetString("Form.XZModemDialog._directionLabel");
            this._directionBox.Items.AddRange(new string[] { sr.GetString("Caption.XZModemDialog.Reception"), sr.GetString("Caption.XZModemDialog.Transmission") });
            this._fileNameLabel.Text = sr.GetString("Form.XZModemDialog._fileNameLabel");
        }
        public bool Executing {
            get {
                return _executing;
            }
        }

        public void Initialize(AbstractTerminal terminal) {
            StringResource sr = XZModemPlugin.Instance.Strings;
            _terminal = terminal;
            this.Text = String.Format(sr.GetString("Caption.XZModemDialog.DialogTitle"), _terminal.TerminalHost.ISession.Caption);

            //ウィンドウのセンタリング
            Rectangle r = terminal.TerminalHost.OwnerWindow.AsForm().DesktopBounds;
            this.Location = new Point(r.Left + r.Width / 2 - this.Width / 2, r.Top + r.Height / 2 - this.Height / 2);

            //TODO 前回の起動時の設定を覚えておくとよい
            _protocolBox.SelectedIndex = 1;
            _directionBox.SelectedIndex = 0;
#if DEBUG
            //テスト時にはここに初期値を設定
            _fileNameBox.Text = "C:\\P4\\Work\\FF4K_R.bin";
#endif
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

        #region Windows フォーム デザイナで生成されたコード
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._fileNameLabel = new System.Windows.Forms.Label();
            this._fileNameBox = new System.Windows.Forms.TextBox();
            this._selectButton = new System.Windows.Forms.Button();
            this._progressText = new System.Windows.Forms.Label();
            this._directionLabel = new Label();
            this._directionBox = new ComboBox();
            this._protocolLabel = new Label();
            this._protocolBox = new ComboBox();
            this.SuspendLayout();
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(200, 92);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 0;
            this._okButton.Click += new EventHandler(OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(288, 92);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 1;
            this._cancelButton.Click += new EventHandler(OnCancel);
            // 
            // _directionLabel
            // 
            this._directionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._directionLabel.Location = new System.Drawing.Point(8, 8);
            this._directionLabel.Name = "_directionLabel";
            this._directionLabel.Size = new System.Drawing.Size(80, 16);
            this._directionLabel.TabIndex = 2;
            // 
            // _directionBox
            // 
            this._directionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this._directionBox.Location = new System.Drawing.Point(88, 8);
            this._directionBox.Name = "_directionBox";
            this._directionBox.Size = new System.Drawing.Size(120, 16);
            this._directionBox.TabIndex = 3;
            // 
            // _protocolLabel
            // 
            this._protocolLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._protocolLabel.Location = new System.Drawing.Point(216, 8);
            this._protocolLabel.Name = "_protocolLabel";
            this._protocolLabel.Size = new System.Drawing.Size(64, 16);
            this._protocolLabel.TabIndex = 4;
            // 
            // _protocolBox
            // 
            this._protocolBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this._protocolBox.Location = new System.Drawing.Point(280, 8);
            this._protocolBox.Name = "_protocolBox";
            this._protocolBox.Size = new System.Drawing.Size(80, 16);
            this._protocolBox.TabIndex = 5;
            // 
            // _fileNameLabel
            // 
            this._fileNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._fileNameLabel.Location = new System.Drawing.Point(8, 32);
            this._fileNameLabel.Name = "_fileNameLabel";
            this._fileNameLabel.Size = new System.Drawing.Size(80, 16);
            this._fileNameLabel.TabIndex = 6;
            // 
            // _fileNameBox
            // 
            this._fileNameBox.Location = new System.Drawing.Point(88, 32);
            this._fileNameBox.Name = "_fileNameBox";
            this._fileNameBox.Size = new System.Drawing.Size(252, 19);
            this._fileNameBox.TabIndex = 7;
            this._fileNameBox.Text = "";
            // 
            // _selectButton
            // 
            this._selectButton.Location = new System.Drawing.Point(340, 32);
            this._selectButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectButton.Name = "_selectButton";
            this._selectButton.Size = new System.Drawing.Size(19, 19);
            this._selectButton.TabIndex = 8;
            this._selectButton.Text = "...";
            this._selectButton.Click += new EventHandler(OnSelectFile);
            // 
            // _progressText
            // 
            this._progressText.Location = new System.Drawing.Point(8, 52);
            this._progressText.Name = "_progressText";
            this._progressText.Size = new System.Drawing.Size(296, 32);
            this._progressText.TabIndex = 5;
            this._progressText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // XZModemDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(376, 118);
            this.Controls.Add(this._progressText);
            this.Controls.Add(this._selectButton);
            this.Controls.Add(this._fileNameBox);
            this.Controls.Add(this._fileNameLabel);
            this.Controls.Add(this._directionLabel);
            this.Controls.Add(this._directionBox);
            this.Controls.Add(this._protocolLabel);
            this.Controls.Add(this._protocolBox);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "XZModemDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.ResumeLayout(false);

        }
        #endregion

        private void OnSelectFile(object sender, EventArgs args) {
            StringResource sr = XZModemPlugin.Instance.Strings;
            FileDialog dlg = null;
            if (_directionBox.SelectedIndex == 0) {
                SaveFileDialog sf = new SaveFileDialog();
                sf.Title = sr.GetString("Caption.XZModemDialog.ReceptionFileSelect");
                dlg = sf;
            }
            else {
                OpenFileDialog of = new OpenFileDialog();
                of.Title = sr.GetString("Caption.XZModemDialog.TransmissionFileSelect");
                of.CheckFileExists = true;
                of.Multiselect = false;
                dlg = of;
            }
            dlg.Filter = "All Files(*)|*";
            if (dlg.ShowDialog(this) == DialogResult.OK)
                _fileNameBox.Text = dlg.FileName;
        }

        private void OnOK(object sedner, EventArgs args) {
            Debug.Assert(!_executing);
            this.DialogResult = DialogResult.None;
            if (_directionBox.SelectedIndex == 0) { //index 0が受信
                if (!StartReceive())
                    return;
            }
            else {
                if (!StartSend())
                    return;
            }

            _terminal.StartModalTerminalTask(_modemTask);
            _modemTask.Start();

            StringResource sr = XZModemPlugin.Instance.Strings;
            _executing = true;
            _okButton.Enabled = false;
            _fileNameBox.Enabled = false;
            _selectButton.Enabled = false;
            _protocolBox.Enabled = false;
            _directionBox.Enabled = false;
            _progressText.Text = sr.GetString("Caption.XZModemDialog.Negotiating");
        }
        private bool StartReceive() {
            try {
                if (GetCurrentProtocol() == XZModemPlugin.Protocol.XModem)
                    _modemTask = new XModemReceiver(this, _fileNameBox.Text);
                else
                    _modemTask = new ZModemReceiver(this, _fileNameBox.Text);
                return true;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                return false;
            }
        }
        private bool StartSend() {
            try {
                if (GetCurrentProtocol() == XZModemPlugin.Protocol.XModem)
                    _modemTask = new XModemSender(this, _fileNameBox.Text);
                else
                    _modemTask = new ZModemSender(this, _fileNameBox.Text);
                return true;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                return false;
            }
        }

        private void Exit() {
            if (_modemTask != null) {
                _modemTask.Abort();
                _modemTask = null;
            }
            _executing = false;
            _okButton.Enabled = true;
            _fileNameBox.Enabled = true;
            _selectButton.Enabled = true;
            _protocolBox.Enabled = true;
            _directionBox.Enabled = true;
        }

        private void OnCancel(object sender, EventArgs args) {
            if (_executing)
                Exit();
            else
                Close();
        }
        protected override void OnClosed(EventArgs e) {
            base.OnClosed(e);
            if (_executing)
                Exit();
        }

        private delegate void CloseAndDisposeDelegate();
        private delegate void SetProgressValueDelegate(int value);
        public void AsyncClose() {
            if (_closed)
                return;

            if (this.InvokeRequired)
                Invoke(new CloseAndDisposeDelegate(CloseAndDispose));
            else
                CloseAndDispose();
        }
        public void AsyncSetProgressValue(int value) {
            if (this.InvokeRequired)
                Invoke(new SetProgressValueDelegate(SetProgressValue), value);
            else
                SetProgressValue(value);
        }
        private void CloseAndDispose() {
            _closed = true;
            Close();
            Dispose();
        }
        private void SetProgressValue(int value) {
            StringResource sr = XZModemPlugin.Instance.Strings;
            if (_modemTask.IsReceivingTask)
                _progressText.Text = String.Format(sr.GetString("Caption.XZModemDialog.ReceptionProgress"), value);
            else
                _progressText.Text = String.Format(sr.GetString("Caption.XZModemDialog.TransmissionProgress"), value);
        }

        private XZModemPlugin.Protocol GetCurrentProtocol() {
            return (XZModemPlugin.Protocol)_protocolBox.SelectedIndex;
        }


    }
}
