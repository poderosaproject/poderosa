/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SSHSocket.cs,v 1.6 2011/11/19 04:58:43 kzmi Exp $
 */
using System;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;

using Granados;
using Granados.SSH2;
using Granados.IO;
using Granados.KeyboardInteractive;

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
            if (_connection != null && _connection.IsOpen) {
                _connection.Close();
            }
        }

        public abstract void Close();

        public virtual void OnConnectionClosed() {
            OnNormalTerminationCore();
            if (_connection != null && _connection.IsOpen) {
                _connection.Close();
            }
        }

        public virtual void OnError(Exception error) {
            OnAbnormalTerminationCore(error.Message);
        }

        //TODO 滅多にないことではあるがこれを拾う先をEXTPで
        public virtual void OnDebugMessage(bool alwaysDisplay, string message) {
            Debug.WriteLine(String.Format("SSH debug {0}", message));
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
            _parent.CloseBySocket();

            try {
                if (_callback != null)
                    _callback.OnAbnormalTermination(msg);
            }
            catch (Exception ex) {
                CloseError(ex);
            }
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

    internal class SSHSocket
        : SSHConnectionEventReceiverBase,
          IPoderosaSocket, ITerminalOutput, ISSHChannelEventReceiver, IKeyboardInteractiveAuthenticationHandler {

        private SSHChannel _channel;
        private ByteDataFragment _data;
        private bool _waitingSendBreakReply;
        //非同期に受信する。
        private MemoryStream _buffer; //RepeatAsyncReadが呼ばれる前に受信してしまったデータを一時保管するバッファ

        private KeyboardInteractiveAuthHanlder _keyboardInteractiveAuthHanlder;

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
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] buf, int offset, int length) {
            if (_keyboardInteractiveAuthHanlder != null) {
                // intercept input
                _keyboardInteractiveAuthHanlder.OnData(buf, offset, length);
                return;
            }
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
        public void OnData(DataFragment data) {
            if (_callback == null) { //RepeatAsyncReadが呼ばれる前のデータを集めておく
                lock (this) {
                    if (_buffer == null)
                        _buffer = new MemoryStream(0x100);
                    _buffer.Write(data.Data, data.Offset, data.Length);
                }
            }
            else {
                _data.Set(data.Data, data.Offset, data.Length);
                _callback.OnReception(_data);
            }
        }
        public void OnExtendedData(uint type, DataFragment data) {
        }
        public void OnMiscPacket(byte type, DataFragment data) {
            if (_waitingSendBreakReply) {
                _waitingSendBreakReply = false;
                if (type == (byte)Granados.SSH2.SSH2PacketType.SSH_MSG_CHANNEL_FAILURE)
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

        public bool Available {
            get {
                return _connection.Available;
            }
        }

        #region IKeyboardInteractiveAuthenticationHandler

        public string[] KeyboardInteractiveAuthenticationPrompt(string[] prompts, bool[] echoes) {
            if (_keyboardInteractiveAuthHanlder != null) {
                return _keyboardInteractiveAuthHanlder.KeyboardInteractiveAuthenticationPrompt(prompts, echoes);
            } else {
                return prompts.Select(s => "").ToArray();
            }
        }

        public void OnKeyboardInteractiveAuthenticationStarted() {
            _keyboardInteractiveAuthHanlder =
                new KeyboardInteractiveAuthHanlder(
                    (data) => {
                        this.OnData(new DataFragment(data, 0, data.Length));
                    });
        }

        public void OnKeyboardInteractiveAuthenticationCompleted(bool success, Exception error) {
            _keyboardInteractiveAuthHanlder = null;
            if (success) {
                OpenShell();
            }
        }

        #endregion
    }

    /// <summary>
    /// Keyboard-interactive authentication support for <see cref="SSHSocket"/>.
    /// </summary>
    internal class KeyboardInteractiveAuthHanlder {
        private bool _echoing = true;
        private readonly MemoryStream _inputBuffer = new MemoryStream();
        private readonly object _inputSync = new object();
        private readonly Action<byte[]> _output;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="output">a method to output data to the terminal</param>
        public KeyboardInteractiveAuthHanlder(Action<byte[]> output) {
            _output = output;
        }

        /// <summary>
        /// Show prompt lines and input texts.
        /// </summary>
        /// <param name="prompts"></param>
        /// <param name="echoes"></param>
        /// <returns></returns>
        public string[] KeyboardInteractiveAuthenticationPrompt(string[] prompts, bool[] echoes) {
            Encoding encoding = (Encoding)Encoding.UTF8.Clone();    // TODO:
            encoding.EncoderFallback = EncoderFallback.ReplacementFallback;
            string[] inputs = new string[prompts.Length];
            for (int i = 0; i < prompts.Length; ++i) {
                bool echo = (i < echoes.Length) ? echoes[i] : true;
                byte[] promptBytes = encoding.GetBytes(prompts[i]);
                // echo prompt text
                byte[] lineBytes;
                lock (_inputSync) {
                    _output(promptBytes);
                    _echoing = echo;
                    _inputBuffer.SetLength(0);
                    Monitor.Wait(_inputSync);
                    _echoing = true;
                    lineBytes = _inputBuffer.ToArray();
                }
                string line = encoding.GetString(lineBytes);
                inputs[i] = line;
            }
            return inputs;
        }

        /// <summary>
        /// Process user input.
        /// </summary>
        public void OnData(byte[] data, int offset, int length) {
            int endIndex = offset + length;
            int currentIndex = offset;
            while (currentIndex < endIndex) {
                lock (_inputSync) {
                    int startIndex = currentIndex;
                    bool newLine = false;
                    for (; currentIndex < endIndex; ++currentIndex) {
                        byte b = data[currentIndex];
                        if (b == 13 || b == 10) { //CR/LF
                            newLine = true;
                            break;
                        }
                        _inputBuffer.WriteByte(b);
                    }
                    // flush
                    if (_echoing && currentIndex > startIndex) {
                        _output(GetBytes(data, startIndex, currentIndex - startIndex));
                    }
                    if (newLine) {
                        currentIndex++;
                        _output(new byte[] { 13, 10 });   // CRLF
                        // notify
                        Monitor.PulseAll(_inputSync);
                    }
                }
            }
        }

        private byte[] GetBytes(byte[] data, int offset, int length) {
            byte[] buf = new byte[length];
            if (length > 0) {
                Buffer.BlockCopy(data, offset, buf, 0, length);
            }
            return buf;
        }
    }

}
