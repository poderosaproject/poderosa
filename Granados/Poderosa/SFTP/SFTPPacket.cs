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
    /// SSH2TransmissionPacket for constructing SFTP packet
    /// </summary>
    internal class SFTPPacket : SSH2TransmissionPacket {

        private const int OFFSET_CHANNEL_DATA_LENGTH = SSH2TransmissionPacket.INITIAL_OFFSET + 5;
        private const int OFFSET_SFTP_DATA_LENGTH = OFFSET_CHANNEL_DATA_LENGTH + 4;
        private const int OFFSET_SFTP_PACKET_TYPE = OFFSET_SFTP_DATA_LENGTH + 4;

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPPacket() {
        }

        /// <summary>
        /// Overrides Open()
        /// </summary>
        public override void Open() {
            throw new NotSupportedException("use Open(SFTPPacketType, int)");
        }

        /// <summary>
        /// Open packet with specifying a packet type.
        /// </summary>
        /// <param name="packetType">SFTP packet type.</param>
        /// <param name="remoteChannel">remote channel number</param>
        public void Open(SFTPPacketType packetType, int remoteChannel) {
            base.Open();
            SSH2DataWriter writer = DataWriter;
            writer.WritePacketType(Granados.SSH2.PacketType.SSH_MSG_CHANNEL_DATA);
            writer.WriteInt32(remoteChannel);
            writer.SetOffset(OFFSET_SFTP_PACKET_TYPE);
            writer.WriteByte((byte)packetType);
        }

        /// <summary>
        /// Overrides Close()
        /// </summary>
        /// <param name="cipher"></param>
        /// <param name="rnd"></param>
        /// <param name="mac"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public override DataFragment Close(Cipher cipher, Random rnd, MAC mac, int sequence) {
            byte[] buf = DataWriter.UnderlyingBuffer;
            int sftpDataLength = DataWriter.Length - OFFSET_SFTP_PACKET_TYPE;
            SSHUtil.WriteIntToByteArray(buf, OFFSET_CHANNEL_DATA_LENGTH, sftpDataLength + 4);
            SSHUtil.WriteIntToByteArray(buf, OFFSET_SFTP_DATA_LENGTH, sftpDataLength);
            return base.Close(cipher, rnd, mac, sequence);
        }
    }
}
