/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipeTerminalSettingsSerializer.cs,v 1.1 2011/08/04 15:28:58 kzmi Exp $
 */
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
