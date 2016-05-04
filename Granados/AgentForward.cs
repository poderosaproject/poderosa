using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Granados.IO;
using Granados.IO.SSH2;
using Granados.SSH2;
using Granados.Util;

namespace Granados {
    public enum AgentForwadPacketType {
        /* Messages sent by the client. */
#if false
    SSH_AGENT_REQUEST_VERSION                     =   1,
    SSH_AGENT_ADD_KEY                             = 202,
    SSH_AGENT_DELETE_ALL_KEYS                     = 203,
    SSH_AGENT_LIST_KEYS                           = 204,
    SSH_AGENT_PRIVATE_KEY_OP                      = 205,
    SSH_AGENT_FORWARDING_NOTICE                   = 206,
    SSH_AGENT_DELETE_KEY                          = 207,
    SSH_AGENT_LOCK                                = 208,
    SSH_AGENT_UNLOCK                              = 209,
    SSH_AGENT_PING                                = 212,
    SSH_AGENT_RANDOM                              = 213,
    SSH_AGENT_EXTENSION                           = 301,

   /* Messages sent by the agent. */
    SSH_AGENT_SUCCESS                             = 101,
    SSH_AGENT_FAILURE                             = 102,
    SSH_AGENT_VERSION_RESPONSE                    = 103,
    SSH_AGENT_KEY_LIST                            = 104,
    SSH_AGENT_OPERATION_COMPLETE                  = 105,
    SSH_AGENT_RANDOM_DATA                         = 106,
    SSH_AGENT_ALIVE                               = 150,
#endif

        /*
     * OpenSSH's SSH-2 agent messages.
     */
        SSH_AGENT_FAILURE = 5,
        SSH_AGENT_SUCCESS = 6,

        SSH2_AGENTC_REQUEST_IDENTITIES = 11,
        SSH2_AGENT_IDENTITIES_ANSWER = 12,
        SSH2_AGENTC_SIGN_REQUEST = 13,
        SSH2_AGENT_SIGN_RESPONSE = 14,
        SSH2_AGENTC_ADD_IDENTITY = 17,
        SSH2_AGENTC_REMOVE_IDENTITY = 18,
        SSH2_AGENTC_REMOVE_ALL_IDENTITIES = 19
    }

    //the client must implement this interface and set SSHConnectionParameter
    public interface IAgentForward {
        bool CanAcceptForwarding(); //ask agent forwarding is available
        SSH2UserAuthKey[] GetAvailableSSH2UserAuthKeys(); //list key

        //notifications
        void NotifyPublicKeyDidNotMatch();
        void Close();
        void OnError(Exception ex);
    }

    //currently OpenSSH's SSH2 connections are only supported
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
}
