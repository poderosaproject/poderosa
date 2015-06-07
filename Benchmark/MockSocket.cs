/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MockSocket.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

using Poderosa.Protocols;

namespace Poderosa.Benchmark {

    /// <summary>
    /// Implements IPoderosaSocket.
    /// This class provides a virtual socket which reads bytes from a specified data source.
    /// </summary>
    internal class MockSocket : IPoderosaSocket {

        private volatile bool _isClosed = false;
        private volatile bool _isProcessing = false;
        private volatile IEnumerable<byte[]> _generator;

        private Thread _pumpThread;
        private readonly object _pumpThreadSync = new object();

        /// <summary>
        /// Get whether this socket was closed
        /// </summary>
        public bool IsClosed {
            get {
                return _isClosed;
            }
        }

        /// <summary>
        /// Feed virtual incoming data and wait till they are processed.
        /// </summary>
        /// <param name="generator">data source</param>
        /// <param name="timeout">timeout in milliseconds</param>
        public void FeedData(IEnumerable<byte[]> generator, int timeout) {
            if (_isClosed)
                return;

            if (!Monitor.TryEnter(_pumpThreadSync, timeout))
                throw new MockSocketTimeoutException();

            _generator = generator;

            Monitor.PulseAll(_pumpThreadSync);
            Monitor.Wait(_pumpThreadSync);
            Monitor.Exit(_pumpThreadSync);
        }

        /// <summary>
        /// Implements IPoderosaSocket.
        /// New pump thread is started.
        /// </summary>
        /// <param name="receiver"></param>
        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            if (_pumpThread != null)
                return;

            _pumpThread = new Thread((ThreadStart)delegate() {
                PumpThread(receiver);
            });
            _pumpThread.Name = "Poderosa.Benchmark.MockSocket";
            _pumpThread.Start();
        }

        /// <summary>
        /// Pump thread
        /// </summary>
        /// <param name="receiver"></param>
        private void PumpThread(IByteAsyncInputStream receiver) {
            lock (_pumpThreadSync) {
                while (true) {
                    if (_isClosed)
                        break;

                    if (_generator != null) {
                        IEnumerable<byte[]> generator = _generator;
                        _generator = null;

                        _isProcessing = true;

                        foreach (byte[] data in generator) {
                            if (_isClosed)
                                break;
                            ByteDataFragment fragment = new ByteDataFragment(data, 0, data.Length);
                            receiver.OnReception(fragment);
                        }

                        _isProcessing = false;

                        Monitor.PulseAll(_pumpThreadSync);
                    }

                    if (_isClosed)
                        break;

                    Monitor.Wait(_pumpThreadSync);
                }
            }
        }

        /// <summary>
        /// Implements IPoderosaSocket.
        /// </summary>
        public bool Available {
            get {
                return _isProcessing;
            }
        }

        /// <summary>
        /// Implements IPoderosaSocket.
        /// </summary>
        public void ForceDisposed() {
            Close();
        }

        /// <summary>
        /// Implements IPoderosaSocket.
        /// </summary>
        public void Transmit(ByteDataFragment data) {
            // do nothing
        }

        /// <summary>
        /// Implements IPoderosaSocket.
        /// </summary>
        public void Transmit(byte[] data, int offset, int length) {
            // do nothing
        }

        /// <summary>
        /// Implements IPoderosaSocket.
        /// </summary>
        public void Close() {
            _isClosed = true;
            if (Monitor.TryEnter(_pumpThreadSync)) {
                Monitor.PulseAll(_pumpThreadSync);
                Monitor.Exit(_pumpThreadSync);
            }
        }
    }

    /// <summary>
    /// Exception when MockSocket.FeedData was timedout
    /// </summary>
    internal class MockSocketTimeoutException : Exception {

        public MockSocketTimeoutException()
            : base() {
        }
    }

}
