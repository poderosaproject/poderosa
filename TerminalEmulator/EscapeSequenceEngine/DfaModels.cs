// Copyright 2019 The Poderosa Project.
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
using System.Collections.Generic;
using System.Linq;

namespace Poderosa.Terminal.EscapeSequenceEngine {
    using DfaStateIDType = UInt16;

    /// <summary>
    /// Transition types
    /// </summary>
    internal enum DfaTransitionType : byte {
        /// <summary>No transition</summary>
        None = 0,
        /// <summary>Normal transition to another state</summary>
        Normal = 1,
        /// <summary>In addition to Normal, starts a new numerical parameter</summary>
        StartNumericalParam = 2,
        /// <summary>In addition to Normal, updates a numerical parameter with a new input character</summary>
        UpdateNumericalParam = 3,
        /// <summary>In addition to Normal, starts a new text parameter</summary>
        StartTextParam = 4,
        /// <summary>In addition to Normal, updates a text parameter with a new input character</summary>
        UpdateTextParam = 5,
        /// <summary>In addition to Normal, adds empty numerical parameter</summary>
        EmptyNumericalParam = 6,
        /// <summary>In addition to Normal, adds empty text parameter</summary>
        EmptyTextParam = 7,
    }

    /// <summary>
    /// Transition
    /// </summary>
    internal struct DfaTransition {
        /// <summary>
        /// ID of the next state
        /// </summary>
        public readonly DfaStateIDType NextStateID;

        /// <summary>
        /// Transition type
        /// </summary>
        public readonly DfaTransitionType TransitionType;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="nextStateID">ID of the next state.</param>
        /// <param name="transitionType">transition type.</param>
        public DfaTransition(DfaStateIDType nextStateID, DfaTransitionType transitionType) {
            this.NextStateID = nextStateID;
            this.TransitionType = transitionType;
        }
    }

    /// <summary>
    /// Transition table
    /// </summary>
    internal class DfaTransitionTable {
        // table that maps character-code to trannsition
        private DfaTransition[] _table;
        // offset from character-code to table index
        private int _offset;

        /// <summary>
        /// Transition associated with the specified value.
        /// </summary>
        /// <param name="b">value</param>
        /// <returns>
        /// Transition struct. If the transition was not associated with the value,
        /// <see cref="DfaTransition"/> with <see cref="DfaTransitionType.None"/> is returned.
        /// </returns>
        public DfaTransition this[byte b] {
            get {
                int index = b + _offset;
                if (index >= 0 && index < _table.Length) {
                    return _table[index];
                }
                else {
                    return new DfaTransition(0, DfaTransitionType.None);
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public DfaTransitionTable() {
            // the size of table will be changed in ReduceSize()
            _table = new DfaTransition[256];
            _offset = 0;
        }

        /// <summary>
        /// Add transition to the transition table.
        /// </summary>
        /// <param name="b">a value to associate transition with.</param>
        /// <param name="nextStateID">ID of the next state.</param>
        /// <param name="transitionType">transition type.</param>
        /// <exception cref="Exception">a transition associated with the specified value already exists.</exception>
        public void AddTransition(byte b, DfaStateIDType nextStateID, DfaTransitionType transitionType) {
            int index = b + _offset;
            if (_table[index].TransitionType != DfaTransitionType.None) {
                throw new Exception("transitions conflict");
            }
            _table[index] = new DfaTransition(nextStateID, transitionType);
        }

        /// <summary>
        /// Reduce table size.
        /// </summary>
        public void ReduceSize() {
            int firstTransitionIndex =
                Array.FindIndex(_table, tr => tr.TransitionType != DfaTransitionType.None);

            if (firstTransitionIndex < 0) {
                _offset = 0;
                _table = new DfaTransition[0];
                return;
            }

            int lastTransitionIndex =
                Array.FindLastIndex(_table, tr => tr.TransitionType != DfaTransitionType.None);

            int length = lastTransitionIndex - firstTransitionIndex + 1;
            var newTable = new DfaTransition[length];
            Array.Copy(_table, firstTransitionIndex, newTable, 0, length);
            _table = newTable;
            _offset -= firstTransitionIndex;
        }

        /// <summary>
        /// Iterate active transitions.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DfaTransition> IterateTransitions() {
            return _table.Where(t => t.TransitionType != DfaTransitionType.None);
        }
    }

    /// <summary>
    /// Interface of a DFA state.
    /// </summary>
    internal interface IDfaState {
        DfaTransitionTable TransitionTable {
            get;
        }

        /// <summary>
        /// A method which will be called when the target state was changed to this state from another state.
        /// </summary>
        /// <param name="context">context object.</param>
        /// <param name="b">input value.</param>
        void OnEnter(EscapeSequenceContext context, byte b);

        /// <summary>
        /// A method which will be called when the self-transition occurred on this state.
        /// </summary>
        /// <param name="context">context object.</param>
        /// <param name="b">input value.</param>
        void OnRepeat(EscapeSequenceContext context, byte b);

        /// <summary>
        /// A method which will be called when the target state was changed from this state to another state.
        /// </summary>
        /// <param name="context">context object.</param>
        void OnExit(EscapeSequenceContext context);
    }

    /// <summary>
    /// Base implementation of <see cref="IDfaState"/>.
    /// </summary>
    internal abstract class DfaStateBase : IDfaState {
        public DfaTransitionTable TransitionTable {
            get;
            private set;
        }

        public abstract void OnEnter(EscapeSequenceContext context, byte b);

        public abstract void OnRepeat(EscapeSequenceContext context, byte b);

        public abstract void OnExit(EscapeSequenceContext context);

        protected DfaStateBase() {
            this.TransitionTable = new DfaTransitionTable();
        }
    }

    /// <summary>
    /// Standard state.
    /// </summary>
    internal class DfaState : DfaStateBase {
        public override void OnEnter(EscapeSequenceContext context, byte b) {
        }

        public override void OnRepeat(EscapeSequenceContext context, byte b) {
        }

        public override void OnExit(EscapeSequenceContext context) {
        }

        public DfaState()
            : base() {
        }
    }

    /// <summary>
    /// Final state.
    /// </summary>
    internal class DfaFinalState : DfaStateBase {
        private readonly string _pattern;

        private Action<EscapeSequenceContext> _action;

        public string Pattern {
            get {
                return _pattern;
            }
        }

        public bool HasAction {
            get {
                return _action != null;
            }
        }

        public void SetAction(Action<EscapeSequenceContext> action) {
            if (_action != null) {
                throw new Exception("action was already assigned.");
            }
            _action = action;
        }

        public override void OnEnter(EscapeSequenceContext context, byte ch) {
            if (_action != null) {
                context.Pattern = _pattern;

                try {
                    _action(context);
                }
                catch (Exception) {
                }
            }
        }

        public override void OnRepeat(EscapeSequenceContext context, byte ch) {
        }

        public override void OnExit(EscapeSequenceContext context) {
        }

        public DfaFinalState(string pattern, Action<EscapeSequenceContext> action)
            : base() {
            this._pattern = pattern;
            this._action = action;
        }

        public DfaFinalState(string pattern)
            : this(pattern, null) {
        }
    }
}
