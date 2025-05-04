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
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

using Poderosa.Document;
using Poderosa.Commands;

namespace Poderosa.Terminal {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class TerminalDocument : CharacterDocument {

        private const int MARGIN_DEFAULT = -1;

        private sealed class ScreenGeometry {
            private readonly int _width;
            private readonly int _height;
            private readonly int _topMarginOffset;
            private readonly int _bottomMarginOffset;
            private readonly int _leftMarginOffset;
            private readonly int _rightMarginOffset;
            private readonly int _actualTopMarginOffset;
            private readonly int _actualBottomMarginOffset;
            private readonly int _actualLeftMarginOffset;
            private readonly int _actualRightMarginOffset;

            public ScreenGeometry(
                int width,
                int height,
                int topMarginOffset,
                int bottomMarginOffset,
                int leftMarginOffset,
                int rightMarginOffset
            ) {
                _width = width;
                _height = height;
                _topMarginOffset = topMarginOffset;
                _bottomMarginOffset = bottomMarginOffset;
                _leftMarginOffset = leftMarginOffset;
                _rightMarginOffset = rightMarginOffset;
                _actualTopMarginOffset = (height < 1) ? 0 : (topMarginOffset < 0) ? 0 : Math.Min(topMarginOffset, height - 1);
                _actualBottomMarginOffset = (height < 1) ? 0 : (bottomMarginOffset < 0) ? height - 1 : Math.Min(bottomMarginOffset, height - 1);
                _actualLeftMarginOffset = (width < 1) ? 0 : (leftMarginOffset < 0) ? 0 : Math.Min(leftMarginOffset, width - 1);
                _actualRightMarginOffset = (width < 1) ? 0 : (rightMarginOffset < 0) ? width - 1 : Math.Min(rightMarginOffset, width - 1);
            }

            public int Width {
                get {
                    return _width;
                }
            }

            public int Height {
                get {
                    return _height;
                }
            }

            public int TopMarginOffset {
                get {
                    return _actualTopMarginOffset;
                }
            }

            public int BottomMarginOffset {
                get {
                    return _actualBottomMarginOffset;
                }
            }

            public int LeftMarginOffset {
                get {
                    return _actualLeftMarginOffset;
                }
            }

            public int RightMarginOffset {
                get {
                    return _actualRightMarginOffset;
                }
            }

            public bool HasTopMargin {
                get {
                    return _topMarginOffset >= 0;
                }
            }

            public bool HasBottomMargin {
                get {
                    return _bottomMarginOffset >= 0;
                }
            }

            public bool HasLeftMargin {
                get {
                    return _leftMarginOffset >= 0;
                }
            }

            public bool HasRightMargin {
                get {
                    return _rightMarginOffset >= 0;
                }
            }

            public ScreenGeometry ChangeSize(int width, int height) {
                return new ScreenGeometry(
                    width: width,
                    height: height,
                    topMarginOffset: _topMarginOffset,
                    bottomMarginOffset: _bottomMarginOffset,
                    leftMarginOffset: _leftMarginOffset,
                    rightMarginOffset: _rightMarginOffset
                );
            }

            public ScreenGeometry ChangeVerticalMargins(int topMarginOffset, int bottomMarginOffset) {
                return new ScreenGeometry(
                    width: _width,
                    height: _height,
                    topMarginOffset: topMarginOffset,
                    bottomMarginOffset: bottomMarginOffset,
                    leftMarginOffset: _leftMarginOffset,
                    rightMarginOffset: _rightMarginOffset
                );
            }

            public ScreenGeometry ChangeHorizontalMargins(int leftMarginOffset, int rightMarginOffset) {
                return new ScreenGeometry(
                    width: _width,
                    height: _height,
                    topMarginOffset: _topMarginOffset,
                    bottomMarginOffset: _bottomMarginOffset,
                    leftMarginOffset: leftMarginOffset,
                    rightMarginOffset: rightMarginOffset
                );
            }

            public ScreenGeometry ClearVerticalMargins() {
                return new ScreenGeometry(
                    width: _width,
                    height: _height,
                    topMarginOffset: MARGIN_DEFAULT,
                    bottomMarginOffset: MARGIN_DEFAULT,
                    leftMarginOffset: _leftMarginOffset,
                    rightMarginOffset: _rightMarginOffset
                );
            }

            public ScreenGeometry ClearHorizontalMargins() {
                return new ScreenGeometry(
                    width: _width,
                    height: _height,
                    topMarginOffset: _topMarginOffset,
                    bottomMarginOffset: _bottomMarginOffset,
                    leftMarginOffset: MARGIN_DEFAULT,
                    rightMarginOffset: MARGIN_DEFAULT
                );
            }

            public ScreenGeometry ClearMargins() {
                return new ScreenGeometry(
                    width: _width,
                    height: _height,
                    topMarginOffset: MARGIN_DEFAULT,
                    bottomMarginOffset: MARGIN_DEFAULT,
                    leftMarginOffset: MARGIN_DEFAULT,
                    rightMarginOffset: MARGIN_DEFAULT
                );
            }
        }

        private readonly Sixel.SixelImageManager _sixelImageManager = new Sixel.SixelImageManager();
        private readonly LogService _logServide;
        private TextDecoration _currentDecoration = TextDecoration.Default;
        private int _caretColumn;
        private bool _wrapPending;
        private ScreenGeometry _geom;
        //ウィンドウの表示用テキスト
        private GLine _topLine; // top of the screen
        private GLine _viewTopLine; // top of the view
        private GLine _currentLine;

        internal TerminalDocument(int width, int height)
            : this(width, height, new LogService()) {
        }

        internal TerminalDocument(int width, int height, LogService logService) {
            _logServide = logService;
            _geom = new ScreenGeometry(
                    width: width,
                    height: height,
                    topMarginOffset: MARGIN_DEFAULT,
                    bottomMarginOffset: MARGIN_DEFAULT,
                    leftMarginOffset: MARGIN_DEFAULT,
                    rightMarginOffset: MARGIN_DEFAULT
                );
            ForceNewLine = false;
            ShowCaret = true;
            UICursor = null;
            KeySendLocked = false;
            Clear();
        }

        public int TerminalHeight {
            get {
                return _geom.Height;
            }
        }

        public int TerminalWidth {
            get {
                return _geom.Width;
            }
        }

        public override IPoderosaMenuGroup[] ContextMenu {
            get {
                return TerminalEmulatorPlugin.Instance.DocumentContextMenu;
            }
        }

        public TextDecoration CurrentDecoration {
            get {
                return _currentDecoration;
            }
            set {
                _currentDecoration = value;
            }
        }

        internal Sixel.SixelImageManager SixelImageManager {
            get {
                return _sixelImageManager;
            }
        }

        #region for TerminalCntrol
        // These properties are used in TerminalControl, but must be managed along with document.

        public bool ForceNewLine {
            get;
            set;
        }

        public bool ShowCaret {
            get;
            set;
        }

        public Cursor UICursor {
            get;
            set;
        }

        public bool KeySendLocked {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Set vertical margins
        /// </summary>
        /// <param name="topOffset">top of scrolling region specified by offset from the first line of the display area. -1 represents the top of the display area.</param>
        /// <param name="bottomOffset">bottom of scrolling region specified by offset from the first line of the display area. -1 represents the bottom of the display area.</param>
        public void SetVerticalMargins(int topOffset, int bottomOffset) {
            _geom = _geom.ChangeVerticalMargins(topOffset, bottomOffset);
        }

        /// <summary>
        /// Set scrolling region
        /// </summary>
        /// <param name="leftOffset">left edge of scrolling region specified by offset from the first column of the display area. -1 represents the left-most column of the display area.</param>
        /// <param name="rightOffset">right edge of scrolling region specified by offset from the first column of the display area. -1 represents the right-most of the display area.</param>
        public void SetHorizontalMargins(int leftOffset, int rightOffset) {
            _geom = _geom.ChangeHorizontalMargins(leftOffset, rightOffset);
        }

        public void Clear() {
            this.CaretColumn = 0;
            _firstLine = null;
            _lastLine = null;
            _size = 0;
            _sixelImageManager.DeleteAll();
            AddLine(CreateErasedGLine());
            InvalidateAll();
        }

        public void SetSubCaption(string caption) {
            _subCaption = caption;
        }

        public void ClearSubCaption() {
            _subCaption = null;
        }

        public void Resize(int width, int height) {
            _geom = _geom.ChangeSize(width, height);
            Debug.WriteLine("{2} Resize: cols {0} x rows {1}", width, height, Caption);
        }

        public void ClearMargins() {
            _geom = _geom.ClearMargins();
        }

        public void ClearHorizontalMargins() {
            _geom = _geom.ClearHorizontalMargins();
        }

        public void ClearVerticalMargins() {
            _geom = _geom.ClearVerticalMargins();
        }

        public int CaretColumn {
            get {
                return _caretColumn;
            }
            set {
                // updating caret position always reset wrap-pending state
                _caretColumn = value;
                _wrapPending = false;
            }
        }

        public bool WrapPending {
            get {
                return _wrapPending;
            }
            set {
                _wrapPending = true;
            }
        }

        public GLine CurrentLine {
            get {
                return _currentLine;
            }
        }

        public GLine ViewTopLine {
            get {
                return _viewTopLine;
            }
        }

        public int ViewTopLineNumber {
            get {
                return _viewTopLine.ID;
            }
        }

        public void SetViewTopLineNumber(int value) {
            int prevID = _viewTopLine.ID;
            _viewTopLine = FindLineOrEdge(value);
            if (_viewTopLine.ID != prevID) {
                _invalidatedRegion.InvalidatedAll = true;
            }
        }

        public void ResetViewTop() {
            if (_viewTopLine != _topLine) {
                _viewTopLine = _topLine;
                _invalidatedRegion.InvalidatedAll = true;
            }
        }

        public GLine TopLine {
            get {
                return _topLine;
            }
        }

        public int TopLineNumber {
            get {
                return _topLine.ID;
            }
        }

        public void SetTopLineNumber(int value) {
            int prevID = _topLine.ID;
            SetTopLine(FindLineOrEdge(value));
            if (_topLine.ID != prevID) {
                _invalidatedRegion.InvalidatedAll = true;
            }
        }

        private void SetTopLine(GLine line) {
            if (line != _topLine) {
                _viewTopLine = _topLine = line;
            }
        }

        public void EnsureLine(int id) {
            while (id > _lastLine.ID) {
                AddLine(CreateErasedGLine());
            }
        }

        public int CurrentLineNumber {
            get {
                return _currentLine.ID;
            }
            set {
                value = Math.Min(Math.Max(value, FirstLineNumber), LastLineNumber + TerminalHeight);
                while (_lastLine.ID < value) {
                    AddLine(CreateErasedGLine());
                }

                _currentLine = FindLineOrEdge(value); //外部から変な値が渡されたり、あるいはどこかにバグがあるせいでこの中でクラッシュすることがまれにあるようだ。なのでOrEdgeバージョンにしてクラッシュは回避
            }
        }

        public bool CurrentIsLast {
            get {
                return _currentLine.NextLine == null;
            }
        }
        #region part of IPoderosaDocument
        public override Image Icon {
            get {
                return _owner.Icon;
            }
        }
        public override string Caption {
            get {
                return _owner.Caption;
            }
        }
        #endregion

        public int TopMarginOffset {
            get {
                return _geom.TopMarginOffset;
            }
        }

        public int BottomMarginOffset {
            get {
                return _geom.BottomMarginOffset;
            }
        }

        public int LeftMarginOffset {
            get {
                return _geom.LeftMarginOffset;
            }
        }

        public int RightMarginOffset {
            get {
                return _geom.RightMarginOffset;
            }
        }

        public int ScrollingTopLineNumber {
            get {
                return TopLineNumber + TopMarginOffset;
            }
        }

        public int ScrollingBottomLineNumber {
            get {
                return TopLineNumber + BottomMarginOffset;
            }
        }

        public bool IsCurrentLineInScrollingRegion {
            get {
                return CurrentLineNumber >= ScrollingTopLineNumber && CurrentLineNumber <= ScrollingBottomLineNumber;
            }
        }

        public bool IsCaretColumnInScrollingRegion {
            get {
                return CaretColumn >= LeftMarginOffset && CaretColumn <= RightMarginOffset;
            }
        }

        public bool HasTopMargin {
            get {
                return _geom.HasTopMargin;
            }
        }

        public bool HasBottomMargin {
            get {
                return _geom.HasBottomMargin;
            }
        }

        public bool HasLeftMargin {
            get {
                return _geom.HasLeftMargin;
            }
        }

        public bool HasRightMargin {
            get {
                return _geom.HasRightMargin;
            }
        }

        /// <summary>
        /// Line feed considering scrolling region.
        /// </summary>
        internal void LineFeed() {
            // If scrolling region was specified, scroll the region as needed, but the cursor will not go outside of the current view.
            // If scrolling region was not specified, the view will be moved as needed.
            if (HasBottomMargin) {
                if (CurrentLineNumber == ScrollingBottomLineNumber) {
                    ScrollDownRegion(1);
                }
                else if (CurrentLineNumber < TopLineNumber + TerminalHeight - 1) {
                    CurrentLineNumber++; // move to the next line. new line will be added as needed.
                }
            }
            else {
                CurrentLineNumber++; // move to the next line. new line will be added as needed.
                int overflow = CurrentLineNumber - (TopLineNumber + TerminalHeight - 1);
                if (overflow > 0) {
                    SetTopLineNumber(TopLineNumber + overflow);
                }
            }
        }

        /// <summary>
        /// Reverse line feed considering scrolling region.
        /// </summary>
        internal void ReverseLineFeed() {
            // Regardless whether scrolling region was specified or not, scroll as needed, but the cursor will not go outside of the current view.
            if (CurrentLineNumber == ScrollingTopLineNumber) {
                ScrollUpRegion(1);
            }
            else if (CurrentLineNumber > TopLineNumber) {
                CurrentLineNumber--;
            }
        }

        /// <summary>
        /// <para>Insert N lines at the top of the specified region and remove overflow lines from the bottom.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="from">line number at the top of the region. (inclusive)</param>
        /// <param name="to">line number at the bottom of the region. (inclusive)</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollUpLines(int from, int to, int n) {
            if (to < from || n < 1) {
                return;
            }

            GLine top = FindLineOrEdge(from);
            if (top == null || top.ID < from || top.ID > to) {
                return;
            }
            int origTopID = top.ID;

            GLine bottom = FindLineOrEdge(to);

            int savedLineNumber = CurrentLineNumber;

            int linesToInsert = Math.Min(n, to - from + 1);
            int linesToRemove = Math.Max(bottom.ID + linesToInsert - to, 0);

            // insert new lines
            GLine firstNewLine = CreateErasedGLine();
            InsertBefore(top, firstNewLine);
            if (top == TopLine) {
                SetTopLine(firstNewLine);
            }

            for (int i = 1; i < linesToInsert; i++) {
                GLine newLine = CreateErasedGLine();
                InsertBefore(top, newLine);
            }

            // remove overflow lines
            GLine l = bottom;
            for (int i = 0; l != null && i < linesToRemove; i++) {
                GLine prev = l.PrevLine;
                Remove(l);
                l = prev;
            }

            // relabel IDs
            l = firstNewLine;
            int id = origTopID;
            while (l != null && id <= to) {
                l.ID = id++;
                l = l.NextLine;
            }

            CurrentLineNumber = savedLineNumber;

            _sixelImageManager.MoveDown(from, to, n);

            InvalidateAll();

#if DEBUG
            {
                GLine ll = (firstNewLine.PrevLine != null) ? firstNewLine.PrevLine : firstNewLine;
                int nextID = ll.ID;
                for (; ll != null; ll = ll.NextLine, nextID++) {
                    if (ll.ID != nextID) {
                        Debug.WriteLine("*** ScrollUp: BAD ID ***");
                    }
                }
            }
#endif
        }

        /// <summary>
        /// <para>Insert N lines at the bottom of the scroll region and remove overflow lines from the top.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="from">line number at the top of the region. (inclusive)</param>
        /// <param name="to">line number at the bottom of the region. (inclusive)</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollDownLines(int from, int to, int n) {
            if (to < from || n < 1) {
                return;
            }

            GLine top = FindLineOrEdge(from);
            if (top == null || top.ID < from || top.ID > to) {
                return;
            }

            GLine bottom = FindLineOrEdge(to);
            int origBottomID = bottom.ID;

            int savedLineNumber = CurrentLineNumber;

            int linesToInsert = Math.Min(n, to - from + 1);
            int linesToRemove = Math.Max(from - (top.ID - linesToInsert), 0);

            // insert new lines
            GLine firstNewLine = CreateErasedGLine();
            InsertAfter(bottom, firstNewLine);

            for (int i = 1; i < linesToInsert; i++) {
                GLine newLine = CreateErasedGLine();
                InsertAfter(bottom, newLine);
            }

            // remove overflow lines
            GLine l = top;
            for (int i = 0; l != null && i < linesToRemove; i++) {
                GLine next = l.NextLine;
                Remove(l);
                l = next;
            }
            if (top == TopLine) {
                SetTopLine(l);
            }

            // relabel IDs
            l = firstNewLine;
            int id = origBottomID;
            while (l != null && id >= from) {
                l.ID = id--;
                l = l.PrevLine;
            }

            CurrentLineNumber = savedLineNumber;

            _sixelImageManager.MoveUp(from, to, n);

            InvalidateAll();

#if DEBUG
            {
                GLine ll = (firstNewLine.NextLine != null) ? firstNewLine.NextLine : firstNewLine;
                int nextID = ll.ID;
                for (; ll != null; ll = ll.PrevLine, nextID--) {
                    if (ll.ID != nextID) {
                        Debug.WriteLine("*** ScrollDown: BAD ID ***");
                    }
                }
            }
#endif
        }

        /// <summary>
        /// <para>Scroll-up the scrolling area with left/right and top/bottom margins.</para>
        /// <para>Insert N lines at the top of the specified region and remove overflow lines from the bottom.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollUpRegion(int n) {
            ScrollUpRegion(ScrollingTopLineNumber, ScrollingBottomLineNumber, n);
        }

        /// <summary>
        /// <para>Scroll-up the scrolling area with left/right and top/bottom margins.</para>
        /// <para>Insert N lines at the top of the specified region and remove overflow lines from the bottom.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="from">line number to start scroll. (inclusive)</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollUpRegionFrom(int from, int n) {
            ScrollUpRegion(from, ScrollingBottomLineNumber, n);
        }

        /// <summary>
        /// <para>Scroll-up the scrolling area with left/right and top/bottom margins.</para>
        /// <para>Insert N lines at the top of the specified region and remove overflow lines from the bottom.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="top">line number at the top of the region. (inclusive)</param>
        /// <param name="bottom">line number at the bottom of the region. (inclusive)</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollUpRegion(int top, int bottom, int n) {
            if (!HasLeftMargin && !HasRightMargin) {
                ScrollUpLines(top, bottom, n);
                return;
            }

            if (bottom < top || n < 1) {
                return;
            }

            EnsureLine(bottom);

            GLine dstLine = FindLineOrEdge(bottom);
            GLine srcLine = FindLineOrNull(bottom - n);
            GLineManipulator srcManip = new GLineManipulator(_zMan);
            GLineManipulator dstManip = new GLineManipulator(_zMan);
            while (srcLine != null && dstLine != null && srcLine.ID >= top) {
                srcManip.Load(srcLine);
                dstManip.Load(dstLine);
                dstManip.CopyFrom(srcManip, LeftMarginOffset, RightMarginOffset + 1, LeftMarginOffset);
                dstManip.ExportTo(dstLine);
                InvalidatedRegion.InvalidateLine(dstLine.ID);
                dstLine = dstLine.PrevLine;
                srcLine = srcLine.PrevLine;
            }

            while (dstLine != null && dstLine.ID >= top) {
                dstManip.Load(dstLine);
                dstManip.FillSpace(LeftMarginOffset, RightMarginOffset + 1, _currentDecoration);
                dstManip.ExportTo(dstLine);
                InvalidatedRegion.InvalidateLine(dstLine.ID);
                dstLine = dstLine.PrevLine;
            }

            bool imageMoved = _sixelImageManager.MoveDown(top, bottom, n);

            if (imageMoved) {
                InvalidateAll();
            }
        }

        /// <summary>
        /// <para>Scroll-down the scrolling area with left/right and top/bottom margins.</para>
        /// <para>Insert N lines at the bottom of the scroll region and remove overflow lines from the top.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollDownRegion(int n) {
            ScrollDownRegion(ScrollingTopLineNumber, ScrollingBottomLineNumber, n);
        }

        /// <summary>
        /// <para>Scroll-down the scrolling area with left/right and top/bottom margins.</para>
        /// <para>Insert N lines at the bottom of the scroll region and remove overflow lines from the top.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="from">line number to start scroll. (inclusive)</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollDownRegionFrom(int from, int n) {
            ScrollDownRegion(from, ScrollingBottomLineNumber, n);
        }

        /// <summary>
        /// <para>Scroll-down the scrolling area with left/right and top/bottom margins.</para>
        /// <para>Insert N lines at the bottom of the scroll region and remove overflow lines from the top.</para>
        /// <para>The current line number is not changed.</para>
        /// </summary>
        /// <param name="top">line number at the top of the region. (inclusive)</param>
        /// <param name="bottom">line number at the bottom of the region. (inclusive)</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollDownRegion(int top, int bottom, int n) {
            if (!HasLeftMargin && !HasRightMargin) {
                ScrollDownLines(top, bottom, n);
                return;
            }

            if (bottom < top || n < 1) {
                return;
            }

            EnsureLine(bottom);

            GLine dstLine = FindLineOrEdge(top);
            GLine srcLine = FindLineOrNull(top + n);
            GLineManipulator srcManip = new GLineManipulator(_zMan);
            GLineManipulator dstManip = new GLineManipulator(_zMan);
            while (srcLine != null && dstLine != null && srcLine.ID <= bottom) {
                srcManip.Load(srcLine);
                dstManip.Load(dstLine);
                dstManip.CopyFrom(srcManip, LeftMarginOffset, RightMarginOffset + 1, LeftMarginOffset);
                dstManip.ExportTo(dstLine);
                InvalidatedRegion.InvalidateLine(dstLine.ID);
                dstLine = dstLine.NextLine;
                srcLine = srcLine.NextLine;
            }

            while (dstLine != null && dstLine.ID <= bottom) {
                dstManip.Load(dstLine);
                dstManip.FillSpace(LeftMarginOffset, RightMarginOffset + 1, _currentDecoration);
                dstManip.ExportTo(dstLine);
                InvalidatedRegion.InvalidateLine(dstLine.ID);
                dstLine = dstLine.NextLine;
            }

            bool imageMoved = _sixelImageManager.MoveUp(top, bottom, n);

            if (imageMoved) {
                InvalidateAll();
            }
        }


        //整数インデクスから見つける　CurrentLineからそう遠くない位置だろうとあたりをつける
        public override GLine FindLine(int index) {
            //currentとtopの近い方から順にみていく
            int d1 = Math.Abs(index - _currentLine.ID);
            int d2 = Math.Abs(index - _topLine.ID);
            if (d1 < d2)
                return FindLineByHint(index, _currentLine);
            else
                return FindLineByHint(index, _topLine);
        }


        public void Replace(GLine target, GLine newline) {
            newline.NextLine = target.NextLine;
            newline.PrevLine = target.PrevLine;
            if (target.NextLine != null)
                target.NextLine.PrevLine = newline;
            if (target.PrevLine != null)
                target.PrevLine.NextLine = newline;

            if (target == _firstLine)
                _firstLine = newline;
            if (target == _lastLine)
                _lastLine = newline;
            if (target == _topLine)
                SetTopLine(newline);
            if (target == _currentLine)
                _currentLine = newline;

            newline.ID = target.ID;
            _invalidatedRegion.InvalidateLine(newline.ID);
        }

        //末尾に追加する
        public override void AddLine(GLine line) {
            base.AddLine(line);
            if (_size == 1) {
                _currentLine = line;
                SetTopLine(line);
            }
        }

        public void Remove(GLine line) {
            if (_size <= 1) {
                Clear();
                return;
            }

            if (line.PrevLine != null) {
                line.PrevLine.NextLine = line.NextLine;
            }
            if (line.NextLine != null) {
                line.NextLine.PrevLine = line.PrevLine;
            }

            if (line == _firstLine)
                _firstLine = line.NextLine;
            if (line == _lastLine)
                _lastLine = line.PrevLine;
            if (line == _topLine) {
                SetTopLine(line.NextLine);
            }
            if (line == _currentLine) {
                _currentLine = line.NextLine;
                if (_currentLine == null)
                    _currentLine = _lastLine;
            }

            _size--;
            _invalidatedRegion.InvalidatedAll = true;
        }

        /// 最後のremain行以前を削除する
        public int DiscardOldLines(int remain) {
            int delete_count = _size - remain;
            if (delete_count <= 0)
                return 0;

            GLine newfirst = _firstLine;
            for (int i = 0; i < delete_count; i++)
                newfirst = newfirst.NextLine;

            //新しい先頭を決める
            _firstLine = newfirst;
            newfirst.PrevLine.NextLine = null;
            newfirst.PrevLine = null;
            _size -= delete_count;
            Debug.Assert(_size == remain);


            if (_topLine.ID < _firstLine.ID)
                SetTopLine(_firstLine);
            if (_currentLine.ID < _firstLine.ID) {
                _currentLine = _firstLine;
                this.CaretColumn = 0;
            }

            return delete_count;
        }

        public void RemoveAfter(int from) {
            GLine delete = FindLineOrNullClipTop(from);
            if (delete == null)
                return;

            GLine remain = delete.PrevLine;
            delete.PrevLine = null;
            if (remain == null) {
                Clear();
            }
            else {
                remain.NextLine = null;
                _lastLine = remain;

                while (delete != null) {
                    _size--;
                    if (delete == _topLine)
                        SetTopLine(remain);
                    if (delete == _currentLine)
                        _currentLine = remain;
                    delete = delete.NextLine;
                }
            }

            _invalidatedRegion.InvalidatedAll = true;
        }

        public void ClearRange(int from, int to, TextDecoration dec, bool selective, bool resetLineRenderingType) {
            // Note: this method doesn't clear sixel images
            GLine l = FindLineOrNullClipTop(from);
            if (l == null)
                return;

            while (l != null && l.ID < to) {
                l.Clear(dec, selective, resetLineRenderingType);
                _invalidatedRegion.InvalidateLine(l.ID);
                l = l.NextLine;
            }
        }

        //再接続用に現在ドキュメントの前に挿入
        public void InsertBefore(TerminalDocument olddoc, int paneheight) {
            lock (this) {
                GLine c = olddoc.LastLine;
                int offset = _currentLine.ID - _topLine.ID;
                bool flag = false;
                while (c != null) {
                    if (flag || c.DisplayLength == 0) {
                        flag = true;
                        GLine nl = c.CloneWithoutUpdateSpans();
                        nl.ID = _firstLine.ID - 1;
                        InsertBefore(_firstLine, nl); //最初に空でない行があれば以降は全部挿入
                        offset++;
                    }
                    c = c.PrevLine;
                }

                //IDが負になるのはちょっと怖いので修正
                if (_firstLine.ID < 0) {
                    int t = -_firstLine.ID;
                    c = _firstLine;
                    while (c != null) {
                        c.ID += t;
                        c = c.NextLine;
                    }
                }

                SetTopLine(FindLineOrEdge(_currentLine.ID - Math.Min(offset, paneheight)));
                //Dump("insert doc");
            }
        }

        public void UpdateCurrentLine(GLineManipulator manipulator) {
            GLine line = _currentLine;
            if (line != null) {
                manipulator.ExportTo(line);
                _invalidatedRegion.InvalidateLine(line.ID);
                _logServide.TextLogger.WriteLine(line);
            }
        }

        private GLine CreateErasedGLine() {
            return new GLine(TerminalWidth, TextDecoration.Default);
        }
    }

}
