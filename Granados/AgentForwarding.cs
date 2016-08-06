using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Granados.IO;
using Granados.IO.SSH2;
using Granados.SSH2;
using Granados.Util;
using Granados.SSH1;
using Granados.SSH;
using Granados.IO.SSH1;
using Granados.Mono.Math;
using Granados.PKI;
using System.Security.Cryptography;

namespace Granados.AgentForwarding {

    /// <summary>
    /// An interface for handling authentication agent forwarding.
    /// </summary>
    public interface IAgentForwardingAuthKeyProvider {
        /// <summary>
        /// A property that indicates whether this provider is active.
        /// </summary>
        /// <remarks>
        /// This property will be checked on each time a request message from the server has been received.
        /// </remarks>
        bool IsAuthKeyProviderEnabled {
            get;
        }

        /// <summary>
        /// Returns SSH1 authentication keys that are available for the authentication.
        /// </summary>
        /// <returns>SSH1 authentication keys</returns>
        SSH1UserAuthKey[] GetAvailableSSH1UserAuthKeys();

        /// <summary>
        /// Returns SSH2 authentication keys that are available for the authentication.
        /// </summary>
        /// <returns>SSH2 authentication keys</returns>
        SSH2UserAuthKey[] GetAvailableSSH2UserAuthKeys();
    }

    /// <summary>
    /// SSH1 agent forwarding message types (OpenSSH's protocol)
    /// </summary>
    internal enum SSH1AgentForwardingMessageType {
        // from client
        SSH_AGENTC_REQUEST_RSA_IDENTITIES = 1,
        SSH_AGENTC_RSA_CHALLENGE = 3,

        // from agent
        SSH_AGENT_RSA_IDENTITIES_ANSWER = 2,
        SSH_AGENT_RSA_RESPONSE = 4,
        SSH_AGENT_FAILURE = 5,
        SSH_AGENT_SUCCESS = 6,
    }

    /// <summary>
    /// SSH2 agent forwarding message types (OpenSSH's protocol)
    /// </summary>
    internal enum SSH2AgentForwardingMessageType {
        // from client
        SSH2_AGENTC_REQUEST_IDENTITIES = 11,
        SSH2_AGENTC_SIGN_REQUEST = 13,

        // from agent
        SSH2_AGENT_IDENTITIES_ANSWER = 12,
        SSH2_AGENT_SIGN_RESPONSE = 14,
        SSH_AGENT_FAILURE = 5,
        SSH_AGENT_SUCCESS = 6,
    }

    /// <summary>
    /// Message builder for the agent forwarding response.
    /// </summary>
    internal abstract class AgentForwardingMessageBase : IPacketBuilder {

        private readonly ByteBuffer _payload = new ByteBuffer(16 * 1024, -1);

        #region IPacketBuilder

        /// <summary>
        /// 
        /// </summary>
        public ByteBuffer Payload {
            get {
                return _payload;
            }
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageType">message type</param>
        protected AgentForwardingMessageBase(byte messageType) {
            _payload.WriteUInt32(0);    // message length (set later)
            _payload.WriteByte((byte)messageType);
        }

        /// <summary>
        /// Gets entire binary image of the message.
        /// </summary>
        /// <returns>binary image of the message</returns>
        public DataFragment GetImage() {
            int messageLength = _payload.Length - 4;
            _payload.OverwriteUInt32(0, (uint)messageLength);
            return _payload.AsDataFragment();
        }
    }

    internal class SSH1AgentForwardingMessage : AgentForwardingMessageBase, ISSH1PacketBuilder {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageType">message type</param>
        public SSH1AgentForwardingMessage(SSH1AgentForwardingMessageType messageType)
            : base((byte)messageType) {
        }
    }

    internal class SSH2AgentForwardingMessage : AgentForwardingMessageBase, ISSH2PacketBuilder {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messageType">message type</param>
        public SSH2AgentForwardingMessage(SSH2AgentForwardingMessageType messageType)
            : base((byte)messageType) {
        }
    }

    /// <summary>
    /// Base class for the agent forwarding message handler
    /// </summary>
    internal abstract class AgentForwardingMessageHandlerBase : SimpleSSHChannelEventHandler {

        private readonly ByteBuffer _buffer = new ByteBuffer(16 * 1024, 64 * 1024);

        /// <summary>
        /// Handles channel data
        /// </summary>
        /// <param name="data">channel data</param>
        public override void OnData(DataFragment data) {
            _buffer.Append(data);

            if (_buffer.Length >= 4) {
                uint messageLength = SSHUtil.ReadUInt32(_buffer.RawBuffer, _buffer.RawBufferOffset);
                if (_buffer.Length >= 4 + messageLength) {
                    DataFragment message = new DataFragment(_buffer.RawBuffer, _buffer.RawBufferOffset + 4, (int)messageLength);
                    try {
                        ProcessMessage(message);
                    }
                    catch (Exception e) {
                        Debug.WriteLine(e.Message);
                        Debug.WriteLine(e.StackTrace);
                    }
                    _buffer.RemoveHead(4 + (int)messageLength);
                }
            }
        }

        /// <summary>
        /// Process an agent forwarding request message
        /// </summary>
        /// <param name="message">message data</param>
        protected abstract void ProcessMessage(DataFragment message);
    }

    /// <summary>
    /// SSH1 agent forwarding message handler
    /// </summary>
    internal class SSH1AgentForwardingMessageHandler : AgentForwardingMessageHandlerBase {

        private readonly ISSHChannel _channel;
        private readonly IAgentForwardingAuthKeyProvider _authKeyProvider;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="channel">channel object</param>
        /// <param name="authKeyProvider">authentication key provider</param>
        public SSH1AgentForwardingMessageHandler(ISSHChannel channel, IAgentForwardingAuthKeyProvider authKeyProvider) {
            _channel = channel;
            _authKeyProvider = authKeyProvider;
        }

        /// <summary>
        /// Process forwarded message.
        /// </summary>
        /// <param name="message">a forwarded message</param>
        protected override void ProcessMessage(DataFragment message) {
            if (_authKeyProvider == null || !_authKeyProvider.IsAuthKeyProviderEnabled) {
                SendFailure();
                return;
            }

            SSH1DataReader reader = new SSH1DataReader(message);
            SSH1AgentForwardingMessageType messageType = (SSH1AgentForwardingMessageType)reader.ReadByte();
            switch (messageType) {
                case SSH1AgentForwardingMessageType.SSH_AGENTC_REQUEST_RSA_IDENTITIES:
                    RSAIdentities();
                    break;
                case SSH1AgentForwardingMessageType.SSH_AGENTC_RSA_CHALLENGE: {
                        reader.ReadUInt32();    // ignored
                        BigInteger e = reader.ReadMPInt();
                        BigInteger n = reader.ReadMPInt();
                        BigInteger encryptedChallenge = reader.ReadMPInt();
                        byte[] sessionId = reader.Read(16);
                        uint responseType = reader.ReadUInt32();

                        RSAChallenge(e, n, encryptedChallenge, sessionId, responseType);
                    }
                    break;
                default:
                    SendFailure();
                    break;
            }
        }

        /// <summary>
        /// List RSA keys
        /// </summary>
        private void RSAIdentities() {
            var authKeys = _authKeyProvider.GetAvailableSSH1UserAuthKeys();
            if (authKeys == null) {
                authKeys = new SSH1UserAuthKey[0];
            }

            var message = new SSH1AgentForwardingMessage(SSH1AgentForwardingMessageType.SSH_AGENT_RSA_IDENTITIES_ANSWER);
            message.WriteInt32(authKeys.Length);
            foreach (var key in authKeys) {
                message.WriteInt32(key.PublicModulus.BitCount());
                SSH1PacketBuilderMixin.WriteBigInteger(message, key.PublicExponent);
                SSH1PacketBuilderMixin.WriteBigInteger(message, key.PublicModulus);
                message.WriteString(key.Comment);
            }

            Send(message);
        }

        /// <summary>
        /// RSA challenge
        /// </summary>
        /// <param name="e">public exponent</param>
        /// <param name="n">public modulus</param>
        /// <param name="encryptedChallenge">encrypted challenge</param>
        /// <param name="sessionId">session id</param>
        /// <param name="responseType">response type</param>
        private void RSAChallenge(BigInteger e, BigInteger n, BigInteger encryptedChallenge, byte[] sessionId, uint responseType) {
            if (responseType != 1) {
                SendFailure();
                return;
            }

            SSH1UserAuthKey key = FindKey(e, n);
            if (key == null) {
                SendFailure();
                return;
            }

            BigInteger challenge = key.decryptChallenge(encryptedChallenge);
            byte[] rawchallenge = RSAUtil.StripPKCS1Pad(challenge, 2).GetBytes();
            byte[] hash;
            using (var md5 = new MD5CryptoServiceProvider()) {
                md5.TransformBlock(rawchallenge, 0, rawchallenge.Length, rawchallenge, 0);
                md5.TransformFinalBlock(sessionId, 0, sessionId.Length);
                hash = md5.Hash;
            }

            Send(
                new SSH1AgentForwardingMessage(SSH1AgentForwardingMessageType.SSH_AGENT_RSA_RESPONSE)
                    .Write(hash)
            );
        }

        /// <summary>
        /// Find a key
        /// </summary>
        /// <param name="e">public exponent</param>
        /// <param name="n">public modulus</param>
        /// <returns>matched key object, or null if not found.</returns>
        private SSH1UserAuthKey FindKey(BigInteger e, BigInteger n) {
            var authKeys = _authKeyProvider.GetAvailableSSH1UserAuthKeys();
            if (authKeys == null) {
                return null;
            }

            foreach (var key in authKeys) {
                if (key.PublicModulus == n && key.PublicExponent == e) {
                    return key;
                }
            }

            return null;
        }

        /// <summary>
        /// Sends SSH_AGENT_FAILURE message.
        /// </summary>
        private void SendFailure() {
            Send(
                new SSH1AgentForwardingMessage(SSH1AgentForwardingMessageType.SSH_AGENT_FAILURE)
            );
        }

        /// <summary>
        /// Sends a message.
        /// </summary>
        /// <param name="message">a message object</param>
        private void Send(SSH1AgentForwardingMessage message) {
            _channel.Send(message.GetImage());
        }
    }



    //currently OpenSSH's SSH2 connections are only supported
    /*
    internal class AgentForwardingChannel : ISSHChannelEventReceiver {
        private readonly IAgentForward _client;
        private readonly ByteBuffer _buffer;
        private SSHChannel _channel;
        private bool _closed;

        public AgentForwardingChannel(IAgentForward client) {
            _client = client;
            _buffer = new ByteBuffer(0x1000, 0x40000);
        }

        internal void SetChannel(SSHChannel channel) {
            _channel = channel;
        }

        public void OnData(DataFragment data) {
            _buffer.Append(data);
            if (_buffer.Length >= 4) {
                SSH2DataReader reader = new SSH2DataReader(_buffer.AsDataFragment());
                int expectedLength = reader.ReadInt32();
                if (expectedLength <= reader.RemainingDataLength) {
                    AgentForwadPacketType pt = (AgentForwadPacketType)reader.ReadByte();
                    switch (pt) {
                        case AgentForwadPacketType.SSH2_AGENTC_REQUEST_IDENTITIES:
                            SendKeyList();
                            break;
                        case AgentForwadPacketType.SSH2_AGENTC_SIGN_REQUEST:
                            byte[] reqKeyBlob = reader.ReadByteString();
                            byte[] reqData = reader.ReadByteString();
                            uint reqFlags = reader.ReadUInt32();
                            SendSign(reqKeyBlob, reqData, reqFlags);
                            break;
                        default:
                            SendFailure();
                            break;
                    }
                }
            }
        }

        public void OnExtendedData(uint type, DataFragment data) {
        }

        public void OnChannelClosed() {
            if (!_closed) {
                _closed = true;
                _client.Close();
            }
        }

        public void OnChannelEOF() {
            if (!_closed) {
                _closed = true;
                _client.Close();
            }
        }

        public void OnChannelError(Exception error) {
            _client.OnError(error);
        }

        public void OnChannelReady() {
        }

        public void OnMiscPacket(byte packetType, DataFragment data) {
        }

        private void SendKeyList() {
            SSH2PayloadImageBuilder image = new SSH2PayloadImageBuilder();
            image.WriteUInt32(0);    // length field
            image.WriteByte((byte)AgentForwadPacketType.SSH2_AGENT_IDENTITIES_ANSWER);
            // keycount, ((blob-len, pubkey-blob, comment-len, comment) * keycount)
            SSH2UserAuthKey[] keys = _client.GetAvailableSSH2UserAuthKeys();
            image.WriteInt32(keys.Length);
            foreach (SSH2UserAuthKey key in keys) {
                byte[] blob = key.GetPublicKeyBlob();
                image.WriteAsString(blob);
                Debug.WriteLine("Userkey comment=" + key.Comment);
                image.WriteUTF8String(key.Comment);
            }
            int length = image.Length;
            image.OverwriteUInt32(0, (uint)(length - 4));
            TransmitWriter(image.AsDataFragment());
        }

        private void SendSign(byte[] blob, byte[] data, uint flags) {
            SSH2UserAuthKey[] keys = _client.GetAvailableSSH2UserAuthKeys();
            SSH2UserAuthKey key = FindKey(keys, blob);
            if (key == null) {
                SendFailure();
                _client.NotifyPublicKeyDidNotMatch();
                return;
            }

            SSH2PayloadImageBuilder image = new SSH2PayloadImageBuilder();
            image.WriteString(SSH2Util.PublicKeyAlgorithmName(key.Algorithm));
            image.WriteAsString(key.Sign(data));
            byte[] signpackImage = image.GetBytes();

            image.Clear();
            image.WriteUInt32(0);    // length field
            image.WriteByte((byte)AgentForwadPacketType.SSH2_AGENT_SIGN_RESPONSE);
            image.WriteAsString(signpackImage);
            int length = image.Length;
            image.OverwriteUInt32(0, (uint)(length - 4));
            TransmitWriter(image.AsDataFragment());
        }

        private void SendFailure() {
            SSH2PayloadImageBuilder image = new SSH2PayloadImageBuilder();
            image.WriteUInt32(1);    // length field
            image.WriteByte((byte)AgentForwadPacketType.SSH_AGENT_FAILURE);
            TransmitWriter(image.AsDataFragment());
        }

        private void TransmitWriter(DataFragment data) {
            _channel.Transmit(data.Data , data.Offset, data.Length);
        }

        private SSH2UserAuthKey FindKey(SSH2UserAuthKey[] keys, byte[] blob) {
            foreach (SSH2UserAuthKey key in keys) {
                byte[] t = key.GetPublicKeyBlob();
                if (Util.SSHUtil.ByteArrayEqual(t, blob)) {
                    return key;
                }
            }
            return null;
        }
    }
     */
}
