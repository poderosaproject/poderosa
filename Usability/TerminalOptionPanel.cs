/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalOptionPanel.cs,v 1.6 2012/03/17 14:53:10 kzmi Exp $
 */
using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Poderosa.ConnectionParam;
using Poderosa.Util;
using Poderosa.UI;
using Poderosa.Usability;
using Poderosa.Preferences;
using Poderosa.Terminal;

namespace Poderosa.Forms {
    internal class TerminalOptionPanel : UserControl {
        private System.Windows.Forms.Label _charDecodeErrorBehaviorLabel;
        private ComboBox _charDecodeErrorBehaviorBox;
        private System.Windows.Forms.Label _bufferSizeLabel;
        private TextBox _bufferSize;
        private Label _additionalWordElementLabel;
        private TextBox _additionalWordElementBox;
        private Label _disconnectNotificationLabel;
        private ComboBox _disconnectNotification;
        private CheckBox _closeOnDisconnect;
        private CheckBox _beepOnBellChar;
        private CheckBox _allowsScrollInAppMode;
        private CheckBox _keepAliveCheck;
        private TextBox _keepAliveIntervalBox;
        private Label _keepAliveLabel;
        private System.Windows.Forms.GroupBox _defaultLogGroup;
        private CheckBox _autoLogCheckBox;
        private System.Windows.Forms.Label _defaultLogTypeLabel;
        private ComboBox _defaultLogTypeBox;
        private System.Windows.Forms.Label _defaultLogDirectoryLabel;
        private TextBox _defaultLogDirectory;
        private System.Windows.Forms.Button _dirSelect;
        private GroupBox _shellSupportGroup;
        private CheckBox _enableComplementForNewConnections;
        private CheckBox _commandPopupAlwaysOnTop;
        private GroupBox _copyAndPasteGroup;
        private CheckBox _enablePasteConfirm;
        private CheckBox _enablePasteConfirmAlways;
        private CheckBox _enableChangeDialogSize;
        private CheckBox _showConfirmedCheckBox;
        private TextBox _pasteAfterSpecifiedTimeBox;
        private CheckBox _pasteAfterSpecifiedTime;
        private Label _highlightKeywordLabel;
        private TextBox _highlightKeyword;
        private CheckBox _commandPopupInTaskBar;

        public TerminalOptionPanel() {
            InitializeComponent();
            FillText();
        }
        private void InitializeComponent() {
            this._charDecodeErrorBehaviorLabel = new System.Windows.Forms.Label();
            this._charDecodeErrorBehaviorBox = new System.Windows.Forms.ComboBox();
            this._bufferSizeLabel = new System.Windows.Forms.Label();
            this._bufferSize = new System.Windows.Forms.TextBox();
            this._additionalWordElementLabel = new System.Windows.Forms.Label();
            this._additionalWordElementBox = new System.Windows.Forms.TextBox();
            this._disconnectNotificationLabel = new System.Windows.Forms.Label();
            this._disconnectNotification = new System.Windows.Forms.ComboBox();
            this._closeOnDisconnect = new System.Windows.Forms.CheckBox();
            this._beepOnBellChar = new System.Windows.Forms.CheckBox();
            this._allowsScrollInAppMode = new System.Windows.Forms.CheckBox();
            this._keepAliveCheck = new System.Windows.Forms.CheckBox();
            this._keepAliveIntervalBox = new System.Windows.Forms.TextBox();
            this._keepAliveLabel = new System.Windows.Forms.Label();
            this._defaultLogGroup = new System.Windows.Forms.GroupBox();
            this._defaultLogTypeLabel = new System.Windows.Forms.Label();
            this._defaultLogTypeBox = new System.Windows.Forms.ComboBox();
            this._defaultLogDirectoryLabel = new System.Windows.Forms.Label();
            this._defaultLogDirectory = new System.Windows.Forms.TextBox();
            this._dirSelect = new System.Windows.Forms.Button();
            this._autoLogCheckBox = new System.Windows.Forms.CheckBox();
            this._shellSupportGroup = new System.Windows.Forms.GroupBox();
            this._enableComplementForNewConnections = new System.Windows.Forms.CheckBox();
            this._commandPopupAlwaysOnTop = new System.Windows.Forms.CheckBox();
            this._commandPopupInTaskBar = new System.Windows.Forms.CheckBox();
            this._copyAndPasteGroup = new System.Windows.Forms.GroupBox();
            this._highlightKeyword = new System.Windows.Forms.TextBox();
            this._highlightKeywordLabel = new System.Windows.Forms.Label();
            this._pasteAfterSpecifiedTimeBox = new System.Windows.Forms.TextBox();
            this._enableChangeDialogSize = new System.Windows.Forms.CheckBox();
            this._showConfirmedCheckBox = new System.Windows.Forms.CheckBox();
            this._pasteAfterSpecifiedTime = new System.Windows.Forms.CheckBox();
            this._enablePasteConfirmAlways = new System.Windows.Forms.CheckBox();
            this._enablePasteConfirm = new System.Windows.Forms.CheckBox();
            this._defaultLogGroup.SuspendLayout();
            this._shellSupportGroup.SuspendLayout();
            this._copyAndPasteGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _charDecodeErrorBehaviorLabel
            // 
            this._charDecodeErrorBehaviorLabel.Location = new System.Drawing.Point(24, 8);
            this._charDecodeErrorBehaviorLabel.Name = "_charDecodeErrorBehaviorLabel";
            this._charDecodeErrorBehaviorLabel.Size = new System.Drawing.Size(160, 23);
            this._charDecodeErrorBehaviorLabel.TabIndex = 0;
            this._charDecodeErrorBehaviorLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _charDecodeErrorBehaviorBox
            // 
            this._charDecodeErrorBehaviorBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._charDecodeErrorBehaviorBox.Location = new System.Drawing.Point(232, 8);
            this._charDecodeErrorBehaviorBox.Name = "_charDecodeErrorBehaviorBox";
            this._charDecodeErrorBehaviorBox.Size = new System.Drawing.Size(152, 20);
            this._charDecodeErrorBehaviorBox.TabIndex = 1;
            // 
            // _bufferSizeLabel
            // 
            this._bufferSizeLabel.Location = new System.Drawing.Point(24, 32);
            this._bufferSizeLabel.Name = "_bufferSizeLabel";
            this._bufferSizeLabel.Size = new System.Drawing.Size(96, 23);
            this._bufferSizeLabel.TabIndex = 2;
            this._bufferSizeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _bufferSize
            // 
            this._bufferSize.Location = new System.Drawing.Point(232, 32);
            this._bufferSize.MaxLength = 5;
            this._bufferSize.Name = "_bufferSize";
            this._bufferSize.Size = new System.Drawing.Size(72, 19);
            this._bufferSize.TabIndex = 3;
            // 
            // _additionalWordElementLabel
            // 
            this._additionalWordElementLabel.Location = new System.Drawing.Point(24, 56);
            this._additionalWordElementLabel.Name = "_additionalWordElementLabel";
            this._additionalWordElementLabel.Size = new System.Drawing.Size(200, 23);
            this._additionalWordElementLabel.TabIndex = 4;
            this._additionalWordElementLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _additionalWordElementBox
            // 
            this._additionalWordElementBox.Location = new System.Drawing.Point(232, 56);
            this._additionalWordElementBox.Name = "_additionalWordElementBox";
            this._additionalWordElementBox.Size = new System.Drawing.Size(144, 19);
            this._additionalWordElementBox.TabIndex = 5;
            // 
            // _disconnectNotificationLabel
            // 
            this._disconnectNotificationLabel.Location = new System.Drawing.Point(24, 80);
            this._disconnectNotificationLabel.Name = "_disconnectNotificationLabel";
            this._disconnectNotificationLabel.Size = new System.Drawing.Size(160, 23);
            this._disconnectNotificationLabel.TabIndex = 6;
            this._disconnectNotificationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _disconnectNotification
            // 
            this._disconnectNotification.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._disconnectNotification.Location = new System.Drawing.Point(232, 80);
            this._disconnectNotification.Name = "_disconnectNotification";
            this._disconnectNotification.Size = new System.Drawing.Size(152, 20);
            this._disconnectNotification.TabIndex = 7;
            // 
            // _closeOnDisconnect
            // 
            this._closeOnDisconnect.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._closeOnDisconnect.Location = new System.Drawing.Point(24, 104);
            this._closeOnDisconnect.Name = "_closeOnDisconnect";
            this._closeOnDisconnect.Size = new System.Drawing.Size(192, 20);
            this._closeOnDisconnect.TabIndex = 8;
            // 
            // _beepOnBellChar
            // 
            this._beepOnBellChar.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._beepOnBellChar.Location = new System.Drawing.Point(24, 128);
            this._beepOnBellChar.Name = "_beepOnBellChar";
            this._beepOnBellChar.Size = new System.Drawing.Size(288, 20);
            this._beepOnBellChar.TabIndex = 9;
            // 
            // _allowsScrollInAppMode
            // 
            this._allowsScrollInAppMode.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._allowsScrollInAppMode.Location = new System.Drawing.Point(24, 152);
            this._allowsScrollInAppMode.Name = "_allowsScrollInAppMode";
            this._allowsScrollInAppMode.Size = new System.Drawing.Size(288, 20);
            this._allowsScrollInAppMode.TabIndex = 11;
            // 
            // _keepAliveCheck
            // 
            this._keepAliveCheck.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._keepAliveCheck.Location = new System.Drawing.Point(24, 176);
            this._keepAliveCheck.Name = "_keepAliveCheck";
            this._keepAliveCheck.Size = new System.Drawing.Size(244, 20);
            this._keepAliveCheck.TabIndex = 12;
            this._keepAliveCheck.CheckedChanged += new System.EventHandler(this.OnKeepAliveCheckChanged);
            // 
            // _keepAliveIntervalBox
            // 
            this._keepAliveIntervalBox.Location = new System.Drawing.Point(276, 176);
            this._keepAliveIntervalBox.MaxLength = 2;
            this._keepAliveIntervalBox.Name = "_keepAliveIntervalBox";
            this._keepAliveIntervalBox.Size = new System.Drawing.Size(40, 19);
            this._keepAliveIntervalBox.TabIndex = 13;
            this._keepAliveIntervalBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // _keepAliveLabel
            // 
            this._keepAliveLabel.Location = new System.Drawing.Point(316, 176);
            this._keepAliveLabel.Name = "_keepAliveLabel";
            this._keepAliveLabel.Size = new System.Drawing.Size(50, 20);
            this._keepAliveLabel.TabIndex = 14;
            this._keepAliveLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _defaultLogGroup
            // 
            this._defaultLogGroup.Controls.Add(this._defaultLogTypeLabel);
            this._defaultLogGroup.Controls.Add(this._defaultLogTypeBox);
            this._defaultLogGroup.Controls.Add(this._defaultLogDirectoryLabel);
            this._defaultLogGroup.Controls.Add(this._defaultLogDirectory);
            this._defaultLogGroup.Controls.Add(this._dirSelect);
            this._defaultLogGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._defaultLogGroup.Location = new System.Drawing.Point(16, 204);
            this._defaultLogGroup.Name = "_defaultLogGroup";
            this._defaultLogGroup.Size = new System.Drawing.Size(392, 76);
            this._defaultLogGroup.TabIndex = 16;
            this._defaultLogGroup.TabStop = false;
            // 
            // _defaultLogTypeLabel
            // 
            this._defaultLogTypeLabel.Location = new System.Drawing.Point(8, 20);
            this._defaultLogTypeLabel.Name = "_defaultLogTypeLabel";
            this._defaultLogTypeLabel.Size = new System.Drawing.Size(96, 23);
            this._defaultLogTypeLabel.TabIndex = 16;
            this._defaultLogTypeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _defaultLogTypeBox
            // 
            this._defaultLogTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._defaultLogTypeBox.Location = new System.Drawing.Point(128, 20);
            this._defaultLogTypeBox.Name = "_defaultLogTypeBox";
            this._defaultLogTypeBox.Size = new System.Drawing.Size(176, 20);
            this._defaultLogTypeBox.TabIndex = 17;
            // 
            // _defaultLogDirectoryLabel
            // 
            this._defaultLogDirectoryLabel.Location = new System.Drawing.Point(8, 48);
            this._defaultLogDirectoryLabel.Name = "_defaultLogDirectoryLabel";
            this._defaultLogDirectoryLabel.Size = new System.Drawing.Size(112, 23);
            this._defaultLogDirectoryLabel.TabIndex = 18;
            this._defaultLogDirectoryLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _defaultLogDirectory
            // 
            this._defaultLogDirectory.Location = new System.Drawing.Point(128, 48);
            this._defaultLogDirectory.Name = "_defaultLogDirectory";
            this._defaultLogDirectory.Size = new System.Drawing.Size(176, 19);
            this._defaultLogDirectory.TabIndex = 19;
            // 
            // _dirSelect
            // 
            this._dirSelect.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._dirSelect.Location = new System.Drawing.Point(312, 48);
            this._dirSelect.Name = "_dirSelect";
            this._dirSelect.Size = new System.Drawing.Size(19, 19);
            this._dirSelect.TabIndex = 20;
            this._dirSelect.Text = "...";
            this._dirSelect.Click += new System.EventHandler(this.OnSelectLogDirectory);
            // 
            // _autoLogCheckBox
            // 
            this._autoLogCheckBox.Checked = true;
            this._autoLogCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this._autoLogCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._autoLogCheckBox.Location = new System.Drawing.Point(24, 200);
            this._autoLogCheckBox.Name = "_autoLogCheckBox";
            this._autoLogCheckBox.Size = new System.Drawing.Size(200, 24);
            this._autoLogCheckBox.TabIndex = 15;
            this._autoLogCheckBox.CheckedChanged += new System.EventHandler(this.OnAutoLogCheckBoxClick);
            // 
            // _shellSupportGroup
            // 
            this._shellSupportGroup.Controls.Add(this._enableComplementForNewConnections);
            this._shellSupportGroup.Controls.Add(this._commandPopupAlwaysOnTop);
            this._shellSupportGroup.Controls.Add(this._commandPopupInTaskBar);
            this._shellSupportGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._shellSupportGroup.Location = new System.Drawing.Point(16, 284);
            this._shellSupportGroup.Name = "_shellSupportGroup";
            this._shellSupportGroup.Size = new System.Drawing.Size(392, 86);
            this._shellSupportGroup.TabIndex = 17;
            this._shellSupportGroup.TabStop = false;
            // 
            // _enableComplementForNewConnections
            // 
            this._enableComplementForNewConnections.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._enableComplementForNewConnections.Location = new System.Drawing.Point(8, 12);
            this._enableComplementForNewConnections.Name = "_enableComplementForNewConnections";
            this._enableComplementForNewConnections.Size = new System.Drawing.Size(375, 24);
            this._enableComplementForNewConnections.TabIndex = 21;
            // 
            // _commandPopupAlwaysOnTop
            // 
            this._commandPopupAlwaysOnTop.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._commandPopupAlwaysOnTop.Location = new System.Drawing.Point(8, 34);
            this._commandPopupAlwaysOnTop.Name = "_commandPopupAlwaysOnTop";
            this._commandPopupAlwaysOnTop.Size = new System.Drawing.Size(375, 24);
            this._commandPopupAlwaysOnTop.TabIndex = 22;
            // 
            // _commandPopupInTaskBar
            // 
            this._commandPopupInTaskBar.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._commandPopupInTaskBar.Location = new System.Drawing.Point(8, 56);
            this._commandPopupInTaskBar.Name = "_commandPopupInTaskBar";
            this._commandPopupInTaskBar.Size = new System.Drawing.Size(375, 24);
            this._commandPopupInTaskBar.TabIndex = 23;
            // 
            // _copyAndPasteGroup
            // 
            this._copyAndPasteGroup.Controls.Add(this._highlightKeyword);
            this._copyAndPasteGroup.Controls.Add(this._highlightKeywordLabel);
            this._copyAndPasteGroup.Controls.Add(this._pasteAfterSpecifiedTimeBox);
            this._copyAndPasteGroup.Controls.Add(this._enableChangeDialogSize);
            this._copyAndPasteGroup.Controls.Add(this._showConfirmedCheckBox);
            this._copyAndPasteGroup.Controls.Add(this._pasteAfterSpecifiedTime);
            this._copyAndPasteGroup.Controls.Add(this._enablePasteConfirmAlways);
            this._copyAndPasteGroup.Controls.Add(this._enablePasteConfirm);
            this._copyAndPasteGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._copyAndPasteGroup.Location = new System.Drawing.Point(16, 374);
            this._copyAndPasteGroup.Name = "_copyAndPasteGroup";
            this._copyAndPasteGroup.Size = new System.Drawing.Size(392, 150);
            this._copyAndPasteGroup.TabIndex = 18;
            this._copyAndPasteGroup.TabStop = false;
            // 
            // _highlightKeyword
            // 
            this._highlightKeyword.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._highlightKeyword.Location = new System.Drawing.Point(163, 36);
            this._highlightKeyword.MaxLength = 0;
            this._highlightKeyword.Name = "_highlightKeyword";
            this._highlightKeyword.Size = new System.Drawing.Size(220, 19);
            this._highlightKeyword.TabIndex = 7;
            // 
            // _highlightKeywordLabel
            // 
            this._highlightKeywordLabel.Location = new System.Drawing.Point(22, 35);
            this._highlightKeywordLabel.Name = "_highlightKeywordLabel";
            this._highlightKeywordLabel.Size = new System.Drawing.Size(140, 23);
            this._highlightKeywordLabel.TabIndex = 15;
            this._highlightKeywordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _pasteAfterSpecifiedTimeBox
            // 
            this._pasteAfterSpecifiedTimeBox.Location = new System.Drawing.Point(343, 103);
            this._pasteAfterSpecifiedTimeBox.MaxLength = 2;
            this._pasteAfterSpecifiedTimeBox.Name = "_pasteAfterSpecifiedTimeBox";
            this._pasteAfterSpecifiedTimeBox.Size = new System.Drawing.Size(40, 19);
            this._pasteAfterSpecifiedTimeBox.TabIndex = 5;
            this._pasteAfterSpecifiedTimeBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // _enableChangeDialogSize
            // 
            this._enableChangeDialogSize.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._enableChangeDialogSize.Location = new System.Drawing.Point(8, 122);
            this._enableChangeDialogSize.Name = "_enableChangeDialogSize";
            this._enableChangeDialogSize.Size = new System.Drawing.Size(375, 24);
            this._enableChangeDialogSize.TabIndex = 6;
            this._enableChangeDialogSize.UseVisualStyleBackColor = true;
            // 
            // _showConfirmedCheckBox
            // 
            this._showConfirmedCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._showConfirmedCheckBox.Location = new System.Drawing.Point(8, 78);
            this._showConfirmedCheckBox.Name = "_showConfirmedCheckBox";
            this._showConfirmedCheckBox.Size = new System.Drawing.Size(375, 24);
            this._showConfirmedCheckBox.TabIndex = 3;
            this._showConfirmedCheckBox.UseVisualStyleBackColor = true;
            // 
            // _pasteAfterSpecifiedTime
            // 
            this._pasteAfterSpecifiedTime.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._pasteAfterSpecifiedTime.Location = new System.Drawing.Point(8, 100);
            this._pasteAfterSpecifiedTime.Name = "_pasteAfterSpecifiedTime";
            this._pasteAfterSpecifiedTime.Size = new System.Drawing.Size(323, 24);
            this._pasteAfterSpecifiedTime.TabIndex = 4;
            this._pasteAfterSpecifiedTime.UseVisualStyleBackColor = true;
            this._pasteAfterSpecifiedTime.CheckedChanged += new System.EventHandler(this._pasteAfterSpecifiedTime_CheckedChanged);
            // 
            // _enablePasteConfirmAlways
            // 
            this._enablePasteConfirmAlways.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._enablePasteConfirmAlways.Location = new System.Drawing.Point(8, 56);
            this._enablePasteConfirmAlways.Name = "_enablePasteConfirmAlways";
            this._enablePasteConfirmAlways.Size = new System.Drawing.Size(375, 24);
            this._enablePasteConfirmAlways.TabIndex = 2;
            this._enablePasteConfirmAlways.UseVisualStyleBackColor = true;
            // 
            // _enablePasteConfirm
            // 
            this._enablePasteConfirm.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._enablePasteConfirm.Location = new System.Drawing.Point(8, 12);
            this._enablePasteConfirm.Name = "_enablePasteConfirm";
            this._enablePasteConfirm.Size = new System.Drawing.Size(375, 24);
            this._enablePasteConfirm.TabIndex = 1;
            this._enablePasteConfirm.UseVisualStyleBackColor = true;
            this._enablePasteConfirm.CheckedChanged += new System.EventHandler(this._enablePasteConfirm_CheckedChanged);
            // 
            // TerminalOptionPanel
            // 
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this._copyAndPasteGroup);
            this.Controls.Add(this._charDecodeErrorBehaviorLabel);
            this.Controls.Add(this._charDecodeErrorBehaviorBox);
            this.Controls.Add(this._bufferSizeLabel);
            this.Controls.Add(this._bufferSize);
            this.Controls.Add(this._additionalWordElementLabel);
            this.Controls.Add(this._additionalWordElementBox);
            this.Controls.Add(this._disconnectNotificationLabel);
            this.Controls.Add(this._disconnectNotification);
            this.Controls.Add(this._closeOnDisconnect);
            this.Controls.Add(this._beepOnBellChar);
            this.Controls.Add(this._allowsScrollInAppMode);
            this.Controls.Add(this._autoLogCheckBox);
            this.Controls.Add(this._keepAliveCheck);
            this.Controls.Add(this._keepAliveIntervalBox);
            this.Controls.Add(this._keepAliveLabel);
            this.Controls.Add(this._defaultLogGroup);
            this.Controls.Add(this._shellSupportGroup);
            this.Name = "TerminalOptionPanel";
            this.Size = new System.Drawing.Size(426, 532);
            this._defaultLogGroup.ResumeLayout(false);
            this._defaultLogGroup.PerformLayout();
            this._shellSupportGroup.ResumeLayout(false);
            this._copyAndPasteGroup.ResumeLayout(false);
            this._copyAndPasteGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._charDecodeErrorBehaviorLabel.Text = sr.GetString("Form.OptionDialog._charDecodeErrorBehaviorLabel");
            this._bufferSizeLabel.Text = sr.GetString("Form.OptionDialog._bufferSizeLabel");
            this._disconnectNotificationLabel.Text = sr.GetString("Form.OptionDialog._disconnectNotificationLabel");
            this._closeOnDisconnect.Text = sr.GetString("Form.OptionDialog._closeOnDisconnect");
            this._beepOnBellChar.Text = sr.GetString("Form.OptionDialog._beepOnBellChar");
            this._allowsScrollInAppMode.Text = sr.GetString("Form.OptionDialog._allowsScrollInAppMode");
            this._keepAliveCheck.Text = sr.GetString("Form.OptionDialog._keepAliveCheck");
            this._keepAliveLabel.Text = sr.GetString("Form.OptionDialog._keepAliveLabel");
            this._defaultLogTypeLabel.Text = sr.GetString("Form.OptionDialog._defaultLogTypeLabel");
            this._defaultLogDirectoryLabel.Text = sr.GetString("Form.OptionDialog._defaultLogDirectoryLabel");
            this._autoLogCheckBox.Text = sr.GetString("Form.OptionDialog._autoLogCheckBox");
            this._additionalWordElementLabel.Text = sr.GetString("Form.OptionDialog._additionalWordElementLabel");
            this._shellSupportGroup.Text = sr.GetString("Form.OptionDialog._shellSupportGroup");
            this._copyAndPasteGroup.Text = sr.GetString("Form.OptionDialog._copyAndPasteGroup");
            this._enableComplementForNewConnections.Text = sr.GetString("Form.OptionDialog._enableComplementForNewConnections");
            this._commandPopupAlwaysOnTop.Text = sr.GetString("Form.OptionDialog._commandPopupAlwaysOnTop");
            this._commandPopupInTaskBar.Text = sr.GetString("Form.OptionDialog._commandPopupInTaskBar");
            this._enablePasteConfirm.Text = sr.GetString("Form.OptionDialog._enablePasteConfirm");
            this._enablePasteConfirmAlways.Text = sr.GetString("Form.OptionDialog._enablePasteConfirmAlways");
            this._showConfirmedCheckBox.Text = sr.GetString("Form.OptionDialog._showConfirmedCheckBox");
            this._pasteAfterSpecifiedTime.Text = sr.GetString("Form.OptionDialog._pasteAfterSpecifiedTime");
            this._enableChangeDialogSize.Text = sr.GetString("Form.OptionDialog._enableChangeDialogSize");
            this._highlightKeywordLabel.Text = sr.GetString("Form.OptionDialog._highlightKeywordLabel");

            _charDecodeErrorBehaviorBox.Items.AddRange(EnumListItem<WarningOption>.GetListItems());
            _disconnectNotification.Items.AddRange(EnumListItem<WarningOption>.GetListItems());
            _defaultLogTypeBox.Items.AddRange(EnumListItem<LogType>.GetListItemsExcept(LogType.None));
        }
        public void InitUI(ITerminalEmulatorOptions options) {
            _bufferSize.Text = options.TerminalBufferSize.ToString();
            _closeOnDisconnect.Checked = options.CloseOnDisconnect;
            _disconnectNotification.SelectedItem = options.DisconnectNotification;      // select EnumListItem<T> by T
            _beepOnBellChar.Checked = options.BeepOnBellChar;
            _charDecodeErrorBehaviorBox.SelectedItem = options.CharDecodeErrorBehavior; // select EnumListItem<T> by T
            _allowsScrollInAppMode.Checked = options.AllowsScrollInAppMode;
            _keepAliveCheck.Checked = options.KeepAliveInterval != 0;
            _keepAliveIntervalBox.Enabled = _keepAliveCheck.Checked;
            _keepAliveIntervalBox.Text = (options.KeepAliveInterval / 60000).ToString();
            _autoLogCheckBox.Checked = options.DefaultLogType != LogType.None;
            _defaultLogTypeBox.SelectedItem = options.DefaultLogType;                   // select EnumListItem<T> by T
            _defaultLogDirectory.Text = options.DefaultLogDirectory;
            _additionalWordElementBox.Text = options.AdditionalWordElement;
            _enableComplementForNewConnections.Checked = options.EnableComplementForNewConnections;
            _commandPopupAlwaysOnTop.Checked = options.CommandPopupAlwaysOnTop;
            _commandPopupInTaskBar.Checked = options.CommandPopupInTaskBar;
            _enablePasteConfirm.Checked = options.EnablePasteConfirm;
            _enablePasteConfirmAlways.Checked = options.EnablePasteConfirmAlways;
            _showConfirmedCheckBox.Checked = options.ShowConfirmedCheckBox;
            _pasteAfterSpecifiedTime.Checked = options.PasteAfterSpecifiedTimeValue != 0;
            _pasteAfterSpecifiedTimeBox.Text = options.PasteAfterSpecifiedTimeValue.ToString();
            _enableChangeDialogSize.Checked = options.EnableChangeDialogSize;
            _highlightKeyword.Text = options.HighlightKeyword;
        }
        public bool Commit(ITerminalEmulatorOptions options) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            bool successful = false;
            string itemname = null;
            try {
                options.CloseOnDisconnect = _closeOnDisconnect.Checked;
                options.BeepOnBellChar = _beepOnBellChar.Checked;
                options.AllowsScrollInAppMode = _allowsScrollInAppMode.Checked;
                itemname = sr.GetString("Caption.OptionDialog.BufferLineCount");
                options.TerminalBufferSize = Int32.Parse(_bufferSize.Text);
                itemname = sr.GetString("Caption.OptionDialog.MRUCount");
                options.CharDecodeErrorBehavior = ((EnumListItem<WarningOption>)_charDecodeErrorBehaviorBox.SelectedItem).Value;
                options.DisconnectNotification = ((EnumListItem<WarningOption>)_disconnectNotification.SelectedItem).Value;
                if (_keepAliveCheck.Checked) {
                    itemname = sr.GetString("Caption.OptionDialog.KeepAliveInterval");
                    options.KeepAliveInterval = Int32.Parse(_keepAliveIntervalBox.Text) * 60000;
                }
                else
                    options.KeepAliveInterval = 0;

                foreach (char ch in _additionalWordElementBox.Text) {
                    if (ch >= 0x100) {
                        GUtil.Warning(this, sr.GetString("Message.OptionDialog.InvalidAdditionalWordElement"));
                        return false;
                    }
                }
                options.AdditionalWordElement = _additionalWordElementBox.Text;


                if (_autoLogCheckBox.Checked) {
                    if (_defaultLogDirectory.Text.Length == 0) {
                        GUtil.Warning(this, sr.GetString("Message.OptionDialog.EmptyLogDirectory"));
                        return false;
                    }
                    options.DefaultLogType = ((EnumListItem<LogType>)_defaultLogTypeBox.SelectedItem).Value;
                    if (!Directory.Exists(_defaultLogDirectory.Text)) {
                        if (GUtil.AskUserYesNo(this, String.Format(sr.GetString("Message.OptionDialog.AskCreateDirectory"), _defaultLogDirectory.Text)) == DialogResult.Yes) {
                            try {
                                System.IO.Directory.CreateDirectory(_defaultLogDirectory.Text);
                            }
                            catch (Exception) {
                                GUtil.Warning(this, String.Format(sr.GetString("Message.OptionDialog.FailedCreateDirectory")));
                            }
                        }
                        else
                            return false;
                    }
                    options.DefaultLogDirectory = _defaultLogDirectory.Text;
                }
                else
                    options.DefaultLogType = LogType.None;


                options.EnableComplementForNewConnections = _enableComplementForNewConnections.Checked;
                options.CommandPopupAlwaysOnTop = _commandPopupAlwaysOnTop.Checked;
                options.CommandPopupInTaskBar = _commandPopupInTaskBar.Checked;

                options.EnablePasteConfirm = _enablePasteConfirm.Checked;
                options.HighlightKeyword = _highlightKeyword.Text;
                options.EnablePasteConfirmAlways = _enablePasteConfirmAlways.Checked;
                options.ShowConfirmedCheckBox = _showConfirmedCheckBox.Checked;
                options.PasteAfterSpecifiedTime = _pasteAfterSpecifiedTime.Checked;
                options.EnableChangeDialogSize = _enableChangeDialogSize.Checked;

                itemname = sr.GetString("Caption.OptionDialog.RegExpPettern");
                Regex RegExp = new Regex(options.HighlightKeyword, RegexOptions.Multiline);
                
                itemname = sr.GetString("Caption.OptionDialog.PasteAfterSpecifiedTime");
                if ((_pasteAfterSpecifiedTime.Checked) && (int.Parse(_pasteAfterSpecifiedTimeBox.Text) > 0)) {
                    options.PasteAfterSpecifiedTimeValue = Int32.Parse(_pasteAfterSpecifiedTimeBox.Text);
                } else {
                    options.PasteAfterSpecifiedTimeValue = 0;
                    options.PasteAfterSpecifiedTime = false;
                }

                successful = true;
            }
            catch (InvalidOptionException ex) {
                GUtil.Warning(this, ex.Message);
            }
            catch (Exception) {
                GUtil.Warning(this, String.Format(sr.GetString("Message.OptionDialog.InvalidItem"), itemname));
            }
            return successful;
        }

        private void OnSelectLogDirectory(object sender, EventArgs e) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.ShowNewFolderButton = true;
            dlg.Description = sr.GetString("Caption.OptionDialog.DefaultLogDirectory");
            if (_defaultLogDirectory.Text.Length > 0 && Directory.Exists(_defaultLogDirectory.Text))
                dlg.SelectedPath = _defaultLogDirectory.Text;
            if (dlg.ShowDialog(this) == DialogResult.OK)
                _defaultLogDirectory.Text = dlg.SelectedPath;
        }
        private void OnAutoLogCheckBoxClick(object sender, EventArgs args) {
            bool e = _autoLogCheckBox.Checked;
            _defaultLogTypeBox.Enabled = e;
            if (_defaultLogTypeBox.SelectedIndex == -1)
                _defaultLogTypeBox.SelectedIndex = 0;
            _defaultLogDirectory.Enabled = e;
            _dirSelect.Enabled = e;
        }
        private void OnKeepAliveCheckChanged(object sender, EventArgs args) {
            _keepAliveIntervalBox.Enabled = _keepAliveCheck.Checked;
        }
        private void _enablePasteConfirm_CheckedChanged(object sender, EventArgs e) {
            bool Flg = _enablePasteConfirm.Checked;
            _enablePasteConfirmAlways.Enabled = Flg;
            _showConfirmedCheckBox.Enabled = Flg;
            _pasteAfterSpecifiedTime.Enabled = Flg;
            _pasteAfterSpecifiedTimeBox.Enabled = (_pasteAfterSpecifiedTime.Enabled && _pasteAfterSpecifiedTime.Checked);
            _enableChangeDialogSize.Enabled = Flg;
            _highlightKeyword.Enabled = Flg;
        }
        private void _pasteAfterSpecifiedTime_CheckedChanged(object sender, EventArgs e) {
            bool Flg = _pasteAfterSpecifiedTime.Checked;
            _pasteAfterSpecifiedTimeBox.Enabled = Flg;
        }
    }


    internal class TerminalOptionPanelExtension : OptionPanelExtensionBase {
        private TerminalOptionPanel _innerPanel;
        private Control _panel;

        public TerminalOptionPanelExtension()
            : base("Form.OptionDialog._terminalPanel", 1) {
        }


        public override string[] PreferenceFolderIDsToEdit {
            get {
                return new string[] { "org.poderosa.terminalemulator" };
            }
        }
        public override Control ContentPanel {
            get {
                return _panel;
            }
        }

        public override void InitiUI(IPreferenceFolder[] values) {
            if (_innerPanel == null)
                _innerPanel = new TerminalOptionPanel();
            if (_panel == null)
                _panel = CreateScrollablePanel(_innerPanel);
            _innerPanel.InitUI((ITerminalEmulatorOptions)values[0].QueryAdapter(typeof(ITerminalEmulatorOptions)));
        }

        public override bool Commit(IPreferenceFolder[] values) {
            Debug.Assert(_innerPanel != null);
            return _innerPanel.Commit((ITerminalEmulatorOptions)values[0].QueryAdapter(typeof(ITerminalEmulatorOptions)));
        }

        public override void Dispose() {
            if (_panel != null) {
                if (_panel.Container == null)
                    _panel.Dispose();
                _panel = null;
                _innerPanel = null;
            }
        }
    }
}
