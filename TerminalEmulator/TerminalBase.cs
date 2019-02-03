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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa.Util;
using Poderosa.Sessions;
using Poderosa.Document;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Forms;
using Poderosa.View;

namespace Poderosa.Terminal {

    /// <summary>
    /// Mouse action
    /// </summary>
    public enum TerminalMouseAction {
        ButtonDown,
        ButtonUp,
        MouseMove,
        WheelUp,
        WheelDown,
    }

    //TODO 名前とは裏腹にあんまAbstractじゃねーな またフィールドが多すぎるので整理する。
    /// <summary>
    /// <ja>
    /// ターミナルエミュレータの中枢となるクラスです。
    /// </ja>
    /// <en>
    /// Class that becomes core of terminal emulator.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// このクラスの解説は、まだありません。
    /// </ja>
    /// <en>
    /// This class has not explained yet. 
    /// </en>
    /// </remarks>
    public abstract class AbstractTerminal : ICharProcessor, IByteAsyncInputStream {

        private readonly ScrollBarValues _scrollBarValues = new ScrollBarValues();
        private readonly TerminalDocument _document;
        private readonly IAbstractTerminalHost _session;
        private readonly LogService _logService;
        private readonly PromptRecognizer _promptRecognizer;
        private readonly IntelliSense _intelliSense;
        private readonly PopupStyleCommandResultRecognizer _commandResultRecognizer;
        private IModalTerminalTask _modalTerminalTask;
        private Cursor _documentCursor = null;

        private EncodingProfile _encodingProfile;
        private ICharDecoder _decoder;
        private UnicodeCharConverter _unicodeCharConverter;

        private bool _cleanup = false;

        protected readonly List<Action> _afterExitLockActions = new List<Action>();
        protected readonly GLineManipulator _manipulator = new GLineManipulator();
        protected TextDecoration _currentdecoration;
        protected TerminalMode _terminalMode;
        protected TerminalMode _cursorKeyMode; //_terminalModeは別物。AIXでのviで、カーソルキーは不変という例が確認されている

        protected abstract void ChangeMode(TerminalMode tm);
        protected abstract void ResetInternal();

        protected enum ProcessCharResult {
            Processed,
            Unsupported,
            Escaping
        }

        protected ProcessCharResult _processCharResult;

        //ICharDecoder
        public abstract void ProcessChar(char ch);

        internal abstract byte[] SequenceKeyData(Keys modifier, Keys body);

        internal abstract byte[] GetPasteLeadingBytes();

        internal abstract byte[] GetPasteTrailingBytes();

        public AbstractTerminal(TerminalInitializeInfo info) {
            TerminalEmulatorPlugin.Instance.LaterInitialize();

            _session = info.Session;

            //_invalidateParam = new InvalidateParam();
            _document = new TerminalDocument(info.InitialWidth, info.InitialHeight);
            _document.SetOwner(_session.ISession);

            _encodingProfile = EncodingProfile.Create(info.Session.TerminalSettings.Encoding);
            _decoder = new ISO2022CharDecoder(this, _encodingProfile);
            _unicodeCharConverter = _encodingProfile.CreateUnicodeCharConverter();
            _terminalMode = TerminalMode.Normal;
            _currentdecoration = TextDecoration.Default;
            _logService = new LogService(info.TerminalParameter, _session.TerminalSettings);
            _promptRecognizer = new PromptRecognizer(this);
            _intelliSense = new IntelliSense(this);
            _commandResultRecognizer = new PopupStyleCommandResultRecognizer(this);

            if (info.Session.TerminalSettings.LogSettings != null)
                _logService.ApplyLogSettings(_session.TerminalSettings.LogSettings, false);

            //event handlers
            ITerminalSettings ts = info.Session.TerminalSettings;
            ts.ChangeEncoding += delegate(EncodingType t) {
                this.Reset();
            };
            ts.ChangeRenderProfile += delegate(RenderProfile prof) {
                TerminalControl tc = _session.TerminalControl;
                if (tc != null)
                    tc.ApplyRenderProfile(prof);
            };
        }

        //XTERMを表に出さないためのメソッド
        public static AbstractTerminal Create(TerminalInitializeInfo info) {
            // We always creates XTerm instance because there are still cases that
            // XTerm's escape sequences are sent even if VT100 was specified as the terminal type.
            return new XTerm(info);
        }

        public IPoderosaDocument IDocument {
            get {
                return _document;
            }
        }
        public TerminalDocument GetDocument() {
            return _document;
        }
        protected ITerminalSettings GetTerminalSettings() {
            return _session.TerminalSettings;
        }
        protected ITerminalConnection GetConnection() {
            return _session.TerminalConnection;
        }
        private TerminalControl GetTerminalControl() {
            // Note that _session.TerminalControl may be null if another terminal
            // is selected by tab.
            return _session.TerminalControl;
        }
        protected RenderProfile GetRenderProfile() {
            ITerminalSettings settings = _session.TerminalSettings;
            if (settings.UsingDefaultRenderProfile)
                return GEnv.DefaultRenderProfile;
            else
                return settings.RenderProfile;
        }

        public TerminalMode TerminalMode {
            get {
                return _terminalMode;
            }
        }
        public TerminalMode CursorKeyMode {
            get {
                return _cursorKeyMode;
            }
        }
        public ILogService ILogService {
            get {
                return _logService;
            }
        }
        internal LogService LogService {
            get {
                return _logService;
            }
        }
        internal PromptRecognizer PromptRecognizer {
            get {
                return _promptRecognizer;
            }
        }
        internal IntelliSense IntelliSense {
            get {
                return _intelliSense;
            }
        }
        internal PopupStyleCommandResultRecognizer PopupStyleCommandResultRecognizer {
            get {
                return _commandResultRecognizer;
            }
        }
        public IShellCommandExecutor ShellCommandExecutor {
            get {
                return _commandResultRecognizer;
            }
        }
        public IAbstractTerminalHost TerminalHost {
            get {
                return _session;
            }
        }

        protected UnicodeCharConverter UnicodeCharConverter {
            get {
                return _unicodeCharConverter;
            }
        }

        /// <summary>
        /// A method called when a TerminalControl has been attached to the session.
        /// </summary>
        /// <param name="terminalControl">TerminalControl which is being attached</param>
        public void Attached(TerminalControl terminalControl) {
            if (_documentCursor != null)
                terminalControl.SetDocumentCursor(_documentCursor);
            else
                terminalControl.ResetDocumentCursor();
        }

        /// <summary>
        /// A method called when a TerminalControl is going to be detached from the session.
        /// </summary>
        /// <param name="terminalControl">TerminalControl which will be detached</param>
        public void Detach(TerminalControl terminalControl) {
            terminalControl.ResetDocumentCursor();
        }

        public void CloseBySession() {
            CleanupCommon();
        }

        protected virtual void ChangeCursorKeyMode(TerminalMode tm) {
            _cursorKeyMode = tm;
        }

        internal ScrollBarValues TransientScrollBarValues {
            get {
                return _scrollBarValues;
            }
        }

        #region ICharProcessor

        public bool NeedNextCharacter {
            get {
                return _processCharResult == ProcessCharResult.Escaping;
            }
        }
        public void UnsupportedCharSetDetected(char code) {
            string desc;
            if (code == '0')
                desc = "0 (DEC Special Character)"; //これはよくあるので但し書きつき
            else
                desc = new String(code, 1);

            CharDecodeError(String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.UnsupportedCharSet"), desc));
        }
        public void InvalidCharDetected(byte[] buf) {
            CharDecodeError(String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.UnexpectedChar"), _encodingProfile.Encoding.WebName));
        }
        #endregion

        //受信側からの簡易呼び出し
        protected void TransmitDirect(byte[] data) {
            _session.TerminalTransmission.Transmit(data);
        }

        protected void TransmitDirect(byte[] data, int offset, int length) {
            _session.TerminalTransmission.Transmit(data, offset, length);
        }

        //文字系のエラー通知
        protected void CharDecodeError(string msg) {
            IPoderosaMainWindow window = _session.OwnerWindow;
            if (window == null)
                return;
            Debug.Assert(window.AsForm().InvokeRequired);

            Monitor.Exit(GetDocument()); //これは忘れるな
            switch (GEnv.Options.CharDecodeErrorBehavior) {
                case WarningOption.StatusBar:
                    window.StatusBar.SetMainText(msg);
                    break;
                case WarningOption.MessageBox:
                    window.AsForm().Invoke(new CharDecodeErrorDialogDelegate(CharDecodeErrorDialog), window, msg);
                    break;
            }
            Monitor.Enter(GetDocument());
        }
        private delegate void CharDecodeErrorDialogDelegate(IPoderosaMainWindow window, string msg);
        private void CharDecodeErrorDialog(IPoderosaMainWindow window, string msg) {
            WarningWithDisableOption dlg = new WarningWithDisableOption(msg);
            dlg.ShowDialog(window.AsForm());
            if (dlg.CheckedDisableOption) {
                GEnv.Options.CharDecodeErrorBehavior = WarningOption.Ignore;
            }
        }

        public void Reset() {
            var currentEncodingSetting = GetTerminalSettings().Encoding;
            if (_encodingProfile.Type != currentEncodingSetting) {
                _encodingProfile = EncodingProfile.Create(currentEncodingSetting);
                _decoder = new ISO2022CharDecoder(this, _encodingProfile);
                _unicodeCharConverter = _encodingProfile.CreateUnicodeCharConverter();
            }
        }

        //これはメインスレッドから呼び出すこと
        public virtual void FullReset() {
            lock (_document) {
                ChangeMode(TerminalMode.Normal);
                _document.ClearScrollingRegion();
                ResetInternal();
                _encodingProfile = EncodingProfile.Create(GetTerminalSettings().Encoding);
                _decoder = new ISO2022CharDecoder(this, _encodingProfile);
                _unicodeCharConverter = _encodingProfile.CreateUnicodeCharConverter();
            }
        }

        //ModalTerminalTask周辺
        public virtual void StartModalTerminalTask(IModalTerminalTask task) {
            _modalTerminalTask = task;
            new ModalTerminalTaskSite(this).Start(task);
        }
        public virtual void EndModalTerminalTask() {
            _modalTerminalTask = null;
        }
        public IModalTerminalTask CurrentModalTerminalTask {
            get {
                return _modalTerminalTask;
            }
        }

        //コマンド結果の処理割り込み
        public void ProcessCommandResult(ICommandResultProcessor processor, bool start_with_linebreak) {
            _commandResultRecognizer.StartCommandResultProcessor(processor, start_with_linebreak);
        }

        /// <summary>
        /// Hande mouse action.
        /// </summary>
        /// <param name="action">Action type</param>
        /// <param name="button">Which mouse button caused the event</param>
        /// <param name="modifierKeys">Modifier keys (Shift, Ctrl or Alt) being pressed</param>
        /// <param name="row">Row index (zero based)</param>
        /// <param name="col">Column index (zero based)</param>
        /// <returns>True if mouse action was processed.</returns>
        public virtual bool ProcessMouse(TerminalMouseAction action, MouseButtons button, Keys modifierKeys, int row, int col) {
            return false;
        }

        public virtual bool GetFocusReportingMode() {
            return false;
        }

        #region IByteAsyncInputStream
        public void OnReception(ByteDataFragment data) {
            try {
                bool pass_to_terminal = true;
                if (_modalTerminalTask != null) {
                    bool show_input = _modalTerminalTask.ShowInputInTerminal;
                    _modalTerminalTask.OnReception(data);
                    if (!show_input)
                        pass_to_terminal = false; //入力を見せない(XMODEMとか)のときはターミナルに与えない
                }

                //バイナリログの出力
                _logService.BinaryLogger.Write(data);

                if (pass_to_terminal) {
                    TerminalDocument document = _document;
                    lock (document) {

                        _manipulator.Load(document.CurrentLine, 0);
                        _manipulator.CaretColumn = document.CaretColumn;

                        _decoder.OnReception(data);

                        document.UpdateCurrentLine(_manipulator);
                        document.CaretColumn = _manipulator.CaretColumn;

                        CheckDiscardDocument();
                        AdjustTransientScrollBar();

                        //現在行が下端に見えるようなScrollBarValueを計算
                        int n = document.CurrentLineNumber - document.TerminalHeight + 1 - document.FirstLineNumber;
                        if (n < 0)
                            n = 0;

                        //Debug.WriteLine(String.Format("E={0} C={1} T={2} H={3} LC={4} MAX={5} n={6}", _transientScrollBarEnabled, _tag.Document.CurrentLineNumber, _tag.Document.TopLineNumber, _tag.Connection.TerminalHeight, _transientScrollBarLargeChange, _transientScrollBarMaximum, n));
                        if (IsAutoScrollMode(n)) {
                            _scrollBarValues.Value = n;
                            document.TopLineNumber = n + document.FirstLineNumber;
                        }
                        else
                            _scrollBarValues.Value = document.TopLineNumber - document.FirstLineNumber;

                        //Invalidateをlockの外に出す。このほうが安全と思われた

                        //受信スレッド内ではマークをつけるのみ。タイマーで行うのはIntelliSenseに副作用あるので一時停止
                        //_promptRecognizer.SetContentUpdateMark();
                        _promptRecognizer.Recognize();
                    }

                    if (_afterExitLockActions.Count > 0) {
                        Control main = _session.OwnerWindow.AsControl();
                        foreach (Action action in _afterExitLockActions) {
                            main.Invoke(action);
                        }
                        _afterExitLockActions.Clear();
                    }
                }

                if (_modalTerminalTask != null)
                    _modalTerminalTask.NotifyEndOfPacket();
                _session.NotifyViewsDataArrived();
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }

        public void OnAbnormalTermination(string msg) {
            //TODO メッセージを GEnv.Strings.GetString("Message.TerminalDataReceiver.GenericError"),_tag.Connection.Param.ShortDescription, msg
            if (!GetConnection().IsClosed) { //閉じる指令を出した後のエラーは表示しない
                GetConnection().Close();
                ShowAbnormalTerminationMessage();
            }
            Cleanup(msg);
        }
        private void ShowAbnormalTerminationMessage() {
            IPoderosaMainWindow window = _session.OwnerWindow;
            if (window != null) {
                Debug.Assert(window.AsForm().InvokeRequired);
                ITCPParameter tcp = (ITCPParameter)GetConnection().Destination.GetAdapter(typeof(ITCPParameter));
                if (tcp != null) {
                    string msg = String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.TCPDisconnected"), tcp.Destination);

                    switch (GEnv.Options.DisconnectNotification) {
                        case WarningOption.StatusBar:
                            window.StatusBar.SetMainText(msg);
                            break;
                        case WarningOption.MessageBox:
                            window.Warning(msg); //TODO Disableオプションつきのサポート
                            break;
                    }
                }
            }
        }

        public void OnNormalTermination() {
            Cleanup(null);
        }
        #endregion

        private void Cleanup(string msg) {
            CleanupCommon();
            //NOTE _session.CloseByReceptionThread()は、そのままアプリ終了と直結する場合がある。すると、_logService.Close()の処理が終わらないうちに強制終了になってログが書ききれない可能性がある
            _session.CloseByReceptionThread(msg);
        }

        private void CleanupCommon() {
            if (!_cleanup) {
                _cleanup = true;
                TerminalEmulatorPlugin.Instance.ShellSchemeCollection.RemoveDynamicChangeListener((IShellSchemeDynamicChangeListener)GetTerminalSettings().GetAdapter(typeof(IShellSchemeDynamicChangeListener)));
                _logService.Close(_document.CurrentLine);
            }
        }

        private bool IsAutoScrollMode(int value_candidate) {
            TerminalDocument doc = _document;
            return _terminalMode == TerminalMode.Normal &&
                doc.CurrentLineNumber >= doc.TopLineNumber + doc.TerminalHeight - 1 &&
                (!_scrollBarValues.Enabled || value_candidate + _scrollBarValues.LargeChange > _scrollBarValues.Maximum);
        }
        private void CheckDiscardDocument() {
            if (_session == null || _terminalMode == TerminalMode.Application)
                return;

            TerminalDocument document = _document;
            int del = document.DiscardOldLines(GEnv.Options.TerminalBufferSize + document.TerminalHeight);
            if (del > 0) {
                int newvalue = _scrollBarValues.Value - del;
                if (newvalue < 0)
                    newvalue = 0;
                _scrollBarValues.Value = newvalue;
                document.InvalidatedRegion.InvalidatedAll = true; //本当はここまでしなくても良さそうだが念のため
            }
        }

        public void AdjustTransientScrollBar() {
            TerminalDocument document = _document;
            int paneheight = document.TerminalHeight;
            int docheight = Math.Max(document.LastLineNumber, document.TopLineNumber + paneheight - 1) - document.FirstLineNumber + 1;

            _scrollBarValues.Dirty = true;
            if ((_terminalMode == TerminalMode.Application && !GEnv.Options.AllowsScrollInAppMode)
                || paneheight >= docheight) {
                _scrollBarValues.Enabled = false;
                _scrollBarValues.Value = 0;
            }
            else {
                _scrollBarValues.Enabled = true;
                _scrollBarValues.Maximum = docheight - 1;
                _scrollBarValues.LargeChange = paneheight;
            }
            //Debug.WriteLine(String.Format("E={0} V={1}", _transientScrollBarEnabled, _transientScrollBarValue));
        }

        public void SetTransientScrollBarValue(int value) {
            _scrollBarValues.Value = value;
            _scrollBarValues.Dirty = true;
        }

        public void CommitScrollBar(VScrollBar sb, bool dirty_only) {
            if (dirty_only && !_scrollBarValues.Dirty)
                return;

            sb.Enabled = _scrollBarValues.Enabled;
            sb.Maximum = _scrollBarValues.Maximum;
            sb.LargeChange = _scrollBarValues.LargeChange;
            //!!本来このif文は不要なはずだが、範囲エラーになるケースが見受けられた。その原因を探ってリリース直前にいろいろいじるのは危険なのでここは逃げる。後でちゃんと解明する。
            if (_scrollBarValues.Value < _scrollBarValues.Maximum)
                sb.Value = _scrollBarValues.Value;
            _scrollBarValues.Dirty = false;
        }

        //ドキュメントロック中でないと呼んではだめ
        public void IndicateBell() {
            IPoderosaMainWindow window = _session.OwnerWindow;
            if (window != null) {
                Debug.Assert(window.AsForm().InvokeRequired);
                Monitor.Exit(GetDocument());
                window.StatusBar.SetStatusIcon(Poderosa.TerminalEmulator.Properties.Resources.Bell16x16);
                Monitor.Enter(GetDocument());
            }
            if (GEnv.Options.BeepOnBellChar)
                Win32.MessageBeep(-1);
        }

        protected void SetDocumentCursor(Cursor cursor) {
            _documentCursor = cursor;
            TerminalControl terminalControl = GetTerminalControl();
            if (terminalControl != null) {
                terminalControl.SetDocumentCursor(cursor);
            }
        }

        protected void ResetDocumentCursor() {
            _documentCursor = null;
            TerminalControl terminalControl = GetTerminalControl();
            if (terminalControl != null) {
                terminalControl.ResetDocumentCursor();
            }
        }
    }

    //受信スレッドから次に設定すべきScrollBarの値を配置する。
    internal class ScrollBarValues {
        //受信スレッドでこれらの値を設定し、次のOnPaint等メインスレッドでの実行でCommitする
        private bool _dirty; //これが立っていると要設定
        private bool _enabled;
        private int _value;
        private int _largeChange;
        private int _maximum;

        public bool Dirty {
            get {
                return _dirty;
            }
            set {
                _dirty = value;
            }
        }
        public bool Enabled {
            get {
                return _enabled;
            }
            set {
                _enabled = value;
            }
        }
        public int Value {
            get {
                return _value;
            }
            set {
                _value = value;
            }
        }
        public int LargeChange {
            get {
                return _largeChange;
            }
            set {
                _largeChange = value;
            }
        }
        public int Maximum {
            get {
                return _maximum;
            }
            set {
                _maximum = value;
            }
        }
    }


    internal class UnknownEscapeSequenceException : Exception {
        public UnknownEscapeSequenceException(string msg)
            : base(msg) {
        }
    }

    internal struct IntPair {
        public int first;
        public int second;

        public IntPair(int f, int s) {
            first = f;
            second = s;
        }
    }
}
