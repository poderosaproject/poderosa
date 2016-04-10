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
        public virtual DataFragment Close(Cipher cipher, MAC mac, int sequence) {
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
	        byte[] tmp = new byte[4];
            Rng rng = RngManager.GetSecureRng();
            for (int i = 0; i < paddingLength; i += 4) {
                rng.GetBytes(tmp);
                _writer.Write(tmp);
            }

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
    internal class SSH2MsgNewKeys : DataFragment, SynchronizedPacketReceiver.IQueueEventListener {

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

        public void Dequeued(bool canceled) {
            if (_onDequeued != null) {
                _onDequeued();
            }
        }

        #endregion
    }

    /// <summary>
    /// <see cref="IDataHandler"/> that extracts SSH packet from the data stream
    /// and passes it to another <see cref="IDataHandler"/>.
    /// </summary>
    internal class SSH2PacketBuilder : FilterDataHandler {
        // RFC4253: The minimum size of a packet is 16 (or the cipher block size, whichever is larger) bytes.
        private const int MIN_PACKET_LENGTH = 12;    // exclude packet_length field (4 bytes)
        private const int MAX_PACKET_LENGTH = 0x80000; //there was the case that 64KB is insufficient

        private readonly ByteBuffer _inputBuffer = new ByteBuffer(MAX_PACKET_LENGTH, MAX_PACKET_LENGTH * 16);
        private readonly ByteBuffer _packetImage = new ByteBuffer(36000, MAX_PACKET_LENGTH * 2);
        private readonly byte[] _dword = new byte[4];
        private int _packetLength;
        private uint _sequence;
        private Cipher _cipher;
        private readonly object _cipherSync = new object();
        private MAC _mac;
        private int _macLength;

        private bool _pending = false;
        private bool _hasError = false;

        private DateTime _keyErrorDetectionTimeout = DateTime.MaxValue;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handler">a handler that SSH packets are passed to</param>
        public SSH2PacketBuilder(IDataHandler handler)
            : base(handler) {
            _sequence = 0;
            _cipher = null;
            _mac = null;
            _macLength = 0;
            _packetLength = -1;
        }

        /// <summary>
        /// Set cipher settings.
        /// </summary>
        /// <param name="cipher">cipher algorithm, or null if not specified.</param>
        /// <param name="mac">MAC algorithm, or null if not specified.</param>
        public void SetCipher(Cipher cipher, MAC mac) {
            lock (_cipherSync) {
                try {
                    _cipher = cipher;
                    _mac = mac;
                    _macLength = (_mac != null) ? _mac.Size : 0;

                    bool resumePending = _pending;
                    _pending = false;
                    _keyErrorDetectionTimeout = DateTime.MaxValue;

                    if (resumePending) {
                        ProcessBuffer();
                    }
                }
                catch (Exception ex) {
                    OnError(ex);
                }
            }
        }

        /// <summary>
        /// Implements <see cref="FilterDataHandler"/>.
        /// </summary>
        /// <param name="data">fragment of the data stream</param>
        protected override void FilterData(DataFragment data) {
            lock (_cipherSync) {
                try {
                    if (_hasError) {
                        return;
                    }

                    // key error detection
                    if (_pending && DateTime.UtcNow > _keyErrorDetectionTimeout) {
                        _hasError = true;   // disable accepting data any more
                        return;
                    }

                    _inputBuffer.Append(data.Data, data.Offset, data.Length);

                    if (!_pending) {
                        ProcessBuffer();
                    }
                }
                catch (Exception ex) {
                    OnError(ex);
                }
            }
        }

        /// <summary>
        /// Extracts SSH packet from the internal buffer and passes it to the next handler.
        /// </summary>
        private void ProcessBuffer() {
            while (true) {
                bool hasPacket;
                try {
                    hasPacket = ConstructPacket();
                }
                catch (Exception) {
                    _hasError = true;
                    throw;
                }

                if (!hasPacket) {
                    return;
                }

                DataFragment packet = _packetImage.AsDataFragment();

                if (IsMsgNewKeys(packet)) {
                    // next packet must be decrypted with the new key
                    _cipher = null;
                    _mac = null;
                    _macLength = 0;

                    _pending = true;    // retain trailing packets in the buffer
                    _keyErrorDetectionTimeout = DateTime.MaxValue;

                    SSH2MsgNewKeys newKeysPacket =
                        new SSH2MsgNewKeys(packet, () => {
                            lock (_cipherSync) {
                                // start key error detection
                                _keyErrorDetectionTimeout = DateTime.UtcNow.AddMilliseconds(1000);
                            }
                        });

                    OnDataInternal(newKeysPacket);
                    break;
                }

                OnDataInternal(packet);
            }
        }

        /// <summary>
        /// Check if a SSH packet is SSH_MSG_NEWKEYS.
        /// </summary>
        /// <param name="packet">a SSH packet</param>
        /// <returns>true if a SSH packet is SSH_MSG_NEWKEYS.</returns>
        private bool IsMsgNewKeys(DataFragment packet) {
            return packet.Length >= 1 && packet.ByteAt(0) == (byte)PacketType.SSH_MSG_NEWKEYS;
        }

        /// <summary>
        /// Extracts SSH packet from the internal buffer.
        /// </summary>
        /// <returns>
        /// true if one SSH packet has been extracted.
        /// in this case, _packetImage contains payload part of the SSH packet.
        /// </returns>
        private bool ConstructPacket() {
            const int SEQUENCE_NUMBER_FIELD_LEN = 4;
            const int PACKET_LENGTH_FIELD_LEN = 4;
            const int PADDING_LENGTH_FIELD_LEN = 1;

            if (_packetLength < 0) {
                int headLen = (_cipher != null) ? _cipher.BlockSize : 4;

                if (_inputBuffer.Length < headLen) {
                    return false;
                }

                _packetImage.Clear();
                SSHUtil.WriteUIntToByteArray(_dword, 0, _sequence);
                _packetImage.Append(_dword); // sequence_number field for computing MAC
                _packetImage.Append(_inputBuffer, 0, headLen);
                _inputBuffer.RemoveHead(headLen);

                int headOffset = _packetImage.RawBufferOffset + SEQUENCE_NUMBER_FIELD_LEN;

                if (_cipher != null) {
                    // decrypt first block
                    _cipher.Decrypt(
                        _packetImage.RawBuffer, headOffset, headLen,
                        _packetImage.RawBuffer, headOffset);
                }

                uint packetLength = SSHUtil.ReadUInt32(_packetImage.RawBuffer, headOffset);

                if (packetLength < MIN_PACKET_LENGTH || packetLength >= MAX_PACKET_LENGTH) {
                    throw new SSHException(String.Format("invalid packet length : {0}", packetLength));
                }

                _packetLength = (int)packetLength;
            }

            int packetHeadLen = _packetImage.Length;    // size already read in
            int requiredLength = SEQUENCE_NUMBER_FIELD_LEN + PACKET_LENGTH_FIELD_LEN + _packetLength + _macLength - packetHeadLen;

            if (_inputBuffer.Length < requiredLength) {
                return false;
            }

            _packetImage.Append(_inputBuffer, 0, requiredLength);
            _inputBuffer.RemoveHead(requiredLength);

            if (_cipher != null) {
                // decrypt excluding MAC
                int headOffset = _packetImage.RawBufferOffset + packetHeadLen;
                _cipher.Decrypt(
                    _packetImage.RawBuffer, headOffset, requiredLength - _macLength,
                    _packetImage.RawBuffer, headOffset);
            }

            int paddingLength = _packetImage[SEQUENCE_NUMBER_FIELD_LEN + PACKET_LENGTH_FIELD_LEN];
            if (paddingLength < 4) {
                throw new SSHException(String.Format("invalid padding length : {0}", paddingLength));
            }

            int payloadLength = _packetLength - PADDING_LENGTH_FIELD_LEN - paddingLength;

            if (_mac != null) {
                int contentLen = SEQUENCE_NUMBER_FIELD_LEN + PACKET_LENGTH_FIELD_LEN + _packetLength;
                byte[] result = _mac.ComputeHash(_packetImage.RawBuffer, _packetImage.RawBufferOffset, contentLen);

                if (result.Length != _macLength ||
                    !SSHUtil.ByteArrayEqual(result, 0, _packetImage.RawBuffer, _packetImage.RawBufferOffset + contentLen, _macLength)) {
                    throw new SSHException("MAC mismatch");
                }
            }

            // retain only payload
            _packetImage.RemoveHead(SEQUENCE_NUMBER_FIELD_LEN + PACKET_LENGTH_FIELD_LEN + PADDING_LENGTH_FIELD_LEN);
            _packetImage.RemoveTail(_macLength + paddingLength);

            // sanity check
            if (_packetImage.Length != payloadLength) {
                throw new InvalidOperationException();
            }

            // prepare for the next packet
            ++_sequence;
            _packetLength = -1;

            return true;
        }
    }
}
