/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SerialLoginDialog.cs,v 1.6 2012/03/15 14:57:42 kzmi Exp $
 */
using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;

using Poderosa.Util;
using Poderosa.ConnectionParam;
using Poderosa.Terminal;
using Poderosa.Protocols;

namespace Poderosa.SerialPort {
    internal class SerialLoginDialog : System.Windows.Forms.Form {

        private SerialTerminalConnection _connection;
        private SerialTerminalSettings _terminalSettings;
        private SerialTerminalParam _terminalParam;

        private System.Windows.Forms.Button _loginButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.GroupBox _terminalGroup;
        private ComboBox _logTypeBox;
        private System.Windows.Forms.Label _logTypeLabel;
        private ComboBox _newLineBox;
        private ComboBox _localEchoBox;
        private System.Windows.Forms.Label _localEchoLabel;
        private System.Windows.Forms.Label _newLineLabel;
        private ComboBox _logFileBox;
        private System.Windows.Forms.Label _logFileLabel;
        private ComboBox _encodingBox;
        private System.Windows.Forms.Label _encodingLabel;
        private Button _selectLogButton;
        private System.Windows.Forms.GroupBox _serialGroup;
        private ComboBox _flowControlBox;
        private System.Windows.Forms.Label _flowControlLabel;
        private ComboBox _stopBitsBox;
        private System.Windows.Forms.Label _stopBitsLabel;
        private ComboBox _parityBox;
        private System.Windows.Forms.Label _parityLabel;
        private ComboBox _dataBitsBox;
        private System.Windows.Forms.Label _dataBitsLabel;
        private ComboBox _baudRateBox;
        private System.Windows.Forms.Label _baudRateLabel;
        private ComboBox _portBox;
        private System.Windows.Forms.Label _portLabel;
        private Label _transmitDelayPerCharLabel;
        private TextBox _transmitDelayPerCharBox;
        private Label _transmitDelayPerLineLabel;
        private TextBox _transmitDelayPerLineBox;
        private Label _autoExecMacroPathLabel;
        private TextBox _autoExecMacroPathBox;
        private Button _selectAutoExecMacroButton;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public SerialLoginDialog() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();

            StringResource sr = SerialPortPlugin.Instance.Strings;
            this._serialGroup.Text = sr.GetString("Form.SerialLoginDialog._serialGroup");
            //以下、SerialConfigとテキストを共用
            this._portLabel.Text = sr.GetString("Form.SerialConfig._portLabel");
            this._baudRateLabel.Text = sr.GetString("Form.SerialConfig._baudRateLabel");
            this._dataBitsLabel.Text = sr.GetString("Form.SerialConfig._dataBitsLabel");
            this._parityLabel.Text = sr.GetString("Form.SerialConfig._parityLabel");
            this._stopBitsLabel.Text = sr.GetString("Form.SerialConfig._stopBitsLabel");
            this._flowControlLabel.Text = sr.GetString("Form.SerialConfig._flowControlLabel");
            this._transmitDelayPerLineLabel.Text = "Transmit Delay(line)";
            this._transmitDelayPerCharLabel.Text = "Transmit Delay(char)";
            string bits = sr.GetString("Caption.SerialConfig.Bits");
            this._parityBox.Items.AddRange(EnumListItem<Parity>.GetListItems());
            this._dataBitsBox.Items.AddRange(new object[] {
                String.Format("{0}{1}", 7, bits),
                String.Format("{0}{1}", 8, bits)});
            this._stopBitsBox.Items.AddRange(EnumListItem<StopBits>.GetListItems());
            this._baudRateBox.Items.AddRange(TerminalUtil.BaudRates);
            this._flowControlBox.Items.AddRange(EnumListItem<FlowControl>.GetListItems());

            this._terminalGroup.Text = sr.GetString("Form.SerialLoginDialog._terminalGroup");

            this._localEchoLabel.Text = sr.GetString("Form.SerialLoginDialog._localEchoLabel");
            this._newLineLabel.Text = sr.GetString("Form.SerialLoginDialog._newLineLabel");
            this._logFileLabel.Text = sr.GetString("Form.SerialLoginDialog._logFileLabel");
            this._encodingLabel.Text = sr.GetString("Form.SerialLoginDialog._encodingLabel");
            this._logTypeLabel.Text = sr.GetString("Form.SerialLoginDialog._logTypeLabel");
            this._logTypeBox.Items.AddRange(EnumListItem<LogType>.GetListItems());
            this._localEchoBox.Items.AddRange(new object[] {
                sr.GetString("Common.DoNot"),
                sr.GetString("Common.Do")});
            this._newLineBox.Items.AddRange(EnumListItem<NewLine>.GetListItems());
            this._encodingBox.Items.AddRange(EnumListItem<EncodingType>.GetListItems());
            this._autoExecMacroPathLabel.Text = sr.GetString("Form.SerialLoginDialog._autoExecMacroPathLabel");
            this._loginButton.Text = sr.GetString("Common.OK");
            this._cancelButton.Text = sr.GetString("Common.Cancel");
            this.Text = sr.GetString("Form.SerialLoginDialog.Text");

            InitUI();
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
            this._serialGroup = new System.Windows.Forms.GroupBox();
            this._transmitDelayPerCharBox = new System.Windows.Forms.TextBox();
            this._transmitDelayPerCharLabel = new System.Windows.Forms.Label();
            this._transmitDelayPerLineBox = new System.Windows.Forms.TextBox();
            this._transmitDelayPerLineLabel = new System.Windows.Forms.Label();
            this._flowControlBox = new System.Windows.Forms.ComboBox();
            this._flowControlLabel = new System.Windows.Forms.Label();
            this._stopBitsBox = new System.Windows.Forms.ComboBox();
            this._stopBitsLabel = new System.Windows.Forms.Label();
            this._parityBox = new System.Windows.Forms.ComboBox();
            this._parityLabel = new System.Windows.Forms.Label();
            this._dataBitsBox = new System.Windows.Forms.ComboBox();
            this._dataBitsLabel = new System.Windows.Forms.Label();
            this._baudRateBox = new System.Windows.Forms.ComboBox();
            this._baudRateLabel = new System.Windows.Forms.Label();
            this._portBox = new System.Windows.Forms.ComboBox();
            this._portLabel = new System.Windows.Forms.Label();
            this._terminalGroup = new System.Windows.Forms.GroupBox();
            this._logTypeBox = new System.Windows.Forms.ComboBox();
            this._logTypeLabel = new System.Windows.Forms.Label();
            this._newLineBox = new System.Windows.Forms.ComboBox();
            this._localEchoBox = new System.Windows.Forms.ComboBox();
            this._localEchoLabel = new System.Windows.Forms.Label();
            this._newLineLabel = new System.Windows.Forms.Label();
            this._logFileBox = new System.Windows.Forms.ComboBox();
            this._logFileLabel = new System.Windows.Forms.Label();
            this._encodingBox = new System.Windows.Forms.ComboBox();
            this._encodingLabel = new System.Windows.Forms.Label();
            this._selectLogButton = new System.Windows.Forms.Button();
            this._autoExecMacroPathLabel = new System.Windows.Forms.Label();
            this._autoExecMacroPathBox = new System.Windows.Forms.TextBox();
            this._selectAutoExecMacroButton = new System.Windows.Forms.Button();
            this._loginButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._serialGroup.SuspendLayout();
            this._terminalGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _serialGroup
            // 
            this._serialGroup.Controls.Add(this._transmitDelayPerCharBox);
            this._serialGroup.Controls.Add(this._transmitDelayPerCharLabel);
            this._serialGroup.Controls.Add(this._transmitDelayPerLineBox);
            this._serialGroup.Controls.Add(this._transmitDelayPerLineLabel);
            this._serialGroup.Controls.Add(this._flowControlBox);
            this._serialGroup.Controls.Add(this._flowControlLabel);
            this._serialGroup.Controls.Add(this._stopBitsBox);
            this._serialGroup.Controls.Add(this._stopBitsLabel);
            this._serialGroup.Controls.Add(this._parityBox);
            this._serialGroup.Controls.Add(this._parityLabel);
            this._serialGroup.Controls.Add(this._dataBitsBox);
            this._serialGroup.Controls.Add(this._dataBitsLabel);
            this._serialGroup.Controls.Add(this._baudRateBox);
            this._serialGroup.Controls.Add(this._baudRateLabel);
            this._serialGroup.Controls.Add(this._portBox);
            this._serialGroup.Controls.Add(this._portLabel);
            this._serialGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._serialGroup.Location = new System.Drawing.Point(8, 8);
            this._serialGroup.Name = "_serialGroup";
            this._serialGroup.Size = new System.Drawing.Size(296, 224);
            this._serialGroup.TabIndex = 0;
            this._serialGroup.TabStop = false;
            // 
            // _transmitDelayPerCharBox
            // 
            this._transmitDelayPerCharBox.Location = new System.Drawing.Point(136, 160);
            this._transmitDelayPerCharBox.MaxLength = 3;
            this._transmitDelayPerCharBox.Name = "_transmitDelayPerCharBox";
            this._transmitDelayPerCharBox.Size = new System.Drawing.Size(120, 19);
            this._transmitDelayPerCharBox.TabIndex = 14;
            // 
            // _transmitDelayPerCharLabel
            // 
            this._transmitDelayPerCharLabel.Location = new System.Drawing.Point(8, 160);
            this._transmitDelayPerCharLabel.Name = "_transmitDelayPerCharLabel";
            this._transmitDelayPerCharLabel.Size = new System.Drawing.Size(128, 23);
            this._transmitDelayPerCharLabel.TabIndex = 13;
            this._transmitDelayPerCharLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _transmitDelayPerLineBox
            // 
            this._transmitDelayPerLineBox.Location = new System.Drawing.Point(136, 184);
            this._transmitDelayPerLineBox.MaxLength = 3;
            this._transmitDelayPerLineBox.Name = "_transmitDelayPerLineBox";
            this._transmitDelayPerLineBox.Size = new System.Drawing.Size(120, 19);
            this._transmitDelayPerLineBox.TabIndex = 16;
            // 
            // _transmitDelayPerLineLabel
            // 
            this._transmitDelayPerLineLabel.Location = new System.Drawing.Point(8, 184);
            this._transmitDelayPerLineLabel.Name = "_transmitDelayPerLineLabel";
            this._transmitDelayPerLineLabel.Size = new System.Drawing.Size(128, 23);
            this._transmitDelayPerLineLabel.TabIndex = 15;
            this._transmitDelayPerLineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _flowControlBox
            // 
            this._flowControlBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._flowControlBox.Location = new System.Drawing.Point(136, 136);
            this._flowControlBox.Name = "_flowControlBox";
            this._flowControlBox.Size = new System.Drawing.Size(120, 20);
            this._flowControlBox.TabIndex = 12;
            // 
            // _flowControlLabel
            // 
            this._flowControlLabel.Location = new System.Drawing.Point(8, 136);
            this._flowControlLabel.Name = "_flowControlLabel";
            this._flowControlLabel.Size = new System.Drawing.Size(88, 23);
            this._flowControlLabel.TabIndex = 11;
            this._flowControlLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _stopBitsBox
            // 
            this._stopBitsBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._stopBitsBox.Location = new System.Drawing.Point(136, 112);
            this._stopBitsBox.Name = "_stopBitsBox";
            this._stopBitsBox.Size = new System.Drawing.Size(120, 20);
            this._stopBitsBox.TabIndex = 10;
            // 
            // _stopBitsLabel
            // 
            this._stopBitsLabel.Location = new System.Drawing.Point(8, 112);
            this._stopBitsLabel.Name = "_stopBitsLabel";
            this._stopBitsLabel.Size = new System.Drawing.Size(88, 23);
            this._stopBitsLabel.TabIndex = 9;
            this._stopBitsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _parityBox
            // 
            this._parityBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._parityBox.Location = new System.Drawing.Point(136, 88);
            this._parityBox.Name = "_parityBox";
            this._parityBox.Size = new System.Drawing.Size(120, 20);
            this._parityBox.TabIndex = 8;
            // 
            // _parityLabel
            // 
            this._parityLabel.Location = new System.Drawing.Point(8, 88);
            this._parityLabel.Name = "_parityLabel";
            this._parityLabel.Size = new System.Drawing.Size(88, 23);
            this._parityLabel.TabIndex = 7;
            this._parityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _dataBitsBox
            // 
            this._dataBitsBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._dataBitsBox.Location = new System.Drawing.Point(136, 64);
            this._dataBitsBox.Name = "_dataBitsBox";
            this._dataBitsBox.Size = new System.Drawing.Size(120, 20);
            this._dataBitsBox.TabIndex = 6;
            // 
            // _dataBitsLabel
            // 
            this._dataBitsLabel.Location = new System.Drawing.Point(8, 64);
            this._dataBitsLabel.Name = "_dataBitsLabel";
            this._dataBitsLabel.Size = new System.Drawing.Size(88, 23);
            this._dataBitsLabel.TabIndex = 5;
            this._dataBitsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _baudRateBox
            // 
            this._baudRateBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._baudRateBox.Location = new System.Drawing.Point(136, 40);
            this._baudRateBox.Name = "_baudRateBox";
            this._baudRateBox.Size = new System.Drawing.Size(120, 20);
            this._baudRateBox.TabIndex = 4;
            // 
            // _baudRateLabel
            // 
            this._baudRateLabel.Location = new System.Drawing.Point(8, 40);
            this._baudRateLabel.Name = "_baudRateLabel";
            this._baudRateLabel.Size = new System.Drawing.Size(88, 23);
            this._baudRateLabel.TabIndex = 3;
            this._baudRateLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _portBox
            // 
            this._portBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._portBox.Location = new System.Drawing.Point(136, 16);
            this._portBox.Name = "_portBox";
            this._portBox.Size = new System.Drawing.Size(120, 20);
            this._portBox.TabIndex = 2;
            // 
            // _portLabel
            // 
            this._portLabel.Location = new System.Drawing.Point(8, 16);
            this._portLabel.Name = "_portLabel";
            this._portLabel.Size = new System.Drawing.Size(88, 23);
            this._portLabel.TabIndex = 1;
            this._portLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _terminalGroup
            // 
            this._terminalGroup.Controls.Add(this._logTypeBox);
            this._terminalGroup.Controls.Add(this._logTypeLabel);
            this._terminalGroup.Controls.Add(this._newLineBox);
            this._terminalGroup.Controls.Add(this._localEchoBox);
            this._terminalGroup.Controls.Add(this._localEchoLabel);
            this._terminalGroup.Controls.Add(this._newLineLabel);
            this._terminalGroup.Controls.Add(this._logFileBox);
            this._terminalGroup.Controls.Add(this._logFileLabel);
            this._terminalGroup.Controls.Add(this._encodingBox);
            this._terminalGroup.Controls.Add(this._encodingLabel);
            this._terminalGroup.Controls.Add(this._selectLogButton);
            this._terminalGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._terminalGroup.Location = new System.Drawing.Point(8, 240);
            this._terminalGroup.Name = "_terminalGroup";
            this._terminalGroup.Size = new System.Drawing.Size(296, 144);
            this._terminalGroup.TabIndex = 1;
            this._terminalGroup.TabStop = false;
            // 
            // _logTypeBox
            // 
            this._logTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._logTypeBox.Location = new System.Drawing.Point(136, 16);
            this._logTypeBox.Name = "_logTypeBox";
            this._logTypeBox.Size = new System.Drawing.Size(154, 20);
            this._logTypeBox.TabIndex = 19;
            this._logTypeBox.SelectedIndexChanged += new System.EventHandler(this.OnLogTypeChanged);
            // 
            // _logTypeLabel
            // 
            this._logTypeLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._logTypeLabel.Location = new System.Drawing.Point(8, 16);
            this._logTypeLabel.Name = "_logTypeLabel";
            this._logTypeLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._logTypeLabel.Size = new System.Drawing.Size(120, 16);
            this._logTypeLabel.TabIndex = 18;
            this._logTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _newLineBox
            // 
            this._newLineBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._newLineBox.Location = new System.Drawing.Point(136, 112);
            this._newLineBox.Name = "_newLineBox";
            this._newLineBox.Size = new System.Drawing.Size(120, 20);
            this._newLineBox.TabIndex = 28;
            // 
            // _localEchoBox
            // 
            this._localEchoBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._localEchoBox.Location = new System.Drawing.Point(136, 88);
            this._localEchoBox.Name = "_localEchoBox";
            this._localEchoBox.Size = new System.Drawing.Size(120, 20);
            this._localEchoBox.TabIndex = 26;
            // 
            // _localEchoLabel
            // 
            this._localEchoLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._localEchoLabel.Location = new System.Drawing.Point(8, 88);
            this._localEchoLabel.Name = "_localEchoLabel";
            this._localEchoLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._localEchoLabel.Size = new System.Drawing.Size(96, 16);
            this._localEchoLabel.TabIndex = 25;
            this._localEchoLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _newLineLabel
            // 
            this._newLineLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._newLineLabel.Location = new System.Drawing.Point(8, 112);
            this._newLineLabel.Name = "_newLineLabel";
            this._newLineLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._newLineLabel.Size = new System.Drawing.Size(96, 16);
            this._newLineLabel.TabIndex = 27;
            this._newLineLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _logFileBox
            // 
            this._logFileBox.Location = new System.Drawing.Point(136, 40);
            this._logFileBox.Name = "_logFileBox";
            this._logFileBox.Size = new System.Drawing.Size(120, 20);
            this._logFileBox.TabIndex = 21;
            // 
            // _logFileLabel
            // 
            this._logFileLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._logFileLabel.Location = new System.Drawing.Point(8, 40);
            this._logFileLabel.Name = "_logFileLabel";
            this._logFileLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._logFileLabel.Size = new System.Drawing.Size(88, 16);
            this._logFileLabel.TabIndex = 20;
            this._logFileLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _encodingBox
            // 
            this._encodingBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._encodingBox.Location = new System.Drawing.Point(136, 64);
            this._encodingBox.Name = "_encodingBox";
            this._encodingBox.Size = new System.Drawing.Size(120, 20);
            this._encodingBox.TabIndex = 24;
            // 
            // _encodingLabel
            // 
            this._encodingLabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._encodingLabel.Location = new System.Drawing.Point(8, 64);
            this._encodingLabel.Name = "_encodingLabel";
            this._encodingLabel.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._encodingLabel.Size = new System.Drawing.Size(96, 16);
            this._encodingLabel.TabIndex = 23;
            this._encodingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _selectLogButton
            // 
            this._selectLogButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectLogButton.ImageIndex = 0;
            this._selectLogButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._selectLogButton.Location = new System.Drawing.Point(256, 40);
            this._selectLogButton.Name = "_selectLogButton";
            this._selectLogButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._selectLogButton.Size = new System.Drawing.Size(19, 19);
            this._selectLogButton.TabIndex = 22;
            this._selectLogButton.Text = "...";
            this._selectLogButton.Click += new System.EventHandler(this.SelectLog);
            // 
            // _autoExecMacroPathLabel
            // 
            this._autoExecMacroPathLabel.Location = new System.Drawing.Point(16, 394);
            this._autoExecMacroPathLabel.Name = "_autoExecMacroPathLabel";
            this._autoExecMacroPathLabel.Size = new System.Drawing.Size(128, 23);
            this._autoExecMacroPathLabel.TabIndex = 2;
            this._autoExecMacroPathLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _autoExecMacroPathBox
            // 
            this._autoExecMacroPathBox.Location = new System.Drawing.Point(144, 394);
            this._autoExecMacroPathBox.MaxLength = 3;
            this._autoExecMacroPathBox.Name = "_autoExecMacroPathBox";
            this._autoExecMacroPathBox.Size = new System.Drawing.Size(120, 19);
            this._autoExecMacroPathBox.TabIndex = 3;
            // 
            // _selectAutoExecMacroButton
            // 
            this._selectAutoExecMacroButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._selectAutoExecMacroButton.ImageIndex = 0;
            this._selectAutoExecMacroButton.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._selectAutoExecMacroButton.Location = new System.Drawing.Point(264, 394);
            this._selectAutoExecMacroButton.Name = "_selectAutoExecMacroButton";
            this._selectAutoExecMacroButton.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this._selectAutoExecMacroButton.Size = new System.Drawing.Size(19, 19);
            this._selectAutoExecMacroButton.TabIndex = 4;
            this._selectAutoExecMacroButton.Text = "...";
            this._selectAutoExecMacroButton.Click += new System.EventHandler(this._selectAutoExecMacroButton_Click);
            // 
            // _loginButton
            // 
            this._loginButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._loginButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._loginButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._loginButton.Location = new System.Drawing.Point(136, 425);
            this._loginButton.Name = "_loginButton";
            this._loginButton.Size = new System.Drawing.Size(75, 23);
            this._loginButton.TabIndex = 5;
            this._loginButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(224, 425);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 6;
            // 
            // SerialLoginDialog
            // 
            this.AcceptButton = this._loginButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(314, 456);
            this.Controls.Add(this._serialGroup);
            this.Controls.Add(this._terminalGroup);
            this.Controls.Add(this._autoExecMacroPathLabel);
            this.Controls.Add(this._autoExecMacroPathBox);
            this.Controls.Add(this._selectAutoExecMacroButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._loginButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SerialLoginDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this._serialGroup.ResumeLayout(false);
            this._serialGroup.PerformLayout();
            this._terminalGroup.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        #endregion

        public SerialTerminalConnection ResultConnection {
            get {
                return _connection;
            }
        }
        public SerialTerminalSettings ResultTerminalSettings {
            get {
                return _terminalSettings;
            }
        }

        private void InitUI() {
            // シリアルポート名をそのままアイテムとする。
            _portBox.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
            if (_portBox.Items.Count <= 0) {
                // ポートが1つも無い場合はOKを無効化しておく。
                _loginButton.Enabled = false;
            }

            _logTypeBox.SelectedItem = LogType.None;    // select EnumListItem<T> by T

            AdjustUI();
        }
        private void AdjustUI() {
            bool e = ((EnumListItem<LogType>)_logTypeBox.SelectedItem).Value != LogType.None;
            _logFileBox.Enabled = e;
            _selectLogButton.Enabled = e;
        }

        public void ApplyParam(SerialTerminalParam param, SerialTerminalSettings settings) {
            _terminalParam = param == null ? new SerialTerminalParam() : param;
            _terminalSettings = settings == null ? SerialPortUtil.CreateDefaultSerialTerminalSettings(_terminalParam.PortName) : settings;

            // 設定のポート名称のアイテムを選択。それが選択できなければ最初の項目を選択。
            _portBox.SelectedItem = _terminalParam.PortName;
            if (_portBox.SelectedItem == null && 0 < _portBox.Items.Count) {
                _portBox.SelectedIndex = 0;
            }

            //これらのSelectedIndexの設定はコンボボックスに設定した項目順に依存しているので注意深くすること
            _baudRateBox.SelectedIndex = _baudRateBox.FindStringExact(_terminalSettings.BaudRate.ToString());
            _dataBitsBox.SelectedIndex = _terminalSettings.ByteSize == 7 ? 0 : 1;
            _parityBox.SelectedItem = _terminalSettings.Parity;             // select EnumListItem<T> by T
            _stopBitsBox.SelectedItem = _terminalSettings.StopBits;         // select EnumListItem<T> by T
            _flowControlBox.SelectedItem = _terminalSettings.FlowControl;   // select EnumListItem<T> by T

            _encodingBox.SelectedItem = _terminalSettings.Encoding;         // select EnumListItem<T> by T
            _newLineBox.SelectedItem = _terminalSettings.TransmitNL;        // select EnumListItem<T> by T
            _localEchoBox.SelectedIndex = _terminalSettings.LocalEcho ? 1 : 0;

            _transmitDelayPerCharBox.Text = _terminalSettings.TransmitDelayPerChar.ToString();
            _transmitDelayPerLineBox.Text = _terminalSettings.TransmitDelayPerLine.ToString();

            IAutoExecMacroParameter autoExecParams = param.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
            if (autoExecParams != null && SerialPortPlugin.Instance.MacroEngine != null) {
                _autoExecMacroPathBox.Text = (autoExecParams.AutoExecMacroPath != null) ? autoExecParams.AutoExecMacroPath : String.Empty;
            }
            else {
                _autoExecMacroPathLabel.Enabled = false;
                _autoExecMacroPathBox.Enabled = false;
                _selectAutoExecMacroButton.Enabled = false;
            }
        }

        private void OnOK(object sender, EventArgs args) {
            _connection = null;
            this.DialogResult = DialogResult.None;

            if (!ValidateParam())
                return;

            try {
                _connection = SerialPortUtil.CreateNewSerialConnection(null, _terminalParam, _terminalSettings);
                if (_connection != null)
                    this.DialogResult = DialogResult.OK;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
            }

        }


        private bool ValidateParam() {
            SerialTerminalSettings settings = _terminalSettings;
            SerialTerminalParam param = _terminalParam;
            try {
                LogType logtype = ((EnumListItem<LogType>)_logTypeBox.SelectedItem).Value;
                ISimpleLogSettings logsettings = null;
                if (logtype != LogType.None) {
                    logsettings = CreateSimpleLogSettings(logtype, _logFileBox.Text);
                    if (logsettings == null)
                        return false; //動作キャンセル
                }

                param.PortName = _portBox.SelectedItem as string;
                if (param.PortName == null) {
                    return false;
                }

                string autoExecMacroPath = null;
                if (_autoExecMacroPathBox.Text.Length != 0)
                    autoExecMacroPath = _autoExecMacroPathBox.Text;

                IAutoExecMacroParameter autoExecParams = param.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
                if (autoExecParams != null)
                    autoExecParams.AutoExecMacroPath = autoExecMacroPath;

                settings.BeginUpdate();
                if (logsettings != null)
                    settings.LogSettings.Reset(logsettings);
                settings.Caption = param.PortName;
                settings.BaudRate = Int32.Parse(_baudRateBox.Text);
                settings.ByteSize = (byte)(_dataBitsBox.SelectedIndex == 0 ? 7 : 8);
                settings.StopBits = ((EnumListItem<StopBits>)_stopBitsBox.SelectedItem).Value;
                settings.Parity = ((EnumListItem<Parity>)_parityBox.SelectedItem).Value;
                settings.FlowControl = ((EnumListItem<FlowControl>)_flowControlBox.SelectedItem).Value;

                settings.Encoding = ((EnumListItem<EncodingType>)_encodingBox.SelectedItem).Value;

                settings.LocalEcho = _localEchoBox.SelectedIndex == 1;
                settings.TransmitNL = ((EnumListItem<NewLine>)_newLineBox.SelectedItem).Value;

                settings.TransmitDelayPerChar = Int32.Parse(_transmitDelayPerCharBox.Text);
                settings.TransmitDelayPerLine = Int32.Parse(_transmitDelayPerLineBox.Text);
                settings.EndUpdate();
                return true;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                return false;
            }

        }
        private void SelectLog(object sender, System.EventArgs e) {
            string fn = LogUtil.SelectLogFileByDialog(this);
            if (fn != null)
                _logFileBox.Text = fn;
        }
        private void OnLogTypeChanged(object sender, System.EventArgs args) {
            AdjustUI();
        }

        //ログ設定を作る。単一ファイル版。
        protected ISimpleLogSettings CreateSimpleLogSettings(LogType logtype, string path) {
            ISimpleLogSettings logsettings = SerialPortPlugin.Instance.TerminalEmulatorService.CreateDefaultSimpleLogSettings();
            logsettings.LogPath = path;
            logsettings.LogType = logtype;
            LogFileCheckResult r = LogUtil.CheckLogFileName(path, this);
            if (r == LogFileCheckResult.Cancel || r == LogFileCheckResult.Error)
                return null;
            logsettings.LogAppend = (r == LogFileCheckResult.Append);
            return logsettings;
        }

        private void _selectAutoExecMacroButton_Click(object sender, EventArgs e) {
            if (SerialPortPlugin.Instance.MacroEngine != null) {
                string path = SerialPortPlugin.Instance.MacroEngine.SelectMacro(this);
                if (path != null)
                    _autoExecMacroPathBox.Text = path;
            }
        }

    }

}
