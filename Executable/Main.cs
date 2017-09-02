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

#if EXECUTABLE
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Poderosa.Util;
using Poderosa.Boot;
using Poderosa.Plugins;

using System.Text.RegularExpressions;

namespace Poderosa.Executable {
    internal class Root {
        private static IPoderosaApplication _poderosaApplication;

        public static void Run(string[] args) {
#if MONOLITHIC
            _poderosaApplication = PoderosaStartup.CreatePoderosaApplication(args, true);
#else
            _poderosaApplication = PoderosaStartup.CreatePoderosaApplication(args);
#endif
            if (_poderosaApplication != null) //アプリケーションが作成されなければ
                _poderosaApplication.Start();
        }

        //実行開始
        [STAThread]
        public static void Main(string[] args) {
            try {
                Run(args);
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }
    }

}
#endif