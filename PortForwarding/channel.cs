/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: channel.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Threading;
using System.Text;

using Granados;

namespace Poderosa.PortForwarding {
    internal class SynchronizedSSHChannel {
        private SSHChannel _channel;
        private SSHConnection _connection;
        private bool _closed;
        private bool _sentEOF;

        public SynchronizedSSHChannel(SSHChannel ch) {
            _channel = ch;
            _connection = _channel.Connection;
            _closed = false;
        }

        public void Transmit(byte[] data, int offset, int length) {
            lock (_connection) {
                if (!_closed && !_sentEOF)
                    _channel.Transmit(data, offset, length);
            }
        }

        public void Close() {
            lock (_connection) {
                if (!_closed) {
                    _closed = true;
                    _channel.Close();
                }
            }
        }

        public void SendEOF() {
            lock (_connection) {
                if (!_sentEOF && !_closed) {
                    _sentEOF = true;
                    _channel.SendEOF();
                }
            }
        }

        public int LocalChannelID {
            get {
                return _channel.LocalChannelID;
            }
        }

        public SSHConnection GranadosConnection {
            get {
                return _connection;
            }
        }
    }

    internal class SynchronizedSocket {
        private Socket _socket;
        private bool _shuttedDown_Send;
        private bool _shuttedDown_Receive;
        private bool _closed;

        public SynchronizedSocket(Socket s) {
            _socket = s;
            _shuttedDown_Send = false;
            _shuttedDown_Receive = false;
            _closed = false;
        }
        public int Send(byte[] data, int offset, int length, SocketFlags flags) {
            lock (this) {
                if (_shuttedDown_Send)
                    return 0;
                else
                    return _socket.Send(data, offset, length, flags);
            }
        }

        public void ShutdownSend() {
            lock (this) {
                if (!_shuttedDown_Send) {
                    _shuttedDown_Send = true;
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
        }
        public void ShutdownReceive() {
            lock (this) {
                if (!_shuttedDown_Receive) {
                    _shuttedDown_Receive = true;
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
        }
        public bool ShuttedDownSend {
            get {
                return _shuttedDown_Send;
            }
        }
        public bool ShuttedDownReceive {
            get {
                return _shuttedDown_Receive;
            }
        }

        public void Close() {
            lock (this) {
                if (!_closed) {
                    _closed = true;
                    _socket.Close();
                }
            }
        }

        public void BeginReceive(byte[] buf, int offset, int length, SocketFlags flags, AsyncCallback cb, object state) {
            lock (this) {
                if (!_shuttedDown_Receive) {
                    _socket.BeginReceive(buf, offset, length, flags, cb, state);
                }
            }
        }

        public int EndReceive(IAsyncResult ar) {
            lock (this) {
                if (!_shuttedDown_Receive)
                    return _socket.EndReceive(ar);
                else
                    return 0;
            }
        }
    }




    internal abstract class ChannelFactory : ISSHConnectionEventReceiver {

        public static ChannelFactory Create(ChannelProfile prof) {
            /*
            if(prof.ProtocolType==ProtocolType.Udp)
                return new UdpChannelFactory(prof);
            else
            */
            if (prof is LocalToRemoteChannelProfile)
                return new LocalToRemoteChannelFactory((LocalToRemoteChannelProfile)prof);
            else
                return new RemoteToLocalChannelFactory((RemoteToLocalChannelProfile)prof);
        }

        protected static int _id_seed = 1;
        protected int _id;
        protected SSHConnection _connection;
        protected bool _closed;
        protected bool _established;

        public ChannelFactory() {
            _id = _id_seed++;
        }
        public void FixConnection(SSHConnection con) {
            _connection = con;
            Env.Log.LogConnectionOpened(this.ChannelProfile, _id);
        }
        public SSHConnection Connection {
            get {
                return _connection;
            }
        }
        public abstract ChannelProfile ChannelProfile {
            get;
        }

        public abstract void WaitRequest();


        public void OnDebugMessage(bool always_display, byte[] msg) {
            Debug.WriteLine("DebugMessage");
        }

        public virtual void OnError(Exception error) {
            Env.Log.LogConnectionError(error.Message, _id);
            Env.Connections.ConnectionError(_connection, error);
        }

        public virtual void EstablishPortforwarding(ISSHChannelEventReceiver receiver, SSHChannel channel) {
        }

        public virtual void OnConnectionClosed() {
            Env.Log.LogConnectionClosed(this.ChannelProfile, _id);
            Env.Connections.ConnectionClosed(_connection);
        }

        public void OnIgnoreMessage(byte[] msg) {
            Debug.WriteLine("IgnoreMessage");
        }

        public virtual PortForwardingCheckResult CheckPortForwardingRequest(string remote_host, int remote_port, string originator_ip, int originator_port) {
            return new PortForwardingCheckResult();
        }

        public void OnUnknownMessage(byte type, byte[] data) {
            Debug.WriteLine("UnknownMessage");
        }

        public void OnAuthenticationPrompt(string[] prompts) {
        }
    }

    internal sealed class LocalToRemoteChannelFactory : ChannelFactory {
        private Socket _bindedLocalSocket;
        private LocalToRemoteChannelProfile _profile;

        public LocalToRemoteChannelFactory(LocalToRemoteChannelProfile prof) {
            _profile = prof;
        }
        public Socket BindedLocalSocket {
            get {
                return _bindedLocalSocket;
            }
        }
        public override ChannelProfile ChannelProfile {
            get {
                return _profile;
            }
        }


        public override void WaitRequest() {
            _bindedLocalSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _bindedLocalSocket.Bind(new IPEndPoint(Util.ChannelProfileToListeningAddress(_profile), _profile.ListenPort));
            _bindedLocalSocket.Listen(30);
            _bindedLocalSocket.BeginAccept(new AsyncCallback(OnRequested), null);

            _established = true;
        }
        public override void OnError(Exception error) {
            _closed = true;
            Debug.WriteLine("ChannelFactory OnError " + error.Message);
            Debug.WriteLine(error.StackTrace);
            if (_established) {
                try {
                    _bindedLocalSocket.Close();
                }
                catch (Exception) {
                    Debug.WriteLine("Binded Socket Close Error");
                }
            }
            base.OnError(error);
        }
        public override void OnConnectionClosed() {
            _closed = true;
            Debug.WriteLine("ChannelFactory OnConnecitonClosed");
            if (_established) {
                try {
                    _bindedLocalSocket.Close();
                }
                catch (Exception) {
                    Debug.WriteLine("Binded Socket Close Error");
                }
            }
            base.OnConnectionClosed();
        }
        private void OnRequested(IAsyncResult r) {
            try {
                if (_closed)
                    return; //SSH切断があったときは非同期受信から戻ってくるが、EndAcceptを呼んでもObjectDisposedExceptionになるだけ
                Socket local = _bindedLocalSocket.EndAccept(r);
                //Port Forwarding
                Channel newchannel = new Channel(_profile.SSHHost, local.RemoteEndPoint.ToString(), _id, null, local);
                lock (_connection) {
                    SSHChannel remote = _connection.ForwardPort(newchannel, _profile.DestinationHost, _profile.DestinationPort, "localhost", 0); //!!最後の２つの引数未完
                    Debug.WriteLine("OnRequested ch=" + remote.LocalChannelID);
                    newchannel.FixChannel(remote);
                    newchannel.StartAsyncReceive();
                }
            }
            catch (Exception ex) {
                if (!_closed) {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                    Util.InterThreadWarning(ex.Message);
                }
            }
            finally {
                if (!_closed) {
                    _bindedLocalSocket.BeginAccept(new AsyncCallback(OnRequested), null);
                    Debug.WriteLine("BeginAccept again");
                }
            }
        }
    }

    internal sealed class RemoteToLocalChannelFactory : ChannelFactory {
        private RemoteToLocalChannelProfile _profile;

        public RemoteToLocalChannelFactory(RemoteToLocalChannelProfile prof) {
            _profile = prof;
        }
        public override ChannelProfile ChannelProfile {
            get {
                return _profile;
            }
        }

        public override void WaitRequest() {
            _connection.ListenForwardedPort("0.0.0.0", _profile.ListenPort);
            _established = true;
        }
        public override void EstablishPortforwarding(ISSHChannelEventReceiver receiver, SSHChannel channel) {
            try {
                Channel ch = (Channel)receiver;
                ch.FixChannel(channel);
                ch.OnChannelReady();
                ch.StartAsyncReceive();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.StackTrace);
                Util.InterThreadWarning(ex.Message);
            }
        }
        public override PortForwardingCheckResult CheckPortForwardingRequest(string remote_host, int remote_port, string originator_ip, int originator_port) {
            PortForwardingCheckResult r = new PortForwardingCheckResult();
            try {
                if (!_profile.AllowsForeignConnection && originator_ip != "127.0.0.1") {
                    r.allowed = false;
                    r.reason_message = "refused";
                    return r;
                }

                Socket local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                local.Connect(new IPEndPoint(Util.ResolveHost(_profile.DestinationHost), _profile.DestinationPort));

                r.allowed = true;
                r.channel = new Channel(_profile.SSHHost, originator_ip, _id, null, local);
                return r;
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.StackTrace);
                Util.InterThreadWarning(ex.Message);
                r.allowed = false;
                r.reason_message = "refused";
                return r;
            }
        }
    }


    //SSHChannelとSocketで相互にデータの受け渡しをする。片方が閉じたらもう片方も閉じる。
    internal class Channel : ISSHChannelEventReceiver {

        private string _serverName;
        private string _remoteDescription;
        private int _connectionID;
        private bool _wroteClosedLog;

        private SynchronizedSSHChannel _channel;
        private SynchronizedSocket _socket;
        private byte[] _buffer;

        private ManualResetEvent _channelReady;

        public Channel(string servername, string rd, int cid, SSHChannel channel, Socket socket) {
            _serverName = servername;
            _remoteDescription = rd;
            _connectionID = cid;
            _wroteClosedLog = false;

            if (channel != null)
                _channel = new SynchronizedSSHChannel(channel);
            _socket = new SynchronizedSocket(socket);
            _buffer = new byte[0x1000];
            _channelReady = new ManualResetEvent(false);
        }
        public void FixChannel(SSHChannel ch) {
            _channel = new SynchronizedSSHChannel(ch);
            Env.Log.LogChannelOpened(_remoteDescription, _connectionID);
        }
        public void StartAsyncReceive() {
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(this.OnSocketData), null);
        }

        public void OnData(byte[] data, int offset, int length) {
            //Debug.WriteLine(String.Format("OnSSHData ch={0} len={1}", _channel.LocalChannelID, length));
            if (!_socket.ShuttedDownSend)
                _socket.Send(data, offset, length, SocketFlags.None);
        }

        public void OnChannelError(Exception error) {
            Debug.WriteLine(String.Format("OnChannelError ch={0}", _channel.LocalChannelID));
            _channelReady.Set();

            //_socket.ShutdownSend();
            //_socket.ShutdownReceive();
            try {
                _socket.Close();
                _channel.Close();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            Util.InterThreadWarning(String.Format(Env.Strings.GetString("Message.Channel.ServerError"), _serverName, error.Message));
        }

        public void OnChannelEOF() {
            Debug.WriteLine(String.Format("OnChannelEOF ch={0}", _channel.LocalChannelID));
            try {
                Env.Log.LogChannelClosed(_remoteDescription, _connectionID);
                _socket.ShutdownSend();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public void OnChannelClosed() {
            try {
                Debug.WriteLine(String.Format("OnChannelClosed ch={0}", _channel.LocalChannelID));
                _channel.Close();
                _socket.Close();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public void OnChannelReady() {
            Debug.WriteLine(String.Format("ChannelReady"));
            _channelReady.Set();
        }

        public void OnExtendedData(int type, byte[] data) {

        }
        public void OnMiscPacket(byte type, byte[] data, int offset, int length) {
        }

        private void WriteChannelCloseLog() {
            if (_wroteClosedLog)
                return;
            _wroteClosedLog = true;
            Env.Log.LogChannelClosed(_remoteDescription, _connectionID);
        }

        private void OnSocketData(IAsyncResult result) {
            try {
                int len = _socket.EndReceive(result);
                if (!_channelReady.WaitOne(3000, false))
                    throw new IOException("channel ready timed out");

                //Debug.WriteLine(String.Format("OnSocketData ch={0} len={1}", _channel.LocalChannelID, len));
                if (len <= 0) {
                    _socket.ShutdownReceive();
                    _channel.SendEOF();
                    _channel.Close();
                }
                else {
                    _channel.Transmit(_buffer, 0, len);
                    _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(this.OnSocketData), null);
                }
            }
            catch (Exception ex) {
                Debug.WriteLine("OnSocketData catch handler" + _channel.LocalChannelID);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                try {
                    _channel.Close();
                }
                catch (Exception ex2) {
                    Debug.WriteLine("Channel Close Error");
                    Debug.WriteLine(ex2.Message);
                    Debug.WriteLine(ex2.StackTrace);
                }

                try {
                    _socket.Close();
                }
                catch (Exception ex2) {
                    Debug.WriteLine("Socket Close Error");
                    Debug.WriteLine(ex2.Message);
                    Debug.WriteLine(ex2.StackTrace);
                }
            }
        }
    }

}
