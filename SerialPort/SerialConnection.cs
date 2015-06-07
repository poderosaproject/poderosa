/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SerialConnection.cs,v 1.8 2011/11/19 05:06:39 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa.Sessions;
using Poderosa.Terminal;
using Poderosa.Forms;
using Poderosa.Util;
using Poderosa.Protocols;

namespace Poderosa.SerialPort {

    internal class Win32Serial {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetCommState(IntPtr handle, ref DCB dcb);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCommState(IntPtr handle, ref DCB dcb);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetCommTimeouts(IntPtr handle, ref COMMTIMEOUTS timeouts);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCommTimeouts(IntPtr handle, ref COMMTIMEOUTS timeouts);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCommBreak(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ClearCommBreak(IntPtr handle);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WaitCommEvent(
            IntPtr hFile,         // handle to comm device
            IntPtr lpEvtMask,     // event type
            IntPtr lpOverlapped   // overlapped structure
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ClearCommError(
            IntPtr hFile,     // handle to communications device
            IntPtr lpErrors, // error codes
            IntPtr lpStat  // communications status (本当はCommStat)
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCommMask(
            IntPtr hFile,                // handle to comm device
            int flags
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CreateFile(
            string filename,
            uint dwDesiredAccess,                      // access mode
            uint dwShareMode,                          // share mode
            IntPtr lpSecurityAttributes, // SD
            uint dwCreationDisposition,                // how to create
            uint dwFlagsAndAttributes,                 // file attributes
            IntPtr hTemplateFile                        // handle to template file
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadFile(
            IntPtr hFile,                // handle to file
            IntPtr lpBuffer,             // data buffer
            int nNumberOfBytesToRead,  // number of bytes to read
            IntPtr lpNumberOfBytesRead, // number of bytes read
            IntPtr lpOverlapped    // overlapped buffer
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteFile(
            IntPtr hFile,                // handle to file
            IntPtr lpBuffer,             // data buffer
            int nNumberOfBytesToRead,  // number of bytes to read
            IntPtr lpNumberOfBytesRead, // number of bytes read
            IntPtr lpOverlapped    // overlapped buffer
            );
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetOverlappedResult(
            IntPtr hFile,                       // handle to file, pipe, or device
            IntPtr lpOverlapped,          // overlapped structure
            IntPtr lpNumberOfBytesTransferred, // bytes transferred
            bool bWait                          // wait option
            );

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        [StructLayout(LayoutKind.Sequential)]
        public struct DCB {
            public uint DCBlength;
            public uint BaudRate;
            public uint Misc;
            /*
            DWORD fBinary: 1; 
            DWORD fParity: 1; 
            DWORD fOutxCtsFlow:1; 
            DWORD fOutxDsrFlow:1; 
            DWORD fDtrControl:2; 
            DWORD fDsrSensitivity:1; 
            DWORD fTXContinueOnXoff:1; 
            DWORD fOutX: 1; 
            DWORD fInX: 1; 
            DWORD fErrorChar: 1; 
            DWORD fNull: 1; 
            DWORD fRtsControl:2; 
            DWORD fAbortOnError:1; 
            DWORD fDummy2:17; 
            */
            public ushort wReserved;
            public ushort XonLim;
            public ushort XoffLim;
            public byte ByteSize;
            public byte Parity;
            public byte StopBits;
            public byte XonChar;
            public byte XoffChar;
            public byte ErrorChar;
            public byte EofChar;
            public byte EvtChar;
            public ushort wReserved1;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        [StructLayout(LayoutKind.Sequential)]
        public struct COMMTIMEOUTS {
            public uint ReadIntervalTimeout;
            public uint ReadTotalTimeoutMultiplier;
            public uint ReadTotalTimeoutConstant;
            public uint WriteTotalTimeoutMultiplier;
            public uint WriteTotalTimeoutConstant;
        }
    }


    internal class SerialTerminalOutput : ITerminalOutput {
        private IntPtr _fileHandle;

        public SerialTerminalOutput(IntPtr filehandle) {
            _fileHandle = filehandle;
        }

        public void SendBreak() {
            Win32Serial.SetCommBreak(_fileHandle);
            System.Threading.Thread.Sleep(500); //500ms待機
            Win32Serial.ClearCommBreak(_fileHandle);
        }

        public void SendKeepAliveData() {
        }

        public void AreYouThere() {
        }

        public void Resize(int width, int height) {
        }
    }

    internal class SerialSocket : IPoderosaSocket {
        private SerialTerminalConnection _parent;
        private IntPtr _fileHandle;
        private IByteAsyncInputStream _callback;
        private ByteDataFragment _dataFragment;
        private ManualResetEvent _writeOverlappedEvent;
        private SerialTerminalSettings _serialSettings;

        public SerialSocket(SerialTerminalConnection parent, IntPtr filehandle, SerialTerminalSettings settings) {
            _parent = parent;
            _serialSettings = settings;
            _fileHandle = filehandle;
            _writeOverlappedEvent = new ManualResetEvent(false);
        }

        public bool Available {
            get {
                return false;
            }
        }
        public void Close() {
            if (_writeOverlappedEvent != null) {
                _writeOverlappedEvent.Close();
                _writeOverlappedEvent = null;
            }
        }
        public void ForceDisposed() {
            Close();
        }


        public void RepeatAsyncRead(IByteAsyncInputStream cb) {
            if (_callback != null)
                throw new InvalidOperationException("duplicated AsyncRead() is attempted");

            _callback = cb;
            _dataFragment = new ByteDataFragment();
            new Thread(new ThreadStart(AsyncEntry)).Start();
            //_stream.BeginRead(_buf, 0, _buf.Length, new AsyncCallback(RepeatCallback), null);
        }

        private void AsyncEntry() {

            const int EV_RXCHAR = 1;

            ManualResetEvent commEventOverlappedEvent = null;
            ManualResetEvent readOverlappedEvent = null;

            try {
                commEventOverlappedEvent = new ManualResetEvent(false);
                readOverlappedEvent = new ManualResetEvent(false);

                NativeOverlapped commEventOverlapped = new NativeOverlapped();
                commEventOverlapped.EventHandle = commEventOverlappedEvent.SafeWaitHandle.DangerousGetHandle();

                NativeOverlapped readOverlapped = new NativeOverlapped();
                readOverlapped.EventHandle = readOverlappedEvent.SafeWaitHandle.DangerousGetHandle();

                GCHandle commEventOverlappedPinned = GCHandle.Alloc(commEventOverlapped, GCHandleType.Pinned);  // Pin a boxed NativeOverlapped
                GCHandle readOverlappedPinned = GCHandle.Alloc(readOverlapped, GCHandleType.Pinned);    // Pin a boxed NativeOverlapped
                int commFlags = 0;
                GCHandle commFlagsPinned = GCHandle.Alloc(commFlags, GCHandleType.Pinned);  // Pin a boxed Int32
                int readLength = 0;
                GCHandle readLengthPinned = GCHandle.Alloc(readLength, GCHandleType.Pinned); // Pin a boxed Int32
                int transferredLength = 0;
                GCHandle transferredLengthPinned = GCHandle.Alloc(transferredLength, GCHandleType.Pinned);  // Pin a boxed Int32

                byte[] buf = new byte[128];
                GCHandle bufPinned = GCHandle.Alloc(buf, GCHandleType.Pinned);

                // Note:
                //  GCHandle.Alloc(<struct>, GCHandleType.Pinned) makes a GCHandle for `Boxed' struct object.
                //  So if you want to read a value of the struct, you have to read it from `Boxed' struct object
                //  which is returned by GCHandle.Target.

                try {
                    bool success = false;

                    success = Win32Serial.ClearCommError(_fileHandle, IntPtr.Zero, IntPtr.Zero);
                    if (!success)
                        throw new Exception("ClearCommError failed " + Marshal.GetLastWin32Error());
                    //このSetCommMaskを実行しないとWaitCommEventが失敗してしまう
                    success = Win32Serial.SetCommMask(_fileHandle, 0);
                    if (!success)
                        throw new Exception("SetCommMask failed " + Marshal.GetLastWin32Error());
                    success = Win32Serial.SetCommMask(_fileHandle, EV_RXCHAR);
                    if (!success)
                        throw new Exception("SetCommMask failed " + Marshal.GetLastWin32Error());

                    while (true) {
                        commFlags = 0;
                        commFlagsPinned.Target = commFlags; // Pin a new boxed Int32
                        transferredLength = 0;
                        transferredLengthPinned.Target = transferredLength; // Pin a new boxed Int32
                        commEventOverlappedPinned.Target = commEventOverlapped; // Pin a new boxed NativeOverlapped

                        success = Win32Serial.WaitCommEvent(
                                        _fileHandle,
                                        commFlagsPinned.AddrOfPinnedObject(),
                                        commEventOverlappedPinned.AddrOfPinnedObject());
                        if (!success) {
                            int lastErr = Marshal.GetLastWin32Error();
                            if (lastErr == Win32.ERROR_INVALID_HANDLE)
                                goto CLOSED;  // closed in another thread ?
                            if (lastErr != Win32.ERROR_IO_PENDING)
                                throw new Exception("WaitCommEvent failed " + lastErr);

                            success = Win32Serial.GetOverlappedResult(
                                            _fileHandle,
                                            commEventOverlappedPinned.AddrOfPinnedObject(),
                                            transferredLengthPinned.AddrOfPinnedObject(),
                                            true);
                            if (!success) {
                                lastErr = Marshal.GetLastWin32Error();
                                if (lastErr == Win32.ERROR_INVALID_HANDLE || lastErr == Win32.ERROR_OPERATION_ABORTED)
                                    goto CLOSED;  // closed in another thread ?
                                throw new Exception("GetOverlappedResult failed " + lastErr);
                            }
                        }

                        if ((int)commFlagsPinned.Target != EV_RXCHAR)
                            goto CLOSED;

                        while (true) {
                            readLength = 0;
                            readLengthPinned.Target = readLength; // Pin a new boxed Int32
                            transferredLength = 0;
                            transferredLengthPinned.Target = transferredLength; // Pin a new boxed Int32
                            readOverlappedPinned.Target = readOverlapped; // Pin a new boxed NativeOverlapped

                            success = Win32Serial.ReadFile(
                                            _fileHandle,
                                            bufPinned.AddrOfPinnedObject(),
                                            buf.Length,
                                            readLengthPinned.AddrOfPinnedObject(),
                                            readOverlappedPinned.AddrOfPinnedObject());
                            if (!success) {
                                int lastErr = Marshal.GetLastWin32Error();
                                if (lastErr == Win32.ERROR_INVALID_HANDLE)
                                    goto CLOSED;  // closed in another thread ?
                                if (lastErr != Win32.ERROR_IO_PENDING)
                                    throw new Exception("ReadFile failed " + lastErr);

                                success = Win32Serial.GetOverlappedResult(
                                                _fileHandle,
                                                readOverlappedPinned.AddrOfPinnedObject(),
                                                transferredLengthPinned.AddrOfPinnedObject(),
                                                true);
                                if (!success) {
                                    lastErr = Marshal.GetLastWin32Error();
                                    if (lastErr == Win32.ERROR_INVALID_HANDLE || lastErr == Win32.ERROR_OPERATION_ABORTED)
                                        goto CLOSED;  // closed in another thread ?
                                    throw new Exception("GetOverlappedResult failed " + lastErr);
                                }
                                readLength = (int)transferredLengthPinned.Target;   // copy from pinned `boxed' Int32
                            }
                            else {
                                readLength = (int)readLengthPinned.Target;  // copy from pinned `boxed' Int32
                            }

                            if (readLength <= 0)
                                break;

                            _dataFragment.Set(buf, 0, readLength);
                            _callback.OnReception(_dataFragment);
                        }
                    }
                }
                finally {
                    commEventOverlappedPinned.Free();
                    readOverlappedPinned.Free();
                    commFlagsPinned.Free();
                    readLengthPinned.Free();
                    transferredLengthPinned.Free();
                    bufPinned.Free();
                }

            CLOSED:
                ;
            }
            catch (Exception ex) {
                if (!_parent.IsClosed) {
                    _callback.OnAbnormalTermination(ex.Message);
                }
            }
            finally {
                if (commEventOverlappedEvent != null)
                    commEventOverlappedEvent.Close();
                if (readOverlappedEvent != null)
                    readOverlappedEvent.Close();
            }
        }

        public void Transmit(ByteDataFragment data) {
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] data, int offset, int length) {
            byte nl = (byte)(_serialSettings.TransmitNL == Poderosa.ConnectionParam.NewLine.CR ? 13 : 10);

            if (_serialSettings.TransmitDelayPerChar == 0) {
                if (_serialSettings.TransmitDelayPerLine == 0)
                    WriteMain(data, offset, length); //最も単純
                else { //改行のみウェイト挿入
                    int limit = offset + length;
                    int c = offset;
                    while (offset < limit) {
                        if (data[offset] == nl) {
                            WriteMain(data, c, offset - c + 1);
                            Thread.Sleep(_serialSettings.TransmitDelayPerLine);
                            c = offset + 1;
                        }
                        offset++;
                    }
                    if (c < limit)
                        WriteMain(data, c, limit - c);
                }
            }
            else {
                for (int i = 0; i < length; i++) {
                    WriteMain(data, offset + i, 1);
                    Thread.Sleep(data[offset + i] == nl ? _serialSettings.TransmitDelayPerLine : _serialSettings.TransmitDelayPerChar);
                }
            }

        }

        private void WriteMain(byte[] buf, int offset, int length) {
            byte[] bufToWrite;

            while (length > 0) {
                if (offset != 0) {
                    bufToWrite = new byte[length];
                    Buffer.BlockCopy(buf, offset, bufToWrite, 0, length);
                }
                else {
                    bufToWrite = buf;
                }

                int wroteLength = 0;
                GCHandle wroteLengthPinned = GCHandle.Alloc(wroteLength, GCHandleType.Pinned);
                int transferredLength = 0;
                GCHandle transferredLengthPinned = GCHandle.Alloc(transferredLength, GCHandleType.Pinned);
                NativeOverlapped writeOverlapped = new NativeOverlapped();
                writeOverlapped.EventHandle = _writeOverlappedEvent.SafeWaitHandle.DangerousGetHandle();
                GCHandle writeOverlappedPinned = GCHandle.Alloc(writeOverlapped, GCHandleType.Pinned);
                GCHandle bufToWritePinned = GCHandle.Alloc(bufToWrite, GCHandleType.Pinned);

                // Note:
                //  GCHandle.Alloc(<struct>, GCHandleType.Pinned) makes a GCHandle for `Boxed' struct object.
                //  So if you want to read a value of the struct, you have to read it from `Boxed' struct object
                //  which is returned by GCHandle.Target.

                try {
                    bool success = Win32Serial.WriteFile(
                                        _fileHandle,
                                        bufToWritePinned.AddrOfPinnedObject(),
                                        length,
                                        wroteLengthPinned.AddrOfPinnedObject(),
                                        writeOverlappedPinned.AddrOfPinnedObject());
                    if (!success) {
                        int lastErr = Marshal.GetLastWin32Error();
                        if (lastErr != Win32.ERROR_IO_PENDING)
                            throw new IOException("WriteFile failed for " + lastErr);

                        success = Win32Serial.GetOverlappedResult(
                                        _fileHandle,
                                        writeOverlappedPinned.AddrOfPinnedObject(),
                                        transferredLengthPinned.AddrOfPinnedObject(),
                                        true);
                        if (!success) {
                            lastErr = Marshal.GetLastWin32Error();
                            throw new Exception("GetOverlappedResult failed " + lastErr);
                        }
                        wroteLength = (int)transferredLengthPinned.Target;  // copy from pinned `boxed' Int32
                    }
                    else {
                        wroteLength = (int)wroteLengthPinned.Target;    // copy from pinned `boxed' Int32
                    }
                }
                finally {
                    wroteLengthPinned.Free();
                    transferredLengthPinned.Free();
                    writeOverlappedPinned.Free();
                    bufToWritePinned.Free();
                }

                offset += wroteLength;
                length -= wroteLength;
            }
        }

    }

    /*
     * ノート　なぜシリアル通信がこんなことになっているか
     * 
     * 　シリアル通信がこんなに大変なのは、もとをただせばWindowsの設計が原因である。
     * 　普通であれば、ReadFileで非同期読み取りを試みて、それからWaitForSingleObjectを呼ぶのだが、
     * シリアルの場合はそこで即時戻ってしまう。従って、.NET Frameworkの挙動としてはBeginReadの直後
     * にコールバックが呼ばれてしまい、事実上ビジーループになるのである。
     * 　ちゃんとした非同期通信をするためには、WaitCommEventを使わなくてはならないので、.NET Framework
     * のサポート外になってしまう。
     * 
    */

    internal class SerialTerminalConnection : ITerminalConnection {
        //シリアルの非同期通信をちゃんとやろうとすると.NETライブラリでは不十分なのでほぼAPI直読み
        private IntPtr _fileHandle;
        private SerialSocket _serialSocket;
        private SerialTerminalOutput _serialTerminalOutput;
        private SerialTerminalParam _serialTerminalParam;
        private bool _closed;

        public SerialTerminalConnection(SerialTerminalParam p, SerialTerminalSettings settings, IntPtr fh) {
            _serialTerminalParam = p;
            _fileHandle = fh;
            _serialSocket = new SerialSocket(this, fh, settings);
            _serialTerminalOutput = new SerialTerminalOutput(fh);
            //_socket = _serialSocket;
            //_terminalOutput = _serialTerminalOutput;
        }
        public void Close() {
            if (_closed)
                return; //２度以上クローズしても副作用なし 

            _closed = true;
            _serialSocket.Close();
            Win32.CloseHandle(_fileHandle);
            _fileHandle = IntPtr.Zero;
            //Debug.WriteLine("COM connection termingating...");
        }



        public void ApplySerialParam(SerialTerminalSettings settings) {
            //paramの内容でDCBを更新してセットしなおす
            Win32Serial.DCB dcb = new Win32Serial.DCB();
            SerialPortUtil.FillDCB(_fileHandle, ref dcb);
            SerialPortUtil.UpdateDCB(ref dcb, settings);

            if (!Win32Serial.SetCommState(_fileHandle, ref dcb))
                throw new ArgumentException(SerialPortPlugin.Instance.Strings.GetString("Message.SerialTerminalConnection.ConfigError"));
        }

        public ITerminalParameter Destination {
            get {
                return _serialTerminalParam;
            }
        }

        public ITerminalOutput TerminalOutput {
            get {
                return _serialTerminalOutput;
            }
        }

        public IPoderosaSocket Socket {
            get {
                return _serialSocket;
            }
        }

        public bool IsClosed {
            get {
                return _closed;
            }
        }

        public IAdaptable GetAdapter(Type adapter) {
            return SerialPortPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }

    internal class SerialConnectionFactory : ITerminalConnectionFactory {
        public bool IsSupporting(ITerminalParameter param, ITerminalSettings settings) {
            SerialTerminalParam sp = param as SerialTerminalParam;
            SerialTerminalSettings ts = settings as SerialTerminalSettings;
            return sp != null && ts != null;
        }

        public ITerminalConnection EstablishConnection(IPoderosaMainWindow window, ITerminalParameter param, ITerminalSettings settings) {
            SerialTerminalParam sp = param as SerialTerminalParam;
            SerialTerminalSettings ts = settings as SerialTerminalSettings;
            Debug.Assert(sp != null && ts != null);

            return SerialPortUtil.CreateNewSerialConnection(window, sp, ts);
        }
    }

    internal class SerialPortUtil {
        public static SerialTerminalConnection CreateNewSerialConnection(IPoderosaMainWindow window, SerialTerminalParam param, SerialTerminalSettings settings) {
            bool successful = false;
            FileStream strm = null;
            try {
                StringResource sr = SerialPortPlugin.Instance.Strings;
                //Debug.WriteLine("OPENING COM"+param.Port);
                string portstr = String.Format("\\\\.\\{0}", param.PortName);
                IntPtr ptr = Win32Serial.CreateFile(portstr, Win32.GENERIC_READ | Win32.GENERIC_WRITE, 0, IntPtr.Zero, Win32.OPEN_EXISTING, Win32.FILE_ATTRIBUTE_NORMAL | Win32.FILE_FLAG_OVERLAPPED, IntPtr.Zero);
                if (ptr == Win32.INVALID_HANDLE_VALUE) {
                    string msg = sr.GetString("Message.FailedToOpenSerial");
                    int err = Marshal.GetLastWin32Error();
                    if (err == 2)
                        msg += sr.GetString("Message.NoSuchDevice");
                    else if (err == 5)
                        msg += sr.GetString("Message.DeviceIsBusy");
                    else
                        msg += "\nGetLastError=" + Marshal.GetLastWin32Error();
                    throw new Exception(msg);
                }
                //strm = new FileStream(ptr, FileAccess.Write, true, 8, true);
                Win32Serial.DCB dcb = new Win32Serial.DCB();
                FillDCB(ptr, ref dcb);
                UpdateDCB(ref dcb, settings);

                if (!Win32Serial.SetCommState(ptr, ref dcb)) {
                    Win32.CloseHandle(ptr);
                    throw new Exception(sr.GetString("Message.FailedToConfigSerial"));
                }
                Win32Serial.COMMTIMEOUTS timeouts = new Win32Serial.COMMTIMEOUTS();
                Win32Serial.GetCommTimeouts(ptr, ref timeouts);
                timeouts.ReadIntervalTimeout = 0xFFFFFFFF;
                timeouts.ReadTotalTimeoutConstant = 0;
                timeouts.ReadTotalTimeoutMultiplier = 0;
                timeouts.WriteTotalTimeoutConstant = 100;
                timeouts.WriteTotalTimeoutMultiplier = 100;
                Win32Serial.SetCommTimeouts(ptr, ref timeouts);
                successful = true;
                SerialTerminalConnection r = new SerialTerminalConnection(param, settings, ptr);
                return r;
            }
            catch (Exception ex) {
                RuntimeUtil.SilentReportException(ex);
                if (window != null)
                    window.Warning(ex.Message);
                else
                    GUtil.Warning(Form.ActiveForm, ex.Message); //TODO 苦しい逃げ。IPoderosaFormを実装したベースクラスをCoreにでも持っていたほうがいいのか
                return null;
            }
            finally {
                if (!successful && strm != null)
                    strm.Close();
            }
        }
        public static bool FillDCB(IntPtr handle, ref Win32Serial.DCB dcb) {
            dcb.DCBlength = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Win32Serial.DCB)); //sizeofくらいunsafeでなくても使わせてくれよ
            return Win32Serial.GetCommState(handle, ref dcb);
        }

        public static void UpdateDCB(ref Win32Serial.DCB dcb, SerialTerminalSettings param) {
            dcb.BaudRate = (uint)param.BaudRate;
            dcb.ByteSize = param.ByteSize;
            dcb.Parity = (byte)param.Parity;
            dcb.StopBits = (byte)param.StopBits;
            //フロー制御：TeraTermのソースからちょっぱってきた
            if (param.FlowControl == FlowControl.Xon_Xoff) {
                //dcb.fOutX = TRUE;
                //dcb.fInX = TRUE;
                //dcbを完全にコントロールするオプションが必要かもな
                dcb.Misc |= 0x300; //上記２行のかわり
                dcb.XonLim = 2048; //CommXonLim;
                dcb.XoffLim = 2048; //CommXoffLim;
                dcb.XonChar = 0x11;
                dcb.XoffChar = 0x13;
            }
            else if (param.FlowControl == FlowControl.Hardware) {
                //dcb.fOutxCtsFlow = TRUE;
                //dcb.fRtsControl = RTS_CONTROL_HANDSHAKE;
                dcb.Misc |= 0x4 | 0x2000;
            }
        }

        public static SerialTerminalSettings CreateDefaultSerialTerminalSettings(string portName) {
            SerialTerminalSettings ts = new SerialTerminalSettings();
            ts.BeginUpdate();
            ts.Icon = SerialPortPlugin.Instance.LoadIcon();
            ts.Caption = portName;
            ts.EndUpdate();
            return ts;
        }

    }
}
