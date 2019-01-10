// Copyright 2019 The Poderosa Project.
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
using System.Linq;
using System.Reflection;

namespace Poderosa.Terminal.EscapeSequenceEngine {

    /// <summary>
    /// Method attribute for specifying escape-sequence pattern.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    internal class ESPatternAttribute : Attribute {
        private readonly string _pattern;

        public string Pattern {
            get {
                return _pattern;
            }
        }

        public ESPatternAttribute(string pattern) {
            this._pattern = pattern;
        }
    }

    /// <summary>
    /// DfaEngine factory
    /// </summary>
    /// <typeparam name="T">executor class</typeparam>
    internal static class DfaEngineFactory<T> where T : IEscapeSequenceExecutor {

        // DfaStateManager instance for T
        private static readonly Lazy<DfaStateManager> dfaStateManager =
            new Lazy<DfaStateManager>(CreateDfaStateManager, true);

        public static void Prepare() {
            var stateManager = dfaStateManager.Value;
        }

        public static DfaEngine CreateDfaEngine(T targetExecutor) {
            var stateManager = dfaStateManager.Value;
            return new DfaEngine(stateManager, targetExecutor);
        }

        private static DfaStateManager CreateDfaStateManager() {
            var nfaManager = new NfaManager();

            foreach (var method in typeof(T).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (!method.IsPublic && !method.IsAssembly) {
                    continue;
                }

                if (method.ReturnType != typeof(void)) {
                    continue;
                }

                var paramInfos = method.GetParameters();
                if (paramInfos.Length != 1 || paramInfos[0].ParameterType != typeof(EscapeSequenceContext)) {
                    continue;
                }

                var patterns =
                    method.GetCustomAttributes<ESPatternAttribute>(true)
                    .Select(attr => attr.Pattern)
                    .Distinct();
                foreach (var pattern in patterns) {
                    MethodInfo methodToCall = method;
                    Action<EscapeSequenceContext> action =
                        (context) => {
                            methodToCall.Invoke(context.Executor, new object[] { context });
                        };
                    nfaManager.AddPattern(pattern, action);
                }
            }

            return nfaManager.CreateDfa();
        }
    }
}
