// Copyright 2011-2017 The Poderosa Project.
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

[assembly: Poderosa.Plugins.PluginDeclaration(typeof(Poderosa.SFTP.SFTPPlugin))]
namespace Poderosa.SFTP {

    using System;
    using System.Reflection;

    using Poderosa;
    using Poderosa.Plugins;
    using Poderosa.Preferences;

    /// <summary>
    /// SFTP Plugin
    /// </summary>
    [PluginInfo(
        ID = SFTPPlugin.PLUGIN_ID,
        Version = VersionInfo.PODEROSA_VERSION,
        Author = VersionInfo.PROJECT_NAME,
      Dependencies = "org.poderosa.protocols;org.poderosa.terminalsessions;org.poderosa.terminalemulator")]
    internal class SFTPPlugin : PluginBase {

        public const string PLUGIN_ID = "org.poderosa.sftp";

        private static SFTPPlugin _instance;

        private readonly StringResource _stringResource;

        private readonly SFTPPreferences _sftpPreferences = new SFTPPreferences();
        private readonly SCPPreferences _scpPreferences = new SCPPreferences();

        /// <summary>
        /// Get plugin instance
        /// </summary>
        public static SFTPPlugin Instance {
            get {
                return _instance;
            }
        }

        /// <summary>
        /// Get plugin's StringResource
        /// </summary>
        public StringResource StringResource {
            get {
                return _stringResource;
            }
        }

        /// <summary>
        /// SFTP preferences
        /// </summary>
        public SFTPPreferences SFTPPreferences {
            get {
                return _sftpPreferences;
            }
        }

        /// <summary>
        /// SCP preferences
        /// </summary>
        public SCPPreferences SCPPreferences {
            get {
                return _scpPreferences;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPPlugin() {
            _stringResource = new StringResource("Poderosa.SFTP.strings", typeof(SFTPPlugin).Assembly);
        }

        /// <summary>
        /// Overrides PluginBase
        /// </summary>
        /// <param name="poderosa">Poderosa World</param>
        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            
            SFTPToolbar toolbar = new SFTPToolbar();
            poderosa.PluginManager.FindExtensionPoint("org.poderosa.core.window.toolbar").RegisterExtension(toolbar);
            poderosa.PluginManager.FindExtensionPoint("org.poderosa.menu.tool").RegisterExtension(toolbar.MenuGroup);
            
            ICoreServices coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            coreServices.SessionManager.AddActiveDocumentChangeListener(toolbar);
            coreServices.PreferenceExtensionPoint.RegisterExtension(_sftpPreferences);
            coreServices.PreferenceExtensionPoint.RegisterExtension(_scpPreferences);

            poderosa.Culture.AddChangeListener(_stringResource);
        }

        /// <summary>
        /// Overrides PluginBase
        /// </summary>
        public override void TerminatePlugin() {
            base.TerminatePlugin();
        }
    }

    /// <summary>
    /// SFTP preferences
    /// </summary>
    internal class SFTPPreferences : IPreferenceSupplier {

        private IIntPreferenceItem _protocolTimeout;

        /// <summary>
        /// Protocol timeout
        /// </summary>
        public int ProtocolTimeout {
            get {
                return _protocolTimeout.Value;
            }
        }

        public string PreferenceID {
            get {
                return "org.poderosa.sftp";
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _protocolTimeout = builder.DefineIntValue(folder, "protocolTimeout", 5000, PreferenceValidatorUtil.PositiveIntegerValidator);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }
    }

    /// <summary>
    /// SCP preferences
    /// </summary>
    internal class SCPPreferences : IPreferenceSupplier {

        private IIntPreferenceItem _protocolTimeout;

        /// <summary>
        /// Protocol timeout
        /// </summary>
        public int ProtocolTimeout {
            get {
                return _protocolTimeout.Value;
            }
        }

        public string PreferenceID {
            get {
                return "org.poderosa.scp";
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _protocolTimeout = builder.DefineIntValue(folder, "protocolTimeout", 5000, PreferenceValidatorUtil.PositiveIntegerValidator);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }
    }
}

