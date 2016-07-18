/*
 Copyright (c) 2016 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.
*/
using Granados.IO;
using Granados.IO.SSH1;
using Granados.SSH;
using Granados.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Granados.SSH1 {

    /// <summary>
    /// SSH1 abstract channel class. (base of the interactive-session and channels)
    /// </summary>
    internal abstract class SSH1ChannelBase : ISSHChannel {
        #region

        private readonly Action<ISSHChannel> _detachAction;
        private readonly SSH1Connection _connection;

        private ISSHChannelEventHandler _handler = new SimpleSSHChannelEventHandler();

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH1ChannelBase(
                Action<ISSHChannel> detachAction,
                SSH1Connection connection,
                uint localChannel,
                uint remoteChannel,
                ChannelType channelType,
                string channelTypeString) {

            _detachAction = detachAction;
            _connection = connection;
            LocalChannel = localChannel;
            RemoteChannel = remoteChannel;
            ChannelType = channelType;
            ChannelTypeString = channelTypeString;
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
        public abstract bool IsOpen {
            get;
        }

        /// <summary>
        /// true if this channel is ready for use.
        /// </summary>
        public abstract bool IsReady {
            get;
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
        public abstract void ResizeTerminal(uint width, uint height, uint pixelWidth, uint pixelHeight);

        /// <summary>
        /// Send data.
        /// </summary>
        /// <param name="data">data to send</param>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public abstract void Send(DataFragment data);

        /// <summary>
        /// Send EOF.
        /// </summary>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public abstract void SendEOF();

        /// <summary>
        /// Send Break. (SSH2, session channel only)
        /// </summary>
        /// <param name="breakLength">break-length in milliseconds</param>
        /// <returns>true if succeeded. false if the request failed.</returns>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public bool SendBreak(int breakLength) {
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
        public abstract void Close();

        #endregion

        /// <summary>
        /// Sets handler
        /// </summary>
        /// <param name="handler"></param>
        public void SetHandler(ISSHChannelEventHandler handler) {
            _handler = handler;
        }

        /// <summary>
        /// Process packet about this channel.
        /// </summary>
        /// <param name="packetType">a packet type (message number)</param>
        /// <param name="packetFragment">a packet image except message number and recipient channel.</param>
        public abstract void ProcessPacket(SSH1PacketType packetType, DataFragment packetFragment);

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="packet">packet object</param>
        protected void Transmit(SSH1Packet packet) {
            _connection.Transmit(packet);
        }

        /// <summary>
        /// Event handler object
        /// </summary>
        protected ISSHChannelEventHandler Handler {
            get {
                return _handler;
            }
        }

        /// <summary>
        /// Sets remote channel.
        /// </summary>
        /// <param name="remoteChannel">remote channel</param>
        protected void SetRemoteChannel(uint remoteChannel) {
            RemoteChannel = remoteChannel;
        }

        /// <summary>
        /// Detach this channel object
        /// </summary>
        protected void Detach() {
            _detachAction(this);
        }

        /// <summary>
        /// Outputs trace message
        /// </summary>
        protected void Trace(SSH1PacketType packetType, string message, params object[] args) {
            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(packetType, message, args);
            }
        }

        #endregion
    }

    /// <summary>
    /// SSH1 pseudo channel class for the interactive session.
    /// </summary>
    internal class SSH1InteractiveSession : SSH1ChannelBase {
        #region

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

        protected enum State {
            /// <summary>initial state</summary>
            Initial,
            /// <summary>SSH_CMSG_REQUEST_PTY has been sent. waiting SSH_SMSG_SUCCESS | SSH_SMSG_FAILURE.</summary>
            WaitStartPTYResponse,
            /// <summary>SSH_SMSG_SUCCESS has been received</summary>
            StartPTYSuccess,
            /// <summary>SSH_SMSG_FAILURE has been received</summary>
            StartPTYFailure,
            /// <summary>the interactive session has been established. more request may be requested.</summary>
            Established,
            /// <summary>the interactive session is ready for use</summary>
            Ready,
            /// <summary>closing has been requested</summary>
            Closing,
            /// <summary>the interactive session has been closed</summary>
            Closed,
        }

        private volatile State _state;
        private readonly object _stateSync = new object();

        private bool _eof = false;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH1InteractiveSession(
                Action<ISSHChannel> detachAction,
                SSH1Connection connection,
                uint localChannel,
                ChannelType channelType,
                string channelTypeString)
            : base(detachAction, connection, localChannel, 0, channelType, channelTypeString) {

            _state = State.Initial;
        }

        #region ISSHChannel properties

        /// <summary>
        /// true if this channel is open.
        /// </summary>
        public override bool IsOpen {
            get {
                return _state != State.Closed;
            }
        }

        /// <summary>
        /// true if this channel is ready for use.
        /// </summary>
        public override bool IsReady {
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
        public override void ResizeTerminal(uint width, uint height, uint pixelWidth, uint pixelHeight) {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_WINDOW_SIZE)
                        .WriteUInt32(height)
                        .WriteUInt32(width)
                        .WriteUInt32(pixelWidth)
                        .WriteUInt32(pixelHeight)
                );
            }

            Trace(SSH1PacketType.SSH_CMSG_WINDOW_SIZE, "width={0} height={1}", width, height);
        }

        /// <summary>
        /// Send data.
        /// </summary>
        /// <param name="data">data to send</param>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public override void Send(DataFragment data) {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_STDIN_DATA)
                        .WriteAsString(data.Data, data.Offset, data.Length)
                );
            }
        }

        /// <summary>
        /// Send EOF.
        /// </summary>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public override void SendEOF() {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_EOF)
                );

                _eof = true;
            }
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
        public override void Close() {
            // quick check for avoiding deadlock
            if (_state != State.Established && _state != State.Ready) {
                return;
            }

            lock (_stateSync) {
                if (_state != State.Established && _state != State.Ready) {
                    return;
                }

                if (!_eof) {
                    SendEOF();
                }

                SetStateClosed(false);
            }
        }

        #endregion

        /// <summary>
        /// Starts a shell
        /// </summary>
        /// <param name="param">connection parameters</param>
        /// <returns>true if shell has been started successfully</returns>
        public bool ExecShell(SSHConnectionParameter param) {
            if (!OpenPTY(param)) {
                return false;
            }

            lock (_stateSync) {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_EXEC_SHELL)
                );
                Trace(SSH1PacketType.SSH_CMSG_EXEC_SHELL, "");

                _state = State.Established;
                DataFragment empty = new DataFragment(0);
                Handler.OnEstablished(empty);

                _state = State.Ready;
                Handler.OnReady();
            }

            return true;
        }

        /// <summary>
        /// Starts a command
        /// </summary>
        /// <param name="param">connection parameters</param>
        /// <param name="command">command line to execute</param>
        public void ExecCommand(SSHConnectionParameter param, string command) {
            Task.Run(() => DoExecCommand(param, command));
        }

        /// <summary>
        /// Starts a command
        /// </summary>
        /// <param name="param">connection parameters</param>
        /// <param name="command">command line to execute</param>
        private void DoExecCommand(SSHConnectionParameter param, string command) {
            if (!OpenPTY(param)) {
                return;
            }
            
            lock (_stateSync) {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_EXEC_CMD)
                        .WriteString(command)
                );
                Trace(SSH1PacketType.SSH_CMSG_EXEC_CMD, "exec command: command={0}", command);

                _state = State.Established;
                DataFragment empty = new DataFragment(0);
                Handler.OnEstablished(empty);

                _state = State.Ready;
                Handler.OnReady();
            }
        }

        /// <summary>
        /// Open PTY
        /// </summary>
        /// <param name="param">connection parameters</param>
        /// <returns>true if pty has been opened successfully</returns>
        private bool OpenPTY(SSHConnectionParameter param) {
            lock (_stateSync) {
                if (_state != State.Initial) {
                    return false;
                }

                _state = State.WaitStartPTYResponse;
            }

            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_REQUEST_PTY)
                    .WriteString(param.TerminalName)
                    .WriteInt32(param.TerminalHeight)
                    .WriteInt32(param.TerminalWidth)
                    .WriteInt32(param.TerminalPixelWidth)
                    .WriteInt32(param.TerminalPixelHeight)
                    .Write(new byte[1]) //TTY_OP_END
            );
            Trace(SSH1PacketType.SSH_CMSG_REQUEST_PTY,
                "open shell: terminal={0} width={1} height={2}",
                param.TerminalName, param.TerminalWidth, param.TerminalHeight);

            DataFragment packet = null;
            if (!_receivedPacket.TryGet(ref packet, RESPONSE_TIMEOUT)) {
                RequestFailed();
                return false;
            }

            lock (_stateSync) {
                if (_state != State.StartPTYSuccess) {
                    RequestFailed();
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Changes state when the request was failed.
        /// </summary>
        private void RequestFailed() {
            Handler.OnRequestFailed();
            SetStateClosed(false);
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
                        Handler.OnClosing(byServer);
                    }

                    if (_state == State.Closing) {
                        _state = State.Closed;
                        Handler.OnClosed(byServer);
                    }

                    Detach();
                }
            }
        }

        /// <summary>
        /// Process packet about this channel.
        /// </summary>
        /// <param name="packetType">a packet type (message number)</param>
        /// <param name="packetFragment">a packet image except message number and recipient channel.</param>
        public override void ProcessPacket(SSH1PacketType packetType, DataFragment packetFragment) {
            if (_state == State.Closed) {
                return; // ignore
            }

            lock (_stateSync) {
                switch (_state) {
                    case State.Initial:
                        break;
                    case State.WaitStartPTYResponse:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _state = State.StartPTYSuccess;
                            _receivedPacket.TrySet(packetFragment, PASSING_TIMEOUT);
                        }
                        else if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _state = State.StartPTYFailure;
                            _receivedPacket.TrySet(packetFragment, PASSING_TIMEOUT);
                        }
                        break;
                    case State.Established:
                        break;
                    case State.Ready:
                        switch (packetType) {
                            case SSH1PacketType.SSH_SMSG_STDOUT_DATA: {
                                    SSH1DataReader reader = new SSH1DataReader(packetFragment);
                                    int len = reader.ReadInt32();
                                    DataFragment frag = reader.GetRemainingDataView(len);
                                    Handler.OnData(frag);
                                }
                                break;
                            case SSH1PacketType.SSH_SMSG_STDERR_DATA: {
                                    SSH1DataReader reader = new SSH1DataReader(packetFragment);
                                    int len = reader.ReadInt32();
                                    DataFragment frag = reader.GetRemainingDataView(len);
                                    Handler.OnData(frag);
                                }
                                break;
                            case SSH1PacketType.SSH_SMSG_EXITSTATUS: {
                                    Transmit(
                                        new SSH1Packet(SSH1PacketType.SSH_CMSG_EXIT_CONFIRMATION)
                                    );
                                    SetStateClosed(true);
                                }
                                break;
                            default: {
                                    Handler.OnUnhandledPacket((byte)packetType, packetFragment);
                                }
                                break;
                        }
                        break;  // case State.Ready
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// SSH1 channel base class.
    /// </summary>
    internal abstract class SSH1SubChannelBase : SSH1ChannelBase {
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

        private volatile State _state;
        private readonly object _stateSync = new object();

        /// <summary>
        /// Constructor (initiated by server)
        /// </summary>
        public SSH1SubChannelBase(
                Action<ISSHChannel> detachAction,
                SSH1Connection connection,
                uint localChannel,
                uint remoteChannel,
                ChannelType channelType,
                string channelTypeString)
            : base(detachAction, connection, localChannel, remoteChannel, channelType, channelTypeString) {

            _state = State.InitiatedByServer; // SendOpenConfirmation() will change state to "Opened"
        }

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH1SubChannelBase(
                Action<ISSHChannel> detachAction,
                SSH1Connection connection,
                uint localChannel,
                ChannelType channelType,
                string channelTypeString)
            : base(detachAction, connection, localChannel, 0, channelType, channelTypeString) {

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
        /// Sends SSH_MSG_CHANNEL_OPEN
        /// </summary>
        public void SendOpen() {
            Transmit(BuildOpenPacket());
        }

        /// <summary>
        /// Builds SSH_MSG_CHANNEL_OPEN packet
        /// </summary>
        protected abstract SSH1Packet BuildOpenPacket();

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
                    new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION)
                        .WriteUInt32(RemoteChannel)
                        .WriteUInt32(LocalChannel)
                );

                _state = State.Established;
                Handler.OnEstablished(new DataFragment(0));
                OnChannelEstablished();
            }
        }

        #region ISSHChannel properties

        /// <summary>
        /// true if this channel is open.
        /// </summary>
        public override bool IsOpen {
            get {
                return _state != State.Closed;
            }
        }

        /// <summary>
        /// true if this channel is ready for use.
        /// </summary>
        public override bool IsReady {
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
        public override void ResizeTerminal(uint width, uint height, uint pixelWidth, uint pixelHeight) {
            // do nothing
        }

        /// <summary>
        /// Send data.
        /// </summary>
        /// <param name="data">data to send</param>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public override void Send(DataFragment data) {
            lock (_stateSync) {
                if (_state == State.Closing || _state == State.Closed) {
                    throw new SSHChannelInvalidOperationException("Channel already closed");
                }

                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_DATA)
                        .WriteUInt32(RemoteChannel)
                        .WriteAsString(data.Data, data.Offset, data.Length)
                );
            }
        }

        /// <summary>
        /// Send EOF.
        /// </summary>
        /// <exception cref="SSHChannelInvalidOperationException">the channel is already closed.</exception>
        public override void SendEOF() {
            // do nothing
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
        public override void Close() {
            // quick check for avoiding deadlock
            if (_state != State.Established && _state != State.Ready) {
                return;
            }

            lock (_stateSync) {
                if (_state != State.Established && _state != State.Ready) {
                    return;
                }

                _state = State.Closing;
            }

            Handler.OnClosing(false);

            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_CLOSE)
                    .WriteUInt32(RemoteChannel)
            );
            Trace(SSH1PacketType.SSH_MSG_CHANNEL_CLOSE, "");
        }

        #endregion

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
                    Handler.OnReady();
                }
            }
        }

        /// <summary>
        /// The derived class can change state from "Established" to "Closing" by calling this method.
        /// </summary>
        protected void RequestFailed() {
            lock (_stateSync) {
                if (_state == State.Established) {
                    Handler.OnRequestFailed();
                    Close();
                }
                else if (_state == State.InitiatedByClient) {
                    Handler.OnRequestFailed();
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
                        Handler.OnClosing(byServer);
                    }

                    if (_state == State.Closing) {
                        _state = State.Closed;
                        Handler.OnClosed(byServer);
                    }

                    Detach();
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
        protected virtual SubPacketProcessResult ProcessPacketSub(SSH1PacketType packetType, DataFragment packetFragment) {
            // derived class can override this.
            return SubPacketProcessResult.NotConsumed;
        }

        /// <summary>
        /// Process packet about this channel.
        /// </summary>
        /// <param name="packetType">a packet type (message number)</param>
        /// <param name="packetFragment">a packet image except message number and recipient channel.</param>
        public override void ProcessPacket(SSH1PacketType packetType, DataFragment packetFragment) {
            if (_state == State.Closed) {
                return; // ignore
            }

            lock (_stateSync) {
                switch (_state) {
                    case State.InitiatedByServer:
                        break;
                    case State.InitiatedByClient:
                        if (packetType == SSH1PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION) {
                            SSH1DataReader reader = new SSH1DataReader(packetFragment);
                            SetRemoteChannel(reader.ReadUInt32());
                            _state = State.Established;
                            Handler.OnEstablished(reader.GetRemainingDataView());
                            OnChannelEstablished();
                            return;
                        }
                        if (packetType == SSH1PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE) {
                            RequestFailed();
                            return;
                        }
                        break;
                    case State.Closing:
                        if (packetType == SSH1PacketType.SSH_MSG_CHANNEL_CLOSE_CONFIRMATION) {
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
                            case SSH1PacketType.SSH_MSG_CHANNEL_DATA: {
                                    SSH1DataReader reader = new SSH1DataReader(packetFragment);
                                    int len = reader.ReadInt32();
                                    DataFragment frag = reader.GetRemainingDataView(len);
                                    Handler.OnData(frag);
                                }
                                break;
                            case SSH1PacketType.SSH_MSG_CHANNEL_CLOSE: {
                                    Transmit(
                                        new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_CLOSE_CONFIRMATION)
                                            .WriteUInt32(RemoteChannel)
                                    );
                                    SetStateClosed(true);
                                }
                                break;
                            default: {
                                    Handler.OnUnhandledPacket((byte)packetType, packetFragment);
                                }
                                break;
                        }
                        break;  // case State.Ready
                }
            }
            return;

        
        }

        #endregion
    }

    /// <summary>
    /// SSH1 channel operator for the local port forwarding.
    /// </summary>
    internal class SSH1LocalPortForwardingChannel : SSH1SubChannelBase {

        private const ChannelType CHANNEL_TYPE = ChannelType.ForwardedLocalToRemote;
        private const string CHANNEL_TYPE_STRING = "ForwardedLocalToRemote";

        private readonly string _remoteHost;
        private readonly uint _remotePort;
        private readonly string _originatorIp;
        private readonly uint _originatorPort;

        /// <summary>
        /// Constructor (initiated by client)
        /// </summary>
        public SSH1LocalPortForwardingChannel(
                Action<ISSHChannel> detachAction,
                SSH1Connection connection,
                uint localChannel,
                string remoteHost,
                uint remotePort,
                string originatorIp,
                uint originatorPort)
            : base(detachAction, connection, localChannel, CHANNEL_TYPE, CHANNEL_TYPE_STRING) {

            _remoteHost = remoteHost;
            _remotePort = remotePort;
            _originatorIp = originatorIp;
            _originatorPort = originatorPort;
        }

        /// <summary>
        /// Builds SSH_MSG_PORT_OPEN packet
        /// </summary>
        protected override SSH1Packet BuildOpenPacket() {
            return new SSH1Packet(SSH1PacketType.SSH_MSG_PORT_OPEN)
                    .WriteUInt32(LocalChannel)
                    .WriteString(_remoteHost)
                    .WriteUInt32(_remotePort);
            // Note:
            //  "originator" is specified only if both sides specified
            //  SSH_PROTOFLAG_HOST_IN_FWD_OPEN in the protocol flags.
            //  currently 0 is used as the protocol flags.
            //  
            //  .WriteString(_originatorIp + ":" + _originatorPort.ToString(NumberFormatInfo.InvariantInfo))
        }
    }

    /// <summary>
    /// SSH1 channel operator for the remote port forwarding.
    /// </summary>
    internal class SSH1RemotePortForwardingChannel : SSH1SubChannelBase {
        #region

        private const ChannelType CHANNEL_TYPE = ChannelType.ForwardedRemoteToLocal;
        private const string CHANNEL_TYPE_STRING = "ForwardedRemoteToLocal";

        /// <summary>
        /// Constructor (initiated by server)
        /// </summary>
        public SSH1RemotePortForwardingChannel(
                Action<ISSHChannel> detachAction,
                SSH1Connection connection,
                uint localChannel,
                uint remoteChannel)
            : base(detachAction, connection, localChannel, remoteChannel, CHANNEL_TYPE, CHANNEL_TYPE_STRING) {
        }

        /// <summary>
        /// Builds an open-channel packet.
        /// </summary>
        protected override SSH1Packet BuildOpenPacket() {
            // this method should not be called.
            throw new InvalidOperationException();
        }

        #endregion
    }
}
