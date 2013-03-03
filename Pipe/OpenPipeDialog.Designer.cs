/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OpenPipeDialog.Designer.cs,v 1.2 2011/12/21 16:34:47 kzmi Exp $
 */
namespace Poderosa.Pipe {
    partial class OpenPipeDialog {
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
            this._radioButtonProcess = new System.Windows.Forms.RadioButton();
            this._radioButtonPipe = new System.Windows.Forms.RadioButton();
            this._groupBoxPipe = new System.Windows.Forms.GroupBox();
            this._checkBoxBidirectinal = new System.Windows.Forms.CheckBox();
            this._labelOutputPath = new System.Windows.Forms.Label();
            this._textBoxOutputPath = new System.Windows.Forms.TextBox();
            this._labelInputPath = new System.Windows.Forms.Label();
            this._textBoxInputPath = new System.Windows.Forms.TextBox();
            this._buttonOK = new System.Windows.Forms.Button();
            this._buttonCancel = new System.Windows.Forms.Button();
            this._labelAutoExecMacroPath = new System.Windows.Forms.Label();
            this._textBoxAutoExecMacroPath = new System.Windows.Forms.TextBox();
            this._buttonSelectAutoExecMacro = new System.Windows.Forms.Button();
            this._groupTerminal = new System.Windows.Forms.GroupBox();
            this._labelLogType = new System.Windows.Forms.Label();
            this._comboBoxLogType = new System.Windows.Forms.ComboBox();
            this._textBoxLogFile = new System.Windows.Forms.TextBox();
            this._labelLogFile = new System.Windows.Forms.Label();
            this._buttonBrowseLogFile = new System.Windows.Forms.Button();
            this._labelEncoding = new System.Windows.Forms.Label();
            this._comboBoxEncoding = new System.Windows.Forms.ComboBox();
            this._labelLocalEcho = new System.Windows.Forms.Label();
            this._comboBoxLocalEcho = new System.Windows.Forms.ComboBox();
            this._labelNewLine = new System.Windows.Forms.Label();
            this._comboBoxNewLine = new System.Windows.Forms.ComboBox();
            this._labelTerminalType = new System.Windows.Forms.Label();
            this._comboBoxTerminalType = new System.Windows.Forms.ComboBox();
            this._groupBoxProcess = new System.Windows.Forms.GroupBox();
            this._labelCommandLineOptions = new System.Windows.Forms.Label();
            this._textBoxCommandLineOptions = new System.Windows.Forms.TextBox();
            this._buttonEnvironmentVariables = new System.Windows.Forms.Button();
            this._labelExePath = new System.Windows.Forms.Label();
            this._textBoxExePath = new System.Windows.Forms.TextBox();
            this._buttonBrowseExePath = new System.Windows.Forms.Button();
            this.panelModeSwitch = new System.Windows.Forms.Panel();
            this._groupBoxPipe.SuspendLayout();
            this._groupTerminal.SuspendLayout();
            this._groupBoxProcess.SuspendLayout();
            this.panelModeSwitch.SuspendLayout();
            this.SuspendLayout();
            // 
            // _radioButtonProcess
            // 
            this._radioButtonProcess.Appearance = System.Windows.Forms.Appearance.Button;
            this._radioButtonProcess.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radioButtonProcess.Location = new System.Drawing.Point(0, 0);
            this._radioButtonProcess.Name = "_radioButtonProcess";
            this._radioButtonProcess.Size = new System.Drawing.Size(104, 24);
            this._radioButtonProcess.TabIndex = 0;
            this._radioButtonProcess.TabStop = true;
            this._radioButtonProcess.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._radioButtonProcess.UseVisualStyleBackColor = true;
            this._radioButtonProcess.CheckedChanged += new System.EventHandler(this._radioButtonProcess_CheckedChanged);
            // 
            // _radioButtonPipe
            // 
            this._radioButtonPipe.Appearance = System.Windows.Forms.Appearance.Button;
            this._radioButtonPipe.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radioButtonPipe.Location = new System.Drawing.Point(110, 0);
            this._radioButtonPipe.Name = "_radioButtonPipe";
            this._radioButtonPipe.Size = new System.Drawing.Size(104, 24);
            this._radioButtonPipe.TabIndex = 1;
            this._radioButtonPipe.TabStop = true;
            this._radioButtonPipe.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._radioButtonPipe.UseVisualStyleBackColor = true;
            this._radioButtonPipe.CheckedChanged += new System.EventHandler(this._radioButtonPipe_CheckedChanged);
            // 
            // _groupBoxPipe
            // 
            this._groupBoxPipe.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._groupBoxPipe.Controls.Add(this._checkBoxBidirectinal);
            this._groupBoxPipe.Controls.Add(this._labelOutputPath);
            this._groupBoxPipe.Controls.Add(this._textBoxOutputPath);
            this._groupBoxPipe.Controls.Add(this._labelInputPath);
            this._groupBoxPipe.Controls.Add(this._textBoxInputPath);
            this._groupBoxPipe.Location = new System.Drawing.Point(12, 45);
            this._groupBoxPipe.Name = "_groupBoxPipe";
            this._groupBoxPipe.Size = new System.Drawing.Size(337, 105);
            this._groupBoxPipe.TabIndex = 1;
            this._groupBoxPipe.TabStop = false;
            // 
            // _checkBoxBidirectinal
            // 
            this._checkBoxBidirectinal.AutoSize = true;
            this._checkBoxBidirectinal.Location = new System.Drawing.Point(6, 55);
            this._checkBoxBidirectinal.Name = "_checkBoxBidirectinal";
            this._checkBoxBidirectinal.Size = new System.Drawing.Size(15, 14);
            this._checkBoxBidirectinal.TabIndex = 2;
            this._checkBoxBidirectinal.UseVisualStyleBackColor = true;
            this._checkBoxBidirectinal.CheckedChanged += new System.EventHandler(this._checkBoxBidirectinal_CheckedChanged);
            // 
            // _labelOutputPath
            // 
            this._labelOutputPath.Location = new System.Drawing.Point(6, 74);
            this._labelOutputPath.Name = "_labelOutputPath";
            this._labelOutputPath.Size = new System.Drawing.Size(100, 23);
            this._labelOutputPath.TabIndex = 3;
            this._labelOutputPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _textBoxOutputPath
            // 
            this._textBoxOutputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxOutputPath.Location = new System.Drawing.Point(112, 76);
            this._textBoxOutputPath.Name = "_textBoxOutputPath";
            this._textBoxOutputPath.Size = new System.Drawing.Size(219, 19);
            this._textBoxOutputPath.TabIndex = 4;
            // 
            // _labelInputPath
            // 
            this._labelInputPath.Location = new System.Drawing.Point(6, 16);
            this._labelInputPath.Name = "_labelInputPath";
            this._labelInputPath.Size = new System.Drawing.Size(100, 23);
            this._labelInputPath.TabIndex = 0;
            this._labelInputPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _textBoxInputPath
            // 
            this._textBoxInputPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxInputPath.Location = new System.Drawing.Point(112, 18);
            this._textBoxInputPath.Name = "_textBoxInputPath";
            this._textBoxInputPath.Size = new System.Drawing.Size(219, 19);
            this._textBoxInputPath.TabIndex = 1;
            // 
            // _buttonOK
            // 
            this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonOK.Location = new System.Drawing.Point(201, 510);
            this._buttonOK.Name = "_buttonOK";
            this._buttonOK.Size = new System.Drawing.Size(71, 23);
            this._buttonOK.TabIndex = 7;
            this._buttonOK.UseVisualStyleBackColor = true;
            this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
            // 
            // _buttonCancel
            // 
            this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonCancel.Location = new System.Drawing.Point(278, 510);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(71, 23);
            this._buttonCancel.TabIndex = 8;
            this._buttonCancel.UseVisualStyleBackColor = true;
            this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
            // 
            // _labelAutoExecMacroPath
            // 
            this._labelAutoExecMacroPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._labelAutoExecMacroPath.Location = new System.Drawing.Point(11, 476);
            this._labelAutoExecMacroPath.Name = "_labelAutoExecMacroPath";
            this._labelAutoExecMacroPath.Size = new System.Drawing.Size(128, 23);
            this._labelAutoExecMacroPath.TabIndex = 4;
            this._labelAutoExecMacroPath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _textBoxAutoExecMacroPath
            // 
            this._textBoxAutoExecMacroPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxAutoExecMacroPath.Location = new System.Drawing.Point(145, 476);
            this._textBoxAutoExecMacroPath.Name = "_textBoxAutoExecMacroPath";
            this._textBoxAutoExecMacroPath.Size = new System.Drawing.Size(173, 19);
            this._textBoxAutoExecMacroPath.TabIndex = 5;
            // 
            // _buttonSelectAutoExecMacro
            // 
            this._buttonSelectAutoExecMacro.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonSelectAutoExecMacro.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonSelectAutoExecMacro.ImageIndex = 0;
            this._buttonSelectAutoExecMacro.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._buttonSelectAutoExecMacro.Location = new System.Drawing.Point(324, 476);
            this._buttonSelectAutoExecMacro.Name = "_buttonSelectAutoExecMacro";
            this._buttonSelectAutoExecMacro.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._buttonSelectAutoExecMacro.Size = new System.Drawing.Size(19, 19);
            this._buttonSelectAutoExecMacro.TabIndex = 6;
            this._buttonSelectAutoExecMacro.Text = "...";
            this._buttonSelectAutoExecMacro.Click += new System.EventHandler(this._buttonSelectAutoExecMacro_Click);
            // 
            // _groupTerminal
            // 
            this._groupTerminal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._groupTerminal.Controls.Add(this._labelLogType);
            this._groupTerminal.Controls.Add(this._comboBoxLogType);
            this._groupTerminal.Controls.Add(this._textBoxLogFile);
            this._groupTerminal.Controls.Add(this._labelLogFile);
            this._groupTerminal.Controls.Add(this._buttonBrowseLogFile);
            this._groupTerminal.Controls.Add(this._labelEncoding);
            this._groupTerminal.Controls.Add(this._comboBoxEncoding);
            this._groupTerminal.Controls.Add(this._labelLocalEcho);
            this._groupTerminal.Controls.Add(this._comboBoxLocalEcho);
            this._groupTerminal.Controls.Add(this._labelNewLine);
            this._groupTerminal.Controls.Add(this._comboBoxNewLine);
            this._groupTerminal.Controls.Add(this._labelTerminalType);
            this._groupTerminal.Controls.Add(this._comboBoxTerminalType);
            this._groupTerminal.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._groupTerminal.Location = new System.Drawing.Point(12, 156);
            this._groupTerminal.Name = "_groupTerminal";
            this._groupTerminal.Size = new System.Drawing.Size(337, 168);
            this._groupTerminal.TabIndex = 3;
            this._groupTerminal.TabStop = false;
            // 
            // _labelLogType
            // 
            this._labelLogType.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._labelLogType.Location = new System.Drawing.Point(8, 16);
            this._labelLogType.Name = "_labelLogType";
            this._labelLogType.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._labelLogType.Size = new System.Drawing.Size(96, 16);
            this._labelLogType.TabIndex = 0;
            this._labelLogType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _comboBoxLogType
            // 
            this._comboBoxLogType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboBoxLogType.Location = new System.Drawing.Point(112, 16);
            this._comboBoxLogType.Name = "_comboBoxLogType";
            this._comboBoxLogType.Size = new System.Drawing.Size(194, 20);
            this._comboBoxLogType.TabIndex = 1;
            this._comboBoxLogType.SelectedIndexChanged += new System.EventHandler(this._comboBoxLogType_SelectedIndexChanged);
            // 
            // _textBoxLogFile
            // 
            this._textBoxLogFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxLogFile.Location = new System.Drawing.Point(112, 40);
            this._textBoxLogFile.Name = "_textBoxLogFile";
            this._textBoxLogFile.Size = new System.Drawing.Size(194, 19);
            this._textBoxLogFile.TabIndex = 3;
            // 
            // _labelLogFile
            // 
            this._labelLogFile.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._labelLogFile.Location = new System.Drawing.Point(8, 41);
            this._labelLogFile.Name = "_labelLogFile";
            this._labelLogFile.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._labelLogFile.Size = new System.Drawing.Size(88, 16);
            this._labelLogFile.TabIndex = 2;
            this._labelLogFile.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _buttonBrowseLogFile
            // 
            this._buttonBrowseLogFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonBrowseLogFile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonBrowseLogFile.ImageIndex = 0;
            this._buttonBrowseLogFile.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._buttonBrowseLogFile.Location = new System.Drawing.Point(312, 40);
            this._buttonBrowseLogFile.Name = "_buttonBrowseLogFile";
            this._buttonBrowseLogFile.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._buttonBrowseLogFile.Size = new System.Drawing.Size(19, 19);
            this._buttonBrowseLogFile.TabIndex = 4;
            this._buttonBrowseLogFile.Text = "...";
            this._buttonBrowseLogFile.Click += new System.EventHandler(this._buttonBrowseLogFile_Click);
            // 
            // _labelEncoding
            // 
            this._labelEncoding.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._labelEncoding.Location = new System.Drawing.Point(8, 64);
            this._labelEncoding.Name = "_labelEncoding";
            this._labelEncoding.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._labelEncoding.Size = new System.Drawing.Size(96, 16);
            this._labelEncoding.TabIndex = 5;
            this._labelEncoding.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _comboBoxEncoding
            // 
            this._comboBoxEncoding.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboBoxEncoding.Location = new System.Drawing.Point(112, 64);
            this._comboBoxEncoding.Name = "_comboBoxEncoding";
            this._comboBoxEncoding.Size = new System.Drawing.Size(96, 20);
            this._comboBoxEncoding.TabIndex = 6;
            // 
            // _labelLocalEcho
            // 
            this._labelLocalEcho.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._labelLocalEcho.Location = new System.Drawing.Point(8, 88);
            this._labelLocalEcho.Name = "_labelLocalEcho";
            this._labelLocalEcho.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._labelLocalEcho.Size = new System.Drawing.Size(96, 16);
            this._labelLocalEcho.TabIndex = 7;
            this._labelLocalEcho.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _comboBoxLocalEcho
            // 
            this._comboBoxLocalEcho.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboBoxLocalEcho.Location = new System.Drawing.Point(112, 88);
            this._comboBoxLocalEcho.Name = "_comboBoxLocalEcho";
            this._comboBoxLocalEcho.Size = new System.Drawing.Size(96, 20);
            this._comboBoxLocalEcho.TabIndex = 8;
            // 
            // _labelNewLine
            // 
            this._labelNewLine.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._labelNewLine.Location = new System.Drawing.Point(8, 112);
            this._labelNewLine.Name = "_labelNewLine";
            this._labelNewLine.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._labelNewLine.Size = new System.Drawing.Size(96, 16);
            this._labelNewLine.TabIndex = 9;
            this._labelNewLine.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _comboBoxNewLine
            // 
            this._comboBoxNewLine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboBoxNewLine.Location = new System.Drawing.Point(112, 112);
            this._comboBoxNewLine.Name = "_comboBoxNewLine";
            this._comboBoxNewLine.Size = new System.Drawing.Size(96, 20);
            this._comboBoxNewLine.TabIndex = 10;
            // 
            // _labelTerminalType
            // 
            this._labelTerminalType.Location = new System.Drawing.Point(8, 136);
            this._labelTerminalType.Name = "_labelTerminalType";
            this._labelTerminalType.Size = new System.Drawing.Size(96, 23);
            this._labelTerminalType.TabIndex = 11;
            this._labelTerminalType.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _comboBoxTerminalType
            // 
            this._comboBoxTerminalType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._comboBoxTerminalType.Location = new System.Drawing.Point(112, 136);
            this._comboBoxTerminalType.Name = "_comboBoxTerminalType";
            this._comboBoxTerminalType.Size = new System.Drawing.Size(96, 20);
            this._comboBoxTerminalType.TabIndex = 12;
            // 
            // _groupBoxProcess
            // 
            this._groupBoxProcess.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._groupBoxProcess.Controls.Add(this._labelCommandLineOptions);
            this._groupBoxProcess.Controls.Add(this._textBoxCommandLineOptions);
            this._groupBoxProcess.Controls.Add(this._buttonEnvironmentVariables);
            this._groupBoxProcess.Controls.Add(this._labelExePath);
            this._groupBoxProcess.Controls.Add(this._textBoxExePath);
            this._groupBoxProcess.Controls.Add(this._buttonBrowseExePath);
            this._groupBoxProcess.Location = new System.Drawing.Point(12, 339);
            this._groupBoxProcess.Name = "_groupBoxProcess";
            this._groupBoxProcess.Size = new System.Drawing.Size(337, 98);
            this._groupBoxProcess.TabIndex = 2;
            this._groupBoxProcess.TabStop = false;
            // 
            // _labelCommandLineOptions
            // 
            this._labelCommandLineOptions.Location = new System.Drawing.Point(6, 41);
            this._labelCommandLineOptions.Name = "_labelCommandLineOptions";
            this._labelCommandLineOptions.Size = new System.Drawing.Size(100, 29);
            this._labelCommandLineOptions.TabIndex = 3;
            this._labelCommandLineOptions.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _textBoxCommandLineOptions
            // 
            this._textBoxCommandLineOptions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxCommandLineOptions.Location = new System.Drawing.Point(112, 43);
            this._textBoxCommandLineOptions.Name = "_textBoxCommandLineOptions";
            this._textBoxCommandLineOptions.Size = new System.Drawing.Size(194, 19);
            this._textBoxCommandLineOptions.TabIndex = 4;
            // 
            // _buttonEnvironmentVariables
            // 
            this._buttonEnvironmentVariables.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonEnvironmentVariables.Location = new System.Drawing.Point(143, 68);
            this._buttonEnvironmentVariables.Name = "_buttonEnvironmentVariables";
            this._buttonEnvironmentVariables.Size = new System.Drawing.Size(163, 23);
            this._buttonEnvironmentVariables.TabIndex = 5;
            this._buttonEnvironmentVariables.UseVisualStyleBackColor = true;
            this._buttonEnvironmentVariables.Click += new System.EventHandler(this._buttonEnvironmentVariables_Click);
            // 
            // _labelExePath
            // 
            this._labelExePath.Location = new System.Drawing.Point(6, 16);
            this._labelExePath.Name = "_labelExePath";
            this._labelExePath.Size = new System.Drawing.Size(100, 23);
            this._labelExePath.TabIndex = 0;
            this._labelExePath.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _textBoxExePath
            // 
            this._textBoxExePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._textBoxExePath.Location = new System.Drawing.Point(112, 18);
            this._textBoxExePath.Name = "_textBoxExePath";
            this._textBoxExePath.Size = new System.Drawing.Size(194, 19);
            this._textBoxExePath.TabIndex = 1;
            // 
            // _buttonBrowseExePath
            // 
            this._buttonBrowseExePath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonBrowseExePath.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonBrowseExePath.ImageIndex = 0;
            this._buttonBrowseExePath.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._buttonBrowseExePath.Location = new System.Drawing.Point(312, 18);
            this._buttonBrowseExePath.Name = "_buttonBrowseExePath";
            this._buttonBrowseExePath.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._buttonBrowseExePath.Size = new System.Drawing.Size(19, 19);
            this._buttonBrowseExePath.TabIndex = 2;
            this._buttonBrowseExePath.Text = "...";
            this._buttonBrowseExePath.Click += new System.EventHandler(this._buttonBrowseExePath_Click);
            // 
            // panelModeSwitch
            // 
            this.panelModeSwitch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.panelModeSwitch.AutoSize = true;
            this.panelModeSwitch.Controls.Add(this._radioButtonProcess);
            this.panelModeSwitch.Controls.Add(this._radioButtonPipe);
            this.panelModeSwitch.Location = new System.Drawing.Point(12, 12);
            this.panelModeSwitch.Name = "panelModeSwitch";
            this.panelModeSwitch.Size = new System.Drawing.Size(249, 27);
            this.panelModeSwitch.TabIndex = 0;
            // 
            // OpenPipeDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this._buttonCancel;
            this.ClientSize = new System.Drawing.Size(361, 545);
            this.Controls.Add(this.panelModeSwitch);
            this.Controls.Add(this._groupBoxProcess);
            this.Controls.Add(this._groupTerminal);
            this.Controls.Add(this._labelAutoExecMacroPath);
            this.Controls.Add(this._textBoxAutoExecMacroPath);
            this.Controls.Add(this._buttonSelectAutoExecMacro);
            this.Controls.Add(this._buttonCancel);
            this.Controls.Add(this._buttonOK);
            this.Controls.Add(this._groupBoxPipe);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OpenPipeDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this._groupBoxPipe.ResumeLayout(false);
            this._groupBoxPipe.PerformLayout();
            this._groupTerminal.ResumeLayout(false);
            this._groupTerminal.PerformLayout();
            this._groupBoxProcess.ResumeLayout(false);
            this._groupBoxProcess.PerformLayout();
            this.panelModeSwitch.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton _radioButtonProcess;
        private System.Windows.Forms.RadioButton _radioButtonPipe;
        private System.Windows.Forms.GroupBox _groupBoxPipe;
        private System.Windows.Forms.Button _buttonOK;
        private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.Label _labelAutoExecMacroPath;
        private System.Windows.Forms.TextBox _textBoxAutoExecMacroPath;
        private System.Windows.Forms.Button _buttonSelectAutoExecMacro;
        private System.Windows.Forms.Label _labelInputPath;
        private System.Windows.Forms.TextBox _textBoxInputPath;
        private System.Windows.Forms.GroupBox _groupTerminal;
        private System.Windows.Forms.Label _labelLogType;
        private System.Windows.Forms.ComboBox _comboBoxLogType;
        private System.Windows.Forms.Label _labelLogFile;
        private System.Windows.Forms.Button _buttonBrowseLogFile;
        private System.Windows.Forms.Label _labelEncoding;
        private System.Windows.Forms.ComboBox _comboBoxEncoding;
        private System.Windows.Forms.Label _labelLocalEcho;
        private System.Windows.Forms.ComboBox _comboBoxLocalEcho;
        private System.Windows.Forms.Label _labelNewLine;
        private System.Windows.Forms.ComboBox _comboBoxNewLine;
        private System.Windows.Forms.Label _labelTerminalType;
        private System.Windows.Forms.ComboBox _comboBoxTerminalType;
        private System.Windows.Forms.GroupBox _groupBoxProcess;
        private System.Windows.Forms.Label _labelExePath;
        private System.Windows.Forms.TextBox _textBoxExePath;
        private System.Windows.Forms.Button _buttonBrowseExePath;
        private System.Windows.Forms.Label _labelOutputPath;
        private System.Windows.Forms.TextBox _textBoxOutputPath;
        private System.Windows.Forms.CheckBox _checkBoxBidirectinal;
        private System.Windows.Forms.Panel panelModeSwitch;
        private System.Windows.Forms.Button _buttonEnvironmentVariables;
        private System.Windows.Forms.TextBox _textBoxLogFile;
        private System.Windows.Forms.Label _labelCommandLineOptions;
        private System.Windows.Forms.TextBox _textBoxCommandLineOptions;
    }
}