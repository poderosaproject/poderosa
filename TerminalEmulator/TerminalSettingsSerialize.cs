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
using System.Diagnostics;

using Poderosa.View;
using Poderosa.Plugins;
using Poderosa.ConnectionParam;
using Poderosa.Serializing;
using Poderosa.Util;

namespace Poderosa.Terminal {
    //NOTE ログ設定はシリアライズしない。既存ファイルの上書きの危険などあり、ろくなことがないだろう

    internal class TerminalSettingsSerializer : ISerializeServiceElement {
        private ISerializeService _serializeService;
        public TerminalSettingsSerializer(IPluginManager pm) {
            _serializeService = (ISerializeService)pm.FindPlugin("org.poderosa.core.serializing", typeof(ISerializeService));
        }
#if UNITTEST
        public TerminalSettingsSerializer() {
        }
#endif

        public Type ConcreteType {
            get {
                return typeof(TerminalSettings);
            }
        }


        public StructuredText Serialize(object obj, SerializationOptions options) {
            StructuredText storage = new StructuredText(this.ConcreteType.FullName);
            TerminalSettings ts = (TerminalSettings)obj;

            storage.Set("encoding", ts.Encoding.ToString());
            if (ts.TerminalType != TerminalType.XTerm)
                storage.Set("terminal-type", ts.TerminalType.ToString());
            if (ts.LocalEcho)
                storage.Set("localecho", "true");
            if (ts.LineFeedRule != LineFeedRule.Normal)
                storage.Set("linefeedrule", ts.LineFeedRule.ToString());
            if (ts.TransmitNL != NewLine.CR)
                storage.Set("transmit-nl", ts.TransmitNL.ToString());
            if (ts.EnabledCharTriggerIntelliSense)
                storage.Set("char-trigger-intellisense", "true");
            if (!ts.ShellScheme.IsGeneric)
                storage.Set("shellscheme", ts.ShellScheme.Name);
            storage.Set("caption", ts.Caption);
#if !UNITTEST
            //現在テストではRenderProfileは対象外
            if (!ts.UsingDefaultRenderProfile)
                storage.AddChild(_serializeService.Serialize(ts.RenderProfile, options));
#endif
            //アイコンはシリアライズしない
            return storage;
        }

        public object Deserialize(StructuredText node) {
            TerminalSettings ts = new TerminalSettings();
            ts.BeginUpdate();

            ts.Encoding = ParseEncodingType(node.Get("encoding", ""), EncodingType.UTF8_Latin);
            ts.TerminalType = ParseUtil.ParseEnum<TerminalType>(node.Get("terminal-type"), TerminalType.XTerm);
            ts.LocalEcho = ParseUtil.ParseBool(node.Get("localecho"), false);
            ts.LineFeedRule = ParseUtil.ParseEnum<LineFeedRule>(node.Get("linefeedrule"), LineFeedRule.Normal);
            ts.TransmitNL = ParseUtil.ParseEnum<NewLine>(node.Get("transmit-nl"), NewLine.CR);
            ts.EnabledCharTriggerIntelliSense = ParseUtil.ParseBool(node.Get("char-trigger-intellisense"), false);
            string shellscheme = node.Get("shellscheme", ShellSchemeCollection.DEFAULT_SCHEME_NAME);
            if (shellscheme.Length > 0)
                ts.SetShellSchemeName(shellscheme);
            ts.Caption = node.Get("caption", "");
#if !UNITTEST
            //現在テストではRenderProfileは対象外
            StructuredText rp = node.FindChild(typeof(RenderProfile).FullName);
            if (rp != null)
                ts.RenderProfile = _serializeService.Deserialize(rp) as RenderProfile;
#endif
            ts.EndUpdate();
            return ts;
        }

        private EncodingType ParseEncodingType(string text, EncodingType defaultValue) {
            if (text == null || text.Length == 0)
                return defaultValue;

            EncodingType enc = defaultValue;
            if (ParseUtil.TryParseEnum<EncodingType>(text, ref enc))
                return enc;

            // compare with the localized names for the backward compatibility.
            foreach (EnumListItem<EncodingType> item in EnumListItem<EncodingType>.GetListItems()) {
                if (text == item.ToString()) {
                    return item.Value;
                }
            }

            // accept "utf-8" as EncodingType.UTF8 for the backward compatibility.
            if (text == "utf-8")
                return EncodingType.UTF8;

            return defaultValue;
        }

    }

}
