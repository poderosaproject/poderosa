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
using System.Windows.Forms;

using Poderosa.Terminal;
using Poderosa.Commands;
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
            ITerminalSession session = AsTerminalSession(target);
            if (session != null) {
                TerminalControl control = session.TerminalControl;
                if (control != null) {
                    IContentReplaceableViewSite site = (IContentReplaceableViewSite)control.GetAdapter(typeof(IContentReplaceableViewSite));
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


    internal class TerminalUIMenuGroup : StandardTerminalUIMenuGroup {
        public override IPoderosaMenu[] ChildMenus {
            get {
                return new IPoderosaMenu[] {
                    new StandardTerminalUIMenuItem("Menu.DivideFrameHorizontal", "org.poderosa.terminalemulator.splithorizontal"),
                    new StandardTerminalUIMenuItem("Menu.DivideFrameVertical", "org.poderosa.terminalemulator.splitvertical"),
                    new StandardTerminalUIMenuItem("Menu.UnifyFrame", "org.poderosa.terminalemulator.splitunify"),
                    new StandardTerminalUIMenuItem("Menu.EditRenderProfile", "org.poderosa.terminalemulator.editrenderprofile"),
                    new StandardTerminalUIMenuItem("Menu.RenameTab", "org.poderosa.terminalemulator.renametab")
                };
            }
        }
        public override PositionType DesignationPosition {
            get {
                return PositionType.Last;
            }
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
