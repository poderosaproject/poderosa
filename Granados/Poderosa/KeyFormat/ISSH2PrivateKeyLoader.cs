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

using Granados;
using Granados.PKI;

namespace Granados.Poderosa.KeyFormat {

    internal interface ISSH2PrivateKeyLoader {

        /// <summary>
        /// Read private key parameters.
        /// </summary>
        /// <param name="passphrase">passphrase for decrypt the key file</param>
        /// <param name="keyPair">key pair is set</param>
        /// <param name="comment">comment is set. empty if it didn't exist</param>
        /// <exception cref="SSHException">failed to parse</exception>
        void Load(
            string passphrase,
            out KeyPair keyPair,
            out string comment);

    }

}
