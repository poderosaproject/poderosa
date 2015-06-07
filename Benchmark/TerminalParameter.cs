/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalParameter.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */
using System;

using Poderosa.Protocols;
using Poderosa.Usability;

namespace Poderosa.Benchmark {

    /// <summary>
    /// Implements ITerminalParameter
    /// </summary>
    [ExcludeFromMRU]
    internal class TerminalParameter : ITerminalParameter {

        private int _initialWidth = 80;
        private int _initialHeight = 25;
        private string _terminalType = "";


        public int InitialWidth {
            get {
                return _initialWidth;
            }
        }

        public int InitialHeight {
            get {
                return _initialHeight;
            }
        }

        public string TerminalType {
            get {
                return _terminalType;
            }
        }

        public void SetTerminalName(string terminaltype) {
            _terminalType = terminaltype;
        }

        public void SetTerminalSize(int width, int height) {
            _initialWidth = width;
            _initialHeight = height;
        }

        public bool UIEquals(ITerminalParameter t) {
            return t is TerminalParameter;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return BenchmarkPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public object Clone() {
            return MemberwiseClone();
        }
    }
}

