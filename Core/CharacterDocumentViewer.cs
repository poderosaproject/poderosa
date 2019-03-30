// Copyright 2004-2019 The Poderosa Project.
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
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Poderosa.View {
    /// <summary>
    /// Viewer control to show a <see cref="ICharacterDocument"/>.
    /// </summary>
    /// <remarks>
    /// This class shows the text with attributes, a scrollbar, and a caret.
    /// Also, this class handles mouse input for the text-selection and the splitter.
    /// This class doesn't handle key input. It will be handled by the derived class on its need.
    /// </remarks>
    public abstract class CharacterDocumentViewer : Control, IPoderosaControl, ISelectionListener, SplitMarkSupport.ISite {
        // inner padding in pixels
        private const int BORDER = 2;
        // timer interval for the periodic redraw
        private const int TIMER_INTERVAL = 50;

        // vertical scrollbar
        private VScrollBar _VScrollBar;

        // text-selection manager
        private readonly TextSelection _textSelection;
        // splitter manager
        private readonly SplitMarkSupport _splitMark;
        // mouse handler manager
        private readonly MouseHandlerManager _mouseHandlerManager;
        // caret manager
        private readonly Caret _caret;

        // document
        private ICharacterDocument _document = null;

        // render-profile
        private RenderProfile _renderProfile;

        // timer for the periodic redraw
        private ITimerSite _timer = null;
        // timer tick counter for the periodic redraw
        private int _tickCount = 0;

        // a flag for preventing repeat of error reports
        private bool _errorRaisedInDrawing = false;
        // a flag which indicates that one or more lines in this view need the periodic redraw
        private bool _requiresPeriodicRedraw = false;

        // mouse pointer on the document (appears during a document is attached)
        private Cursor _documentCursor = Cursors.IBeam;

        // whether this view is scrollable
        private bool _scrollable = true;

        // size of the viewport
        private int _viewportRows = 0;
        private int _viewportColumns = 0;

        // Row ID of the first row at the top of this view
        private int _topRowID = 0;
        // Row ID of the first row in the document
        private int _docFirstRowID = 0;

        // temporal copy of lines
        private readonly GLineChunk _linePool = new GLineChunk(0);

#if ONPAINT_TIME_MEASUREMENT
        private Action<Stopwatch> _onPaintTimeObserver = null;
#endif

        /// <summary>
        /// Do extra work when the viewport size was changed.
        /// </summary>
        protected abstract void OnViewportSizeChanged();

        /// <summary>
        /// Do extra work when the document was attched or detached.
        /// </summary>
        protected abstract void OnCharacterDocumentChanged();

        /// <summary>
        /// Constructor
        /// </summary>
        protected CharacterDocumentViewer()
            : this(mouseHandler: null) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mouseHandler">primary mouse-handler, or null</param>
        protected CharacterDocumentViewer(IMouseHandler mouseHandler) {
            InitializeComponent();

            AdjustScrollBarPosition();

            this.DoubleBuffered = true;

            _caret = new Caret();

            _splitMark = new SplitMarkSupport(this, this) {
                Pen = new Pen(SystemColors.ControlDark) {
                    DashStyle = System.Drawing.Drawing2D.DashStyle.Dot
                }
            };

            _textSelection = new TextSelection(this);
            _textSelection.AddSelectionListener(this);

            _mouseHandlerManager = new MouseHandlerManager();
            if (mouseHandler != null) {
                _mouseHandlerManager.AddLastHandler(mouseHandler);
            }
            _mouseHandlerManager.AddLastHandler(new TextSelectionUIHandler(this));
            _mouseHandlerManager.AddLastHandler(new SplitMarkUIHandler(_splitMark));
            _mouseHandlerManager.AddLastHandler(new DefaultMouseWheelHandler(this));
            _mouseHandlerManager.AttachControl(this);

            // interim profile until SetRenderProfile() was called
            _renderProfile = new RenderProfile() {
                FontName = "Courier New",
                FontSize = 9,
                BackColor = SystemColors.Window,
                ForeColor = SystemColors.WindowText,
            };

            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        }

        private void InitializeComponent() {
            this.SuspendLayout();
            this._VScrollBar = new System.Windows.Forms.VScrollBar();
            // 
            // _VScrollBar
            // 
            this._VScrollBar.Enabled = false;
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
            this._VScrollBar.ValueChanged += _VScrollBar_ValueChanged;
            this.Controls.Add(_VScrollBar);

            this.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.ResumeLayout();
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);
            if (disposing) {
                _caret.Dispose();
                if (_timer != null) {
                    _timer.Close();
                }
                _splitMark.Pen.Dispose();
            }
        }

        /// <summary>
        /// A document attached to this view.
        /// Null if no document was attached.
        /// </summary>
        public ICharacterDocument CharacterDocument {
            get {
                return _document;
            }
        }

        /// <summary>
        /// Whether this view has an attached document.
        /// </summary>
        public bool HasDocument {
            get {
                return _document != null;
            }
        }

        /// <summary>
        /// Caret manager
        /// </summary>
        public Caret Caret {
            get {
                return _caret;
            }
        }

        /// <summary>
        /// Text-selection manager
        /// </summary>
        public ITextSelection Selection {
            get {
                return _textSelection;
            }
        }

        /// <summary>
        /// Number of rows in the viewport
        /// </summary>
        public int ViewportRows {
            get {
                return _viewportRows;
            }
        }

        /// <summary>
        /// Number of columns in the viewport
        /// </summary>
        public int ViewportColumns {
            get {
                return _viewportColumns;
            }
        }

        /// <summary>
        /// Set document.
        /// </summary>
        /// <param name="doc">document to set. can be null.</param>
        public void SetDocument(ICharacterDocument doc) {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)(() => SetDocument(doc)));
                return;
            }

            bool hasDocument = doc != null;

            _splitMark.Pen.Color = hasDocument ? SystemColors.ControlDark : SystemColors.Window;
            this.Cursor = hasDocument ? _documentCursor : Cursors.Default;
            this.ImeMode = hasDocument ? ImeMode.NoControl : ImeMode.Disable;

            // after a document was set, BackColor will be updated in OnPaint().
            // reset BackColor here if the document was detached.
            if (!hasDocument) {
                this.BackColor = SystemColors.ControlDark;
            }

            _document = doc;

            // setup timer
            if (_timer != null) {
                _timer.Close();
                _timer = null;
            }

            if (hasDocument) {
                _timer = WindowManagerPlugin.Instance.CreateTimer(TIMER_INTERVAL, new TimerDelegate(OnWindowManagerTimer));
                _tickCount = 0;
            }

            // clear selection
            _textSelection.Clear();

            // do extra work
            OnCharacterDocumentChanged();

            // request repaint
            if (hasDocument) {
                doc.InvalidatedRegion.InvalidatedAll = true;
                RefreshViewer();
                // make sacrolbar visible after UpdateScrollBar() was called
                this._VScrollBar.Visible = true;
            }
            else {
                this._VScrollBar.Visible = false;
                this.InvalidateFull();
            }
        }

        /// <summary>
        /// Sets a render profile
        /// </summary>
        /// <param name="prof">profile to set</param>
        public void SetRenderProfile(RenderProfile prof) {
            if (prof == null) {
                return;
            }
            _renderProfile = (RenderProfile)prof.Clone();
            UpdateViewportSize();
            RefreshViewerFull();
        }

        /// <summary>
        /// Sets whether this view is scrollable
        /// </summary>
        /// <param name="scrollable">true if this view is scrollable</param>
        protected void SetScrollable(bool scrollable) {
            _scrollable = scrollable;
            UpdateScrollBar();
        }

        /// <summary>
        /// Sets mouse pointer on the document.
        /// </summary>
        /// <param name="cursor"></param>
        protected void SetDocumentCursor(Cursor cursor) {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)(() => SetDocumentCursor(cursor)));
                return;
            }

            _documentCursor = cursor ?? Cursors.IBeam;

            if (_document != null) {
                this.Cursor = cursor;
            }
        }

        /// <summary>
        /// Resets mouse pointer on the document.
        /// </summary>
        protected void ResetDocumentCursor() {
            SetDocumentCursor(null);
        }

        /// <summary>
        /// Get row ID
        /// </summary>
        /// <param name="rowIndex">row index on the screen</param>
        /// <returns>row ID</returns>
        private int GetRowID(int rowIndex) {
            return _topRowID + rowIndex;
        }

        #region IPoderosaControl

        public Control AsControl() {
            return this;
        }

        #endregion

        #region IAdaptable

        public virtual IAdaptable GetAdapter(Type adapter) {
            return SessionManagerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion

        #region periodic repaint

        /// <summary>
        /// Timer event handler
        /// </summary>
        private void OnWindowManagerTimer() {
            int caretInterval = WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.CaretInterval;
            int caretIntervalTicks = Math.Max(1, caretInterval / TIMER_INTERVAL);
            _tickCount = (_tickCount + 1) % caretIntervalTicks;
            if (_tickCount == 0) {
                CaretTick();
            }
        }

        private void CaretTick() {
            ICharacterDocument doc = _document;
            if (doc != null) {
                // Note:
                //  Currently, blinking status of the caret is used also for displaying "blink" characters.
                //  So the blinking status of the caret have to be updated here even if the caret blinking was not enabled.
                _caret.Tick();
                if (_requiresPeriodicRedraw) {
                    _requiresPeriodicRedraw = false;
                    doc.InvalidatedRegion.InvalidatedAll = true;
                }
                else {
                    doc.InvalidatedRegion.InvalidateRow(_topRowID + _caret.Y);   // FIXME: Caret.Y sould be a Row ID, not a position on the screen
                }

                InvalidateRowsRegion();
            }
        }

        #endregion

        #region scrollbar

        /// <summary>
        /// Type of scroll action in AdjustScrollBar()
        /// </summary>
        protected enum ScrollAction {
            /// <summary>keep current position (row ID)</summary>
            KeepRowID,
            /// <summary>scroll to bottom</summary>
            ScrollToBottom,
        }

        /// <summary>
        /// Updates scrollbar properties with considering position of the viewport.
        /// </summary>
        /// <remarks>
        /// Changes are made in UI thread later.
        /// </remarks>
        protected void UpdateScrollBar() {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)UpdateScrollBar);
                return;
            }

            UpdateScrollBar(_scrollable ? ScrollAction.KeepRowID : ScrollAction.ScrollToBottom);
        }

        private void UpdateScrollBar(ScrollAction scrollAction) {
            var doc = _document;
            if (doc == null) {
                _VScrollBar.Enabled = false;
                _VScrollBar.Visible = false;
                return;
            }
            UpdateScrollBar(doc.GetRowIDSpan(), scrollAction);
        }

        /// <summary>
        /// Adjust settings of a vertical scrollbar to fit with size of the current document.
        /// </summary>
        /// <param name="docRowIDSpan">row span of the document</param>
        /// <param name="scrollAction">specify the next scroll position</param>
        private void UpdateScrollBar(RowIDSpan docRowIDSpan, ScrollAction scrollAction) {
            // at least one row is required for setup the scrololbar
            int screenLines = Math.Max(_viewportRows, 1);

            if (docRowIDSpan.Length <= screenLines) {
                _docFirstRowID = docRowIDSpan.Start;
                _topRowID = docRowIDSpan.Start;
                _VScrollBar.Enabled = false;
                return;
            }

            int oldTopRowID = _topRowID;

            // The size of the thumb of ScrollBar:
            //    => screenLines
            // Minimum of the ScrollBar.Value:
            //    => 0
            // Maxuimum of the ScrollBar.Value should be:
            //    => docRowIDSpan.Length - screenLines
            // Upper limits of the ScrollBar.Value is: ScrollBar.Maximum - ScrollBar.LargeChange + 1
            // So the ScrollBar.Maximum sould be:
            //    => (docRowIDSpan.Length - screenLines) + screenLines - 1
            //    => docRowIDSpan.Length - 1

            // fix current position
            switch (scrollAction) {
                case ScrollAction.ScrollToBottom:
                    _topRowID = Math.Max(docRowIDSpan.Start + docRowIDSpan.Length - screenLines, docRowIDSpan.Start);
                    break;

                case ScrollAction.KeepRowID:
                default:
                    _topRowID = Math.Max(Math.Min(_topRowID, docRowIDSpan.Start + docRowIDSpan.Length - screenLines), docRowIDSpan.Start);
                    break;
            }
            _docFirstRowID = docRowIDSpan.Start;

            _VScrollBar.Enabled = true;
            _VScrollBar.Maximum = docRowIDSpan.Length - 1;
            _VScrollBar.LargeChange = screenLines;
            _VScrollBar.Value = _topRowID - docRowIDSpan.Start;

            if (_topRowID != oldTopRowID) {
                // repaint all
                InvalidateFull();
            }
        }

        /// <summary>
        /// VScrollBar event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _VScrollBar_ValueChanged(object sender, EventArgs e) {
            _topRowID = _docFirstRowID + _VScrollBar.Value;
            // repaint all
            InvalidateFull();
        }

        /// <summary>
        /// Scroll specified number of rows
        /// </summary>
        /// <param name="rows">number of rows (increase of the row index. positive value causes scroll-up, negative value causes scroll-down.)</param>
        protected void ScrollDocument(int rows) {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)(() => ScrollDocument(rows)));
                return;
            }

            if (_VScrollBar.Visible && _VScrollBar.Enabled) {
                _VScrollBar.Value =
                    Math.Min(
                        Math.Max(_VScrollBar.Value + rows, 0),
                        _VScrollBar.Maximum - _VScrollBar.LargeChange + 1);
            }
        }

        /// <summary>
        /// Scrolls until the specified row is visible.
        /// </summary>
        /// <param name="rowID">target row ID</param>
        protected void ScrollToVisible(int rowID) {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)(() => ScrollToVisible(rowID)));
                return;
            }

            if (_VScrollBar.Visible && _VScrollBar.Enabled) {
                int newVal;
                if (rowID < _topRowID) {
                    newVal = rowID - _docFirstRowID;
                }
                else if (rowID - _topRowID >= _VScrollBar.LargeChange) {
                    newVal = rowID - _docFirstRowID - _VScrollBar.LargeChange + 1;
                }
                else {
                    return;
                }

                _VScrollBar.Value =
                    Math.Min(
                        Math.Max(newVal, 0),
                        _VScrollBar.Maximum - _VScrollBar.LargeChange + 1);
            }
        }

        // Set scrollbar's placement.
        // FIXME:
        //  Old comment says: When this.Dock was DockStyle.Top or DockStyle.Left, the ScrollBar doesn't move automatically.
        //  Is it true?
        private void AdjustScrollBarPosition() {
            _VScrollBar.Height = this.ClientSize.Height;
            _VScrollBar.Left = this.ClientSize.Width - _VScrollBar.Width;
        }

        #endregion

        #region refresh viewer

        /// <summary>
        /// Refresh viewer
        /// </summary>
        protected void RefreshViewer() {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)RefreshViewer);
                return;
            }

            UpdateScrollBar();
            InvalidateRowsRegion();
        }

        /// <summary>
        /// Refresh viewer
        /// </summary>
        protected void RefreshViewer(ScrollAction scrollAction) {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)(() => RefreshViewer(scrollAction)));
                return;
            }

            UpdateScrollBar(scrollAction);
            InvalidateRowsRegion();
        }

        /// <summary>
        /// Refresh viewer
        /// </summary>
        protected void RefreshViewerFull() {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)RefreshViewerFull);
                return;
            }

            UpdateScrollBar();
            InvalidateFull();
        }

        /// <summary>
        /// Refresh viewer
        /// </summary>
        protected void RefreshViewerFull(ScrollAction scrollAction) {
            if (this.InvokeRequired) {
                this.BeginInvoke((Action)(() => RefreshViewerFull(scrollAction)));
                return;
            }

            UpdateScrollBar(scrollAction);
            InvalidateFull();
        }

        /// <summary>
        /// Updates view port size
        /// </summary>
        private void UpdateViewportSize() {
            RenderProfile prof = _renderProfile;
            SizeF pitch = prof.Pitch;
            _viewportColumns = Math.Max((int)Math.Floor((this.ClientSize.Width - BORDER * 2) / pitch.Width), 0);
            _viewportRows = Math.Max((int)Math.Floor((this.ClientSize.Height - BORDER * 2 + prof.LineSpacing) / (pitch.Height + prof.LineSpacing)), 0);

            ICharacterDocument doc = _document;
            if (doc != null) {
                doc.VisibleAreaSizeChanged(_viewportRows, _viewportColumns);
            }
            OnViewportSizeChanged();
        }

        #endregion

        #region convert point

        /// <summary>
        /// Convert the point in the client coordinate to the character position.
        /// Negative position may be returned as it was.
        /// </summary>
        /// <param name="px">client coordinate position X in pixels</param>
        /// <param name="py">client coordinate position Y in pixels</param>
        /// <param name="colIndex">character position column index (may be negative value)</param>
        /// <param name="rowIndex">character position row index (may be negative value)</param>
        protected void ClientPosToTextPos(int px, int py, out int colIndex, out int rowIndex) {
            RenderProfile prof = _renderProfile;
            SizeF pitch = prof.Pitch;
            colIndex = (int)Math.Floor((px - CharacterDocumentViewer.BORDER) / pitch.Width);
            rowIndex = (int)Math.Floor((py - CharacterDocumentViewer.BORDER) / (pitch.Height + prof.LineSpacing));
        }

        #endregion

        #region invalidate region

        /// <summary>
        /// Invalidates region for painting rows that need to be repainted.
        /// </summary>
        private void InvalidateRowsRegion() {
            if (this.IsDisposed || this.Disposing) {
                return;
            }

            ICharacterDocument doc = _document;

            bool fullInvalidate;
            Rectangle r = new Rectangle();

            if (doc != null) {
                if (doc.InvalidatedRegion.IsEmpty) {
                    return;
                }
                InvalidatedRegion rgn = doc.InvalidatedRegion.GetCopyAndClear();
                if (rgn.IsEmpty) {
                    return;
                }
                if (rgn.InvalidatedAll) {
                    fullInvalidate = true;
                }
                else {
                    fullInvalidate = false;
                    r.X = 0;
                    r.Width = this.ClientSize.Width;
                    int topRowID = _topRowID;
                    int y1 = rgn.StartRowID - topRowID;
                    int y2 = rgn.EndRowID - topRowID;
                    RenderProfile prof = _renderProfile;
                    r.Y = BORDER + (int)(y1 * (prof.Pitch.Height + prof.LineSpacing));
                    r.Height = (int)((y2 - y1) * (prof.Pitch.Height + prof.LineSpacing)) + 1;
                }
            }
            else {
                fullInvalidate = true;
            }

            if (fullInvalidate) {
                Invalidate();   // Invalidate() can be called in the non-UI thread
            }
            else {
                Invalidate(r);  // Invalidate() can be called in the non-UI thread
            }
        }

        /// <summary>
        /// Invalidates full of the viewport
        /// </summary>
        private void InvalidateFull() {
            ICharacterDocument doc = _document;
            if (doc != null) {
                doc.InvalidatedRegion.Clear();
            }
            Invalidate();   // Invalidate() can be called in the non-UI thread
        }

        #endregion

        #region OnPaint time measurement

        public void SetOnPaintTimeObserver(Action<Stopwatch> observer) {
#if ONPAINT_TIME_MEASUREMENT
            _onPaintTimeObserver = observer;
#endif
        }

        #endregion

        #region OnPaint

        protected override sealed void OnPaint(PaintEventArgs e) {
#if ONPAINT_TIME_MEASUREMENT
            Stopwatch onPaintSw = (_onPaintTimeObserver != null) ? Stopwatch.StartNew() : null;
#endif

            base.OnPaint(e);

            ICharacterDocument doc = _document;

            try {
                if (!this.DesignMode) {
                    Rectangle clip = e.ClipRectangle;
                    Graphics g = e.Graphics;
                    RenderProfile profile = _renderProfile;

                    // determine background color of the view
                    Color backColor = (doc != null) ? doc.DetermineBackgroundColor(profile) : profile.BackColor;

                    if (this.BackColor != backColor) {
                        this.BackColor = backColor; // set background color of the view
                    }

                    // draw background image if it is required.
                    if (doc != null) {
                        Image img = doc.DetermineBackgroundImage(profile);
                        if (img != null) {
                            DrawBackgroundImage(g, img, profile.ImageStyle, clip);
                        }
                    }

                    if (doc != null) {
                        RenderParameter param;
                        lock (doc.SyncRoot) {   // synchronize the document during copying content
                            param = BuildTransientDocument(doc, profile, e);
                        }

                        DrawLines(g, param, backColor);

                        if (_caret.Style == CaretType.Line) {
                            if (_caret.Enabled && this.Focused && (!_caret.Blink || _caret.IsActiveTick)) {
                                DrawBarCaret(g, _caret.X, _caret.Y);
                            }
                        }
                        else if (_caret.Style == CaretType.Underline) {
                            if (_caret.Enabled && this.Focused && (!_caret.Blink || _caret.IsActiveTick)) {
                                DrawUnderLineCaret(g, _caret.X, _caret.Y);
                            }
                        }
                    }
                }

                _splitMark.OnPaint(e);
            }
            catch (Exception ex) {
                if (!_errorRaisedInDrawing) {   // prevents repeated error reports
                    _errorRaisedInDrawing = true;
                    RuntimeUtil.ReportException(ex);
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

        private RenderParameter BuildTransientDocument(ICharacterDocument doc, RenderProfile profile, PaintEventArgs e) {
            Rectangle clip = e.ClipRectangle;

            int maxRows = _viewportRows;

            int rowOffset1 = (int)Math.Floor((clip.Top - BORDER) / (profile.Pitch.Height + profile.LineSpacing));
            int rowOffset2 = (int)Math.Floor((clip.Bottom - BORDER) / (profile.Pitch.Height + profile.LineSpacing));

            if (rowOffset1 >= maxRows || rowOffset2 < 0) {
                return new RenderParameter(_linePool.Span(0, 0), 0);
            }
            rowOffset1 = Math.Max(Math.Min(rowOffset1, maxRows - 1), 0);
            rowOffset2 = Math.Max(Math.Min(rowOffset2, maxRows - 1), 0);

            int rowNum = rowOffset2 - rowOffset1 + 1;
            int startRowID = _topRowID + rowOffset1;
            //Debug.WriteLine(String.Format("{0} {1} ", rowIndex, rowNum));

            // clone rows
            _linePool.EnsureCapacity(rowNum);
            doc.ForEach(startRowID, rowNum,
                (rowID, line) => {
                    int arrayIndex = rowID - startRowID;
                    GLine srcLine = (line != null) ? line : new GLine(1);
                    GLine destLine = _linePool.Array[arrayIndex];
                    if (destLine == null) {
                        _linePool.Array[arrayIndex] = destLine = new GLine(1);
                    }
                    if (line != null) {
                        destLine.CopyFrom(line);
                    }
                    else {
                        destLine.Clear();
                    }
                }
            );

            // set selection to the cloned GLines
            TextSelection.Region? selRegion = _textSelection.GetRegion();
            if (selRegion.HasValue) {
                RowIDSpan drawRowsSpan = new RowIDSpan(startRowID, rowNum);
                RowIDSpan selRowsSpan = new RowIDSpan(selRegion.Value.StartRowID, selRegion.Value.EndRowID - selRegion.Value.StartRowID + 1);
                RowIDSpan drawSelRowsSpan = drawRowsSpan.Intersect(selRowsSpan);

                for (int i = 0; i < drawSelRowsSpan.Length; i++) {
                    int rowID = drawSelRowsSpan.Start + i;
                    GLine l = _linePool.Array[rowID - startRowID];
                    int colFrom = (rowID == selRegion.Value.StartRowID) ? selRegion.Value.StartPos : 0;
                    int colTo = (rowID == selRegion.Value.EndRowID && selRegion.Value.EndPos.HasValue) ? selRegion.Value.EndPos.Value : l.DisplayLength;
                    l.SetSelection(colFrom, colTo);
                }
            }

            if (_caret.Enabled && _caret.Style == CaretType.Box) {
                if (_caret.Y >= rowOffset1 && _caret.Y <= rowOffset2) {
                    _linePool.Array[_caret.Y - rowOffset1].SetCursor(_caret.X);
                }
            }

            return new RenderParameter(_linePool.Span(0, rowNum), rowOffset1);
        }

        private void DrawLines(Graphics g, RenderParameter param, Color baseBackColor) {
            RenderProfile prof = _renderProfile;
            Caret caret = _caret;
            //Rendering Core
            IntPtr hdc = g.GetHdc();
            try {
                float y = (prof.Pitch.Height + prof.LineSpacing) * param.RowIndex + BORDER;
                int lineNum = param.GLines.Length;
                for (int i = 0; i < lineNum; i++) {
                    GLine line = param.GLines.Array[param.GLines.Offset + i];
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

        private void DrawBarCaret(Graphics g, int x, int y) {
            RenderProfile profile = _renderProfile;
            PointF pt1 = new PointF(profile.Pitch.Width * x + BORDER, (profile.Pitch.Height + profile.LineSpacing) * y + BORDER + 2);
            PointF pt2 = new PointF(pt1.X, pt1.Y + profile.Pitch.Height - 2);
            Pen p = _caret.ToPen(profile);
            g.DrawLine(p, pt1, pt2);
            pt1.X += 1;
            pt2.X += 1;
            g.DrawLine(p, pt1, pt2);
        }

        private void DrawUnderLineCaret(Graphics g, int x, int y) {
            RenderProfile profile = _renderProfile;
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

            Rectangle target = Rectangle.Intersect(new Rectangle(clip.Left - offset_x, clip.Top - offset_y, clip.Width, clip.Height), new Rectangle(0, 0, img.Width, img.Height));
            if (target != Rectangle.Empty) {
                g.DrawImage(img, new Rectangle(target.Left + offset_x, target.Top + offset_y, target.Width, target.Height), target, GraphicsUnit.Pixel);
            }
        }

        #endregion

        #region OnResize

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            if (_VScrollBar.Visible) {
                AdjustScrollBarPosition();
            }
            UpdateViewportSize();
            UpdateScrollBar();
            // repaint whole viewport is needed for erasing padding area
            InvalidateFull();
        }

        #endregion

        #region SplitMark.ISite

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            if (_splitMark.IsSplitMarkVisible) {
                _mouseHandlerManager.EndCapture();
            }
            _splitMark.ClearMark();
        }

        public bool CanSplit {
            get {
                IContentReplaceableView v = AsControlReplaceableView();
                return v == null ? false : GetSplittableViewManager().CanSplit(v);
            }
        }

        public int SplitClientWidth {
            get {
                return this.ClientSize.Width - (_VScrollBar.Visible ? _VScrollBar.Width : 0);
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
            this.Cursor = _documentCursor;
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

        private ISplittableViewManager GetSplittableViewManager() {
            IContentReplaceableView v = AsControlReplaceableView();
            return (v == null) ? null : (ISplittableViewManager)v.ViewManager.GetAdapter(typeof(ISplittableViewManager));
        }

        private IContentReplaceableView AsControlReplaceableView() {
            IContentReplaceableViewSite site = (IContentReplaceableViewSite)this.GetAdapter(typeof(IContentReplaceableViewSite));
            return site == null ? null : site.CurrentContentReplaceableView;
        }

        #endregion

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
                        if (gv != null) {
                            cm.Execute(gv.Copy, ct);
                        }
                    }
                }
            }
        }

        #endregion

        #region RenderParameter

        /// <summary>
        /// Parameters for internal use.
        /// </summary>
        private class RenderParameter {
            /// <summary>
            /// GLines to draw
            /// </summary>
            public GLineChunkSpan GLines {
                get;
                private set;
            }

            /// <summary>
            /// Row index on the screen
            /// </summary>
            public int RowIndex {
                get;
                private set;
            }

            /// <summary>
            /// Constructor;
            /// </summary>
            /// <param name="glines">GLines to draw</param>
            /// <param name="rowIndex">row index on the screen</param>
            public RenderParameter(GLineChunkSpan glines, int rowIndex) {
                this.GLines = glines;
                this.RowIndex = rowIndex;
            }
        }

        #endregion

        #region DefaultMouseWheelHandler

        /// <summary>
        /// Default mouse wheel handler.
        /// </summary>
        private class DefaultMouseWheelHandler : DefaultMouseHandler {
            private readonly CharacterDocumentViewer _viewer;

            public DefaultMouseWheelHandler(CharacterDocumentViewer viewer)
                : base("defaultmousewheel") {
                _viewer = viewer;
            }

            public override UIHandleResult OnMouseWheel(MouseEventArgs args) {
                const int WHEEL_DELTA = 120;
                const int ROWS_PER_NOTCH = 3;
                int scroll = (args.Delta / WHEEL_DELTA) * ROWS_PER_NOTCH;

                _viewer.ScrollDocument(-scroll);

                return UIHandleResult.Stop;
            }
        }

        #endregion

        #region TextSelectionUIHandler

        /// <summary>
        /// Mouse handler for the text selection.
        /// </summary>
        private class TextSelectionUIHandler : DefaultMouseHandler {

            private readonly CharacterDocumentViewer _viewer;

            // previous mouse position
            private int _prevMouseX;
            private int _prevMouseY;

            public TextSelectionUIHandler(CharacterDocumentViewer v)
                : base("textselection") {
                _viewer = v;
            }

            public override UIHandleResult OnMouseDown(MouseEventArgs args) {
                if (args.Button != MouseButtons.Left) {
                    return UIHandleResult.Pass;
                }

                TextSelection sel = _viewer._textSelection;

                if (!sel.CanHandleMouseDown) {
                    return UIHandleResult.Pass;
                }

                if (!_viewer.Focused) {
                    _viewer.Focus();
                }

                TextSelection.Mode? selModeOverride =
                    ((Control.ModifierKeys & Keys.Control) != Keys.None) ? TextSelection.Mode.Word :
                    ((Control.ModifierKeys & Keys.Shift) != Keys.None) ? TextSelection.Mode.Line :
                    (TextSelection.Mode?)null;

                ICharacterDocument doc = _viewer._document;
                if (doc != null) {
                    lock (doc.SyncRoot) {
                        int col, row;
                        _viewer.ClientPosToTextPos(args.X, args.Y, out col, out row);
                        if (row < 0) {
                            sel.Clear();
                        }
                        else {
                            col = Math.Max(col, 0);
                            int targetRowID = _viewer.GetRowID(row);
                            doc.Apply(targetRowID, line => {
                                if (line != null) {
                                    sel.OnMouseDown(targetRowID, line, col, selModeOverride, args.X, args.Y);
                                }
                                else {
                                    sel.Clear();
                                }
                            });
                        }
                    }
                }

                _prevMouseX = args.X;
                _prevMouseY = args.Y;

                // we need a full repaint because the old selection may be cleared
                _viewer.Invalidate();

                return UIHandleResult.Capture;
            }

            public override UIHandleResult OnMouseMove(MouseEventArgs args) {
                if (args.Button != MouseButtons.Left) {
                    return UIHandleResult.Pass;
                }

                TextSelection sel = _viewer._textSelection;

                if (!sel.CanHandleMouseMove) {
                    return UIHandleResult.Pass;
                }

                if (args.X == _prevMouseX && args.Y == _prevMouseY) {
                    return UIHandleResult.Capture;
                }

                TextSelection.Mode? selModeOverride =
                    ((Control.ModifierKeys & Keys.Control) != Keys.None) ? TextSelection.Mode.Word :
                    ((Control.ModifierKeys & Keys.Shift) != Keys.None) ? TextSelection.Mode.Line :
                    (TextSelection.Mode?)null;

                ICharacterDocument doc = _viewer._document;
                if (doc != null) {
                    lock (doc.SyncRoot) {
                        int row, col;
                        _viewer.ClientPosToTextPos(args.X, args.Y, out col, out row);
                        int targetRowID = _viewer.GetRowID(row);
                        RowIDSpan rawIDSpan = doc.GetRowIDSpan();
                        if (targetRowID < rawIDSpan.Start) {
                            targetRowID = rawIDSpan.Start;
                            col = 0;
                        }
                        else {
                            int lastRowID = rawIDSpan.Start + rawIDSpan.Length - 1;
                            if (targetRowID > lastRowID) {
                                targetRowID = lastRowID;
                                col = Int32.MaxValue;   // fix later
                            }
                        }

                        doc.Apply(targetRowID, line => {
                            if (line != null) {
                                int fixedCol = (col == Int32.MaxValue) ? line.DisplayLength : col;
                                sel.OnMouseMove(targetRowID, line, fixedCol, selModeOverride);
                            }
                        });

                        _viewer.ScrollToVisible(targetRowID);
                    }
                }

                _prevMouseX = args.X;
                _prevMouseY = args.Y;

                _viewer.Invalidate();

                return UIHandleResult.Capture;
            }

            public override UIHandleResult OnMouseUp(MouseEventArgs args) {
                if (args.Button == MouseButtons.Left) {
                    TextSelection sel = _viewer._textSelection;

                    if (sel.CanHandleMouseUp) {
                        sel.OnMouseUp();
                    }
                }

                return _viewer._mouseHandlerManager.CapturingHandler == this ?
                        UIHandleResult.EndCapture : UIHandleResult.Pass;
            }
        }

        #endregion

        #region SplitMarkUIHandler

        /// <summary>
        /// Mouse handler for the splitter
        /// </summary>
        private class SplitMarkUIHandler : DefaultMouseHandler {

            private readonly SplitMarkSupport _splitMark;

            public SplitMarkUIHandler(SplitMarkSupport split)
                : base("splitmark") {
                _splitMark = split;
            }

            public override UIHandleResult OnMouseDown(MouseEventArgs args) {
                return UIHandleResult.Pass;
            }

            public override UIHandleResult OnMouseMove(MouseEventArgs args) {
                bool isSplitMarkVisible = _splitMark.IsSplitMarkVisible;
                if (isSplitMarkVisible || WindowManagerPlugin.Instance.WindowPreference.OriginalPreference.ViewSplitModifier == Control.ModifierKeys) {
                    _splitMark.OnMouseMove(args);
                }
                return _splitMark.IsSplitMarkVisible ? UIHandleResult.Capture : isSplitMarkVisible ? UIHandleResult.EndCapture : UIHandleResult.Pass;
            }

            public override UIHandleResult OnMouseUp(MouseEventArgs args) {
                bool isSplitMarkVisible = _splitMark.IsSplitMarkVisible;
                if (isSplitMarkVisible) {
                    _splitMark.OnMouseUp(args);
                    return UIHandleResult.EndCapture;
                }
                return UIHandleResult.Pass;
            }
        }

        #endregion
    }

}
