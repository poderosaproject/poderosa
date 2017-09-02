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
