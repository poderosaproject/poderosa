/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Connector.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
 */
using System;
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

using Granados;
using Poderosa.Plugins;
using Poderosa.Util;
using Poderosa.Forms;

namespace Poderosa.Protocols {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    class SSHDebugTracer : ISSHEventTracer {
        public void OnTranmission(string type, string detail) {
            Debug.WriteLine("T:" + type + ":" + detail);
        }
        public void OnReception(string type, string detail) {
            Debug.WriteLine("R:" + type + ":" + detail);
        }
    }

    internal class SSHConnector : InterruptableConnector {

        private ISSHLoginParameter _destination;
        private HostKeyVerifierBridge _keycheck;
        private TerminalConnection _result;

        public SSHConnector(ISSHLoginParameter destination, HostKeyVerifierBridge keycheck) {
            _destination = destination;
            _keycheck = keycheck;
        }

        protected override void Negotiate() {
            ITerminalParameter term = (ITerminalParameter)_destination.GetAdapter(typeof(ITerminalParameter));
            ITCPParameter tcp = (ITCPParameter)_destination.GetAdapter(typeof(ITCPParameter));

            SSHConnectionParameter con = new SSHConnectionParameter();
#if DEBUG
            // con.EventTracer = new SSHDebugTracer();
#endif
            con.Protocol = _destination.Method;
            con.CheckMACError = PEnv.Options.SSHCheckMAC;
            con.UserName = _destination.Account;
            con.Password = _destination.PasswordOrPassphrase;
            con.AuthenticationType = _destination.AuthenticationType;
            con.IdentityFile = _destination.IdentityFileName;
            con.TerminalWidth = term.InitialWidth;
            con.TerminalHeight = term.InitialHeight;
            con.TerminalName = term.TerminalType;
            con.WindowSize = PEnv.Options.SSHWindowSize;
            con.PreferableCipherAlgorithms = LocalSSHUtil.ParseCipherAlgorithm(PEnv.Options.CipherAlgorithmOrder);
            con.PreferableHostKeyAlgorithms = LocalSSHUtil.ParsePublicKeyAlgorithm(PEnv.Options.HostKeyAlgorithmOrder);
            con.AgentForward = _destination.AgentForward;
            if (ProtocolsPlugin.Instance.ProtocolOptions.LogSSHEvents)
                con.EventTracer = new SSHEventTracer(tcp.Destination);
            if (_keycheck != null)
                con.KeyCheck += new HostKeyCheckCallback(this.CheckKey);


            SSHTerminalConnection r = new SSHTerminalConnection(_destination);
            SSHConnection ssh = SSHConnection.Connect(con, r.ConnectionEventReceiver, _socket);
            if (ssh != null) {
                if (PEnv.Options.RetainsPassphrase && _destination.AuthenticationType != AuthenticationType.KeyboardInteractive)
                    ProtocolsPlugin.Instance.PassphraseCache.Add(tcp.Destination, _destination.Account, _destination.PasswordOrPassphrase); //接続成功時のみセット
                //_destination.PasswordOrPassphrase = ""; 接続の複製のためにここで消さずに残しておく
                r.AttachTransmissionSide(ssh);
                r.UsingSocks = _socks != null;
                _result = r;
            }
            else {
                throw new IOException(PEnv.Strings.GetString("Message.SSHConnector.Cancelled"));
            }
        }
        internal override TerminalConnection Result {
            get {
                return _result;
            }
        }

        private bool CheckKey(SSHConnectionInfo ci) {
            return _keycheck.Vefiry(_destination, ci);
        }
    }

    internal class TelnetConnector : InterruptableConnector {
        private ITCPParameter _destination;
        private TelnetTerminalConnection _result;

        public TelnetConnector(ITCPParameter destination) {
            _destination = destination;
        }

        protected override void Negotiate() {
            ITerminalParameter term = (ITerminalParameter)_destination.GetAdapter(typeof(ITerminalParameter));
            TelnetNegotiator neg = new TelnetNegotiator(term.TerminalType, term.InitialWidth, term.InitialHeight);
            TelnetTerminalConnection r = new TelnetTerminalConnection(_destination, neg, new PlainPoderosaSocket(_socket));
            //BACK-BURNER r.UsingSocks = _socks!=null;
            _result = r;
        }

        internal override TerminalConnection Result {
            get {
                return _result;
            }
        }
    }

    internal class SilentClient : ISynchronizedConnector, IInterruptableConnectorClient {
        private IPoderosaForm _form;
        private AutoResetEvent _event;
        private ITerminalConnection _result;
        private string _errorMessage;
        private bool _timeout;

        public SilentClient(IPoderosaForm form) {
            _event = new AutoResetEvent(false);
            _form = form;
        }

        public void SuccessfullyExit(ITerminalConnection result) {
            if (_timeout)
                return;
            _result = result;
            //_result.SetServerInfo(((TCPTerminalParam)_result.Param).Host, swt.IPAddress);
            _event.Set();
        }
        public void ConnectionFailed(string message) {
            Debug.Assert(message != null);
            _errorMessage = message;
            if (_timeout)
                return;
            _event.Set();
        }

        public IInterruptableConnectorClient InterruptableConnectorClient {
            get {
                return this;
            }
        }

        public ITerminalConnection WaitConnection(IInterruptable intr, int timeout) {
            //ちょっと苦しい判定
            if (!(intr is InterruptableConnector) && !(intr is LocalShellUtil.Connector))
                throw new ArgumentException("IInterruptable object is not correct");

            if (!_event.WaitOne(timeout, true)) {
                _timeout = true; //TODO 接続を中止すべきか
                _errorMessage = PEnv.Strings.GetString("Message.ConnectionTimedOut");
            }
            _event.Close();

            if (_result == null) {
                if (_form != null)
                    _form.Warning(_errorMessage);
                return null;
            }
            else
                return _result;
        }
    }

    internal class SSHEventTracer : ISSHEventTracer {
        private IPoderosaLog _log;
        private PoderosaLogCategoryImpl _category;

        public SSHEventTracer(string destination) {
            _log = ((IPoderosaApplication)ProtocolsPlugin.Instance.PoderosaWorld.GetAdapter(typeof(IPoderosaApplication))).PoderosaLog;
            _category = new PoderosaLogCategoryImpl(String.Format("SSH:{0}", destination));
        }

        public void OnReception(string type, string detail) {
            _log.AddItem(_category, String.Format("Received: {0}", detail));
        }

        public void OnTranmission(string type, string detail) {
            _log.AddItem(_category, String.Format("Transmitted: {0}", detail));
        }
    }
}
