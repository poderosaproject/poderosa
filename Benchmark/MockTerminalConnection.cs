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

namespace Poderosa.Benchmark {

    /// <summary>
    /// An implementation of ITerminalConnection using MockSocket.
    /// </summary>
    internal class MockTerminalConnection : ITerminalConnection, ITerminalOutput {

        private readonly ITerminalParameter _param;
        private readonly MockSocket _mockSocket;


        public MockTerminalConnection(string terminalType, MockSocket mockSocket) {
            _param = new TerminalParameter();
            _param.SetTerminalName(terminalType);
            _mockSocket = mockSocket;
        }

        public ITerminalParameter Destination {
            get {
                return _param;
            }
        }

        public ITerminalOutput TerminalOutput {
            get {
                return this;
            }
        }

        public IPoderosaSocket Socket {
            get {
                return _mockSocket;
            }
        }

        public bool IsClosed {
            get {
                return _mockSocket.IsClosed;
            }
        }

        public void Close() {
            _mockSocket.Close();
        }

        public IAdaptable GetAdapter(Type adapter) {
            return BenchmarkPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public void SendBreak() {
            // do nothing
        }

        public void SendKeepAliveData() {
            // do nothing
        }

        public void AreYouThere() {
            // do nothing
        }

        public void Resize(int width, int height) {
            // do nothing
        }

    }
}

