// Copyright 2004-2025 The Poderosa Project.
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public delegate void AfterExitLockDelegate();

        private readonly LogService _logService;
        private readonly ScrollBarValues _scrollBarValues;
        private readonly TerminalDocument _document;
        private readonly IAbstractTerminalHost _session;
        private readonly PromptRecognizer _promptRecognizer;
        private readonly IntelliSense _intelliSense;
        private readonly PopupStyleCommandResultRecognizer _commandResultRecognizer;

        private EncodingProfile _encodingProfile;
        private ICharDecoder _decoder;
        private UnicodeCharConverter _unicodeCharConverter;
        private IModalTerminalTask _modalTerminalTask;

        private bool _cleanup = false;

        protected readonly ConcurrentQueue<AfterExitLockDelegate> _afterExitLockActions;
        protected readonly GLineManipulator _manipulator;
        protected TerminalMode _terminalMode;
        protected TerminalMode _cursorKeyMode; //_terminalModeは別物。AIXでのviで、カーソルキーは不変という例が確認されている
        protected LineContinuationMode _lineContinuationMode;

        protected abstract void ChangeMode(TerminalMode tm);
        protected abstract void FullResetInternal();
        protected abstract void SoftResetInternal();

        //ICharDecoder
        public abstract void ProcessChar(char ch);

        internal abstract byte[] SequenceKeyData(Keys modifier, Keys body);

        internal abstract byte[] GetPasteLeadingBytes();

        internal abstract byte[] GetPasteTrailingBytes();

        public AbstractTerminal(TerminalInitializeInfo info) {
            TerminalEmulatorPlugin.Instance.LaterInitialize();

            _session = info.Session;

            //_invalidateParam = new InvalidateParam();

            _logService = new LogService();
            _logService.SetupDefaultLogger(GEnv.Options, info.TerminalParameter, info.Session.TerminalSettings);
            if (info.Session.TerminalSettings.LogSettings != null) {
                _logService.ApplyLogSettings(info.Session.TerminalSettings.LogSettings, false);
            }

            _document = new TerminalDocument(info.InitialWidth, info.InitialHeight, _logService);
            _document.SetOwner(_session.ISession);
            _afterExitLockActions = new ConcurrentQueue<AfterExitLockDelegate>();

            _encodingProfile = EncodingProfile.Create(info.Session.TerminalSettings.Encoding);
            _decoder = new ISO2022CharDecoder(this, _encodingProfile);
            _unicodeCharConverter = _encodingProfile.CreateUnicodeCharConverter();
            _terminalMode = TerminalMode.Normal;
            _lineContinuationMode = _session.TerminalSettings.LineContinuationMode;
            _manipulator = new GLineManipulator(_document.GLineZOrderManager);
            _scrollBarValues = new ScrollBarValues();
            _promptRecognizer = new PromptRecognizer(this);
            _intelliSense = new IntelliSense(this);
            _commandResultRecognizer = new PopupStyleCommandResultRecognizer(this);

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
            ts.ChangeLineContinuationMode += delegate(LineContinuationMode mode) {
                this.Reset();
            };
        }

        public static AbstractTerminal Create(TerminalInitializeInfo info) {
            return new XTerm(info);
        }

        public IPoderosaDocument IDocument {
            get {
                return _document;
            }
        }

        public TerminalDocument Document {
            get {
                return _document;
            }
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

        protected ICharacterSetManager CharacterSetManager {
            get {
                return _decoder;
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
        }

        /// <summary>
        /// A method called when a TerminalControl is going to be detached from the session.
        /// </summary>
        /// <param name="terminalControl">TerminalControl which will be detached</param>
        public void Detach(TerminalControl terminalControl) {
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
        public abstract bool IsEscapeSequenceReading {
            get;
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

            TerminalDocument doc = Document;
            Monitor.Exit(doc);
            switch (GEnv.Options.CharDecodeErrorBehavior) {
                case WarningOption.StatusBar:
                    window.StatusBar.SetMainText(msg);
                    break;
                case WarningOption.MessageBox:
                    window.AsForm().Invoke(new CharDecodeErrorDialogDelegate(CharDecodeErrorDialog), window, msg);
                    break;
            }
            Monitor.Enter(doc);
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

        public void FullReset() {
            lock (_document) {
                ChangeMode(TerminalMode.Normal);
                ChangeCursorKeyMode(TerminalMode.Normal);
                _document.ClearMargins();
                _document.CurrentDecoration = TextDecoration.Default;
                _encodingProfile = EncodingProfile.Create(GetTerminalSettings().Encoding);
                _decoder = new ISO2022CharDecoder(this, _encodingProfile);
                _unicodeCharConverter = _encodingProfile.CreateUnicodeCharConverter();
                _document.KeySendLocked = false;
                _document.ForceNewLine = false;
                _document.ShowCaret = true;
                _document.SixelImageManager.ResetSharedPalette();
                FullResetInternal();
                _document.InvalidateAll();
            }
        }

        public void SoftReset() {
            lock (_document) {
                ChangeMode(TerminalMode.Normal);
                _document.ClearMargins();
                _document.CurrentDecoration = _document.CurrentDecoration.GetCopyWithProtected(false);
                _document.KeySendLocked = false;
                _document.ShowCaret = true;
                SoftResetInternal();
                _document.InvalidateAll();
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
        /// Handle mouse action.
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

                        _manipulator.Load(document.CurrentLine);

                        _decoder.OnReception(data);

                        // this ensures that the character cell at the caret position exists
                        _manipulator.ExpandBuffer(Document.TerminalWidth);

                        document.UpdateCurrentLine(_manipulator);

                        CheckDiscardDocument();
                        AdjustTransientScrollBar();

                        //現在行が下端に見えるようなScrollBarValueを計算
                        int n = document.CurrentLineNumber - document.TerminalHeight + 1 - document.FirstLineNumber;
                        if (n < 0)
                            n = 0;

                        //Debug.WriteLine(String.Format("E={0} C={1} T={2} H={3} LC={4} MAX={5} n={6}", _transientScrollBarEnabled, _tag.Document.CurrentLineNumber, _tag.Document.TopLineNumber, _tag.Connection.TerminalHeight, _transientScrollBarLargeChange, _transientScrollBarMaximum, n));
                        if (IsAutoScrollMode(n)) {
                            _scrollBarValues.Value = n;
                            document.SetViewTopLineNumber(document.FirstLineNumber + n);
                        }
                        else {
                            _scrollBarValues.Value = document.ViewTopLineNumber - document.FirstLineNumber;
                        }

                        //Invalidateをlockの外に出す。このほうが安全と思われた

                        //受信スレッド内ではマークをつけるのみ。タイマーで行うのはIntelliSenseに副作用あるので一時停止
                        //_promptRecognizer.SetContentUpdateMark();
                        _promptRecognizer.Recognize();
                    }

                    if (_afterExitLockActions.Count > 0) {
                        Control main = _session.OwnerWindow.AsControl();
                        AfterExitLockDelegate action;
                        while (_afterExitLockActions.TryDequeue(out action)) {
                            main.Invoke(action);
                        }
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

        public void IndicateBell() {
            IPoderosaMainWindow window = _session.OwnerWindow;
            if (window != null) {
                // If this thread is not a GUI thread, SetStatusIcon() calls Form.Invoke() and waits for completion of the task in the GUI thread.
                // However, if a document lock has already been acquired by this thread, a deadlock will occur, because the GUI thread will also
                // wait for the acquisition of the document lock to redraw the screen.
                var document = Document;
                bool isLocked = Monitor.IsEntered(document);
                if (isLocked) {
                    Monitor.Exit(document);
                }
                window.StatusBar.SetStatusIcon(Poderosa.TerminalEmulator.Properties.Resources.Bell16x16);
                if (isLocked) {
                    Monitor.Enter(document);
                }
            }

            if (GEnv.Options.BeepOnBellChar) {
                Win32.MessageBeep(-1);
            }
        }

        protected void ChangeUICursor(Cursor uiCursor) {
            Document.UICursor = uiCursor;
            TerminalControl terminalControl = GetTerminalControl();
            if (terminalControl != null) {
                terminalControl.ChangeUICursor(uiCursor);
            }
        }
    }

    /// <summary>
    /// Represents the row number; vertical position on the screen, 1-based.
    /// </summary>
    public struct Row {
        public readonly int Value;

        public Row(int v) {
            Value = v;
        }

        public Row Clamp(int min, int max) {
            return new Row(Math.Min(Math.Max(Value, min), max));
        }

        public Row ClipLower(int min) {
            return new Row(Math.Max(Value, min));
        }

        public Row ClipUpper(int max) {
            return new Row(Math.Min(Value, max));
        }

        public string ToInvariantString() {
            return Value.ToInvariantString();
        }

        public static Row operator +(Row r, int n) {
            return new Row(r.Value + n);
        }

        public static Row operator -(Row r, int n) {
            return new Row(r.Value - n);
        }
    }

    /// <summary>
    /// Represents the column number; horizontal position on the screen, 1-based.
    /// </summary>
    public struct Col {
        public readonly int Value;

        public Col(int v) {
            Value = v;
        }

        public Col Clamp(int min, int max) {
            return new Col(Math.Min(Math.Max(Value, min), max));
        }

        public Col ClipLower(int min) {
            return new Col(Math.Max(Value, min));
        }

        public Col ClipUpper(int max) {
            return new Col(Math.Min(Value, max));
        }

        public string ToInvariantString() {
            return Value.ToInvariantString();
        }

        public static Col operator +(Col r, int n) {
            return new Col(r.Value + n);
        }

        public static Col operator -(Col r, int n) {
            return new Col(r.Value - n);
        }
    }

    public static class RowColMixin {
        public static Row AsRow(this int v) {
            return new Row(v);
        }

        public static Col AsCol(this int v) {
            return new Col(v);
        }
    }

    internal static class ControlCode {
        public const char NUL = '\u0000';
        public const char ENQ = '\u0005';
        public const char BEL = '\u0007';
        public const char BS = '\u0008';
        public const char HT = '\u0009';
        public const char LF = '\u000a';
        public const char VT = '\u000b';
        public const char FF = '\u000c';
        public const char CR = '\u000d';
        public const char SO = '\u000e';
        public const char SI = '\u000f';
        public const char CAN = '\u0018';
        public const char SUB = '\u001a';
        public const char ESC = '\u001b';
        public const char DEL = '\u007f';
        public const char IND = '\u0084';
        public const char NEL = '\u0085';
        public const char HTS = '\u0088';
        public const char RI = '\u008d';
        public const char SS2 = '\u008e';
        public const char SS3 = '\u008f';
        public const char DCS = '\u0090';
        public const char SPA = '\u0096';
        public const char EPA = '\u0097';
        public const char SOS = '\u0098';
        public const char DECID = '\u009a';
        public const char CSI = '\u009b';
        public const char ST = '\u009c';
        public const char OSC = '\u009d';
        public const char PM = '\u009e';
        public const char APC = '\u009f';

        private static readonly string[] _controlCodeNameTable;

        static ControlCode() {
            List<string> map = new List<string>();
            foreach (FieldInfo f in typeof(ControlCode).GetFields(BindingFlags.Public | BindingFlags.Static)) {
                if (f.IsLiteral && f.FieldType == typeof(char)) {
                    char value = (char)f.GetValue(null);
                    while (map.Count <= value) {
                        map.Add(null);
                    }
                    map[value] = f.Name;
                }
            }
            _controlCodeNameTable = map.ToArray();
        }

        public static string ToName(char ch) {
            return (ch < _controlCodeNameTable.Length) ? _controlCodeNameTable[ch] : null;
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

    internal interface ICharProcessor {
        void ProcessChar(char ch);
        bool IsEscapeSequenceReading {
            get;
        }
        void UnsupportedCharSetDetected(char code);
        void InvalidCharDetected(byte[] data);
    }

    internal class TabStops {
        private uint[] _tabStopBlocks = new uint[0];
        private bool _cleared = false;

        private static readonly int[] _deBruijnRightMostLookup = {
                0, 1, 28, 2, 29, 14, 24, 3, 30, 22, 20, 15, 25, 17, 4, 8,
                31, 27, 13, 23, 21, 19, 16, 7, 26, 12, 18, 6, 11, 5, 10, 9,
            };

        private static readonly int[] _deBruijnLeftMostLookup = {
                0, 1, 16, 2, 29, 17, 3, 22, 30, 20, 18, 11, 13, 4, 7, 23,
                31, 15, 28, 21, 19, 10, 12, 6, 14, 27, 9, 5, 26, 8, 25, 24,
            };

        public void Extend(int width) {
            int blockNum = (width + 31) / 32;
            if (blockNum > _tabStopBlocks.Length) {
                uint[] newBlocks = new uint[blockNum];
                Array.Copy(_tabStopBlocks, newBlocks, _tabStopBlocks.Length);
                if (!_cleared) {
                    for (int i = _tabStopBlocks.Length; i < blockNum; i++) {
                        newBlocks[i] = 0x01010101u;
                    }
                }
                _tabStopBlocks = newBlocks;
            }
        }

        public int? GetNextTabStop(int currentIndex) {
            int startIndex = Math.Max(currentIndex + 1, 0);
            int blockIndex = startIndex >> 5;
            uint mask = ~0u << (startIndex & 0x1f);
            while (blockIndex < _tabStopBlocks.Length) {
                uint block = _tabStopBlocks[blockIndex] & mask;
                if (block != 0u) {
                    // find index of right-most bit using de Bruijn sequence
                    int bitIndex = _deBruijnRightMostLookup[((block & (uint)-(int)block) * 0x077cb531u) >> 27];
                    return (blockIndex << 5) + bitIndex;
                }
                mask = ~0u;
                blockIndex++;
            }
            return null;
        }

        public int? GetPrevTabStop(int currentIndex) {
            int startIndex = Math.Min(currentIndex - 1, _tabStopBlocks.Length * 32 - 1);
            if (startIndex < 0) {
                return null;
            }
            int blockIndex = startIndex >> 5;
            uint mask = ((1u << (startIndex & 0x1f)) << 1) - 1u;
            while (blockIndex >= 0) {
                uint block = _tabStopBlocks[blockIndex] & mask;
                if (block != 0u) {
                    // find index of left-most bit using de Bruijn sequence
                    block |= block >> 1;
                    block |= block >> 2;
                    block |= block >> 4;
                    block |= block >> 8;
                    block |= block >> 16;
                    block ^= block >> 1;
                    int bitIndex = _deBruijnLeftMostLookup[(block * 0x06eb14f9u) >> 27];
                    return (blockIndex << 5) + bitIndex;
                }
                mask = ~0u;
                blockIndex--;
            }
            return null;
        }

        public void Clear() {
            for (int i = 0; i < _tabStopBlocks.Length; i++) {
                _tabStopBlocks[i] = 0u;
            }
            _cleared = true;
        }

        public void Initialize() {
            for (int i = 0; i < _tabStopBlocks.Length; i++) {
                _tabStopBlocks[i] = 0x01010101u;
            }
            _cleared = false;
        }

        public void Set(int index) {
            Extend(index + 1);
            _tabStopBlocks[index / 32] |= (1u << (index % 32));
        }

        public void Unset(int index) {
            Extend(index + 1);
            _tabStopBlocks[index / 32] &= ~(1u << (index % 32));
        }

        public IEnumerable<int> GetIndices() {
            for (int blockIndex = 0; blockIndex < _tabStopBlocks.Length; blockIndex++) {
                uint block = _tabStopBlocks[blockIndex];
                uint bit = 1u;
                for (int i = 0; i < 32; i++) {
                    if ((block & bit) != 0u) {
                        yield return blockIndex * 32 + i;
                    }
                    bit <<= 1;
                }
            }
        }

#if UNITTEST
        public uint[] GetRawBitsForTest() {
            return (uint[])_tabStopBlocks.Clone();
        }

        public void SetRawBitsForTest(uint[] data) {
            _tabStopBlocks = (uint[])data.Clone();
        }
#endif
    }
}
