// Copyright 2004-2025 The Poderosa Project.
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
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;

using Poderosa.Util;

using Granados;
using Granados.SSH2;
using Granados.KeyboardInteractive;

namespace Poderosa.Protocols {
    internal class PlainPoderosaSocket : IPoderosaSocketInet {
        private IByteAsyncInputStream _callback;
        private readonly Socket _socket;
        private readonly string _remote;
        private readonly IPEndPoint _endPoint;
        private readonly byte[] _buf;
        private readonly ByteDataFragment _dataFragment;
        private readonly AsyncCallback _callbackRoot;
        private readonly TerminalConnection _ownerConnection;

        public PlainPoderosaSocket(TerminalConnection owner, Socket s, string remote) {
            _ownerConnection = owner;
            _remote = remote;
            _endPoint = s.RemoteEndPoint as IPEndPoint;
            _socket = s;
            _buf = new byte[ProtocolsPlugin.Instance.ProtocolOptions.SocketBufferSize];
            _dataFragment = new ByteDataFragment(_buf, 0, 0);
            _callbackRoot = new AsyncCallback(RepeatCallback);
        }

        public void Transmit(ByteDataFragment data) {
            _socket.Send(data.Buffer, data.Offset, data.Length, SocketFlags.None);
        }
        public void Transmit(byte[] data, int offset, int length) {
            _socket.Send(data, offset, length, SocketFlags.None);
        }
        public void Close() {
            try {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Disconnect(false);
                Debug.WriteLineIf(DebugOpt.Socket, "PlainSocket close");
            }
            catch (Exception ex) {
                RuntimeUtil.SilentReportException(ex);
            }
        }
        public void ForceDisposed() {
            _socket.Close();
        }

        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            _callback = receiver;
            BeginReceive();
        }

        private void RepeatCallback(IAsyncResult result) {
            try {
                int n = _socket.EndReceive(result);
                _dataFragment.Set(_buf, 0, n);
                Debug.Assert(_ownerConnection != null); //これを呼び出すようになるまでにはセットされていること！

                if (n > 0) {
                    if (OnReceptionCore(_dataFragment) == GenericResult.Succeeded)
                        BeginReceive();
                }
                else if (n < 0) {
                    //WindowsMEにおいては、ときどきここで-1が返ってきていることが発覚した。下のErrorCode 995の場合も同様
                    BeginReceive();
                }
                else {
                    OnNormalTerminationCore();
                }
            }
            catch (ObjectDisposedException) {
                // _socket has been closed
                OnNormalTerminationCore();
            }
            catch (Exception ex) {
                if (!_ownerConnection.IsClosed) {
                    RuntimeUtil.SilentReportException(ex);
                    if ((ex is SocketException) && ((SocketException)ex).ErrorCode == 995) {
                        BeginReceive();
                    }
                    else
                        OnAbnormalTerminationCore(ex.Message);
                }
            }
        }

        //IByteAsuncInputStreamのハンドラで例外が来るとけっこう惨事なのでこの中でしっかりガード

        private GenericResult OnReceptionCore(ByteDataFragment data) {
            try {
                _callback.OnReception(_dataFragment);
                return GenericResult.Succeeded;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                Close();
                return GenericResult.Failed;
            }
        }

        private GenericResult OnNormalTerminationCore() {
            try {
                _ownerConnection.CloseBySocket();
                _callback.OnNormalTermination();
                return GenericResult.Succeeded;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                _socket.Disconnect(false);
                return GenericResult.Failed;
            }
        }

        private GenericResult OnAbnormalTerminationCore(string msg) {
            try {
                _ownerConnection.CloseBySocket();
                _callback.OnAbnormalTermination(msg);
                return GenericResult.Succeeded;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                _socket.Disconnect(false);
                return GenericResult.Failed;
            }
        }

        public bool Available {
            get {
                return _socket.Available > 0;
            }
        }

        private void BeginReceive() {
            _socket.BeginReceive(_buf, 0, _buf.Length, SocketFlags.None, _callbackRoot, null);
        }

        public string Remote {
            get {
                return _remote;
            }
        }

        public IPAddress RemoteAddress {
            get {
                return (_endPoint != null) ? _endPoint.Address : null;
            }
        }

        public int? RemotePortNumber {
            get {
                return (_endPoint != null) ? _endPoint.Port : (int?)null;
            }
        }
    }

    //送信したものをそのまま戻す
    internal class LoopbackSocket : IPoderosaSocket {
        private IByteAsyncInputStream _receiver;

        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            _receiver = receiver;
        }

        public bool Available {
            get {
                return false;
            }
        }

        public void ForceDisposed() {
        }

        public void Transmit(ByteDataFragment data) {
            _receiver.OnReception(data);
        }

        public void Transmit(byte[] data, int offset, int length) {
            Transmit(new ByteDataFragment(data, offset, length));
        }

        public void Close() {
        }

        public string Remote {
            get {
                return "(loopback)";
            }
        }
    }


    internal class ConnectionStats {
        private int _sentDataAmount;
        private int _receivedDataAmount;

        public int SentDataAmount {
            get {
                return _sentDataAmount;
            }
        }
        public int ReceivedDataAmount {
            get {
                return _receivedDataAmount;
            }
        }
        public void AddSentDataStats(int bytecount) {
            //_sentPacketCount++;
            _sentDataAmount += bytecount;
        }
        public void AddReceivedDataStats(int bytecount) {
            //_receivedPacketCount++;
            _receivedDataAmount += bytecount;
        }
    }

    internal abstract class TerminalConnection : ITerminalConnection {
        protected readonly ITerminalParameter _destination;
        protected readonly ConnectionStats _stats;

        //すでにクローズされたかどうかのフラグ
        protected bool _closed;

        protected TerminalConnection(ITerminalParameter p) {
            _destination = p;
            _stats = new ConnectionStats();
        }

        public ITerminalParameter Destination {
            get {
                return _destination;
            }
        }

        // Note:
        //  Many of the concrete classes of ITerminalOutput and IPoderosaSocket are designed to have mutual references to TerminalConnection.
        //  For easier setup of mutual references, ITerminalOutput and IPoderosaSocket are returned by the derived class as follows.
        //
        // class DerivedTerminalConnection : TerminalConnection {
        //     private readonly ITerminalOutput _terminalOutput;
        //     private readonly IPoderosaSocket _poderosaSocket;
        //
        //     DerivedTerminalConnection(...) {
        //         _terminalOutput = new TerminalOutputImpl(this); // make mutual reference
        //         _poderosaSocket = new PoderosaSocketImpl(this); // make mutual reference
        //     }
        //
        //     public ITerminalOutput TerminalOutput { get { return _terminalOutput; } }
        //     public IPoderosaSocket Socket { get { return _poderosaSocket; } }
        // }

        public abstract ITerminalOutput TerminalOutput {
            get;
        }

        public abstract IPoderosaSocket Socket {
            get;
        }

        public bool IsClosed {
            get {
                return _closed;
            }
        }

        //ソケット側でエラーが起きたときの処置
        public void CloseBySocket() {
            if (!_closed)
                CloseCore();
        }

        //終了処理
        public virtual void Close() {
            if (!_closed)
                CloseCore();
        }

        private void CloseCore() {
            _closed = true;
        }

        public virtual IAdaptable GetAdapter(Type adapter) {
            return ProtocolsPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }

    internal abstract class TCPTerminalConnection : TerminalConnection {

        protected TCPTerminalConnection(ITCPParameter p)
            : base((ITerminalParameter)p.GetAdapter(typeof(ITerminalParameter))) {
        }
    }


    internal class SSHTerminalConnection : TCPTerminalConnection {

        private readonly SSHSocket _sshSocket;
        private readonly ISSHLoginParameter _sshLoginParameter;

        public SSHTerminalConnection(ISSHLoginParameter ssh, string remote, IPEndPoint endPoint)
            : base((ITCPParameter)ssh.GetAdapter(typeof(ITCPParameter))) {
            _sshLoginParameter = ssh;
            _sshSocket = new SSHSocket(this, remote, endPoint);
        }

        public override ITerminalOutput TerminalOutput {
            get {
                return _sshSocket;
            }
        }

        public override IPoderosaSocket Socket {
            get {
                return _sshSocket;
            }
        }

        public ISSHConnectionEventHandler ConnectionEventReceiver {
            get {
                return _sshSocket;
            }
        }
        public ISSHLoginParameter SSHLoginParameter {
            get {
                return _sshLoginParameter;
            }
        }
        public IKeyboardInteractiveAuthenticationHandler GetKeyboardInteractiveAuthenticationHandler() {
            return _sshSocket;
        }

        public void AttachTransmissionSide(ISSHConnection con, AuthenticationStatus authStatus) {
            _sshSocket.SetSSHConnection(con);
            if (authStatus == AuthenticationStatus.Success) {
                SSHSocket ss = (SSHSocket)_sshSocket;
                ss.OpenShell();
            }
            else if (authStatus == AuthenticationStatus.NeedKeyboardInput) {
                SSHSocket ss = (SSHSocket)_sshSocket;
                ss.OpenKeyboardInteractiveShell();
            }
        }

        public override void Close() {
            if (_closed)
                return; //２度以上クローズしても副作用なし 
            base.Close();
            _sshSocket.Close();
        }

#if false
        //BACK-BURNER: keyboard-interactive
        public override void Write(byte[] buf) {
            if (_connection.AuthenticationResult == AuthenticationResult.Prompt)
                InputAuthenticationResponse(buf, 0, buf.Length);
            else {
                AddSentDataStats(buf.Length);
                _channel.Transmit(buf);
            }
        }
        public override void Write(byte[] buf, int offset, int length) {
            if (_connection.AuthenticationResult == AuthenticationResult.Prompt)
                InputAuthenticationResponse(buf, offset, length);
            else {
                AddSentDataStats(length);
                _channel.Transmit(buf, offset, length);
            }
        }

        //authentication process for keyboard-interactive
        private void InputAuthenticationResponse(byte[] buf, int offset, int length) {
            for (int i = offset; i < offset + length; i++) {
                byte b = buf[i];
                if (_passwordBuffer == null)
                    _passwordBuffer = new MemoryStream();
                if (b == 13 || b == 10) { //CR/LF
                    byte[] pwd = _passwordBuffer.ToArray();
                    if (pwd.Length > 0) {
                        _passwordBuffer.Close();
                        string[] response = new string[1];
                        response[0] = Encoding.ASCII.GetString(pwd);
                        OnData(Encoding.ASCII.GetBytes("\r\n"), 0, 2); //表示上改行しないと格好悪い
                        if (((Granados.SSHCV2.SSH2Connection)_connection).DoKeyboardInteractiveAuth(response) == AuthenticationResult.Success)
                            _channel = _connection.OpenShell(this);
                        _passwordBuffer = null;
                    }
                }
                else if (b == 3 || b == 27) { //Ctrl+C, ESC
                    GEnv.GetConnectionCommandTarget(this).Disconnect();
                    return;
                }
                else
                    _passwordBuffer.WriteByte(b);
            }
        }
#endif

    }

    internal class TelnetReceiver : IByteAsyncInputStream {
        private IByteAsyncInputStream _callback;
        private readonly TelnetNegotiator _negotiator;
        private readonly TelnetTerminalConnection _parent;
        private readonly ByteDataFragment _localdata;
        private bool _gotCR;

        public TelnetReceiver(TelnetTerminalConnection parent, TelnetNegotiator negotiator) {
            _parent = parent;
            _negotiator = negotiator;
            _localdata = new ByteDataFragment();
        }

        public void SetReceiver(IByteAsyncInputStream receiver) {
            _callback = receiver;
        }

        public void OnReception(ByteDataFragment data) {
            ProcessBuffer(data);
            if (!_parent.IsClosed)
                _negotiator.Flush(_parent.RawSocket);
        }

        public void OnNormalTermination() {
            _callback.OnNormalTermination();
        }

        public void OnAbnormalTermination(string msg) {
            _callback.OnAbnormalTermination(msg);
        }

        public void SetTerminalSize(int width, int height) {
            _negotiator.SetTerminalSize(width, height);
        }

        //CR NUL -> CR 変換および IACからはじまるシーケンスの処理
        private void ProcessBuffer(ByteDataFragment data) {
            int limit = data.Offset + data.Length;
            int offset = data.Offset;
            byte[] buf = data.Buffer;
            //Debug.WriteLine(String.Format("Telnet len={0}, proc={1}", data.Length, _negotiator.InProcessing));

            while (offset < limit) {
                while (offset < limit && _negotiator.InProcessing) {
                    if (_negotiator.Process(buf[offset++]) == TelnetNegotiator.ProcessResult.REAL_0xFF)
                        _callback.OnReception(_localdata.Set(buf, offset - 1, 1));
                }

                int delim = offset;
                while (delim < limit) {
                    byte b = buf[delim];
                    if (b == 0xFF) {
                        _gotCR = false;
                        _negotiator.StartNegotiate();
                        break;
                    }
                    if (b == 0 && _gotCR) {
                        _gotCR = false;
                        break; //CR NUL
                    }
                    _gotCR = (b == 0xd);
                    delim++;
                }

                if (delim > offset)
                    _callback.OnReception(_localdata.Set(buf, offset, delim - offset)); //delimの手前まで処理
                offset = delim + 1;
            }

        }
    }

    internal class TelnetSocket : IPoderosaSocketInet, ITerminalOutput {
        private readonly IPoderosaSocketInet _socket;
        private readonly TelnetReceiver _receiver;
        private readonly TelnetTerminalConnection _parent;
        private readonly bool _telnetNewLine;

        public TelnetSocket(TelnetTerminalConnection parent, IPoderosaSocketInet socket, TelnetReceiver receiver, bool telnetNewLine) {
            _parent = parent;
            _receiver = receiver;
            _socket = socket;
            _telnetNewLine = telnetNewLine;
        }

        public void RepeatAsyncRead(IByteAsyncInputStream callback) {
            _receiver.SetReceiver(callback);
            _socket.RepeatAsyncRead(_receiver);
        }

        public void Close() {
            _socket.Close();
        }
        public void ForceDisposed() {
            _socket.Close();
        }

        public void Resize(int width, int height) {
            if (!_parent.IsClosed) {
                TelnetOptionWriter wr = new TelnetOptionWriter();
                wr.WriteTerminalSize(width, height);
                wr.WriteTo(_socket);
            }
            // prepare IAC DO NAWS request
            _receiver.SetTerminalSize(width, height);
        }

        public void Transmit(ByteDataFragment data) {
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] buf, int offset, int length) {
            for (int i = 0; i < length; i++) {
                byte t = buf[offset + i];
                if (t == 0xFF || t == 0x0D) { //0xFFまたはCRLF以外のCRを見つけたら
                    WriteEscaping(buf, offset, length);
                    return;
                }
            }
            _socket.Transmit(buf, offset, length); //大抵の場合はこういうデータは入っていないので、高速化のためそのまま送り出す
        }
        private void WriteEscaping(byte[] buf, int offset, int length) {
            byte[] newbuf = new byte[length * 2];
            int newoffset = 0;
            for (int i = 0; i < length; i++) {
                byte t = buf[offset + i];
                if (t == 0xFF) {
                    newbuf[newoffset++] = 0xFF;
                    newbuf[newoffset++] = 0xFF; //２個
                }
                else if (t == 0x0D && !(_telnetNewLine && i + 1 < length && buf[offset + i + 1] == 0x0A)) {
                    // CR    --> CR NUL (Telnet CR)
                    // CR LF --> CR NUL LF
                    //        or CR LF (Telnet New Line)
                    newbuf[newoffset++] = 0x0D;
                    newbuf[newoffset++] = 0x00;
                }
                else
                    newbuf[newoffset++] = t;
            }
            _socket.Transmit(newbuf, 0, newoffset);
        }

        public bool Available {
            get {
                return _socket.Available;
            }
        }

        public void AreYouThere() {
            byte[] data = new byte[2];
            data[0] = (byte)TelnetCode.IAC;
            data[1] = (byte)TelnetCode.AreYouThere;
            _socket.Transmit(data, 0, data.Length);
        }
        public void SendBreak() {
            byte[] data = new byte[2];
            data[0] = (byte)TelnetCode.IAC;
            data[1] = (byte)TelnetCode.Break;
            _socket.Transmit(data, 0, data.Length);
        }
        public void SendKeepAliveData() {
            byte[] data = new byte[2];
            data[0] = (byte)TelnetCode.IAC;
            data[1] = (byte)TelnetCode.NOP;
            // Note:
            //  Disconnecting or Closing socket may happen before Send() is called.
            //  In such case, SocketException or ObjectDisposedException will be thrown in Send().
            //  We just ignore the exceptions.
            try {
                _socket.Transmit(data, 0, data.Length);
            }
            catch (SocketException) {
            }
            catch (ObjectDisposedException) {
            }
        }

        public string Remote {
            get {
                return _socket.Remote;
            }
        }

        public IPAddress RemoteAddress {
            get {
                return _socket.RemoteAddress;
            }
        }

        public int? RemotePortNumber {
            get {
                return _socket.RemotePortNumber;
            }
        }
    }

    internal class TelnetTerminalConnection : TCPTerminalConnection {
        private readonly TelnetReceiver _telnetReceiver;
        private readonly TelnetSocket _telnetSocket;
        private readonly IPoderosaSocket _rawSocket;

        public TelnetTerminalConnection(ITCPParameter p, TelnetNegotiator neg, Socket socket, string remote)
            : base(p) {
            _telnetReceiver = new TelnetReceiver(this, neg);
            ITelnetParameter telnetParams = (ITelnetParameter)p.GetAdapter(typeof(ITelnetParameter));
            bool telnetNewLine = (telnetParams != null) ? telnetParams.TelnetNewLine : true/*default*/;

            PlainPoderosaSocket plainSocket = new PlainPoderosaSocket(this, socket, remote);
            _rawSocket = plainSocket;
            _telnetSocket = new TelnetSocket(this, plainSocket, _telnetReceiver, telnetNewLine);
        }

        //Telnetのエスケープ機能つき
        public TelnetSocket TelnetSocket {
            get {
                return _telnetSocket;
            }
        }
        //TelnetSocketが内包する生ソケット
        public IPoderosaSocket RawSocket {
            get {
                return _rawSocket;
            }
        }

        public override void Close() {
            if (_closed)
                return; //２度以上クローズしても副作用なし 
            _telnetSocket.Close();
            base.Close();
        }

        public override ITerminalOutput TerminalOutput {
            get {
                return _telnetSocket;
            }
        }

        public override IPoderosaSocket Socket {
            get {
                return _telnetSocket;
            }
        }
    }

    internal class RawTerminalConnection : ITerminalConnection, ITerminalOutput {
        private readonly IPoderosaSocket _socket;
        private readonly ITerminalParameter _terminalParameter;

        public RawTerminalConnection(IPoderosaSocket socket, ITerminalParameter tp) {
            _socket = socket;
            _terminalParameter = tp;
        }


        public ITerminalParameter Destination {
            get {
                return _terminalParameter;
            }
        }

        public ITerminalOutput TerminalOutput {
            get {
                return this;
            }
        }

        public IPoderosaSocket Socket {
            get {
                return _socket;
            }
        }

        public bool IsClosed {
            get {
                return false;
            }
        }

        public void Close() {
            _socket.Close();
        }

        public IAdaptable GetAdapter(Type adapter) {
            return ProtocolsPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        //ITerminalOutputはシカト
        public void SendBreak() {
        }

        public void SendKeepAliveData() {
        }

        public void AreYouThere() {
        }

        public void Resize(int width, int height) {
        }
    }


}
