// Copyright 2004-2016 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.

using Poderosa.Protocols;

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Poderosa.XZModem {

    /// <summary>
    /// ZMODEM protocol base class
    /// </summary>
    internal abstract class ZModem : ModemBase {
        // zmodem packet type(1byte)
        protected const byte ZPAD = (byte)'*';
        protected const byte ZDLE = 0x18;
        protected const byte ZDLEE = 0x58;
        protected const byte ZBIN = (byte)'A';
        protected const byte ZHEX = (byte)'B';
        protected const byte ZBIN32 = (byte)'C';

        protected const byte ZRQINIT = 0;
        protected const byte ZRINIT = 1;
        protected const byte ZSINIT = 2;
        protected const byte ZACK = 3;
        protected const byte ZFILE = 4;
        protected const byte ZSKIP = 5;
        protected const byte ZNAK = 6;
        protected const byte ZABORT = 7;
        protected const byte ZFIN = 8;
        protected const byte ZRPOS = 9;
        protected const byte ZDATA = 10;
        protected const byte ZEOF = 11;
        protected const byte ZFERR = 12;
        protected const byte ZCRC = 13;
        protected const byte ZCHALLENGE = 14;
        protected const byte ZCOMPL = 15;
        protected const byte ZCAN = 16;
        protected const byte ZFREECNT = 17;
        protected const byte ZCOMMAND = 18;
        protected const byte ZSTDERR = 19;

        protected const byte ZCRCE = (byte)'h';
        protected const byte ZCRCG = (byte)'i';
        protected const byte ZCRCQ = (byte)'j';
        protected const byte ZCRCW = (byte)'k';
        protected const byte ZRUB0 = (byte)'l';
        protected const byte ZRUB1 = (byte)'m';

        protected const int ZF0 = 3;
        protected const int ZF1 = 2;
        protected const int ZF2 = 1;
        protected const int ZF3 = 0;
        protected const int ZP0 = 0;
        protected const int ZP1 = 1;
        protected const int ZP2 = 2;
        protected const int ZP3 = 3;

        protected const byte CANFDX = 0x01;
        protected const byte CANOVIO = 0x02;
        protected const byte CANBRK = 0x04;
        protected const byte CANCRY = 0x08;
        protected const byte CANLZW = 0x10;
        protected const byte CANFC32 = 0x20;
        protected const byte ESCCTL = 0x40;
        protected const byte ESC8 = 0x80;

        protected const byte ZCBIN = 1;
        protected const byte ZCNL = 2;

        protected const byte CR = 0x0d;
        protected const byte LF = 0x0a;
        protected const byte XON = 0x11;
        protected const byte XOFF = 0x13;
        protected const byte CAN = 0x18;
        protected const byte BS = 0x8;

        protected const int MAX_BLOCK = 8192;

        protected enum CRCType {
            CRC16,
            CRC32,
        }

        private enum State {
            None,
            Error,

            // for sending
            WaitingZPAD,
            WaitingZDLE,
            GetHeaderFormat,
            GetBinaryData,
            GetHexData,
            GetHexEOL,

            // for receiving
            GetFileInfo,
            GetFileInfoCRC,
            GetFileData,
            GetFileDataCRC,
        }

        protected struct Header {
            public readonly byte Type;
            public readonly byte ZP0;
            public readonly byte ZP1;
            public readonly byte ZP2;
            public readonly byte ZP3;

            public byte ZF3 {
                get {
                    return ZP0;
                }
            }
            public byte ZF2 {
                get {
                    return ZP1;
                }
            }
            public byte ZF1 {
                get {
                    return ZP2;
                }
            }
            public byte ZF0 {
                get {
                    return ZP3;
                }
            }

            public Header(byte[] buf) {
                Type = buf[0];
                ZP0 = buf[1];
                ZP1 = buf[2];
                ZP2 = buf[3];
                ZP3 = buf[4];
            }

            public Header(byte type, byte zf0 = 0, byte zf1 = 0, byte zf2 = 0, byte zf3 = 0) {
                Type = type;
                ZP0 = zf3;
                ZP1 = zf2;
                ZP2 = zf1;
                ZP3 = zf0;
            }

            public Header(byte type, int pos) {
                Type = type;
                ZP0 = (byte)pos;
                ZP1 = (byte)(pos >> 8);
                ZP2 = (byte)(pos >> 16);
                ZP3 = (byte)(pos >> 24);
            }
        }

        protected readonly XZModemDialog _parent;

        // packet buffer
        private readonly byte[] _rcvPacket = new byte[MAX_BLOCK + 6];   // data + ZDLE + ZCRCx + CRC*4
        // packet length
        private int _rcvPacketLen;
        // byte count to be read
        private int _bytesNeeded;

        // buffer for sending packet
        private byte[] _sndBuff = new byte[21]; // for hex header

        // current receiving state
        private State _state = State.None;

        // flag for receiving hex digits character
        private bool _hexLo;
        // for ZDLE handling
        private bool _gotZDLE;
        // abort sequence detection
        private int _canCount;
        // current CRC type
        private CRCType _crcType = CRCType.CRC16;

        // a flag for preventing multiple closing of the instance
        private bool _closed;

        public override string Caption {
            get {
                return "ZMODEM";
            }
        }

        protected ZModem(XZModemDialog parent) {
            _parent = parent;
        }

        protected void StartListening() {
            _state = State.WaitingZPAD;
        }

        protected int PutHex(byte[] data, int index, byte b) {
            // b => ['0'-'9'|'a'-'f']['0'-'9'|'a'-'f']
            if (b <= 0x9f) {
                data[index] = (byte)((b >> 4) + 0x30);
            }
            else {
                data[index] = (byte)((b >> 4) + 0x57);
            }
            index++;
            if ((b & 0x0F) <= 0x09) {
                data[index] = (byte)((b & 0x0F) + 0x30);
            }
            else {
                data[index] = (byte)((b & 0x0F) + 0x57);
            }
            index++;

            return (index);
        }

        protected int PutBin(byte[] data, int index, byte b) {
            switch (b) {
                case 0x0d:  // CR
                case 0x10:  // DLE
                case 0x11:  // XON
                case 0x13:  // XOFF
                case 0x16:  // SYN
                case 0x18:  // ZDLE
                case 0x8d:  // CR | PARITY
                case 0x90:  // DLE | PARITY
                case 0x91:  // XON | PARITY
                case 0x93:  // XOFF | PARITY
                case 0x96:  // SYN | PARITY
                    data[index++] = ZDLE;
                    b ^= 0x40;
                    break;

                default:
                    break;
            }
            data[index++] = b;

            return (index);
        }

        protected int PutCRC16(byte[] data, int index, ushort crc) {
            index = PutBin(data, index, (byte)(crc >> 8));
            index = PutBin(data, index, (byte)(crc));
            return index;
        }

        protected int PutCRC32(byte[] data, int index, uint crc) {
            index = PutBin(data, index, (byte)(crc));
            index = PutBin(data, index, (byte)(crc >> 8));
            index = PutBin(data, index, (byte)(crc >> 16));
            index = PutBin(data, index, (byte)(crc >> 24));
            return index;
        }

        // サーバからの受信データを解析する
        // Readerクラスから呼ばれる
        public override void OnReception(ByteDataFragment fragment) {
            byte[] data = fragment.Buffer;
            int offset = fragment.Offset;
            int length = fragment.Length;

            //Debug.WriteLine(String.Format("OnReception len={0} state={1}", length, _state.ToString()));

            if (_state == State.None || _state == State.Error) {
                return;
            }

            string errorMessage = null;

            for (int i = 0; i < length; i++) {
                byte c = data[offset + i];

                // abort sequence detection
                if (c == CAN) {
                    _canCount++;
                    if (_canCount > 5) {
                        _state = State.None;    // don't accept any more
                        ProcessAbortByPeer();
                        return;
                    }
                }
                else {
                    _canCount = 0;
                }

                // 0x11, 0x13, 0x81, 0x83は無視する
                if ((c & 0x7f) == XON || (c & 0x7f) == XOFF)
                    continue;

            CheckByte:
                switch (_state) {
                    case State.WaitingZPAD: {
                            switch (c) {
                                case ZPAD:
                                    _state = State.WaitingZDLE;
                                    break;
                                default:
                                    break;
                            }
                        }
                        break;
                    case State.WaitingZDLE: {
                            switch (c) {
                                case ZPAD:
                                    break;
                                case ZDLE:
                                    _state = State.GetHeaderFormat;
                                    break;
                                default:
                                    _state = State.WaitingZPAD;
                                    break;
                            }
                        }
                        break;
                    case State.GetHeaderFormat: {
                            switch (c) {
                                case ZBIN:
                                    //Debug.WriteLine("ZBIN");
                                    _crcType = CRCType.CRC16;
                                    _bytesNeeded = 7;
                                    _state = State.GetBinaryData;
                                    break;

                                case ZHEX:
                                    //Debug.WriteLine("ZHEX");
                                    _crcType = CRCType.CRC16;
                                    _bytesNeeded = 7;
                                    _state = State.GetHexData;
                                    break;

                                case ZBIN32:
                                    //Debug.WriteLine("ZBIN32");
                                    _crcType = CRCType.CRC32;
                                    _bytesNeeded = 9;
                                    _state = State.GetBinaryData;
                                    break;

                                default:
                                    _state = State.WaitingZPAD;
                                    break;
                            }
                            // initialize variables
                            _rcvPacketLen = 0;
                            _hexLo = false;
                            _gotZDLE = false;
                        }
                        break;
                    case State.GetBinaryData: { // binary('A') or binary('C') data
                            if (_gotZDLE) {
                                // unescape
                                _rcvPacket[_rcvPacketLen++] = (byte)(c ^ 0x40);
                                _bytesNeeded--;
                                _gotZDLE = false;
                            }
                            else if (c == ZDLE) {
                                _gotZDLE = true;
                            }
                            else {
                                _rcvPacket[_rcvPacketLen++] = c;
                                _bytesNeeded--;
                                _gotZDLE = false;
                            }

                            if (_bytesNeeded <= 0) {
                                _state = State.WaitingZPAD;
                                Header hdr;
                                if (CheckHeader(_crcType, _rcvPacket, _rcvPacketLen, out hdr)) {
                                    ProcessHeader(hdr);
                                    if (hdr.Type == ZDATA) {
                                        _state = State.GetFileData;
                                        _rcvPacketLen = 0;
                                        _gotZDLE = false;
                                    }
                                    else if (hdr.Type == ZFILE) {
                                        _state = State.GetFileInfo;
                                        _rcvPacketLen = 0;
                                        _gotZDLE = false;
                                    }
                                }
                            }
                        }
                        break;
                    case State.GetHexData: {  // HEX('B') data
                            if ((c >= '0') && (c <= '9')) {
                                c -= 0x30;
                            }
                            else if ((c >= 'a') && (c <= 'f')) {
                                c -= 0x57;
                            }
                            else {
                                Debug.WriteLine("Unexpected character in {0}", _state);
                                errorMessage = XZModemPlugin.Instance.Strings.GetString("Message.ZModem.InvalidHeader");
                                goto Error;
                            }

                            if (_hexLo) {  // lower
                                _rcvPacket[_rcvPacketLen++] |= c;
                                _hexLo = false;
                                _bytesNeeded--;

                                if (_bytesNeeded <= 0) {
                                    Header hdr;
                                    if (CheckHeader(_crcType, _rcvPacket, _rcvPacketLen, out hdr)) {
                                        ProcessHeader(hdr);
                                        _state = State.GetHexEOL;
                                        _bytesNeeded = 2;    // CR LF
                                    }
                                    else {
                                        _state = State.WaitingZPAD;
                                    }
                                }
                            }
                            else {  // upper
                                _rcvPacket[_rcvPacketLen] = (byte)(c << 4);
                                _hexLo = true;
                            }
                        }
                        break;
                    case State.GetHexEOL: {
                            byte cc = (byte)(c & 0x7f); // sz sends { 0x0d, 0x8a } as CR/LF
                            if (cc == 0x0a || cc == 0x0d) {
                                _bytesNeeded--;
                                if (_bytesNeeded <= 0) {
                                    _state = State.WaitingZPAD;
                                }
                            }
                            else {
                                _state = State.WaitingZPAD;
                                goto CheckByte;
                            }
                        }
                        break;

                    case State.GetFileInfo:
                    case State.GetFileData: {
                            if (_rcvPacketLen >= _rcvPacket.Length) {
                                Debug.WriteLine("Buffer full in {0}", _state);
                                errorMessage = XZModemPlugin.Instance.Strings.GetString("Message.ZModem.BufferFull");
                                goto Error;
                            }
                            if (_gotZDLE) {
                                if (c == ZCRCE || c == ZCRCG || c == ZCRCQ || c == ZCRCW) {
                                    // end of frame. need CRC bytes.
                                    _rcvPacket[_rcvPacketLen++] = c;
                                    _gotZDLE = false;
                                    _bytesNeeded = (_crcType == CRCType.CRC32) ? 4 : 2;    // CRC bytes
                                    _state = (_state == State.GetFileInfo) ? State.GetFileInfoCRC :
                                            (_state == State.GetFileData) ? State.GetFileDataCRC : State.Error;
                                }
                                else {
                                    // unescape
                                    _rcvPacket[_rcvPacketLen++] = (byte)(c ^ 0x40);
                                    _gotZDLE = false;
                                }
                            }
                            else if (c == ZDLE) {
                                _gotZDLE = true;
                            }
                            else {
                                _rcvPacket[_rcvPacketLen++] = c;
                                _gotZDLE = false;
                            }
                        }
                        break;
                    case State.GetFileInfoCRC:
                    case State.GetFileDataCRC: {
                            if (_rcvPacketLen >= _rcvPacket.Length) {
                                Debug.WriteLine("Buffer full in {0}", _state);
                                errorMessage = XZModemPlugin.Instance.Strings.GetString("Message.ZModem.BufferFull");
                                goto Error;
                            }
                            if (_gotZDLE) {
                                // unescape
                                _rcvPacket[_rcvPacketLen++] = (byte)(c ^ 0x40);
                                _bytesNeeded--;
                                _gotZDLE = false;
                            }
                            else if (c == ZDLE) {
                                _gotZDLE = true;
                            }
                            else {
                                _rcvPacket[_rcvPacketLen++] = c;
                                _bytesNeeded--;
                                _gotZDLE = false;
                            }

                            if (_bytesNeeded <= 0) {
                                int dataLen = _rcvPacketLen - (_crcType == CRCType.CRC32 ? 5 : 3);
                                if (_state == State.GetFileInfoCRC) {
                                    if (CheckCRC(_crcType, _rcvPacket, _rcvPacketLen)) {
                                        ParseFileInfo(_rcvPacket, 0, dataLen);
                                        _rcvPacketLen = 0;
                                        _gotZDLE = false;
                                        _state = State.WaitingZPAD;
                                    }
                                    else {
                                        Debug.WriteLine("CRC Error in {0}", _state);
                                        errorMessage = XZModemPlugin.Instance.Strings.GetString("Message.ZModem.CRCError");
                                        goto Error;
                                    }
                                }
                                else if (_state == State.GetFileDataCRC) {
                                    byte frameType = _rcvPacket[dataLen];
                                    //Debug.WriteLine("frameType = 0x{0:x2}", frameType);

                                    if (CheckCRC(_crcType, _rcvPacket, _rcvPacketLen)) {
                                        ProcessFileData(_rcvPacket, 0, dataLen);
                                        _rcvPacketLen = 0;
                                        _gotZDLE = false;
                                        if (frameType == ZCRCE) {
                                            // finished
                                            _state = State.WaitingZPAD;
                                        }
                                        else if (frameType == ZCRCW) {
                                            SendACK();
                                            // read next subpacket
                                            _state = State.WaitingZPAD;
                                        }
                                        else {
                                            // read next subpacket
                                            _state = State.GetFileData;
                                        }
                                    }
                                    else {
                                        Debug.WriteLine("CRC Error in {0}", _state);
                                        errorMessage = XZModemPlugin.Instance.Strings.GetString("Message.ZModem.CRCError");
                                        goto Error;
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            return;

        Error:
            _state = State.Error;
            Abort(errorMessage);
        }

        private bool CheckCRC(CRCType crcType, byte[] data, int len) {
            if (crcType == CRCType.CRC32) {
                uint crc = Crc32.Update(Crc32.InitialValue, data, 0, len - 4) ^ Crc32.XorValue;
                //Debug.WriteLine("CRC32: {0:x8}", crc);
                uint crcfld = (((uint)data[len - 4]) |
                               ((uint)data[len - 3] << 8) |
                               ((uint)data[len - 2] << 16) |
                               ((uint)data[len - 1] << 24));
                return crc == crcfld;
            }
            else {
                // CRC16
                ushort crc = Crc16.Update(Crc16.InitialValue, data, 0, len - 2);
                //Debug.WriteLine("CRC16: {0:x4}", crc);
                ushort crcfld = (ushort)((data[len - 2] << 8) | data[len - 1]);
                return crc == crcfld;
            }
        }

        private bool CheckHeader(CRCType crcType, byte[] data, int len, out Header hdr) {
            if (CheckCRC(crcType, data, len)) {
                hdr = new Header(data);
                return true;
            }
            else {
                hdr = new Header();
                return false;
            }
        }

        protected void SendPacket(byte[] data, int len) {
            _connection.Socket.Transmit(data, 0, len);
        }

        protected int BuildHEXHeader(byte[] data, Header hdr) {
            data[0] = ZPAD;
            data[1] = ZPAD;
            data[2] = ZDLE;
            data[3] = ZHEX;
            int index = 4;
            index = PutHex(data, index, hdr.Type);
            ushort crc = Crc16.Update(Crc16.InitialValue, hdr.Type);
            index = PutHex(data, index, hdr.ZP0);
            crc = Crc16.Update(crc, hdr.ZP0);
            index = PutHex(data, index, hdr.ZP1);
            crc = Crc16.Update(crc, hdr.ZP1);
            index = PutHex(data, index, hdr.ZP2);
            crc = Crc16.Update(crc, hdr.ZP2);
            index = PutHex(data, index, hdr.ZP3);
            crc = Crc16.Update(crc, hdr.ZP3);
            index = PutHex(data, index, (byte)(crc >> 8));
            index = PutHex(data, index, (byte)(crc));
            data[index++] = CR;
            data[index++] = LF | 0x80;  // sz/rz send 8a instead of 0a

            if (!(hdr.Type == ZFIN || hdr.Type == ZACK)) {
                data[index++] = XON;
            }

            return index;
        }

        protected int BuildBin16Header(byte[] data, Header hdr) {
            data[0] = ZPAD;
            data[1] = ZDLE;
            data[2] = ZBIN;
            int index = 3;
            index = PutBin(data, index, hdr.Type);
            ushort crc = Crc16.Update(Crc16.InitialValue, hdr.Type);
            index = PutBin(data, index, hdr.ZP0);
            crc = Crc16.Update(crc, hdr.ZP0);
            index = PutBin(data, index, hdr.ZP1);
            crc = Crc16.Update(crc, hdr.ZP1);
            index = PutBin(data, index, hdr.ZP2);
            crc = Crc16.Update(crc, hdr.ZP2);
            index = PutBin(data, index, hdr.ZP3);
            crc = Crc16.Update(crc, hdr.ZP3);
            index = PutCRC16(data, index, crc);
            return index;
        }

        protected int BuildBin32Header(byte[] data, Header hdr) {
            data[0] = ZPAD;
            data[1] = ZDLE;
            data[2] = ZBIN32;
            int index = 3;
            index = PutBin(data, index, hdr.Type);
            uint crc = Crc32.Update(Crc32.InitialValue, hdr.Type);
            index = PutBin(data, index, hdr.ZP0);
            crc = Crc32.Update(crc, hdr.ZP0);
            index = PutBin(data, index, hdr.ZP1);
            crc = Crc32.Update(crc, hdr.ZP1);
            index = PutBin(data, index, hdr.ZP2);
            crc = Crc32.Update(crc, hdr.ZP2);
            index = PutBin(data, index, hdr.ZP3);
            crc = Crc32.Update(crc, hdr.ZP3);
            crc ^= Crc32.XorValue;
            index = PutCRC32(data, index, crc);
            return index;
        }

        protected void SendACK() {
            int pktOutCount = BuildHEXHeader(_sndBuff, new Header(ZACK));
            SendPacket(_sndBuff, pktOutCount);
        }

        private readonly byte[] _abortSeq = { CAN, CAN, CAN, CAN, CAN, CAN, CAN, CAN, CAN, CAN, BS, BS, BS, BS, BS, BS, BS, BS, BS, BS, };

        protected void SendAbortSequence() {
            SendPacket(_abortSeq, _abortSeq.Length);
        }

        // Process received header
        protected abstract void ProcessHeader(Header hdr);

        // Process subpacket of the ZFILE
        private void ParseFileInfo(byte[] data, int offset, int length) {
            // we don't use these informations.
            /*
            byte[] filename = new byte[1024];
            byte c;
            string fname;
            int i, n;
            int size;

            n = 0;
            for (i = 0; i < length; i++) {
                c = data[offset + i];
                if (c != 0x00) {
                    filename[n++] = c;
                }
                else {
                    break;
                }
            }
            fname = Encoding.ASCII.GetString(filename, 0, n);
             */
        }

        // Process subpacket of the ZDATA
        protected abstract void ProcessFileData(byte[] data, int offset, int length);

        // Process abort sequence
        protected abstract void ProcessAbortByPeer();

        // Additional tasks for aborting the protocol
        protected abstract void OnAbort();

        // Called when the protocol is going to be stopped
        protected abstract void OnStop();

        public override void Abort() {
            Abort(null);
        }

        private void Abort(string message) {
            if (_closed) {
                return;
            }
            OnAbort();
            Cancel(message);
        }

        protected void Cancel(string message) {
            if (_closed) {
                return;
            }
            _closed = true;
            OnStop();
            // pending UI tasks have to be processed before the dialog is closed.
            DoUIEvents();
            _site.Cancel(message);
            _parent.AsyncClose();
            Dispose();
        }

        protected void Complete(string message) {
            if (_closed) {
                return;
            }
            _closed = true;
            _site.MainWindow.Information(message);
            OnStop();
            // pending UI tasks have to be processed before the dialog is closed.
            DoUIEvents();
            _site.Complete();
            _parent.AsyncClose();
            Dispose();
        }

        private void DoUIEvents() {
            if (_parent.InvokeRequired) {
                _parent.Invoke((Action)(() => {
                    // do nothing
                }));
            }
            else {
                Application.DoEvents();
            }
        }
    }


    /// <summary>
    /// ZMODEM sender
    /// </summary>
    internal class ZModemSender : ZModem {
        private readonly string _fileName;
        private readonly int _fileSize;
        private readonly byte[] _sndBuff = new byte[MAX_BLOCK * 2 + 10];    // data(escaped) + ZDLE + ZCRCx + CRC(escaped)*4

        private Task _sendingTask;
        private readonly CancellationTokenSource _cancellation = new CancellationTokenSource();

        private FileStream _fileStream;
        private int _frameSize = 1024;
        private CRCType _txCrcType = CRCType.CRC16;

        private int _filePosReq;
        private bool _filePosReqChanged;

        private bool _afterZRPOS;
        private bool _fileSkipped;
        private bool _stopped;

        public ZModemSender(XZModemDialog parent, string filename)
            : base(parent) {
            _fileName = filename;
            _fileSize = (int)new FileInfo(filename).Length;
            _fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }

        public override bool IsReceivingTask {
            get {
                return false;
            }
        }

        public override void Start() {
            StartListening();
            Task.Run(() => {
                SendRZ();
                Thread.Sleep(500);
                SendZRQInit();
            });
        }

        protected override void OnAbort() {
            StopSendingTask(false);
            SendAbortSequence();
        }

        protected override void OnStop() {
            _stopped = true;
            StopSendingTask(false);
        }

        private void StopSendingTask(bool wait) {
            if (_sendingTask != null && !_sendingTask.IsCompleted) {
                _cancellation.Cancel();
                if (wait) {
                    // Note:
                    // if this thread was the UI thread, this waiting may cause the deadlock
                    // because the sending task calls Invoke() in the worker thread.
                    _sendingTask.Wait();
                }
            }
        }

        // "rz\r"
        private void SendRZ() {
            if (!_stopped) {
                _sndBuff[0] = (byte)'r';
                _sndBuff[1] = (byte)'z';
                _sndBuff[2] = (byte)'\r';
                SendPacket(_sndBuff, 3);
            }
        }

        // ZRQINIT
        private void SendZRQInit() {
            if (!_stopped) {
                int pktOutCount = BuildHEXHeader(_sndBuff, new Header(ZRQINIT, pos: 0));
                SendPacket(_sndBuff, pktOutCount);
            }
        }

        // ZFILE
        private void SendZFILE() {
            if (_stopped) {
                return;
            }

            Header hdr = new Header(ZFILE, zf0: ZCBIN);
            int pktOutCount =
                (_txCrcType == CRCType.CRC32) ?
                    BuildBin32Header(_sndBuff, hdr) :
                    BuildBin16Header(_sndBuff, hdr);
            SendPacket(_sndBuff, pktOutCount);

            string fn = Path.GetFileName(_fileName);
            fn = fn.Replace(" ", "_");

            string data = fn + '\0' + _fileSize.ToString() + '\0';
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            int index = 0;
            foreach (byte b in bytes) {
                index = PutBin(_sndBuff, index, b);
            }
            uint crc =
                (_txCrcType == CRCType.CRC32) ?
                    Crc32.Update(Crc32.InitialValue, bytes, 0, bytes.Length) :
                    Crc16.Update(Crc16.InitialValue, bytes, 0, bytes.Length);

            _sndBuff[index++] = ZDLE;
            _sndBuff[index++] = ZCRCW;
            crc = (_txCrcType == CRCType.CRC32) ?
                    Crc32.Update(crc, ZCRCW) ^ Crc32.XorValue :
                    Crc16.Update((ushort)crc, ZCRCW);

            if (_txCrcType == CRCType.CRC32) {
                index = PutCRC32(_sndBuff, index, crc);
            }
            else {
                index = PutCRC16(_sndBuff, index, (ushort)crc);
            }

            _sndBuff[index++] = XON;    // ZCRCW requires response

            SendPacket(_sndBuff, index);
        }

        // ZDATA
        private void SendZDATA(CancellationToken cancelToken) {
            int filePos = Volatile.Read(ref _filePosReq);
            Volatile.Write(ref _filePosReqChanged, false);
            _fileStream.Seek(filePos, SeekOrigin.Begin);

            // Header
            Header hdr = new Header(ZDATA, pos: filePos);
            int pktOutCount = (_txCrcType == CRCType.CRC32) ?
                                BuildBin32Header(_sndBuff, hdr) :
                                BuildBin16Header(_sndBuff, hdr);
            SendPacket(_sndBuff, pktOutCount);

            // Sub frames
            while (!cancelToken.IsCancellationRequested) {
                if (Volatile.Read(ref _filePosReqChanged)) {
                    filePos = Volatile.Read(ref _filePosReq);
                    Volatile.Write(ref _filePosReqChanged, false);
                    _fileStream.Seek(filePos, SeekOrigin.Begin);
                }

                uint crc = (_txCrcType == CRCType.CRC32) ? Crc32.InitialValue : Crc16.InitialValue;
                int index = 0;
                int len = Math.Min(_fileSize - filePos, _frameSize);
                for (int i = 0; i < len; ++i) {
                    int c = _fileStream.ReadByte();
                    if (c < 0) {
                        break;
                    }
                    filePos++;
                    byte b = (byte)c;
                    index = PutBin(_sndBuff, index, b);
                    crc = (_txCrcType == CRCType.CRC32) ?
                            Crc32.Update(crc, b) :
                            Crc16.Update((ushort)crc, b);
                }

                byte frameType = (filePos >= _fileSize) ? ZCRCE : ZCRCG;
                _sndBuff[index++] = ZDLE;
                _sndBuff[index++] = frameType;
                crc = (_txCrcType == CRCType.CRC32) ?
                            Crc32.Update(crc, frameType) ^ Crc32.XorValue :
                            Crc16.Update((ushort)crc, frameType);

                if (_txCrcType == CRCType.CRC32) {
                    index = PutCRC32(_sndBuff, index, crc);
                }
                else {
                    index = PutCRC16(_sndBuff, index, (ushort)crc);
                }

                //Debug.WriteLine("frameType = {0:x2}", frameType);
                //Debug.WriteLine("CRC = {0:x8}", crc);
                SendPacket(_sndBuff, index);

                if (!_stopped) {
                    _parent.SetProgressValue(filePos);
                }

                if (frameType == ZCRCE) {
                    break;
                }
            }

            // ZEOF
            int zeofCount = BuildHEXHeader(_sndBuff, new Header(ZEOF, pos: filePos));
            SendPacket(_sndBuff, zeofCount);
        }

        // ZFIN
        private void SendZFIN() {
            int pktOutCount = BuildHEXHeader(_sndBuff, new Header(ZFIN));
            SendPacket(_sndBuff, pktOutCount);
        }

        // OO
        private void SendOverAndOut() {
            _sndBuff[0] = _sndBuff[1] = (byte)'O';
            SendPacket(_sndBuff, 2);
        }

        protected override void ProcessHeader(Header hdr) {
            switch (hdr.Type) {

                case ZRINIT:
                    Debug.WriteLine("Got ZRINIT");
                    if (_afterZRPOS) {
                        Debug.WriteLine("--> ZFIN");
                        SendZFIN();
                    }
                    else {
                        int bufSize = (hdr.ZP1 << 8) | hdr.ZP0;
                        _frameSize = Math.Min(Math.Max(_frameSize, bufSize), MAX_BLOCK);
                        _txCrcType = ((hdr.ZF0 & CANFC32) != 0) ? CRCType.CRC32 : CRCType.CRC16;

                        SendZFILE();
                    }
                    break;

                case ZRPOS: {
                        Debug.WriteLine("Got ZRPOS");
                        _afterZRPOS = true;
                        int pos = (hdr.ZP3 << 24)
                                | (hdr.ZP2 << 16)
                                | (hdr.ZP1 << 8)
                                | (hdr.ZP0);
                        _filePosReq = Math.Min(Math.Max(0, pos), _fileSize);
                        _filePosReqChanged = true;
                        if (_sendingTask == null) {
                            _sendingTask = Task.Run(() => SendZDATA(_cancellation.Token), _cancellation.Token);
                        }
                    }
                    break;

                case ZFIN:
                    Debug.WriteLine("Got ZFIN");
                    SendOverAndOut();
                    Complete(_fileSkipped ?
                        XZModemPlugin.Instance.Strings.GetString("Message.ZModem.FileSkipped") :
                        XZModemPlugin.Instance.Strings.GetString("Message.XModem.SendComplete"));
                    break;

                case ZSKIP:
                    Debug.WriteLine("Got ZSKIP");
                    _fileSkipped = true;
                    SendZFIN();
                    break;

                default:
                    Debug.WriteLine("Unknown Header : 0x{0:x2}", hdr.Type);
                    break;
            }
        }

        protected override void ProcessFileData(byte[] data, int offset, int length) {
            // do nothing
        }

        protected override void ProcessAbortByPeer() {
            if (!_stopped) {
                StopSendingTask(false);
                Cancel(XZModemPlugin.Instance.Strings.GetString("Message.ZModem.Aborted"));
            }
        }

        public override void Dispose() {
            StopSendingTask(true);

            if (_fileStream != null) {
                _fileStream.Dispose();
            }
        }
    }


    /// <summary>
    /// ZMODEM receiver
    /// </summary>
    internal class ZModemReceiver : ZModem {
        private readonly string _filename;
        private readonly byte[] _sndBuff = new byte[1032];
        private FileStream _fileStream;
        private int _filePos;

        private bool _stopped = false;

        public ZModemReceiver(XZModemDialog dialog, string filename)
            : base(dialog) {
            _filename = filename;
        }

        public override bool IsReceivingTask {
            get {
                return true;
            }
        }

        public override void Start() {
            _fileStream = new FileStream(_filename, FileMode.Create);
            _filePos = 0;
            StartListening();
            SendZRInit();
        }

        protected override void OnAbort() {
            SendAbortSequence();
            // FIXME:
            //  sender will send more data until the sender recognizes the abort sequence.
            //  trailing data will be displayed on the terminal, and may cause problems.
        }

        protected override void OnStop() {
            _stopped = true;
        }

        // ZRINIT
        private void SendZRInit() {
            if (!_stopped) {
                int pktOutCount = BuildHEXHeader(_sndBuff, new Header(ZRINIT, zf0: CANFC32 | CANOVIO));
                SendPacket(_sndBuff, pktOutCount);
            }
        }

        // ZRPOS
        private void SendZRPOS() {
            if (!_stopped) {
                int pktOutCount = BuildHEXHeader(_sndBuff, new Header(ZRPOS, pos: _filePos));
                SendPacket(_sndBuff, pktOutCount);
            }
        }

        // ZFIN
        private void SendZFIN() {
            if (!_stopped) {
                int pktOutCount = BuildHEXHeader(_sndBuff, new Header(ZFIN));
                SendPacket(_sndBuff, pktOutCount);
            }
        }

        protected override void ProcessHeader(Header hdr) {
            switch (hdr.Type) {
                case ZRQINIT:
                    Debug.WriteLine("Got ZRQINIT");
                    SendZRInit();
                    break;

                case ZFIN:
                    Debug.WriteLine("Got ZFIN");
                    SendZFIN();
                    Complete(XZModemPlugin.Instance.Strings.GetString("Message.XModem.ReceiveComplete"));
                    break;

                case ZFILE:
                    Debug.WriteLine("Got ZFILE");
                    _filePos = 0;
                    SendZRPOS();
                    break;

                case ZDATA:
                    Debug.WriteLine("Got ZDATA");
                    break;

                case ZEOF:
                    Debug.WriteLine("Got ZEOF");
                    SendZRInit();
                    break;

                case ZABORT:
                    Debug.WriteLine("Got ZABORT");
                    if (!_stopped) {
                        Cancel(XZModemPlugin.Instance.Strings.GetString("Message.ZModem.Aborted"));
                    }
                    break;

                default:
                    Debug.WriteLine("Unknown Header : 0x{0:x2}", hdr.Type);
                    break;
            }
        }

        protected override void ProcessFileData(byte[] data, int offset, int length) {
            if (length <= 0) {
                return;
            }

            _fileStream.Write(data, offset, length);
            int oldPos = _filePos;
            _filePos += length;
            if ((oldPos / 1024) != (_filePos / 1024)) {
                if (!_stopped) {
                    _parent.SetProgressValue((int)_filePos);
                }
            }
        }

        protected override void ProcessAbortByPeer() {
            if (!_stopped) {
                Cancel(XZModemPlugin.Instance.Strings.GetString("Message.ZModem.Aborted"));
            }
        }

        public override void Dispose() {
            if (_fileStream != null) {
                try {
                    _fileStream.Dispose();
                }
                catch {
                }
            }
        }
    }

}
