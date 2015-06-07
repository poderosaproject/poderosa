/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: DataLoadBenchmark.cs,v 1.2 2012/05/20 09:03:44 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;

using Poderosa.Commands;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.Terminal;

namespace Poderosa.Benchmark {

    /// <summary>
    /// XTerm Benchmark
    /// </summary>
    internal class DataLoadBenchmark : AbstractTerminalBenchmark {

        private readonly MockSocket _socket;
        private readonly MockTerminalConnection _connection;
        private readonly byte[] _data;
        private readonly int _repeat;
        private ITerminalEmulatorOptions _options;
        private ITerminalSession _session;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="target">target object</param>
        /// <param name="data">data to send to the terminal</param>
        /// <param name="repeat">repeat count to send data</param>
        public DataLoadBenchmark(ICommandTarget target, byte[] data, int repeat)
            : base(target) {
            _data = data;
            _repeat = repeat;
            _socket = new MockSocket();
            _connection = new MockTerminalConnection("xterm", _socket);
        }

        /// <summary>
        /// Returns terminal's caption
        /// </summary>
        protected override string GetTerminalCaption() {
            return "XTerm Benchmark";
        }

        /// <summary>
        /// Returns ITerminalConnection instance from the derived class
        /// </summary>
        /// <returns>connection object</returns>
        protected override ITerminalConnection GetTerminalConnection() {
            return _connection;
        }

        /// <summary>
        /// Starts benchmark thread int the derived class
        /// </summary>
        protected override void StartBenchmarkThread(ITerminalEmulatorOptions options, ITerminalSession session) {
            _options = options;
            _session = session;
            Thread thread = new Thread(new ThreadStart(BenchmarkThread));
            thread.Name = "Poderosa.Benchmark.DataLoadBenchmark";
            thread.Start();
        }

        private const int TIMEOUT = 5000;
        private const string NEWLINE = "\r\n";

        /// <summary>
        /// Benchmark thread
        /// </summary>
        private void BenchmarkThread() {
            Thread.Sleep(2000);

            try {
                OnPaintTimeStatistics onPaintStats = new OnPaintTimeStatistics();
                _session.TerminalControl.SetOnPaintTimeObserver(
                    delegate(Stopwatch s) {
                        onPaintStats.Update(s);
                    }
                );

                SockWriteLine("Start XTerm Benchmark.");

                Stopwatch swTotal = Stopwatch.StartNew();

                const int DATA_CHUNK_SIZE = 200;

                for (int i = 0; i < _repeat; i++) {
                    _socket.FeedData(BenchmarkDataGenerator(DATA_CHUNK_SIZE), TIMEOUT);
                }

                swTotal.Stop();

                SockWriteLine("End XTerm Benchmark.");

                _session.TerminalControl.SetOnPaintTimeObserver(null);

                TerminalDocument doc = (TerminalDocument)_session.Terminal.IDocument.GetAdapter(typeof(TerminalDocument));

                SockWriteLine("---------------------------------------");
                SockWriteLine(String.Format(NumberFormatInfo.InvariantInfo,
                            "Terminal Size : {0} x {1}", doc.TerminalWidth, doc.TerminalHeight));
                SockWriteLine(String.Format(NumberFormatInfo.InvariantInfo,
                            "Terminal Buffer Size : {0}", _options.TerminalBufferSize));
                SockWriteLine("---------------------------------------");
                SockWriteLine(String.Format("OnPaint {0} samples", onPaintStats.GetSampleCount()));
                SockWriteLine(String.Format("        Max  {0} msec", onPaintStats.GetMaxTimeMilliseconds()));
                SockWriteLine(String.Format("        Min  {0} msec", onPaintStats.GetMinTimeMilliseconds()));
                SockWriteLine(String.Format("        Avg  {0} msec", onPaintStats.GetAverageTimeMilliseconds()));
                SockWriteLine("---------------------------------------");
                ReportBenchmark("Total          ", swTotal);
                SockWriteLine("---------------------------------------");
            }
            catch (MockSocketTimeoutException) {
            }
        }

        private void SockWriteLine() {
            _socket.FeedData(new byte[][] { Encoding.UTF8.GetBytes(NEWLINE) }, TIMEOUT);
        }

        private void SockWriteLine(string text) {
            _socket.FeedData(new byte[][] { Encoding.UTF8.GetBytes(text + NEWLINE) }, TIMEOUT);
        }

        private void ReportBenchmark(string title, Stopwatch w) {
            SockWriteLine(String.Format(NumberFormatInfo.InvariantInfo,
                "{0} : {1}.{2:D3} sec", title, w.ElapsedMilliseconds / 1000, w.ElapsedMilliseconds % 1000));
        }

        private IEnumerable<byte[]> BenchmarkDataGenerator(int chunkSize) {
            byte[] data = _data;
            int offset = 0;
            int dataLength = data.Length;
            byte[] chunk = new byte[chunkSize];
            byte[] lastChunk = new byte[dataLength % chunkSize];

            while (offset < dataLength) {
                int remain = dataLength - offset;
                if (remain >= chunkSize) {
                    Buffer.BlockCopy(data, offset, chunk, 0, chunkSize);
                    offset += chunkSize;
                    yield return chunk;
                }
                else {
                    Buffer.BlockCopy(data, offset, lastChunk, 0, remain);
                    offset += remain;
                    yield return lastChunk;
                }
            }
        }

    }
}
