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
using System.Xml;

namespace Poderosa {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class XmlStructuredTextReader : StructuredTextReader {
        private XmlElement _root;

        public XmlStructuredTextReader(XmlElement root) {
            _root = root;
        }
        public override StructuredText Read() {
            return Read(_root);
        }

        private StructuredText Read(XmlElement elem) {
            StructuredText node = new StructuredText(elem.LocalName);
            foreach (XmlAttribute attr in elem.Attributes)
                node.Set(attr.LocalName, attr.Value);
            foreach (XmlNode ch in elem.ChildNodes) {
                XmlElement ce = ch as XmlElement;
                if (ce != null)
                    node.AddChild(Read(ce));
            }
            return node;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class XmlStructuredTextWriter : StructuredTextWriter {
        private XmlWriter _writer;

        public XmlStructuredTextWriter(XmlWriter writer) {
            _writer = writer;
        }

        public override void Write(StructuredText node) {
            WriteNode(node);
        }

        private void WriteNode(StructuredText node) {
            _writer.WriteStartElement(node.Name);

            // first, output StructuredText.Entry as a XML attribute.
            List<StructuredText> childNodes = new List<StructuredText>();
            foreach (object ch in node.Children) {
                StructuredText.Entry e = ch as StructuredText.Entry;
                if (e != null) { //entry
                    _writer.WriteAttributeString(e.name, e.value);
                }
                else { //child node
                    childNodes.Add((StructuredText)ch);
                }
            }

            // second, output StructuredText as a XML tag.
            foreach (StructuredText ch in childNodes) {
                WriteNode(ch);
            }

            _writer.WriteEndElement();
        }
    }
}
