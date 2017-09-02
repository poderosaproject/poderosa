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
