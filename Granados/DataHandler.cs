/*
 Copyright (c) 2016 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.
*/

//#define DEBUG_SYNCHRONOUSPACKETHANDLER

using System;
using System.Threading;

namespace Granados.IO {

    /// <summary>
    /// <see cref="IDataHandler"/> adapter which redirects to the delegate methods.
    /// </summary>
    internal class DataHandlerAdapter : IDataHandler {

        private readonly Action<DataFragment> _onData;
        private readonly Action _onClosed;
        private readonly Action<Exception> _onError;

        public DataHandlerAdapter(Action<DataFragment> onData, Action onClosed, Action<Exception> onError) {
            _onData = onData;
            _onClosed = onClosed;
            _onError = onError;
        }

        public void OnData(DataFragment data) {
            _onData(data);
        }

        public void OnClosed() {
            _onClosed();
        }

        public void OnError(Exception error) {
            _onError(error);
        }
    }

    /// <summary>
    /// A base class for the synchronization of sending/receiving packets.
    /// </summary>
    /// <typeparam name="PacketType">type of the packet object.</typeparam>
    internal abstract class AbstractSynchronousPacketHandler<PacketType> : IDataHandler {

        // lock object for preventing simultaneous wait by multiple threads
        private readonly object _waitResponseLock = new object();
        // lock object for synchronization of sending packet
        private readonly object _syncSend = new object();
        // lock object for synchronization of receiving packet
        private readonly object _syncReceive = new object();

        private readonly IGranadosSocket _socket;
        private readonly IDataHandler _handler;

        private bool _waitingResponse;
        private DataFragment _receivedPacket;

        /// <summary>
        /// Gets the binary image of the packet to be sent.
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>binary image of the packet</returns>
        protected abstract DataFragment GetPacketImage(PacketType packet);

        /// <summary>
        /// Gets the packet type name of the packet to be sent. (for debugging)
        /// </summary>
        /// <param name="packet">a packet object</param>
        /// <returns>packet name.</returns>
        protected abstract String GetMessageName(PacketType packet);

        /// <summary>
        /// Gets the packet type name of the received packet. (for debugging)
        /// </summary>
        /// <param name="packet">a packet image</param>
        /// <returns>packet name.</returns>
        protected abstract String GetMessageName(DataFragment packet);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="socket">socket object for sending packets.</param>
        /// <param name="handler">the next handler received packets are redirected to.</param>
        protected AbstractSynchronousPacketHandler(IGranadosSocket socket, IDataHandler handler) {
            _socket = socket;
            _handler = handler;
        }

        /// <summary>
        /// Sends a packet.
        /// </summary>
        /// <param name="packet">a packet object.</param>
        public void Send(PacketType packet) {
            lock (_syncSend) {
#if DEBUG_SYNCHRONOUSPACKETHANDLER
                System.Diagnostics.Debug.WriteLine("S <-- [{0}]", new object[] { GetMessageName(packet) });
#endif
                _socket.Write(GetPacketImage(packet));
            }
        }

        /// <summary>
        /// Sends a packet, then waits a response packet.
        /// </summary>
        /// <param name="packet">a packet object to send</param>
        /// <param name="msecTimeout">timeout for waiting a response packet in milliseconds.</param>
        /// <returns>a response packet image. or null if no response has been received.</returns>
        public DataFragment SendAndWaitResponse(PacketType packet, int msecTimeout) {
            lock (_waitResponseLock) {
                lock (_syncReceive) {
                    Send(packet);
                    _receivedPacket = null;
                    _waitingResponse = true;
                    bool signaled = Monitor.Wait(_syncReceive, msecTimeout);
                    _waitingResponse = false;
                    DataFragment rcvPacket = signaled ? _receivedPacket : null;
                    _receivedPacket = null;
#if DEBUG_SYNCHRONOUSPACKETHANDLER
                    if (rcvPacket != null) {
                        System.Diagnostics.Debug.WriteLine("      [{0}] {1} bytes --> retrieved", GetMessageName(rcvPacket), rcvPacket.Length);
                    }
#endif
                    return rcvPacket;
                }
            }
        }

        /// <summary>
        /// Waits a received packet.
        /// </summary>
        /// <param name="msecTimeout">timeout for waiting a packet in milliseconds.</param>
        /// <returns>the received packet image. or null if no packet has been received.</returns>
        public DataFragment WaitResponse(int msecTimeout) {
            lock (_waitResponseLock) {
                lock (_syncReceive) {
                    _receivedPacket = null;
                    _waitingResponse = true;
                    bool signaled = Monitor.Wait(_syncReceive, msecTimeout);
                    _waitingResponse = false;
                    DataFragment rcvPacket = signaled ? _receivedPacket : null;
                    _receivedPacket = null;
#if DEBUG_SYNCHRONOUSPACKETHANDLER
                    if (rcvPacket != null) {
                        System.Diagnostics.Debug.WriteLine("      [{0}] {1} bytes --> retrieved", GetMessageName(rcvPacket), rcvPacket.Length);
                    }
#endif
                    return rcvPacket;
                }
            }
        }

        /// <summary>
        /// Handles received packet.
        /// </summary>
        /// <param name="data">packet image</param>
        public void OnData(DataFragment data) {
            lock (_syncReceive) {
                if (_waitingResponse) {
                    _receivedPacket = data;
#if DEBUG_SYNCHRONOUSPACKETHANDLER
                    System.Diagnostics.Debug.WriteLine("S --> [{0}] {1} bytes (wait)", GetMessageName(data), data.Length);
#endif
                    Monitor.PulseAll(_syncReceive);
                }
                else {
#if DEBUG_SYNCHRONOUSPACKETHANDLER
                    System.Diagnostics.Debug.WriteLine("S --> [{0}] {1} bytes --> OnData", GetMessageName(data), data.Length);
#endif
                    _handler.OnData(data);
                }
            }
        }

        /// <summary>
        /// Handles closed event.
        /// </summary>
        public void OnClosed() {
            _handler.OnClosed();
        }

        /// <summary>
        /// Handles error event.
        /// </summary>
        public void OnError(Exception error) {
            _handler.OnError(error);
        }
    }

}
