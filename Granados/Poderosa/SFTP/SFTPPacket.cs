/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SFTPPacket.cs,v 1.1 2011/11/14 14:01:52 kzmi Exp $
 */
using System;

using Granados.SSH2;
using Granados.IO;
using Granados.IO.SSH2;
using Granados.Util;
using Granados.Crypto;

namespace Granados.Poderosa.SFTP {

    /// <summary>
    /// Specialized <see cref="SSH2Packet"/> for constructing SFTP packet.
    /// </summary>
    /// <remarks>
    /// The instances of this class share single thread-local buffer.
    /// You should be careful that only single instance is used while constructing a packet.
    /// </remarks>
    internal class SFTPPacket : SSH2Packet {

        private readonly int _sftpDataOffset;

        private const int CHANNEL_DATA_LENGTH_FIELD_LEN = 4;
        private const int SFTP_MESSAGE_LENGTH_FIELD_LEN = 4;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="packetType">SFTP packet type.</param>
        /// <param name="remoteChannel">remote channel number</param>
        public SFTPPacket(SFTPPacketType packetType, uint remoteChannel)
            : base(SSH2PacketType.SSH_MSG_CHANNEL_DATA) {
            Payload.WriteUInt32(remoteChannel);
            Payload.WriteUInt32(0);  // channel data length
            Payload.WriteUInt32(0);  // SFTP message length
            _sftpDataOffset = Payload.Length;
            Payload.WriteByte((byte)packetType);
        }

        /// <summary>
        /// Prepare SFTP message before making a packet image.
        /// </summary>
        protected override void BeforeBuildImage() {
            int sftpDataLength = Payload.Length - _sftpDataOffset;
            int offset = _sftpDataOffset - SFTP_MESSAGE_LENGTH_FIELD_LEN;
            Payload.OverwriteUInt32(offset, (uint)sftpDataLength);
            offset -= CHANNEL_DATA_LENGTH_FIELD_LEN;
            Payload.OverwriteUInt32(offset, (uint)(sftpDataLength + SFTP_MESSAGE_LENGTH_FIELD_LEN));
        }

    }
}
