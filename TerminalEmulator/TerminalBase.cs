/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalBase.cs,v 1.19 2012/05/20 08:30:59 kzmi Exp $
 */
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
    
    //TODO ���O�Ƃ͗����ɂ����Abstract����ˁ[�� �܂��t�B�[���h����������̂Ő�������B
    /// <summary>
    /// <ja>
    /// �^�[�~�i���G�~�����[�^�̒����ƂȂ�N���X�ł��B
    /// </ja>
    /// <en>
    /// Class that becomes core of terminal emulator.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// ���̃N���X�̉���́A�܂�����܂���B
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

        private ScrollBarValues _scrollBarValues;
        private ICharDecoder _decoder;
        private TerminalDocument _document;
        private IAbstractTerminalHost _session;
        private LogService _logService;
        private IModalTerminalTask _modalTerminalTask;
        private PromptRecognizer _promptRecognizer;
        private IntelliSense _intelliSense;
        private PopupStyleCommandResultRecognizer _commandResultRecognizer;
        private Cursor _documentCursor = null;

        private bool _cleanup = false;

        protected List<AfterExitLockDelegate> _afterExitLockActions;
        protected GLineManipulator _manipulator;
        protected TextDecoration _currentdecoration;
        protected TerminalMode _terminalMode;
        protected TerminalMode _cursorKeyMode; //_terminalMode�͕ʕ��BAIX�ł�vi�ŁA�J�[�\���L�[�͕s�ςƂ����Ⴊ�m�F����Ă���

        protected abstract void ChangeMode(TerminalMode tm);
        protected abstract void ResetInternal();

        protected ProcessCharResult _processCharResult;

        //ICharDecoder
        public abstract void ProcessChar(char ch);

        internal abstract byte[] SequenceKeyData(Keys modifier, Keys body);

        public AbstractTerminal(TerminalInitializeInfo info) {
            TerminalEmulatorPlugin.Instance.LaterInitialize();

            _session = info.Session;

            //_invalidateParam = new InvalidateParam();
            _document = new TerminalDocument(info.InitialWidth, info.InitialHeight);
            _document.SetOwner(_session.ISession);
            _afterExitLockActions = new List<AfterExitLockDelegate>();

            _decoder = new ISO2022CharDecoder(this, EncodingProfile.Get(info.Session.TerminalSettings.Encoding));
            _terminalMode = TerminalMode.Normal;
            _currentdecoration = TextDecoration.Default;
            _manipulator = new GLineManipulator();
            _scrollBarValues = new ScrollBarValues();
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

        //XTERM��\�ɏo���Ȃ����߂̃��\�b�h
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
        ProcessCharResult ICharProcessor.State {
            get {
                return _processCharResult;
            }
        }
        public void UnsupportedCharSetDetected(char code) {
            string desc;
            if (code == '0')
                desc = "0 (DEC Special Character)"; //����͂悭����̂ŒA��������
            else
                desc = new String(code, 1);

            CharDecodeError(String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.UnsupportedCharSet"), desc));
        }
        public void InvalidCharDetected(byte[] buf) {
            CharDecodeError(String.Format(GEnv.Strings.GetString("Message.AbstractTerminal.UnexpectedChar"), EncodingProfile.Get(GetTerminalSettings().Encoding).Encoding.WebName));
        }
        #endregion

        //��M������̊ȈՌĂяo��
        protected void TransmitDirect(byte[] data) {
            _session.TerminalTransmission.Transmit(data);
        }

        protected void TransmitDirect(byte[] data, int offset, int length) {
            _session.TerminalTransmission.Transmit(data, offset, length);
        }

        //�����n�̃G���[�ʒm
        protected void CharDecodeError(string msg) {
            IPoderosaMainWindow window = _session.OwnerWindow;
            if (window == null)
                return;
            Debug.Assert(window.AsForm().InvokeRequired);

            Monitor.Exit(GetDocument()); //����͖Y����
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
            //Encoding���������͊ȒP�ɍς܂��邱�Ƃ��ł���
            if (_decoder.CurrentEncoding.Type == GetTerminalSettings().Encoding)
                _decoder.Reset(_decoder.CurrentEncoding);
            else
                _decoder = new ISO2022CharDecoder(this, EncodingProfile.Get(GetTerminalSettings().Encoding));
        }

        //����̓��C���X���b�h����Ăяo������
        public virtual void FullReset() {
            lock (_document) {
                ChangeMode(TerminalMode.Normal);
                _document.ClearScrollingRegion();
                ResetInternal();
                _decoder = new ISO2022CharDecoder(this, EncodingProfile.Get(GetTerminalSettings().Encoding));
            }
        }

        //ModalTerminalTask����
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

        //�R�}���h���ʂ̏������荞��
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
                        pass_to_terminal = false; //���͂������Ȃ�(XMODEM�Ƃ�)�̂Ƃ��̓^�[�~�i���ɗ^���Ȃ�
                }

                //�o�C�i�����O�̏o��
                _logService.BinaryLogger.Write(data);

                if (pass_to_terminal) {
                    TerminalDocument document = _document;
                    lock (document) {
                        //_invalidateParam.Reset();
                        //�������狌Input()
                        _manipulator.Load(GetDocument().CurrentLine, 0);
                        _manipulator.CaretColumn = GetDocument().CaretColumn;
                        _manipulator.DefaultDecoration = _currentdecoration;

                        //�����{��
                        _decoder.OnReception(data);

                        GetDocument().ReplaceCurrentLine(_manipulator.Export());
                        GetDocument().CaretColumn = _manipulator.CaretColumn;
                        //�����܂�

                        //�E�[�ɃL�����b�g�������Ƃ��͕֋X�I�Ɏ��s�̓��ɂ����Ă���
                        if (document.CaretColumn == document.TerminalWidth) {
                            document.CurrentLineNumber++; //����ɂ���Ď��s�̑��݂�ۏ�
                            document.CaretColumn = 0;
                        }

                        CheckDiscardDocument();
                        AdjustTransientScrollBar();

                        //���ݍs�����[�Ɍ�����悤��ScrollBarValue���v�Z
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

                        //Invalidate��lock�̊O�ɏo���B���̂ق������S�Ǝv��ꂽ

                        //��M�X���b�h���ł̓}�[�N������̂݁B�^�C�}�[�ōs���̂�IntelliSense�ɕ���p����̂ňꎞ��~
                        //_promptRecognizer.SetContentUpdateMark();
                        _promptRecognizer.Recognize();
                    }

                    if (_afterExitLockActions.Count > 0) {
                        Control main = _session.OwnerWindow.AsControl();
                        foreach (AfterExitLockDelegate action in _afterExitLockActions) {
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
            //TODO ���b�Z�[�W�� GEnv.Strings.GetString("Message.TerminalDataReceiver.GenericError"),_tag.Connection.Param.ShortDescription, msg
            if (!GetConnection().IsClosed) { //����w�߂��o������̃G���[�͕\�����Ȃ�
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
                            window.Warning(msg); //TODO Disable�I�v�V�������̃T�|�[�g
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
            //NOTE _session.CloseByReceptionThread()�́A���̂܂܃A�v���I���ƒ�������ꍇ������B����ƁA_logService.Close()�̏������I���Ȃ������ɋ����I���ɂȂ��ă��O����������Ȃ��\��������
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
                document.InvalidatedRegion.InvalidatedAll = true; //�{���͂����܂ł��Ȃ��Ă��ǂ����������O�̂���
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
            //!!�{������if���͕s�v�Ȃ͂������A�͈̓G���[�ɂȂ�P�[�X�����󂯂�ꂽ�B���̌�����T���ă����[�X���O�ɂ��낢�낢����̂͊댯�Ȃ̂ł����͓�����B��ł����Ɖ𖾂���B
            if (_scrollBarValues.Value < _scrollBarValues.Maximum)
                sb.Value = _scrollBarValues.Value;
            _scrollBarValues.Dirty = false;
        }

        //�h�L�������g���b�N���łȂ��ƌĂ�ł͂���
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

    //Escape Sequence���g���^�[�~�i��
    internal abstract class EscapeSequenceTerminal : AbstractTerminal {
        private StringBuilder _escapeSequence;
        private IModalCharacterTask _currentCharacterTask;

        protected static class ControlCode {
            public const char NUL = '\u0000';
            public const char BEL = '\u0007';
            public const char BS = '\u0008';
            public const char HT = '\u0009';
            public const char LF = '\u000a';
            public const char VT = '\u000b';
            public const char CR = '\u000d';
            public const char SO = '\u000e';
            public const char SI = '\u000f';
            public const char ESC = '\u001b';
            public const char ST = '\u009c';
        }

        public EscapeSequenceTerminal(TerminalInitializeInfo info)
            : base(info) {
            _escapeSequence = new StringBuilder();
            _processCharResult = ProcessCharResult.Processed;
        }

        protected override void ResetInternal() {
            _escapeSequence = new StringBuilder();
            _processCharResult = ProcessCharResult.Processed;
        }

        public override void ProcessChar(char ch) {
            if (_processCharResult != ProcessCharResult.Escaping) {
                if (ch == ControlCode.ESC) {
                    _processCharResult = ProcessCharResult.Escaping;
                }
                else {
                    if (_currentCharacterTask != null) { //�}�N���ȂǁAchar�����^�C�v
                        _currentCharacterTask.ProcessChar(ch);
                    }

                    this.LogService.XmlLogger.Write(Unicode.ToOriginal(ch));

                    if (Unicode.IsControlCharacter(ch))
                        _processCharResult = ProcessControlChar(ch);
                    else
                        _processCharResult = ProcessNormalChar(ch);
                }
            }
            else {
                if (ch == ControlCode.NUL)
                    return; //�V�[�P���X����NULL�����������Ă���P�[�X���m�F���ꂽ �Ȃ�����XmlLogger�ɂ����̃f�[�^�͍s���Ȃ��B

                if (ch == ControlCode.ESC) {
                    // escape sequence restarted ?
                    // save log silently
                    RuntimeUtil.SilentReportException(new UnknownEscapeSequenceException("Incomplete escape sequence: ESC " + _escapeSequence.ToString()));
                    _escapeSequence.Remove(0, _escapeSequence.Length);
                    return;
                }

                _escapeSequence.Append(ch);
                bool end_flag = false; //escape sequence�̏I��肩�ǂ����������t���O
                if (_escapeSequence.Length == 1) { //ESC+�P�����ł���ꍇ
                    end_flag = ('0' <= ch && ch <= '9') || ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ch == '>' || ch == '=' || ch == '|' || ch == '}' || ch == '~';
                }
                else if (_escapeSequence[0] == ']') { //OSC�̏I�[��BEL��ST(String Terminator)
                    end_flag = (ch == ControlCode.BEL) || (ch == ControlCode.ST);
                    // Note: The conversion from "ESC \" to ST would be done in XTerm.ProcessChar(char).
                }
                else if (this._escapeSequence[0] == '@') {
                    end_flag = (ch == '0') || (ch == '1');
                }
                else {
                    end_flag = ('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ch == '@' || ch == '~' || ch == '|' || ch == '{';
                }

                if (end_flag) { //�V�[�P���X�̂����
                    char[] seq = _escapeSequence.ToString().ToCharArray();

                    this.LogService.XmlLogger.EscapeSequence(seq);

                    try {
                        char code = seq[0];
                        _processCharResult = ProcessCharResult.Unsupported; //ProcessEscapeSequence�ŗ�O��������ŏ�Ԃ�Escaping�͂Ђǂ����ʂ������̂�
                        _processCharResult = ProcessEscapeSequence(code, seq, 1);
                        if (_processCharResult == ProcessCharResult.Unsupported)
                            throw new UnknownEscapeSequenceException("Unknown escape sequence: ESC " + new string(seq));
                    }
                    catch (UnknownEscapeSequenceException ex) {
                        CharDecodeError(GEnv.Strings.GetString("Message.EscapesequenceTerminal.UnsupportedSequence") + ex.Message);
                        RuntimeUtil.SilentReportException(ex);
                    }
                    finally {
                        _escapeSequence.Remove(0, _escapeSequence.Length);
                    }
                }
                else
                    _processCharResult = ProcessCharResult.Escaping;
            }
        }

        protected virtual ProcessCharResult ProcessControlChar(char ch) {
            if (ch == ControlCode.LF || ch == ControlCode.VT) { //Vertical Tab��LF�Ɠ�����
                LineFeedRule rule = GetTerminalSettings().LineFeedRule;
                if (rule == LineFeedRule.Normal || rule == LineFeedRule.LFOnly) {
                    if (rule == LineFeedRule.LFOnly) //LF�݂̂̓���ł���Ƃ�
                        DoCarriageReturn();
                    DoLineFeed();
                }
                return ProcessCharResult.Processed;
            }
            else if (ch == ControlCode.CR) {
                LineFeedRule rule = GetTerminalSettings().LineFeedRule;
                if (rule == LineFeedRule.Normal || rule == LineFeedRule.CROnly) {
                    DoCarriageReturn();
                    if (rule == LineFeedRule.CROnly)
                        DoLineFeed();
                }
                return ProcessCharResult.Processed;
            }
            else if (ch == ControlCode.BEL) {
                this.IndicateBell();
                return ProcessCharResult.Processed;
            }
            else if (ch == ControlCode.BS) {
                //�s���ŁA���O�s�̖������p���ł������ꍇ�s��߂�
                if (_manipulator.CaretColumn == 0) {
                    TerminalDocument doc = GetDocument();
                    int line = doc.CurrentLineNumber - 1;
                    if (line >= 0 && doc.FindLineOrEdge(line).EOLType == EOLType.Continue) {
                        doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
                        doc.CurrentLineNumber = line;
                        if (doc.CurrentLine == null)
                            _manipulator.Clear(doc.TerminalWidth);
                        else
                            _manipulator.Load(doc.CurrentLine, doc.CurrentLine.DisplayLength - 1); //NOTE ������CharLength���������������Ǝv���ĉ�������
                        doc.InvalidatedRegion.InvalidateLine(doc.CurrentLineNumber);
                    }
                }
                else
                    _manipulator.BackCaret();

                return ProcessCharResult.Processed;
            }
            else if (ch == ControlCode.HT) {
                _manipulator.CaretColumn = GetNextTabStop(_manipulator.CaretColumn);
                return ProcessCharResult.Processed;
            }
            else if (ch == ControlCode.SO) {
                return ProcessCharResult.Processed; //�ȉ��Q��CharDecoder�̒��ŏ�������Ă���͂��Ȃ̂Ŗ���
            }
            else if (ch == ControlCode.SI) {
                return ProcessCharResult.Processed;
            }
            else if (ch == ControlCode.NUL) {
                return ProcessCharResult.Processed; //null char�͖��� !!CR NUL��CR LF�Ƃ݂Ȃ��d�l�����邪�ACR LF CR NUL�Ƃ��邱�Ƃ������ē��
            }
            else {
                //Debug.WriteLine("Unknown char " + (int)ch);
                //�K���ȃO���t�B�b�N�\���ق���
                return ProcessCharResult.Unsupported;
            }
        }
        private void DoLineFeed() {
            GLine nl = _manipulator.Export();
            nl.EOLType = (nl.EOLType == EOLType.CR || nl.EOLType == EOLType.CRLF) ? EOLType.CRLF : EOLType.LF;
            this.LogService.TextLogger.WriteLine(nl); //���O�ɍs��commit
            GetDocument().ReplaceCurrentLine(nl);
            GetDocument().LineFeed();

            //�J�����ێ��͕K�v�B�T���v��:linuxconf.log
            int col = _manipulator.CaretColumn;
            _manipulator.Load(GetDocument().CurrentLine, col);
        }
        private void DoCarriageReturn() {
            _manipulator.CarriageReturn();
        }

        protected virtual int GetNextTabStop(int start) {
            int t = start;
            //t���ōŏ��̂W�̔{���ւ����Ă���
            t += (8 - t % 8);
            if (t >= GetDocument().TerminalWidth)
                t = GetDocument().TerminalWidth - 1;
            return t;
        }

        protected virtual ProcessCharResult ProcessNormalChar(char ch) {
            //���ɉ�ʉE�[�ɃL�����b�g������̂ɕ�������������s������
            int tw = GetDocument().TerminalWidth;
            if (_manipulator.CaretColumn + Unicode.GetCharacterWidth(ch) > tw) {
                GLine l = _manipulator.Export();
                l.EOLType = EOLType.Continue;
                this.LogService.TextLogger.WriteLine(l); //���O�ɍs��commit
                GetDocument().ReplaceCurrentLine(l);
                GetDocument().LineFeed();
                _manipulator.Load(GetDocument().CurrentLine, 0);
            }

            //��ʂ̃��T�C�Y���������Ƃ��́A_manipulator�̃o�b�t�@�T�C�Y���s���̉\��������
            if (tw > _manipulator.BufferSize)
                _manipulator.ExpandBuffer(tw);

            //�ʏ핶���̏���
            _manipulator.PutChar(ch, _currentdecoration);

            return ProcessCharResult.Processed;
        }

        protected abstract ProcessCharResult ProcessEscapeSequence(char code, char[] seq, int offset);

        //FormatException�̂ق���OverflowException�̉\��������̂�
        protected static int ParseInt(string param, int default_value) {
            try {
                if (param.Length > 0)
                    return Int32.Parse(param);
                else
                    return default_value;
            }
            catch (Exception ex) {
                throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", param, ex.Message));
            }
        }

        protected static IntPair ParseIntPair(string param, int default_first, int default_second) {
            IntPair ret = new IntPair(default_first, default_second);

            string[] s = param.Split(';');

            if (s.Length >= 1 && s[0].Length > 0) {
                try {
                    ret.first = Int32.Parse(s[0]);
                }
                catch (Exception ex) {
                    throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", s[0], ex.Message));
                }
            }

            if (s.Length >= 2 && s[1].Length > 0) {
                try {
                    ret.second = Int32.Parse(s[1]);
                }
                catch (Exception ex) {
                    throw new UnknownEscapeSequenceException(String.Format("bad number format [{0}] : {1}", s[1], ex.Message));
                }
            }

            return ret;
        }

        //ModalTask�̃Z�b�g������
        public override void StartModalTerminalTask(IModalTerminalTask task) {
            base.StartModalTerminalTask(task);
            _currentCharacterTask = (IModalCharacterTask)task.GetAdapter(typeof(IModalCharacterTask));
        }
        public override void EndModalTerminalTask() {
            base.EndModalTerminalTask();
            _currentCharacterTask = null;
        }
    }

    //��M�X���b�h���玟�ɐݒ肷�ׂ�ScrollBar�̒l��z�u����B
    internal class ScrollBarValues {
        //��M�X���b�h�ł����̒l��ݒ肵�A����OnPaint�����C���X���b�h�ł̎��s��Commit����
        private bool _dirty; //���ꂪ�����Ă���Ɨv�ݒ�
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
        ProcessCharResult State {
            get;
        }
        void UnsupportedCharSetDetected(char code);
        void InvalidCharDetected(byte[] data);
    }


    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum ProcessCharResult {
        Processed,
        Unsupported,
        Escaping
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
