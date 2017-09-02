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
using System.Globalization;
using System.Resources;
using System.Reflection;

using Poderosa.Util;
using Poderosa.Plugins;

namespace Poderosa {
    /// <summary>
    /// <ja>
    /// カルチャ情報を示すオブジェクトです。
    /// </ja>
    /// <en>
    /// Object that shows culture information.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// このクラスの解説は、まだありません。
    /// </ja>
    /// <en>
    /// This class has not explained yet. 
    /// </en>
    /// </remarks>
    public class StringResource : ICultureChangeListener {
        private readonly string _resourceName;
        private readonly Assembly _assembly;
        private ResourceManager _resourceManager;

        protected string ResourceName {
            get {
                return _resourceName;
            }
        }

        protected Assembly Assembly {
            get {
                return _assembly;
            }
        }

        public StringResource(string name, Assembly assembly) {
            _resourceName = name;
            _assembly = assembly;
            _stringResourceDictionary.Add(this);
            CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
            OnCultureChanged(ci);
        }

        // for backward compatibility
        public StringResource(string name, Assembly assembly, bool registerEnumDesc)
            : this(name, assembly) {
        }

        public string GetString(string id) {
            return _resourceManager.GetString(id); //もしこれが遅いようならこのクラスでキャッシュでもつくればいいだろう
        }

        public void OnCultureChanged(CultureInfo newculture) {
            //当面は英語・日本語しかしない
            if (newculture.Name.StartsWith("ja"))
                _resourceManager = new ResourceManager(_resourceName + "_ja", _assembly);
            else
                _resourceManager = new ResourceManager(_resourceName, _assembly);
        }


        #region StringResourceDictionary

        private class StringResourceDictionary {

            private readonly Dictionary<string, List<StringResource>> _dict = new Dictionary<string, List<StringResource>>();

            public StringResourceDictionary() {
            }

            public void Add(StringResource resource) {
                string key = GetKey(resource.Assembly);

                List<StringResource> list;
                if (!_dict.TryGetValue(key, out list)) {
                    const int INITIAL_LIST_SIZE = 4;
                    list = new List<StringResource>(INITIAL_LIST_SIZE);
                    _dict.Add(key, list);
                }

                for (int i = 0; i < list.Count; i++) {
                    if (list[i].ResourceName == resource.ResourceName) {
                        list[i] = resource;
                        return;
                    }
                }

                list.Add(resource);
            }

            public IEnumerable<StringResource> GetStringResourceEnumerable(Assembly assembly) {
                List<StringResource> list;
                if (_dict.TryGetValue(GetKey(assembly), out list)) {
                    return list;
                }
                else {
                    return new StringResource[0];
                }
            }

            private string GetKey(Assembly assembly) {
                return assembly.CodeBase;
            }
        }

        #endregion

        #region Static

        private static readonly StringResourceDictionary _stringResourceDictionary = new StringResourceDictionary();

        public static IEnumerable<StringResource> GetStringResourceEnumerable(Assembly assembly) {
            return _stringResourceDictionary.GetStringResourceEnumerable(assembly);
        }

        #endregion
    }
}