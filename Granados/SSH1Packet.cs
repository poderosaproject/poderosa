/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: SSH1Packet.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/
/*
 * structure of packet
 * 
 * length(4) padding(1-8) type(1) data(0+) crc(4)    
 * 
 * 1. length = type+data+crc
 * 2. the length of padding+type+data+crc must be a multiple of 8
 * 3. padding length must be 1 at least
 * 4. crc is calculated from padding,type and data
 *
 */

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Granados.Crypto;
using Granados.IO;
using Granados.IO.SSH1;

using Granados.Util;

namespace Granados.SSH1 {

    internal class SSH1Packet {
        private byte _type;
        private byte[] _data;
        private uint _CRC;

        /**
        * constructs from the packet type and the body
        */
        public static SSH1Packet FromPlainPayload(PacketType type, byte[] data) {
            SSH1Packet p = new SSH1Packet();
            p._type = (byte)type;
            p._data = data;
            return p;
        }
        public static SSH1Packet FromPlainPayload(PacketType type) {
            SSH1Packet p = new SSH1Packet();
            p._type = (byte)type;
            p._data = new byte[0];
            return p;
        }
        /**
        * creates a packet as the input of shell
        */
        static SSH1Packet AsStdinString(byte[] input) {
            SSH1DataWriter w = new SSH1DataWriter();
            w.WriteAsString(input);
            SSH1Packet p = SSH1Packet.FromPlainPayload(PacketType.SSH_CMSG_STDIN_DATA, w.ToByteArray());
            return p;
        }

        private byte[] BuildImage() {
            int packet_length = (_data == null ? 0 : _data.Length) + 5; //type and CRC
            int padding_length = 8 - (packet_length % 8);

            byte[] image = new byte[packet_length + padding_length + 4];
            SSHUtil.WriteIntToByteArray(image, 0, packet_length);

            for (int i = 0; i < padding_length; i++)
                image[4 + i] = 0; //padding: filling by random values is better
            image[4 + padding_length] = _type;
            if (_data != null)
                Array.Copy(_data, 0, image, 4 + padding_length + 1, _data.Length);

            _CRC = CRC.Calc(image, 4, image.Length - 8);
            SSHUtil.WriteIntToByteArray(image, image.Length - 4, (int)_CRC);

            return image;
        }

        /**
        * writes to plain stream
        */
        public void WriteTo(IGranadosSocket output) {
            byte[] image = BuildImage();
            output.Write(image, 0, image.Length);
        }
        /**
        * writes to encrypted stream
        */
        public void WriteTo(IGranadosSocket output, Cipher cipher) {
            byte[] image = BuildImage();
            //dumpBA(image);
            byte[] encrypted = new byte[image.Length - 4];
            cipher.Encrypt(image, 4, image.Length - 4, encrypted, 0); //length field must not be encrypted

            Array.Copy(encrypted, 0, image, 4, encrypted.Length);
            output.Write(image, 0, image.Length);
        }

        public PacketType Type {
            get {
                return (PacketType)_type;
            }
        }
        public byte[] Data {
            get {
                return _data;
            }
        }
        public int DataLength {
            get {
                return _data == null ? 0 : _data.Length;
            }
        }
    }


    internal class CallbackSSH1PacketHandler : IDataHandler {
        internal SSH1Connection _connection;

        internal CallbackSSH1PacketHandler(SSH1Connection con) {
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

    /// <summary>
    /// <see cref="IDataHandler"/> that extracts SSH packet from the data stream
    /// and passes it to another <see cref="IDataHandler"/>.
    /// </summary>
    internal class SSH1Packetizer : FilterDataHandler {
        private const int MIN_PACKET_LENGTH = 5;
        private const int MAX_PACKET_LENGTH = 262144;
        private const int MAX_PACKET_DATA_SIZE = MAX_PACKET_LENGTH + (8 - (MAX_PACKET_LENGTH % 8)) + 4;

        private readonly ByteBuffer _inputBuffer = new ByteBuffer(MAX_PACKET_DATA_SIZE, MAX_PACKET_DATA_SIZE * 16);
        private readonly ByteBuffer _packetImage = new ByteBuffer(36000, MAX_PACKET_DATA_SIZE * 2);
        private Cipher _cipher;
        private readonly object _cipherSync = new object();
        private bool _checkMAC;
        private int _packetLength;

        private bool _hasError = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="handler">a handler that SSH packets are passed to</param>
        public SSH1Packetizer(IDataHandler handler)
            : base(handler) {
            _cipher = null;
            _checkMAC = false;
            _packetLength = -1;
        }

        /// <summary>
        /// Set cipher settings.
        /// </summary>
        /// <param name="cipher">cipher algorithm, or null if not specified.</param>
        /// <param name="checkMac">specifies whether CRC check is performed.</param>
        public void SetCipher(Cipher cipher, bool checkMac) {
            lock (_cipherSync) {
                _cipher = cipher;
                _checkMAC = checkMac;
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

                    _inputBuffer.Append(data.Data, data.Offset, data.Length);

                    ProcessBuffer();
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
                OnDataInternal(packet);
            }
        }

        /// <summary>
        /// Extracts SSH packet from the internal buffer.
        /// </summary>
        /// <returns>
        /// true if one SSH packet has been extracted.
        /// in this case, _packetImage contains Packet Type field and Data field of the SSH packet.
        /// </returns>
        private bool ConstructPacket() {
            const int PACKET_LENGTH_FIELD_LEN = 4;
            const int CHECK_BYTES_FIELD_LEN = 4;

            if (_packetLength < 0) {
                if (_inputBuffer.Length < PACKET_LENGTH_FIELD_LEN) {
                    return false;
                }

                uint packetLength = SSHUtil.ReadUInt32(_inputBuffer.RawBuffer, _inputBuffer.RawBufferOffset);
                _inputBuffer.RemoveHead(PACKET_LENGTH_FIELD_LEN);

                if (packetLength < MIN_PACKET_LENGTH || packetLength > MAX_PACKET_LENGTH) {
                    throw new SSHException(String.Format("invalid packet length : {0}", packetLength));
                }

                _packetLength = (int)packetLength;
            }

            int paddingLength = 8 - (_packetLength % 8);
            int requiredLength = paddingLength + _packetLength;

            if (_inputBuffer.Length < requiredLength) {
                return false;
            }

            _packetImage.Clear();
            _packetImage.Append(_inputBuffer, 0, requiredLength);   // Padding, Packet Type, Data, and Check fields
            _inputBuffer.RemoveHead(requiredLength);

            if (_cipher != null) {
                _cipher.Decrypt(
                    _packetImage.RawBuffer, _packetImage.RawBufferOffset, requiredLength,
                    _packetImage.RawBuffer, _packetImage.RawBufferOffset);
            }

            if (_checkMAC) {
                uint crc = CRC.Calc(
                            _packetImage.RawBuffer,
                            _packetImage.RawBufferOffset,
                            requiredLength - CHECK_BYTES_FIELD_LEN);
                uint expected = SSHUtil.ReadUInt32(
                            _packetImage.RawBuffer,
                            _packetImage.RawBufferOffset + requiredLength - CHECK_BYTES_FIELD_LEN);
                if (crc != expected) {
                    throw new SSHException("CRC Error");
                }
            }

            // retain only Packet Type and Data fields
            _packetImage.RemoveHead(paddingLength);
            _packetImage.RemoveTail(CHECK_BYTES_FIELD_LEN);

            // sanity check
            if (_packetImage.Length != _packetLength - CHECK_BYTES_FIELD_LEN) {
                throw new InvalidOperationException();
            }

            // prepare for the next packet
            _packetLength = -1;

            return true;
        }
    }
}
