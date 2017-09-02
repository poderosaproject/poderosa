// Copyright 2005-2017 The Poderosa Project.
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
using System.IO;

namespace Poderosa.PortForwarding {
    /// <summary>
    /// ConnectionLog の概要の説明です。
    /// </summary>
    internal class ConnectionLog {
        private StreamWriter _strm;

        public ConnectionLog(string filename) {
            _strm = new StreamWriter(filename, true);
        }
        public void Close() {
            _strm.Close();
        }

        public void LogConnectionOpened(ChannelProfile prof, int id) {
            string t = String.Format("{0} {1} connection opened; id={2}", DateTime.Now.ToString(), prof.ProtocolType.ToString(), id);
            if (prof is LocalToRemoteChannelProfile)
                t = String.Format("{0}; from=localhost:{1}; to={2}:{3}", t, prof.ListenPort, prof.DestinationHost, prof.DestinationPort);
            else
                t = String.Format("{0}; from={1}:{2}; to={3}:{4}", t, prof.SSHHost, prof.ListenPort, prof.DestinationHost, prof.DestinationPort);

            _strm.WriteLine(t);
            _strm.Flush();
        }
        public void LogConnectionError(string msg, int id) {
            string t = String.Format("{0} connection error; id={1}; msg={2}", DateTime.Now.ToString(), id, msg);

            _strm.WriteLine(t);
            _strm.Flush();
        }
        public void LogConnectionClosed(ChannelProfile prof, int id) {
            string t = String.Format("{0} {1} connection closed; id={2}", DateTime.Now.ToString(), prof.ProtocolType.ToString(), id);

            _strm.WriteLine(t);
            _strm.Flush();
        }
        public void LogChannelOpened(string originator, int id) {
            string t = String.Format("{0} channel opened; id={1}; originator={2}", DateTime.Now.ToString(), id, originator);

            _strm.WriteLine(t);
            _strm.Flush();
        }
        public void LogChannelClosed(string originator, int id) {
            string t = String.Format("{0} channel closed; id={1}; originator={2}", DateTime.Now.ToString(), id, originator);

            _strm.WriteLine(t);
            _strm.Flush();
        }
        public void LogUdp(string originator, int id) {
            string t = String.Format("{0} udp data; id={1}; originator={2}", DateTime.Now.ToString(), originator, id);

            _strm.WriteLine(t);
            _strm.Flush();
        }
    }
}
