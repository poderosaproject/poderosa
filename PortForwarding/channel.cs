/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: channel.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using Granados;
using Granados.IO;
using Granados.PortForwarding;

namespace Poderosa.PortForwarding {
    internal class SynchronizedSSHChannel {
        private ISSHChannel _channel;
        private bool _closed;
        private bool _sentEOF;
        private readonly object _sync = new object();

        public SynchronizedSSHChannel(ISSHChannel ch) {
            _channel = ch;
            _closed = false;
        }

        public void Transmit(byte[] data, int offset, int length) {
            lock (_sync) {
                if (!_closed && !_sentEOF) {
                    _channel.Send(new DataFragment(data, offset, length));
                }
            }
        }

        public void Close() {
            lock (_sync) {
                if (!_closed) {
                    _closed = true;
                    _channel.Close();
                }
            }
        }

        public void SendEOF() {
            lock (_sync) {
                if (!_sentEOF && !_closed) {
                    _sentEOF = true;
                    _channel.SendEOF();
                }
            }
        }

        public uint LocalChannelID {
            get {
                return _channel.LocalChannel;
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




    internal abstract class ChannelFactory : ISSHConnectionEventHandler {

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
        protected ISSHConnection _connection;
        protected bool _closed;
        protected bool _established;

        public ChannelFactory() {
            _id = _id_seed++;
        }
        public void FixConnection(ISSHConnection con) {
            _connection = con;
            Env.Log.LogConnectionOpened(this.ChannelProfile, _id);
        }
        public ISSHConnection Connection {
            get {
                return _connection;
            }
        }
        public abstract ChannelProfile ChannelProfile {
            get;
        }

        public abstract void WaitRequest();


        public void OnDebugMessage(bool alwaysDisplay, string message) {
            Debug.WriteLine("DebugMessage");
        }

        public virtual void OnError(Exception error) {
            Env.Log.LogConnectionError(error.Message, _id);
            Env.Connections.ConnectionError(_connection, error);
        }

        public virtual void OnConnectionClosed() {
            Env.Log.LogConnectionClosed(this.ChannelProfile, _id);
            Env.Connections.ConnectionClosed(_connection);
        }

        public void OnIgnoreMessage(byte[] msg) {
            Debug.WriteLine("IgnoreMessage");
        }

        public virtual RemotePortForwardingReply CheckPortForwardingRequest(string remote_host, int remote_port, string originator_ip, int originator_port) {
            return RemotePortForwardingReply.Reject(RemotePortForwardingReply.Reason.AdministrativelyProhibited, "rejected");
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
                Channel newChannel;
                lock (_connection) {
                    newChannel = _connection.ForwardPort(
                        channel => {
                            return new Channel(_profile.SSHHost, local.RemoteEndPoint.ToString(), _id, channel, local);
                        },
                        _profile.DestinationHost,
                        _profile.DestinationPort,
                        "localhost",    // FIXME
                        0   // FIXME
                    );
                    newChannel.StartAsyncReceive();
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

    internal sealed class RemoteToLocalChannelFactory : ChannelFactory, IRemotePortForwardingHandler {
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
            bool success = _connection.ListenForwardedPort(this, "0.0.0.0", _profile.ListenPort);
            if (!success) {
                throw new Exception("starting remote port-forwarding failed.");
            }

            _established = true;
        }

        public void OnRemotePortForwardingStarted(uint port) {
            Debug.WriteLine("port forwarding started: port = {0}", port);
        }

        public void OnRemotePortForwardingFailed() {
            Debug.WriteLine("port forwarding failed");
        }

        public RemotePortForwardingReply OnRemotePortForwardingRequest(RemotePortForwardingRequest request, ISSHChannel channel) {
            try {
                if (!_profile.AllowsForeignConnection && !IsLoopbackAddress(request.OriginatorIp)) {
                    return RemotePortForwardingReply.Reject(
                            RemotePortForwardingReply.Reason.AdministrativelyProhibited, "rejected");
                }

                Socket local = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                local.Connect(new IPEndPoint(Util.ResolveHost(_profile.DestinationHost), _profile.DestinationPort));

                var newChannel =
                    new Channel(
                        _profile.SSHHost, _profile.DestinationHost, (int)channel.LocalChannel, channel, local);

                newChannel.StartAsyncReceive();

                return RemotePortForwardingReply.Accept(newChannel);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.StackTrace);
                Util.InterThreadWarning(ex.Message);
                return RemotePortForwardingReply.Reject(
                        RemotePortForwardingReply.Reason.AdministrativelyProhibited, "rejected");
            }
        }

        private bool IsLoopbackAddress(string ip) {
            IPAddress ipAddr;
            if (IPAddress.TryParse(ip, out ipAddr)) {
                return IPAddress.IsLoopback(ipAddr);
            }
            return false;
        }
    }


    //SSHChannelとSocketで相互にデータの受け渡しをする。片方が閉じたらもう片方も閉じる。
    internal class Channel : SimpleSSHChannelEventHandler {

        private string _serverName;
        private string _remoteDescription;
        private int _connectionID;
        private bool _wroteClosedLog;

        private SynchronizedSSHChannel _channel;
        private SynchronizedSocket _socket;
        private byte[] _buffer;

        private ManualResetEvent _channelReady;

        public Channel(string servername, string rd, int cid, ISSHChannel channel, Socket socket) {
            _serverName = servername;
            _remoteDescription = rd;
            _connectionID = cid;
            _wroteClosedLog = false;

            _channel = new SynchronizedSSHChannel(channel);
            _socket = new SynchronizedSocket(socket);
            _buffer = new byte[0x1000];
            _channelReady = new ManualResetEvent(false);
        }

        public void StartAsyncReceive() {
            _socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(this.OnSocketData), null);
        }

        public override void OnData(DataFragment data) {
            //Debug.WriteLine(String.Format("OnSSHData ch={0} len={1}", _channel.LocalChannelID, length));
            if (!_socket.ShuttedDownSend)
                _socket.Send(data.Data, data.Offset, data.Length, SocketFlags.None);
        }

        public override void OnError(Exception error) {
            Debug.WriteLine(String.Format("OnChannelError ch={0}", _channel.LocalChannelID));
            _channelReady.Set();

            //_socket.ShutdownSend();
            //_socket.ShutdownReceive();
            try {
                _socket.ShutdownReceive();
                _socket.Close();
                _channel.Close();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            Util.InterThreadWarning(String.Format(Env.Strings.GetString("Message.Channel.ServerError"), _serverName, error.Message));
        }

        public override void OnEOF() {
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

        public override void OnClosed(bool byServer) {
            try {
                Debug.WriteLine(String.Format("OnChannelClosed ch={0}", _channel.LocalChannelID));
                _socket.ShutdownReceive();
                _socket.Close();
                _channel.Close();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        public override void OnReady() {
            Debug.WriteLine(String.Format("ChannelReady"));
            _channelReady.Set();
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
