/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: BenchmarkPlugin.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */

using System;
using System.Reflection;

using Poderosa;
using Poderosa.Plugins;

[assembly: Poderosa.Plugins.PluginDeclaration(typeof(Poderosa.Benchmark.BenchmarkPlugin))]
namespace Poderosa.Benchmark {

    /// <summary>
    /// Benchmark Plugin
    /// </summary>
    [PluginInfo(
        ID = BenchmarkPlugin.PLUGIN_ID,
        Version = VersionInfo.PODEROSA_VERSION,
        Author = VersionInfo.PROJECT_NAME,
        Dependencies = "org.poderosa.terminalsessions;org.poderosa.terminalemulator")]
    internal class BenchmarkPlugin : PluginBase {

        public const string PLUGIN_ID = "org.poderosa.benchmark";

        private static BenchmarkPlugin _instance;

        private readonly StringResource _stringResource;


        /// <summary>
        /// Get plugin instance
        /// </summary>
        public static BenchmarkPlugin Instance {
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
        public BenchmarkPlugin() {
            _stringResource = new StringResource("Poderosa.Benchmark.strings", typeof(BenchmarkPlugin).Assembly);
        }

        /// <summary>
        /// Overrides PluginBase
        /// </summary>
        /// <param name="poderosa">Poderosa World</param>
        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;
            poderosa.PluginManager.FindExtensionPoint("org.poderosa.menu.tool").RegisterExtension(new BenchmarkMenuGroup());
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

