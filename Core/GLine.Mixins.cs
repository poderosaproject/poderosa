// Copyright 2004-2018 The Poderosa Project.
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


namespace Poderosa.Document.Internal.Mixins {

    /// <summary>
    /// Extension methods for struct array.
    /// </summary>
    internal static class StructArrayMixin {

        /// <summary>
        /// Copy items to another array.
        /// </summary>
        /// <param name="srcArray">source array</param>
        /// <param name="dstArray">destination array</param>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this T[] srcArray, T[] dstArray) where T : struct {
            int len = srcArray.Length;
            for (int i = 0; i < len; i++) {
                dstArray[i] = srcArray[i];
            }
        }
    }

}
