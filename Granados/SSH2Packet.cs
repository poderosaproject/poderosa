/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

  I implemented this algorithm with reference to following products and books though the algorithm is known publicly.
    * MindTerm ( AppGate Network Security )
    * Applied Cryptography ( Bruce Schneier )

 $Id: SSH2Packet.cs,v 1.8 2011/11/14 13:35:59 kzmi Exp $
*/

using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Diagnostics;
using HMACSHA1 = System.Security.Cryptography.HMACSHA1;

using Granados.Crypto;
using Granados.IO;
using Granados.IO.SSH2;
using Granados.Util;

namespace Granados.SSH2 {
    /* SSH2 Packet Structure
     * 
     * uint32    packet_length
     * byte      padding_length
     * byte[n1]  payload; n1 = packet_length - padding_length - 1
     * byte[n2]  random padding; n2 = padding_length (max 255)
     * byte[m]   mac (message authentication code); m = mac_length
     * 
     * 4+1+n1+n2 must be a multiple of the cipher block size
     */

    //SSH2 Packet for Transmission
    internal class SSH2TransmissionPacket {
        private readonly SSH2DataWriter _writer;
        private readonly DataFragment _dataFragment;

        private bool _isOpen;

        private const int SEQUENCE_MARGIN = 4;
        private const int LENGTH_MARGIN = 4;
        private const int PADDING_MARGIN = 1;

        public const int INITIAL_OFFSET = SEQUENCE_MARGIN + LENGTH_MARGIN + PADDING_MARGIN;

        public SSH2TransmissionPacket() {
            _writer = new SSH2DataWriter();
            _dataFragment = new DataFragment(null, 0, 0);
            _isOpen = false;
        }

        // Derived class can override this method for additional setup.
        public virtual void Open() {
            if (_isOpen)
                throw new SSHException("internal state error");
            _writer.Reset();
            _writer.SetOffset(INITIAL_OFFSET);
            _isOpen = true;
        }

        public SSH2DataWriter DataWriter {
            get {
                return _writer;
            }
        }

        // Derived class can override this method to modify the buffer.
        public virtual DataFragment Close(Cipher cipher, Random rnd, MAC mac, int sequence) {
            if (!_isOpen)
                throw new SSHException("internal state error");

            int blocksize = cipher == null ? 8 : cipher.BlockSize;
            int payloadLength = _writer.Length - (SEQUENCE_MARGIN + LENGTH_MARGIN + PADDING_MARGIN);
            int paddingLength = 11 - payloadLength % blocksize;
            while (paddingLength < 4)
                paddingLength += blocksize;
            int packetLength = PADDING_MARGIN + payloadLength + paddingLength;
            int imageLength = packetLength + LENGTH_MARGIN;

            //fill padding
            for (int i = 0; i < paddingLength; i += 4)
                _writer.WriteInt32(rnd.Next());

            //manipulate stream
            byte[] rawbuf = _writer.UnderlyingBuffer;
            SSHUtil.WriteIntToByteArray(rawbuf, 0, sequence);
            SSHUtil.WriteIntToByteArray(rawbuf, SEQUENCE_MARGIN, packetLength);
            rawbuf[SEQUENCE_MARGIN + LENGTH_MARGIN] = (byte)paddingLength;

            //mac
            if (mac != null) {
                byte[] macCode = mac.ComputeHash(rawbuf, 0, packetLength + LENGTH_MARGIN + SEQUENCE_MARGIN);
                Array.Copy(macCode, 0, rawbuf, packetLength + LENGTH_MARGIN + SEQUENCE_MARGIN, macCode.Length);
                imageLength += macCode.Length;
            }

            //encrypt
            if (cipher != null)
                cipher.Encrypt(rawbuf, SEQUENCE_MARGIN, packetLength + LENGTH_MARGIN, rawbuf, SEQUENCE_MARGIN);

            _dataFragment.Init(rawbuf, SEQUENCE_MARGIN, imageLength);
            _isOpen = false;
            return _dataFragment;
        }
    }



    internal class CallbackSSH2PacketHandler : IDataHandler {
        internal SSH2Connection _connection;

        internal CallbackSSH2PacketHandler(SSH2Connection con) {
            _connection = con;
        }
        public void OnData(DataFragment data) {
            _connection.AsyncReceivePacket(data);
        }
        public void OnError(Exception error) {
            _connection.EventReceiver.OnError(error);
        }
        public void OnClosed() {
            _connection.EventReceiver.OnConnectionClosed();
        }
    }

    // Special DataFragment for SSH_MSG_NEWKEYS.
    internal class SSH2MsgNewKeys : DataFragment, SynchronizedDataHandler.IQueueEventListener {

        public delegate void Handler();

        private Handler _onDequeued;

        public SSH2MsgNewKeys(DataFragment data, Handler onDequeued)
            : base(data.Data, data.Offset, data.Length) {
            _onDequeued = onDequeued;
        }

        public override DataFragment Isolate() {
            // The new instance returned from this method will be queued into the packet queue.
            // Need to pass delegate object to the new instance to process "dequeued" event.
            DataFragment newData = base.Isolate();
            SSH2MsgNewKeys newMsgNewKeys = new SSH2MsgNewKeys(newData, _onDequeued);
            _onDequeued = null;
            return newMsgNewKeys;
        }

        #region IQueueEventListener Members

        public void Dequeued() {
            if (_onDequeued != null) {
                _onDequeued();
            }
        }

        #endregion
    }

    internal class SSH2PacketBuilder : FilterDataHandler {
        private const int MAX_PACKET_LENGTH = 0x80000; //there was the case that 64KB is insufficient

        private readonly DataFragment _buffer;
        private readonly DataFragment _packet;
        private byte[] _head;
        private bool _head_is_available;
        private int _sequence;
        private Cipher _cipher;
        private readonly object _cipherSync = new object();
        private MAC _mac;
        private bool _macEnabled;

        private bool _pending = false;
        private bool _keyError = false;

        private DateTime _keyErrorDetectionTimeout = DateTime.MaxValue;

        private const int SEQUENCE_FIELD_LEN = 4;
        private const int PACKET_LENGTH_FIELD_LEN = 4;
        private const int PADDING_LENGTH_FIELD_LEN = 1;

        public SSH2PacketBuilder(IDataHandler handler)
            : base(handler) {
            _buffer = new DataFragment(0x1000);
            _packet = new DataFragment(_buffer.Capacity);
            _sequence = 0;
            _cipher = null;
            _mac = null;
            _head = null;
        }

        public void SetCipher(Cipher cipher, MAC mac, bool mac_enabled) {
            lock (_cipherSync) {
                try {
                    _cipher = cipher;
                    _mac = mac;
                    _macEnabled = mac_enabled;
                    _head = new byte[cipher.BlockSize];

                    _pending = false;
                    _keyErrorDetectionTimeout = DateTime.MaxValue;

                    ProcessBuffer();
                } catch (Exception ex) {
                    OnError(ex);
                }
            }
        }

        public override void OnData(DataFragment data) {
            lock (_cipherSync) {
                try {
                    if (!_keyError) {
                        // key error detection
                        if (_pending && DateTime.UtcNow > _keyErrorDetectionTimeout) {
                            _keyError = true;   // disable accepting data any more
                            return;
                        }

                        _buffer.Append(data);

                        if (!_pending) {
                            ProcessBuffer();
                        }
                    }
                } catch (Exception ex) {
                    OnError(ex);
                }
            }
        }

        private void ProcessBuffer() {
            // buffer may contains multiple packet data
            while (ConstructPacket()) {
                if (IsMsgNewKeys(_packet)) {
                    // next packet must be decrypted with the new key
                    _cipher = null;
                    _mac = null;
                    _macEnabled = false;
                    _head = null;

                    _pending = true;    // retain trailing packets in the buffer
                    _keyErrorDetectionTimeout = DateTime.MaxValue;

                    SSH2MsgNewKeys newKeysPacket =
                        new SSH2MsgNewKeys(_packet, new SSH2MsgNewKeys.Handler(OnMsgNewKeysDequeued));

                    _inner_handler.OnData(newKeysPacket);
                    break;
                }

                _inner_handler.OnData(_packet);
            }
        }

        private bool IsMsgNewKeys(DataFragment packet) {
            return packet.Length >= 1 && packet.ByteAt(0) == (byte)PacketType.SSH_MSG_NEWKEYS;
        }

        private void OnMsgNewKeysDequeued() {
            lock (_cipherSync) {
                // start key error detection
                _keyErrorDetectionTimeout = DateTime.UtcNow.AddMilliseconds(1000);
            }
        }

        public override void OnClosed() {
            base.OnClosed();
        }

        //returns true if a new packet is obtained to _packet
        private bool ConstructPacket() {
            if (_cipher == null) { //暗号が確立する前
                if (_buffer.Length < PACKET_LENGTH_FIELD_LEN)
                    return false;
                int len = SSHUtil.ReadInt32(_buffer.Data, _buffer.Offset);
                if (_buffer.Length < PACKET_LENGTH_FIELD_LEN + len)
                    return false;

                ReadPacketFromPlainStream();
            }
            else {
                if (!_head_is_available) {
                    if (_buffer.Length < _cipher.BlockSize)
                        return false;
                    _cipher.Decrypt(_buffer.Data, _buffer.Offset, _head.Length, _head, 0);
                    _buffer.Consume(_head.Length);
                    _head_is_available = true;
                }

                int len = SSHUtil.ReadInt32(_head, 0);
                if (_buffer.Length < len + PACKET_LENGTH_FIELD_LEN - _head.Length + _mac.Size)
                    return false;

                ReadPacketWithDecryptedHead();
                _head_is_available = false;
            }

            _sequence++;
            return true;
        }

        //no decryption, no mac
        private void ReadPacketFromPlainStream() {
            int offset = _buffer.Offset;
            int packet_length = SSHUtil.ReadInt32(_buffer.Data, offset);
            if (packet_length <= 0 || packet_length >= MAX_PACKET_LENGTH)
                throw new SSHException(String.Format("packet size {0} is invalid", packet_length));
            offset += PACKET_LENGTH_FIELD_LEN;

            byte padding_length = _buffer.Data[offset++];
            if (padding_length < 4)
                throw new SSHException(String.Format("padding length {0} is invalid", padding_length));

            int payload_length = packet_length - 1 - padding_length;
            Array.Copy(_buffer.Data, offset, _packet.Data, 0, payload_length);
            _packet.SetLength(0, payload_length);

            _buffer.Consume(packet_length + PACKET_LENGTH_FIELD_LEN);
        }

        private void ReadPacketWithDecryptedHead() {
            /* SOURCE      : _head(packet_size, padding_length) + _buffer(payload + mac)
             * DESTINATION : _packet(payload)
             */

            int offset = _buffer.Offset;
            int packet_length = SSHUtil.ReadInt32(_head, 0);
            if (packet_length <= 0 || packet_length >= MAX_PACKET_LENGTH)
                throw new SSHException(String.Format("packet size {0} is invalid", packet_length));

            _packet.AssureCapacity(packet_length + PACKET_LENGTH_FIELD_LEN + SEQUENCE_FIELD_LEN);
            int padding_length = (int)_head[PACKET_LENGTH_FIELD_LEN];
            if (padding_length < 4)
                throw new SSHException("padding length is invalid");

            //to compute hash, we write _sequence at the top of _packet.Data
            SSHUtil.WriteIntToByteArray(_packet.Data, 0, _sequence);
            Array.Copy(_head, 0, _packet.Data, SEQUENCE_FIELD_LEN, _head.Length);

            if (packet_length > (_cipher.BlockSize - PACKET_LENGTH_FIELD_LEN)) { //in case of _head is NOT the entire of the packet
                int decrypting_size = packet_length - (_cipher.BlockSize - PACKET_LENGTH_FIELD_LEN);
                _cipher.Decrypt(_buffer.Data, _buffer.Offset, decrypting_size, _packet.Data, SEQUENCE_FIELD_LEN + _head.Length);
            }

            _packet.SetLength(SEQUENCE_FIELD_LEN + PACKET_LENGTH_FIELD_LEN + PADDING_LENGTH_FIELD_LEN, packet_length - 1 - padding_length);
            _buffer.Consume(packet_length + PACKET_LENGTH_FIELD_LEN - _head.Length + _mac.Size);

            if (_macEnabled) {
                byte[] result = _mac.ComputeHash(_packet.Data, 0, 4 + PACKET_LENGTH_FIELD_LEN + packet_length);

                if (SSHUtil.memcmp(result, 0, _buffer.Data, _buffer.Offset - _mac.Size, _mac.Size) != 0)
                    throw new SSHException("MAC mismatch");
            }
        }

    }
}
