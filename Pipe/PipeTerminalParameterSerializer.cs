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
using System.Collections.Generic;
using System.Diagnostics;

using Poderosa.Serializing;


namespace Poderosa.Pipe {

    /// <summary>
    /// Serializer for PipeTerminalParameter
    /// </summary>
    internal class PipeTerminalParameterSerializer : ISerializeServiceElement {

        public Type ConcreteType {
            get {
                return typeof(PipeTerminalParameter);
            }
        }

        public StructuredText Serialize(object obj) {
            PipeTerminalParameter tp = obj as PipeTerminalParameter;
            Debug.Assert(tp != null);

            StructuredText node = new StructuredText(ConcreteType.FullName);

            if (tp.ExeFilePath != null)
                node.Set("exeFilePath", tp.ExeFilePath);

            if (!String.IsNullOrEmpty(tp.CommandLineOptions))
                node.Set("commandLineOptions", tp.CommandLineOptions);

            if (tp.EnvironmentVariables != null && tp.EnvironmentVariables.Length > 0) {
                foreach (PipeTerminalParameter.EnvironmentVariable e in tp.EnvironmentVariables) {
                    StructuredText envNode = new StructuredText("environmentVariable");
                    envNode.Set("name", e.Name);
                    envNode.Set("value", e.Value);
                    node.AddChild(envNode);
                }
            }

            if (tp.InputPipePath != null)
                node.Set("inputPipePath", tp.InputPipePath);

            if (tp.OutputPipePath != null)
                node.Set("outputPipePath", tp.OutputPipePath);

            if (tp.TerminalType != null)
                node.Set("terminal-type", tp.TerminalType);

            if (tp.AutoExecMacroPath != null)
                node.Set("autoexec-macro", tp.AutoExecMacroPath);

            return node;
        }

        public object Deserialize(StructuredText node) {
            PipeTerminalParameter tp = new PipeTerminalParameter();

            tp.ExeFilePath = node.Get("exeFilePath", null);
            tp.CommandLineOptions = node.Get("commandLineOptions", null);
            List<PipeTerminalParameter.EnvironmentVariable> envList = new List<PipeTerminalParameter.EnvironmentVariable>();
            foreach (StructuredText s in node.FindMultipleNote("environmentVariable")) {
                string name = s.Get("name", null);
                string value = s.Get("value", null);
                if (name != null && value != null) {
                    envList.Add(new PipeTerminalParameter.EnvironmentVariable(name, value));
                }
            }
            tp.EnvironmentVariables = (envList.Count > 0) ? envList.ToArray() : null;
            tp.InputPipePath = node.Get("inputPipePath", null);
            tp.OutputPipePath = node.Get("outputPipePath", null);
            tp.SetTerminalName(node.Get("terminal-type", "vt100"));
            tp.AutoExecMacroPath = node.Get("autoexec-macro", null);
            return tp;
        }
    }
}
