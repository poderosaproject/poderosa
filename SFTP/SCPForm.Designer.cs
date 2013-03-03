/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SCPForm.Designer.cs,v 1.3 2012/05/05 12:42:45 kzmi Exp $
 */
namespace Poderosa.SFTP {
    partial class SCPForm {

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
            this.textLog = new System.Windows.Forms.TextBox();
            this.labelRemotePath = new System.Windows.Forms.Label();
            this.textRemotePath = new System.Windows.Forms.TextBox();
            this.panelDrop = new System.Windows.Forms.Panel();
            this.labelDropHere = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.checkPreserveTime = new System.Windows.Forms.CheckBox();
            this.buttonDownload = new System.Windows.Forms.Button();
            this.checkRecursive = new System.Windows.Forms.CheckBox();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.panelDrop.SuspendLayout();
            this.SuspendLayout();
            // 
            // textLog
            // 
            this.textLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textLog.BackColor = System.Drawing.SystemColors.Window;
            this.textLog.Location = new System.Drawing.Point(5, 182);
            this.textLog.Multiline = true;
            this.textLog.Name = "textLog";
            this.textLog.ReadOnly = true;
            this.textLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textLog.Size = new System.Drawing.Size(366, 126);
            this.textLog.TabIndex = 9;
            this.textLog.WordWrap = false;
            // 
            // labelRemotePath
            // 
            this.labelRemotePath.AutoSize = true;
            this.labelRemotePath.Location = new System.Drawing.Point(6, 4);
            this.labelRemotePath.Name = "labelRemotePath";
            this.labelRemotePath.Size = new System.Drawing.Size(70, 12);
            this.labelRemotePath.TabIndex = 0;
            this.labelRemotePath.Text = "Remote path";
            // 
            // textRemotePath
            // 
            this.textRemotePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textRemotePath.Location = new System.Drawing.Point(5, 19);
            this.textRemotePath.Name = "textRemotePath";
            this.textRemotePath.Size = new System.Drawing.Size(366, 19);
            this.textRemotePath.TabIndex = 1;
            // 
            // panelDrop
            // 
            this.panelDrop.AllowDrop = true;
            this.panelDrop.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDrop.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelDrop.Controls.Add(this.labelDropHere);
            this.panelDrop.Location = new System.Drawing.Point(5, 66);
            this.panelDrop.Name = "panelDrop";
            this.panelDrop.Size = new System.Drawing.Size(236, 61);
            this.panelDrop.TabIndex = 4;
            this.panelDrop.DragDrop += new System.Windows.Forms.DragEventHandler(this.panelDrop_DragDrop);
            this.panelDrop.DragEnter += new System.Windows.Forms.DragEventHandler(this.panelDrop_DragEnter);
            // 
            // labelDropHere
            // 
            this.labelDropHere.AutoSize = true;
            this.labelDropHere.Location = new System.Drawing.Point(14, 15);
            this.labelDropHere.Name = "labelDropHere";
            this.labelDropHere.Size = new System.Drawing.Size(55, 12);
            this.labelDropHere.TabIndex = 0;
            this.labelDropHere.Text = "Drop here";
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(6, 160);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(291, 16);
            this.progressBar.TabIndex = 7;
            // 
            // labelProgress
            // 
            this.labelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgress.AutoEllipsis = true;
            this.labelProgress.Location = new System.Drawing.Point(4, 141);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(366, 16);
            this.labelProgress.TabIndex = 6;
            this.labelProgress.Text = "Progress";
            // 
            // checkPreserveTime
            // 
            this.checkPreserveTime.AutoSize = true;
            this.checkPreserveTime.Location = new System.Drawing.Point(5, 44);
            this.checkPreserveTime.Name = "checkPreserveTime";
            this.checkPreserveTime.Size = new System.Drawing.Size(161, 16);
            this.checkPreserveTime.TabIndex = 2;
            this.checkPreserveTime.Text = "Preserve modification time";
            this.checkPreserveTime.UseVisualStyleBackColor = true;
            // 
            // buttonDownload
            // 
            this.buttonDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDownload.Location = new System.Drawing.Point(260, 66);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(110, 64);
            this.buttonDownload.TabIndex = 5;
            this.buttonDownload.Text = "Download";
            this.buttonDownload.UseVisualStyleBackColor = true;
            this.buttonDownload.Click += new System.EventHandler(this.buttonDownload_Click);
            // 
            // checkRecursive
            // 
            this.checkRecursive.AutoSize = true;
            this.checkRecursive.Location = new System.Drawing.Point(189, 44);
            this.checkRecursive.Name = "checkRecursive";
            this.checkRecursive.Size = new System.Drawing.Size(75, 16);
            this.checkRecursive.TabIndex = 3;
            this.checkRecursive.Text = "Recursive";
            this.checkRecursive.UseVisualStyleBackColor = true;
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(303, 154);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(68, 24);
            this.buttonCancel.TabIndex = 8;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // SCPForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(376, 313);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonDownload);
            this.Controls.Add(this.checkRecursive);
            this.Controls.Add(this.checkPreserveTime);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.panelDrop);
            this.Controls.Add(this.textRemotePath);
            this.Controls.Add(this.labelRemotePath);
            this.Controls.Add(this.textLog);
            this.MinimumSize = new System.Drawing.Size(320, 250);
            this.Name = "SCPForm";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SCPForm";
            this.Load += new System.EventHandler(this.SCPForm_Load);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SCPForm_FormClosed);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SCPForm_FormClosing);
            this.panelDrop.ResumeLayout(false);
            this.panelDrop.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textLog;
        private System.Windows.Forms.Label labelRemotePath;
        private System.Windows.Forms.TextBox textRemotePath;
        private System.Windows.Forms.Panel panelDrop;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelDropHere;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.CheckBox checkPreserveTime;
        private System.Windows.Forms.Button buttonDownload;
        private System.Windows.Forms.CheckBox checkRecursive;
        private System.Windows.Forms.Button buttonCancel;
    }
}