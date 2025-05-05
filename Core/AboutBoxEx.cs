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

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Reflection;

using Poderosa.Preferences;
using Poderosa.Plugins;

namespace Poderosa.Forms {

    //一応OEMとかあるかもな、というタテマエだがおそらくゲバラモード専用機能。名前改善したい
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IPoderosaAboutBoxFactory {
        string AboutBoxID {
            get;
        }
        Form CreateAboutBox();
        Icon ApplicationIcon {
            get;
        }

        string EnterMessage {
            get;
        }
        string ExitMessage {
            get;
        }
    }

    //各種AboutBoxが共通して使うであろう機能
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class AboutBoxUtil {
        public const string DEFAULT_ABOUTBOX_ID = "default";

        public static string[] GetVersionInfoContent() {
            string[] s = {
                "Terminal Emulator <Poderosa>",
                "Copyright(c) " + VersionInfo.COPYRIGHT_YEARS + " " + VersionInfo.PROJECT_NAME + ",",
                "All Rights Reserved.",
                "",
                " Version : " + VersionInfo.PODEROSA_VERSION,
                " CLR : " + System.Environment.Version.ToString() + (System.Environment.Is64BitProcess ? " (64 bit)" : " (32 bit)"),
                " Runtime Framework : " + GetFrameworkVersion(),
            };
            return s;
        }

        private static string GetFrameworkVersion() {
            // >= .NET Framework 4.7.1
            // return System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
            AssemblyFileVersionAttribute attr = (AssemblyFileVersionAttribute)(typeof(object).GetTypeInfo().Assembly.GetCustomAttribute(typeof(AssemblyFileVersionAttribute)));
            return attr.Version;
        }

        //ExtensionPointとPreference。別クラスに分離してWindowManagerのメンバに入れようかな？
        private static IStringPreferenceItem _aboutBoxID;
        public static IStringPreferenceItem AboutBoxID {
            get {
                return _aboutBoxID;
            }
        }
        internal static void DefineExtensionPoint(IPluginManager pm) {
            IExtensionPoint pt = pm.CreateExtensionPoint("org.poderosa.window.aboutbox", typeof(IPoderosaAboutBoxFactory), WindowManagerPlugin.Instance);
            pt.RegisterExtension(new DefaultAboutBoxFactory());
        }
        internal static void InitPreference(IPreferenceBuilder builder, IPreferenceFolder window_root) {
            _aboutBoxID = builder.DefineStringValue(window_root, "aboutBoxFactoryID", "default", null);
        }
        public static IPoderosaAboutBoxFactory GetCurrentAboutBoxFactory() {
            //AboutBox実装を見つける
            if (AboutBoxUtil.AboutBoxID == null)
                return null;
            string factory_id = AboutBoxUtil.AboutBoxID.Value;
            IPoderosaAboutBoxFactory found_factory = null;
            IPoderosaAboutBoxFactory[] factories = (IPoderosaAboutBoxFactory[])WindowManagerPlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint("org.poderosa.window.aboutbox").GetExtensions();
            foreach (IPoderosaAboutBoxFactory f in factories) {
                if (f.AboutBoxID == factory_id) {
                    found_factory = f;
                    break;
                }
                else if (f.AboutBoxID == DEFAULT_ABOUTBOX_ID) { //TODO ちゃんとしたconst string参照
                    found_factory = f; //このあとのループで正式に一致するやつが見つかったら上書きされることに注意
                }
            }

            Debug.Assert(found_factory != null);
            return found_factory;
        }

        private static StringBuilder _keyBufferInAboutBox;

        public static void ResetKeyBufferInAboutBox() {
            _keyBufferInAboutBox = new StringBuilder();
        }
        //ダイアログボックス内でのキー入力処理
        public static bool ProcessDialogChar(char charCode) {
            if ('A' <= charCode && charCode <= 'Z')
                charCode = (char)('a' + charCode - 'A');
            _keyBufferInAboutBox.Append(charCode);
            string t = _keyBufferInAboutBox.ToString();
            IPoderosaAboutBoxFactory[] factories = (IPoderosaAboutBoxFactory[])WindowManagerPlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint("org.poderosa.window.aboutbox").GetExtensions();
            foreach (IPoderosaAboutBoxFactory f in factories) {
                if (t == f.AboutBoxID) {
                    if (_aboutBoxID.Value == f.AboutBoxID) { //リセット
                        MessageBox.Show(f.ExitMessage, "Poderosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _aboutBoxID.Value = DEFAULT_ABOUTBOX_ID;
                    }
                    else { //モード変更
                        MessageBox.Show(f.EnterMessage, "Poderosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _aboutBoxID.Value = f.AboutBoxID;
                    }

                    WindowManagerPlugin.Instance.ReloadPreference(WindowManagerPlugin.Instance.WindowPreference.OriginalPreference);
                    return true;
                }
            }

            return false;

        }
    }
}
