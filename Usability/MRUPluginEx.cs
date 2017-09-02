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

namespace Poderosa.Usability {
    /// <summary>
    /// Add this attribute if you don't want to save the session informations in the Most-Recently-Used list.
    /// </summary>
    /// <remarks>
    /// Use this attribute for the concrete class of the following interfaces.
    /// If one of these objects has <see cref="ExcludeFromMRUAttribute"/>, session information is not saved in MRU.
    /// <list type="bullet">
    ///  <item><description><see cref="Poderosa.Sessions.ITerminalSession"/></description></item>
    ///  <item><description><see cref="Poderosa.Protocols.ITerminalParameter"/></description></item>
    /// </list>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class ExcludeFromMRUAttribute : Attribute {

        /// <summary>
        /// Constructor
        /// </summary>
        public ExcludeFromMRUAttribute() {
        }

    }

}
