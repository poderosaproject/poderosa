/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalControl.cs,v 1.14 2012/02/19 08:15:09 kzmi Exp $
 */

//#define DEBUG_MOUSETRACKING

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Threading;

using Poderosa.Document;
using Poderosa.View;
using Poderosa.Sessions;
using Poderosa.ConnectionParam;
using Poderosa.Protocols;
using Poderosa.Forms;
using Poderosa.Commands;

namespace Poderosa.Terminal {

    /// <summary>
    /// <ja>
    /// �^�[�~�i���������R���g���[���ł��B
    /// </ja>
    /// <en>
    /// Control to show the terminal.
    /// </en>
    /// </summary>
    /// <exclude/>
    /// 
    public class TerminalControl : CharacterDocumentViewer {
        //ID
        private int _instanceID;
        private static int _instanceCount = 1;
        public string InstanceID {
            get {
                return "TC" + _instanceID;
            }
        }

        private System.Windows.Forms.Timer _sizeTipTimer;
        private ITerminalControlHost _session;

        private readonly TerminalEmulatorMouseHandler _terminalEmulatorMouseHandler;
        private readonly MouseTrackingHandler _mouseTrackingHandler;
        private readonly MouseWheelHandler _mouseWheelHandler;

        private Label _sizeTip;

        private delegate void AdjustIMECompositionDelegate();

        private bool _inIMEComposition; //IME�ɂ�镶�����͂̍Œ��ł����true�ɂȂ�
        private bool _ignoreValueChangeEvent;

        private bool _escForVI;

        //�ĕ`��̏�ԊǗ�
        private int _drawOptimizingState = 0; //���̏�ԊǗ���OnWindowManagerTimer(), SmartInvalidate()�Q��

        internal TerminalDocument GetDocument() {
            // FIXME: In rare case, _session may be null...
            return _session.Terminal.GetDocument();
        }
        protected ITerminalSettings GetTerminalSettings() {
            // FIXME: In rare case, _session may be null...
            return _session.TerminalSettings;
        }
        protected TerminalTransmission GetTerminalTransmission() {
            // FIXME: In rare case, _session may be null...
            return _session.TerminalTransmission;
        }
        protected AbstractTerminal GetTerminal() {
            // FIXME: In rare case, _session may be null...
            return _session.Terminal;
        }
        private bool IsConnectionClosed() {
            // FIXME: In rare case, _session may be null...
            return _session.TerminalTransmission.Connection.IsClosed;
        }


        /// <summary>
        /// �K�v�ȃf�U�C�i�ϐ��ł��B
        /// </summary>
        private System.ComponentModel.Container components = null;

        public TerminalControl() {
            _instanceID = _instanceCount++;
            _enableAutoScrollBarAdjustment = false;
            _escForVI = false;
            this.EnabledEx = false;

            // ���̌Ăяo���́AWindows.Forms �t�H�[�� �f�U�C�i�ŕK�v�ł��B
            InitializeComponent();

            _mouseWheelHandler = new MouseWheelHandler(this, _VScrollBar);
            _mouseHandlerManager.AddFirstHandler(_mouseWheelHandler);    // mouse wheel handler will become second handler
            _mouseTrackingHandler = new MouseTrackingHandler(this);
            _mouseHandlerManager.AddFirstHandler(_mouseTrackingHandler);    // mouse tracking handler become first handler
            _terminalEmulatorMouseHandler = new TerminalEmulatorMouseHandler(this);
            _mouseHandlerManager.AddLastHandler(_terminalEmulatorMouseHandler);
            //TODO �^�C�}�[�͋��p���H
            _sizeTipTimer = new System.Windows.Forms.Timer();
            _sizeTipTimer.Interval = 2000;
            _sizeTipTimer.Tick += new EventHandler(this.OnHideSizeTip);

            this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }
        public void Attach(ITerminalControlHost session) {
            _session = session;
            SetContent(session.Terminal.GetDocument());

            _mouseTrackingHandler.Attach(session);
            _mouseWheelHandler.Attach(session);

            ITerminalEmulatorOptions opt = TerminalEmulatorPlugin.Instance.TerminalEmulatorOptions;
            _caret.Blink = opt.CaretBlink;
            _caret.Color = opt.CaretColor;
            _caret.Style = opt.CaretType;
            _caret.Reset();

            //KeepAlive�^�C�}�N���͍ł��x�点���ꍇ�ŃR�R
            TerminalEmulatorPlugin.Instance.KeepAlive.Refresh(opt.KeepAliveInterval);

            //ASCIIWordBreakTable : ���͋��L�ݒ肾���ASession�ŗL�Ƀf�[�^�����悤�ɂ��邩������Ȃ��܂݂��������āB
            ASCIIWordBreakTable table = ASCIIWordBreakTable.Default;
            table.Reset();
            foreach (char ch in opt.AdditionalWordElement)
                table.Set(ch, ASCIIWordBreakTable.LETTER);

            lock (GetDocument()) {
                _ignoreValueChangeEvent = true;
                _session.Terminal.CommitScrollBar(_VScrollBar, false);
                _ignoreValueChangeEvent = false;

                if (!IsConnectionClosed()) {
                    Size ts = CalcTerminalSize(GetRenderProfile());

                    //TODO �l�S�J�n�O�͂�����}��������
                    if (ts.Width != GetDocument().TerminalWidth || ts.Height != GetDocument().TerminalHeight)
                        ResizeTerminal(ts.Width, ts.Height);
                }
            }
            Invalidate(true);
        }
        public void Detach() {
            if (DebugOpt.DrawingPerformance)
                DrawingPerformance.Output();

            if (_inIMEComposition)
                ClearIMEComposition();

            _mouseTrackingHandler.Detach();
            _mouseWheelHandler.Detach();

            _session = null;
            SetContent(null);
        }

        /// <summary>
        /// �g�p����Ă��郊�\�[�X�Ɍ㏈�������s���܂��B
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }


        private void InitializeComponent() {
            this.SuspendLayout();
            this._sizeTip = new Label();
            // 
            // _sizeTip
            // 
            this._sizeTip.Visible = false;
            this._sizeTip.BorderStyle = BorderStyle.FixedSingle;
            this._sizeTip.TextAlign = ContentAlignment.MiddleCenter;
            this._sizeTip.BackColor = Color.FromKnownColor(KnownColor.Info);
            this._sizeTip.ForeColor = Color.FromKnownColor(KnownColor.InfoText);
            this._sizeTip.Size = new Size(64, 16);
            // 
            // TerminalPane
            // 
            this.TabStop = false;
            this.AllowDrop = true;

            this.Controls.Add(_sizeTip);
            this.ImeMode = ImeMode.NoControl;
            this.ResumeLayout(false);

        }

        /// <summary>
        /// Sends bytes. Data may be repeated as local echo.
        /// </summary>
        /// <param name="data">Byte array that contains data to send.</param>
        /// <param name="offset">Offset in data</param>
        /// <param name="length">Length of bytes to transmit</param>
        internal void Transmit(byte[] data, int offset, int length) {
            if (this.InvokeRequired) {
                // UI thread may be waiting for unlocking of the current document in the OnPaint handler.
                // If the caller is locking the current document, Invoke() causes dead lock.
                this.BeginInvoke((MethodInvoker)delegate() {
                    GetTerminalTransmission().Transmit(data, offset, length);
                });
            }
            else {
                GetTerminalTransmission().Transmit(data, offset, length);
            }
        }

        /// <summary>
        /// Sends bytes without local echo.
        /// </summary>
        /// <param name="data">Byte array that contains data to send.</param>
        /// <param name="offset">Offset in data</param>
        /// <param name="length">Length of bytes to transmit</param>
        internal void TransmitDirect(byte[] data, int offset, int length) {
            if (this.InvokeRequired) {
                // UI thread may be waiting for unlocking of the current document in the OnPaint handler.
                // If the caller is locking the current document, Invoke() causes dead lock.
                this.BeginInvoke((MethodInvoker)delegate() {
                    GetTerminalTransmission().TransmitDirect(data, offset, length);
                });
            }
            else {
                GetTerminalTransmission().TransmitDirect(data, offset, length);
            }
        }

        /*
         * ��  ��M�X���b�h�ɂ����s�̃G���A
         */

        public void DataArrived() {
            //�悭�݂�ƁA���������s���Ă���Ƃ���document�����b�N���Ȃ̂ŁA��̃p�^�[���̂悤��SendMessage���g���ƃf�b�h���b�N�̊댯������
            InternalDataArrived();
        }

        private void InternalDataArrived() {
            if (_session == null)
                return;	// �y�C������鎞�� _tag �� null �ɂȂ��Ă��邱�Ƃ�����

            TerminalDocument document = GetDocument();
            if (!this.ITextSelection.IsEmpty) {
                document.InvalidatedRegion.InvalidatedAll = true; //�ʓ|����
                this.ITextSelection.Clear();
            }
            //Debug.WriteLine(String.Format("v={0} l={1} m={2}", _VScrollBar.Value, _VScrollBar.LargeChange, _VScrollBar.Maximum));
            if (DebugOpt.DrawingPerformance)
                DrawingPerformance.MarkReceiveData(GetDocument().InvalidatedRegion);
            SmartInvalidate();

            //�����ϊ����ł������Ƃ��̂��߂̒���
            if (_inIMEComposition) {
                if (this.InvokeRequired)
                    this.Invoke(new AdjustIMECompositionDelegate(AdjustIMEComposition));
                else
                    AdjustIMEComposition();
            }
        }

        private void SmartInvalidate() {
            //������DrawOptimizeState��������B�ߐڂ��ē�������f�[�^�ɂ��ߏ�ȍĕ`���������A�^�C�}�[�ň�莞�Ԍ�ɂ͊m���ɕ`�悳���悤�ɂ���B
            //��ԑJ�ڂ́A�f�[�^�����ƃ^�C�}�[���g���K�Ƃ���R��Ԃ̊ȒP�ȃI�[�g�}�g���ł���B
            switch (_drawOptimizingState) {
                case 0:
                    _drawOptimizingState = 1;
                    InvalidateEx();
                    break;
                case 1:
                    if (_session.TerminalConnection.Socket.Available)
                        Interlocked.Exchange(ref _drawOptimizingState, 2); //�Ԉ������[�h��
                    else
                        InvalidateEx();
                    break;
                case 2:
                    break; //do nothing
            }
        }

        /*
         * ��  ��M�X���b�h�ɂ����s�̃G���A
         * -------------------------------
         * ��  UI�X���b�h�ɂ����s�̃G���A
         */

        protected override void OnWindowManagerTimer() {
            base.OnWindowManagerTimer();

            switch (_drawOptimizingState) {
                case 0:
                    break; //do nothing
                case 1:
                    Interlocked.CompareExchange(ref _drawOptimizingState, 0, 1);
                    break;
                case 2: //�Z�����Ă���ɂ͕`��
                    _drawOptimizingState = 1;
                    InvalidateEx();
                    break;
            }
        }

        private delegate void InvalidateDelegate1();
        private delegate void InvalidateDelegate2(Rectangle rc);
        private void DelInvalidate(Rectangle rc) {
            Invalidate(rc);
        }
        private void DelInvalidate() {
            Invalidate();
        }


        protected override void VScrollBarValueChanged() {
            if (_ignoreValueChangeEvent)
                return;
            TerminalDocument document = GetDocument();
            lock (document) {
                document.TopLineNumber = document.FirstLineNumber + _VScrollBar.Value;
                _session.Terminal.TransientScrollBarValues.Value = _VScrollBar.Value;
                Invalidate();
            }
        }

        /* �L�[�{�[�h�����n�ɂ���
         * �@���M�͍ŏI�I�ɂ�SendChar/String�֍s���B
         * 
         * �@�����Ɏ���ߒ��ł́A
         *  ProcessCmdKey: Alt�L�[�̐ݒ莟��ŁA�x�[�X�N���X�ɓn���i���R�}���h�N�������݂�j���ǂ������߂�
         *  ProcessDialogKey: �����L�[�ȊO�͊�{�I�ɂ����ŏ����B
         *  OnKeyPress: �����̑��M
         */
        private byte[] _sendCharBuffer = new byte[1];
        public void SendChar(char ch) { //IS����̃R�[���o�b�N����̂�
            if (ch < 0x80) {
                //Debug.WriteLine("SendChar " + (int)ch);
                _sendCharBuffer[0] = (byte)ch;
                SendBytes(_sendCharBuffer);
            }
            else {
                byte[] data = EncodingProfile.Get(GetTerminalSettings().Encoding).GetBytes(ch);
                SendBytes(data);
            }
        }
        public void SendCharArray(char[] chs) {
            byte[] bytes = EncodingProfile.Get(GetTerminalSettings().Encoding).GetBytes(chs);
            SendBytes(bytes);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            Keys modifiers = keyData & Keys.Modifiers;
            if (IsAcceptableUserInput() && (modifiers & Keys.Alt) != Keys.None) { //Alt�L�[�̉���菈�����J�n
                Keys keybody = keyData & Keys.KeyCode;
                if (GEnv.Options.LeftAltKey != AltKeyAction.Menu && (Win32.GetKeyState(Win32.VK_LMENU) & 0x8000) != 0) {
                    ProcessSpecialAltKey(GEnv.Options.LeftAltKey, modifiers, keybody);
                    return true;
                }
                else if (GEnv.Options.RightAltKey != AltKeyAction.Menu && (Win32.GetKeyState(Win32.VK_RMENU) & 0x8000) != 0) {
                    ProcessSpecialAltKey(GEnv.Options.RightAltKey, modifiers, keybody);
                    return true;
                }
            }

            //����܂łŏ����ł��Ȃ���Ώ�ʂ֓n��
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool IsInputKey(Keys key) {
            Keys mod = key & Keys.Modifiers;
            Keys body = key & Keys.KeyCode;
            if (mod == Keys.None && (body == Keys.Tab || body == Keys.Escape))
                return true;
            else
                return false;
        }

        protected override bool ProcessDialogKey(Keys key) {
            Keys modifiers = key & Keys.Modifiers;
            Keys keybody = key & Keys.KeyCode;

            //�ڑ����łȂ��Ƃ��߂ȃL�[
            if (IsAcceptableUserInput()) {
                //TODO Enter,Space,SequenceKey�n���J�X�^���L�[�ɓ���Ă��܂�����
                char[] custom = TerminalEmulatorPlugin.Instance.CustomKeySettings.Scan(key); //�J�X�^���L�[
                if (custom != null) {
                    SendCharArray(custom);
                    return true;
                }
                else if (ProcessAdvancedFeatureKey(modifiers, keybody)) {
                    return true;
                }
                else if (keybody == Keys.Enter && modifiers == Keys.None) {
                    _escForVI = false;
                    SendCharArray(TerminalUtil.NewLineChars(GetTerminalSettings().TransmitNL));
                    return true;
                }
                else if (keybody == Keys.Space && modifiers == Keys.Control) { //�����OnKeyPress�ɂ킽���Ă���Ȃ�
                    SendChar('\0');
                    return true;
                }
                if ((keybody == Keys.Tab) && (modifiers == Keys.Shift)) {
                    this.SendChar('\t');
                    return true;
                }
                else if (IsSequenceKey(keybody)) {
                    ProcessSequenceKey(modifiers, keybody);
                    return true;
                }
            }

            //��ɑ����L�[
            if (keybody == Keys.Apps) { //�R���e�L�X�g���j���[
                TerminalDocument document = GetDocument();
                int x = document.CaretColumn;
                int y = document.CurrentLineNumber - document.TopLineNumber;
                SizeF p = GetRenderProfile().Pitch;
                _terminalEmulatorMouseHandler.ShowContextMenu(new Point((int)(p.Width * x), (int)(p.Height * y)));
                return true;
            }

            return base.ProcessDialogKey(key);
        }

        private bool ProcessAdvancedFeatureKey(Keys modifiers, Keys keybody) {
            if (_session.Terminal.TerminalMode == TerminalMode.Application)
                return false;

            if (_session.Terminal.IntelliSense.ProcessKey(modifiers, keybody))
                return true;
            else if (_session.Terminal.PopupStyleCommandResultRecognizer.ProcessKey(modifiers, keybody))
                return true;
            else
                return false;
        }

        private static bool IsSequenceKey(Keys key) {
            return ((int)Keys.F1 <= (int)key && (int)key <= (int)Keys.F12) ||
                key == Keys.Insert || key == Keys.Delete || IsScrollKey(key);
        }
        private static bool IsScrollKey(Keys key) {
            return key == Keys.Up || key == Keys.Down ||
                key == Keys.Left || key == Keys.Right ||
                key == Keys.PageUp || key == Keys.PageDown ||
                key == Keys.Home || key == Keys.End;
        }
        private void ProcessSpecialAltKey(AltKeyAction act, Keys modifiers, Keys body) {
            if (!this.EnabledEx)
                return;
            char ch = KeyboardInfo.Scan(body, (modifiers & Keys.Shift) != Keys.None);
            if (ch == '\0')
                return; //���蓖�Ă��Ă��Ȃ���͖���

            if ((modifiers & Keys.Control) != Keys.None)
                ch = (char)((int)ch % 32); //Control���������琧�䕶��

            if (act == AltKeyAction.ESC) {
                byte[] t = new byte[2];
                t[0] = 0x1B;
                t[1] = (byte)ch;
                //Debug.WriteLine("ESC " + (int)ch);
                SendBytes(t);
            }
            else { //Meta
                ch = (char)(0x80 + ch);
                byte[] t = new byte[1];
                t[0] = (byte)ch;
                //Debug.WriteLine("META " + (int)ch);
                SendBytes(t);
            }
        }

        protected override void OnKeyPress(KeyPressEventArgs e) {
            base.OnKeyPress(e);
            if (e.KeyChar == '\x001b') {
                _escForVI = true;
            }
            if (!IsAcceptableUserInput())
                return;
            /* �����̏����ɂ���
             * �@IME�œ��͕������m�肷��Ɓi�����m��ł͂Ȃ��j�AWM_IME_CHAR�AWM_ENDCOMPOSITION�AWM_CHAR�̏��Ń��b�Z�[�W�������Ă���BControl�͂��̗�����KeyPress�C�x���g��
             * �@����������̂ŁAIME�̓��͂��Q�񑗐M����Ă��܂��B
             * �@��������m��̂Ƃ���WM_IME_CHAR�݂̂ł���B
             */
            //if((int)e.KeyChar>=100) {
            //    if(_currentMessage.Msg!=Win32.WM_IME_CHAR) return;
            //}
            if (this._escForVI) {
                this.SendChar(e.KeyChar);
            }
            else {
                this.SendChar(e.KeyChar);
                if (_session.TerminalSettings.EnabledCharTriggerIntelliSense && _session.Terminal.TerminalMode == TerminalMode.Normal)
                    _session.Terminal.IntelliSense.ProcessChar(e.KeyChar);
            }
        }

        private void SendBytes(byte[] data) {
            TerminalDocument doc = GetDocument();
            lock (doc) {
                //�L�[���������ςȂ��ɂ����Ƃ��ɃL�����b�g���u�����N����̂͂�����ƌ��ꂵ���̂ŃL�[���͂����邽�тɃ^�C�}�����Z�b�g
                _caret.KeepActiveUntilNextTick();

                MakeCurrentLineVisible();
            }
            GetTerminalTransmission().Transmit(data);
        }
        private bool IsAcceptableUserInput() {
            //TODO: ModalTerminalTask�̑��݂����R�ŋ��ۂ���Ƃ��̓X�e�[�^�X�o�[�������ɏo���̂��悢����
            if (!this.EnabledEx || IsConnectionClosed() || _session.Terminal.CurrentModalTerminalTask != null)
                return false;
            else
                return true;

        }

        private void ProcessScrollKey(Keys key) {
            TerminalDocument doc = GetDocument();
            int current = doc.TopLineNumber - doc.FirstLineNumber;
            int newvalue = 0;
            switch (key) {
                case Keys.Up:
                    newvalue = current - 1;
                    break;
                case Keys.Down:
                    newvalue = current + 1;
                    break;
                case Keys.PageUp:
                    newvalue = current - doc.TerminalHeight;
                    break;
                case Keys.PageDown:
                    newvalue = current + doc.TerminalHeight;
                    break;
                case Keys.Home:
                    newvalue = 0;
                    break;
                case Keys.End:
                    newvalue = doc.LastLineNumber - doc.FirstLineNumber + 1 - doc.TerminalHeight;
                    break;
            }

            if (newvalue < 0)
                newvalue = 0;
            else if (newvalue > _VScrollBar.Maximum + 1 - _VScrollBar.LargeChange)
                newvalue = _VScrollBar.Maximum + 1 - _VScrollBar.LargeChange;

            _VScrollBar.Value = newvalue; //����ŃC�x���g����������̂Ń}�E�X�œ��������ꍇ�Ɠ��������ɂȂ�
        }

        private void ProcessSequenceKey(Keys modifier, Keys body) {
            byte[] data;
            data = GetTerminal().SequenceKeyData(modifier, body);
            SendBytes(data);
        }

        private void MakeCurrentLineVisible() {

            TerminalDocument document = GetDocument();
            if (document.CurrentLineNumber - document.FirstLineNumber < _VScrollBar.Value) { //��ɉB�ꂽ
                document.TopLineNumber = document.CurrentLineNumber;
                _session.Terminal.TransientScrollBarValues.Value = document.TopLineNumber - document.FirstLineNumber;
            }
            else if (_VScrollBar.Value + document.TerminalHeight <= document.CurrentLineNumber - document.FirstLineNumber) { //���ɉB�ꂽ
                int n = document.CurrentLineNumber - document.FirstLineNumber - document.TerminalHeight + 1;
                if (n < 0)
                    n = 0;
                GetTerminal().TransientScrollBarValues.Value = n;
                GetDocument().TopLineNumber = n + document.FirstLineNumber;
            }
        }

        protected override void OnResize(EventArgs args) {
            base.OnResize(args);

            //Debug.WriteLine(String.Format("TC RESIZE {0} {1} {2},{3}", _resizeCount++, DateTime.Now.ToString(), this.Size.Width, this.Size.Height));
            //Debug.WriteLine(new StackTrace(true).ToString());
            //�ŏ������ɂ͂Ȃ������g�̕��������O�ɂȂ��Ă��܂�
            if (this.DesignMode || this.FindForm() == null || this.FindForm().WindowState == FormWindowState.Minimized || _session == null)
                return;

            Size ts = CalcTerminalSize(GetRenderProfile());

            if (!IsConnectionClosed() && (ts.Width != GetDocument().TerminalWidth || ts.Height != GetDocument().TerminalHeight)) {
                ResizeTerminal(ts.Width, ts.Height);
                ShowSizeTip(ts.Width, ts.Height);
                CommitTransientScrollBar();
            }
        }
        private void OnHideSizeTip(object sender, EventArgs args) {
            Debug.Assert(!this.InvokeRequired);
            _sizeTip.Visible = false;
            _sizeTipTimer.Stop();
        }

        public override RenderProfile GetRenderProfile() {
            if (_session != null) {
                ITerminalSettings ts = _session.TerminalSettings;
                if (ts.UsingDefaultRenderProfile)
                    return GEnv.DefaultRenderProfile;
                else
                    return ts.RenderProfile;
            }
            else
                return GEnv.DefaultRenderProfile;
        }
        protected override void CommitTransientScrollBar() {
            if (_session != null) {	// TerminalPane�����^�C�~���O�ł��̃��\�b�h���Ă΂ꂽ�Ƃ���NullReferenceException�ɂȂ�̂�h��
                _ignoreValueChangeEvent = true;
                GetTerminal().CommitScrollBar(_VScrollBar, true);	//!! �����i�X�N���[���o�[�j�̏����͏d��
                _ignoreValueChangeEvent = false;
            }
        }

        public override GLine GetTopLine() {
            //TODO Pane���̃N���X�`�F���W���ł���悤�ɂȂ����炱�������P
            return _session == null ? base.GetTopLine() : GetDocument().TopLine;
        }

        protected override void AdjustCaret(Caret caret) {
            if (_session == null)
                return;

            if (IsConnectionClosed() || !this.Focused || _inIMEComposition)
                caret.Enabled = false;
            else {
                TerminalDocument d = GetDocument();
                caret.X = d.CaretColumn;
                caret.Y = d.CurrentLineNumber - d.TopLineNumber;
                caret.Enabled = caret.Y >= 0 && caret.Y < d.TerminalHeight;
            }
        }

        public Size CalcTerminalSize(RenderProfile prof) {
            SizeF charPitch = prof.Pitch;
            Win32.SystemMetrics sm = GEnv.SystemMetrics;
            int width = (int)Math.Floor((float)(this.ClientSize.Width - sm.ScrollBarWidth - CharacterDocumentViewer.BORDER * 2) / charPitch.Width);
            int height = (int)Math.Floor((float)(this.ClientSize.Height - CharacterDocumentViewer.BORDER * 2 + prof.LineSpacing) / (charPitch.Height + prof.LineSpacing));
            if (width <= 0)
                width = 1; //�ɒ[�ȃ��T�C�Y������ƕ��̒l�ɂȂ邱�Ƃ�����
            if (height <= 0)
                height = 1;
            return new Size(width, height);
        }



        private void ShowSizeTip(int width, int height) {
            const int MARGIN = 8;
            //Form form = GEnv.Frame.AsForm();
            //if(form==null || !form.Visible) return; //�N�����ɂ͕\�����Ȃ�
            if (!this.Visible)
                return;

            Point pt = new Point(this.Width - _VScrollBar.Width - _sizeTip.Width - MARGIN, this.Height - _sizeTip.Height - MARGIN);

            _sizeTip.Text = String.Format("{0} * {1}", width, height);
            _sizeTip.Location = pt;
            _sizeTip.Visible = true;

            _sizeTipTimer.Stop();
            _sizeTipTimer.Start();
        }
        //�s�N�Z���P�ʂ̃T�C�Y���󂯎��A�`�b�v��\��
        public void SplitterDragging(int width, int height) {
            SizeF charSize = GetRenderProfile().Pitch;
            Win32.SystemMetrics sm = GEnv.SystemMetrics;
            width = (int)Math.Floor(((float)width - sm.ScrollBarWidth - sm.ControlBorderWidth * 2) / charSize.Width);
            height = (int)Math.Floor((float)(height - sm.ControlBorderHeight * 2) / charSize.Height);
            ShowSizeTip(width, height);
        }

        private void ResizeTerminal(int width, int height) {
            //Debug.WriteLine(String.Format("Resize {0} {1}", width, height));

            //Document�֒ʒm
            GetDocument().Resize(width, height);

            if (_session.Terminal.CurrentModalTerminalTask != null)
                return; //�ʃ^�X�N�������Ă���Ƃ��͖���
            if (GetTerminal().TerminalMode == TerminalMode.Application) //���T�C�Y���Ă��X�N���[�����[�W�������X�V����邩�͕�����Ȃ����A�ꉞ�S��ʂ��X�V����
                GetDocument().SetScrollingRegion(0, height - 1);
            GetTerminal().Reset();
            if (_VScrollBar.Enabled) {
                bool scroll = IsAutoScrollMode();
                _VScrollBar.LargeChange = height;
                if (scroll)
                    MakeCurrentLineVisible();
            }

            //�ڑ���֒ʒm
            GetTerminalTransmission().Resize(width, height);
            InvalidateEx();
        }
        //���ݍs��������悤�Ɏ����I�ɒǐ����Ă����ׂ����ǂ����̔���
        private bool IsAutoScrollMode() {
            TerminalDocument doc = GetDocument();
            return GetTerminal().TerminalMode == TerminalMode.Normal &&
                doc.CurrentLineNumber >= doc.TopLineNumber + doc.TerminalHeight - 1 &&
                (!_VScrollBar.Enabled || _VScrollBar.Value + _VScrollBar.LargeChange > _VScrollBar.Maximum);
        }


        //IME�̈ʒu���킹�ȂǁB���{����͊J�n���A���݂̃L�����b�g�ʒu����IME���X�^�[�g������B
        private void AdjustIMEComposition() {
            TerminalDocument document = GetDocument();
            IntPtr hIMC = Win32.ImmGetContext(this.Handle);
            RenderProfile prof = GetRenderProfile();

            //�t�H���g�̃Z�b�g�͂P����΂悢�̂��H
            Win32.LOGFONT lf = new Win32.LOGFONT();
            prof.CalcFont(null, CharGroup.CJKZenkaku).ToLogFont(lf);
            Win32.ImmSetCompositionFont(hIMC, lf);

            Win32.COMPOSITIONFORM form = new Win32.COMPOSITIONFORM();
            form.dwStyle = Win32.CFS_POINT;
            Win32.SystemMetrics sm = GEnv.SystemMetrics;
            //Debug.WriteLine(String.Format("{0} {1} {2}", document.CaretColumn, charwidth, document.CurrentLine.CharPosToDisplayPos(document.CaretColumn)));
            form.ptCurrentPos.x = sm.ControlBorderWidth + (int)(prof.Pitch.Width * (document.CaretColumn));
            form.ptCurrentPos.y = sm.ControlBorderHeight + (int)((prof.Pitch.Height + prof.LineSpacing) * (document.CurrentLineNumber - document.TopLineNumber));
            bool r = Win32.ImmSetCompositionWindow(hIMC, ref form);
            Debug.Assert(r);
            Win32.ImmReleaseContext(this.Handle, hIMC);
        }
        private void ClearIMEComposition() {
            IntPtr hIMC = Win32.ImmGetContext(this.Handle);
            Win32.ImmNotifyIME(hIMC, Win32.NI_COMPOSITIONSTR, Win32.CPS_CANCEL, 0);
            Win32.ImmReleaseContext(this.Handle, hIMC);
            _inIMEComposition = false;
        }

        public void ApplyRenderProfile(RenderProfile prof) {
            if (this.EnabledEx) {
                this.BackColor = prof.BackColor;
                Size ts = CalcTerminalSize(prof);
                if (!IsConnectionClosed() && (ts.Width != GetDocument().TerminalWidth || ts.Height != GetDocument().TerminalHeight)) {
                    ResizeTerminal(ts.Width, ts.Height);
                }
                Invalidate();
            }
        }
        public void ApplyTerminalOptions(ITerminalEmulatorOptions opt) {
            if (this.EnabledEx) {
                if (GetTerminalSettings().UsingDefaultRenderProfile) {
                    ApplyRenderProfile(opt.CreateRenderProfile());
                }
                _caret.Style = opt.CaretType;
                _caret.Blink = opt.CaretBlink;
                _caret.Color = opt.CaretColor;
                _caret.Reset();
            }
        }

        // Overrides CharacterDocumentViewer's scrollbar control
        protected override void OnMouseWheelCore(MouseEventArgs e) {
            // do nothing.
            // Scrollbar control will be done by MouseWheelHandler.
        }

        protected override void OnGotFocus(EventArgs args) {
            base.OnGotFocus(args);
            if (!this.EnabledEx)
                return;
            if (GetTerminal().GetFocusReportingMode()) {
                byte[] data = new byte[] { 0x1b, 0x5b, 0x49 };
                TransmitDirect(data, 0, data.Length);
            }

            if (this.CharacterDocument != null) { //�������ߒ��̂Ƃ��͖���

                //NOTE TerminalControl��Session�ɂ��Ă͖��m�A�Ƃ����O��ɂ����ق��������̂�������Ȃ�
                TerminalEmulatorPlugin.Instance.GetSessionManager().ActivateDocument(this.CharacterDocument, ActivateReason.ViewGotFocus);

            }
        }

        protected override void OnLostFocus(EventArgs args) {
            base.OnLostFocus(args);
            if (!this.EnabledEx)
                return;
            if (GetTerminal().GetFocusReportingMode()) {
                byte[] data = new byte[] { 0x1b, 0x5b, 0x4f };
                TransmitDirect(data, 0, data.Length);
            }

            if (_inIMEComposition)
                ClearIMEComposition();
        }
        //Drag&Drop�֌W
        protected override void OnDragEnter(DragEventArgs args) {
            base.OnDragEnter(args);
            try {
                IWinFormsService wfs = TerminalEmulatorPlugin.Instance.GetWinFormsService();
                IPoderosaDocument document = (IPoderosaDocument)wfs.GetDraggingObject(args.Data, typeof(IPoderosaDocument));
                if (document != null)
                    args.Effect = DragDropEffects.Move;
                else
                    wfs.BypassDragEnter(this, args);
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }
        protected override void OnDragDrop(DragEventArgs args) {
            base.OnDragDrop(args);
            try {
                IWinFormsService wfs = TerminalEmulatorPlugin.Instance.GetWinFormsService();
                IPoderosaDocument document = (IPoderosaDocument)wfs.GetDraggingObject(args.Data, typeof(IPoderosaDocument));
                if (document != null) {
                    IPoderosaView view = (IPoderosaView)this.GetAdapter(typeof(IPoderosaView));
                    TerminalEmulatorPlugin.Instance.GetSessionManager().AttachDocumentAndView(document, view);
                    TerminalEmulatorPlugin.Instance.GetSessionManager().ActivateDocument(document, ActivateReason.DragDrop);
                }
                else
                    wfs.BypassDragDrop(this, args);
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }

        private void ProcessVScrollMessage(int cmd) {
            int newval = _VScrollBar.Value;
            switch (cmd) {
                case 0: //SB_LINEUP
                    newval--;
                    break;
                case 1: //SB_LINEDOWN
                    newval++;
                    break;
                case 2: //SB_PAGEUP
                    newval -= GetDocument().TerminalHeight;
                    break;
                case 3: //SB_PAGEDOWN
                    newval += GetDocument().TerminalHeight;
                    break;
            }

            if (newval < 0)
                newval = 0;
            if (newval > _VScrollBar.Maximum - _VScrollBar.LargeChange)
                newval = _VScrollBar.Maximum - _VScrollBar.LargeChange + 1;
            _VScrollBar.Value = newval;
        }


        /*
         * ���̎��ӂŎg�������ȃf�o�b�O�p�̃R�[�h�f��
         private static bool _IMEFlag;
         private static int _callnest;
         
            _callnest++;
            if(_IMEFlag) {
                if(msg.Msg!=13 && msg.Msg!=14 && msg.Msg!=15 && msg.Msg!=0x14 && msg.Msg!=0x85 && msg.Msg!=0x20 && msg.Msg!=0x84) //�������̂͂���
                    Debug.WriteLine(String.Format("{0} Msg {1:X} WP={2:X} LP={3:X}", _callnest, msg.Msg, msg.WParam.ToInt32(), msg.LParam.ToInt32()));
            }
            base.WndProc(ref msg);
            _callnest--;
         */
        private bool _lastCompositionFlag;
        //IME�֌W���������邽�߂ɂ��Ȃ�̋�J�B�Ȃ������Ȃ̂��ɂ��Ă͕ʃh�L�������g�Q��
        protected override void WndProc(ref Message msg) {
            if (_lastCompositionFlag) {
                LastCompositionWndProc(ref msg);
                return;
            }

            int m = msg.Msg;
            if (m == Win32.WM_IME_COMPOSITION) {
                if ((msg.LParam.ToInt32() & 0xFF) == 0) { //�ŏI�m�莞�̓��ꏈ���։I�񂳂���t���O�𗧂Ă�
                    _lastCompositionFlag = true;
                    base.WndProc(ref msg); //���̒��ő����Ă���WM_IME_CHAR�͖���
                    _lastCompositionFlag = false;
                    return;
                }
            }

            base.WndProc(ref msg); //�ʏ펞

            if (m == Win32.WM_IME_STARTCOMPOSITION) {
                _inIMEComposition = true; //_inIMEComposition��WM_IME_STARTCOMPOSITION�ł����Z�b�g���Ȃ�
                AdjustIMEComposition();
            }
            else if (m == Win32.WM_IME_ENDCOMPOSITION) {
                _inIMEComposition = false;
            }
        }
        private void LastCompositionWndProc(ref Message msg) {
            if (msg.Msg == Win32.WM_IME_CHAR) {
                char ch = (char)msg.WParam;
                SendChar(ch);
            }
            else
                base.WndProc(ref msg);
        }
    }

    /// <summary>
    /// Mouse wheel handler.
    /// </summary>
    internal class MouseWheelHandler : DefaultMouseHandler {
        private readonly TerminalControl _control;
        private readonly VScrollBar _scrollBar;
        private AbstractTerminal _terminal = null;
        private readonly object _terminalSync = new object();

        public MouseWheelHandler(TerminalControl control, VScrollBar scrollBar)
            : base("mousewheel") {
            _control = control;
            _scrollBar = scrollBar;
        }

        public void Attach(ITerminalControlHost session) {
            lock (_terminalSync) {
                _terminal = session.Terminal;
            }
        }

        public void Detach() {
            lock (_terminalSync) {
                _terminal = null;
            }
        }

        public override UIHandleResult OnMouseWheel(MouseEventArgs args) {
            if (!_control.EnabledEx)
                return UIHandleResult.Pass;

            lock (_terminalSync) {
                if (_terminal != null && !GEnv.Options.AllowsScrollInAppMode && _terminal.TerminalMode == TerminalMode.Application) {
                    // Emulate Up Down keys
                    int m = GEnv.Options.WheelAmount;
                    for (int i = 0; i < m; i++) {
                        byte[] data = _terminal.SequenceKeyData(Keys.None, args.Delta > 0 ? Keys.Up : Keys.Down);
                        _control.TransmitDirect(data, 0, data.Length);
                    }
                    return UIHandleResult.Stop;
                }
            }

            if (_scrollBar.Enabled) {
                int d = args.Delta / 120; //�J��������Delta��120�B�����1��-1������͂�
                d *= GEnv.Options.WheelAmount;

                int newval = _scrollBar.Value - d;
                if (newval < 0)
                    newval = 0;
                if (newval > _scrollBar.Maximum - _scrollBar.LargeChange)
                    newval = _scrollBar.Maximum - _scrollBar.LargeChange + 1;
                _scrollBar.Value = newval;
            }

            return UIHandleResult.Stop;
        }
    }

    /// <summary>
    /// XTerm mouse tracking support.
    /// </summary>
    /// <remarks>
    /// <para>This handler must be placed on the head of the handler list.</para>
    /// <para>This handler controls whether other handler should process the incoming event.
    /// Actual processes for the mouse tracking are delegated to the AbstractTerminal.</para>
    /// </remarks>
    internal class MouseTrackingHandler : DefaultMouseHandler {
        private readonly TerminalControl _control;
        private AbstractTerminal _terminal = null;
        private readonly object _terminalSync = new object();
        private MouseButtons _pressedButtons = MouseButtons.None;   // buttons that being grabbed by mouse tracking

#if DEBUG_MOUSETRACKING
        private static int _instanceCounter = 0;
        private readonly string _instance;
#endif

        public MouseTrackingHandler(TerminalControl control)
            : base("mousetracking") {
            _control = control;
#if DEBUG_MOUSETRACKING
            _instance = "MT[" + (++_instanceCounter).ToString() + "]";
#endif
        }

        public void Attach(ITerminalControlHost session) {
            lock (_terminalSync) {
                _terminal = session.Terminal;
            }
        }

        public void Detach() {
            lock (_terminalSync) {
                _terminal = null;
            }
        }

        private bool IsGrabbing() {
            return _pressedButtons != MouseButtons.None;
        }

        private bool IsEscaped() {
            return false;   // TODO
        }

        public override UIHandleResult OnMouseDown(MouseEventArgs args) {
            Keys modKeys = Control.ModifierKeys;

#if DEBUG_MOUSETRACKING
            Debug.WriteLine(_instance + " OnMouseDown: Buttons = " + _pressedButtons.ToString());
#endif
            if (!IsGrabbing()) {
                if (IsEscaped())
                    return UIHandleResult.Pass; // bypass mouse tracking
            }

            int col, row;
            _control.MousePosToTextPos(args.X, args.Y, out col, out row);

            bool processed;

            lock (_terminalSync) {
                if (_terminal == null)
                    return UIHandleResult.Pass;

                processed = _terminal.ProcessMouse(TerminalMouseAction.ButtonDown, args.Button, modKeys, row, col);
            }

            if (processed) {
                _pressedButtons |= args.Button;
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseDown: Processed --> Capture : Buttons = " + _pressedButtons.ToString());
#endif
                return UIHandleResult.Capture;  // process next mouse events exclusively.
            }
            else {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseDown: Not Processed : Buttons = " + _pressedButtons.ToString());
#endif
                if (IsGrabbing())
                    return UIHandleResult.Stop;
                else
                    return UIHandleResult.Pass;
            }
        }

        public override UIHandleResult OnMouseUp(MouseEventArgs args) {
#if DEBUG_MOUSETRACKING
            Debug.WriteLine(_instance + " OnMouseUp: Buttons = " + _pressedButtons.ToString());
#endif
            if (!IsGrabbing())
                return UIHandleResult.Pass;

            Keys modKeys = Control.ModifierKeys;

            int col, row;
            _control.MousePosToTextPos(args.X, args.Y, out col, out row);

            // Note:
            // We keep this handler in "Captured" status while any other mouse buttons being pressed.
            // "Captured" handler can process mouse events exclusively.
            //
            // This trick would provide good experience to the user,
            // but it doesn't work expectedly in the following scenario.
            //
            //   1. Press left button on Terminal-1
            //   2. Press right button on Terminal-1
            //   3. Move (drag) mouse to Terminal-2
            //   4. Release left button on Terminal-2
            //   5. Release right button on Terminal-2
            //
            // In step 1, System.Windows.Forms.Control object starts mouse capture automatically
            // when left button was pressed.
            // So the next mouse-up event will be notified to the Terminal-1 (step 4).
            // But Control object stops mouse capture by mouse-up event for any button.
            // Mouse-up event of the right button in step 5 will not be notified to the Terminal-1,
            // and the handler of the Terminal-1 will not end "Captured" status.
            //
            // The case like above will happen rarely.
            // To avoid never ending "Captured" status, OnMouseMove() ends "Captured" status
            // if no mouse buttons were set in the MouseEventArgs.Button.
            // 

            lock (_terminalSync) {
                if (_terminal != null) {
                    // Mouse tracking mode may be already turned off.
                    // We just ignore result of ProcessMouse().
                    _terminal.ProcessMouse(TerminalMouseAction.ButtonUp, args.Button, modKeys, row, col);
                }
            }

            _pressedButtons &= ~args.Button;

            if (IsGrabbing()) {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseUp: Continue Capture : Buttons = " + _pressedButtons.ToString());
#endif
                return UIHandleResult.Stop;
            }
            else {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseUp: End Capture : Buttons = " + _pressedButtons.ToString());
#endif
                return UIHandleResult.EndCapture;
            }
        }

        public override UIHandleResult OnMouseMove(MouseEventArgs args) {
            Keys modKeys = Control.ModifierKeys;

#if DEBUG_MOUSETRACKING
            Debug.WriteLine(_instance + " OnMouseMove: Buttons = " + _pressedButtons.ToString());
#endif
            if (!IsGrabbing()) {
                if (IsEscaped())
                    return UIHandleResult.Pass; // bypass mouse tracking
            }

            int col, row;
            _control.MousePosToTextPos(args.X, args.Y, out col, out row);

            if (IsGrabbing() && args.Button == MouseButtons.None) {
                // mouse button has been released in another terminal ?
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseMove: End Capture (Reset)");
#endif
                lock (_terminalSync) {
                    if (_terminal != null) {
                        int buttons = (int)_pressedButtons;
                        int buttonBit = 1;
                        while (buttonBit != 0) {
                            if ((buttons & buttonBit) != 0) {
#if DEBUG_MOUSETRACKING
                                Debug.WriteLine(_instance + " OnMouseMove: MouseUp " + ((MouseButtons)buttonBit).ToString());
#endif
                                _terminal.ProcessMouse(TerminalMouseAction.ButtonUp, (MouseButtons)buttonBit, modKeys, row, col);
                            }
                            buttonBit <<= 1;
                        }
                    }
                }

                _pressedButtons = MouseButtons.None;
                return UIHandleResult.EndCapture;
            }

            bool processed;

            lock (_terminalSync) {
                if (_terminal == null)
                    return UIHandleResult.Pass;

                processed = _terminal.ProcessMouse(TerminalMouseAction.MouseMove, MouseButtons.None, modKeys, row, col);
            }

            if (processed) {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseMove: Processed");
#endif
                return UIHandleResult.Stop;
            }
            else {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseMove: Ignored");
#endif
                return UIHandleResult.Pass;
            }
        }

        public override UIHandleResult OnMouseWheel(MouseEventArgs args) {
            Keys modKeys = Control.ModifierKeys;

#if DEBUG_MOUSETRACKING
            Debug.WriteLine(_instance + " OnMouseWheel: Buttons = " + _pressedButtons.ToString());
#endif
            if (!IsGrabbing()) {
                if (IsEscaped())
                    return UIHandleResult.Pass; // bypass mouse tracking
            }

            int col, row;
            _control.MousePosToTextPos(args.X, args.Y, out col, out row);

            TerminalMouseAction action = (args.Delta > 0) ?
                TerminalMouseAction.WheelUp : TerminalMouseAction.WheelDown;

            bool processed;

            lock (_terminalSync) {
                if (_terminal == null)
                    return UIHandleResult.Pass;

                processed = _terminal.ProcessMouse(action, MouseButtons.None, modKeys, row, col);
            }

            if (processed) {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseWheel: Processed");
#endif
                return UIHandleResult.Stop;
            }
            else {
#if DEBUG_MOUSETRACKING
                Debug.WriteLine(_instance + " OnMouseWheel: Ignored");
#endif
                return UIHandleResult.Pass;
            }
        }
    }

    internal class TerminalEmulatorMouseHandler : DefaultMouseHandler {
        private TerminalControl _control;

        public TerminalEmulatorMouseHandler(TerminalControl control)
            : base("terminal") {
            _control = control;
        }

        public override UIHandleResult OnMouseDown(MouseEventArgs args) {
            return UIHandleResult.Pass;
        }
        public override UIHandleResult OnMouseMove(MouseEventArgs args) {
            return UIHandleResult.Pass;
        }
        public override UIHandleResult OnMouseUp(MouseEventArgs args) {
            if (!_control.EnabledEx)
                return UIHandleResult.Pass;

            if (args.Button == MouseButtons.Right || args.Button == MouseButtons.Middle) {
                ITerminalEmulatorOptions opt = TerminalEmulatorPlugin.Instance.TerminalEmulatorOptions;
                MouseButtonAction act = args.Button == MouseButtons.Right ? opt.RightButtonAction : opt.MiddleButtonAction;
                if (act != MouseButtonAction.None) {
                    if (Control.ModifierKeys == Keys.Shift ^ act == MouseButtonAction.ContextMenu) //�V�t�g�L�[�œ��씽�]
                        ShowContextMenu(new Point(args.X, args.Y));
                    else { //Paste
                        IGeneralViewCommands vc = (IGeneralViewCommands)_control.GetAdapter(typeof(IGeneralViewCommands));
                        TerminalEmulatorPlugin.Instance.GetCommandManager().Execute(vc.Paste, (ICommandTarget)vc.GetAdapter(typeof(ICommandTarget)));
                        //�y�[�X�g��̓t�H�[�J�X
                        if (!_control.Focused)
                            _control.Focus();
                    }

                    return UIHandleResult.Stop;
                }
            }

            return UIHandleResult.Pass;
        }

        public void ShowContextMenu(Point pt) {
            IPoderosaView view = (IPoderosaView)_control.GetAdapter(typeof(IPoderosaView));
            view.ParentForm.ShowContextMenu(TerminalEmulatorPlugin.Instance.ContextMenu, view, _control.PointToScreen(pt), ContextMenuFlags.None);
            //�R�}���h���s�㎩���Ƀt�H�[�J�X
            if (!_control.Focused)
                _control.Focus();
        }
    }

    //�`��p�t�H�[�}���X�����p�N���X
    internal static class DrawingPerformance {
        private static int _receiveDataCount;
        private static long _lastReceivedTime;
        private static int _shortReceiveTimeCount;

        private static int _fullInvalidateCount;
        private static int _partialInvalidateCount;
        private static int _totalInvalidatedLineCount;
        private static int _invalidate1LineCount;

        public static void MarkReceiveData(InvalidatedRegion region) {
            _receiveDataCount++;
            long now = DateTime.Now.Ticks;
            if (_lastReceivedTime != 0) {
                if (now - _lastReceivedTime < 10 * 1000 * 100)
                    _shortReceiveTimeCount++;
            }
            _lastReceivedTime = now;

            if (region.InvalidatedAll)
                _fullInvalidateCount++;
            else {
                _partialInvalidateCount++;
                _totalInvalidatedLineCount += region.LineIDEnd - region.LineIDStart + 1;
                if (region.LineIDStart == region.LineIDEnd)
                    _invalidate1LineCount++;
            }
        }

        public static void Output() {
            Debug.WriteLine(String.Format("ReceiveData:{0}  (short:{1})", _receiveDataCount, _shortReceiveTimeCount));
            Debug.WriteLine(String.Format("FullInvalidate:{0} PartialInvalidate:{1} 1-Line:{2} AvgLine:{3:F2}", _fullInvalidateCount, _partialInvalidateCount, _invalidate1LineCount, (double)_totalInvalidatedLineCount / _partialInvalidateCount));
        }

    }


}
