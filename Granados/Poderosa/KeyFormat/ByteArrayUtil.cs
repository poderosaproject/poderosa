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

    internal static class ByteArrayUtil {

        public static bool ByteArrayStartsWith(byte[] a1, byte[] a2) {
            if (a1 == null || a2 == null)
                return false;
            if (a1.Length < a2.Length)
                return false;
            for (int i = 0; i < a2.Length; i++) {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

        public static bool AreEqual(byte[] a1, byte[] a2) {
            if (a1 == null || a2 == null)
                return a1 == null && a2 == null;
            if (a1.Length != a2.Length)
                return false;
            for (int i = 0; i < a1.Length; i++) {
                if (a1[i] != a2[i])
                    return false;
            }
            return true;
        }

    }

}
