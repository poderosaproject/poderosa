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
using System.IO;
using System.Windows.Forms;

using Poderosa.Commands;

namespace Poderosa.Benchmark {

    /// <summary>
    /// A menu group for Benchmark Plugin
    /// </summary>
    internal class BenchmarkMenuGroup : PoderosaMenuGroupImpl {

        /// <summary>
        /// Constructor
        /// </summary>
        public BenchmarkMenuGroup()
            : base(new BenchmarkMenuFolder()) {
        }

        /// <summary>
        /// Menu folder
        /// </summary>
        private class BenchmarkMenuFolder : IPoderosaMenuFolder {

            private readonly IPoderosaMenuGroup[] _childGroups;

            public BenchmarkMenuFolder() {
                _childGroups = new IPoderosaMenuGroup[] {
                    new PoderosaMenuGroupImpl(
                        new IPoderosaMenu[] {
                            new StartXTermBenchmark(),
                            new StartDataLoadBenchmark(),
                        })
                };
            }

            public IPoderosaMenuGroup[] ChildGroups {
                get {
                    return _childGroups;
                }
            }

            public string Text {
                get {
                    return BenchmarkPlugin.Instance.StringResource.GetString("Menu.Benchmark");
                }
            }

            public bool IsEnabled(ICommandTarget target) {
                return true;
            }

            public bool IsChecked(ICommandTarget target) {
                return false;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return BenchmarkPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        /// <summary>
        /// Base class of a menu item which implements IPoderosaCommand
        /// </summary>
        private abstract class AbstractBenchmarkMenuItem : IPoderosaMenuItem, IPoderosaCommand {

            private readonly string _menuTextId;

            public AbstractBenchmarkMenuItem(string menuTextId) {
                _menuTextId = menuTextId;
            }

            public abstract CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args);

            public bool CanExecute(ICommandTarget target) {
                return true;
            }

            public IPoderosaCommand AssociatedCommand {
                get {
                    return this;
                }
            }

            public string Text {
                get {
                    return BenchmarkPlugin.Instance.StringResource.GetString(_menuTextId);
                }
            }

            public bool IsEnabled(ICommandTarget target) {
                return true;
            }

            public bool IsChecked(ICommandTarget target) {
                return false;
            }

            public IAdaptable GetAdapter(Type adapter) {
                return BenchmarkPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        /// <summary>
        /// XTerm benchmark
        /// </summary>
        private class StartXTermBenchmark : AbstractBenchmarkMenuItem {

            public StartXTermBenchmark()
                : base("Menu.StartXTermBenchmark") {
            }

            public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
                XTermBenchmarkPattern pattern;
                using (ChoosePatternDialog dialog = new ChoosePatternDialog()) {
                    DialogResult result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                        return CommandResult.Cancelled;
                    pattern = dialog.Pattern;
                }

                XTermBenchmark benchmark = new XTermBenchmark(target, pattern);
                return benchmark.Start();
            }
        }

        /// <summary>
        /// XTerm benchmark
        /// </summary>
        private class StartDataLoadBenchmark : AbstractBenchmarkMenuItem {

            public StartDataLoadBenchmark()
                : base("Menu.StartDataLoadBenchmark") {
            }

            public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
                string path;
                int repeat;
                using (DataLoadDialog dialog = new DataLoadDialog()) {
                    DialogResult result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                        return CommandResult.Cancelled;
                    path = dialog.DataFileToLoad;
                    repeat = dialog.Repeat;
                }

                byte[] data = LoadFile(path);
                if (data == null)
                    return CommandResult.Failed;

                DataLoadBenchmark benchmark = new DataLoadBenchmark(target, data, repeat);
                return benchmark.Start();
            }

            private static byte[] LoadFile(string path) {
                FileInfo fileInfo = new FileInfo(path);
                long fileSize = fileInfo.Length;
                int dataSize = (fileSize > Int32.MaxValue) ? Int32.MaxValue : (int)fileSize;
                byte[] data = new byte[dataSize];
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    int readSize = fs.Read(data, 0, dataSize);
                    if (readSize != dataSize)
                        return null;
                }
                return data;
            }
        }
    }

}
