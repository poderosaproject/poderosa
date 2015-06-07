/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: XZModemPlugin.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Terminal;
using Poderosa.Commands;
using Poderosa.Protocols;

[assembly: PluginDeclaration(typeof(Poderosa.XZModem.XZModemPlugin))]

namespace Poderosa.XZModem {
    [PluginInfo(ID = XZModemPlugin.PLUGIN_ID, Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.terminalsessions")]
    internal class XZModemPlugin : PluginBase {
        public const string PLUGIN_ID = "org.poderosa.xzmodem";
        private static XZModemPlugin _instance;

        public enum Protocol {
            XModem,
            ZModem
        }

        private ICoreServices _coreServices;
        private StartXZModemCommand _startXZModemCommand;
        private ITerminalEmulatorService _terminalEmulatorService;
        private StringResource _stringResource;

        public static XZModemPlugin Instance {
            get {
                return _instance;
            }
        }

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            _stringResource = new StringResource("Poderosa.XZModem.strings", typeof(XZModemPlugin).Assembly);
            poderosa.Culture.AddChangeListener(_stringResource);

            IPluginManager pm = poderosa.PluginManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _terminalEmulatorService = (ITerminalEmulatorService)pm.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService));

            _startXZModemCommand = new StartXZModemCommand();
            _coreServices.CommandManager.Register(_startXZModemCommand);

            IExtensionPoint consolemenu = pm.FindExtensionPoint(TerminalEmulatorConstants.TERMINALSPECIAL_EXTENSIONPOINT);
            consolemenu.RegisterExtension(new XZModemMenuGroup());

        }

        public StartXZModemCommand StartXZModemCommand {
            get {
                return _startXZModemCommand;
            }
        }
        public ITerminalEmulatorService TerminalEmulatorService {
            get {
                return _terminalEmulatorService;
            }
        }
        public StringResource Strings {
            get {
                return _stringResource;
            }
        }
    }

    internal class StartXZModemCommand : GeneralCommandImpl {
        public StartXZModemCommand()
            : base("org.poderosa.xzmodem.start", XZModemPlugin.Instance.Strings, "Command.XZModem", XZModemPlugin.Instance.TerminalEmulatorService.TerminalCommandCategory) {
        }

        public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            ITerminalControlHost host = TerminalCommandTarget.AsOpenTerminal(target);
            if (host.Terminal.CurrentModalTerminalTask != null) {
                //TODO 関連付けられたXZModemDialogをActivateするようにしたい
                return CommandResult.Ignored;
            }
            else {
                XZModemDialog dlg = new XZModemDialog();
                dlg.Owner = CommandTargetUtil.AsViewOrLastActivatedView(target).ParentForm.AsForm();
                dlg.Initialize(host.Terminal);
                dlg.Show();
                return CommandResult.Succeeded;
            }
        }

        public override bool CanExecute(ICommandTarget target) {
            return TerminalCommandTarget.AsOpenTerminal(target) != null;
        }
    }

    internal class XZModemMenuGroup : PoderosaMenuGroupImpl {
        private XZModemMenuItem _menuItem = new XZModemMenuItem();

        public XZModemMenuGroup()
            : base(new XZModemMenuItem()) {
            _positionType = PositionType.Last;
        }

        private class XZModemMenuItem : PoderosaMenuItemImpl {
            public XZModemMenuItem()
                : base(XZModemPlugin.Instance.StartXZModemCommand, XZModemPlugin.Instance.Strings, "Menu.XZModem") {
            }
        }
    }

    //XModem/ZModemのベースクラス
    internal abstract class ModemBase : IModalTerminalTask, IDisposable {
        protected IModalTerminalTaskSite _site;
        protected IByteAsyncInputStream _defaultHandler;
        protected ITerminalConnection _connection;

        public void InitializeModelTerminalTask(IModalTerminalTaskSite site, IByteAsyncInputStream default_handler, ITerminalConnection connection) {
            _site = site;
            _defaultHandler = default_handler;
            _connection = connection;
        }

        public abstract string Caption {
            get;
        }

        public bool ShowInputInTerminal {
            get {
                return false;
            }
        }
        public void NotifyEndOfPacket() { //do nothing
        }

        public IAdaptable GetAdapter(Type adapter) {
            return XZModemPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public abstract void OnReception(ByteDataFragment data);

        public void OnNormalTermination() {
            _site.Cancel(null);
            Dispose();
            _defaultHandler.OnNormalTermination();
        }

        public void OnAbnormalTermination(string message) {
            _site.Cancel(message);
            Dispose();
            _defaultHandler.OnAbnormalTermination(message);
        }

        //開始と終了
        public abstract void Start();
        public abstract void Abort();

        //送信か受信か
        public abstract bool IsReceivingTask {
            get;
        }

        public abstract void Dispose();
    }
}
