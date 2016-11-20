// Copyright 2004-2016 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.

//#define TRACE_XMODEM

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Poderosa.Protocols;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Poderosa.XZModem {

    /// <summary>
    /// XMODEM protocol base class
    /// </summary>
    internal abstract class XModem : ModemBase {
        protected const byte SOH = 0x01;
        protected const byte STX = 0x02;
        protected const byte EOT = 0x04;
        protected const byte ACK = 0x06;
        protected const byte NAK = 0x15;
        protected const byte CAN = 0x18;
        protected const byte CPMEOF = 0x1a;
        protected const byte LETTER_C = 0x43;

        private readonly byte[] _singleByteBuff = new byte[1];

        private readonly XZModemDialog _parent;

        protected XModem(XZModemDialog dialog)
            : base(dialog) {
            _parent = dialog;
        }

        #region IModalTerminalTask

        public override string Caption {
            get {
                return "XMODEM";
            }
        }

        #endregion

        protected void Send(byte[] data, int len) {
            _connection.Socket.Transmit(data, 0, len);
        }

        protected void Send(byte ch) {
            lock (_singleByteBuff) {
                _singleByteBuff[0] = ch;
                Send(_singleByteBuff, 1);
            }
        }

        protected void SetProgressValue(long pos) {
            _parent.SetProgressValue(pos);
        }

        protected void Trace(string message) {
#if TRACE_XMODEM
            Debug.WriteLine(message);
#endif
        }

        protected void Trace(string format, params object[] args) {
#if TRACE_XMODEM
            Debug.WriteLine(format, args);
#endif
        }
    }

    /// <summary>
    /// XMODEM receiver
    /// </summary>
    internal class XModemReceiver : XModem {

        private const int WAIT_CRCBLOCK_TIMEOUT = 3000;
        private const int WAIT_BLOCK_TIMEOUT = 10000;
        private const int MAX_ERROR = 10;

        private const int MODE_CHECKSUM = 0;
        private const int MODE_CRC = 1;

        private struct BlockTypeInfo {
            public readonly bool HasCRC;
            public readonly int BlockSize;
            public readonly int DataOffset;
            public readonly int DataLength;

            public BlockTypeInfo(bool hasCRC, int blockSize, int dataOffset, int dataLength) {
                HasCRC = hasCRC;
                BlockSize = blockSize;
                DataOffset = dataOffset;
                DataLength = dataLength;
            }
        }

        private readonly string _filePath;

        private FileStream _output;
        private Task _monitorTask;
        private bool _teminateMonitorTask;
        private bool _fileClosed;
        private long _fileSize = 0;
        private readonly byte[] _pendingBuff = new byte[1024];
        private int _pendingLen = 0;
        private readonly byte[] _recvBuff = new byte[1029];
        private int _recvLen = 0;
        private byte _nextSequenceNumber;
        private int _mode;  // MODE_CHECKSUM or MODE_CRC

        private DateTime _lastReceptionUtcTime;
        private readonly object _lastReceptionUtcTimeSync = new object();

        private DateTime _lastBlockUtcTime;
        private readonly object _lastBlockUtcTimeSync = new object();

        // count of the consecutive errors
        private int _errorCount;
        // true when the file transfer is aborting
        private bool _aborting;

        public XModemReceiver(XZModemDialog parent, string filePath)
            : base(parent) {
            _filePath = filePath;
        }

        public override bool IsReceivingTask {
            get {
                return true;
            }
        }

        private void Monitor() {
            int crcModeRetries = 0;

            while (!Volatile.Read(ref _teminateMonitorTask)) {
                DateTime lastAcc;
                lock (_lastBlockUtcTimeSync) {
                    lastAcc = _lastBlockUtcTime;
                }

                DateTime now = DateTime.UtcNow;
                int elapsedMsec = (int)((now - lastAcc).Ticks / TimeSpan.TicksPerMillisecond);

                int mode = Volatile.Read(ref _mode);
                int sn = Volatile.Read(ref _nextSequenceNumber);

                if (mode == MODE_CRC && sn == 1 && elapsedMsec > WAIT_CRCBLOCK_TIMEOUT) {
                    if (crcModeRetries < 3) {
                        crcModeRetries++;
                        lock (_lastBlockUtcTimeSync) {
                            _lastBlockUtcTime = DateTime.UtcNow;
                        }
                        Trace("<-- Retry: C");
                        Send(LETTER_C);
                    }
                    else {
                        lock (_lastBlockUtcTimeSync) {
                            _lastBlockUtcTime = DateTime.UtcNow;
                        }
                        Volatile.Write(ref _mode, MODE_CHECKSUM);
                        Trace("<-- Retry: NAK");
                        Send(NAK);  // fallback into checksum mode
                    }
                    goto Continue;
                }

                if (elapsedMsec > WAIT_BLOCK_TIMEOUT) {
                    Abort(XZModemPlugin.Instance.Strings.GetString("Message.XModem.ReceivingTimedOut"), false);
                    break;
                }

            Continue:
                Thread.Sleep(200);
            }

            Trace("exit monitor thread");
        }

        protected override void OnStart() {
            _output = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            _teminateMonitorTask = false;
            _mode = MODE_CRC;
            _nextSequenceNumber = 1;
            _lastBlockUtcTime = DateTime.UtcNow;
            _errorCount = 0;
            Thread.MemoryBarrier();
            _monitorTask = Task.Run(() => Monitor());
            Trace("<-- C");
            Send(LETTER_C); // request CRC mode
        }

        private void StopMonitor() {
            if (_monitorTask != null) {
                Volatile.Write(ref _teminateMonitorTask, true);
                // don't wait completion of monitor task here
                // because cancellation may be invoked from monitor task.
                _monitorTask = null;
            }
        }

        protected override void OnAbort(string message, bool closeDialog) {
            // stop monitor thread
            StopMonitor();
            // run aborting sequence
            Task.Run(() => {
                _aborting = true;
                Thread.MemoryBarrier();
                Thread.Sleep(200);
                Trace("<-- CAN");
                Send(CAN);
                Send(CAN);
                DiscardAllIncomingData();
                Completed(true, closeDialog, message);
            });
        }

        protected override void OnStopped() {
            StopMonitor();
            _fileClosed = true;
            if (_output != null) {
                _output.Close();
                Trace("file closed");
            }
        }

        public override void Dispose() {
            StopMonitor();
            if (_output != null) {
                _output.Dispose();
            }
        }

        public override void OnReception(ByteDataFragment fragment) {
            lock (_lastReceptionUtcTimeSync) {
                _lastReceptionUtcTime = DateTime.UtcNow;
            }

            if (_aborting) {
                return;
            }

            byte[] data = fragment.Buffer;
            int offset = fragment.Offset;
            int length = fragment.Length;


            BlockTypeInfo blockInfo;
            if (_recvLen > 0) {
                blockInfo = GetBlockTypeInfo(_recvBuff[0], Volatile.Read(ref _mode));
            }
            else {
                blockInfo = new BlockTypeInfo();    // update later
            }

            for (int i = 0; i < length; i++) {
                byte c = data[offset + i];

                if (_recvLen == 0) {
                    if (c == EOT) {
                        Trace("--> EOT");
                        FlushPendingBuffer(true);
                        Trace("<-- ACK");
                        Send(ACK);
                        Completed(false, true, XZModemPlugin.Instance.Strings.GetString("Message.XModem.ReceiveComplete"));
                        return;
                    }

                    if (c != SOH && c != STX) {
                        continue;   // skip
                    }

                    blockInfo = GetBlockTypeInfo(c, Volatile.Read(ref _mode));
                }

                _recvBuff[_recvLen++] = c;

                if (_recvLen >= blockInfo.BlockSize) {
                    break;
                }
            }

            // a block has been received
            lock (_lastBlockUtcTimeSync) {
                _lastBlockUtcTime = DateTime.UtcNow;
            }

            Trace("--> {0:X2} {1:X2} ...({2})", _recvBuff[0], _recvBuff[1], _recvLen);

            // check sequence number
            if (_recvBuff[1] != _nextSequenceNumber || _recvBuff[2] != (255 - _nextSequenceNumber)) {
                Trace("<-- NAK (bad seq)");
                goto Error;
            }

            // check CRC or checksum
            if (blockInfo.HasCRC) {
                ushort crc = Crc16.Update(Crc16.InitialValue, _recvBuff, blockInfo.DataOffset, blockInfo.DataLength);
                int crcIndex = blockInfo.DataOffset + blockInfo.DataLength;
                if (_recvBuff[crcIndex] != (byte)(crc >> 8) || _recvBuff[crcIndex + 1] != (byte)crc) {
                    // CRC error
                    Trace("<-- NAK (CRC error)");
                    goto Error;
                }
            }
            else {
                byte checksum = 0;
                int index = blockInfo.DataOffset;
                for (int n = 0; n < blockInfo.DataLength; ++n) {
                    checksum += _recvBuff[index++];
                }
                if (_recvBuff[index] != checksum) {
                    // checksum error
                    Trace("<-- NAK (checksum error)");
                    goto Error;
                }
            }

            // ok
            _nextSequenceNumber++;

            FlushPendingBuffer(false);
            SaveToPendingBuffer(_recvBuff, blockInfo.DataOffset, blockInfo.DataLength);

            _errorCount = 0;
            _recvLen = 0;
            Send(ACK);
            return;

        Error:
            _recvLen = 0;
            _errorCount++;
            if (_errorCount > MAX_ERROR) {
                Abort(XZModemPlugin.Instance.Strings.GetString("Message.XModem.CouldNotReceiveCorrectData"), false);
            }
            else {
                Send(NAK);
            }
        }

        private void FlushPendingBuffer(bool isLastBlock) {
            if (isLastBlock) {
                while (_pendingLen > 0 && _pendingBuff[_pendingLen - 1] == CPMEOF) {
                    _pendingLen--;
                }
            }

            if (_pendingLen > 0) {
                if (!Volatile.Read(ref _fileClosed)) {
                    _output.Write(_pendingBuff, 0, _pendingLen);
                }
                _fileSize += _pendingLen;
                _pendingLen = 0;
            }

            SetProgressValue(_fileSize);
        }

        private void SaveToPendingBuffer(byte[] buff, int offset, int length) {
            Buffer.BlockCopy(buff, offset, _pendingBuff, 0, length);
            _pendingLen = length;
        }

        private BlockTypeInfo GetBlockTypeInfo(byte firstByte, int mode) {
            if (firstByte == STX) {
                // XMODEM/1k
                return new BlockTypeInfo(true, 1029, 3, 1024);
            }

            if (mode == MODE_CRC) {
                // XMODEM/CRC
                return new BlockTypeInfo(true, 133, 3, 128);
            }

            // XMODEM
            return new BlockTypeInfo(false, 132, 3, 128);
        }

        private void DiscardAllIncomingData() {
            while (true) {
                DateTime last;
                lock (_lastReceptionUtcTimeSync) {
                    last = _lastReceptionUtcTime;
                }
                if ((DateTime.UtcNow - last).Ticks > 500 * TimeSpan.TicksPerMillisecond) {
                    return;
                }
                Thread.Sleep(100);
            }
        }
    }

    /// <summary>
    /// XMODEM sender
    /// </summary>
    internal class XModemSender : XModem {

        private const int RESPONSE_TIMEOUT = 10000;

        private readonly string _filePath;

        private long _fileSize;
        private FileStream _input;
        private Task _monitorTask;
        private bool _teminateMonitorTask;
        private bool _fileClosed;

        private byte _sequenceNumber;
        private bool _crcMode;
        private long _prevPos;
        private long _nextPos;
        private readonly byte[] _sendBuff = new byte[1029];

        private DateTime _lastResponseUtcTime;
        private readonly object _lastResponseUtcTimeSync = new object();

        private enum State {
            None,
            AfterEOT,
            Aborting,
            Stopped,
        }

        private volatile State _state = State.None;

        public XModemSender(XZModemDialog parent, string filePath)
            : base(parent) {
            _filePath = filePath;
        }

        public override bool IsReceivingTask {
            get {
                return false;
            }
        }

        protected override void OnStart() {
            _fileSize = new FileInfo(_filePath).Length;
            _input = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
            _sequenceNumber = 1;
            _teminateMonitorTask = false;
            _prevPos = _nextPos = 0;
            _crcMode = false;
            _state = State.None;
            _lastResponseUtcTime = DateTime.UtcNow;
            Thread.MemoryBarrier();
            _monitorTask = Task.Run(() => Monitor());
        }

        private void Monitor() {
            while (!Volatile.Read(ref _teminateMonitorTask)) {
                DateTime lastRes;
                lock (_lastResponseUtcTimeSync) {
                    lastRes = _lastResponseUtcTime;
                }
                int elapsedMsec = (int)((lastRes - DateTime.UtcNow).Ticks / TimeSpan.TicksPerMillisecond);

                if (elapsedMsec > RESPONSE_TIMEOUT) {
                    Abort(XZModemPlugin.Instance.Strings.GetString("Message.XModem.NoResponse"), false);
                    break;
                }

                Thread.Sleep(200);
            }

            Trace("exit monitor thread");
        }

        private void StopMonitor() {
            if (_monitorTask != null) {
                Volatile.Write(ref _teminateMonitorTask, true);
                // don't wait completion of sending task here
                // because cancellation may be invoked from sending task.
                _monitorTask = null;
            }
        }

        protected override void OnAbort(string message, bool closeDialog) {
            // stop monitor thread
            StopMonitor();
            // run aborting sequence
            Task.Run(() => {
                _state = State.Aborting;
                Thread.MemoryBarrier();
                // CAN mast be sent after the ACK or NAK from peer
                DateTime limit = DateTime.UtcNow.AddMilliseconds(3000);
                SpinWait.SpinUntil(() => {
                    if (DateTime.UtcNow > limit) {
                        // timeout
                        return true;
                    }
                    if (_state == State.Stopped) {
                        // CAN has been sent
                        return true;
                    }
                    return false;
                });
                Thread.MemoryBarrier();
                if (_state != State.Stopped) {
                    // no response ?
                    Send(CAN);
                    Send(CAN);
                    Thread.Sleep(500);
                }
                Completed(true, closeDialog, message);
            });
        }

        protected override void OnStopped() {
            StopMonitor();
            _fileClosed = true;
            Thread.MemoryBarrier();
            if (_input != null) {
                _input.Close();
                Trace("file closed");
            }
        }

        public override void Dispose() {
            StopMonitor();
            if (_input != null) {
                _input.Dispose();
            }
        }

        public override void OnReception(ByteDataFragment fragment) {
            if (_state == State.Stopped) {
                return;
            }

            if (_state == State.Aborting) {
                Send(CAN);
                Send(CAN);
                _state = State.Stopped;
                return;
            }

            byte[] data = fragment.Buffer;
            int offset = fragment.Offset;
            int length = fragment.Length;

            byte response = 0;
            for (int i = 0; i < length; ++i) {
                byte c = data[offset + i];
                if (c == LETTER_C || c == ACK || c == NAK || c == CAN) {
                    response = c;
                    break;
                }
            }
            if (response == 0) {
                return;
            }

            lock (_lastResponseUtcTimeSync) {
                _lastResponseUtcTime = DateTime.UtcNow;
            }

            switch (response) {
                case NAK:
                    Trace("--> NAK");
                Resend:
                    if (_state == State.AfterEOT) {
                        Trace("<-- EOT(resend)");
                        Send(EOT);
                    }
                    else {
                        SendBlock(_crcMode, true);
                    }
                    break;
                case LETTER_C:
                    Trace("--> C");
                    _crcMode = true;
                    goto Resend;
                case ACK:
                    Trace("--> ACK");
                    if (_state == State.AfterEOT) {
                        _state = State.Stopped;
                        Completed(false, true, XZModemPlugin.Instance.Strings.GetString("Message.XModem.SendComplete"));
                    }
                    else {
                        SendBlock(_crcMode, false);
                    }
                    break;
                case CAN:
                    Trace("--> CAN");
                    _state = State.Stopped;
                    Abort(XZModemPlugin.Instance.Strings.GetString("Message.ZModem.Aborted"), false);
                    break;
            }
        }

        private void SendBlock(bool useCrc, bool resend) {
            if (_input == null || _fileClosed) {
                return;
            }

            if (resend) {
                Trace("Seek to {0}", _prevPos);
                _input.Seek(_prevPos, SeekOrigin.Begin);
            }
            else {
                _prevPos = _nextPos;
                _sequenceNumber++;
            }

            int dataLength;
            if (useCrc) {
                dataLength = (_fileSize - _prevPos < 1024L) ? 128 : 1024;
            }
            else {
                dataLength = 128;
            }

            int readLen = _input.Read(_sendBuff, 3, dataLength);
            _nextPos = _input.Position;

            if (readLen == 0) {
                _state = State.AfterEOT;
                Trace("<-- EOT");
                Send(EOT);
                return;
            }

            _sendBuff[0] = (dataLength == 1024) ? STX : SOH;
            _sendBuff[1] = _sequenceNumber;
            _sendBuff[2] = (byte)(255 - _sequenceNumber);

            for (int i = 3 + readLen; i < 3 + dataLength; ++i) {
                _sendBuff[i] = CPMEOF;
            }

            int blockLen = 3 + dataLength;
            if (useCrc) {
                ushort crc = Crc16.Update(Crc16.InitialValue, _sendBuff, 3, dataLength);
                _sendBuff[blockLen++] = (byte)(crc >> 8);
                _sendBuff[blockLen++] = (byte)crc;
            }
            else {
                byte checksum = 0;
                for (int i = 3; i < 3 + dataLength; ++i) {
                    checksum += _sendBuff[i];
                }
                _sendBuff[blockLen++] = checksum;
            }

            Trace("<-- {0:X2} {1:X2} ...({2})", _sendBuff[0], _sendBuff[1], blockLen);

            Send(_sendBuff, blockLen);

            SetProgressValue((int)_nextPos);
        }
    }
}
