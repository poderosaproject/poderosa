/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipeConnection.cs,v 1.3 2011/10/27 23:21:56 kzmi Exp $
 */
using System;

using Poderosa.Protocols;

namespace Poderosa.Pipe {

    /// <summary>
    /// Implementation of ITerminalConnection
    /// </summary>
    internal class PipeTerminalConnection : ITerminalConnection {

        private readonly PipeTerminalOutput _terminalOutput;
        private readonly PipeTerminalParameter _terminalParameter;
        private readonly PipeSocket _socket;
        private readonly PipedProcess _pipedProcess;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="terminalParameter">Terminal parameter</param>
        /// <param name="socket">PipeSocket object</param>
        /// <param name="pipedProcess">Process data (or null)</param>
        public PipeTerminalConnection(PipeTerminalParameter terminalParameter, PipeSocket socket, PipedProcess pipedProcess) {
            _terminalOutput = new PipeTerminalOutput();
            _terminalParameter = terminalParameter;
            _socket = socket;
            _pipedProcess = pipedProcess;

            if (_pipedProcess != null) {
                _pipedProcess.Exited += delegate(object sender, EventArgs e) {
                    _socket.ProcessExited();
                };
            }
        }

        #region ITerminalConnection

        public ITerminalParameter Destination {
            get {
                return _terminalParameter;
            }
        }

        public ITerminalOutput TerminalOutput {
            get {
                return _terminalOutput;
            }
        }

        public IPoderosaSocket Socket {
            get {
                return _socket;
            }
        }

        public bool IsClosed {
            get {
                return _socket.IsClosed;
            }
        }

        public void Close() {
            _socket.Close();
            if (_pipedProcess != null)
                _pipedProcess.Dispose();
        }

        #endregion

        #region IAdaptable

        public IAdaptable GetAdapter(Type adapter) {
            return PipePlugin.Instance.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion

    }

}
