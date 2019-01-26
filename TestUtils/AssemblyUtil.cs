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
#if UNITTEST
using System;
using System.Reflection;

namespace Poderosa.TestUtils {

    public static class AssemblyUtil {

        /// <summary>
        /// Set assembly object that Assembly.GetEntryAssembly() returns.
        /// </summary>
        /// <param name="type">specifies type defined in the target assembly.</param>
        public static void SetEntryAssembly(Type type) {
            SetEntryAssembly(Assembly.GetAssembly(type));
        }

        /// <summary>
        /// Set assembly object that Assembly.GetEntryAssembly() returns.
        /// </summary>
        /// <param name="assemblyToSet">assembly object to set</param>
        public static void SetEntryAssembly(Assembly assemblyToSet) {
            Assembly entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null) {
                AppDomainManager manager = AppDomain.CurrentDomain.DomainManager;
                if (manager == null) {
                    manager = new AppDomainManager();
                    FieldInfo domainManagerField = typeof(AppDomain).GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
                    domainManagerField.SetValue(AppDomain.CurrentDomain, manager);
                }

                FieldInfo entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
                entryAssemblyfield.SetValue(manager, assemblyToSet);

                entryAssembly = Assembly.GetEntryAssembly();
                Console.WriteLine("entry assembly : {0}", entryAssembly);
            }
        }
    }
}
#endif
