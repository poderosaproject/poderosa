// Copyright 2016-2017 The Poderosa Project.
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

using Poderosa.Forms;
using Poderosa.Protocols;
using Poderosa.Terminal;

namespace Poderosa.Sessions {

    /// <summary>
    /// Interface of the tab page for <see cref="OpenSessionDialog"/>.
    /// </summary>
    public interface IOpenSessionTabPage {

        /// <summary>
        /// Session type name
        /// </summary>
        string SessionTypeName {
            get;
        }

        /// <summary>
        /// Initialize the page
        /// </summary>
        /// <remarks>
        /// This method will be called in the constructor of the container dialog.
        /// </remarks>
        /// <param name="mainWindow">main window</param>
        void Initialize(IPoderosaMainWindow mainWindow);

        /// <summary>
        /// Set focus to the appropriate control.
        /// </summary>
        /// <returns>
        /// true if a control in this tab page was focused.
        /// If false was returned, parent form will set the focus on the "OK" button.
        /// </returns>
        bool SetFocus();

        /// <summary>
        /// Start opening session
        /// </summary>
        /// <remarks>
        /// The implementation of this method also do validation of the input values.
        /// </remarks>
        /// <param name="client">an instance who receive the result of opening session.</param>
        /// <param name="terminalSettings">terminal settings is set if this method returns true.</param>
        /// <param name="interruptable">an object for cancellation is set if this method returns true.</param>
        /// <returns>true if the opening session has been started, or false if failed.</returns>
        bool OpenSession(IInterruptableConnectorClient client, out ITerminalSettings terminalSettings, out IInterruptable interruptable);
    }
}
