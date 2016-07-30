/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: ConnectionRoot.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/

using System;
using System.Net.Sockets;
using Granados.SSH1;
using Granados.SSH2;
using Granados.IO;
using Granados.Util;
using Granados.SSH;
using Granados.PortForwarding;

namespace Granados {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public abstract class SSHConnection {

        protected ChannelCollection _channel_collection;      //channels
        protected IGranadosSocket _stream;                    //underlying socket
        protected ISSHConnectionEventReceiver _eventReceiver; //outgoing interface for this connection
        protected byte[] _sessionID;                          //session ID
        protected bool _autoDisconnect;                       //if this is true, this connection will be closed with the last channel
        protected AuthenticationResult _authenticationResult; //authentication result

        // for scp
        protected string _execCmd;        // exec command string
        protected bool _execCmdWaitFlag;  // wait response flag for sending exec command to server

        protected SSHConnection(SSHConnectionParameter param, IGranadosSocket strm, ISSHConnectionEventReceiver receiver) {
            _stream = strm;
            _eventReceiver = receiver;
            _channel_collection = new ChannelCollection();
            _autoDisconnect = true;
            _execCmd = null;
            _execCmdWaitFlag = true;
        }


        ///abstract properties

        // stream->packet converter
        internal abstract IDataHandler Packetizer {
            get;
        }

        ///  paramters
        public ISSHConnectionEventReceiver EventReceiver {
            get {
                return _eventReceiver;
            }
            set {
                _eventReceiver = value;
            }
        }
        public SocketStatus SocketStatus {
            get {
                return _stream.SocketStatus;
            }
        }
        public bool IsOpen {
            get {
                return _stream.SocketStatus == SocketStatus.Ready && _authenticationResult == AuthenticationResult.Success;
            }
        }
        internal IGranadosSocket UnderlyingStream {
            get {
                return _stream;
            }
        }
        internal ChannelCollection ChannelCollection {
            get {
                return _channel_collection;
            }
        }

        //configurable properties
        public bool AutoDisconnect {
            get {
                return _autoDisconnect;
            }
            set {
                _autoDisconnect = value;
            }
        }


        //returns true if any data from server is available
        public bool Available {
            get {
                if (_stream.SocketStatus != SocketStatus.Ready)
                    return false;
                else
                    return _stream.DataAvailable;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal abstract AuthenticationResult Connect();

        /**
        * terminates this connection
        */
        public abstract void Disconnect(string msg);

        /**
        * opens a pseudo terminal
        */
        public abstract THandler OpenShell<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator)
                where THandler : ISSHChannelEventHandler;

        /** 
         * forwards the remote end to another host
         */
        public abstract THandler ForwardPort<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string remoteHost, uint remotePort, string originatorIp, uint originatorPort)
                where THandler : ISSHChannelEventHandler;

        /**
         * listens a connection on the remote end
         */
        public abstract bool ListenForwardedPort(IRemotePortForwardingHandler requestHandler, string addressToBind, uint portNumberToBind);

        /**
         * cancels binded port
         */
        public abstract bool CancelForwardedPort(string addressToBind, uint portNumberToBind);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="handlerCreator"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public abstract THandler ExecCommand<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string command)
                where THandler : ISSHChannelEventHandler;

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Supported on SSH2 only.
        /// </remarks>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="handlerCreator"></param>
        /// <param name="subsystemName"></param>
        /// <returns></returns>
        public abstract THandler OpenSubsystem<THandler>(SSHChannelEventHandlerCreator<THandler> handlerCreator, string subsystemName)
                where THandler : ISSHChannelEventHandler;

        /**
        * closes socket directly.
        */
        public void Close() {
            if (_stream.SocketStatus == SocketStatus.Closed || _stream.SocketStatus == SocketStatus.RequestingClose)
                return;
            _stream.Close();
        }


        /**
         * sends ignorable data: the server may record the message into the log
         */
        public abstract void SendIgnorableData(string msg);

        /**
         * open a new SSH connection via the .NET socket
         */
        public static SSHConnection Connect(
                    SSHConnectionParameter param,
                    ISSHConnectionEventReceiver receiver,
                    ISSHProtocolEventListener protocolEventListener,
                    Socket underlying_socket,
                    out AuthenticationResult authResult) {

            if (param.UserName == null)
                throw new InvalidOperationException("UserName property is not set");
            if (param.AuthenticationType != AuthenticationType.KeyboardInteractive && param.Password == null)
                throw new InvalidOperationException("Password property is not set");

            PlainSocket s = new PlainSocket(underlying_socket, null);
            try {
                SSHProtocolVersionReceiver protoVerReceiver = new SSHProtocolVersionReceiver();
                protoVerReceiver.Receive(s, 5000);
                protoVerReceiver.Verify(param.Protocol);

                SSHConnection con;
                if (param.Protocol == SSHProtocol.SSH1)
                    con = new SSH1Connection(
                            param, s, receiver, protocolEventListener,
                            protoVerReceiver.ServerVersion, SSHUtil.ClientVersionString(param.Protocol));
                else
                    con = new SSH2Connection(
                            param, s, receiver, protocolEventListener,
                            protoVerReceiver.ServerVersion, SSHUtil.ClientVersionString(param.Protocol));

                s.SetHandler(con.Packetizer);
                s.RepeatAsyncRead();
                con.SendMyVersion();

                authResult = con.Connect();
                if (authResult == AuthenticationResult.Failure) {
                    s.Close();
                    return null;
                }

                return con;
            }
            catch (Exception) {
                s.Close();
                throw;
            }
        }

        protected abstract void SendMyVersion();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum ChannelType {
        Session,
        Shell,
        ForwardedLocalToRemote,
        ForwardedRemoteToLocal,
        ExecCommand,  // for scp
        Subsystem,
        AgentForward,
        Other,
    }

    /**
     * the base class for SSH channels
     */
    /// <exclude/>
    public abstract class SSHChannel {
        protected ChannelType _type;
        protected int _localID; // FIXME: should be uint
        protected int _remoteID; // FIXME: should be uint
        private SSHConnection _connection;

        protected SSHChannel(SSHConnection con, ChannelType type, int localID) {
            con.ChannelCollection.RegisterChannel(localID, this);
            _connection = con;
            _type = type;
            _localID = localID;
        }

        public int LocalChannelID {
            get {
                return _localID;
            }
        }
        public int RemoteChannelID {
            get {
                return _remoteID;
            }
        }
        public SSHConnection Connection {
            get {
                return _connection;
            }
        }
        public ChannelType Type {
            get {
                return _type;
            }
        }

        /**
         * resizes the size of terminal
         */
        public abstract void ResizeTerminal(int width, int height, int pixel_width, int pixel_height);

        /**
        * transmits channel data 
        */
        public abstract void Transmit(byte[] data);

        /**
        * transmits channel data 
        */
        public abstract void Transmit(byte[] data, int offset, int length);

        /**
         * sends EOF(SSH2 only)
         */
        public abstract void SendEOF();

        /**
         * closes this channel
         */
        public abstract void Close();


    }
}
