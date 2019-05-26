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
using System.Windows.Forms;

using Poderosa.Forms;
using Poderosa.Sessions;
using Poderosa.View;
using Poderosa.Document;
using Poderosa.Plugins;
using Poderosa.Commands;

namespace Poderosa.Terminal {
    internal class CommandResultSession : ISession {

        private CommandResultViewerControl _view;
        private CommandResultDocument _document;
        private RenderProfile _renderProfile;
        private ISessionHost _host;

        public CommandResultSession(CommandResultDocument doc, RenderProfile prof) {
            _document = doc;
            doc.OwnerSession = this;
            _renderProfile = prof;
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
            _view = (CommandResultViewerControl)view.GetAdapter(typeof(CommandResultViewerControl));
            Debug.Assert(_view != null);
            _view.SetParent(this);
        }

        public void InternalDetachView(IPoderosaDocument document, IPoderosaView view) {
            _view = null;
        }

        public void InternalCloseDocument(IPoderosaDocument document) {
        }

        public IAdaptable GetAdapter(Type adapter) {
            return TerminalEmulatorPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public CommandResultViewerControl CurrentView {
            get {
                return _view;
            }
        }
        public bool IsWindowVisible {
            get {
                return _view != null;
            }
        }

        public CommandResultDocument Document {
            get {
                return _document;
            }
        }
        public RenderProfile RenderProfile {
            get {
                return _renderProfile;
            }
        }

        public static void Start(AbstractTerminal terminal, CommandResultDocument document) {
            try {
                TerminalControl tc = terminal.TerminalHost.TerminalControl;
                Debug.Assert(tc != null);
                RenderProfile rp = (RenderProfile)tc.GetRenderProfile().Clone();
                CommandResultSession session = new CommandResultSession(document, rp);
                TerminalDocument terminaldoc = terminal.GetDocument();
                PopupViewCreationParam cp = new PopupViewCreationParam(_viewFactory);
                Size initialViewSize =
                    CharacterDocumentViewer.EstimateViewSize(
                        rp, Math.Min(document.GetRowIDSpan().Length, 20), 100 /*dummy*/);
                initialViewSize.Width = tc.ClientSize.Width;
                cp.InitialSize = initialViewSize;
                cp.OwnedByCommandTargetWindow = GEnv.Options.CommandPopupAlwaysOnTop;
                cp.ShowInTaskBar = GEnv.Options.CommandPopupInTaskBar;

                IWindowManager wm = TerminalEmulatorPlugin.Instance.GetWindowManager();
                ISessionManager sm = TerminalEmulatorPlugin.Instance.GetSessionManager();
                IPoderosaPopupWindow window = wm.CreatePopupView(cp);
                sm.StartNewSession(session, window.InternalView);
                sm.ActivateDocument(session.Document, ActivateReason.InternalAction);
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }

        //起動時の初期化
        private static CommandResultViewerFactory _viewFactory;

        public static void Init(IPoderosaWorld world) {
            _viewFactory = new CommandResultViewerFactory();
            world.PluginManager.FindExtensionPoint(WindowManagerConstants.VIEW_FACTORY_ID).RegisterExtension(_viewFactory);
        }

    }

    //ViewClass
    internal class CommandResultViewerControl : CharacterDocumentViewer, IPoderosaView, IGeneralViewCommands {
        private readonly IPoderosaForm _form;
        private CommandResultSession _session;

        public CommandResultViewerControl(IPoderosaForm form) {
            _form = form;
            this.Caret.Enabled = false;
            this.Caret.Blink = false;
        }

        public void SetParent(CommandResultSession session) {
            _session = session;
            this.SetDocument(_session.Document);
        }

        #region CharacterDocumentViewer

        protected override void OnViewportSizeChanged() {
            // do nothing
        }

        protected override void OnCharacterDocumentChanged() {
            // do nothing
        }

        protected override RenderProfile GetCurrentRenderProfile() {
            if (_session != null) {
                return _session.RenderProfile ?? GEnv.DefaultRenderProfile;
            }

            return GEnv.DefaultRenderProfile;
        }

        protected override bool DetermineScrollable() {
            return true;
        }

        #endregion

        #region IPoderosaView

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

        #endregion

        #region IGeneralViewCommands

        //Command
        public IPoderosaCommand Copy {
            get {
                return TerminalEmulatorPlugin.Instance.GetWindowManager().SelectionService.DefaultCopyCommand;
            }
        }

        public IPoderosaCommand Paste {
            get {
                return null; //ペースト不可
            }
        }

        #endregion

        // Close by ESC
        protected override bool ProcessDialogKey(Keys keyData) {
            if (keyData == Keys.Escape) {
                this.FindForm().Close();
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }
    }

    //DocClass
    internal class CommandResultDocument : AppendOnlyCharacterDocument, IPoderosaDocument {

        public CommandResultDocument(string title) {
            this.Caption = title;
        }

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
            get;
            set;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return TerminalEmulatorPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public void AppendLines(IEnumerable<GLine> lines) {
            this.Append(lines);
        }
    }

    //ViewFactory
    internal class CommandResultViewerFactory : IViewFactory {
        public IPoderosaView CreateNew(IPoderosaForm parent) {
            return new CommandResultViewerControl(parent);
        }

        public Type GetViewType() {
            return typeof(CommandResultViewerControl);
        }

        public Type GetDocumentType() {
            return typeof(CommandResultDocument);
        }

        public IAdaptable GetAdapter(Type adapter) {
            return TerminalEmulatorPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }
}
