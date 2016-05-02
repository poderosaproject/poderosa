/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.


 $Id: LibraryClient.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/
using System;

namespace Granados {

    //port forwarding check result
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public struct PortForwardingCheckResult {
        /**
         * if you allow this request, set 'allowed' to true.
         */
        public bool allowed;

        /**
         * if you allow this request, you must set 'channel' for this request. otherwise, 'channel' is ignored
         */
        public ISSHChannelEventReceiver channel;

        /**
         * if you disallow this request, you can set 'reason_code'.
            The following reason codes are defined:

            #define SSH_OPEN_ADMINISTRATIVELY_PROHIBITED    1
            #define SSH_OPEN_CONNECT_FAILED                 2
            #define SSH_OPEN_UNKNOWN_CHANNEL_TYPE           3
            #define SSH_OPEN_RESOURCE_SHORTAGE              4
         */
        public int reason_code;

        /**
         * if you disallow this request, you can set 'reason_message'. this message can contain only ASCII characters.
         */
        public string reason_message;
    }

    /// <summary>
    /// Connection specific receiver
    /// </summary>
    public interface ISSHConnectionEventReceiver {
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
        /// <param name="error">exception obuject</param>
        void OnError(Exception error);

        /// <summary>
        /// Notifies that the connection has been closed.
        /// </summary>
        void OnConnectionClosed();

        /// <summary>
        /// In the keyboard-interactive authentication, this method will be called
        /// to display some prompt texts.
        /// </summary>
        /// <param name="prompts">prompt texts</param>
        void OnAuthenticationPrompt(string[] prompts);

        /// <summary>
        /// Check new channel in the server-to-client port forwarding.
        /// </summary>
        /// <remarks>
        /// The arguments are field value of the SSH_MSG_CHANNEL_OPEN "forwarded-tcpip" message from the server.
        /// </remarks>
        /// <param name="remoteHost">address that was connected</param>
        /// <param name="remotePort">port that was connected</param>
        /// <param name="originatorIp">originator IP address</param>
        /// <param name="originatorPort">originator port</param>
        /// <returns>informations about acceptance or denial of this port-forwarding.</returns>
        PortForwardingCheckResult CheckPortForwardingRequest(string remoteHost, int remotePort, string originatorIp, int originatorPort);

        /// <summary>
        /// Notifies new channel in the server-to-client port forwarding.
        /// </summary>
        /// <remarks>
        /// This method will be called after <see cref="CheckPortForwardingRequest(string,int,string,int)"/> accepted the new channel.
        /// </remarks>
        /// <param name="receiver"><see cref="PortForwardingCheckResult.channel"/> which was returned by <see cref="CheckPortForwardingRequest(string,int,string,int)"/>.</param>
        /// <param name="channel">new channel object</param>
        void EstablishPortforwarding(ISSHChannelEventReceiver receiver, SSHChannel channel);
    }

    /// <summary>
    /// Channel specific receiver 
    /// </summary>
    /// <exclude/>
    public interface ISSHChannelEventReceiver {
        void OnData(byte[] data, int offset, int length);
        void OnExtendedData(int type, byte[] data);
        void OnChannelClosed();
        void OnChannelEOF();
        void OnChannelError(Exception error);
        void OnChannelReady();
        void OnMiscPacket(byte packet_type, byte[] data, int offset, int length);
    }

}
