// Copyright 2011-2025 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

using Poderosa.Protocols;
using Microsoft.Win32.SafeHandles;

namespace Poderosa.Pipe {

    /// <summary>
    /// Implementation of IPoderosaSocket
    /// </summary>
    internal class PipeSocket : IPoderosaSocket {

        private readonly string _remote;

        private readonly FileStream _inputStream;
        private readonly FileStream _outputStream;

        private Thread _inputThread = null;
        private bool _skipInputThreadJoin = false;
        private volatile bool _terminateInputThread = false;
        private volatile bool _processExited = false;
        private bool _closed = false;


        /// <summary>
        /// Get whether PipeSocket was closed
        /// </summary>
        public bool IsClosed {
            get {
                return _closed;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inputStream">Stream to input from.</param>
        /// <param name="outputStream">Stream to output to. Can be same instance as inputStream.</param>
        /// <param name="remote">text representing the remote side</param>
        public PipeSocket(FileStream inputStream, FileStream outputStream, string remote) {
            Debug.Assert(inputStream != null);
            Debug.Assert(outputStream != null);

            _remote = remote;
            _inputStream = inputStream;
            _outputStream = outputStream;
        }

        /// <summary>
        /// Called when the process exited.
        /// The receiving thread will stop and pipes will be closed.
        /// </summary>
        public void ProcessExited() {
            _processExited = true;
        }

        /// <summary>
        /// Thread input from stream
        /// </summary>
        /// <param name="asyncInput"></param>
        private void InputThread(IByteAsyncInputStream asyncInput) {
            byte[] buff = new byte[4096];

            ByteDataFragment _dataFragment = new ByteDataFragment();

            try {
                bool endOfStream = false;

                while (!_terminateInputThread && !_processExited) {
                    IAsyncResult asyncResult = _inputStream.BeginRead(buff, 0, buff.Length, null, null);

                    while (!_terminateInputThread && !_processExited) {
                        bool signaled = asyncResult.AsyncWaitHandle.WaitOne(500);
                        if (signaled) {
                            int len = _inputStream.EndRead(asyncResult);
                            if (len == 0) {
                                endOfStream = true;
                                goto EndThread;
                            }

                            _dataFragment.Set(buff, 0, len);
                            asyncInput.OnReception(_dataFragment);

                            break;
                        }
                    }
                }

            EndThread:
                if (endOfStream || _processExited) {
                    _skipInputThreadJoin = true; // avoids deadlock
                    Close();
                    asyncInput.OnNormalTermination();
                }
            }
            catch (Exception e) {
                RuntimeUtil.SilentReportException(e);
                _skipInputThreadJoin = true; // avoids deadlock
                Close();
                asyncInput.OnAbnormalTermination("Input thread error: " + e.Message);
            }
        }


        #region IPoderosaSocket

        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            if (_inputThread != null)
                throw new InvalidOperationException("duplicated RepeatAsyncRead() is attempted");
            if (_closed)
                throw new InvalidOperationException("invalid call of RepeatAsyncRead()");

            _skipInputThreadJoin = false;
            IByteAsyncInputStream asyncInput = receiver;
            _inputThread = new Thread((ThreadStart)delegate() {
                InputThread(asyncInput);
            });
            _inputThread.Name = "Poderosa.Pipe.PipeSocket.InputThread";
            _inputThread.Start();
        }

        public string Remote {
            get {
                return _remote;
            }
        }

        public bool Available {
            get {
                return false;
            }
        }

        public void ForceDisposed() {
            Close();
        }

        #endregion

        #region IByteOutputStream

        public void Transmit(ByteDataFragment data) {
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] data, int offset, int length) {
            try {
                _outputStream.Write(data, offset, length);
                _outputStream.Flush();
            }
            catch (IOException e) {
                RuntimeUtil.ReportException(e);
            }
            catch (ObjectDisposedException e) {
                RuntimeUtil.ReportException(e);
            }
        }

        public void Close() {

            lock (this) {
                if (_closed)
                    return;

                _closed = true;
            }

            _terminateInputThread = true;
            if (_inputThread != null && !_skipInputThreadJoin) {
                _inputThread.Join();
            }

            try {
                _inputStream.Dispose();
            }
            catch (Exception) {
            }

            if (!Object.ReferenceEquals(_outputStream, _inputStream)) {
                try {
                    _outputStream.Dispose();
                }
                catch (Exception) {
                }
            }
        }

        #endregion
    }

}
