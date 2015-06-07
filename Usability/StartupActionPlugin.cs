using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Poderosa.Plugins;
using Poderosa.Forms;
using Poderosa.Util;
using Poderosa.Preferences;
using Poderosa.Commands;
using Poderosa.Sessions;
using Poderosa.Protocols;

[assembly: PluginDeclaration(typeof(Poderosa.Usability.StartupActionPlugin))]

namespace Poderosa.Usability {
    [PluginInfo(ID = StartupActionPlugin.PLUGIN_ID, Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.usability")]
    internal class StartupActionPlugin : PluginBase, IMainWindowEventHandler, IPreferenceSupplier {
        public const string PLUGIN_ID = "org.poderosa.startupAction";

        private static StartupActionPlugin _instance;
        public static StartupActionPlugin Instance {
            get {
                return _instance;
            }
        }

        private ICommandManager _commandManager;
        private IWindowManager _windowManager;
        private ITerminalSessionsService _sessionService;
        private IProtocolService _protocolService;
        private ICygwinPlugin _cygwinService;

        private IPreferenceFolder _originalFolder;
        private StartupActionOptions _originalOptions;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            ICoreServices cs = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));

            _commandManager = cs.CommandManager;
            _windowManager = cs.WindowManager;
            _sessionService = (ITerminalSessionsService)poderosa.PluginManager.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService));
            _protocolService = (IProtocolService)poderosa.PluginManager.FindPlugin("org.poderosa.protocols", typeof(IProtocolService));
            _cygwinService = (ICygwinPlugin)poderosa.PluginManager.FindPlugin("org.poderosa.cygwin", typeof(ICygwinPlugin));
            Debug.Assert(_sessionService != null);
            Debug.Assert(_protocolService != null);
            Debug.Assert(_cygwinService != null);

            cs.PreferenceExtensionPoint.RegisterExtension(this);
            poderosa.PluginManager.FindExtensionPoint(WindowManagerConstants.MAINWINDOWEVENTHANDLER_ID).RegisterExtension(this);
        }

        #region IMainWindowEventHandler
        public void OnFirstMainWindowLoaded(IPoderosaMainWindow window) {
            IPoderosaApplication app = (IPoderosaApplication)_poderosaWorld.GetAdapter(typeof(IPoderosaApplication));
            if (app.InitialOpenFile != null)
                OpenFile(window, app.InitialOpenFile);
            else
                DoAction(window, _originalOptions.StartupAction);
        }

        public void OnMainWindowLoaded(IPoderosaMainWindow window) {
        }

        public void OnMainWindowUnloaded(IPoderosaMainWindow window) {
        }

        public void OnLastMainWindowUnloaded(IPoderosaMainWindow window) {
        }
        #endregion

        #region IPreferenceSupplier
        public string PreferenceID {
            get {
                return PLUGIN_ID;
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _originalFolder = folder;
            _originalOptions = new StartupActionOptions(folder);
            _originalOptions.DefineItems(builder);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            Debug.Assert(folder.Id == _originalFolder.Id);
            if (type == typeof(IStartupActionOptions))
                return folder == _originalFolder ? _originalOptions : new StartupActionOptions(folder).Import(_originalOptions);
            else
                return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }
        #endregion

        //メイン動作
        public void DoAction(ICommandTarget target, StartupAction act) {
            switch (act) {
                case StartupAction.TelnetSSHDialog:
                    _commandManager.Execute(_commandManager.Find("org.poderosa.session.telnetSSH"), target);
                    break;
                case StartupAction.OpenCygwin:
                    _sessionService.TerminalSessionStartCommand.StartTerminalSession(target,
                        (ITerminalParameter)_protocolService.CreateDefaultCygwinParameter().GetAdapter(typeof(ITerminalParameter)),
                        _cygwinService.CreateDefaultCygwinTerminalSettings());
                    break;
            }
        }

        public void OpenFile(ICommandTarget target, string filename) {
            //将来的にはショートカットファイル以外を開くこともあるかもしれないが...
            _sessionService.TerminalSessionStartCommand.OpenShortcutFile(target, filename);
        }
    }

    internal enum StartupAction {
        [EnumValue(Description = "Enum.StartupAction.DoNothing")]
        DoNothing,
        [EnumValue(Description = "Enum.StartupAction.TelnetSSHDialog")]
        TelnetSSHDialog,
        [EnumValue(Description = "Enum.StartupAction.OpenCygwin")]
        OpenCygwin
    }

    internal interface IStartupActionOptions {
        StartupAction StartupAction {
            get;
            set;
        }
    }
    internal class StartupActionOptions : SnapshotAwarePreferenceBase, IStartupActionOptions {
        private EnumPreferenceItem<StartupAction> _action;
        public StartupActionOptions(IPreferenceFolder folder)
            : base(folder) {
        }

        public override void DefineItems(IPreferenceBuilder builder) {
            _action = new EnumPreferenceItem<StartupAction>(builder.DefineStringValue(_folder, "startupAction", "DoNothing", null), StartupAction.DoNothing);
        }

        public StartupActionOptions Import(StartupActionOptions src) {
            _action = ConvertItem(src._action);
            return this;
        }

        public StartupAction StartupAction {
            get {
                return _action.Value;
            }
            set {
                _action.Value = value;
            }
        }
    }
}
