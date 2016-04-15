/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: ReaderWriter.cs,v 1.6 2011/11/08 12:24:05 kzmi Exp $
*/
using System;
using System.Text;
using System.IO;

using Granados.PKI;
using Granados.Util;
using Granados.Mono.Math;

namespace Granados.IO {

    /// <summary>
    /// An interface of the SSH packet which provides a payload buffer.
    /// </summary>
    internal interface IPacketBuilder {
        /// <summary>
        /// Payload buffer.
        /// </summary>
        ByteBuffer Payload {
            get;
        }
    }

    /// <summary>
    /// Extension methods for writing content of the payload.
    /// </summary>
    internal static class PacketBuilderMixin {

        /// <summary>
        /// Writes binary data.
        /// </summary>
        public static T Write<T>(this T packet, byte[] data) where T : IPacketBuilder {
            packet.Payload.Append(data);
            return packet;
        }

        /// <summary>
        /// Writes single byte.
        /// </summary>
        public static T WriteByte<T>(this T packet, byte val) where T : IPacketBuilder {
            packet.Payload.WriteByte(val);
            return packet;
        }

        /// <summary>
        /// Writes boolean value as a byte.
        /// </summary>
        public static T WriteBool<T>(this T packet, bool val) where T : IPacketBuilder {
            packet.Payload.WriteByte(val ? (byte)1 : (byte)0);
            return packet;
        }

        /// <summary>
        /// Writes Int32 value in the network byte order.
        /// </summary>
        public static T WriteInt32<T>(this T packet, int val) where T : IPacketBuilder {
            packet.Payload.WriteInt32(val);
            return packet;
        }

        /// <summary>
        /// Writes UInt32 value in the network byte order.
        /// </summary>
        public static T WriteUInt32<T>(this T packet, uint val) where T : IPacketBuilder {
            packet.Payload.WriteUInt32(val);
            return packet;
        }

        /// <summary>
        /// Writes UInt64 value in the network byte order.
        /// </summary>
        public static T WriteUInt64<T>(this T packet, ulong val) where T : IPacketBuilder {
            packet.Payload.WriteUInt64(val);
            return packet;
        }

        /// <summary>
        /// Writes Int64 value in the network byte order.
        /// </summary>
        public static T WriteInt64<T>(this T packet, long val) where T : IPacketBuilder {
            packet.Payload.WriteInt64(val);
            return packet;
        }

        /// <summary>
        /// Writes ASCII text according to the SSH 1.5/2.0 specification.
        /// </summary>
        public static T WriteString<T>(this T packet, string text) where T : IPacketBuilder {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            WriteAsString(packet, bytes);
            return packet;
        }

        /// <summary>
        /// Writes byte string according to the SSH 1.5/2.0 specification.
        /// </summary>
        public static T WriteAsString<T>(this T packet, byte[] data) where T : IPacketBuilder {
            packet.Payload.WriteInt32(data.Length);
            if (data.Length > 0) {
                packet.Payload.Append(data);
            }
            return packet;
        }

        /// <summary>
        /// Writes byte string according to the SSH 1.5/2.0 specification.
        /// </summary>
        public static T WriteAsString<T>(this T packet, byte[] data, int offset, int length) where T : IPacketBuilder {
            packet.Payload.WriteInt32(length);
            if (length > 0) {
                packet.Payload.Append(data, offset, length);
            }
            return packet;
        }
    }

    ////////////////////////////////////////////////////////////
    /// read/write primitive types
    /// 
    internal abstract class SSHDataReader {

        protected byte[] _data;
        protected int _offset;
        protected int _limit;

        public SSHDataReader(byte[] image) {
            _data = image;
            _offset = 0;
            _limit = image.Length;
        }
        public SSHDataReader(DataFragment data) {
            Init(data);
        }
        public void Recycle(DataFragment data) {
            Init(data);
        }
        private void Init(DataFragment data) {
            _data = data.Data;
            _offset = data.Offset;
            _limit = _offset + data.Length;
        }

        public byte[] Image {
            get {
                return _data;
            }
        }
        public int Offset {
            get {
                return _offset;
            }
        }

        public int ReadInt32() {
            return (int)ReadUInt32();
        }

        public uint ReadUInt32() {
            if (_offset + 3 >= _limit)
                throw new IOException(Strings.GetString("UnexpectedEOF"));

            uint ret = (((uint)_data[_offset]) << 24) | (((uint)_data[_offset + 1]) << 16) | (((uint)_data[_offset + 2]) << 8) | (uint)_data[_offset + 3];

            _offset += 4;
            return ret;
        }

        public long ReadInt64() {
            return (long)ReadUInt64();
        }

        public ulong ReadUInt64() {
            if (_offset + 7 >= _limit)
                throw new IOException(Strings.GetString("UnexpectedEOF"));

            uint i1 = (((uint)_data[_offset]) << 24) | (((uint)_data[_offset + 1]) << 16) | (((uint)_data[_offset + 2]) << 8) | (uint)_data[_offset + 3];
            uint i2 = (((uint)_data[_offset + 4]) << 24) | (((uint)_data[_offset + 5]) << 16) | (((uint)_data[_offset + 6]) << 8) | (uint)_data[_offset + 7];

            _offset += 8;
            return ((ulong)i1 << 32) | (ulong)i2;
        }

        public byte ReadByte() {
            if (_offset >= _limit)
                throw new IOException(Strings.GetString("UnexpectedEOF"));
            return _data[_offset++];
        }
        public bool ReadBool() {
            return ReadByte() != 0 ? true : false;
        }
        /**
        * multi-precise integer
        */
        public abstract BigInteger ReadMPInt();

        public byte[] ReadString() {
            int length = ReadInt32();
            return Read(length);
        }

        public byte[] Read(int length) {
            byte[] image = new byte[length];
            if (_offset + length > _limit)
                throw new IOException(Strings.GetString("UnexpectedEOF"));
            Array.Copy(_data, _offset, image, 0, length);
            _offset += length;
            return image;
        }

        public byte[] ReadAll() {
            byte[] t = new byte[_limit - _offset];
            Array.Copy(_data, _offset, t, 0, t.Length);
            _offset = _limit;
            return t;
        }

        public int Rest {
            get {
                return _limit - _offset;
            }
        }
    }

    internal abstract class SSHDataWriter : IKeyWriter {
        protected SimpleMemoryStream _strm;

        public SSHDataWriter() {
            _strm = new SimpleMemoryStream();
        }

        public byte[] ToByteArray() {
            return _strm.ToNewArray();
        }

        public int Length {
            get {
                return _strm.Length;
            }
        }
        public void Reset() {
            _strm.Reset();
        }
        public void SetOffset(int value) {
            _strm.SetOffset(value);
        }
        public byte[] UnderlyingBuffer {
            get {
                return _strm.UnderlyingBuffer;
            }
        }

        public void Write(byte[] data) {
            _strm.Write(data, 0, data.Length);
        }
        public void Write(byte[] data, int offset, int count) {
            _strm.Write(data, offset, count);
        }
        public void WriteByte(byte data) {
            _strm.WriteByte(data);
        }
        public void WriteBool(bool data) {
            _strm.WriteByte(data ? (byte)1 : (byte)0);
        }

        public void WriteInt32(int data) {
            WriteUInt32((uint)data);
        }

        public void WriteUInt32(uint data) {
            _strm.WriteByte((byte)(data >> 24));
            _strm.WriteByte((byte)(data >> 16));
            _strm.WriteByte((byte)(data >> 8));
            _strm.WriteByte((byte)data);
        }

        public void WriteInt64(long data) {
            WriteUInt64((ulong)data);
        }

        public void WriteUInt64(ulong data) {
            WriteUInt32((uint)(data >> 32));
            WriteUInt32((uint)data);
        }

        public abstract void WriteBigInteger(BigInteger data);

        public void WriteString(string data) {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            WriteInt32(bytes.Length);
            if (bytes.Length > 0)
                Write(bytes);
        }

        public void WriteAsString(byte[] data) {
            WriteInt32(data.Length);
            if (data.Length > 0)
                Write(data);
        }
        public void WriteAsString(byte[] data, int offset, int length) {
            WriteInt32(length);
            if (length > 0)
                Write(data, offset, length);
        }
    }
}

namespace Granados.IO.SSH1 {
    using Granados.SSH1;

    internal class SSH1DataReader : SSHDataReader {

        public SSH1DataReader(byte[] image)
            : base(image) {
        }
        public SSH1DataReader(DataFragment data)
            : base(data) {
        }

        public SSH1PacketType ReadPacketType() {
            return (SSH1PacketType)this.ReadByte();
        }

        public override BigInteger ReadMPInt() {
            //first 2 bytes describes the bit count
            int bits = (((int)_data[_offset]) << 8) + _data[_offset + 1];
            _offset += 2;

            return new BigInteger(Read((bits + 7) / 8));
        }
    }

    internal class SSH1DataWriter : SSHDataWriter {
        public override void WriteBigInteger(BigInteger data) {
            byte[] image = data.GetBytes();
            int len = image.Length * 8;

            int a = len & 0x0000FF00;
            a >>= 8;
            _strm.WriteByte((byte)a);

            a = len & 0x000000FF;
            _strm.WriteByte((byte)a);

            _strm.Write(image, 0, image.Length);
        }
    }
}

namespace Granados.IO.SSH2 {
    using Granados.SSH2;

    internal class SSH2DataReader : SSHDataReader {

        public SSH2DataReader(byte[] image)
            : base(image) {
        }
        public SSH2DataReader(DataFragment data)
            : base(data) {
        }

        //SSH2 Key File Only
        public BigInteger ReadBigIntWithBits() {
            int bits = ReadInt32();
            int bytes = (bits + 7) / 8;
            return new BigInteger(Read(bytes));
        }
        public override BigInteger ReadMPInt() {
            return new BigInteger(ReadString());
        }
        public PacketType ReadPacketType() {
            return (PacketType)ReadByte();
        }
    }

    internal class SSH2DataWriter : SSHDataWriter {
        //writes mpint in SSH2 format
        public override void WriteBigInteger(BigInteger data) {
            byte[] t = data.GetBytes();
            int len = t.Length;
            if (t[0] >= 0x80) {
                WriteInt32(++len);
                WriteByte((byte)0);
            }
            else
                WriteInt32(len);
            Write(t);
        }

        public void WriteBigIntWithBits(BigInteger bi) {
            WriteInt32(bi.BitCount());
            Write(bi.GetBytes());
        }

        public void WritePacketType(PacketType pt) {
            WriteByte((byte)pt);
        }
    }
}
