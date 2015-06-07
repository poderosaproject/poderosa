/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: AbstractTerminalBenchmark.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */
using System;

using Poderosa.Commands;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.Terminal;

namespace Poderosa.Benchmark {

    /// <summary>
    /// Base class for the terminal benchmark
    /// </summary>
    internal abstract class AbstractTerminalBenchmark {

        private readonly ICommandTarget _target;

        /// <summary>
        /// Returns terminal's caption
        /// </summary>
        protected abstract string GetTerminalCaption();

        /// <summary>
        /// Returns ITerminalConnection instance from the derived class
        /// </summary>
        protected abstract ITerminalConnection GetTerminalConnection();

        /// <summary>
        /// Starts benchmark thread in the derived class
        /// </summary>
        protected abstract void StartBenchmarkThread(ITerminalEmulatorOptions options, ITerminalSession session);


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">target object</param>
        protected AbstractTerminalBenchmark(ICommandTarget target) {
            _target = target;
        }

        /// <summary>
        /// Start benchmark
        /// </summary>
        /// <returns></returns>
        public CommandResult Start() {
            GC.Collect();
            GC.WaitForPendingFinalizers();

            ITerminalEmulatorService emulatorService =
                BenchmarkPlugin.Instance.PoderosaWorld.PluginManager.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService)) as ITerminalEmulatorService;
            ITerminalSessionsService sessionService =
                (ITerminalSessionsService)BenchmarkPlugin.Instance.PoderosaWorld.PluginManager.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService));

            if (emulatorService == null || sessionService == null)
                return CommandResult.Ignored;

            ITerminalSettings settings = emulatorService.CreateDefaultTerminalSettings(GetTerminalCaption(), null);
            settings.BeginUpdate();
            settings.Encoding = Poderosa.ConnectionParam.EncodingType.UTF8;
            settings.EndUpdate();
            ITerminalConnection connection = GetTerminalConnection();
            ITerminalSessionStartCommand startCommand = sessionService.TerminalSessionStartCommand;
            ITerminalSession session = startCommand.StartTerminalSession(_target, connection, settings);

            StartBenchmarkThread(emulatorService.TerminalEmulatorOptions, session);

            return CommandResult.Succeeded;
        }
    }
}
