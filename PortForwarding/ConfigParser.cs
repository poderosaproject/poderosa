// Copyright 2005-2017 The Poderosa Project.
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
using System.Collections;

namespace Poderosa.PortForwarding {
    public class ConfigNode {
        private string _name;
        private Hashtable _data;
        private ArrayList _childConfigNodes;

        public string Name {
            get {
                return _name;
            }
        }
        public IDictionaryEnumerator GetPairEnumerator() {
            return _data.GetEnumerator();
        }
        public Hashtable InnerHashtable {
            get {
                return _data;
            }
        }
        public string this[string name] {
            get {
                return (string)_data[name];
            }
            set {
                _data[name] = value;
            }
        }
        public string GetValue(string name, string defval) {
            object t = _data[name];
            return t == null ? defval : (string)t;
        }
        public bool Contains(string name) {
            return _data.Contains(name);
        }
        public bool HasChild {
            get {
                return _childConfigNodes.Count > 0;
            }
        }
        public IEnumerable Children {
            get {
                return _childConfigNodes;
            }
        }
        public void AddChild(ConfigNode child) {
            _childConfigNodes.Add(child);
        }

        public ConfigNode FindChildConfigNode(string name) {
            foreach (ConfigNode s in _childConfigNodes) {
                if (s.Name == name)
                    return s;
            }
            return null;
        }

        public ConfigNode(string name) {
            _name = name;
            _data = new Hashtable();
            _childConfigNodes = new ArrayList();
        }
        public ConfigNode(string name, TextReader reader) {
            _name = name;
            _data = new Hashtable();
            _childConfigNodes = new ArrayList();
            ReadFrom(reader);
        }
        public void ReadFrom(TextReader reader) {
            string line = ReadLine(reader);
            while (line != null) {
                int e = line.IndexOf('=');
                if (e != -1) {
                    string name0 = Normalize(line.Substring(0, e));
                    string value = e == line.Length - 1 ? "" : Normalize(line.Substring(e + 1));
                    _data[name0] = value;
                }
                else if (line.EndsWith("{")) {
                    string[] v = line.Split(' ');
                    foreach (string t in v) {
                        if (t.Length > 0) {
                            _childConfigNodes.Add(new ConfigNode(t, reader));
                            break;
                        }
                    }
                }
                else if (line.EndsWith("}")) {
                    break;
                }
                line = ReadLine(reader);
            }
        }

        private static string ReadLine(TextReader reader) {
            string line = reader.ReadLine();
            return Normalize(line);
        }
        private static string Normalize(string s) {
            int i = 0;
            if (s == null)
                return null;
            do {
                if (i == s.Length)
                    return "";
                char ch = s[i++];
                if (ch != ' ' && ch != '\t')
                    return s.Substring(i - 1);
            } while (true);
        }

        public void WriteTo(TextWriter writer) {
            WriteTo(writer, 0);
        }
        private void WriteTo(TextWriter writer, int indent) {
            WriteIndent(writer, indent);
            writer.Write(_name);
            writer.WriteLine(" {");
            indent += 2;

            IDictionaryEnumerator i = _data.GetEnumerator();
            while (i.MoveNext()) {
                object v = i.Value;
                if (v != null) {
                    WriteIndent(writer, indent);
                    writer.Write(i.Key.ToString());
                    writer.Write('=');
                    writer.WriteLine(v.ToString());
                }
            }
            foreach (ConfigNode ch in _childConfigNodes)
                ch.WriteTo(writer, indent);

            indent -= 2;
            WriteIndent(writer, indent);
            writer.WriteLine("}");
        }
        private void WriteIndent(TextWriter writer, int indent) {
            for (int i = 0; i < indent; i++)
                writer.Write(' ');
        }

        public static ConfigNode CreateIndirect(string name, Hashtable values) {
            ConfigNode n = new ConfigNode(name);
            n._data = (Hashtable)values.Clone();
            return n;
        }
    }


}
