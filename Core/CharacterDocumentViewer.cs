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

#if DEBUG
#define ONPAINT_TIME_MEASUREMENT
#endif

using Poderosa.Commands;
using Poderosa.Document;
using Poderosa.Forms;
using Poderosa.Sessions;
using Poderosa.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Poderosa.View {
    /*
     * CharacterDocumentの表示を行うコントロール。機能としては次がある。
     * 　縦方向のみスクロールバーをサポート
     * 　再描画の最適化
     * 　キャレットの表示。ただしキャレットを適切に移動する機能は含まれない
     * 
     * 　今後あってもいいかもしれない機能は、行間やPadding(HTML用語の)、行番号表示といったところ
     */
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public abstract class CharacterDocumentViewer : Control, IPoderosaControl, ISelectionListener, SplitMarkSupport.ISite {

        public const int BORDER = 2; //内側の枠線のサイズ

        private int _maxDisplayLines; // restrict lines to display to avoid artifacts
        private bool _errorRaisedInDrawing;
        private readonly List<GLine> _transientLines; //再描画するGLineを一時的に保管する
        private readonly List<GLine> _glinePool;
        private bool _requiresPeriodicRedraw;
        private readonly TextSelection _textSelection;
        private readonly SplitMarkSupport _splitMark;

        private Cursor _uiCursor = INACTIVE_CURSOR; // used to revert cursor that has been changed by OverrideCursor()

        protected readonly MouseHandlerManager _mouseHandlerManager;
        protected VScrollBar _VScrollBar;
        protected bool _enableAutoScrollBarAdjustment; //リサイズ時に自動的に_VScrollBarの値を調整するかどうか
        protected readonly Caret _caret;
        protected DateTime _nextCaretUpdate = DateTime.UtcNow;

        private readonly System.Timers.Timer _updatingTimer;
        private int _updatingState = 0; // avoid overlapping execution of timer event

        private bool _requiresFullInvalidate = false; // requests full-invalidate regardless of the document status

        public delegate void OnPaintTimeObserver(Stopwatch s);

#if ONPAINT_TIME_MEASUREMENT
        private OnPaintTimeObserver _onPaintTimeObserver = null;
#endif

        private static Color INACTIVE_BACK_COLOR {
            get {
                return SystemColors.ControlDark;
            }
        }
        private static Color INACTIVE_SPLITMARK_COLOR {
            get {
                return SystemColors.Window;
            }
        }
        private static Color ACTIVE_SPLITMARK_COLOR {
            get {
                return SystemColors.ControlDark;
            }
        }
        private static Cursor DEFAULT_CURSOR {
            get {
                return Cursors.IBeam;
            }
        }
        private static Cursor INACTIVE_CURSOR {
            get {
                return Cursors.Default;
            }
        }

        /// <summary>
        /// Scope to guarantee consistent access to the document bound to the viewer
        /// </summary>
        /// <remarks>
        /// To avoid frequent memory allocations, this type is defined as struct.
        /// </remarks>
        public struct DocumentScope : IDisposable {
            /// <summary>
            /// Document bound to the viewer. This may be null.
            /// </summary>
            public readonly CharacterDocument Document;

            private readonly ReaderWriterLockSlim _lock;

            public DocumentScope(CharacterDocument document, ReaderWriterLockSlim documentLock) {
                this.Document = document;
                this._lock = documentLock;
            }

            public void Dispose() {
                _lock.ExitReadLock();
            }
        }

        protected CharacterDocumentViewer() {
            _updatingTimer = new System.Timers.Timer();
            _updatingTimer.Interval = 1000.0 / 60.0;
            _updatingTimer.Elapsed += UpdatingTimerElapsed;
            _updatingTimer.AutoReset = true;

            _maxDisplayLines = Int32.MaxValue;
            _enableAutoScrollBarAdjustment = true;
            _transientLines = new List<GLine>();
            _glinePool = new List<GLine>();

            InitializeComponent();

            AdjustScrollBarPosition();
            this.ImeMode = ImeMode.NoControl;
            //SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.DoubleBuffer, true);
            this.DoubleBuffered = true;

            this._VScrollBar.Visible = false;
            this.Cursor = INACTIVE_CURSOR;
            this.BackColor = INACTIVE_BACK_COLOR;
            this.ImeMode = ImeMode.Disable;

            _caret = new Caret();

            _splitMark = new SplitMarkSupport(this, this);
            Pen p = new Pen(INACTIVE_SPLITMARK_COLOR);
            p.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            _splitMark.Pen = p;

            _textSelection = new TextSelection(this);
            _textSelection.AddSelectionListener(this);

            _mouseHandlerManager = new MouseHandlerManager();
            _mouseHandlerManager.AddLastHandler(new TextSelectionUIHandler(this));
            _mouseHandlerManager.AddLastHandler(new SplitMarkUIHandler(_splitMark));
            _mouseHandlerManager.AttachControl(this);

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        internal TextSelection TextSelection {
            get {
                return _textSelection;
            }
        }
        public ITextSelection ITextSelection {
            get {
                return _textSelection;
            }
        }
        internal MouseHandlerManager MouseHandlerManager {
            get {
                return _mouseHandlerManager;
            }
        }

        public Caret Caret {
            get {
                return _caret;
            }
        }

        public VScrollBar VScrollBar {
            get {
                return _VScrollBar;
            }
        }

        public void ShowVScrollBar() {
            _VScrollBar.Visible = true;
        }

        public void HideVScrollBar() {
            _VScrollBar.Visible = false;
        }

        public void SetUICursor(Cursor cursor) {
            if (this.InvokeRequired) {
                this.BeginInvoke((MethodInvoker)delegate() {
                    SetUICursor(cursor);
                });
                return;
            }
            _uiCursor = cursor;
            this.Cursor = _uiCursor;
        }

        public void ResetUICursor() {
            SetUICursor(HasDocument ? DEFAULT_CURSOR : INACTIVE_CURSOR);
        }

        public abstract bool HasDocument {
            get;
        }

        public abstract DocumentScope GetDocumentScope();

        protected virtual void OnUpdatingTimer() {
            // do pending tasks
        }

        #region IAdaptable
        public virtual IAdaptable GetAdapter(Type adapter) {
            return SessionManagerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
        #endregion

        #region OnPaint time measurement

        public void SetOnPaintTimeObserver(OnPaintTimeObserver observer) {
#if ONPAINT_TIME_MEASUREMENT
            _onPaintTimeObserver = observer;
#endif
        }

        #endregion

        protected void DocumentChanged(bool hasDocument) {
            _textSelection.Clear();

            _requiresPeriodicRedraw = false;

            _VScrollBar.Visible = hasDocument;
            _splitMark.Pen.Color = hasDocument ? ACTIVE_SPLITMARK_COLOR : INACTIVE_SPLITMARK_COLOR;
            _uiCursor = hasDocument ? DEFAULT_CURSOR : INACTIVE_CURSOR;
            this.Cursor = _uiCursor;
            this.BackColor = hasDocument ? GetRenderProfile().BackColor : INACTIVE_BACK_COLOR;
            this.ImeMode = hasDocument ? ImeMode.NoControl : ImeMode.Disable;

            _updatingTimer.Enabled = hasDocument;

            if (_enableAutoScrollBarAdjustment) {
                AdjustScrollBar();
            }
        }

        private void UpdatingTimerElapsed(object sender, System.Timers.ElapsedEventArgs e) {
            if (Interlocked.CompareExchange(ref _updatingState, 1, 0) != 0) {
                return;
            }

            if (!this.IsDisposed) {
                OnUpdatingTimer();
                using (DocumentScope docScope = GetDocumentScope()) {
                    DateTime now = DateTime.UtcNow;
                    if (now >= _nextCaretUpdate && docScope.Document != null) {
                        // Note:
                        //  Currently, blinking status of the caret is used also for displaying "blink" characters.
                        //  So the blinking status of the caret have to be updated here even if the caret blinking was not enabled.
                        _caret.Tick();

                        if (_requiresPeriodicRedraw) { // blinking characters exist
                            _requiresPeriodicRedraw = false;
                            docScope.Document.InvalidateAll();
                        }
                        else {
                            docScope.Document.InvalidatedRegion.InvalidateLine(GetTopLine().ID + _caret.Y);
                        }

                        TimeSpan d = TimeSpan.FromMilliseconds(WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.CaretInterval);
                        DateTime next = _nextCaretUpdate + d;
                        if (next < now) {
                            next = now + d;
                        }
                        _nextCaretUpdate = next;
                    }

                    AdaptiveInvalidate(docScope.Document);
                }
            }

            Interlocked.Exchange(ref _updatingState, 0);
        }

        private void AdaptiveInvalidate(CharacterDocument document) {
            if (document == null) {
                return;
            }

            // In order to clear the InvalidatedRegion, the call of GetCopyAndReset() is required
            // even if full-invalidate will be done.
            InvalidatedRegion rgn = document.InvalidatedRegion.GetCopyAndReset();

            if (_requiresFullInvalidate) {
                _requiresFullInvalidate = false;
                FullInvalidate();
                return;
            }

            if (rgn.IsEmpty) {
                return;
            }

            if (rgn.InvalidatedAll) {
                FullInvalidate();
                return;
            }

            Rectangle r = new Rectangle();
            r.X = 0;
            r.Width = this.ClientSize.Width;
            int topLine = GetTopLine().ID;
            int y1 = rgn.LineIDStart - topLine;
            int y2 = rgn.LineIDEnd + 1 - topLine;
            RenderProfile prof = GetRenderProfile();
            r.Y = BORDER + (int)(y1 * (prof.Pitch.Height + prof.LineSpacing));
            r.Height = (int)((y2 - y1) * (prof.Pitch.Height + prof.LineSpacing)) + 1;

            PartialInvalidate(r);
        }

        private void FullInvalidate() {
            if (this.InvokeRequired) {
                this.BeginInvoke((MethodInvoker)delegate() {
                    Invalidate();
                });
            }
            else {
                Invalidate();
            }
        }

        private void PartialInvalidate(Rectangle r) {
            if (this.InvokeRequired) {
                this.BeginInvoke((MethodInvoker)delegate() {
                    Invalidate(r);
                });
            }
            else {
                Invalidate(r);
            }
        }

        //自己サイズからScrollBarを適切にいじる
        public void AdjustScrollBar() {
            using (DocumentScope docScope = GetDocumentScope()) {
                if (docScope.Document == null) {
                    return;
                }

                RenderProfile prof = GetRenderProfile();
                float ch = prof.Pitch.Height + prof.LineSpacing;
                int largechange = (int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / ch); //きちんと表示できる行数をLargeChangeにセット
                int current = GetTopLine().ID - docScope.Document.FirstLineNumber;
                int size = Math.Max(docScope.Document.Size, current + largechange);
                if (size <= largechange) {
                    _VScrollBar.Enabled = false;
                }
                else {
                    _VScrollBar.Enabled = true;
                    _VScrollBar.LargeChange = largechange;
                    _VScrollBar.Maximum = size - 1; //この-1が必要なのが妙な仕様だ
                }
            }
        }

        //このあたりの処置定まっていない
        private RenderProfile _privateRenderProfile = null;
        public void SetPrivateRenderProfile(RenderProfile prof) {
            _privateRenderProfile = prof;
        }

        //overrideして別の方法でRenderProfileを取得することもある
        public virtual RenderProfile GetRenderProfile() {
            return _privateRenderProfile;
        }

        protected virtual void CommitTransientScrollBar() {
            //ViewerはUIによってしか切り取れないからここでは何もしなくていい
        }

        //行数で表示可能な高さを返す
        protected virtual int GetHeightInLines() {
            RenderProfile prof = GetRenderProfile();
            float ch = prof.Pitch.Height + prof.LineSpacing;
            int height = (int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / ch);
            return (height > 0) ? height : 0;
        }

        //_documentのうちどれを先頭(1行目)として表示するかを返す
        public virtual GLine GetTopLine() {
            using (DocumentScope docScope = GetDocumentScope()) {
                if (docScope.Document == null) {
                    return null;
                }
                return docScope.Document.FindLine(docScope.Document.FirstLine.ID + _VScrollBar.Value);
            }
        }

        public void MousePosToTextPos(int mouseX, int mouseY, out int textX, out int textY) {
            SizeF pitch = GetRenderProfile().Pitch;
            textX = RuntimeUtil.AdjustIntRange((int)Math.Floor((mouseX - CharacterDocumentViewer.BORDER) / pitch.Width), 0, Int32.MaxValue);
            textY = RuntimeUtil.AdjustIntRange((int)Math.Floor((mouseY - CharacterDocumentViewer.BORDER) / (pitch.Height + GetRenderProfile().LineSpacing)), 0, Int32.MaxValue);
        }

        public void MousePosToTextPos_AllowNegative(int mouseX, int mouseY, out int textX, out int textY) {
            SizeF pitch = GetRenderProfile().Pitch;
            textX = (int)Math.Floor((mouseX - CharacterDocumentViewer.BORDER) / pitch.Width);
            textY = (int)Math.Floor((mouseY - CharacterDocumentViewer.BORDER) / (pitch.Height + GetRenderProfile().LineSpacing));
        }

        private void OnVScrollBarValueChanged(object sender, EventArgs args) {
            VScrollBarValueChanged();
        }

        //_VScrollBar.ValueChangedイベント
        protected virtual void VScrollBarValueChanged() {
            if (_enableAutoScrollBarAdjustment)
                Invalidate();
        }

        //キャレットの座標設定、表示の可否を設定
        protected virtual void AdjustCaret(Caret caret) {
        }

        protected void RestrictDisplayArea(int width, int height) {
            _maxDisplayLines = height;
        }

        public void InvalidateAll() {
            _requiresFullInvalidate = true;
        }

        private void InitializeComponent() {
            this.SuspendLayout();
            this._VScrollBar = new System.Windows.Forms.VScrollBar();
            // 
            // _VScrollBar
            // 
            this._VScrollBar.Enabled = false;
            //this._VScrollBar.Dock = DockStyle.Right;
            this._VScrollBar.Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom;
            this._VScrollBar.LargeChange = 1;
            this._VScrollBar.Minimum = 0;
            this._VScrollBar.Value = 0;
            this._VScrollBar.Maximum = 2;
            this._VScrollBar.Name = "_VScrollBar";
            this._VScrollBar.TabIndex = 0;
            this._VScrollBar.TabStop = false;
            this._VScrollBar.Cursor = Cursors.Default;
            this._VScrollBar.Visible = false;
            this._VScrollBar.ValueChanged += OnVScrollBarValueChanged;
            this.Controls.Add(_VScrollBar);
            this.ResumeLayout();
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                _updatingTimer.Dispose();
                _caret.Dispose();
                _splitMark.Pen.Dispose();
            }
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            if (_VScrollBar.Visible)
                AdjustScrollBarPosition();
            if (_enableAutoScrollBarAdjustment && HasDocument)
                AdjustScrollBar();

            Invalidate();
        }

        //NOTE 自分のDockがTopかLeftのとき、スクロールバーの位置が追随してくれないみたい
        private void AdjustScrollBarPosition() {
            _VScrollBar.Height = this.ClientSize.Height;
            _VScrollBar.Left = this.ClientSize.Width - _VScrollBar.Width;
        }

        //描画の本体
        protected override sealed void OnPaint(PaintEventArgs e) {
#if ONPAINT_TIME_MEASUREMENT
            Stopwatch onPaintSw = (_onPaintTimeObserver != null) ? Stopwatch.StartNew() : null;
#endif

            base.OnPaint(e);

            if (!this.DesignMode) {
                try {
                    using (DocumentScope docScope = GetDocumentScope()) {
                        if (docScope.Document != null) {
                            ShowVScrollBar();

                            Rectangle clip = e.ClipRectangle;
                            Graphics g = e.Graphics;
                            RenderProfile profile = GetRenderProfile();

                            // determine background color of the view
                            Color backColor;
                            if (docScope.Document.IsApplicationMode) {
                                backColor = profile.GetBackColor(docScope.Document.ApplicationModeBackColor);
                            }
                            else {
                                backColor = profile.BackColor;
                            }

                            if (this.BackColor != backColor)
                                this.BackColor = backColor; // set background color of the view

                            // draw background image if it is required.
                            if (!docScope.Document.IsApplicationMode) {
                                Image img = profile.GetImage();
                                if (img != null) {
                                    DrawBackgroundImage(g, img, profile.ImageStyle, clip);
                                }
                            }

                            _caret.Enabled = _caret.Enabled && this.Focused; //TODO さらにIME起動中はキャレットを表示しないように. TerminalControlだったらAdjustCaretでIMEをみてるので問題はない

                            //描画用にテンポラリのGLineを作り、描画中にdocumentをロックしないようにする
                            //!!ここは実行頻度が高いのでnewを毎回するのは避けたいところだ
                            RenderParameter param;
                            lock (docScope.Document) {
                                CommitTransientScrollBar();
                                BuildTransientDocument(docScope.Document, clip, out param);
                            }

                            DrawLines(g, param, backColor);

                            if (_caret.Enabled && (!_caret.Blink || _caret.IsActiveTick)) { //点滅しなければEnabledによってのみ決まる
                                if (_caret.Style == CaretType.Line)
                                    DrawBarCaret(g, param, _caret.X, _caret.Y);
                                else if (_caret.Style == CaretType.Underline)
                                    DrawUnderLineCaret(g, param, _caret.X, _caret.Y);
                            }
                        }
                        else {
                            HideVScrollBar();
                        }

                        //マークの描画
                        _splitMark.OnPaint(e);
                    }
                }
                catch (Exception ex) {
                    if (!_errorRaisedInDrawing) { //この中で一度例外が発生すると繰り返し起こってしまうことがままある。なので初回のみ表示してとりあえず切り抜ける
                        _errorRaisedInDrawing = true;
                        RuntimeUtil.ReportException(ex);
                    }
                }
            }

#if ONPAINT_TIME_MEASUREMENT
            if (onPaintSw != null) {
                onPaintSw.Stop();
                if (_onPaintTimeObserver != null) {
                    _onPaintTimeObserver(onPaintSw);
                }
            }
#endif
        }

        private void BuildTransientDocument(CharacterDocument document, Rectangle clip, out RenderParameter param) {
            Debug.Assert(document != null);

            RenderProfile profile = GetRenderProfile();
            _transientLines.Clear();

            //Win32.SystemMetrics sm = GEnv.SystemMetrics;
            //param.TargetRect = new Rectangle(sm.ControlBorderWidth+1, sm.ControlBorderHeight,
            //	this.Width - _VScrollBar.Width - sm.ControlBorderWidth + 8, //この８がない値が正当だが、.NETの文字サイズ丸め問題のため行の最終文字が表示されないことがある。これを回避するためにちょっと増やす
            //	this.Height - sm.ControlBorderHeight);
            Rectangle targetRect = this.ClientRectangle;

            float linePitch = profile.Pitch.Height + profile.LineSpacing;
            int lineFrom = (int)Math.Floor(Math.Max(clip.Top - BORDER, 0) / linePitch);
            int lineTo = (int)Math.Floor(Math.Max(clip.Bottom - BORDER, 0) / linePitch);
            int lineCount;
            if (lineFrom >= _maxDisplayLines) {
                lineCount = 0;
            }
            else {
                if (lineTo >= _maxDisplayLines) {
                    lineTo = _maxDisplayLines - 1;
                }
                lineCount = lineTo - lineFrom + 1;
            }

            //Debug.WriteLine(String.Format("{0} {1} ", param.LineFrom, param.LineCount));

            int topline_id = GetTopLine().ID;
            GLine l = document.FindLineOrNull(topline_id + lineFrom);
            if (l != null) {
                int poolIndex = 0;
                for (int i = 0; i < lineCount; i++) {
                    GLine cloned;
                    if (poolIndex < _glinePool.Count) {
                        cloned = _glinePool[poolIndex];
                        poolIndex++;
                        cloned.CopyFrom(l);
                    }
                    else {
                        cloned = l.Clone();
                        cloned.NextLine = cloned.PrevLine = null;
                        _glinePool.Add(cloned); // store for next use
                        poolIndex++;
                    }

                    _transientLines.Add(cloned);
                    l = l.NextLine;
                    if (l == null)
                        break;
                }
            }

            //以下、_transientLinesにはparam.LineFromから示される値が入っていることに注意

            //選択領域の描画
            if (!_textSelection.IsEmpty) {
                TextSelection.TextPoint from = _textSelection.HeadPoint;
                TextSelection.TextPoint to = _textSelection.TailPoint;
                l = document.FindLineOrNull(from.Line);
                GLine t = document.FindLineOrNull(to.Line);
                if (l != null && t != null) { //本当はlがnullではいけないはずだが、それを示唆するバグレポートがあったので念のため
                    t = t.NextLine;
                    int pos = from.Column; //たとえば左端を越えてドラッグしたときの選択範囲は前行末になるので pos==TerminalWidthとなるケースがある。
                    do {
                        int index = l.ID - (topline_id + lineFrom);
                        if (pos >= 0 && pos < l.DisplayLength && index >= 0 && index < _transientLines.Count) {
                            if (l.ID == to.Line) {
                                if (pos != to.Column) {
                                    _transientLines[index].SetSelection(pos, to.Column);
                                }
                            }
                            else {
                                _transientLines[index].SetSelection(pos, l.DisplayLength);
                            }
                        }
                        pos = 0; //２行目からの選択は行頭から
                        l = l.NextLine;
                    } while (l != t);
                }
            }

            AdjustCaret(_caret);
            _caret.Enabled = _caret.Enabled && (lineFrom <= _caret.Y && _caret.Y < lineFrom + lineCount);

            //Caret画面外にあるなら処理はしなくてよい。２番目の条件は、Attach-ResizeTerminalの流れの中でこのOnPaintを実行した場合にTerminalHeight>lines.Countになるケースがあるのを防止するため
            if (_caret.Enabled) {
                //ヒクヒク問題のため、キャレットを表示しないときでもこの操作は省けない
                if (_caret.Style == CaretType.Box) {
                    int y = _caret.Y - lineFrom;
                    if (y >= 0 && y < _transientLines.Count) {
                        _transientLines[y].SetCursor(_caret.X);
                    }
                }
            }

            param = new RenderParameter(
                lineFrom: lineFrom,
                lineCount: lineCount,
                targetRect: targetRect
            );
        }

        private void DrawLines(Graphics g, RenderParameter param, Color baseBackColor) {
            RenderProfile prof = GetRenderProfile();
            Caret caret = _caret;
            IntPtr hdc = g.GetHdc();
            try {
                float y = (prof.Pitch.Height + prof.LineSpacing) * param.LineFrom + BORDER;
                for (int i = 0; i < _transientLines.Count; i++) {
                    GLine line = _transientLines[i];
                    line.Render(hdc, prof, caret, baseBackColor, BORDER, (int)y);
                    if (line.IsPeriodicRedrawRequired()) {
                        _requiresPeriodicRedraw = true;
                    }
                    y += prof.Pitch.Height + prof.LineSpacing;
                }
            }
            finally {
                g.ReleaseHdc(hdc);
            }
        }

        private void DrawBarCaret(Graphics g, RenderParameter param, int x, int y) {
            RenderProfile profile = GetRenderProfile();
            PointF pt1 = new PointF(profile.Pitch.Width * x + BORDER, (profile.Pitch.Height + profile.LineSpacing) * y + BORDER + 2);
            PointF pt2 = new PointF(pt1.X, pt1.Y + profile.Pitch.Height - 2);
            Pen p = _caret.ToPen(profile);
            g.DrawLine(p, pt1, pt2);
            pt1.X += 1;
            pt2.X += 1;
            g.DrawLine(p, pt1, pt2);
        }
        private void DrawUnderLineCaret(Graphics g, RenderParameter param, int x, int y) {
            RenderProfile profile = GetRenderProfile();
            PointF pt1 = new PointF(profile.Pitch.Width * x + BORDER + 2, (profile.Pitch.Height + profile.LineSpacing) * y + BORDER + profile.Pitch.Height);
            PointF pt2 = new PointF(pt1.X + profile.Pitch.Width - 2, pt1.Y);
            Pen p = _caret.ToPen(profile);
            g.DrawLine(p, pt1, pt2);
            pt1.Y += 1;
            pt2.Y += 1;
            g.DrawLine(p, pt1, pt2);
        }

        private void DrawBackgroundImage(Graphics g, Image img, ImageStyle style, Rectangle clip) {
            if (style == ImageStyle.HorizontalFit) {
                this.DrawBackgroundImage_Scaled(g, img, clip, true, false);
            }
            else if (style == ImageStyle.VerticalFit) {
                this.DrawBackgroundImage_Scaled(g, img, clip, false, true);
            }
            else if (style == ImageStyle.Scaled) {
                this.DrawBackgroundImage_Scaled(g, img, clip, true, true);
            }
            else {
                DrawBackgroundImage_Normal(g, img, style, clip);
            }
        }
        private void DrawBackgroundImage_Scaled(Graphics g, Image img, Rectangle clip, bool fitWidth, bool fitHeight) {
            Size clientSize = this.ClientSize;
            PointF drawPoint;
            SizeF drawSize;

            if (fitWidth && fitHeight) {
                drawSize = new SizeF(clientSize.Width - _VScrollBar.Width, clientSize.Height);
                drawPoint = new PointF(0, 0);
            }
            else if (fitWidth) {
                float drawWidth = clientSize.Width - _VScrollBar.Width;
                float drawHeight = drawWidth * img.Height / img.Width;
                drawSize = new SizeF(drawWidth, drawHeight);
                drawPoint = new PointF(0, (clientSize.Height - drawSize.Height) / 2f);
            }
            else {
                float drawHeight = clientSize.Height;
                float drawWidth = drawHeight * img.Width / img.Height;
                drawSize = new SizeF(drawWidth, drawHeight);
                drawPoint = new PointF((clientSize.Width - _VScrollBar.Width - drawSize.Width) / 2f, 0);
            }

            Region oldClip = g.Clip;
            using (Region newClip = new Region(clip)) {
                g.Clip = newClip;
                g.DrawImage(img, new RectangleF(drawPoint, drawSize), new RectangleF(0, 0, img.Width, img.Height), GraphicsUnit.Pixel);
                g.Clip = oldClip;
            }
        }

        private void DrawBackgroundImage_Normal(Graphics g, Image img, ImageStyle style, Rectangle clip) {
            int offset_x, offset_y;
            if (style == ImageStyle.Center) {
                offset_x = (this.Width - _VScrollBar.Width - img.Width) / 2;
                offset_y = (this.Height - img.Height) / 2;
            }
            else {
                offset_x = (style == ImageStyle.TopLeft || style == ImageStyle.BottomLeft) ? 0 : (this.ClientSize.Width - _VScrollBar.Width - img.Width);
                offset_y = (style == ImageStyle.TopLeft || style == ImageStyle.TopRight) ? 0 : (this.ClientSize.Height - img.Height);
            }
            //if(offset_x < BORDER) offset_x = BORDER;
            //if(offset_y < BORDER) offset_y = BORDER;

            //画像内のコピー開始座標
            Rectangle target = Rectangle.Intersect(new Rectangle(clip.Left - offset_x, clip.Top - offset_y, clip.Width, clip.Height), new Rectangle(0, 0, img.Width, img.Height));
            if (target != Rectangle.Empty)
                g.DrawImage(img, new Rectangle(target.Left + offset_x, target.Top + offset_y, target.Width, target.Height), target, GraphicsUnit.Pixel);
        }

        //IPoderosaControl
        public Control AsControl() {
            return this;
        }

        //マウスホイールでのスクロール
        protected virtual void OnMouseWheelCore(MouseEventArgs e) {
            if (!this.HasDocument)
                return;

            int d = e.Delta / 120; //開発環境だとDeltaに120。これで1か-1が入るはず
            d *= 3; //可変にしてもいいかも

            int newval = _VScrollBar.Value - d;
            if (newval < 0)
                newval = 0;
            if (newval > _VScrollBar.Maximum - _VScrollBar.LargeChange)
                newval = _VScrollBar.Maximum - _VScrollBar.LargeChange + 1;
            _VScrollBar.Value = newval;
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);
            OnMouseWheelCore(e);
        }


        //SplitMark関係
        #region SplitMark.ISite
        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (_splitMark.IsSplitMarkVisible)
                _mouseHandlerManager.EndCapture();
            _splitMark.ClearMark();
        }

        public bool CanSplitWithSplitMark {
            get {
                if (!WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.EnableOldSplitterUI) {
                    return false;
                }

                IContentReplaceableView v = AsControlReplaceableView();
                return v == null ? false : GetSplittableViewManager().CanSplit(v);
            }
        }
        public int SplitClientWidth {
            get {
                return this.ClientSize.Width - (HasDocument ? _VScrollBar.Width : 0);
            }
        }
        public int SplitClientHeight {
            get {
                return this.ClientSize.Height;
            }
        }
        public void OverrideCursor(Cursor cursor) {
            this.Cursor = cursor;
        }
        public void RevertCursor() {
            this.Cursor = _uiCursor;
        }

        public void SplitVertically() {
            GetSplittableViewManager().SplitVertical(AsControlReplaceableView(), null);
        }
        public void SplitHorizontally() {
            GetSplittableViewManager().SplitHorizontal(AsControlReplaceableView(), null);
        }

        public SplitMarkSupport SplitMark {
            get {
                return _splitMark;
            }
        }

        #endregion

        private ISplittableViewManager GetSplittableViewManager() {
            IContentReplaceableView v = AsControlReplaceableView();
            if (v == null)
                return null;
            else
                return (ISplittableViewManager)v.ViewManager.GetAdapter(typeof(ISplittableViewManager));
        }
        private IContentReplaceableView AsControlReplaceableView() {
            IContentReplaceableViewSite site = (IContentReplaceableViewSite)this.GetAdapter(typeof(IContentReplaceableViewSite));
            return site == null ? null : site.CurrentContentReplaceableView;
        }

        #region ISelectionListener
        public void OnSelectionStarted() {
        }
        public void OnSelectionFixed() {
            if (WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.AutoCopyByLeftButton) {
                ICommandTarget ct = (ICommandTarget)this.GetAdapter(typeof(ICommandTarget));
                if (ct != null) {
                    CommandManagerPlugin cm = CommandManagerPlugin.Instance;
                    if (Control.ModifierKeys == Keys.Shift) { //CopyAsLook
                        //Debug.WriteLine("CopyAsLook");
                        cm.Execute(cm.Find("org.poderosa.terminalemulator.copyaslook"), ct);
                    }
                    else {
                        //Debug.WriteLine("NormalCopy");
                        IGeneralViewCommands gv = (IGeneralViewCommands)GetAdapter(typeof(IGeneralViewCommands));
                        if (gv != null)
                            cm.Execute(gv.Copy, ct);
                    }
                }
            }

        }
        #endregion

    }

    /*
     * 何行目から何行目までを描画すべきかの情報を収録
     */
    internal class RenderParameter {
        public readonly int LineFrom;
        public readonly int LineCount;
        public readonly Rectangle TargetRect;

        public RenderParameter(
            int lineFrom,
            int lineCount,
            Rectangle targetRect
        ) {
            this.LineFrom = lineFrom;
            this.LineCount = lineCount;
            this.TargetRect = targetRect;
        }
    }

    //テキスト選択のハンドラ
    internal class TextSelectionUIHandler : DefaultMouseHandler {
        private CharacterDocumentViewer _viewer;
        public TextSelectionUIHandler(CharacterDocumentViewer v)
            : base("textselection") {
            _viewer = v;
        }

        public override UIHandleResult OnMouseDown(MouseEventArgs args) {
            using (CharacterDocumentViewer.DocumentScope docScope = _viewer.GetDocumentScope()) {
                if (docScope.Document == null) {
                    return UIHandleResult.Pass;
                }

                if (args.Button != MouseButtons.Left || !_viewer.HasDocument) {
                    return UIHandleResult.Pass;
                }

                //テキスト選択ではないのでちょっと柄悪いが。UserControl->Controlの置き換えに伴う
                if (!_viewer.Focused) {
                    _viewer.Focus();
                }

                lock (docScope.Document) {
                    int col, row;
                    _viewer.MousePosToTextPos(args.X, args.Y, out col, out row);
                    int target_id = _viewer.GetTopLine().ID + row;
                    TextSelection sel = _viewer.TextSelection;
                    if (sel.State == SelectionState.Fixed)
                        sel.Clear(); //変なところでMouseDownしたとしてもClearだけはする
                    if (target_id <= docScope.Document.LastLineNumber) {
                        //if(InFreeSelectionMode) ExitFreeSelectionMode();
                        //if(InAutoSelectionMode) ExitAutoSelectionMode();
                        RangeType rt;
                        //Debug.WriteLine(String.Format("MouseDown {0} {1}", sel.State, sel.PivotType));

                        //同じ場所でポチポチと押すとChar->Word->Line->Charとモード変化する
                        if (sel.StartX != args.X || sel.StartY != args.Y)
                            rt = RangeType.Char;
                        else
                            rt = sel.PivotType == RangeType.Char ? RangeType.Word : sel.PivotType == RangeType.Word ? RangeType.Line : RangeType.Char;

                        //マウスを動かしていなくても、MouseDownとともにMouseMoveが来てしまうようだ
                        GLine tl = docScope.Document.FindLine(target_id);
                        if (tl.IsDoubleWidth) {
                            col /= 2;
                        }
                        sel.StartSelection(tl, col, rt, args.X, args.Y);
                    }
                }

                _viewer.InvalidateAll(); //NOTE 選択状態に変化のあった行のみ更新すればなおよし
                return UIHandleResult.Capture;
            }
        }

        public override UIHandleResult OnMouseMove(MouseEventArgs args) {
            using (CharacterDocumentViewer.DocumentScope docScope = _viewer.GetDocumentScope()) {
                if (docScope.Document == null) {
                    return UIHandleResult.Pass;
                }

                if (args.Button != MouseButtons.Left) {
                    return UIHandleResult.Pass;
                }

                TextSelection sel = _viewer.TextSelection;
                if (sel.State == SelectionState.Fixed || sel.State == SelectionState.Empty) {
                    return UIHandleResult.Pass;
                }

                //クリックだけでもなぜかMouseDownの直後にMouseMoveイベントが来るのでこのようにしてガード。でないと単発クリックでも選択状態になってしまう
                if (sel.StartX == args.X && sel.StartY == args.Y) {
                    return UIHandleResult.Capture;
                }

                lock (docScope.Document) {
                    int topline_id = _viewer.GetTopLine().ID;
                    SizeF pitch = _viewer.GetRenderProfile().Pitch;
                    int row, col;
                    _viewer.MousePosToTextPos_AllowNegative(args.X, args.Y, out col, out row);
                    int viewheight = (int)Math.Floor(_viewer.ClientSize.Height / pitch.Width);
                    int target_id = topline_id + row;

                    GLine target_line = docScope.Document.FindLineOrEdge(target_id);
                    if (target_line.IsDoubleWidth) {
                        col /= 2;
                    }
                    TextSelection.TextPoint point = sel.ConvertSelectionPosition(target_line, col);

                    point.Line = RuntimeUtil.AdjustIntRange(point.Line, docScope.Document.FirstLineNumber, docScope.Document.LastLineNumber);

                    if (_viewer.VScrollBar.Enabled) { //スクロール可能なときは
                        VScrollBar vsc = _viewer.VScrollBar;
                        if (target_id < topline_id) //前方スクロール
                            vsc.Value = point.Line - docScope.Document.FirstLineNumber;
                        else if (point.Line >= topline_id + vsc.LargeChange) { //後方スクロール
                            int newval = point.Line - docScope.Document.FirstLineNumber - vsc.LargeChange + 1;
                            if (newval < 0)
                                newval = 0;
                            if (newval > vsc.Maximum - vsc.LargeChange)
                                newval = vsc.Maximum - vsc.LargeChange + 1;
                            vsc.Value = newval;
                        }
                    }
                    else { //スクロール不可能なときは見えている範囲で
                        point.Line = RuntimeUtil.AdjustIntRange(point.Line, topline_id, topline_id + viewheight - 1);
                    } //ここさぼっている
                    //Debug.WriteLine(String.Format("MouseMove {0} {1} {2}", sel.State, sel.PivotType, args.X));
                    RangeType rt = sel.PivotType;
                    if ((Control.ModifierKeys & Keys.Control) != Keys.None)
                        rt = RangeType.Word;
                    else if ((Control.ModifierKeys & Keys.Shift) != Keys.None)
                        rt = RangeType.Line;

                    GLine tl = docScope.Document.FindLine(point.Line);
                    sel.ExpandTo(tl, point.Column, rt);
                }
            }

            _viewer.Invalidate(); //TODO 選択状態に変化のあった行のみ更新するようにすればなおよし
            return UIHandleResult.Capture;
        }

        public override UIHandleResult OnMouseUp(MouseEventArgs args) {
            TextSelection sel = _viewer.TextSelection;
            if (args.Button == MouseButtons.Left) {
                if (sel.State == SelectionState.Expansion || sel.State == SelectionState.Pivot)
                    sel.FixSelection();
                else
                    sel.Clear();
            }
            return _viewer.MouseHandlerManager.CapturingHandler == this ? UIHandleResult.EndCapture : UIHandleResult.Pass;
        }
    }

    //スプリットマークのハンドラ
    internal class SplitMarkUIHandler : DefaultMouseHandler {
        private SplitMarkSupport _splitMark;
        public SplitMarkUIHandler(SplitMarkSupport split)
            : base("splitmark") {
            _splitMark = split;
        }

        public override UIHandleResult OnMouseDown(MouseEventArgs args) {
            return UIHandleResult.Pass;
        }
        public override UIHandleResult OnMouseMove(MouseEventArgs args) {
            bool v = _splitMark.IsSplitMarkVisible;
            if (v || WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.ViewSplitModifier == Control.ModifierKeys)
                _splitMark.OnMouseMove(args);
            //直前にキャプチャーしていたらEndCapture
            return _splitMark.IsSplitMarkVisible ? UIHandleResult.Capture : v ? UIHandleResult.EndCapture : UIHandleResult.Pass;
        }
        public override UIHandleResult OnMouseUp(MouseEventArgs args) {
            bool visible = _splitMark.IsSplitMarkVisible;
            if (visible) {
                //例えば、マーク表示位置から選択したいような場合を考慮し、マーク上で右クリックすると選択が消えるようにする。
                _splitMark.OnMouseUp(args);
                return UIHandleResult.EndCapture;
            }
            else
                return UIHandleResult.Pass;
        }
    }

    /// <summary>
    /// CharacterDocumentViewer with CharacterDocument
    /// </summary>
    public abstract class SimpleCharacterDocumentViewer : CharacterDocumentViewer {
        private CharacterDocument _document = null;
        private readonly ReaderWriterLockSlim _documentLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        protected void DocumentChanged(CharacterDocument document) {
            _documentLock.EnterWriteLock();
            _document = document;
            DocumentChanged(document != null);
            _documentLock.ExitWriteLock();
        }

        public override bool HasDocument {
            get {
                return _document != null;
            }
        }

        public override DocumentScope GetDocumentScope() {
            _documentLock.EnterReadLock();
            return new DocumentScope(_document, _documentLock);
        }
    }
}
