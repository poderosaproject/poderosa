// Copyright 2011-2017 The Poderosa Project.
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

using Poderosa.Protocols;

namespace Poderosa.Pipe {

    /// <summary>
    /// Implementation of ITerminalConnection
    /// </summary>
    internal class PipeTerminalConnection : ITerminalConnection {

        private readonly PipeTerminalOutput _terminalOutput;
        private readonly PipeTerminalParameter _terminalParameter;
        private readonly PipeSocket _socket;
        private readonly PipedProcess _pipedProcess;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="terminalParameter">Terminal parameter</param>
        /// <param name="socket">PipeSocket object</param>
        /// <param name="pipedProcess">Process data (or null)</param>
        public PipeTerminalConnection(PipeTerminalParameter terminalParameter, PipeSocket socket, PipedProcess pipedProcess) {
            _terminalOutput = new PipeTerminalOutput();
            _terminalParameter = terminalParameter;
            _socket = socket;
            _pipedProcess = pipedProcess;

            if (_pipedProcess != null) {
                _pipedProcess.Exited += delegate(object sender, EventArgs e) {
                    _socket.ProcessExited();
                };
            }
        }

        #region ITerminalConnection

        public ITerminalParameter Destination {
            get {
                return _terminalParameter;
            }
        }

        public ITerminalOutput TerminalOutput {
            get {
                return _terminalOutput;
            }
        }

        public IPoderosaSocket Socket {
            get {
                return _socket;
            }
        }

        public bool IsClosed {
            get {
                return _socket.IsClosed;
            }
        }

        public void Close() {
            _socket.Close();
            if (_pipedProcess != null)
                _pipedProcess.Dispose();
        }

        #endregion

        #region IAdaptable

        public IAdaptable GetAdapter(Type adapter) {
            return PipePlugin.Instance.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion

    }

}
