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

using Poderosa.Sessions;
using Poderosa.MacroInternal;

namespace Poderosa.MacroEngine {

    /// <summary>
    /// Binds MacroExecutor to a session.
    /// </summary>
    internal class SessionBinder : ISessionListener {

        private readonly Dictionary<ISession, MacroExecutor> executorMap = new Dictionary<ISession, MacroExecutor>();

        /// <summary>
        /// Binds MacroExecutor to a session.
        /// </summary>
        /// <param name="executor">macro executor</param>
        /// <param name="session">session bind to</param>
        /// <returns></returns>
        public bool Bind(MacroExecutor executor, ISession session) {
            lock (executorMap) {
                if (executorMap.ContainsKey(session))
                    return false;
                executorMap.Add(session, executor);
                return true;
            }
        }

        #region ISessionListener

        public void OnSessionStart(ISession session) {
            // do nothing
        }

        public void OnSessionEnd(ISession session) {
            MacroExecutor executor;
            bool found;
            lock (executorMap) {
                found = executorMap.TryGetValue(session, out executor);
                if (found)
                    executorMap.Remove(session);
            }
            if (found) {
                executor.Abort();
            }
        }

        #endregion
    }
}
