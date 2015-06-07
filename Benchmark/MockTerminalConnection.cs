/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MockTerminalConnection.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */
using System;

using Poderosa.Protocols;

namespace Poderosa.Benchmark {

    /// <summary>
    /// An implementation of ITerminalConnection using MockSocket.
    /// </summary>
    internal class MockTerminalConnection : ITerminalConnection, ITerminalOutput {

        private readonly ITerminalParameter _param;
        private readonly MockSocket _mockSocket;


        public MockTerminalConnection(string terminalType, MockSocket mockSocket) {
            _param = new TerminalParameter();
            _param.SetTerminalName(terminalType);
            _mockSocket = mockSocket;
        }

        public ITerminalParameter Destination {
            get {
                return _param;
            }
        }

        public ITerminalOutput TerminalOutput {
            get {
                return this;
            }
        }

        public IPoderosaSocket Socket {
            get {
                return _mockSocket;
            }
        }

        public bool IsClosed {
            get {
                return _mockSocket.IsClosed;
            }
        }

        public void Close() {
            _mockSocket.Close();
        }

        public IAdaptable GetAdapter(Type adapter) {
            return BenchmarkPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public void SendBreak() {
            // do nothing
        }

        public void SendKeepAliveData() {
            // do nothing
        }

        public void AreYouThere() {
            // do nothing
        }

        public void Resize(int width, int height) {
            // do nothing
        }

    }
}

