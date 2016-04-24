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
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;

using Granados.PKI;
using Granados.Util;
using Granados.Crypto;
using Granados.IO;
using Granados.IO.SSH1;
using Granados.Mono.Math;

namespace Granados.SSH1 {

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public sealed class SSH1Connection : SSHConnection {

        private const int AUTH_NOT_REQUIRED = 0;
        private const int AUTH_REQUIRED = 1;

        private readonly SSH1ConnectionInfo _cInfo;
        private readonly SynchronizedPacketReceiver _packetReceiver;
        private readonly SSH1Packetizer _packetizer;
        private bool _executingShell;
        private int _shellID;
        private Cipher _cipher;

        private readonly object _transmitSync = new object();

        // exec command for SCP
        //private bool _executingExecCmd = false;

        public SSH1Connection(SSHConnectionParameter param, IGranadosSocket s, ISSHConnectionEventReceiver er, string serverVersion, string clientVersion)
            : base(param, s, er) {
            _cInfo = new SSH1ConnectionInfo(param.HostName, param.PortNumber, serverVersion, clientVersion);
            _shellID = -1;
            _packetReceiver = new SynchronizedPacketReceiver(this);
            _packetizer = new SSH1Packetizer(_packetReceiver);
        }
        internal override IDataHandler Packetizer {
            get {
                return _packetizer;
            }
        }

        internal override AuthenticationResult Connect() {

            // Phase1 receives server keys
            ReceiveServerKeys();
            if (_param.VerifySSHHostKey != null) {
                if (!_param.VerifySSHHostKey(_cInfo.GetSSHHostKeyInformationProvider())) {
                    _stream.Close();
                    return AuthenticationResult.Failure;
                }
            }

            // Phase2 generates session key
            byte[] session_key = GenerateSessionKey();

            // Phase3 establishes the session key
            InitCipher(session_key);
            SendSessionKey(session_key);
            ReceiveKeyConfirmation();

            // Phase4 user authentication
            SendUserName(_param.UserName);
            if (ReceiveAuthenticationRequirement() == AUTH_REQUIRED) {
                if (_param.AuthenticationType == AuthenticationType.Password) {
                    SendPlainPassword();
                }
                else if (_param.AuthenticationType == AuthenticationType.PublicKey) {
                    DoRSAChallengeResponse();
                }
                bool auth = ReceiveAuthenticationResult();
                if (!auth)
                    throw new SSHException(Strings.GetString("AuthenticationFailed"));

            }

            if (_authenticationResult != AuthenticationResult.Failure) {
                _packetizer.SetInnerHandler(new SSH1PacketizerPacketHandler(this));
            }
            return AuthenticationResult.Success;
        }

        internal void Transmit(SSH1Packet p) {
            lock (_transmitSync) {
                _stream.Write(p.GetImage(_cipher));
            }
        }

        private void TransmitWithoutEncryption(SSH1Packet p) {
            lock (_transmitSync) {
                _stream.Write(p.GetImage());
            }
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

        private void ReceiveServerKeys() {
            DataFragment packet = ReceivePacket();
            SSH1DataReader reader = new SSH1DataReader(packet);
            SSH1PacketType pt = (SSH1PacketType) reader.ReadByte();

            if (pt != SSH1PacketType.SSH_SMSG_PUBLIC_KEY)
                throw new SSHException("unexpected SSH SSH1Packet type " + pt, packet.GetBytes());

            _cInfo.AntiSpoofingCookie = reader.Read(8);
            _cInfo.ServerKeyBits = reader.ReadInt32();
            BigInteger serverKeyExponent = reader.ReadMPInt();
            BigInteger serverKeyModulus = reader.ReadMPInt();
            _cInfo.ServerKey = new RSAPublicKey(serverKeyExponent, serverKeyModulus);
            _cInfo.HostKeyBits = reader.ReadInt32();
            BigInteger hostKeyExponent = reader.ReadMPInt();
            BigInteger hostKeyModulus = reader.ReadMPInt();
            _cInfo.HostKey = new RSAPublicKey(hostKeyExponent, hostKeyModulus);

            //read protocol support parameters
            int protocol_flags = reader.ReadInt32();
            int supported_ciphers_mask = reader.ReadInt32();
            _cInfo.SupportedEncryptionAlgorithmsMask = supported_ciphers_mask;
            int supported_authentications_mask = reader.ReadInt32();
            //Debug.WriteLine(String.Format("ServerOptions {0} {1} {2}", protocol_flags, supported_ciphers_mask, supported_authentications_mask));

            if (reader.RemainingDataLength > 0)
                throw new SSHException("data length mismatch", packet.GetBytes());

            bool found = false;
            foreach (CipherAlgorithm a in _param.PreferableCipherAlgorithms) {
                if (a != CipherAlgorithm.Blowfish && a != CipherAlgorithm.TripleDES)
                    continue;
                else if (a == CipherAlgorithm.Blowfish && (supported_ciphers_mask & (1 << (int)CipherAlgorithm.Blowfish)) == 0)
                    continue;
                else if (a == CipherAlgorithm.TripleDES && (supported_ciphers_mask & (1 << (int)CipherAlgorithm.TripleDES)) == 0)
                    continue;

                _cInfo.IncomingPacketCipher = _cInfo.OutgoingPacketCipher = a;
                found = true;
                break;
            }

            if (!found)
                throw new SSHException(String.Format(Strings.GetString("ServerNotSupportedX"), "Blowfish/TripleDES"));

            if (_param.AuthenticationType == AuthenticationType.Password && (supported_authentications_mask & (1 << (int)AuthenticationType.Password)) == 0)
                throw new SSHException(String.Format(Strings.GetString("ServerNotSupportedPassword")), packet.GetBytes());
            if (_param.AuthenticationType == AuthenticationType.PublicKey && (supported_authentications_mask & (1 << (int)AuthenticationType.PublicKey)) == 0)
                throw new SSHException(String.Format(Strings.GetString("ServerNotSupportedRSA")), packet.GetBytes());

            TraceReceptionEvent(pt, "received server key");
        }

        private byte[] GenerateSessionKey() {
            //session key(256bits)
            byte[] session_key = new byte[32];
            RngManager.GetSecureRng().GetBytes(session_key);

            return session_key;
        }

        private void SendSessionKey(byte[] session_key) {
            try {
                //step1 XOR with session_id
                byte[] working_data = new byte[session_key.Length];
                byte[] session_id = CalcSessionID();
                Array.Copy(session_key, 0, working_data, 0, session_key.Length);
                for (int i = 0; i < session_id.Length; i++)
                    working_data[i] ^= session_id[i];

                //step2 decrypts with RSA
                RSAPublicKey first_encryption;
                RSAPublicKey second_encryption;
                RSAPublicKey serverKey = _cInfo.ServerKey;
                RSAPublicKey hostKey = _cInfo.HostKey;
                int first_key_bytelen, second_key_bytelen;
                if (serverKey.Modulus < hostKey.Modulus) {
                    first_encryption = serverKey;
                    second_encryption = hostKey;
                    first_key_bytelen = (_cInfo.ServerKeyBits + 7) / 8;
                    second_key_bytelen = (_cInfo.HostKeyBits + 7) / 8;
                }
                else {
                    first_encryption = hostKey;
                    second_encryption = serverKey;
                    first_key_bytelen = (_cInfo.HostKeyBits + 7) / 8;
                    second_key_bytelen = (_cInfo.ServerKeyBits + 7) / 8;
                }

                Rng rng = RngManager.GetSecureRng();
                BigInteger first_result = RSAUtil.PKCS1PadType2(working_data, first_key_bytelen, rng).ModPow(first_encryption.Exponent, first_encryption.Modulus);
                BigInteger second_result = RSAUtil.PKCS1PadType2(first_result.GetBytes(), second_key_bytelen, rng).ModPow(second_encryption.Exponent, second_encryption.Modulus);

                //send
                TransmitWithoutEncryption(
                    new SSH1Packet(SSH1PacketType.SSH_CMSG_SESSION_KEY)
                        .WriteByte((byte)_cInfo.OutgoingPacketCipher.Value)
                        .Write(_cInfo.AntiSpoofingCookie)
                        .WriteBigInteger(second_result)
                        .WriteInt32(0) //protocol flags
                );
                TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_SESSION_KEY, "sent encrypted session-keys");
                _sessionID = session_id;
            }
            catch (Exception e) {
                if (e is IOException)
                    throw (IOException)e;
                else {
                    string t = e.StackTrace;
                    throw new SSHException(e.Message); //IOException以外はみなSSHExceptionにしてしまう
                }
            }
        }

        private void ReceiveKeyConfirmation() {
            DataFragment packet = ReceivePacket();
            if (SneakPacketType(packet) != SSH1PacketType.SSH_SMSG_SUCCESS)
                throw new SSHException("unexpected packet type [" + SneakPacketType(packet).ToString() + "] at ReceiveKeyConfirmation()");
        }

        private int ReceiveAuthenticationRequirement() {
            DataFragment packet = ReceivePacket();
            SSH1PacketType pt = SneakPacketType(packet);
            if (pt == SSH1PacketType.SSH_SMSG_SUCCESS)
                return AUTH_NOT_REQUIRED;
            else if (pt == SSH1PacketType.SSH_SMSG_FAILURE)
                return AUTH_REQUIRED;
            else
                throw new SSHException("unexpected type " + pt);
        }

        private void SendUserName(string username) {
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_USER)
                    .WriteString(username)
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_USER, "sent user name");
        }

        private void SendPlainPassword() {
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_AUTH_PASSWORD)
                    .WriteString(_param.Password)
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_AUTH_PASSWORD, "sent password");
        }

        //RSA authentication
        private void DoRSAChallengeResponse() {
            //read key
            SSH1UserAuthKey key = new SSH1UserAuthKey(_param.IdentityFile, _param.Password);
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_AUTH_RSA)
                    .WriteBigInteger(key.PublicModulus)
            );
            TraceTransmissionEvent(SSH1PacketType.SSH_CMSG_AUTH_RSA, "RSA challenge-reponse");

            DataFragment response = ReceivePacket();
            SSH1DataReader reader = new SSH1DataReader(response);
            SSH1PacketType pt = (SSH1PacketType) reader.ReadByte();
            if (pt == SSH1PacketType.SSH_SMSG_FAILURE)
                throw new SSHException(Strings.GetString("ServerRefusedRSA"));
            else if (pt != SSH1PacketType.SSH_SMSG_AUTH_RSA_CHALLENGE)
                throw new SSHException(String.Format(Strings.GetString("UnexpectedResponse"), pt));
            TraceReceptionEvent(SSH1PacketType.SSH_SMSG_AUTH_RSA_CHALLENGE, "received challenge");

            //creating challenge
            BigInteger challenge = key.decryptChallenge(reader.ReadMPInt());
            byte[] rawchallenge = RSAUtil.StripPKCS1Pad(challenge, 2).GetBytes();

            //building response
            byte[] hash;
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider()) {
                md5.TransformBlock(rawchallenge, 0, rawchallenge.Length, rawchallenge, 0);
                md5.TransformFinalBlock(_sessionID, 0, _sessionID.Length);
                hash = md5.Hash;
            }
            Transmit(
                new SSH1Packet(SSH1PacketType.SSH_CMSG_AUTH_RSA_RESPONSE)
                    .Write(hash)
            );
            TraceReceptionEvent(SSH1PacketType.SSH_CMSG_AUTH_RSA_RESPONSE, "received response");
        }

        private bool ReceiveAuthenticationResult() {
            DataFragment packet = ReceivePacket();
            SSH1DataReader r = new SSH1DataReader(packet);
            SSH1PacketType type = (SSH1PacketType) r.ReadByte();
            TraceReceptionEvent(type, "user authentication response");
            if (type == SSH1PacketType.SSH_MSG_DEBUG) {
                //Debug.WriteLine("receivedd debug message:"+Encoding.ASCII.GetString(r.ReadString()));
                return ReceiveAuthenticationResult();
            }
            else if (type == SSH1PacketType.SSH_SMSG_SUCCESS)
                return true;
            else if (type == SSH1PacketType.SSH_SMSG_FAILURE)
                return false;
            else
                throw new SSHException("unexpected type: " + type);
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

            byte[] session_id = new MD5CryptoServiceProvider().ComputeHash(bos.ToArray());
            //System.out.println("sess-id-len=" + session_id.Length);
            return session_id;
        }

        //init ciphers
        private void InitCipher(byte[] session_key) {
            lock (_transmitSync) {
                _cipher = CipherFactory.CreateCipher(SSHProtocol.SSH1, _cInfo.OutgoingPacketCipher.Value, session_key);
                Cipher rc = CipherFactory.CreateCipher(SSHProtocol.SSH1, _cInfo.IncomingPacketCipher.Value, session_key);
                _packetizer.SetCipher(rc, _param.CheckMACError);
            }
        }

        private DataFragment ReceivePacket() {
            while (true) {
                DataFragment data = _packetReceiver.WaitResponse();

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
                        _eventReceiver.OnDebugMessage(false, r.ReadByteString());
                }
                else
                    return data;
            }
        }

        internal void AsyncReceivePacket(DataFragment data) {
            try {
                SSH1DataReader re = new SSH1DataReader(data);
                SSH1PacketType pt = (SSH1PacketType)re.ReadByte();
                switch (pt) {
                    case SSH1PacketType.SSH_SMSG_STDOUT_DATA: {
                            int len = re.ReadInt32();
                            DataFragment frag = re.GetRemainingDataView(len);
                            _channel_collection.FindChannelEntry(_shellID).Receiver.OnData(frag.Data, frag.Offset, frag.Length);
                        }
                        break;
                    case SSH1PacketType.SSH_SMSG_STDERR_DATA: {
                            _channel_collection.FindChannelEntry(_shellID).Receiver.OnExtendedData((int)SSH1PacketType.SSH_SMSG_STDERR_DATA, re.ReadByteString());
                        }
                        break;
                    case SSH1PacketType.SSH_MSG_CHANNEL_DATA: {
                            int channel = re.ReadInt32();
                            int len = re.ReadInt32();
                            DataFragment frag = re.GetRemainingDataView(len);
                            _channel_collection.FindChannelEntry(channel).Receiver.OnData(frag.Data, frag.Offset, frag.Length);
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
                        _eventReceiver.OnDebugMessage(false, re.ReadByteString());
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
                        _eventReceiver.OnUnknownMessage((byte)pt, data.GetBytes());
                        break;
                }
            }
            catch (Exception ex) {
                _eventReceiver.OnError(ex);
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
        public void OnData(byte[] data, int offset, int length) {
        }
        public void OnExtendedData(int type, byte[] data) {
        }
        public void OnChannelClosed() {
        }
        public void OnChannelEOF() {
        }
        public void OnChannelReady() {
        }
        public void OnChannelError(Exception error) {
        }
        public void OnMiscPacket(byte packet_type, byte[] data, int offset, int length) {
        }
    }
}
