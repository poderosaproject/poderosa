/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: ConnectionParameter.cs,v 1.5 2011/10/27 23:21:56 kzmi Exp $
*/

using System;
using System.Security.Cryptography;

using Granados.PKI;
using Granados.KnownHosts;

namespace Granados {

    /// <summary>
    /// SSH connection parameter.
    /// </summary>
    /// <remarks>
    /// Fill the properties of ConnectionParameter object before you start the connection.
    /// </remarks>
    public class SSHConnectionParameter : ICloneable {

        /// <summary>
        /// Host name.
        /// </summary>
        public string HostName {
            get;
            set;
        }

        /// <summary>
        /// Port number.
        /// </summary>
        public int PortNumber {
            get;
            set;
        }

        /// <summary>
        /// SSH Protocol version.
        /// </summary>
        public SSHProtocol Protocol {
            get;
            set;
        }

        /// <summary>
        /// Preferable cipher algorithms.
        /// </summary>
        public CipherAlgorithm[] PreferableCipherAlgorithms {
            get;
            set;
        }

        /// <summary>
        /// Preferable host key algorithms.
        /// </summary>
        public PublicKeyAlgorithm[] PreferableHostKeyAlgorithms {
            get;
            set;
        }

        /// <summary>
        /// Authentication type.
        /// </summary>
        public AuthenticationType AuthenticationType {
            get;
            set;
        }

        /// <summary>
        /// User name for login.
        /// </summary>
        public string UserName {
            get;
            set;
        }

        /// <summary>
        /// Password for login.
        /// </summary>
        public string Password {
            get;
            set;
        }

        /// <summary>
        /// Identity file path.
        /// </summary>
        public string IdentityFile {
            get;
            set;
        }

        /// <summary>
        /// Callback to verify a host key.
        /// </summary>
        public VerifySSHHostKeyDelegate VerifySSHHostKey {
            get;
            set;
        }

        /// <summary>
        /// Terminal name. (vt100, xterm, etc.)
        /// </summary>
        public string TerminalName {
            get;
            set;
        }

        /// <summary>
        /// Terminal columns.
        /// </summary>
        public int TerminalWidth {
            get;
            set;
        }

        /// <summary>
        /// Terminal raws.
        /// </summary>
        public int TerminalHeight {
            get;
            set;
        }

        /// <summary>
        /// Terminal width in pixels.
        /// </summary>
        public int TerminalPixelWidth {
            get;
            set;
        }

        /// <summary>
        /// Terminal height in pixels.
        /// </summary>
        public int TerminalPixelHeight {
            get;
            set;
        }

        /// <summary>
        /// Whether integrity of the incoming packet is checked using MAC.
        /// </summary>
        public bool CheckMACError {
            get;
            set;
        }

        /// <summary>
        /// Window size of the SSH2 channel.
        /// </summary>
        /// <remarks>This property is used only in SSH2.</remarks>
        public int WindowSize {
            get;
            set;
        }

        /// <summary>
        /// Maximum packet size of the SSH2 connection.
        /// </summary>
        /// <remarks>This property is used only in SSH2.</remarks>
        public int MaxPacketSize {
            get;
            set;
        }

        /// <summary>
        /// End of line characters for terminating a version string.
        /// </summary>
        /// <remarks>
        /// Some server may expect irregular end-of-line character(s).
        /// Initial value is '\n' for SSH1 and '/r/n' for SSH2.
        /// </remarks>
        public string VersionEOL {
            get {
                return _versionEOL ?? ((Protocol == SSHProtocol.SSH1) ? "\n" : "\r\n");
            }
            set {
                _versionEOL = value;
            }
        }
        private string _versionEOL;

        /// <summary>
        /// Protocol event tracer. (optional)
        /// </summary>
        public ISSHEventTracer EventTracer {
            get;
            set;
        }

        /// <summary>
        /// Agent forward (optional)
        /// </summary>
        public IAgentForward AgentForward {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hostName">Host name</param>
        /// <param name="portNumber">port number</param>
        /// <param name="protocol">SSH protocol version</param>
        /// <param name="authType">authentication type</param>
        /// <param name="userName">user name for login</param>
        /// <param name="password">password for login. pass empty string for the keyboard interactive mode.</param>
        public SSHConnectionParameter(string hostName, int portNumber, SSHProtocol protocol, AuthenticationType authType, string userName, string password) {
            HostName = hostName;
            PortNumber = portNumber;
            Protocol = protocol;
            PreferableCipherAlgorithms = new CipherAlgorithm[] { CipherAlgorithm.AES256CTR, CipherAlgorithm.AES256, CipherAlgorithm.AES192CTR, CipherAlgorithm.AES192, CipherAlgorithm.AES128CTR, CipherAlgorithm.AES128, CipherAlgorithm.Blowfish, CipherAlgorithm.TripleDES };
            PreferableHostKeyAlgorithms = new PublicKeyAlgorithm[] { PublicKeyAlgorithm.DSA, PublicKeyAlgorithm.RSA };
            AuthenticationType = authType;
            UserName = userName;
            Password = password;
            TerminalName = "vt100";
            WindowSize = 0x1000;
            MaxPacketSize = 0x10000;
            CheckMACError = true;
        }

        /// <summary>
        /// Clone this object.
        /// </summary>
        /// <returns>a new object.</returns>
        public object Clone() {
            return MemberwiseClone();
        }
    }

    //To receive the events of the SSH protocol negotiation, set an implementation of this interface to ConnectionParameter
    //note that :
    // * these methods are called by different threads asynchronously
    // * DO NOT throw any exceptions in the implementation
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ISSHEventTracer {
        void OnTranmission(string type, string detail);
        void OnReception(string type, string detail);
    }
}
