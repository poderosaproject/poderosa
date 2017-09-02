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
using System.Collections.Generic;
using System.Text;

namespace Granados.Poderosa.KeyFormat {

    /// <summary>
    /// SSH private key file format
    /// </summary>
    internal enum PrivateKeyFileFormat {
        UNKNOWN,
        SSH1,
        SSH2_OPENSSH,
        SSH2_SSHCOM,
        SSH2_PUTTY,
    }

}
