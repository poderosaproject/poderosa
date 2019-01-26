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
#if UNITTEST
using System;
using System.Diagnostics;
using System.IO;

namespace Poderosa.TestUtils {

    public static class UnitTestUtil {
        public static void Trace(string text) {
            Console.Out.WriteLine(text);
            Debug.WriteLine(text);
        }

        public static void Trace(string fmt, params object[] args) {
            Trace(String.Format(fmt, args));
        }

        public static string DumpStructuredText(StructuredText st) {
            StringWriter wr = new StringWriter();
            new TextStructuredTextWriter(wr).Write(st);
            wr.Close();
            return wr.ToString();
        }
    }

}
#endif
