// Copyright 2004-2017 The Poderosa Project.
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
using System.Threading;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net;

using Granados;
using Poderosa.Util;
using Poderosa.Forms;

namespace Poderosa.Protocols {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class CommunicationUtil {
        //cygwinの同期的接続
        public static ITerminalConnection CreateNewLocalShellConnection(IPoderosaForm form, ICygwinParameter param) {
            return LocalShellUtil.PrepareSocket(form, param);
        }


    }
}
