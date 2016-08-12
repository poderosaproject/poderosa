/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: ConnectionRoot.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/

using Granados.IO;
using Granados.SSH1;
using Granados.SSH2;
using Granados.Util;
using System;
using System.Net.Sockets;

namespace Granados {

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class SSHConnection {

        /**
         * open a new SSH connection via the .NET socket
         */
        public static ISSHConnection Connect(
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

                ISSHConnection sshConnection;
                if (param.Protocol == SSHProtocol.SSH1) {
                    var con = new SSH1Connection(
                                param, s, receiver, protocolEventListener,
                                protoVerReceiver.ServerVersion, SSHUtil.ClientVersionString(param.Protocol));
                    s.SetHandler(con.Packetizer);
                    s.RepeatAsyncRead();
                    con.SendMyVersion();
                    authResult = con.Connect();
                    sshConnection = con;
                }
                else {
                    var con = new SSH2Connection(
                                param, s, receiver, protocolEventListener,
                                protoVerReceiver.ServerVersion, SSHUtil.ClientVersionString(param.Protocol));
                    s.SetHandler(con.Packetizer);
                    s.RepeatAsyncRead();
                    con.SendMyVersion();
                    authResult = con.Connect();
                    sshConnection = con;
                }

                if (authResult == AuthenticationResult.Failure) {
                    s.Close();
                    return null;
                }

                return sshConnection;
            }
            catch (Exception) {
                s.Close();
                throw;
            }
        }

    }


}
