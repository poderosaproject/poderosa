// Copyright 2019 The Poderosa Project.
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
using System.Linq;
using System.Text.RegularExpressions;

namespace Granados.Poderosa.Util {

    internal class FilePathValidatorException : Exception {
        public FilePathValidatorException(string path, bool isDirectory)
            : base(
                String.Format(
                    "Invalid {0} : {1}",
                    isDirectory ? "directory name" : "file name",
                    path == null ? "null" :
                        String.Join("",
                            path.Select(ch => (ch <= 0x1f) ? ("<" + ((uint)ch).ToString("X4")) + ">" : Char.ToString(ch)))
                )
            ) {
        }
    }

    /// <summary>
    /// File path validator
    /// </summary>
    internal class FilePathValidator {

        // assume windows environment
        private readonly char[] _pathSeparators = { '/' };
        private readonly Regex _inhibitedFileNameCharExceptSeparators = new Regex(@"[\\<>:""|?*\u0000-\u001f]");
        private readonly Regex _inhibitedFileNameChar = new Regex(@"[/\\<>:""|?*\u0000-\u001f]");
        private readonly Regex _inhibitedFileName = new Regex(@"\A(?:CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9]|.*\u0020|.+\.|)\z", RegexOptions.IgnoreCase);

        /// <summary>
        /// <para>Validate relative Unix path for using on Windows.</para>
        /// <para>
        /// The following paths are not allowed.
        /// <list type="bullet">
        /// <item><description>absolute path (containing drive letter, or starts with slash or backslash)</description></item>
        /// <item><description>path containing ".."</description></item>
        /// <item><description>path containing inhibited characters for the Windows file name</description></item>
        /// <item><description>path containing reserved file name in Windows</description></item>
        /// <item><description>path containing empty file name</description></item>
        /// <item><description>(when <paramref name="isDirectory"/> is false) file name is "."</description></item>
        /// <item><description>null or empty path</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Single dot (".") is allowed as the directory name.
        /// </para>
        /// </summary>
        /// <param name="path">file path or directory path.</param>
        /// <param name="isDirectory">true if the specified path was a directory path.</param>
        public void ValidateRelativeUnixPath(string path, bool isDirectory) {
            if (path == null) {
                throw new FilePathValidatorException(path, isDirectory);
            }

            if (_inhibitedFileNameCharExceptSeparators.IsMatch(path)) {
                throw new FilePathValidatorException(path, isDirectory);
            }

            string[] components = path.Split(_pathSeparators);
            if (components.FirstOrDefault(n => _inhibitedFileName.IsMatch(n)) != null) {
                throw new FilePathValidatorException(path, isDirectory);
            }

            if (!isDirectory && components[components.Length - 1] == ".") {
                throw new FilePathValidatorException(path, isDirectory);
            }
        }

        /// <summary>
        /// <para>Validate file name for using on Windows.</para>
        /// <para>
        /// The following file names are not allowed.
        /// <list type="bullet">
        /// <item><description>single dot (".")</description></item>
        /// <item><description>double dot ("..")</description></item>
        /// <item><description>name containing inhibited characters for the Windows file name</description></item>
        /// <item><description>reserved file name in Windows</description></item>
        /// <item><description>null or empty name</description></item>
        /// </list>
        /// </para>
        /// </summary>
        /// <param name="name">file name or directory name</param>
        /// <param name="isDirectory">true if the specified name was a directory name.</param>
        public void ValidateFileName(string name, bool isDirectory) {
            if (name == null
                || _inhibitedFileNameChar.IsMatch(name)
                || _inhibitedFileName.IsMatch(name)
                || name == ".") {
                throw new FilePathValidatorException(name, isDirectory);
            }
        }
    }
}
