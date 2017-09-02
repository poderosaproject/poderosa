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
using System.Diagnostics;

using Poderosa.Serializing;
using Poderosa.Terminal;

namespace Poderosa.Pipe {

    /// <summary>
    /// Serializer for PipeTerminalSettings
    /// </summary>
    internal class PipeTerminalSettingsSerializer : ISerializeServiceElement {

        public Type ConcreteType {
            get {
                return typeof(PipeTerminalSettings);
            }
        }

        public StructuredText Serialize(object obj) {
            PipeTerminalSettings ts = obj as PipeTerminalSettings;
            Debug.Assert(ts != null);

            StructuredText node = new StructuredText(this.ConcreteType.FullName);
            node.AddChild(PipePlugin.Instance.SerializeService.Serialize(typeof(TerminalSettings), obj));

            return node;
        }

        public object Deserialize(StructuredText node) {
            PipeTerminalSettings ts = new PipeTerminalSettings();

            StructuredText baseNode = node.GetChildOrNull(0);
            if (baseNode != null) {
                TerminalSettings baseTs = PipePlugin.Instance.SerializeService.Deserialize(baseNode) as TerminalSettings;
                if (baseTs != null) {
                    ts.Import(baseTs);
                }
            }
            return ts;
        }
    }
}
