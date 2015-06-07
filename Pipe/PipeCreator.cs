/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PipeCreator.cs,v 1.3 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Poderosa.Pipe {

    /// <summary>
    /// Creating Pipe
    /// </summary>
    internal class PipeCreator {

        #region Win32 API

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateNamedPipe(
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpName,
            int dwOpenMode,
            int dwPipeMode,
            int nMaxInstances,
            int nOutBufferSize,
            int nInBufferSize,
            int nDefaultTimeOut,
            IntPtr lpSecurityAttributes
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DuplicateHandle(
            IntPtr hSourceProcessHandle,
            SafeFileHandle hSourceHandle,
            IntPtr hTargetProcess,
            out SafeFileHandle lpTargetHandle,
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwOptions
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool CreateProcess(
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPTStr)]
            string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(
            [MarshalAs(UnmanagedType.LPTStr)]
            string pszPath,
            int dwFileAttributes,
            ref SHFILEINFO psfi,
            int cbFileInfo,
            int uFlags
        );

        [StructLayout(LayoutKind.Sequential)]
        private class STARTUPINFO {
            public int cb = Marshal.SizeOf(typeof(STARTUPINFO));
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpReserved = null;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDesktop = null;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpTitle = null;
            public int dwX = 0;
            public int dwY = 0;
            public int dwXSize = 0;
            public int dwYSize = 0;
            public int dwXCountChars = 0;
            public int dwYCountChars = 0;
            public int dwFillAttribute = 0;
            public int dwFlags = 0;
            public short wShowWindow = 0;
            public short cbReserved2 = 0;
            public IntPtr lpReserved2 = IntPtr.Zero;
            public SafeFileHandle hStdInput = null;
            public SafeFileHandle hStdOutput = null;
            public SafeFileHandle hStdError = null;

            public STARTUPINFO() {
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO {
            public IntPtr hIcon;
            public int iIcon;
            public int dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const int CREATE_NO_WINDOW = 0x08000000;
        private const int CREATE_NEW_CONSOLE = 0x00000010;
        private const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;

        private const int STARTF_USESTDHANDLES = 0x00000100;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const int SW_HIDE = 0;

        private const int PIPE_ACCESS_INBOUND = 1;
        private const int PIPE_ACCESS_OUTBOUND = 2;
        private const int PIPE_ACCESS_DUPLEX = 3;
        private const int FILE_FLAG_OVERLAPPED = 0x40000000;

        private const int PIPE_TYPE_BYTE = 0;

        private const int GENERIC_READ = unchecked((int)0x80000000);
        private const int GENERIC_WRITE = 0x40000000;
        private const int OPEN_EXISTING = 3;
        private const int FILE_ATTRIBUTE_NORMAL = 0x00000080;

        private const int DUPLICATE_CLOSE_SOURCE = 1;
        private const int DUPLICATE_SAME_ACCESS = 2;

        private const int SHGFI_ICON = 0x000000100;
        private const int SHGFI_LARGEICON = 0x000000000;
        private const int SHGFI_SMALLICON = 0x000000001;
        private const int SHGFI_SYSICONINDEX = 0x000004000;

        #endregion

        private static int pipeCounter = 0;

        /// <summary>
        /// Create a new PipeTerminalConnection
        /// </summary>
        /// <param name="param">Terminal parameter</param>
        /// <param name="settings">Terminal settings</param>
        /// <returns>created object</returns>
        /// <exception cref="PipeCreatorException">Creation was failed.</exception>
        public static PipeTerminalConnection CreateNewPipeTerminalConnection(PipeTerminalParameter param, PipeTerminalSettings settings) {
            Debug.Assert(param != null);
            Debug.Assert(settings != null);

            if (param.ExeFilePath != null) {
                try {
                    OverrideSettings(param, settings);
                    return OpenExeFile(param);
                }
                catch (Exception e) {
                    string message = PipePlugin.Instance.Strings.GetString("PipeCreator.LaunchingApplicationFailed");
                    throw new PipeCreatorException(message, e);
                }
            }
            else if (param.InputPipePath != null) {
                try {
                    return OpenNamedPipe(param);
                }
                catch (Exception e) {
                    string message = PipePlugin.Instance.Strings.GetString("PipeCreator.OpeningPipeFailed");
                    throw new PipeCreatorException(message, e);
                }
            }
            else {
                throw new ArgumentException("Parameter error: exe file or pipe path must be present.");
            }
        }

        /// <summary>
        /// Modify icon to which is obtained from the executable file
        /// </summary>
        /// <param name="param"></param>
        /// <param name="settings"></param>
        private static void OverrideSettings(PipeTerminalParameter param, PipeTerminalSettings settings) {
            if (param.ExeFilePath != null) {
                Image icon = GetExeFileIcon(param.ExeFilePath);
                if (icon != null) {
                    Image oldIcon = settings.Icon;
                    settings.BeginUpdate();
                    settings.Icon = icon;
                    settings.EndUpdate();

                    // FIXME:
                    //   oldIcon may being used to repainting, so we don't dispose it here.
                    //   I don't know where the icon is disposed...
                    //   
                    //if (oldIcon != null)
                    //    oldIcon.Dispose();
                }
            }
        }

        /// <summary>
        /// Start exe file and create a new PipeTerminalConnection
        /// </summary>
        /// <param name="param">Terminal parameter</param>
        /// <returns>created object</returns>
        /// <exception cref="Win32Exception">Error in Win32 API</exception>
        private static PipeTerminalConnection OpenExeFile(PipeTerminalParameter param) {

            // System.Diagnostics.Process has functionality that creates STDIN/STDOUT/STDERR pipe.
            // But we need two pipes. One connects to STDIN and another one connects to STDOUT and STDERR.
            // So we use Win32 API to invoke a new process.

            SafeFileHandle parentReadHandle = null;
            SafeFileHandle parentWriteHandle = null;
            SafeFileHandle childReadHandle = null;
            SafeFileHandle childWriteHandle = null;
            SafeFileHandle childStdInHandle = null;
            SafeFileHandle childStdOutHandle = null;
            SafeFileHandle childStdErrHandle = null;

            FileStream parentReadStream = null;
            FileStream parentWriteStream = null;

            try {
                // Create pipes
                CreateAsyncPipe(out parentReadHandle, true, out childWriteHandle, false);
                CreateAsyncPipe(out childReadHandle, false, out parentWriteHandle, true);

                // Duplicate handles as inheritable handles.
                childStdOutHandle = DuplicatePipeHandle(childWriteHandle, true, "ChildWrite");
                childStdInHandle = DuplicatePipeHandle(childReadHandle, true, "ChildRead");
                childStdErrHandle = DuplicatePipeHandle(childWriteHandle, true, "ChildWrite");

                // Close non-inheritable handles
                childWriteHandle.Dispose();
                childWriteHandle = null;
                childReadHandle.Dispose();
                childReadHandle = null;

                // Create parent side streams
                parentReadStream = new FileStream(parentReadHandle, FileAccess.Read, 4096, true /*Async*/);
                parentWriteStream = new FileStream(parentWriteHandle, FileAccess.Write, 4096, true /*Async*/);

                // Prepare command line
                string commandLine = GetCommandLine(param.ExeFilePath, param.CommandLineOptions);

                // Determine character encoding of the environment variables
                bool unicodeEnvironment = (Environment.OSVersion.Platform == PlatformID.Win32NT) ? true : false;

                // Prepare flags
                // Note:
                //  We use CREATE_NEW_CONSOLE for separating console.
                //  It disables CREATE_NO_WINDOW, so we use setting below
                //  to hide the console window.
                //    STARTUPINFO.dwFlags |= STARTF_USESHOWWINDOW
                //    STARTUPINFO.wShowWindow = SW_HIDE
                int creationFlags = CREATE_NEW_CONSOLE /*| CREATE_NO_WINDOW*/;
                if (unicodeEnvironment)
                    creationFlags |= CREATE_UNICODE_ENVIRONMENT;

                // Prepare environment variables
                Dictionary<String, String> envDict = new Dictionary<String, String>();
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables()) {
                    string key = entry.Key as string;
                    string value = entry.Value as string;
                    if (key != null && value != null) {
                        envDict.Add(key.ToLowerInvariant(), key + "=" + value);
                    }
                }

                if (param.EnvironmentVariables != null) {
                    foreach (PipeTerminalParameter.EnvironmentVariable ev in param.EnvironmentVariables) {
                        string expandedValue = Environment.ExpandEnvironmentVariables(ev.Value);
                        string key = ev.Name.ToLowerInvariant();
                        envDict.Remove(key);
                        envDict.Add(key, ev.Name + "=" + expandedValue);
                    }
                }

                byte[] environmentByteArray = GetEnvironmentBytes(envDict, unicodeEnvironment);

                // Prepare current directory
                string currentDirectory = Path.GetDirectoryName(param.ExeFilePath);

                // Prepare STARTUPINFO
                STARTUPINFO startupInfo = new STARTUPINFO();
                startupInfo.dwFlags |= STARTF_USESTDHANDLES | STARTF_USESHOWWINDOW;
                startupInfo.hStdInput = childStdInHandle;
                startupInfo.hStdOutput = childStdOutHandle;
                startupInfo.hStdError = childStdErrHandle;
                startupInfo.wShowWindow = SW_HIDE;

                // Prepare PROCESS_INFORMATION
                PROCESS_INFORMATION processInfo = new PROCESS_INFORMATION();

                // Start process
                GCHandle environmentGCHandle = GCHandle.Alloc(environmentByteArray, GCHandleType.Pinned);
                bool apiret = CreateProcess(
                               null,
                               commandLine,
                               IntPtr.Zero,
                               IntPtr.Zero,
                               true,
                               creationFlags,
                               environmentGCHandle.AddrOfPinnedObject(),
                               currentDirectory,
                               startupInfo,
                               ref processInfo
                           );
                environmentGCHandle.Free();

                if (!apiret)
                    throw new Win32Exception("CreateProcess", Marshal.GetLastWin32Error(), "commandLine=" + commandLine);

                Process process = Process.GetProcessById(processInfo.dwProcessId);

                PipedProcess pipedProcess = new PipedProcess(process, childStdInHandle, childStdOutHandle, childStdErrHandle);
                PipeSocket socket = new PipeSocket(parentReadStream, parentWriteStream);
                PipeTerminalConnection connection = new PipeTerminalConnection(param, socket, pipedProcess);

                return connection;
            }
            catch (Exception) {
                if (parentReadStream != null)
                    parentReadStream.Dispose();

                if (parentWriteStream != null)
                    parentWriteStream.Dispose();

                if (parentReadHandle != null)
                    parentReadHandle.Dispose();

                if (parentWriteHandle != null)
                    parentWriteHandle.Dispose();

                if (childReadHandle != null)
                    childReadHandle.Dispose();

                if (childWriteHandle != null)
                    childWriteHandle.Dispose();

                if (childStdInHandle != null)
                    childStdInHandle.Dispose();

                if (childStdOutHandle != null)
                    childStdOutHandle.Dispose();

                if (childStdErrHandle != null)
                    childStdErrHandle.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Open file path and create a new PipeTerminalConnection
        /// </summary>
        /// <param name="param">Terminal parameter</param>
        /// <returns>created object</returns>
        /// <exception cref="Win32Exception">Error in Win32 API</exception>
        private static PipeTerminalConnection OpenNamedPipe(PipeTerminalParameter param) {

            SafeFileHandle inputHandle = null;
            SafeFileHandle outputHandle = null;
            FileStream readStream = null;
            FileStream writeStream = null;

            try {
                bool hasOutputPipePath = param.OutputPipePath != null;

                inputHandle = CreateFile(
                                param.InputPipePath,
                                GENERIC_READ | (hasOutputPipePath ? 0 : GENERIC_WRITE),
                                0,
                                IntPtr.Zero,
                                OPEN_EXISTING,
                                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                                IntPtr.Zero);
                if (inputHandle.IsInvalid)
                    throw new Win32Exception("CreateFile", Marshal.GetLastWin32Error(),
                        "path=" + param.InputPipePath + " mode=GENERIC_READ" + (hasOutputPipePath ? "" : "|GENERIC_WRITE"));

                readStream = new FileStream(inputHandle, hasOutputPipePath ? FileAccess.Read : FileAccess.ReadWrite, 4096, true /*Async*/);

                if (hasOutputPipePath) {
                    outputHandle = CreateFile(
                                        param.OutputPipePath,
                                        GENERIC_WRITE,
                                        0,
                                        IntPtr.Zero,
                                        OPEN_EXISTING,
                                        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED,
                                        IntPtr.Zero);
                    if (outputHandle.IsInvalid)
                        throw new Win32Exception("CreateFile", Marshal.GetLastWin32Error(),
                            "path=" + param.OutputPipePath + " mode=GENERIC_WRITE");

                    writeStream = new FileStream(outputHandle, FileAccess.Write, 4096, true /*Async*/);
                }
                else {
                    writeStream = readStream;
                }

                PipeSocket sock = new PipeSocket(readStream, writeStream);
                PipeTerminalConnection conn = new PipeTerminalConnection(param, sock, null);

                return conn;
            }
            catch (Exception) {
                if (readStream != null)
                    readStream.Dispose();

                if (writeStream != null)
                    writeStream.Dispose();

                if (inputHandle != null)
                    inputHandle.Dispose();

                if (outputHandle != null)
                    outputHandle.Dispose();

                throw;
            }
        }

        private static void CreateAsyncPipe(out SafeFileHandle readHandle, bool readAsync, out SafeFileHandle writeHandle, bool writeAsync) {

            // CreatePipe() API creates a synchronous Pipe.
            // So we use CreateNamedPipe() to create an asynchronous pipe.

            SafeFileHandle tmpReadHandle = null;
            SafeFileHandle tmpWriteHandle = null;

            const int PIPE_BUFFER_SIZE = 4096;

            try {
                StringBuilder pipeNameBuf = new StringBuilder();
                pipeNameBuf.Append(@"\\.\pipe\Poderosa-");
                pipeNameBuf.Append((System.Threading.Interlocked.Increment(ref pipeCounter)).ToString("x8", NumberFormatInfo.InvariantInfo));
                pipeNameBuf.Append(DateTime.UtcNow.ToBinary().ToString("x16", NumberFormatInfo.InvariantInfo));
                byte[] rnd = new byte[10];
                (new Random()).NextBytes(rnd);
                foreach (byte b in rnd) {
                    pipeNameBuf.Append(b.ToString("x2", NumberFormatInfo.InvariantInfo));
                }
                string pipeName = pipeNameBuf.ToString();

                // Note: lpSecurityAttributes is NULL. The created handle is non-inheritable handle.
                tmpWriteHandle = CreateNamedPipe(
                                    pipeName,
                                    PIPE_ACCESS_OUTBOUND | (writeAsync ? FILE_FLAG_OVERLAPPED : 0),
                                    PIPE_TYPE_BYTE,
                                    1,
                                    PIPE_BUFFER_SIZE,
                                    PIPE_BUFFER_SIZE,
                                    0,
                                    IntPtr.Zero);

                if (tmpWriteHandle.IsInvalid)
                    throw new Win32Exception("CreateNamedPipe", Marshal.GetLastWin32Error(), "path=" + pipeName);

                // Note: lpSecurityAttributes is NULL. The created handle is non-inheritable handle.
                tmpReadHandle = CreateFile(
                                    pipeName,
                                    GENERIC_READ,
                                    0,
                                    IntPtr.Zero,
                                    OPEN_EXISTING,
                                    FILE_ATTRIBUTE_NORMAL | (readAsync ? FILE_FLAG_OVERLAPPED : 0),
                                    IntPtr.Zero);

                if (tmpReadHandle.IsInvalid)
                    throw new Win32Exception("CreateFile", Marshal.GetLastWin32Error(), "path=" + pipeName);

                readHandle = tmpReadHandle;
                writeHandle = tmpWriteHandle;
            }
            catch (Exception) {
                if (tmpReadHandle != null)
                    tmpReadHandle.Dispose();

                if (tmpWriteHandle != null)
                    tmpWriteHandle.Dispose();

                throw;
            }
        }

        private static SafeFileHandle DuplicatePipeHandle(SafeFileHandle pipeHandle, bool inheritable, string handleName) {
            IntPtr currentProcessHandle = Process.GetCurrentProcess().Handle;
            SafeFileHandle duplicatedHandle;
            bool apiret = DuplicateHandle(
                            currentProcessHandle,
                            pipeHandle,
                            currentProcessHandle,
                            out duplicatedHandle,
                            0,
                            inheritable,
                            DUPLICATE_SAME_ACCESS);
            if (!apiret)
                throw new Win32Exception("DuplicateHandle", Marshal.GetLastWin32Error(), "handle:" + handleName);

            return duplicatedHandle;
        }

        private static string GetCommandLine(string exeFilePath, string commandLineOptions) {
            StringBuilder commandLine = new StringBuilder();

            if (!exeFilePath.StartsWith("\""))
                commandLine.Append('"');
            commandLine.Append(exeFilePath);
            if (!exeFilePath.EndsWith("\""))
                commandLine.Append('"');

            if (commandLineOptions != null)
                commandLine.Append(' ').Append(commandLineOptions);

            return commandLine.ToString();
        }

        private static byte[] GetEnvironmentBytes(Dictionary<String, String> env, bool unicode) {

            // sort names
            List<String> names = new List<String>(env.Keys);
            names.Sort((Comparison<string>)delegate(string a, string b) {
                return String.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            StringBuilder buff = new StringBuilder();
            foreach (string name in names) {
                buff.Append(env[name]).Append('\0');
            }
            buff.Append('\0');

            if (unicode)
                return Encoding.Unicode.GetBytes(buff.ToString());
            else
                return Encoding.Default.GetBytes(buff.ToString());
        }

        private static Image GetExeFileIcon(string path) {
            foreach (int flag in new int[] {
                SHGFI_SMALLICON,
                SHGFI_LARGEICON,
            }) {
                SHFILEINFO info = new SHFILEINFO();
                IntPtr apiret = SHGetFileInfo(path, 0, ref info, Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_ICON | flag);

                if (apiret == IntPtr.Zero)
                    return null;    // failed

                if (info.hIcon != IntPtr.Zero) {
                    using (Icon icon = Icon.FromHandle(info.hIcon)) {
                        Bitmap bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(bmp)) {
                            if (icon.Width == 16 && icon.Height == 16)
                                g.DrawIcon(icon, 0, 0);
                            else
                                g.DrawIcon(icon, new Rectangle(0, 0, 16, 16));
                        }
                        return bmp;
                    }
                }
            }

            return null;    // not found
        }
    }


    /// <summary>
    /// Exception thrown in PipeCreator
    /// </summary>
    internal class PipeCreatorException : Exception {
        public PipeCreatorException(string message, Exception innerException)
            : base(message, innerException) {
        }
    }
}
