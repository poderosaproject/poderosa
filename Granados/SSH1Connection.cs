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
using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

using Granados.PKI;
using Granados.Util;
using Granados.Crypto;
using Granados.IO;
using Granados.IO.SSH1;
using Granados.Mono.Math;
using Granados.SSH;

namespace Granados.SSH1 {

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public sealed class SSH1Connection : SSHConnection {

        private const int AUTH_NOT_REQUIRED = 0;
        private const int AUTH_REQUIRED = 1;

        private readonly SSH1Packetizer _packetizer;
        private readonly SSH1SynchronousPacketHandler _syncHandler;
        private readonly SSHPacketInterceptorCollection _packetInterceptors;
        private readonly SSH1KeyExchanger _keyExchanger;

        private readonly SSH1ConnectionInfo _cInfo;
        private bool _executingShell;
        private int _shellID;

        // exec command for SCP
        //private bool _executingExecCmd = false;

        public SSH1Connection(SSHConnectionParameter param, IGranadosSocket socket, ISSHConnectionEventReceiver er, string serverVersion, string clientVersion)
            : base(param, socket, er) {
            _cInfo = new SSH1ConnectionInfo(param.HostName, param.PortNumber, serverVersion, clientVersion);
            _shellID = -1;

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
            _syncHandler = new SSH1SynchronousPacketHandler(socket, adapter);
            _packetizer = new SSH1Packetizer(_syncHandler);

            _packetInterceptors = new SSHPacketInterceptorCollection();
            _keyExchanger = new SSH1KeyExchanger(this, _syncHandler, _param, _cInfo, UpdateClientKey, UpdateServerKey);
            _packetInterceptors.Add(_keyExchanger);
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

        // sending exec command for SCP
        // TODO: まだ実装中です
        public override SSHChannel DoExecCommand(ISSHChannelEventReceiver receiver, string command) {
            //_executingExecCmd = true;
            SendExecCommand();
            return null;
        }

        private void SendExecCommand() {
            Debug.WriteLine("EXEC COMMAND");
            string cmd = _execCmd;
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_EXEC_CMD)
                    .WriteString(cmd)
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_EXEC_CMD, "exec command: cmd={0}", cmd);
        }

        public override SSHChannel OpenShell(ISSHChannelEventReceiver receiver) {
            if (_shellID != -1)
                throw new SSHException("A shell is opened already");
            _shellID = _channel_collection.RegisterChannelEventReceiver(null, receiver).LocalID;
            SendRequestPTY();
            _executingShell = true;
            return new SSH1Channel(this, ChannelType.Shell, _shellID);
        }

        private void SendRequestPTY() {
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_REQUEST_PTY)
                    .WriteString(_param.TerminalName)
                    .WriteInt32(_param.TerminalHeight)
                    .WriteInt32(_param.TerminalWidth)
                    .WriteInt32(_param.TerminalPixelWidth)
                    .WriteInt32(_param.TerminalPixelHeight)
                    .Write(new byte[1]) //TTY_OP_END
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_REQUEST_PTY, "open shell: terminal={0} width={1} height={2}", _param.TerminalName, _param.TerminalWidth, _param.TerminalHeight);
        }

        private void ExecShell() {
            //System.out.println("EXEC SHELL");
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_EXEC_SHELL)
            );
        }

        public override SSHChannel ForwardPort(ISSHChannelEventReceiver receiver, string remote_host, int remote_port, string originator_host, int originator_port) {
            if (_shellID == -1) {
                ExecShell();
                _shellID = _channel_collection.RegisterChannelEventReceiver(null, new SSH1DummyReceiver()).LocalID;
            }

            int local_id = _channel_collection.RegisterChannelEventReceiver(null, receiver).LocalID;

            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_MSG_PORT_OPEN)
                    .WriteInt32(local_id) //channel id is fixed to 0
                    .WriteString(remote_host)
                    .WriteInt32(remote_port)
                //originator is specified only if SSH_PROTOFLAG_HOST_IN_FWD_OPEN is specified
                //writer.Write(originator_host);
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_MSG_PORT_OPEN, "open forwarded port: host={0} port={1}", remote_host, remote_port);

            return new SSH1Channel(this, ChannelType.ForwardedLocalToRemote, local_id);
        }

        public override void ListenForwardedPort(string allowed_host, int bind_port) {
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_PORT_FORWARD_REQUEST)
                    .WriteInt32(bind_port)
                    .WriteString(allowed_host)
                    .WriteInt32(0)
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_PORT_FORWARD_REQUEST, "start to listening to remote port: host={0} port={1}", allowed_host, bind_port);

            if (_shellID == -1) {
                ExecShell();
                _shellID = _channel_collection.RegisterChannelEventReceiver(null, new SSH1DummyReceiver()).LocalID;
            }

        }
        public override void CancelForwardedPort(string host, int port) {
            throw new NotSupportedException("not implemented");
        }

        private void ProcessPortforwardingRequest(ISSHConnectionEventReceiver receiver, SSH1DataReader reader) {
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

            SSH1DataReader re = new SSH1DataReader(packet);
            SSH1PacketType pt = (SSH1PacketType)re.ReadByte();
            switch (pt) {
                case SSH1PacketType.SSH_SMSG_STDOUT_DATA: {
                        int len = re.ReadInt32();
                        DataFragment frag = re.GetRemainingDataView(len);
                        _channel_collection.FindChannelEntry(_shellID).Receiver.OnData(frag);
                    }
                    break;
                case SSH1PacketType.SSH_SMSG_STDERR_DATA: {
                        int len = re.ReadInt32();
                        DataFragment frag = re.GetRemainingDataView(len);
                        _channel_collection.FindChannelEntry(_shellID).Receiver.OnExtendedData((uint)pt, frag);
                    }
                    break;
                case SSH1PacketType.SSH_MSG_CHANNEL_DATA: {
                        int channel = re.ReadInt32();
                        int len = re.ReadInt32();
                        DataFragment frag = re.GetRemainingDataView(len);
                        _channel_collection.FindChannelEntry(channel).Receiver.OnData(frag);
                    }
                    break;
                case SSH1PacketType.SSH_MSG_PORT_OPEN:
                    ProcessPortforwardingRequest(_eventReceiver, re);
                    break;
                case SSH1PacketType.SSH_MSG_CHANNEL_CLOSE: {
                        int channel = re.ReadInt32();
                        ISSHChannelEventReceiver r = _channel_collection.FindChannelEntry(channel).Receiver;
                        _channel_collection.UnregisterChannelEventReceiver(channel);
                        r.OnChannelClosed();
                    }
                    break;
                case SSH1PacketType.SSH_MSG_CHANNEL_CLOSE_CONFIRMATION: {
                        int channel = re.ReadInt32();
                    }
                    break;
                case SSH1PacketType.SSH_MSG_DISCONNECT:
                    _eventReceiver.OnConnectionClosed();
                    break;
                case SSH1PacketType.SSH_SMSG_EXITSTATUS:
                    _channel_collection.FindChannelEntry(_shellID).Receiver.OnChannelClosed();
                    break;
                case SSH1PacketType.SSH_MSG_DEBUG:
                    _eventReceiver.OnDebugMessage(false, re.ReadString());
                    break;
                case SSH1PacketType.SSH_MSG_IGNORE:
                    _eventReceiver.OnIgnoreMessage(re.ReadByteString());
                    break;
                case SSH1PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION: {
                        int local = re.ReadInt32();
                        int remote = re.ReadInt32();
                        _channel_collection.FindChannelEntry(local).Receiver.OnChannelReady();
                    }
                    break;
                case SSH1PacketType.SSH_SMSG_SUCCESS:
                    if (_executingShell) {
                        ExecShell();
                        _channel_collection.FindChannelEntry(_shellID).Receiver.OnChannelReady();
                        _executingShell = false;
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

        //alternative version
        internal void TraceTransmissionEvent(SSH1PacketType pt, string message, params object[] args) {
            ISSHEventTracer t = _param.EventTracer;
            if (t != null)
                t.OnTranmission(pt.ToString(), String.Format(message, args));
        }
        internal void TraceReceptionEvent(SSH1PacketType pt, string message, params object[] args) {
            ISSHEventTracer t = _param.EventTracer;
            if (t != null)
                t.OnReception(pt.ToString(), String.Format(message, args));
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

    //if port forwardings are performed without a shell, we use SSH1DummyChannel to receive shell data
    internal class SSH1DummyReceiver : ISSHChannelEventReceiver {
        public void OnData(DataFragment data) {
        }
        public void OnExtendedData(uint type, DataFragment data) {
        }
        public void OnChannelClosed() {
        }
        public void OnChannelEOF() {
        }
        public void OnChannelReady() {
        }
        public void OnChannelError(Exception error) {
        }
        public void OnMiscPacket(byte packetType, DataFragment data) {
        }
    }

    /// <summary>
    /// Synchronization of sending/receiving packets.
    /// </summary>
    internal class SSH1SynchronousPacketHandler : AbstractSynchronousPacketHandler<SSH1Packet> {
        #region SSH1SynchronousPacketHandler

        private readonly object _cipherSync = new object();
        private Cipher _cipher = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket object for sending packets.</param>
        /// <param name="handler">the next handler received packets are redirected to.</param>
        public SSH1SynchronousPacketHandler(IGranadosSocket socket, IDataHandler handler)
            : base(socket, handler) {
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
        #region SSH1KeyExchanger

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
        #region SSH1UserAuthentication

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

}
