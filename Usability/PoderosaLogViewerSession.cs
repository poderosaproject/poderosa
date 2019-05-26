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
using System.Drawing;
using System.Diagnostics;

using Poderosa.Forms;
using Poderosa.Sessions;
using Poderosa.View;
using Poderosa.Document;
using Poderosa.Plugins;
using Poderosa.Commands;

namespace Poderosa.LogViewer {
    internal class PoderosaLogViewerSession : ISession {
        private PoderosaLogViewControl _view;
        private PoderosaLogDocument _document;
        private ISessionHost _host;

        public PoderosaLogViewerSession() {
            _document = new PoderosaLogDocument(this);
            IPoderosaLog log = ((IPoderosaApplication)PoderosaLogViewerPlugin.Instance.PoderosaWorld.GetAdapter(typeof(IPoderosaApplication))).PoderosaLog;
            log.AddChangeListener(_document);
        }

        public string Caption {
            get {
                return _document.Caption;
            }
        }

        public Image Icon {
            get {
                return _document.Icon;
            }
        }

        public void InternalStart(ISessionHost host) {
            _host = host;
            _host.RegisterDocument(_document);
        }

        public void InternalTerminate() {
        }

        public PrepareCloseResult PrepareCloseDocument(IPoderosaDocument document) {
            return PrepareCloseResult.TerminateSession;
        }

        public PrepareCloseResult PrepareCloseSession() {
            return PrepareCloseResult.TerminateSession;
        }

        public void InternalAttachView(IPoderosaDocument document, IPoderosaView view) {
            _view = (PoderosaLogViewControl)view.GetAdapter(typeof(PoderosaLogViewControl));
            Debug.Assert(_view != null);
            _view.SetParent(this);
        }

        public void InternalDetachView(IPoderosaDocument document, IPoderosaView view) {
            Debug.WriteLineIf(DebugOpt.LogViewer, "LogView InternalDetach");
            _view = null;
        }

        public void InternalCloseDocument(IPoderosaDocument document) {
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PoderosaLogViewerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public PoderosaLogViewControl CurrentView {
            get {
                return _view;
            }
        }
        public bool IsWindowVisible {
            get {
                return _view != null;
            }
        }

        public PoderosaLogDocument Document {
            get {
                return _document;
            }
        }
    }

    //ViewClass
    internal class PoderosaLogViewControl : CharacterDocumentViewer, IPoderosaView, IGeneralViewCommands {
        private readonly RenderProfile _renderProfile;
        private readonly IPoderosaForm _form;
        private PoderosaLogViewerSession _session;

        public PoderosaLogViewControl(IPoderosaForm form) {
            _renderProfile = CreateLogRenderProfile();
            _form = form;
            Caret.Enabled = false;
            Caret.Blink = false;
        }

        private static RenderProfile CreateLogRenderProfile() {
            return new RenderProfile() {
                FontName = "Courier New",
                FontSize = 9,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
            };
        }

        public void SetParent(PoderosaLogViewerSession session) {
            _session = session;
            SetDocument(_session.Document);
            UpdateDocument();
        }

        public IPoderosaDocument Document {
            get {
                return _session == null ? null : _session.Document;
            }
        }

        public ISelection CurrentSelection {
            get {
                return this.Selection;
            }
        }

        public IPoderosaForm ParentForm {
            get {
                return this.FindForm() as IPoderosaForm;
            }
        }

        public void UpdateDocument() {
            if (this.IsDisposed || this.Disposing) {
                return;
            }

            RefreshViewer(ScrollAction.ScrollToBottom);
        }

        //Command
        public IPoderosaCommand Copy {
            get {
                return PoderosaLogViewerPlugin.Instance.CoreServices.WindowManager.SelectionService.DefaultCopyCommand;
            }
        }

        public IPoderosaCommand Paste {
            get {
                return null; //ペースト不可
            }
        }

        //標準幅
        public static int DefaultWidth {
            get {
                return (int)(CreateLogRenderProfile().Pitch.Width * PoderosaLogDocument.DefaultWidth);
            }
        }

        protected override RenderProfile GetCurrentRenderProfile() {
            return _renderProfile;
        }

        protected override bool DetermineScrollable() {
            return true;
        }

        protected override void OnCharacterDocumentChanged() {
            // do nothing
        }

        protected override void OnViewportSizeChanged() {
            // do nothing
        }
    }

    //DocClass
    internal class PoderosaLogDocument : AppendOnlyCharacterDocument, IPoderosaDocument, IPoderosaLogListener {
        private readonly PoderosaLogViewerSession _session;

        #region IPoderosaDocument

        public Image Icon {
            get {
                return null;
            }
        }

        public string Caption {
            get;
            private set;
        }

        public ISession OwnerSession {
            get {
                return _session;
            }
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PoderosaLogViewerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion

        public PoderosaLogDocument(PoderosaLogViewerSession session) {
            this.Caption = "Poderosa Event Log";
            this._session = session;
        }

        //初期状態の１行の文字数
        public static int DefaultWidth {
            get {
                return 80; //可変にしてもよい
            }
        }

        public void OnNewItem(IPoderosaLogItem item) {
            //カテゴリ分けなどあるかもしれないが...
            String text = String.Format("[{0}] {1}", item.Category.Name, item.Text);
            int width = PoderosaLogDocument.DefaultWidth;

            //width文字ごとに切り取り。日本語文字があるケースは未サポート
            int offset = 0;
            while (offset < text.Length) {
                int next = RuntimeUtil.AdjustIntRange(offset + width, 0, text.Length);
                GLine line = GLine.CreateSimpleGLine(text.Substring(offset, next - offset), TextDecoration.Default);
                line.EOLType = next < text.Length ? EOLType.Continue : EOLType.CRLF;
                Append(line);
                offset = next;
            }

            PoderosaLogViewControl vc = _session.CurrentView;
            if (vc != null) {
                vc.UpdateDocument();
            }
        }
    }

    //ViewFactory
    internal class LogViewerFactory : IViewFactory {
        public IPoderosaView CreateNew(IPoderosaForm parent) {
            return new PoderosaLogViewControl(parent);
        }

        public Type GetViewType() {
            return typeof(PoderosaLogViewControl);
        }

        public Type GetDocumentType() {
            return typeof(PoderosaLogDocument);
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PoderosaLogViewerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }
}
