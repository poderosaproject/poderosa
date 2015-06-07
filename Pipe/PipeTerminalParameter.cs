/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipeTerminalParameter.cs,v 1.3 2011/11/01 15:35:44 kzmi Exp $
 */
using System;

using Poderosa.Protocols;
using Poderosa.MacroEngine;

namespace Poderosa.Pipe {

    /// <summary>
    /// Terminal Parameters
    /// </summary>
    internal class PipeTerminalParameter : ITerminalParameter, IAutoExecMacroParameter {

        /// <summary>
        /// Environment variable entry
        /// </summary>
        public class EnvironmentVariable {
            public readonly string Name;
            public readonly string Value;

            public EnvironmentVariable(string name, string value) {
                Name = name;
                Value = value;
            }
        }

        private string _terminalType;
        private string _autoExecMacro;

        private string _exeFilePath;
        private string _commandLineOptions;
        private EnvironmentVariable[] _environmentVariables;
        private string _inputPipePath;
        private string _outputPipePath;


        /// <summary>
        /// Constructor
        /// </summary>
        public PipeTerminalParameter() {
        }

        /// <summary>
        /// Path of an executable file to invoke (or null)
        /// </summary>
        /// <remarks>
        /// ExeFilePath and InputPipePath must be set exclusively from each other.
        /// </remarks>
        [MacroConnectionParameter]
        public string ExeFilePath {
            get {
                return _exeFilePath;
            }
            set {
                _exeFilePath = value;
            }
        }

        /// <summary>
        /// Command line options for invoking the executable file (or null)
        /// </summary>
        /// <remarks>
        /// This property is meaningful only if ExeFilePath was not null.
        /// </remarks>
        [MacroConnectionParameter]
        public string CommandLineOptions {
            get {
                return _commandLineOptions;
            }
            set {
                _commandLineOptions = value;
            }
        }

        /// <summary>
        /// Environment variables to be set before invoking the executable file (or null)
        /// </summary>
        /// <remarks>
        /// This property is meaningful only if ExeFilePath was not null.
        /// </remarks>
        [MacroConnectionParameter]
        public EnvironmentVariable[] EnvironmentVariables {
            get {
                return _environmentVariables;
            }
            set {
                _environmentVariables = value;
            }
        }

        /// <summary>
        /// Path of a pipe for input (or null)
        /// </summary>
        /// <remarks>
        /// InputPipePath and ExeFilePath must be set exclusively from each other.
        /// </remarks>
        [MacroConnectionParameter]
        public string InputPipePath {
            get {
                return _inputPipePath;
            }
            set {
                _inputPipePath = value;
            }
        }

        /// <summary>
        /// Path of a pipe for output (or null)
        /// </summary>
        /// <remarks>
        /// This property is meaningful only if InputPipePath was not null.
        /// If this property was null, a file handle of the InputPipePath is used for output.
        /// </remarks>
        [MacroConnectionParameter]
        public string OutputPipePath {
            get {
                return _outputPipePath;
            }
            set {
                _outputPipePath = value;
            }
        }


        #region ITerminalParameter

        public int InitialWidth {
            get {
                return 80;
            }
        }

        public int InitialHeight {
            get {
                return 25;
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
            // do nothing
        }

        public bool UIEquals(ITerminalParameter t) {
            PipeTerminalParameter p = t as PipeTerminalParameter;
            return p != null
                && ((_exeFilePath == null && p._exeFilePath == null)
                    || (_exeFilePath != null && p._exeFilePath != null && String.Compare(_exeFilePath, p._exeFilePath, true) == 0))
                && ((_commandLineOptions == null && p._commandLineOptions == null)
                    || (_commandLineOptions != null && p._commandLineOptions != null && String.Compare(_commandLineOptions, p._commandLineOptions, false) == 0))
                && ((_inputPipePath == null && p._inputPipePath == null)
                    || (_inputPipePath != null && p._inputPipePath != null && String.Compare(_inputPipePath, p._inputPipePath, true) == 0))
                && ((_outputPipePath == null && p._outputPipePath == null)
                    || (_outputPipePath != null && p._outputPipePath != null && String.Compare(_outputPipePath, p._outputPipePath, true) == 0));
        }

        #endregion

        #region IAdaptable

        public IAdaptable GetAdapter(System.Type adapter) {
            return PipePlugin.Instance.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion

        #region ICloneable

        public object Clone() {
            PipeTerminalParameter p = (PipeTerminalParameter)MemberwiseClone();
            if (_environmentVariables != null)
                p._environmentVariables = (EnvironmentVariable[])_environmentVariables.Clone();
            return p;
        }

        #endregion

        #region IAutoExecMacroParameter

        public string AutoExecMacroPath {
            get {
                return _autoExecMacro;
            }
            set {
                _autoExecMacro = value;
            }
        }

        #endregion
    }
}
