/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SFTPForm.Designer.cs,v 1.2 2012/05/05 12:42:45 kzmi Exp $
 */
namespace Poderosa.SFTP {
    partial class SFTPForm {

        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows form designer

        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.treeViewImageList = new System.Windows.Forms.ImageList(this.components);
            this.textLog = new System.Windows.Forms.TextBox();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.labelProgress = new System.Windows.Forms.Label();
            this.buttonDownload = new System.Windows.Forms.Button();
            this.labelDropHere = new System.Windows.Forms.Label();
            this.treeViewRemote = new Poderosa.SFTP.MultiSelectTreeView();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // treeViewImageList
            // 
            this.treeViewImageList.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            this.treeViewImageList.ImageSize = new System.Drawing.Size(16, 16);
            this.treeViewImageList.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // textLog
            // 
            this.textLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textLog.BackColor = System.Drawing.SystemColors.Window;
            this.textLog.Location = new System.Drawing.Point(6, 224);
            this.textLog.Multiline = true;
            this.textLog.Name = "textLog";
            this.textLog.ReadOnly = true;
            this.textLog.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textLog.Size = new System.Drawing.Size(373, 126);
            this.textLog.TabIndex = 6;
            this.textLog.WordWrap = false;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(6, 202);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(299, 16);
            this.progressBar.TabIndex = 4;
            // 
            // labelProgress
            // 
            this.labelProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgress.AutoEllipsis = true;
            this.labelProgress.Location = new System.Drawing.Point(4, 183);
            this.labelProgress.Name = "labelProgress";
            this.labelProgress.Size = new System.Drawing.Size(375, 16);
            this.labelProgress.TabIndex = 3;
            this.labelProgress.Text = "Progress";
            // 
            // buttonDownload
            // 
            this.buttonDownload.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDownload.Location = new System.Drawing.Point(269, 24);
            this.buttonDownload.Name = "buttonDownload";
            this.buttonDownload.Size = new System.Drawing.Size(110, 64);
            this.buttonDownload.TabIndex = 2;
            this.buttonDownload.Text = "Download";
            this.buttonDownload.UseVisualStyleBackColor = true;
            this.buttonDownload.Click += new System.EventHandler(this.buttonDownload_Click);
            // 
            // labelDropHere
            // 
            this.labelDropHere.AutoSize = true;
            this.labelDropHere.Location = new System.Drawing.Point(6, 3);
            this.labelDropHere.Name = "labelDropHere";
            this.labelDropHere.Size = new System.Drawing.Size(40, 12);
            this.labelDropHere.TabIndex = 0;
            this.labelDropHere.Text = "Upload";
            // 
            // treeViewRemote
            // 
            this.treeViewRemote.AllowDrop = true;
            this.treeViewRemote.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewRemote.HideSelection = false;
            this.treeViewRemote.Location = new System.Drawing.Point(6, 24);
            this.treeViewRemote.Name = "treeViewRemote";
            this.treeViewRemote.PathSeparator = "/";
            this.treeViewRemote.ShowNodeToolTips = true;
            this.treeViewRemote.ShowRootLines = false;
            this.treeViewRemote.Size = new System.Drawing.Size(257, 144);
            this.treeViewRemote.TabIndex = 1;
            this.treeViewRemote.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewRemote_BeforeExpand);
            this.treeViewRemote.SingleNodeSelected += new System.Windows.Forms.TreeViewEventHandler(this.treeViewRemote_SingleNodeSelected);
            this.treeViewRemote.BeforeCollapse += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewRemote_BeforeCollapse);
            this.treeViewRemote.DragDrop += new System.Windows.Forms.DragEventHandler(this.treeViewRemote_DragDrop);
            this.treeViewRemote.DragOver += new System.Windows.Forms.DragEventHandler(this.treeViewRemote_DragOver);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(311, 196);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(68, 24);
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // SFTPForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(385, 356);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonDownload);
            this.Controls.Add(this.treeViewRemote);
            this.Controls.Add(this.textLog);
            this.Controls.Add(this.labelDropHere);
            this.Controls.Add(this.labelProgress);
            this.Controls.Add(this.progressBar);
            this.MinimumSize = new System.Drawing.Size(300, 320);
            this.Name = "SFTPForm";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "SFTPForm";
            this.Load += new System.EventHandler(this.SFTPForm_Load);
            this.Shown += new System.EventHandler(this.SFTPForm_Shown);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SFTPForm_FormClosed);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SFTPForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private MultiSelectTreeView treeViewRemote;
        private System.Windows.Forms.ImageList treeViewImageList;
        private System.Windows.Forms.TextBox textLog;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label labelProgress;
        private System.Windows.Forms.Button buttonDownload;
        private System.Windows.Forms.Label labelDropHere;
        private System.Windows.Forms.Button buttonCancel;
    }
}