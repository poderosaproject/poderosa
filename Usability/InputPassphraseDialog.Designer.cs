/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: InputPassphraseDialog.Designer.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
namespace Poderosa.Forms {
    partial class InputPassphraseDialog {
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
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._fileNameLabel = new System.Windows.Forms.Label();
            this._fileNameBox = new System.Windows.Forms.TextBox();
            this._passphraseLabel = new System.Windows.Forms.Label();
            this._passphraseBox = new System.Windows.Forms.TextBox();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _fileNameLabel
            // 
            this._fileNameLabel.AutoSize = true;
            this._fileNameLabel.Location = new System.Drawing.Point(13, 13);
            this._fileNameLabel.Name = "_fileNameLabel";
            this._fileNameLabel.Size = new System.Drawing.Size(35, 12);
            this._fileNameLabel.TabIndex = 0;
            this._fileNameLabel.Text = "label1";
            // 
            // _fileNameBox
            // 
            this._fileNameBox.Location = new System.Drawing.Point(95, 13);
            this._fileNameBox.Multiline = true;
            this._fileNameBox.Name = "_fileNameBox";
            this._fileNameBox.ReadOnly = true;
            this._fileNameBox.Size = new System.Drawing.Size(207, 35);
            this._fileNameBox.TabIndex = 1;
            // 
            // _passphraseLabel
            // 
            this._passphraseLabel.AutoSize = true;
            this._passphraseLabel.Location = new System.Drawing.Point(13, 58);
            this._passphraseLabel.Name = "_passphraseLabel";
            this._passphraseLabel.Size = new System.Drawing.Size(35, 12);
            this._passphraseLabel.TabIndex = 2;
            this._passphraseLabel.Text = "label2";
            // 
            // _passphraseBox
            // 
            this._passphraseBox.Location = new System.Drawing.Point(95, 58);
            this._passphraseBox.Name = "_passphraseBox";
            this._passphraseBox.PasswordChar = '*';
            this._passphraseBox.Size = new System.Drawing.Size(207, 19);
            this._passphraseBox.TabIndex = 3;
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(227, 83);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 4;
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(146, 83);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 5;
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(OnOK);
            // 
            // InputPassphraseDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(314, 114);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._passphraseBox);
            this.Controls.Add(this._passphraseLabel);
            this.Controls.Add(this._fileNameBox);
            this.Controls.Add(this._fileNameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InputPassphraseDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _fileNameLabel;
        private System.Windows.Forms.TextBox _fileNameBox;
        private System.Windows.Forms.Label _passphraseLabel;
        private System.Windows.Forms.TextBox _passphraseBox;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton;
    }
}