/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OpenPipeDialog.cs,v 1.5 2012/03/15 14:55:39 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Terminal;
using Poderosa.Sessions;
using Poderosa.Util;

namespace Poderosa.Pipe {

    /// <summary>
    /// Open pipe dialog
    /// </summary>
    internal partial class OpenPipeDialog : Form {

        private PipeTerminalParameter.EnvironmentVariable[] _environmentVariables = null;

        /// <summary>
        /// Open pipe delegate
        /// </summary>
        /// <param name="param">Terminal parameter</param>
        /// <param name="settings">Terminal settings</param>
        /// <returns>Returns true if pipe was opened successfully. Otherwise returns false.</returns>
        public delegate bool OpenPipeDelegate(PipeTerminalParameter param, PipeTerminalSettings settings);

        /// <summary>
        /// Opening pipe event
        /// </summary>
        public OpenPipeDelegate OpenPipe = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public OpenPipeDialog() {
            InitializeComponent();

            InitialLayout();
            SetupControls();
        }

        /// <summary>
        /// Apply parameters to controls
        /// </summary>
        /// <param name="param">Terminal parameters</param>
        /// <param name="settings">Terminal settings</param>
        public void ApplyParams(PipeTerminalParameter param, PipeTerminalSettings settings) {
            Debug.Assert(param != null);
            Debug.Assert(settings != null);

            Control boxToFocus = null;

            bool isProcessMode = true;  // process mode is default
            bool isBidirectinal = true; // bidirectinal is default
            boxToFocus = _textBoxExePath;

            if (param.ExeFilePath != null) {
                _textBoxExePath.Text = param.ExeFilePath;
                if (param.CommandLineOptions != null)
                    _textBoxCommandLineOptions.Text = param.CommandLineOptions;
                _environmentVariables = param.EnvironmentVariables;
            }
            else if (param.InputPipePath != null) {
                isProcessMode = false;
                _textBoxInputPath.Text = param.InputPipePath;

                if (param.OutputPipePath != null) {
                    isBidirectinal = false;
                    _textBoxOutputPath.Text = param.OutputPipePath;
                }

                boxToFocus = _textBoxInputPath;
            }

            SetMode(isProcessMode);
            SetBidirectional(isBidirectinal);

            _comboBoxLogType.SelectedItem = LogType.None;   // select EnumListItem<T> by T
            _textBoxLogFile.Enabled = false;
            _buttonBrowseLogFile.Enabled = false;

            _comboBoxEncoding.SelectedItem = settings.Encoding;     // select EnumListItem<T> by T
            _comboBoxNewLine.SelectedItem = settings.TransmitNL;    // select EnumListItem<T> by T
            _comboBoxLocalEcho.SelectedIndex = settings.LocalEcho ? 1 : 0;
            _comboBoxTerminalType.SelectedItem = settings.TerminalType; // select EnumListItem<T> by T

            IAutoExecMacroParameter autoExecParams = param.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
            if (autoExecParams != null && PipePlugin.Instance.MacroEngine != null) {
                _textBoxAutoExecMacroPath.Text = (autoExecParams.AutoExecMacroPath != null) ? autoExecParams.AutoExecMacroPath : String.Empty;
            }
            else {
                _labelAutoExecMacroPath.Enabled = false;
                _textBoxAutoExecMacroPath.Enabled = false;
                _buttonSelectAutoExecMacro.Enabled = false;
            }

            if (boxToFocus != null)
                boxToFocus.Focus();
        }

        /// <summary>
        /// Change layout.
        /// Move "Process" group box and shrink the window.
        /// </summary>
        private void InitialLayout() {
            this.SuspendLayout();
            _groupBoxProcess.Width = _groupBoxPipe.Width;
            _groupBoxProcess.Height = _groupBoxPipe.Height;
            _groupBoxProcess.Location = _groupBoxPipe.Location;

            int shrinkHeight = _textBoxAutoExecMacroPath.Top - _groupTerminal.Bottom - 8/* margin */;
            this.Height -= shrinkHeight;
            this.ResumeLayout();
        }

        /// <summary>
        /// Set i18n text.
        /// </summary>
        private void SetupControls() {
            StringResource res = PipePlugin.Instance.Strings;

            this.Text = res.GetString("Form.OpenPipeDialog.Title");

            _radioButtonProcess.Text = res.GetString("Form.OpenPipeDialog._radioButtonProcess");
            _radioButtonPipe.Text = res.GetString("Form.OpenPipeDialog._radioButtonPipe");

            _groupBoxProcess.Text = res.GetString("Form.OpenPipeDialog._groupBoxProcess");
            _groupBoxPipe.Text = res.GetString("Form.OpenPipeDialog._groupBoxPipe");

            _labelExePath.Text = res.GetString("Form.OpenPipeDialog._labelExePath");
            _labelCommandLineOptions.Text = res.GetString("Form.OpenPipeDialog._labelCommandLineOptions");
            _labelInputPath.Text = res.GetString("Form.OpenPipeDialog._labelInputPath");
            _labelOutputPath.Text = res.GetString("Form.OpenPipeDialog._labelOutputPath");
            _labelLogFile.Text = res.GetString("Form.OpenPipeDialog._labelLogFile");
            _labelLogType.Text = res.GetString("Form.OpenPipeDialog._labelLogType");
            _labelEncoding.Text = res.GetString("Form.OpenPipeDialog._labelEncoding");
            _labelLocalEcho.Text = res.GetString("Form.OpenPipeDialog._labelLocalEcho");
            _labelNewLine.Text = res.GetString("Form.OpenPipeDialog._labelNewLine");
            _labelTerminalType.Text = res.GetString("Form.OpenPipeDialog._labelTerminalType");
            _labelAutoExecMacroPath.Text = res.GetString("Form.OpenPipeDialog._labelAutoExecMacroPath");

            _checkBoxBidirectinal.Text = res.GetString("Form.OpenPipeDialog._checkBoxBidirectinal");

            _buttonEnvironmentVariables.Text = res.GetString("Form.OpenPipeDialog._buttonEnvironmentVariables");

            _buttonOK.Text = res.GetString("Common.OK");
            _buttonCancel.Text = res.GetString("Common.Cancel");

            _comboBoxLogType.Items.AddRange(EnumListItem<LogType>.GetListItems());
            _comboBoxEncoding.Items.AddRange(EnumListItem<EncodingType>.GetListItems());
            _comboBoxLocalEcho.Items.AddRange(new object[] {
                res.GetString("Common.DoNot"),
                res.GetString("Common.Do")
            });
            _comboBoxNewLine.Items.AddRange(EnumListItem<NewLine>.GetListItems());
            _comboBoxTerminalType.Items.AddRange(EnumListItem<TerminalType>.GetListItems());
        }

        private void SwitchMode(bool isProcessMode) {
            if (isProcessMode) {
                _groupBoxProcess.Visible = true;
                _groupBoxPipe.Visible = false;
            }
            else {
                _groupBoxPipe.Visible = true;
                _groupBoxProcess.Visible = false;
            }
        }

        private void SetMode(bool isProcessMode) {
            _radioButtonProcess.Checked = isProcessMode;
            _radioButtonPipe.Checked = !isProcessMode;
            SwitchMode(isProcessMode);
        }

        private void SwitchBidirectional(bool isBidirectinal) {
            _labelOutputPath.Enabled = isBidirectinal ? false : true;
            _textBoxOutputPath.Enabled = isBidirectinal ? false : true;
        }

        private void SetBidirectional(bool isBidirectinal) {
            _checkBoxBidirectinal.Checked = isBidirectinal;
            SwitchBidirectional(isBidirectinal);
        }

        private bool ValidateParams(out PipeTerminalParameter param, out PipeTerminalSettings settings) {
            PipeTerminalParameter paramTmp = new PipeTerminalParameter();
            PipeTerminalSettings settingsTmp = new PipeTerminalSettings();

            StringResource res = PipePlugin.Instance.Strings;

            try {
                string caption;

                if (_radioButtonProcess.Checked) {
                    string exePath = _textBoxExePath.Text;
                    if (exePath.Length == 0)
                        throw new Exception(res.GetString("Form.OpenPipeDialog.Error.NoExePath"));
                    paramTmp.ExeFilePath = exePath;
                    paramTmp.CommandLineOptions = _textBoxCommandLineOptions.Text;
                    paramTmp.EnvironmentVariables = _environmentVariables;
                    caption = Path.GetFileName(exePath);
                }
                else if (_radioButtonPipe.Checked) {
                    string path = _textBoxInputPath.Text;
                    if (path.Length == 0)
                        throw new Exception(res.GetString("Form.OpenPipeDialog.Error.NoInputPath"));
                    paramTmp.InputPipePath = path;
                    caption = Path.GetFileName(path);

                    if (!_checkBoxBidirectinal.Checked) {
                        path = _textBoxOutputPath.Text;
                        if (path.Length == 0)
                            throw new Exception(res.GetString("Form.OpenPipeDialog.Error.NoOutputPath"));
                        paramTmp.OutputPipePath = path;
                        caption += "/" + Path.GetFileName(path);
                    }
                }
                else {
                    throw new Exception(res.GetString("Form.OpenPipeDialog.Error.NoOpenMode"));
                }

                TerminalType terminalType = ((EnumListItem<TerminalType>)_comboBoxTerminalType.SelectedItem).Value;
                paramTmp.SetTerminalName(terminalType.ToString().ToLowerInvariant());

                LogType logType = ((EnumListItem<LogType>)_comboBoxLogType.SelectedItem).Value;
                ISimpleLogSettings logSettings = null;
                if (logType != LogType.None) {
                    string logFile = _textBoxLogFile.Text;
                    LogFileCheckResult r = LogUtil.CheckLogFileName(logFile, this);
                    if (r == LogFileCheckResult.Cancel || r == LogFileCheckResult.Error)
                        throw new Exception("");

                    logSettings = PipePlugin.Instance.TerminalEmulatorService.CreateDefaultSimpleLogSettings();
                    logSettings.LogPath = logFile;
                    logSettings.LogType = logType;
                    logSettings.LogAppend = (r == LogFileCheckResult.Append);
                }

                string autoExecMacroPath = null;
                if (_textBoxAutoExecMacroPath.Text.Length != 0)
                    autoExecMacroPath = _textBoxAutoExecMacroPath.Text;

                IAutoExecMacroParameter autoExecParams = paramTmp.GetAdapter(typeof(IAutoExecMacroParameter)) as IAutoExecMacroParameter;
                if (autoExecParams != null)
                    autoExecParams.AutoExecMacroPath = autoExecMacroPath;

                settingsTmp.BeginUpdate();
                settingsTmp.Caption = caption;
                settingsTmp.Icon = Poderosa.Pipe.Properties.Resources.Icon16x16;
                settingsTmp.Encoding = ((EnumListItem<EncodingType>)_comboBoxEncoding.SelectedItem).Value;
                settingsTmp.LocalEcho = _comboBoxLocalEcho.SelectedIndex == 1;
                settingsTmp.TransmitNL = ((EnumListItem<NewLine>)_comboBoxNewLine.SelectedItem).Value;
                settingsTmp.TerminalType = terminalType;
                if (logSettings != null)
                    settingsTmp.LogSettings.Reset(logSettings);
                settingsTmp.EndUpdate();

                param = paramTmp;
                settings = settingsTmp;
                return true;
            }
            catch (Exception e) {
                if (e.Message.Length > 0)
                    GUtil.Warning(this, e.Message);
                param = null;
                settings = null;
                return false;
            }
        }

        private void _radioButtonProcess_CheckedChanged(object sender, EventArgs e) {
            if (_radioButtonProcess.Checked)
                SwitchMode(true);
        }

        private void _radioButtonPipe_CheckedChanged(object sender, EventArgs e) {
            if (_radioButtonPipe.Checked)
                SwitchMode(false);
        }

        private void _checkBoxBidirectinal_CheckedChanged(object sender, EventArgs e) {
            SwitchBidirectional(_checkBoxBidirectinal.Checked);
        }

        private void _comboBoxLogType_SelectedIndexChanged(object sender, EventArgs e) {
            bool enableLogFile = (((EnumListItem<LogType>)_comboBoxLogType.SelectedItem).Value != LogType.None);
            _textBoxLogFile.Enabled = enableLogFile;
            _buttonBrowseLogFile.Enabled = enableLogFile;
        }

        private void _buttonBrowseLogFile_Click(object sender, EventArgs e) {
            string fileName = LogUtil.SelectLogFileByDialog(this);
            if (fileName != null)
                _textBoxLogFile.Text = fileName;
        }

        private void _buttonBrowseExePath_Click(object sender, EventArgs e) {
            StringResource res = PipePlugin.Instance.Strings;

            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.AddExtension = false;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                try {
                    dialog.InitialDirectory = Path.GetDirectoryName(_textBoxExePath.Text);
                }
                catch (Exception) {
                }
                dialog.RestoreDirectory = true;
                dialog.Title = res.GetString("Form.OpenPipeDialog.SelectExeFile.Title");
                dialog.Filter = String.Format("{0}(*.exe)|*.exe|{1}(*.*)|*.*",
                                    res.GetString("Form.OpenPipeDialog.SelectExeFile.ExeFiles"),
                                    res.GetString("Form.OpenPipeDialog.SelectExeFile.AllFiles"));
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    _textBoxExePath.Text = dialog.FileName;
            }
        }

        private void _buttonSelectAutoExecMacro_Click(object sender, EventArgs e) {
            if (PipePlugin.Instance.MacroEngine != null) {
                string path = PipePlugin.Instance.MacroEngine.SelectMacro(this);
                if (path != null)
                    _textBoxAutoExecMacroPath.Text = path;
            }
        }

        private void _buttonEnvironmentVariables_Click(object sender, EventArgs e) {
            using (EnvironmentVariablesDialog dialog = new EnvironmentVariablesDialog()) {
                dialog.ApplyParams(_environmentVariables);
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    _environmentVariables = dialog.EnvironmentVariables;
                }
            }
        }

        private void _buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void _buttonOK_Click(object sender, EventArgs e) {

            PipeTerminalParameter param;
            PipeTerminalSettings settings;

            if (ValidateParams(out param, out settings)) {
                try {
                    bool succeeded = false;
                    try {
                        Cursor.Current = Cursors.WaitCursor;
                        this.Enabled = false;

                        if (OpenPipe != null)
                            succeeded = OpenPipe(param, settings);
                    }
                    finally {
                        // when succeeded, this form will be closed. no need to change form status.
                        if (!succeeded)
                            this.Enabled = true;
                        Cursor.Current = Cursors.Default;
                    }

                    if (succeeded) {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                }
                catch (Exception ex) {
                    RuntimeUtil.SilentReportException(ex);
                    GUtil.Warning(this.Parent, ex.Message);
                }
            }
        }

    }

}