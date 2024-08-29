// Copyright (c) 2005-2017 Poderosa Project, All Rights Reserved.
// This file is a part of the Granados SSH Client Library that is subject to
// the license included in the distributed package.
// You may not use this file except in compliance with the license.

using Granados.AgentForwarding;
using Granados.KeyboardInteractive;
using Granados.KnownHosts;
using Granados.PKI;
using Granados.X11Forwarding;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Granados {

    /// <summary>
    /// SSH timeout settings.
    /// </summary>
    public class SSHTimeouts {
        /// <summary>
        /// General response timeout in milliseconds.
        /// </summary>
        public int ResponseTimeout {
            get;
            set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SSHTimeouts() {
            ResponseTimeout = 10000;
        }

        /// <summary>
        /// Clone this object.
        /// </summary>
        /// <returns>a new object.</returns>
        public SSHTimeouts Clone() {
            return (SSHTimeouts)MemberwiseClone();
        }
    }

    /// <summary>
    /// SSH connection parameter.
    /// </summary>
    /// <remarks>
    /// Fill the properties of ConnectionParameter object before you start the connection.
    /// </remarks>
    public class SSHConnectionParameter {

        private PublicKeyAlgorithm[] _preferableHostKeyAlgorithms;
        private PublicKeySignatureAlgorithm[] _preferableHostKeySignatureAlgorithms;

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
        /// Preferable MAC algorithms.
        /// </summary>
        public MACAlgorithm[] PreferableMacAlgorithms {
            get;
            set;
        }

        /// <summary>
        /// Preferable host key algorithms.
        /// </summary>
        public PublicKeyAlgorithm[] PreferableHostKeyAlgorithms {
            get {
                return _preferableHostKeyAlgorithms;
            }
            set {
                _preferableHostKeyAlgorithms = value;
                _preferableHostKeySignatureAlgorithms = null;
            }
        }

        /// <summary>
        /// Preferable host key algorithms and signature algorithm variants. (readonly)
        /// <para>
        /// The items are determined from <see name="PreferableHostKeyAlgorithms"/> automatically.
        /// </para>
        /// </summary>
        public PublicKeySignatureAlgorithm[] PreferableHostKeySignatureAlgorithms {
            get {
                if (_preferableHostKeySignatureAlgorithms == null) {
                    _preferableHostKeySignatureAlgorithms = MakePublicKeySignatureAlgorithmList(_preferableHostKeyAlgorithms);
                }
                return _preferableHostKeySignatureAlgorithms;
            }
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
        /// A factory function to create a handler for the keyboard-interactive authentication.
        /// </summary>
        /// <remarks>
        /// This property can be null if the keyboard-interactive authentication is not used.
        /// </remarks>
        public Func<ISSHConnection, IKeyboardInteractiveAuthenticationHandler> KeyboardInteractiveAuthenticationHandlerCreator {
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
        /// Key provider for the agent forwarding.
        /// </summary>
        /// <remarks>
        /// This property can be null.<br/>
        /// If this property was not null, the agent forwarding will be requested to the server before a new shell is opened.
        /// </remarks>
        public IAgentForwardingAuthKeyProvider AgentForwardingAuthKeyProvider {
            get;
            set;
        }

        /// <summary>
        /// X11 forwarding parameters.
        /// </summary>
        /// <remarks>
        /// This property can be null.<br/>
        /// If this property was not null, the X11 forwarding will be requested to the server before a new shell is opened.
        /// </remarks>
        public X11ForwardingParams X11ForwardingParams {
            get;
            set;
        }

        /// <summary>
        /// Timeout settings.
        /// </summary>
        public SSHTimeouts Timeouts {
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
            // PreferableCipherAlgorithms will be replaced later with a list created according to user-defined priorities or default priorities
            PreferableCipherAlgorithms = new CipherAlgorithm[] { CipherAlgorithm.AES256GCM, CipherAlgorithm.AES128GCM, CipherAlgorithm.AES256CTR, CipherAlgorithm.AES256, CipherAlgorithm.AES192CTR, CipherAlgorithm.AES192, CipherAlgorithm.AES128CTR, CipherAlgorithm.AES128, CipherAlgorithm.Blowfish, CipherAlgorithm.TripleDES };
            // MAC algorithms "AEAD_AES_256_GCM" and "AEAD_AES_128_GCM" should have the lowest priority.
            // If they are selected prior to the other algorithms, the cipher algorithm will also be overridden as described in RFC5647. This would make it difficult to control the cipher algorithm.
            PreferableMacAlgorithms = new MACAlgorithm[] { MACAlgorithm.HMACSHA256, MACAlgorithm.HMACSHA512, MACAlgorithm.HMACSHA1, MACAlgorithm.AEAD_AES_256_GCM, MACAlgorithm.AEAD_AES_128_GCM };
            PreferableHostKeyAlgorithms = new PublicKeyAlgorithm[] { PublicKeyAlgorithm.DSA, PublicKeyAlgorithm.RSA };
            AuthenticationType = authType;
            UserName = userName;
            Password = password;
            TerminalName = "vt100";
            WindowSize = 0x1000;
            MaxPacketSize = 0x10000;
            CheckMACError = true;
            VerifySSHHostKey = p => true;
            Timeouts = new SSHTimeouts();
        }

        /// <summary>
        /// Clone this object.
        /// </summary>
        /// <returns>a new object.</returns>
        public SSHConnectionParameter Clone() {
            SSHConnectionParameter p = (SSHConnectionParameter)MemberwiseClone();
            if (p.PreferableCipherAlgorithms != null) {
                p.PreferableCipherAlgorithms = (CipherAlgorithm[])p.PreferableCipherAlgorithms.Clone();
            }
            if (p._preferableHostKeyAlgorithms != null) {
                p._preferableHostKeyAlgorithms = (PublicKeyAlgorithm[])p._preferableHostKeyAlgorithms.Clone();
            }
            if (p._preferableHostKeySignatureAlgorithms != null) {
                p._preferableHostKeySignatureAlgorithms = (PublicKeySignatureAlgorithm[])p._preferableHostKeySignatureAlgorithms.Clone();
            }
            if (p.X11ForwardingParams != null) {
                p.X11ForwardingParams = p.X11ForwardingParams.Clone();
            }
            p.Timeouts = Timeouts.Clone();
            return p;
        }

        /// <summary>
        /// Make a list of PublicKeyAndSignatureAlgorithm from a list of PublicKeyAlgorithm
        /// </summary>
        /// <param name="algorithms"></param>
        /// <returns></returns>
        private PublicKeySignatureAlgorithm[] MakePublicKeySignatureAlgorithmList(PublicKeyAlgorithm[] algorithms) {
            var list = new List<PublicKeySignatureAlgorithm>();
            foreach (PublicKeyAlgorithm a in algorithms) {
                IEnumerable<PublicKeySignatureAlgorithm> variants;
                if (PublicKeySignatureAlgorithm.VariantsMap.TryGetValue(a, out variants)) {
                    list.AddRange(variants);
                }
            }

            // add missing algorithms/variants
            var names = new HashSet<string>(list.Select(v => v.SignatureAlgorithmName));
            list.AddRange(
                PublicKeySignatureAlgorithm.Supported
                    .Where(v => !names.Contains(v.SignatureAlgorithmName))
            );
            return list.ToArray();
        }
    }
}
