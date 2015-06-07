/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipeTerminalSettings.cs,v 1.1 2011/08/04 15:28:58 kzmi Exp $
 */
using System;

using Poderosa.Terminal;

namespace Poderosa.Pipe {

    /// <summary>
    /// Terminal Settings
    /// </summary>
    internal class PipeTerminalSettings : TerminalSettings {

        public PipeTerminalSettings() {
        }

        public override ITerminalSettings Clone() {
            PipeTerminalSettings ts = new PipeTerminalSettings();
            ts.Import(this);
            return ts;
        }
    }
}
