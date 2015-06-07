/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: EditVariableDialog.Designer.cs,v 1.1 2011/08/04 15:28:58 kzmi Exp $
 */
namespace Poderosa.Pipe {
    partial class EditVariableDialog {
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
            this._buttonOK = new System.Windows.Forms.Button();
            this._buttonCancel = new System.Windows.Forms.Button();
            this._labelName = new System.Windows.Forms.Label();
            this._textBoxName = new System.Windows.Forms.TextBox();
            this._textBoxValue = new System.Windows.Forms.TextBox();
            this._labelValue = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _buttonOK
            // 
            this._buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonOK.Location = new System.Drawing.Point(228, 81);
            this._buttonOK.Name = "_buttonOK";
            this._buttonOK.Size = new System.Drawing.Size(75, 23);
            this._buttonOK.TabIndex = 4;
            this._buttonOK.UseVisualStyleBackColor = true;
            this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
            // 
            // _buttonCancel
            // 
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonCancel.Location = new System.Drawing.Point(309, 81);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(75, 23);
            this._buttonCancel.TabIndex = 5;
            this._buttonCancel.UseVisualStyleBackColor = true;
            this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
            // 
            // _labelName
            // 
            this._labelName.Location = new System.Drawing.Point(13, 6);
            this._labelName.Name = "_labelName";
            this._labelName.Size = new System.Drawing.Size(55, 30);
            this._labelName.TabIndex = 0;
            this._labelName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _textBoxName
            // 
            this._textBoxName.Location = new System.Drawing.Point(74, 12);
            this._textBoxName.Name = "_textBoxName";
            this._textBoxName.Size = new System.Drawing.Size(310, 19);
            this._textBoxName.TabIndex = 1;
            // 
            // _textBoxValue
            // 
            this._textBoxValue.Location = new System.Drawing.Point(74, 46);
            this._textBoxValue.Name = "_textBoxValue";
            this._textBoxValue.Size = new System.Drawing.Size(310, 19);
            this._textBoxValue.TabIndex = 3;
            // 
            // _labelValue
            // 
            this._labelValue.Location = new System.Drawing.Point(13, 40);
            this._labelValue.Name = "_labelValue";
            this._labelValue.Size = new System.Drawing.Size(55, 30);
            this._labelValue.TabIndex = 2;
            this._labelValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // EditVariableDialog
            // 
            this.AcceptButton = this._buttonOK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this._buttonCancel;
            this.ClientSize = new System.Drawing.Size(396, 116);
            this.Controls.Add(this._textBoxValue);
            this.Controls.Add(this._labelValue);
            this.Controls.Add(this._textBoxName);
            this.Controls.Add(this._labelName);
            this.Controls.Add(this._buttonCancel);
            this.Controls.Add(this._buttonOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditVariableDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button _buttonOK;
        private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _textBoxName;
        private System.Windows.Forms.TextBox _textBoxValue;
        private System.Windows.Forms.Label _labelValue;
    }
}