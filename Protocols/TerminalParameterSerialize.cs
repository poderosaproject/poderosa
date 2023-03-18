// Copyright 2004-2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Granados;

using Poderosa.Serializing;
using System.Globalization;
using Granados.X11Forwarding;

namespace Poderosa.Protocols {
    internal abstract class TerminalParameterSerializer : ISerializeServiceElement {

        private Type _concreteType;

        public TerminalParameterSerializer(Type concreteType) {
            _concreteType = concreteType;
        }

        public void Serialize(TerminalParameter tp, StructuredText node) {
            if (tp.TerminalType != TerminalParameter.DEFAULT_TERMINAL_TYPE)
                node.Set("terminal-type", tp.TerminalType);
            if (tp.AutoExecMacroPath != null)
                node.Set("autoexec-macro", tp.AutoExecMacroPath);
        }
        public void Deserialize(TerminalParameter tp, StructuredText node) {
            tp.SetTerminalName(node.Get("terminal-type", TerminalParameter.DEFAULT_TERMINAL_TYPE));
            tp.AutoExecMacroPath = node.Get("autoexec-macro", null);
        }

        public Type ConcreteType {
            get {
                return _concreteType;
            }
        }

        public abstract StructuredText Serialize(object obj);
        public abstract object Deserialize(StructuredText node);
    }

    internal abstract class TCPParameterSerializer : TerminalParameterSerializer {
        private int _defaultPort;

        public TCPParameterSerializer(Type concreteType, int defaultport)
            : base(concreteType) {
            _defaultPort = defaultport;
        }


        public void Serialize(TCPParameter tp, StructuredText node) {
            base.Serialize(tp, node);
            node.Set("destination", tp.Destination);
            if (tp.Port != _defaultPort)
                node.Set("port", tp.Port.ToString());
        }
        public void Deserialize(TCPParameter tp, StructuredText node) {
            base.Deserialize(tp, node);
            tp.Destination = node.Get("destination", "");
            Debug.Assert(tp.Destination != null);
            tp.Port = ParseUtil.ParseInt(node.Get("port"), _defaultPort);
        }
    }

    internal class TelnetParameterSerializer : TCPParameterSerializer {

        public TelnetParameterSerializer()
            : base(typeof(TelnetParameter), 23) {
        }

        public override StructuredText Serialize(object obj) {
            StructuredText node = new StructuredText(this.ConcreteType.FullName);
            base.Serialize((TCPParameter)obj, node);
            node.Set("telnetNewLine", ((TelnetParameter)obj).TelnetNewLine.ToString());
            return node;
        }

        public override object Deserialize(StructuredText node) {
            TelnetParameter t = new TelnetParameter();
            base.Deserialize(t, node);
            // Note:
            //   for the backward compatibility, TelnetNewLine becomes false
            //   if parameter "telnetNewLine" doesn't exist.
            t.TelnetNewLine = Boolean.Parse(node.Get("telnetNewLine", Boolean.FalseString));
            return t;
        }
    }

    internal class SSHParameterSerializer : TCPParameterSerializer {
        public SSHParameterSerializer()
            : base(typeof(SSHLoginParameter), 22) {
        }
        //派生クラスからの指定用
        protected SSHParameterSerializer(Type t)
            : base(t, 22) {
        }

        public void Serialize(SSHLoginParameter tp, StructuredText node) {
            base.Serialize(tp, node);
            if (tp.Method != SSHProtocol.SSH2)
                node.Set("method", tp.Method.ToString());
            if (tp.AuthenticationType != AuthenticationType.Password)
                node.Set("authentication", tp.AuthenticationType.ToString());
            node.Set("account", tp.Account);
            if (tp.IdentityFileName.Length > 0)
                node.Set("identityFileName", tp.IdentityFileName);

            node.Set("enableAgentForwarding", tp.EnableAgentForwarding.ToString());

            node.Set("enableX11Forwarding", tp.EnableX11Forwarding.ToString());

            if (tp.X11Forwarding != null) {
                StructuredText x11Node = node.AddChild("x11Forwarding");
                x11Node.Set("display", tp.X11Forwarding.Display.ToString(NumberFormatInfo.InvariantInfo));
                x11Node.Set("screen", tp.X11Forwarding.Screen.ToString(NumberFormatInfo.InvariantInfo));
                x11Node.Set("needAuth", tp.X11Forwarding.NeedAuth.ToString());
                if (tp.X11Forwarding.XauthorityFile != null) {
                    x11Node.Set("xauthorityFile", tp.X11Forwarding.XauthorityFile);
                }
                x11Node.Set("useCygwinUnixDomainSocket", tp.X11Forwarding.UseCygwinUnixDomainSocket.ToString());
                if (tp.X11Forwarding.X11UnixFolder != null) {
                    x11Node.Set("x11UnixFolder", tp.X11Forwarding.X11UnixFolder);
                }
            }
        }
        public void Deserialize(SSHLoginParameter tp, StructuredText node) {
            base.Deserialize(tp, node);
            tp.Method = "SSH1".Equals(node.Get("method")) ? SSHProtocol.SSH1 : SSHProtocol.SSH2;
            tp.AuthenticationType = ParseUtil.ParseEnum<AuthenticationType>(node.Get("authentication", ""), AuthenticationType.Password);
            tp.Account = node.Get("account", "");
            tp.IdentityFileName = node.Get("identityFileName", "");

            tp.EnableAgentForwarding = GetBoolValue(node, "enableAgentForwarding", false);

            tp.EnableX11Forwarding = GetBoolValue(node, "enableX11Forwarding", false);

            StructuredText x11Node = node.FindChild("x11Forwarding");
            if (x11Node != null) {
                int display = GetIntValue(x11Node, "display", 0);
                X11ForwardingParams x11params = new X11ForwardingParams(display);
                x11params.Screen = GetIntValue(x11Node, "screen", 0);
                x11params.NeedAuth = GetBoolValue(x11Node, "needAuth", false);
                x11params.XauthorityFile = x11Node.Get("xauthorityFile", null);
                x11params.UseCygwinUnixDomainSocket = GetBoolValue(x11Node, "useCygwinUnixDomainSocket", false);
                x11params.X11UnixFolder = x11Node.Get("x11UnixFolder", null);
                tp.X11Forwarding = x11params;
            }
        }

        public override StructuredText Serialize(object obj) {
            StructuredText t = new StructuredText(this.ConcreteType.FullName);
            Serialize((SSHLoginParameter)obj, t);
            return t;
        }

        public override object Deserialize(StructuredText node) {
            SSHLoginParameter t = new SSHLoginParameter();
            Deserialize(t, node);
            return t;
        }

        private bool GetBoolValue(StructuredText node, string key, bool defaultValue) {
            string str = node.Get(key);
            if (str != null) {
                bool val;
                if (Boolean.TryParse(str, out val)) {
                    return val;
                }
            }
            return defaultValue;
        }

        private int GetIntValue(StructuredText node, string key, int defaultValue) {
            string str = node.Get(key);
            if (str != null) {
                int val;
                if (Int32.TryParse(str, out val)) {
                    return val;
                }
            }
            return defaultValue;
        }
    }

    internal class LocalShellParameterSerializer : TerminalParameterSerializer {
        public LocalShellParameterSerializer()
            : base(typeof(LocalShellParameter)) {
        }

        public void Serialize(LocalShellParameter tp, StructuredText node) {
            base.Serialize(tp, node);
            if (CygwinUtil.DefaultHome != tp.Home)
                node.Set("home", tp.Home);
            if (CygwinUtil.DefaultShell != tp.ShellName)
                node.Set("shellName", tp.ShellName);
            if (CygwinUtil.DefaultCygwinDir != tp.CygwinDir)
                node.Set("cygwin-directory", tp.CygwinDir);
            if (CygwinUtil.DefaultCygwinArchitecture != tp.CygwinArchitecture)
                node.Set("cygwin-architecture", tp.CygwinArchitecture.ToString());
        }
        public void Deserialize(LocalShellParameter tp, StructuredText node) {
            base.Deserialize(tp, node);
            tp.Home = node.Get("home", CygwinUtil.DefaultHome);
            tp.ShellName = node.Get("shellName", CygwinUtil.DefaultShell);
            tp.CygwinDir = node.Get("cygwin-directory", CygwinUtil.DefaultCygwinDir);

            string cygwinArch = node.Get("cygwin-architecture", null);
            if (cygwinArch == null) {
                tp.CygwinArchitecture = CygwinUtil.DefaultCygwinArchitecture;
            }
            else {
                CygwinArchitecture cygwinArchVal;
                if (Enum.TryParse(cygwinArch, out cygwinArchVal)) {
                    tp.CygwinArchitecture = cygwinArchVal;
                }
                else {
                    tp.CygwinArchitecture = CygwinUtil.DefaultCygwinArchitecture;
                }
            }
        }
        public override StructuredText Serialize(object obj) {
            StructuredText t = new StructuredText(this.ConcreteType.FullName);
            Serialize((LocalShellParameter)obj, t);
            return t;
        }

        public override object Deserialize(StructuredText node) {
            LocalShellParameter t = new LocalShellParameter();
            Deserialize(t, node);
            return t;
        }
    }

    //TODO シリアルポート

}
