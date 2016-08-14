/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.


 $Id: LibraryClient.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/
using System;
using Granados.IO;

namespace Granados {

    /// <summary>
    /// Connection event handler
    /// </summary>
    public interface ISSHConnectionEventHandler {
        /// <summary>
        /// Notifies SSH_MSG_DEBUG.
        /// </summary>
        /// <param name="alwaysDisplay">
        /// If true, the message should be displayed.
        /// Otherwise, it should not be displayed unless debugging information has been explicitly requested by the user.
        /// </param>
        /// <param name="message">a message text</param>
        void OnDebugMessage(bool alwaysDisplay, string message);

        /// <summary>
        /// Notifies SSH_MSG_IGNORE.
        /// </summary>
        /// <param name="data">data</param>
        void OnIgnoreMessage(byte[] data);

        /// <summary>
        /// Notifies unknown message.
        /// </summary>
        /// <param name="type">value of the message number field</param>
        /// <param name="data">packet image</param>
        void OnUnknownMessage(byte type, byte[] data);

        /// <summary>
        /// Notifies that an exception has occurred. 
        /// </summary>
        /// <param name="error">exception object</param>
        void OnError(Exception error);

        /// <summary>
        /// Notifies that the connection has been closed.
        /// </summary>
        void OnConnectionClosed();
    }

}

namespace Granados.SSH {

    /// <summary>
    /// A wrapper class of <see cref="ISSHConnectionEventHandler"/> for internal use.
    /// </summary>
    internal class SSHConnectionEventHandlerIgnoreErrorWrapper : ISSHConnectionEventHandler {

        private readonly ISSHConnectionEventHandler _coreHandler;

        public SSHConnectionEventHandlerIgnoreErrorWrapper(ISSHConnectionEventHandler handler) {
            _coreHandler = handler;
        }

        public void OnDebugMessage(bool alwaysDisplay, string message) {
            try {
                _coreHandler.OnDebugMessage(alwaysDisplay, message);
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }

        public void OnIgnoreMessage(byte[] data) {
            try {
                _coreHandler.OnIgnoreMessage(data);
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }

        public void OnUnknownMessage(byte type, byte[] data) {
            try {
                _coreHandler.OnUnknownMessage(type, data);
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }

        public void OnError(Exception error) {
            try {
                _coreHandler.OnError(error);
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }

        public void OnConnectionClosed() {
            try {
                _coreHandler.OnConnectionClosed();
            }
            catch (Exception e) {
                System.Diagnostics.Debug.WriteLine(e.Message);
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
        }
    }
}