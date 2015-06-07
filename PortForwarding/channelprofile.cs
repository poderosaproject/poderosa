/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: channelprofile.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;

using Granados;

namespace Poderosa.PortForwarding {
    internal class ChannelProfileCollection : IEnumerable {
        private ArrayList _data;

        public ChannelProfileCollection() {
            _data = new ArrayList();
        }
        public int Count {
            get {
                return _data.Count;
            }
        }

        public void Load(ConfigNode parent) {
            ConfigNode n = parent.FindChildConfigNode("profiles");
            if (n != null) {
                foreach (ConfigNode ch in n.Children) {
                    ChannelProfile p = null;
                    if (ch.Name == "local-to-remote")
                        p = new LocalToRemoteChannelProfile();
                    else if (ch.Name == "remote-to-local")
                        p = new RemoteToLocalChannelProfile();
                    else
                        throw new FormatException(ch.Name + " is invalid channel profile name.");

                    p.Import(ch);
                    _data.Add(p);
                }
            }
        }
        public void Save(ConfigNode parent) {
            ConfigNode n = new ConfigNode("profiles");
            foreach (ChannelProfile p in _data)
                p.Save(n);
            parent.AddChild(n);
        }

        public IEnumerator GetEnumerator() {
            return _data.GetEnumerator();
        }

        public void AddProfile(ChannelProfile p) {
            _data.Add(p);
        }

        public void RemoveProfile(ChannelProfile p) {
            _data.Remove(p);
        }

        public void ReplaceProfile(ChannelProfile p1, ChannelProfile p2) {
            _data[_data.IndexOf(p1)] = p2;
        }
        public int IndexOf(ChannelProfile p) {
            return _data.IndexOf(p);
        }
        public void InsertAt(int index, ChannelProfile prof) {
            _data.Insert(index, prof);
        }
    }

    //接続パラメータ
    internal abstract class ChannelProfile {

        protected string _sshHost; //SSHサーバのホスト
        protected ushort _sshPort;
        protected string _sshAccount;
        protected AuthenticationType _authType;
        protected string _privateKeyFile;
        protected string _passphrase;

        protected ProtocolType _protocol;
        protected ushort _listenPort;
        protected string _destinationHost;
        protected ushort _destinationPort;

        protected bool _allowsForeignConnection;
        protected bool _useIPv6;

        public ChannelProfile() {
            _sshPort = 22;
            _authType = AuthenticationType.Password;
            _protocol = ProtocolType.Tcp;
        }

        public string SSHHost {
            get {
                return _sshHost;
            }
            set {
                _sshHost = value;
            }
        }
        public ushort SSHPort {
            get {
                return _sshPort;
            }
            set {
                _sshPort = value;
            }
        }
        public string SSHAccount {
            get {
                return _sshAccount;
            }
            set {
                _sshAccount = value;
            }
        }
        public AuthenticationType AuthType {
            get {
                return _authType;
            }
            set {
                _authType = value;
            }
        }
        public string PrivateKeyFile {
            get {
                return _privateKeyFile;
            }
            set {
                _privateKeyFile = value;
            }
        }
        public string Passphrase {
            get {
                return _passphrase;
            }
            set {
                _passphrase = value;
            }
        }

        public ushort ListenPort {
            get {
                return _listenPort;
            }
            set {
                _listenPort = value;
            }
        }
        public string DestinationHost {
            get {
                return _destinationHost;
            }
            set {
                _destinationHost = value;
            }
        }
        public ushort DestinationPort {
            get {
                return _destinationPort;
            }
            set {
                _destinationPort = value;
            }
        }

        //これはTCP、UDPのどちらか
        public ProtocolType ProtocolType {
            get {
                return _protocol;
            }
            set {
                _protocol = value;
            }
        }
        public bool AllowsForeignConnection {
            get {
                return _allowsForeignConnection;
            }
            set {
                _allowsForeignConnection = value;
            }
        }
        public bool UseIPv6 {
            get {
                return _useIPv6;
            }
            set {
                _useIPv6 = value;
            }
        }

        public abstract void Save(ConfigNode node);

        public void ExportTo(ConfigNode node) {
            node["ssh-host"] = _sshHost;
            node["ssh-port"] = _sshPort.ToString();
            node["account"] = _sshAccount;
            node["auth-type"] = _authType.ToString();
            if (_authType == AuthenticationType.PublicKey)
                node["keyfile"] = _privateKeyFile;
            node["protocol"] = _protocol.ToString();
            node["listen-port"] = _listenPort.ToString();
            node["dest-host"] = _destinationHost;
            node["dest-port"] = _destinationPort.ToString();
            node["allows-foreign-connection"] = _allowsForeignConnection.ToString();
            node["ipv6"] = _useIPv6.ToString();
        }
        public void Import(ConfigNode node) {
            _sshHost = node["ssh-host"];
            _sshPort = Util.ParsePort(node["ssh-port"], 22);
            _sshAccount = node["account"];
            _authType = Util.ParseAuthType(node["auth-type"], AuthenticationType.Password);
            if (_authType == AuthenticationType.PublicKey)
                _privateKeyFile = node["keyfile"];
            _protocol = Util.ParseProtocol(node["protocol"], ProtocolType.Tcp);
            _listenPort = Util.ParsePort(node["listen-port"]);
            _destinationHost = node["dest-host"];
            _destinationPort = Util.ParsePort(node["dest-port"]);
            _allowsForeignConnection = Util.ParseBool(node["allows-foreign-connection"], false);
            _useIPv6 = Util.ParseBool(node["ipv6"], false);
        }
    }

    internal sealed class LocalToRemoteChannelProfile : ChannelProfile {
        public override void Save(ConfigNode parent) {
            ConfigNode ch = new ConfigNode("local-to-remote");
            base.ExportTo(ch);
            parent.AddChild(ch);
        }
    }

    internal sealed class RemoteToLocalChannelProfile : ChannelProfile {
        public override void Save(ConfigNode parent) {
            ConfigNode ch = new ConfigNode("remote-to-local");
            base.ExportTo(ch);
            parent.AddChild(ch);
        }
    }


}
