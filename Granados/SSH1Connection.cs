/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

  I implemented this algorithm with reference to following products and books though the algorithm is known publicly.
    * MindTerm ( AppGate Network Security )
    * Applied Cryptography ( Bruce Schneier )

 $Id: SSH1Connection.cs,v 1.4 2011/11/08 12:24:05 kzmi Exp $
*/
using Granados.AgentForwarding;
using Granados.Crypto;
using Granados.IO;
using Granados.IO.SSH1;
using Granados.Mono.Math;
using Granados.PKI;
using Granados.PortForwarding;
using Granados.SSH;
using Granados.Util;

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Granados.SSH1 {

    /// <summary>
    /// SSH1
    /// </summary>
    public sealed class SSH1Connection : SSHConnection {

        private const int AUTH_NOT_REQUIRED = 0;
        private const int AUTH_REQUIRED = 1;

        private readonly SSHConnectionParameter _param;
        private readonly SSHProtocolEventManager _protocolEventManager;

        private readonly SSHChannelCollection _channelCollection;
        private SSH1InteractiveSession _interactiveSession;

        private readonly SSH1Packetizer _packetizer;
        private readonly SSH1SynchronousPacketHandler _syncHandler;
        private readonly SSHPacketInterceptorCollection _packetInterceptors;
        private readonly SSH1KeyExchanger _keyExchanger;

        private readonly Lazy<SSH1RemotePortForwarding> _remotePortForwarding;
        private readonly Lazy<SSH1AgentForwarding> _agentForwarding;

        private readonly SSH1ConnectionInfo _cInfo;

        private int _remotePortForwardCount = 0;

        public SSH1Connection(SSHConnectionParameter param, IGranadosSocket socket, ISSHConnectionEventReceiver er, ISSHProtocolEventListener protocolEventListener, string serverVersion, string clientVersion)
            : base(param, socket, er) {
            _param = param.Clone();
            _protocolEventManager = new SSHProtocolEventManager(protocolEventListener);
            _channelCollection = new SSHChannelCollection();
            _interactiveSession = null;

            _cInfo = new SSH1ConnectionInfo(param.HostName, param.PortNumber, serverVersion, clientVersion);

            IDataHandler adapter = new DataHandlerAdapter(
                            (data) => {
                                AsyncReceivePacket(data);
                            },
                            () => {
                                OnConnectionClosed();
                                EventReceiver.OnConnectionClosed();
                            },
                            (error) => {
                                EventReceiver.OnError(error);
                            }
                        );
            _syncHandler = new SSH1SynchronousPacketHandler(socket, adapter, _protocolEventManager);
            _packetizer = new SSH1Packetizer(_syncHandler);

            _packetInterceptors = new SSHPacketInterceptorCollection();
            _keyExchanger = new SSH1KeyExchanger(this, _syncHandler, _param, _cInfo, UpdateClientKey, UpdateServerKey);
            _packetInterceptors.Add(_keyExchanger);

            _remotePortForwarding = new Lazy<SSH1RemotePortForwarding>(CreateRemotePortForwarding);
            _agentForwarding = new Lazy<SSH1AgentForwarding>(CreateAgentForwarding);
        }

        public SSHConnectionParameter Param {
            get {
                return _param;
            }
        }

        internal override IDataHandler Packetizer {
            get {
                return _packetizer;
            }
        }

        internal override AuthenticationResult Connect() {
            try {
                //key exchange
                Task kexTask = _keyExchanger.StartKeyExchange();
                kexTask.Wait();

                SSH1UserAuthentication userAuth = new SSH1UserAuthentication(this, _param, _syncHandler, _sessionID);
                _packetInterceptors.Add(userAuth);
                Task userAuthTask = userAuth.StartAuthentication();
                userAuthTask.Wait();

                return AuthenticationResult.Success;
            }
            catch (Exception ex) {
                Close();
                if (ex is AggregateException) {
                    Exception actualException = ((AggregateException)ex).InnerException;
                    throw new SSHException(actualException.Message, actualException);
                }
                throw;
            }
        }

        protected override void SendMyVersion() {
            string cv = SSHUtil.ClientVersionString(SSHProtocol.SSH1);
            string cv2 = cv + _param.VersionEOL;
            byte[] data = Encoding.ASCII.GetBytes(cv2);
            _stream.Write(data, 0, data.Length);
            _protocolEventManager.Trace("client version-string : {0}", cv);
        }

        private void OnConnectionClosed() {
            _packetInterceptors.OnConnectionClosed();
        }


        internal void Transmit(SSH1Packet packet) {
            _syncHandler.Send(packet);
        }

        public override void Disconnect(string msg) {
            if (!this.IsOpen)
                return;
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_MSG_DISCONNECT)
                    .WriteString(msg)
            );
            base.Close();
        }

        public override void SendIgnorableData(string msg) {
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_MSG_IGNORE)
                    .WriteString(msg)
            );
        }

        public override THandler OpenSubsystem<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string subsystemName) {
            throw new NotSupportedException("OpenSubsystem is not supported on the SSH1 connection.");
        }

        /// <summary>
        /// Create a new channel (initialted by the client)
        /// </summary>
        /// <returns></returns>
        private THandler CreateChannelByClient<TChannel, THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, Func<uint, TChannel> channelCreator, Action<TChannel> initiate)
            where TChannel : SSH1ChannelBase
            where THandler : ISSHChannelEventHandler {

            uint localChannel = _channelCollection.GetNewChannelNumber();
            var channel = channelCreator(localChannel);
            var eventHandler = handlerCreator(channel);
            channel.SetHandler(eventHandler);

            _channelCollection.Add(channel, eventHandler);

            try {
                initiate(channel);
            }
            catch (Exception) {
                DetachChannel(channel);
                throw;
            }

            return eventHandler;
        }

        /// <summary>
        /// Detach channel object.
        /// </summary>
        /// <param name="channelOperator">a channel operator</param>
        private void DetachChannel(ISSHChannel channelOperator) {
            if (Object.ReferenceEquals(channelOperator, _interactiveSession)) {
                _interactiveSession = null;
            }
            var handler = _channelCollection.FindHandler(channelOperator.LocalChannel);
            _channelCollection.Remove(channelOperator);
            if (handler != null) {
                handler.Dispose();
            }
        }

        public override THandler OpenShell<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator) {
            if (_interactiveSession != null) {
                throw new SSHException(Strings.GetString("OnlySingleInteractiveSessionCanBeStarted"));
            }

            if (_param.AgentForwardingAuthKeyProvider != null) {
                bool started = _agentForwarding.Value.StartAgentForwarding();
                if (!started) {
                    _protocolEventManager.Trace("the request of the agent forwarding has been rejected.");
                }
                else {
                    _protocolEventManager.Trace("the request of the agent forwarding has been accepted.");
                }
            }

            return CreateChannelByClient(
                        handlerCreator,
                        localChannel =>
                            new SSH1InteractiveSession(
                                DetachChannel, this, _protocolEventManager, localChannel, ChannelType.Shell, "Shell"),
                        channel => {
                            _interactiveSession = channel;
                            channel.ExecShell(_param);
                        }
                    );
        }

        public override THandler ExecCommand<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string command) {
            if (_interactiveSession != null) {
                throw new SSHException(Strings.GetString("OnlySingleInteractiveSessionCanBeStarted"));
            }

            return CreateChannelByClient(
                        handlerCreator,
                        localChannel =>
                            new SSH1InteractiveSession(
                                DetachChannel, this, _protocolEventManager, localChannel, ChannelType.ExecCommand, "ExecCommand"),
                        channel => {
                            _interactiveSession = channel;
                            channel.ExecCommand(_param, command);
                        }
                    );
        }

        public override THandler ForwardPort<THandler>(
                SSHChannelEventHandlerCreator<THandler> handlerCreator, string remoteHost, uint remotePort, string originatorIp, uint originatorPort) {

            StartIdleInteractiveSession();

            return CreateChannelByClient(
                        handlerCreator,
                        localChannel => new SSH1LocalPortForwardingChannel(
                                            DetachChannel, this, _protocolEventManager, localChannel,
                                            remoteHost, remotePort, originatorIp, originatorPort),
                        channel => {
                            channel.SendOpen();
                        }
                    );
        }

        public override bool ListenForwardedPort(IRemotePortForwardingHandler requestHandler, string addressToBind, uint portNumberToBind) {

            SSH1RemotePortForwarding.CreateChannelFunc createChannel =
                (requestInfo, remoteChannel) => {
                    uint localChannel = _channelCollection.GetNewChannelNumber();
                    return new SSH1RemotePortForwardingChannel(
                                    DetachChannel,
                                    this,
                                    _protocolEventManager,
                                    localChannel,
                                    remoteChannel
                                );
                };

            SSH1RemotePortForwarding.RegisterChannelFunc registerChannel =
                (channel, eventHandler) => {
                    channel.SetHandler(eventHandler);
                    _channelCollection.Add(channel, eventHandler);
                };

            // Note:
            //  According to the SSH 1.5 protocol specification, the client has to specify host and port
            //  the connection should be forwarded to.
            //  For keeping the interface compatible with SSH2, we use generated host-port pair that indicates
            //  which port on the server is listening.
            string hostToConnect = "granados" + Interlocked.Increment(ref _remotePortForwardCount).ToString(NumberFormatInfo.InvariantInfo);
            uint portToConnect = portNumberToBind;

            return _remotePortForwarding.Value.ListenForwardedPort(
                    requestHandler, createChannel, registerChannel, portNumberToBind, hostToConnect, portToConnect);
        }

        public override bool CancelForwardedPort(string addressToBind, uint portNumberToBind) {
            throw new NotSupportedException("cancellation of the port forwarding is not supported");
        }

        private SSH1RemotePortForwarding CreateRemotePortForwarding() {
            var instance = new SSH1RemotePortForwarding(_syncHandler, StartIdleInteractiveSession);
            _packetInterceptors.Add(instance);
            return instance;
        }

        private SSH1AgentForwarding CreateAgentForwarding() {
            var instance = new SSH1AgentForwarding(
                            _syncHandler,
                            _param.AgentForwardingAuthKeyProvider,
                            remoteChannel => {
                                uint localChannel = _channelCollection.GetNewChannelNumber();
                                return new SSH1AgentForwardingChannel(
                                                DetachChannel, this, _protocolEventManager, localChannel, remoteChannel);
                            },
                            (channel, eventHandler) => {
                                channel.SetHandler(eventHandler);
                                _channelCollection.Add(channel, eventHandler);
                            }
                        );
            _packetInterceptors.Add(instance);
            return instance;
        }

        /// <summary>
        /// Start idle interactive session with opening shell.
        /// </summary>
        private void StartIdleInteractiveSession() {
            if (_interactiveSession != null) {
                return;
            }
            OpenShell(channel => new SimpleSSHChannelEventHandler());
        }

        private void ProcessPortforwardingRequest(ISSHConnectionEventReceiver receiver, SSH1DataReader reader) {
            /*
            int server_channel = reader.ReadInt32();
            string host = reader.ReadString();
            int port = reader.ReadInt32();

            PortForwardingCheckResult result = receiver.CheckPortForwardingRequest(host, port, "", 0);
            if (result.allowed) {
                int local_id = _channel_collection.RegisterChannelEventReceiver(null, result.channel).LocalID;
                _eventReceiver.EstablishPortforwarding(result.channel, new SSH1Channel(this, ChannelType.ForwardedRemoteToLocal, local_id, server_channel));

                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION)
                        .WriteInt32(server_channel)
                        .WriteInt32(local_id)
                );
            }
            else {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        .WriteInt32(server_channel)
                );
            }
             */
        }

        private byte[] CalcSessionID() {
            MemoryStream bos = new MemoryStream();
            byte[] h = _cInfo.HostKey.Modulus.GetBytes();
            byte[] s = _cInfo.ServerKey.Modulus.GetBytes();
            //System.out.println("len h="+h.Length);
            //System.out.println("len s="+s.Length);

            int off_h = (h[0] == 0 ? 1 : 0);
            int off_s = (s[0] == 0 ? 1 : 0);
            bos.Write(h, off_h, h.Length - off_h);
            bos.Write(s, off_s, s.Length - off_s);
            bos.Write(_cInfo.AntiSpoofingCookie, 0, _cInfo.AntiSpoofingCookie.Length);

            byte[] session_id;
            using (var md5 = new MD5CryptoServiceProvider()) {
                session_id = md5.ComputeHash(bos.ToArray());
            }
            //System.out.println("sess-id-len=" + session_id.Length);
            return session_id;
        }

        private void UpdateClientKey(Cipher cipherClient) {
            _packetizer.SetCipher(cipherClient, _param.CheckMACError);
        }

        private void UpdateServerKey(byte[] sessionId, Cipher cipherServer) {
            _sessionID = sessionId;
            _syncHandler.SetCipher(cipherServer);
        }

        //init ciphers
        private void InitCipher(byte[] session_key) {
            Cipher cipherServer = CipherFactory.CreateCipher(SSHProtocol.SSH1, _cInfo.OutgoingPacketCipher.Value, session_key);
            Cipher cipherLocal = CipherFactory.CreateCipher(SSHProtocol.SSH1, _cInfo.IncomingPacketCipher.Value, session_key);
            _syncHandler.SetCipher(cipherServer);
            _packetizer.SetCipher(cipherLocal, _param.CheckMACError);
        }

        private DataFragment ReceivePacket() {
            while (true) {
                DataFragment data = _syncHandler.WaitResponse(10000);

                SSH1PacketType pt = (SSH1PacketType)data[0]; //shortcut
                if (pt == SSH1PacketType.SSH_MSG_IGNORE) {
                    SSH1DataReader r = new SSH1DataReader(data);
                    r.ReadByte();
                    if (_eventReceiver != null)
                        _eventReceiver.OnIgnoreMessage(r.ReadByteString());
                }
                else if (pt == SSH1PacketType.SSH_MSG_DEBUG) {
                    SSH1DataReader r = new SSH1DataReader(data);
                    r.ReadByte();
                    if (_eventReceiver != null)
                        _eventReceiver.OnDebugMessage(false, r.ReadString());
                }
                else
                    return data;
            }
        }

        internal void AsyncReceivePacket(DataFragment packet) {
            try {
                ProcessPacket(packet);
            }
            catch (Exception ex) {
                _eventReceiver.OnError(ex);
            }
        }

        private void ProcessPacket(DataFragment packet) {
            if (_packetInterceptors.InterceptPacket(packet)) {
                return;
            }

            if (packet.Length < 1) {
                return; // invalid packet
            }

            SSH1DataReader reader = new SSH1DataReader(packet);
            SSH1PacketType pt = (SSH1PacketType)reader.ReadByte();
            switch (pt) {
                case SSH1PacketType.SSH_SMSG_STDOUT_DATA:
                case SSH1PacketType.SSH_SMSG_STDERR_DATA:
                case SSH1PacketType.SSH_SMSG_SUCCESS:
                case SSH1PacketType.SSH_SMSG_FAILURE:
                case SSH1PacketType.SSH_SMSG_EXITSTATUS: {
                        SSH1InteractiveSession interactiveSession = _interactiveSession;
                        if (interactiveSession != null) {
                            interactiveSession.ProcessPacket(pt, reader.GetRemainingDataView());
                        }
                    }
                    break;
                case SSH1PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION:
                case SSH1PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE:
                case SSH1PacketType.SSH_MSG_CHANNEL_DATA:
                case SSH1PacketType.SSH_MSG_CHANNEL_CLOSE:
                case SSH1PacketType.SSH_MSG_CHANNEL_CLOSE_CONFIRMATION: {
                        uint localChannel = reader.ReadUInt32();
                        var channelOperator = _channelCollection.FindOperator(localChannel) as SSH1ChannelBase;
                        if (channelOperator != null) {
                            channelOperator.ProcessPacket(pt, reader.GetRemainingDataView());
                        }
                    }
                    break;
                default:
                    _eventReceiver.OnUnknownMessage((byte)pt, packet.GetBytes());
                    break;
            }
        }

        private SSH1PacketType SneakPacketType(DataFragment data) {
            return (SSH1PacketType)data[0];
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class SSH1Channel : SSHChannel {

        private SSH1Connection _connection;

        public SSH1Channel(SSH1Connection con, ChannelType type, int local_id)
            : base(con, type, local_id) {
            _connection = con;
        }
        public SSH1Channel(SSH1Connection con, ChannelType type, int local_id, int remote_id)
            : base(con, type, local_id) {
            _connection = con;
            _remoteID = remote_id;
        }

        /**
         * resizes the size of terminal
         */
        public override void ResizeTerminal(int width, int height, int pixel_width, int pixel_height) {
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_WINDOW_SIZE)
                    .WriteInt32(height)
                    .WriteInt32(width)
                    .WriteInt32(pixel_width)
                    .WriteInt32(pixel_height)
            );
        }

        /**
        * transmits channel data 
        */
        public override void Transmit(byte[] data) {
            if (_type == ChannelType.Shell) {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_STDIN_DATA)
                        .WriteAsString(data)
                );
            }
            else {
                Transmit(
                   new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_DATA)
                        .WriteInt32(_remoteID)
                        .WriteAsString(data)
                );
            }
        }
        /**
        * transmits channel data 
        */
        public override void Transmit(byte[] data, int offset, int length) {
            if (_type == ChannelType.Shell) {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_STDIN_DATA)
                        .WriteAsString(data, offset, length)
                );
            }
            else {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_DATA)
                        .WriteInt32(_remoteID)
                        .WriteAsString(data, offset, length)
                );
            }
        }

        public override void SendEOF() {
        }


        /**
         * closes this channel
         */
        public override void Close() {
            if (!_connection.IsOpen)
                return;

            if (_type == ChannelType.Shell) {
                Transmit(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_EOF)
                        .WriteInt32(_remoteID)
                );
            }

            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_CLOSE)
                    .WriteInt32(_remoteID)
            );
        }

        private void Transmit(SSH1Packet p) {
            _connection.Transmit(p);
        }

    }

    /// <summary>
    /// Synchronization of sending/receiving packets.
    /// </summary>
    internal class SSH1SynchronousPacketHandler : AbstractSynchronousPacketHandler<SSH1Packet> {
        #region SSH1SynchronousPacketHandler

        private readonly object _cipherSync = new object();
        private Cipher _cipher = null;

        private readonly SSHProtocolEventManager _protocolEventManager;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket object for sending packets.</param>
        /// <param name="handler">the next handler received packets are redirected to.</param>
        /// <param name="protocolEventManager">protocol event manager</param>
        public SSH1SynchronousPacketHandler(IGranadosSocket socket, IDataHandler handler, SSHProtocolEventManager protocolEventManager)
            : base(socket, handler) {

            _protocolEventManager = protocolEventManager;
        }

        /// <summary>
        /// Set cipher settings.
        /// </summary>
        /// <param name="cipher">cipher to encrypt a packet to be sent.</param>
        public void SetCipher(Cipher cipher) {
            lock (_cipherSync) {
                _cipher = cipher;
            }
        }

        /// <summary>
        /// Do additional work for a packet to be sent.
        /// </summary>
        /// <param name="packet">a packet object</param>
        protected override void BeforeSend(SSH1Packet packet) {
            SSH1PacketType packetType = packet.GetPacketType();
            switch (packetType) {
                case SSH1PacketType.SSH_CMSG_STDIN_DATA:
                case SSH1PacketType.SSH_SMSG_STDOUT_DATA:
                case SSH1PacketType.SSH_SMSG_STDERR_DATA:
                case SSH1PacketType.SSH_MSG_CHANNEL_DATA:
                case SSH1PacketType.SSH_MSG_IGNORE:
                case SSH1PacketType.SSH_MSG_DEBUG:
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

            SSH1PacketType packetType = (SSH1PacketType)packet.Data[packet.Offset];
            switch (packetType) {
                case SSH1PacketType.SSH_CMSG_STDIN_DATA:
                case SSH1PacketType.SSH_SMSG_STDOUT_DATA:
                case SSH1PacketType.SSH_SMSG_STDERR_DATA:
                case SSH1PacketType.SSH_MSG_CHANNEL_DATA:
                case SSH1PacketType.SSH_MSG_IGNORE:
                case SSH1PacketType.SSH_MSG_DEBUG:
                    return;
            }

            _protocolEventManager.NotifyReceive(packetType, String.Empty);
        }

        /// <summary>
        /// Gets the binary image of the packet to be sent.
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>binary image of the packet</returns>
        protected override DataFragment GetPacketImage(SSH1Packet packet) {
            lock (_cipherSync) {
                return packet.GetImage(_cipher);
            }
        }

        /// <summary>
        /// Gets the packet type name of the packet to be sent. (for debugging)
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>packet name.</returns>
        protected override string GetMessageName(SSH1Packet packet) {
            return packet.GetPacketType().ToString();
        }

        /// <summary>
        /// Gets the packet type name of the received packet. (for debugging)
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>packet name.</returns>
        protected override string GetMessageName(DataFragment packet) {
            if (packet.Length > 0) {
                return ((SSH1PacketType)packet.Data[packet.Offset]).ToString();
            }
            else {
                return "?";
            }
        }

        #endregion
    }

    /// <summary>
    /// Class for supporting key exchange sequence.
    /// </summary>
    internal class SSH1KeyExchanger : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

        private enum SequenceStatus {
            /// <summary>next key exchange can be started</summary>
            Idle,
            /// <summary>key exchange has been succeeded</summary>
            Succeeded,
            /// <summary>key exchange has been failed</summary>
            Failed,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>waiting for SSH_SMSG_PUBLIC_KEY</summary>
            WaitPublicKey,
            /// <summary>SSH_SMSG_PUBLIC_KEY has been received.</summary>
            PublicKeyReceived,
            /// <summary>SSH_CMSG_SESSION_KEY has been sent. waiting for SSH_SMSG_SUCCESS|SSH_SMSG_FAILURE</summary>
            WaitSessionKeyResult,
        }

        public delegate void UpdateClientKeyDelegate(Cipher cipherClient);
        public delegate void UpdateServerKeyDelegate(byte[] sessionID, Cipher cipherServer);

        private readonly UpdateClientKeyDelegate _updateClientKey;
        private readonly UpdateServerKeyDelegate _updateServerKey;

        private readonly SSH1Connection _connection;
        private readonly SSH1SynchronousPacketHandler _syncHandler;
        private readonly SSHConnectionParameter _param;
        private readonly SSH1ConnectionInfo _cInfo;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private Task _kexTask;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="syncHandler"></param>
        /// <param name="param"></param>
        /// <param name="info"></param>
        /// <param name="updateClientKey"></param>
        /// <param name="updateServerKey"></param>
        public SSH1KeyExchanger(
                    SSH1Connection connection,
                    SSH1SynchronousPacketHandler syncHandler,
                    SSHConnectionParameter param,
                    SSH1ConnectionInfo info,
                    UpdateClientKeyDelegate updateClientKey,
                    UpdateServerKeyDelegate updateServerKey) {
            _connection = connection;
            _syncHandler = syncHandler;
            _param = param;
            _cInfo = info;
            _updateClientKey = updateClientKey;
            _updateServerKey = updateServerKey;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            if (_sequenceStatus == SequenceStatus.Succeeded || _sequenceStatus == SequenceStatus.Failed) {
                return SSHPacketInterceptorResult.Finished;
            }

            SSH1PacketType packetType = (SSH1PacketType)packet[0];
            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.WaitPublicKey:
                        if (packetType == SSH1PacketType.SSH_SMSG_PUBLIC_KEY) {
                            _sequenceStatus = SequenceStatus.PublicKeyReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.WaitSessionKeyResult:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _sequenceStatus = SequenceStatus.Succeeded;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        else if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.Failed;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
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
        /// Start key exchange asynchronously.
        /// </summary>
        /// <returns>a new task if the key exchange has been started, or existing task if another key exchange is running.</returns>
        public Task StartKeyExchange() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.Idle) {
                    return _kexTask;
                }
                _sequenceStatus = SequenceStatus.WaitPublicKey;
                _kexTask = Task.Run(() => DoKeyExchange());
                return _kexTask;
            }
        }

        /// <summary>
        /// Key exchange sequence.
        /// </summary>
        /// <exception cref="SSHException">no response</exception>
        private void DoKeyExchange() {
            try {
                ReceiveServerKey();

                if (_param.VerifySSHHostKey != null) {
                    bool accepted = _param.VerifySSHHostKey(_cInfo.GetSSHHostKeyInformationProvider());
                    if (!accepted) {
                        throw new SSHException(Strings.GetString("HostKeyDenied"));
                    }
                }

                byte[] sessionId = ComputeSessionId();
                byte[] sessionKey = new byte[32];
                RngManager.GetSecureRng().GetBytes(sessionKey);

                SendSessionKey(sessionId, sessionKey);
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
        /// Waits SSH_SMSG_PUBLIC_KEY from server, then parse it.
        /// </summary>
        private void ReceiveServerKey() {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.WaitPublicKey || _sequenceStatus == SequenceStatus.PublicKeyReceived);
            }

            DataFragment packet = null;
            if (!_receivedPacket.TryGet(ref packet, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.PublicKeyReceived);
            }

            SSH1DataReader reader = new SSH1DataReader(packet);
            SSH1PacketType packetType = (SSH1PacketType)reader.ReadByte();
            Debug.Assert(packetType == SSH1PacketType.SSH_SMSG_PUBLIC_KEY);
            _cInfo.AntiSpoofingCookie = reader.Read(8);
            _cInfo.ServerKeyBits = reader.ReadInt32();
            BigInteger serverKeyExponent = reader.ReadMPInt();
            BigInteger serverKeyModulus = reader.ReadMPInt();
            _cInfo.ServerKey = new RSAPublicKey(serverKeyExponent, serverKeyModulus);
            _cInfo.HostKeyBits = reader.ReadInt32();
            BigInteger hostKeyExponent = reader.ReadMPInt();
            BigInteger hostKeyModulus = reader.ReadMPInt();
            _cInfo.HostKey = new RSAPublicKey(hostKeyExponent, hostKeyModulus);
            int protocolFlags = reader.ReadInt32();
            int supportedCiphersMask = reader.ReadInt32();
            _cInfo.SupportedEncryptionAlgorithmsMask = supportedCiphersMask;
            int supportedAuthenticationsMask = reader.ReadInt32();

            bool foundCipher = false;
            foreach (CipherAlgorithm algorithm in _param.PreferableCipherAlgorithms) {
                if ((algorithm == CipherAlgorithm.Blowfish || algorithm == CipherAlgorithm.TripleDES)
                    && ((supportedCiphersMask & (1 << (int)algorithm)) != 0)) {

                    _cInfo.IncomingPacketCipher = _cInfo.OutgoingPacketCipher = algorithm;
                    foundCipher = true;
                    break;
                }
            }
            if (!foundCipher) {
                throw new SSHException(String.Format(Strings.GetString("ServerNotSupportedX"), "Blowfish/TripleDES"));
            }

            switch (_param.AuthenticationType) {
                case AuthenticationType.Password:
                    if ((supportedAuthenticationsMask & (1 << (int)AuthenticationType.Password)) == 0) {
                        throw new SSHException(String.Format(Strings.GetString("ServerNotSupportedPassword")));
                    }
                    break;
                case AuthenticationType.PublicKey:
                    if ((supportedAuthenticationsMask & (1 << (int)AuthenticationType.PublicKey)) == 0) {
                        throw new SSHException(String.Format(Strings.GetString("ServerNotSupportedRSA")));
                    }
                    break;
                default:
                    throw new SSHException(Strings.GetString("InvalidAuthenticationType"));
            }
        }

        /// <summary>
        /// Computes session id.
        /// </summary>
        /// <returns>session id</returns>
        private byte[] ComputeSessionId() {
            byte[] hostKeyMod = _cInfo.HostKey.Modulus.GetBytes();
            byte[] serverKeyMod = _cInfo.ServerKey.Modulus.GetBytes();

            using (var md5 = new MD5CryptoServiceProvider()) {
                md5.TransformBlock(hostKeyMod, 0, hostKeyMod.Length, hostKeyMod, 0);
                md5.TransformBlock(serverKeyMod, 0, serverKeyMod.Length, serverKeyMod, 0);
                md5.TransformFinalBlock(_cInfo.AntiSpoofingCookie, 0, _cInfo.AntiSpoofingCookie.Length);
                return md5.Hash;
            }
        }

        /// <summary>
        /// Builds SSH_CMSG_SESSION_KEY packet.
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="sessionKey"></param>
        /// <returns>a packet object</returns>
        private SSH1Packet BuildSessionKeyPacket(byte[] sessionId, byte[] sessionKey) {
            byte[] sessionKeyXor = (byte[])sessionKey.Clone();
            // xor first 16 bytes
            for (int i = 0; i < sessionId.Length; i++) {
                sessionKeyXor[i] ^= sessionId[i];
            }

            RSAPublicKey firstEncryption;
            RSAPublicKey secondEncryption;
            int firstKeyByteLen;
            int secondKeyByteLen;
            RSAPublicKey serverKey = _cInfo.ServerKey;
            RSAPublicKey hostKey = _cInfo.HostKey;
            if (serverKey.Modulus < hostKey.Modulus) {
                firstEncryption = serverKey;
                secondEncryption = hostKey;
                firstKeyByteLen = (_cInfo.ServerKeyBits + 7) / 8;
                secondKeyByteLen = (_cInfo.HostKeyBits + 7) / 8;
            }
            else {
                firstEncryption = hostKey;
                secondEncryption = serverKey;
                firstKeyByteLen = (_cInfo.HostKeyBits + 7) / 8;
                secondKeyByteLen = (_cInfo.ServerKeyBits + 7) / 8;
            }

            Rng rng = RngManager.GetSecureRng();
            BigInteger firstResult = RSAUtil.PKCS1PadType2(sessionKeyXor, firstKeyByteLen, rng).ModPow(firstEncryption.Exponent, firstEncryption.Modulus);
            BigInteger secondResult = RSAUtil.PKCS1PadType2(firstResult.GetBytes(), secondKeyByteLen, rng).ModPow(secondEncryption.Exponent, secondEncryption.Modulus);

            return new SSH1Packet(SSH1PacketType.SSH_CMSG_SESSION_KEY)
                    .WriteByte((byte)_cInfo.OutgoingPacketCipher.Value)
                    .Write(_cInfo.AntiSpoofingCookie)
                    .WriteBigInteger(secondResult)
                    .WriteInt32(0); //protocol flags
        }

        /// <summary>
        /// Sends SSH_CMSG_SESSION_KEY packet and waits SSH_SMSG_SUCCESS.
        /// </summary>
        /// <param name="sessionId">session id</param>
        /// <param name="sessionKey">session key</param>
        public void SendSessionKey(byte[] sessionId, byte[] sessionKey) {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.PublicKeyReceived);
            }

            Cipher cipherServer = CipherFactory.CreateCipher(SSHProtocol.SSH1, _cInfo.OutgoingPacketCipher.Value, sessionKey);
            Cipher cipherClient = CipherFactory.CreateCipher(SSHProtocol.SSH1, _cInfo.IncomingPacketCipher.Value, sessionKey);

            _updateClientKey(cipherClient); // prepare decryption of the response

            lock (_sequenceLock) {
                CheckConnectionClosed();
                _sequenceStatus = SequenceStatus.WaitSessionKeyResult;
            }

            var packet = BuildSessionKeyPacket(sessionId, sessionKey);
            _syncHandler.Send(packet);

            _updateServerKey(sessionId, cipherServer);  // prepare encryption for the trailing packets

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                if (_sequenceStatus != SequenceStatus.Succeeded) {
                    throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
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

        #endregion
    }

    /// <summary>
    /// Class for supporting user authentication
    /// </summary>
    internal class SSH1UserAuthentication : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

        private readonly SSHConnectionParameter _param;
        private readonly SSH1Connection _connection;
        private readonly SSH1SynchronousPacketHandler _syncHandler;
        private readonly byte[] _sessionID;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private Task _authTask;

        private enum SequenceStatus {
            /// <summary>authentication can be started</summary>
            Idle,
            /// <summary>authentication has been finished.</summary>
            Done,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>authentication has been started</summary>
            StartAuthentication,
            /// <summary>SSH_CMSG_USER has been sent. waiting for SSH_SMSG_SUCCESS|SSH_SMSG_FAILURE.</summary>
            User_WaitResult,
            /// <summary>SSH_SMSG_SUCCESS has been received</summary>
            User_SuccessReceived,
            /// <summary>SSH_SMSG_FAILURE has been received</summary>
            User_FailureReceived,

            //--- RSA challenge-response

            /// <summary>SSH_CMSG_AUTH_RSA has been sent. waiting for SSH_SMSG_AUTH_RSA_CHALLENGE|SSH_SMSG_FAILURE</summary>
            RSA_WaitChallenge,
            /// <summary>SSH_SMSG_AUTH_RSA_CHALLENGE has been received</summary>
            RSA_ChallengeReceived,
            /// <summary>SSH_CMSG_AUTH_RSA_RESPONSE has been sent. waiting for SSH_SMSG_SUCCESS|SSH_SMSG_FAILURE</summary>
            RSA_WaitResponseResult,
            /// <summary>SSH_SMSG_SUCCESS has been received</summary>
            RSA_SuccessReceived,
            /// <summary>SSH_SMSG_FAILURE has been received</summary>
            RSA_FailureReceived,

            //--- password authentication

            /// <summary>SSH_CMSG_AUTH_PASSWORD has been sent. waiting for SSH_SMSG_SUCCESS|SSH_SMSG_FAILURE</summary>
            PA_WaitResult,
            /// <summary>SSH_MSG_USERAUTH_SUCCESS has been received</summary>
            PA_SuccessReceived,
            /// <summary>SSH_MSG_USERAUTH_FAILURE has been received</summary>
            PA_FailureReceived,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="param"></param>
        /// <param name="syncHandler"></param>
        /// <param name="sessionID"></param>
        public SSH1UserAuthentication(
                    SSH1Connection connection,
                    SSHConnectionParameter param,
                    SSH1SynchronousPacketHandler syncHandler,
                    byte[] sessionID) {
            _connection = connection;
            _param = param;
            _syncHandler = syncHandler;
            _sessionID = sessionID;
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

            SSH1PacketType packetType = (SSH1PacketType)packet[0];
            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.User_WaitResult:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _sequenceStatus = SequenceStatus.User_SuccessReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.User_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;

                    // RSA challenge-response

                    case SequenceStatus.RSA_WaitChallenge:
                        if (packetType == SSH1PacketType.SSH_SMSG_AUTH_RSA_CHALLENGE) {
                            _sequenceStatus = SequenceStatus.RSA_ChallengeReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.RSA_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.RSA_WaitResponseResult:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _sequenceStatus = SequenceStatus.RSA_SuccessReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.RSA_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;

                    // Password authentication

                    case SequenceStatus.PA_WaitResult:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _sequenceStatus = SequenceStatus.PA_SuccessReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.PA_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
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
        /// Start authentication asynchronously.
        /// </summary>
        /// <returns>a new task if the authentication has been started, or existing task if another authentication is running.</returns>
        public Task StartAuthentication() {
            lock (_sequenceLock) {
                if (_sequenceStatus != SequenceStatus.Idle) {
                    return _authTask;
                }
                _sequenceStatus = SequenceStatus.StartAuthentication;
                _authTask = Task.Run(() => DoAuthentication());
                return _authTask;
            }
        }

        /// <summary>
        /// Authentication sequence.
        /// </summary>
        /// <returns>true if the sequence was succeeded</returns>
        /// <exception cref="SSHException">no response</exception>
        private void DoAuthentication() {
            try {
                bool success = DeclareUser();

                if (success) {
                    return;
                }

                switch (_param.AuthenticationType) {
                    case AuthenticationType.Password:
                        PasswordAuthentication();
                        break;
                    case AuthenticationType.PublicKey:
                        PublickeyAuthentication();
                        break;
                    default:
                        throw new SSHException(Strings.GetString("InvalidAuthenticationType"));
                }
            }
            finally {
                _receivedPacket.Clear();
                lock (_sequenceLock) {
                    _sequenceStatus = SequenceStatus.Done;
                }
            }
        }

        /// <summary>
        /// Builds SSH_CMSG_USER packet.
        /// </summary>
        /// <returns>a packet object</returns>
        private SSH1Packet BuildUserPacket() {
            return new SSH1Packet(SSH1PacketType.SSH_CMSG_USER)
                    .WriteString(_param.UserName);
        }

        /// <summary>
        /// Declaring-User sequence.
        /// </summary>
        /// <returns>result of declaring user.</returns>
        private bool DeclareUser() {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.StartAuthentication);
                _sequenceStatus = SequenceStatus.User_WaitResult;
            }

            var packet = BuildUserPacket();
            _syncHandler.Send(packet);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();

                if (_sequenceStatus == SequenceStatus.User_SuccessReceived) {
                    return true;
                }
                else {
                    return false;
                }
            }
        }

        /// <summary>
        /// Builds SSH_CMSG_AUTH_PASSWORD packet.
        /// </summary>
        /// <returns>a packet object</returns>
        private SSH1Packet BuildAuthPasswordPacket() {
            return new SSH1Packet(SSH1PacketType.SSH_CMSG_AUTH_PASSWORD)
                        .WriteString(_param.Password);
        }

        /// <summary>
        /// Password authentication sequence.
        /// </summary>
        private void PasswordAuthentication() {
            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.User_FailureReceived);
                _sequenceStatus = SequenceStatus.PA_WaitResult;
            }

            var packet = BuildAuthPasswordPacket();
            _syncHandler.Send(packet);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                if (_sequenceStatus != SequenceStatus.PA_SuccessReceived) {
                    throw new SSHException(Strings.GetString("AuthenticationFailed"));
                }
            }
        }

        /// <summary>
        /// Builds SSH_CMSG_AUTH_RSA packet.
        /// </summary>
        /// <param name="key">private key data</param>
        /// <returns>a packet object</returns>
        private SSH1Packet BuildAuthRSAPacket(SSH1UserAuthKey key) {
            return new SSH1Packet(SSH1PacketType.SSH_CMSG_AUTH_RSA)
                    .WriteBigInteger(key.PublicModulus);
        }

        /// <summary>
        /// Builds SSH_CMSG_AUTH_RSA_RESPONSE packet.
        /// </summary>
        /// <param name="hash">hash data</param>
        /// <returns>a packet object</returns>
        private SSH1Packet BuildRSAResponsePacket(byte[] hash) {
            return new SSH1Packet(SSH1PacketType.SSH_CMSG_AUTH_RSA_RESPONSE)
                    .Write(hash);
        }

        /// <summary>
        /// RSA Publickey authentication sequence.
        /// </summary>
        private void PublickeyAuthentication() {
            SSH1UserAuthKey key = new SSH1UserAuthKey(_param.IdentityFile, _param.Password);

            lock (_sequenceLock) {
                CheckConnectionClosed();
                Debug.Assert(_sequenceStatus == SequenceStatus.User_FailureReceived);
                _sequenceStatus = SequenceStatus.RSA_WaitChallenge;
            }

            var packetRsa = BuildAuthRSAPacket(key);
            _syncHandler.Send(packetRsa);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                if (_sequenceStatus == SequenceStatus.RSA_FailureReceived) {
                    throw new SSHException(Strings.GetString("AuthenticationFailed"));
                }
                Debug.Assert(_sequenceStatus == SequenceStatus.RSA_ChallengeReceived);
            }

            SSH1DataReader challengeReader = new SSH1DataReader(response);
            challengeReader.ReadByte(); // skip message number
            BigInteger encryptedChallenge = challengeReader.ReadMPInt();
            BigInteger challenge = key.decryptChallenge(encryptedChallenge);
            byte[] rawchallenge = RSAUtil.StripPKCS1Pad(challenge, 2).GetBytes();

            byte[] hash;
            using (var md5 = new MD5CryptoServiceProvider()) {
                md5.TransformBlock(rawchallenge, 0, rawchallenge.Length, rawchallenge, 0);
                md5.TransformFinalBlock(_sessionID, 0, _sessionID.Length);
                hash = md5.Hash;
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                _sequenceStatus = SequenceStatus.RSA_WaitResponseResult;
            }

            var packetRes = BuildRSAResponsePacket(hash);
            _syncHandler.Send(packetRes);

            response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                CheckConnectionClosed();
                if (_sequenceStatus != SequenceStatus.RSA_SuccessReceived) {
                    throw new SSHException(Strings.GetString("AuthenticationFailed"));
                }
                Debug.Assert(_sequenceStatus == SequenceStatus.RSA_SuccessReceived);
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

        #endregion
    }

    /// <summary>
    /// Class for supporting remote port-forwarding
    /// </summary>
    internal class SSH1RemotePortForwarding : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

        public delegate SSH1RemotePortForwardingChannel CreateChannelFunc(RemotePortForwardingRequest requestInfo, uint remoteChannel);
        public delegate void RegisterChannelFunc(SSH1RemotePortForwardingChannel channel, ISSHChannelEventHandler eventHandler);

        private readonly SSH1SynchronousPacketHandler _syncHandler;

        private readonly Action _startInteractiveSession;

        private class PortDictKey {
            private readonly string _host;
            private readonly uint _port;

            public PortDictKey(string host, uint port) {
                _host = host;
                _port = port;
            }

            public override bool Equals(object obj) {
                var other = obj as PortDictKey;
                if (other == null) {
                    return false;
                }
                return this._host == other._host && this._port == other._port;
            }

            public override int GetHashCode() {
                return _host.GetHashCode() + _port.GetHashCode();
            }
        }

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
        private readonly ConcurrentDictionary<PortDictKey, PortInfo> _portDict = new ConcurrentDictionary<PortDictKey, PortInfo>();

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private enum SequenceStatus {
            /// <summary>Idle</summary>
            Idle,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>SSH_CMSG_PORT_FORWARD_REQUEST has been sent. waiting for SSH_SMSG_SUCCESS | SSH_SMSG_FAILURE.</summary>
            WaitPortForwardResponse,
            /// <summary>SSH_SMSG_SUCCESS has been received.</summary>
            PortForwardSuccess,
            /// <summary>SSH_SMSG_FAILURE has been received.</summary>
            PortForwardFailure,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH1RemotePortForwarding(SSH1SynchronousPacketHandler syncHandler, Action startInteractiveSession) {
            _syncHandler = syncHandler;
            _startInteractiveSession = startInteractiveSession;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH1PacketType packetType = (SSH1PacketType)packet[0];
            SSHPacketInterceptorResult result = CheckPortOpenRequestPacket(packetType, packet);
            if (result != SSHPacketInterceptorResult.PassThrough) {
                return result;
            }

            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.WaitPortForwardResponse:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _sequenceStatus = SequenceStatus.PortForwardSuccess;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.PortForwardFailure;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                }

                return SSHPacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles new request.
        /// </summary>
        /// <param name="packetType">packet type</param>
        /// <param name="packet">packet data</param>
        /// <returns>result</returns>
        private SSHPacketInterceptorResult CheckPortOpenRequestPacket(SSH1PacketType packetType, DataFragment packet) {
            if (packetType != SSH1PacketType.SSH_MSG_PORT_OPEN) {
                return SSHPacketInterceptorResult.PassThrough;
            }

            SSH1DataReader reader = new SSH1DataReader(packet);
            reader.ReadByte();    // skip message number
            uint remoteChannel = reader.ReadUInt32();
            string host = reader.ReadString();
            uint port = reader.ReadUInt32();

            // reject the request if we don't know the host / port pair.
            PortDictKey key = new PortDictKey(host, port);
            PortInfo portInfo;
            if (!_portDict.TryGetValue(key, out portInfo)) {
                RejectPortForward(remoteChannel);
                return SSHPacketInterceptorResult.Consumed;
            }

            RemotePortForwardingRequest requestInfo = new RemotePortForwardingRequest("", 0, "", 0);

            // create a temporary channel
            var channel = portInfo.CreateChannel(requestInfo, remoteChannel);

            // check the request by the request handler
            RemotePortForwardingReply reply;
            try {
                reply = portInfo.RequestHandler.OnRemotePortForwardingRequest(requestInfo, channel);
            }
            catch (Exception) {
                RejectPortForward(remoteChannel);
                return SSHPacketInterceptorResult.Consumed;
            }

            if (!reply.Accepted) {
                RejectPortForward(remoteChannel);
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
        private void RejectPortForward(uint remoteChannel) {
            var packet = new SSH1Packet(SSH1PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE);
            _syncHandler.Send(packet);
        }

        /// <summary>
        /// Builds SSH_CMSG_PORT_FORWARD_REQUEST packet.
        /// </summary>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <param name="hostToConnect">host the connection should be be forwarded to</param>
        /// <param name="portNumberToConnect">port the connection should be be forwarded to</param>
        /// <returns></returns>
        private SSH1Packet BuildPortForwardPacket(uint portNumberToBind, string hostToConnect, uint portNumberToConnect) {
            return new SSH1Packet(SSH1PacketType.SSH_CMSG_PORT_FORWARD_REQUEST)
                    .WriteUInt32(portNumberToBind)
                    .WriteString(hostToConnect)
                    .WriteUInt32(portNumberToConnect);
        }

        /// <summary>
        /// Starts remote port forwarding.
        /// </summary>
        /// <param name="requestHandler">request handler</param>
        /// <param name="createChannel">a function for creating a new channel object</param>
        /// <param name="registerChannel">a function for registering a new channel object</param>
        /// <param name="portNumberToBind">port number to bind on the server</param>
        /// <param name="hostToConnect">host the connection should be be forwarded to</param>
        /// <param name="portNumberToConnect">port the connection should be be forwarded to</param>
        /// <returns>true if the remote port forwarding has been started.</returns>
        public bool ListenForwardedPort(
                IRemotePortForwardingHandler requestHandler,
                CreateChannelFunc createChannel,
                RegisterChannelFunc registerChannel,
                uint portNumberToBind,
                string hostToConnect,
                uint portNumberToConnect) {

            IRemotePortForwardingHandler requestHandlerWrapper =
                new RemotePortForwardingHandlerIgnoreErrorWrapper(requestHandler);

            bool success = ListenForwardedPortCore(
                                requestHandlerWrapper,
                                createChannel,
                                registerChannel,
                                portNumberToBind,
                                hostToConnect,
                                portNumberToConnect);

            if (success) {
                _startInteractiveSession();
                requestHandlerWrapper.OnRemotePortForwardingStarted(portNumberToBind);
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
                uint portNumberToBind,
                string hostToConnect,
                uint portNumberToConnect) {

            lock (_sequenceLock) {
                while (_sequenceStatus != SequenceStatus.Idle) {
                    if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                        return false;
                    }
                    Monitor.Wait(_sequenceLock);
                }

                _receivedPacket.Clear();
                _sequenceStatus = SequenceStatus.WaitPortForwardResponse;
            }

            var packet = BuildPortForwardPacket(portNumberToBind, hostToConnect, portNumberToConnect);
            _syncHandler.Send(packet);

            DataFragment response = null;
            bool accepted = false;
            if (_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                lock (_sequenceLock) {
                    if (_sequenceStatus == SequenceStatus.PortForwardSuccess) {
                        accepted = true;
                        PortDictKey key = new PortDictKey(hostToConnect, portNumberToConnect);
                        _portDict.TryAdd(
                            key,
                            new PortInfo(requestHandler, createChannel, registerChannel));
                    }
                }
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
    /// Class for supporting authentication agent forwarding
    /// </summary>
    internal class SSH1AgentForwarding : ISSHPacketInterceptor {
        #region

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

        public delegate SSH1AgentForwardingChannel CreateChannelFunc(uint remoteChannel);
        public delegate void RegisterChannelFunc(SSH1AgentForwardingChannel channel, ISSHChannelEventHandler eventHandler);

        private readonly SSH1SynchronousPacketHandler _syncHandler;
        private readonly CreateChannelFunc _createChannel;
        private readonly RegisterChannelFunc _registerChannel;
        private readonly IAgentForwardingAuthKeyProvider _authKeyProvider;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private enum SequenceStatus {
            /// <summary>Idle</summary>
            Idle,
            /// <summary>the connection has been closed</summary>
            ConnectionClosed,
            /// <summary>SSH_CMSG_AGENT_REQUEST_FORWARDING has been sent. waiting for SSH_SMSG_SUCCESS | SSH_SMSG_FAILURE.</summary>
            WaitAgentForwardingResponse,
            /// <summary>SSH_SMSG_SUCCESS has been received.</summary>
            AgentForwardingSuccess,
            /// <summary>SSH_SMSG_FAILURE has been received.</summary>
            AgentForwardingFailure,
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSH1AgentForwarding(
                SSH1SynchronousPacketHandler syncHandler,
                IAgentForwardingAuthKeyProvider authKeyProvider,
                CreateChannelFunc createChannel,
                RegisterChannelFunc registerChannel) {
            _syncHandler = syncHandler;
            _createChannel = createChannel;
            _registerChannel = registerChannel;
            _authKeyProvider = authKeyProvider;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSHPacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH1PacketType packetType = (SSH1PacketType)packet[0];
            SSHPacketInterceptorResult result = CheckAgentOpenRequestPacket(packetType, packet);
            if (result != SSHPacketInterceptorResult.PassThrough) {
                return result;
            }

            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.WaitAgentForwardingResponse:
                        if (packetType == SSH1PacketType.SSH_SMSG_SUCCESS) {
                            _sequenceStatus = SequenceStatus.AgentForwardingSuccess;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH1PacketType.SSH_SMSG_FAILURE) {
                            _sequenceStatus = SequenceStatus.AgentForwardingFailure;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSHPacketInterceptorResult.Consumed;
                        }
                        break;
                }

                return SSHPacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles new request.
        /// </summary>
        /// <param name="packetType">packet type</param>
        /// <param name="packet">packet data</param>
        /// <returns>result</returns>
        private SSHPacketInterceptorResult CheckAgentOpenRequestPacket(SSH1PacketType packetType, DataFragment packet) {
            if (packetType != SSH1PacketType.SSH_SMSG_AGENT_OPEN) {
                return SSHPacketInterceptorResult.PassThrough;
            }

            SSH1DataReader reader = new SSH1DataReader(packet);
            reader.ReadByte();    // skip message number
            uint remoteChannel = reader.ReadUInt32();

            // create a temporary channel
            var channel = _createChannel(remoteChannel);

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
        /// Builds SSH_CMSG_AGENT_REQUEST_FORWARDING packet.
        /// </summary>
        /// <returns></returns>
        private SSH1Packet BuildAgentRequestForwardingPacket() {
            return new SSH1Packet(SSH1PacketType.SSH_CMSG_AGENT_REQUEST_FORWARDING);
        }

        /// <summary>
        /// Starts agent forwarding.
        /// </summary>
        /// <returns>true if the agent forwarding has been started.</returns>
        public bool StartAgentForwarding() {
            lock (_sequenceLock) {
                while (_sequenceStatus != SequenceStatus.Idle) {
                    if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                        return false;
                    }
                    Monitor.Wait(_sequenceLock);
                }

                _receivedPacket.Clear();
                _sequenceStatus = SequenceStatus.WaitAgentForwardingResponse;
            }

            var packet = BuildAgentRequestForwardingPacket();
            _syncHandler.Send(packet);

            DataFragment response = null;
            bool accepted = false;
            if (_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                lock (_sequenceLock) {
                    if (_sequenceStatus == SequenceStatus.AgentForwardingSuccess) {
                        accepted = true;
                    }
                }
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
}
