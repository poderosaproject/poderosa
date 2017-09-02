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
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa.Sessions;

namespace Poderosa.Terminal {
    internal class KeepAlive {
        private Timer _timer;
        private int _prevInterval;

        public KeepAlive() {
            _prevInterval = 0;
        }

        public void Refresh(int interval) {
            bool first = false;
            if (_timer == null) {
                first = true;
                _timer = new Timer();
                _timer.Tick += new EventHandler(OnTimer);
            }

            if (!first && _prevInterval == interval)
                return; //既存設定と変更のない場合は何もしない

            if (interval > 0) {
                _timer.Interval = interval;
                _timer.Start();
            }
            else
                _timer.Stop();

            _prevInterval = interval;
        }


        private void OnTimer(object sender, EventArgs args) {
            //TODO アプリケーション内部イベントログでも作ってこういうのは記録していくのがいいのか？
            foreach (ISession s in TerminalEmulatorPlugin.Instance.GetSessionManager().AllSessions) {
                IAbstractTerminalHost ts = (IAbstractTerminalHost)s.GetAdapter(typeof(IAbstractTerminalHost));
                if (ts != null && ts.TerminalConnection != null && ts.TerminalConnection.TerminalOutput != null) {
                    ts.TerminalConnection.TerminalOutput.SendKeepAliveData();
                }
            }
        }


    }
}
