/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PortForwardingCommandPlugin.cs,v 1.3 2011/12/23 10:45:49 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

using Poderosa.Plugins;
using Poderosa.Sessions;
using Poderosa.Util;
using Poderosa.Commands;
using Poderosa.Forms;

[assembly: PluginDeclaration(typeof(Poderosa.PortForwardingCommand.PortForwardingCommandPlugin))]

namespace Poderosa.PortForwardingCommand {
    [PluginInfo(ID = "org.poderosa.portforwarding", Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.window")]
    internal class PortForwardingCommandPlugin : PluginBase {
        private static PortForwardingCommandPlugin _instance;
        private StringResource _strings;
        private ICoreServices _coreServices;
        private ExecPortForwardingCommand _execPortForwardingCommand;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            _strings = new StringResource("Poderosa.PortForwardingCommand.strings", typeof(PortForwardingCommandPlugin).Assembly);
            poderosa.Culture.AddChangeListener(_strings);

            IPluginManager pm = poderosa.PluginManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));

            _execPortForwardingCommand = new ExecPortForwardingCommand();
            _coreServices.CommandManager.Register(_execPortForwardingCommand);
            IExtensionPoint toolmenu = pm.FindExtensionPoint("org.poderosa.menu.tool");
            toolmenu.RegisterExtension(new PoderosaMenuGroupImpl(
                new IPoderosaMenu[] { new PoderosaMenuItemImpl(_execPortForwardingCommand, _strings, "Menu.PortForwarding") }, false));
        }
        public static PortForwardingCommandPlugin Instance {
            get {
                return _instance;
            }
        }

        public StringResource Strings {
            get {
                return _strings;
            }
        }
        public ExecPortForwardingCommand ExecPortForwardingCommand {
            get {
                return _execPortForwardingCommand;
            }
        }
        public ICommandManager CommandManager {
            get {
                return _coreServices.CommandManager;
            }
        }
    }

    internal class ExecPortForwardingCommand : GeneralCommandImpl {
        public ExecPortForwardingCommand()
            : base("org.poderosa.portforwarding.exectool", PortForwardingCommandPlugin.Instance.Strings, "Command.PortForwarding", PortForwardingCommandPlugin.Instance.CommandManager.CommandCategories.Dialogs) {
        }


        public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            try {
                string path = GetPortForwardingExePath();

                if (path == null) {
                    string message = PortForwardingCommandPlugin.Instance.Strings.GetString("Command.PortForwardingExeNotFound");
                    MessageBox.Show(message, "Poderosa", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return CommandResult.Failed;
                }
                
                Process.Start(path);
                return CommandResult.Succeeded;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                return CommandResult.Failed;
            }
        }

        private static string GetPortForwardingExePath() {
            const string PORTFORWARDING_DIR = "PortForwarding";
            const string PORTFORWARDING_EXE = "portforwarding.exe";

            IPoderosaApplication app = (IPoderosaApplication)PortForwardingCommandPlugin.Instance.PoderosaWorld.GetAdapter(typeof(IPoderosaApplication));

            // 1st candidate: <HomeDirectory>/PortForwarding/portforwarding.exe
            // Previous default for release version
            string path = Path.Combine(Path.Combine(app.HomeDirectory, PORTFORWARDING_DIR), PORTFORWARDING_EXE);
            if (File.Exists(path))
                return path;

            // 2nd candidate: <HomeDirectory>/portforwarding.exe
            // Previous default for monolithic version
            path = Path.Combine(app.HomeDirectory, PORTFORWARDING_EXE);
            if (File.Exists(path))
                return path;

            return null;
        }
    
    }

#if false
    internal class PortForwardingMenuGroup : IPoderosaMenuGroup {
        public IPoderosaMenu[] ChildMenus {
            get {
                return new IPoderosaMenu[] { new PortForwardingMenu() };
            }
        }

        public bool IsVolatileContent {
            get {
                return false;
            }
        }

        public bool ShowSeparator {
            get {
                return false;
            }
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PortForwardingCommandPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

    }

    internal class PortForwardingMenu : IPoderosaMenuItem {
        public IPoderosaCommand AssociatedCommand {
            get {
                return PortForwardingCommandPlugin.Instance.ExecPortForwardingCommand;
            }
        }

        public string Text {
            get {
                return PortForwardingCommandPlugin.Instance.Strings.GetString("Menu.PortForwarding");
            }
        }

        public bool IsEnabled(ICommandTarget target) {
            return PortForwardingCommandPlugin.Instance.ExecPortForwardingCommand.CanExecute(target);
        }

        public bool IsChecked(ICommandTarget target) {
            return false;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PortForwardingCommandPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }
#endif
}
