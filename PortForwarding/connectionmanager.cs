/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.

 $Id: connectionmanager.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.IO;
using System.Collections;
using System.Net;
using System.Diagnostics;
using System.Net.Sockets;
using System.Windows.Forms;

using Granados;
using Poderosa.Toolkit;

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
        public SSHConnection GetOrCreateConnection(ChannelProfile prof, Form parent) {
            //ホスト名とアカウントのペアからコネクションを共有する仕組みがあるとよいかも
            SSHConnection c = (SSHConnection)_profileToConnection[prof];
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

        public static SocketWithTimeout StartNewConnection(ISocketWithTimeoutClient client, ChannelProfile prof, string password, HostKeyCheckCallback keycheck) {
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
                SSHConnection c = (SSHConnection)_profileToConnection[prof];
                _manualClosingConnections.Add(c);
                c.Disconnect("");
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
        public void ConnectionClosed(SSHConnection connection) {
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
        public void ConnectionError(SSHConnection connection, Exception error) {
            Debug.WriteLine(error.StackTrace);
            Util.InterThreadWarning(error.Message);
            ConnectionClosed(connection);
        }

    }

    internal class SSHConnector : SocketWithTimeout {

        private ChannelProfile _profile;
        private string _password;
        private HostKeyCheckCallback _keycheck;
        private ChannelFactory _result;

        public SSHConnector(ChannelProfile prof, string password, HostKeyCheckCallback keycheck) {
            _profile = prof;
            _password = password;
            _keycheck = keycheck;
        }
        protected override string GetHostDescription() {
            return "";
        }

        protected override void Negotiate() {
            SSHConnectionParameter con = new SSHConnectionParameter();
            con.Protocol = SSHProtocol.SSH2;
            con.UserName = _profile.SSHAccount;
            con.Password = _password;
            con.AuthenticationType = _profile.AuthType;
            con.IdentityFile = _profile.PrivateKeyFile;
            con.PreferableCipherAlgorithms = SSHUtil.ParseCipherAlgorithm(Env.Options.CipherAlgorithmOrder);
            con.PreferableHostKeyAlgorithms = SSHUtil.ParsePublicKeyAlgorithm(Env.Options.HostKeyAlgorithmOrder);
            con.WindowSize = Env.Options.SSHWindowSize;
            con.CheckMACError = Env.Options.SSHCheckMAC;
            if (_keycheck != null)
                con.KeyCheck += new HostKeyCheckCallback(this.CheckKey);

            _result = ChannelFactory.Create(_profile);
            SSHConnection c = SSHConnection.Connect(con, _result, _socket);
            c.AutoDisconnect = false;
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

        private bool CheckKey(SSHConnectionInfo ci) {
            SetIgnoreTimeout(); //これが呼ばれるということは途中までSSHのネゴシエートができているのでタイムアウトはしないようにする
            return _keycheck(ci);
        }

    }
}
