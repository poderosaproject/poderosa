/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: SSH2Connection.cs,v 1.11 2012/02/25 03:49:46 kzmi Exp $
*/
using System;
using System.Threading;
using System.Diagnostics;
using System.Text;
using System.Security.Cryptography;

using Granados.PKI;
using Granados.Crypto;
using Granados.Util;
using Granados.IO;
using Granados.IO.SSH2;
using Granados.Mono.Math;

namespace Granados.SSH2 {

    /// <summary>
    /// SSH2
    /// </summary>
    public class SSH2Connection : SSHConnection {

        // packet sequence number
        private uint _sequence;
        private readonly object _transmitSync = new object();   // for keeping correct packet order

        private MAC _mac;
        private Cipher _cipher;
        private readonly SSH2Packetizer _packetizer;
        private readonly SynchronizedPacketReceiver _packetReceiver;

        //server info
        private readonly SSH2ConnectionInfo _cInfo;

        private bool _waitingForPortForwardingResponse;
        private bool _agentForwardConfirmed;

        private KeyExchanger _asyncKeyExchanger;
        private int _requiredResponseCount; //for keyboard-interactive authentication


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="param"></param>
        /// <param name="strm"></param>
        /// <param name="r"></param>
        /// <param name="serverVersion"></param>
        /// <param name="clientVersion"></param>
        public SSH2Connection(SSHConnectionParameter param, IGranadosSocket strm, ISSHConnectionEventReceiver r, string serverVersion, string clientVersion)
            : base(param, strm, r) {
            _cInfo = new SSH2ConnectionInfo(param.HostName, param.PortNumber, serverVersion, clientVersion);
            _packetReceiver = new SynchronizedPacketReceiver(this);
            _packetizer = new SSH2Packetizer(_packetReceiver);
        }

        internal void SetAgentForwardConfirmed(bool value) {
            _agentForwardConfirmed = value;
        }
        internal override IDataHandler Packetizer {
            get {
                return _packetizer;
            }
        }
        internal SSH2ConnectionInfo ConnectionInfo {
            get {
                return _cInfo;
            }
        }

        internal override AuthenticationResult Connect() {
            //key exchange
            KeyExchanger kex = new KeyExchanger(this, null);
            if (!kex.SynchronizedKexExchange()) {
                Close();
                return AuthenticationResult.Failure;
            }

            //user authentication
            ServiceRequest("ssh-userauth");
            _authenticationResult = UserAuth();
            return _authenticationResult;
        }


        private void ServiceRequest(string servicename) {
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_SERVICE_REQUEST)
                    .WriteString(servicename)
            );
            TraceTransmissionEvent("SSH_MSG_SERVICE_REQUEST", servicename);

            DataFragment response = ReceivePacket();
            SSH2DataReader re = new SSH2DataReader(response);
            SSH2PacketType t = (SSH2PacketType) re.ReadByte();
            if (t != SSH2PacketType.SSH_MSG_SERVICE_ACCEPT) {
                TraceReceptionEvent(t.ToString(), "service request failed");
                throw new SSHException("service establishment failed " + t);
            }

            string s = re.ReadString();
            if (servicename != s)
                throw new SSHException("protocol error");
        }

        private AuthenticationResult UserAuth() {
            const string sn = "ssh-connection";
            if (_param.AuthenticationType == AuthenticationType.KeyboardInteractive) {
                Transmit(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                        .WriteUTF8String(_param.UserName)
                        .WriteString(sn)
                        .WriteString("keyboard-interactive")
                        .WriteString("") //lang
                        .WriteString("") //submethod
                );
                TraceTransmissionEvent(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST, "starting keyboard-interactive authentication");
                _authenticationResult = ProcessAuthenticationResponse();
            }
            else {
                if (_param.AuthenticationType == AuthenticationType.Password) {
                    //Password authentication
                    Transmit(
                        new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                            .WriteUTF8String(_param.UserName)
                            .WriteString(sn)
                            .WriteString("password")
                            .WriteBool(false)
                            .WriteUTF8String(_param.Password)
                    );
                    TraceTransmissionEvent(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST, "starting password authentication");
                }
                else {
                    //public key authentication
                    SSH2UserAuthKey kp = SSH2UserAuthKey.FromSECSHStyleFile(_param.IdentityFile, _param.Password);
                    string algorithmName = SSH2Util.PublicKeyAlgorithmName(kp.Algorithm);

                    // construct a packet except signature
                    SSH2Packet packet =
                        new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                            .WriteUTF8String(_param.UserName)
                            .WriteString(sn)
                            .WriteString("publickey")
                            .WriteBool(true)
                            .WriteString(algorithmName)
                            .WriteAsString(kp.GetPublicKeyBlob());

                    // take payload image for the signature
                    byte[] payloadImage = packet.GetPayloadBytes();

                    // construct the signature source
                    SSH2PayloadImageBuilder workPayload =
                        new SSH2PayloadImageBuilder()
                            .WriteAsString(_sessionID)
                            .Write(payloadImage);

                    // take a signature blob
                    byte[] signatureBlob = kp.Sign(workPayload.GetBytes());

                    // encode signature (RFC4253)
                    workPayload.Clear();
                    byte[] signature =
                        workPayload
                            .WriteString(algorithmName)
                            .WriteAsString(signatureBlob)
                            .GetBytes();

                    // append signature to the packet
                    packet.WriteAsString(signature);

                    Transmit(packet);
                    TraceTransmissionEvent(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST, "starting public key authentication");
                }

                _authenticationResult = ProcessAuthenticationResponse();
                if (_authenticationResult == AuthenticationResult.Failure)
                    throw new SSHException(Strings.GetString("AuthenticationFailed"));
            }
            return _authenticationResult;
        }
        private AuthenticationResult ProcessAuthenticationResponse() {
            do {
                SSH2DataReader response = new SSH2DataReader(ReceivePacket());
                SSH2PacketType h = (SSH2PacketType) response.ReadByte();
                if (h == SSH2PacketType.SSH_MSG_USERAUTH_FAILURE) {
                    string msg = response.ReadString();
                    TraceReceptionEvent(h, "user authentication failed:" + msg);
                    return AuthenticationResult.Failure;
                }
                else if (h == SSH2PacketType.SSH_MSG_USERAUTH_BANNER) {
                    TraceReceptionEvent(h, "");
                }
                else if (h == SSH2PacketType.SSH_MSG_USERAUTH_SUCCESS) {
                    TraceReceptionEvent(h, "user authentication succeeded");
                    _packetizer.SetInnerHandler(new CallbackSSH2PacketHandler(this));
                    return AuthenticationResult.Success; //successfully exit
                }
                else if (h == SSH2PacketType.SSH_MSG_USERAUTH_INFO_REQUEST) {
                    string name = response.ReadUTF8String();
                    string inst = response.ReadUTF8String();
                    string lang = response.ReadString();
                    int num = response.ReadInt32();
                    string[] prompts = new string[num];
                    for (int i = 0; i < num; i++) {
                        prompts[i] = response.ReadUTF8String();
                        bool echo = response.ReadBool();
                    }
                    _eventReceiver.OnAuthenticationPrompt(prompts);
                    _requiredResponseCount = num;
                    return AuthenticationResult.Prompt;
                }
                else
                    throw new SSHException("protocol error: unexpected packet type " + h);
            } while (true);
        }
        public AuthenticationResult DoKeyboardInteractiveAuth(string[] input) {
            if (_param.AuthenticationType != AuthenticationType.KeyboardInteractive)
                throw new SSHException("DoKeyboardInteractiveAuth() must be called with keyboard-interactive authentication");

            bool sent = false;
            do {
                SSH2Packet packet = new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_INFO_RESPONSE);
                if (sent) {
                    packet.WriteInt32(0);
                }
                else {
                    packet.WriteInt32(input.Length);
                    foreach (string t in input) {
                        packet.WriteString(t);
                    }
                    sent = true;
                }
                Transmit(packet);

                _authenticationResult = ProcessAuthenticationResponse();

                //recent OpenSSH sends SSH_MSG_USERAUTH_INFO_REQUEST with 0-length prompt array after the first negotiation
            } while (_authenticationResult == AuthenticationResult.Prompt && _requiredResponseCount == 0);

            return _authenticationResult;
        }

        public override SSHChannel OpenShell(ISSHChannelEventReceiver receiver) {
            return DoExecCommandInternal(receiver, ChannelType.Shell, null, "opening shell");
        }

        // open new channel for SCP
        public override SSHChannel DoExecCommand(ISSHChannelEventReceiver receiver, string command) {
            return DoExecCommandInternal(receiver, ChannelType.ExecCommand, command, "executing " + command);
        }

        // open subsystem such as NETCONF
        public SSHChannel OpenSubsystem(ISSHChannelEventReceiver receiver, string subsystem) {
            return DoExecCommandInternal(receiver, ChannelType.Subsystem, subsystem, "subsystem " + subsystem);
        }

        //open channel
        private SSHChannel DoExecCommandInternal(ISSHChannelEventReceiver receiver, ChannelType channel_type, string command, string message) {
            int local_channel = this.ChannelCollection.RegisterChannelEventReceiver(null, receiver).LocalID;
            int windowsize = _param.WindowSize;
            SSH2Channel channel = new SSH2Channel(this, channel_type, local_channel, command);
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN)
                    .WriteString("session")
                    .WriteInt32(local_channel)
                    .WriteInt32(_param.WindowSize) //initial window size
                    .WriteInt32(_param.MaxPacketSize) //max packet size
            );
            TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_OPEN, message);
            return channel;
        }

        public override SSHChannel ForwardPort(ISSHChannelEventReceiver receiver, string remote_host, int remote_port, string originator_host, int originator_port) {
            int local_id = this.ChannelCollection.RegisterChannelEventReceiver(null, receiver).LocalID;
            int windowsize = _param.WindowSize;
            SSH2Channel channel = new SSH2Channel(this, ChannelType.ForwardedLocalToRemote, local_id, null);
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN)
                    .WriteString("direct-tcpip")
                    .WriteInt32(local_id)
                    .WriteInt32(_param.WindowSize) //initial window size
                    .WriteInt32(_param.MaxPacketSize) //max packet size
                    .WriteString(remote_host)
                    .WriteInt32(remote_port)
                    .WriteString(originator_host)
                    .WriteInt32(originator_port)
            );
            TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_OPEN, "opening a forwarded port : host={0} port={1}", remote_host, remote_port);
            return channel;
        }

        public override void ListenForwardedPort(string allowed_host, int bind_port) {
            _waitingForPortForwardingResponse = true;
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_GLOBAL_REQUEST)
                    .WriteString("tcpip-forward")
                    .WriteBool(true)
                    .WriteString(allowed_host)
                    .WriteInt32(bind_port)
            );
            TraceTransmissionEvent(SSH2PacketType.SSH_MSG_GLOBAL_REQUEST, "starting to listen to a forwarded port : host={0} port={1}", allowed_host, bind_port);
        }

        public override void CancelForwardedPort(string host, int port) {
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_GLOBAL_REQUEST)
                    .WriteString("cancel-tcpip-forward")
                    .WriteBool(true)
                    .WriteString(host)
                    .WriteInt32(port)
            );
            TraceTransmissionEvent(SSH2PacketType.SSH_MSG_GLOBAL_REQUEST, "terminating to listen to a forwarded port : host={0} port={1}", host, port);
        }

        private void ProcessPortforwardingRequest(ISSHConnectionEventReceiver receiver, SSH2DataReader reader) {

            int remote_channel = reader.ReadInt32();
            int window_size = reader.ReadInt32(); //skip initial window size
            int servermaxpacketsize = reader.ReadInt32();
            string host = reader.ReadString();
            int port = reader.ReadInt32();
            string originator_ip = reader.ReadString();
            int originator_port = reader.ReadInt32();

            TraceReceptionEvent("port forwarding request", String.Format("host={0} port={1} originator-ip={2} originator-port={3}", host, port, originator_ip, originator_port));
            PortForwardingCheckResult r = receiver.CheckPortForwardingRequest(host, port, originator_ip, originator_port);

            if (r.allowed) {
                //send OPEN_CONFIRMATION
                SSH2Channel channel = new SSH2Channel(this, ChannelType.ForwardedRemoteToLocal, this.ChannelCollection.RegisterChannelEventReceiver(null, r.channel).LocalID, remote_channel, servermaxpacketsize);
                receiver.EstablishPortforwarding(r.channel, channel);
                Transmit(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION)
                        .WriteInt32(remote_channel)
                        .WriteInt32(channel.LocalChannelID)
                        .WriteInt32(_param.WindowSize) //initial window size
                        .WriteInt32(_param.MaxPacketSize) //max packet size
                );
                TraceTransmissionEvent("port-forwarding request is confirmed", "host={0} port={1} originator-ip={2} originator-port={3}", host, port, originator_ip, originator_port);
            }
            else {
                Transmit(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        .WriteInt32(remote_channel)
                        .WriteInt32(r.reason_code)
                        .WriteUTF8String(r.reason_message)
                        .WriteString("") //lang tag
                );
                TraceTransmissionEvent("port-forwarding request is rejected", "host={0} port={1} originator-ip={2} originator-port={3}", host, port, originator_ip, originator_port);
            }
        }

        private void ProcessAgentForwardRequest(ISSHConnectionEventReceiver receiver, SSH2DataReader reader) {
            int remote_channel = reader.ReadInt32();
            int window_size = reader.ReadInt32(); //skip initial window size
            int servermaxpacketsize = reader.ReadInt32();
            TraceReceptionEvent("agent forward request", "");

            IAgentForward af = _param.AgentForward;
            if (_agentForwardConfirmed && af != null && af.CanAcceptForwarding()) {
                //send OPEN_CONFIRMATION
                AgentForwardingChannel ch = new AgentForwardingChannel(af);
                SSH2Channel channel = new SSH2Channel(this, ChannelType.AgentForward, this.ChannelCollection.RegisterChannelEventReceiver(null, ch).LocalID, remote_channel, servermaxpacketsize);
                ch.SetChannel(channel);
                Transmit(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION)
                        .WriteInt32(remote_channel)
                        .WriteInt32(channel.LocalChannelID)
                        .WriteInt32(_param.WindowSize) //initial window size
                        .WriteInt32(_param.MaxPacketSize) //max packet size
                );
                TraceTransmissionEvent("granados confirmed agent-forwarding request", "");
            }
            else {
                Transmit(
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        .WriteInt32(remote_channel)
                        .WriteInt32(0)
                        .WriteString("reject")
                        .WriteString("") //lang tag
                );
                TraceTransmissionEvent("granados rejected agent-forwarding request", "");
            }
        }

        internal void Transmit(SSH2Packet packet) {
            lock (_transmitSync) {
                _stream.Write(packet.GetImage(_cipher, _mac, _sequence++));
            }
        }

        internal DataFragment TransmitAndWaitResponse(SSH2Packet packet) {
            lock (_transmitSync) {
                var data = packet.GetImage(_cipher, _mac, _sequence++);
                return _packetReceiver.SendAndWaitResponse(data);
            }
        }

        //synchronous reception
        internal DataFragment ReceivePacket() {
            while (true) {
                DataFragment data = _packetReceiver.WaitResponse();

                SSH2PacketType pt = (SSH2PacketType)data[0]; //sneak

                //filter unnecessary packet
                if (pt == SSH2PacketType.SSH_MSG_IGNORE) {
                    SSH2DataReader r = new SSH2DataReader(data);
                    r.ReadByte(); //skip
                    byte[] msg = r.ReadByteString();
                    if (_eventReceiver != null)
                        _eventReceiver.OnIgnoreMessage(msg);
                    TraceReceptionEvent(pt, msg);
                }
                else if (pt == SSH2PacketType.SSH_MSG_DEBUG) {
                    SSH2DataReader r = new SSH2DataReader(data);
                    r.ReadByte(); //skip
                    bool f = r.ReadBool();
                    byte[] msg = r.ReadByteString();
                    if (_eventReceiver != null)
                        _eventReceiver.OnDebugMessage(f, msg);
                    TraceReceptionEvent(pt, msg);
                }
                else {
                    return data;
                }
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

        private bool ProcessPacket(DataFragment packet) {
            SSH2DataReader r = new SSH2DataReader(packet);
            SSH2PacketType pt = (SSH2PacketType) r.ReadByte();

            if (pt == SSH2PacketType.SSH_MSG_DISCONNECT) {
                int errorcode = r.ReadInt32();
                _eventReceiver.OnConnectionClosed();
                return false;
            }
            else if (_waitingForPortForwardingResponse) {
                if (pt != SSH2PacketType.SSH_MSG_REQUEST_SUCCESS)
                    _eventReceiver.OnUnknownMessage((byte)pt, packet.GetBytes());
                _waitingForPortForwardingResponse = false;
                return true;
            }
            else if (pt == SSH2PacketType.SSH_MSG_CHANNEL_OPEN) {
                string method = r.ReadString();
                if (method == "forwarded-tcpip")
                    ProcessPortforwardingRequest(_eventReceiver, r);
                else if (method.StartsWith("auth-agent")) //in most cases, method is "auth-agent@openssh.com"
                    ProcessAgentForwardRequest(_eventReceiver, r);
                else {
                    SSH2DataWriter wr = new SSH2DataWriter();
                    wr.WriteByte((byte)SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE);
                    wr.WriteInt32(r.ReadInt32());
                    wr.WriteInt32(0);
                    wr.WriteString("unknown method");
                    wr.WriteString(""); //lang tag
                    TraceReceptionEvent("SSH_MSG_CHANNEL_OPEN rejected", "method={0}", method);
                }
                return true;
            }
            else if (pt >= SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION && pt <= SSH2PacketType.SSH_MSG_CHANNEL_FAILURE) {
                int local_channel = r.ReadInt32();
                ChannelCollection.Entry e = this.ChannelCollection.FindChannelEntry(local_channel);
                if (e != null)
                    ((SSH2Channel)e.Channel).ProcessPacket(e.Receiver, pt, r);
                else
                    Debug.WriteLine("unexpected channel pt=" + pt + " local_channel=" + local_channel.ToString());
                return true;
            }
            else if (pt == SSH2PacketType.SSH_MSG_IGNORE) {
                _eventReceiver.OnIgnoreMessage(r.ReadByteString());
                return true;
            }
            else if (_asyncKeyExchanger != null) {
                _asyncKeyExchanger.AsyncProcessPacket(packet);
                return true;
            }
            else if (pt == SSH2PacketType.SSH_MSG_KEXINIT) {
                //Debug.WriteLine("Host sent KEXINIT");
                _asyncKeyExchanger = new KeyExchanger(this, _sessionID);
                _asyncKeyExchanger.AsyncProcessPacket(packet);
                return true;
            }
            else {
                _eventReceiver.OnUnknownMessage((byte)pt, packet.GetBytes());
                return false;
            }
        }

        public override void Disconnect(string msg) {
            if (!this.IsOpen)
                return;
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_DISCONNECT)
                    .WriteInt32(0)
                    .WriteString(msg)
                    .WriteString("") //language
            );
            Close();
        }

        public override void SendIgnorableData(string msg) {
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_IGNORE)
                    .WriteString(msg)
            );
        }

        //Start key refresh
        public void ReexchangeKeys() {
            _asyncKeyExchanger = new KeyExchanger(this, _sessionID);
            _asyncKeyExchanger.AsyncStartReexchange();
        }

        internal void RefreshKeys(byte[] sessionID, Cipher tc, Cipher rc, MAC tm, MAC rm) {
            lock (this) { //these must change synchronously
                _sessionID = sessionID;
                _cipher = tc;
                _mac = tm;
                _packetizer.SetCipher(rc, _param.CheckMACError ? rm : null);
                _asyncKeyExchanger = null;
            }
        }

        //alternative version
        internal void TraceTransmissionEvent(SSH2PacketType pt, string message, params object[] args) {
            ISSHEventTracer t = _param.EventTracer;
            if (t != null)
                t.OnTranmission(pt.ToString(), String.Format(message, args));
        }
        internal void TraceReceptionEvent(SSH2PacketType pt, string message, params object[] args) {
            ISSHEventTracer t = _param.EventTracer;
            if (t != null)
                t.OnReception(pt.ToString(), String.Format(message, args));
        }
        internal void TraceReceptionEvent(SSH2PacketType pt, byte[] msg) {
            TraceReceptionEvent(pt.ToString(), Encoding.ASCII.GetString(msg));
        }
    }

    /**
     * Channel
     */
    /// <exclude/>
    public class SSH2Channel : SSHChannel {
        private readonly SSH2Connection _connection;
        //channel property
        private readonly string _command;
        private readonly int _windowSize;
        private int _leftWindowSize;
        private int _serverMaxPacketSize;
        private uint _allowedDataSize;

        //negotiation status
        private enum NegotiationStatus {
            Ready,
            WaitingChannelConfirmation,
            WaitingPtyReqConfirmation,
            WaitingShellConfirmation,
            WaitingExecCmdConfirmation,
            WaitingAuthAgentReqConfirmation,
            WaitingSubsystemConfirmation,
        }
        private NegotiationStatus _negotiationStatus;

        //closing sequence control
        private bool _waitingChannelClose = false;

        public SSH2Channel(SSH2Connection con, ChannelType type, int local_id, string command)
            : base(con, type, local_id) {
            _command = command;
            if (type == ChannelType.ExecCommand || type == ChannelType.Subsystem)
                Debug.Assert(command != null); //'command' is required for ChannelType.ExecCommand
            _connection = con;
            _windowSize = _leftWindowSize = con.Param.WindowSize;
            _negotiationStatus = NegotiationStatus.WaitingChannelConfirmation;
        }

        //attach to an existing channel
        public SSH2Channel(SSH2Connection con, ChannelType type, int local_id, int remote_id, int maxpacketsize)
            : base(con, type, local_id) {
            _connection = con;
            _windowSize = _leftWindowSize = con.Param.WindowSize;
            Debug.Assert(type == ChannelType.ForwardedRemoteToLocal || type == ChannelType.AgentForward);
            _remoteID = remote_id;
            _serverMaxPacketSize = maxpacketsize;
            _negotiationStatus = NegotiationStatus.Ready;
        }

        public override void ResizeTerminal(int width, int height, int pixel_width, int pixel_height) {
            Transmit(
                0,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteInt32(_remoteID)
                    .WriteString("window-change")
                    .WriteBool(false)
                    .WriteInt32(width)
                    .WriteInt32(height)
                    .WriteInt32(pixel_width) //no graphics
                    .WriteInt32(pixel_height)
            );
            _connection.TraceTransmissionEvent("window-change", "width={0} height={1}", width, height);
        }

        public override void Transmit(byte[] data) {
            Transmit(
                data.Length,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_DATA)
                    .WriteInt32(_remoteID)
                    .WriteAsString(data)
            );
        }

        public override void Transmit(byte[] data, int offset, int length) {
            Transmit(
                length,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_DATA)
                    .WriteInt32(_remoteID)
                    .WriteAsString(data, offset, length)
            );
        }

        public override void SendEOF() {
            if (!_connection.IsOpen)
                return;
            Transmit(
                0,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_EOF)
                    .WriteInt32(_remoteID)
            );
            _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_EOF, "");
        }

        public override void Close() {
            if (!_connection.IsOpen)
                return;
            _waitingChannelClose = true;
            Transmit(
                0,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_CLOSE)
                    .WriteInt32(_remoteID)
            );
            _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_CLOSE, "");
        }

        //maybe this is SSH2 only feature
        public void SetEnvironmentVariable(string name, string value) {
            Transmit(
                0,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteInt32(_remoteID)
                    .WriteString("env")
                    .WriteBool(false)
                    .WriteString(name)
                    .WriteString(value)
            );
            _connection.TraceTransmissionEvent("env", "name={0} value={1}", name, value);
        }

        public void SendBreak(int time) {
            Transmit(
                0,
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteInt32(_remoteID)
                    .WriteString("break")
                    .WriteBool(true)
                    .WriteInt32(time)
            );
            _connection.TraceTransmissionEvent("break", "time={0}", time);
        }

        internal void ProcessPacket(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader re) {
            //NOTE: the offset of 're' is next to 'receipiant channel' field

            if (pt == SSH2PacketType.SSH_MSG_CHANNEL_DATA || pt == SSH2PacketType.SSH_MSG_CHANNEL_EXTENDED_DATA) {
                AdjustWindowSize(pt, re.RemainingDataLength);
            }

            //SSH_MSG_CHANNEL_WINDOW_ADJUST comes before the complete of channel establishment
            if (pt == SSH2PacketType.SSH_MSG_CHANNEL_WINDOW_ADJUST) {
                uint w = re.ReadUInt32();
                //some servers may not send SSH_MSG_CHANNEL_WINDOW_ADJUST. 
                //it is dangerous to wait this message in send procedure
                _allowedDataSize += w;
                if (_connection.IsEventTracerAvailable)
                    _connection.TraceReceptionEvent("SSH_MSG_CHANNEL_WINDOW_ADJUST", "adjusted to {0} by increasing {1}", _allowedDataSize, w);
                return;
            }

            // check closing sequence
            if (_waitingChannelClose && pt == SSH2PacketType.SSH_MSG_CHANNEL_CLOSE) {
                _waitingChannelClose = false;
                return; // ignore it
            }

            if (_negotiationStatus != NegotiationStatus.Ready) //when the negotiation is not completed
                ProgressChannelNegotiation(receiver, pt, re);
            else
                ProcessChannelLocalData(receiver, pt, re);
        }

        private void AdjustWindowSize(SSH2PacketType pt, int dataLength) {
            // need not send window size to server when the channel is not opened.
            if (_negotiationStatus != NegotiationStatus.Ready)
                return;

            _leftWindowSize = Math.Max(_leftWindowSize - dataLength, 0);

            if (_leftWindowSize < _windowSize / 2) {
                Transmit(
                    0,
                    new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_WINDOW_ADJUST)
                        .WriteInt32(_remoteID)
                        .WriteInt32(_windowSize - _leftWindowSize)
                );
                if (_connection.IsEventTracerAvailable)
                    _connection.TraceTransmissionEvent("SSH_MSG_CHANNEL_WINDOW_ADJUST", "adjusted window size : {0} --> {1}", _leftWindowSize, _windowSize);
                _leftWindowSize = _windowSize;
            }
        }

        //Progress the state of this channel establishment negotiation
        private void ProgressChannelNegotiation(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader re) {
            if (_type == ChannelType.Shell)
                OpenShellOrSubsystem(receiver, pt, re, "shell");
            else if (_type == ChannelType.ForwardedLocalToRemote)
                ReceivePortForwardingResponse(receiver, pt, re);
            else if (_type == ChannelType.Session)
                EstablishSession(receiver, pt, re);
            else if (_type == ChannelType.ExecCommand)  // for SCP
                ExecCommand(receiver, pt, re);
            else if (_type == ChannelType.Subsystem)
                OpenShellOrSubsystem(receiver, pt, re, "subsystem");
        }

        private void ProcessChannelLocalData(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader re) {
            switch (pt) {
                case SSH2PacketType.SSH_MSG_CHANNEL_DATA: {
                        int len = re.ReadInt32();
                        DataFragment frag = re.GetRemainingDataView(len);
                        receiver.OnData(frag.Data, frag.Offset, frag.Length);
                    }
                    break;
                case SSH2PacketType.SSH_MSG_CHANNEL_EXTENDED_DATA: {
                        int t = re.ReadInt32();
                        byte[] data = re.ReadByteString();
                        receiver.OnExtendedData(t, data);
                    }
                    break;
                case SSH2PacketType.SSH_MSG_CHANNEL_REQUEST: {
                        string request = re.ReadString();
                        bool reply = re.ReadBool();
                        if (request == "exit-status") {
                            int status = re.ReadInt32();
                        }
                        else if (reply) { //we reject unknown requests including keep-alive check
                            Transmit(
                                0,
                                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_FAILURE)
                                    .WriteInt32(_remoteID)
                            );
                        }
                    }
                    break;
                case SSH2PacketType.SSH_MSG_CHANNEL_EOF:
                    receiver.OnChannelEOF();
                    break;
                case SSH2PacketType.SSH_MSG_CHANNEL_CLOSE:
                    _connection.ChannelCollection.UnregisterChannelEventReceiver(_localID);
                    receiver.OnChannelClosed();
                    break;
                case SSH2PacketType.SSH_MSG_CHANNEL_FAILURE: {
                        DataFragment frag = re.GetRemainingDataView();
                        receiver.OnMiscPacket((byte)pt, frag.Data, frag.Offset, frag.Length);
                    }
                    break;
                default: {
                        DataFragment frag = re.GetRemainingDataView();
                        receiver.OnMiscPacket((byte)pt, frag.Data, frag.Offset, frag.Length);
                    }
                    Debug.WriteLine("Unknown Packet " + pt);
                    break;
            }
        }

        private void Transmit(int consumedSize, SSH2Packet packet) {
            if (_allowedDataSize < (uint)consumedSize) {
                // FIXME: currently, window size on the remote side is totally ignored...
                _allowedDataSize = 0;
            }
            else {
                _allowedDataSize -= (uint)consumedSize;
            }
            _connection.Transmit(packet);
        }

        private void OpenShellOrSubsystem(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader reader, string scheme) {
            if (_negotiationStatus == NegotiationStatus.WaitingChannelConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION) {
                    if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        receiver.OnChannelError(new SSHException("opening channel failed; packet type=" + pt));
                    else {
                        int errcode = reader.ReadInt32();
                        string msg = reader.ReadUTF8String();
                        receiver.OnChannelError(new SSHException(msg));
                    }
                    // Close() shouldn't be called because remote channel number is not given yet.
                    // We just remove an event receiver from the collection of channels.
                    // FIXME: _negotiationStatus sould be set an error status ?
                    _connection.ChannelCollection.UnregisterChannelEventReceiver(_localID);
                }
                else {
                    _remoteID = reader.ReadInt32();
                    _allowedDataSize = reader.ReadUInt32();
                    _serverMaxPacketSize = reader.ReadInt32();

                    if (_type == ChannelType.Subsystem) {
                        OpenScheme(scheme);
                        _negotiationStatus = NegotiationStatus.WaitingSubsystemConfirmation;
                    }
                    else {
                        //open pty
                        SSHConnectionParameter param = _connection.Param;
                        Transmit(
                            0,
                            new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                                .WriteInt32(_remoteID)
                                .WriteString("pty-req")
                                .WriteBool(true)
                                .WriteString(param.TerminalName)
                                .WriteInt32(param.TerminalWidth)
                                .WriteInt32(param.TerminalHeight)
                                .WriteInt32(param.TerminalPixelWidth)
                                .WriteInt32(param.TerminalPixelHeight)
                                .WriteAsString(new byte[0])
                        );

                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceTransmissionEvent(
                                SSH2PacketType.SSH_MSG_CHANNEL_REQUEST, "pty-req", "terminal={0} width={1} height={2}",
                                param.TerminalName, param.TerminalWidth, param.TerminalHeight);
                        }

                        _negotiationStatus = NegotiationStatus.WaitingPtyReqConfirmation;
                    }
                }
            }
            else if (_negotiationStatus == NegotiationStatus.WaitingPtyReqConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS) {
                    receiver.OnChannelError(new SSHException("opening pty failed"));
                    Close();
                }
                else {
                    //agent request (optional)
                    if (_connection.Param.AgentForward != null) {
                        Transmit(
                            0,
                            new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                                .WriteInt32(_remoteID)
                                .WriteString("auth-agent-req@openssh.com")
                                .WriteBool(true)
                        );
                        _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST, "auth-agent-req", "");
                        _negotiationStatus = NegotiationStatus.WaitingAuthAgentReqConfirmation;
                    }
                    else {
                        OpenScheme(scheme);
                        _negotiationStatus = NegotiationStatus.WaitingShellConfirmation;
                    }
                }
            }
            else if (_negotiationStatus == NegotiationStatus.WaitingAuthAgentReqConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS && pt != SSH2PacketType.SSH_MSG_CHANNEL_FAILURE) {
                    receiver.OnChannelError(new SSHException("auth-agent-req error"));
                    Close();
                }
                else { //auth-agent-req is optional
                    _connection.SetAgentForwardConfirmed(pt == SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS);
                    _connection.TraceReceptionEvent(pt, "auth-agent-req");

                    OpenScheme(scheme);
                    _negotiationStatus = NegotiationStatus.WaitingShellConfirmation;
                }
            }
            else if (_negotiationStatus == NegotiationStatus.WaitingShellConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS) {
                    receiver.OnChannelError(new SSHException("Opening shell failed: packet type=" + pt.ToString()));
                    Close();
                }
                else {
                    receiver.OnChannelReady();
                    _negotiationStatus = NegotiationStatus.Ready; //goal!
                }
            }
            else if (_negotiationStatus == NegotiationStatus.WaitingSubsystemConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_SUCCESS) {
                    receiver.OnChannelError(new SSHException("Opening subsystem failed: packet type=" + pt.ToString()));
                    Close();
                }
                else {
                    receiver.OnChannelReady();
                    _negotiationStatus = NegotiationStatus.Ready; //goal!
                }
            }
        }

        private void OpenScheme(string scheme) {
            //open shell / subsystem
            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                    .WriteInt32(_remoteID)
                    .WriteString(scheme)
                    .WriteBool(true);
            if (_command != null) {
                packet.WriteString(_command);
            }
            Transmit(0, packet);
        }

        // sending "exec" service for SCP protocol.
        private void ExecCommand(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader reader) {
            if (_negotiationStatus == NegotiationStatus.WaitingChannelConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION) {
                    if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        receiver.OnChannelError(new SSHException("opening channel failed; packet type=" + pt));
                    else {
                        int errcode = reader.ReadInt32();
                        string msg = reader.ReadUTF8String();
                        receiver.OnChannelError(new SSHException(msg));
                    }
                    Close();
                }
                else {
                    _remoteID = reader.ReadInt32();
                    _allowedDataSize = reader.ReadUInt32();
                    _serverMaxPacketSize = reader.ReadInt32();

                    // exec command
                    SSHConnectionParameter param = _connection.Param;
                    Transmit(
                        0,
                        new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_REQUEST)
                            .WriteInt32(_remoteID)
                            .WriteString("exec")  // "exec"
                            .WriteBool(false)   // want confirm is disabled. (*)
                            .WriteString(_command)
                    );
                    if (_connection.IsEventTracerAvailable)
                        _connection.TraceTransmissionEvent("exec command", "cmd={0}", _command);

                    //confirmation is omitted
                    receiver.OnChannelReady();
                    _negotiationStatus = NegotiationStatus.Ready; //goal!
                }
            }
            else if (_negotiationStatus == NegotiationStatus.WaitingExecCmdConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_DATA) {
                    receiver.OnChannelError(new SSHException("exec command failed"));
                    Close();
                }
                else {
                    receiver.OnChannelReady();
                    _negotiationStatus = NegotiationStatus.Ready; //goal!
                }
            }
            else
                throw new SSHException("internal state error");
        }


        private void ReceivePortForwardingResponse(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader reader) {
            if (_negotiationStatus == NegotiationStatus.WaitingChannelConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION) {
                    if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        receiver.OnChannelError(new SSHException("opening channel failed; packet type=" + pt));
                    else {
                        int errcode = reader.ReadInt32();
                        string msg = reader.ReadUTF8String();
                        receiver.OnChannelError(new SSHException(msg));
                    }
                    Close();
                }
                else {
                    _remoteID = reader.ReadInt32();
                    _serverMaxPacketSize = reader.ReadInt32();
                    _negotiationStatus = NegotiationStatus.Ready;
                    receiver.OnChannelReady();
                }
            }
            else
                throw new SSHException("internal state error");
        }
        private void EstablishSession(ISSHChannelEventReceiver receiver, SSH2PacketType pt, SSH2DataReader reader) {
            if (_negotiationStatus == NegotiationStatus.WaitingChannelConfirmation) {
                if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION) {
                    if (pt != SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                        receiver.OnChannelError(new SSHException("opening channel failed; packet type=" + pt));
                    else {
                        int remote_id = reader.ReadInt32();
                        int errcode = reader.ReadInt32();
                        string msg = reader.ReadUTF8String();
                        receiver.OnChannelError(new SSHException(msg));
                    }
                    Close();
                }
                else {
                    _remoteID = reader.ReadInt32();
                    _serverMaxPacketSize = reader.ReadInt32();
                    _negotiationStatus = NegotiationStatus.Ready;
                    receiver.OnChannelReady();
                }
            }
            else
                throw new SSHException("internal state error");
        }
    }

    /**
     * Key Exchange
     */
    internal class KeyExchanger {
        private readonly SSH2Connection _connection;
        private readonly SSHConnectionParameter _param;
        private readonly SSH2ConnectionInfo _cInfo;
        //payload of KEXINIT message
        private byte[] _serverKEXINITPayload;
        private byte[] _clientKEXINITPayload;

        //true if the host sent KEXINIT first
        private bool _startedByHost;

        //asynchronously?
        private enum Mode {
            Synchronized,
            Asynchronized
        }

        //status
        private enum Status {
            INITIAL,
            WAIT_KEXINIT,
            WAIT_KEXDH_REPLY,
            WAIT_NEWKEYS,
            FINISHED
        }
        private Status _status;

        private BigInteger _x;
        private BigInteger _e;
        private BigInteger _k;
        private byte[] _hash;
        private byte[] _sessionID;

        //results
        private Cipher _rc;
        private Cipher _tc;
        private MAC _rm;
        private MAC _tm;

        private struct SupportedKexAlgorithm {
            public readonly string name;
            public readonly KexAlgorithm value;

            public SupportedKexAlgorithm(string name, KexAlgorithm value) {
                this.name = name;
                this.value = value;
            }
        }

        private static readonly SupportedKexAlgorithm[] supportedKexAlgorithms = new SupportedKexAlgorithm[] {
            new SupportedKexAlgorithm("diffie-hellman-group16-sha512", KexAlgorithm.DH_G16_SHA512),
            new SupportedKexAlgorithm("diffie-hellman-group18-sha512", KexAlgorithm.DH_G18_SHA512),
            new SupportedKexAlgorithm("diffie-hellman-group14-sha256", KexAlgorithm.DH_G14_SHA256),
            new SupportedKexAlgorithm("diffie-hellman-group14-sha1", KexAlgorithm.DH_G14_SHA1),
            new SupportedKexAlgorithm("diffie-hellman-group1-sha1", KexAlgorithm.DH_G1_SHA1),
        };

        public KeyExchanger(SSH2Connection con, byte[] sessionID) {
            _connection = con;
            _param = con.Param;
            _cInfo = (SSH2ConnectionInfo)con.ConnectionInfo;
            _sessionID = sessionID;
            _status = Status.INITIAL;
        }

        private void Transmit(SSH2Packet packet) {
            _connection.Transmit(packet);
        }

        private DataFragment TransmitAndWaitResponse(SSH2Packet packet) {
            return _connection.TransmitAndWaitResponse(packet);
        }

        private void TraceTransmissionNegotiation(SSH2PacketType pt, string msg) {
            _connection.TraceTransmissionEvent(pt.ToString(), msg);
        }

        private void TraceReceptionNegotiation(SSH2PacketType pt, string msg) {
            _connection.TraceReceptionEvent(pt.ToString(), msg);
        }

        public bool SynchronizedKexExchange() {
            Mode m = Mode.Synchronized;
            DataFragment response;
            //note that the KEXINIT is sent asynchronously in most cases
            SendKEXINIT(m);
            response = _connection.ReceivePacket();

            ProcessKEXINIT(response);
            response = SendKEXDHINIT(m);
            if (!ProcessKEXDHREPLY(response))
                return false;
            response = SendNEWKEYS(m);
            ProcessNEWKEYS(response);
            return true;
        }
        public void AsyncStartReexchange() {
            _startedByHost = false;
            _status = Status.WAIT_KEXINIT;
            TraceTransmissionNegotiation(SSH2PacketType.SSH_MSG_KEXINIT, "starting asynchronously key exchange");
            SendKEXINIT(Mode.Asynchronized);

        }
        public void AsyncProcessPacket(DataFragment packet) {
            Mode m = Mode.Asynchronized;
            switch (_status) {
                case Status.INITIAL:
                    _startedByHost = true;
                    ProcessKEXINIT(packet);
                    SendKEXINIT(m);
                    SendKEXDHINIT(m);
                    break;
                case Status.WAIT_KEXINIT:
                    ProcessKEXINIT(packet);
                    SendKEXDHINIT(m);
                    break;
                case Status.WAIT_KEXDH_REPLY:
                    ProcessKEXDHREPLY(packet);
                    SendNEWKEYS(m);
                    break;
                case Status.WAIT_NEWKEYS:
                    ProcessNEWKEYS(packet);
                    Debug.Assert(_status == Status.FINISHED);
                    break;
            }
        }

        private void SendKEXINIT(Mode mode) {
            const string mac_algorithm = "hmac-sha1";

            _status = Status.WAIT_KEXINIT;

            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXINIT)
                    .WriteSecureRandomBytes(16) // cookie
                    .WriteString(GetSupportedKexAlgorithms()) // kex_algorithms
                    .WriteString(FormatHostKeyAlgorithmDescription()) // server_host_key_algorithms
                    .WriteString(FormatCipherAlgorithmDescription()) // encryption_algorithms_client_to_server
                    .WriteString(FormatCipherAlgorithmDescription()) // encryption_algorithms_server_to_client
                    .WriteString(mac_algorithm) // mac_algorithms_client_to_server
                    .WriteString(mac_algorithm) // mac_algorithms_server_to_client
                    .WriteString("none") // compression_algorithms_client_to_server
                    .WriteString("none") // compression_algorithms_server_to_client
                    .WriteString("") // languages_client_to_server
                    .WriteString("") // languages_server_to_client
                    .WriteBool(false) // indicates whether a guessed key exchange packet follows
                    .WriteInt32(0); // reserved for future extension

            _clientKEXINITPayload = packet.GetPayloadBytes();

            Transmit(packet);

            if (_connection.IsEventTracerAvailable) {
                StringBuilder bld = new StringBuilder();
                bld.Append("kex_algorithm=");
                bld.Append(GetSupportedKexAlgorithms());
                bld.Append("; server_host_key_algorithms=");
                bld.Append(FormatHostKeyAlgorithmDescription());
                bld.Append("; encryption_algorithms_client_to_server=");
                bld.Append(FormatCipherAlgorithmDescription());
                bld.Append("; encryption_algorithms_server_to_client=");
                bld.Append(FormatCipherAlgorithmDescription());
                bld.Append("; mac_algorithms_client_to_server=");
                bld.Append(mac_algorithm);
                bld.Append("; mac_algorithms_server_to_client=");
                bld.Append(mac_algorithm);
                TraceTransmissionNegotiation(SSH2PacketType.SSH_MSG_KEXINIT, bld.ToString());
            }
        }

        private void ProcessKEXINIT(DataFragment packet) {
            SSH2DataReader re = null;
            do {
                _serverKEXINITPayload = packet.GetBytes();
                re = new SSH2DataReader(_serverKEXINITPayload);
                byte[] head = re.Read(17); //Type and cookie
                SSH2PacketType pt = (SSH2PacketType)head[0];

                if (pt == SSH2PacketType.SSH_MSG_KEXINIT)
                    break; //successfully exit
                
                if (pt == SSH2PacketType.SSH_MSG_IGNORE || pt == SSH2PacketType.SSH_MSG_DEBUG) { //continue
                    packet = _connection.ReceivePacket();
                }
                else {
                    throw new SSHException(String.Format("Server response is not SSH_MSG_KEXINIT but {0}", head[0]));
                }
            } while (true);

            string kex = re.ReadString();
            _cInfo.SupportedKEXAlgorithms = kex;
            _cInfo.KEXAlgorithm = DecideKexAlgorithm(kex);

            string host_key = re.ReadString();
            _cInfo.SupportedHostKeyAlgorithms = host_key;
            _cInfo.HostKeyAlgorithm = DecideHostKeyAlgorithm(host_key);

            string enc_cs = re.ReadString();
            _cInfo.SupportedEncryptionAlgorithmsClientToServer = enc_cs;
            _cInfo.OutgoingPacketCipher = DecideCipherAlgorithm(enc_cs);

            string enc_sc = re.ReadString();
            _cInfo.SupportedEncryptionAlgorithmsServerToClient = enc_sc;
            _cInfo.IncomingPacketCipher = DecideCipherAlgorithm(enc_sc);

            string mac_cs = re.ReadString();
            CheckAlgorithmSupport("mac", mac_cs, "hmac-sha1");

            string mac_sc = re.ReadString();
            CheckAlgorithmSupport("mac", mac_sc, "hmac-sha1");

            string comp_cs = re.ReadString();
            CheckAlgorithmSupport("compression", comp_cs, "none");
            string comp_sc = re.ReadString();
            CheckAlgorithmSupport("compression", comp_sc, "none");

            string lang_cs = re.ReadString();
            string lang_sc = re.ReadString();
            bool flag = re.ReadBool();
            int reserved = re.ReadInt32();
            Debug.Assert(re.RemainingDataLength == 0);

            if (_connection.IsEventTracerAvailable) {
                StringBuilder bld = new StringBuilder();
                bld.Append("kex_algorithm=");
                bld.Append(kex);
                bld.Append("; server_host_key_algorithms=");
                bld.Append(host_key);
                bld.Append("; encryption_algorithms_client_to_server=");
                bld.Append(enc_cs);
                bld.Append("; encryption_algorithms_server_to_client=");
                bld.Append(enc_sc);
                bld.Append("; mac_algorithms_client_to_server=");
                bld.Append(mac_cs);
                bld.Append("; mac_algorithms_server_to_client=");
                bld.Append(mac_sc);
                bld.Append("; comression_algorithms_client_to_server=");
                bld.Append(comp_cs);
                bld.Append("; comression_algorithms_server_to_client=");
                bld.Append(comp_sc);
                TraceReceptionNegotiation(SSH2PacketType.SSH_MSG_KEXINIT, bld.ToString());
            }

            if (flag)
                throw new SSHException("Algorithm negotiation failed");
        }


        private DataFragment SendKEXDHINIT(Mode mode) {
            //Round1 computes and sends [e]
            BigInteger p = GetDiffieHellmanPrime(_cInfo.KEXAlgorithm.Value);
            //Generate x : 1 < x < (p-1)/2
            int xBytes = (p.BitCount() - 2) / 8;
            Rng rng = RngManager.GetSecureRng();
            do {
                byte[] sx = new byte[xBytes];
                rng.GetBytes(sx);
                _x = new BigInteger(sx);
            } while (_x <= 1);
            _e = new BigInteger(2).ModPow(_x, p);

            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXDH_INIT)
                    .WriteBigInteger(_e);

            _status = Status.WAIT_KEXDH_REPLY;
            TraceTransmissionNegotiation(SSH2PacketType.SSH_MSG_KEXDH_INIT, "");

            if (mode == Mode.Synchronized) {
                return TransmitAndWaitResponse(packet);
            }
            else {
                Transmit(packet);
                return null;
            }
        }

        private byte[] KexComputeHash(byte[] b) {
            switch (_cInfo.KEXAlgorithm) {
                case KexAlgorithm.DH_G1_SHA1:
                case KexAlgorithm.DH_G14_SHA1:
                    return new SHA1CryptoServiceProvider().ComputeHash(b);

                case KexAlgorithm.DH_G14_SHA256:
                    return new SHA256CryptoServiceProvider().ComputeHash(b);

                case KexAlgorithm.DH_G16_SHA512:
                case KexAlgorithm.DH_G18_SHA512:
                    return new SHA512CryptoServiceProvider().ComputeHash(b);

                default:
                    throw new SSHException("KexAlgorithm is not set");
            }
        }

        private bool ProcessKEXDHREPLY(DataFragment packet) {
            //Round2 receives response
            SSH2DataReader re = null;
            SSH2PacketType h;
            do {
                re = new SSH2DataReader(packet);
                h = (SSH2PacketType) re.ReadByte();
                if (h == SSH2PacketType.SSH_MSG_KEXDH_REPLY)
                    break; //successfully exit
                else if (h == SSH2PacketType.SSH_MSG_IGNORE || h == SSH2PacketType.SSH_MSG_DEBUG) { //continue
                    packet = _connection.ReceivePacket();
                }
                else
                    throw new SSHException(String.Format("KeyExchange response is not KEXDH_REPLY but {0}", h));
            } while (true);

            byte[] key_and_cert = re.ReadByteString();
            BigInteger f = re.ReadMPInt();
            byte[] signature = re.ReadByteString();
            Debug.Assert(re.RemainingDataLength == 0);

            //Round3 calc hash H
            SSH2DataWriter wr = new SSH2DataWriter();
            _k = f.ModPow(_x, GetDiffieHellmanPrime(_cInfo.KEXAlgorithm.Value));
            wr = new SSH2DataWriter();
            wr.WriteString(_cInfo.ClientVersionString);
            wr.WriteString(_cInfo.ServerVersionString);
            wr.WriteAsString(_clientKEXINITPayload);
            wr.WriteAsString(_serverKEXINITPayload);
            wr.WriteAsString(key_and_cert);
            wr.WriteBigInteger(_e);
            wr.WriteBigInteger(f);
            wr.WriteBigInteger(_k);
            _hash = KexComputeHash(wr.ToByteArray());

            _connection.TraceReceptionEvent(h, "verifying host key");
            if (!VerifyHostKey(key_and_cert, signature, _hash))
                return false;

            //Debug.WriteLine("hash="+DebugUtil.DumpByteArray(hash));
            if (_sessionID == null)
                _sessionID = _hash;
            return true;
        }

        private DataFragment SendNEWKEYS(Mode mode) {
            _status = Status.WAIT_NEWKEYS;
            Monitor.Enter(_connection); //lock the connection during we exchange sSH_MSG_NEWKEYS
            Transmit(
                new SSH2Packet(SSH2PacketType.SSH_MSG_NEWKEYS)
            );

            //establish Ciphers
            _tc = CipherFactory.CreateCipher(SSHProtocol.SSH2, _cInfo.OutgoingPacketCipher.Value,
                DeriveKey(_k, _hash, 'C', CipherFactory.GetKeySize(_cInfo.OutgoingPacketCipher.Value)), DeriveKey(_k, _hash, 'A', CipherFactory.GetBlockSize(_cInfo.OutgoingPacketCipher.Value)));
            _rc = CipherFactory.CreateCipher(SSHProtocol.SSH2, _cInfo.IncomingPacketCipher.Value,
                DeriveKey(_k, _hash, 'D', CipherFactory.GetKeySize(_cInfo.IncomingPacketCipher.Value)), DeriveKey(_k, _hash, 'B', CipherFactory.GetBlockSize(_cInfo.IncomingPacketCipher.Value)));

            //establish MACs
            MACAlgorithm ma = MACAlgorithm.HMACSHA1;
            _tm = MACFactory.CreateMAC(MACAlgorithm.HMACSHA1, DeriveKey(_k, _hash, 'E', MACFactory.GetSize(ma)));
            _rm = MACFactory.CreateMAC(MACAlgorithm.HMACSHA1, DeriveKey(_k, _hash, 'F', MACFactory.GetSize(ma)));

            if (mode == Mode.Synchronized)
                return _connection.ReceivePacket();
            else
                return null;
        }

        private void ProcessNEWKEYS(DataFragment packet) {
            //confirms new key
            if (packet.Length != 1 || packet[0] != (byte)SSH2PacketType.SSH_MSG_NEWKEYS) {
                Monitor.Exit(_connection);
                throw new SSHException("SSH_MSG_NEWKEYS failed");
            }

            _connection.RefreshKeys(_sessionID, _tc, _rc, _tm, _rm);
            Monitor.Exit(_connection);
            TraceReceptionNegotiation(SSH2PacketType.SSH_MSG_NEWKEYS, "the keys are refreshed");
            _status = Status.FINISHED;
        }

        private bool VerifyHostKey(byte[] K_S, byte[] signature, byte[] hash) {
            SSH2DataReader re1 = new SSH2DataReader(K_S);
            string algorithm = re1.ReadString();
            if (algorithm != SSH2Util.PublicKeyAlgorithmName(_cInfo.HostKeyAlgorithm.Value))
                throw new SSHException("Protocol Error: Host Key Algorithm Mismatch");

            SSH2DataReader re2 = new SSH2DataReader(signature);
            algorithm = re2.ReadString();
            if (algorithm != SSH2Util.PublicKeyAlgorithmName(_cInfo.HostKeyAlgorithm.Value))
                throw new SSHException("Protocol Error: Host Key Algorithm Mismatch");
            byte[] sigbody = re2.ReadByteString();
            Debug.Assert(re2.RemainingDataLength == 0);

            if (_cInfo.HostKeyAlgorithm == PublicKeyAlgorithm.RSA) {
                RSAPublicKey pk = ReadRSAPublicKey(re1, sigbody, hash);
                pk.VerifyWithSHA1(sigbody, new SHA1CryptoServiceProvider().ComputeHash(hash));
                _cInfo.HostKey = pk;
            }
            else if (_cInfo.HostKeyAlgorithm == PublicKeyAlgorithm.DSA) {
                DSAPublicKey pk = ReadDSAPublicKey(re1, sigbody, hash);
                pk.Verify(sigbody, new SHA1CryptoServiceProvider().ComputeHash(hash));
                _cInfo.HostKey = pk;
            }
            else
                throw new SSHException("Bad host key algorithm " + _cInfo.HostKeyAlgorithm);

            //ask the client whether he accepts the host key
            if (!_startedByHost && _param.VerifySSHHostKey != null) {
                if (!_param.VerifySSHHostKey(_cInfo.GetSSHHostKeyInformationProvider())) {
                    return false;
                }
            }

            return true;
        }

        private RSAPublicKey ReadRSAPublicKey(SSH2DataReader pubkey, byte[] sigbody, byte[] hash) {
            BigInteger exp = pubkey.ReadMPInt();
            BigInteger mod = pubkey.ReadMPInt();
            Debug.Assert(pubkey.RemainingDataLength == 0);

            RSAPublicKey pk = new RSAPublicKey(exp, mod);
            return pk;
        }

        private DSAPublicKey ReadDSAPublicKey(SSH2DataReader pubkey, byte[] sigbody, byte[] hash) {
            BigInteger p = pubkey.ReadMPInt();
            BigInteger q = pubkey.ReadMPInt();
            BigInteger g = pubkey.ReadMPInt();
            BigInteger y = pubkey.ReadMPInt();
            Debug.Assert(pubkey.RemainingDataLength == 0);

            DSAPublicKey pk = new DSAPublicKey(p, g, q, y);
            return pk;
        }

        private byte[] DeriveKey(BigInteger key, byte[] hash, char ch, int length) {
            byte[] result = new byte[length];

            SSH2DataWriter wr = new SSH2DataWriter();
            wr.WriteBigInteger(key);
            wr.Write(hash);
            wr.WriteByte((byte)ch);
            wr.Write(_sessionID);
            byte[] h1 = KexComputeHash(wr.ToByteArray());
            if (h1.Length >= length) {
                Array.Copy(h1, 0, result, 0, length);
                return result;
            }
            else {
                wr = new SSH2DataWriter();
                wr.WriteBigInteger(key);
                wr.Write(_sessionID);
                wr.Write(h1);
                byte[] h2 = KexComputeHash(wr.ToByteArray());
                if (h1.Length + h2.Length >= length) {
                    Array.Copy(h1, 0, result, 0, h1.Length);
                    Array.Copy(h2, 0, result, h1.Length, length - h1.Length);
                    return result;
                }
                else
                    throw new SSHException("necessary key length is too big"); //long key is not supported
            }
        }

        private static void CheckAlgorithmSupport(string title, string data, string algorithm_name) {
            string[] t = data.Split(',');
            foreach (string s in t) {
                if (s == algorithm_name)
                    return; //found!
            }
            throw new SSHException("Server does not support " + algorithm_name + " for " + title);
        }
        private KexAlgorithm DecideKexAlgorithm(string data) {
            string[] k = data.Split(',');
            foreach (string s in k) {
                foreach (SupportedKexAlgorithm algorithm in supportedKexAlgorithms) {
                    if (algorithm.name == s) {
                        return algorithm.value;
                    }
                }
            }
            throw new SSHException("The negotiation of kex algorithm is failed");
        }
        private string GetSupportedKexAlgorithms() {
            StringBuilder s = new StringBuilder();
            string sep = "";
            for (int i = 0; i < supportedKexAlgorithms.Length; ++i) {
                s.Append(sep).Append(supportedKexAlgorithms[i].name);
                sep = ",";
            }
            return s.ToString();
        }
        private PublicKeyAlgorithm DecideHostKeyAlgorithm(string data) {
            string[] t = data.Split(',');
            foreach (PublicKeyAlgorithm a in _param.PreferableHostKeyAlgorithms) {
                if (SSHUtil.ContainsString(t, SSH2Util.PublicKeyAlgorithmName(a))) {
                    return a;
                }
            }
            throw new SSHException("The negotiation of host key verification algorithm is failed");
        }
        private CipherAlgorithm DecideCipherAlgorithm(string data) {
            string[] t = data.Split(',');
            foreach (CipherAlgorithm a in _param.PreferableCipherAlgorithms) {
                if (SSHUtil.ContainsString(t, CipherFactory.AlgorithmToSSH2Name(a))) {
                    return a;
                }
            }
            throw new SSHException("The negotiation of encryption algorithm is failed");
        }
        private string FormatHostKeyAlgorithmDescription() {
            StringBuilder b = new StringBuilder();
            if (_param.PreferableHostKeyAlgorithms.Length == 0)
                throw new SSHException("HostKeyAlgorithm is not set");
            b.Append(SSH2Util.PublicKeyAlgorithmName(_param.PreferableHostKeyAlgorithms[0]));
            for (int i = 1; i < _param.PreferableHostKeyAlgorithms.Length; i++) {
                b.Append(',');
                b.Append(SSH2Util.PublicKeyAlgorithmName(_param.PreferableHostKeyAlgorithms[i]));
            }
            return b.ToString();
        }
        private string FormatCipherAlgorithmDescription() {
            StringBuilder b = new StringBuilder();
            if (_param.PreferableCipherAlgorithms.Length == 0)
                throw new SSHException("CipherAlgorithm is not set");
            b.Append(CipherFactory.AlgorithmToSSH2Name(_param.PreferableCipherAlgorithms[0]));
            for (int i = 1; i < _param.PreferableCipherAlgorithms.Length; i++) {
                b.Append(',');
                b.Append(CipherFactory.AlgorithmToSSH2Name(_param.PreferableCipherAlgorithms[i]));
            }
            return b.ToString();
        }

        /*
         * the seed of diffie-hellman KX defined in the spec of SSH2
         */
        private static BigInteger _dh_g1_prime = null;
        private static BigInteger _dh_g14_prime = null;
        private static BigInteger _dh_g16_prime = null;
        private static BigInteger _dh_g18_prime = null;
        private BigInteger GetDiffieHellmanPrime(KexAlgorithm algorithm) {
            switch (algorithm) {
                case KexAlgorithm.DH_G1_SHA1:
                    if (_dh_g1_prime == null) {
                        _dh_g1_prime = new BigInteger(ToBytes(
                            // RFC2409 1024-bit MODP Group 2
                            "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1" +
                            "29024E088A67CC74020BBEA63B139B22514A08798E3404DD" +
                            "EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245" +
                            "E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED" +
                            "EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE65381" +
                            "FFFFFFFFFFFFFFFF"
                            ));
                    }
                    return _dh_g1_prime;

                case KexAlgorithm.DH_G14_SHA1:
                case KexAlgorithm.DH_G14_SHA256:
                    if (_dh_g14_prime == null) {
                        _dh_g14_prime = new BigInteger(ToBytes(
                            // RFC3526 2048-bit MODP Group 14
                            "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1" +
                            "29024E088A67CC74020BBEA63B139B22514A08798E3404DD" +
                            "EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245" +
                            "E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED" +
                            "EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3D" +
                            "C2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F" +
                            "83655D23DCA3AD961C62F356208552BB9ED529077096966D" +
                            "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3B" +
                            "E39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9" +
                            "DE2BCBF6955817183995497CEA956AE515D2261898FA0510" +
                            "15728E5A8AACAA68FFFFFFFFFFFFFFFF"
                            ));
                    }
                    return _dh_g14_prime;

                case KexAlgorithm.DH_G16_SHA512:
                    if (_dh_g16_prime == null) {
                        _dh_g16_prime = new BigInteger(ToBytes(
                            // RFC3526 4096-bit MODP Group 16
                            "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1" +
                            "29024E088A67CC74020BBEA63B139B22514A08798E3404DD" +
                            "EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245" +
                            "E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED" +
                            "EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3D" +
                            "C2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F" +
                            "83655D23DCA3AD961C62F356208552BB9ED529077096966D" +
                            "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3B" +
                            "E39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9" +
                            "DE2BCBF6955817183995497CEA956AE515D2261898FA0510" +
                            "15728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64" +
                            "ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7" +
                            "ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6B" +
                            "F12FFA06D98A0864D87602733EC86A64521F2B18177B200C" +
                            "BBE117577A615D6C770988C0BAD946E208E24FA074E5AB31" +
                            "43DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D7" +
                            "88719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA" +
                            "2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6" +
                            "287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED" +
                            "1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA9" +
                            "93B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934063199" +
                            "FFFFFFFFFFFFFFFF"
                            ));
                    }
                    return _dh_g16_prime;

                case KexAlgorithm.DH_G18_SHA512:
                    if (_dh_g18_prime == null) {
                        _dh_g18_prime = new BigInteger(ToBytes(
                            // RFC3526 8192-bit MODP Group 18
                            "FFFFFFFFFFFFFFFFC90FDAA22168C234C4C6628B80DC1CD1" +
                            "29024E088A67CC74020BBEA63B139B22514A08798E3404DD" +
                            "EF9519B3CD3A431B302B0A6DF25F14374FE1356D6D51C245" +
                            "E485B576625E7EC6F44C42E9A637ED6B0BFF5CB6F406B7ED" +
                            "EE386BFB5A899FA5AE9F24117C4B1FE649286651ECE45B3D" +
                            "C2007CB8A163BF0598DA48361C55D39A69163FA8FD24CF5F" +
                            "83655D23DCA3AD961C62F356208552BB9ED529077096966D" +
                            "670C354E4ABC9804F1746C08CA18217C32905E462E36CE3B" +
                            "E39E772C180E86039B2783A2EC07A28FB5C55DF06F4C52C9" +
                            "DE2BCBF6955817183995497CEA956AE515D2261898FA0510" +
                            "15728E5A8AAAC42DAD33170D04507A33A85521ABDF1CBA64" +
                            "ECFB850458DBEF0A8AEA71575D060C7DB3970F85A6E1E4C7" +
                            "ABF5AE8CDB0933D71E8C94E04A25619DCEE3D2261AD2EE6B" +
                            "F12FFA06D98A0864D87602733EC86A64521F2B18177B200C" +
                            "BBE117577A615D6C770988C0BAD946E208E24FA074E5AB31" +
                            "43DB5BFCE0FD108E4B82D120A92108011A723C12A787E6D7" +
                            "88719A10BDBA5B2699C327186AF4E23C1A946834B6150BDA" +
                            "2583E9CA2AD44CE8DBBBC2DB04DE8EF92E8EFC141FBECAA6" +
                            "287C59474E6BC05D99B2964FA090C3A2233BA186515BE7ED" +
                            "1F612970CEE2D7AFB81BDD762170481CD0069127D5B05AA9" +
                            "93B4EA988D8FDDC186FFB7DC90A6C08F4DF435C934028492" +
                            "36C3FAB4D27C7026C1D4DCB2602646DEC9751E763DBA37BD" +
                            "F8FF9406AD9E530EE5DB382F413001AEB06A53ED9027D831" +
                            "179727B0865A8918DA3EDBEBCF9B14ED44CE6CBACED4BB1B" +
                            "DB7F1447E6CC254B332051512BD7AF426FB8F401378CD2BF" +
                            "5983CA01C64B92ECF032EA15D1721D03F482D7CE6E74FEF6" +
                            "D55E702F46980C82B5A84031900B1C9E59E7C97FBEC7E8F3" +
                            "23A97A7E36CC88BE0F1D45B7FF585AC54BD407B22B4154AA" +
                            "CC8F6D7EBF48E1D814CC5ED20F8037E0A79715EEF29BE328" +
                            "06A1D58BB7C5DA76F550AA3D8A1FBFF0EB19CCB1A313D55C" +
                            "DA56C9EC2EF29632387FE8D76E3C0468043E8F663F4860EE" +
                            "12BF2D5B0B7474D6E694F91E6DBE115974A3926F12FEE5E4" +
                            "38777CB6A932DF8CD8BEC4D073B931BA3BC832B68D9DD300" +
                            "741FA7BF8AFC47ED2576F6936BA424663AAB639C5AE4F568" +
                            "3423B4742BF1C978238F16CBE39D652DE3FDB8BEFC848AD9" +
                            "22222E04A4037C0713EB57A81A23F0C73473FC646CEA306B" +
                            "4BCBC8862F8385DDFA9D4B7FA2C087E879683303ED5BDD3A" +
                            "062B3CF5B3A278A66D2A13F83F44F82DDF310EE074AB6A36" +
                            "4597E899A0255DC164F31CC50846851DF9AB48195DED7EA1" +
                            "B1D510BD7EE74D73FAF36BC31ECFA268359046F4EB879F92" +
                            "4009438B481C6CD7889A002ED5EE382BC9190DA6FC026E47" +
                            "9558E4475677E9AA9E3050E2765694DFC81F56E880B96E71" +
                            "60C980DD98EDD3DFFFFFFFFFFFFFFFFF"
                            ));
                    }
                    return _dh_g18_prime;

                default:
                    throw new SSHException("KexAlgorithm is not set");
            }
        }

        private static byte[] ToBytes(string hexnum) {
            byte[] data = new byte[hexnum.Length / 2];
            for (int i = 0, j = 0; i < hexnum.Length; i += 2, j++) {
                data[j] = (byte)((GetHexValue(hexnum[i]) << 4) | GetHexValue(hexnum[i + 1]));
            }
            return data;
        }

        private static int GetHexValue(char c) {
            switch (c) {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    return c - '0';
                case 'a':
                case 'b':
                case 'c':
                case 'd':
                case 'e':
                case 'f':
                    return c - 'a' + 10;
                case 'A':
                case 'B':
                case 'C':
                case 'D':
                case 'E':
                case 'F':
                    return c - 'A' + 10;
                default:
                    throw new ArgumentException("invalid hex number");
            }
        }
    }

}
