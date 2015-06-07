/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SCPForm.cs,v 1.4 2012/05/05 12:56:27 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Granados.Poderosa.SCP;
using Granados.Poderosa.FileTransfer;

namespace Poderosa.SFTP {

    /// <summary>
    /// SCP interface
    /// </summary>
    public partial class SCPForm : Form {

        #region Private fields

        private Form _ownerForm;
        private SCPClient _scp;

        private bool _scpExecuting = false;
        private Thread _scpThread = null;
        private bool _closedByOwner = false;
        private bool _formClosed = false;
        private string _saveFolderPath = null;

        private Cancellation _fileTransferCancellation = null;

        #endregion

        #region Private constants

        private const int PROGRESSBAR_MAX = Int32.MaxValue;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public SCPForm()
            : this(null, null, String.Empty) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ownerForm">Owner form</param>
        /// <param name="scp">SCP client</param>
        /// <param name="connectionName">Connection name</param>
        public SCPForm(Form ownerForm, SCPClient scp, string connectionName) {
            InitializeComponent();

            if (!this.DesignMode) {
                this._scp = scp;
                this._ownerForm = ownerForm;
                this.Text = "SCP - " + connectionName;
                this.progressBar.Maximum = PROGRESSBAR_MAX;
                this.checkRecursive.Checked = true;

                SetIcon();
                SetText();

                ChangeExecutingState(false);
                ClearProgressBar();
            }
        }

        private void SetIcon() {
            this.Icon = Properties.Resources.FormIconSCP;
        }

        private void SetText() {
            StringResource res = SFTPPlugin.Instance.StringResource;
            this.labelRemotePath.Text = res.GetString("SCPForm.labelRemotePath");
            this.checkPreserveTime.Text = res.GetString("SCPForm.checkPreserveTime");
            this.checkRecursive.Text = res.GetString("SCPForm.checkRecursive");
            this.labelDropHere.Text = res.GetString("SCPForm.labelDropHere");
            this.buttonDownload.Text = res.GetString("SCPForm.buttonDownload");
            this.buttonCancel.Text = res.GetString("SCPForm.buttonCancel");
        }

        #endregion

        #region Upload

        private void StartUpload(string[] localFiles) {
            Debug.Assert(localFiles != null);

            if (_scp == null || _scpExecuting)
                return;

            Debug.Assert(_scpThread == null || !_scpThread.IsAlive);

            string remotePath;
            if (!TryGetRemotePath(out remotePath))
                return;

            bool recursive = this.checkRecursive.Checked;
            bool preseveTime = this.checkPreserveTime.Checked;

            _fileTransferCancellation = new Cancellation();

            ChangeExecutingState(true);

            _scpThread = new Thread((ThreadStart)
                delegate() {
                    UploadThread(localFiles, remotePath, recursive, preseveTime);

                    ChangeExecutingState(false);
                    _fileTransferCancellation = null;
                });
            _scpThread.Start();
        }

        private void UploadThread(string[] localFiles, string remotePath, bool recursive, bool preseveTime) {
            Debug.Assert(_scp != null);
            Debug.Assert(localFiles != null);
            Debug.Assert(remotePath != null);
            Debug.Assert(remotePath.Length > 0);

            ClearLog();
            ClearProgressBar();

            bool assumeRemoteIsDir;
            if (localFiles.Length >= 2)
                assumeRemoteIsDir = true;
            else
                assumeRemoteIsDir = false;

            Log("=== UPLOAD ===");
            Log("Remote: " + remotePath);

            try {
                foreach (string localPath in localFiles) {
                    string scpLocalPath = Path.GetFullPath(localPath);

                    string scpRemotePath;
                    if (assumeRemoteIsDir && !recursive)
                        scpRemotePath = CombineUnixPath(remotePath, Path.GetFileName(scpLocalPath));
                    else
                        scpRemotePath = remotePath;

                    string basePath;
                    if (recursive)
                        basePath = Path.GetDirectoryName(scpLocalPath);
                    else
                        basePath = null;

                    Log((recursive ? "Uplaod (recursive): " : "Uplaod: ") + scpLocalPath);
                    _scp.Upload(scpLocalPath, scpRemotePath, recursive, preseveTime, _fileTransferCancellation,
                        delegate(string localFullPath, string fileName, SCPFileTransferStatus status, ulong fileSize, ulong transmitted) {
                            ShowProgress(basePath, localFullPath, fileName, status, fileSize, transmitted);
                        });
                }

                Log("Completed.");
            }
            catch (Exception e) {
                RuntimeUtil.SilentReportException(e);
                if (e is SCPClientException)
                    Log("*** FAILED: " + e.Message);
                else
                    Log("*** ERROR: " + e.Message);
            }

            ClearProgressBar();
        }

        #endregion

        #region Download

        private void StartDownload(string localDirectory) {
            Debug.Assert(localDirectory != null);

            if (_scp == null || _scpExecuting)
                return;

            Debug.Assert(_scpThread == null || !_scpThread.IsAlive);

            string remotePath;
            if (!TryGetRemotePath(out remotePath))
                return;

            bool recursive = this.checkRecursive.Checked;
            bool preseveTime = this.checkPreserveTime.Checked;

            _fileTransferCancellation = new Cancellation();

            ChangeExecutingState(true);

            _scpThread = new Thread((ThreadStart)
                delegate() {
                    DownloadThread(localDirectory, remotePath, recursive, preseveTime);

                    ChangeExecutingState(false);
                    _fileTransferCancellation = null;
                });
            _scpThread.Start();
        }

        private void DownloadThread(string localDirectory, string remotePath, bool recursive, bool preseveTime) {
            Debug.Assert(_scp != null);
            Debug.Assert(localDirectory != null);
            Debug.Assert(remotePath != null);
            Debug.Assert(remotePath.Length > 0);

            ClearLog();
            ClearProgressBar();

            Log("=== DOWNLOAD ===");
            Log("Remote: " + remotePath);

            try {
                string scpLocalPath = Path.GetFullPath(localDirectory);
                string scpRemotePath = remotePath;

                string basePath;
                if (recursive)
                    basePath = scpLocalPath;
                else
                    basePath = null;

                Log((recursive ? "Download to (recursive): " : "Download to: ") + scpLocalPath);
                _scp.Download(scpRemotePath, scpLocalPath, recursive, preseveTime, _fileTransferCancellation,
                    delegate(string localFullPath, string fileName, SCPFileTransferStatus status, ulong fileSize, ulong transmitted) {
                        ShowProgress(basePath, localFullPath, fileName, status, fileSize, transmitted);
                    });

                Log("Completed.");
            }
            catch (Exception e) {
                RuntimeUtil.SilentReportException(e);
                if (e is SCPClientException)
                    Log("*** FAILED: " + e.Message);
                else
                    Log("*** ERROR: " + e.Message);
            }

            ClearProgressBar();
        }

        #endregion

        #region Common

        private void ChangeExecutingState(bool executing) {
            if (_formClosed)
                return;

            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)
                    delegate() {
                        ChangeExecutingState(executing);
                    });
                return;
            }

            _scpExecuting = executing;

            bool settingsEnabled = executing ? false : true;
            this.textRemotePath.Enabled = settingsEnabled;
            this.checkPreserveTime.Enabled = settingsEnabled;
            this.checkRecursive.Enabled = settingsEnabled;
            this.panelDrop.BackColor = executing ? SystemColors.ControlDark : SystemColors.Control;
            this.buttonDownload.Enabled = settingsEnabled;

            this.buttonCancel.Enabled = !settingsEnabled;
        }

        private bool TryGetRemotePath(out string remotePath) {
            string path = this.textRemotePath.Text.Trim();
            if (String.IsNullOrEmpty(path)) {
                Poderosa.GUtil.Warning(
                    this,
                    SFTPPlugin.Instance.StringResource.GetString("SCPForm.NeedRemotePath"),
                    MessageBoxIcon.Information);
                remotePath = null;
                return false;
            }
            else {
                remotePath = path;
                return true;
            }
        }

        private void ShowProgress(string basePath, string localFullPath, string fileName, SCPFileTransferStatus status, ulong fileSize, ulong transmitted) {
            switch (status) {
                case SCPFileTransferStatus.CreateDirectory:
                    Log(" | Directory: " + GetProcessingPath(basePath, localFullPath, fileName));
                    Log(" | ... Creating");
                    UpdateProgressBar(fileName, fileSize, transmitted, false);
                    break;
                case SCPFileTransferStatus.DirectoryCreated:
                    LogOverwrite(" | ... Done");
                    UpdateProgressBar(fileName, fileSize, transmitted, true);
                    break;
                case SCPFileTransferStatus.Open:
                    Log(" | File: " + GetProcessingPath(basePath, localFullPath, fileName));
                    Log(" | ... Open");
                    UpdateProgressBar(fileName, fileSize, transmitted, false);
                    break;
                case SCPFileTransferStatus.Transmitting:
                    LogOverwrite(" | ... Transmitting");
                    UpdateProgressBar(fileName, fileSize, transmitted, false);
                    break;
                case SCPFileTransferStatus.CompletedSuccess:
                    LogOverwrite(" | ... Done");
                    UpdateProgressBar(fileName, fileSize, transmitted, true);
                    break;
                case SCPFileTransferStatus.CompletedAbort:
                    LogOverwrite(" | ... Aborted");
                    UpdateProgressBar(fileName, fileSize, transmitted, true);
                    break;
            }
        }

        private string GetProcessingPath(string basePath, string localFullPath, string fileName) {
            if (basePath == null)
                return fileName;
            else if (localFullPath.StartsWith(basePath))
                return localFullPath.Substring(basePath.Length + 1 /* skip next path separator */);
            else
                return localFullPath;
        }

        private string CombineUnixPath(string path1, string path2) {
            return path1.TrimEnd('/') + "/" + path2;
        }

        private bool ContainsDirectory(string[] paths) {
            foreach (string path in paths) {
                if (Directory.Exists(path))
                    return true;
            }
            return false;
        }

        #endregion

        #region Log display

        private int prevLineTop = 0;

        private void ClearLog() {
            if (_formClosed)
                return;

            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() {
                    ClearLog();
                });
                return;
            }

            this.textLog.Text = String.Empty;
        }

        private void Log(string message) {
            if (_formClosed)
                return;

            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() {
                    Log(message);
                });
                return;
            }

            this.textLog.SelectionStart = this.textLog.TextLength;

            if (this.textLog.TextLength > 0)
                this.textLog.SelectedText = Environment.NewLine;

            prevLineTop = this.textLog.TextLength;
            this.textLog.SelectedText = message;
        }

        private void LogOverwrite(string message) {
            if (_formClosed)
                return;

            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() {
                    LogOverwrite(message);
                });
                return;
            }

            this.textLog.SelectionStart = prevLineTop;
            this.textLog.SelectionLength = this.textLog.TextLength - prevLineTop;
            this.textLog.SelectedText = message;
        }

        #endregion

        #region Progress bar

        private void ClearProgressBar() {
            UpdateProgressBarCore(String.Empty, 0);
        }

        private void UpdateProgressBar(string targetFile, ulong total, ulong current, bool isCompleted) {
            int progress;
            if (total == 0) {
                progress = isCompleted ? PROGRESSBAR_MAX : 0;
            }
            else if (total <= (ulong)Int32.MaxValue && current <= (ulong)Int32.MaxValue) {
                progress = (int)((ulong)PROGRESSBAR_MAX * current / total);
            }
            else {
                progress = (int)(PROGRESSBAR_MAX * ((double)current / (double)total));
            }

            UpdateProgressBarCore(targetFile, progress);
        }

        private void UpdateProgressBarCore(string targetFile, int progress) {
            if (_formClosed)
                return;

            if (this.InvokeRequired) {
                this.Invoke((MethodInvoker)delegate() {
                    UpdateProgressBarCore(targetFile, progress);
                });
                return;
            }

            this.labelProgress.Text = targetFile;
            this.progressBar.Value = progress;
        }

        #endregion

        #region Event handlers

        private void SCPForm_Load(object sender, EventArgs e) {
            if (_ownerForm != null) {
                _ownerForm.FormClosed += new FormClosedEventHandler(_ownerForm_FormClosed);

                this.Location = new Point(
                    _ownerForm.Left + (_ownerForm.Width - this.Width) / 2,
                    _ownerForm.Top + (_ownerForm.Height - this.Height) / 2
                );
            }
        }

        private void SCPForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing && !_closedByOwner) {
                if (_scpThread != null && _scpThread.IsAlive) {
                    e.Cancel = true;
                    return;
                }
            }

            _formClosed = true;

            if (_scpThread != null && _scpThread.IsAlive)
                _scpThread.Abort(); // FIXME: we need graceful cancellation
        }

        private void SCPForm_FormClosed(object sender, FormClosedEventArgs e) {
            if (_ownerForm != null) {
                _ownerForm.FormClosed -= new FormClosedEventHandler(_ownerForm_FormClosed);
                _ownerForm = null;
                _scp = null;
            }
        }

        private void _ownerForm_FormClosed(object sender, FormClosedEventArgs e) {
            _closedByOwner = true;
            this.Close();
        }

        private void panelDrop_DragDrop(object sender, DragEventArgs e) {
            if (e.Effect == DragDropEffects.Copy) {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                    string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null) {
                        if (!checkRecursive.Checked && ContainsDirectory(files)) {
                            string message = SFTPPlugin.Instance.StringResource.GetString("SCPForm.RecursiveOptionIsRequired");
                            DialogResult result = GUtil.AskUserYesNo(this, message, MessageBoxIcon.Information);
                            if (result != DialogResult.Yes)
                                return;
                            this.checkRecursive.Checked = true;
                        }

                        StartUpload(files);
                    }
                }
            }
        }

        private void panelDrop_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)
                && (e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy) {

                e.Effect = e.AllowedEffect & DragDropEffects.Copy;
            }
            else {
                e.Effect = DragDropEffects.None;
            }
        }

        private void buttonDownload_Click(object sender, EventArgs e) {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                if (_saveFolderPath == null)
                    _saveFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.SelectedPath = _saveFolderPath;
                dialog.ShowNewFolderButton = true;
                dialog.Description = SFTPPlugin.Instance.StringResource.GetString("SCPForm.ChooseFolder");
                DialogResult result = dialog.ShowDialog(this);
                if (result != DialogResult.OK)
                    return;
                _saveFolderPath = dialog.SelectedPath;
            }

            StartDownload(_saveFolderPath);
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Cancellation cancellation = _fileTransferCancellation;
            if (cancellation != null) {
                cancellation.Cancel();
                buttonCancel.Enabled = false;
            }
        }

        #endregion

    }
}