/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: SSH2Connection.cs,v 1.11 2012/02/25 03:49:46 kzmi Exp $
*/
using System;
using System.Linq;
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
using System.Threading.Tasks;
using Granados.KeyboardInteractive;
using System.Collections.Generic;

namespace Granados.SSH2 {

    /// <summary>
    /// SSH2
    /// </summary>
    public class SSH2Connection : SSHConnection {
        private const int RESPONSE_TIMEOUT = 10000;

        private readonly SSH2Packetizer _packetizer;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSH2PacketInterceptorCollection _packetInterceptors;
        private readonly SSH2KeyExchanger _keyExchanger;

        //server info
        private readonly SSH2ConnectionInfo _cInfo;

        private bool _waitingForPortForwardingResponse;
        private bool _agentForwardConfirmed;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="param"></param>
        /// <param name="socket"></param>
        /// <param name="r"></param>
        /// <param name="serverVersion"></param>
        /// <param name="clientVersion"></param>
        public SSH2Connection(SSHConnectionParameter param, IGranadosSocket socket, ISSHConnectionEventReceiver r, string serverVersion, string clientVersion)
            : base(param, socket, r) {
            _cInfo = new SSH2ConnectionInfo(param.HostName, param.PortNumber, serverVersion, clientVersion);
            IDataHandler adapter = new DataHandlerAdapter(
                            (data) => {
                                AsyncReceivePacket(data);
                            },
                            () => {
                                EventReceiver.OnConnectionClosed();
                            },
                            (error) => {
                                EventReceiver.OnError(error);
                            }
                        );
            _syncHandler = new SSH2SynchronousPacketHandler(socket, adapter);
            _packetizer = new SSH2Packetizer(_syncHandler);

            _packetInterceptors = new SSH2PacketInterceptorCollection();
            _keyExchanger = new SSH2KeyExchanger(this, _syncHandler, param, _cInfo, UpdateKey);
            _packetInterceptors.Add(_keyExchanger);
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
            try {
                //key exchange
                Task kexTask = _keyExchanger.StartKeyExchange();
                kexTask.Wait();

                //user authentication
                SSH2UserAuthentication userAuthentication = new SSH2UserAuthentication(this, _param, _syncHandler, _sessionID);
                _packetInterceptors.Add(userAuthentication);
                Task authTask = userAuthentication.StartAuthentication();
                authTask.Wait();

                if (_param.AuthenticationType == AuthenticationType.KeyboardInteractive) {
                    return AuthenticationResult.Prompt;
                }
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
            _syncHandler.Send(packet);
        }

        //synchronous reception
        internal DataFragment ReceivePacket() {
            while (true) {
                DataFragment data = _syncHandler.WaitResponse(RESPONSE_TIMEOUT);
                if (data == null) {
                    continue;
                }

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
                    string msg = r.ReadUTF8String();
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

        private void ProcessPacket(DataFragment packet) {
            if (_packetInterceptors.InterceptPacket(packet)) {
                return;
            }

            if (packet.Length < 1) {
                return; // invalid packet
            }

            SSH2DataReader r = new SSH2DataReader(packet);
            SSH2PacketType pt = (SSH2PacketType) r.ReadByte();

            if (pt == SSH2PacketType.SSH_MSG_DISCONNECT) {
                int errorcode = r.ReadInt32();
                _eventReceiver.OnConnectionClosed();
                return;
            }

            if (_waitingForPortForwardingResponse) {
                if (pt != SSH2PacketType.SSH_MSG_REQUEST_SUCCESS)
                    _eventReceiver.OnUnknownMessage((byte)pt, packet.GetBytes());
                _waitingForPortForwardingResponse = false;
                return;
            }

            if (pt == SSH2PacketType.SSH_MSG_CHANNEL_OPEN) {
                string method = r.ReadString();
                if (method == "forwarded-tcpip")
                    ProcessPortforwardingRequest(_eventReceiver, r);
                else if (method.StartsWith("auth-agent")) //in most cases, method is "auth-agent@openssh.com"
                    ProcessAgentForwardRequest(_eventReceiver, r);
                else {
                    Transmit(
                        new SSH2Packet(SSH2PacketType.SSH_MSG_CHANNEL_OPEN_FAILURE)
                            .WriteInt32(r.ReadInt32())
                            .WriteInt32(3)  // SSH_OPEN_UNKNOWN_CHANNEL_TYPE
                            .WriteUTF8String("unknown channel type")
                            .WriteString("") //lang tag
                    );
                    TraceReceptionEvent("SSH_MSG_CHANNEL_OPEN rejected", "method={0}", method);
                }
                return;
            }

            if (pt >= SSH2PacketType.SSH_MSG_CHANNEL_OPEN_CONFIRMATION && pt <= SSH2PacketType.SSH_MSG_CHANNEL_FAILURE) {
                int local_channel = r.ReadInt32();
                ChannelCollection.Entry e = this.ChannelCollection.FindChannelEntry(local_channel);
                if (e != null)
                    ((SSH2Channel)e.Channel).ProcessPacket(e.Receiver, pt, r);
                else
                    Debug.WriteLine("unexpected channel pt=" + pt + " local_channel=" + local_channel.ToString());
                return;
            }

            if (pt == SSH2PacketType.SSH_MSG_IGNORE) {
                _eventReceiver.OnIgnoreMessage(r.ReadByteString());
                return;
            }

            _eventReceiver.OnUnknownMessage((byte)pt, packet.GetBytes());
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

        private void UpdateKey(byte[] sessionID, Cipher cipherServer, Cipher cipherClient, MAC macServer, MAC macClient) {
            _sessionID = sessionID;
            _syncHandler.SetCipher(cipherServer, macServer);
            _packetizer.SetCipher(cipherClient, _param.CheckMACError ? macClient : null);
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
                        receiver.OnData(frag);
                    }
                    break;
                case SSH2PacketType.SSH_MSG_CHANNEL_EXTENDED_DATA: {
                        uint type = re.ReadUInt32();
                        int len = re.ReadInt32();
                        DataFragment frag = re.GetRemainingDataView(len);
                        receiver.OnExtendedData(type, frag);
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
                        receiver.OnMiscPacket((byte)pt, frag);
                    }
                    break;
                default: {
                        DataFragment frag = re.GetRemainingDataView();
                        receiver.OnMiscPacket((byte)pt, frag);
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

    /// <summary>
    /// Synchronization of sending/receiving packets.
    /// </summary>
    internal class SSH2SynchronousPacketHandler : AbstractSynchronousPacketHandler<SSH2Packet> {

        private readonly object _cipherSync = new object();
        private uint _sequenceNumber = 0;
        private Cipher _cipher = null;
        private MAC _mac = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket object for sending packets.</param>
        /// <param name="handler">the next handler received packets are redirected to.</param>
        public SSH2SynchronousPacketHandler(IGranadosSocket socket, IDataHandler handler)
            : base(socket, handler) {
        }

        /// <summary>
        /// Set cipher settings.
        /// </summary>
        /// <param name="cipher">cipher to encrypt a packet to be sent.</param>
        /// <param name="mac">MAC for a packet to be sent.</param>
        public void SetCipher(Cipher cipher, MAC mac) {
            lock (_cipherSync) {
                _cipher = cipher;
                _mac = mac;
            }
        }

        /// <summary>
        /// Gets the binary image of the packet to be sent.
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>binary image of the packet</returns>
        protected override DataFragment GetPacketImage(SSH2Packet packet) {
            lock (_cipherSync) {
                return packet.GetImage(_cipher, _mac, _sequenceNumber++);
            }
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
                return ((SSH2PacketType) packet.Data[packet.Offset]).ToString();
            }
            else {
                return "?";
            }
        }
    }

    /// <summary>
    /// Return value of the <see cref="ISSH2PacketInterceptor.InterceptPacket(DataFragment)"/>.
    /// </summary>
    internal enum SSH2PacketInterceptorResult {
        /// <summary>the packet was not consumed</summary>
        PassThrough,
        /// <summary>the packet was consumed</summary>
        Consumed,
        /// <summary>the packet was not consumed. the interceptor has already finished.</summary>
        Finished,
    }

    /// <summary>
    /// An interface of a class that can intercept a received packet.
    /// </summary>
    internal interface ISSH2PacketInterceptor {
        SSH2PacketInterceptorResult InterceptPacket(DataFragment packet);
    }

    /// <summary>
    /// Collection of the <see cref="ISSH2PacketInterceptor"/>.
    /// </summary>
    internal class SSH2PacketInterceptorCollection {

        private readonly LinkedList<ISSH2PacketInterceptor> _interceptors = new LinkedList<ISSH2PacketInterceptor>();

        /// <summary>
        /// Add packet interceptor to the collection.
        /// </summary>
        /// <remarks>
        /// Do nothing if the packet interceptor already exists in the collection.
        /// </remarks>
        /// <param name="interceptor">a packet interceptor</param>
        public void Add(ISSH2PacketInterceptor interceptor) {
            lock (_interceptors) {
                if (_interceptors.All(i => i.GetType() != interceptor.GetType())) {
                    _interceptors.AddLast(interceptor);
                    Debug.WriteLine("PacketInterceptor: Add {0}", interceptor.GetType());
                }
            }
        }

        /// <summary>
        /// Feed packet to the packet interceptors.
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>true if the packet was consumed.</returns>
        public bool InterceptPacket(DataFragment packet) {
            lock (_interceptors) {
                var node = _interceptors.First;
                while (node != null) {
                    var result = node.Value.InterceptPacket(packet);
                    if (result == SSH2PacketInterceptorResult.Consumed) {
                        return true;
                    }
                    if (result == SSH2PacketInterceptorResult.Finished) {
                        var nodeToRemove = node;
                        node = node.Next;
                        _interceptors.Remove(nodeToRemove);
                        Debug.WriteLine("PacketInterceptor: Del {0}", nodeToRemove.Value.GetType());
                    }
                    else {
                        node = node.Next;
                    }
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Class for supporting key exchange sequence.
    /// </summary>
    internal class SSH2KeyExchanger : ISSH2PacketInterceptor {
        #region SSH2KeyExchanger

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

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

        private class KexState {
            // payload of KEX_INIT message
            public byte[] serverKEXINITPayload;
            public byte[] clientKEXINITPayload;

            // values for Diffie-Hellman
            public BigInteger p;   // prime number
            public BigInteger x;   // random number
            public BigInteger e;   // g^x mod p
            public BigInteger k;   // f^x mod p
            public byte[] hash;
        }

        private class CipherSettings {
            public Cipher cipherServer;
            public Cipher cipherClient;
            public MAC macServer;
            public MAC macClient;
        }

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

        public delegate void UpdateKeyDelegate(byte[] sessionID, Cipher cipherServer, Cipher cipherClient, MAC macServer, MAC macClient);

        private readonly UpdateKeyDelegate _updateKey;

        private readonly SSH2Connection _connection;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
        private readonly SSHConnectionParameter _param;
        private readonly SSH2ConnectionInfo _cInfo;

        private byte[] _sessionID;

        private readonly object _sequenceLock = new object();
        private volatile SequenceStatus _sequenceStatus = SequenceStatus.Idle;

        private readonly AtomicBox<DataFragment> _receivedPacket = new AtomicBox<DataFragment>();

        private Task _kexTask;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="syncHandler"></param>
        /// <param name="info"></param>
        /// <param name="updateKey"></param>
        public SSH2KeyExchanger(
                    SSH2Connection connection,
                    SSH2SynchronousPacketHandler syncHandler,
                    SSHConnectionParameter param,
                    SSH2ConnectionInfo info,
                    UpdateKeyDelegate updateKey) {
            _connection = connection;
            _syncHandler = syncHandler;
            _param = param;
            _cInfo = info;
            _updateKey = updateKey;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSH2PacketInterceptorResult InterceptPacket(DataFragment packet) {
            SSH2PacketType packetType = (SSH2PacketType)packet[0];
            lock (_sequenceLock) {
                switch (_sequenceStatus) {
                    case SequenceStatus.Idle:
                        if (packetType == SSH2PacketType.SSH_MSG_KEXINIT) {
                            _sequenceStatus = SequenceStatus.InitiatedByServer;
                            _kexTask = StartKeyExchangeAsync(packet);
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.InitiatedByServer:
                        break;
                    case SequenceStatus.InitiatedByClient:
                        if (packetType == SSH2PacketType.SSH_MSG_KEXINIT) {
                            _sequenceStatus = SequenceStatus.KexInitReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.KexInitReceived:
                        break;
                    case SequenceStatus.WaitKexDHReplay:
                        if (packetType == SSH2PacketType.SSH_MSG_KEXDH_REPLY) {
                            _sequenceStatus = SequenceStatus.WaitNewKeys;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
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
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        break;
                    default:
                        break;
                }
                return SSH2PacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnClosed() {
            lock (_sequenceLock) {
                this._sequenceStatus = SequenceStatus.ConnectionClosed;
                DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
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
                _sequenceStatus = SequenceStatus.InitiatedByClient;
                _kexTask = StartKeyExchangeAsync(null);
                return _kexTask;
            }
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
                KexState state = new KexState();

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

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_KEXINIT, traceMsg);
            }

            if (kexinitFromServer != null) {
                // if the key exchange was initiated by the server,
                // no need to wait for the SSH_MSG_KEXINIT response.
                _syncHandler.Send(packetToSend);
                return;
            }

            // send KEXINIT
            _syncHandler.Send(packetToSend);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                    throw new SSHException(Strings.GetString("ConnectionClosed"));
                }
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
                Debug.Assert(_sequenceStatus == SequenceStatus.KexInitReceived || _sequenceStatus == SequenceStatus.InitiatedByServer);
            }

            SSH2Packet packetToSend = BuildKEXDHINITPacket(state);

            lock (_sequenceLock) {
                _sequenceStatus = SequenceStatus.WaitKexDHReplay;
            }

            // send KEXDH_INIT
            _syncHandler.Send(packetToSend);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                    throw new SSHException(Strings.GetString("ConnectionClosed"));
                }
                Debug.Assert(_sequenceStatus == SequenceStatus.WaitNewKeys || _sequenceStatus == SequenceStatus.WaitUpdateCipher);    // already set in FeedReceivedPacket
            }

            bool isAccepted = ProcessKEXDHREPLY(response, state);

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_KEXDH_REPLY,
                    isAccepted ? "host key has been accepted" : "host key has been denied");
            }

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
                Debug.Assert(_sequenceStatus == SequenceStatus.WaitNewKeys || _sequenceStatus == SequenceStatus.WaitUpdateCipher);
            }

            // make NEWKEYS packet
            SSH2Packet packetToSend = BuildNEWKEYSPacket();

            // send KEXDH_INIT
            _syncHandler.Send(packetToSend);

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                    throw new SSHException(Strings.GetString("ConnectionClosed"));
                }
            }

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_NEWKEYS, "the keys are updated");
            }
        }

        /// <summary>
        /// Build a SSH_MSG_KEXINIT packet.
        /// </summary>
        /// <param name="traceMessage">trace message will be set</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildKEXINITPacket(out string traceMessage) {
            const string MAC_ALGORITHM = "hmac-sha1";

            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXINIT)
                    .WriteSecureRandomBytes(16) // cookie
                    .WriteString(GetSupportedKexAlgorithms()) // kex_algorithms
                    .WriteString(FormatHostKeyAlgorithmDescription()) // server_host_key_algorithms
                    .WriteString(FormatCipherAlgorithmDescription()) // encryption_algorithms_client_to_server
                    .WriteString(FormatCipherAlgorithmDescription()) // encryption_algorithms_server_to_client
                    .WriteString(MAC_ALGORITHM) // mac_algorithms_client_to_server
                    .WriteString(MAC_ALGORITHM) // mac_algorithms_server_to_client
                    .WriteString("none") // compression_algorithms_client_to_server
                    .WriteString("none") // compression_algorithms_server_to_client
                    .WriteString("") // languages_client_to_server
                    .WriteString("") // languages_server_to_client
                    .WriteBool(false) // indicates whether a guessed key exchange packet follows
                    .WriteInt32(0); // reserved for future extension

            traceMessage = new StringBuilder()
                .Append("kex_algorithm=")
                .Append(GetSupportedKexAlgorithms())
                .Append("; server_host_key_algorithms=")
                .Append(FormatHostKeyAlgorithmDescription())
                .Append("; encryption_algorithms_client_to_server=")
                .Append(FormatCipherAlgorithmDescription())
                .Append("; encryption_algorithms_server_to_client=")
                .Append(FormatCipherAlgorithmDescription())
                .Append("; mac_algorithms_client_to_server=")
                .Append(MAC_ALGORITHM)
                .Append("; mac_algorithms_server_to_client=")
                .Append(MAC_ALGORITHM)
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
            _cInfo.HostKeyAlgorithm = DecideHostKeyAlgorithm(host_key);

            string enc_cs = reader.ReadString();
            _cInfo.SupportedEncryptionAlgorithmsClientToServer = enc_cs;
            _cInfo.OutgoingPacketCipher = DecideCipherAlgorithm(enc_cs);

            string enc_sc = reader.ReadString();
            _cInfo.SupportedEncryptionAlgorithmsServerToClient = enc_sc;
            _cInfo.IncomingPacketCipher = DecideCipherAlgorithm(enc_sc);

            string mac_cs = reader.ReadString();
            CheckAlgorithmSupport("mac", mac_cs, "hmac-sha1");

            string mac_sc = reader.ReadString();
            CheckAlgorithmSupport("mac", mac_sc, "hmac-sha1");

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

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_KEXINIT, traceMessage);
            }
        }

        /// <summary>
        /// Builds a SSH_MSG_KEXDH_INIT packet.
        /// </summary>
        /// <param name="state">informations about current key exchange</param>
        /// <returns>a packet object</returns>
        private SSH2Packet BuildKEXDHINITPacket(KexState state) {
            //Round1 computes and sends [e]
            state.p = GetDiffieHellmanPrime(_cInfo.KEXAlgorithm.Value);
            //Generate x : 1 < x < (p-1)/2
            int xBytes = (state.p.BitCount() - 2) / 8;
            BigInteger x;
            Rng rng = RngManager.GetSecureRng();
            do {
                byte[] sx = new byte[xBytes];
                rng.GetBytes(sx);
                x = new BigInteger(sx);
            } while (x <= 1);
            state.x = x;
            state.e = new BigInteger(2).ModPow(x, state.p);

            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_KEXDH_INIT)
                    .WriteBigInteger(state.e);

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
            SSH2DataWriter wr = new SSH2DataWriter();
            state.k = f.ModPow(state.x, state.p);
            wr = new SSH2DataWriter();
            wr.WriteString(_cInfo.ClientVersionString);
            wr.WriteString(_cInfo.ServerVersionString);
            wr.WriteAsString(state.clientKEXINITPayload);
            wr.WriteAsString(state.serverKEXINITPayload);
            wr.WriteAsString(key_and_cert);
            wr.WriteBigInteger(state.e);
            wr.WriteBigInteger(f);
            wr.WriteBigInteger(state.k);
            state.hash = KexComputeHash(wr.ToByteArray());

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceReceptionEvent(packetType, "verifying host key");
            }

            bool verifyExternally = (_sessionID == null) ? true : false;
            bool accepted = VerifyHostKey(key_and_cert, signature, state.hash, verifyExternally);

            if (accepted && _sessionID == null) {
                //Debug.WriteLine("hash="+DebugUtil.DumpByteArray(hash));
                _sessionID = state.hash;
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
                    DeriveKey(state.k, state.hash, 'C', CipherFactory.GetKeySize(_cInfo.OutgoingPacketCipher.Value)),
                    DeriveKey(state.k, state.hash, 'A', CipherFactory.GetBlockSize(_cInfo.OutgoingPacketCipher.Value))
                );
            settings.cipherClient =
                CipherFactory.CreateCipher(
                    SSHProtocol.SSH2,
                    _cInfo.IncomingPacketCipher.Value,
                    DeriveKey(state.k, state.hash, 'D', CipherFactory.GetKeySize(_cInfo.IncomingPacketCipher.Value)),
                    DeriveKey(state.k, state.hash, 'B', CipherFactory.GetBlockSize(_cInfo.IncomingPacketCipher.Value))
                );

            MACAlgorithm ma = MACAlgorithm.HMACSHA1;
            settings.macServer = MACFactory.CreateMAC(MACAlgorithm.HMACSHA1, DeriveKey(state.k, state.hash, 'E', MACFactory.GetSize(ma)));
            settings.macClient = MACFactory.CreateMAC(MACAlgorithm.HMACSHA1, DeriveKey(state.k, state.hash, 'F', MACFactory.GetSize(ma)));

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
            if (algorithm != SSH2Util.PublicKeyAlgorithmName(_cInfo.HostKeyAlgorithm.Value)) {
                throw new SSHException(Strings.GetString("HostKeyAlgorithmMismatch"));
            }

            SSH2DataReader sigReader = new SSH2DataReader(signature);
            string sigAlgorithm = sigReader.ReadString();
            if (sigAlgorithm != algorithm) {
                throw new SSHException(Strings.GetString("HostKeyAlgorithmMismatch"));
            }
            byte[] signatureBlob = sigReader.ReadByteString();

            if (_cInfo.HostKeyAlgorithm == PublicKeyAlgorithm.RSA) {
                RSAPublicKey pk = ReadRSAPublicKey(ksReader);
                pk.VerifyWithSHA1(signatureBlob, new SHA1CryptoServiceProvider().ComputeHash(hash));
                _cInfo.HostKey = pk;
            }
            else if (_cInfo.HostKeyAlgorithm == PublicKeyAlgorithm.DSA) {
                DSAPublicKey pk = ReadDSAPublicKey(ksReader);
                pk.Verify(signatureBlob, new SHA1CryptoServiceProvider().ComputeHash(hash));
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
        /// Reads RSA public key informations.
        /// </summary>
        /// <param name="reader">packet reader</param>
        /// <returns>public key object</returns>
        private RSAPublicKey ReadRSAPublicKey(SSH2DataReader reader) {
            BigInteger exp = reader.ReadMPInt();
            BigInteger mod = reader.ReadMPInt();
            return new RSAPublicKey(exp, mod);
        }

        /// <summary>
        /// Reads DSA public key informations.
        /// </summary>
        /// <param name="reader">packet reader</param>
        /// <returns>public key object</returns>
        private DSAPublicKey ReadDSAPublicKey(SSH2DataReader reader) {
            BigInteger p = reader.ReadMPInt();
            BigInteger q = reader.ReadMPInt();
            BigInteger g = reader.ReadMPInt();
            BigInteger y = reader.ReadMPInt();
            return new DSAPublicKey(p, g, q, y);
        }

        /// <summary>
        /// Creates a key from K and H.
        /// </summary>
        /// <param name="k">a shared secret K</param>
        /// <param name="h">an exchange hash H</param>
        /// <param name="letter">letter ('A', 'B',...)</param>
        /// <param name="length">key length</param>
        /// <returns></returns>
        private byte[] DeriveKey(BigInteger k, byte[] h, char letter, int length) {
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
                byte[] hash = KexComputeHash(image.GetBytes());

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
        /// Computes hash according to the current key exchange algorithm.
        /// </summary>
        /// <param name="b">source bytes</param>
        /// <returns>hash value</returns>
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
        /// <param name="candidates">candidate algorithms</param>
        /// <returns>key exchange algorithm to use</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private KexAlgorithm DecideKexAlgorithm(string candidates) {
            string[] candidateNames = candidates.Split(',');
            foreach (string candidateName in candidateNames) {
                foreach (SupportedKexAlgorithm algorithm in supportedKexAlgorithms) {
                    if (algorithm.name == candidateName) {
                        return algorithm.value;
                    }
                }
            }
            throw new SSHException(Strings.GetString("KeyExchangeAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Decides host key algorithm to use.
        /// </summary>
        /// <param name="candidates">candidate algorithms</param>
        /// <returns>host key algorithm to use</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private PublicKeyAlgorithm DecideHostKeyAlgorithm(string candidates) {
            string[] candidateNames = candidates.Split(',');
            foreach (PublicKeyAlgorithm pref in _param.PreferableHostKeyAlgorithms) {
                string prefName = SSH2Util.PublicKeyAlgorithmName(pref);
                if (candidateNames.Contains(prefName)) {
                    return pref;
                }
            }
            throw new SSHException(Strings.GetString("HostKeyAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Decides cipher algorithm to use.
        /// </summary>
        /// <param name="candidates">candidate algorithms</param>
        /// <returns>cipher algorithm to use</returns>
        /// <exception cref="SSHException">no suitable algorithm was found</exception>
        private CipherAlgorithm DecideCipherAlgorithm(string candidates) {
            string[] candidateNames = candidates.Split(',');
            foreach (CipherAlgorithm pref in _param.PreferableCipherAlgorithms) {
                string prefName = CipherFactory.AlgorithmToSSH2Name(pref);
                if (candidateNames.Contains(prefName)) {
                    return pref;
                }
            }
            throw new SSHException(Strings.GetString("EncryptionAlgorithmNegotiationFailed"));
        }

        /// <summary>
        /// Makes kex_algorithms field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string GetSupportedKexAlgorithms() {
            return string.Join(",",
                    supportedKexAlgorithms
                        .Select(algorithm => algorithm.name));
        }

        /// <summary>
        /// Makes server_host_key_algorithms field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string FormatHostKeyAlgorithmDescription() {
            if (_param.PreferableHostKeyAlgorithms.Length == 0) {
                throw new SSHException("HostKeyAlgorithm is not set");
            }
            return string.Join(",",
                    _param.PreferableHostKeyAlgorithms
                        .Select(algorithm => SSH2Util.PublicKeyAlgorithmName(algorithm)));
        }

        /// <summary>
        /// Makes encryption_algorithms_client_to_server field for the SSH_MSG_KEXINIT
        /// </summary>
        /// <returns>name list</returns>
        private string FormatCipherAlgorithmDescription() {
            if (_param.PreferableCipherAlgorithms.Length == 0) {
                throw new SSHException("CipherAlgorithm is not set");
            }
            return string.Join(",",
                    _param.PreferableCipherAlgorithms
                        .Select(algorithm => CipherFactory.AlgorithmToSSH2Name(algorithm)));
        }

        private static BigInteger _dh_g1_prime = null;
        private static BigInteger _dh_g14_prime = null;
        private static BigInteger _dh_g16_prime = null;
        private static BigInteger _dh_g18_prime = null;

        /// <summary>
        /// Gets a prime number for the Diffie-Hellman key exchange.
        /// </summary>
        /// <param name="algorithm">key exchange algorithm</param>
        /// <returns>a prime number</returns>
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

        #endregion  // SSH2KeyExchanger
    }

    /// <summary>
    /// Class for supporting user authentication
    /// </summary>
    internal class SSH2UserAuthentication : ISSH2PacketInterceptor {
        #region SSH2UserAuthentication

        private const int PASSING_TIMEOUT = 1000;
        private const int RESPONSE_TIMEOUT = 5000;

        private readonly IKeyboardInteractiveAuthenticationHandler _kiHandler;

        private readonly SSHConnectionParameter _param;
        private readonly SSH2Connection _connection;
        private readonly SSH2SynchronousPacketHandler _syncHandler;
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
        /// <param name="connection"></param>
        /// <param name="param"></param>
        /// <param name="syncHandler"></param>
        /// <param name="sessionID"></param>
        public SSH2UserAuthentication(
                    SSH2Connection connection,
                    SSHConnectionParameter param,
                    SSH2SynchronousPacketHandler syncHandler,
                    byte[] sessionID) {
            _connection = connection;
            _param = param;
            _syncHandler = syncHandler;
            _sessionID = sessionID;
            _kiHandler = param.KeyboardInteractiveAuthenticationHandler;
        }

        /// <summary>
        /// Intercept a received packet.
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>result</returns>
        public SSH2PacketInterceptorResult InterceptPacket(DataFragment packet) {
            if (_sequenceStatus == SequenceStatus.Done) {   // fast check
                return SSH2PacketInterceptorResult.Finished;
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
                            return SSH2PacketInterceptorResult.Consumed;
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
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_SUCCESS) {
                            _sequenceStatus = SequenceStatus.KI_SuccessReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_FAILURE) {
                            _sequenceStatus = SequenceStatus.KI_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_BANNER) {
                            _sequenceStatus = SequenceStatus.KI_BannerReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
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
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_FAILURE) {
                            _sequenceStatus = SequenceStatus.PA_FailureReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        if (packetType == SSH2PacketType.SSH_MSG_USERAUTH_BANNER) {
                            _sequenceStatus = SequenceStatus.PA_BannerReceived;
                            _receivedPacket.TrySet(packet, PASSING_TIMEOUT);
                            return SSH2PacketInterceptorResult.Consumed;
                        }
                        break;
                    case SequenceStatus.PA_SuccessReceived:
                    case SequenceStatus.PA_FailureReceived:
                        break;

                    default:
                        break;
                }
                return SSH2PacketInterceptorResult.PassThrough;
            }
        }

        /// <summary>
        /// Handles connection close.
        /// </summary>
        public void OnClosed() {
            lock (_sequenceLock) {
                this._sequenceStatus = SequenceStatus.ConnectionClosed;
                DataFragment dummyPacket = new DataFragment(new byte[1] { 0xff }, 0, 1);
                _receivedPacket.TrySet(dummyPacket, PASSING_TIMEOUT);
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
                _receivedPacket.Clear();

                if (!keepSequenceStatusOnExit) {
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
            }

            var packet = BuildServiceRequestPacket(serviceName);

            lock (_sequenceLock) {
                _sequenceStatus = SequenceStatus.WaitServiceAccept;
            }

            _syncHandler.Send(packet);

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_SERVICE_REQUEST, serviceName);
            }

            DataFragment response = null;
            if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                throw new SSHException(Strings.GetString("ServerDoesntRespond"));
            }

            lock (_sequenceLock) {
                if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                    throw new SSHException(Strings.GetString("ConnectionClosed"));
                }
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }

            SSH2DataReader reader = new SSH2DataReader(response);
            SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();
            Debug.Assert(packetType == SSH2PacketType.SSH_MSG_SERVICE_ACCEPT);

            string responseServiceName = reader.ReadString();

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceReceptionEvent(SSH2PacketType.SSH_MSG_SERVICE_ACCEPT, responseServiceName);
            }

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
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }

            // check handler
            if (_kiHandler == null) {
                throw new SSHException("KeyboardInteractiveAuthenticationHandler is not set.");
            }

            var packet = BuildKeyboardInteractiveUserAuthRequestPacket(serviceName);

            lock (_sequenceLock) {
                _sequenceStatus = SequenceStatus.KI_WaitUserAuthInfoRequest;
            }

            _syncHandler.Send(packet);

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST, "starting keyboard-interactive authentication");
            }

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

            if (userAuthResult == false) {
                _connection.Close();
            }

            // notify
            _kiHandler.OnKeyboardInteractiveAuthenticationCompleted(userAuthResult, error);
        }

        /// <summary>
        /// Prompt lines, user input loop.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void DoKeyboardInteractiveUserAuthInput(string serviceName) {
            while (true) {
                DataFragment response = null;
                if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                    throw new SSHException(Strings.GetString("ServerDoesntRespond"));
                }

                SSH2DataReader reader = new SSH2DataReader(response);
                SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

                lock (_sequenceLock) {
                    if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                        throw new SSHException(Strings.GetString("ConnectionClosed"));
                    }

                    Debug.Assert(_sequenceStatus == SequenceStatus.KI_UserAuthInfoRequestReceived
                        || _sequenceStatus == SequenceStatus.KI_FailureReceived
                        || _sequenceStatus == SequenceStatus.KI_SuccessReceived
                        || _sequenceStatus == SequenceStatus.KI_BannerReceived);

                    if (_sequenceStatus == SequenceStatus.KI_SuccessReceived) {
                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceReceptionEvent(packetType, "user authentication succeeded");
                        }
                        return;
                    }

                    if (_sequenceStatus == SequenceStatus.KI_FailureReceived) {
                        string msg = reader.ReadString();
                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceReceptionEvent(packetType, "user authentication failed: " + msg);
                        }
                        throw new SSHException(Strings.GetString("AuthenticationFailed"));
                    }

                    if (_sequenceStatus == SequenceStatus.KI_BannerReceived) {
                        string msg = reader.ReadUTF8String();
                        string langtag = reader.ReadString();
                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceReceptionEvent(packetType, "banner: " + msg);
                        }
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

                var infoResponsePacket = BuildKeyboardInteractiveUserAuthInfoResponsePacket(serviceName, inputs);

                lock (_sequenceLock) {
                    _sequenceStatus = SequenceStatus.KI_WaitUserAuthInfoRequest;
                }

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
            string algorithmName = SSH2Util.PublicKeyAlgorithmName(kp.Algorithm);

            // construct a packet except signature
            SSH2Packet packet =
                new SSH2Packet(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST)
                    .WriteUTF8String(_param.UserName)
                    .WriteString(serviceName)
                    .WriteString("publickey")
                    .WriteBool(true)    // has signature
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

            return packet;
        }

        /// <summary>
        /// Password authentication sequence.
        /// </summary>
        /// <param name="serviceName">service name</param>
        private void PasswordAuthentication(string serviceName) {
            lock (_sequenceLock) {
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
                Debug.Assert(_sequenceStatus == SequenceStatus.ServiceAcceptReceived);
            }

            lock (_sequenceLock) {
                _sequenceStatus = SequenceStatus.PA_WaitUserAuthResponse;
            }

            _syncHandler.Send(packet);

            if (_connection.IsEventTracerAvailable) {
                _connection.TraceTransmissionEvent(SSH2PacketType.SSH_MSG_USERAUTH_REQUEST, traceMessage);
            }

            while (true) {
                DataFragment response = null;
                if (!_receivedPacket.TryGet(ref response, RESPONSE_TIMEOUT)) {
                    throw new SSHException(Strings.GetString("ServerDoesntRespond"));
                }

                lock (_sequenceLock) {
                    if (_sequenceStatus == SequenceStatus.ConnectionClosed) {
                        throw new SSHException(Strings.GetString("ConnectionClosed"));
                    }

                    SSH2DataReader reader = new SSH2DataReader(response);
                    SSH2PacketType packetType = (SSH2PacketType)reader.ReadByte();

                    Debug.Assert(_sequenceStatus == SequenceStatus.PA_FailureReceived
                        || _sequenceStatus == SequenceStatus.PA_SuccessReceived
                        || _sequenceStatus == SequenceStatus.PA_BannerReceived);

                    if (_sequenceStatus == SequenceStatus.PA_SuccessReceived) {
                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceReceptionEvent(packetType, "user authentication succeeded");
                        }
                        return;
                    }

                    if (_sequenceStatus == SequenceStatus.PA_FailureReceived) {
                        string msg = reader.ReadString();
                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceReceptionEvent(packetType, "user authentication failed: " + msg);
                        }
                        throw new SSHException(Strings.GetString("AuthenticationFailed"));
                    }

                    if (_sequenceStatus == SequenceStatus.PA_BannerReceived) {
                        string msg = reader.ReadUTF8String();
                        string langtag = reader.ReadString();
                        if (_connection.IsEventTracerAvailable) {
                            _connection.TraceReceptionEvent(packetType, "banner: " + msg);
                        }
                        _sequenceStatus = SequenceStatus.PA_WaitUserAuthResponse;
                        continue;   // wait for the next response packet
                    }
                }
            }
        }

        #endregion  // SSH2UserAuthentication
    }

}
