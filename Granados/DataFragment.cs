/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

  I implemented this algorithm with reference to following products and books though the algorithm is known publicly.
    * MindTerm ( AppGate Network Security )
    * Applied Cryptography ( Bruce Schneier )

 $Id: DataFragment.cs,v 1.4 2012/03/10 18:00:15 kzmi Exp $
*/
using System;
using System.Diagnostics;
using Granados.Util;

namespace Granados.IO {
    /// <summary>
    /// DataFragment represents one or more tuples of (byte[], offset, length).
    /// To reduce memory usage, the source byte[] will not be copied.
    /// If this behavior is not convenient, call Isolate() method.
    /// </summary>
    /// <exclude/>
    public class DataFragment {
        private byte[] _data;
        private int _offset;
        private int _length;

        public DataFragment(byte[] data, int offset, int length) {
            Init(data, offset, length);
        }
        public DataFragment(int capacity) {
            _data = new byte[capacity];
            _offset = 0;
            _length = 0;
        }

        public int Length {
            get {
                return _length;
            }
        }
        public int Capacity {
            get {
                return _data.Length;
            }
        }
        public int Offset {
            get {
                return _offset;
            }
        }
        public byte[] Data {
            get {
                return _data;
            }
        }

        public byte ByteAt(int offset) {
            return _data[_offset + offset];
        }

        public void Append(byte[] data, int offset, int length) {
            if (_length == 0) {
                _offset = 0;
            }

            int dataSize = _offset + _length;
            AssureCapacity(dataSize + length);
            Buffer.BlockCopy(data, offset, _data, dataSize, length);
            _length += length;
        }

        public void Append(DataFragment data) {
            Append(data.Data, data.Offset, data.Length);
        }

        //reuse this instance
        public void Init(byte[] data, int offset, int length) {
            _data = data;
            _offset = offset;
            _length = length;
        }

        //clear
        public void Clear() {
            _offset = 0;
            _length = 0;
        }

        public virtual DataFragment Isolate() {
            int newcapacity = RoundUp(_length);
            byte[] t = new byte[newcapacity];
            Buffer.BlockCopy(_data, _offset, t, 0, _length);
            DataFragment f = new DataFragment(t, 0, _length);
            return f;
        }

        //be careful!
        public void Consume(int length) {
            _offset += length;
            _length -= length;
            Debug.Assert(_length >= 0);
        }
        //be careful!
        public void SetLength(int offset, int length) {
            _offset = offset;
            _length = length;
            Debug.Assert(_offset + _length <= this.Capacity);
        }

        public void AssureCapacity(int size) {
            size = RoundUp(size);
            if (_data.Length < size) {
                byte[] t = new byte[size];
                Buffer.BlockCopy(_data, 0, t, 0, _data.Length);
                _data = t;
            }
        }

        public byte[] ToNewArray() {
            byte[] t = new byte[_length];
            Buffer.BlockCopy(_data, _offset, t, 0, _length);
            return t;
        }

        private static int RoundUp(int size) {
            if (size <= 16)
                return 16;
            size--;
            size |= size >> 1;
            size |= size >> 2;
            size |= size >> 4;
            size |= size >> 8;
            size |= size >> 16;
            return size + 1;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class SimpleMemoryStream {
        private byte[] _buffer;
        private int _offset;

        public SimpleMemoryStream(int capacity) {
            Init(capacity);
        }
        public SimpleMemoryStream() {
            Init(512);
        }
        private void Init(int capacity) {
            _buffer = new byte[capacity];
            Reset();
        }

        public int Length {
            get {
                return _offset;
            }
        }
        public byte[] UnderlyingBuffer {
            get {
                return _buffer;
            }
        }
        public void Reset() {
            _offset = 0;
        }
        public void SetOffset(int value) {
            _offset = value;
        }
        public byte[] ToNewArray() {
            byte[] r = new byte[_offset];
            Buffer.BlockCopy(_buffer, 0, r, 0, _offset);
            return r;
        }

        private void AssureSize(int size) {
            if (_buffer.Length < size) {
                byte[] t = new byte[Math.Max(size, _buffer.Length * 2)];
                Buffer.BlockCopy(_buffer, 0, t, 0, _buffer.Length);
                _buffer = t;
            }
        }

        public void Write(byte[] data, int offset, int length) {
            AssureSize(_offset + length);
            Buffer.BlockCopy(data, offset, _buffer, _offset, length);
            _offset += length;
        }
        public void Write(byte[] data) {
            Write(data, 0, data.Length);
        }
        public void Write(DataFragment data) {
            Write(data.Data, data.Offset, data.Length);
        }
        public void WriteByte(byte b) {
            AssureSize(_offset + 1);
            _buffer[_offset++] = b;
        }
    }
}
