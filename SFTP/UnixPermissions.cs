/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: UnixPermissions.cs,v 1.1 2011/11/30 22:53:08 kzmi Exp $
 */
using System;
using System.Text;

namespace Poderosa.SFTP {

    /// <summary>
    /// Utility methods about file permissions on Unix.
    /// </summary>
    internal static class UnixPermissions {

        /// <summary>setuid (0004000)</summary>
        public const uint S_ISUID = 0x0800u;
        /// <summary>setgid (0002000)</summary>
        public const uint S_ISGID = 0x0400u;
        /// <summary>Sticky bit (0001000)</summary>
        public const uint S_ISTXT = 0x0200u;

        /// <summary>Read for owner (0000400)</summary>
        public const uint S_IRUSR = 0x0100u;
        /// <summary>Write for owner (0000200)</summary>
        public const uint S_IWUSR = 0x0080u;
        /// <summary>Execute for owner (0000100)</summary>
        public const uint S_IXUSR = 0x0040u;

        /// <summary>Read for group (0000040)</summary>
        public const uint S_IRGRP = 0x0020u;
        /// <summary>Write for group (0000020)</summary>
        public const uint S_IWGRP = 0x0010u;
        /// <summary>Execute for group (0000010)</summary>
        public const uint S_IXGRP = 0x0008u;

        /// <summary>Read for other (0000004)</summary>
        public const uint S_IROTH = 0x0004u;
        /// <summary>Write for other (0000002)</summary>
        public const uint S_IWOTH = 0x0002u;
        /// <summary>Execute for other (0000001)</summary>
        public const uint S_IXOTH = 0x0001u;

        /// <summary>File type mask (0170000)</summary>
        public const uint S_IFMT = 0xf000u;
        /// <summary>Named pipe (fifo) (0010000)</summary>
        public const uint S_IFIFO = 0x1000u;
        /// <summary>Character special file (0020000)</summary>
        public const uint S_IFCHR = 0x2000u;
        /// <summary>Directory (0040000)</summary>
        public const uint S_IFDIR = 0x4000u;
        /// <summary>Block special file (0060000)</summary>
        public const uint S_IFBLK = 0x6000u;
        /// <summary>Regular file (0100000)</summary>
        public const uint S_IFREG = 0x8000u;
        /// <summary>Symbolic link (0120000)</summary>
        public const uint S_IFLNK = 0xa000u;
        /// <summary>Socket (0140000)</summary>
        public const uint S_IFSOCK = 0xc000u;


        /// <summary>
        /// Gets whether the file is a regular file.
        /// </summary>
        /// <param name="permissions">Permission flag</param>
        /// <returns>True if the file is a regular file.</returns>
        public static bool IsRegularFile(uint permissions) {
            return (permissions & S_IFMT) == S_IFREG;
        }

        /// <summary>
        /// Gets whether the file is a directory.
        /// </summary>
        /// <param name="permissions">Permission flag</param>
        /// <returns>True if the file is a directory.</returns>
        public static bool IsDirectory(uint permissions) {
            return (permissions & S_IFMT) == S_IFDIR;
        }

        /// <summary>
        /// Gets whether the file is a symbolic link.
        /// </summary>
        /// <param name="permissions">Permission flag</param>
        /// <returns>True if the file is a symbolic link.</returns>
        public static bool IsSymbolicLink(uint permissions) {
            return (permissions & S_IFMT) == S_IFLNK;
        }

        /// <summary>
        /// Format unix permissions like the "ls -l" format.
        /// </summary>
        /// <param name="permissions">Permission flag</param>
        /// <returns>Text</returns>
        public static string Format(uint permissions) {
            StringBuilder s = new StringBuilder();
            switch (permissions & S_IFMT) {
                case S_IFDIR:
                    s.Append('d');
                    break;
                case S_IFCHR:
                    s.Append('c');
                    break;
                case S_IFBLK:
                    s.Append('b');
                    break;
                case S_IFREG:
                    s.Append('-');
                    break;
                case S_IFLNK:
                    s.Append('l');
                    break;
                case S_IFSOCK:
                    s.Append('s');
                    break;
                case S_IFIFO:
                    s.Append('p');
                    break;
                default:
                    s.Append('?');
                    break;
            }

            s.Append((permissions & S_IRUSR) != 0u ? 'r' : '-');
            s.Append((permissions & S_IWUSR) != 0u ? 'w' : '-');
            switch (permissions & (S_IXUSR | S_ISUID)) {
                case S_IXUSR:
                    s.Append('x');
                    break;
                case S_ISUID:
                    s.Append('S');
                    break;
                case (S_IXUSR | S_ISUID):
                    s.Append('s');
                    break;
                default:
                    s.Append('-');
                    break;
            }

            s.Append((permissions & S_IRGRP) != 0u ? 'r' : '-');
            s.Append((permissions & S_IWGRP) != 0u ? 'w' : '-');
            switch (permissions & (S_IXGRP | S_ISGID)) {
                case S_IXGRP:
                    s.Append('x');
                    break;
                case S_ISGID:
                    s.Append('S');
                    break;
                case (S_IXGRP | S_ISGID):
                    s.Append('s');
                    break;
                default:
                    s.Append('-');
                    break;
            }

            s.Append((permissions & S_IROTH) != 0u ? 'r' : '-');
            s.Append((permissions & S_IWOTH) != 0u ? 'w' : '-');
            switch (permissions & (S_IXOTH | S_ISTXT)) {
                case S_IXOTH:
                    s.Append('x');
                    break;
                case S_ISTXT:
                    s.Append('T');
                    break;
                case (S_IXOTH | S_ISTXT):
                    s.Append('t');
                    break;
                default:
                    s.Append('-');
                    break;
            }

            return s.ToString();
        }

    }

}
