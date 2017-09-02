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

using Granados.Mono.Math;

namespace Granados.Poderosa.KeyFormat {

    internal interface ISSH1PrivateKeyLoader {

        /// <summary>
        /// Read private key parameters.
        /// </summary>
        /// <param name="passphrase">passphrase for decrypt the key file</param>
        /// <param name="modulus">private key parameter is set</param>
        /// <param name="publicExponent">private key parameter is set</param>
        /// <param name="privateExponent">private key parameter is set</param>
        /// <param name="primeP">private key parameter is set</param>
        /// <param name="primeQ">private key parameter is set</param>
        /// <param name="crtCoefficient">private key parameter is set</param>
        /// <param name="comment">comment is set</param>
        /// <exception cref="SSHException">failed to parse</exception>
        void Load(
            string passphrase,
            out BigInteger modulus,
            out BigInteger publicExponent,
            out BigInteger privateExponent,
            out BigInteger primeP,
            out BigInteger primeQ,
            out BigInteger crtCoefficient,
            out string comment);

    }

}
