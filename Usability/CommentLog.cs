/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: CommentLog.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

using Poderosa.Sessions;

namespace Poderosa.Usability {
    internal class CommentLog : System.Windows.Forms.Form {
        private ITerminalSession _session;
        private System.Windows.Forms.TextBox _textBox;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _insertButton;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public CommentLog(ITerminalSession session) {
            InitializeComponent();
            _session = session;

            StringResource sr = TerminalUIPlugin.Instance.Strings;
            this._okButton.Text = sr.GetString("Common.OK");
            this._cancelButton.Text = sr.GetString("Common.Cancel");
            this._insertButton.Text = sr.GetString("Form.CommentLog._insertButton");
            this.Text = sr.GetString("Form.CommentLog.Text");
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
            this._textBox = new System.Windows.Forms.TextBox();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._insertButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _textBox
            // 
            this._textBox.Location = new System.Drawing.Point(8, 8);
            this._textBox.Name = "_textBox";
            this._textBox.Size = new System.Drawing.Size(336, 19);
            this._textBox.TabIndex = 0;
            this._textBox.Text = "";
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(208, 32);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(64, 24);
            this._okButton.TabIndex = 1;
            this._okButton.Click += new EventHandler(OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(280, 32);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(64, 23);
            this._cancelButton.TabIndex = 2;
            // 
            // _insertButton
            // 
            this._insertButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._insertButton.Location = new System.Drawing.Point(8, 32);
            this._insertButton.Name = "_insertButton";
            this._insertButton.Size = new System.Drawing.Size(72, 23);
            this._insertButton.TabIndex = 3;
            this._insertButton.Click += new EventHandler(OnClickInsertButton);
            // 
            // LogNoteForm
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(346, 61);
            this.Controls.Add(this._insertButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._textBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "LogNoteForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }
        #endregion

        protected override bool ProcessDialogKey(Keys k) {
            if (k == Keys.Escape) {
                this.DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            else
                return base.ProcessDialogKey(k);
        }

        private void OnOK(object sender, EventArgs args) {
            _session.Terminal.ILogService.Comment(_textBox.Text);
        }

        private void OnClickInsertButton(object sender, EventArgs args) {
            //コンテキストメニューをつくる
            ContextMenuStrip menu = new ContextMenuStrip();
            StringResource sr = TerminalUIPlugin.Instance.Strings;
            menu.Items.Add(CreateMenuItem(sr.GetString("Menu.CommentLog.Time"), new EventHandler(OnInsertTime)));
            menu.Items.Add(CreateMenuItem(sr.GetString("Menu.CommentLog.DateTime"), new EventHandler(OnInsertDateTime)));
            menu.Show(this, new Point(_insertButton.Left, _insertButton.Bottom));
        }
        private ToolStripMenuItem CreateMenuItem(string text, EventHandler handler) {
            ToolStripMenuItem mex = new ToolStripMenuItem();
            mex.Text = text;
            mex.Click += handler;
            return mex;
        }
        private void OnInsertTime(object sender, EventArgs args) {
            InsertText(DateTime.Now.ToShortTimeString());
        }
        private void OnInsertDateTime(object sender, EventArgs args) {
            InsertText(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString());
        }
        private void InsertText(string t) {
            //_textBox.AppendText(t);
            for (int i = 0; i < t.Length; i++)
                Win32.SendMessage(_textBox.Handle, Win32.WM_CHAR, new IntPtr((int)t[i]), IntPtr.Zero);
            _textBox.Focus();
        }
    }
}
