// Copyright 2023-2025 The Poderosa Project.
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Poderosa.Util {

    /// <summary>
    /// Saving and loading password from the Windows Credential Manager
    /// </summary>
    public static class WinCred {

        private static bool noEntryPoint = false;

        /// <summary>
        /// Reads user password from the Windows Credential Manager
        /// </summary>
        /// <param name="protocol">protocol name (e.g. "ssh")</param>
        /// <param name="host">host name</param>
        /// <param name="port">port number (optional)</param>
        /// <param name="user">user name</param>
        /// <param name="password">password if it was found, otherwise null.</param>
        /// <returns>true if the password was found. otherwise false.</returns>
        public static bool ReadUserPassword(string protocol, string host, int? port, string user, out string password) {
            string targetName = BuildUserPasswordTargetName(protocol, host, port, user);
            return ReadPassword(targetName, out password);
        }

        /// <summary>
        /// Saves user password to the Windows Credential Manager
        /// </summary>
        /// <param name="protocol">protocol name (e.g. "ssh")</param>
        /// <param name="host">host name</param>
        /// <param name="port">port number (optional)</param>
        /// <param name="user">user name</param>
        /// <param name="password">password</param>
        /// <returns>true if the password was saved. otherwise false.</returns>
        public static bool SaveUserPassword(string protocol, string host, int? port, string user, string password) {
            string targetName = BuildUserPasswordTargetName(protocol, host, port, user);
            return SavePassword(targetName, "User Password", password);
        }

        /// <summary>
        /// Deletes user password from the Windows Credential Manager
        /// </summary>
        /// <param name="protocol">protocol name (e.g. "ssh")</param>
        /// <param name="host">host name</param>
        /// <param name="port">port number (optional)</param>
        /// <param name="user">user name</param>
        public static void DeleteUserPassword(string protocol, string host, int? port, string user) {
            string targetName = BuildUserPasswordTargetName(protocol, host, port, user);
            DeletePassword(targetName);
        }

        /// <summary>
        /// Reads key file password from the Windows Credential Manager
        /// </summary>
        /// <param name="protocol">protocol name (e.g. "ssh")</param>
        /// <param name="keyFilePath">path of the key file</param>
        /// <param name="password">password if it was found, otherwise null.</param>
        /// <returns>true if the password was found. otherwise false.</returns>
        public static bool ReadKeyFilePassword(string protocol, string keyFilePath, out string password) {
            keyFilePath = Path.GetFullPath(keyFilePath);
            string targetName = BuildKeyFilePasswordTargetName(protocol, keyFilePath);
            return ReadPassword(targetName, out password);
        }

        /// <summary>
        /// Saves key file password from the Windows Credential Manager
        /// </summary>
        /// <param name="protocol">protocol name (e.g. "ssh")</param>
        /// <param name="keyFilePath">path of the key file</param>
        /// <param name="password">password</param>
        /// <returns>true if the password was saved. otherwise false.</returns>
        public static bool SaveKeyFilePassword(string protocol, string keyFilePath, string password) {
            keyFilePath = Path.GetFullPath(keyFilePath);
            string targetName = BuildKeyFilePasswordTargetName(protocol, keyFilePath);
            return SavePassword(targetName, "Password for a Key File", password);
        }

        /// <summary>
        /// Deletes key file password from the Windows Credential Manager
        /// </summary>
        /// <param name="protocol">protocol name (e.g. "ssh")</param>
        /// <param name="keyFilePath">path of the key file</param>
        public static void DeleteKeyFilePassword(string protocol, string keyFilePath) {
            keyFilePath = Path.GetFullPath(keyFilePath);
            string targetName = BuildKeyFilePasswordTargetName(protocol, keyFilePath);
            DeletePassword(targetName);
        }

        private static bool ReadPassword(string targetName, out string password) {
            if (noEntryPoint) {
                password = null;
                return false;
            }

            if (targetName.Length > CRED_MAX_GENERIC_TARGET_NAME_LENGTH) {
                RuntimeUtil.SilentReportError("WinCred: failed to read password. too long target name.");
                password = null;
                return false;
            }

            IntPtr credential = IntPtr.Zero;
            try {
                bool r;
                unsafe {
                    r = CredRead(targetName, CredType.CRED_TYPE_GENERIC, 0, out credential);
                }
                if (r) {
                    Credential cred = (Credential)Marshal.PtrToStructure(credential, typeof(Credential));
                    byte[] blob = new byte[cred.CredentialBlobSize];
                    Marshal.Copy(cred.CredentialBlob, blob, 0, (int)cred.CredentialBlobSize);
                    password = Encoding.Unicode.GetString(blob);
                    return true;
                }
                else {
                    int err = Win32.GetLastError();
                    if (err != ERROR_NOT_FOUND) {
                        RuntimeUtil.SilentReportError("WinCred: CredRead failed. error=" + err.ToString(NumberFormatInfo.InvariantInfo));
                    }
                    password = null;
                    return false;
                }
            }
            catch (Exception e) {
                if (e is System.EntryPointNotFoundException) {
                    noEntryPoint = true;
                }
                RuntimeUtil.SilentReportException(e);
                password = null;
                return false;
            }
            finally {
                if (credential != IntPtr.Zero) {
                    try {
                        CredFree(credential);
                    }
                    catch (Exception e) {
                        RuntimeUtil.SilentReportException(e);
                    }
                }
            }
        }

        private static bool SavePassword(string targetName, string description, string password) {
            if (noEntryPoint) {
                RuntimeUtil.SilentReportError("WinCred: failed to save password. no entry point.");
                return false;
            }

            if (targetName.Length > CRED_MAX_GENERIC_TARGET_NAME_LENGTH) {
                RuntimeUtil.SilentReportError("WinCred: failed to save password. too long target name.");
                return false;
            }
            if (description.Length > CRED_MAX_STRING_LENGTH) {
                RuntimeUtil.SilentReportError("WinCred: failed to save password. too long description.");
                return false;
            }

            // A password entered from the control panel is stored in UTF-16.
            // Store password in the same manner.
            byte[] blob = Encoding.Unicode.GetBytes(password);
            if (blob.Length > CRED_MAX_CREDENTIAL_BLOB_SIZE) {
                RuntimeUtil.SilentReportError("WinCred: failed to save password. too long blob.");
                return false;
            }
            try {
                bool r;
                unsafe {
                    fixed (byte* pBlob = blob) {
                        Credential credential = new Credential() {
                            Flags = CredFlags.CRED_FLAGS_NONE,
                            Type = CredType.CRED_TYPE_GENERIC,
                            TargetName = targetName,
                            Comment = description,
                            CredentialBlobSize = (uint)blob.Length,
                            CredentialBlob = (IntPtr)pBlob,
                            Persist = CredPersist.CRED_PERSIST_LOCAL_MACHINE,
                            AttributeCount = 0,
                            Attributes = IntPtr.Zero,
                            TargetAlias = null,
                            // The user can change the value of UserName from the control panel,
                            // but UserName is ignored in ReadPassword().
                            // To avoid confusion, a description string is used for UserName.
                            UserName = description,
                        };
                        r = CredWrite(ref credential, CredPreserve.CRED_PRESERVE_NONE);
                    }
                }
                if (!r) {
                    int err = Win32.GetLastError();
                    RuntimeUtil.SilentReportError("WinCred: CredWrite failed. error=" + err.ToString(NumberFormatInfo.InvariantInfo));
                }
                return r;
            }
            catch (Exception e) {
                if (e is System.EntryPointNotFoundException) {
                    noEntryPoint = true;
                }
                RuntimeUtil.SilentReportException(e);
                return false;
            }
        }

        private static bool DeletePassword(string targetName) {
            if (noEntryPoint) {
                RuntimeUtil.SilentReportError("WinCred: failed to delete password. no entry point.");
                return false;
            }

            if (targetName.Length > CRED_MAX_GENERIC_TARGET_NAME_LENGTH) {
                RuntimeUtil.SilentReportError("WinCred: failed to delete password. too long target name.");
                return false;
            }

            try {
                bool r = CredDelete(targetName, CredType.CRED_TYPE_GENERIC, 0);
                if (!r) {
                    int err = Win32.GetLastError();
                    RuntimeUtil.SilentReportError("WinCred: CredDelete failed. error=" + err.ToString(NumberFormatInfo.InvariantInfo));
                }
                return r;
            }
            catch (Exception e) {
                if (e is System.EntryPointNotFoundException) {
                    noEntryPoint = true;
                }
                RuntimeUtil.SilentReportException(e);
                return false;
            }
        }

        private static string BuildUserPasswordTargetName(string protocol, string host, int? port, string user) {
            string n = "Poderosa-" + protocol + " " + user + "@" + host;
            if (port.HasValue) {
                n += ":" + port.Value.ToString(NumberFormatInfo.InvariantInfo);
            }
            return n;
        }

        private static string BuildKeyFilePasswordTargetName(string protocol, string keyFilePath) {
            return "Poderosa-" + protocol + " key file (" + keyFilePath + ")";
        }

        #region WIN32API

        private enum CredType : uint {
            CRED_TYPE_GENERIC = 1u,
            CRED_TYPE_DOMAIN_PASSWORD = 2u,
            CRED_TYPE_DOMAIN_CERTIFICATE = 3u,
            CRED_TYPE_DOMAIN_VISIBLE_PASSWORD = 4u,
            CRED_TYPE_GENERIC_CERTIFICATE = 5u,
            CRED_TYPE_DOMAIN_EXTENDED = 6u,
        }

        [Flags]
        private enum CredFlags : uint {
            CRED_FLAGS_NONE = 0x0u,
            CRED_FLAGS_PASSWORD_FOR_CERT = 0x1u,
            CRED_FLAGS_PROMPT_NOW = 0x2u,
            CRED_FLAGS_USERNAME_TARGET = 0x4u,
            CRED_FLAGS_OWF_CRED_BLOB = 0x8u,
            CRED_FLAGS_REQUIRE_CONFIRMATION = 0x10u,
            CRED_FLAGS_WILDCARD_MATCH = 0x20u,
            CRED_FLAGS_VSM_PROTECTED = 0x40u,
            CRED_FLAGS_NGC_CERT = 0x80u,
        }

        private enum CredPersist : uint {
            CRED_PERSIST_NONE = 0u,
            CRED_PERSIST_SESSION = 1u,
            CRED_PERSIST_LOCAL_MACHINE = 2u,
            CRED_PERSIST_ENTERPRISE = 3u,
        }

        private enum CredPreserve : uint {
            CRED_PRESERVE_NONE = 0u,
            CRED_PRESERVE_CREDENTIAL_BLOB = 1u,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CredentialAttribute {
            public string Keyword;
            public uint Flags;
            public uint ValueSize;
            public IntPtr Value;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct Credential {
            public CredFlags Flags;
            public CredType Type;
            public string TargetName;
            public string Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public CredPersist Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string targetName, CredType type, uint reservedFlags, out IntPtr credential);
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite(ref Credential credential, CredPreserve flags);
        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);
        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string targetName, CredType type, uint reservedFlags);

        private const int CRED_MAX_GENERIC_TARGET_NAME_LENGTH = 32767;
        private const int CRED_MAX_STRING_LENGTH = 256;
        private const int CRED_MAX_CREDENTIAL_BLOB_SIZE = 5 * 512;

        private const int ERROR_NOT_FOUND = 1168;

        #endregion
    }
}
