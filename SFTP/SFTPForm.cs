/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SFTPForm.cs,v 1.4 2012/05/05 12:42:45 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Granados.Poderosa.SFTP;
using Granados.Poderosa.FileTransfer;
using Granados.SSH2;
using System.Threading;
using System.Collections;
using System.IO;
using System.Diagnostics;

namespace Poderosa.SFTP {

    /// <summary>
    /// SFTP interface
    /// </summary>
    public partial class SFTPForm : Form {

        #region Private fields

        private Form _ownerForm;
        private SFTPClient _sftp;
        private readonly String _remoteName;

        private Thread _sftpThread = null;
        private bool _sftpExecuting = false;
        private bool _treeConstructing = false;
        private bool _closedByOwner = false;
        private bool _formClosed = false;
        private string _saveFolderPath = null;

        // keep focus before and after controls are disabled temporarily
        private Control _prevActiveControl = null;

        private Cancellation _fileTransferCancellation = null;

        #endregion

        #region Private constants

        private const int PROGRESSBAR_MAX = Int32.MaxValue;

        private const int IMAGE_INDEX_ROOT = 0;
        private const int IMAGE_INDEX_FOLDER_CLOSE = 1;
        private const int IMAGE_INDEX_FOLDER_OPEN = 2;
        private const int IMAGE_INDEX_FILE = 3;
        private const int IMAGE_INDEX_SYMBOLICLINK = 4;

        #endregion

        #region NodeType

        private enum NodeType {
            Root,
            File,
            Directory,
            SymbolicLink,
        }

        #endregion

        #region NodeTag

        private class NodeTag {
            public readonly NodeType Type;
            public readonly string SortKey;
            public readonly SFTPFileInfo FileInfo;

            private NodeTag(NodeType nodeType, string sortKey, SFTPFileInfo fileInfo) {
                this.Type = nodeType;
                this.SortKey = sortKey;
                this.FileInfo = fileInfo;
            }

            public static NodeTag CreateForRoot() {
                return new NodeTag(NodeType.Root, String.Empty, null);
            }

            public static NodeTag CreateForDirectory(string name) {
                return new NodeTag(NodeType.Directory, "D:" + name, null);
            }

            public static NodeTag CreateForFileOrDirectory(SFTPFileInfo fileInfo) {
                NodeType nodeType;
                string prefix;
                if (UnixPermissions.IsDirectory(fileInfo.Permissions)) {
                    nodeType = NodeType.Directory;
                    prefix = "D:";
                }
                else if (UnixPermissions.IsSymbolicLink(fileInfo.Permissions)) {
                    nodeType = NodeType.SymbolicLink;
                    prefix = "F:";
                }
                else {
                    nodeType = NodeType.File;
                    prefix = "F:";
                }
                return new NodeTag(nodeType, prefix + fileInfo.FileName, fileInfo);
            }
        }

        #endregion

        #region NodeSorter

        private class NodeSorter : IComparer {
            public int Compare(object x, object y) {
                NodeTag tagX = (NodeTag)((TreeNode)x).Tag;
                NodeTag tagY = (NodeTag)((TreeNode)y).Tag;
                return String.Compare(tagX.SortKey, tagY.SortKey);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPForm()
            : this(null, null, String.Empty) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPForm(Form ownerForm, SFTPClient sftp, string connectionName) {
            InitializeComponent();

            if (!this.DesignMode) {
                this._sftp = sftp;
                this._ownerForm = ownerForm;
                this._remoteName = connectionName;
                this.Text = "SFTP - " + connectionName;
                this.progressBar.Maximum = PROGRESSBAR_MAX;

                PrepareTreeIcons();
                SetIcon();
                SetText();

                ClearProgressBar();

                // Note: Setting TreeViewNodeSorter property enables sorting.
                treeViewRemote.TreeViewNodeSorter = new NodeSorter();
            }
        }

        private void PrepareTreeIcons() {
            treeViewImageList.Images.Clear();
            treeViewImageList.ColorDepth = ColorDepth.Depth32Bit;
            treeViewImageList.Images.Add(Properties.Resources.Host16x16);
            treeViewImageList.Images.Add(Properties.Resources.FolderClose16x16);
            treeViewImageList.Images.Add(Properties.Resources.FolderOpen16x16);
            treeViewImageList.Images.Add(Properties.Resources.File16x16);
            treeViewImageList.Images.Add(Properties.Resources.Link16x16);
            treeViewRemote.ImageList = treeViewImageList;
        }

        private void SetIcon() {
            this.Icon = Properties.Resources.FormIconSFTP;
        }

        private void SetText() {
            StringResource res = SFTPPlugin.Instance.StringResource;
            this.labelDropHere.Text = res.GetString("SFTPForm.labelDropHere");
            this.buttonDownload.Text = res.GetString("SFTPForm.buttonDownload");
            this.buttonCancel.Text = res.GetString("SFTPForm.buttonCancel");
        }

        #endregion

        #region Thread control

        private bool CanExecuteSFTP() {
            return !_sftpExecuting;
        }

        private void BeginSFTPThread(MethodInvoker threadMethod, bool modifyTree, bool isCancelable) {
            _sftpExecuting = true;
            DisableControls(isCancelable ? true : false);
            Cursor.Current = Cursors.WaitCursor;
            _sftpThread = new Thread(
                delegate() {
                    try {
                        if (modifyTree)
                            _treeConstructing = true;

                        try {
                            threadMethod();
                        }
                        finally {
                            if (modifyTree)
                                _treeConstructing = false;

                            _sftpExecuting = false;

                            Invoke((MethodInvoker)delegate() {
                                EnableControls();
                                Cursor.Current = Cursors.Default;
                            });
                        }
                    }
                    catch (SFTPClientException e) {
                        Log("*** Error: " + e.Message);
                    }
                    catch (IOException e) {
                        Log("*** I/O Error: " + e.Message);
                    }
                    catch (UnauthorizedAccessException e) {
                        Log("*** Access Error: " + e.Message);
                    }
                    catch (Exception e) {
                        RuntimeUtil.ReportException(e);
                    }
                });
            _sftpThread.Start();
        }

        private void DisableControls(bool isCancelEnabled) {
            _prevActiveControl = this.ActiveControl;
            this.buttonDownload.Enabled = false;
            this.treeViewRemote.Enabled = false;
            this.buttonCancel.Enabled = isCancelEnabled;
        }

        private void EnableControls() {
            this.buttonDownload.Enabled = true;
            this.treeViewRemote.Enabled = true;
            this.ActiveControl = _prevActiveControl;
            this.buttonCancel.Enabled = false;
        }

        #endregion

        #region SFTP Initialize

        private void SFTPInitailize() {
            if (!CanExecuteSFTP())
                return;

            BeginSFTPThread(delegate() {
                SFTPInitailize_Init();

                this.Invoke((MethodInvoker)delegate() {
                    treeViewRemote.BeginUpdate();
                    treeViewRemote.Nodes.Clear();
                    TreeNode rootNode = CreateRootNode();
                    treeViewRemote.Nodes.Add(rootNode);
                    treeViewRemote.EndUpdate();
                });

                Log("Retrieving home directory...");
                string homeDir = _sftp.GetRealPath(".");
                Log("...Done: " + homeDir);

                SFTPOpenDirectory_Core(homeDir, true);
            }, true, false);
        }

        private void SFTPInitailize_Init() {
            Log("Initializing SFTP...");
            _sftp.Init();
            Log("...Done");
        }

        #endregion

        #region SFTP Updating tree

        private void SFTPOpenDirectory(string fullPath, bool expand) {
            if (!CanExecuteSFTP())
                return;

            BeginSFTPThread(delegate() {
                SFTPOpenDirectory_Core(fullPath, expand);
            }, true, false);
        }

        private void SFTPOpenDirectory_Core(string fullPath, bool expand) {
            Log("Retrieving directory entries...: " + fullPath);
            SFTPFileInfo[] entries = _sftp.GetDirectoryEntries(fullPath);

            for (int i = 0; i < entries.Length; i++) {
                SFTPFileInfo ent = entries[i];
                if (UnixPermissions.IsSymbolicLink(ent.Permissions)) {
                    // If the symbolic link points a directory,
                    // replace the file information so as to open the node.
                    string path = CombineUnixPath(fullPath, ent.FileName);
                    SFTPFileAttributes attr;
                    try {
                        attr = _sftp.GetFileInformations(path, false);
                    }
                    catch (SFTPClientException e) {
                        if (!IsSFTPError(e, SFTPStatusCode.SSH_FX_NO_SUCH_FILE))
                            throw;
                        // file missing or dead symbolic link ?
                        attr = null;
                    }
                    if (attr != null) {
                        if (UnixPermissions.IsDirectory(attr.Permissions)) {
                            entries[i] = new SFTPFileInfo(ent.FileName, ent.LongName, attr);
                        }
                    }
                }
            }

            this.Invoke((MethodInvoker)delegate() {
                treeViewRemote.BeginUpdate();
                TreeNode dirNode = MakeDirectoryTree(fullPath, expand);
                UpdateTreeDirectoryEntries(dirNode, entries);
                dirNode.EnsureVisible();
                treeViewRemote.EndUpdate();
            });
            Log("...Done");
        }

        #endregion

        #region SFTP Upload

        private void SFTPUpload(string[] localFiles, string remoteDirectoryPath) {
            if (!CanExecuteSFTP())
                return;

            _fileTransferCancellation = new Cancellation();

            BeginSFTPThread(delegate() {
                ClearProgressBar();

                Log("=== UPLOAD ===");
                Log("Remote: " + remoteDirectoryPath);

                bool continued = true;
                try {
                    bool overwite = false;
                    foreach (string localFile in localFiles) {
                        string localFullPath = Path.GetFullPath(localFile);
                        continued = SFTPUpload_UploadRecursively(localFullPath, remoteDirectoryPath, ref overwite);
                        if (!continued)
                            break;
                    }
                }
                finally {
                    ClearProgressBar();
                }

                Log("UPLOAD " + (continued ? "completed." : "canceled."));

                SFTPOpenDirectory_Core(remoteDirectoryPath, true);

                _fileTransferCancellation = null;
            }, true, true);
        }

        private bool SFTPUpload_UploadRecursively(string localFileFullPath, string remoteDirectoryPath, ref bool overwite) {
            string fileName = Path.GetFileName(localFileFullPath);
            string remoteFullPath = CombineUnixPath(remoteDirectoryPath, fileName);
            if (Directory.Exists(localFileFullPath)) {
                SFTPUpload_CreateRemoteDirectoryIfNotExist(fileName, remoteFullPath);
                foreach (string path in Directory.GetDirectories(localFileFullPath)) {
                    bool cont = SFTPUpload_UploadRecursively(path, remoteFullPath, ref overwite);
                    if (!cont)
                        return false;   // cancel
                }
                foreach (string path in Directory.GetFiles(localFileFullPath)) {
                    bool cont = SFTPUpload_UploadRecursively(path, remoteFullPath, ref overwite);
                    if (!cont)
                        return false;   // cancel
                }
                return true;
            }
            else {
                bool cont = SFTPUpload_UploadFile(fileName, localFileFullPath, remoteFullPath, ref overwite);
                return cont;
            }
        }

        private bool SFTPUpload_UploadFile(string fileName, string localFileFullPath, string remoteFullPath, ref bool overwite) {
            if (IsFileTransferCanceled()) {
                Log("Canceled");
                return false;   // cancel
            }

            if (!overwite) {
                bool existence;
                try {
                    _sftp.GetFileInformations(remoteFullPath, true);
                    existence = true;
                }
                catch (SFTPClientException e) {
                    if (!IsSFTPError(e, SFTPStatusCode.SSH_FX_NO_SUCH_FILE))
                        throw;
                    existence = false;
                }

                if (existence) {
                    DialogResult result = DialogResult.None;
                    this.Invoke((MethodInvoker)delegate() {
                        string caption = SFTPPlugin.Instance.StringResource.GetString("SFTPForm.Confirmation");
                        string format = SFTPPlugin.Instance.StringResource.GetString("SFTPForm.AskOverwriteFormat");
                        string message = String.Format(format, remoteFullPath);

                        using (YesNoAllDialog dialog = new YesNoAllDialog(message, caption)) {
                            result = dialog.ShowDialog(this);
                        }
                    });

                    if (result == DialogResult.Cancel) {
                        Log("Canceled");
                        return false;   // cancel
                    }
                    if (result == DialogResult.No) {
                        Log(" | Skipped: " + localFileFullPath);
                        return true;    // skip
                    }
                    if (result == YesNoAllDialog.YesToAll) {
                        overwite = true;
                    }
                }
            }

            FileInfo localFileInfo = new FileInfo(localFileFullPath);
            ulong fileSize = (ulong)localFileInfo.Length;
            _sftp.UploadFile(localFileFullPath, remoteFullPath, _fileTransferCancellation,
                delegate(SFTPFileTransferStatus status, ulong transmitted) {
                    ShowProgress(localFileFullPath, fileName, status, fileSize, transmitted);
                });

            if (IsFileTransferCanceled())
                return false;   // canceled
            else
                return true;
        }

        private void SFTPUpload_CreateRemoteDirectoryIfNotExist(string localName, string remotePath) {
            try {
                _sftp.GetFileInformations(remotePath, true);
                Log(" | Directory already exists: " + remotePath);
                return;
            }
            catch (SFTPClientException e) {
                if (!IsSFTPError(e, SFTPStatusCode.SSH_FX_NO_SUCH_FILE))
                    throw;
            }

            Log(" | Create directory: " + remotePath);
            UpdateProgressBar(localName, 0, 0, false);
            _sftp.CreateDirectory(remotePath);
            UpdateProgressBar(localName, 0, 0, true);
        }

        private void ShowProgress(string localFullPath, string fileName, SFTPFileTransferStatus status, ulong fileSize, ulong transmitted) {
            switch (status) {
                case SFTPFileTransferStatus.Open:
                    Log(" | File: " + localFullPath);
                    Log(" | ... Open");
                    UpdateProgressBar(fileName, fileSize, transmitted, false);
                    break;
                case SFTPFileTransferStatus.Transmitting:
                    LogOverwrite(" | ... Transmitting");
                    UpdateProgressBar(fileName, fileSize, transmitted, false);
                    break;
                case SFTPFileTransferStatus.Close:
                    LogOverwrite(" | ... Closing");
                    UpdateProgressBar(fileName, fileSize, transmitted, false);
                    break;
                case SFTPFileTransferStatus.CompletedSuccess:
                    LogOverwrite(" | ... Done");
                    UpdateProgressBar(fileName, fileSize, transmitted, true);
                    break;
                case SFTPFileTransferStatus.CompletedError:
                    LogOverwrite(" | ... Error");
                    UpdateProgressBar(fileName, fileSize, transmitted, true);
                    break;
                case SFTPFileTransferStatus.CompletedAbort:
                    LogOverwrite(" | ... Aborted");
                    UpdateProgressBar(fileName, fileSize, transmitted, true);
                    break;
            }
        }

        #endregion

        #region SFTP Download

        private void SFTPDownload(string[] remoteFiles, string localDirectoryPath) {

            _fileTransferCancellation = new Cancellation();

            BeginSFTPThread(delegate() {
                ClearProgressBar();

                Log("=== DOWNLOAD ===");

                string localFullPath = Path.GetFullPath(localDirectoryPath);

                bool continued = true;
                try {
                    bool overwite = false;
                    foreach (string remotePath in remoteFiles) {
                        continued = SFTPDownload_DownloadRecursively(remotePath, localFullPath, ref overwite);
                        if (!continued)
                            break;
                    }
                }
                finally {
                    ClearProgressBar();
                }

                _fileTransferCancellation = null;

                Log("DOWNLOAD " + (continued ? "completed." : "canceled."));
            }, true, true);
        }

        private bool SFTPDownload_DownloadRecursively(string remoteFilePath, string localDirectoryPath, ref bool overwite) {
            string fileName = GetUnixPathFileName(remoteFilePath);
            string localPath = Path.Combine(localDirectoryPath, fileName);  // local path to save

            SFTPFileAttributes fileAttr;
            try {
                fileAttr = _sftp.GetFileInformations(remoteFilePath, false);
            }
            catch (SFTPClientException e) {
                if (!IsSFTPError(e, SFTPStatusCode.SSH_FX_NO_SUCH_FILE))
                    throw;
                // file missing or dead symbolic link ?
                Log(" | File: " + remoteFilePath);
                Log("*** Warning: " + e.Message);
                return true;    // skip
            }

            if (UnixPermissions.IsDirectory(fileAttr.Permissions)) {
                if (!Directory.Exists(localPath))
                    Directory.CreateDirectory(localPath);

                SFTPFileInfo[] remoteFiles = _sftp.GetDirectoryEntries(remoteFilePath);
                foreach (SFTPFileInfo fileInfo in remoteFiles) {
                    if (IsDots(fileInfo.FileName))
                        continue;
                    string newRemoteFilePath = CombineUnixPath(remoteFilePath, fileInfo.FileName);
                    bool cont = SFTPDownload_DownloadRecursively(newRemoteFilePath, localPath, ref overwite);
                    if (!cont)
                        return false;   // cancel
                }
                return true;
            }
            else {
                if (!Directory.Exists(localDirectoryPath))
                    Directory.CreateDirectory(localDirectoryPath);

                bool cont = SFTPDownload_DownloadFile(remoteFilePath, fileName, localPath, fileAttr, ref overwite);
                return cont;
            }
        }

        private bool SFTPDownload_DownloadFile(
                        string remoteFullPath, string fileName, string localFileFullPath,
                        SFTPFileAttributes fileAttr, ref bool overwite) {
            if (IsFileTransferCanceled()) {
                Log("Canceled");
                return false;   // cancel
            }

            if (!overwite) {
                bool existence = File.Exists(localFileFullPath);

                if (existence) {
                    DialogResult result = DialogResult.None;
                    this.Invoke((MethodInvoker)delegate() {
                        string caption = SFTPPlugin.Instance.StringResource.GetString("SFTPForm.Confirmation");
                        string format = SFTPPlugin.Instance.StringResource.GetString("SFTPForm.AskOverwriteFormat");
                        string message = String.Format(format, localFileFullPath);

                        using (YesNoAllDialog dialog = new YesNoAllDialog(message, caption)) {
                            result = dialog.ShowDialog(this);
                        }
                    });

                    if (result == DialogResult.Cancel) {
                        Log("Canceled");
                        return false;   // cancel
                    }
                    if (result == DialogResult.No) {
                        Log(" | Skipped: " + localFileFullPath);
                        return true;    // skip
                    }
                    if (result == YesNoAllDialog.YesToAll) {
                        overwite = true;
                    }
                }
            }

            ulong fileSize = fileAttr.FileSize;
            _sftp.DownloadFile(remoteFullPath, localFileFullPath, _fileTransferCancellation,
                delegate(SFTPFileTransferStatus status, ulong transmitted) {
                    ShowProgress(localFileFullPath, fileName, status, fileSize, transmitted);
                });

            if (IsFileTransferCanceled())
                return false;   // canceled
            else
                return true;
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

        #region Tree node operations

        private TreeNode MakeDirectoryTree(string fullPath, bool expand) {
            TreeNode rootNode;
            if (treeViewRemote.Nodes.Count == 0) {
                rootNode = CreateRootNode();
                treeViewRemote.Nodes.Add(rootNode);
            }
            else {
                rootNode = treeViewRemote.Nodes[0];
            }

            if (fullPath == "/")
                return rootNode;

            string[] pathElems = fullPath.Split('/');
            TreeNode parentNode = rootNode;
            for (int i = 0; i < pathElems.Length; i++) {
                string dirName = pathElems[i];
                if (i == 0 && dirName == String.Empty)
                    continue;

                TreeNode dirNode = parentNode.Nodes[dirName];

                if (dirNode == null) {
                    dirNode = CreateDirectoryNode(dirName);
                    parentNode.Nodes.Add(dirNode);
                }

                if (expand)
                    dirNode.Expand();

                parentNode = dirNode;
            }

            return parentNode;
        }

        private void UpdateTreeDirectoryEntries(TreeNode directoryNode, SFTPFileInfo[] entries) {
            TreeNodeCollection children = directoryNode.Nodes;

            Dictionary<string, SFTPFileInfo> newEntries = new Dictionary<string, SFTPFileInfo>();
            foreach (SFTPFileInfo ent in entries) {
                if (IsDots(ent.FileName))
                    continue;
                newEntries.Add(ent.FileName, ent);
            }

            List<TreeNode> nodesToDelete = new List<TreeNode>();
            foreach (TreeNode node in children) {
                if (!newEntries.ContainsKey(node.Name)) {
                    nodesToDelete.Add(node);
                }
            }
            foreach (TreeNode node in nodesToDelete) {
                children.Remove(node);
            }

            foreach (SFTPFileInfo ent in entries) {
                if (IsDots(ent.FileName))
                    continue;
                TreeNode entNode = children[ent.FileName];
                if (entNode == null) {
                    entNode = CreateFileOrDirectoryNode(ent);
                    children.Add(entNode);
                }
                else {
                    UpdateTreeNode(entNode, ent);
                }
            }
        }

        private TreeNode CreateRootNode() {
            TreeNode node = new TreeNode(_remoteName, IMAGE_INDEX_ROOT, IMAGE_INDEX_ROOT);
            node.Name = String.Empty;   // TreeNodeCollection uses this as a key.
            node.Tag = NodeTag.CreateForRoot();
            return node;
        }

        private TreeNode CreateDirectoryNode(string name) {
            TreeNode node = new TreeNode(name, IMAGE_INDEX_FOLDER_CLOSE, IMAGE_INDEX_FOLDER_CLOSE);
            node.Name = name;   // TreeNodeCollection uses this as a key.
            node.ToolTipText = name;
            node.Tag = NodeTag.CreateForDirectory(name);
            return node;
        }

        private TreeNode CreateFileOrDirectoryNode(SFTPFileInfo fileInfo) {
            NodeTag nodeTag = NodeTag.CreateForFileOrDirectory(fileInfo);
            int iconIndex = GetNodeImageIndex(nodeTag.Type);
            TreeNode node = new TreeNode(fileInfo.FileName, iconIndex, iconIndex);
            node.Name = fileInfo.FileName; // TreeNodeCollection uses this as a key.
            node.Tag = nodeTag;
            node.ToolTipText = GetTooltipText(fileInfo);
            return node;
        }

        private void UpdateTreeNode(TreeNode node, SFTPFileInfo fileInfo) {
            NodeTag nodeTag = NodeTag.CreateForFileOrDirectory(fileInfo);
            node.SelectedImageIndex = node.ImageIndex = GetNodeImageIndex(nodeTag.Type);
            node.Tag = nodeTag;
            node.ToolTipText = GetTooltipText(fileInfo);
        }

        private int GetNodeImageIndex(NodeType nodeType) {
            switch (nodeType) {
                case NodeType.Root:
                    return IMAGE_INDEX_ROOT;
                case NodeType.Directory:
                    return IMAGE_INDEX_FOLDER_CLOSE;
                case NodeType.SymbolicLink:
                    return IMAGE_INDEX_SYMBOLICLINK;
                case NodeType.File:
                default:
                    return IMAGE_INDEX_FILE;
            }
        }

        private string GetTooltipText(SFTPFileInfo fileInfo) {
            return new StringBuilder()
                .Append(UnixPermissions.Format(fileInfo.Permissions))
                .Append(' ')
                .Append(fileInfo.FileSize)
                .Append(' ')
                .Append(fileInfo.FileName)
                .ToString();
        }

        private string GetPathOf(TreeNode node) {
            string path = node.FullPath;
            if (path.StartsWith(_remoteName)) {
                path = path.Substring(_remoteName.Length);
                if (path.Length == 0)
                    return "/";
                else
                    return path;
            }
            else {
                return path;
            }
        }

        #endregion

        #region Cancellation status

        private bool IsFileTransferCanceled() {
            Cancellation cancellation = _fileTransferCancellation;
            return cancellation != null && cancellation.IsRequested;
        }

        #endregion

        #region Common

        private static string CombineUnixPath(string path1, string path2) {
            return path1.TrimEnd('/') + "/" + path2;
        }

        private static string GetUnixPathFileName(string path) {
            int s = path.LastIndexOf('/');
            if (s >= 0)
                return path.Substring(s + 1);
            else
                return path;
        }

        private static bool IsSFTPError(Exception e, uint expectedStatusCode) {
            SFTPClientErrorException err = e as SFTPClientErrorException;
            if (err == null) {
                err = e.InnerException as SFTPClientErrorException;
                if (err == null)
                    return false;
            }

            Debug.Assert(err != null);

            return (err.Code == expectedStatusCode);
        }

        private static bool IsDots(string fileName) {
            switch (fileName) {
                case ".":
                case "..":
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Form event handlers

        private void SFTPForm_Shown(object sender, EventArgs e) {
            SFTPInitailize();
        }

        private void SFTPForm_Load(object sender, EventArgs e) {
            if (_ownerForm != null) {
                _ownerForm.FormClosed += new FormClosedEventHandler(_ownerForm_FormClosed);

                this.Location = new Point(
                    _ownerForm.Left + (_ownerForm.Width - this.Width) / 2,
                    _ownerForm.Top + (_ownerForm.Height - this.Height) / 2
                );
            }

        }

        private void SFTPForm_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing && !_closedByOwner) {
                if (_sftpThread != null && _sftpThread.IsAlive) {
                    e.Cancel = true;
                    return;
                }
            }

            _formClosed = true;

            if (_sftpThread != null && _sftpThread.IsAlive)
                _sftpThread.Abort(); // FIXME: we need graceful cancellation

            try {
                _sftp.Close();
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }

        private void SFTPForm_FormClosed(object sender, FormClosedEventArgs e) {
            if (_ownerForm != null) {
                _ownerForm.FormClosed -= new FormClosedEventHandler(_ownerForm_FormClosed);
                _ownerForm = null;
                _sftp = null;
            }
        }

        private void _ownerForm_FormClosed(object sender, FormClosedEventArgs e) {
            _closedByOwner = true;
            this.Close();
        }

        #endregion

        #region Button event handlers

        private void buttonDownload_Click(object sender, EventArgs e) {
            if (!CanExecuteSFTP())
                return;

            TreeNode[] selectedNodes = this.treeViewRemote.SelectedNodes;

            if (selectedNodes.Length == 0)
                return;

            string[] selectedPaths = new string[selectedNodes.Length];
            for (int i = 0; i < selectedNodes.Length; i++) {
                selectedPaths[i] = GetPathOf(selectedNodes[i]);
            }


            using (FolderBrowserDialog dialog = new FolderBrowserDialog()) {
                if (_saveFolderPath == null)
                    _saveFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.SelectedPath = _saveFolderPath;
                dialog.ShowNewFolderButton = true;
                dialog.Description = SFTPPlugin.Instance.StringResource.GetString("SFTPForm.ChooseFolder");
                DialogResult result = dialog.ShowDialog(this);
                if (result != DialogResult.OK)
                    return;
                _saveFolderPath = dialog.SelectedPath;
            }

            SFTPDownload(selectedPaths, _saveFolderPath);
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            Cancellation cancellation = _fileTransferCancellation;
            if (cancellation != null) {
                cancellation.Cancel();
                buttonCancel.Enabled = false;
            }
        }

        #endregion

        #region TreeView event handlers

        private void treeViewRemote_BeforeExpand(object sender, TreeViewCancelEventArgs e) {
            // Change directory's icon
            TreeNode node = e.Node;
            NodeTag tag = node.Tag as NodeTag;
            if (tag != null && tag.Type == NodeType.Directory)
                node.SelectedImageIndex = node.ImageIndex = IMAGE_INDEX_FOLDER_OPEN;
        }

        private void treeViewRemote_BeforeCollapse(object sender, TreeViewCancelEventArgs e) {
            // Change directory's icon
            TreeNode node = e.Node;
            NodeTag tag = node.Tag as NodeTag;
            if (tag != null && tag.Type == NodeType.Directory)
                node.SelectedImageIndex = node.ImageIndex = IMAGE_INDEX_FOLDER_CLOSE;
        }

        private void treeViewRemote_SingleNodeSelected(object sender, TreeViewEventArgs e) {
            if (e.Action != TreeViewAction.ByMouse && e.Action != TreeViewAction.ByKeyboard)
                return;

            if (_treeConstructing)
                return;

            // Retrieve directory entries
            TreeNode node = e.Node;
            NodeTag tag = node.Tag as NodeTag;
            if (tag != null && (tag.Type == NodeType.Directory || tag.Type == NodeType.Root)) {
                string fullPath = GetPathOf(node);
                SFTPOpenDirectory(fullPath, false);
            }
        }

        private void treeViewRemote_DragOver(object sender, DragEventArgs e) {
            TreeNode node = GetDroppableNode(e);
            if (node != null)
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;

            if (node != null) {
                treeViewRemote.SelectNode(node);
            }
        }

        private void treeViewRemote_DragDrop(object sender, DragEventArgs e) {
            TreeNode node = GetDroppableNode(e);
            if (node != null) {
                string remotePath = GetPathOf(node);

                string[] localFiles = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (localFiles != null) {
                    SFTPUpload(localFiles, remotePath);
                }
            }
        }

        private TreeNode GetDroppableNode(DragEventArgs e) {
            if ((e.AllowedEffect & DragDropEffects.Copy) == DragDropEffects.Copy
                && e.Data.GetDataPresent(DataFormats.FileDrop)) {

                Point clientPoint = treeViewRemote.PointToClient(new Point(e.X, e.Y));
                TreeNode node = treeViewRemote.GetNodeAt(clientPoint);
                if (node != null) {
                    NodeTag tag = node.Tag as NodeTag;
                    if (tag != null && tag.Type == NodeType.Directory) {
                        return node;
                    }
                }
            }
            return null;
        }

        #endregion

    }
}