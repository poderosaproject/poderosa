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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Poderosa.Plugins;

namespace Poderosa.Forms {
    internal partial class PluginList : Form {
        public PluginList() {
            InitializeComponent();
            InitText();
            FillList();
        }
        private void InitText() {
            StringResource sr = CoreUtil.Strings;
            this.Text = sr.GetString("Form.PluginList.Text");
            _enableHeader.Text = sr.GetString("Form.PluginList._enableHeader");
            _idHeader.Text = "ID";
            _versionHeader.Text = sr.GetString("Form.PluginList._versionHeader");
            _venderHeader.Text = sr.GetString("Form.PluginList._venderHeader");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");
            //構成変更はとりあえず先送り
            _createShortcutButton.Visible = false;
        }

        private void FillList() {
            IPluginInspector pi = (IPluginInspector)WindowManagerPlugin.Instance.PoderosaWorld.PluginManager.GetAdapter(typeof(IPluginInspector));
            foreach (IPluginInfo plugin in pi.Plugins) {
                ListViewItem li = new ListViewItem();
                //li.Checked = plugin.Status==PluginStatus.Activated;
                li.Text = plugin.PluginInfoAttribute.ID;
                //li.SubItems.Add(plugin.PluginInfoAttribute.ID);
                li.SubItems.Add(plugin.PluginInfoAttribute.Version);
                li.SubItems.Add(plugin.PluginInfoAttribute.Author);

                _list.Items.Add(li);
            }
        }
    }
}