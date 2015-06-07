/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: xmodem.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Poderosa.Terminal;
using Poderosa.Protocols;

namespace Poderosa.XZModem {
    internal abstract class XModem : ModemBase {
        public const byte SOH = 1;
        public const byte STX = 2;
        public const byte EOT = 4;
        public const byte ACK = 6;
        public const byte NAK = 21;
        public const byte CAN = 24;
        public const byte SUB = 26;


        //CRC
        public static ushort CalcCRC(byte[] data, int offset, int length) {
            ushort crc = 0;
            for (int i = 0; i < length; i++) {
                byte d = data[offset + i];
                /*
                int count = 8;
                while(--count>=0) {
                    if((crc & 0x8000)!=0) {
                        crc <<= 1;
                        crc += (((d<<=1) & 0x0400) != 0);
                        crc ^= 0x1021;
                    }
                    else {
                        crc <<= 1;
                        crc += (((d<<=1) & 0x0400) != 0);
                    }
                }
                */
                crc ^= (ushort)((ushort)d << 8);
                for (int j = 1; j <= 8; j++) {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ (ushort)0x1021);
                    else
                        crc <<= 1;
                }
            }
            return crc;
        }

        protected XZModemDialog _parent;
        protected string _fileName;
        protected byte _sequenceNumber;
        protected long _processedLength;
        protected bool _crcEnabled;
        protected Timer _timer;

        public XModem(XZModemDialog parent, string fn) {
            _parent = parent;
            _fileName = fn;
            _sequenceNumber = 1;
        }
        public bool CRCEnabled {
            get {
                return _crcEnabled;
            }
        }

        public override void Dispose() {
            if (_timer != null)
                _timer.Dispose();
        }
        protected void Fail(string msg) {
            _parent.AsyncClose();
            _site.Cancel(msg);
            Dispose();
        }

        private byte[] _byteBuf = new byte[1];
        protected void SendByte(byte b) {
            _byteBuf[0] = b;
            _connection.Socket.Transmit(_byteBuf, 0, 1);
        }

        protected void Complete() {
            _site.Complete();
            _parent.AsyncClose();
            Dispose();
        }


        #region IModalTerminalTask
        public override string Caption {
            get {
                return "XMODEM";
            }
        }

        #endregion
    }

    internal class XModemReceiver : XModem {
        private int _retryCount;
        private Stream _outputStream;
        private MemoryStream _buffer;


        private const int CRC_TIMEOUT = 1;
        private const int NEGOTIATION_TIMEOUT = 2;

        public XModemReceiver(XZModemDialog dlg, string filename)
            : base(dlg, filename) {
            _outputStream = new FileStream(_fileName, FileMode.Create, FileAccess.Write);
        }
        public override void Start() {
            _timer = new Timer(new TimerCallback(OnTimeout), CRC_TIMEOUT, 3000, Timeout.Infinite);
            _crcEnabled = true;
            SendByte((byte)'C'); //CRCモードでトライ
        }


        private void OnTimeout(object state) {
            _timer.Dispose();
            _timer = null;
            switch ((int)state) {
                case CRC_TIMEOUT:
                    _crcEnabled = false;
                    _timer = new Timer(new TimerCallback(OnTimeout), NEGOTIATION_TIMEOUT, 5000, Timeout.Infinite);
                    SendByte(NAK);
                    break;
                case NEGOTIATION_TIMEOUT:
                    SendByte(CAN);
                    Fail(XZModemPlugin.Instance.Strings.GetString("Message.XModem.StartTimedOut"));
                    break;
            }

        }
        public override void Dispose() {
            base.Dispose();
            _outputStream.Close();
        }
        public override void Abort() {
            SendByte(CAN);
            _site.Cancel(null);
            Dispose();
        }
        public override bool IsReceivingTask {
            get {
                return true;
            }
        }

        public override void OnReception(ByteDataFragment fragment) {
            if (_timer != null) {
                _timer.Dispose();
                _timer = null;
            }

            //Debug.WriteLine(String.Format("Received {0}", count));
            //_debugStream.Write(data, offset, count);
            //_debugStream.Flush();
            AdjustBuffer(ref fragment);
            byte[] data = fragment.Buffer;
            int offset = fragment.Offset;
            int count = fragment.Length;

            byte head = data[offset];
            if (head == EOT) { //successfully exit
                SendByte(ACK);
                _site.MainWindow.Information(XZModemPlugin.Instance.Strings.GetString("Message.XModem.ReceiveComplete"));
                Complete();
                //_debugStream.Close();
            }
            else {
                int required = 3 + (head == STX ? 1024 : 128) + (_crcEnabled ? 2 : 1);
                if (required > count) {
                    ReserveBuffer(data, offset, count); //途中で切れていた場合
                    //Debug.WriteLine(String.Format("Reserving #{0} last={1} offset={2} count={3}", seq, last, offset, count));
                    return;
                }

                byte seq = data[offset + 1];
                byte neg = data[offset + 2];
                if (seq != _sequenceNumber || seq + neg != 255) {
                    Fail(XZModemPlugin.Instance.Strings.GetString("Message.XModem.SequenceError"));
                }
                else {
                    //Debug.WriteLine(String.Format("Processing #{0}", seq));
                    bool success;
                    int body_offset = offset + 3;
                    int body_len = head == STX ? 1024 : 128;
                    int checksum_offset = offset + 3 + body_len;
                    if (_crcEnabled) {
                        ushort sent = (ushort)((((ushort)data[checksum_offset]) << 8) + (ushort)data[checksum_offset + 1]);
                        ushort sum = CalcCRC(data, body_offset, body_len);
                        success = (sent == sum);
                    }
                    else {
                        byte sent = data[checksum_offset];
                        byte sum = 0;
                        for (int i = body_offset; i < checksum_offset; i++)
                            sum += data[i];
                        success = (sent == sum);
                    }

                    _buffer = null; //ブロックごとにACKを待つ仕様なので、もらってきたデータが複数ブロックにまたがることはない。したがってここで破棄して構わない。
                    if (success) {
                        SendByte(ACK);
                        _sequenceNumber++;

                        int t = checksum_offset - 1;
                        while (t >= body_offset && data[t] == 26)
                            t--; //Ctrl+Zで埋まっているところは無視
                        int len = t + 1 - body_offset;
                        _outputStream.Write(data, body_offset, len);
                        _processedLength += len;
                        _parent.AsyncSetProgressValue((int)_processedLength);
                        _retryCount = 0;
                    }
                    else {
                        //_debugStream.Close();
                        if (++_retryCount == 3) { //もうあきらめる
                            Fail(XZModemPlugin.Instance.Strings.GetString("Message.XModem.CheckSumError"));
                        }
                        else {
                            SendByte(NAK);
                        }
                    }

                }
            }
        }

        private void ReserveBuffer(byte[] data, int offset, int count) {
            _buffer = new MemoryStream();
            _buffer.Write(data, offset, count);
        }
        private void AdjustBuffer(ref ByteDataFragment fragment) {
            if (_buffer == null || _buffer.Position == 0)
                return;

            _buffer.Write(fragment.Buffer, fragment.Offset, fragment.Length);
            int count = (int)_buffer.Position;
            _buffer.Close();
            fragment.Set(_buffer.ToArray(), 0, count);
        }

    }

    internal class XModemSender : XModem {
        private bool _negotiating;
        private int _retryCount;
        private byte[] _body;
        private int _offset;
        private int _nextOffset;

        private const int NEGOTIATION_TIMEOUT = 1;

        public int TotalLength {
            get {
                return _body.Length;
            }
        }
        public override bool IsReceivingTask {
            get {
                return false;
            }
        }

        public XModemSender(XZModemDialog dlg, string filename)
            : base(dlg, filename) {
            _body = new byte[new FileInfo(filename).Length];
            FileStream strm = new FileStream(filename, FileMode.Open, FileAccess.Read);
            strm.Read(_body, 0, _body.Length);
            strm.Close();
        }
        public override void Start() {
            _timer = new Timer(new TimerCallback(OnTimeout), NEGOTIATION_TIMEOUT, 60000, Timeout.Infinite);
            _negotiating = true;
        }

        private void OnTimeout(object state) {
            _timer.Dispose();
            _timer = null;
            switch ((int)state) {
                case NEGOTIATION_TIMEOUT:
                    Fail(XZModemPlugin.Instance.Strings.GetString("Message.XModem.StartTimedOut"));
                    break;
            }

        }

        public override void Abort() {
            SendByte(CAN);
            _site.Cancel(null);
            Dispose();
        }

        public override void OnReception(ByteDataFragment fragment) {
            if (_timer != null) {
                _timer.Dispose();
                _timer = null;
            }

            byte[] data = fragment.Buffer;
            int offset = fragment.Offset;
            int count = fragment.Length;

            if (_negotiating) {
                for (int i = 0; i < count; i++) {
                    byte t = data[offset + i];
                    if (t == NAK || t == (byte)'C') {
                        _crcEnabled = t == (byte)'C';
                        _negotiating = false;
                        _sequenceNumber = 1;
                        _offset = _nextOffset = 0;
                        break;
                    }
                }
                if (_negotiating)
                    return; //あたまがきていない
            }
            else {
                byte t = data[offset];
                if (t == ACK) {
                    _sequenceNumber++;
                    _retryCount = 0;
                    if (_offset == _body.Length) { //successfully exit
                        _site.MainWindow.Information(XZModemPlugin.Instance.Strings.GetString("Message.XModem.SendComplete"));
                        Complete();
                        return;
                    }
                    _offset = _nextOffset;
                }
                else if (t != NAK || (++_retryCount == 3)) {
                    Fail(XZModemPlugin.Instance.Strings.GetString("Message.XModem.BlockStartError"));
                    return;
                }
            }

            if (_nextOffset >= _body.Length) { //last
                SendByte(EOT);
                _offset = _body.Length;
            }
            else {
                int len = 128;
                if (_crcEnabled && _offset + 1024 <= _body.Length)
                    len = 1024;
                byte[] buf = new byte[3 + len + (_crcEnabled ? 2 : 1)];
                buf[0] = len == 128 ? SOH : STX;
                buf[1] = (byte)_sequenceNumber;
                buf[2] = (byte)(255 - buf[1]);
                int body_len = Math.Min(len, _body.Length - _offset);
                Array.Copy(_body, _offset, buf, 3, body_len);
                for (int i = body_len; i < len; i++)
                    buf[3 + i] = 26; //padding
                if (_crcEnabled) {
                    ushort sum = CalcCRC(buf, 3, len);
                    buf[3 + len] = (byte)(sum >> 8);
                    buf[3 + len + 1] = (byte)(sum & 0xFF);
                }
                else {
                    byte sum = 0;
                    for (int i = 0; i < len; i++)
                        sum += buf[3 + i];
                    buf[3 + len] = sum;
                }

                _nextOffset = _offset + len;
                _connection.Socket.Transmit(buf, 0, buf.Length);
                _parent.AsyncSetProgressValue(_nextOffset);
                //Debug.WriteLine("Transmitted "+_sequenceNumber+" " +_offset);
            }

        }
    }
}
