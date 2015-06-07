/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Main.cs,v 1.3 2011/12/23 19:22:06 kzmi Exp $
 */
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