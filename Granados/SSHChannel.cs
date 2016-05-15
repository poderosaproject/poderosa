/*
 Copyright (c) 2016 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.
*/
using Granados.IO;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Granados.SSH {

    /// <summary>
    /// An interface of the class that can send data through the specific channel.
    /// </summary>
    /// <remarks>
    /// The concrete class is provided by the Granados.
    /// </remarks>
    public interface ISSHChannel {

        /// <summary>
        /// Local channel number.
        /// </summary>
        uint LocalChannel {
            get;
        }

        /// <summary>
        /// Remote channel number.
        /// </summary>
        uint RemoteChannel {
            get;
        }

        /// <summary>
        /// Channel type. (predefined type) 
        /// </summary>
        ChannelType ChannelType {
            get;
        }

        /// <summary>
        /// Channel type string. (actual channel type name)
        /// </summary>
        string ChannelTypeString {
            get;
        }

        /// <summary>
        /// true if this channel is open.
        /// </summary>
        bool IsOpen {
            get;
        }

        /// <summary>
        /// true if this channel is ready for use.
        /// </summary>
        bool IsReady {
            get;
        }

        /// <summary>
        /// Send window dimension change message.
        /// </summary>
        /// <remarks>
        /// In SSH1's interactive-session, this method will send SSH_CMSG_WINDOW_SIZE packet.
        /// In SSH1's other channel, this method will be ignored.
        /// In SSH2, this method will send SSH_MSG_CHANNEL_REQUEST "window-change" packet.
        /// </remarks>
        /// <param name="width">terminal width, columns</param>
        /// <param name="height">terminal height, rows</param>
        /// <param name="pixelWidth">terminal width, pixels</param>
        /// <param name="pixelHeight">terminal height, pixels</param>
        /// <exception cref="SSHChannelInvalidOperationException">the operation is not allowed.</exception>
        void ResizeTerminal(uint width, uint height, uint pixelWidth, uint pixelHeight);

        /// <summary>
        /// Send data.
        /// </summary>
        /// <remarks>
        /// In SSH1's interactive-session, this method will send SSH_CMSG_STDIN_DATA packet.
        /// Otherwise, this method will send SSH_MSG_CHANNEL_DATA packet.
        /// </remarks>
        /// <param name="data">data to send</param>
        /// <exception cref="SSHChannelInvalidOperationException">the operation is not allowed.</exception>
        void Send(DataFragment data);

        /// <summary>
        /// Send EOF.
        /// </summary>
        /// <remarks>
        /// In SSH1's interactive-session, this method will send SSH_CMSG_EOF packet.
        /// In SSH1's other channel, this method will be ignored.
        /// In SSH2, this method will send SSH_MSG_CHANNEL_EOF packet.
        /// </remarks>
        /// <exception cref="SSHChannelInvalidOperationException">the operation is not allowed.</exception>
        void SendEOF();

        /// <summary>
        /// Send Break. (SSH2, session channel only)
        /// </summary>
        /// <param name="breakLength">break-length in milliseconds</param>
        /// <returns>true if succeeded. false if the request failed.</returns>
        /// <exception cref="SSHChannelInvalidOperationException">the operation is not allowed.</exception>
        bool SendBreak(int breakLength);

        /// <summary>
        /// Close this channel.
        /// </summary>
        /// <remarks>
        /// After calling this method, all mothods of the <see cref="ISSHChannel"/> will be throw <see cref="SSHChannelInvalidOperationException"/>.
        /// </remarks>
        /// <remarks>
        /// If this method was called under the inappropriate channel state, the method call will be ignored silently.
        /// </remarks>
        void Close();
    }

    /// <summary>
    /// An interface of the channel object that can handle events about the specific channel.
    /// </summary>
    /// <remarks>
    /// The user of Granados needs to implement the concrete class of this interface.
    /// </remarks>
    public interface ISSHChannelEventHandler : IDisposable {

        // <>---+---> OnEstablished --+---> OnReady --->| OnData            | ----+
        //      |                     |                 | OnExtendedData    |     |
        //      |                     |                 | OnEOF             |     |
        //      |                     |                 | OnUnhandledPacket |     |
        //      |                     |                                           |
        //      +---------------------+---> OnRequestFailed ----------------------+---> OnClosing --> OnClosed

        /// <summary>
        /// Notifies that the channel has been established.
        /// </summary>
        /// <param name="data">channel type specific data</param>
        void OnEstablished(DataFragment data);

        /// <summary>
        /// Notifies that the channel is ready for use.
        /// </summary>
        void OnReady();

        /// <summary>
        /// Notifies received channel data. (SSH_MSG_CHANNEL_DATA)
        /// </summary>
        /// <param name="data">data fragment</param>
        void OnData(DataFragment data);

        /// <summary>
        /// Notifies received extended channel data. (SSH_MSG_CHANNEL_EXTENDED_DATA)
        /// </summary>
        /// <param name="type">data type code. (e.g. SSH_EXTENDED_DATA_STDERR)</param>
        /// <param name="data">data fragment</param>
        void OnExtendedData(uint type, DataFragment data);

        /// <summary>
        /// Notifies that the channel is going to close.
        /// </summary>
        /// <remarks>
        /// Note that this method may be called before <see cref="OnEstablished(DataFragment)"/> or <see cref="OnReady()"/> is called.
        /// </remarks>
        /// <param name="byServer">true if the server requested closing the channel.</param>
        void OnClosing(bool byServer);

        /// <summary>
        /// Notifies that the channel has been closed.
        /// </summary>
        /// <remarks>
        /// Note that this method may be called before <see cref="OnEstablished(DataFragment)"/> or <see cref="OnReady()"/> is called.
        /// </remarks>
        /// <param name="byServer">true if the server requested closing the channel.</param>
        void OnClosed(bool byServer);

        /// <summary>
        /// Notifies SSH_MSG_CHANNEL_EOF. (SSH2)
        /// </summary>
        void OnEOF();

        /// <summary>
        /// Notifies that the setup request has been failed.
        /// </summary>
        void OnRequestFailed();

        /// <summary>
        /// Notifies that an exception has occurred.
        /// </summary>
        /// <param name="error">exception object</param>
        void OnError(Exception error);

        /// <summary>
        /// Notifies unhandled packet.
        /// </summary>
        /// <param name="packetType">a message number</param>
        /// <param name="data">packet image excluding message number field and channel number field.</param>
        void OnUnhandledPacket(byte packetType, DataFragment data);

    }

    /// <summary>
    /// A channel event handler class that do nothing.
    /// </summary>
    internal class NullSSHChannelHandler : ISSHChannelEventHandler {

        public void OnEstablished(DataFragment data) {
        }

        public void OnReady() {
        }

        public void OnData(DataFragment data) {
        }

        public void OnExtendedData(uint type, DataFragment data) {
        }

        public void OnClosing(bool byServer) {
        }

        public void OnClosed(bool byServer) {
        }

        public void OnEOF() {
        }

        public void OnRequestFailed() {
        }

        public void OnError(Exception error) {
        }

        public void OnUnhandledPacket(byte packetType, DataFragment data) {
        }

        public void Dispose() {
        }
    }

    /// <summary>
    /// A function type that creates a new channel handler object.
    /// </summary>
    public delegate THandler SSHChannelEventHandlerCreator<THandler>(ISSHChannel channel)
                where THandler : ISSHChannelEventHandler;

    /// <summary>
    /// An exception that indicates the operation is not allowed under the current state of the channel.
    /// </summary>
    public class SSHChannelInvalidOperationException : SSHException {
        public SSHChannelInvalidOperationException(string message) : base(message) {
        }
    }
    
    /// <summary>
    /// An internal class to manage the pair of the <see cref="ISSHChannel"/> and <see cref="ISSHChannelEventHandler"/>.
    /// </summary>
    internal class SSHChannelCollection {

        private int _channelNumber = -1;

        private class ChannelEntry {
            public readonly ISSHChannel Channel;
            public readonly ISSHChannelEventHandler EventHandler;

            public ChannelEntry(ISSHChannel channel, ISSHChannelEventHandler handler) {
                this.Channel = channel;
                this.EventHandler = handler;
            }
        }

        private readonly ConcurrentDictionary<uint, ChannelEntry> _dic = new ConcurrentDictionary<uint, ChannelEntry>();

        /// <summary>
        /// Constructor
        /// </summary>
        public SSHChannelCollection() {
        }

        /// <summary>
        /// Get the new channel number.
        /// </summary>
        /// <returns>channel number</returns>
        public uint GetNewChannelNumber() {
            return (uint)Interlocked.Increment(ref _channelNumber);
        }

        /// <summary>
        /// Add new channel.
        /// </summary>
        /// <param name="channelOperator">channel operator</param>
        /// <param name="channelHandler">channel handler</param>
        public void Add(ISSHChannel channelOperator, ISSHChannelEventHandler channelHandler) {
            uint channelNumber = channelOperator.LocalChannel;
            var entry = new ChannelEntry(channelOperator, channelHandler);
            _dic.TryAdd(channelNumber, entry);
        }

        /// <summary>
        /// Remove channel.
        /// </summary>
        /// <param name="channelOperator">channel operator</param>
        public void Remove(ISSHChannel channelOperator) {
            uint channelNumber = channelOperator.LocalChannel;
            ChannelEntry entry;
            _dic.TryRemove(channelNumber, out entry);
        }

        /// <summary>
        /// Find channel operator by a local channel number.
        /// </summary>
        /// <param name="channelNumber">a local channel number</param>
        /// <returns>channel operator object, or null if no channel number didn't match.</returns>
        public ISSHChannel FindOperator(uint channelNumber) {
            ChannelEntry entry;
            if (_dic.TryGetValue(channelNumber, out entry)) {
                return entry.Channel;
            }
            return null;
        }

        /// <summary>
        /// Find channel handler by a local channel number.
        /// </summary>
        /// <param name="channelNumber">a local channel number</param>
        /// <returns>channel handler object, or null if no channel number didn't match.</returns>
        public ISSHChannelEventHandler FindHandler(uint channelNumber) {
            ChannelEntry entry;
            if (_dic.TryGetValue(channelNumber, out entry)) {
                return entry.EventHandler;
            }
            return null;
        }
    }

}
