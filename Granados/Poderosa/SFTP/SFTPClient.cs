/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SFTPClient.cs,v 1.6 2012/05/05 12:42:45 kzmi Exp $
 */

//#define DUMP_PACKET
//#define TRACE_RECEIVER

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;

using Granados.SSH2;
using Granados.IO;
using Granados.IO.SSH2;
using Granados.Util;
using Granados.Poderosa.FileTransfer;

namespace Granados.Poderosa.SFTP {

    /// <summary>
    /// Statuses of the file transfer
    /// </summary>
    public enum SFTPFileTransferStatus {
        /// <summary>Not started</summary>
        None,
        /// <summary>Opening remote file is attempted</summary>
        Open,
        /// <summary>File was opened successfully and data has been requested</summary>
        Transmitting,
        /// <summary>Closing remote file is attempted</summary>
        Close,
        /// <summary>File was closed successfully</summary>
        CompletedSuccess,
        /// <summary>Error has occurred</summary>
        CompletedError,
        /// <summary>File transfer was aborted by the cancellation.</summary>
        CompletedAbort,
    }

    /// <summary>
    /// Delegate notifies progress of the file transfer.
    /// </summary>
    /// <param name="status">Status of the file transfer.</param>
    /// <param name="transmitted">Transmitted data length.</param>
    public delegate void SFTPFileTransferProgressDelegate(SFTPFileTransferStatus status, ulong transmitted);

    /// <summary>
    /// SFTP Client
    /// </summary>
    /// 
    /// <remarks>
    /// <para>This class is designed to be used in the worker thread.</para>
    /// <para>Some methods block thread while waiting for the response.</para>
    /// </remarks>
    /// 
    /// <remarks>
    /// Referenced specification:<br/>
    /// IETF Network Working Group Internet Draft<br/>
    /// SSH File Transfer Protocol<br/>
    /// draft-ietf-secsh-filexfer-02 (protocol version 3)
    /// </remarks>
    public class SFTPClient {

        #region Private fields

        private readonly SSHChannel _channel;
        private readonly SFTPClientChannelEventReceiver _channelReceiver;
        private readonly SFTPPacket _packet = new SFTPPacket(); // each method reuses this

        private int _protocolTimeout = 5000;

        private Encoding _encoding = Encoding.UTF8;

        private uint _requestId = 0;

        private bool _closed = false;

        #endregion

        #region Private constants

        // Note: OpenSSH uses version 3
        private const int SFTP_VERSION = 3;

        // attribute flags
        private const uint SSH_FILEXFER_ATTR_SIZE = 0x00000001;
        private const uint SSH_FILEXFER_ATTR_UIDGID = 0x00000002;
        private const uint SSH_FILEXFER_ATTR_PERMISSIONS = 0x00000004;
        private const uint SSH_FILEXFER_ATTR_ACMODTIME = 0x00000008;
        private const uint SSH_FILEXFER_ATTR_EXTENDED = 0x80000000;

        // file open flag
        private const uint SSH_FXF_READ = 0x00000001;
        private const uint SSH_FXF_WRITE = 0x00000002;
        private const uint SSH_FXF_APPEND = 0x00000004;
        private const uint SSH_FXF_CREAT = 0x00000008;
        private const uint SSH_FXF_TRUNC = 0x00000010;
        private const uint SSH_FXF_EXCL = 0x00000020;

        private const int FILE_TRANSFER_BLOCK_SIZE = 10240; // FIXME: should it be flexible ?

        #endregion

        #region Properties

        /// <summary>
        /// Protocol timeout in milliseconds.
        /// </summary>
        public int ProtocolTimeout {
            get {
                return _protocolTimeout;
            }
            set {
                _protocolTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets encoding to convert path name or file name.
        /// </summary>
        public Encoding Encoding {
            get {
                return _encoding;
            }
            set {
                _encoding = value;
            }
        }

        /// <summary>
        /// Gets whether the channel has been closed
        /// </summary>
        public bool IsClosed {
            get {
                return _closed;
            }
        }

        #endregion

        #region Static methods

        /// <summary>
        /// Opens SFTP channel and creates a new instance.
        /// </summary>
        /// <param name="connection">SSH2 connection object</param>
        /// <returns>New instance.</returns>
        public static SFTPClient OpenSFTPChannel(SSH2Connection connection) {
            SFTPClientChannelEventReceiver channelReceiver = new SFTPClientChannelEventReceiver();
            SSHChannel channel = connection.OpenSubsystem(channelReceiver, "sftp");
            return new SFTPClient(channel, channelReceiver);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel">SSH2 channel object</param>
        /// <param name="channelReceiver">event receiver object</param>
        private SFTPClient(SSHChannel channel, SFTPClientChannelEventReceiver channelReceiver) {
            this._channel = channel;
            this._channelReceiver = channelReceiver;
        }

        #endregion

        #region Initialize

        /// <summary>
        /// Initialize
        /// </summary>
        /// <remarks>
        /// Send SSH_FXP_INIT and receive SSH_FXP_VERSION.
        /// </remarks>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public void Init() {
            WaitReady();
            CheckStatus();

            _packet.Open(SFTPPacketType.SSH_FXP_INIT, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteInt32(SFTP_VERSION);

            bool[] result = new bool[] { false };

            lock (_channelReceiver.ResponseNotifier) {
                Transmit(_packet);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_VERSION) {
                            int version = dataReader.ReadInt32();
                            Debug.WriteLine("SFTP: SSH_FXP_VERSION => " + version);

                            result[0] = true;   // OK, received SSH_FXP_VERSION

                            while (dataReader.Rest > 4) {
                                byte[] extensionData = dataReader.ReadString();
                                string extensionText = Encoding.ASCII.GetString(extensionData);
                                Debug.WriteLine("SFTP: SSH_FXP_VERSION => " + extensionText);
                            }

                            return true;    // processed
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (!result[0])
                throw new SFTPClientException("Missing SSH_FXP_VERSION");
        }

        /// <summary>
        /// Waits for channel status to be "READY".
        /// The current thread is blocked until the status comes to "READY" or "CLOSED".
        /// </summary>
        /// <returns>Whether the channel status is READY.</returns>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        private bool WaitReady() {
            lock (_channelReceiver.StatusChangeNotifier) {
                if (_channelReceiver.ChannelStatus == SFTPChannelStatus.UNKNOWN) {
                    bool signaled = Monitor.Wait(_channelReceiver.StatusChangeNotifier, _protocolTimeout);
                    if (!signaled)
                        throw new SFTPClientTimeoutException();
                }
                return (_channelReceiver.ChannelStatus == SFTPChannelStatus.READY);
            }
        }

        #endregion

        #region Close

        /// <summary>
        /// Close channel.
        /// </summary>
        public void Close() {
            CheckStatus();

            _closed = true;
            _channel.SendEOF();
            _channel.Close();
        }

        #endregion

        #region Path operations

        /// <summary>
        /// Gets canonical path.
        /// </summary>
        /// <param name="path">Path to be canonicalized.</param>
        /// <returns>Canonical path.</returns>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status.</exception>
        public string GetRealPath(string path) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(SFTPPacketType.SSH_FXP_REALPATH, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] pathData = _encoding.GetBytes(path);
            writer.WriteAsString(pathData);

            bool[] result = new bool[] { false };
            string realPath = null;

            lock (_channelReceiver.ResponseNotifier) {
                Transmit(_packet);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_STATUS) {
                            SFTPClientErrorException exception = SFTPClientErrorException.Create(dataReader);
                            if (exception.ID == requestId) {
                                throw exception;
                            }
                        }
                        else if (packetType == SFTPPacketType.SSH_FXP_NAME) {
                            uint id = dataReader.ReadUInt32();
                            if (id == requestId) {
                                uint count = (uint)dataReader.ReadInt32();

                                if (count > 0) {
                                    // use Encoding object with replacement fallback
                                    Encoding encoding = Encoding.GetEncoding(
                                                        _encoding.CodePage,
                                                        EncoderFallback.ReplacementFallback,
                                                        DecoderFallback.ReplacementFallback);

                                    SFTPFileInfo fileInfo = ReadFileInfo(dataReader, encoding);
                                    realPath = fileInfo.FileName;
                                }

                                result[0] = true;   // OK, received SSH_FXP_NAME
                                return true;    // processed
                            }
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (!result[0])
                throw new SFTPClientException("Missing SSH_FXP_NAME");

            return realPath;
        }

        #endregion

        #region Directory operations

        /// <summary>
        /// Gets directory entries in the specified directory path.
        /// </summary>
        /// <param name="directoryPath">Directory path.</param>
        /// <returns>Array of the file information.</returns>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status.</exception>
        public SFTPFileInfo[] GetDirectoryEntries(string directoryPath) {
            CheckStatus();

            uint requestId = ++_requestId;

            while (directoryPath != "/" && directoryPath.EndsWith("/")) {
                directoryPath = directoryPath.Substring(0, directoryPath.Length - 1);
            }

            byte[] handle = OpenDir(requestId, directoryPath);

            List<SFTPFileInfo> files = new List<SFTPFileInfo>();

            while (true) {
                ICollection<SFTPFileInfo> tmpList = ReadDir(requestId, handle);
                if (tmpList.Count == 0)
                    break;
                files.AddRange(tmpList);
            }

            CloseHandle(requestId, handle);
            return files.ToArray();
        }

        /// <summary>
        /// Create directory.
        /// </summary>
        /// <param name="path">Directory path to create.</param>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public void CreateDirectory(string path) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(SFTPPacketType.SSH_FXP_MKDIR, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] pathData = _encoding.GetBytes(path);
            writer.WriteAsString(pathData);
            writer.WriteInt32(0);   // attributes flag

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        /// <summary>
        /// Remove directory.
        /// </summary>
        /// <param name="path">Directory path to remove.</param>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public void RemoveDirectory(string path) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(SFTPPacketType.SSH_FXP_RMDIR, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] pathData = _encoding.GetBytes(path);
            writer.WriteAsString(pathData);

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        #endregion

        #region File operations

        /// <summary>
        /// Get file informations.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="lstat">Specifies to use lstat. Symbolic link is not followed and informations about the symbolic link are returned.</param>
        /// <returns>File attribute informations</returns>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public SFTPFileAttributes GetFileInformations(string path, bool lstat) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(lstat ? SFTPPacketType.SSH_FXP_LSTAT : SFTPPacketType.SSH_FXP_STAT, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] pathData = _encoding.GetBytes(path);
            writer.WriteAsString(pathData);

            bool[] result = new bool[] { false };
            SFTPFileAttributes attributes = null;

            lock (_channelReceiver.ResponseNotifier) {
                Transmit(_packet);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_STATUS) {
                            SFTPClientErrorException exception = SFTPClientErrorException.Create(dataReader);
                            if (exception.ID == requestId) {
                                throw exception;
                            }
                        }
                        else if (packetType == SFTPPacketType.SSH_FXP_ATTRS) {
                            uint id = dataReader.ReadUInt32();
                            if (id == requestId) {
                                attributes = ReadFileAttributes(dataReader);
                                result[0] = true;   // Ok, received SSH_FXP_ATTRS
                                return true;    // processed
                            }
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (!result[0])
                throw new SFTPClientException("Missing SSH_FXP_ATTRS");

            return attributes;
        }

        /// <summary>
        /// Remove file.
        /// </summary>
        /// <param name="path">File path to remove.</param>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public void RemoveFile(string path) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(SFTPPacketType.SSH_FXP_REMOVE, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] pathData = _encoding.GetBytes(path);
            writer.WriteAsString(pathData);

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        /// <summary>
        /// Rename file or directory.
        /// </summary>
        /// <param name="oldPath">Old path.</param>
        /// <param name="newPath">New path.</param>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public void Rename(string oldPath, string newPath) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(SFTPPacketType.SSH_FXP_RENAME, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] oldPathData = _encoding.GetBytes(oldPath);
            writer.WriteAsString(oldPathData);
            byte[] newPathData = _encoding.GetBytes(newPath);
            writer.WriteAsString(newPathData);

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        /// <summary>
        /// Set permissions of the file or directory.
        /// </summary>
        /// <param name="path">Path.</param>
        /// <param name="permissions">Permissions to set.</param>
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status</exception>
        /// <exception cref="Exception">An exception which was thrown while processing the response.</exception>
        public void SetPermissions(string path, int permissions) {
            CheckStatus();

            uint requestId = ++_requestId;

            _packet.Open(SFTPPacketType.SSH_FXP_SETSTAT, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            byte[] pathData = _encoding.GetBytes(path);
            writer.WriteAsString(pathData);
            writer.WriteUInt32(SSH_FILEXFER_ATTR_PERMISSIONS);
            writer.WriteUInt32((uint)permissions & 0xfffu /* 07777 */);

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        #endregion

        #region File transfer

        /// <summary>
        /// Download a file.
        /// </summary>
        /// <remarks>
        /// Even if download failed, local file is not deleted.
        /// </remarks>
        /// 
        /// <param name="remotePath">Remote file path to download.</param>
        /// <param name="localPath">Local file path to save.</param>
        /// <param name="cancellation">An object to request the cancellation. Set null if the cancellation is not needed.</param>
        /// <param name="progressDelegate">Delegate to notify progress. Set null if notification is not needed.</param>
        /// 
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status.</exception>
        /// <exception cref="SFTPClientException">Error.</exception>
        /// <exception cref="Exception">Error.</exception>
        public void DownloadFile(string remotePath, string localPath, Cancellation cancellation, SFTPFileTransferProgressDelegate progressDelegate) {
            CheckStatus();

            uint requestId = ++_requestId;

            ulong transmitted = 0;

            Exception pendingException = null;

            try {
                if (progressDelegate != null)
                    progressDelegate(SFTPFileTransferStatus.Open, transmitted);

                byte[] handle = OpenFile(requestId, remotePath, SSH_FXF_READ);

                bool isTransferring = false;
                bool hasError = false;
                bool isCanceled = false;
                try {
                    using (FileStream fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                        while (true) {
                            if (cancellation != null && cancellation.IsRequested) {
                                isCanceled = true;
                                break;
                            }

                            if (progressDelegate != null)
                                progressDelegate(SFTPFileTransferStatus.Transmitting, transmitted);

                            isTransferring = true;
                            byte[] data = ReadFile(requestId, handle, transmitted, FILE_TRANSFER_BLOCK_SIZE);
                            isTransferring = false;

                            if (data == null)
                                break; // EOF

                            if (data.Length > 0) {
                                fileStream.Write(data, 0, data.Length);
                                transmitted += (ulong)data.Length;
                            }
                        }
                    }
                }
                catch (Exception e) {
                    if (isTransferring)    // exception was raised in ReadFile() ?
                        throw;

                    pendingException = e;
                    hasError = true;
                }

                if (progressDelegate != null)
                    progressDelegate(SFTPFileTransferStatus.Close, transmitted);

                CloseHandle(requestId, handle);

                if (progressDelegate != null) {
                    SFTPFileTransferStatus status =
                        hasError ? SFTPFileTransferStatus.CompletedError :
                        isCanceled ? SFTPFileTransferStatus.CompletedAbort : SFTPFileTransferStatus.CompletedSuccess;
                    progressDelegate(status, transmitted);
                }
            }
            catch (Exception) {
                if (progressDelegate != null)
                    progressDelegate(SFTPFileTransferStatus.CompletedError, transmitted);
                throw;
            }

            if (pendingException != null)
                throw new SFTPClientException(pendingException.Message, pendingException);
        }

        /// <summary>
        /// Upload a file.
        /// </summary>
        /// 
        /// <param name="localPath">Local file path to upload.</param>
        /// <param name="remotePath">Remote file path to write.</param>
        /// <param name="cancellation">An object to request the cancellation. Set null if the cancellation is not needed.</param>
        /// <param name="progressDelegate">Delegate to notify progress. Set null if notification is not needed.</param>
        /// 
        /// <exception cref="SFTPClientErrorException">Operation failed.</exception>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="SFTPClientInvalidStatusException">Invalid status.</exception>
        /// <exception cref="SFTPClientException">Error.</exception>
        /// <exception cref="Exception">Error.</exception>
        public void UploadFile(string localPath, string remotePath, Cancellation cancellation, SFTPFileTransferProgressDelegate progressDelegate) {
            CheckStatus();

            uint requestId = ++_requestId;

            ulong transmitted = 0;

            Exception pendingException = null;

            try {
                bool isTransferring = false;
                bool hasError = false;
                bool isCanceled = false;
                byte[] handle = null;
                try {
                    using (FileStream fileStream = new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {

                        if (progressDelegate != null)
                            progressDelegate(SFTPFileTransferStatus.Open, transmitted);

                        isTransferring = true;
                        handle = OpenFile(requestId, remotePath, SSH_FXF_WRITE | SSH_FXF_CREAT | SSH_FXF_TRUNC);
                        isTransferring = false;

                        byte[] buff = new byte[FILE_TRANSFER_BLOCK_SIZE];

                        while (true) {
                            if (cancellation != null && cancellation.IsRequested) {
                                isCanceled = true;
                                break;
                            }

                            if (progressDelegate != null)
                                progressDelegate(SFTPFileTransferStatus.Transmitting, transmitted);

                            int length = fileStream.Read(buff, 0, buff.Length);

                            if (length > 0) {
                                isTransferring = true;
                                WriteFile(requestId, handle, transmitted, buff, length);
                                isTransferring = false;

                                transmitted += (ulong)length;
                            }

                            if (length == 0 || length < buff.Length)
                                break; // EOF
                        }
                    }
                }
                catch (Exception e) {
                    if (isTransferring)    // exception was raised in OpenFile() or WriteFile() ?
                        throw;

                    pendingException = e;
                    hasError = true;
                }

                if (handle != null) {
                    if (progressDelegate != null)
                        progressDelegate(SFTPFileTransferStatus.Close, transmitted);

                    CloseHandle(requestId, handle);
                }

                if (progressDelegate != null) {
                    SFTPFileTransferStatus status =
                        hasError ? SFTPFileTransferStatus.CompletedError :
                        isCanceled ? SFTPFileTransferStatus.CompletedAbort : SFTPFileTransferStatus.CompletedSuccess;
                    progressDelegate(status, transmitted);
                }
            }
            catch (Exception) {
                if (progressDelegate != null)
                    progressDelegate(SFTPFileTransferStatus.CompletedError, transmitted);
                throw;
            }

            if (pendingException != null)
                throw new SFTPClientException(pendingException.Message, pendingException);
        }

        #endregion

        #region Private methods about status

        private void TransmitPacketAndWaitForStatusOK(uint requestId, SFTPPacket packet) {
            bool[] result = new bool[] { false };

            lock (_channelReceiver.ResponseNotifier) {
                Transmit(packet);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_STATUS) {
                            SFTPClientErrorException exception = SFTPClientErrorException.Create(dataReader);
                            if (exception.ID == requestId) {
                                if (exception.Code == SFTPStatusCode.SSH_FX_OK) {
                                    result[0] = true;   // Ok, received SSH_FX_OK
                                    return true;    // processed
                                }
                                else {
                                    throw exception;
                                }
                            }
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (!result[0])
                throw new SFTPClientException("Missing SSH_FXP_STATUS");
        }

        #endregion

        #region Private methods about file

        private byte[] OpenFile(uint requestId, string filePath, uint flags) {
            _packet.Open(SFTPPacketType.SSH_FXP_OPEN, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            writer.WriteAsString(_encoding.GetBytes(filePath));
            writer.WriteUInt32(flags);
            writer.WriteUInt32(0);  // attribute flag

            return WaitHandle(_packet, requestId);
        }

        private byte[] ReadFile(uint requestId, byte[] handle, ulong offset, int length) {
            _packet.Open(SFTPPacketType.SSH_FXP_READ, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            writer.WriteAsString(handle);
            writer.WriteUInt64(offset);
            writer.WriteInt32(length);

            byte[] data = null;
            bool[] result = new bool[] { false };

            lock (_channelReceiver.ResponseNotifier) {
                Transmit(_packet);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_STATUS) {
                            SFTPClientErrorException exception = SFTPClientErrorException.Create(dataReader);
                            if (exception.ID == requestId) {
                                if (exception.Code == SFTPStatusCode.SSH_FX_EOF) {
                                    data = null;    // EOF
                                    result[0] = true;   // OK, received SSH_FX_EOF
                                    return true;    // processed
                                }
                                else {
                                    throw exception;
                                }
                            }
                        }
                        else if (packetType == SFTPPacketType.SSH_FXP_DATA) {
                            uint id = dataReader.ReadUInt32();
                            if (id == requestId) {
                                data = dataReader.ReadString();
                                result[0] = true;   // OK, received SSH_FXP_DATA
                                return true;    // processed
                            }
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (!result[0])
                throw new SFTPClientException("Missing SSH_FXP_DATA");

            return data;
        }

        private void WriteFile(uint requestId, byte[] handle, ulong offset, byte[] buff, int length) {
            _packet.Open(SFTPPacketType.SSH_FXP_WRITE, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            writer.WriteAsString(handle);
            writer.WriteUInt64(offset);
            writer.WriteAsString(buff, 0, length);

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        #endregion

        #region Private methods about directory

        private byte[] OpenDir(uint requestId, string directoryPath) {
            _packet.Open(SFTPPacketType.SSH_FXP_OPENDIR, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            writer.WriteAsString(_encoding.GetBytes(directoryPath));

            return WaitHandle(_packet, requestId);
        }

        private ICollection<SFTPFileInfo> ReadDir(uint requestId, byte[] handle) {
            _packet.Open(SFTPPacketType.SSH_FXP_READDIR, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            writer.WriteAsString(handle);

            List<SFTPFileInfo> fileList = new List<SFTPFileInfo>();
            bool[] result = new bool[] { false };

            lock (_channelReceiver.ResponseNotifier) {
                Transmit(_packet);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_STATUS) {
                            SFTPClientErrorException exception = SFTPClientErrorException.Create(dataReader);
                            if (exception.ID == requestId) {
                                if (exception.Code == SFTPStatusCode.SSH_FX_EOF) {
                                    result[0] = true;
                                    return true;    // processed
                                }
                                else {
                                    throw exception;
                                }
                            }
                        }
                        else if (packetType == SFTPPacketType.SSH_FXP_NAME) {
                            uint id = dataReader.ReadUInt32();
                            if (id == requestId) {
                                uint count = (uint)dataReader.ReadInt32();

                                // use Encoding object with replacement fallback
                                Encoding encoding = Encoding.GetEncoding(
                                                    _encoding.CodePage,
                                                    EncoderFallback.ReplacementFallback,
                                                    DecoderFallback.ReplacementFallback);

                                for (int i = 0; i < count; i++) {
                                    SFTPFileInfo fileInfo = ReadFileInfo(dataReader, encoding);
                                    fileList.Add(fileInfo);
                                }

                                result[0] = true;   // OK, received SSH_FXP_NAME

                                return true;    // processed
                            }
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (!result[0])
                throw new SFTPClientException("Missing SSH_FXP_NAME");

            return fileList;
        }

        private SFTPFileInfo ReadFileInfo(SSHDataReader dataReader, Encoding encoding) {
            byte[] fileNameData = dataReader.ReadString();
            string fileName = encoding.GetString(fileNameData);
            byte[] longNameData = dataReader.ReadString();
            string longName = encoding.GetString(longNameData);

            SFTPFileAttributes attributes = ReadFileAttributes(dataReader);

            return new SFTPFileInfo(fileName, longName, attributes);
        }

        private SFTPFileAttributes ReadFileAttributes(SSHDataReader dataReader) {
            ulong fileSize = 0;
            uint uid = 0;
            uint gid = 0;
            uint permissions = 0666;
            uint atime = 0;
            uint mtime = 0;

            uint flags = (uint)dataReader.ReadInt32();

            if ((flags & SSH_FILEXFER_ATTR_SIZE) != 0) {
                fileSize = dataReader.ReadUInt64();
            }

            if ((flags & SSH_FILEXFER_ATTR_UIDGID) != 0) {
                uid = dataReader.ReadUInt32();
                gid = dataReader.ReadUInt32();
            }

            if ((flags & SSH_FILEXFER_ATTR_PERMISSIONS) != 0) {
                permissions = dataReader.ReadUInt32();
            }

            if ((flags & SSH_FILEXFER_ATTR_ACMODTIME) != 0) {
                atime = dataReader.ReadUInt32();
                mtime = dataReader.ReadUInt32();
            }

            if ((flags & SSH_FILEXFER_ATTR_EXTENDED) != 0) {
                int count = dataReader.ReadInt32();
                for (int i = 0; i < count; i++) {
                    dataReader.ReadString();    // extended type
                    dataReader.ReadString();    // extended data
                }
            }

            return new SFTPFileAttributes(fileSize, uid, gid, permissions, atime, mtime);
        }

        #endregion

        #region Private methods about handle

        private byte[] WaitHandle(SFTPPacket requestPacket, uint requestId) {
            byte[] handle = null;
            lock (_channelReceiver.ResponseNotifier) {
                Transmit(requestPacket);
                _channelReceiver.WaitResponse(
                    delegate(SFTPPacketType packetType, SSHDataReader dataReader) {
                        if (packetType == SFTPPacketType.SSH_FXP_STATUS) {
                            SFTPClientErrorException exception = SFTPClientErrorException.Create(dataReader);
                            if (exception.ID == requestId)
                                throw exception;
                        }
                        else if (packetType == SFTPPacketType.SSH_FXP_HANDLE) {
                            uint id = dataReader.ReadUInt32();
                            if (id == requestId) {
                                handle = dataReader.ReadString();
                                return true;    // processed
                            }
                        }

                        return false;   // ignored
                    },
                    _protocolTimeout);
            }

            // sanity check
            if (handle == null)
                throw new SFTPClientException("Missing SSH_FXP_HANDLE");

            return handle;
        }

        private void CloseHandle(uint requestId, byte[] handle) {
            _packet.Open(SFTPPacketType.SSH_FXP_CLOSE, _channel.RemoteChannelID);
            SSH2DataWriter writer = _packet.DataWriter;
            writer.WriteUInt32(requestId);
            writer.WriteAsString(handle);

            TransmitPacketAndWaitForStatusOK(requestId, _packet);
        }

        #endregion

        #region Other private methods

        private void Transmit(SFTPPacket packet) {
            ((SSH2Connection)_channel.Connection).TransmitPacket(packet);
        }

        private void CheckStatus() {
            if (_closed || _channelReceiver.ChannelStatus != SFTPChannelStatus.READY)
                throw new SFTPClientInvalidStatusException();
        }

        #endregion
    }

    /// <summary>
    /// Channel status
    /// </summary>
    internal enum SFTPChannelStatus {
        UNKNOWN,
        READY,
        CLOSED,
        ERROR,
    }

    /// <summary>
    /// Delegate that handles SFTP packet data.
    /// </summary>
    /// <param name="packetType">SFTP packet type</param>
    /// <param name="dataReader">Data reader which can read SFTP data.</param>
    /// <returns>true if the packet was processed.</returns>
    internal delegate bool DataReceivedDelegate(SFTPPacketType packetType, SSHDataReader dataReader);

    /// <summary>
    /// Channel data handler for SFTPClient
    /// </summary>
    internal class SFTPClientChannelEventReceiver : ISSHChannelEventReceiver {

        #region Private fields

        private SFTPChannelStatus _channelStatus = SFTPChannelStatus.UNKNOWN;

        private readonly Object _statusChangeNotifier = new object();
        private readonly Object _responseNotifier = new object();

        private DataReceivedDelegate _responseHandler = null;
        private Exception _responseHandlerException = null;

        private bool _isDataIncomplete = false;
        private readonly SimpleMemoryStream _dataBuffer = new SimpleMemoryStream();
        private int _dataTotal = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets an object for synchronizing change of the channel status.
        /// </summary>
        public object StatusChangeNotifier {
            get {
                return _statusChangeNotifier;
            }
        }

        /// <summary>
        /// Gets an object for synchronizing handling of the response.
        /// </summary>
        public object ResponseNotifier {
            get {
                return _responseNotifier;
            }
        }

        /// <summary>
        /// Gets channel status
        /// </summary>
        public SFTPChannelStatus ChannelStatus {
            get {
                return _channelStatus;
            }
        }

        #endregion

        #region Utility methods

        /// <summary>
        /// Wait for response.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Caller should lock ResponseNotifier before send a request packet,
        /// and this method should be called in the lock-block.
        /// </para>
        /// </remarks>
        /// <param name="responseHandler">delegate which handles response data</param>
        /// <param name="millisecondsTimeout">timeout in milliseconds</param>
        /// <exception cref="SFTPClientTimeoutException">Timeout has occured.</exception>
        /// <exception cref="Exception">an exception which was thrown while executing responseHandler.</exception>
        public void WaitResponse(DataReceivedDelegate responseHandler, int millisecondsTimeout) {
            // Note: This lock is not required if the caller has locked ResponseNotifier.
            //       We do this for make clear that Monitor.Wait() is called in the locked context.
            lock (_responseNotifier) {
                _isDataIncomplete = false;
                _dataTotal = 0;
                _responseHandler = responseHandler;
                _responseHandlerException = null;
                bool signaled = Monitor.Wait(_responseNotifier, millisecondsTimeout);
                _responseHandler = null;
                if (_responseHandlerException != null)
                    throw new SFTPClientException(_responseHandlerException.Message, _responseHandlerException);
                if (!signaled)
                    throw new SFTPClientTimeoutException();
            }
        }

        #endregion

        #region ISSHChannelEventReceiver

        public void OnData(byte[] data, int offset, int length) {
#if DUMP_PACKET
            Dump("SFTP: OnData", data, offset, length);
#endif
            DataFragment dataFragment;
            if (_isDataIncomplete) {
                // append to buffer
                _dataBuffer.Write(data, offset, length);

                if (_dataTotal == 0) {  // not determined yet
                    if (_dataBuffer.Length < 4)
                        return;
                    _dataTotal = SSHUtil.ReadInt32(_dataBuffer.UnderlyingBuffer, 0);
                }

                if (_dataBuffer.Length < _dataTotal)
                    return;

                _isDataIncomplete = false;
                _dataTotal = 0;
                dataFragment = new DataFragment(_dataBuffer.UnderlyingBuffer, 0, (int)_dataBuffer.Length);
            }
            else {
                if (length < 4) {
                    _dataBuffer.Reset();
                    _dataBuffer.Write(data, offset, length);
                    _isDataIncomplete = true;
                    _dataTotal = 0; // determine later...
                    return;
                }

                int total = SSHUtil.ReadInt32(data, offset);
                if (length - 4 < total) {
                    _dataBuffer.Reset();
                    _dataBuffer.Write(data, offset, length);
                    _isDataIncomplete = true;
                    _dataTotal = total;
                    return;
                }
                dataFragment = new DataFragment(data, offset, length);
            }

            SSH2DataReader reader = new SSH2DataReader(dataFragment);
            int dataLength = reader.ReadInt32();
            if (dataLength >= 1) {
                SFTPPacketType packetType = (SFTPPacketType)reader.ReadByte();
                dataLength--;
                lock (ResponseNotifier) {
                    bool processed = false;
                    if (_responseHandler != null) {
                        try {
                            processed = _responseHandler(packetType, reader);
                        }
                        catch (Exception e) {
                            _responseHandlerException = e;
                            processed = true;
                        }
                    }
                    else {
                        processed = true;
                    }

                    if (processed) {
                        Monitor.PulseAll(_responseNotifier);
                    }
                }
            }
            // FIXME: invalid packet should be alerted
        }

        public void OnExtendedData(int type, byte[] data) {
#if DUMP_PACKET
            Dump("SFTP: OnExtendedData: " + type, data, 0, data.Length);
#endif
        }

        public void OnChannelClosed() {
            lock (StatusChangeNotifier) {
#if TRACE_RECEIVER
                System.Diagnostics.Debug.WriteLine("SFTP: Closed");
#endif
                TransitStatus(SFTPChannelStatus.CLOSED);
            }
        }

        public void OnChannelEOF() {
#if TRACE_RECEIVER
            System.Diagnostics.Debug.WriteLine("SFTP: EOF");
#endif
        }

        public void OnChannelError(Exception error) {
            lock (StatusChangeNotifier) {
#if TRACE_RECEIVER
                System.Diagnostics.Debug.WriteLine("SFTP: Error: " + error.Message);
#endif
                TransitStatus(SFTPChannelStatus.ERROR);
            }
        }

        public void OnChannelReady() {
            lock (StatusChangeNotifier) {
#if TRACE_RECEIVER
                System.Diagnostics.Debug.WriteLine("SFTP: OnChannelReady");
#endif
                TransitStatus(SFTPChannelStatus.READY);
            }
        }

        public void OnMiscPacket(byte packet_type, byte[] data, int offset, int length) {
#if DUMP_PACKET
            Dump("SFTP: OnMiscPacket: " + packet_type, data, offset, length);
#endif
        }

        #endregion

        #region Private methods

        private void TransitStatus(SFTPChannelStatus newStatus) {
            _channelStatus = newStatus;
            Monitor.PulseAll(StatusChangeNotifier);
        }

#if DUMP_PACKET
        // for debug
        private void Dump(string caption, byte[] data, int offset, int length) {
            StringBuilder s = new StringBuilder();
            s.AppendLine(caption);
            s.Append("0--1--2--3--4--5--6--7--8--9--A--B--C--D--E--F-");
            for (int i = 0; i < length; i++) {
                byte b = data[offset + i];
                int pos = i % 16;
                if (pos == 0)
                    s.AppendLine();
                else
                    s.Append(' ');
                s.Append("0123456789abcdef"[b >> 4]).Append("0123456789abcdef"[b & 0xf]);
            }
            s.AppendLine().Append("0--1--2--3--4--5--6--7--8--9--A--B--C--D--E--F-");
            System.Diagnostics.Debug.WriteLine(s);
        }
#endif

        #endregion
    }


}
