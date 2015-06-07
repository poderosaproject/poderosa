/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SSHSocket.cs,v 1.6 2011/11/19 04:58:43 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Poderosa.Util;

using Granados;
using Granados.SSH2;

namespace Poderosa.Protocols {
    //SSHの入出力系
    internal abstract class SSHConnectionEventReceiverBase : ISSHConnectionEventReceiver {
        protected SSHTerminalConnection _parent;
        protected SSHConnection _connection;
        protected IByteAsyncInputStream _callback;
        private bool _normalTerminationCalled;

        public SSHConnectionEventReceiverBase(SSHTerminalConnection parent) {
            _parent = parent;
        }
        //SSHConnection確立時に呼ぶ
        public void SetSSHConnection(SSHConnection connection) {
            _connection = connection;
            _connection.AutoDisconnect = true; //最後のチャネル切断でコネクションも切断
        }
        public SSHConnection Connection {
            get {
                return _connection;
            }
        }
        public virtual void CleanupErrorStatus() {
            if (_connection != null && _connection.IsOpen)
                _connection.Close();
        }

        public abstract void Close();

        public virtual void OnAuthenticationPrompt(string[] prompts) {
        }

        public virtual void OnConnectionClosed() {
            OnNormalTerminationCore();
            _connection.Close();
        }

        public virtual void OnError(Exception error) {
            OnAbnormalTerminationCore(error.Message);
        }

        //TODO 滅多にないことではあるがこれを拾う先をEXTPで
        public virtual void OnDebugMessage(bool always_display, byte[] data) {
            Debug.WriteLine(String.Format("SSH debug {0}[{1}]", data.Length, data[0]));
        }

        public virtual void OnIgnoreMessage(byte[] data) {
            Debug.WriteLine(String.Format("SSH ignore {0}[{1}]", data.Length, data[0]));
        }

        public virtual void OnUnknownMessage(byte type, byte[] data) {
            Debug.WriteLine(String.Format("Unexpected SSH packet type {0}", type));
        }

        //以下は呼ばれることはない。空実装
        public virtual PortForwardingCheckResult CheckPortForwardingRequest(string remote_host, int remote_port, string originator_ip, int originator_port) {
            return new Granados.PortForwardingCheckResult();
        }
        public virtual void EstablishPortforwarding(ISSHChannelEventReceiver receiver, SSHChannel channel) {
        }

        protected void OnNormalTerminationCore() {
            if (_normalTerminationCalled)
                return;

            /* NOTE
             *  正常終了の場合でも、SSHパケットレベルではChannelEOF, ChannelClose, ConnectionCloseがあり、場合によっては複数個が組み合わされることもある。
             *  組み合わせの詳細はサーバの実装依存でもあるので、ここでは１回だけ必ず呼ぶということにする。
             */
            _normalTerminationCalled = true;
            EnsureCallbackHandler();
            _parent.CloseBySocket();

            try {
                if (_callback != null)
                    _callback.OnNormalTermination();
            }
            catch (Exception ex) {
                CloseError(ex);
            }
        }
        protected void OnAbnormalTerminationCore(string msg) {
            EnsureCallbackHandler();
            _parent.CloseBySocket();

            try {
                if (_callback != null)
                    _callback.OnAbnormalTermination(msg);
            }
            catch (Exception ex) {
                CloseError(ex);
            }
        }
        protected void EnsureCallbackHandler() {
            int n = 0;
            //TODO きれいでないが、接続～StartRepeatまでの間にエラーがサーバから通知されたときに。
            while (_callback == null && n++ < 100) //わずかな時間差でハンドラがセットされないこともある
                Thread.Sleep(100);
        }
        //Termination処理の失敗時の処理
        private void CloseError(Exception ex) {
            try {
                RuntimeUtil.ReportException(ex);
                CleanupErrorStatus();
            }
            catch (Exception ex2) {
                RuntimeUtil.ReportException(ex2);
            }
        }
    }

    internal class SSHSocket : SSHConnectionEventReceiverBase, IPoderosaSocket, ITerminalOutput, ISSHChannelEventReceiver {
        private SSHChannel _channel;
        private ByteDataFragment _data;
        private bool _waitingSendBreakReply;
        //非同期に受信する。
        private MemoryStream _buffer; //RepeatAsyncReadが呼ばれる前に受信してしまったデータを一時保管するバッファ

        public SSHSocket(SSHTerminalConnection parent)
            : base(parent) {
            _data = new ByteDataFragment();
        }

        public SSHChannel Channel {
            get {
                return _channel;
            }
        }

        public void RepeatAsyncRead(IByteAsyncInputStream cb) {
            _callback = cb;
            //バッファに何がしか溜まっている場合：
            //NOTE これは、IPoderosaSocket#StartAsyncReadを呼ぶシーケンスをなくし、接続を開始する瞬間(IProtocolServiceのメソッド系)から
            //データ本体を受信する口を提供させるようにすれば除去できる。しかしプログラマの側としては、接続成功を確認してからデータ受信口を用意したいので、
            //（Poderosaでいえば、ログインボタンのOKを押す時点でAbstractTerminalまで準備せねばならないということ）、それよりはデータを保留しているほうがいいだろう
            if (_buffer != null) {
                lock (this) {
                    _buffer.Close();
                    byte[] t = _buffer.ToArray();
                    _data.Set(t, 0, t.Length);
                    if (t.Length > 0)
                        _callback.OnReception(_data);
                    _buffer = null;
                }
            }
        }

        public override void CleanupErrorStatus() {
            if (_channel != null)
                _channel.Close();
            base.CleanupErrorStatus();
        }

        public void OpenShell() {
            _channel = _connection.OpenShell(this);
        }
        public void OpenSubsystem(string subsystem) {
            SSH2Connection ssh2 = _connection as SSH2Connection;
            if (ssh2 == null)
                throw new SSHException("OpenSubsystem() can be applied to only SSH2 connection");
            _channel = ssh2.OpenSubsystem(this, subsystem);
        }

        public override void Close() {
            if (_channel != null)
                _channel.Close();
        }
        public void ForceDisposed() {
            _connection.Close(); //マルチチャネルだとアウトかも
        }

        public void Transmit(ByteDataFragment data) {
            _channel.Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] buf, int offset, int length) {
            _channel.Transmit(buf, offset, length);
        }

        //以下、ITerminalOutput
        public void Resize(int width, int height) {
            if (!_parent.IsClosed)
                _channel.ResizeTerminal(width, height, 0, 0);
        }
        public void SendBreak() {
            if (_parent.SSHLoginParameter.Method == SSHProtocol.SSH1)
                throw new NotSupportedException();
            else {
                _waitingSendBreakReply = true;
                ((Granados.SSH2.SSH2Channel)_channel).SendBreak(500);
            }
        }
        public void SendKeepAliveData() {
            if (!_parent.IsClosed) {
                // Note:
                //  Disconnecting or Closing socket may happen before Send() is called.
                //  In such case, SocketException or ObjectDisposedException will be thrown in Send().
                //  We just ignore the exceptions.
                try {
                    _connection.SendIgnorableData("keep alive");
                }
                catch (SocketException) {
                }
                catch (ObjectDisposedException) {
                }
            }
        }
        public void AreYouThere() {
            throw new NotSupportedException();
        }

        public void OnChannelClosed() {
            OnNormalTerminationCore();
        }
        public void OnChannelEOF() {
            OnNormalTerminationCore();
        }
        public void OnData(byte[] data, int offset, int length) {
            if (_callback == null) { //RepeatAsyncReadが呼ばれる前のデータを集めておく
                lock (this) {
                    if (_buffer == null)
                        _buffer = new MemoryStream(0x100);
                    _buffer.Write(data, offset, length);
                }
            }
            else {
                _data.Set(data, offset, length);
                _callback.OnReception(_data);
            }
        }
        public void OnExtendedData(int type, byte[] data) {
        }
        public void OnMiscPacket(byte type, byte[] data, int offset, int length) {
            if (_waitingSendBreakReply) {
                _waitingSendBreakReply = false;
                if (type == (byte)Granados.SSH2.PacketType.SSH_MSG_CHANNEL_FAILURE)
                    PEnv.ActiveForm.Warning(PEnv.Strings.GetString("Message.SSHTerminalconnection.BreakError"));
            }
        }

        public void OnChannelReady() { //!!Transmitを許可する通知が必要？
        }

        public void OnChannelError(Exception ex) {
            // FIXME: In this case, something message should be displayed for the user.
            //        OnAbnormalTerminationCore() doesn't show the message.
            OnAbnormalTerminationCore(ex.Message);
        }


        public SSHConnectionInfo ConnectionInfo {
            get {
                return _connection.ConnectionInfo;
            }
        }

        public bool Available {
            get {
                return _connection.Available;
            }
        }
    }

    //Keyboard Interactive認証中
    internal class KeyboardInteractiveAuthHanlder : SSHConnectionEventReceiverBase, IPoderosaSocket {
        private MemoryStream _passwordBuffer;
        private string[] _prompts;

        public KeyboardInteractiveAuthHanlder(SSHTerminalConnection parent)
            : base(parent) {
        }

        public override void OnAuthenticationPrompt(string[] prompts) {
            //ここに来るケースは２つ。

            if (_callback == null) //1. 最初の認証中
                _prompts = prompts;
            else { //2. パスワード入力まちがいなどでもう一回という場合
                EnsureCallbackHandler();
                ShowPrompt(prompts);
            }
        }

        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            _callback = receiver;
            if (_prompts != null)
                ShowPrompt(_prompts);
        }
        private void ShowPrompt(string[] prompts) {
            Debug.Assert(_callback != null);
            bool hasPassword = _parent.SSHLoginParameter.PasswordOrPassphrase != null
                            && !_parent.SSHLoginParameter.LetUserInputPassword;
            bool sendPassword = false;
            for (int i = 0; i < prompts.Length; i++) {
                if (hasPassword && prompts[i].Contains("assword")) {
                    sendPassword = true;
                    break;
                }
                if (i != 0)
                    prompts[i] += "\r\n";
                byte[] buf = Encoding.Default.GetBytes(prompts[i]);
                _callback.OnReception(new ByteDataFragment(buf, 0, buf.Length));
            }

            if (sendPassword) {
                SendPassword(_parent.SSHLoginParameter.PasswordOrPassphrase);
            }
        }

        public bool Available {
            get {
                return _connection.Available;
            }
        }

        public void Transmit(ByteDataFragment data) {
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] data, int offset, int length) {
            if (_passwordBuffer == null)
                _passwordBuffer = new MemoryStream();

            for (int i = offset; i < offset + length; i++) {
                byte b = data[i];
                if (b == 13 || b == 10) { //CR/LF
                    SendPassword(null);
                }
                else
                    _passwordBuffer.WriteByte(b);
            }
        }
        private void SendPassword(string password) {
            string[] response;
            if (password != null) {
                response = new string[] { password };
            }
            else {
                byte[] pwd = _passwordBuffer.ToArray();
                if (pwd.Length > 0) {
                    _passwordBuffer.Close();
                    _passwordBuffer.Dispose();
                    _passwordBuffer = null;
                    response = new string[] { Encoding.ASCII.GetString(pwd) };
                }
                else {
                    response = null;
                }
            }

            if (response != null) {
                _callback.OnReception(new ByteDataFragment(new byte[] { 13, 10 }, 0, 2)); //表示上CR+LFで改行しないと格好悪い
                if (((Granados.SSH2.SSH2Connection)_connection).DoKeyboardInteractiveAuth(response) == AuthenticationResult.Success) {
                    _parent.SSHLoginParameter.PasswordOrPassphrase = response[0];
                    SuccessfullyExit();
                    return;
                }
            }
            _connection.Disconnect("");
            throw new IOException(PEnv.Strings.GetString("Message.SSHConnector.Cancelled"));
        }
        //シェルを開き、イベントレシーバを書き換える
        private void SuccessfullyExit() {
            SSHSocket sshsocket = new SSHSocket(_parent);
            sshsocket.SetSSHConnection(_connection);
            sshsocket.RepeatAsyncRead(_callback); //_callbackから先の処理は同じ
            _connection.EventReceiver = sshsocket;
            _parent.ReplaceSSHSocket(sshsocket);
            sshsocket.OpenShell();
        }

        public override void Close() {
            _connection.Close();
        }
        public void ForceDisposed() {
            _connection.Close();
        }

    }
}
