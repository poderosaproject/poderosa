// Copyright 2004-2025 The Poderosa Project.
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

#if UNITTEST
using NUnit.Framework;
using Poderosa.Boot;
using Poderosa.Commands;
using Poderosa.Plugins;
using Poderosa.Preferences;
using Poderosa.Sessions;
using Poderosa.TestUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace Poderosa.Forms {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class MainWindowMenuTests {

        private static IPoderosaApplication _poderosaApplication;
        private static IPoderosaWorld _poderosaWorld;

        private static string CreatePluginManifest() {
            return String.Format("manifest {{\r\n  {0} {{\r\n  plugin={1}\r\n}}\r\n  {2} {{\r\n  plugin={3}\r\n  plugin={4}\r\n  plugin={5}\r\n}}\r\n}}",
                Assembly.GetAssembly(typeof(MenuTestPlugin)).CodeBase,
                "Poderosa.Forms.MenuTestPlugin",
                Assembly.GetAssembly(typeof(PreferencePlugin)).CodeBase,
                "Poderosa.Preferences.PreferencePlugin",
                "Poderosa.Commands.CommandManagerPlugin",
                "Poderosa.Forms.WindowManagerPlugin");
        }

        [OneTimeSetUp]
        public void Init() {
            AssemblyUtil.SetEntryAssembly(typeof(MainWindowMenuTests));
            try {
                _poderosaApplication = PoderosaStartup.CreatePoderosaApplication(CreatePluginManifest(), new StructuredText("Poderosa"), new string[0]);
                _poderosaWorld = _poderosaApplication.Start();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.StackTrace);
            }
        }

        [OneTimeTearDown]
        public void Terminate() {
            _poderosaApplication.Shutdown();
        }

        [Test]
        public void Test0_Menu1() {
            string caption = "Init1";
            MenuTestPlugin._instance.SetContent(caption, 1);
            IWindowManager wm = (IWindowManager)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.core.window", typeof(IWindowManager));
            //TODO 明示的にリロード強要はいやらしい
            wm.ReloadMenu();

            Form form = wm.MainWindows[0].AsForm();
            ToolStripDropDownItem fm = form.MainMenuStrip.Items[0] as ToolStripDropDownItem;
            Assert.AreEqual(3, fm.DropDown.Items.Count); //デリミタが入るので
            Assert.AreEqual(caption, fm.DropDown.Items[0].Text);
            Assert.AreEqual(caption, fm.DropDown.Items[2].Text);
        }

        [Test]
        public void Test1_Enabled_checked() {
            string caption = "Init2";
            MenuTestPlugin._instance.InitEnabledChecked(caption, true, true);
            IWindowManager wm = (IWindowManager)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.core.window", typeof(IWindowManager));
            wm.ReloadMenu();

            Form form = wm.MainWindows[0].AsForm();
            ToolStripDropDownItem fm = form.MainMenuStrip.Items[0] as ToolStripDropDownItem;
            Assert.AreEqual(caption, fm.DropDown.Items[0].Text);

            fm.ShowDropDown();  // need for updating controls

            Assert.IsTrue(((ToolStripMenuItem)fm.DropDown.Items[0]).Checked);
            Assert.IsTrue(fm.DropDown.Items[0].Enabled);
        }

        [Test]
        public void Test1_Disabled_checked() {
            string caption = "Init2";
            MenuTestPlugin._instance.InitEnabledChecked(caption, false, true);
            IWindowManager wm = (IWindowManager)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.core.window", typeof(IWindowManager));
            wm.ReloadMenu();

            Form form = wm.MainWindows[0].AsForm();
            ToolStripDropDownItem fm = form.MainMenuStrip.Items[0] as ToolStripDropDownItem;
            Assert.AreEqual(caption, fm.DropDown.Items[0].Text);

            fm.ShowDropDown();  // need for updating controls

            Assert.IsFalse(((ToolStripMenuItem)fm.DropDown.Items[0]).Checked);
            Assert.IsFalse(fm.DropDown.Items[0].Enabled);
        }

        [Test]
        public void Test3_DynamicContent() {
            string caption = "Init3";
            MenuTestPlugin._instance.SetContent(caption, 1);
            IWindowManager wm = (IWindowManager)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.core.window", typeof(IWindowManager));
            wm.ReloadMenu();

            Form form = wm.MainWindows[0].AsForm();
            ToolStripDropDownItem fm = form.MainMenuStrip.Items[0] as ToolStripDropDownItem;
            Assert.AreEqual(3, fm.DropDown.Items.Count);

            string caption2 = "Changed";
            MenuTestPlugin._instance.SetContent(caption2, 3);

            fm.ShowDropDown();  // need for updating controls

            Assert.AreEqual(5, fm.DropDown.Items.Count);
            Assert.AreEqual(caption2, fm.DropDown.Items[0].Text);
            Assert.AreEqual(caption2, fm.DropDown.Items[4].Text);
        }
    }

    [PluginInfo(ID = "org.poderosa.test.menutest", Dependencies = "org.poderosa.core.window")]
    internal class MenuTestPlugin : PluginBase {
        public static MenuTestPlugin _instance;

        private MenuGroup1 _group1;
        private MenuGroup2 _group2;

        private class Item : IPoderosaMenuItem {
            private string _text;
            private bool _enabled;
            private bool _checked;

            public Item(string text) {
                _text = text;
                _enabled = true;
                _checked = false;
            }

            public IPoderosaCommand AssociatedCommand {
                get {
                    return null;
                }
            }

            public string Text {
                get {
                    return _text;
                }
            }

            public bool IsEnabled(ICommandTarget target) {
                return _enabled;
            }

            public bool IsChecked(ICommandTarget target) {
                return _checked;
            }

            public void SetEnabled(bool value) {
                _enabled = value;
            }
            public void SetChecked(bool value) {
                _checked = value;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return adapter.IsInstanceOfType(this) ? this : null;
            }
        }

        private class MenuGroup1 : IPoderosaMenuGroup {
            private List<Item> _items;
            public MenuGroup1() {
                _items = new List<Item>();
            }

            public IPoderosaMenu[] ChildMenus {
                get {
                    return _items.ToArray();
                }
            }

            public bool IsVolatileContent {
                get {
                    return false;
                }
            }
            public bool ShowSeparator {
                get {
                    return true;
                }
            }

            public void Init(string caption) {
                _items.Clear();
                _items.Add(new Item(caption));
            }
            public void SetStatus(bool enabled, bool checked_) {
                Item i = _items[0];
                i.SetEnabled(enabled);
                i.SetChecked(checked_);
            }
            public IAdaptable GetAdapter(Type adapter) {
                return adapter.IsInstanceOfType(this) ? this : null;
            }
        }

        private class MenuGroup2 : IPoderosaMenuGroup {
            private List<Item> _items;
            public MenuGroup2() {
                _items = new List<Item>();
            }

            public IPoderosaMenu[] ChildMenus {
                get {
                    return _items.ToArray();
                }
            }

            public bool IsVolatileContent {
                get {
                    return true;
                }
            }
            public bool ShowSeparator {
                get {
                    return true;
                }
            }

            public void SetItems(string caption, int count) {
                _items.Clear();
                for (int i = 0; i < count; i++)
                    _items.Add(new Item(caption));
            }

            public void Clear() {
                _items.Clear();
            }
            public IAdaptable GetAdapter(Type adapter) {
                return adapter.IsInstanceOfType(this) ? this : null;
            }
        }

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _group1 = new MenuGroup1();
            _group2 = new MenuGroup2();
            IExtensionPoint ep = poderosa.PluginManager.FindExtensionPoint("org.poderosa.menu.file");
            ep.RegisterExtension(_group1);
            ep.RegisterExtension(_group2);
            _instance = this;

            foreach (IViewManagerFactory mf in
                poderosa.PluginManager.FindExtensionPoint(WindowManagerConstants.MAINWINDOWCONTENT_ID).GetExtensions()) {

                mf.DefaultViewFactory = new ViewFactoryForTest();
            }
        }

        public void SetContent(string caption, int group2_item_count) {
            _group1.Init(caption);
            _group2.SetItems(caption, group2_item_count);
        }

        public void InitEnabledChecked(string caption, bool enabled, bool checked_) {
            _group1.Init(caption);
            _group1.SetStatus(enabled, checked_);
            _group2.Clear();
        }
        public void ResetEnabledChecked(bool enabled, bool checked_) {
            _group1.SetStatus(enabled, checked_);
        }

        #region ViewFactoryForTest

        private class ViewFactoryForTest : IViewFactory {

            public IPoderosaView CreateNew(IPoderosaForm parent) {
                return new PoderosaViewForTest();
            }

            public Type GetViewType() {
                throw new NotImplementedException();
            }

            public Type GetDocumentType() {
                throw new NotImplementedException();
            }

            public IAdaptable GetAdapter(Type adapter) {
                throw new NotImplementedException();
            }
        }

        private class PoderosaViewForTest : Panel, IPoderosaView {

            public IPoderosaDocument Document {
                get {
                    throw new NotImplementedException();
                }
            }

            public View.ISelection CurrentSelection {
                get {
                    throw new NotImplementedException();
                }
            }

            public IPoderosaForm ParentForm {
                get {
                    throw new NotImplementedException();
                }
            }

            public Control AsControl() {
                return this;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return null;
            }

            public void SuspendResize() {
            }

            public void ResumeResize() {
            }
        }

        #endregion
    }
}
#endif
