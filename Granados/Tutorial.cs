/*s
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.
 This file is a part of the Granados SSH Client Library that is subject to
 the license included in the distributed package.
 You may not use this file except in compliance with the license.

 $Id: Tutorial.cs,v 1.2 2011/10/27 23:21:56 kzmi Exp $
*/
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Globalization;

using Granados.Crypto;
using Granados.IO;
using Granados.SSH1;
using Granados.SSH2;
using Granados.Util;
using Granados.PKI;

namespace Granados.Tutorial {
#if ENABLE_TUTORIAL

    /**
     * Granados Tutorial
     *   To learn the usage of Granados, please read the code in this file.
     */
    /// <exclude/>
    class Tutorial {
        private static SSHConnection _conn;

        [STAThread]
        static void Main(string[] args) {

            //NOTE: modify this number to run these samples!
            int tutorial = 5;

            if (tutorial == 0)
                GenerateRSAKey();
            else if (tutorial == 1)
                GenerateDSAKey();
            else if (tutorial == 2)
                ConnectAndOpenShell();
            else if (tutorial == 3)
                ConnectSSH2AndPortforwarding();
            else if (tutorial == 4)
                ScpCommand(args);
            else if (tutorial == 5)
                AgentForward();
        }

        //Tutorial: Generating a new RSA key for user authentication
        private static void GenerateRSAKey() {
            //RSA KEY GENERATION TEST
            byte[] testdata = Encoding.ASCII.GetBytes("CHRISTIAN VIERI");
            RSAKeyPair kp = RSAKeyPair.GenerateNew(2048, new Random());

            //sign and verify test
            byte[] sig = kp.Sign(testdata);
            kp.Verify(sig, testdata);

            //export / import test
            SSH2UserAuthKey key = new SSH2UserAuthKey(kp);
            key.WritePublicPartInOpenSSHStyle(new FileStream("newrsakey.pub", FileMode.Create));
            key.WritePrivatePartInSECSHStyleFile(new FileStream("newrsakey.bin", FileMode.Create), "comment", "passphrase");
            //read test
            SSH2UserAuthKey newpk = SSH2UserAuthKey.FromSECSHStyleFile("newrsakey.bin", "passphrase");
        }

        //Tutorial: Generating a new DSA key for user authentication
        private static void GenerateDSAKey() {
            //DSA KEY GENERATION TEST
            byte[] testdata = Encoding.ASCII.GetBytes("CHRISTIAN VIERI");
            DSAKeyPair kp = DSAKeyPair.GenerateNew(2048, new Random());

            //sign and verify test
            byte[] sig = kp.Sign(testdata);
            kp.Verify(sig, testdata);

            //export / import test
            SSH2UserAuthKey key = new SSH2UserAuthKey(kp);
            key.WritePublicPartInOpenSSHStyle(new FileStream("newdsakey.pub", FileMode.Create));
            key.WritePrivatePartInSECSHStyleFile(new FileStream("newrsakey.bin", FileMode.Create), "comment", "passphrase");
            //read test
            SSH2UserAuthKey newpk = SSH2UserAuthKey.FromSECSHStyleFile("newrsakey.bin", "passphrase");
        }

        //Tutorial: Connecting to a host and opening a shell
        private static void ConnectAndOpenShell() {
            SSHConnectionParameter f = new SSHConnectionParameter();
            f.EventTracer = new Tracer(); //to receive detailed events, set ISSHEventTracer
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            f.Protocol = SSHProtocol.SSH2; //this sample works on both SSH1 and SSH2
            string host_ip = "172.22.1.15"; //<--!!! [TO USERS OF Granados]
            f.UserName = "okajima";               //<--!!! if you try this sample, edit these values for your environment!
            string password = "aaa";
            s.Connect(new IPEndPoint(IPAddress.Parse(host_ip), 22)); //22 is the default SSH port

            //former algorithm is given priority in the algorithm negotiation
            f.PreferableHostKeyAlgorithms = new PublicKeyAlgorithm[] { PublicKeyAlgorithm.RSA, PublicKeyAlgorithm.DSA };
            f.PreferableCipherAlgorithms = new CipherAlgorithm[] { CipherAlgorithm.Blowfish, CipherAlgorithm.TripleDES };
            f.WindowSize = 0x1000; //this option is ignored with SSH1
            Reader reader = new Reader(); //simple event receiver

            AuthenticationType at = AuthenticationType.PublicKey;
            f.AuthenticationType = at;

            if (at == AuthenticationType.KeyboardInteractive) {
                //Creating a new SSH connection over the underlying socket
                _conn = SSHConnection.Connect(f, reader, s);
                reader._conn = _conn;
                Debug.Assert(_conn.AuthenticationResult == AuthenticationResult.Prompt);
                AuthenticationResult r = ((SSH2Connection)_conn).DoKeyboardInteractiveAuth(new string[] { password });
                Debug.Assert(r == AuthenticationResult.Success);
            }
            else {
                //NOTE: if you use public-key authentication, follow this sample instead of the line above:
                //f.AuthenticationType = AuthenticationType.PublicKey;
                f.IdentityFile = "C:\\P4\\tools\\keys\\aaa";
                f.Password = password;
                f.KeyCheck = delegate(SSHConnectionInfo info) {
                    byte[] h = info.HostKeyMD5FingerPrint();
                    foreach (byte b in h)
                        Debug.Write(String.Format("{0:x2} ", b));
                    return true;
                };

                //Creating a new SSH connection over the underlying socket
                _conn = SSHConnection.Connect(f, reader, s);
                reader._conn = _conn;
            }

            //Opening a shell
            SSHChannel ch = _conn.OpenShell(reader);
            reader._pf = ch;

            //you can get the detailed connection information in this way:
            SSHConnectionInfo ci = _conn.ConnectionInfo;

            //Go to sample shell
            SampleShell(reader);
        }

        //Tutorial: port forwarding
        private static void ConnectSSH2AndPortforwarding() {
            SSHConnectionParameter f = new SSHConnectionParameter();
            f.EventTracer = new Tracer(); //to receive detailed events, set ISSHEventTracer
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            f.Protocol = SSHProtocol.SSH1; //this sample works on both SSH1 and SSH2
            string host_ip = "10.10.9.8"; //<--!!! [TO USERS OF Granados]
            f.UserName = "root";          //<--!!! if you try this sample, edit these values for your environment!
            f.Password = "";              //<--!!! 
            s.Connect(new IPEndPoint(IPAddress.Parse(host_ip), 22)); //22 is the default SSH port

            f.Protocol = SSHProtocol.SSH2;

            f.AuthenticationType = AuthenticationType.Password;
            //NOTE: if you use public-key authentication, follow this sample instead of the line above:
            //  f.AuthenticationType = AuthenticationType.PublicKey;
            //  f.IdentityFile = "privatekey.bin";
            //  f.Password = "passphrase";

            //former algorithm is given priority in the algorithm negotiation
            f.PreferableHostKeyAlgorithms = new PublicKeyAlgorithm[] { PublicKeyAlgorithm.DSA };
            f.PreferableCipherAlgorithms = new CipherAlgorithm[] { CipherAlgorithm.Blowfish, CipherAlgorithm.TripleDES };

            f.WindowSize = 0x1000; //this option is ignored with SSH1

            Reader reader = new Reader(); //simple event receiver

            //Creating a new SSH connection over the underlying socket
            _conn = SSHConnection.Connect(f, reader, s);
            reader._conn = _conn;

            //Local->Remote port forwarding
            SSHChannel ch = _conn.ForwardPort(reader, "www.google.co.jp", 80, "localhost", 0);
            reader._pf = ch;
            while (!reader._ready)
                System.Threading.Thread.Sleep(100); //wait response
            reader._pf.Transmit(Encoding.ASCII.GetBytes("GET / HTTP/1.0\r\n\r\n")); //get the toppage

            //Remote->Local
            // if you want to listen to a port on the SSH server, follow this line:
            //_conn.ListenForwardedPort("0.0.0.0", 10000);

            //NOTE: if you use SSH2, dynamic key exchange feature is supported.
            //((SSH2Connection)_conn).ReexchangeKeys();
        }

        private static void SampleShell(Reader reader) {
            byte[] b = new byte[1];
            while (true) {
                int input = System.Console.Read();

                b[0] = (byte)input;
                reader._pf.Transmit(b);
            }
        }

        // This method uses SCP protocol.
        private static void ScpCommand(string[] args) {
            ScpParameter scp_param = new ScpParameter();
#if true //OKAJIMA
#if true
            scp_param.Direction = SCPCopyDirection.LocalToRemote;
            scp_param.RemoteFilename = "test.txt";
            scp_param.LocalSource = new ScpLocalSource("C:\\IOPort\\test.txt");
#else
            scp_param.Direction = SCPCopyDirection.RemoteToLocal;
            scp_param.RemoteFilename = "hiro.jpg";
            scp_param.LocalSource = new ScpLocalSource("C:\\IOPort\\hiro.jpg");
#endif
            //string host_ip;
            //string username, password;

            SSHConnectionParameter f = new SSHConnectionParameter();
            f.EventTracer = new Tracer(); //to receive detailed events, set ISSHEventTracer
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            f.Protocol = SSHProtocol.SSH2;
            f.UserName = "root";          //<--!!! if you try this sample, edit these values for your environment!
            f.Password = "intb0bo";              //<--!!! 
            f.AuthenticationType = AuthenticationType.Password;
            s.Connect(new IPEndPoint(IPAddress.Parse("172.22.1.2"), 22)); //22 is the default SSH port

            SSHConnection conn = SSHConnection.Connect(f, new Reader(), s);
            conn.AutoDisconnect = false; //auto close is disabled for multiple scp operations
            conn.ExecuteSCP(scp_param);

            conn.Disconnect("");
#endif
#if HIRATA
            // check argument
            if (args.Length != 6) {
                Console.WriteLine("Usage: ScpCommand <server:port> <username> <password> to|from <src_file> <dst_file>");
                Environment.Exit(0);
            }

            // test pattern
            int test = 103;

            if (test == 0) {
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "to";
                args[4] = "hoge6.txt";
                args[5] = "hoge6s.txt";
            }
            if (test == 1) {
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "to";
                args[4] = "hoge28k.txt";
                args[5] = "hoge28ks.txt";
            }
            if (test == 2) {
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "to";
                args[4] = null;   // use Local Memory
                args[5] = "hogeLM.txt";
            }
            if (test == 3) { // big file transfer
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "to";
                args[4] = "bigfile.bin";
                args[5] = "bigfile.bin";
            }

            if (test == 100) {
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "from";
                args[4] = "hoge6.txt";
                args[5] = "hoge6c.txt";
            }
            if (test == 101) {
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "from";
                args[4] = "hoge28k.txt";
                args[5] = "hoge28kc.txt";
            }
            if (test == 102) {
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "from";
                args[4] = "hoge6.txt";
                //args[4] = "hoge28k.txt";
                args[5] = null;   // use Local Memory
            }
            if (test == 103) {  // big file transfer
                args[0] = "192.168.1.2";
                args[1] = "yutaka";
                args[2] = "yutaka";
                args[3] = "from";
                args[4] = "bigfile.bin";
                args[5] = "bigfilec.bin";
            }

            host_ip = args[0];
            username = args[1];
            password = args[2];

            // setup SCP parameter
            if (args[3] == "to") {  // Local to Remote
                if (args[5] == null || args[5] == "") {
                    param.RemoteFilename = null;
                }
                else {
                    param.RemoteFilename = args[5];  // remote file
                }

                // 転送元の指定（ローカルファイルおよびローカルメモリを選択）
                if (args[4] != null) {
                    // ローカルファイルの転送
                    param.LocalSource = args[4]; // src file

                }
                else {
                    // オンラインメモリの転送
                    //param.IoStream = new MemoryStream(256);
                    param.IoStream = new MemoryStream(8192);
                    for (int i = 0; i < 8192; i++) {
                        param.IoStream.WriteByte((byte)i);
                    }
                    param.IoStream.Seek(0, SeekOrigin.Begin);
                }
                param.Direction = true;

                param.Permission = "0666";

            }
            else {  // Remote to Local
                param.RemoteFilename = args[4]; // remote file

                // 転送元の指定（ローカルファイルおよびローカルメモリを選択）
                if (args[5] != null) {
                    // ローカルファイルの転送
                    param.LocalSource = args[5];
                }
                else {
                    // オンラインメモリの転送
                    param.IoStream = null;
                }
                param.Direction = false;
            }

            // connect to server with SSH protocol
            SSHConnectionParameter f = new SSHConnectionParameter();
            f.EventTracer = new Tracer(); //to receive detailed events, set ISSHEventTracer
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            f.Protocol = SSHProtocol.SSH2; //this sample works on both SSH1 and SSH2
            f.UserName = username;          //<--!!! if you try this sample, edit these values for your environment!
            f.Password = password;              //<--!!! 
            s.Connect(new IPEndPoint(IPAddress.Parse(host_ip), 22)); //22 is the default SSH port

            f.AuthenticationType = AuthenticationType.Password;
            //NOTE: if you use public-key authentication, follow this sample instead of the line above:
            //  f.AuthenticationType = AuthenticationType.PublicKey;
            //  f.IdentityFile = "privatekey.bin";
            //  f.Password = "passphrase";

            //former algorithm is given priority in the algorithm negotiation
            f.PreferableHostKeyAlgorithms = new PublicKeyAlgorithm[] { PublicKeyAlgorithm.DSA };
            f.PreferableCipherAlgorithms = new CipherAlgorithm[] { CipherAlgorithm.Blowfish, CipherAlgorithm.TripleDES };

            //this option is ignored with SSH1
            f.WindowSize = 0x1000; //NG: ERROR: MAC mismatch
            //f.WindowSize = 0x800; //NG
            //f.WindowSize = 0x30000; //NG
            //f.WindowSize = 0x400; //OK
            //f.CheckMACError = false; //NG: unexpected channel pt=SSH_MSG_CHANNEL_DATA local_channel=33243

            /* USER OPTION */
            //param.CancelTransfer = true;  // cancel flag
            param.ProgressCallback = delegate() {
                Debug.Write("*");
            };   // callback function

            if (SSHConnection.SCPExecute(param, f, s)) {
                Debug.WriteLine("scp success!");

                if (param.Direction == false) {
                    if (param.IoStream != null) {
                        Debug.Write("IO Stream: ");
                        for (int i = 0; i < param.IoStream.Length; i++) {
                            byte b = (byte)param.IoStream.ReadByte();
                            Debug.Write(b.ToString("x2") + " ");
                        }
                        Debug.WriteLine("");
                    }
                }
            }
            else {
                Debug.WriteLine("scp failure: " + param.ErrorMessage);
            }

#endif
        }

        private static void AgentForward() {
            SSHConnectionParameter f = new SSHConnectionParameter();
            f.EventTracer = new Tracer(); //to receive detailed events, set ISSHEventTracer
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            f.Protocol = SSHProtocol.SSH2; //this sample works on both SSH1 and SSH2
            string host_ip = "172.22.1.15"; //<--!!! [TO USERS OF Granados]
            f.UserName = "root";               //<--!!! if you try this sample, edit these values for your environment!
            string password = "";
            s.Connect(new IPEndPoint(IPAddress.Parse(host_ip), 22)); //22 is the default SSH port

            //former algorithm is given priority in the algorithm negotiation
            f.PreferableHostKeyAlgorithms = new PublicKeyAlgorithm[] { PublicKeyAlgorithm.RSA, PublicKeyAlgorithm.DSA };
            f.PreferableCipherAlgorithms = new CipherAlgorithm[] { CipherAlgorithm.Blowfish, CipherAlgorithm.TripleDES };
            f.WindowSize = 0x1000; //this option is ignored with SSH1
            f.AgentForward = new AgentForwardClient();
            Reader reader = new Reader(); //simple event receiver

            AuthenticationType at = AuthenticationType.Password;
            f.AuthenticationType = at;
            f.Password = password;

            //Creating a new SSH connection over the underlying socket
            _conn = SSHConnection.Connect(f, reader, s);
            reader._conn = _conn;

            //Opening a shell
            SSHChannel ch = _conn.OpenShell(reader);
            reader._pf = ch;

            while (!reader._ready)
                Thread.Sleep(100);

            Thread.Sleep(1000);
            ch.Transmit(Encoding.Default.GetBytes("ssh -A -l okajima localhost\r"));

            //Go to sample shell
            SampleShell(reader);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    class Reader : ISSHConnectionEventReceiver, ISSHChannelEventReceiver {
        public SSHConnection _conn;
        public bool _ready;

        public void OnData(byte[] data, int offset, int length) {
            System.Console.Write(Encoding.ASCII.GetString(data, offset, length));
        }
        public void OnDebugMessage(bool always_display, byte[] data) {
            Debug.WriteLine("DEBUG: " + Encoding.ASCII.GetString(data));
        }
        public void OnIgnoreMessage(byte[] data) {
            Debug.WriteLine("Ignore: " + Encoding.ASCII.GetString(data));
        }
        public void OnAuthenticationPrompt(string[] msg) {
            Debug.WriteLine("Auth Prompt " + (msg.Length > 0 ? msg[0] : "(empty)"));
        }

        public void OnError(Exception error) {
            Debug.WriteLine("ERROR: " + error.Message);
            Debug.WriteLine(error.StackTrace);
        }
        public void OnChannelClosed() {
            Debug.WriteLine("Channel closed");
            //_conn.AsyncReceive(this);
        }
        public void OnChannelEOF() {
            _pf.Close();
            Debug.WriteLine("Channel EOF");
        }
        public void OnExtendedData(int type, byte[] data) {
            Debug.WriteLine("EXTENDED DATA");
        }
        public void OnConnectionClosed() {
            Debug.WriteLine("Connection closed");
        }
        public void OnUnknownMessage(byte type, byte[] data) {
            Debug.WriteLine("Unknown Message " + type);
        }
        public void OnChannelReady() {
            _ready = true;
        }
        public void OnChannelError(Exception error) {
            Debug.WriteLine("Channel ERROR: " + error.Message);
        }
        public void OnMiscPacket(byte type, byte[] data, int offset, int length) {
        }

        public PortForwardingCheckResult CheckPortForwardingRequest(string host, int port, string originator_host, int originator_port) {
            PortForwardingCheckResult r = new PortForwardingCheckResult();
            r.allowed = true;
            r.channel = this;
            return r;
        }
        public void EstablishPortforwarding(ISSHChannelEventReceiver rec, SSHChannel channel) {
            _pf = channel;
        }

        public SSHChannel _pf;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    class Tracer : ISSHEventTracer {
        public void OnTranmission(string type, string detail) {
            Debug.WriteLine("T:" + type + ":" + detail);
        }
        public void OnReception(string type, string detail) {
            Debug.WriteLine("R:" + type + ":" + detail);
        }
    }

    class AgentForwardClient : IAgentForward {
        private SSH2UserAuthKey[] _keys;
        public SSH2UserAuthKey[] GetAvailableSSH2UserAuthKeys() {
            if (_keys == null) {
                SSH2UserAuthKey k = SSH2UserAuthKey.FromSECSHStyleFile(@"C:\P4\Tools\keys\aaa", "aaa");
                _keys = new SSH2UserAuthKey[] { k };
            }
            return _keys;
        }

        public void NotifyPublicKeyDidNotMatch() {
            Debug.WriteLine("KEY NOT MATCH");
        }
        public bool CanAcceptForwarding() {
            return true;
        }

        public void Close() {
        }

        public void OnError(Exception ex) {
        }
    }
#endif
}
