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

using Poderosa.Commands;
using Poderosa.Document;
using Poderosa.Forms;
using Poderosa.Sessions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Poderosa.View {

    /// <summary>
    /// A class that manages the text selection on the document.
    /// </summary>
    internal class TextSelection : ITextSelection {

        internal enum Mode {
            Char,
            Word,
            Line
        }

        internal enum State {
            /// <summary>Empty. (no selection)</summary>
            Empty,
            /// <summary>The first anchor point has been set. (mouse-down)</summary>
            Started,
            /// <summary>Expanding the region. (during mouse-move)</summary>
            Expanding,
            /// <summary>The second anchor point has been set. (mouse-up)</summary>
            Fixed,
        }

        internal struct Region {
            /// <summary>starting row ID (inclusive)</summary>
            public readonly int StartRowID;
            /// <summary>starting caret position</summary>
            public readonly int StartPos;
            /// <summary>ending row ID (inclusive)</summary>
            public readonly int EndRowID;
            /// <summary>ending caret position. null represents the end-of-line.</summary>
            public readonly int? EndPos;

            public Region(int startRowID, int startPos, int endRowID, int? endPos) {
                this.StartRowID = startRowID;
                this.StartPos = startPos;
                this.EndRowID = endRowID;
                this.EndPos = endPos;
            }
        }

        private readonly CharacterDocumentViewer _ownerViewer;

        private readonly List<ISelectionListener> _listeners = new List<ISelectionListener>();

        private State _state = State.Empty;

        // Row ID of the first anchor point
        private int _firstRowID;
        // Caret position of the first anchor point
        private int _firstPosLeft;      // left-end position of the word
        private int? _firstPosRight;    // right-end position of the word, or null (end-of-line)

        // Row ID of the second anchor point
        private int _secondRowID;
        // Caret position of the first anchor point
        private int _secondPosLeft;      // left-end position of the word
        private int? _secondPosRight;    // right-end position of the word, or null (end-of-line)

        private Mode _mode = Mode.Char;

        // Mouse position at the first anchor point of the previous selection
        private int _prevStartMouseX;
        private int _prevStartMouseY;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="viewer"></param>
        public TextSelection(CharacterDocumentViewer viewer) {
            _ownerViewer = viewer;
        }

        /// <summary>
        /// Current state of the text-selection
        /// </summary>
        public State CurrentState {
            get {
                return _state;
            }
        }

        /// <summary>
        /// Current mode of the text-selection
        /// </summary>
        public Mode CurrentMode {
            get {
                return _mode;
            }
        }

        #region ISelection

        public IPoderosaView OwnerView {
            get {
                return (IPoderosaView)_ownerViewer.GetAdapter(typeof(IPoderosaView));
            }
        }

        public IPoderosaCommand TranslateCommand(IGeneralCommand command) {
            return null;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return WindowManagerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public void AddSelectionListener(ISelectionListener listener) {
            _listeners.Add(listener);
        }
        public void RemoveSelectionListener(ISelectionListener listener) {
            _listeners.Remove(listener);
        }

        #endregion

        #region ITextSelection

        public bool IsEmpty {
            get {
                return _state == State.Empty
                    || (_firstRowID == _secondRowID
                        && _firstPosLeft == _firstPosRight
                        && _secondPosLeft == _secondPosRight
                        && _firstPosLeft == _secondPosLeft);
            }
        }

        /// <summary>
        /// Clear selection
        /// </summary>
        public void Clear() {
            _state = State.Empty;
            _firstRowID = _secondRowID = 0;
            _firstPosLeft = _secondPosLeft = 0;
            _firstPosRight = _secondPosRight = 0;
            _mode = Mode.Char;
        }

        /// <summary>
        /// Select entire screen
        /// </summary>
        public void SelectAll() {
            ICharacterDocument doc = _ownerViewer.CharacterDocument;
            if (doc == null) {
                return;
            }
            RowIDSpan rowIDSpan = doc.GetRowIDSpan();
            if (rowIDSpan.Length <= 0) {
                Clear();
                return;
            }
            _firstRowID = rowIDSpan.Start;
            _firstPosLeft = 0;
            _firstPosRight = 0;
            _secondRowID = rowIDSpan.Start + rowIDSpan.Length - 1;
            _secondPosLeft = 0;
            _secondPosRight = null; // end-of-line
            _state = State.Fixed;
            FireSelectionFixed();
        }

        /// <summary>
        /// Gets text in the selection.
        /// </summary>
        /// <param name="opt"></param>
        /// <returns></returns>
        public string GetSelectedText(TextFormatOption opt) {
            Region? regionOrNull = GetRegion();
            if (!regionOrNull.HasValue) {
                return "";
            }

            Region region = regionOrNull.Value;

            StringBuilder buff = new StringBuilder();

            ICharacterDocument doc = _ownerViewer.CharacterDocument;
            if (doc != null) {
                lock (doc.SyncRoot) {
                    RowIDSpan docSpan = doc.GetRowIDSpan();
                    RowIDSpan selSpan = docSpan.Intersect(
                        new RowIDSpan(region.StartRowID, region.EndRowID - region.StartRowID + 1));

                    if (selSpan.Length <= 0) {
                        // selected rows were already missing in the current document
                        return "";
                    }

                    doc.ForEach(selSpan.Start, selSpan.Length, (rowID, line) => {
                        if (line != null) {
                            bool eolRequired = (opt == TextFormatOption.AsLook || line.EOLType != EOLType.Continue);
                            int lineLen = line.DisplayLength;
                            int startCol = (rowID == region.StartRowID) ? Math.Min(region.StartPos, lineLen) : 0;
                            if (rowID == region.EndRowID) {
                                // the last line
                                CopyGLineContent(line, buff, startCol, region.EndPos);
                                if (eolRequired && _mode == Mode.Line) {
                                    buff.Append("\r\n");
                                }
                            }
                            else {
                                CopyGLineContent(line, buff, startCol, null);
                                if (eolRequired) {
                                    buff.Append("\r\n");
                                }
                            }
                        }
                    });
                }
            }

            return buff.ToString();
        }

        private void CopyGLineContent(GLine line, StringBuilder buff, int start, int? end) {
            if (start > 0 && line.IsRightSideOfZenkaku(start)) {
                start--;
            }

            if (end.HasValue) {
                line.WriteTo(
                    (data, len) => buff.Append(data, 0, len),
                    start, end.Value);
            }
            else {
                line.WriteTo(
                    (data, len) => buff.Append(data, 0, len),
                    start);
            }
        }

        #endregion

        /// <summary>
        /// Gets current selected region.
        /// </summary>
        /// <returns>region, or null if the selection is empty.</returns>
        public Region? GetRegion() {
            if (IsEmpty) {
                return null;
            }

            if (_firstRowID < _secondRowID) {
                return new Region(
                            startRowID: _firstRowID,
                            startPos: _firstPosLeft,
                            endRowID: _secondRowID,
                            endPos: _secondPosRight);
            }
            else if (_firstRowID == _secondRowID) {
                int? selEndPos;
                if (_firstPosRight.HasValue && _secondPosRight.HasValue) {
                    selEndPos = Math.Max(_firstPosRight.Value, _secondPosRight.Value);
                }
                else {
                    selEndPos = null;   // end-of-line
                }

                return new Region(
                            startRowID: _firstRowID,
                            startPos: Math.Min(_firstPosLeft, _secondPosLeft),
                            endRowID: _firstRowID,
                            endPos: selEndPos);
            }
            else {
                return new Region(
                            startRowID: _secondRowID,
                            startPos: _secondPosLeft,
                            endRowID: _firstRowID,
                            endPos: _firstPosRight);
            }
        }

        public bool CanHandleMouseDown {
            get {
                return true;
            }
        }

        public bool CanHandleMouseMove {
            get {
                return _state == State.Expanding || _state == State.Started;
            }
        }

        public bool CanHandleMouseUp {
            get {
                return _state == State.Expanding || _state == State.Started;
            }
        }

        /// <summary>
        /// Handle mouse-down
        /// </summary>
        /// <param name="rowID">row ID of the target row.</param>
        /// <param name="line">line data at the target row.</param>
        /// <param name="position">caret position</param>
        /// <param name="modeOverride">mode to override, or null to use a mode determined.</param>
        /// <param name="mouseX">raw mouse position</param>
        /// <param name="mouseY">raw mouse position</param>
        public void OnMouseDown(int rowID, GLine line, int position, Mode? modeOverride, int mouseX, int mouseY) {
            // adjust position with the boundary of the wide-width character
            line.ExpandBuffer(position + 1);
            if (line.IsRightSideOfZenkaku(position)) {
                position--;
            }

            if (modeOverride.HasValue) {
                _mode = modeOverride.Value;
            }
            else if (Math.Abs(mouseX - _prevStartMouseX) <= 1 && Math.Abs(mouseY - _prevStartMouseY) <= 1) {
                // switch modes
                switch (_mode) {
                    case Mode.Char:
                        _mode = Mode.Word;
                        break;
                    case Mode.Word:
                        _mode = Mode.Line;
                        break;
                    case Mode.Line:
                    default:
                        _mode = Mode.Char;
                        break;
                }
            }
            else {
                _mode = Mode.Char;
            }

            _firstRowID = _secondRowID = rowID;

            switch (_mode) {
                case Mode.Word: {
                        int start;
                        int end;
                        line.FindWordBreakPoint(position, out start, out end);
                        _firstPosLeft = _secondPosLeft = start;
                        _firstPosRight = _secondPosRight = end;
                    }
                    break;
                case Mode.Line:
                    _firstPosLeft = _secondPosLeft = 0;
                    _firstPosRight = _secondPosRight = null;    // end-of-line
                    break;
                case Mode.Char:
                default:
                    _firstPosLeft = _secondPosLeft = position;
                    _firstPosRight = _secondPosRight = position;
                    break;
            }
            _state = State.Started;
            _prevStartMouseX = mouseX;
            _prevStartMouseY = mouseY;
            FireSelectionStarted();
        }

        /// <summary>
        /// Handle mouse-move
        /// </summary>
        /// <param name="rowID">row ID of the target row.</param>
        /// <param name="line">line data at the target row.</param>
        /// <param name="position">caret position</param>
        /// <param name="modeOverride">mode to override, or null to use a mode determined at the mouse-down.</param>
        public void OnMouseMove(int rowID, GLine line, int position, Mode? modeOverride) {
            // adjust position with the boundary of the wide-width character
            line.ExpandBuffer(position + 1);
            if (line.IsRightSideOfZenkaku(position)) {
                position--;
            }

            _secondRowID = rowID;

            switch (modeOverride ?? _mode) {
                case Mode.Word: {
                        int start;
                        int end;
                        line.FindWordBreakPoint(position, out start, out end);
                        _secondPosLeft = start;
                        _secondPosRight = end;
                    }
                    break;
                case Mode.Line:
                    _secondPosLeft = 0;
                    _secondPosRight = null; // end-of-line
                    break;
                case Mode.Char:
                default:
                    _secondPosLeft = position;
                    _secondPosRight = position;
                    break;
            }
            _state = State.Expanding;
        }

        /// <summary>
        /// Handle mouse-up
        /// </summary>
        public void OnMouseUp() {
            if (IsEmpty) {
                Clear();
            }
            else {
                _state = State.Fixed;
            }
            FireSelectionFixed();
        }


        private void FireSelectionStarted() {
            foreach (ISelectionListener listener in _listeners) {
                listener.OnSelectionStarted();
            }
        }

        private void FireSelectionFixed() {
            foreach (ISelectionListener listener in _listeners) {
                listener.OnSelectionFixed();
            }
        }

    }
}
