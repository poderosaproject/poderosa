/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: StringResource.cs,v 1.3 2012/03/18 12:05:53 kzmi Exp $
*/
using System;
using System.Globalization;
using System.Resources;
using System.Reflection;
using System.Collections.Generic;

namespace Poderosa.Toolkit {
    /// <summary>
    /// StringResource の概要の説明です。
    /// </summary>
    public class StringResources {
        private readonly string _resourceName;
        private readonly Assembly _assembly;
        private ResourceManager _resMan;

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

        public StringResources(string name, Assembly asm) {
            _resourceName = name;
            _assembly = asm;
            _stringResourceDictionary.Add(this);
            LoadResourceManager(name, asm);
        }

        public string GetString(string id) {
            return _resMan.GetString(id); //もしこれが遅いようならこのクラスでキャッシュでもつくればいいだろう
        }

        private void LoadResourceManager(string name, Assembly asm) {
            //当面は英語・日本語しかしない
            CultureInfo ci = System.Threading.Thread.CurrentThread.CurrentUICulture;
            if (ci.Name.StartsWith("ja"))
                _resMan = new ResourceManager(name + "_ja", asm);
            else
                _resMan = new ResourceManager(name, asm);
        }

        #region StringResourceDictionary

        private class StringResourceDictionary {

            private readonly Dictionary<string, List<StringResources>> _dict = new Dictionary<string, List<StringResources>>();

            public StringResourceDictionary() {
            }

            public void Add(StringResources resource) {
                string key = GetKey(resource.Assembly);

                List<StringResources> list;
                if (!_dict.TryGetValue(key, out list)) {
                    const int INITIAL_LIST_SIZE = 4;
                    list = new List<StringResources>(INITIAL_LIST_SIZE);
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

            public IEnumerable<StringResources> GetStringResourceEnumerable(Assembly assembly) {
                List<StringResources> list;
                if (_dict.TryGetValue(GetKey(assembly), out list)) {
                    return list;
                }
                else {
                    return new StringResources[0];
                }
            }

            private string GetKey(Assembly assembly) {
                return assembly.CodeBase;
            }
        }

        #endregion

        #region Static

        private static readonly StringResourceDictionary _stringResourceDictionary = new StringResourceDictionary();

        public static IEnumerable<StringResources> GetStringResourceEnumerable(Assembly assembly) {
            return _stringResourceDictionary.GetStringResourceEnumerable(assembly);
        }

        #endregion
    }
}