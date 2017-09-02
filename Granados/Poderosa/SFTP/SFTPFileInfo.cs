// Copyright 2011-2017 The Poderosa Project.
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

namespace Granados.Poderosa.SFTP {

    /// <summary>
    /// SFTP remote file informations
    /// </summary>
    public class SFTPFileInfo : SFTPFileAttributes {

        private readonly string _fileName;
        private readonly string _longName;

        /// <summary>
        /// File name
        /// </summary>
        public string FileName {
            get {
                return _fileName;
            }
        }

        /// <summary>
        /// Long format line. Commonly this will be a result of ls -l.
        /// </summary>
        public string LongName {
            get {
                return _longName;
            }
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="fileName">file name</param>
        /// <param name="longName">long name</param>
        /// <param name="attributes">attributes</param>
        public SFTPFileInfo(string fileName, string longName, SFTPFileAttributes attributes)
            : base(attributes) {

            this._fileName = fileName;
            this._longName = longName;
        }
    }
}
