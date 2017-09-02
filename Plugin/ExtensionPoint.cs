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
using System.Collections;
using System.Diagnostics;

namespace Poderosa.Plugins {
    internal class ExtensionPoint : IExtensionPoint {
        public const string ROOT = "org.poderosa.root";

        private IPlugin _ownerPlugin;
        private string _id;
        private Type _extensionType;
        private ArrayList _extensions; //ToArray()の型付きを考慮してArrayList
        private bool _isDirty; //_extensionsの中身が変化すると立つフラグ
        private Array _extensionArray;

        public ExtensionPoint(string id, Type extensionType, IPlugin owner) {
            Debug.Assert(extensionType.IsInterface, "sanity");
            _id = id;
            _ownerPlugin = owner;
            _extensionType = extensionType;
            _extensions = new ArrayList();
            _isDirty = true;
        }

        public string ID {
            get {
                return _id;
            }
        }
        public Type ExtensionInterface {
            get {
                return _extensionType;
            }
        }
        public IPlugin OwnerPlugin {
            get {
                return _ownerPlugin;
            }
        }

        public void RegisterExtension(object extension) {
            if (!_extensionType.IsInstanceOfType(extension))
                throw new ArgumentException("Type mismatch: the argument must be an instance of " + _extensionType.Name);

            _extensions.Add(extension);
            _isDirty = true;
        }
        public Array GetExtensions() {
            if (_isDirty) {
                _extensionArray = _extensions.ToArray(_extensionType);
                _isDirty = false;
            }
            return _extensionArray;
        }
    }
}
