// Copyright 2011-2019 The Poderosa Project.
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
#if UNITTEST
using Granados.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Granados.Poderosa.SCP {

    [TestFixture]
    public class SCPChannelStreamTest {

        private SCPChannelStream _stream;

        private const int INITAL_BUFFER_SIZE = 10;

        [SetUp]
        public void Setup() {
            _stream = new SCPChannelStream();
            _stream.SetBuffer(INITAL_BUFFER_SIZE);  // for testing
            _stream.OpenForTest(new DummySSHChannel());
        }

        //-----------------------------------------------
        // Open()
        //-----------------------------------------------

        [Test]
        public void Opened_Success() {
            Assert.AreEqual("Opened", _stream.Status);
        }

        [Test]
        public void Open_AlreadyOpened() {
            Assert.Catch<SCPClientInvalidStatusException>(
                () => _stream.Open(null, null, 0)
            );
        }

        [Test]
        public void Open_AlreadyClosed() {
            _stream.Close();
            Assert.Catch<SCPClientInvalidStatusException>(
                () => _stream.Open(null, null, 0)
            );
        }

        //-----------------------------------------------
        // Close()
        //-----------------------------------------------

        [Test]
        public void Close_Success() {
            _stream.Close();
            Assert.AreEqual("Closed", _stream.Status);
        }

        [Test]
        public void Close_AlreadyClosed() {
            _stream.Close();
            _stream.Close();
            Assert.AreEqual("Closed", _stream.Status);
        }

        [Test]
        public void Close_OpenedButError() {
            _stream.ChannelEventHandler.OnError(new Exception("Channel error"));
            Assert.AreEqual("Error", _stream.Status);
            _stream.Close();
            Assert.AreEqual("Closed", _stream.Status);
        }

        [Test]
        public void Close_NotOpened() {
            _stream = new SCPChannelStream();
            _stream.Close();
            Assert.AreEqual("NotOpened", _stream.Status);
        }

        //-----------------------------------------------
        // ReadByte()
        //-----------------------------------------------

        [Test]
        public void ReadByte_Timeout() {
            Assert.Catch<SCPClientTimeoutException>(
                () => _stream.ReadByte(1000)
            );
        }

        [Test]
        public void ReadByte_NotOpened() {
            _stream = new SCPChannelStream();
            Assert.AreEqual("NotOpened", _stream.Status);
            Assert.Catch<SCPClientInvalidStatusException>(
                () => _stream.ReadByte(1000)
            );
        }

        [Test]
        public void ReadByte_AlreadyClosed() {
            _stream.Close();
            Assert.AreEqual("Closed", _stream.Status);
            Assert.Catch<SCPClientInvalidStatusException>(
                () => _stream.ReadByte(1000)
            );
        }

        [Test]
        public void ReadByte_OpenedButError() {
            _stream.ChannelEventHandler.OnError(new Exception("Channel error"));
            Assert.AreEqual("Error", _stream.Status);
            Assert.Catch<SCPClientInvalidStatusException>(
                () => _stream.ReadByte(1000)
            );
        }

        [Test]
        public void ReadByte_HasData() {
            _stream.SetBufferContent(new byte[] { 0xaa });
            byte b = _stream.ReadByte(1000);
            Assert.AreEqual((byte)0xaa, b);
        }

        [Test]
        public void ReadByte_WaitForData() {
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleData(1);
            tester.StartAfter(200);
            byte b = _stream.ReadByte(1000);
            tester.WaitForCompletion();
            Assert.AreEqual((byte)1, b);
        }

        [Test]
        public void ReadByte_ErrorWhileWaiting() {
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleChannelError();
            tester.StartAfter(200);
            try {
                Assert.Catch<SCPClientInvalidStatusException>(
                    () => _stream.ReadByte(1000)
                );
            }
            finally {
                tester.WaitForCompletion();
            }
        }

        //-----------------------------------------------
        // Read()
        //-----------------------------------------------

        [Test]
        public void Read_BufferHasOneByte() {
            _stream.SetBufferContent(new byte[] { 0xaa });
            byte[] buff = new byte[100];
            int readLength = _stream.Read(buff, 1000);
            Assert.AreEqual(1, readLength);
            Assert.AreEqual((byte)0xaa, buff[0]);
        }

        [Test]
        public void Read_BufferHasSomeBytes() {
            _stream.SetBufferContent(new byte[] { 0xaa, 0xbb, 0xcc });
            byte[] buff = new byte[100];
            int readLength = _stream.Read(buff, 1000);
            Assert.AreEqual(3, readLength);
            Assert.AreEqual((byte)0xaa, buff[0]);
            Assert.AreEqual((byte)0xbb, buff[1]);
            Assert.AreEqual((byte)0xcc, buff[2]);
        }

        [Test]
        public void Read_WithMaxLength_BufferHasSomeBytes() {
            _stream.SetBufferContent(new byte[] { 0xaa, 0xbb, 0xcc });

            byte[] buff = new byte[100];
            int readLength = _stream.Read(buff, 2, 1000);
            Assert.AreEqual(2, readLength);
            Assert.AreEqual((byte)0xaa, buff[0]);
            Assert.AreEqual((byte)0xbb, buff[1]);
            Assert.AreEqual((byte)0, buff[2]);

            buff = new byte[100];
            readLength = _stream.Read(buff, 2, 1000);
            Assert.AreEqual(1, readLength);
            Assert.AreEqual((byte)0xcc, buff[0]);
            Assert.AreEqual((byte)0, buff[1]);
        }

        [Test]
        public void Read_Timeout() {
            byte[] buff = new byte[100];
            Assert.Catch<SCPClientTimeoutException>(
                () => _stream.Read(buff, 1000)
            );
        }

        [Test]
        public void Read_WaitData_ReadAll() {
            byte[] buff = new byte[100];
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleData(5);
            tester.StartAfter(200);
            int readLength = _stream.Read(buff, 1000);
            tester.WaitForCompletion();
            Assert.AreEqual(5, readLength);
            Assert.AreEqual((byte)1, buff[0]);
            Assert.AreEqual((byte)2, buff[1]);
            Assert.AreEqual((byte)3, buff[2]);
            Assert.AreEqual((byte)4, buff[3]);
            Assert.AreEqual((byte)5, buff[4]);
        }

        [Test]
        public void Read_WaitData_ReadPartial() {
            byte[] buff = new byte[2];
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleData(5); // add [ 1, 2, 3, 4, 5 ]
            tester.StartAfter(200);
            int readLength = _stream.Read(buff, 1000);
            tester.WaitForCompletion();
            Assert.AreEqual(2, readLength);
            CollectionAssert.AreEqual(new byte[] { 1, 2, }, buff);
            // remained data is [ 3, 4, 5 ]
            CollectionAssert.AreEqual(new byte[] { 3, 4, 5, }, _stream.DataBuffer);
        }

        [Test]
        public void Read_ErrorWhileWaiting() {
            byte[] buff = new byte[100];
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleChannelError();
            tester.StartAfter(200);
            try {
                Assert.Catch<SCPClientInvalidStatusException>(
                    () => _stream.Read(buff, 1000)
                );
            }
            finally {
                tester.WaitForCompletion();
            }
        }

        //-----------------------------------------------
        // ReadUntil()
        //-----------------------------------------------

        [Test]
        public void ReadUntil_Timeout() {
            Assert.Catch<SCPClientTimeoutException>(
                () => _stream.ReadUntil(0xff, 1000)
            );
        }

        [Test]
        public void ReadUntil_BufferHasData_Timeout() {
            _stream.SetBufferContent(new byte[] { 1, 2, 3, 4, });
            Assert.Catch<SCPClientTimeoutException>(
                () => _stream.ReadUntil(0xff, 1000)
            );
        }

        [Test]
        public void ReadUntil_BufferHasData_Success() {
            _stream.SetBufferContent(new byte[] { 1, 2, 3, 4, });
            byte[] buff = _stream.ReadUntil(3, 1000);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, }, buff);
        }

        [Test]
        public void ReadUntil_BufferHasData_WaitMore_Success() {
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleData(4);
            tester.ScheduleData(4);
            tester.ScheduleData(4);
            tester.StartAfter(200);
            byte[] buff = _stream.ReadUntil(9, 1000);
            tester.WaitForCompletion();
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, }, buff);
        }

        [Test]
        public void ReadUntil_ErrorWhileWaiting() {
            StreamTester tester = new StreamTester(_stream);
            tester.ScheduleData(6);
            tester.ScheduleChannelError();
            tester.StartAfter(200);
            try {
                Assert.Catch<SCPClientInvalidStatusException>(
                    () => _stream.ReadUntil(10, 1000)
                );
            }
            finally {
                tester.WaitForCompletion();
            }
        }
    }

    /// <summary>
    /// Schedule OnData() event and execute them.
    /// After each OnData() event, status of internal buffer of SCPChannelStream is recorded.
    /// </summary>
    internal class StreamTester {

        private enum ScheduledEventType {
            OnData,
            OnChannelError,
        }

        private class ScheduledEvent {
            public readonly ScheduledEventType Type;
            public readonly int Size;

            public ScheduledEvent(ScheduledEventType type, int size) {
                this.Type = type;
                this.Size = size;
            }
        }

        private readonly SCPChannelStream _stream;

        private int _nextValue = 1;

        private readonly List<ScheduledEvent> _schedule = new List<ScheduledEvent>();

        private Thread _thread;

        public StreamTester(SCPChannelStream stream) {
            this._stream = stream;
        }

        /// <summary>
        /// Secdule OnData event.
        /// Passed data contains serial values like (byte)0x1, (byte)0x2, (byte)0x3 ...
        /// </summary>
        /// <param name="size"></param>
        public void ScheduleData(int size) {
            _schedule.Add(new ScheduledEvent(ScheduledEventType.OnData, size));
        }

        /// <summary>
        /// Schedule channel error.
        /// </summary>
        public void ScheduleChannelError() {
            _schedule.Add(new ScheduledEvent(ScheduledEventType.OnChannelError, 0));
        }

        public void StartAfter(int delay) {
            StartCore(delay);
        }

        public void StartAndWait() {
            StartCore(0);
            WaitForCompletion();
        }

        public void WaitForCompletion() {
            _thread.Join();
        }

        private void StartCore(int delay) {
            _thread = new Thread((ThreadStart)delegate() {
                if (delay > 0) {
                    Thread.Sleep(delay);
                }
                foreach (ScheduledEvent ev in _schedule) {
                    if (ev.Type == ScheduledEventType.OnData) {
                        byte[] data = new byte[ev.Size + 10];
                        for (int i = 0; i < ev.Size; i++) {
                            data[10 + i] = (byte)_nextValue++;
                        }
                        _stream.ChannelEventHandler.OnData(new DataFragment(data, 10, ev.Size));
                        Thread.Sleep(200);  // process Read()
                    }
                    else if (ev.Type == ScheduledEventType.OnChannelError) {
                        _stream.ChannelEventHandler.OnError(new Exception("Channel error"));
                        Thread.Sleep(200);  // process Read()
                    }
                }
                _schedule.Clear();
            });
            _thread.Start();
        }
    }

    internal class DummySSHChannel : ISSHChannel {

        public DummySSHChannel() {
        }

        public uint LocalChannel {
            get {
                return 100;
            }
        }

        public uint RemoteChannel {
            get {
                return 200;
            }
        }

        public ChannelType ChannelType {
            get {
                return ChannelType.Session;
            }
        }

        public string ChannelTypeString {
            get {
                return "session";
            }
        }

        public bool IsOpen {
            get {
                return true;
            }
        }

        public bool IsReady {
            get {
                return true;
            }
        }

        public int MaxChannelDatagramSize {
            get {
                return 16384;
            }
        }

        public void ResizeTerminal(uint width, uint height, uint pixelWidth, uint pixelHeight) {
        }

        public bool WaitReady() {
            return true;
        }

        public void Send(DataFragment data) {
        }

        public void SendEOF() {
        }

        public bool SendBreak(int breakLength) {
            return true;
        }

        public void Close() {
        }
    }

}
#endif
