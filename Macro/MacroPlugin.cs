/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MacroPlugin.cs,v 1.3 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;

using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Terminal;
using Poderosa.Commands;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.MacroEngine;

[assembly: PluginDeclaration(typeof(Poderosa.MacroInternal.MacroPlugin))]

namespace Poderosa.MacroInternal {
    [PluginInfo(ID = MacroPlugin.PLUGIN_ID, Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.sessions;org.poderosa.terminalsessions")]
    internal class MacroPlugin : PluginBase, IMacroEngine {
        public const string PLUGIN_ID = "org.poderosa.macro";
        private static MacroPlugin _instance;

        private MacroListCommand _macroListCommand;
        private MacroManager _macroManager;
        private ICoreServices _coreServices;
        private StringResource _stringResource;

        private readonly SessionBinder _sessionBinder = new SessionBinder();

        public static MacroPlugin Instance {
            get {
                return _instance;
            }
        }

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            _stringResource = new StringResource("Poderosa.Macro.strings", typeof(MacroPlugin).Assembly);
            _instance.PoderosaWorld.Culture.AddChangeListener(_stringResource);

            IPluginManager pm = poderosa.PluginManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _macroManager = new MacroManager();

            _macroListCommand = new MacroListCommand();
            _coreServices.CommandManager.Register(_macroListCommand);

            pm.FindExtensionPoint("org.poderosa.menu.tool").RegisterExtension(new MacroMenuGroup());

            _coreServices.PreferenceExtensionPoint.RegisterExtension(_macroManager);

            ISessionManager sessionManager = poderosa.PluginManager.FindPlugin("org.poderosa.core.sessions", typeof(ISessionManager)) as ISessionManager;
            if (sessionManager != null) {
                sessionManager.AddSessionListener(_sessionBinder);
            }
        }
        public override void TerminatePlugin() {
            base.TerminatePlugin();
            _macroManager.SaveToPreference();
        }

        public MacroListCommand MacroListCommand {
            get {
                return _macroListCommand;
            }
        }

        public MacroManager MacroManager {
            get {
                return _macroManager;
            }
        }

        public StringResource Strings {
            get {
                return _stringResource;
            }
        }

        public IPoderosaApplication PoderosaApplication {
            get {
                return (IPoderosaApplication)_poderosaWorld.GetAdapter(typeof(IPoderosaApplication));
            }
        }

        public ISessionManager SessionManager {
            get {
                return _coreServices.SessionManager;
            }
        }
        public IWindowManager WindowManager {
            get {
                return _coreServices.WindowManager;
            }
        }
        public ICommandManager CommandManager {
            get {
                return _coreServices.CommandManager;
            }
        }
        public ITerminalEmulatorService TerminalEmulatorService {
            get {
                return (ITerminalEmulatorService)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService));
            }
        }
        public IProtocolService ProtocolService {
            get {
                return (IProtocolService)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.protocols", typeof(IProtocolService));
            }
        }
        public ITerminalSessionsService TerminalSessionsService {
            get {
                return (ITerminalSessionsService)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService));
            }
        }

        #region IMacroEngine

        public void RunMacro(string path, ISession session) {
            if (path != null && File.Exists(path)) {
                string name = Path.GetFileNameWithoutExtension(path);
                MacroModule module = new MacroModule(0, path, name);
                RunMacroModule(module, session, null);
            }
        }

        public string SelectMacro(Form owner) {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;
            dlg.Title = Strings.GetString("Caption.ModuleProperty.SelectMacroFile");
            dlg.Filter = "JScript.NET Files(*.js)|*.js|.NET Executables(*.exe;*.dll)|*.exe;*.dll";
            if (dlg.ShowDialog(owner) == DialogResult.OK)
                return dlg.FileName;
            else
                return null;
        }

        #endregion

        internal MacroExecutor RunMacroModule(MacroModule module, ISession sessionToBind, IMacroEventListener listener) {
            try {
                Assembly asm = MacroUtil.LoadMacroAssembly(module);
                MacroExecutor macroExec = new MacroExecutor(module, asm);

                if (sessionToBind != null) {
                    bool bound = _sessionBinder.Bind(macroExec, sessionToBind);
                    if (!bound) {
                        GUtil.Warning(null, Strings.GetString("Message.MacroPlugin.AnotherMacroIsRunningInThisSession"));
                        return null;
                    }
                }

                if (listener != null)
                    listener.IndicateMacroStarted();
                macroExec.AsyncExec(listener);
                return macroExec;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                return null;
            }
        }
    }

    internal class MacroListCommand : GeneralCommandImpl {
        public MacroListCommand()
            : base("org.poderosa.macro.showMacroList", MacroPlugin.Instance.Strings, "Command.MacroList", MacroPlugin.Instance.CommandManager.CommandCategories.Dialogs) {
        }

        public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            MacroList dlg = new MacroList();
            dlg.ShowDialog(window.AsForm());
            return CommandResult.Succeeded;
        }
    }

    internal class MacroMenuGroup : PoderosaMenuGroupImpl {
        public MacroMenuGroup()
            : base(new MacroTopMenu()) {
        }

        private class MacroTopMenu : IPoderosaMenuFolder {
            private IPoderosaMenuGroup[] _childGroups;
            public MacroTopMenu() {
                _childGroups = new IPoderosaMenuGroup[] {
                    new PoderosaMenuGroupImpl(new PoderosaMenuItemImpl(MacroPlugin.Instance.MacroListCommand, MacroPlugin.Instance.Strings, "Menu.MacroList")),
                    new MacroItemsMenuGroup()
                };
            }

            public IPoderosaMenuGroup[] ChildGroups {
                get {
                    return _childGroups;
                }
            }

            public string Text {
                get {
                    return MacroPlugin.Instance.Strings.GetString("Menu.Macro");
                }
            }

            public bool IsEnabled(ICommandTarget target) {
                return true;
            }

            public bool IsChecked(ICommandTarget target) {
                return false;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return MacroPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        //個々のマクロのアイテムを記述
        private class MacroItemsMenuGroup : IPoderosaMenuGroup {
            public IPoderosaMenu[] ChildMenus {
                get {
                    List<MacroExecMenu> t = new List<MacroExecMenu>();
                    foreach (MacroModule m in MacroPlugin.Instance.MacroManager.Modules)
                        t.Add(new MacroExecMenu(new MacroExecCommand(m)));
                    return t.ToArray();
                }
            }

            public bool IsVolatileContent {
                get {
                    return true;
                }
            }

            public bool ShowSeparator {
                get {
                    return true;
                }
            }

            public IAdaptable GetAdapter(Type adapter) {
                return MacroPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        private class MacroExecCommand : IPoderosaCommand {
            private MacroModule _module;
            public MacroExecCommand(MacroModule module) {
                _module = module;
            }

            public MacroModule Module {
                get {
                    return _module;
                }
            }

            public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
                Debug.Assert(args.Length == 1);
                Debug.Assert(_module == args[0]);

                MacroPlugin.Instance.MacroManager.Execute(CommandTargetUtil.AsWindow(target).AsForm(), _module);
                return CommandResult.Succeeded;
            }

            public bool CanExecute(ICommandTarget target) {
                return true;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return MacroPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        private class MacroExecMenu : PoderosaMenuItemImpl, IPoderosaMenuItemWithArgs {
            private MacroExecCommand _exec;
            public MacroExecMenu(MacroExecCommand command)
                : base(command, command.Module.Title) {
                _exec = command;
            }

            public IAdaptable[] AdditionalArgs {
                get {
                    return new IAdaptable[] { _exec.Module };
                }
            }

            public bool CanExecute(ICommandTarget target) {
                return _exec.CanExecute(target);
            }
        }
    }

}
