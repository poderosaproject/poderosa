/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Win32Exception.cs,v 1.1 2011/08/04 15:28:58 kzmi Exp $
 */
using System;

namespace Poderosa.Pipe {

    internal class Win32Exception : Exception {

        public Win32Exception(string api, int lastError)
            : base(Format(api, lastError)) {
        }

        public Win32Exception(string api, int lastError, string information)
            : base(Format(api, lastError, information)) {
        }

        private static string Format(string api, int lastError) {
            return String.Format("Error in {0}. Error = {1} (0x{1:X8})", api, lastError);
        }

        private static string Format(string api, int lastError, string information) {
            return String.Format("Error in {0}. Error = {1} (0x{1:X8}) {2}", api, lastError, information);
        }
    }

}
