// Copyright 2004-2017 The Poderosa Project.
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Poderosa.Plugins;
using Poderosa.Sessions;
using Poderosa.Util;
using Poderosa.Commands;
using Poderosa.Terminal;
using Poderosa.Forms;

[assembly: PluginDeclaration(typeof(Poderosa.Usability.TerminalUIPlugin))]

namespace Poderosa.Usability {
    [PluginInfo(ID = "org.poderosa.terminalui", Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.terminalsessions;org.poderosa.optiondialog")]
    internal class TerminalUIPlugin : PluginBase {
        private static TerminalUIPlugin _instance;
        private ITerminalEmulatorService _terminalEmulatorPlugin;
        private ICoreServices _coreServices;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;

            IPluginManager pm = poderosa.PluginManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _terminalEmulatorPlugin = (ITerminalEmulatorService)pm.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService));
            Debug.Assert(_terminalEmulatorPlugin != null);

            TerminalUICommand.Register(_coreServices.CommandManager);

            TerminalUIMenuGroup uimenu = new TerminalUIMenuGroup();
            LogMenuGroup logmenu = new LogMenuGroup();
            //Console Menu
            IExtensionPoint consolemenu = pm.FindExtensionPoint("org.poderosa.menu.console");
            consolemenu.RegisterExtension(uimenu);
            consolemenu.RegisterExtension(logmenu);
            IExtensionPoint contextmenu = pm.FindExtensionPoint("org.poderosa.terminalemulator.contextMenu");
            contextmenu.RegisterExtension(uimenu);
            contextmenu.RegisterExtension(logmenu);
            IExtensionPoint documentContextMenu = pm.FindExtensionPoint("org.poderosa.terminalemulator.documentContextMenu");
            documentContextMenu.RegisterExtension(uimenu);
            documentContextMenu.RegisterExtension(logmenu);

            IExtensionPoint toolmenu = pm.FindExtensionPoint("org.poderosa.menu.tool");
            toolmenu.RegisterExtension(new ShellSchemeEditMenuGroup());
        }
        public static TerminalUIPlugin Instance {
            get {
                return _instance;
            }
        }

        public StringResource Strings {
            get {
                return UsabilityPlugin.Strings;
            }
        }
        public ITerminalEmulatorService TerminalEmulatorPlugin {
            get {
                return _terminalEmulatorPlugin;
            }
        }
        public ICommandManager CommandManager {
            get {
                return _coreServices.CommandManager;
            }
        }
        public IWindowManager WindowManager {
            get {
                return _coreServices.WindowManager;
            }
        }

    }
}
