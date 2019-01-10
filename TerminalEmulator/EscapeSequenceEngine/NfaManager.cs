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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Poderosa.Terminal.EscapeSequenceEngine {
    /// <summary>
    /// NFA manager
    /// </summary>
    internal class NfaManager {
        private readonly List<INfaState> _states = new List<INfaState>();

        private int _nextStateID = 1;

        private readonly INfaState _initialState;

        private class PatternInfo {
            public readonly string Pattern;
            public readonly Action<EscapeSequenceContext> Action;

            public PatternInfo(string pattern, Action<EscapeSequenceContext> action) {
                this.Pattern = pattern;
                this.Action = action;
            }
        }

        private readonly List<PatternInfo> _patterns = new List<PatternInfo>();

        private readonly PatternParser _parser = new PatternParser();

        public NfaManager() {
            _initialState = NewState();
        }

        private int GetNextID() {
            return _nextStateID++;
        }

        private NfaState NewState() {
            var newState = new NfaState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        private NfaFinalState NewFinalState(string pattern) {
            var newState = new NfaFinalState(GetNextID(), pattern);
            _states.Add(newState);
            return newState;
        }

        private NfaZeroOrMoreNumericalParamsState NewZeroOrMoreNumericalParamsState() {
            var newState = new NfaZeroOrMoreNumericalParamsState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        private NfaNNumericalParamsState NewNNumericalParamsState(int number) {
            var newState = new NfaNNumericalParamsState(GetNextID(), number);
            _states.Add(newState);
            return newState;
        }

        private NfaTextParamState NewTextParamState() {
            var newState = new NfaTextParamState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        private NfaAnyCharStringState NewAnyCharStringState() {
            var newState = new NfaAnyCharStringState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        private NfaSingleNumericalParamContentState NewSingleNumericalParamContentState() {
            var newState = new NfaSingleNumericalParamContentState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        private NfaSingleTextParamContentState NewSingleTextParamContentState() {
            var newState = new NfaSingleTextParamContentState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        private NfaSingleAnyCharStringContentState NewSingleAnyCharStringContentState() {
            var newState = new NfaSingleAnyCharStringContentState(GetNextID());
            _states.Add(newState);
            return newState;
        }

        public void AddPattern(string pattern, Action<EscapeSequenceContext> action) {
            if (Regex.IsMatch(pattern, @"{(P\*|P\d+)};{(P\*|P\d+)}")) {
                throw new ArgumentException(String.Format("multiple numerical parameters must be specified with a single specifier : {0}", pattern));
            }

            _patterns.Add(new PatternInfo(pattern, action));

            // add pattern to the NFA.
            //
            // Note:
            //  In this step, parameters ({P*},{Pt},{Ps},{P1},{P2},...) are represented as the special "Parameter State"
            //  for the convenience of the optimization in the later step.

            var patternElements = _parser.Parse(pattern);

            INfaState curState = _initialState;

            for (int i = 0; i < patternElements.Count; i++) {
                IPatternElement elem = patternElements[i];

                if (i == patternElements.Count - 1) {
                    if (!(elem is CharacterSet)) {
                        throw new ArgumentException("invalid pattern: pattern must end with a single character or a character-set ([]).");
                    }

                    var nextState = NewFinalState(pattern);
                    curState.Transitions.Add(new NfaTransition(((CharacterSet)elem).Characters, nextState));
                    curState = nextState;
                    break;
                }

                if (elem is CharacterSet) {
                    var nextState = NewState();
                    curState.Transitions.Add(new NfaTransition(((CharacterSet)elem).Characters, nextState));
                    curState = nextState;
                    continue;
                }

                if (elem is ZeroOrMoreNumericalParams) {
                    var nextState = NewZeroOrMoreNumericalParamsState();
                    curState.Transitions.Add(new NfaDigitTransition(nextState));
                    curState = nextState;
                    continue;
                }

                if (elem is NNumericalParams) {
                    var nextState = NewNNumericalParamsState(((NNumericalParams)elem).Number);
                    curState.Transitions.Add(new NfaDigitTransition(nextState));
                    curState = nextState;
                    continue;
                }

                if (elem is TextParam) {
                    var nextState = NewTextParamState();
                    curState.Transitions.Add(new NfaPrintableTransition(nextState));
                    curState = nextState;
                    continue;
                }

                if (elem is AnyCharString) {
                    var nextState = NewAnyCharStringState();
                    curState.Transitions.Add(new NfaAnyCharStringTransition(nextState));
                    curState = nextState;
                    continue;
                }

                throw new Exception("unknown pattern element.");
            }
        }

        /// <summary>
        /// Create DFA graph.
        /// </summary>
        /// <returns>DFA state manager</returns>
        /// <exception cref="Exception">Failed to convert to DFA. Maybe the given patterns were not deterministic.</exception>
        public DfaStateManager CreateDfa() {
            PrepareDFA();

            DfaStateManager dfa = new DfaStateManager();

#if true    // set false to debug non-deterministic graph

            // extract active states
            List<INfaState> activeNfaStates = new List<INfaState>();
            ProcessStateRecursively(nfaState => activeNfaStates.Add(nfaState));

            Dictionary<int, ushort> nfaStateIDToDfaStateID = new Dictionary<int, ushort>();

            // create all DFA states
            foreach (var nfaState in activeNfaStates) {
                IDfaState dfaState;
                if (nfaState is NfaFinalState) {
                    dfaState = new DfaFinalState(((NfaFinalState)nfaState).Pattern);
                }
                else {
                    dfaState = new DfaState();
                }
                ushort dfaStateID = dfa.AddState(dfaState);
                nfaStateIDToDfaStateID.Add(nfaState.ID, dfaStateID);
            }

            // add transitions
            foreach (var nfaState in activeNfaStates) {
                var dfaStateID = nfaStateIDToDfaStateID[nfaState.ID];
                IDfaState dfaState = dfa.GetState(dfaStateID);

                foreach (var nfaTransition in nfaState.Transitions) {
                    ushort nextDfaStateID = nfaStateIDToDfaStateID[nfaTransition.NextState.ID];

                    DfaTransitionType transitionType;

                    if (nfaTransition is NfaTransition) {
                        if (nfaTransition.NextState is NfaSingleNumericalParamContentState) {
                            if (Object.ReferenceEquals(nfaTransition.NextState, nfaState)) {
                                // self-transition on a numerical parameter
                                transitionType = DfaTransitionType.UpdateNumericalParam;
                            }
                            else {
                                transitionType = DfaTransitionType.StartNumericalParam;
                            }
                        }
                        else if (nfaTransition.NextState is NfaSingleTextParamContentState) {
                            if (Object.ReferenceEquals(nfaTransition.NextState, nfaState)) {
                                // self-transition on a text parameter
                                transitionType = DfaTransitionType.UpdateTextParam;
                            }
                            else {
                                transitionType = DfaTransitionType.StartTextParam;
                            }
                        }
                        else if (nfaTransition.NextState is NfaSingleAnyCharStringContentState) {
                            if (Object.ReferenceEquals(nfaTransition.NextState, nfaState)) {
                                // self-transition on a text parameter
                                transitionType = DfaTransitionType.UpdateTextParam;
                            }
                            else {
                                transitionType = DfaTransitionType.StartTextParam;
                            }
                        }
                        else if (nfaTransition.NextState is NfaState || nfaTransition.NextState is NfaFinalState) {
                            transitionType = DfaTransitionType.Normal;
                        }
                        else {
                            throw new Exception("unexpected state type : " + nfaTransition.NextState.GetType().Name);
                        }
                    }
                    else if (nfaTransition is NfaEmptyNumericalParamTransition) {
                        if (nfaTransition.NextState is NfaState || nfaTransition.NextState is NfaFinalState) {
                            transitionType = DfaTransitionType.EmptyNumericalParam;
                        }
                        else {
                            throw new Exception("unexpected state type : " + nfaTransition.NextState.GetType().Name);
                        }
                    }
                    else if (nfaTransition is NfaEmptyTextParamTransition) {
                        if (nfaTransition.NextState is NfaState || nfaTransition.NextState is NfaFinalState) {
                            transitionType = DfaTransitionType.EmptyTextParam;
                        }
                        else {
                            throw new Exception("unexpected state type : " + nfaTransition.NextState.GetType().Name);
                        }
                    }
                    else {
                        throw new Exception("unknown NFA transition type");
                    }

                    foreach (byte b in nfaTransition.Matches) {
                        try {
                            dfaState.TransitionTable.AddTransition(b, nextDfaStateID, transitionType);
                        }
                        catch (Exception e) {
                            Debug.WriteLine("{0} : b=0x{1:x2} transitionType={2} src={3} dest={4}",
                                e.Message, b, transitionType, nfaState.ID, nfaTransition.NextState.ID);
                            throw;
                        }
                    }
                }
            }

            dfa.SetInitialState(nfaStateIDToDfaStateID[_initialState.ID]);

            foreach (var pat in _patterns) {
                dfa.SetActionToFinalState(pat.Pattern, pat.Action);
            }

            dfa.ReduceSize();
            dfa.CheckConsistency();
#endif
            return dfa;
        }

        /// <summary>
        /// Convert NFA graph to DFA graph.
        /// </summary>
        /// <remarks>
        /// Common NFA-to-DFA algorithm is not used in this step.
        /// 
        /// For capturing parameters in DFA, we need consider
        /// "a character which is part of a parameter" and
        /// "a character which is not part of a parameter" differently.
        /// Common NFA-to-DFA algorithm doesn't retain such informations.
        /// 
        /// So we use "merge states and transitions" strategy.
        /// It works well for the escape-sequence patterns.
        /// </remarks>
        private void PrepareDFA() {
            ProcessStateRecursively(state => {
                // merge transitions that will be triggered by same caharacter(s).
                MergeCharacterTransitions(state);
                // merge multiple numeric parameters and break down them into more detailed transitions
                MergeNumericalParameters(state);
                // break down a text parameter into more detailed transitions.
                // (multiple transitions to the text parameters have already merged into a single transition.)
                ConvertTextParameters(state);
                // break down an any-character string into more detailed transitions.
                // (multiple transitions to the any-character string have already merged into a single transition.)
                ConvertAnyCharString(state);
                // merge transitions, again
                MergeCharacterTransitions(state);
            });
        }

        private void ProcessStateRecursively(Action<INfaState> action) {
            ProcessStateRecursively(_initialState, new HashSet<int>(), action);
        }

        private void ProcessStateRecursively(INfaState state, ISet<int> processedStateIDs, Action<INfaState> action) {
            action(state);
            processedStateIDs.Add(state.ID);

            foreach (var st in state.Transitions.Select(tr => tr.NextState).ToList()) {
                if (!processedStateIDs.Contains(st.ID)) {
                    ProcessStateRecursively(st, processedStateIDs, action);
                }
            }
        }

        /// <summary>
        /// Merge character transitions.
        /// </summary>
        /// <param name="state">target state</param>
        private void MergeCharacterTransitions(INfaState state) {
            /*
             Example
             Before:
                digraph before {
                  graph [rankdir="LR"]

                  s1 -> s2 [label="[abc]"]
                  s1 -> s3 [label="[abc]"]
                  s2 -> s4
                  s2 -> s5
                  s3 -> s6
                  s3 -> s7
                }

             If s2 and s3 were the same state type, and character-set "[abc]" causes transition to s2 and s3,
             s3 will be merged into s2.
             After:
                digraph before {
                  graph [rankdir="LR"]

                  s1 -> s2 [label="[abc]"]
                  s2 -> s4
                  s2 -> s5
                  s2 -> s6
                  s2 -> s7
                }
             */

            var newTransitions = new List<INfaTransition>();

            foreach (var origTransition in state.Transitions) {
                INfaTransition altTransition =
                    newTransitions.Find(
                        tr => origTransition.IsIdenticalWith(tr) && origTransition.NextState.IsSameType(tr.NextState));

                if (altTransition != null) {
                    bool isOrigSelfTransition = Object.ReferenceEquals(origTransition.NextState, state);
                    bool isAltSelfTransition = Object.ReferenceEquals(altTransition.NextState, state);

                    if (isOrigSelfTransition != isAltSelfTransition) {
                        throw new Exception("cannot merge self-transition and non-self-transition.");
                    }

                    if (isOrigSelfTransition) {
                        continue;
                    }

                    CopyTransitions(origTransition.NextState, altTransition.NextState);
                    // the state referred by origTransition.NextState might be referred by another transition.
                    // so don't clear origTransition.NextState.Transitions.
                }
                else {
                    newTransitions.Add(origTransition);
                }
            }

            state.Transitions.Clear();
            state.Transitions.AddRange(newTransitions);
        }

        /// <summary>
        /// Copy transitions from a state to another one.
        /// </summary>
        /// <remarks>
        /// This method doesn't change the destination state of the transitions except in the case of self-transition.
        /// </remarks>
        /// <param name="stateFrom"></param>
        /// <param name="stateTo"></param>
        private void CopyTransitions(INfaState stateFrom, INfaState stateTo) {
            foreach (var origTransition in stateFrom.Transitions) {
                if (Object.ReferenceEquals(origTransition.NextState, stateFrom)) {
                    // copy self-transition
                    stateTo.Transitions.Add(origTransition.CopyWithNewNextState(stateTo));
                }
                else {
                    stateTo.Transitions.Add(origTransition);
                }
            }
        }

        /// <summary>
        /// <para>
        /// Merge transitions to the numerical parameters into the DFA-like transitions.
        /// </para>
        /// <para>
        /// <see cref="NfaNNumericalParamsState"/> and <see cref="NfaZeroOrMoreNumericalParamsState"/> are replaced with
        /// the combination of some <see cref="NfaSingleNumericalParamContentState"/> and <see cref="NfaState"/>.
        /// </para>
        /// </summary>
        /// <param name="targetState"></param>
        private void MergeNumericalParameters(INfaState targetState) {
            // count number of params
            int maxParamNum = 0;
            bool hasVariableParams = false;
            foreach (var transition in targetState.Transitions) {
                if (transition.NextState is NfaZeroOrMoreNumericalParamsState) {
                    hasVariableParams = true;
                    continue;
                }

                if (transition.NextState is NfaNNumericalParamsState) {
                    maxParamNum = Math.Max(maxParamNum, ((NfaNNumericalParamsState)transition.NextState).Number);
                    continue;
                }
            }

            if (maxParamNum == 0 && !hasVariableParams) {
                // no need to merge
                return;
            }

            var origTransitions = new List<INfaTransition>(targetState.Transitions);

            // add new transitions for matching numerical parameters.
            // paramsStates is array like [ targetState, paramState1, afterState1, paramState2, afterState2, ... ] 
            var paramsStates = CreateNumericalParamsTransitions(targetState, maxParamNum, hasVariableParams);

            // break down NfaNNumericalParamsState or NfaZeroOrMoreNumericalParamsState into more detailed transitions.
            foreach (var transition in origTransitions) {
                // note that targetState.Transitions may be modified in this loop.

                if (transition.NextState is NfaNNumericalParamsState) {
                    var npState = (NfaNNumericalParamsState)transition.NextState;
                    var paramNum = npState.Number;
                    // transition after N numerical params
                    CopyTransitions(npState, paramsStates[paramNum * 2 - 1]);
                    // transition after (N - 1) numerical params and semicolon (the last parameter was empty)
                    CopyTransitions(npState, paramsStates[paramNum * 2 - 2]);
                    if (paramNum >= 2) {
                        // for the case that the last parameter was omitted:
                        //   add transition after (N - 1) numerical params
                        CopyTransitions(npState, paramsStates[paramNum * 2 - 3]);
                    }

                    npState.Transitions.Clear();
                    targetState.Transitions.Remove(transition);
                }
                else if (transition.NextState is NfaZeroOrMoreNumericalParamsState) {
                    var mpState = (NfaZeroOrMoreNumericalParamsState)transition.NextState;
                    // any state, including targetState, have possibility to end numerical parameters.
                    foreach (INfaState st in paramsStates) {
                        CopyTransitions(mpState, st);
                    }

                    mpState.Transitions.Clear();
                    targetState.Transitions.Remove(transition);
                }
            }
        }

        /// <summary>
        /// Create DFA-like transitions for the numerical parameters.
        /// </summary>
        /// <remarks>
        /// Only digits are accepted.
        /// Colon (':') as "a separator in a parameter sub-string" (ECMA-48 5.4.2 (b)) is not supported.
        /// </remarks>
        /// <param name="baseState">an existing state that new transitions will be added.</param>
        /// <param name="paramNum">number of the non-variable numerical parameters</param>
        /// <param name="hasVariableParams">if this parameter was true, transitions for the variable numerical parameters are added.</param>
        /// <returns>state list</returns>
        private List<INfaState> CreateNumericalParamsTransitions(INfaState baseState, int paramNum, bool hasVariableParams) {
            List<INfaState> states = new List<INfaState>();

            /*
             Add transitions:
                digraph addtransitions {
                  graph [rankdir="LR"]

                  baseState -> NfaSingleNumericalParamContentState1 [label="digit"]
                  NfaSingleNumericalParamContentState1 -> NfaSingleNumericalParamContentState1 [label="digit"]
                  NfaSingleNumericalParamContentState1 -> afterState1 [label=";"]
                  baseState -> afterState1 [label=";"]

                  afterState1 -> NfaSingleNumericalParamContentState2 [label="digit"]
                  NfaSingleNumericalParamContentState2 -> NfaSingleNumericalParamContentState2 [label="digit"]
                  NfaSingleNumericalParamContentState2 -> afterState2 [label=";"]
                  afterState1 -> afterState2 [label=";"]

                  afterState2 -> afterStateN_1  [style="dotted"]

                  afterStateN_1 -> NfaSingleNumericalParamContentStateN [label="digit"]
                  NfaSingleNumericalParamContentStateN -> NfaSingleNumericalParamContentStateN [label="digit"]

                  afterStateN_1 [label="afterState(N-1)"]
                }

             If hasVariableParams was true, tail of graph will be:
                digraph addtransitions2 {
                  graph [rankdir="LR"]

                  afterStateN_1 -> NfaSingleNumericalParamContentStateN [label="digit"]
                  NfaSingleNumericalParamContentStateN -> NfaSingleNumericalParamContentStateN [label="digit"]
                  NfaSingleNumericalParamContentStateN -> afterStateN [label=";"]
                  afterStateN_1 -> afterStateN [label=";"]

                  afterStateN -> NfaSingleNumericalParamContentStateM [label="digit"]
                  NfaSingleNumericalParamContentStateM -> NfaSingleNumericalParamContentStateM [label="digit"]
                  NfaSingleNumericalParamContentStateM -> afterStateN [label=";"]
                  afterStateN -> afterStateN [label=";"]

                  afterStateN_1 [label="afterState(N-1)"]
                }
             */

            int paramStateNum = paramNum + (hasVariableParams ? 1 : 0);

            for (int i = 0; i < paramStateNum; i++) {
                if (i == 0) {
                    states.Add(baseState);
                }
                else {
                    // the next state after transition by ";"
                    states.Add(NewState());
                }

                // [0-9]+
                states.Add(NewSingleNumericalParamContentState());
            }

            for (int i = 0; i < paramStateNum; i++) {
                if (i > 0) {
                    // end param by semicolon
                    states[i * 2 - 1].Transitions.Add(new NfaSemicolonTransition(states[i * 2]));
                    // empty param
                    byte[] semicolon = new byte[] { 0x3b };
                    states[i * 2 - 2].Transitions.Add(new NfaEmptyNumericalParamTransition(semicolon, states[i * 2]));
                }

                // start param by digit
                states[i * 2].Transitions.Add(new NfaDigitTransition(states[i * 2 + 1]));
                // repeat digits
                states[i * 2 + 1].Transitions.Add(new NfaDigitTransition(states[i * 2 + 1]));
            }

            if (hasVariableParams) {
                // self-transition by semicolon
                byte[] semicolon = new byte[] { 0x3b };
                states[paramStateNum * 2 - 2].Transitions.Add(
                    new NfaEmptyNumericalParamTransition(semicolon, states[paramStateNum * 2 - 2]));
                // end param by semicolon (backward-transition)
                states[paramStateNum * 2 - 1].Transitions.Add(
                    new NfaSemicolonTransition(states[paramStateNum * 2 - 2]));
            }

            return states;
        }

        /// <summary>
        /// <para>
        /// Convert text parameter into the DFA-like transitions.
        /// </para>
        /// <para>
        /// <see cref="NfaTextParamState"/> is replaced with <see cref="NfaState"/>.
        /// </para>
        /// </summary>
        /// <param name="targetState"></param>
        private void ConvertTextParameters(INfaState targetState) {
            /*
             Before:
                digraph before {
                  graph [rankdir="LR"]
                  targetState -> NfaTextParamState [label="printable"]
                  NfaTextParamState -> next [label="terminator"]
                }

             After:
                digraph after {
                  graph [rankdir="LR"]
                  targetState -> NfaSingleTextParamContentState [label="printable"]
                  NfaSingleTextParamContentState -> NfaSingleTextParamContentState [label="printable"]
                  NfaSingleTextParamContentState -> next [label="terminator"]
                  targetState -> next [label="terminator"]
                }
             */

            var oldTransitions = new List<INfaTransition>(targetState.Transitions);

            foreach (var transition in oldTransitions) {
                if (!(transition.NextState is NfaTextParamState)) {
                    continue;
                }

                NfaTextParamState textParamState = (NfaTextParamState)transition.NextState;

                // check terminators
                bool terminatorsAreValid =
                    textParamState.Transitions.SelectMany(tr => tr.Matches)
                        .All(b => b < 0x08 || (b > 0x0d && b < 0x20) || b > 0x7e);
                if (!terminatorsAreValid) {
                    throw new Exception("terminator of text parameter must be a non-printable character.");
                }

                var newParamState = NewSingleTextParamContentState();
                targetState.Transitions.Add(new NfaPrintableTransition(newParamState));
                newParamState.Transitions.Add(new NfaPrintableTransition(newParamState));
                CopyTransitions(textParamState, newParamState);
                // add transitions for the case of empty parameter
                CopyTransitions(textParamState, targetState);

                textParamState.Transitions.Clear();
                targetState.Transitions.Remove(transition);
            }
        }

        /// <summary>
        /// <para>
        /// Convert any-character string into the DFA-like transitions.
        /// </para>
        /// <para>
        /// <see cref="NfaAnyCharStringState"/> is replaced with <see cref="NfaState"/>.
        /// </para>
        /// </summary>
        /// <param name="targetState"></param>
        private void ConvertAnyCharString(INfaState targetState) {
            /*
             Before:
                digraph before {
                  graph [rankdir="LR"]
                  targetState -> NfaAnyCharStringState [label="any"]
                  NfaAnyCharStringState -> next [label="terminator"]
                }

             After:
                digraph after {
                  graph [rankdir="LR"]
                  targetState -> NfaSingleAnyCharStringContentState [label="any"]
                  NfaSingleAnyCharStringContentState -> NfaSingleAnyCharStringContentState [label="any"]
                  NfaSingleAnyCharStringContentState -> next [label="terminator"]
                  targetState -> next [label="terminator"]
                }
             */

            var oldTransitions = new List<INfaTransition>(targetState.Transitions);

            foreach (var transition in oldTransitions) {
                if (!(transition.NextState is NfaAnyCharStringState)) {
                    continue;
                }

                NfaAnyCharStringState anyCharStrState = (NfaAnyCharStringState)transition.NextState;

                // check terminators
                bool terminatorsAreValid =
                    anyCharStrState.Transitions.SelectMany(tr => tr.Matches)
                        .All(b => b == 0x9c);
                if (!terminatorsAreValid) {
                    throw new Exception("terminator of any-character string must be ST.");
                }

                var newContentState = NewSingleAnyCharStringContentState();
                targetState.Transitions.Add(new NfaAnyCharStringTransition(newContentState));
                newContentState.Transitions.Add(new NfaAnyCharStringTransition(newContentState));
                CopyTransitions(anyCharStrState, newContentState);
                // add transitions for the case of empty parameter
                CopyTransitions(anyCharStrState, targetState);

                anyCharStrState.Transitions.Clear();
                targetState.Transitions.Remove(transition);
            }
        }

        /// <summary>
        /// Dump state graph in dot format (Graphviz)
        /// </summary>
        /// <param name="writer"></param>
        public void DumpDot(TextWriter writer) {
            Func<string, string> escape = s => s.Replace("\\", "\\\\").Replace("\"", "\\\"");

            writer.WriteLine("digraph NFA {");

            writer.WriteLine("  graph [rankdir=\"LR\"];");

            var stateSet = new HashSet<INfaState>();

            writer.WriteLine("  // transitions");
            foreach (INfaState state in _states) {
                stateSet.Add(state);

                foreach (INfaTransition transition in state.Transitions) {
                    stateSet.Add(transition.NextState);
                    writer.WriteLine("  s{0} -> s{1} [label=\"{2}\"];",
                        state.ID, transition.NextState.ID, escape(transition.Description));
                }
            }


            writer.WriteLine("  // states");
            foreach (INfaState state in stateSet) {
                writer.WriteLine("  s{0} [label=\"{1}\"];", state.ID, escape(state.Description));
            }

            writer.WriteLine("}");
        }
    }
}
