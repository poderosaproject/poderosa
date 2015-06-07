/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: UsabilityPlugin.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Poderosa.Plugins;
using Poderosa.Protocols;
using Poderosa.Commands;
using Poderosa.Forms;
using Poderosa.Preferences;

[assembly: PluginDeclaration(typeof(Poderosa.Usability.UsabilityPlugin))]

namespace Poderosa.Usability {
    [PluginInfo(ID = "org.poderosa.usability", Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.terminalsessions")]
    internal class UsabilityPlugin : PluginBase {
        private static UsabilityPlugin _instance;
        private static StringResource _stringResource;
        private ICommandManager _commandManager;
        private IWindowManager _windowManager;


        private SSHKnownHosts _sshKnownHosts;
        public static UsabilityPlugin Instance {
            get {
                return _instance;
            }
        }

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            ICoreServices cs = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));

            poderosa.Culture.AddChangeListener(UsabilityPlugin.Strings);
            IPluginManager pm = poderosa.PluginManager;

            _commandManager = cs.CommandManager;
            Debug.Assert(_commandManager != null);

            _windowManager = cs.WindowManager;
            Debug.Assert(_windowManager != null);

            //Guevara AboutBox
            pm.FindExtensionPoint("org.poderosa.window.aboutbox").RegisterExtension(new GuevaraAboutBoxFactory());

            //SSH KnownHost
            _sshKnownHosts = new SSHKnownHosts();
            cs.PreferenceExtensionPoint.RegisterExtension(_sshKnownHosts);
            pm.FindExtensionPoint(ProtocolsPluginConstants.HOSTKEYCHECKER_EXTENSION).RegisterExtension(_sshKnownHosts);
        }
        public override void TerminatePlugin() {
            base.TerminatePlugin();
            if (_sshKnownHosts.Modified)
                _sshKnownHosts.Flush();
        }

        public IWindowManager WindowManager {
            get {
                return _windowManager;
            }
        }

        public static StringResource Strings {
            get {
                if (_stringResource == null)
                    _stringResource = new StringResource("Poderosa.Usability.strings", typeof(TerminalUIPlugin).Assembly);
                return _stringResource;
            }
        }
    }

#if false
    //UsabilityPluginのオプション。GUIでの設定はないので楽な実装
    internal class UsabilityPluginPreference : IPreferenceSupplier {
        private IStringPreferenceItem _knownHostsPath;

        public string PreferenceID {
            get {
                return "org.poderosa.usability";
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _knownHostsPath = builder.DefineStringValue(folder, "knownHostsPath", "ssh_known_hosts", null);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }

        public IStringPreferenceItem KnownHostsPath {
            get {
                return _knownHostsPath;
            }
        }
    }
#endif
}
