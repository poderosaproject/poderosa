/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SSHUtilPlugin.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa.Plugins;
using Poderosa.Commands;
using Poderosa.Forms;
using Poderosa.Protocols;
[assembly: PluginDeclaration(typeof(Poderosa.Usability.SSHUtilPlugin))]

namespace Poderosa.Usability {
    [PluginInfo(ID = "org.poderosa.sshutil", Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.window;org.poderosa.protocols")]
    internal class SSHUtilPlugin : PluginBase {

        private static SSHUtilPlugin _instance;

        private ICommandManager _commandManager;
        private KeyAgent _keyAgent;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;

            IPluginManager pm = poderosa.PluginManager;
            ICoreServices cs = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _commandManager = cs.CommandManager;
            Debug.Assert(_commandManager != null);
            SSHUtilCommand.Register(_commandManager);

            SSHUtilMenuGroup sshmenu = new SSHUtilMenuGroup();
            IExtensionPoint toolmenu = pm.FindExtensionPoint("org.poderosa.menu.tool");
            toolmenu.RegisterExtension(sshmenu);

            _keyAgent = new KeyAgent();
            cs.PreferenceExtensionPoint.RegisterExtension(_keyAgent);
            pm.FindExtensionPoint(ProtocolsPluginConstants.RESULTEVENTHANDLER_EXTENSION).RegisterExtension(_keyAgent);
        }
        public StringResource Strings {
            get {
                return UsabilityPlugin.Strings; //共用
            }
        }
        public ICommandManager CommandManager {
            get {
                return _commandManager;
            }
        }
        public KeyAgent KeyAgent {
            get {
                return _keyAgent;
            }
        }
        public static SSHUtilPlugin Instance {
            get {
                return _instance;
            }
        }
    }

    //ポジションはdontcareでいいや
    internal class SSHUtilCommandCategory : ICommandCategory {
        public string Name {
            get {
                return SSHUtilPlugin.Instance.Strings.GetString("CommandCategory.SSH");
            }
        }

        public bool IsKeybindCustomizable {
            get {
                return true;
            }
        }

        public IAdaptable GetAdapter(Type adapter) {
            return SSHUtilPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public static SSHUtilCommandCategory _instance = new SSHUtilCommandCategory(); //これ一個で十分
    }

    internal class SSHUtilCommand : IGeneralCommand {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <exclude/>
        public delegate CommandResult ExecuteDelegate(ICommandTarget target);

        private string _id;
        private string _description;
        private Keys _defaultKey;
        private ExecuteDelegate _body;

        public SSHUtilCommand(string id, string description, ExecuteDelegate body) {
            _id = id;
            _body = body;
            _description = description;
            _defaultKey = Keys.None;
        }

        #region IGeneralCommand
        public string CommandID {
            get {
                return _id;
            }
        }
        public string Description {
            get {
                return SSHUtilPlugin.Instance.Strings.GetString(_description);
            }
        }
        public Keys DefaultShortcutKey {
            get {
                return _defaultKey;
            }
        }
        public ICommandCategory CommandCategory {
            get {
                return SSHUtilCommandCategory._instance;
            }
        }
        public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            Debug.Assert(args == null || args.Length == 0);
            return _body(target);
        }
        public bool CanExecute(ICommandTarget target) {
            return true;
        }
        #endregion

        #region IAdaptable
        public IAdaptable GetAdapter(Type adapter) {
            return SSHUtilPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
        #endregion

        public static void Register(ICommandManager cm) {
            cm.Register(new SSHUtilCommand("org.poderosa.sshutil.generatekeypair", "Command.GenerateKeyPair", new ExecuteDelegate(CmdGenerateKeyPair)));
            cm.Register(new SSHUtilCommand("org.poderosa.sshutil.changepassphrase", "Command.ChangePassphrase", new ExecuteDelegate(CmdChangePassphrase)));
            cm.Register(new SSHUtilCommand("org.poderosa.sshutil.agentkeylistdialog", "Command.AgentKeyListDialog", new ExecuteDelegate(CmdAgentKeyListDialog)));
        }

        private static CommandResult CmdGenerateKeyPair(ICommandTarget target) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            Debug.Assert(window != null);

            KeyGenWizard dlg = new KeyGenWizard();
            DialogResult r = dlg.ShowDialog(window.AsForm());
            return r == DialogResult.OK ? CommandResult.Succeeded : CommandResult.Cancelled;
        }
        private static CommandResult CmdChangePassphrase(ICommandTarget target) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            Debug.Assert(window != null);

            ChangePassphrase dlg = new ChangePassphrase();
            DialogResult r = dlg.ShowDialog(window.AsForm());
            return r == DialogResult.OK ? CommandResult.Succeeded : CommandResult.Cancelled;
        }
        private static CommandResult CmdAgentKeyListDialog(ICommandTarget target) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            Debug.Assert(window != null);

            AgentKeyListDialog dlg = new AgentKeyListDialog();
            DialogResult r = dlg.ShowDialog(window.AsForm());
            return r == DialogResult.OK ? CommandResult.Succeeded : CommandResult.Cancelled;
        }
    }

    internal class SSHUtilMenuItem : IPoderosaMenuItem {
        private string _textID;
        private IGeneralCommand _command;

        public SSHUtilMenuItem(string textID, string commandID) {
            Init(textID, commandID);
        }
        private void Init(string textID, string commandID) {
            _textID = textID;
            _command = SSHUtilPlugin.Instance.CommandManager.Find(commandID);
            Debug.Assert(_command != null);
        }
        public IPoderosaCommand AssociatedCommand {
            get {
                return _command;
            }
        }
        public string Text {
            get {
                return SSHUtilPlugin.Instance.Strings.GetString(_textID);
            }
        }
        public bool IsEnabled(ICommandTarget target) {
            return _command.CanExecute(target);
        }
        public bool IsChecked(ICommandTarget target) {
            return false;
        }
        public IAdaptable GetAdapter(Type adapter) {
            return SSHUtilPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }

    internal class SSHUtilMenuGroup : IPoderosaMenuGroup, IPositionDesignation {
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
            return SSHUtilPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public IAdaptable DesignationTarget {
            get {
                return null;
            }
        }

        public PositionType DesignationPosition {
            get {
                return PositionType.First;
            }
        }
        public IPoderosaMenu[] ChildMenus {
            get {
                return new IPoderosaMenu[] {
                    new SSHUtilMenuItem("Menu.GenerateKeyPair", "org.poderosa.sshutil.generatekeypair"),
                    new SSHUtilMenuItem("Menu.ChangePassphrase", "org.poderosa.sshutil.changepassphrase"),
                    new SSHUtilMenuItem("Menu.AgentKeyListDialog", "org.poderosa.sshutil.agentkeylistdialog")
                };
            }
        }
    }
}
