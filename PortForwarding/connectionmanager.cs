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
using System.Collections;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows.Forms;

using Granados;
using Poderosa.Toolkit;
using Granados.KnownHosts;

namespace Poderosa.PortForwarding {
    //ホスト名/IPアドレスからSSHのコネクションへのマップを管理する
    internal class ConnectionManager {
        private Hashtable _profileToConnection;
        private ArrayList _manualClosingConnections;

        public ConnectionManager() {
            _profileToConnection = new Hashtable();
            _manualClosingConnections = new ArrayList();
        }

        //profに対応したSSHConnectionを返す。接続がなければparentを親に認証ダイアログを出して認証する
        public ISSHConnection GetOrCreateConnection(ChannelProfile prof, Form parent) {
            //ホスト名とアカウントのペアからコネクションを共有する仕組みがあるとよいかも
            ISSHConnection c = _profileToConnection[prof] as ISSHConnection;
            if (c != null)
                return c;

            SSHShortcutLoginDialog dlg = new SSHShortcutLoginDialog(prof);
            if (dlg.ShowDialog(parent) == DialogResult.OK) {
                c = dlg.Result.Connection;
                try {
                    dlg.Result.WaitRequest();
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.StackTrace);
                    Util.Warning(parent, ex.Message);
                    c.Close();
                    return null;
                }
                _profileToConnection[prof] = c;
                Env.MainForm.RefreshProfileStatus(prof);
            }

            return c;
        }
        public bool IsConnected(ChannelProfile prof) {
            return _profileToConnection[prof] != null;
        }
        public bool HasConnection {
            get {
                return _profileToConnection.Count > 0;
            }
        }

        public static SocketWithTimeout StartNewConnection(ISocketWithTimeoutClient client, ChannelProfile prof, string password, VerifySSHHostKeyDelegate keycheck) {
            SocketWithTimeout swt;
            swt = new SSHConnector(prof, password, keycheck);
            /*
            if (Env.Options.UseSocks)
                swt.AsyncConnect(client, CreateSocksParam(prof.SSHHost, prof.SSHPort));
            else*/
            swt.AsyncConnect(client, prof.SSHHost, prof.SSHPort);
            return swt;
        }
        /*
        private static Socks CreateSocksParam(string dest_host, int dest_port) {
            Socks s = new Socks();
            s.DestName = dest_host;
            s.DestPort = (short)dest_port;
            s.Account = Env.Options.SocksAccount;
            s.Password = Env.Options.SocksPassword;
            s.ServerName = Env.Options.SocksServer;
            s.ServerPort = (short)Env.Options.SocksPort;
            s.ExcludingNetworks = Env.Options.SocksNANetworks;
            return s;
        }
        */
        public void ManualClose(ChannelProfile prof) {
            if (!IsConnected(prof)) {
                Debug.WriteLine("ManualClose - Not connected");
                return;
            }

            lock (this) {
                ISSHConnection c = (ISSHConnection)_profileToConnection[prof];
                _manualClosingConnections.Add(c);
                c.Disconnect(DisconnectionReasonCode.ByApplication, "close by application");
            }
        }
        public void CloseAll() {
            foreach (ChannelProfile prof in new ArrayList(_profileToConnection.Keys)) {
                if (IsConnected(prof))
                    ManualClose(prof);
            }
        }

        private delegate void RefreshProfileStatusDelegate(ChannelProfile prof);

        //終了のハンドリング 非同期に別スレッドから呼ばれるので注意
        public void ConnectionClosed(ISSHConnection connection) {
            IDictionaryEnumerator e = _profileToConnection.GetEnumerator();
            while (e.MoveNext()) {
                if (connection == e.Value) {
                    ChannelProfile prof = (ChannelProfile)e.Key;
                    _profileToConnection.Remove(e.Key);
                    bool manual = false;
                    lock (this) {
                        manual = _manualClosingConnections.Contains(connection);
                        if (manual)
                            _manualClosingConnections.Remove(connection);
                    }

                    if (!manual) {
                        Util.InterThreadWarning(Env.Strings.GetString("Message.ConnectionManager.Disconnected"));
                    }

                    Env.MainForm.Invoke(new RefreshProfileStatusDelegate(Env.MainForm.RefreshProfileStatus), prof);

                    break;
                }
            }
        }
        public void ConnectionError(ISSHConnection connection, Exception error) {
            Debug.WriteLine(error.StackTrace);
            Util.InterThreadWarning(error.Message);
            ConnectionClosed(connection);
        }

    }

    internal class SSHConnector : SocketWithTimeout {

        private ChannelProfile _profile;
        private string _password;
        private VerifySSHHostKeyDelegate _keycheck;
        private ChannelFactory _result;

        public SSHConnector(ChannelProfile prof, string password, VerifySSHHostKeyDelegate keycheck) {
            _profile = prof;
            _password = password;
            _keycheck = keycheck;
        }
        protected override string GetHostDescription() {
            return "";
        }

        protected override void Negotiate() {
            SSHConnectionParameter con = new SSHConnectionParameter(_host, _port, SSHProtocol.SSH2, _profile.AuthType, _profile.SSHAccount, _password);
            con.IdentityFile = _profile.PrivateKeyFile;
            con.PreferableCipherAlgorithms = SSHUtil.ParseCipherAlgorithm(Env.Options.CipherAlgorithmOrder);
            con.PreferableHostKeyAlgorithms = SSHUtil.ParsePublicKeyAlgorithm(Env.Options.HostKeyAlgorithmOrder);
            con.WindowSize = Env.Options.SSHWindowSize;
            con.CheckMACError = Env.Options.SSHCheckMAC;
            if (_keycheck != null)
                con.VerifySSHHostKey = this.CheckKey;

            _result = ChannelFactory.Create(_profile);
            ISSHConnection c = SSHConnection.Connect(
                                _socket, con,
                                sshconn => _result, null);
            if (c != null) {
                /*
                if(_profile.ProtocolType==ProtocolType.Udp)
                    OpenUdpDestination(c, (UdpChannelFactory)_result);
                else
                */
                _result.FixConnection(c);
                if (Env.Options.RetainsPassphrase)
                    _profile.Passphrase = _password; //接続成功時のみセット
            }
            else {
                throw new IOException(Env.Strings.GetString("Message.ConnectionManager.ConnectionCancelled"));
            }
        }
        protected override object Result {
            get {
                return _result;
            }
        }

        private bool CheckKey(ISSHHostKeyInformationProvider info) {
            SetIgnoreTimeout(); //これが呼ばれるということは途中までSSHのネゴシエートができているのでタイムアウトはしないようにする
            return _keycheck(info);
        }

    }
}
