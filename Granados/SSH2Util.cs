/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

  I implemented this algorithm with reference to following products and books though the algorithm is known publicly.
    * MindTerm ( AppGate Network Security )
    * Applied Cryptography ( Bruce Schneier )

 $Id: SSH2Util.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/
using System;
using Granados.PKI;

namespace Granados.SSH2 {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum DisconnectReason {
        SSH_DISCONNECT_HOST_NOT_ALLOWED_TO_CONNECT = 1,
        SSH_DISCONNECT_PROTOCOL_ERROR = 2,
        SSH_DISCONNECT_KEY_EXCHANGE_FAILED = 3,
        SSH_DISCONNECT_RESERVED = 4,
        SSH_DISCONNECT_MAC_ERROR = 5,
        SSH_DISCONNECT_COMPRESSION_ERROR = 6,
        SSH_DISCONNECT_SERVICE_NOT_AVAILABLE = 7,
        SSH_DISCONNECT_PROTOCOL_VERSION_NOT_SUPPORTED = 8,
        SSH_DISCONNECT_HOST_KEY_NOT_VERIFIABLE = 9,
        SSH_DISCONNECT_CONNECTION_LOST = 10,
        SSH_DISCONNECT_BY_APPLICATION = 11,
        SSH_DISCONNECT_TOO_MANY_CONNECTIONS = 12,
        SSH_DISCONNECT_AUTH_CANCELLED_BY_USER = 13,
        SSH_DISCONNECT_NO_MORE_AUTH_METHODS_AVAILABLE = 14,
        SSH_DISCONNECT_ILLEGAL_USER_NAME = 15
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum ChannelOpenFailureReason {
        SSH_OPEN_ADMINISTRATIVELY_PROHIBITED = 1,
        SSH_OPEN_CONNECT_FAILED = 2,
        SSH_OPEN_UNKNOWN_CHANNEL_TYPE = 3,
        SSH_OPEN_RESOURCE_SHORTAGE = 4
    }

    internal static class SSH2Util {
        public static string PublicKeyAlgorithmName(PublicKeyAlgorithm algorithm) {
            switch (algorithm) {
                case PublicKeyAlgorithm.DSA:
                    return "ssh-dss";
                case PublicKeyAlgorithm.RSA:
                    return "ssh-rsa";
                default:
                    throw new SSHException("unknown HostKeyAlgorithm " + algorithm);
            }
        }
    }
}
