// Copyright 2024 The Poderosa Project.
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
using System.Reflection;
using System.Text;

namespace Poderosa.Terminal.EscapeSequence {

    /// <summary>
    /// Escape sequence parameter type
    /// </summary>
    internal enum EscapeSequenceParamType {
        /// <summary>
        /// No parameters
        /// </summary>
        None,
        /// <summary>
        /// Numeric parameters
        /// </summary>
        Numeric,
        /// <summary>
        /// Text parameter
        /// </summary>
        Text,
    }

    /// <summary>
    /// Attribute to specify escape sequence pattern
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    internal class EscapeSequenceAttribute : Attribute {

        /// <summary>
        /// Parameter type
        /// </summary>
        public EscapeSequenceParamType ParamType {
            get;
            private set;
        }

        /// <summary>
        /// Characters before parameters
        /// </summary>
        public char[] Prefix {
            get;
            private set;
        }

        /// <summary>
        /// Characters after parameters
        /// </summary>
        public char[] Suffix {
            get;
            private set;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="c">characters</param>
        public EscapeSequenceAttribute(params char[] c) {
            if (c.Length == 0) {
                throw new ArgumentException("characters must have at least one character");
            }
            Prefix = c;
            ParamType = EscapeSequenceParamType.None;
            Suffix = new char[] { };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefix">prefix character</param>
        /// <param name="paramType">parameter type</param>
        /// <param name="suffix">suffix characters</param>
        public EscapeSequenceAttribute(char prefix, EscapeSequenceParamType paramType, params char[] suffix) {
            if (suffix.Length == 0) {
                throw new ArgumentException("suffix must have at least one character");
            }
            Prefix = new char[] { prefix };
            ParamType = paramType;
            Suffix = suffix;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p1">1st prefix character</param>
        /// <param name="p2">2nd prefix character</param>
        /// <param name="paramType">parameter type</param>
        /// <param name="suffix">suffix characters</param>
        public EscapeSequenceAttribute(char p1, char p2, EscapeSequenceParamType paramType, params char[] suffix) {
            if (suffix.Length == 0) {
                throw new ArgumentException("suffix must have at least one character");
            }
            Prefix = new char[] { p1, p2 };
            ParamType = paramType;
            Suffix = suffix;
        }
    }

    /// <summary>
    /// Numeric parameters
    /// </summary>
    internal class NumericParams {
        private readonly int?[] _p;
        private readonly int[][] _c;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p">An array of single numeric parameters. If the parameter is not a single numeric parameter, the element is null.</param>
        /// <param name="c">An array of numeric parameters consisting of integer combinations.</param>
        public NumericParams(int?[] p, int[][] c) {
            _p = p;
            _c = c;
        }

        public bool IsSingleInteger(int index) {
            return (index >= 0 && index < _p.Length) ? _p[index].HasValue : false;
        }

        public bool IsIntegerCombination(int index) {
            return (index >= 0 && index < _c.Length) ? _c[index] != null : false;
        }

        public int Get(int index, int defaultValue) {
            return (index >= 0 && index < _p.Length) ? _p[index].GetValueOrDefault(defaultValue) : defaultValue;
        }

        public int[] GetIntegerCombination(int index) {
            return (index >= 0 && index < _c.Length) ? _c[index] : null;
        }

        public IEnumerable<int?> Enumerate() {
            return _p;
        }

        public IEnumerable<int> EnumerateWithDefault(int defaultValue) {
            return _p.Select(v => v.GetValueOrDefault(defaultValue));
        }

        public IEnumerable<int> EnumerateWithoutNull() {
            return _p.Where(v => v.HasValue).Select(v => v.Value);
        }
    }

    /// <summary>
    /// <para>Base class of <see cref="EscapeSequenceEngine{T}"/>.</para>
    /// <para>This class defines internal classes for the implementation of <see cref="EscapeSequenceEngine{T}"/>.</para>
    /// </summary>
    internal abstract class EscapeSequenceEngineBase {
        #region Context

        /// <summary>
        /// Pattern matching context
        /// </summary>
        internal class Context {

            private readonly StringBuilder _buff = new StringBuilder();
            private int _paramStart = 0; // inclusive
            private int _paramEnd = 0; // exclusive

            /// <summary>
            /// Reset
            /// </summary>
            public void Reset() {
                _buff.Clear();
                _paramStart = _paramEnd = 0;
            }

            /// <summary>
            /// Append a single character to buffer
            /// </summary>
            /// <param name="ch"></param>
            public void AppendChar(char ch) {
                _buff.Append(ch);
            }

            /// <summary>
            /// Append a single character to buffer as parameter
            /// </summary>
            /// <param name="ch"></param>
            public void AppendParamChar(char ch) {
                int index = _buff.Length;
                _buff.Append(ch);
                if (_paramStart == _paramEnd) {
                    _paramStart = index;
                }
                _paramEnd = index + 1;
            }

            /// <summary>
            /// Get buffered text
            /// </summary>
            /// <returns>buffered text</returns>
            public string GetBufferedText() {
                return _buff.ToString();
            }

            /// <summary>
            /// Get text parameter
            /// </summary>
            /// <returns>text parameter</returns>
            public string GetTextParam() {
                return _buff.ToString(_paramStart, _paramEnd - _paramStart);
            }

            /// <summary>
            /// Get numeric parameters
            /// </summary>
            /// <param name="numericParams">An array of single numeric parameters. If the parameter is not a single numeric parameter, the element is null.</param>
            /// <param name="combinationParams">An array of numeric parameters consisting of integer combinations.</param>
            public void GetNumericParams(out int?[] numericParams, out int[][] combinationParams) {
                string p = GetTextParam();
                int paramCount = CountParams(p);
                numericParams = new int?[paramCount];
                combinationParams = new int[paramCount][];
                int index = 0;
                int value = 0;
                int digits = 0;
                bool error = false;
                List<int> combination = new List<int>();
                foreach (char ch in p) {
                    switch (ch) {
                        case ';':
                            if (!error) {
                                SetNumericParam(numericParams, combinationParams, index, value, digits, combination);
                            }
                            index++;
                            value = 0;
                            digits = 0;
                            error = false;
                            combination.Clear();
                            break;
                        case ':':
                            if (!error) {
                                if (digits > 0) {
                                    combination.Add(value);
                                    value = 0;
                                    digits = 0;
                                }
                                else {
                                    error = true;
                                }
                            }
                            break;
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            value = value * 10 + (ch - '0');
                            digits++;
                            break;
                        default:
                            error = true;
                            break;
                    }
                }
                if (!error) {
                    SetNumericParam(numericParams, combinationParams, index, value, digits, combination);
                }
            }

            private int CountParams(string p) {
                int sepCount = 0;
                foreach (char ch in p) {
                    if (ch == ';') {
                        sepCount++;
                    }
                }
                return sepCount + 1;
            }

            private void SetNumericParam(int?[] numericParams, int[][] combinationParams, int index, int value, int digits, List<int> combination) {
                if (digits > 0) {
                    if (combination.Count > 0) {
                        combination.Add(value);
                        combinationParams[index] = combination.ToArray();
                    }
                    else {
                        numericParams[index] = value;
                    }
                }
                else {
                    if (combination.Count > 0) {
                        combinationParams[index] = combination.ToArray();
                    }
                }
            }
        }

        #endregion

        #region States

        private static int _nextStateId = 1;

        /// <summary>
        /// State interface
        /// </summary>
        internal interface State {
            int Id {
                get;
            }

            State Accept(Context context, char ch);
        }

        /// <summary>
        /// Final state
        /// </summary>
        internal class FinalState : State {
            public readonly Action<object, Context> Action;
            public int Id {
                get;
                private set;
            }

            public FinalState(Action<object, Context> action) {
                Action = action;
                Id = _nextStateId++;
            }

            public State Accept(Context context, char ch) {
                return null;
            }
        }

        /// <summary>
        /// Base state class that accepts one character and transitions to the next state.
        /// </summary>
        internal abstract class CharStateBase : State {
            private const int CHAR_MAX = 0x9f;
            private readonly State[] table = new State[CHAR_MAX + 1];

            public int Id {
                get;
                private set;
            }

            protected CharStateBase() {
                Id = _nextStateId++;
            }

            public abstract State Accept(Context context, char ch);

            protected State GetNextState(char ch) {
                return (ch <= CHAR_MAX) ? table[ch] : null;
            }

            public void Register(EscapeSequenceAttribute attr, Action<object, Context> action) {
                FinalState final = new FinalState(action);

                int prefixLen = attr.Prefix.Length;
                if (prefixLen == 0) {
                    throw new ArgumentException("missing prefix");
                }

                CharStateBase s = this;
                for (int i = 0; i < prefixLen - 1; i++) {
                    s = s.RegisterOrReuseState<CharState>(attr.Prefix[i]);
                }

                CharStateBase s2 = null;
                if (attr.ParamType == EscapeSequenceParamType.Numeric) {
                    // (this) ---[last-prefix-char]--> (CharState) --[0-9;:]--> (NumericParamsState) -->
                    //                                     |
                    //                                     V       numeric parameters were omitted
                    //                                      ------------------------------------------->
                    CharState charState = s.RegisterOrReuseState<CharState>(attr.Prefix[prefixLen - 1]);
                    NumericParamsState numericParamsState = charState.RegisterOrReuseState<NumericParamsState>('0');
                    foreach (char c in new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', ';', ':' }) {
                        // If numericParamsState was a reused one, the same instance should already be registered for '1' to ':'.
                        // In such case, RegisterState() returns successfully.
                        charState.RegisterState(c, numericParamsState);
                    }
                    s = numericParamsState;
                    s2 = charState;
                }
                else if (attr.ParamType == EscapeSequenceParamType.Text) {
                    s = s.RegisterOrReuseState<TextParamState>(attr.Prefix[prefixLen - 1]);
                }
                else /* if (attr.ParamType == EscapeSequenceParamType.None) */ {
                    s.RegisterFinal(attr.Prefix[prefixLen - 1], final);
                    return;
                }

                int suffixLen = attr.Suffix.Length;
                if (suffixLen == 0) {
                    throw new ArgumentException("missing suffix");
                }
                for (int i = 0; i < suffixLen - 1; i++) {
                    CharStateBase lastState = s.RegisterOrReuseState<CharState>(attr.Suffix[i]);
                    if (s2 != null) {
                        s2.CopyTransitionFrom(s, attr.Suffix[i]);
                    }
                    s = lastState;
                    s2 = null;
                }

                s.RegisterFinal(attr.Suffix[suffixLen - 1], final);
                if (s2 != null) {
                    s2.CopyTransitionFrom(s, attr.Suffix[suffixLen - 1]);
                }
            }

            private void CopyTransitionFrom(CharStateBase reference, char ch) {
                // register the same state instance in reference
                RegisterState(ch, reference.table[ch]);

                // if the character is a special control character, alternative transiton would also registered in reference
                char alt1, alt2;
                if (To7bitPair(ch, out alt1, out alt2)) {
                    RegisterState(alt1, reference.table[alt1]);
                }
            }

            private T RegisterOrReuseState<T>(char ch) where T : CharStateBase, new() {
                // (this)---[ch]--->(T)
                T s = this.RegisterOrReuseStateCore<T>(ch);

                char alt1, alt2;
                if (To7bitPair(ch, out alt1, out alt2)) {
                    // (this)---------------[ch]----------------->(T)
                    //    |                                        ^
                    //    V                                        |
                    //     -----[alt1]-->(CharState)---[alt2]------
                    CharState altState = this.RegisterOrReuseStateCore<CharState>(alt1);
                    altState.RegisterState(alt2, s);
                }

                return s;
            }

            private T RegisterOrReuseStateCore<T>(char ch) where T : CharStateBase, new() {
                if (ch > CHAR_MAX) {
                    throw new ArgumentException(String.Format("invalid character: u{0:x4}", (uint)ch));
                }

                if (table[ch] == null) {
                    T nextState = new T();
                    table[ch] = nextState;
                    return nextState;
                }

                Type existingStateType = table[ch].GetType();

                if (!(table[ch] is T)) {
                    throw new ArgumentException(String.Format("conflict: char=u{0:x4} expected={1} actual={2}", (uint)ch, typeof(T).Name, table[ch].GetType().Name));
                }
                return (T)table[ch];
            }

            private void RegisterState(char ch, State state) {
                if (ch > CHAR_MAX) {
                    throw new ArgumentException(String.Format("invalid character: u{0:x4}", (uint)ch));
                }

                if (table[ch] != null) {
                    if (Object.ReferenceEquals(table[ch], state)) {
                        // ok
                        return;
                    }
                    throw new ArgumentException(String.Format("conflict: char=u{0:x4} expected={1} actual={2}", (uint)ch, "null", table[ch].GetType().Name));
                }

                table[ch] = state;
            }

            private void RegisterFinal(char ch, FinalState final) {
                // (this)---[ch]--->(FinalState)
                this.RegisterState(ch, final);

                char alt1, alt2;
                if (To7bitPair(ch, out alt1, out alt2)) {
                    // (this)---------------[ch]----------------->(FinalState)
                    //    |                                        ^
                    //    V                                        |
                    //     -----[alt1]-->(CharState)---[alt2]------
                    CharState altState = this.RegisterOrReuseStateCore<CharState>(alt1);
                    altState.RegisterState(alt2, final);
                }
            }

            private bool To7bitPair(char ch, out char c1, out char c2) {
                switch (ch) {
                    case ControlCode.IND:
                        c1 = ControlCode.ESC;
                        c2 = 'D';
                        return true;
                    case ControlCode.NEL:
                        c1 = ControlCode.ESC;
                        c2 = 'E';
                        return true;
                    case ControlCode.HTS:
                        c1 = ControlCode.ESC;
                        c2 = 'H';
                        return true;
                    case ControlCode.RI:
                        c1 = ControlCode.ESC;
                        c2 = 'M';
                        return true;
                    case ControlCode.SS2:
                        c1 = ControlCode.ESC;
                        c2 = 'N';
                        return true;
                    case ControlCode.SS3:
                        c1 = ControlCode.ESC;
                        c2 = 'O';
                        return true;
                    case ControlCode.DCS:
                        c1 = ControlCode.ESC;
                        c2 = 'P';
                        return true;
                    case ControlCode.SPA:
                        c1 = ControlCode.ESC;
                        c2 = 'V';
                        return true;
                    case ControlCode.EPA:
                        c1 = ControlCode.ESC;
                        c2 = 'W';
                        return true;
                    case ControlCode.SOS:
                        c1 = ControlCode.ESC;
                        c2 = 'X';
                        return true;
                    case ControlCode.DECID:
                        c1 = ControlCode.ESC;
                        c2 = 'Z';
                        return true;
                    case ControlCode.CSI:
                        c1 = ControlCode.ESC;
                        c2 = '[';
                        return true;
                    case ControlCode.ST:
                        c1 = ControlCode.ESC;
                        c2 = '\\';
                        return true;
                    case ControlCode.OSC:
                        c1 = ControlCode.ESC;
                        c2 = ']';
                        return true;
                    case ControlCode.PM:
                        c1 = ControlCode.ESC;
                        c2 = '^';
                        return true;
                    case ControlCode.APC:
                        c1 = ControlCode.ESC;
                        c2 = '_';
                        return true;
                    default:
                        c1 = c2 = '\0';
                        return false;
                }
            }

            public string[] Dump() {
                return DumpInternal("").ToArray();
            }

            private IEnumerable<string> DumpInternal(string prefix) {
                yield return prefix + "<" + GetType().Name + ">(#" + Id + ")";

                string indent = new string(' ', prefix.Length + 2);
                for (int ch = 0; ch < table.Length; ch++) {
                    if (table[ch] == null) {
                        continue;
                    }
                    string charName = (ch < 0x21 || ch > 0x7e) ? String.Format("0x{0:x2}", ch) : new string(new char[] { (char)ch });
                    if (table[ch] is CharStateBase) {
                        foreach (string s in ((CharStateBase)table[ch]).DumpInternal(indent + "[" + charName + "] --> ")) {
                            yield return s;
                        }
                    }
                    else {
                        yield return indent + "[" + charName + "] --> <" + table[ch].GetType().Name + ">(#" + table[ch].Id + ")";
                    }
                }
            }
        }

        /// <summary>
        /// State that accepts one character and transitions to the next state.
        /// </summary>
        internal class CharState : CharStateBase {

            public override State Accept(Context context, char ch) {
                State s = GetNextState(ch);
                if (s != null) {
                    if (s is NumericParamsState) {
                        context.AppendParamChar(ch);
                    }
                    else {
                        context.AppendChar(ch);
                    }
                }
                return s;
            }
        }

        /// <summary>
        /// State that continues to accept numeric parameters or accepts one terminal character that transitions to the next state.
        /// </summary>
        /// <remarks>
        /// The pattern of numeric parameters consists of a preceding CharState and a subsequent NumericParamsState.
        /// The preceding CharState handles the case when the numeric parameters are omitted and the case when a prefix continues.
        /// </remarks>
        internal class NumericParamsState : CharStateBase {

            // Note: The base transition table contains only terminating characters, not characters for numerical parameters.

            public override State Accept(Context context, char ch) {
                State s = GetNextState(ch);
                if (s != null) {
                    context.AppendChar(ch);
                    return s;
                }

                // ':' may be used as a separator in a parameter
                if ((ch >= '0' && ch <= '9') || ch == ';' || ch == ':') {
                    context.AppendParamChar(ch);
                    return this;
                }

                return null;
            }
        }

        /// <summary>
        /// State that continues to accept text parameter or accepts one terminal character that transitions to the next state.
        /// </summary>
        /// <remarks>
        /// Unlike numeric parameters, the text parameter pattern consists of a single TextParamState.
        /// </remarks>
        internal class TextParamState : CharStateBase {

            // Note: The base transition table contains only terminating characters.

            public override State Accept(Context context, char ch) {
                State s = GetNextState(ch);
                if (s != null) {
                    context.AppendChar(ch);
                    return s;
                }

                if (ch >= 0x20) {
                    context.AppendParamChar(ch);
                    return this;
                }

                return null;
            }
        }

        #endregion

        /// <summary>
        /// Register escape sequence handlers marked with EscapeSequenceAttribute.
        /// </summary>
        /// <param name="root">root state</param>
        /// <param name="type">class containing escape sequence handlers</param>
        protected static void RegisterHandlers(CharState root, Type type) {
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                foreach (EscapeSequenceAttribute attr in method.GetCustomAttributes<EscapeSequenceAttribute>()) {
                    RegisterHandler(root, attr, method);
                }
            }
        }

        private static void RegisterHandler(CharState root, EscapeSequenceAttribute attribute, MethodInfo method) {
            Action<object, Context> action;
            switch (attribute.ParamType) {
                case EscapeSequenceParamType.None: {
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 0) {
                            action = (obj, context) => method.Invoke(obj, null);
                        }
                        else {
                            throw new ArgumentException(String.Format("method must have no arguments: {0}", method.Name));
                        }
                        break;
                    }

                case EscapeSequenceParamType.Numeric: {
                        ParameterInfo[] parameters = method.GetParameters();

                        if (parameters.Length == 0) {
                            action = (obj, context) => method.Invoke(obj, null);
                        }
                        else if (parameters.Length == 1) {
                            if (!parameters[0].ParameterType.IsAssignableFrom(typeof(NumericParams))) {
                                throw new ArgumentException(String.Format("method argument type must be NumericParams: {0}", method.Name));
                            }
                            action = (obj, context) => {
                                int?[] numericParams;
                                int[][] combinationParams;
                                context.GetNumericParams(out numericParams, out combinationParams);
                                method.Invoke(obj, new object[] { new NumericParams(numericParams, combinationParams) });
                            };
                        }
                        else {
                            throw new ArgumentException(String.Format("too many arguments: {0}", method.Name));
                        }
                        break;
                    }

                case EscapeSequenceParamType.Text: {
                        ParameterInfo[] parameters = method.GetParameters();
                        if (parameters.Length == 0) {
                            action = (obj, context) => method.Invoke(obj, null);
                        }
                        else if (parameters.Length == 1) {
                            if (!parameters[0].ParameterType.IsAssignableFrom(typeof(string))) {
                                throw new ArgumentException(String.Format("method argument type must be string: {0}", method.Name));
                            }
                            action = (obj, context) => {
                                string textParam = context.GetTextParam();
                                method.Invoke(obj, new object[] { textParam });
                            };
                        }
                        else {
                            throw new ArgumentException(String.Format("too many arguments: {0}", method.Name));
                        }
                        break;
                    }

                default:
                    throw new ArgumentException("invalid EscapeSequenceParamType");
            }

            root.Register(attribute, action);
        }
    }

    /// <summary>
    /// Escape sequence engine
    /// </summary>
    /// <typeparam name="T">class containing escape sequence handlers</typeparam>
    internal class EscapeSequenceEngine<T> : EscapeSequenceEngineBase where T : class {
        private static object _initializeSync = new object();
        private static bool _initialized = false;
        private static readonly CharState _root = new CharState();

        private readonly Context _context = new Context();
        private readonly Action<Exception, string> _exceptionHandler;
        private readonly Action<string> _incompleteHandler;
        private State _currentState;

#if UNITTEST
        /// <summary>
        /// Constructor
        /// </summary>
        public EscapeSequenceEngine()
            : this((e, s) => {
            }, (s) => {
            }) {
        }
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        public EscapeSequenceEngine(Action<Exception, string> exceptionHandler, Action<string> incompleteHandler) {
            lock (_initializeSync) {
                if (!_initialized) {
                    RegisterHandlers(_root, typeof(T));
                    _initialized = true;
                }
            }
            _currentState = _root;
            _exceptionHandler = exceptionHandler;
            _incompleteHandler = incompleteHandler;
        }

        /// <summary>
        /// Process a single input character.
        /// </summary>
        /// <param name="instance">instance of <typeparamref name="T"/></param>
        /// <param name="ch">input</param>
        /// <returns>true if <paramref name="ch"/> was handled. otherwise false.</returns>
        public bool Process(T instance, char ch) {
            State nextState = _currentState.Accept(_context, ch);
            if (nextState == null) {
                if (!Object.ReferenceEquals(_currentState, _root)) {
                    _context.AppendChar(ch); // report including a character not accepted
                    _incompleteHandler(_context.GetBufferedText());
                    Reset();
                }
                return false; // not handled
            }

            FinalState final = nextState as FinalState;
            if (final == null) {
                _currentState = nextState;
                return true; // handled
            }

            try {
                final.Action(instance, _context);
            }
            catch (Exception e) {
                var ie = e as TargetInvocationException;
                _exceptionHandler((ie != null) ? ie.InnerException : e, _context.GetBufferedText());
            }
            Reset();
            return true; // handled
        }

        /// <summary>
        /// Reset internal state.
        /// </summary>
        public void Reset() {
            _context.Reset();
            _currentState = _root;
        }

        /// <summary>
        /// Returns whether escape sequence is reading.
        /// </summary>
        public bool IsEscapeSequenceReading {
            get {
                return !Object.ReferenceEquals(_currentState, _root);
            }
        }

#if UNITTEST
        internal string[] DumpStates() {
            return _root.Dump();
        }
#endif
    }

    /// <summary>
    /// OSC parameters
    /// </summary>
    internal class OSCParams {
        private readonly int _ps;
        private readonly string _pt;
        private int _pos;

        private OSCParams(int ps, string pt, bool hasNextParam) {
            _ps = ps;
            _pt = pt;
            _pos = hasNextParam ? 0 : 1;
        }

        public int GetCode() {
            return _ps;
        }

        public string GetText() {
            return _pt;
        }

        public bool HasNextParam() {
            // _pos == _pt.Length : the next parameter is empty
            return _pos <= _pt.Length;
        }

        public bool TryGetNextInteger(out int value) {
            int start;
            int end;
            if (!TryGetNextParamRange(out start, out end)) {
                value = 0;
                return false;
            }

            int len = end - start;
            if (len > 0) {
                int v = 0;
                for (int i = start; i < end; i++) {
                    char ch = _pt[i];
                    if (ch >= '0' && ch <= '9') {
                        v = v * 10 + (ch - '0');
                    }
                    else {
                        value = 0;
                        return false;
                    }
                }

                value = v;
                return true;
            }
            else {
                value = 0;
                return false;
            }
        }

        public bool TryGetNextText(out string value) {
            int start;
            int end;
            if (!TryGetNextParamRange(out start, out end)) {
                value = null;
                return false;
            }

            value = _pt.Substring(start, end - start);
            return true;
        }

        private bool TryGetNextParamRange(out int start, out int end) {
            if (_pos > _pt.Length) {
                start = end = _pos;
                return false;
            }

            int s = _pos;
            int e = s;
            while (e < _pt.Length && _pt[e] != ';') {
                e++;
            }
            _pos = e + 1;
            // _pos == _pt.Length : Pt ends with a semicolon and the next parameter is empty
            // _pos  > _pt.Length : the last parameter has been consumed

            start = s;
            end = e;
            return true;
        }

        public static bool Parse(string p, out OSCParams oscParams) {
            int ps = 0;
            bool nonNumeric = false;
            int end = 0;
            while (end < p.Length && p[end] != ';') {
                char ch = p[end];
                if (ch >= '0' && ch <= '9') {
                    ps = ps * 10 + (ch - '0');
                }
                else {
                    nonNumeric = true;
                }
                end++;
            }

            if (end > 0 && !nonNumeric) {
                if (end >= p.Length) {
                    oscParams = new OSCParams(ps, "", false);
                }
                else {
                    // found semicolon
                    oscParams = new OSCParams(ps, p.Substring(end + 1), true);
                }
                return true;
            }

            oscParams = null;
            return false;
        }
    }

}
