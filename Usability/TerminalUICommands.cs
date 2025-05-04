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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

using Poderosa.Terminal;
using Poderosa.Commands;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.Forms;

namespace Poderosa.Usability {
    internal class TerminalUICommand : GeneralCommandImpl {

        public TerminalUICommand(string id, string description, ExecuteDelegate body, CanExecuteDelegate enabled)
            :
            base(id, TerminalUIPlugin.Instance.Strings, description, TerminalUIPlugin.Instance.TerminalEmulatorPlugin.TerminalCommandCategory, body, enabled) {
        }
        public TerminalUICommand(string id, string description, ExecuteDelegate body)
            :
            base(id, TerminalUIPlugin.Instance.Strings, description, TerminalUIPlugin.Instance.TerminalEmulatorPlugin.TerminalCommandCategory, body) {
        }

        public static void Register(ICommandManager cm) {
            CanExecuteDelegate does_open_target_session = new CanExecuteDelegate(DoesOpenTargetSession);

            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.editrenderprofile", "Command.EditRenderProfile", new ExecuteDelegate(CmdEditRenderProfile), does_open_target_session));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.renametab", "Command.RenameTab", new ExecuteDelegate(CmdRenameTab), does_open_target_session));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.commentlog", "Command.CommentLog", new ExecuteDelegate(CmdCommentLog), does_open_target_session));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.changelog", "Command.ChangeLog", new ExecuteDelegate(CmdChangeLog), does_open_target_session));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.shellSchemeEditor", "Command.ShellSchemeEditor", new ExecuteDelegate(CmdShellSchemeEditor)));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.splithorizontal", "Command.SplitHorizontal", new ExecuteDelegate(CmdSplitHorizontal), new CanExecuteDelegate(CanSplit)));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.splitvertical", "Command.SplitVertical", new ExecuteDelegate(CmdSplitVertical), new CanExecuteDelegate(CanSplit)));
            cm.Register(new TerminalUICommand("org.poderosa.terminalemulator.splitunify", "Command.SplitUnify", new ExecuteDelegate(CmdSplitUnify), new CanExecuteDelegate(CanSplitUnify)));
        }
        private static CommandResult CmdEditRenderProfile(ICommandTarget target) {
            ITerminalSession s = AsTerminalSession(target);
            if (s == null)
                return CommandResult.Ignored;

            EditRenderProfile dlg = new EditRenderProfile(s.TerminalSettings.RenderProfile);
            if (dlg.ShowDialog(s.OwnerWindow.AsForm()) == DialogResult.OK) {
                s.TerminalSettings.BeginUpdate();
                s.TerminalSettings.RenderProfile = dlg.Result;
                s.TerminalSettings.EndUpdate();
                return CommandResult.Succeeded;
            }
            else
                return CommandResult.Cancelled;
        }
        private static CommandResult CmdRenameTab(ICommandTarget target) {
            ITerminalSession s = AsTerminalSession(target);
            if (s == null)
                return CommandResult.Ignored;
            RenameTabBox dlg = new RenameTabBox();
            dlg.Content = s.TerminalSettings.Caption;
            if (dlg.ShowDialog(s.OwnerWindow.AsForm()) == DialogResult.OK) {
                s.TerminalSettings.BeginUpdate();
                s.TerminalSettings.Caption = dlg.Content;
                s.TerminalSettings.EndUpdate();
                return CommandResult.Succeeded;
            }
            else
                return CommandResult.Cancelled;
        }
        private static CommandResult CmdCommentLog(ICommandTarget target) {
            ITerminalSession s = AsTerminalSession(target);
            if (s == null)
                return CommandResult.Ignored;
            CommentLog dlg = new CommentLog(s);
            if (dlg.ShowDialog(s.OwnerWindow.AsForm()) == DialogResult.OK) {
                return CommandResult.Succeeded;
            }
            else
                return CommandResult.Cancelled;
        }
        private static CommandResult CmdChangeLog(ICommandTarget target) {
            ITerminalSession s = AsTerminalSession(target);
            if (s == null)
                return CommandResult.Ignored;
            ChangeLog dlg = new ChangeLog(s);
            if (dlg.ShowDialog(s.OwnerWindow.AsForm()) == DialogResult.OK) {
                return CommandResult.Succeeded;
            }
            else
                return CommandResult.Cancelled;
        }
        private static CommandResult CmdShellSchemeEditor(ICommandTarget target) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            ITerminalControlHost session = TerminalCommandTarget.AsTerminal(target);
            ShellSchemeEditor dlg = new ShellSchemeEditor(session == null ? null : session.TerminalSettings.ShellScheme);
            dlg.ShowDialog(window.AsForm());
            return CommandResult.Succeeded;
        }

        private static bool CanSplit(ICommandTarget target) {
            IContentReplaceableView view = GetContentReplaceableView(target);
            if (view != null) {
                return GetSplittableViewManager(view).CanSplit(view);
            }
            return false;
        }

        private static CommandResult CmdSplitHorizontal(ICommandTarget target) {
            if (CanSplit(target)) {
                IContentReplaceableView view = GetContentReplaceableView(target);
                if (view != null) {
                    return GetSplittableViewManager(view).SplitHorizontal(view, null);
                }
            }
            return CommandResult.Ignored;
        }

        private static CommandResult CmdSplitVertical(ICommandTarget target) {
            if (CanSplit(target)) {
                IContentReplaceableView view = GetContentReplaceableView(target);
                if (view != null) {
                    return GetSplittableViewManager(view).SplitVertical(view, null);
                }
            }
            return CommandResult.Ignored;
        }

        private static bool CanSplitUnify(ICommandTarget target) {
            IContentReplaceableView view = GetContentReplaceableView(target);
            if (view != null) {
                return GetSplittableViewManager(view).CanUnify(view);
            }
            return false;
        }

        private static CommandResult CmdSplitUnify(ICommandTarget target) {
            if (CanSplitUnify(target)) {
                IContentReplaceableView view = GetContentReplaceableView(target);
                if (view != null) {
                    IContentReplaceableView nextView;
                    return GetSplittableViewManager(view).Unify(view, out nextView);
                }
            }
            return CommandResult.Ignored;
        }

        private static IContentReplaceableView GetContentReplaceableView(ICommandTarget target) {
            IContentReplaceableView view = (IContentReplaceableView)target.GetAdapter(typeof(IContentReplaceableView));
            if (view != null) {
                return view;
            }

            IContentReplaceableViewSite site = (IContentReplaceableViewSite)target.GetAdapter(typeof(IContentReplaceableViewSite));
            if (site != null) {
                return site.CurrentContentReplaceableView;
            }

            ITerminalSession session = AsTerminalSession(target);
            if (session != null) {
                TerminalControl control = session.TerminalControl;
                if (control != null) {
                    site = (IContentReplaceableViewSite)control.GetAdapter(typeof(IContentReplaceableViewSite));
                    if (site != null) {
                        return site.CurrentContentReplaceableView;
                    }
                }
            }
            return null;
        }

        private static ISplittableViewManager GetSplittableViewManager(IContentReplaceableView view) {
            return (ISplittableViewManager)view.ViewManager.GetAdapter(typeof(ISplittableViewManager));
        }

        //CommandTargetからTerminalSessionを得る
        public static ITerminalSession AsTerminalSession(ICommandTarget target) {
            IPoderosaDocument document = CommandTargetUtil.AsDocumentOrViewOrLastActivatedDocument(target);
            if (document == null)
                return null;
            else {
                ISession session = document.OwnerSession;
                return (ITerminalSession)session.GetAdapter(typeof(ITerminalSession));
            }
        }
        public static ITerminalSession AsOpenTerminalSession(ICommandTarget target) {
            ITerminalSession s = AsTerminalSession(target);
            return (s == null || s.TerminalTransmission.Connection.IsClosed) ? null : s;
        }
        public static EnabledDelegate DoesOpenTargetSession {
            get {
                return delegate(ICommandTarget target) {
                    ITerminalSession s = AsTerminalSession(target);
                    return s != null && !s.TerminalTransmission.Connection.IsClosed;
                };
            }
        }
    }

    internal class StandardTerminalUIMenuItem : PoderosaMenuItemImpl {

        public StandardTerminalUIMenuItem(string textID, string commandID)
            : base(commandID, TerminalUIPlugin.Instance.Strings, textID) {
        }
        public StandardTerminalUIMenuItem(string textID, string commandID, CheckedDelegate cd)
            : base(commandID, TerminalUIPlugin.Instance.Strings, textID) {
            _checked = cd;
        }
    }

    internal abstract class StandardTerminalUIMenuGroup : PoderosaMenuGroupImpl {
        public StandardTerminalUIMenuGroup() {
            _designationTarget = null;
            _positionType = PositionType.First;
        }

    }

    internal class StandardTerminalUIMenuFolder : IPoderosaMenuFolder {
        private string _textID;
        private EnabledDelegate _enabled;
        private IPoderosaMenuGroup[] _children;

        public StandardTerminalUIMenuFolder(string textID, EnabledDelegate enabled, params IPoderosaMenuGroup[] groups) {
            _textID = textID;
            _enabled = enabled;
            _children = groups;
        }

        public IPoderosaMenuGroup[] ChildGroups {
            get {
                return _children;
            }
        }

        public string Text {
            get {
                return UsabilityPlugin.Strings.GetString(_textID);
            }
        }

        public bool IsEnabled(ICommandTarget target) {
            return _enabled(target);
        }

        public bool IsChecked(ICommandTarget target) {
            return false;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return UsabilityPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }


    internal class TerminalUIMenuGroup : StandardTerminalUIMenuGroup {
        public override IPoderosaMenu[] ChildMenus {
            get {
                return new IPoderosaMenu[] {
                    new StandardTerminalUIMenuItem("Menu.DivideFrameHorizontal", "org.poderosa.terminalemulator.splithorizontal"),
                    new StandardTerminalUIMenuItem("Menu.DivideFrameVertical", "org.poderosa.terminalemulator.splitvertical"),
                    new StandardTerminalUIMenuItem("Menu.UnifyFrame", "org.poderosa.terminalemulator.splitunify"),
                    new StandardTerminalUIMenuItem("Menu.EditRenderProfile", "org.poderosa.terminalemulator.editrenderprofile"),
                    new StandardTerminalUIMenuItem("Menu.RenameTab", "org.poderosa.terminalemulator.renametab"),
                    new TerminalUICopyRemoteMenuFolder("Menu.CopyRemote"),
                };
            }
        }

        public override PositionType DesignationPosition {
            get {
                return PositionType.Last;
            }
        }
    }

    internal class TerminalUICopyRemoteMenuFolder : StandardTerminalUIMenuFolder {

        private class CopyRemoteGroup : StandardTerminalUIMenuGroup {
            private IPoderosaMenu[] _children = new IPoderosaMenu[0];

            public bool DetermineEnabled(ICommandTarget target) {
                ITerminalSession session = TerminalUICommand.AsTerminalSession(target);
                if (session != null) {
                    if (session.TerminalConnection != null) {
                        IPoderosaSocket socket = session.TerminalConnection.Socket;
                        if (socket != null) {
                            _children = BuildMenuItems(socket);
                            return _children.Length > 0;
                        }
                    }
                }
                _children = new IPoderosaMenu[0];
                return false;
            }

            private IPoderosaMenu[] BuildMenuItems(IPoderosaSocket socket) {
                List<string> items = new List<string>(3);

                if (!String.IsNullOrEmpty(socket.Remote)) {
                    items.Add(socket.Remote);
                }

                IPoderosaSocketInet socketInet = socket as IPoderosaSocketInet;
                if (socketInet != null) {
                    if (socketInet.RemoteAddress != null) {
                        bool isIPv6 = socketInet.RemoteAddress.AddressFamily == AddressFamily.InterNetworkV6;
                        string addr = socketInet.RemoteAddress.ToString();
                        if (addr != socket.Remote) {
                            items.Add(addr);
                        }
                        if (socketInet.RemotePortNumber.HasValue) {
                            string port = socketInet.RemotePortNumber.Value.ToString(NumberFormatInfo.InvariantInfo);
                            string addrAndPort;
                            if (isIPv6) {
                                addrAndPort = "[" + addr + "]:" + port;
                            }
                            else {
                                addrAndPort = addr + ":" + port;
                            }

                            if (addrAndPort != socket.Remote) {
                                items.Add(addrAndPort);
                            }
                        }
                    }
                }

                return items
                    .Select(item => new CopyRemoteMenuItem(item))
                    .ToArray();
            }

            public override IPoderosaMenu[] ChildMenus {
                get {
                    return _children;
                }
            }
        }

        private class CopyRemoteMenuItem : IPoderosaMenuItem, IPoderosaCommand {

            private readonly string _text;

            public CopyRemoteMenuItem(string text) {
                _text = text;
            }

            public IPoderosaCommand AssociatedCommand {
                get {
                    return this;
                }
            }

            public string Text {
                get {
                    return _text;
                }
            }

            public bool IsEnabled(ICommandTarget target) {
                return true;
            }

            public bool IsChecked(ICommandTarget target) {
                return false;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return UsabilityPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }

            public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
                try {
                    Clipboard.SetDataObject(_text, true);
                    return CommandResult.Succeeded;
                }
                catch (Exception ex) {
                    RuntimeUtil.ReportException(ex);
                    return CommandResult.Failed;
                }
            }

            public bool CanExecute(ICommandTarget target) {
                return true;
            }
        }

        private readonly CopyRemoteGroup _group = new CopyRemoteGroup();

        public TerminalUICopyRemoteMenuFolder(string textID)
            : this(textID, new CopyRemoteGroup()) {
        }

        private TerminalUICopyRemoteMenuFolder(string textID, CopyRemoteGroup group)
            : base(textID, group.DetermineEnabled, group) {
        }
    }

    internal class LogMenuGroup : StandardTerminalUIMenuGroup {
        public override IPoderosaMenu[] ChildMenus {
            get {
                return new IPoderosaMenu[] {
                    new StandardTerminalUIMenuItem("Menu.CommentLog", "org.poderosa.terminalemulator.commentlog"),
                    new StandardTerminalUIMenuItem("Menu.ChangeLog", "org.poderosa.terminalemulator.changelog")
                };
            }
        }
        public override PositionType DesignationPosition {
            get {
                return PositionType.Last;
            }
        }
    }

    internal class ShellSchemeEditMenuGroup : PoderosaMenuGroupImpl {
        public ShellSchemeEditMenuGroup()
            : base(new PoderosaMenuItemImpl("org.poderosa.terminalemulator.shellSchemeEditor", TerminalUIPlugin.Instance.Strings, "Menu.ShellSchemeEditor")) {
            _positionType = PositionType.PreviousTo;
            _designationTarget = OptionDialogPlugin.Instance.OptionDialogMenuGroup;
            _showSeparator = true;
        }
    }

}
