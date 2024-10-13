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
using System.Drawing;
using System.Diagnostics;

using Poderosa.Document;
using Poderosa.Commands;

namespace Poderosa.Terminal {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class TerminalDocument : CharacterDocument {
        private TextDecoration _currentDecoration = TextDecoration.Default;
        private int _caretColumn;
        private int _scrollingTopOffset;
        private int _scrollingBottomOffset;
        //ウィンドウの表示用テキスト
        private string _windowTitle; //ホストOSCシーケンスで指定されたタイトル
        private GLine _topLine; // top of the screen
        private GLine _viewTopLine; // top of the view
        private GLine _currentLine;

        //画面に見えている幅と高さ
        private int _width;
        private int _height;

        internal TerminalDocument(int width, int height) {
            Resize(width, height);
            Clear();
            _scrollingTopOffset = -1;
            _scrollingBottomOffset = -1;
        }

        public string WindowTitle {
            get {
                return _windowTitle;
            }
            set {
                _windowTitle = value;
            }
        }
        public int TerminalHeight {
            get {
                return _height;
            }
        }
        public int TerminalWidth {
            get {
                return _width;
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

        /// <summary>
        /// Set scrolling region
        /// </summary>
        /// <param name="topOffset">top of scrolling region specified by offset from the first line of the display area. -1 represents the top of the display area.</param>
        /// <param name="bottomOffset">bottom of scrolling region specified by offset from the first line of the display area. -1 represents the bottom of the display area.</param>
        public void SetScrollingRegion(int topOffset, int bottomOffset) {
            _scrollingTopOffset = topOffset;
            _scrollingBottomOffset = bottomOffset;
        }

        public void Clear() {
            _caretColumn = 0;
            _firstLine = null;
            _lastLine = null;
            _size = 0;
            AddLine(CreateErasedGLine());
        }

        public void Resize(int width, int height) {
            _width = width;
            _height = height;
        }

        public void ClearScrollingRegion() {
            _scrollingTopOffset = -1;
            _scrollingBottomOffset = -1;
        }

        public int CaretColumn {
            get {
                return _caretColumn;
            }
            set {
                _caretColumn = value;
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

        public int ScrollingTopOffset {
            get {
                return (TerminalHeight < 2) ? 0 : (_scrollingTopOffset < 0) ? 0 : Math.Min(_scrollingTopOffset, TerminalHeight - 2);
            }
        }

        public int ScrollingBottomOffset {
            get {
                return (TerminalHeight < 2) ? 1 : (_scrollingBottomOffset < 0) ? TerminalHeight - 1 : Math.Min(_scrollingBottomOffset, TerminalHeight - 1);
            }
        }

        public int ScrollingTop {
            get {
                return TopLineNumber + ScrollingTopOffset;
            }
        }

        public int ScrollingBottom {
            get {
                return TopLineNumber + ScrollingBottomOffset;
            }
        }

        public bool IsCurrentLineInScrollingRegion {
            get {
                return CurrentLineNumber >= ScrollingTop && CurrentLineNumber <= ScrollingBottom;
            }
        }

        public bool HasScrollingRegionTop {
            get {
                return _scrollingTopOffset >= 0;
            }
        }

        public bool HasScrollingRegionBottom {
            get {
                return _scrollingBottomOffset >= 0;
            }
        }

        internal void LineFeed() {
            if (HasScrollingRegionBottom) {
                if (CurrentLineNumber == ScrollingBottom) {
                    ScrollDown();
                }
                else {
                    CurrentLineNumber = Math.Min(CurrentLineNumber + 1, TopLineNumber + TerminalHeight - 1); // move to the next line. new line will be added as needed.
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
        /// <para>Insert one line at the top of the scroll region and remove overflow line from the bottom.</para>
        /// <para>The current line is reset to the new line inserted.</para>
        /// </summary>
        internal void ScrollUp() {
            ScrollUp(ScrollingTop, ScrollingBottom);
        }

        /// <summary>
        /// <para>Insert N lines at the top of the specified region and remove overflow lines from the bottom.</para>
        /// <para>The current line is reset to the first new line inserted.</para>
        /// </summary>
        /// <param name="from">line number at the top of the region.</param>
        /// <param name="to">line number at the bottom of the region.</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollUp(int from, int to, int n = 1) {
            if (to < from || n < 1) {
                return;
            }

            GLine top = FindLineOrEdge(from);
            if (top == null || top.ID < from || top.ID > to) {
                return;
            }
            int origTopID = top.ID;

            GLine bottom = FindLineOrEdge(to);

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

            _currentLine = firstNewLine;

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
        /// <para>Insert one line at the bottom of the scroll region and remove overflow line from the top.</para>
        /// <para>The current line is reset to the new line inserted.</para>
        /// </summary>
        internal void ScrollDown() {
            ScrollDown(ScrollingTop, ScrollingBottom);
        }

        /// <summary>
        /// <para>Insert N lines at the bottom of the scroll region and remove overflow lines from the top.</para>
        /// <para>The current line is reset to the new line inserted.</para>
        /// </summary>
        /// <param name="from">line number at the top of the region.</param>
        /// <param name="to">line number at the bottom of the region.</param>
        /// <param name="n">number of lines to insert.</param>
        internal void ScrollDown(int from, int to, int n = 1) {
            if (to < from || n < 1) {
                return;
            }

            GLine top = FindLineOrEdge(from);
            if (top == null || top.ID < from || top.ID > to) {
                return;
            }

            GLine bottom = FindLineOrEdge(to);
            int origBottomID = bottom.ID;

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

            _currentLine = firstNewLine;

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
                _caretColumn = 0;
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

        public void ClearAfter(int from, TextDecoration dec) {
            GLine l = FindLineOrNullClipTop(from);
            if (l == null)
                return;

            while (l != null) {
                l.Clear(dec);
                l = l.NextLine;
            }

            _invalidatedRegion.InvalidatedAll = true;
        }

        public void ClearRange(int from, int to, TextDecoration dec, bool selective = false) {
            GLine l = FindLineOrNullClipTop(from);
            if (l == null)
                return;

            while (l != null && l.ID < to) {
                l.Clear(dec, selective);
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
                        GLine nl = c.Clone();
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

        public GLine UpdateCurrentLine(GLineManipulator manipulator) {
            GLine line = _currentLine;
            if (line != null) {
                manipulator.ExportTo(line);
                _invalidatedRegion.InvalidateLine(line.ID);
            }
            return line;
        }

        private GLine CreateErasedGLine() {
            return new GLine(_width, _currentDecoration);
        }
    }

}
