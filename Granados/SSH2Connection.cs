// Copyright (c) 2005-2017 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using Granados.AgentForwarding;
using Granados.Crypto;
using Granados.DH;
using Granados.ECDH;
using Granados.IO;
using Granados.IO.SSH2;
using Granados.KeyboardInteractive;
using Granados.Mono.Math;
using Granados.PKI;
using Granados.PortForwarding;
using Granados.SSH;
using Granados.Util;
using Granados.X11;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Granados.SSH2 {

    /// <summary>
    /// SSH2 connection
    /// </summary>
    internal class SSH2Connection : ISSHConnection, IDisposable {
        private readonly IGranadosSocket _socket;
        private readonly ISSHConnectionEventHandler _eventHandler;
        private readonly SocketStatusReader _socketStatusReader;

        private readonly SSHConnectionParameter _param;
        private readonly SSHTimeouts _timeouts;
        private readonly SSHProtocolEventManager _protocolEventManager;

        private readonly SSHChannelCollection _channelCollection;
        private readonly SSH2Packetizer _packetizer;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSHPacketInterceptorCollection _packetInterceptors;
        private readonly SSH2KeyExchanger _keyExchanger;

        private readonly X11ConnectionManager _x11ConnectionManager;

        private readonly Lazy<SSH2RemotePortForwarding> _remotePortForwarding;
        private readonly Lazy<SSH2OpenSSHAgentForwarding> _agentForwarding;
        private readonly Lazy<SSH2X11Forwarding> _x11Forwarding;

        //server info
        private readonly SSH2ConnectionInfo _connectionInfo;

        private byte[] _sessionID = null;
        private AuthenticationStatus _authenticationStatus = AuthenticationStatus.NotStarted;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket wrapper</param>
        /// <param name="param">connection parameter</param>
        /// <param name="serverVersion">server version</param>
        /// <param name="clientVersion">client version</param>
        /// <param name="connectionEventHandlerCreator">a factory function to create a connection event handler (can be null)</param>
        /// <param name="protocolEventLoggerCreator">a factory function to create a protocol log event handler (can be null)</param>
        internal SSH2Connection(
                    PlainSocket socket,
                    SSHConnectionParameter param,
                    string serverVersion,
                    string clientVersion,
                    Func<ISSHConnection, ISSHConnectionEventHandler> connectionEventHandlerCreator,
                    Func<ISSHConnection, ISSHProtocolEventLogger> protocolEventLoggerCreator) {

            _socket = socket;

            var connEventHandler = connectionEventHandlerCreator != null ? connectionEventHandlerCreator(this) : null;
            if (connEventHandler != null) {
                _eventHandler = new SSHConnectionEventHandlerIgnoreErrorWrapper(connEventHandler);
            }
            else {
                _eventHandler = new SimpleSSHConnectionEventHandler();
            }

            _protocolEventManager = new SSHProtocolEventManager(
                            protocolEventLoggerCreator != null ?
                                protocolEventLoggerCreator(this) : null);

            _protocolEventManager.Trace("server version-string : {0}", serverVersion);

            _socketStatusReader = new SocketStatusReader(socket);
            _param = param.Clone();
            _timeouts = (_param.Timeouts != null) ? _param.Timeouts : new SSHTimeouts();
            _channelCollection = new SSHChannelCollection();
            _connectionInfo = new SSH2ConnectionInfo(param.HostName, param.PortNumber, serverVersion, clientVersion);
            IDataHandler adapter =
                new DataHandlerAdapter(
                    onData:
                        ProcessPacket,
                    onClosed:
                        OnConnectionClosed,
                    onError:
                        OnError
                );
            _syncHandler = new SSH2SynchronousPacketHandler(socket, adapter, _protocolEventManager);
            _packetizer = new SSH2Packetizer(_syncHandler);

            _packetInterceptors = new SSHPacketInterceptorCollection();

            _packetInterceptors.Add(new SSH2ExtInfo(_protocolEventManager, _connectionInfo));

            _keyExchanger = new SSH2KeyExchanger(_timeouts, _syncHandler, param, _protocolEventManager, _connectionInfo, UpdateKey);
            _packetInterceptors.Add(_keyExchanger);

            _x11ConnectionManager = (param.X11ForwardingParams != null) ? new X11ConnectionManager(_protocolEventManager) : null;

            _remotePortForwarding = new Lazy<SSH2RemotePortForwarding>(CreateRemotePortForwarding);
            _agentForwarding = new Lazy<SSH2OpenSSHAgentForwarding>(CreateAgentForwarding);
            _x11Forwarding = new Lazy<SSH2X11Forwarding>(CreateX11Forwarding);

            // set packetizer as a socket data handler
            socket.SetHandler(_packetizer);
        }

        /// <summary>
        /// Lazy initialization of the <see cref="SSH2RemotePortForwarding"/>.
        /// </summary>
        /// <returns>an instance of <see cref="SSH2RemotePortForwarding"/></returns>
        private SSH2RemotePortForwarding CreateRemotePortForwarding() {
            var instance = new SSH2RemotePortForwarding(_timeouts, _syncHandler, _protocolEventManager);
            _packetInterceptors.Add(instance);
            return instance;
        }

        /// <summary>
        /// Lazy initialization of the <see cref="SSH2OpenSSHAgentForwarding"/>.
        /// </summary>
        /// <returns>an instance of <see cref="SSH2OpenSSHAgentForwarding"/></returns>
        private SSH2OpenSSHAgentForwarding CreateAgentForwarding() {
            var instance = new SSH2OpenSSHAgentForwarding(
                                syncHandler:
                                    _syncHandler,
                                authKeyProvider:
                                    _param.AgentForwardingAuthKeyProvider,
                                protocolEventManager:
                                    _protocolEventManager,
                                createChannel:
                                    (remoteChannel, serverWindowSize, serverMaxPacketSize) => {
                                        uint localChannel = _channelCollection.GetNewChannelNumber();
                                        return new SSH2OpenSSHAgentForwardingChannel(
                                                    _timeouts,
                                                    _syncHandler,
                                                    _param,
                                                    _protocolEventManager,
                                                    localChannel,
                                                    remoteChannel,
                                                    serverWindowSize,
                                                    serverMaxPacketSize);
                                    },
                                registerChannel:
                                    RegisterChannel
                            );
            _packetInterceptors.Add(instance);
            return instance;
        }

        /// <summary>
        /// Lazy initialization of the <see cref="SSH2X11Forwarding"/>.
        /// </summary>
        /// <returns>an instance of <see cref="SSH2X11Forwarding"/></returns>
        private SSH2X11Forwarding CreateX11Forwarding() {
            var instance = new SSH2X11Forwarding(
                            syncHandler:
                                _syncHandler,
                            protocolEventManager:
                                _protocolEventManager,
                            x11ConnectionManager:
                                _x11ConnectionManager,
                            createChannel:
                                (remoteChannel, serverWindowSize, serverMaxPacketSize) => {
                                    uint localChannel = _channelCollection.GetNewChannelNumber();
                                    return new SSH2X11ForwardingChannel(
                                                _timeouts,
                                                _syncHandler,
                                                _param,
                                                _protocolEventManager,
                                                localChannel,
                                                remoteChannel,
                                                serverWindowSize,
                                                serverMaxPacketSize);
                                },
                            registerChannel:
                                RegisterChannel
                        );
            _packetInterceptors.Add(instance);
            return instance;
        }

        /// <summary>
        /// SSH protocol (SSH1 or SSH2)
        /// </summary>
        public SSHProtocol SSHProtocol {
            get {
                return SSHProtocol.SSH2;
            }
        }

        /// <summary>
        /// Connection parameter
        /// </summary>
        public SSHConnectionParameter ConnectionParameter {
            get {
                return _param;
            }
        }

        /// <summary>
        /// A property that indicates whether this connection is open.
        /// </summary>
        public bool IsOpen {
            get {
                return _socket.SocketStatus == SocketStatus.Ready && _authenticationStatus == AuthenticationStatus.Success;
            }
        }

        /// <summary>
        /// Authenticatrion status
        /// </summary>
        public AuthenticationStatus AuthenticationStatus {
            get {
                return _authenticationStatus;
            }
        }

        /// <summary>
        /// A proxy object for reading status of the underlying <see cref="IGranadosSocket"/> object.
        /// </summary>
        public SocketStatusReader SocketStatusReader {
            get {
                return _socketStatusReader;
            }
        }

        /// <summary>
        /// Implements <see cref="IDisposable"/>.
        /// </summary>
        public void Dispose() {
            if (_socket is IDisposable) {
                ((IDisposable)_socket).Dispose();
            }
        }

        /// <summary>
        /// Establishes a SSH connection to the server.
        /// </summary>
        /// <exception cref="SSHException">error</exception>
        internal void Connect() {
            if (_authenticationStatus != AuthenticationStatus.NotStarted) {
                throw new SSHException("Connect() was called twice.");
            }

            try {
                //key exchange
                _keyExchanger.ExecKeyExchange();

                //user authentication
                SSH2UserAuthentication userAuthentication =
                    new SSH2UserAuthentication(this, _connectionInfo, _param, _protocolEventManager, _timeouts, _syncHandler, _sessionID);
                if (_param.AuthenticationType == AuthenticationType.KeyboardInteractive) {
                    userAuthentication.KeyboardInteractiveAuthenticationFinished +=
                        result => {
                            _authenticationStatus = result ? AuthenticationStatus.Success : AuthenticationStatus.Failure;
                        };
                }
                _packetInterceptors.Add(userAuthentication);
                userAuthentication.ExecAuthentication();

                if (_param.AuthenticationType == AuthenticationType.KeyboardInteractive) {
                    _authenticationStatus = AuthenticationStatus.NeedKeyboardInput;
                }
                else {
                    _authenticationStatus = AuthenticationStatus.Success;
                }
            }
            catch (Exception) {
                _authenticationStatus = AuthenticationStatus.Failure;
                Close();
                throw;
            }
        }

        /// <summary>
        /// Sends a disconnect message to the server, then closes this connection.
        /// </summary>
        /// <param name="reasonCode">reason code (this value is ignored on the SSH1 connection)</param>
        /// <param name="message">a message to be notified to the server</param>
        public void Disconnect(DisconnectionReasonCode reasonCode, string message) {
            if (this.IsOpen) {
                _syncHandler.SendDisconnect(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_DISCONNECT)
                        .WriteInt32((int)reasonCode)
                        .WriteString(message)
                        .WriteString("") //language
                );
            }
            Close();
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <remarks>
        /// This method closes the underlying socket object.
        /// </remarks>
        public void Close() {
            if (_socket.SocketStatus == SocketStatus.Closed || _socket.SocketStatus == SocketStatus.RequestingClose) {
                return;
            }
            _socket.Close();
        }

        /// <summary>
        /// Sends version information to the server.
        /// </summary>
        internal void SendMyVersion() {
            string cv = _connectionInfo.ClientVersionString;
            string cv2 = cv + _param.VersionEOL;
            byte[] data = Encoding.ASCII.GetBytes(cv2);
            _socket.Write(data, 0, data.Length);
            _protocolEventManager.Trace("client version-string : {0}", cv);
        }

        /// <summary>
        /// Opens shell channel.
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/></returns>
        public THandler OpenShell<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator)
                where THandler : ISSHChannelEventHandler {

            if (_x11ConnectionManager != null && !_x11ConnectionManager.SetupDone) {
                _x11ConnectionManager.Setup(_param.X11ForwardingParams);
                var x11Forwarding = _x11Forwarding.Value;   // create instance
            }

            if (_param.AgentForwardingAuthKeyProvider != null) {
                var agentForwarding = _agentForwarding.Value;   // create instance
            }


            return CreateChannelByClient(
                        handlerCreator:
                            handlerCreator,
                        channelCreator:
                            localChannel =>
                                new SSH2ShellChannel(
                                    _timeouts, _syncHandler, _param, _protocolEventManager, localChannel, _x11ConnectionManager)
                    );
        }

        /// <summary>
        /// Opens execute-command channel
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <param name="command">command to execute</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/></returns>
        public THandler ExecCommand<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string command)
                where THandler : ISSHChannelEventHandler {

            return CreateChannelByClient(
                        handlerCreator:
                            handlerCreator,
                        channelCreator:
                            localChannel =>
                                new SSH2ExecChannel(_timeouts, _syncHandler, _param, _protocolEventManager, localChannel, command)
                    );
        }

        /// <summary>
        /// Opens subsystem channel (SSH2 only)
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <param name="subsystemName">subsystem name</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/>.</returns>
        public THandler OpenSubsystem<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string subsystemName)
                where THandler : ISSHChannelEventHandler {

            return CreateChannelByClient(
                        handlerCreator:
                            handlerCreator,
                        channelCreator:
                            localChannel =>
                                new SSH2SubsystemChannel(
                                    _timeouts, _syncHandler, _param, _protocolEventManager, localChannel, subsystemName)
                    );
        }

        /// <summary>
        /// Opens local port forwarding channel
        /// </summary>
        /// <typeparam name="THandler">type of the channel event handler</typeparam>
        /// <param name="handlerCreator">a function that creates a channel event handler</param>
        /// <param name="remoteHost">the host to connect to</param>
        /// <param name="remotePort">the port number to connect to</param>
        /// <param name="originatorIp">originator's IP address</param>
        /// <param name="originatorPort">originator's port number</param>
        /// <returns>a new channel event handler which was created by <paramref name="handlerCreator"/>.</returns>
        public THandler ForwardPort<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string remoteHost, uint remotePort, string originatorIp, uint originatorPort)
                where THandler : ISSHChannelEventHandler {

            return CreateChannelByClient(
                        handlerCreator:
                            handlerCreator,
                        channelCreator:
                            localChannel =>
                                new SSH2LocalPortForwardingChannel(
                                    _timeouts, _syncHandler, _param, _protocolEventManager, localChannel,
                                    remoteHost, remotePort, originatorIp, originatorPort)
                    );
        }

        /// <summary>
        /// Requests the remote port forwarding.
        /// </summary>
        /// <param name="requestHandler">a handler that handles the port forwarding requests from the server</param>
        /// <param name="addressToBind">address to bind on the server</param>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <returns>true if the request has been accepted, otherwise false.</returns>
        public bool ListenForwardedPort(IRemotePortForwardingHandler requestHandler, string addressToBind, uint portNumberToBind) {

            return _remotePortForwarding.Value.ListenForwardedPort(
                        requestHandler:
                            requestHandler,
                        createChannel:
                            (requestInfo, remoteChannel, serverWindowSize, serverMaxPacketSize) => {
                                uint localChannel = _channelCollection.GetNewChannelNumber();
                                return new SSH2RemotePortForwardingChannel(
                                            _timeouts,
                                            _syncHandler,
                                            _param,
                                            _protocolEventManager,
                                            localChannel,
                                            remoteChannel,
                                            serverWindowSize,
                                            serverMaxPacketSize
                                        );
                            },
                        registerChannel:
                            RegisterChannel,
                        addressToBind:
                            addressToBind,
                        portNumberToBind:
                            portNumberToBind
                    );
        }

        /// <summary>
        /// Cancels the remote port forwarding. (SSH2 only)
        /// </summary>
        /// <param name="addressToBind">address to bind on the server</param>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <returns>true if the remote port forwarding has been cancelled, otherwise false.</returns>
        public bool CancelForwardedPort(string addressToBind, uint portNumberToBind) {
            return _remotePortForwarding.Value.CancelForwardedPort(addressToBind, portNumberToBind);
        }

        /// <summary>
        /// Sends ignorable data
        /// </summary>
        /// <param name="message">a message to be sent. the server may record this message into the log.</param>
        public void SendIgnorableData(string message) {
            Transmit(
                 new SSH2Packet(SSH2PacketType.SSH_MSG_IGNORE)
                     .WriteString(message)
             );
        }

        /// <summary>
        /// Creates a new channel (initialted by the client)
        /// </summary>
        /// <typeparam name="TChannel">type of the channel object</typeparam>
        /// <typeparam name="THandler">type of the event handler</typeparam>
        /// <param name="handlerCreator">function to create an event handler</param>
        /// <param name="channelCreator">function to create a channel object</param>
        /// <returns></returns>
        private THandler CreateChannelByClient<TChannel, THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, Func<uint, TChannel> channelCreator)
            where TChannel : SSH2ChannelBase
            where THandler : ISSHChannelEventHandler {

            uint localChannel = _channelCollection.GetNewChannelNumber();
            var channel = channelCreator(localChannel);
            var eventHandler = handlerCreator(channel);

            RegisterChannel(channel, eventHandler);

            try {
                channel.SendOpen();
            }
            catch (Exception) {
                DetachChannel(channel);
                throw;
            }

            return eventHandler;
        }

        /// <summary>
        /// Registers a new channel to this connection.
        /// </summary>
        /// <param name="channel">a channel object</param>
        /// <param name="eventHandler">an event handler</param>
        private void RegisterChannel(SSH2ChannelBase channel, ISSHChannelEventHandler eventHandler) {
            channel.SetHandler(eventHandler);
            channel.Died += DetachChannel;
            _channelCollection.Add(channel, eventHandler);
        }

        /// <summary>
        /// Detach channel object.
        /// </summary>
        /// <param name="channelOperator">a channel operator</param>
        private void DetachChannel(ISSHChannel channelOperator) {
            var handler = _channelCollection.FindHandler(channelOperator.LocalChannel);
            _channelCollection.Remove(channelOperator);
            if (handler != null) {
                handler.Dispose();
            }
        }

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="packet">packet to send</param>
        private void Transmit(SSH2Packet packet) {
            _syncHandler.Send(packet);
        }

        /// <summary>
        /// Processes a received packet.
        /// </summary>
        /// <param name="packet">a received packet</param>
        private void ProcessPacket(DataFragment packet) {
            try {
                DoProcessPacket(packet);
            }
            catch (Exception ex) {
                _eventHandler.OnError(ex);
            }
        }

        /// <summary>
        /// Processes a received packet.
        /// </summary>
        /// <param name="packet">a received packet</param>
        private void DoProcessPacket(DataFragment packet) {
            if (_packetInterceptors.InterceptPacket(packet)) {
                return;
            }

            if (packet.Length < 1) {
                return; // invalid packet
            }

            SSH2DataReader reader = new SSH2DataReader(packet);
            SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

            if (packetType >= SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION && packetType <= SSH2PacketType.SSH_MSG_CHANNEL_FAILURE) {
                uint localChannel = reader.ReadUInt32();
                var channel = _channelCollection.FindOperator(localChannel) as SSH2ChannelBase;
                if (channel != null) {
                    channel.ProcessPacket(packetType, reader.GetRemainingDataView());
                }
                else {
                    Debug.WriteLine("unexpected channel pt=" + packetType + " local_channel=" + localChannel.ToString());
                }
                return;
            }

            switch (packetType) {
                case SSH2PacketType.SSH_MSG_DISCONNECT: {
                        int errorcode = reader.ReadInt32();
                        _eventHandler.OnConnectionClosed();
                    }
                    return;
                case SSH2PacketType.SSH_MSG_IGNORE: {
                        _eventHandler.OnIgnoreMessage(reader.ReadByteString());
                    }
                    return;
                case SSH2PacketType.SSH_MSG_DEBUG: {
                        bool alwaysDisplay = reader.ReadBool();
                        string message = reader.ReadUTF8String();
                        string languageTag = reader.ReadString();
                        _protocolEventManager.Trace("SSH_MSG_DEBUG : \"{0}\" (alwaysDisplay={1}, languageTag=\"{2}\")", message, alwaysDisplay, languageTag);
                        _eventHandler.OnDebugMessage(alwaysDisplay, message);
                    }
                    return;
                case SSH2PacketType.SSH_MSG_GLOBAL_REQUEST: {
                        string requestName = reader.ReadString();
                        bool wantReply = reader.ReadBool();
                        _protocolEventManager.Trace("Unhandled request: name={0} wantReply={1}", requestName, wantReply);
                        if (wantReply) {
                            Transmit(
                                new SSH2Packet(SSH2PacketType.SSH_MSG_REQUEST_FAILURE)
                            );
                        }
                    }
                    return;
                case SSH2PacketType.SSH_MSG_CHANNEL_OPEN: { // unhandled channel-open request
                        string channelType = reader.ReadString();
                        uint remoteChannel = reader.ReadUInt32();
                        _protocolEventManager.Trace("Unhandled channel open: channelType={0} remoteChannel={1}", channelType, remoteChannel);
                        _syncHandler.Send(
                            new SSH2ChannelOpenFailurePacket(
                                remoteChannel,
                                "Unknown channel type",
                                SSH2ChannelOpenFailureCode.SSH_OPEN_UNKNOWN_CHANNEL_TYPE
                            )
                        );
                    }
                    return;
            }

            _eventHandler.OnUnhandledMessage((byte)packetType, packet.GetBytes());
        }

        /// <summary>
        /// Tasks to do when the underlying socket has been closed. 
        /// </summary>
        private void OnConnectionClosed() {
            _channelCollection.ForEach((channel, handler) => handler.OnConnectionLost());
            _packetInterceptors.ForEach(interceptor => interceptor.OnConnectionClosed());
            _eventHandler.OnConnectionClosed();
        }

        /// <summary>
        /// Tasks to do when the underlying socket raised an exception.
        /// </summary>
        private void OnError(Exception error) {
            _channelCollection.ForEach((channel, handler) => channel.AbortWaitReady());
            _eventHandler.OnError(error);
        }

        /// <summary>
        /// Updates cipher settings.
        /// </summary>
        /// <param name="sessionID">session ID</param>
        /// <param name="cipherServer"></param>
        /// <param name="cipherClient"></param>
        /// <param name="macServer"></param>
        /// <param name="macClient"></param>
        private void UpdateKey(byte[] sessionID, Cipher cipherServer, Cipher cipherClient, MAC macServer, MAC macClient) {
            _sessionID = sessionID;
            _syncHandler.SetCipher(cipherServer, macServer);
            _packetizer.SetCipher(cipherClient, macClient, _param.CheckMACError);
        }
    }

    /// <summary>
    /// Synchronization of sending/receiving packets.
    /// </summary>
    internal class SSH2SynchronousPacketHandler : AbstractSynchronousPacketHandler<SSH2Packet> {

        private readonly object _cipherSync = new object();
        private uint _sequenceNumber = 0;
        private Func<SSH2Packet, DataFragment> _getPacketImageFunc;

        private readonly SSHProtocolEventManager _protocolEventManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket object for sending packets.</param>
        /// <param name="handler">the next handler received packets are redirected to.</param>
        /// <param name="protocolEventManager">protocol event manager</param>
        public SSH2SynchronousPacketHandler(IGranadosSocket socket, IDataHandler handler, SSHProtocolEventManager protocolEventManager)
            : base(socket, handler) {

            _getPacketImageFunc = (packet) => GetPacketImageInternal(packet, null, null);
            _protocolEventManager = protocolEventManager;
        }

        /// <summary>
        /// Set cipher settings.
        /// </summary>
        /// <param name="cipher">cipher to encrypt a packet to be sent.</param>
        /// <param name="mac">MAC for a packet to be sent.</param>
        public void SetCipher(Cipher cipher, MAC mac) {
            lock (_cipherSync) {
                Granados.Crypto.SSH2.AESGCMCipher2 aesGcm = cipher as Granados.Crypto.SSH2.AESGCMCipher2;
                if (aesGcm != null) {
                    _getPacketImageFunc = (packet) => GetPacketImageInternalAESGCM(packet, aesGcm);
                }
                else {
                    _getPacketImageFunc = (packet) => GetPacketImageInternal(packet, cipher, mac);
                }
            }
        }

        /// <summary>
        /// Gets the binary image of the packet to be sent.
        /// </summary>
        /// <remarks>
        /// The packet object will be allowed to be reused.
        /// </remarks>
        /// <param name="packet">a packet object</param>
        /// <returns>binary image of the packet</returns>
        protected override DataFragment GetPacketImage(SSH2Packet packet) {
            lock (_cipherSync) {
                return _getPacketImageFunc(packet);
            }
        }

        private DataFragment GetPacketImageInternal(SSH2Packet packet, Cipher cipher, MAC mac) {
            return packet.GetImage(cipher, mac, _sequenceNumber++);
        }

        private DataFragment GetPacketImageInternalAESGCM(SSH2Packet packet, Granados.Crypto.SSH2.AESGCMCipher2 cipher) {
            return packet.GetImageAESGCM(cipher, _sequenceNumber++);
        }

        /// <summary>
        /// Allows to reuse a packet object.
        /// </summary>
        /// <param name="packet">a packet object</param>
        protected override void Recycle(SSH2Packet packet) {
            packet.Recycle();
        }

        /// <summary>
        /// Do additional work for a packet to be sent.
        /// </summary>
        /// <param name="packet">a packet object</param>
        protected override void BeforeSend(SSH2Packet packet) {
            SSH2PacketType packetType = packet.GetPacketType();
            switch (packetType) {
                case SSH2PacketType.SSH_MSG_CHANNEL_DATA:
                case SSH2PacketType.SSH_MSG_CHANNEL_EXTENDED_DATA:
                case SSH2PacketType.SSH_MSG_IGNORE:
                    return;
            }

            _protocolEventManager.NotifySend(packetType, String.Empty);
        }

        /// <summary>
        /// Do additional work for a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        protected override void AfterReceived(DataFragment packet) {
            if (packet.Length == 0) {
                return;
            }

            SSH2PacketType packetType = (SSH2PacketType)packet.Data[packet.Offset];
            switch (packetType) {
                case SSH2PacketType.SSH_MSG_CHANNEL_DATA:
                case SSH2PacketType.SSH_MSG_CHANNEL_EXTENDED_DATA:
                case SSH2PacketType.SSH_MSG_IGNORE:
                    return;
            }

            _protocolEventManager.NotifyReceive(packetType, String.Empty);
        }

        /// <summary>
        /// Gets the packet type name of the packet to be sent. (for debugging)
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>packet name.</returns>
        protected override string GetMessageName(SSH2Packet packet) {
            return packet.GetPacketType().ToString();
        }

        /// <summary>
        /// Gets the packet type name of the received packet. (for debugging)
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>packet name.</returns>
        protected override string GetMessageName(DataFragment packet) {
            if (packet.Length > 0) {
                return ((SSH2PacketType)packet.Data[packet.Offset]).ToString();
            }
            else {
                return "?";
            }
        }
    }

    /// <summary>
    /// Class for supporting SSH_MSG_EXT_INFO.
    /// </summary>
    internal class SSH2ExtInfo : ISSHPacketInterceptor {
        #region SSH2ExtInfo

        private readonly SSHProtocolEventManager _protocolEventManager;
        private readonly SSH2ConnectionInfo _cInfo;

        private bool _serverSigAlgs = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH2ExtInfo(
                    SSHProtocolEventManager protocolEventManager,
                    SSH2ConnectionInfo info) {
            _protocolEventManager = protocolEventManager;
            _cInfo = info;
        }

        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH2PacketType packetType = (SSH2PacketType)packet[0];

            switch (packetType) {
                case SSH2PacketType.SSH_MSG_EXT_INFO:
                    ProcessEXTINFO(packet);
                    return SSHPacketInterceptorResult.Consumed;

                case SSH2PacketType.SSH_MSG_USERAUTH_SUCCESS:
                case SSH2PacketType.SSH_MSG_USERAUTH_FAILURE:
                case SSH2PacketType.SSH_MSG_REQUEST_FAILURE:
                    return SSHPacketInterceptorResult.Finished;

                default:
                    return SSHPacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Reads a received SSH_MSG_EXT_INFO packet.
        /// </summary>
        /// <param name="packet">a received packet image</param>
        private void ProcessEXTINFO(DataFragment packet) {

            SSH2DataReader reader = new SSH2DataReader(packet);
            SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();
            uint extensions = reader.ReadUInt32();
            for (uint i = 0; i < extensions; i++) {
                string extensionName = reader.ReadString();

                if (extensionName == "server-sig-algs" && !_serverSigAlgs) {
                    _serverSigAlgs = true;

                    string algorithmNames = reader.ReadString();
                    _cInfo.SupportedPublicKeyAlgorithms = algorithmNames;
                    _cInfo.SupportedPublicKeyAlgorithmsArray = algorithmNames.Split(',');

                    _protocolEventManager.Trace("SSH_MSG_EXT_INFO [{0}] : {1}={2}", i + 1, extensionName, algorithmNames);
                }
                else {
                    reader.ReadByteString();
                    _protocolEventManager.Trace("SSH_MSG_EXT_INFO [{0}] : {1} (ignored)", i + 1, extensionName);
                }
            }
        }

        public void OnConnectionClosed() {
        }

        #endregion
    }

    /// <summary>
    /// Class for supporting key exchange sequence.
    /// </summary>
    internal class SSH2KeyExchanger : ISSHPacketInterceptor {
        #region SSH2KeyExchanger

        private const int PASSING_TIMEOUT = 1000;

        private const string ALGORITHM_NAME_AEAD_AES_128_GCM = "AEAD_AES_128_GCM";
        private const string ALGORITHM_NAME_AEAD_AES_256_GCM = "AEAD_AES_256_GCM";
        private const string ALGORITHM_NAME_AES128_GCM_OPENSSH_COM = "aes128-gcm@openssh.com";
        private const string ALGORITHM_NAME_AES256_GCM_OPENSSH_COM = "aes256-gcm@openssh.com";

        private enum SequenceStatus {
            /// <summary>next key exchange can be started</summary>
            Idle,
            /// <summary>key exchange has been failed</summary>
            Failed,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>SSH_MSG_KEXINIT has been received. key exchange has been initiated by server.</summary>
            InitiatedByServer,
            /// <summary>key exchange has been initiated by client. SSH_MSG_KEXINIT from server will be accepted.</summary>
            InitiatedByClient,
            /// <summary>SSH_MSG_KEXINIT has been received.</summary>
            KexInitReceived,
            /// <summary>waiting for SSH_MSG_KEXDH_REPLY</summary>
            WaitKexDHReplay,
            /// <summary>waiting for SSH_MSG_NEWKEYS</summary>
            WaitNewKeys,
            /// <summary>waiting for updating cipher settings</summary>
            WaitUpdateCipher,
        }

        private class KexState : IDisposable {
            // payload of KEX_INIT message
            public byte[] serverKEXINITPayload;
            public byte[] clientKEXINITPayload;

            // values for Diffie-Hellman
            public DiffieHellman dh;

            // values for Elliptic Curve Diffie-Hellman
            public EllipticCurveDiffieHellman ecdh;

            // Hashing algorithm
            public HashAlgorithm hashAlgorithm;

            // result
            public BigInteger secret;   // shared secret
            public byte[] hash;     // exchange hash

            public void Dispose() {
                if (hashAlgorithm != null) {
                    hashAlgorithm.Dispose();
                }
            }
        }

        private class CipherSettings {
            public Cipher cipherServer;
            public Cipher cipherClient;
            public MAC macServer;
            public MAC macClient;
        }

        public delegate void UpdateKeyDelegate(byte[] sessionID, Cipher cipherServer, Cipher cipherClient, MAC macServer, MAC macClient);

        private readonly UpdateKeyDelegate _updateKey;

        private readonly SSHProtocolEventManager _protocolEventManager;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSHConnectionParameter _param;
        private readonly SSHTimeouts _timeouts;
        private readonly SSH2ConnectionInfo _cInfo;

        private byte[] _sessionID;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH2KeyExchanger(
                    SSHTimeouts timeouts,
                    SSH2SynchronousPacketHandler syncHandler,
                    SSHConnectionParameter param,
                    SSHProtocolEventManager protocolEventManager,
                    SSH2ConnectionInfo info,
                    UpdateKeyDelegate updateKey) {
            _timeouts = timeouts;
            _syncHandler = syncHandler;
            _param = param;
            _protocolEventManager = protocolEventManager;
            _cInfo = info;
            _updateKey = updateKey;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH2PacketType packetType = (SSH2PacketType)packet[0];
            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.Idle:
                        if (packetType == SSH2PacketType.SSH_MSG_KEXINIT) {
                            _sequenceStatus = SequenceStatus.InitiatedByServer;
                            StartKeyExchangeAsync(packet);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.InitiatedByServer:
                        break;
                    case SequenceStatus.InitiatedByClient:
                        if (packetType == SSH2PacketType.SSH_MSG_KEXINIT) {
                            _sequenceStatus = SequenceStatus.KexInitReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.KexInitReceived:
                        break;
                    case SequenceStatus.WaitKexDHReplay:
                        if (packetType == SSH2PacketType.SSH_MSG_KEXDH_REPLY) {
                            _sequenceStatus = SequenceStatus.WaitNewKeys;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.WaitNewKeys:
                        if (packetType == SSH2PacketType.SSH_MSG_NEWKEYS) {
                            _sequenceStatus = SequenceStatus.WaitUpdateCipher;
                            if (_receivedPacket.TrySet(packet, PASSING_TIMEOUT)) {
                                // block this thread until the cipher settings are updated.
                                do {
                                    Monitor.Wait(_sequenceLock);
                                } while (_sequenceStatus == SequenceStatus.WaitUpdateCipher);
                            }
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    default:
                        break;
                }
                return SSHPacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.ConnectionClosed) {
                    _sequenceStatus = SequenceStatus.ConnectionClosed;
                    DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                    _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
                }
            }
        }

        /// <summary>
        /// Execute key exchange.
        /// </summary>
        /// <remarks>
        /// if an error has been occurred during the key-exchange, an exception will be thrown.
        /// </remarks>
        public void ExecKeyExchange() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.Idle) {
                    throw new InvalidOperationException(Strings.GetString("RequestedTaskIsAlreadyRunning"));
                }
                _sequenceStatus = SequenceStatus.InitiatedByClient;
            }
            DoKeyExchange(null);
        }

        /// <summary>
        /// Key exchange sequence.
        /// </summary>
        /// <param name="kexinitFromServer">
        /// a received SSH_MSG_KEXINIT packet image if the server initiates the key exchange,
        /// or null if the client initiates the key exchange.
        /// </param>
        private Task StartKeyExchangeAsync(DataFragment kexinitFromServer) {
            return Task.Run(() => DoKeyExchange(kexinitFromServer));
        }

        /// <summary>
        /// Key exchange sequence.
        /// </summary>
        /// <param name="kexinitFromServer">
        /// a received SSH_MSG_KEXINIT packet image if the server initiates the key exchange,
        /// or null if the client initiates the key exchange.
        /// </param>
        /// <exception cref="SSHException">no response</exception>
        private void DoKeyExchange(DataFragment kexinitFromServer) {
            try {
                using (KexState state = new KexState()) {
                    KexInit(state, kexinitFromServer);

                    KexDiffieHellman(state);

                    CipherSettings cipherSettings = GetCipherSettings(state);

                    KexNewKeys(state);

                    _updateKey(
                        _sessionID,
                        cipherSettings.cipherServer,
                        cipherSettings.cipherClient,
                        cipherSettings.macServer,
                        cipherSettings.macClient);
                }

                _protocolEventManager.Trace(
                    "update cipher settings: server [{0}, {1}] client [{2}, {3}]",
                    _cInfo.OutgoingPacketCipher.Value,
                    _cInfo.OutgoingPacketMacAlgorithm.Value,
                    _cInfo.IncomingPacketCipher.Value,
                    _cInfo.IncomingPacketMacAlgorithm.Value
                );

                lock (_sequenceLock) {
                    _sequenceStatus = SequenceStatus.Idle;
                    Monitor.PulseAll(_sequenceLock);
                }

                return; // success
            }
            catch (Exception) {
                lock (_sequenceLock) {
                    _sequenceStatus = SequenceStatus.Failed;
                    Monitor.PulseAll(_sequenceLock);
                }
                throw;
            }
            finally {
                _receivedPacket.Clear();
            }
        }

        /// <summary>
        /// SSH_MSG_KEXINIT sequence.
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <param name="kexinitFromServer">
        /// a received SSH_MSG_KEXINIT packet image if the server initiates the key exchange,
        /// or null if the client initiates the key exchange.
        /// </param>
        /// <exception cref="SSHException">no response</exception>
        private void KexInit(KexState state, DataFragment kexinitFromServer) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.InitiatedByClient
                    || _sequenceStatus == SequenceStatus.InitiatedByServer
                    || _sequenceStatus == SequenceStatus.KexInitReceived);
            }

            if (kexinitFromServer != null) {
                ProcessKEXINIT(kexinitFromServer, state);
            }

            string traceMsg;
            SSH2Packet packetToSend = BuildKEXINITPacket(out traceMsg);

            state.clientKEXINITPayload = packetToSend.GetPayloadBytes();

            if (kexinitFromServer != null) {
                // if the key exchange was initiated by the server,
                // no need to wait for the SSH_MSG_KEXINIT response.
                _syncHandler.Send(packetToSend);
                _protocolEventManager.Trace(traceMsg);
                return;
            }

            // send KEXINIT
            _syncHandler.Send(packetToSend);
            _protocolEventManager.Trace(traceMsg);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.KexInitReceived);    // already set in FeedReceivedPacket
            }

            ProcessKEXINIT(response, state);
        }

        /// <summary>
        /// Diffie-Hellman key exchange sequence.
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <exception cref="SSHException">no response</exception>
        private void KexDiffieHellman(KexState state) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.KexInitReceived || _sequenceStatus == SequenceStatus.InitiatedByServer);
                _sequenceStatus = SequenceStatus.WaitKexDHReplay;
            }

            bool useEcdh = _cInfo.KEXAlgorithm.Value.IsECDH();

            // send KEXDH_INIT
            SSH2Packet packetToSend;
            if (useEcdh) {
                packetToSend = BuildKEX_ECDH_INITPacket(state);
            }
            else {
                packetToSend = BuildKEXDHINITPacket(state);
            }

            _syncHandler.Send(packetToSend);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.WaitNewKeys || _sequenceStatus == SequenceStatus.WaitUpdateCipher);    // already set in FeedReceivedPacket
            }

            bool isAccepted;
            if (useEcdh) {
                isAccepted = ProcessKEX_ECDH_REPLY(response, state);
            }
            else {
                isAccepted = ProcessKEXDHREPLY(response, state);
            }

            _protocolEventManager.Trace(
                isAccepted ? "host key has been accepted" : "host key has been denied");

            if (!isAccepted) {
                throw new SSHException(Strings.GetString("HostKeyDenied"));
            }
        }

        /// <summary>
        /// SSH_MSG_NEWKEYS sequence.
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>true if the sequence was succeeded</returns>
        /// <exception cref="SSHException">no response</exception>
        private void KexNewKeys(KexState state) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.WaitNewKeys || _sequenceStatus == SequenceStatus.WaitUpdateCipher);
            }

            var packetToSend = BuildNEWKEYSPacket();
            _syncHandler.Send(packetToSend);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
            }

            _protocolEventManager.Trace("the keys are updated");
        }

        /// <summary>
        /// Build a SSH_MSG_KEXINIT packet.
        /// </summary>
        /// <param name="traceMessage">trace message will be set</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildKEXINITPacket(out string traceMessage) {

            string kexAlgorithms = GetSupportedKexAlgorithms();
            string hostKeyAlgorithmDescription = FormatHostKeyAlgorithmDescription();
            string cipherAlgorithmDescription = FormatCipherAlgorithmDescription();
            string macAlgorithmDescription = FormatMacAlgorithmDescription();

            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXINIT)
                    .WriteSecureRandomBytes(16) // cookie
                    .WriteString(kexAlgorithms) // kex_algorithms
                    .WriteString(hostKeyAlgorithmDescription) // server_host_key_algorithms
                    .WriteString(cipherAlgorithmDescription) // encryption_algorithms_client_to_server
                    .WriteString(cipherAlgorithmDescription) // encryption_algorithms_server_to_client
                    .WriteString(macAlgorithmDescription) // mac_algorithms_client_to_server
                    .WriteString(macAlgorithmDescription) // mac_algorithms_server_to_client
                    .WriteString("none") // compression_algorithms_client_to_server
                    .WriteString("none") // compression_algorithms_server_to_client
                    .WriteString("") // languages_client_to_server
                    .WriteString("") // languages_server_to_client
                    .WriteBool(false) // indicates whether a guessed key exchange packet follows
                    .WriteInt32(0); // reserved for future extension

            traceMessage = new StringBuilder()
                .Append("kex_algorithm=")
                .Append(kexAlgorithms)
                .Append("; server_host_key_algorithms=")
                .Append(hostKeyAlgorithmDescription)
                .Append("; encryption_algorithms_client_to_server=")
                .Append(cipherAlgorithmDescription)
                .Append("; encryption_algorithms_server_to_client=")
                .Append(cipherAlgorithmDescription)
                .Append("; mac_algorithms_client_to_server=")
                .Append(macAlgorithmDescription)
                .Append("; mac_algorithms_server_to_client=")
                .Append(macAlgorithmDescription)
                .ToString();

            return packet;
        }

        /// <summary>
        /// Reads a received SSH_MSG_KEXINIT packet.
        /// </summary>
        /// <param name="packet">a received packet image</param>
        /// <param name="state">informations about current key exchange</param>
        private void ProcessKEXINIT(DataFragment packet, KexState state) {

            state.serverKEXINITPayload = packet.GetBytes();

            SSH2DataReader reader = new SSH2DataReader(packet);
            reader.Read(17);    // skip message number and cookie

            string kex = reader.ReadString();
            _cInfo.SupportedKEXAlgorithms = kex;
            _cInfo.KEXAlgorithm = DecideKexAlgorithm(kex);

            string host_key = reader.ReadString();
            _cInfo.SupportedHostKeyAlgorithms = host_key;
            _cInfo.SupportedHostKeyAlgorithmsArray = host_key.Split(',');
            _cInfo.HostKeyAlgorithm = DecideHostKeyAlgorithm(_cInfo.SupportedHostKeyAlgorithmsArray);

            string enc_cs = reader.ReadString();
            _cInfo.SupportedEncryptionAlgorithmsClientToServer = enc_cs;

            string enc_sc = reader.ReadString();
            _cInfo.SupportedEncryptionAlgorithmsServerToClient = enc_sc;

            string mac_cs = reader.ReadString();
            _cInfo.SupportedMacAlgorithmsClientToServer = mac_cs;

            string mac_sc = reader.ReadString();
            _cInfo.SupportedMacAlgorithmsServerToClient = mac_sc;

            string comp_cs = reader.ReadString();
            CheckAlgorithmSupport("compression", comp_cs, "none");
            string comp_sc = reader.ReadString();
            CheckAlgorithmSupport("compression", comp_sc, "none");

            string lang_cs = reader.ReadString();
            string lang_sc = reader.ReadString();
            bool firstKexPacketFollows = reader.ReadBool();
            int reserved = reader.ReadInt32();

            if (firstKexPacketFollows) {
                throw new SSHException(Strings.GetString("AlgorithmNegotiationFailed"));
            }

            string traceMessage = new StringBuilder()
                .Append("kex_algorithm=")
                .Append(kex)
                .Append("; server_host_key_algorithms=")
                .Append(host_key)
                .Append("; encryption_algorithms_client_to_server=")
                .Append(enc_cs)
                .Append("; encryption_algorithms_server_to_client=")
                .Append(enc_sc)
                .Append("; mac_algorithms_client_to_server=")
                .Append(mac_cs)
                .Append("; mac_algorithms_server_to_client=")
                .Append(mac_sc)
                .Append("; comression_algorithms_client_to_server=")
                .Append(comp_cs)
                .Append("; comression_algorithms_server_to_client=")
                .Append(comp_sc)
                .ToString();

            _protocolEventManager.Trace(traceMessage);

            CipherAlgorithmAndName outgoingPacketCipher;
            MACAlgorithmAndName outgoingPacketMacAlgorithm;
            DecideCipherAlgorithmAndMACAlgorithm(enc_cs, mac_cs, out outgoingPacketCipher, out outgoingPacketMacAlgorithm);

            CipherAlgorithmAndName incomingPacketCipher;
            MACAlgorithmAndName incomingPacketMacAlgorithm;
            DecideCipherAlgorithmAndMACAlgorithm(enc_sc, mac_sc, out incomingPacketCipher, out incomingPacketMacAlgorithm);

            _cInfo.OutgoingPacketCipher = outgoingPacketCipher.Algorithm;
            _cInfo.IncomingPacketCipher = incomingPacketCipher.Algorithm;
            _cInfo.OutgoingPacketMacAlgorithm = outgoingPacketMacAlgorithm.Algorithm;
            _cInfo.IncomingPacketMacAlgorithm = incomingPacketMacAlgorithm.Algorithm;
        }

        /// <summary>
        /// Builds a SSH_MSG_KEXDH_INIT packet.
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildKEXDHINITPacket(KexState state) {
            //Round1 computes and sends [e]
            try {
                state.dh = new DiffieHellman(_cInfo.KEXAlgorithm.Value);
            }
            catch (DiffieHellmanException e) {
                throw new SSHException(e.Message, e);
            }

            switch (_cInfo.KEXAlgorithm.Value) {
                case KexAlgorithm.DH_G1_SHA1:
                case KexAlgorithm.DH_G14_SHA1:
                    state.hashAlgorithm = new SHA1CryptoServiceProvider();
                    break;
                case KexAlgorithm.DH_G14_SHA256:
                    state.hashAlgorithm = new SHA256CryptoServiceProvider();
                    break;
                case KexAlgorithm.DH_G16_SHA512:
                case KexAlgorithm.DH_G18_SHA512:
                    state.hashAlgorithm = new SHA512CryptoServiceProvider();
                    break;
                default:
                    throw new SSHException("Cannot determine the hashing algorithm: " + _cInfo.KEXAlgorithm.Value.ToString());
            }

            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXDH_INIT)
                    .WriteBigInteger(state.dh.GPowXModP);

            return packet;
        }

        /// <summary>
        /// Builds a SSH_MSG_KEX_ECDH_INIT packet.
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildKEX_ECDH_INITPacket(KexState state) {
            try {
                state.ecdh = EllipticCurveDiffieHellmanFactory.GetInstance(_cInfo.KEXAlgorithm.Value);
            }
            catch (EllipticCurveDiffieHellmanException e) {
                throw new SSHException(e.Message, e);
            }

            state.hashAlgorithm = ECDSAHashAlgorithmChooser.Choose(state.ecdh.GetCurveSize());

            // the message number of SSH_MSG_KEX_ECDH_INIT is identical to the message number of SSH_MSG_KEXDH_INIT.
            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXDH_INIT)
                    .WriteAsString(state.ecdh.GetEphemeralPublicKey());

            return packet;
        }

        /// <summary>
        /// Reads and verifies SSH_MSG_KEXDH_REPLY packet.
        /// </summary>
        /// <param name="packet">a received packet image</param>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>true if verification was succeeded</returns>
        private bool ProcessKEXDHREPLY(DataFragment packet, KexState state) {
            //Round2 receives response
            SSH2DataReader reader = new SSH2DataReader(packet);
            SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

            byte[] key_and_cert = reader.ReadByteString();
            BigInteger f = reader.ReadMPInt();
            byte[] signature = reader.ReadByteString();
            Debug.Assert(reader.RemainingDataLength == 0);

            //Round3 calc hash H
            state.secret = state.dh.CalculateSecret(f);
            SSH2DataWriter wr = new SSH2DataWriter();
            wr.WriteString(_cInfo.ClientVersionString);
            wr.WriteString(_cInfo.ServerVersionString);
            wr.WriteAsString(state.clientKEXINITPayload);
            wr.WriteAsString(state.serverKEXINITPayload);
            wr.WriteAsString(key_and_cert);
            wr.WriteBigInteger(state.dh.GPowXModP);
            wr.WriteBigInteger(f);
            wr.WriteBigInteger(state.secret);
            state.hash = state.hashAlgorithm.ComputeHash(wr.ToByteArray());

            return VerifyHostKeyAndUpdateSessionID(key_and_cert, signature, state.hash);
        }

        /// <summary>
        /// Reads and verifies SSH_MSG_KEX_ECDH_REPLY packet.
        /// </summary>
        /// <param name="packet">a received packet image</param>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>true if verification was succeeded</returns>
        private bool ProcessKEX_ECDH_REPLY(DataFragment packet, KexState state) {
            SSH2DataReader reader = new SSH2DataReader(packet);
            SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

            byte[] serverHostKey = reader.ReadByteString();
            byte[] serverPublicKey = reader.ReadByteString();
            byte[] signature = reader.ReadByteString();
            Debug.Assert(reader.RemainingDataLength == 0);

            // get shared secret
            try {
                state.secret = state.ecdh.CalcSharedSecret(serverPublicKey);
            }
            catch (EllipticCurveDiffieHellmanException e) {
                throw new SSHException(e.Message, e);
            }

            // get hash
            SSH2DataWriter wr = new SSH2DataWriter();
            wr.WriteString(_cInfo.ClientVersionString);
            wr.WriteString(_cInfo.ServerVersionString);
            wr.WriteAsString(state.clientKEXINITPayload);
            wr.WriteAsString(state.serverKEXINITPayload);
            wr.WriteAsString(serverHostKey);
            wr.WriteAsString(state.ecdh.GetEphemeralPublicKey());
            wr.WriteAsString(serverPublicKey);
            wr.WriteBigInteger(state.secret);
            state.hash = state.hashAlgorithm.ComputeHash(wr.ToByteArray());

            return VerifyHostKeyAndUpdateSessionID(serverHostKey, signature, state.hash);
        }

        /// <summary>
        /// Verifies server host key and certificates, then set session ID if it hasn't been set yet.
        /// </summary>
        /// <param name="ks">server public host key and certificates (K_S)</param>
        /// <param name="signature">signature of exchange hash</param>
        /// <param name="hash">computed exchange hash</param>
        /// <returns>true if server host key and certificates were verified and accepted.</returns>
        private bool VerifyHostKeyAndUpdateSessionID(byte[] ks, byte[] signature, byte[] hash) {
            _protocolEventManager.Trace("verifying host key");

            bool verifyExternally = (_sessionID == null) ? true : false;
            bool accepted = VerifyHostKey(ks, signature, hash, verifyExternally);

            _protocolEventManager.Trace("verifying host key : {0}", accepted ? "accepted" : "rejected");

            if (accepted && _sessionID == null) {
                //Debug.WriteLine("hash="+DebugUtil.DumpByteArray(hash));
                _sessionID = hash;
            }
            return accepted;
        }

        /// <summary>
        /// Builds a SSH_MSG_NEWKEYS packet.
        /// </summary>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildNEWKEYSPacket() {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_NEWKEYS);
        }

        /// <summary>
        /// Gets cipher settings
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>cipher settings</returns>
        private CipherSettings GetCipherSettings(KexState state) {
            CipherSettings settings = new CipherSettings();

            settings.cipherServer =
                CipherFactory.CreateCipher(
                    SSHProtocol.SSH2,
                    _cInfo.OutgoingPacketCipher.Value,
                    DeriveKey(state.secret, state.hash, 'C', CipherFactory.GetKeySize(_cInfo.OutgoingPacketCipher.Value), state.hashAlgorithm),
                    DeriveKey(state.secret, state.hash, 'A', CipherFactory.GetIVSize(_cInfo.OutgoingPacketCipher.Value), state.hashAlgorithm)
                );
            settings.cipherClient =
                CipherFactory.CreateCipher(
                    SSHProtocol.SSH2,
                    _cInfo.IncomingPacketCipher.Value,
                    DeriveKey(state.secret, state.hash, 'D', CipherFactory.GetKeySize(_cInfo.IncomingPacketCipher.Value), state.hashAlgorithm),
                    DeriveKey(state.secret, state.hash, 'B', CipherFactory.GetIVSize(_cInfo.IncomingPacketCipher.Value), state.hashAlgorithm)
                );

            settings.macServer =
                MACFactory.CreateMAC(
                    _cInfo.OutgoingPacketMacAlgorithm.Value,
                    DeriveKey(state.secret, state.hash, 'E', MACFactory.GetSize(_cInfo.OutgoingPacketMacAlgorithm.Value), state.hashAlgorithm)
                );
            settings.macClient =
                MACFactory.CreateMAC(
                    _cInfo.IncomingPacketMacAlgorithm.Value,
                    DeriveKey(state.secret, state.hash, 'F', MACFactory.GetSize(_cInfo.IncomingPacketMacAlgorithm.Value), state.hashAlgorithm)
                );

            return settings;
        }

        /// <summary>
        /// Verifies server host key and certificates.
        /// </summary>
        /// <param name="ks">server public host key and certificates (K_S)</param>
        /// <param name="signature">signature of exchange hash</param>
        /// <param name="hash">computed exchange hash</param>
        /// <param name="verifyExternally">specify true if the additional verification by delegate VerifySSHHostKey is required.</param>
        /// <returns>true if server host key and certificates were verified and accepted.</returns>
        private bool VerifyHostKey(byte[] ks, byte[] signature, byte[] hash, bool verifyExternally) {
            SSH2DataReader ksReader = new SSH2DataReader(ks);
            string algorithm = ksReader.ReadString();
            if (algorithm != _cInfo.HostKeyAlgorithm.PublicKeyAlgorithmName) {
                throw new SSHException(Strings.GetString("HostKeyAlgorithmMismatch"));
            }

            SSH2DataReader sigReader = new SSH2DataReader(signature);
            string sigAlgorithm = sigReader.ReadString();
            if (sigAlgorithm != _cInfo.HostKeyAlgorithm.SignatureAlgorithmName) {
                throw new SSHException(Strings.GetString("HostKeyAlgorithmMismatch"));
            }

            byte[] signatureBlob = sigReader.ReadByteString();

            PublicKeyAlgorithm publicKeyAlgorithm = _cInfo.HostKeyAlgorithm.PublicKeyAlgorithm;
            SignatureAlgorithmVariant signatureAlgorithmVariant = _cInfo.HostKeyAlgorithm.SignatureAlgorithmVariant;

            if (publicKeyAlgorithm == PublicKeyAlgorithm.RSA) {
                RSAPublicKey pk = RSAPublicKey.ReadFrom(ksReader);
                pk.VerifyWithSHA(signatureBlob, hash, signatureAlgorithmVariant);
                _cInfo.HostKey = pk;
            }
            else if (publicKeyAlgorithm == PublicKeyAlgorithm.DSA) {
                DSAPublicKey pk = DSAPublicKey.ReadFrom(ksReader);
                pk.Verify(signatureBlob, new SHA1CryptoServiceProvider().ComputeHash(hash));
                _cInfo.HostKey = pk;
            }
            else if (publicKeyAlgorithm == PublicKeyAlgorithm.ECDSA_SHA2_NISTP256
                    || publicKeyAlgorithm == PublicKeyAlgorithm.ECDSA_SHA2_NISTP384
                    || publicKeyAlgorithm == PublicKeyAlgorithm.ECDSA_SHA2_NISTP521) {

                ECDSAPublicKey pk = ECDSAPublicKey.ReadFrom(ksReader);
                pk.Verify(signatureBlob, hash);
                _cInfo.HostKey = pk;
            }
            else if (publicKeyAlgorithm == PublicKeyAlgorithm.ED25519) {
                EDDSAPublicKey pk = EDDSAPublicKey.ReadFrom(publicKeyAlgorithm, ksReader);
                pk.Verify(signatureBlob, hash);
                _cInfo.HostKey = pk;
            }
            else {
                throw new SSHException(Strings.GetString("UnsupportedHostKeyAlgorithm"));
            }

            //ask the client whether he accepts the host key
            if (verifyExternally && _param.VerifySSHHostKey != null) {
                return _param.VerifySSHHostKey(_cInfo.GetSSHHostKeyInformationProvider());
            }

            return true;
        }

        /// <summary>
        /// Creates a key from K and H.
        /// </summary>
        /// <param name="k">a shared secret K</param>
        /// <param name="h">an exchange hash H</param>
        /// <param name="letter">letter ('A', 'B',...)</param>
        /// <param name="length">key length</param>
        /// <param name="hashAlgorithm">hashing algorithm</param>
        /// <returns></returns>
        private byte[] DeriveKey(BigInteger k, byte[] h, char letter, int length, HashAlgorithm hashAlgorithm) {
            SSH2PayloadImageBuilder image = new SSH2PayloadImageBuilder();
            ByteBuffer hashBuff = new ByteBuffer(length * 2, -1);

            while (true) {
                image.Clear();
                image.WriteBigInteger(k);
                image.Write(h);
                if (hashBuff.Length == 0) {
                    image.WriteByte((byte)letter);
                    image.Write(_sessionID);
                }
                else {
                    image.Payload.Append(hashBuff);
                }
                byte[] hash = hashAlgorithm.ComputeHash(image.GetBytes());

                hashBuff.Append(hash);

                if (hashBuff.Length > length) {
                    int trimLen = hashBuff.Length - length;
                    if (trimLen > 0) {
                        hashBuff.RemoveTail(trimLen);
                    }
                    return hashBuff.GetBytes();
                }
            }
        }

        /// <summary>
        /// Checks if the name list contains the specified algorithm.
        /// </summary>
        /// <param name="title">title</param>
        /// <param name="nameList">name-list string</param>
        /// <param name="algorithmName">algorithm name</param>
        /// <exception cref="SSHException">the name list doesn't contain the specified algorithm</exception>
        private static void CheckAlgorithmSupport(string title, string nameList, string algorithmName) {
            string[] names = nameList.Split(',');
            if (names.Contains(algorithmName)) {
                return; // found
            }
            throw new SSHException(
                String.Format(Strings.GetString("AlgorithmNotSupportedByServer"), algorithmName, title));
        }

        /// <summary>
        /// Decides Key exchange algorithm to use.
        /// </summary>
        /// <param name="candidates">algorithm candidates</param>
        /// <returns>key exchange algorithm to use</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private KexAlgorithm DecideKexAlgorithm(string candidates) {
            string[] candidateNames = candidates.Split(',');
            foreach (KexAlgorithm algorithm in AlgorithmSpecUtil<KexAlgorithm>.GetAlgorithmsByPriorityOrder()) {
                string algorithmName = AlgorithmSpecUtil<KexAlgorithm>.GetAlgorithmName(algorithm);
                if (candidateNames.Contains(algorithmName)) {
                    return algorithm;
                }
            }
            throw new SSHException(Strings.GetString("KeyExchangeAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Decides host key algorithm to use.
        /// </summary>
        /// <param name="candidates">algorithm candidates</param>
        /// <returns>host key algorithm to use</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private PublicKeySignatureAlgorithm DecideHostKeyAlgorithm(string[] candidates) {
            foreach (PublicKeySignatureAlgorithm pref in _param.PreferableHostKeySignatureAlgorithms) {
                if (candidates.Contains(pref.SignatureAlgorithmName)) {
                    return pref;
                }
            }
            throw new SSHException(Strings.GetString("HostKeyAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Decides cipher algorithm and MAC algorithm to use.
        /// </summary>
        /// <param name="cipherAlgorithmCandidates">cipher algorithm candidates</param>
        /// <param name="macAlgorithmCandidates">mac algorithm candidates</param>
        /// <param name="cipherAlgorithm">cipher algorithm to use</param>
        /// <param name="macAlgorithm">MAC algorithm to use</param>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private void DecideCipherAlgorithmAndMACAlgorithm(
            string cipherAlgorithmCandidates,
            string macAlgorithmCandidates,
            out CipherAlgorithmAndName cipherAlgorithm,
            out MACAlgorithmAndName macAlgorithm
        ) {
            String[] cipherAlgorithmCandidatesArray = cipherAlgorithmCandidates.Split(',');
            String[] macAlgorithmCandidatesArray = macAlgorithmCandidates.Split(',');
            cipherAlgorithm = DecideCipherAlgorithm(cipherAlgorithmCandidatesArray);
            macAlgorithm = DecideMacAlgorithm(macAlgorithmCandidatesArray, cipherAlgorithm);
            cipherAlgorithm = AdjustCipherAlgorithmByMACAlgorithm(cipherAlgorithmCandidatesArray, cipherAlgorithm, macAlgorithm);
            ValidateAlgorithms(cipherAlgorithm, macAlgorithm);
        }

        /// <summary>
        /// Decides cipher algorithm to use.
        /// </summary>
        /// <param name="candidates">algorithm candidates</param>
        /// <returns>cipher algorithm to use and its original name</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private CipherAlgorithmAndName DecideCipherAlgorithm(string[] candidates) {
            foreach (CipherAlgorithm pref in _param.PreferableCipherAlgorithms) {
                foreach (string prefName in CipherFactory.AlgorithmToSSH2Name(pref)) {
                    if (candidates.Contains(prefName)) {
                        return new CipherAlgorithmAndName(pref, prefName);
                    }
                }
            }
            throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Adjust cipher algorithm if selected MAC algorithm requires the specific cipher algorithm. (RFC5647)
        /// </summary>
        /// <param name="candidates">cipher algorithm candidates</param>
        /// <param name="cipherAlgorithm">selected cipher algorithm</param>
        /// <param name="macAlgorithm">selected MAC algorithm</param>
        /// <returns>cipher algorithm to use and its original name</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private CipherAlgorithmAndName AdjustCipherAlgorithmByMACAlgorithm(string[] candidates, CipherAlgorithmAndName cipherAlgorithm, MACAlgorithmAndName macAlgorithm) {
            // RFC5647 calaims that AES-GCM must be selected as the cipher algorithm if AES-GCM is selected as the MAC algorithm.
            CipherAlgorithmAndName select;
            switch (macAlgorithm.Name) {
                case ALGORITHM_NAME_AEAD_AES_128_GCM:
                    if (cipherAlgorithm.Algorithm == CipherAlgorithm.AES128GCM ||
                        cipherAlgorithm.Algorithm == CipherAlgorithm.AES256GCM) {
                        // CipherAlgorithm.AES256GCM is inconsistent with the MAC algorithm AEAD_AES_128_GCM.
                        // In that case, ValidateAlgorithms() will detect the inconsistency and raise error.
                        return cipherAlgorithm;
                    }
                    select = new CipherAlgorithmAndName(CipherAlgorithm.AES128GCM, ALGORITHM_NAME_AEAD_AES_128_GCM);
                    goto checkCandidates;
                case ALGORITHM_NAME_AEAD_AES_256_GCM:
                    if (cipherAlgorithm.Algorithm == CipherAlgorithm.AES128GCM ||
                        cipherAlgorithm.Algorithm == CipherAlgorithm.AES256GCM) {
                        // CipherAlgorithm.AES128GCM is inconsistent with the MAC algorithm AEAD_AES_256_GCM.
                        // In that case, ValidateAlgorithms() will detect the inconsistency and raise error.
                        return cipherAlgorithm;
                    }
                    select = new CipherAlgorithmAndName(CipherAlgorithm.AES256GCM, ALGORITHM_NAME_AEAD_AES_256_GCM);
                checkCandidates:
                    if (!candidates.Contains(select.Name)) {
                        _protocolEventManager.Trace("cannot select cipher algorithm : {0}", select.Name);
                        throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
                    }
                    return select;
            }
            return cipherAlgorithm;
        }

        /// <summary>
        /// Decides MAC algorithm to use.
        /// </summary>
        /// <param name="candidates">algorithm candidates</param>
        /// <param name="cipherAlgorithm">selected cipher algorithm</param>
        /// <returns>MAC algorithm to use and its original name</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private MACAlgorithmAndName DecideMacAlgorithm(string[] candidates, CipherAlgorithmAndName cipherAlgorithm) {
            MACAlgorithmAndName select;
            switch (cipherAlgorithm.Name) {
                case ALGORITHM_NAME_AES128_GCM_OPENSSH_COM:
                    // ignore candidates
                    return new MACAlgorithmAndName(MACAlgorithm.AEAD_AES_128_GCM, ALGORITHM_NAME_AEAD_AES_128_GCM);
                case ALGORITHM_NAME_AES256_GCM_OPENSSH_COM:
                    // ignore candidates
                    return new MACAlgorithmAndName(MACAlgorithm.AEAD_AES_256_GCM, ALGORITHM_NAME_AEAD_AES_256_GCM);
                case ALGORITHM_NAME_AEAD_AES_128_GCM:
                    // RFC5647 claims that AES-GCM must be selected as the MAC algorithm if AES-GCM is selected as the cipher algorithm.
                    // We support it only if AES-GCM is present in the candidates.
                    select = new MACAlgorithmAndName(MACAlgorithm.AEAD_AES_128_GCM, ALGORITHM_NAME_AEAD_AES_128_GCM);
                    goto checkCandidates;
                case ALGORITHM_NAME_AEAD_AES_256_GCM:
                    select = new MACAlgorithmAndName(MACAlgorithm.AEAD_AES_256_GCM, ALGORITHM_NAME_AEAD_AES_256_GCM);
                checkCandidates:
                    if (!candidates.Contains(select.Name)) {
                        _protocolEventManager.Trace("cannot select MAC algorithm : {0}", select.Name);
                        throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
                    }
                    return select;
            }

            foreach (MACAlgorithm pref in _param.PreferableMacAlgorithms) {
                foreach (string prefName in MACFactory.AlgorithmToSSH2Name(pref)) {
                    if (candidates.Contains(prefName)) {
                        return new MACAlgorithmAndName(pref, prefName);
                    }
                }
            }
            throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Validate consistency of the selected algorithms.
        /// </summary>
        /// <param name="cipherAlgorithm">selected cipher algorithm</param>
        /// <param name="macAlgorithm">selected MAC algorithm</param>
        /// <exception cref="SSHException">inconsistency was detected</exception>
        private void ValidateAlgorithms(CipherAlgorithmAndName cipherAlgorithm, MACAlgorithmAndName macAlgorithm) {
            if (
                ((cipherAlgorithm.Algorithm == CipherAlgorithm.AES128GCM) != (macAlgorithm.Algorithm == MACAlgorithm.AEAD_AES_128_GCM)) ||
                ((cipherAlgorithm.Algorithm == CipherAlgorithm.AES256GCM) != (macAlgorithm.Algorithm == MACAlgorithm.AEAD_AES_256_GCM))
            ) {
                _protocolEventManager.Trace("selected algorithms are inconsistent : cipher algorithm = {0}, MAC algorithm = {1}", cipherAlgorithm.Name, macAlgorithm.Name);
                throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
            }
        }

        /// <summary>
        /// Makes kex_algorithms field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string GetSupportedKexAlgorithms() {
            return string.Join(",", Enumerable.Concat(
                AlgorithmSpecUtil<KexAlgorithm>.GetAlgorithmNamesByPriorityOrder(),
                new string[] { "ext-info-c" } // RFC 8308 Extension Negotiation
            ));
        }

        /// <summary>
        /// Makes server_host_key_algorithms field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string FormatHostKeyAlgorithmDescription() {
            if (_param.PreferableHostKeySignatureAlgorithms.Length == 0) {
                throw new SSHException("HostKeyAlgorithm is not set");
            }

            return string.Join(",",
                    _param.PreferableHostKeySignatureAlgorithms
                        .Select(algorithm => algorithm.SignatureAlgorithmName));
        }

        /// <summary>
        /// Makes encryption_algorithms_client_to_server field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string FormatCipherAlgorithmDescription() {
            if (_param.PreferableCipherAlgorithms.Length == 0) {
                throw new SSHException("preferable cipher algorithms were not set");
            }
            return string.Join(",",
                    _param.PreferableCipherAlgorithms
                        .Select(algorithm => CipherFactory.AlgorithmToSSH2Name(algorithm))
                        .SelectMany(a => a));
        }

        /// <summary>
        /// Makes mac_algorithms_client_to_server field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string FormatMacAlgorithmDescription() {
            if (_param.PreferableMacAlgorithms.Length == 0) {
                throw new SSHException("preferable MAC algorithms were not set");
            }
            return string.Join(",",
                    _param.PreferableMacAlgorithms
                        .Select(algorithm => MACFactory.AlgorithmToSSH2Name(algorithm))
                        .SelectMany(a => a));
        }

        /// <summary>
        /// Check ConnectionClosed.
        /// </summary>
        private void CheckConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                    throw new SSHException(Strings.GetString("ConnectionClosed"));
                }
            }
        }

        #endregion  // SSH2KeyExchanger

        #region Structs for internal use

        private struct CipherAlgorithmAndName {
            public readonly CipherAlgorithm Algorithm;
            public readonly string Name;

            public CipherAlgorithmAndName(CipherAlgorithm algorithm, string name) {
                this.Algorithm = algorithm;
                this.Name = name;
            }
        }

        private struct MACAlgorithmAndName {
            public readonly MACAlgorithm Algorithm;
            public readonly string Name;

            public MACAlgorithmAndName(MACAlgorithm algorithm, string name) {
                this.Algorithm = algorithm;
                this.Name = name;
            }
        }

        #endregion
    }

    /// <summary>
    /// Class for supporting user authentication
    /// </summary>
    internal class SSH2UserAuthentication : ISSHPacketInterceptor {
        #region SSH2UserAuthentication

        private const int PASSING_TIMEOUT = 1000;

        private readonly IKeyboardInteractiveAuthenticationHandler _kiHandler;

        private readonly SSHConnectionParameter _param;
        private readonly SSH2Connection _connection;
        private readonly SSH2ConnectionInfo _connectionInfo;
        private readonly SSHProtocolEventManager _protocolEventManager;
        private readonly SSHTimeouts _timeouts;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly byte[] _sessionID;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        internal event Action<bool> KeyboardInteractiveAuthenticationFinished;

        private enum SequenceStatus {
            /// <summary>authentication can be started</summary>
            Idle,
            /// <summary>authentication has been finished.</summary>
            Done,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>authentication has been started</summary>
            StartAuthentication,
            /// <summary>waiting for SSH_MSG_SERVICE_ACCEPT</summary>
            WaitServiceAccept,
            /// <summary>SSH_MSG_SERVICE_ACCEPT has been received</summary>
            ServiceAcceptReceived,

            //--- keyboard-interactive authentication

            /// <summary>waiting for SSH_MSG_USERAUTH_INFO_REQUEST|SSH_MSG_USERAUTH_SUCCESS|SSH_MSG_USERAUTH_FAILURE|SSH_MSG_USERAUTH_BANNER</summary>
            KI_WaitUserAuthInfoRequest,
            /// <summary>SSH_MSG_USERAUTH_INFO_REQUEST has been received</summary>
            KI_UserAuthInfoRequestReceived,
            /// <summary>SSH_MSG_USERAUTH_SUCCESS has been received</summary>
            KI_SuccessReceived,
            /// <summary>SSH_MSG_USERAUTH_FAILURE has been received</summary>
            KI_FailureReceived,
            /// <summary>
            /// SSH_MSG_USERAUTH_BANNER has been received.
            /// still waiting for SSH_MSG_USERAUTH_INFO_REQUEST|SSH_MSG_USERAUTH_SUCCESS|SSH_MSG_USERAUTH_FAILURE|SSH_MSG_USERAUTH_BANNER.
            /// </summary>
            KI_BannerReceived,

            //--- password authentication or publickey authentication

            /// <summary>waiting for SSH_MSG_USERAUTH_SUCCESS|SSH_MSG_USERAUTH_FAILURE|SSH_MSG_USERAUTH_BANNER</summary>
            PA_WaitUserAuthResponse,
            /// <summary>SSH_MSG_USERAUTH_SUCCESS has been received</summary>
            PA_SuccessReceived,
            /// <summary>SSH_MSG_USERAUTH_FAILURE has been received</summary>
            PA_FailureReceived,
            /// <summary>
            /// SSH_MSG_USERAUTH_BANNER has been received.
            /// still waiting for SSH_MSG_USERAUTH_SUCCESS|SSH_MSG_USERAUTH_FAILURE|SSH_MSG_USERAUTH_BANNER.
            /// </summary>
            PA_BannerReceived,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH2UserAuthentication(
                    SSH2Connection connection,
                    SSH2ConnectionInfo connectionInfo,
                    SSHConnectionParameter param,
                    SSHProtocolEventManager protocolEventManager,
                    SSHTimeouts timeouts,
                    SSH2SynchronousPacketHandler syncHandler,
                    byte[] sessionID) {
            _connection = connection;
            _connectionInfo = connectionInfo;
            _param = param;
            _protocolEventManager = protocolEventManager;
            _timeouts = timeouts;
            _syncHandler = syncHandler;
            _sessionID = sessionID;
            _kiHandler =
                param.KeyboardInteractiveAuthenticationHandlerCreator != null ?
                    param.KeyboardInteractiveAuthenticationHandlerCreator(_connection) : null;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            if (_sequenceStatus == SequenceStatus.Done) {   // fast check
                return SSHPacketInterceptorResult.Finished;
            }

            SSH2PacketType packetType = (SSH2PacketType)packet[0];
            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.Idle:
                    case SequenceStatus.Done:
                    case SequenceStatus.StartAuthentication:
                        break;
                    case SequenceStatus.WaitServiceAccept:
                        if (packetType == SSH2PacketType.SSH_MSG_SERVICE_ACCEPT) {
                            _sequenceStatus = SequenceStatus.ServiceAcceptReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.ServiceAcceptReceived:
                        break;

                    // Keyboard Interactive

                    case SequenceStatus.KI_WaitUserAuthInfoRequest:
                    case SequenceStatus.KI_BannerReceived:
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_INFO_REQUEST) {
                            _sequenceStatus = SequenceStatus.KI_UserAuthInfoRequestReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_SUCCESS) {
                            _sequenceStatus = SequenceStatus.KI_SuccessReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_FAILURE) {
                            _sequenceStatus = SequenceStatus.KI_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_BANNER) {
                            _sequenceStatus = SequenceStatus.KI_BannerReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.KI_UserAuthInfoRequestReceived:
                    case SequenceStatus.KI_SuccessReceived:
                    case SequenceStatus.KI_FailureReceived:
                        break;

                    // Password authentication or Publickey authentication

                    case SequenceStatus.PA_WaitUserAuthResponse:
                    case SequenceStatus.PA_BannerReceived:
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_SUCCESS) {
                            _sequenceStatus = SequenceStatus.PA_SuccessReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_FAILURE) {
                            _sequenceStatus = SequenceStatus.PA_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_BANNER) {
                            _sequenceStatus = SequenceStatus.PA_BannerReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.PA_SuccessReceived:
                    case SequenceStatus.PA_FailureReceived:
                        break;

                    default:
                        break;
                }
                return SSHPacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.ConnectionClosed) {
                    _sequenceStatus = SequenceStatus.ConnectionClosed;
                    DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                    _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
                }
            }
        }

        /// <summary>
        /// Authentication.
        /// </summary>
        /// <remarks>
        /// if an error has been occurred during the authentication, an exception will be thrown.
        /// </remarks>
        public void ExecAuthentication() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.Idle) {
                    throw new InvalidOperationException(Strings.GetString("RequestedTaskIsAlreadyRunning"));
                }
                _sequenceStatus = SequenceStatus.StartAuthentication;
            }
            DoAuthentication();
        }

        /// <summary>
        /// Authentication sequence.
        /// </summary>
        /// <returns>true if the sequence was succeeded</returns>
        /// <exception cref="SSHException">no response</exception>
        private void DoAuthentication() {
            bool keepSequenceStatusOnExit = false;
            try {
                ServiceRequest("ssh-userauth");

                switch (_param.AuthenticationType) {
                    case AuthenticationType.KeyboardInteractive:
                        KeyboardInteractiveUserAuth("ssh-connection");
                        keepSequenceStatusOnExit = true;
                        break;
                    case AuthenticationType.Password:
                        PasswordAuthentication("ssh-connection");
                        break;
                    case AuthenticationType.PublicKey:
                        PublickeyAuthentication("ssh-connection");
                        break;
                    default:
                        throw new SSHException(Strings.GetString("InvalidAuthenticationType"));
                }
            }
            finally {
                if (!keepSequenceStatusOnExit) {
                    _receivedPacket.Clear();
                    lock (_sequenceLock) {
                        _sequenceStatus = SequenceStatus.Done;
                    }
                }
            }
        }

        /// <summary>
        /// Build SSH_MSG_SERVICE_REQUEST packet.
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildServiceRequestPacket(string serviceName) {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_SERVICE_REQUEST)
                        .WriteString(serviceName);
        }

        /// <summary>
        /// SSH_MSG_SERVICE_REQUEST sequence.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void ServiceRequest(string serviceName) {
            lock (_sequenceLock) {
                Debug.Assert(_sequenceStatus == SequenceStatus.StartAuthentication);
                _sequenceStatus = SequenceStatus.WaitServiceAccept;
            }

            var packet = BuildServiceRequestPacket(serviceName);
            _syncHandler.Send(packet);

            _protocolEventManager.Trace("SSH_MSG_SERVICE_REQUEST : serviceName={0}", serviceName);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }

            SSH2DataReader reader = new SSH2DataReader(response);
            SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();
            Debug.Assert(packetType == SSH2PacketType.SSH_MSG_SERVICE_ACCEPT);

            string responseServiceName = reader.ReadString();

            _protocolEventManager.Trace("SSH_MSG_SERVICE_ACCEPT serviceName={0} ({1})",
                responseServiceName, (responseServiceName != serviceName) ? "invalid" : "valid");

            if (responseServiceName != serviceName) {
                throw new SSHException("Invalid service name : " + responseServiceName);
            }
        }

        /// <summary>
        /// Build SSH_MSG_USERAUTH_REQUEST packet for the keyboard interactive authentication.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        private SSH2Packet BuildKeyboardInteractiveUserAuthRequestPacket(string serviceName) {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                        .WriteUTF8String(_param.UserName)
                        .WriteString(serviceName)
                        .WriteString("keyboard-interactive")
                        .WriteString("") //lang
                        .WriteString(""); //submethod
        }

        /// <summary>
        /// Build SSH_MSG_USERAUTH_INFO_RESPONSE packet for the keyboard interactive authentication.
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <param name="inputs">user input</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildKeyboardInteractiveUserAuthInfoResponsePacket(string serviceName, string[] inputs) {
            var packet = new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_INFO_RESPONSE);
            packet.WriteInt32(inputs.Length);
            foreach (string line in inputs) {
                packet.WriteUTF8String(line);
            }
            return packet;
        }

        /// <summary>
        /// Keyboard interactive authentication sequence.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void KeyboardInteractiveUserAuth(string serviceName) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }

            // check handler
            if (_kiHandler == null) {
                throw new SSHException("KeyboardInteractiveAuthenticationHandler is required.");
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                _sequenceStatus = SequenceStatus.KI_WaitUserAuthInfoRequest;
            }

            var packet = BuildKeyboardInteractiveUserAuthRequestPacket(serviceName);
            _syncHandler.Send(packet);

            _protocolEventManager.Trace("starting keyboard-interactive authentication");

            // start asynchronous prompt-input-verify loop
            Task.Run(() => KeyboardInteractiveUserAuthInput(serviceName));
        }

        /// <summary>
        /// Keyboard interactive authentication sequence. (runs asynchronously)
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void KeyboardInteractiveUserAuthInput(string serviceName) {
            // notify
            _kiHandler.OnKeyboardInteractiveAuthenticationStarted();

            bool userAuthResult;
            Exception error;
            try {
                DoKeyboardInteractiveUserAuthInput(serviceName);
                userAuthResult = true;
                error = null;
            }
            catch (Exception e) {
                userAuthResult = false;
                error = e;
            }

            lock (_sequenceLock) {
                _sequenceStatus = SequenceStatus.Done;
            }

            if (KeyboardInteractiveAuthenticationFinished != null) {
                KeyboardInteractiveAuthenticationFinished(userAuthResult);
            }

            if (userAuthResult == false) {
                _connection.Close();
            }

            // notify
            _kiHandler.OnKeyboardInteractiveAuthenticationCompleted(userAuthResult, error);

            _receivedPacket.Clear();
        }

        /// <summary>
        /// Prompt lines, user input loop.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void DoKeyboardInteractiveUserAuthInput(string serviceName) {
            while (true) {
                DataFragment response = null;
                if (!_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                    throw new SSHException(Strings.GetString("ServerDoesntRespond"));
                }

                SSH2DataReader reader = new SSH2DataReader(response);
                SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

                lock (_sequenceLock) {
                    CheckConnectionClosed();

                    Debug.Assert(_sequenceStatus == SequenceStatus.KI_UserAuthInfoRequestReceived
                        || _sequenceStatus == SequenceStatus.KI_FailureReceived
                        || _sequenceStatus == SequenceStatus.KI_SuccessReceived
                        || _sequenceStatus == SequenceStatus.KI_BannerReceived);

                    if (_sequenceStatus == SequenceStatus.KI_SuccessReceived) {
                        _protocolEventManager.Trace("user authentication succeeded");
                        return;
                    }

                    if (_sequenceStatus == SequenceStatus.KI_FailureReceived) {
                        string msg = reader.ReadString();
                        _protocolEventManager.Trace("user authentication failed: {0}", msg);
                        throw new SSHException(Strings.GetString("AuthenticationFailed"));
                    }

                    if (_sequenceStatus == SequenceStatus.KI_BannerReceived) {
                        string msg = reader.ReadUTF8String();
                        string langtag = reader.ReadString();
                        _protocolEventManager.Trace("banner: lang={0} message={1}", langtag, msg);
                        _sequenceStatus = SequenceStatus.KI_WaitUserAuthInfoRequest;
                        continue;   // wait for the next response packet
                    }

                    Debug.Assert(_sequenceStatus == SequenceStatus.KI_UserAuthInfoRequestReceived);
                }

                // parse SSH_MSG_USERAUTH_INFO_REQUEST

                string name = reader.ReadUTF8String();
                string instruction = reader.ReadUTF8String();
                string lang = reader.ReadString();
                int numPrompts = reader.ReadInt32();

                string[] inputs;
                if (numPrompts > 0) {
                    string[] prompts = new string[numPrompts];
                    bool[] echoes = new bool[numPrompts];
                    for (int i = 0; i < numPrompts; i++) {
                        prompts[i] = reader.ReadUTF8String();
                        echoes[i] = reader.ReadBool();
                    }

                    // display prompt lines, and input lines
                    inputs = _kiHandler.KeyboardInteractiveAuthenticationPrompt(prompts, echoes);
                }
                else {
                    inputs = new string[0];
                }

                lock (_sequenceLock) {
                    CheckConnectionClosed();
                    _sequenceStatus = SequenceStatus.KI_WaitUserAuthInfoRequest;
                }

                var infoResponsePacket = BuildKeyboardInteractiveUserAuthInfoResponsePacket(serviceName, inputs);
                _syncHandler.Send(infoResponsePacket);
            }
        }

        /// <summary>
        /// Build SSH_MSG_USERAUTH_REQUEST packet for the password authentication.
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildPasswordAuthRequestPacket(string serviceName) {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                    .WriteUTF8String(_param.UserName)
                    .WriteString(serviceName)
                    .WriteString("password")
                    .WriteBool(false)
                    .WriteUTF8String(_param.Password);
        }

        /// <summary>
        /// Build SSH_MSG_USERAUTH_REQUEST packet for the public key authentication.
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildPublickeyAuthRequestPacket(string serviceName) {
            //public key authentication
            SSH2UserAuthKey kp = SSH2UserAuthKey.FromSECSHStyleFile(_param.IdentityFile, _param.Password);
            SignatureAlgorithmVariant sigVariant = DecideSignatureAlgorithmVariant(kp.Algorithm);
            string signatureAlgorithmName = sigVariant.GetActualSignatureAlgorithmName(kp.Algorithm);

            // construct a packet except signature
            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                    .WriteUTF8String(_param.UserName)
                    .WriteString(serviceName)
                    .WriteString("publickey")
                    .WriteBool(true)    // has signature
                    .WriteString(signatureAlgorithmName)
                    .WriteAsString(kp.GetPublicKeyBlob());

            // take payload image for the signature
            byte[] payloadImage = packet.GetPayloadBytes();

            // construct the signature source
            SSH2PayloadImageBuilder workPayload =
                new SSH2PayloadImageBuilder()
                    .WriteAsString(_sessionID)
                    .Write(payloadImage);

            // take a signature blob
            byte[] signatureBlob = kp.Sign(workPayload.GetBytes(), sigVariant);

            // encode signature (RFC4253)
            workPayload.Clear();
            byte[] signature =
                workPayload
                    .WriteString(signatureAlgorithmName)
                    .WriteAsString(signatureBlob)
                    .GetBytes();

            // append signature to the packet
            packet.WriteAsString(signature);

            return packet;
        }

        private SignatureAlgorithmVariant DecideSignatureAlgorithmVariant(PublicKeyAlgorithm keyAlgorithm) {
            string[] supportedAlgorithm =
                (_connectionInfo.SupportedPublicKeyAlgorithmsArray != null)
                    ? _connectionInfo.SupportedPublicKeyAlgorithmsArray
                    : _connectionInfo.SupportedHostKeyAlgorithmsArray;
            foreach (SignatureAlgorithmVariant variant in AlgorithmSpecUtil<SignatureAlgorithmVariant>.GetAlgorithmsByPriorityOrder()) {
                if (variant.IsRelatedTo(keyAlgorithm)
                    && supportedAlgorithm.Contains(variant.GetSignatureAlgorithmName())) {
                    return variant;
                }
            }
            return SignatureAlgorithmVariant.Default;
        }

        /// <summary>
        /// Password authentication sequence.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void PasswordAuthentication(string serviceName) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }
            var packet = BuildPasswordAuthRequestPacket(serviceName);
            string traceMessage = "starting password authentication";
            AuthenticationCore(serviceName, packet, traceMessage);
        }

        /// <summary>
        /// Public key authentication sequence.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void PublickeyAuthentication(string serviceName) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }
            var packet = BuildPublickeyAuthRequestPacket(serviceName);
            string traceMessage = "starting public key authentication";
            AuthenticationCore(serviceName, packet, traceMessage);
        }

        /// <summary>
        /// Password/Public key authentication common sequence.
        /// </summary>
        /// <param name="serviceName">service name</param>
        /// <param name="packet">a request packet to send</param>
        /// <param name="traceMessage">trace message</param>
        private void AuthenticationCore(string serviceName, SSH2Packet packet, string traceMessage) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
                _sequenceStatus = SequenceStatus.PA_WaitUserAuthResponse;
            }

            _syncHandler.Send(packet);

            _protocolEventManager.Trace(traceMessage);

            while (true) {
                DataFragment response = null;
                if (!_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                    throw new SSHException(Strings.GetString("ServerDoesntRespond"));
                }

                lock (_sequenceLock) {
                    CheckConnectionClosed();

                    SSH2DataReader reader = new SSH2DataReader(response);
                    SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

                    Debug.Assert(_sequenceStatus == SequenceStatus.PA_FailureReceived
                        || _sequenceStatus == SequenceStatus.PA_SuccessReceived
                        || _sequenceStatus == SequenceStatus.PA_BannerReceived);

                    if (_sequenceStatus == SequenceStatus.PA_SuccessReceived) {
                        _protocolEventManager.Trace("user authentication succeeded");
                        return;
                    }

                    if (_sequenceStatus == SequenceStatus.PA_FailureReceived) {
                        string msg = reader.ReadString();
                        _protocolEventManager.Trace("user authentication failed: {0}", msg);
                        throw new SSHException(Strings.GetString("AuthenticationFailed"));
                    }

                    if (_sequenceStatus == SequenceStatus.PA_BannerReceived) {
                        string msg = reader.ReadUTF8String();
                        string langtag = reader.ReadString();
                        _protocolEventManager.Trace("banner: lang={0} message={1}", langtag, msg);
                        _sequenceStatus = SequenceStatus.PA_WaitUserAuthResponse;
                        continue;   // wait for the next response packet
                    }
                }
            }
        }

        /// <summary>
        /// Check ConnectionClosed.
        /// </summary>
        private void CheckConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                    throw new SSHException(Strings.GetString("ConnectionClosed"));
                }
            }
        }

        #endregion  // SSH2UserAuthentication
    }

    /// <summary>
    /// Class for supporting remote port-forwarding
    /// </summary>
    internal class SSH2RemotePortForwarding : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;

        public delegate SSH2RemotePortForwardingChannel CreateChannelFunc(RemotePortForwardingRequest requestInfo, uint remoteChannel, uint serverWindowSize, uint serverMaxPacketSize);
        public delegate void RegisterChannelFunc(SSH2RemotePortForwardingChannel channel, ISSHChannelEventHandler eventHandler);

        private readonly SSHTimeouts _timeouts;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSHProtocolEventManager _protocolEventManager;

        private class PortInfo {
            public readonly IRemotePortForwardingHandler RequestHandler;
            public readonly CreateChannelFunc CreateChannel;
            public readonly RegisterChannelFunc RegisterChannel;

            public PortInfo(IRemotePortForwardingHandler requestHandler, CreateChannelFunc createChannel, RegisterChannelFunc registerChannel) {
                this.RequestHandler = requestHandler;
                this.CreateChannel = createChannel;
                this.RegisterChannel = registerChannel;
            }
        }
        private readonly ConcurrentDictionary<uint, PortInfo> _portDict = new ConcurrentDictionary<uint, PortInfo>();

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private enum SequenceStatus {
            /// <summary>Idle</summary>
            Idle,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>SSH_MSG_GLOBAL_REQUEST "tcpip-forward" has been sent. waiting for SSH_MSG_REQUEST_SUCCESS | SSH_MSG_REQUEST_FAILURE.</summary>
            WaitTcpIpForwardResponse,
            /// <summary>SSH_MSG_REQUEST_SUCCESS has been received.</summary>
            TcpIpForwardSuccess,
            /// <summary>SSH_MSG_REQUEST_FAILURE has been received.</summary>
            TcpIpForwardFailure,
            /// <summary>SSH_MSG_GLOBAL_REQUEST "cancel-tcpip-forward" has been sent. waiting for SSH_MSG_REQUEST_SUCCESS | SSH_MSG_REQUEST_FAILURE.</summary>
            WaitCancelTcpIpForwardResponse,
            /// <summary>SSH_MSG_REQUEST_SUCCESS has been received.</summary>
            CancelTcpIpForwardSuccess,
            /// <summary>SSH_MSG_REQUEST_FAILURE has been received.</summary>
            CancelTcpIpForwardFailure,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH2RemotePortForwarding(SSHTimeouts timeouts, SSH2SynchronousPacketHandler syncHandler, SSHProtocolEventManager protocolEventManager) {
            _timeouts = timeouts;
            _syncHandler = syncHandler;
            _protocolEventManager = protocolEventManager;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH2PacketType packetType = (SSH2PacketType)packet[0];
            SSHPacketInterceptorResult result = CheckForwardedTcpIpPacket(packetType, packet);
            if (result != SSHPacketInterceptorResult.PassThrough) {
                return result;
            }

            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.WaitTcpIpForwardResponse:
                        if (packetType == SSH2PacketType.SSH_MSG_REQUEST_SUCCESS) {
                            _sequenceStatus = SequenceStatus.TcpIpForwardSuccess;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_REQUEST_FAILURE) {
                            _sequenceStatus = SequenceStatus.TcpIpForwardFailure;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;

                    case SequenceStatus.WaitCancelTcpIpForwardResponse:
                        if (packetType == SSH2PacketType.SSH_MSG_REQUEST_SUCCESS) {
                            _sequenceStatus = SequenceStatus.CancelTcpIpForwardSuccess;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_REQUEST_FAILURE) {
                            _sequenceStatus = SequenceStatus.CancelTcpIpForwardFailure;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                }

                return SSHPacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles new "forwarded-tcpip" request.
        /// </summary>
        /// <param name="packetType">packet type</param>
        /// <param name="packet">packet data</param>
        /// <returns>result</returns>
        private SSHPacketInterceptorResult CheckForwardedTcpIpPacket(SSH2PacketType packetType, DataFragment packet) {
            if (packetType != SSH2PacketType.SSH_MSG_CHANNEL_OPEN) {
                return SSHPacketInterceptorResult.PassThrough;
            }

            SSH2DataReader reader = new SSH2DataReader(packet);
            reader.ReadByte();  // skip packet type (message number)
            string channelType = reader.ReadString();
            if (channelType != "forwarded-tcpip") {
                return SSHPacketInterceptorResult.PassThrough;
            }

            uint remoteChannel = reader.ReadUInt32();
            uint initialWindowSize = reader.ReadUInt32();
            uint maxPacketSize = reader.ReadUInt32();
            string addressConnected = reader.ReadString();
            uint portConnected = reader.ReadUInt32();
            string originatorIp = reader.ReadString();
            uint originatorPort = reader.ReadUInt32();

            _protocolEventManager.Trace(
                "forwarded-tcpip : address={0} port={1} originatorIP={2} originatorPort={3} windowSize={4} maxPacketSize={5}",
                addressConnected, portConnected, originatorIp, originatorPort, initialWindowSize, maxPacketSize);

            // reject the request if we don't know the port number.
            PortInfo portInfo;
            if (!_portDict.TryGetValue(portConnected, out portInfo)) {
                RejectForwardedTcpIp(remoteChannel, "Cannot accept the request");
                return SSHPacketInterceptorResult.Consumed;
            }

            RemotePortForwardingRequest requestInfo =
                new RemotePortForwardingRequest(addressConnected, portConnected, originatorIp, originatorPort);

            // create a temporary channel
            var channel = portInfo.CreateChannel(requestInfo, remoteChannel, initialWindowSize, maxPacketSize);

            _protocolEventManager.Trace("new port-forwarding channel : local={0} remote={1}", channel.LocalChannel, channel.RemoteChannel);

            // check the request by the request handler
            RemotePortForwardingReply reply;
            try {
                reply = portInfo.RequestHandler.OnRemotePortForwardingRequest(requestInfo, channel);
            }
            catch (Exception) {
                RejectForwardedTcpIp(remoteChannel, "Cannot accept the request");
                return SSHPacketInterceptorResult.Consumed;
            }

            if (!reply.Accepted) {
                RejectForwardedTcpIp(remoteChannel, reply.ReasonMessage, (uint)reply.ReasonCode);
                return SSHPacketInterceptorResult.Consumed;
            }

            // register a channel to the connection object
            portInfo.RegisterChannel(channel, reply.EventHandler);

            // send SSH_MSG_CHANNEL_OPEN_CONFIRMATION
            channel.SendOpenConfirmation();

            return SSHPacketInterceptorResult.Consumed;
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.ConnectionClosed) {
                    _sequenceStatus = SequenceStatus.ConnectionClosed;
                    DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                    _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
                    Monitor.PulseAll(_sequenceLock);
                }
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_OPEN_FAILURE for rejecting the request.
        /// </summary>
        /// <param name="remoteChannel">remote channel number</param>
        /// <param name="description">description</param>
        /// <param name="reasonCode">reason code</param>
        private void RejectForwardedTcpIp(
                uint remoteChannel,
                string description,
                uint reasonCode = SSH2ChannelOpenFailureCode.SSH_OPEN_ADMINISTRATIVELY_PROHIBITED) {

            var packet = new SSH2ChannelOpenFailurePacket(remoteChannel, description, reasonCode);
            _syncHandler.Send(packet);
            _protocolEventManager.Trace("reject forwarded-tcpip : {0}", description);
        }

        /// <summary>
        /// Builds SSH_MSG_GLOBAL_REQUEST "tcpip-forward" packet.
        /// </summary>
        /// <param name="addressToBind">IP address to bind</param>
        /// <param name="portNumberToBind">port number to bind</param>
        /// <returns></returns>
        private SSH2Packet BuildTcpIpForwardPacket(string addressToBind, uint portNumberToBind) {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_GLOBAL_REQUEST)
                    .WriteString("tcpip-forward")
                    .WriteBool(true)    // want reply
                    .WriteString(addressToBind)
                    .WriteUInt32(portNumberToBind);
        }

        /// <summary>
        /// Builds SSH_MSG_GLOBAL_REQUEST "cancel-tcpip-forward" packet.
        /// </summary>
        /// <param name="addressToBind">IP address to bind</param>
        /// <param name="portNumberToBind">port number to bind</param>
        /// <returns></returns>
        private SSH2Packet BuildCancelTcpIpForwardPacket(string addressToBind, uint portNumberToBind) {
            return new SSH2Packet(SSH2PacketType.SSH_MSG_GLOBAL_REQUEST)
                    .WriteString("cancel-tcpip-forward")
                    .WriteBool(true)    // want reply
                    .WriteString(addressToBind)
                    .WriteUInt32(portNumberToBind);
        }

        /// <summary>
        /// Starts remote port forwarding.
        /// </summary>
        /// <param name="requestHandler">request handler</param>
        /// <param name="createChannel">a function for creating a new channel object</param>
        /// <param name="registerChannel">a function for registering a new channel object</param>
        /// <param name="addressToBind">IP address to bind on the server</param>
        /// <param name="portNumberToBind">port number to bind on the server. "0" means that the next available port is used.</param>
        /// <returns>true if the remote port forwarding has been started.</returns>
        public bool ListenForwardedPort(
                IRemotePortForwardingHandler requestHandler,
                CreateChannelFunc createChannel,
                RegisterChannelFunc registerChannel,
                string addressToBind,
                uint portNumberToBind) {

            IRemotePortForwardingHandler requestHandlerWrapper =
                new RemotePortForwardingHandlerIgnoreErrorWrapper(requestHandler);

            uint portNumberBound;
            bool success = ListenForwardedPortCore(
                                requestHandlerWrapper,
                                createChannel,
                                registerChannel,
                                addressToBind,
                                portNumberToBind,
                                out portNumberBound);
            if (success) {
                requestHandlerWrapper.OnRemotePortForwardingStarted(portNumberBound);
            }
            else {
                requestHandlerWrapper.OnRemotePortForwardingFailed();
            }

            return success;
        }

        private bool ListenForwardedPortCore(
                IRemotePortForwardingHandler requestHandler,
                CreateChannelFunc createChannel,
                RegisterChannelFunc registerChannel,
                string addressToBind,
                uint portNumberToBind,
                out uint portNumberBound) {

            portNumberBound = 0;

            lock (_sequenceLock) {
                while (_sequenceStatus != SequenceStatus.Idle) {
                    if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                        return false;
                    }
                    Monitor.Wait(_sequenceLock);
                }

                _receivedPacket.Clear();
                _sequenceStatus = SequenceStatus.WaitTcpIpForwardResponse;
            }

            var packet = BuildTcpIpForwardPacket(addressToBind, portNumberToBind);
            _syncHandler.Send(packet);

            _protocolEventManager.Trace("tcpip-forward : address={0} port={1}", addressToBind, portNumberToBind);

            DataFragment response = null;
            bool accepted = false;
            if (_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                lock (_sequenceLock) {
                    if (_sequenceStatus == SequenceStatus.TcpIpForwardSuccess) {
                        accepted = true;
                        if (portNumberToBind != 0) {
                            portNumberBound = portNumberToBind;
                        }
                        else {
                            SSH2DataReader reader = new SSH2DataReader(response);
                            reader.ReadByte();  // message number
                            portNumberBound = reader.ReadUInt32();
                        }
                        _portDict.TryAdd(
                            portNumberBound,
                            new PortInfo(requestHandler, createChannel, registerChannel));
                    }
                }
            }

            if (accepted) {
                _protocolEventManager.Trace("tcpip-forward succeeded : bound port={0}", portNumberBound);
            }
            else {
                _protocolEventManager.Trace("tcpip-forward failed");
            }

            lock (_sequenceLock) {
                // reset status
                _sequenceStatus = SequenceStatus.Idle;
                Monitor.PulseAll(_sequenceLock);
            }

            return accepted;
        }

        /// <summary>
        /// Cancels the remote port forwarding.
        /// </summary>
        /// <param name="addressToBind">IP address to bind on the server</param>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <returns>true if the remote port forwarding has been cancelled.</returns>
        public bool CancelForwardedPort(string addressToBind, uint portNumberToBind) {
            lock (_sequenceLock) {
                while (_sequenceStatus != SequenceStatus.Idle) {
                    if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                        return false;
                    }
                    Monitor.Wait(_sequenceLock);
                }

                _receivedPacket.Clear();
                _sequenceStatus = SequenceStatus.WaitCancelTcpIpForwardResponse;
            }

            var packet = BuildCancelTcpIpForwardPacket(addressToBind, portNumberToBind);
            _syncHandler.Send(packet);

            _protocolEventManager.Trace("cancel-tcpip-forward : address={0} port={1}", addressToBind, portNumberToBind);

            DataFragment response = null;
            bool accepted = false;
            if (_receivedPacket.TryGet(ref response, _timeouts.ResponseTimeout)) {
                lock (_sequenceLock) {
                    if (_sequenceStatus == SequenceStatus.CancelTcpIpForwardSuccess) {
                        accepted = true;
                        if (portNumberToBind == 0) {
                            _portDict.Clear();
                        }
                        else {
                            PortInfo oldVal;
                            _portDict.TryRemove(portNumberToBind, out oldVal);
                        }
                    }
                }
            }

            if (accepted) {
                _protocolEventManager.Trace("cancel-tcpip-forward succeeded");
            }
            else {
                _protocolEventManager.Trace("cancel-tcpip-forward failed");
            }

            lock (_sequenceLock) {
                // reset status
                _sequenceStatus = SequenceStatus.Idle;
                Monitor.PulseAll(_sequenceLock);
            }

            return accepted;
        }

        #endregion
    }

    /// <summary>
    /// Class for supporting OpenSSH's agent forwarding
    /// </summary>
    internal class SSH2OpenSSHAgentForwarding : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;

        public delegate SSH2OpenSSHAgentForwardingChannel CreateChannelFunc(uint remoteChannel, uint serverWindowSize, uint serverMaxPacketSize);
        public delegate void RegisterChannelFunc(SSH2OpenSSHAgentForwardingChannel channel, ISSHChannelEventHandler eventHandler);

        private readonly IAgentForwardingAuthKeyProvider _authKeyProvider;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSHProtocolEventManager _protocolEventManager;
        private readonly CreateChannelFunc _createChannel;
        private readonly RegisterChannelFunc _registerChannel;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private enum SequenceStatus {
            /// <summary>Idle</summary>
            Idle,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH2OpenSSHAgentForwarding(
                SSH2SynchronousPacketHandler syncHandler,
                IAgentForwardingAuthKeyProvider authKeyProvider,
                SSHProtocolEventManager protocolEventManager,
                CreateChannelFunc createChannel,
                RegisterChannelFunc registerChannel) {

            _authKeyProvider = authKeyProvider;
            _syncHandler = syncHandler;
            _protocolEventManager = protocolEventManager;
            _createChannel = createChannel;
            _registerChannel = registerChannel;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH2PacketType packetType = (SSH2PacketType)packet[0];
            if (packetType != SSH2PacketType.SSH_MSG_CHANNEL_OPEN) {
                return SSHPacketInterceptorResult.PassThrough;
            }

            SSH2DataReader reader = new SSH2DataReader(packet);
            reader.ReadByte();  // skip packet type (message number)
            string channelType = reader.ReadString();
            if (channelType != "auth-agent@openssh.com") {
                return SSHPacketInterceptorResult.PassThrough;
            }

            uint remoteChannel = reader.ReadUInt32();
            uint initialWindowSize = reader.ReadUInt32();
            uint maxPacketSize = reader.ReadUInt32();

            if (_authKeyProvider == null || !_authKeyProvider.IsAuthKeyProviderEnabled) {
                RejectAuthAgent(remoteChannel, "Cannot accept the request");
                _protocolEventManager.Trace("auth-agent@openssh.com : rejected");
                return SSHPacketInterceptorResult.Consumed;
            }

            _protocolEventManager.Trace(
                "auth-agent@openssh.com : windowSize={0} maxPacketSize={1}", initialWindowSize, maxPacketSize);

            // create a channel
            var channel = _createChannel(remoteChannel, initialWindowSize, maxPacketSize);

            _protocolEventManager.Trace("new agent forwarding channel : local={0} remote={1}", channel.LocalChannel, channel.RemoteChannel);

            // create a handler
            var handler = new OpenSSHAgentForwardingMessageHandler(channel, _authKeyProvider);

            // register a channel to the connection object
            _registerChannel(channel, handler);

            // send SSH_MSG_CHANNEL_OPEN_CONFIRMATION
            channel.SendOpenConfirmation();

            return SSHPacketInterceptorResult.Consumed;
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.ConnectionClosed) {
                    _sequenceStatus = SequenceStatus.ConnectionClosed;
                    DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                    _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
                    Monitor.PulseAll(_sequenceLock);
                }
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_OPEN_FAILURE for rejecting the request.
        /// </summary>
        /// <param name="remoteChannel">remote channel number</param>
        /// <param name="description">description</param>
        private void RejectAuthAgent(uint remoteChannel, string description) {
            var packet = new SSH2ChannelOpenFailurePacket(remoteChannel, description);
            _syncHandler.Send(packet);
            _protocolEventManager.Trace("reject auth-agent@openssh.com : {0}", description);
        }

        #endregion
    }

    /// <summary>
    /// Class for supporting X11 forwarding
    /// </summary>
    internal class SSH2X11Forwarding : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;

        public delegate SSH2X11ForwardingChannel CreateChannelFunc(uint remoteChannel, uint serverWindowSize, uint serverMaxPacketSize);
        public delegate void RegisterChannelFunc(SSH2X11ForwardingChannel channel, ISSHChannelEventHandler eventHandler);

        private readonly X11ConnectionManager _x11ConnectionManager;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSHProtocolEventManager _protocolEventManager;
        private readonly CreateChannelFunc _createChannel;
        private readonly RegisterChannelFunc _registerChannel;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private enum SequenceStatus {
            /// <summary>Idle</summary>
            Idle,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH2X11Forwarding(
                SSH2SynchronousPacketHandler syncHandler,
                SSHProtocolEventManager protocolEventManager,
                X11ConnectionManager x11ConnectionManager,
                CreateChannelFunc createChannel,
                RegisterChannelFunc registerChannel) {

            _x11ConnectionManager = x11ConnectionManager;
            _syncHandler = syncHandler;
            _protocolEventManager = protocolEventManager;
            _createChannel = createChannel;
            _registerChannel = registerChannel;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH2PacketType packetType = (SSH2PacketType)packet[0];
            if (packetType != SSH2PacketType.SSH_MSG_CHANNEL_OPEN) {
                return SSHPacketInterceptorResult.PassThrough;
            }

            SSH2DataReader reader = new SSH2DataReader(packet);
            reader.ReadByte();  // skip packet type (message number)
            string channelType = reader.ReadString();
            if (channelType != "x11") {
                return SSHPacketInterceptorResult.PassThrough;
            }

            uint remoteChannel = reader.ReadUInt32();
            uint initialWindowSize = reader.ReadUInt32();
            uint maxPacketSize = reader.ReadUInt32();
            string originatorAddr = reader.ReadString();
            uint originatorPort = reader.ReadUInt32();

            if (_x11ConnectionManager == null || !_x11ConnectionManager.SetupDone) {
                RejectX11(remoteChannel, "Cannot accept the request");
                _protocolEventManager.Trace("x11 : rejected");
                return SSHPacketInterceptorResult.Consumed;
            }

            _protocolEventManager.Trace(
                "x11 : windowSize={0} maxPacketSize={1} originator={2}:{3}",
                initialWindowSize, maxPacketSize, originatorAddr, originatorPort);

            IPAddress ipaddr;
            if (IPAddress.TryParse(originatorAddr, out ipaddr)) {
                if (!IPAddress.IsLoopback(ipaddr)) {
                    RejectX11(remoteChannel, "Cannot accept the request");
                    _protocolEventManager.Trace("x11 : rejected");
                    return SSHPacketInterceptorResult.Consumed;
                }
            }

            // create a channel
            var channel = _createChannel(remoteChannel, initialWindowSize, maxPacketSize);

            _protocolEventManager.Trace("new X11 forwarding channel : local={0} remote={1}", channel.LocalChannel, channel.RemoteChannel);

            // create a handler
            var handler = _x11ConnectionManager.CreateChannelHandler(channel);

            // register a channel to the connection object
            _registerChannel(channel, handler);

            // send SSH_MSG_CHANNEL_OPEN_CONFIRMATION
            channel.SendOpenConfirmation();

            return SSHPacketInterceptorResult.Consumed;
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnConnectionClosed() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.ConnectionClosed) {
                    _sequenceStatus = SequenceStatus.ConnectionClosed;
                    DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                    _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
                    Monitor.PulseAll(_sequenceLock);
                }
            }
        }

        /// <summary>
        /// Sends SSH_MSG_CHANNEL_OPEN_FAILURE for rejecting the request.
        /// </summary>
        /// <param name="remoteChannel">remote channel number</param>
        /// <param name="description">description</param>
        private void RejectX11(uint remoteChannel, string description) {
            var packet = new SSH2ChannelOpenFailurePacket(remoteChannel, description);
            _syncHandler.Send(packet);
            _protocolEventManager.Trace("reject x11 : {0}", description);
        }

        #endregion
    }

    /// <summary>
    /// SSH_MSG_CHANNEL_OPEN_FAILURE packet.
    /// </summary>
    internal class SSH2ChannelOpenFailurePacket : SSH2Packet {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="remoteChannel">remote channel number</param>
        /// <param name="description">description</param>
        /// <param name="reasonCode">reason code</param>
        public SSH2ChannelOpenFailurePacket(
                uint remoteChannel,
                string description,
                uint reasonCode = SSH2ChannelOpenFailureCode.SSH_OPEN_ADMINISTRATIVELY_PROHIBITED)
            : base(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE) {

            this.WriteUInt32(remoteChannel)
                .WriteUInt32(reasonCode)
                .WriteUTF8String(description)
                .WriteString("");   // lang tag
        }
    }
}
