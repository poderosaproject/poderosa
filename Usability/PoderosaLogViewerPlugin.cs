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
using System.Text;
using System.Drawing;

using Poderosa.Plugins;
using Poderosa.Protocols;
using Poderosa.Commands;
using Poderosa.Forms;
using Poderosa.Sessions;
using Poderosa.Preferences;

[assembly: PluginDeclaration(typeof(Poderosa.LogViewer.PoderosaLogViewerPlugin))]

namespace Poderosa.LogViewer {
    [PluginInfo(ID = "org.poderosa.poderosalogviewer", Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.window")]
    internal class PoderosaLogViewerPlugin : PluginBase {
        private static PoderosaLogViewerPlugin _instance;

        private StringResource _strings;
        private ICoreServices _coreServices;
        private PoderosaLogViewerSession _session;
        private LogViewerFactory _viewFactory;
        private GeneralCommandImpl _command;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);

            _instance = this;
            _strings = new StringResource("Poderosa.Usability.strings", typeof(PoderosaLogViewerPlugin).Assembly);
            poderosa.Culture.AddChangeListener(_strings);
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _viewFactory = new LogViewerFactory();
            poderosa.PluginManager.FindExtensionPoint(WindowManagerConstants.VIEW_FACTORY_ID).RegisterExtension(_viewFactory);
            _session = new PoderosaLogViewerSession();

            ICommandManager cm = _coreServices.CommandManager;
            //Command and Menu
            _command = new GeneralCommandImpl("org.poderosa.poderosalogviewer.show", _strings, "Command.PoderosaLog", cm.CommandCategories.Dialogs,
                new ExecuteDelegate(CmdShowPoderosaLog));
            poderosa.PluginManager.FindExtensionPoint("org.poderosa.menu.tool").RegisterExtension(
                new PoderosaMenuGroupImpl(new PoderosaMenuItemImpl(_command, _strings, "Menu.PoderosaLog")));
        }

        public static PoderosaLogViewerPlugin Instance {
            get {
                return _instance;
            }
        }
        public ICoreServices CoreServices {
            get {
                return _coreServices;
            }
        }
        public StringResource Strings {
            get {
                return _strings;
            }
        }

        private CommandResult CmdShowPoderosaLog(ICommandTarget target) {
            if (_session.IsWindowVisible) { //表示中の場合
                _session.CurrentView.ParentForm.AsForm().Activate();
                return CommandResult.Succeeded;
            }
            else {
                //セッションの作成（オブジェクトとしては再利用）
                PopupViewCreationParam cp = new PopupViewCreationParam(_viewFactory);
                cp.InitialSize = new Size(PoderosaLogViewControl.DefaultWidth, 300);
                IPoderosaPopupWindow window = _coreServices.WindowManager.CreatePopupView(cp);
                _coreServices.SessionManager.StartNewSession(_session, window.InternalView);
                _coreServices.SessionManager.ActivateDocument(_session.Document, ActivateReason.InternalAction);
                return CommandResult.Succeeded;
            }
        }
    }



}
