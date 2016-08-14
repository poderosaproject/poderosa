/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: ConnectionRoot.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/

using Granados;
using Granados.IO;
using Granados.SSH1;
using Granados.SSH2;
using Granados.Util;
using Granadso.SSH;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

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
                    ISSHConnectionEventHandler connectionEventHandler,
                    ISSHProtocolEventLogger protocolEventLogger,
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
                                param, s, connectionEventHandler, protocolEventLogger,
                                protoVerReceiver.ServerVersion, SSHUtil.ClientVersionString(param.Protocol));
                    s.SetHandler(con.Packetizer);
                    s.RepeatAsyncRead();
                    con.SendMyVersion();
                    authResult = con.Connect();
                    sshConnection = con;
                }
                else {
                    var con = new SSH2Connection(
                                param, s, connectionEventHandler, protocolEventLogger,
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

namespace Granadso.SSH {

    /// <summary>
    /// A class reads SSH protocol version
    /// </summary>
    internal class SSHProtocolVersionReceiver {

        private string _serverVersion = null;
        private readonly List<string> _lines = new List<string>();

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
            byte[] buf = new byte[1];
            DateTime tm = DateTime.UtcNow.AddMilliseconds(timeout);
            using (MemoryStream mem = new MemoryStream()) {
                while (DateTime.UtcNow < tm && sock.SocketStatus == SocketStatus.Ready) {
                    int n = sock.ReadIfAvailable(buf);
                    if (n != 1) {
                        Thread.Sleep(10);
                        continue;
                    }
                    byte b = buf[0];
                    mem.WriteByte(b);
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

}