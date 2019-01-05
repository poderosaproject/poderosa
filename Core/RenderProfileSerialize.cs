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
using System.Diagnostics;

using Poderosa.Serializing;

namespace Poderosa.View {
    internal class RenderProfileSerializer : ISerializeServiceElement {
        public Type ConcreteType {
            get {
                return typeof(RenderProfile);
            }
        }

        public StructuredText Serialize(object obj) {
            StructuredText storage = new StructuredText(typeof(RenderProfile).FullName);
            RenderProfile prof = (RenderProfile)obj;
            storage.Set("font-name", prof.FontName);
            storage.Set("cjk-font-name", prof.CJKFontName);
            storage.Set("font-size", prof.FontSize.ToString());
            storage.Set("line-spacing", prof.LineSpacing.ToString());
            if (prof.UseClearType)
                storage.Set("clear-type", "true");
            if (!prof.EnableBoldStyle)
                storage.Set("enable-bold-style", "false");
            if (prof.ForceBoldStyle)
                storage.Set("force-bold-style", "true");
            storage.Set("text-color", prof.ForeColor.Name);
            storage.Set("back-color", prof.BackColor.Name);
            if (prof.BackgroundImageFileName.Length > 0) {
                storage.Set("back-image", prof.BackgroundImageFileName);
                storage.Set("back-style", prof.ImageStyle.ToString());
            }
            if (!prof.ESColorSet.IsDefault)
                storage.Set("escape-sequence-color", prof.ESColorSet.Format());
            storage.Set("darken-escolor-for-background", prof.DarkenEsColorForBackground.ToString());
            return storage;
        }

        public object Deserialize(StructuredText storage) {
            RenderProfile prof = new RenderProfile();
            prof.FontName = storage.Get("font-name", "Courier New");
            prof.CJKFontName = storage.Get("cjk-font-name",
                               storage.Get("japanese-font-name",
                               storage.Get("chinese-font-name", "Courier New")));
            prof.FontSize = ParseUtil.ParseFloat(storage.Get("font-size"), 10.0f);
            prof.LineSpacing = ParseUtil.ParseInt(storage.Get("line-spacing"), 0);
            prof.UseClearType = ParseUtil.ParseBool(storage.Get("clear-type"), false);
            prof.EnableBoldStyle = ParseUtil.ParseBool(storage.Get("enable-bold-style"), true);
            prof.ForceBoldStyle = ParseUtil.ParseBool(storage.Get("force-bold-style"), false);
            prof.ForeColor = ParseUtil.ParseColor(storage.Get("text-color"), Color.FromKnownColor(KnownColor.WindowText));
            prof.BackColor = ParseUtil.ParseColor(storage.Get("back-color"), Color.FromKnownColor(KnownColor.Window));
            prof.ImageStyle = ParseUtil.ParseEnum<ImageStyle>(storage.Get("back-style"), ImageStyle.Center);
            prof.BackgroundImageFileName = storage.Get("back-image", "");

            prof.ESColorSet = new EscapesequenceColorSet();
            string escolor = storage.Get("escape-sequence-color");
            if (escolor != null)
                prof.ESColorSet.Load(escolor);
            prof.DarkenEsColorForBackground = ParseUtil.ParseBool(storage.Get("darken-escolor-for-background"), true);

            return prof;
        }
    }

}
