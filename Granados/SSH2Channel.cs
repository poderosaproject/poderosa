/*
 Copyright (c) 2016 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.
*/
using Granados.IO;
using Granados.IO.SSH2;
using Granados.SSH;
using Granados.Util;
using System;
using System.Threading;

namespace Granados.SSH2 {

    /// <summary>
    /// SSH2 channel base class.
    /// </summary>
    internal class SSH2ChannelBase : ISSHChannel {
        #region

        protected enum State {
            /// <summary>channel has been requested by the server</summary>
            InitiatedByServer,
            /// <summary>channel has been requested by the client</summary>
            InitiatedByClient,
            /// <summary>channel has been established. more request may be requested.</summary>
            Established,
            /// <summary>channel is ready for use</summary>
            Ready,
            /// <summary>closing has been requested</summary>
            Closing,
            /// <summary>channel has been closed</summary>
            Closed,
        }

        protected enum SubPacketProcessResult {
            /// <summary>the packet was not consumed</summary>
            NotConsumed,
            /// <summary>the packet was consumed</summary>
            Consumed,
        }

        private readonly Action<ISSHChannel> _detachAction;
        private readonly SSH2Connection _connection;
        private readonly int _localMaxPacketSize;
        private readonly int _localWindowSize;
        private int _localWindowSizeLeft;
        private uint _serverMaxPacketSize;
        private uint _serverWindowSizeLeft;

        private volatile State _state;
        private readonly object _stateSync = new object();

        // channel request slot
        private volatile Action<bool> _channelRequestReplyCallback;
        private readonly object _channelRequestSync = new object();

        private ISSHChannelEventHandler _handler = new SimpleSSHChannelEventHandler();

        /// <summary>
        /// Constructor (initiated by server)
        /// </summary>
        public SSH2ChannelBase(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel,
                uint remoteChannel,
                ChannelType channelType,
                string channelTypeString,
                uint serverWindowSize,
                uint serverMaxPacketSize) {

            _detachAction = detachAction;
            _connection = connection;
            LocalChannel = localChannel;
            RemoteChannel = remoteChannel;
            ChannelType = channelType;
            ChannelTypeString = channelTypeString;

            _localMaxPacketSize = param.MaxPacketSize;
            _localWindowSize = _localWindowSizeLeft = param.WindowSize;
            _serverMaxPacketSize = serverMaxPacketSize;
            _serverWindowSizeLeft = serverWindowSize;

            _state = State.InitiatedByServer; // SendOpenConfirmation() will change state to "Opened"
        }

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH2ChannelBase(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel,
                ChannelType channelType,
                string channelTypeString) {

            _detachAction = detachAction;
            _connection = connection;
            LocalChannel = localChannel;
            RemoteChannel = 0;
            ChannelType = channelType;
            ChannelTypeString = channelTypeString;

            _localMaxPacketSize = param.MaxPacketSize;
            _localWindowSize = _localWindowSizeLeft = param.WindowSize;
            _serverMaxPacketSize = 0;
            _serverWindowSizeLeft = 0;

            _state = State.InitiatedByClient; // receiving SSH_MSG_CHANNEL_OPEN_CONFIRMATION will change state to "Opened"
        }

        /// <summary>
        /// Major state
        /// </summary>
        protected State MajorState {
            get {
                return _state;
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_REQUEST packet
        /// </summary>
        /// <param name="requestPacket">SSH_MSG_CHANNEL_REQUEST packet</param>
        /// <param name="resultCallback">
        /// an action that will be called when SSH_MSG_REQUEST_SUCCESS or SSH_MSG_REQUEST_FAILURE has been received.
        /// or null if no reply is wanted.
        /// </param>
        protected void SendRequest(SSH2Packet requestPacket, Action<bool> resultCallback) {
            lock (_channelRequestSync) {
                while (_channelRequestReplyCallback != null) {
                    Monitor.Wait(_channelRequestSync);
                }

                _channelRequestReplyCallback = resultCallback;

                Transmit(0, requestPacket);
            }
        }

        #region ISSHChannel properties

        /// <summary>
        /// Local channel number.
        /// </summary>
        public uint LocalChannel {
            get;
            private set;
        }

        /// <summary>
        /// Remote channel number.
        /// </summary>
        public uint RemoteChannel {
            get;
            private set;
        }

        /// <summary>
        /// Channel type. (predefined type)
        /// </summary>
        public ChannelType ChannelType {
            get;
            private set;
        }

        /// <summary>
        /// Channel type string. (actual channel type name)
        /// </summary>
        public string ChannelTypeString {
            get;
            private set;
        }

        /// <summary>
        /// true if this channel is open.
        /// </summary>
        public bool IsOpen {
            get {
                return _state != State.Closed;
            }
        }

        /// <summary>
        /// true if this channel is ready for use.
        /// </summary>
        public bool IsReady {
            get {
                return _state == State.Ready;
            }
        }

        #endregion

        #region ISSHChannel methods

        /// <summary>
        /// Send window dimension change message.
        /// </summary>
        /// <param name="width">terminal width, columns</param>
        /// <param name="height">terminal height, rows</param>
        /// <param name="pixelWidth">terminal width, pixels</param>
        /// <param name="pixelHeight">terminal height, pixels</param>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public void ResizeTerminal(uint width, uint height, uint pixelWidth, uint pixelHeight) {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                SendRequest(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                        .WriteUInt32(RemoteChannel)
                        .WriteString("window-change")
                        .WriteBool(false)
                        .WriteUInt32(width)
                        .WriteUInt32(height)
                        .WriteUInt32(pixelWidth)
                        .WriteUInt32(pixelHeight),

                    null    // no reply
                );
            }

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent("window-change", "width={0} height={1}", width, height);
            }
        }

        /// <summary>
        /// Send data.
        /// </summary>
        /// <param name="data">data to send</param>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public void Send(DataFragment data) {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                Transmit(
                    data.Length,
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_DATA)
                        .WriteUInt32(RemoteChannel)
                        .WriteAsString(data.Data, data.Offset, data.Length)
                );
            }
        }

        /// <summary>
        /// Send EOF.
        /// </summary>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public void SendEOF() {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                Transmit(
                    0,
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_EOF)
                        .WriteUInt32(RemoteChannel)
                );
            }

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_EOF, "");
            }
        }

        /// <summary>
        /// Send Break. (SSH2, session channel only)
        /// </summary>
        /// <param name="breakLength">break-length in milliseconds</param>
        /// <returns>true if succeeded. false if the request failed.</returns>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public virtual bool SendBreak(int breakLength) {
            // derived class can override this.
            return false;
        }

        /// <summary>
        /// Close this channel.
        /// </summary>
        /// <remarks>
        /// After calling this method, all mothods of the <see cref="ISSHChannel"/> will throw <see cref="SSHChannelInvalidOperationException"/>.
        /// </remarks>
        /// <remarks>
        /// If this method was called under the inappropriate channel state, the method call will be ignored silently.
        /// </remarks>
        public void Close() {
            lock (_stateSync) {
                if (_state != State.Established && _state != State.Ready) {
                    return;
                }

                _state = State.Closing;
                _handler.OnClosing(false);

                Transmit(
                    0,
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_CLOSE)
                        .WriteUInt32(RemoteChannel)
                );
            }

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_CLOSE, "");
            }
        }

        #endregion

        /// <summary>
        /// Sets handler
        /// </summary>
        /// <param name="handler"></param>
        public void SetHandler(ISSHChannelEventHandler handler) {
            _handler = handler;
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_OPEN
        /// </summary>
        public void SendOpen() {
            _connection.Transmit(BuildOpenPacket());
        }

        /// <summary>
        /// Builds SSH_MSG_CHANNEL_OPEN packet
        /// </summary>
        protected virtual SSH2Packet BuildOpenPacket() {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN)
                        .WriteString(ChannelTypeString)
                        .WriteUInt32(LocalChannel)
                        .WriteInt32(_localWindowSize)
                        .WriteInt32(_localMaxPacketSize);
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_OPEN_CONFIRMATION
        /// </summary>
        /// <exception cref="InvalidOperationException">inappropriate channel state</exception>
        public void SendOpenConfirmation() {
            lock (_stateSync) {
                if (_state != State.InitiatedByServer) {
                    throw new InvalidOperationException();
                }

                Transmit(
                    0,
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION)
                        .WriteUInt32(RemoteChannel)
                        .WriteUInt32(LocalChannel)
                        .WriteInt32(_localWindowSize)
                        .WriteInt32(_localMaxPacketSize)
                );

                _state = State.Established;
                _handler.OnEstablished(new DataFragment(0));
                OnChannelEstablished();
            }
        }

        /// <summary>
        /// Do additional work when <see cref="State"/> was changed to <see cref="State.Established"/>.
        /// </summary>
        /// <returns>true if the channel is ready for use.</returns>
        protected virtual void OnChannelEstablished() {
            // derived class can override this.
            SetStateReady();
        }

        /// <summary>
        /// The derived class can change state from "Established" to "Ready" by calling this method.
        /// </summary>
        protected void SetStateReady() {
            lock (_stateSync) {
                if (_state == State.Established) {
                    _state = State.Ready;
                    _handler.OnReady();
                }
            }
        }

        /// <summary>
        /// The derived class can change state from "Established" to "Closing" by calling this method.
        /// </summary>
        protected void RequestFailed() {
            lock (_stateSync) {
                if (_state == State.Established) {
                    _handler.OnRequestFailed();
                    Close();
                }
                else if (_state == State.InitiatedByClient) {
                    _handler.OnRequestFailed();
                    SetStateClosed(false);
                }
            }
        }

        /// <summary>
        /// Set state to "Closed".
        /// </summary>
        /// <param name="byServer"></param>
        private void SetStateClosed(bool byServer) {
            lock (_stateSync) {
                if (_state != State.Closed) {
                    if (_state != State.Closing) {
                        _state = State.Closing;
                        _handler.OnClosing(byServer);
                    }

                    if (_state == State.Closing) {
                        _state = State.Closed;
                        _handler.OnClosed(byServer);
                    }

                    _detachAction(this);
                }
            }
        }

        /// <summary>
        /// Process packet additionally.
        /// </summary>
        /// <remarks>
        /// This method will be called repeatedly while <see cref="State"/> is <see cref="State.Established"/> or <see cref="State.Ready"/>.
        /// </remarks>
        /// <param name="packetType">a packet type (message number)</param>
        /// <param name="packetFragment">a packet image except message number and recipient channel.</param>
        /// <returns>result</returns>
        protected virtual SubPacketProcessResult ProcessPacketSub(SSH2PacketType packetType, DataFragment packetFragment) {
            // derived class can override this.
            return SubPacketProcessResult.NotConsumed;
        }

        /// <summary>
        /// Process packet about this channel.
        /// </summary>
        /// <param name="packetType">a packet type (message number)</param>
        /// <param name="packetFragment">a packet image except message number and recipient channel.</param>
        public void ProcessPacket(SSH2PacketType packetType, DataFragment packetFragment) {
            if (_state == State.Closed) {
                return; // ignore
            }

            lock (_stateSync) {
                switch (_state) {
                    case State.InitiatedByServer:
                        break;
                    case State.InitiatedByClient:
                        if (packetType == SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION) {
                            SSH2DataReader reader = new SSH2DataReader(packetFragment);
                            RemoteChannel = reader.ReadUInt32();
                            _serverWindowSizeLeft = reader.ReadUInt32();
                            _serverMaxPacketSize = reader.ReadUInt32();

                            _state = State.Established;
                            _handler.OnEstablished(reader.GetRemainingDataView());
                            OnChannelEstablished();
                            return;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE) {
                            SSH2DataReader reader = new SSH2DataReader(packetFragment);
                            uint reasonCode = reader.ReadUInt32();
                            string description = reader.ReadUTF8String();
                            string lang = reader.ReadString();
                            RequestFailed();
                            return;
                        }
                        break;
                    case State.Closing:
                        if (packetType == SSH2PacketType.SSH_MSG_CHANNEL_CLOSE) {
                            SetStateClosed(false);
                            return;
                        }
                        break;
                    case State.Established:
                    case State.Ready:
                        if (ProcessPacketSub(packetType, packetFragment) == SubPacketProcessResult.Consumed) {
                            return;
                        }
                        switch (packetType) {
                            case SSH2PacketType.SSH_MSG_CHANNEL_DATA: {
                                    SSH2DataReader reader = new SSH2DataReader(packetFragment);
                                    int len = reader.ReadInt32();
                                    DataFragment frag = reader.GetRemainingDataView(len);
                                    AdjustWindowSize(len);
                                    _handler.OnData(frag);
                                }
                                break;
                            case SSH2PacketType.SSH_MSG_CHANNEL_EXTENDED_DATA: {
                                    SSH2DataReader reader = new SSH2DataReader(packetFragment);
                                    uint dataTypeCode = reader.ReadUInt32();
                                    int len = reader.ReadInt32();
                                    DataFragment frag = reader.GetRemainingDataView(len);
                                    AdjustWindowSize(len);
                                    _handler.OnExtendedData(dataTypeCode, frag);
                                }
                                break;
                            case SSH2PacketType.SSH_MSG_CHANNEL_REQUEST: {
                                    SSH2DataReader reader = new SSH2DataReader(packetFragment);
                                    string request = reader.ReadString();
                                    bool wantReply = reader.ReadBool();
                                    if (wantReply) { //we reject unknown requests including keep-alive check
                                        Transmit(
                                            0,
                                            new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_FAILURE)
                                                .WriteUInt32(RemoteChannel)
                                        );
                                    }
                                }
                                break;
                            case SSH2PacketType.SSH_MSG_CHANNEL_EOF: {
                                    _handler.OnEOF();
                                }
                                break;
                            case SSH2PacketType.SSH_MSG_CHANNEL_CLOSE: {
                                    Transmit(
                                        0,
                                        new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_CLOSE)
                                            .WriteUInt32(RemoteChannel)
                                    );
                                    SetStateClosed(true);
                                }
                                break;
                            case SSH2PacketType.SSH_MSG_CHANNEL_WINDOW_ADJUST: {
                                    SSH2DataReader reader = new SSH2DataReader(packetFragment);
                                    uint bytesToAdd = reader.ReadUInt32();
                                    // some servers may not send SSH_MSG_CHANNEL_WINDOW_ADJUST.
                                    // it is dangerous to wait this message in send procedure
                                    _serverWindowSizeLeft += bytesToAdd;
                                    if (_connection.IsEventTracerAvailable) {
                                        _connection.TraceReceptionEvent(SSH2PacketType.SSH_MSG_CHANNEL_WINDOW_ADJUST,
                                            "adjusted to {0} by increasing {1}", _serverWindowSizeLeft, bytesToAdd);
                                    }
                                }
                                break;
                            case SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS:
                            case SSH2PacketType.SSH_MSG_CHANNEL_FAILURE: {
                                    Action<bool> callback;
                                    lock (_channelRequestSync) {
                                        callback = _channelRequestReplyCallback;
                                        _channelRequestReplyCallback = null;
                                        Monitor.PulseAll(_channelRequestSync); // the next request can entry in the slot
                                    }
                                    if (callback != null) {
                                        callback(packetType == SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS);
                                    }
                                }
                                break;
                            default: {
                                    _handler.OnUnhandledPacket((byte)packetType, packetFragment);
                                }
                                break;
                        }
                        break;  // case State.Ready
                }
            }
        }

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="consumedSize">consumed window size.</param>
        /// <param name="packet">packet object</param>
        protected void Transmit(int consumedSize, SSH2Packet packet) {
            if (_serverWindowSizeLeft < (uint)consumedSize) {
                // FIXME: currently, window size on the remote side is totally ignored...
                _serverWindowSizeLeft = 0;
            }
            else {
                _serverWindowSizeLeft -= (uint)consumedSize;
            }

            _connection.Transmit(packet);
        }

        /// <summary>
        /// Adjust window size.
        /// </summary>
        /// <param name="dataLength">consumed length.</param>
        private void AdjustWindowSize(int dataLength) {
            _localWindowSizeLeft = Math.Max(_localWindowSizeLeft - dataLength, 0);

            if (_localWindowSizeLeft < _localWindowSize / 2) {
                Transmit(
                    0,
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_WINDOW_ADJUST)
                        .WriteUInt32(RemoteChannel)
                        .WriteInt32(_localWindowSize - _localWindowSizeLeft)
                );
                if (_connection.IsEventTracerAvailable) {
                    _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_WINDOW_ADJUST,
                        "adjusted window size : {0} --> {1}", _localWindowSizeLeft, _localWindowSize);
                }
                _localWindowSizeLeft = _localWindowSize;
            }
        }

        #endregion
    }

    /// <summary>
    /// SSH2 channel operator for the session.
    /// </summary>
    internal class SSH2SessionChannel : SSH2ChannelBase {
        #region

        private const ChannelType CHANNEL_TYPE = ChannelType.Session;
        private const string CHANNEL_TYPE_STRING = "session";

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH2SessionChannel(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel)
            : base(detachAction, connection, param, localChannel, CHANNEL_TYPE, CHANNEL_TYPE_STRING) {
        }

        /// <summary>
        /// Send Break. (SSH2, session channel only)
        /// </summary>
        /// <param name="breakLength">break-length in milliseconds</param>
        /// <returns>true if succeeded. false if the request failed.</returns>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public override bool SendBreak(int breakLength) {
            if (MajorState == State.Closing || MajorState == State.Closed) {
                throw new SSHChannelInvalidOperationException("Channel already closed");
            }

            const int RESPONCE_TIMEOUT = 10000;
            AtomicBox<bool> resultBox = new AtomicBox<bool>();

            SendRequest(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteUInt32(RemoteChannel)
                    .WriteString("break")
                    .WriteBool(true)
                    .WriteInt32(breakLength),

                success => {
                    resultBox.TrySet(success, 1000);
                }
            );

            bool resultValue = false;
            if (!resultBox.TryGet(ref resultValue, RESPONCE_TIMEOUT)) {
                return false;
            }
            return resultValue;
        }

        #endregion
    }

    /// <summary>
    /// SSH2 channel operator for the shell.
    /// </summary>
    internal class SSH2ShellChannel : SSH2SessionChannel {
        #region

        private enum MinorState {
            /// <summary>initial state</summary>
            NotReady,
            /// <summary>waiting SSH_MSG_CHANNEL_SUCCESS | SSH_MSG_CHANNEL_FAILURE for "pty-req" request</summary>
            WaitPtyReqConfirmation,
            /// <summary>waiting SSH_MSG_CHANNEL_SUCCESS | SSH_MSG_CHANNEL_FAILURE for "shell" request</summary>
            WaitShellConfirmation,
            /// <summary></summary>
            Ready,
        }

        private readonly SSHConnectionParameter _param;

        private volatile MinorState _state;
        private readonly object _stateSync = new object();

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH2ShellChannel(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel)
            : base(detachAction, connection, param, localChannel) {
            _param = param;
        }

        /// <summary>
        /// Do additional work when <see cref="SSH2ChannelBase.State"/> was changed to <see cref="SSH2ChannelBase.State.Established"/>.
        /// </summary>
        /// <returns>true if the channel is ready for use.</returns>
        protected override void OnChannelEstablished() {
            lock (_stateSync) {
                if (_state != MinorState.NotReady) {
                    return;
                }

                SendPtyRequest();
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_REQUEST "pty-req"
        /// </summary>
        private void SendPtyRequest() {
            lock (_stateSync) {
                _state = MinorState.WaitPtyReqConfirmation;
            }

            SendRequest(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteUInt32(RemoteChannel)
                    .WriteString("pty-req")
                    .WriteBool(true)
                    .WriteString(_param.TerminalName)
                    .WriteInt32(_param.TerminalWidth)
                    .WriteInt32(_param.TerminalHeight)
                    .WriteInt32(_param.TerminalPixelWidth)
                    .WriteInt32(_param.TerminalPixelHeight)
                    .WriteAsString(new byte[0]),

                success => {
                    lock (_stateSync) {
                        if (_state == MinorState.WaitPtyReqConfirmation) {
                            if (success) {
                                SendShellRequest();
                            }
                            else {
                                _state = MinorState.NotReady;
                                RequestFailed();
                            }
                        }
                    }
                }
            );
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_REQUEST "shell"
        /// </summary>
        private void SendShellRequest() {
            lock (_stateSync) {
                _state = MinorState.WaitShellConfirmation;
            }

            SendRequest(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteUInt32(RemoteChannel)
                    .WriteString("shell")
                    .WriteBool(true),

                success => {
                    lock (_stateSync) {
                        if (_state == MinorState.WaitShellConfirmation) {
                            if (success) {
                                _state = MinorState.Ready;
                                SetStateReady();
                            }
                            else {
                                _state = MinorState.NotReady;
                                RequestFailed();
                            }
                        }
                    }
                }
            );
        }

        #endregion
    }

    /// <summary>
    /// SSH2 channel operator for the command execution.
    /// </summary>
    internal class SSH2ExecChannel : SSH2SessionChannel {
        #region

        private enum MinorState {
            /// <summary>initial state</summary>
            NotReady,
            /// <summary>waiting SSH_MSG_CHANNEL_SUCCESS | SSH_MSG_CHANNEL_FAILURE for "exec" request</summary>
            WaitExecConfirmation,
            /// <summary></summary>
            Ready,
        }

        private readonly string _command;

        private volatile MinorState _state;
        private readonly object _stateSync = new object();

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH2ExecChannel(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel,
                string command)
            : base(detachAction, connection, param, localChannel) {

            _command = command;
        }

        /// <summary>
        /// Do additional work when <see cref="SSH2ChannelBase.State"/> was changed to <see cref="SSH2ChannelBase.State.Established"/>.
        /// </summary>
        /// <returns>true if the channel is ready for use.</returns>
        protected override void OnChannelEstablished() {
            lock (_stateSync) {
                if (_state != MinorState.NotReady) {
                    return;
                }

                SendExecRequest();
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_REQUEST "exec"
        /// </summary>
        private void SendExecRequest() {
            lock (_stateSync) {
                _state = MinorState.WaitExecConfirmation;
            }

            SendRequest(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteUInt32(RemoteChannel)
                    .WriteString("exec")
                    .WriteBool(true)
                    .WriteString(_command),

                success => {
                    lock (_stateSync) {
                        if (_state == MinorState.WaitExecConfirmation) {
                            if (success) {
                                _state = MinorState.Ready;
                                SetStateReady();
                            }
                            else {
                                _state = MinorState.NotReady;
                                RequestFailed();
                            }
                        }
                    }
                }
            );
        }

        #endregion
    }

    /// <summary>
    /// SSH2 channel operator for the subsystem.
    /// </summary>
    internal class SSH2SubsystemChannel : SSH2SessionChannel {
        #region

        private enum MinorState {
            /// <summary>initial state</summary>
            NotReady,
            /// <summary>waiting SSH_MSG_CHANNEL_SUCCESS | SSH_MSG_CHANNEL_FAILURE for "subsystem" request</summary>
            WaitSubsystemConfirmation,
            /// <summary></summary>
            Ready,
        }

        private readonly string _subsystemName;

        private volatile MinorState _state;
        private readonly object _stateSync = new object();

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH2SubsystemChannel(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel,
                string subsystemName)
            : base(detachAction, connection, param, localChannel) {

            _subsystemName = subsystemName;
        }

        /// <summary>
        /// Do additional work when <see cref="SSH2ChannelBase.State"/> was changed to <see cref="SSH2ChannelBase.State.Established"/>.
        /// </summary>
        /// <returns>true if the channel is ready for use.</returns>
        protected override void OnChannelEstablished() {
            lock (_stateSync) {
                if (_state != MinorState.NotReady) {
                    return;
                }

                SendSubsystemRequest();
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_REQUEST "subsystem"
        /// </summary>
        private void SendSubsystemRequest() {
            lock (_stateSync) {
                _state = MinorState.WaitSubsystemConfirmation;
            }

            SendRequest(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteUInt32(RemoteChannel)
                    .WriteString("subsystem")
                    .WriteBool(true)
                    .WriteString(_subsystemName),

                success => {
                    lock (_stateSync) {
                        if (_state == MinorState.WaitSubsystemConfirmation) {
                            if (success) {
                                _state = MinorState.Ready;
                                SetStateReady();
                            }
                            else {
                                _state = MinorState.NotReady;
                                RequestFailed();
                            }
                        }
                    }
                }
            );
        }

        #endregion
    }

    /// <summary>
    /// SSH2 channel operator for the local port forwarding.
    /// </summary>
    internal class SSH2LocalPortForwardingChannel : SSH2ChannelBase {
        #region

        private const ChannelType CHANNEL_TYPE = ChannelType.ForwardedLocalToRemote;
        private const string CHANNEL_TYPE_STRING = "direct-tcpip";

        private readonly int _localWindowSize;
        private readonly int _localMaxPacketSize;
        private readonly string _remoteHost;
        private readonly uint _remotePort;
        private readonly string _originatorIp;
        private readonly uint _originatorPort;

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH2LocalPortForwardingChannel(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel,
                string remoteHost,
                uint remotePort,
                string originatorIp,
                uint originatorPort)
            : base(detachAction, connection, param, localChannel, CHANNEL_TYPE, CHANNEL_TYPE_STRING) {

            _localWindowSize = param.WindowSize;
            _localMaxPacketSize = param.WindowSize;
            _remoteHost = remoteHost;
            _remotePort = remotePort;
            _originatorIp = originatorIp;
            _originatorPort = originatorPort;
        }

        /// <summary>
        /// Builds SSH_MSG_CHANNEL_OPEN packet
        /// </summary>
        protected override SSH2Packet BuildOpenPacket() {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN)
                    .WriteString("direct-tcpip")
                    .WriteUInt32(LocalChannel)
                    .WriteInt32(_localWindowSize)
                    .WriteInt32(_localMaxPacketSize)
                    .WriteString(_remoteHost)
                    .WriteUInt32(_remotePort)
                    .WriteString(_originatorIp)
                    .WriteUInt32(_originatorPort);
        }

        #endregion
    }

    /// <summary>
    /// SSH2 channel operator for the remote port forwarding.
    /// </summary>
    internal class SSH2RemotePortForwardingChannel : SSH2ChannelBase {
        #region

        private const ChannelType CHANNEL_TYPE = ChannelType.ForwardedRemoteToLocal;
        private const string CHANNEL_TYPE_STRING = "forwarded-tcpip";

        /// <summary>
        /// Constructor (initiated by server)
        /// </summary>
        public SSH2RemotePortForwardingChannel(
                Action<ISSHChannel> detachAction,
                SSH2Connection connection,
                SSHConnectionParameter param,
                uint localChannel,
                uint remoteChannel,
                uint serverWindowSize,
                uint serverMaxPacketSize)
            : base(detachAction, connection, param, localChannel, remoteChannel, CHANNEL_TYPE, CHANNEL_TYPE_STRING, serverWindowSize, serverMaxPacketSize) {
        }

        #endregion
    }
}
