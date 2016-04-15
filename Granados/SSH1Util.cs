/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: SSH1Util.cs,v 1.4 2011/10/27 23:21:56 kzmi Exp $
*/
using System;
using Granados.IO;
using Granados.Mono.Math;

namespace Granados.SSH1 {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class SSHServerInfo {
        public byte[] anti_spoofing_cookie;
        public int server_key_bits;
        public BigInteger server_key_public_exponent;
        public BigInteger server_key_public_modulus;
        public int host_key_bits;
        public BigInteger host_key_public_exponent;
        public BigInteger host_key_public_modulus;

        internal SSHServerInfo(SSHDataReader reader) {
            anti_spoofing_cookie = reader.Read(8); //first 8 bytes are cookie

            server_key_bits = reader.ReadInt32();
            server_key_public_exponent = reader.ReadMPInt();
            server_key_public_modulus = reader.ReadMPInt();
            host_key_bits = reader.ReadInt32();
            host_key_public_exponent = reader.ReadMPInt();
            host_key_public_modulus = reader.ReadMPInt();
        }

    }
}
