/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipePlugin.cs,v 1.2 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa;
using Poderosa.Commands;
using Poderosa.Forms;
using Poderosa.MacroEngine;
using Poderosa.Plugins;
using Poderosa.Protocols;
using Poderosa.Terminal;
using Poderosa.Sessions;
using Poderosa.Serializing;

[assembly: PluginDeclaration(typeof(Poderosa.Pipe.PipePlugin))]

namespace Poderosa.Pipe {

    /// <summary>
    /// Pipe plugin
    /// 
    /// <para>This plugin provides the connections which is using the pipe.</para>
    /// 
    /// <para>There are two connection types, "Process" and "Named Pipe".</para>
    /// 
    /// <para>Process type connection :
    /// Launches an application whose standard input, standard output, and standard error were conneced to Poderosa with pipes.
    /// The text input on a terminal in Poderosa are transmitted to the application's standard input through a pipe.
    /// And the text output from application's standard output or standard error are received through a pipe and are displayed on a terminal in Poderosa.
    /// </para>
    /// 
    /// <para>Named pipe type connection :
    /// Opens one or two existing named pipe for the communication.
    /// You can use one bidirectional named pipe for input and output,
    /// and can also use two named pipes; one is for input and another is for output.
    /// </para>
    /// 
    /// <para>When you use the process type connection, note that some applications use console APIs for its input and output.
    /// If the application doesn't use standard input or standard output but use console APIs,
    /// no text will be displayed on Poderosa or the application doesn't accept your key input.
    /// </para>
    /// </summary>

    [PluginInfo(ID = PipePlugin.PLUGIN_ID,
        Version = VersionInfo.PODEROSA_VERSION,
        Author = VersionInfo.PROJECT_NAME,
        Dependencies = "org.poderosa.terminalsessions;org.poderosa.terminalemulator;org.poderosa.core.serializing")]

    internal class PipePlugin : PluginBase {
        public const string PLUGIN_ID = "org.poderosa.pipe";

        private static PipePlugin _instance;

        private StringResource _stringResource;
        private OpenPipeCommand _openPipeCommand;
        private ITerminalSessionsService _terminalSessionsService;
        private ITerminalEmulatorService _terminalEmulatorService;
        private IAdapterManager _adapterManager;
        private ICoreServices _coreServices;
        private IMacroEngine _macroEngine;
        private ISerializeService _serializeService;

        /// <summary>
        /// Get plugin's instance
        /// </summary>
        internal static PipePlugin Instance {
            get {
                return _instance;
            }
        }

        /// <summary>
        /// Get plugin's string resources
        /// </summary>
        internal StringResource Strings {
            get {
                return _stringResource;
            }
        }

        /// <summary>
        /// Get implementation of ITerminalSessionsService
        /// </summary>
        internal ITerminalSessionsService TerminalSessionsService {
            get {
                return _terminalSessionsService;
            }
        }

        /// <summary>
        /// Get implementation of ITerminalEmulatorService
        /// </summary>
        internal ITerminalEmulatorService TerminalEmulatorService {
            get {
                return _terminalEmulatorService;
            }
        }

        /// <summary>
        /// Get implementation of IAdapterManager
        /// </summary>
        internal IAdapterManager AdapterManager {
            get {
                return _adapterManager;
            }
        }

        /// <summary>
        /// Get implementation of ICommandManager
        /// </summary>
        internal ICommandManager CommandManager {
            get {
                return _coreServices.CommandManager;
            }
        }

        /// <summary>
        /// Get implementation of IMacroEngine
        /// </summary>
        public IMacroEngine MacroEngine {
            get {
                if (_macroEngine == null) {
                    _macroEngine = _poderosaWorld.PluginManager.FindPlugin("org.poderosa.macro", typeof(IMacroEngine)) as IMacroEngine;
                }
                return _macroEngine;
            }
        }

        /// <summary>
        /// Get implementation of ISerializeService
        /// </summary>
        public ISerializeService SerializeService {
            get {
                return _serializeService;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="poderosa">an instance of PoderosaWorld</param>
        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);

            _instance = this;
            _poderosaWorld = poderosa;

            _adapterManager = poderosa.AdapterManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));

            _stringResource = new StringResource("Poderosa.Pipe.strings", typeof(PipePlugin).Assembly);
            poderosa.Culture.AddChangeListener(_stringResource);

            _terminalSessionsService = poderosa.PluginManager.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService)) as ITerminalSessionsService;
            _terminalEmulatorService = poderosa.PluginManager.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService)) as ITerminalEmulatorService;
            _serializeService = poderosa.PluginManager.FindPlugin("org.poderosa.core.serializing", typeof(ISerializeService)) as ISerializeService;

            IExtensionPoint extSer = _coreServices.SerializerExtensionPoint;
            extSer.RegisterExtension(new PipeTerminalParameterSerializer());
            extSer.RegisterExtension(new PipeTerminalSettingsSerializer());

            _openPipeCommand = new OpenPipeCommand();

            IPluginManager pm = poderosa.PluginManager;
            pm.FindExtensionPoint("org.poderosa.menu.file").RegisterExtension(new PipeMenuGroup(_openPipeCommand));

            // Toolbar button has not been added yet
            //pm.FindExtensionPoint("org.poderosa.core.window.toolbar").RegisterExtension(new PipeToolBarComponent());

            pm.FindExtensionPoint("org.poderosa.termianlsessions.terminalConnectionFactory").RegisterExtension(new PipeConnectionFactory());
        }
    }

    /// <summary>
    /// Menu group
    /// </summary>
    internal class PipeMenuGroup : PoderosaMenuGroupImpl {

        /// <summary>
        /// Constructor
        /// </summary>
        public PipeMenuGroup(IPoderosaCommand command)
            : base(new PipeMenuItem(command)) {

            _positionType = PositionType.DontCare;
        }
    }

    /// <summary>
    /// Menu item
    /// </summary>
    internal class PipeMenuItem : PoderosaMenuItemImpl {

        /// <summary>
        /// Constructor
        /// </summary>
        public PipeMenuItem(IPoderosaCommand command)
            : base(command, PipePlugin.Instance.Strings, "Menu.OpenPipe") {
        }
    }

    /// <summary>
    /// Open pipe command
    /// </summary>
    internal class OpenPipeCommand : GeneralCommandImpl {

        /// <summary>
        /// Constructor
        /// </summary>
        public OpenPipeCommand()
            : base("org.poderosa.session.openpipe", PipePlugin.Instance.Strings, "Command.OpenPipe", PipePlugin.Instance.TerminalSessionsService.ConnectCommandCategory) {
        }

        /// <summary>
        /// Command execution
        /// </summary>
        public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {

            PipeTerminalParameter paramInit = null;
            PipeTerminalSettings settingsInit = null;

            IExtensionPoint ext = PipePlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint("org.poderosa.terminalsessions.loginDialogUISupport");
            if (ext != null && ext.ExtensionInterface == typeof(ILoginDialogUISupport)) {
                foreach (ILoginDialogUISupport sup in ext.GetExtensions()) {
                    ITerminalParameter terminalParam;
                    ITerminalSettings terminalSettings;
                    sup.FillTopDestination(typeof(PipeTerminalParameter), out terminalParam, out terminalSettings);
                    PipeTerminalParameter paramTemp = terminalParam as PipeTerminalParameter;
                    PipeTerminalSettings settingsTemp = terminalSettings as PipeTerminalSettings;
                    if (paramInit == null)
                        paramInit = paramTemp;
                    if (settingsInit == null)
                        settingsInit = settingsTemp;
                }
            }
            if (paramInit == null)
                paramInit = new PipeTerminalParameter();
            if (settingsInit == null)
                settingsInit = new PipeTerminalSettings();

            IPoderosaMainWindow window = (IPoderosaMainWindow)target.GetAdapter(typeof(IPoderosaMainWindow));

            CommandResult commandResult = CommandResult.Failed;

            using (OpenPipeDialog dialog = new OpenPipeDialog()) {

                dialog.OpenPipe =
                    delegate(PipeTerminalParameter param, PipeTerminalSettings settings) {
                        PipeTerminalConnection connection = PipeCreator.CreateNewPipeTerminalConnection(param, settings);
                        commandResult = PipePlugin.Instance.CommandManager.Execute(
                                            PipePlugin.Instance.TerminalSessionsService.TerminalSessionStartCommand,
                                            window, connection, settings);
                        return (commandResult == CommandResult.Succeeded);
                    };

                dialog.ApplyParams(paramInit, settingsInit);

                DialogResult dialogResult = dialog.ShowDialog(window != null ? window.AsForm() : null);
                if (dialogResult == DialogResult.Cancel)
                    commandResult = CommandResult.Cancelled;
            }

            return commandResult;
        }
    }

    /// <summary>
    /// Implementation of ITerminalConnectionFactory
    /// </summary>
    internal class PipeConnectionFactory : ITerminalConnectionFactory {

        public bool IsSupporting(ITerminalParameter param, ITerminalSettings settings) {
            return (param is PipeTerminalParameter) && (settings is PipeTerminalSettings);
        }

        public ITerminalConnection EstablishConnection(IPoderosaMainWindow window, ITerminalParameter param, ITerminalSettings settings) {
            PipeTerminalParameter tp = param as PipeTerminalParameter;
            PipeTerminalSettings ts = settings as PipeTerminalSettings;
            Debug.Assert(tp != null && ts != null);

            return PipeCreator.CreateNewPipeTerminalConnection(tp, ts);
        }
    }
}
