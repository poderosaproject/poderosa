/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SessionTestPlugin.cs,v 1.1 2010/11/19 15:41:20 kzmi Exp $
 */
#if TESTSESSION && MONOLITHIC
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Document;
using Poderosa.View;
using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Commands;
using Poderosa.Protocols;
using Poderosa.Terminal;

[assembly: PluginDeclaration(typeof(Poderosa.Sessions.SessionTestPlugin))]

namespace Poderosa.Sessions {

    internal class DummySession : ISession {
        private ISessionHost _host;
        private ViewBridge _view;
        private CharacterDocument _document;
        private string _caption;
        private Image _icon;

        private static int _count = 1;

        public DummySession() {
            _caption = String.Format("dummy {0}", _count);
            //_document = CharacterDocument.SingleLine(String.Format("document {0}", _count));
            _document = new CharacterDocument();
            _document.LoadForTest(@"C:\P4\doc\test\characterdocument.txt");

            _count++;
            _document.SetOwner(this);
            _icon = null;

            _document.Caption = _caption;
            
        }

        //IAdaptable
        public IAdaptable GetAdapter(Type adapter) {
            return TerminalSessionsPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }


        //ISession
        public string Caption {
            get {
                return _caption;
            }
        }
        public Image Icon {
            get {
                return _icon;
            }
        }
        public void InternalStart(ISessionHost host) {
            host.RegisterDocument(_document);
            _host = host;
        }
        public void InternalTerminate() {
        }
        public PrepareCloseResult PrepareCloseDocument(IPoderosaDocument document) {
            Debug.Assert(document==_document);

            //閉じるのをキャンセルするテスト
            IPoderosaForm f = _host.GetParentFormFor(document);
            DialogResult r = f.AskUserYesNo("Close?");
            return r==DialogResult.Yes? PrepareCloseResult.ContinueSession : PrepareCloseResult.Cancel;

            //return PrepareCloseResult.TerminateSession;
        }
        public PrepareCloseResult PrepareCloseSession() {
            //閉じるのをキャンセルするテスト
            IPoderosaForm f = _host.GetParentFormFor(_document);
            DialogResult r = f.AskUserYesNo("Close?");
            return r==DialogResult.Yes? PrepareCloseResult.TerminateSession : PrepareCloseResult.Cancel;

            //return PrepareCloseResult.TerminateSession;
        }

        public void InternalAttachView(IPoderosaDocument document, IPoderosaView view) {
            Debug.Assert(document==_document);
            ViewBridge viewbridge = (ViewBridge)view.GetAdapter(typeof(ViewBridge));
            _view = viewbridge;
            viewbridge.Attach(this, _document);
            Debug.WriteLine("Replace DUMMYSESSION");

            //苦しい条件だが、Ctrl+Shiftならキャプション変更をテスト
            Keys m = Control.ModifierKeys;
            if(m==(Keys.Control|Keys.Shift)) {
                _caption += "P";
                _document.Caption = _caption;
                SessionManagerPlugin.Instance.RefreshDocumentStatus(document);
            }
        }
        public void InternalDetachView(IPoderosaDocument document, IPoderosaView view) {
            Debug.Assert(document==_document);
            _view.Attach(this, null);
            _view = null;
        }
        public void InternalCloseDocument(IPoderosaDocument document) {
            //do nothing
        }

        public void NotifyViewsDataArrived() {
        }
        public void CloseByReceptionThread(string msg) {
            Debug.Assert(false, "unimplemented");
        }

        public IPoderosaDocument Document {
            get {
                return _document;
            }
        }

        public class ViewBridge : IPoderosaView, IContentReplaceableViewSite {
            private CharacterDocumentViewer _viewer;
            private DummySession _parent;
            private IContentReplaceableView _outer;

            public ViewBridge() {
                _viewer = new CharacterDocumentViewer(); //セッション固有コントロールとする
                _viewer.SetPrivateRenderProfile(TerminalEmulatorPlugin.Instance.TerminalEmulatorOptions.CreateRenderProfile());
            }

            public void Attach(DummySession parent, CharacterDocument doc) {
                _parent = parent;
                if(!_viewer.IsDisposed)
                    _viewer.SetContent(doc);
            }

            public IPoderosaDocument Document {
                get {
                    return _viewer.EnabledEx? _parent._document : null;
                }
            }

            public ISelection CurrentSelection {
                get {
                    return _viewer.TextSelection;
                }
            }

            public IPoderosaForm ParentForm {
                get {
                    return _viewer.FindForm() as IPoderosaForm;
                }
            }

            public Control AsControl() {
                return _viewer;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return SessionTestPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }

            public IContentReplaceableView CurrentContentReplaceableView {
                get {
                    return _outer;
                }
                set {
                    _outer = value;
                }
            }
        }
    }

    [PluginInfo(ID="org.poderosa.testsession", Version=VersionInfo.PODEROSA_VERSION, Author=VersionInfo.PROJECT_NAME, Dependencies="org.poderosa.core.window")]
    internal class SessionTestPlugin : PluginBase, IViewFormatEventHandler, IDocViewRelationEventHandler, IViewFactory {

        private static SessionTestPlugin _instance;
        public static SessionTestPlugin Instance {
            get {
                return _instance;
            }
        }

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            IExtensionPoint ep = poderosa.PluginManager.FindExtensionPoint("org.poderosa.menu.file");
            ep.RegisterExtension(new DummySessionMenuGroup());

            ICoreServices cs = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));

            poderosa.PluginManager.FindExtensionPoint(WindowManagerConstants.VIEWFORMATEVENTHANDLER_ID).RegisterExtension(this);
            poderosa.PluginManager.FindExtensionPoint("org.poderosa.core.sessions.docViewRelationHandler").RegisterExtension(this);
            poderosa.PluginManager.FindExtensionPoint(WindowManagerConstants.VIEW_FACTORY_ID).RegisterExtension(this);
        }

        private class DummySessionMenuGroup : IPoderosaMenuGroup, IPositionDesignation {
            public IPoderosaMenu[] ChildMenus {
                get {
                    return new IPoderosaMenu[] {
                        new PoderosaMenuItemImpl(new DummySessionCommand(), "Dummy Session"),
                        new PoderosaMenuItemImpl(new PoderosaCommandImpl(new ExecuteDelegate(LoopbackTerminalCommand)), "Loopback Terminal"),
                        new PoderosaMenuItemImpl(new PoderosaCommandImpl(new ExecuteDelegate(PromptRecogTestCommand)), "PromptRecog Test"),
                        new PoderosaMenuItemImpl(new PoderosaCommandImpl(new ExecuteDelegate(ApplySplitFormat.Test)), "Split Test")
                    };
                }
            }

            public bool IsVolatileContent {
                get {
                    return false;
                }
            }
            public bool ShowSeparator {
                get {
                    return true;
                }
            }
            public IAdaptable GetAdapter(Type adapter) {
                return _instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }

            public IAdaptable DesignationTarget {
                get {
                    return null;
                }
            }

            public PositionType DesignationPosition {
                get {
                    return PositionType.Last;
                }
            }
        }

        private class ApplySplitFormat {
            public static CommandResult Test(ICommandTarget target) {
                IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
                ISplittableViewManager sp = (ISplittableViewManager)window.ViewManager.GetAdapter(typeof(ISplittableViewManager));
                sp.ApplySplitInfo("V(33:Lterminal,33:Lterminal,L:Lterminal)");
                return CommandResult.Succeeded;
            }
        }


        private class DummySessionCommand : IPoderosaCommand {
            public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
                ISessionManager sm = (ISessionManager)SessionTestPlugin.Instance.PoderosaWorld.PluginManager.FindPlugin("org.poderosa.core.sessions", typeof(ISessionManager));
                DummySession ts = new DummySession();
                IViewManager vm = WindowManagerPlugin.Instance.ActiveWindow.ViewManager;
                sm.StartNewSession(ts, vm.GetCandidateViewForNewDocument());
                sm.ActivateDocument(ts.Document, ActivateReason.InternalAction);

                return CommandResult.Succeeded;
            }
            public bool CanExecute(ICommandTarget target) {
                return true;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return SessionTestPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        private static CommandResult LoopbackTerminalCommand(ICommandTarget target) {
            IPluginManager pm = SessionTestPlugin.Instance.PoderosaWorld.PluginManager;
            IProtocolTestService ps = (IProtocolTestService)pm.FindPlugin("org.poderosa.protocols", typeof(IProtocolTestService));
            ITerminalSessionStartCommand s = ((ITerminalSessionsService)pm.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService))).TerminalSessionStartCommand;
            ITerminalEmulatorService es = (ITerminalEmulatorService)pm.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService));
            ITerminalSettings ts = es.CreateDefaultTerminalSettings("LOOPBACK", null);
            //改行はCRLFがいいっすね
            ts.BeginUpdate();
            ts.TransmitNL = Poderosa.ConnectionParam.NewLine.CRLF;
            ts.EndUpdate();
            s.StartTerminalSession(target, ps.CreateLoopbackConnection(), ts);
            return CommandResult.Succeeded;
        }
        private static CommandResult PromptRecogTestCommand(ICommandTarget target) {
            ITerminalSession ts = (ITerminalSession)CommandTargetUtil.AsDocumentOrViewOrLastActivatedDocument(target).OwnerSession.GetAdapter(typeof(ITerminalSession));
            Debug.Assert(ts!=null);

            new PromptRecognitionTest().Start(ts);
            return CommandResult.Succeeded;
        }


        #region IViewFormatChangeEventHandler
        public void OnSplit(ISplittableViewManager viewmanager) {
            Debug.WriteLine("SESSIONTEST " + viewmanager.FormatSplitInfo());
        }

        public void OnUnify(ISplittableViewManager viewmanager) {
            Debug.WriteLine("SESSIONTEST " + viewmanager.FormatSplitInfo());
        }
        #endregion

        #region IDocViewRelationChangeHanlder
        public void OnDocViewRelationChange() {
#if false
            foreach(DocumentHost dh in SessionManagerPlugin.Instance.GetAllDocumentHosts()) {
                IPoderosaView v = dh.CurrentView;
                IPoderosaView l = dh.LastAttachedView;
                Debug.WriteLine(String.Format("Attach doc={0} view={1} last={2}",
                    dh.Document.Caption,
                    v==null? "null" : ((CharacterDocumentViewer)v.AsControl()).InstanceID,
                    ((Poderosa.Terminal.TerminalControl)l.AsControl()).InstanceID));
            }
            Debug.WriteLine("END DUMP");
#endif
        }

        #endregion

        #region IViewFactory
        public IPoderosaView CreateNew(IPoderosaForm parent) {
            return new DummySession.ViewBridge();
        }

        public Type GetViewType() {
            return typeof(DummySession.ViewBridge);
        }
        public Type GetDocumentType() {
            return typeof(CharacterDocument);
        }
        #endregion
    }
}
#endif
