/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: YesNoAllDialog.Designer.cs,v 1.1 2011/11/30 22:53:08 kzmi Exp $
 */
namespace Poderosa.SFTP {
    partial class YesNoAllDialog {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows form designer

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this.labelText = new System.Windows.Forms.Label();
            this.buttonYes = new System.Windows.Forms.Button();
            this.buttonNo = new System.Windows.Forms.Button();
            this.buttonYesToAll = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelText
            // 
            this.labelText.AutoSize = true;
            this.labelText.Location = new System.Drawing.Point(62, 25);
            this.labelText.Name = "labelText";
            this.labelText.Size = new System.Drawing.Size(28, 12);
            this.labelText.TabIndex = 0;
            this.labelText.Text = "Text";
            // 
            // buttonYes
            // 
            this.buttonYes.Location = new System.Drawing.Point(0, 7);
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(90, 23);
            this.buttonYes.TabIndex = 0;
            this.buttonYes.Text = "&Yes";
            this.buttonYes.UseVisualStyleBackColor = true;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // buttonNo
            // 
            this.buttonNo.Location = new System.Drawing.Point(102, 7);
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(90, 23);
            this.buttonNo.TabIndex = 1;
            this.buttonNo.Text = "&No";
            this.buttonNo.UseVisualStyleBackColor = true;
            this.buttonNo.Click += new System.EventHandler(this.buttonNo_Click);
            // 
            // buttonYesToAll
            // 
            this.buttonYesToAll.Location = new System.Drawing.Point(204, 7);
            this.buttonYesToAll.Name = "buttonYesToAll";
            this.buttonYesToAll.Size = new System.Drawing.Size(90, 23);
            this.buttonYesToAll.TabIndex = 2;
            this.buttonYesToAll.Text = "Yes to &all";
            this.buttonYesToAll.UseVisualStyleBackColor = true;
            this.buttonYesToAll.Click += new System.EventHandler(this.buttonYesToAll_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Location = new System.Drawing.Point(306, 7);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(90, 23);
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "&Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.buttonYes);
            this.panelButtons.Controls.Add(this.buttonCancel);
            this.panelButtons.Controls.Add(this.buttonYesToAll);
            this.panelButtons.Controls.Add(this.buttonNo);
            this.panelButtons.Location = new System.Drawing.Point(9, 47);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(396, 30);
            this.panelButtons.TabIndex = 1;
            // 
            // YesNoAllDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(473, 115);
            this.ControlBox = false;
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.labelText);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "YesNoAllDialog";
            this.Padding = new System.Windows.Forms.Padding(8, 6, 8, 6);
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "YesNoAllDialog";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.YesNoAllDialog_Load);
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelText;
        private System.Windows.Forms.Button buttonYes;
        private System.Windows.Forms.Button buttonNo;
        private System.Windows.Forms.Button buttonYesToAll;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Panel panelButtons;
    }
}