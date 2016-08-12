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
    /// A proxy class for reading status of the underlying socket object.
    /// </summary>
    public class SocketStatusReader {

        private readonly IGranadosSocket _socket;

        internal SocketStatusReader(IGranadosSocket socket) {
            _socket = socket;
        }

        /// <summary>
        /// Get status of the socket object.
        /// </summary>
        public SocketStatus SocketStatus {
            get {
                return _socket.SocketStatus;
            }
        }

        /// <summary>
        /// Get whether any received data are available on the socket
        /// </summary>
        public bool DataAvailable {
            get {
                return _socket.DataAvailable;
            }
        }

    }


    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public abstract class SSHConnection {


        protected SSHConnection() {
        }

        ///abstract properties

        // stream->packet converter
        internal abstract IDataHandler Packetizer {
            get;
        }

        public abstract SocketStatusReader SocketStatusReader {
            get;
        }

        public abstract bool IsOpen {
            get;
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
        public abstract void Close();

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
    /// Channel type
    /// </summary>
    public enum ChannelType {
        Session,
        Shell,
        ForwardedLocalToRemote,
        ForwardedRemoteToLocal,
        ExecCommand,
        Subsystem,
        AgentForwarding,
        Other,
    }

}
