/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: Socket.cs,v 1.5 2012/02/21 14:16:52 kzmi Exp $
*/
using System;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Collections;
using System.Diagnostics;

using Granados.Util;
using System.Collections.Generic;
using System.Globalization;

namespace Granados.IO {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum SocketStatus {
        Ready,
        Negotiating,       //preparing for connection
        RequestingClose,   //the client is requesting termination
        Closed,            //closed
        Unknown
    }

    //interface to receive data through AbstractGranadosSocket asynchronously
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IDataHandler {
        void OnData(DataFragment data);
        void OnClosed();
        void OnError(Exception error);
    }

    /// <summary>
    /// IDataHandler implementation that do nothing
    /// </summary>
    internal class NullDataHandler : IDataHandler {

        public void OnData(DataFragment data) {
        }

        public void OnClosed() {
        }

        public void OnError(Exception error) {
        }
    }

    //System.IO.SocketとIChannelEventReceiverを抽象化する
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public abstract class AbstractGranadosSocket {
        protected IDataHandler _handler;
        protected SocketStatus _socketStatus;

        protected AbstractGranadosSocket(IDataHandler h) {
            _handler = (h != null) ? h : new NullDataHandler();
            _single = new byte[1];
            _socketStatus = SocketStatus.Unknown;
        }

        public SocketStatus SocketStatus {
            get {
                return _socketStatus;
            }
        }
        public IDataHandler DataHandler {
            get {
                return _handler;
            }
        }

        public void SetHandler(IDataHandler h) {
            _handler = h;
        }

        internal abstract void Write(byte[] data, int offset, int length);

        private byte[] _single;
        internal void WriteByte(byte data) {
            _single[0] = data;
            Write(_single, 0, 1);
        }

        internal abstract void Close();
        internal abstract bool DataAvailable {
            get;
        }
    }

    // base class for processing data and passing another IDataHandler
    internal abstract class FilterDataHandler : IDataHandler {
        protected IDataHandler _inner_handler;

        public FilterDataHandler(IDataHandler inner_handler) {
            _inner_handler = inner_handler;
        }
        public IDataHandler InnerHandler {
            get {
                return _inner_handler;
            }
            set {
                _inner_handler = value;
            }
        }

        public abstract void OnData(DataFragment data);

        public virtual void OnClosed() {
            _inner_handler.OnClosed();
        }
        public virtual void OnError(Exception error) {
            _inner_handler.OnError(error);
        }
    }

    //Handler for receiving the response synchronously
    internal abstract class SynchronizedDataHandler : IDataHandler {

        private AbstractGranadosSocket _socket;
        private ManualResetEvent _event;
        private Queue _results;

        internal interface IQueueEventListener {
            void Dequeued();
        }

        public SynchronizedDataHandler(AbstractGranadosSocket socket) {
            _socket = socket;
            _event = new ManualResetEvent(false);
            _results = new Queue();
        }

        public void Close() {
            lock (_socket) {
                _event.Close();
                ClearQueue();
            }
        }

        public void OnData(DataFragment data) {
            lock (_socket) {
                OnDataInLock(data);
            }
        }
        public void OnClosed() {
            lock (_socket) {
                OnClosedInLock();
            }
        }
        public void OnError(Exception error) {
            lock (_socket) {
                OnErrorInLock(error);
            }
        }

        protected abstract void OnDataInLock(DataFragment data);
        protected virtual void OnClosedInLock() {
            SetFailureResult(new SSHException("the connection is closed with unexpected condition."));
        }
        protected virtual void OnErrorInLock(Exception error) {
            SetFailureResult(error);
        }

        //Set the response
        protected void SetSuccessfulResult(DataFragment data) {
            _results.Enqueue(data.Isolate());
            _event.Set();
        }
        protected void SetFailureResult(Exception error) {
            _results.Enqueue(error);
            _event.Set();
        }

        //Send request and wait response
        public DataFragment SendAndWaitResponse(DataFragment data) {
            //this lock is important
            lock (_socket) {
                Debug.Assert(_results.Count == 0);
                _event.Reset();
                if (data.Length > 0)
                    _socket.Write(data.Data, data.Offset, data.Length);
            }

            _event.WaitOne();
            Debug.Assert(_results.Count > 0);
            return Dequeue();
        }

        //asynchronously data exchange
        public DataFragment WaitResponse() {
            lock (_socket) {
                if (_results.Count > 0)
                    return Dequeue(); //we have data already 
                else
                    _event.Reset();
            }

            _event.WaitOne();
            return Dequeue();
        }

        //Pop the data from the queue
        private DataFragment Dequeue() {
            lock (_socket) {
                object t = _results.Dequeue();
                Debug.Assert(t != null);

                IQueueEventListener el = t as IQueueEventListener;
                if (el != null) {
                    el.Dequeued();
                }

                DataFragment d = t as DataFragment;
                if (d != null)
                    return d;
                else {
                    Exception e = t as Exception;
                    Debug.Assert(e != null);
                    ClearQueue();
                    throw e;
                }
            }
        }

        private void ClearQueue() {
            lock (_socket) {
                while (_results.Count > 0) {
                    IQueueEventListener el = _results.Dequeue() as IQueueEventListener;
                    if (el != null) {
                        el.Dequeued();
                    }
                }
            }
        }
    }

    /// <summary>
    /// A class reads SSH protocol version
    /// </summary>
    internal class SSHProtocolVersionReceiver {

        private string _serverVersion = null;
        private List<string> _lines = new List<string>();

        /// <summary>
        /// Constructor
        /// </summary>
        public SSHProtocolVersionReceiver() {
        }

        /// <summary>
        /// All lines recevied from the server including the version string.
        /// </summary>
        /// <remarks>Each string value doesn't contain the new-line characters.</remarks>
        public string[] Lines {
            get {
                return _lines.ToArray();
            }
        }

        /// <summary>
        /// Version string recevied from the server.
        /// </summary>
        /// <remarks>The string value doesn't contain the new-line characters.</remarks>
        public string ServerVersion {
            get {
                return _serverVersion;
            }
        }

        /// <summary>
        /// Receive version string.
        /// </summary>
        /// <param name="sock">socket object</param>
        /// <param name="timeout">timeout in msec</param>
        /// <returns>true if version string was received.</returns>
        public bool Receive(PlainSocket sock, long timeout) {
            DateTime tm = DateTime.UtcNow.AddMilliseconds(timeout);
            using (MemoryStream mem = new MemoryStream()) {
                while (DateTime.UtcNow < tm && sock.SocketStatus == SocketStatus.Ready) {
                    byte? b = sock.ReadByte();
                    if (b == null) {
                        Thread.Sleep(10);
                        continue;
                    }
                    mem.WriteByte(b.Value);
                    if (b == 0xa) { // LF
                        byte[] bytestr = mem.ToArray();
                        mem.SetLength(0);
                        string line = Encoding.UTF8.GetString(bytestr).TrimEnd('\xd', '\xa');
                        _lines.Add(line);
                        if (line.StartsWith("SSH-")) {
                            _serverVersion = line;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Verify server version
        /// </summary>
        /// <param name="protocol">expected protocol version</param>
        /// <exception cref="SSHException">server version doesn't match</exception>
        public void Verify(SSHProtocol protocol) {
            if (_serverVersion == null) {
                throw new SSHException(Strings.GetString("NotSSHServer"));
            }

            string[] sv = _serverVersion.Split('-');
            if (sv.Length >= 3 && sv[0] == "SSH") {
                string protocolVersion = sv[1];
                string[] pv = protocolVersion.Split('.');
                if (pv.Length >= 2) {
                    if (protocol == SSHProtocol.SSH1) {
                        if (pv[0] == "1") {
                            return; // OK
                        }
                    }
                    else if (protocol == SSHProtocol.SSH2) {
                        if (pv[0] == "2" || (pv[0] == "1" && pv[1] == "99")) {
                            return; // OK
                        }
                    }
                    throw new SSHException(
                        String.Format(Strings.GetString("IncompatibleProtocolVersion"), _serverVersion, protocol.ToString()));
                }
            }

            throw new SSHException(
                String.Format(Strings.GetString("InvalidServerVersionFormat"), _serverVersion));
        }
    }

    //directly notification to synchronized
    internal class SynchronizedPacketReceiver : SynchronizedDataHandler {

        private SSHConnection _connection;

        public SynchronizedPacketReceiver(SSHConnection c)
            : base(c.UnderlyingStream) {
            _connection = c;
        }

        protected override void OnDataInLock(DataFragment data) {
            this.SetSuccessfulResult(data);
        }
    }

    // GranadosSocket on an underlying .NET socket
    internal class PlainSocket : AbstractGranadosSocket {
        private Socket _socket;
        private DataFragment _data;

        private AsyncCallback _callback;

        private volatile bool onClosedFired = false;

        internal PlainSocket(Socket s, IDataHandler h)
            : base(h) {
            _socket = s;
            Debug.Assert(_socket.Connected);
            _socketStatus = SocketStatus.Ready;

            _data = new DataFragment(0x1000);
            _callback = new AsyncCallback(RepeatCallback);
        }

        internal byte? ReadByte() {
            byte[] buf = new byte[1];
            if (_socket.Available > 0) {
                int n = _socket.Receive(buf);
                if (n > 0) {
                    return buf[0];
                }
            }
            return null;
        }

        internal override void Write(byte[] data, int offset, int length) {
            _socket.Send(data, offset, length, SocketFlags.None);
        }

        internal override void Close() {
            if (_socketStatus != SocketStatus.Closed) {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
                _socketStatus = SocketStatus.Closed;
                FireOnClosed();
            }
        }

        internal void RepeatAsyncRead() {
            _socket.BeginReceive(_data.Data, 0, _data.Capacity, SocketFlags.None, _callback, null);
        }

        internal override bool DataAvailable {
            get {
                return _socket.Available > 0;
            }
        }

        private void RepeatCallback(IAsyncResult result) {
            try {
                int n = _socket.EndReceive(result);
                if (n > 0) {
                    _data.SetLength(0, n);
                    _handler.OnData(_data);
                    if (_socketStatus != SocketStatus.Closed)
                        RepeatAsyncRead();
                }
                else if (n < 0) {
                    //in case of Win9x, EndReceive() returns 0 every 288 seconds even if no data is available
                    RepeatAsyncRead();
                }
                else {
                    FireOnClosed();
                }
            }
            catch (ObjectDisposedException) {
                // _socket has been closed
                FireOnClosed();
            }
            catch (Exception ex) {
                if ((ex is SocketException) && ((SocketException)ex).ErrorCode == 995) {
                    //in case of .NET1.1 on Win9x, EndReceive() changes the behavior. it throws SocketException with an error code 995. 
                    RepeatAsyncRead();
                }
                else if (_socketStatus != SocketStatus.Closed)
                    _handler.OnError(ex);
            }
        }

        private void FireOnClosed() {
            lock (_handler) {
                if (onClosedFired) {
                    return;
                }
                onClosedFired = true;
            }
            // PlainSocket.Close() may be called from another thread again in _handler.OnClosed().
            // For avoiding deadlock, _handler.OnClosed() have to be called out of the lock() block.
            _handler.OnClosed();
        }
    }

    // GranadosSocket on an underlying another SSH channel
    internal class ChannelSocket : AbstractGranadosSocket, ISSHChannelEventReceiver {
        private SSHChannel _channel;
        private DataFragment _fragment;

        internal ChannelSocket(IDataHandler h)
            : base(h) {
        }
        internal SSHChannel SSHChennal {
            get {
                return _channel;
            }
            set {
                _channel = value;
                _socketStatus = SocketStatus.Negotiating;
            }
        }

        internal override void Write(byte[] data, int offset, int length) {
            if (_socketStatus != SocketStatus.Ready)
                throw new SSHException("channel not ready");
            _channel.Transmit(data, offset, length);
        }
        internal override bool DataAvailable {
            get {
                //Note: this may be not correct
                return _channel.Connection.Available;
            }
        }

        internal override void Close() {
            if (_socketStatus != SocketStatus.Ready)
                throw new SSHException("channel not ready");

            _channel.Close();
            if (_channel.Connection.ChannelCollection.Count <= 1) //close last channel
                _channel.Connection.Close();
        }

        public void OnData(byte[] data, int offset, int length) {
            if (_fragment == null)
                _fragment = new DataFragment(data, offset, length);
            else
                _fragment.Init(data, offset, length);

            _handler.OnData(_fragment);
        }

        public void OnChannelEOF() {
            _handler.OnClosed();
        }

        public void OnChannelError(Exception error) {
            _handler.OnError(error);
        }

        public void OnChannelClosed() {
            _handler.OnClosed();
        }

        public void OnChannelReady() {
            _socketStatus = SocketStatus.Ready;
        }

        public void OnExtendedData(int type, byte[] data) {
            //!!handle data
        }
        public void OnMiscPacket(byte type, byte[] data, int offset, int length) {
            //!!handle data
        }
    }
}
