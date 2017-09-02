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

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.View;
using Poderosa.Commands;
using Poderosa.Sessions;

//選択領域の管理と、選択したものに関わる基本コマンド（コピーなど）の実装

namespace Poderosa.Forms {
    internal class SelectionService : ISelectionService {

        private WindowManagerPlugin _parent;
        private SelectedTextCopyCommand _copyCommand;

        public SelectionService(WindowManagerPlugin parent) {
            _parent = parent;
            _copyCommand = new SelectedTextCopyCommand();
        }

        public ISelection ActiveSelection {
            get {
                IPoderosaMainWindow window = _parent.ActiveWindow;
                if (window == null)
                    return null;

                IPoderosaView view = window.LastActivatedView;
                if (view == null)
                    return null;

                return view.CurrentSelection;
            }
        }

        public IPoderosaCommand DefaultCopyCommand {
            get {
                return _copyCommand;
            }
        }
    }
}
