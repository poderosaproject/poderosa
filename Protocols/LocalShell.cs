// Copyright 2004-2025 The Poderosa Project.
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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;

using Poderosa.Util;
using Poderosa.Forms;
using Poderosa.Plugins;

namespace Poderosa.Protocols {
    internal abstract class LocalShellUtil {

        //接続先のSocketを準備して返す。失敗すればparentを親にしてエラーを表示し、nullを返す。
        internal static ITerminalConnection PrepareSocket(IPoderosaForm parent, ICygwinParameter param) {
            try {
                return new Connector(param).Connect();
            }
            catch (Exception ex) {
                //string key = IsCygwin(param)? "Message.CygwinUtil.FailedToConnect" : "Message.SFUUtil.FailedToConnect";
                string key = "Message.CygwinUtil.FailedToConnect";
                parent.Warning(PEnv.Strings.GetString(key) + ex.Message);
                return null;
            }
        }

        public static Connector AsyncPrepareSocket(IInterruptableConnectorClient client, ICygwinParameter param) {
            Connector c = new Connector(param, client);
            new Thread(new ThreadStart(c.AsyncConnect)).Start();
            return c;
        }

        /// <summary>
        /// Exception from LocalShellUtil
        /// </summary>
        internal class LocalShellUtilException : Exception {
            public LocalShellUtilException(string message)
                : base(message) {
            }
            public LocalShellUtilException(string message, Exception innerException)
                : base(message, innerException) {
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public class Connector : IInterruptable {
            const string CYGBRIDGE_X86_EXE = "cygwin-bridge32.exe";
            const string CYGBRIDGE_X86_64_EXE = "cygwin-bridge64.exe";

            private ICygwinParameter _param;
            private Process _process;
            private IInterruptableConnectorClient _client;
            private Thread _asyncThread;
            private bool _interrupted;

            public Connector(ICygwinParameter param) {
                _param = param;
            }

            public Connector(ICygwinParameter param, IInterruptableConnectorClient client) {
                _param = param;
                _client = client;
            }

            public void AsyncConnect() {
                bool success = false;
                _asyncThread = Thread.CurrentThread;
                try {
                    ITerminalConnection result = Connect();
                    if (!_interrupted) {
                        success = true;
                        Debug.Assert(result != null);
                        ProtocolUtil.FireConnectionSucceeded(_param);
                        _client.SuccessfullyExit(result);
                    }
                }
                catch (Exception ex) {
                    if (!(ex is LocalShellUtilException)) {
                        RuntimeUtil.ReportException(ex);
                    }
                    if (!_interrupted) {
                        _client.ConnectionFailed(ex.Message);
                        ProtocolUtil.FireConnectionFailure(_param, ex.Message);
                    }
                }
                finally {
                    if (!success && _process != null) {
                        if (!_process.HasExited) {
                            _process.Kill();
                        }
                        _process.Dispose();
                        _process = null;
                    }
                }
            }
            public void Interrupt() {
                _interrupted = true;
            }

            public ITerminalConnection Connect() {
                string exeName =
                    (_param.CygwinArchitecture == CygwinArchitecture.X86) ? CYGBRIDGE_X86_EXE : CYGBRIDGE_X86_64_EXE;
                string cygwinBridgePath = GetCygwinBridgePath(exeName);
                if (cygwinBridgePath == null)
                    throw new LocalShellUtilException(
                        String.Format(PEnv.Strings.GetString("Message.CygwinUtil.CygwinBridgeExeNotFound"), exeName));

                using (Socket listener = new Socket(SocketType.Stream, ProtocolType.Tcp)) {
                    listener.Bind(new IPEndPoint(new IPAddress(new byte[] { 127, 0, 0, 1 }), 0));
                    int localPort = (listener.LocalEndPoint as IPEndPoint).Port;
                    listener.Listen(1);

                    ITerminalParameter term = (ITerminalParameter)_param.GetAdapter(typeof(ITerminalParameter));

                    string args = "-p " + localPort.ToString(NumberFormatInfo.InvariantInfo);
                    args += String.Format(NumberFormatInfo.InvariantInfo, " -z {0}x{1}", term.InitialWidth, term.InitialHeight);
                    args += " -e";
                    if (_param.UseUTF8) {
                        args += " -u";
                    }
                    if (!String.IsNullOrEmpty(_param.Home)) {
                        args += " -v \"HOME=" + _param.Home + "\"";
                    }
                    if (!String.IsNullOrEmpty(term.TerminalType)) {
                        args += " -v \"TERM=" + term.TerminalType + "\"";
                    }
                    if (!String.IsNullOrEmpty(_param.ShellName)) {
                        args += " -- " + _param.ShellName;
                    }
                    ProcessStartInfo psi = new ProcessStartInfo(cygwinBridgePath, args);
                    string cygwinDir = _param.CygwinDir;
                    if (cygwinDir == null || cygwinDir.Length == 0)
                        cygwinDir = CygwinUtil.GuessRootDirectory();
                    PrepareEnv(psi, cygwinDir);
                    psi.CreateNoWindow = true;
                    psi.ErrorDialog = false;
                    psi.UseShellExecute = false;
                    psi.WindowStyle = ProcessWindowStyle.Hidden;

                    try {
                        _process = Process.Start(psi);
                    }
                    catch (System.ComponentModel.Win32Exception ex) {
                        throw StartingError(cygwinBridgePath, ex);
                    }

                    Socket sock;
                    using (ManualResetEventSlim acceptEvent = new ManualResetEventSlim())
                    using (SocketAsyncEventArgs listenArgs = new SocketAsyncEventArgs()) {
                        listenArgs.Completed += (e, a) => acceptEvent.Set();
                        listener.AcceptAsync(listenArgs);
                        if (!acceptEvent.Wait(5000)) {
                            throw StartingError(cygwinBridgePath, null);
                        }
                        sock = listenArgs.AcceptSocket;
                    }

                    if (_interrupted) {
                        sock.Close();
                        return null;
                    }

                    return new CygwinTerminalConnection(term, sock, cygwinDir);
                }
            }

            private LocalShellUtilException StartingError(string cygwinBridgePath, Exception ex) {
                string guide1 = PEnv.Strings.GetString("Message.CygwinUtil.CheckCygwin1DLL");
                string guide2 = PEnv.Strings.GetString("Message.CygwinUtil.CheckCygwinArchitecture");
                return new LocalShellUtilException(
                    String.Format(PEnv.Strings.GetString("Message.CygwinUtil.FailedToRunCygwinBridge"), cygwinBridgePath, guide1, guide2), ex);
            }

            private string GetCygwinBridgePath(string exeName) {
                foreach (string path in EnumerateCygwinBridgePath(exeName)) {
                    if (File.Exists(path)) {
                        return path;
                    }
                }
                return null;
            }

            private IEnumerable<string> EnumerateCygwinBridgePath(string exeName) {
                foreach (string basePath in EnumerateCygwinBridgeParentPath()) {
                    // 1st candidate: <base>/CygwinBridge/cygwin-bridge.exe
                    yield return Path.Combine(basePath, "cygwinbridge", exeName);

                    // 2nd candidate: <base>/cygwin-bridge.exe
                    yield return Path.Combine(basePath, exeName);
                }
            }

            private IEnumerable<string> EnumerateCygwinBridgeParentPath() {
                // 1st candidate: <assembly's location>
                yield return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

                IPoderosaApplication app = (IPoderosaApplication)ProtocolsPlugin.Instance.PoderosaWorld.GetAdapter(typeof(IPoderosaApplication));

                // 2nd candidate: <HomeDirectory>/Protocols
                yield return Path.Combine(app.HomeDirectory, "protocols");

                // 3rd candidate: <HomeDirectory>
                yield return app.HomeDirectory;
            }

        }

        protected static void PrepareEnv(ProcessStartInfo psi, string cygwinDir) {
            string path = psi.EnvironmentVariables["PATH"];
            if (path == null)
                path = String.Empty;
            else if (!path.EndsWith(";"))
                path += ";";
            path += cygwinDir + "\\bin";
            psi.EnvironmentVariables.Remove("PATH");
            psi.EnvironmentVariables.Add("PATH", path);
        }

        public static void Terminate() {
        }

        private static bool IsCygwin(LocalShellParameter tp) {
            return true;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class SFUUtil {
        public static string DefaultHome {
            get {
                string a = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                //最後の\の後にApplication Dataがあるので
                int t = a.LastIndexOf('\\');
                char drive = a[0];
                return "/dev/fs/" + Char.ToUpper(drive) + a.Substring(2, t - 2).Replace('\\', '/');
            }
        }
        public static string DefaultShell {
            get {
                return "/bin/csh -l";
            }
        }
        public static string GuessRootDirectory() {
            RegistryKey reg = null;
            string keyname = "SOFTWARE\\Microsoft\\Services for UNIX";
            reg = Registry.LocalMachine.OpenSubKey(keyname);
            if (reg == null) {
                //GUtil.Warning(GEnv.Frame, String.Format(PEnv.Strings.GetString("Message.SFUUtil.KeyNotFound"), keyname));
                return "";
            }
            string t = (string)reg.GetValue("InstallPath");
            reg.Close();
            return t;
        }

    }

    /// <summary>
    /// <ja>
    /// Cygwin接続時のパラメータを示すヘルパクラスです。
    /// </ja>
    /// <en>
    /// Helper class that shows parameter when Cygwin connecting.
    /// </en>
    /// </summary>
    /// <exclude/>
    public class CygwinUtil {
        /// <summary>
        /// <ja>
        /// デフォルトのホームディレクトリを返します。
        /// </ja>
        /// <en>
        /// Return the default home directory.
        /// </en>
        /// </summary>
        public static string DefaultHome {
            get {
                return String.Empty;
            }
        }
        /// <summary>
        /// <ja>
        /// デフォルトのシェルを返します。
        /// </ja>
        /// <en>
        /// Return the default shell.
        /// </en>
        /// </summary>
        public static string DefaultShell {
            get {
                return String.Empty;
            }
        }

        /// <summary>
        /// <ja>
        /// デフォルトのCygwinのパスを返します。
        /// </ja>
        /// <en>
        /// Return the default Cygwin path.
        /// </en>
        /// </summary>
        public static string DefaultCygwinDir {
            get {
                return String.Empty;    // not specify
            }
        }

        /// <summary>
        /// Default Cygwin architecture
        /// </summary>
        public static CygwinArchitecture DefaultCygwinArchitecture {
            get {
                return CygwinArchitecture.X86_64;
            }
        }

        /// <summary>
        /// <ja>
        /// デフォルトの端末タイプを返します。
        /// </ja>
        /// <en>
        /// Return the default terminal type.
        /// </en>
        /// </summary>
        public static string DefaultTerminalType {
            get {
                return "xterm";
            }
        }

        /// <summary>
        /// <ja>
        /// レジストリを検索し、Cygwinのルートディレクトリを返します。
        /// </ja>
        /// <en>
        /// The registry is retrieved, and the root directory of Cygwin is returned. 
        /// </en>
        /// </summary>
        /// <returns><ja>Cygwinのルートディレクトリと思わしき場所が返されます。</ja><en>A root directory of Cygwin and a satisfactory place are returned. </en></returns>
        public static string GuessRootDirectory() {
            //HKCU -> HKLMの順でサーチ
            string rootDir;
            rootDir = GetCygwinRootDirectory(Registry.CurrentUser, false);
            if (rootDir != null)
                return rootDir;
            rootDir = GetCygwinRootDirectory(Registry.LocalMachine, false);
            if (rootDir != null)
                return rootDir;
            if (IntPtr.Size == 8) {	// we're in 64bit
                rootDir = GetCygwinRootDirectory(Registry.LocalMachine, true);
                if (rootDir != null)
                    return rootDir;
            }

            //TODO 必ずしもActiveFormでいいのか、というのはあるけどな
            PEnv.ActiveForm.Warning(PEnv.Strings.GetString("Message.CygwinUtil.KeyNotFound"));
            return String.Empty;
        }

        private static string GetCygwinRootDirectory(RegistryKey baseKey, bool check64BitHive) {
            string software = check64BitHive ? "SOFTWARE\\Wow6432Node" : "SOFTWARE";
            string[][] keyValueNameArray = new string[][] {
                new string[] { software + "\\Cygnus Solutions\\Cygwin\\mounts v2\\/", "native" },
                new string[] { software + "\\Cygwin\\setup", "rootdir" }
            };

            foreach (string[] keyValueName in keyValueNameArray) {
                using (RegistryKey subKey = baseKey.OpenSubKey(keyValueName[0])) {
                    if (subKey != null) {
                        string val = subKey.GetValue(keyValueName[1]) as string;
                        if (val != null)
                            return val;
                    }
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Implementation of ITerminalConnection
    /// </summary>
    internal class CygwinTerminalConnection : TerminalConnection, ITerminalConnection {

        private readonly CygwinSocket _cygwinSocket;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="terminalParameter">Terminal parameter</param>
        /// <param name="socket">Socket object</param>
        /// <param name="remote">text representing the remote side</param>
        public CygwinTerminalConnection(ITerminalParameter terminalParameter, Socket socket, string remote)
            : base(terminalParameter) {

            _cygwinSocket = new CygwinSocket(socket, this, remote);
        }

        public override ITerminalOutput TerminalOutput {
            get {
                return _cygwinSocket;
            }
        }

        public override IPoderosaSocket Socket {
            get {
                return _cygwinSocket;
            }
        }
    }

    internal class CygwinSocket : IPoderosaSocket, ITerminalOutput {
        private readonly PlainPoderosaSocket _inner;
        private readonly TelnetOptionWriter _telnetOptionWriter = new TelnetOptionWriter();
        private readonly object _telnetOptionWriterSync = new object();
        private byte[] _buff = new byte[0];
        private readonly object _buffSync = new object();

        public CygwinSocket(Socket socket, TerminalConnection conn, string remote) {
            _inner = new PlainPoderosaSocket(conn, socket, remote);
        }

        public void Transmit(ByteDataFragment data) {
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] data, int offset, int length) {
            // Outbound data must be encoded to the TELNET-like stream.

            for (int i = offset; i < offset + length; i++) {
                if (data[i] == (byte)TelnetCode.IAC) {
                    goto encode;
                }
            }
            _inner.Transmit(data, offset, length);
            return;

        encode:
            lock (_buffSync) {
                byte[] buff = _buff;
                if (buff.Length < length * 2) {
                    buff = _buff = new byte[length * 2];
                }
                int buffLength = 0;
                for (int i = offset; i < offset + length; i++) {
                    byte t = data[i];
                    buff[buffLength++] = t;
                    if (t == (byte)TelnetCode.IAC) {
                        buff[buffLength++] = t;
                    }
                }
                _inner.Transmit(buff, 0, buffLength);
            }
        }

        public void Close() {
            _inner.Close();
        }

        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            _inner.RepeatAsyncRead(receiver);
        }

        public bool Available {
            get {
                return _inner.Available;
            }
        }

        public void ForceDisposed() {
            _inner.ForceDisposed();
        }

        public void SendBreak() {
            // do nothing
        }

        public void SendKeepAliveData() {
            // do nothing
        }

        public void AreYouThere() {
            // do nothing
        }

        public void Resize(int width, int height) {
            lock (_telnetOptionWriterSync) {
                _telnetOptionWriter.Clear();
                _telnetOptionWriter.WriteTerminalSize(width, height);
                try {
                    _telnetOptionWriter.WriteTo(_inner);
                }
                catch (Exception) {
                    // ignore
                }
            }
        }

        public string Remote {
            get {
                return _inner.Remote;
            }
        }
    }
}
