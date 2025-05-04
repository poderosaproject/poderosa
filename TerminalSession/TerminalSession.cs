// Copyright 2004-2025 The Poderosa Project.
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
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Diagnostics;

using Poderosa.Document;
using Poderosa.Terminal;
using Poderosa.View;
using Poderosa.Protocols;
using Poderosa.ConnectionParam;
using Poderosa.Forms;
using Poderosa.Util;

namespace Poderosa.Sessions {

    //接続に対して関連付けるデータ
    internal class TerminalSession : ITerminalSession, IAbstractTerminalHost, ITerminalControlHost {
        private delegate void HostCauseCloseDelagate(string msg);

        private readonly TerminalTransmission _output;
        private readonly AbstractTerminal _terminal;
        private readonly ITerminalSettings _terminalSettings;
        private ISessionHost _sessionHost = null;
        private TerminalControl _terminalControl = null;
        private bool _terminated = false;
        private bool _commStarted = false;

        public TerminalSession(ITerminalConnection connection, ITerminalSettings terminalSettings) {
            _terminalSettings = terminalSettings;
            _terminal = AbstractTerminal.Create(new TerminalInitializeInfo(this, connection.Destination));
            _output = new TerminalTransmission(_terminal, _terminalSettings, connection);

            _terminalSettings.ChangeCaption += delegate(string caption) {
                this.OwnerWindow.DocumentTabFeature.Update(_terminal.IDocument);
            };
        }

        public void Revive(ITerminalConnection connection) {
            _output.Revive(connection, _terminal.Document.TerminalWidth, _terminal.Document.TerminalHeight);
            this.OwnerWindow.DocumentTabFeature.Update(_terminal.IDocument);
            _output.Connection.Socket.RepeatAsyncRead(_terminal); //再受信
        }

        //IAdaptable
        public IAdaptable GetAdapter(Type adapter) {
            return TerminalSessionsPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #region ITerminalSession
        public AbstractTerminal Terminal {
            get {
                return _terminal;
            }
        }
        public TerminalControl TerminalControl {
            get {
                return _terminalControl;
            }
        }
        public IPoderosaMainWindow OwnerWindow {
            get {
                ISessionHost host = _sessionHost;
                if (host == null || _terminated) {
                    return TerminalSessionsPlugin.Instance.WindowManager.ActiveWindow;
                }
                else {
                    return (IPoderosaMainWindow)host.GetParentFormFor(_terminal.IDocument).GetAdapter(typeof(IPoderosaMainWindow));
                }
            }
        }
        public ITerminalConnection TerminalConnection {
            get {
                return _output.Connection;
            }
        }
        public ITerminalSettings TerminalSettings {
            get {
                return _terminalSettings;
            }
        }
        public TerminalTransmission TerminalTransmission {
            get {
                return _output;
            }
        }
        public ISession ISession {
            get {
                return this;
            }
        }
        #endregion

        //受信スレッドから呼ぶ、Document更新の通知
        public void NotifyViewsDataArrived() {
            TerminalControl control = _terminalControl;
            if (control != null) {
                control.DataArrived();
            }
        }
        //正常・異常とも呼ばれる
        public void CloseByReceptionThread(string msg) {
            if (_terminated) {
                return;
            }

            IPoderosaMainWindow window = this.OwnerWindow;
            if (window != null) {
                Debug.Assert(window.AsControl().InvokeRequired);
                //TerminalSessionはコントロールを保有しないので、ウィンドウで代用する
                window.AsControl().Invoke(new HostCauseCloseDelagate(HostCauseClose), msg);
            }
        }
        private void HostCauseClose(string msg) {
            if (TerminalSessionsPlugin.Instance.TerminalEmulatorService.TerminalEmulatorOptions.CloseOnDisconnect) {
                ISessionHost host = _sessionHost;
                if (host != null) {
                    host.TerminateSession();
                }
            }
            else {
                IPoderosaMainWindow window = this.OwnerWindow;
                window.DocumentTabFeature.Update(_terminal.IDocument);
            }
        }

        //ISession
        public string Caption {
            get {
                string s = _terminalSettings.Caption;
                if (_output.Connection.IsClosed)
                    s += TEnv.Strings.GetString("Caption.Disconnected");
                return s;
            }
        }
        public Image Icon {
            get {
                return _terminalSettings.Icon;
            }
        }
        //TerminalSessionの開始
        public void InternalStart(ISessionHost host) {
            _sessionHost = host;
            host.RegisterDocument(_terminal.IDocument);
        }
        public void InternalTerminate() {
            _terminated = true;
            try {
                _output.Connection.Close();
                _output.Connection.Socket.ForceDisposed();
            }
            catch (Exception) {
            }
            _terminal.CloseBySession();
        }
        public PrepareCloseResult PrepareCloseDocument(IPoderosaDocument document) {
            Debug.Assert(document == _terminal.IDocument);
            return PrepareCloseResult.TerminateSession;
        }
        public PrepareCloseResult PrepareCloseSession() {
            if (TerminalSessionsPlugin.Instance.TerminalSessionOptions.AskCloseOnExit && !_output.Connection.IsClosed) {
                if (this.OwnerWindow.AskUserYesNo(String.Format(TEnv.Strings.GetString("Message.AskCloseTerminalSession"), this.Caption)) == DialogResult.Yes)
                    return PrepareCloseResult.TerminateSession;
                else
                    return PrepareCloseResult.Cancel;
            }
            else
                return PrepareCloseResult.TerminateSession;
        }

        public void InternalAttachView(IPoderosaDocument document, IPoderosaView view) {
            Debug.WriteLineIf(DebugOpt.ViewManagement, "ATTACH VIEW");
            Debug.Assert(document == _terminal.IDocument);
            TerminalView tv = (TerminalView)view.GetAdapter(typeof(TerminalView));
            Debug.Assert(tv != null);
            TerminalControl tp = tv.TerminalControl;
            Debug.Assert(tp != null);
            tp.Attach(this);

            _terminalControl = tp;
            _terminal.Attached(tp);

            // The data receiving loop is started after the first attachment.
            // To control the scroll position properly, the terminal size must be determined first.
            if (!_commStarted) {
                _output.Connection.Socket.RepeatAsyncRead(_terminal);
                _commStarted = true;
            }
        }
        public void InternalDetachView(IPoderosaDocument document, IPoderosaView view) {
            Debug.WriteLineIf(DebugOpt.ViewManagement, "DETACH VIEW");
            Debug.Assert(document == _terminal.IDocument);
            TerminalView tv = (TerminalView)view.GetAdapter(typeof(TerminalView));
            Debug.Assert(tv != null);
            TerminalControl tp = tv.TerminalControl;
            Debug.Assert(tp != null); //Detachするときにはこのビューになっている必要あり

            if (!tp.IsDisposed) {
                _terminal.Detach(tp);
                tp.Detach();
            }

            _terminalControl = null;
        }
        public void InternalCloseDocument(IPoderosaDocument document) {
            //do nothing
        }
    }

}
