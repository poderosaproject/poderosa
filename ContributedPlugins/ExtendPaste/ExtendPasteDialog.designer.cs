/*
 * Copyright 2015 yoshikixxxx.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 */
namespace Poderosa.ExtendPaste
{
    partial class ExtendPasteDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._messageLabel = new System.Windows.Forms.Label();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this._clipBoardBox = new System.Windows.Forms.RichTextBox();
            this._findPrevButton = new System.Windows.Forms.Button();
            this._findNextButton = new System.Windows.Forms.Button();
            this._confirmedCheck = new System.Windows.Forms.CheckBox();
            this._infoPanel = new System.Windows.Forms.Panel();
            this._lineCountLabel = new System.Windows.Forms.Label();
            this._newLineLabel = new System.Windows.Forms.Label();
            this._keywordMatchLabel = new System.Windows.Forms.Label();
            this._targetSessionLabel = new System.Windows.Forms.Label();
            this._subMessageLabel = new System.Windows.Forms.Label();
            this._infoPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _messageLabel
            // 
            this._messageLabel.AutoSize = true;
            this._messageLabel.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._messageLabel.Location = new System.Drawing.Point(57, 18);
            this._messageLabel.Name = "_messageLabel";
            this._messageLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._messageLabel.Size = new System.Drawing.Size(81, 12);
            this._messageLabel.TabIndex = 6;
            this._messageLabel.Text = "_messageLabel";
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._cancelButton.Location = new System.Drawing.Point(384, 57);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 1;
            this._cancelButton.Text = "_cancelButton";
            this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._okButton.Location = new System.Drawing.Point(303, 57);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 0;
            this._okButton.Text = "_okButton";
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            // 
            // _clipBoardBox
            // 
            this._clipBoardBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._clipBoardBox.DetectUrls = false;
            this._clipBoardBox.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._clipBoardBox.HideSelection = false;
            this._clipBoardBox.Location = new System.Drawing.Point(0, 128);
            this._clipBoardBox.Name = "_clipBoardBox";
            this._clipBoardBox.ReadOnly = true;
            this._clipBoardBox.Size = new System.Drawing.Size(464, 168);
            this._clipBoardBox.TabIndex = 5;
            this._clipBoardBox.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
            this._clipBoardBox.WordWrap = false;
            // 
            // _findPrevButton
            // 
            this._findPrevButton.Enabled = false;
            this._findPrevButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._findPrevButton.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._findPrevButton.Location = new System.Drawing.Point(127, 57);
            this._findPrevButton.Name = "_findPrevButton";
            this._findPrevButton.Size = new System.Drawing.Size(75, 23);
            this._findPrevButton.TabIndex = 3;
            this._findPrevButton.Text = "_findPrevButton";
            this._findPrevButton.Click += new System.EventHandler(this._findPrevButton_Click);
            // 
            // _findNextButton
            // 
            this._findNextButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._findNextButton.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._findNextButton.Location = new System.Drawing.Point(208, 57);
            this._findNextButton.Name = "_findNextButton";
            this._findNextButton.Size = new System.Drawing.Size(75, 23);
            this._findNextButton.TabIndex = 4;
            this._findNextButton.Text = "_findNextButton";
            this._findNextButton.Click += new System.EventHandler(this._findNextButton_Click);
            // 
            // _confirmedCheck
            // 
            this._confirmedCheck.AutoSize = true;
            this._confirmedCheck.Enabled = false;
            this._confirmedCheck.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._confirmedCheck.ForeColor = System.Drawing.SystemColors.ControlText;
            this._confirmedCheck.Location = new System.Drawing.Point(7, 61);
            this._confirmedCheck.Name = "_confirmedCheck";
            this._confirmedCheck.Size = new System.Drawing.Size(110, 16);
            this._confirmedCheck.TabIndex = 2;
            this._confirmedCheck.Text = "_confirmedCheck";
            this._confirmedCheck.UseVisualStyleBackColor = true;
            this._confirmedCheck.CheckedChanged += new System.EventHandler(this._confirmedCheck_CheckedChanged);
            // 
            // _infoPanel
            // 
            this._infoPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this._infoPanel.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this._infoPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._infoPanel.Controls.Add(this._lineCountLabel);
            this._infoPanel.Controls.Add(this._newLineLabel);
            this._infoPanel.Controls.Add(this._keywordMatchLabel);
            this._infoPanel.Controls.Add(this._targetSessionLabel);
            this._infoPanel.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._infoPanel.Location = new System.Drawing.Point(0, 83);
            this._infoPanel.Name = "_infoPanel";
            this._infoPanel.Size = new System.Drawing.Size(464, 45);
            this._infoPanel.TabIndex = 9;
            // 
            // _lineCountLabel
            // 
            this._lineCountLabel.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._lineCountLabel.Location = new System.Drawing.Point(355, 25);
            this._lineCountLabel.Name = "_lineCountLabel";
            this._lineCountLabel.Size = new System.Drawing.Size(99, 19);
            this._lineCountLabel.TabIndex = 3;
            this._lineCountLabel.Text = "_lineCountLabel";
            // 
            // _newLineLabel
            // 
            this._newLineLabel.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._newLineLabel.Location = new System.Drawing.Point(253, 25);
            this._newLineLabel.Name = "_newLineLabel";
            this._newLineLabel.Size = new System.Drawing.Size(99, 19);
            this._newLineLabel.TabIndex = 2;
            this._newLineLabel.Text = "_newLineLabel";
            // 
            // _keywordMatchLabel
            // 
            this._keywordMatchLabel.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._keywordMatchLabel.Location = new System.Drawing.Point(3, 25);
            this._keywordMatchLabel.Name = "_keywordMatchLabel";
            this._keywordMatchLabel.Size = new System.Drawing.Size(234, 19);
            this._keywordMatchLabel.TabIndex = 1;
            this._keywordMatchLabel.Text = "_keywordMatchLabel";
            // 
            // _targetSessionLabel
            // 
            this._targetSessionLabel.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._targetSessionLabel.Location = new System.Drawing.Point(3, 5);
            this._targetSessionLabel.Name = "_targetSessionLabel";
            this._targetSessionLabel.Size = new System.Drawing.Size(454, 21);
            this._targetSessionLabel.TabIndex = 0;
            this._targetSessionLabel.Text = "_targetSessionLabel";
            // 
            // _subMessageLabel
            // 
            this._subMessageLabel.AutoSize = true;
            this._subMessageLabel.Font = new System.Drawing.Font("ＭＳ Ｐゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._subMessageLabel.ForeColor = System.Drawing.Color.Red;
            this._subMessageLabel.Location = new System.Drawing.Point(57, 38);
            this._subMessageLabel.Name = "_subMessageLabel";
            this._subMessageLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._subMessageLabel.Size = new System.Drawing.Size(99, 12);
            this._subMessageLabel.TabIndex = 10;
            this._subMessageLabel.Text = "_subMessageLabel";
            // 
            // ExtendPasteDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(464, 296);
            this.Controls.Add(this._subMessageLabel);
            this.Controls.Add(this._infoPanel);
            this.Controls.Add(this._confirmedCheck);
            this.Controls.Add(this._findNextButton);
            this.Controls.Add(this._findPrevButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._clipBoardBox);
            this.Controls.Add(this._messageLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExtendPasteDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "ExtendPaste";
            this._infoPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _messageLabel;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.RichTextBox _clipBoardBox;
        private System.Windows.Forms.Button _findPrevButton;
        private System.Windows.Forms.Button _findNextButton;
        private System.Windows.Forms.CheckBox _confirmedCheck;
        private System.Windows.Forms.Panel _infoPanel;
        private System.Windows.Forms.Label _targetSessionLabel;
        private System.Windows.Forms.Label _keywordMatchLabel;
        private System.Windows.Forms.Label _lineCountLabel;
        private System.Windows.Forms.Label _newLineLabel;
        private System.Windows.Forms.Label _subMessageLabel;
    }
}