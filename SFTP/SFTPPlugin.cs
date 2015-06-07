/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SFTPPlugin.cs,v 1.1 2011/11/30 22:53:08 kzmi Exp $
 */

[assembly: Poderosa.Plugins.PluginDeclaration(typeof(Poderosa.SFTP.SFTPPlugin))]
namespace Poderosa.SFTP {

    using System;
    using System.Reflection;

    using Poderosa;
    using Poderosa.Plugins;

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

            poderosa.Culture.AddChangeListener(_stringResource);
        }

        /// <summary>
        /// Overrides PluginBase
        /// </summary>
        public override void TerminatePlugin() {
            base.TerminatePlugin();
        }
    }
}

