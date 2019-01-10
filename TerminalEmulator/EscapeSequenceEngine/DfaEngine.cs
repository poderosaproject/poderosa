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

    public class DfaException : Exception {
        public DfaException(string message)
            : base(message) {
        }

        public DfaException(string format, params object[] args)
            : base(String.Format(format, args)) {
        }
    }

    /// <summary>
    /// DFA state manager.
    /// </summary>
    internal class DfaStateManager {
        // list of states. the index of the list equals to the state ID.
        private readonly List<IDfaState> _states = new List<IDfaState>();

        // initial state
        private IDfaState _initialState = null;

        // maps pattern string to DfaFinalState
        private readonly Dictionary<String, DfaFinalState> _finalStateDict = new Dictionary<string, DfaFinalState>();

        /// <summary>
        /// Constructor
        /// </summary>
        public DfaStateManager() {
            _states.Add(null);  // avoid state whose ID equals to 0
        }

        /// <summary>
        /// Get state from its ID.
        /// </summary>
        /// <param name="stateID">state ID</param>
        /// <returns>a state obtained</returns>
        /// <exception cref="DfaException">no state matched.</exception>
        public IDfaState GetState(DfaStateIDType stateID) {
            if (stateID >= 0 && stateID < _states.Count) {
                IDfaState state = _states[stateID];

                if (state != null) {
                    return state;
                }
            }

            throw new DfaException("no state matched. [ID={0}]", stateID);
        }

        /// <summary>
        /// Get initial state.
        /// </summary>
        /// <returns>a state obtained.</returns>
        /// <exception cref="DfaException">initial state is not set.</exception>
        public IDfaState GetInitialState() {
            if (_initialState == null) {
                throw new DfaException("initial state is not set.");
            }

            return _initialState;
        }

        /// <summary>
        /// Add state as an intial state.
        /// </summary>
        /// <param name="stateID">state ID</param>
        /// <exception cref="DfaException">another initial state has been already set.</exception>
        public void SetInitialState(DfaStateIDType stateID) {
            if (_initialState != null) {
                throw new DfaException("initial state has been already set.");
            }

            _initialState = GetState(stateID);
        }

        /// <summary>
        /// Add state.
        /// </summary>
        /// <param name="state">state object</param>
        /// <returns>ID of the state</returns>
        /// <exception cref="DfaException">failed to add</exception>
        public DfaStateIDType AddState(IDfaState state) {
            DfaStateIDType nextStateID = Convert.ToUInt16(_states.Count);
            _states.Add(state);

            DfaFinalState finalState = state as DfaFinalState;
            if (finalState != null) {
                if (_finalStateDict.ContainsKey(finalState.Pattern)) {
                    throw new DfaException(
                        "final state of the pattern \"{0}\" already exists.", finalState.Pattern);
                }
                _finalStateDict.Add(finalState.Pattern, finalState);
            }

            return nextStateID;
        }

        /// <summary>
        /// Set action to the final state of the specified pattern.
        /// </summary>
        /// <param name="pattern">pattern string</param>
        /// <param name="action">action to set</param>
        /// <exception cref="DfaException">failed to set</exception>
        public void SetActionToFinalState(string pattern, Action<EscapeSequenceContext> action) {
            DfaFinalState finalState;
            if (!_finalStateDict.TryGetValue(pattern, out finalState)) {
                throw new DfaException(
                    "final state of the pattern \"{0}\" not found.", pattern);
            }
            finalState.SetAction(action);
        }

        /// <summary>
        /// Check consistency of the DFA graph.
        /// </summary>
        /// <exception cref="DfaException">error was detected.</exception>
        public void CheckConsistency() {
            int minStateID = 1;
            int maxStateID = _states.Count - 1;

            bool[] refered = new bool[_states.Count];

            for (int stateID = minStateID; stateID <= maxStateID; stateID++) {
                IDfaState state = _states[stateID];

                DfaFinalState finalState = state as DfaFinalState;
                if (finalState != null) {
                    if (!finalState.HasAction) {
                        throw new DfaException(
                            "no action is assigned to the final state. [ID={0}, Pattern=\"{1}\"]",
                            stateID, finalState.Pattern);
                    }

                    if (finalState.TransitionTable.IterateTransitions().Count() != 0) {
                        throw new DfaException(
                            "final state must have no transitions. [ID={0}]",
                            stateID);
                    }
                }
                else {
                    int count = 0;
                    foreach (var transition in state.TransitionTable.IterateTransitions()) {
                        count++;

                        if (transition.NextStateID < minStateID || transition.NextStateID > maxStateID) {
                            throw new DfaException(
                                "invalid destination state ID. [ID={0} -> dest={1}]",
                                stateID, transition.NextStateID);
                        }

                        refered[transition.NextStateID] = true;
                    }

                    if (count == 0) {
                        throw new DfaException(
                            "non-final state must have transitions. [ID={0}]",
                            stateID);
                    }

                    // prevent initial state from be detected as an orphan state.
                    if (Object.ReferenceEquals(state, _initialState)) {
                        refered[stateID] = true;
                    }
                }
            }

            List<int> orphanStateIDs =
                Enumerable.Range(minStateID, maxStateID - minStateID + 1)
                    .Where(stateID => refered[stateID] == false)
                    .ToList();
            if (orphanStateIDs.Count > 0) {
                throw new DfaException(
                    "orphan states. [ID={0}]",
                    String.Join(", ", orphanStateIDs.Select(id => id.ToString())));
            }
        }

        /// <summary>
        /// Reduce DFA size.
        /// </summary>
        public void ReduceSize() {
            foreach (IDfaState state in _states) {
                if (state != null) {
                    state.TransitionTable.ReduceSize();
                }
            }

            _states.TrimExcess();
        }
    }

    /// <summary>
    /// DFA engine.
    /// </summary>
    internal class DfaEngine {
        private readonly DfaStateManager _stateManager;

        private IDfaState _currentState = null;

        private readonly EscapeSequenceContext _context;

        public const int MAX_SEQUENCE_LENGTH = 65536;

        private enum ExecutionStatus {
            // initial state. no data has been matched yet.
            Idle,
            // data has been matched.
            Running,
            // pattern matching has finished. (completed or aborted)
            Finished,
        }

        private ExecutionStatus _execStatus = ExecutionStatus.Finished;

        /// <summary>
        /// Current context.
        /// </summary>
        public EscapeSequenceContext Context {
            get {
                return _context;
            }
        }

#if UNITTEST
        internal DfaStateManager DfaStateManager {
            get {
                return _stateManager;
            }
        }
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="stateManager">state manager</param>
        /// <param name="executor">target executor object</param>
        public DfaEngine(DfaStateManager stateManager, IEscapeSequenceExecutor executor) {
            this._stateManager = stateManager;
            this._context = new EscapeSequenceContext(executor);
        }

        /// <summary>
        /// Process single byte data.
        /// </summary>
        /// <param name="b">byte data to process</param>
        /// <param name="origBytes">original bytes from the input source. these bytes are appended to the <see cref="EscapeSequenceContext.Matched"/>.</param>
        /// <returns>Whether the byte data was accepted.</returns>
        /// <exception cref="DfaException">illegal status</exception>
        public bool Process(byte b, IEnumerable<byte> origBytes) {
            ExecPendingReset();

            var transition = _currentState.TransitionTable[b];
            if (transition.TransitionType == DfaTransitionType.None) {
                // no transition
                Finish();
                return false;
            }

            IDfaState nextState = _stateManager.GetState(transition.NextStateID);

            if (_context.Matched.Count >= MAX_SEQUENCE_LENGTH - 1 && !(nextState is DfaFinalState)) {
                Finish();
                return false;
            }

            // execute the transition

            _execStatus = ExecutionStatus.Running;

            _context.Matched.AddRange(origBytes);

            if (Object.ReferenceEquals(_currentState, nextState)) {
                ExecTransitionEvent(transition.TransitionType, b, _context);
                nextState.OnRepeat(_context, b);
            }
            else {
                _currentState.OnExit(_context);
                ExecTransitionEvent(transition.TransitionType, b, _context);
                nextState.OnEnter(_context, b);
            }

            if (nextState is DfaFinalState) {
                Finish();
            }
            else {
                _currentState = nextState;
            }

            return true;
        }

        /// <summary>
        /// Abort matching.
        /// </summary>
        public void Abort() {
            ExecPendingReset();
            Finish();
        }

        private void Finish() {
            if (_execStatus != ExecutionStatus.Idle) {
                // only execution status is changed here.
                // resetting DFA state and context object are delayed until the next call of Process() or Abort().
                _execStatus = ExecutionStatus.Finished;
            }
        }

        private void ExecPendingReset() {
            if (_execStatus == ExecutionStatus.Finished) {
                _currentState = _stateManager.GetInitialState();
                _context.Clear();
                _execStatus = ExecutionStatus.Idle;
            }
        }

        private void ExecTransitionEvent(DfaTransitionType type, byte ch, EscapeSequenceContext context) {
            switch (type) {
                case DfaTransitionType.StartNumericalParam: {
                        NumericalParameter newParam = new NumericalParameter();
                        newParam.AppendDigit(ch);
                        context.NumericalParams.Add(newParam);
                    }
                    break;

                case DfaTransitionType.UpdateNumericalParam: {
                        if (context.NumericalParams.Count > 0) {
                            NumericalParameter currentParam = context.NumericalParams[context.NumericalParams.Count - 1];
                            currentParam.AppendDigit(ch);
                        }
                    }
                    break;

                case DfaTransitionType.StartTextParam: {
                        TextParameter newParam = new TextParameter();
                        newParam.AppendChar(ch);
                        context.TextParam = newParam;
                    }
                    break;

                case DfaTransitionType.UpdateTextParam: {
                        TextParameter currentParam = context.TextParam;
                        if (currentParam != null) {
                            currentParam.AppendChar(ch);
                        }
                    }
                    break;

                case DfaTransitionType.EmptyNumericalParam: {
                        context.NumericalParams.Add(new NumericalParameter());
                    }
                    break;

                case DfaTransitionType.EmptyTextParam: {
                        context.TextParam = new TextParameter();
                    }
                    break;
            }
        }
    }
}
