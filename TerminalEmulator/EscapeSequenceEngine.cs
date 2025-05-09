// Copyright 2024-2025 The Poderosa Project.
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
using System.Threading;

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
        /// <summary>
        /// Text parameter for control strings
        /// </summary>
        /// <remarks>
        /// Accepts UTF-8 data.
        /// C1 control characters are treated as UTF-8 data.
        /// </remarks>
        ControlString,
        /// <summary>
        /// A next single printable character
        /// </summary>
        SinglePrintable,
    }

    /// <summary>
    /// Pattern matching context interface
    /// </summary>
    internal interface IEscapeSequenceContext {
        char[] GetSequence();
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
        public EscapeSequenceAttribute(char prefix, EscapeSequenceParamType paramType, params char[] suffix)
            : this(new char[] { prefix }, paramType, suffix) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p1">1st prefix character</param>
        /// <param name="p2">2nd prefix character</param>
        /// <param name="paramType">parameter type</param>
        /// <param name="suffix">suffix characters</param>
        public EscapeSequenceAttribute(char p1, char p2, EscapeSequenceParamType paramType, params char[] suffix)
            : this(new char[] { p1, p2 }, paramType, suffix) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p1">1st prefix character</param>
        /// <param name="p2">2nd prefix character</param>
        /// <param name="p3">3rd prefix character</param>
        /// <param name="paramType">parameter type</param>
        /// <param name="suffix">suffix characters</param>
        public EscapeSequenceAttribute(char p1, char p2, char p3, EscapeSequenceParamType paramType, params char[] suffix)
            : this(new char[] { p1, p2, p3 }, paramType, suffix) {
        }

        private EscapeSequenceAttribute(char[] prefix, EscapeSequenceParamType paramType, char[] suffix) {
            if (paramType == EscapeSequenceParamType.None || paramType == EscapeSequenceParamType.SinglePrintable) {
                if (suffix.Length != 0) {
                    throw new ArgumentException("suffix must be empty for EscapeSequenceParamType." + paramType.ToString());
                }
            }
            else {
                if (suffix.Length == 0) {
                    throw new ArgumentException("suffix must have at least one character for EscapeSequenceParamType." + paramType.ToString());
                }
            }

            Prefix = prefix;
            ParamType = paramType;
            Suffix = suffix;
        }
    }

    /// <summary>
    /// Numeric parameters
    /// </summary>
    internal class NumericParams {
        private readonly int?[] _p;
        private readonly int?[][] _c;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p">An array of single numeric parameters. If the parameter is not a single numeric parameter, the element is null.</param>
        /// <param name="c">An array of numeric parameters consisting of integer combinations.</param>
        public NumericParams(int?[] p, int?[][] c) {
            _p = p;
            _c = c;
        }

        public int Length {
            get {
                return _p.Length;
            }
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

        public int GetNonZero(int index, int defaultValue) {
            int val = Get(index, defaultValue);
            return (val == 0) ? defaultValue : val;
        }

        public int?[] GetIntegerCombination(int index) {
            if (index >= 0 && index < _c.Length) {
                int?[] sub = _c[index];
                return (sub != null) ? sub : new int?[0];
            }
            return new int?[0];
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

#if UNITTEST
        public int?[] GetNumericParametersForTesting() {
            return (int?[])_p.Clone();
        }

        public int?[][] GetIntegerCombinationsForTesting() {
            return _c.Select(sub => (sub != null) ? (int?[])sub.Clone() : null).ToArray();
        }
#endif
    }

    internal static class NumericParamsParser {
        /// <summary>
        /// Parse numeric parameters
        /// </summary>
        /// <param name="parameterText">parameter text</param>
        public static NumericParams Parse(string parameterText) {
            int paramCount = CountParams(parameterText);
            int?[] numericParams = new int?[paramCount];
            int?[][] combinationParams = new int?[paramCount][];
            int index = 0;
            int value = 0;
            int digits = 0;
            bool error = false;
            List<int?> combination = new List<int?>();
            foreach (char ch in parameterText) {
                switch (ch) {
                    case ';':
                        SetNumericParam(numericParams, combinationParams, index, value, digits, error, combination);
                        index++;
                        value = 0;
                        digits = 0;
                        error = false;
                        combination.Clear();
                        break;
                    case ':':
                        combination.Add((!error && digits > 0) ? value : (int?)null);
                        value = 0;
                        digits = 0;
                        error = false;
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

            SetNumericParam(numericParams, combinationParams, index, value, digits, error, combination);

            return new NumericParams(numericParams, combinationParams);
        }

        private static int CountParams(string p) {
            int sepCount = 0;
            foreach (char ch in p) {
                if (ch == ';') {
                    sepCount++;
                }
            }
            return sepCount + 1;
        }

        private static void SetNumericParam(int?[] numericParams, int?[][] combinationParams, int index, int value, int digits, bool error, List<int?> combination) {
            if (combination.Count > 0) {
                combination.Add((!error && digits > 0) ? value : (int?)null);
                combinationParams[index] = combination.ToArray();
            }
            else {
                numericParams[index] = (!error && digits > 0) ? value : (int?)null;
            }
        }
    }

    /// <summary>
    /// <para>Base class of <see cref="EscapeSequenceEngine{T}"/>.</para>
    /// <para>This class defines internal classes for the implementation of <see cref="EscapeSequenceEngine{T}"/>.</para>
    /// </summary>
    internal abstract class EscapeSequenceEngineBase {

        #region Context

        internal enum Introducer {
            APC,
            CSI,
            DCS,
            OSC,
            PM,
            SOS,
            Other,
        }

        /// <summary>
        /// Pattern matching context
        /// </summary>
        internal class Context : IEscapeSequenceContext {

            private readonly List<char> _buff = new List<char>(128);
            private int _paramStart = 0; // inclusive
            private int _paramEnd = 0; // exclusive

            private bool _ignoreMode = false;

            #region IEscapeSequenceContext

            /// <summary>
            /// Get sequence
            /// </summary>
            /// <returns>char sequence</returns>
            public char[] GetSequence() {
                int len = _buff.Count;
                char[] tmp = new char[len];
                if (tmp.Length > 0) {
                    _buff.CopyTo(tmp);
                }
                return tmp;
            }

            #endregion

            /// <summary>
            /// Reset
            /// </summary>
            public void Reset() {
                _buff.Clear();
                _paramStart = _paramEnd = 0;
                _ignoreMode = false;
            }

            /// <summary>
            /// Start ignore mode
            /// </summary>
            public void StartIgnoreMode() {
                _ignoreMode = true;
            }

            /// <summary>
            /// Whether the current sequence is in ignore mode
            /// </summary>
            public bool IsIgnoreMode {
                get {
                    return _ignoreMode;
                }
            }

            /// <summary>
            /// Get introducer type
            /// </summary>
            /// <returns>introducer type</returns>
            public Introducer GetIntroducer() {
                int len = _buff.Count;
                if (len >= 1) {
                    switch (_buff[0]) {
                        case ControlCode.APC:
                            return Introducer.APC;
                        case ControlCode.CSI:
                            return Introducer.CSI;
                        case ControlCode.DCS:
                            return Introducer.DCS;
                        case ControlCode.OSC:
                            return Introducer.OSC;
                        case ControlCode.PM:
                            return Introducer.PM;
                        case ControlCode.SOS:
                            return Introducer.SOS;
                        case ControlCode.ESC:
                            if (len >= 2) {
                                switch (_buff[1]) {
                                    case '_':
                                        return Introducer.APC;
                                    case '[':
                                        return Introducer.CSI;
                                    case 'P':
                                        return Introducer.DCS;
                                    case ']':
                                        return Introducer.OSC;
                                    case '^':
                                        return Introducer.PM;
                                    case 'X':
                                        return Introducer.SOS;
                                }
                            }
                            break;
                    }
                }
                return Introducer.Other;
            }

            /// <summary>
            /// Append a single character to buffer
            /// </summary>
            /// <param name="ch"></param>
            public void AppendChar(char ch) {
                _buff.Add(ch);
            }

            /// <summary>
            /// Append a single character to buffer as parameter
            /// </summary>
            /// <param name="ch"></param>
            public void AppendParamChar(char ch) {
                int index = _buff.Count;
                _buff.Add(ch);
                if (_paramStart == _paramEnd) {
                    _paramStart = index;
                }
                _paramEnd = index + 1;
            }

            /// <summary>
            /// Get buffered text converted to printable characters
            /// </summary>
            /// <returns>buffered text</returns>
            public string GetPrintableBufferedText() {
                return _buff
                        .Aggregate(
                            new StringBuilder(),
                            (s, c) => {
                                if (Char.IsControl(c) || Char.IsWhiteSpace(c)) {
                                    s.AppendFormat("<#{0:X2}>", (uint)c);
                                }
                                else {
                                    s.Append(c);
                                }
                                return s;
                            }
                        )
                        .ToString();
            }

            /// <summary>
            /// Get text parameter
            /// </summary>
            /// <returns>text parameter</returns>
            public string GetTextParam() {
                int len = _paramEnd - _paramStart;
                char[] tmp = new char[len];
                if (tmp.Length > 0) {
                    _buff.CopyTo(_paramStart, tmp, 0, len);
                }
                return new String(tmp);
            }

            /// <summary>
            /// Get a last character
            /// </summary>
            /// <returns>a last character</returns>
            public char GetLastChar() {
                return _buff[_buff.Count - 1];
            }

            /// <summary>
            /// Get numeric parameters
            /// </summary>
            /// <param name="numericParams">An array of single numeric parameters. If the parameter is not a single numeric parameter, the element is null.</param>
            /// <param name="combinationParams">An array of numeric parameters consisting of integer combinations.</param>
            public NumericParams GetNumericParams() {
                return NumericParamsParser.Parse(GetTextParam());
            }
        }

        #endregion

        #region States

        private static int _nextStateId = 1;

        /// <summary>
        /// State interface
        /// </summary>
        internal interface IState {
            int Id {
                get;
            }

            IState Accept(Context context, char ch);
        }

        /// <summary>
        /// Final state
        /// </summary>
        internal class FinalState : IState {
            public readonly Action<object, Context> Action;
            public int Id {
                get;
                private set;
            }

            public bool IsConsuming {
                get;
                private set;
            }

            public FinalState(Action<object, Context> action)
                : this(action, true) {
            }

            protected FinalState(Action<object, Context> action, bool consuming) {
                Action = action;
                Id = _nextStateId++;
                IsConsuming = consuming;
            }

            public IState Accept(Context context, char ch) {
                return null;
            }
        }

        /// <summary>
        /// Final state without consuming input char
        /// </summary>
        internal class NonConsumingFinalState : FinalState {
            public NonConsumingFinalState(Action<object, Context> action)
                : base(action, false) {
            }
        }

        /// <summary>
        /// Base state class that accepts one character and transitions to the next state.
        /// </summary>
        internal abstract class CharStateBase : IState {
            private const int CHAR_MAX = 0x9f;
            private readonly IState[] table = new IState[CHAR_MAX + 1];

            public int Id {
                get;
                private set;
            }

            protected CharStateBase() {
                Id = _nextStateId++;
            }

            public abstract IState Accept(Context context, char ch);

            protected IState GetNextState(char ch) {
                return (ch <= CHAR_MAX) ? table[ch] : null;
            }

            public void CopyTransitionFrom(CharStateBase reference, char ch) {
                // register the same state instance in reference
                RegisterStateInternal(ch, reference.table[ch]);

                // if the character is a special control character, alternative transiton would also registered in reference
                char alt1, alt2;
                if (To7bitPair(ch, out alt1, out alt2)) {
                    RegisterStateInternal(alt1, reference.table[alt1]);
                }
            }

            public T RegisterOrReuseState<T>(char ch) where T : CharStateBase, new() {
                // (this)---[ch]--->(T)
                T s = this.RegisterOrReuseStateCore<T>(ch);

                char alt1, alt2;
                if (To7bitPair(ch, out alt1, out alt2)) {
                    // (this)---------------[ch]----------------->(T)
                    //    |                                        ^
                    //    V                                        |
                    //     -----[alt1]-->(CharState)---[alt2]------
                    CharState altState = this.RegisterOrReuseStateCore<CharState>(alt1);
                    altState.RegisterStateInternal(alt2, s);
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

            private void RegisterStateInternal(char ch, IState state) {
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

            public void RegisterState(char ch, IState state) {
                // (this)---[ch]--->(State)
                this.RegisterStateInternal(ch, state);

                char alt1, alt2;
                if (To7bitPair(ch, out alt1, out alt2)) {
                    // (this)---------------[ch]----------------->(State)
                    //    |                                        ^
                    //    V                                        |
                    //     -----[alt1]-->(CharState)---[alt2]------
                    CharState altState = this.RegisterOrReuseStateCore<CharState>(alt1);
                    altState.RegisterStateInternal(alt2, state);
                }
            }

            public void RegisterStateIfNotSet(char ch, IState state) {
                if (ch > CHAR_MAX) {
                    throw new ArgumentException(String.Format("invalid character: u{0:x4}", (uint)ch));
                }

                if (table[ch] == null) {
                    RegisterState(ch, state);
                }
            }

            public bool HasNextState(char ch) {
                // this method is assumed to be used when constructing the state machine
                if (ch > CHAR_MAX) {
                    throw new ArgumentException(String.Format("invalid character: u{0:x4}", (uint)ch));
                }

                return table[ch] != null;
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

            public override IState Accept(Context context, char ch) {
                IState s = GetNextState(ch);
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

            public override IState Accept(Context context, char ch) {
                IState s = GetNextState(ch);
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

            public override IState Accept(Context context, char ch) {
                IState s = GetNextState(ch);
                if (s != null) {
                    context.AppendChar(ch);
                    return s;
                }

                if ((ch >= 0x20 && ch <= 0x7e) || (ch >= 0x08 && ch <= 0x0d)) {
                    context.AppendParamChar(ch);
                    return this;
                }

                return null;
            }
        }

        /// <summary>
        /// Same as <see cref="TextParamState"/>, but assumed to be used for control string.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Xterm accepts UTF-8 text as control string data.
        /// As a result, the C1 control character cannot be used as the terminal character.
        /// </para>
        /// <para>
        /// This state does not treat the C1 control character as a terminal character, whereas TextParamState does.
        /// </para>
        /// </remarks>
        internal class ControlStringTextParamState : TextParamState {

            // Note: The base transition table contains only terminating characters.

            public override IState Accept(Context context, char ch) {
                if (ch >= 0x80 && ch <= 0xf4) {
                    // accept as UTF-8 data
                    context.AppendParamChar(ch);
                    return this;
                }

                return base.Accept(context, ch);
            }
        }

        /// <summary>
        /// Special state that reads CSI to the end and ignores it
        /// </summary>
        internal class IgnoreCSIState : IState {

            private static readonly FinalState _final = new FinalState((obj, context) => {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("Ignore CSI: {0}", new object[] { context.GetPrintableBufferedText() });
#endif
            });

            public int Id {
                get;
                private set;
            }

            public IgnoreCSIState() {
                Id = _nextStateId++;
            }

            public IState Accept(Context context, char ch) {
                if (ch >= 0x40 && ch <= 0x7e) {
                    // final byte
                    context.AppendChar(ch);
                    return _final;
                }

                if (ch >= 0x20 && ch <= 0x3f) {
                    // parameter bytes or intermediate bytes
                    context.AppendChar(ch);
                    return this;
                }

                return null;
            }
        }

        /// <summary>
        /// Special state that reads control string (APC, DCS, OSC, PM and SOS) to the end and ignores it
        /// </summary>
        /// <remarks>
        /// As Xterm does, this state accepts UTF-8 text as control string data.
        /// As a result, ST or the C1 control character cannot be used as the terminal character.
        /// </remarks>
        internal class IgnoreControlStringState : CharStateBase {

            private IgnoreControlStringState() {
            }

            public static IgnoreControlStringState BuildForAPC() {
                return Build("APC", false);
            }

            public static IgnoreControlStringState BuildForDCS() {
                return Build("DCS", false);
            }

            public static IgnoreControlStringState BuildForOSC() {
                return Build("OSC", true);
            }

            public static IgnoreControlStringState BuildForPM() {
                return Build("PM", false);
            }

            public static IgnoreControlStringState BuildForSOS() {
                return Build("SOS", false);
            }

            private static IgnoreControlStringState Build(string introducer, bool terminateByBEL) {
                FinalState finalState = new FinalState((obj, context) => {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Ignore " + introducer);
#endif
                });

                NonConsumingFinalState nonConsumingFinalState = new NonConsumingFinalState((obj, context) => {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Ignore " + introducer + " without consuming last character");
#endif
                });

                IgnoreControlStringState s = new IgnoreControlStringState();

                for (int c = 0x08; c <= 0x0d; c++) {
                    s.RegisterState((char)c, s);
                }

                for (int c = 0x20; c <= 0x7e; c++) {
                    s.RegisterState((char)c, s);
                }

                s.RegisterState(ControlCode.CAN, finalState);
                s.RegisterState(ControlCode.SUB, finalState);
                if (terminateByBEL) {
                    s.RegisterState(ControlCode.BEL, finalState);
                }

                // s.RegisterState(ControlCode.ST, finalState);
                // Note:
                //  Formally, ST(0x9c) or ESC-backslash terminates the control string.
                //  However, in this implementation, ST is treated as a normal character to accept Unicode characters.
                //  In addition, it is recommended to terminate the control string with ESC for backward compatibility.
                s.RegisterState(ControlCode.ESC, nonConsumingFinalState);

                return s;
            }

            public override IState Accept(Context context, char ch) {
                // control string can be very long, so data is not stored in the buffer

                if (ch >= 0x80 && ch <= 0xf4) {
                    // accept as UTF-8 data
                    return this;
                }

                IState s = GetNextState(ch);
                if (s != null) {
                    return s;
                }

                return null;
            }
        }

        #endregion

        #region Thread-local context

        // Context temporarily stored in thread-local during the escape sequence handler being called
        protected static readonly ThreadLocal<Context> _currentThreadContext = new ThreadLocal<Context>();

        /// <summary>
        /// Get the escape sequence captured in the current thread.
        /// </summary>
        /// <remarks>
        /// <para>This method is used to obtain the captured escape sequence when an EscapeSequenceHandlerException is thrown.</para>
        /// </remarks>
        /// <returns>escape sequence</returns>
        public static char[] GetCurrentThreadEscapeSequence() {
            return (_currentThreadContext.Value != null) ? _currentThreadContext.Value.GetSequence() : null;
        }

        #endregion

        #region State Machine Builder

        internal class StateMachineBuilder {

            private readonly CharState _root;
            private readonly List<Tuple<CharState, FinalState>> _singlePrintableStates = new List<Tuple<CharState, FinalState>>();

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="root">root state</param>
            public StateMachineBuilder(CharState root) {
                _root = root;
            }

            /// <summary>
            /// Register escape sequence handlers marked with EscapeSequenceAttribute.
            /// </summary>
            /// <param name="type">class containing escape sequence handlers</param>
            public StateMachineBuilder RegisterHandlers(Type type) {
                return RegisterHandlers(type, (method) => method.GetCustomAttributes<EscapeSequenceAttribute>());
            }

            /// <summary>
            /// Register escape sequence handlers marked with EscapeSequenceAttribute.
            /// </summary>
            /// <param name="type">class containing escape sequence handlers</param>
            /// <param name="getAttributes">function to get Attributes from MethodInfo</param>
            public StateMachineBuilder RegisterHandlers(Type type, Func<MethodInfo, IEnumerable<EscapeSequenceAttribute>> getAttributes) {
                List<EscapeSequenceAttribute> attrsParamTypeNone = new List<EscapeSequenceAttribute>();
                List<EscapeSequenceAttribute> attrsParamTypeNumeric = new List<EscapeSequenceAttribute>();
                List<EscapeSequenceAttribute> attrsParamTypeText = new List<EscapeSequenceAttribute>();
                List<EscapeSequenceAttribute> attrsParamTypeSinglePrintable = new List<EscapeSequenceAttribute>();

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                    attrsParamTypeNone.Clear();
                    attrsParamTypeNumeric.Clear();
                    attrsParamTypeText.Clear();
                    attrsParamTypeSinglePrintable.Clear();

                    foreach (EscapeSequenceAttribute attr in getAttributes(method)) {
                        switch (attr.ParamType) {
                            case EscapeSequenceParamType.None:
                                attrsParamTypeNone.Add(attr);
                                break;
                            case EscapeSequenceParamType.Numeric:
                                attrsParamTypeNumeric.Add(attr);
                                break;
                            case EscapeSequenceParamType.Text:
                            case EscapeSequenceParamType.ControlString:
                                attrsParamTypeText.Add(attr);
                                break;
                            case EscapeSequenceParamType.SinglePrintable:
                                attrsParamTypeSinglePrintable.Add(attr);
                                break;
                            default:
                                throw new ArgumentException("invalid EscapeSequenceParamType");
                        }
                    }

                    if (attrsParamTypeNone.Count > 0) {
                        RegisterParamTypeNoneHandlers(attrsParamTypeNone, method);
                    }

                    if (attrsParamTypeNumeric.Count > 0) {
                        RegisterParamTypeNumericHandlers(attrsParamTypeNumeric, method);
                    }

                    if (attrsParamTypeText.Count > 0) {
                        RegisterParamTypeTextHandlers(attrsParamTypeText, method);
                    }

                    if (attrsParamTypeSinglePrintable.Count > 0) {
                        RegisterParamTypeSinglePrintableHandlers(attrsParamTypeSinglePrintable, method);
                    }
                }

                // set final state for SinglePrintable parameter
                foreach (var t in _singlePrintableStates) {
                    CharState s = t.Item1;
                    FinalState final = t.Item2;
                    for (char ch = '\u0021'; ch <= '\u007e'; ch++) {
                        s.RegisterStateIfNotSet(ch, final);
                    }
                }

                return this;
            }

            private void RegisterParamTypeNoneHandlers(IEnumerable<EscapeSequenceAttribute> attributes, MethodInfo method) {
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != 0) {
                    throw new ArgumentException(String.Format("method must have no arguments: {0}", method.Name));
                }

                Action<object, Context> action = (obj, context) => method.Invoke(obj, null);

                foreach (EscapeSequenceAttribute attr in attributes) {
                    RegisterHandlerCore(attr, action);
                }
            }

            private void RegisterParamTypeNumericHandlers(IEnumerable<EscapeSequenceAttribute> attributes, MethodInfo method) {
                ParameterInfo[] parameters = method.GetParameters();

                Action<object, Context> action;

                if (parameters.Length == 0) {
                    action = (obj, context) => method.Invoke(obj, null);
                }
                else if (parameters.Length == 1) {
                    if (!parameters[0].ParameterType.IsAssignableFrom(typeof(NumericParams))) {
                        throw new ArgumentException(String.Format("method argument type must be NumericParams: {0}", method.Name));
                    }
                    action = (obj, context) => {
                        NumericParams numericParams = context.GetNumericParams();
                        method.Invoke(obj, new object[] { numericParams });
                    };
                }
                else {
                    throw new ArgumentException(String.Format("too many arguments: {0}", method.Name));
                }

                foreach (EscapeSequenceAttribute attr in attributes) {
                    RegisterHandlerCore(attr, action);
                }
            }

            private void RegisterParamTypeTextHandlers(IEnumerable<EscapeSequenceAttribute> attributes, MethodInfo method) {
                ParameterInfo[] parameters = method.GetParameters();

                Action<object, Context> action;

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

                foreach (EscapeSequenceAttribute attr in attributes) {
                    RegisterHandlerCore(attr, action);
                }
            }

            private void RegisterParamTypeSinglePrintableHandlers(IEnumerable<EscapeSequenceAttribute> attributes, MethodInfo method) {
                ParameterInfo[] parameters = method.GetParameters();

                Action<object, Context> action;

                if (parameters.Length == 0) {
                    action = (obj, context) => method.Invoke(obj, null);
                }
                else if (parameters.Length == 1) {
                    if (!parameters[0].ParameterType.IsAssignableFrom(typeof(char))) {
                        throw new ArgumentException(String.Format("method argument type must be char: {0}", method.Name));
                    }
                    action = (obj, context) => {
                        char paramChar = context.GetLastChar();
                        method.Invoke(obj, new object[] { paramChar });
                    };
                }
                else {
                    throw new ArgumentException(String.Format("too many arguments: {0}", method.Name));
                }

                foreach (EscapeSequenceAttribute attr in attributes) {
                    RegisterHandlerCore(attr, action);
                }
            }

            private void RegisterHandlerCore(EscapeSequenceAttribute attr, Action<object, Context> action) {
                FinalState final = new FinalState(action);
                RegisterHandlerCore(attr, final);
            }

            private void RegisterHandlerCore(EscapeSequenceAttribute attr, FinalState final) {
                int prefixLen = attr.Prefix.Length;
                if (prefixLen == 0) {
                    throw new ArgumentException("missing prefix");
                }

                CharStateBase s = _root;
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
                else if (attr.ParamType == EscapeSequenceParamType.ControlString) {
                    s = s.RegisterOrReuseState<ControlStringTextParamState>(attr.Prefix[prefixLen - 1]);
                }
                else if (attr.ParamType == EscapeSequenceParamType.SinglePrintable) {
                    CharState charState = s.RegisterOrReuseState<CharState>(attr.Prefix[prefixLen - 1]);
                    if (_singlePrintableStates.Select(t => t.Item1).Where(st => Object.ReferenceEquals(st, charState)).Any()) {
                        throw new ArgumentException("conflict SinglePrintable");
                    }
                    _singlePrintableStates.Add(Tuple.Create(charState, final)); // set final state later
                    return;
                }
                else /* if (attr.ParamType == EscapeSequenceParamType.None) */ {
                    s.RegisterState(attr.Prefix[prefixLen - 1], final);
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

                s.RegisterState(attr.Suffix[suffixLen - 1], final);
                if (s2 != null) {
                    s2.CopyTransitionFrom(s, attr.Suffix[suffixLen - 1]);
                }
            }

            public StateMachineBuilder RegisterMissingHandlers() {
                // IgnoreControlStringState requires at least one handler that handles introducer

                NonConsumingFinalState final = new NonConsumingFinalState((obj, context) => {
                });

                if (!_root.HasNextState(ControlCode.APC)) {
                    RegisterHandlerCore(new EscapeSequenceAttribute(ControlCode.APC, ControlCode.ESC), final);
                }

                if (!_root.HasNextState(ControlCode.DCS)) {
                    RegisterHandlerCore(new EscapeSequenceAttribute(ControlCode.DCS, ControlCode.ESC), final);
                }

                if (!_root.HasNextState(ControlCode.OSC)) {
                    RegisterHandlerCore(new EscapeSequenceAttribute(ControlCode.OSC, ControlCode.ESC), final);
                }

                if (!_root.HasNextState(ControlCode.PM)) {
                    RegisterHandlerCore(new EscapeSequenceAttribute(ControlCode.PM, ControlCode.ESC), final);
                }

                if (!_root.HasNextState(ControlCode.SOS)) {
                    RegisterHandlerCore(new EscapeSequenceAttribute(ControlCode.SOS, ControlCode.ESC), final);
                }

                return this;
            }
        }

        #endregion
    }

    /// <summary>
    /// Escape sequence engine
    /// </summary>
    /// <typeparam name="T">class containing escape sequence handlers</typeparam>
    internal class EscapeSequenceEngine<T> : EscapeSequenceEngineBase where T : class {
        private static object _initializeSync = new object();
        private static bool _initialized = false;
        private static readonly CharState _root = new CharState();
        private static readonly IgnoreCSIState _ignoreCSIState = new IgnoreCSIState();
        private static readonly IgnoreControlStringState _ignoreAPCState = IgnoreControlStringState.BuildForAPC();
        private static readonly IgnoreControlStringState _ignoreDCSState = IgnoreControlStringState.BuildForDCS();
        private static readonly IgnoreControlStringState _ignoreOSCState = IgnoreControlStringState.BuildForOSC();
        private static readonly IgnoreControlStringState _ignorePMState = IgnoreControlStringState.BuildForPM();
        private static readonly IgnoreControlStringState _ignoreSOSState = IgnoreControlStringState.BuildForSOS();

        private readonly Context _context = new Context();
        private readonly Action<Exception, IEscapeSequenceContext> _exceptionHandler;
        private readonly Action<IEscapeSequenceContext> _incompleteHandler;
        private readonly Action<IEscapeSequenceContext> _completedHandler;
        private IState _currentState;

        private IDCSProcessor _dcsProcessor = null;

#if UNITTEST
        /// <summary>
        /// Constructor
        /// </summary>
        public EscapeSequenceEngine()
            : this(
            (c) => {
            },
            (c) => {
            },
            (e, s) => {
            }
            ) {
        }
#endif

        /// <summary>
        /// Constructor
        /// </summary>
        public EscapeSequenceEngine(
                Action<IEscapeSequenceContext> completedHandler,
                Action<IEscapeSequenceContext> incompleteHandler,
                Action<Exception, IEscapeSequenceContext> exceptionHandler
        ) {
            BuildStateMachine();
            _currentState = _root;
            _completedHandler = completedHandler;
            _incompleteHandler = incompleteHandler;
            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Build global instance of the state machine
        /// </summary>
        public static void BuildStateMachine() {
            lock (_initializeSync) {
                if (!_initialized) {
#if DEBUG
                    long before = GC.GetTotalMemory(true);
#endif
                    new StateMachineBuilder(_root)
                        .RegisterHandlers(typeof(T))
                        .RegisterMissingHandlers();
#if DEBUG
                    long after = GC.GetTotalMemory(true);
#endif
                    _initialized = true;

#if DEBUG
                    System.Diagnostics.Debug.WriteLine("Size of the escape sequence state table : {0} bytes", after - before);
#endif
                }
            }
        }

        /// <summary>
        /// Process a single input character.
        /// </summary>
        /// <param name="instance">instance of <typeparamref name="T"/></param>
        /// <param name="ch">input</param>
        /// <param name="noRecursive">if true, recursive processing of <paramref name="ch"/> is not allowed</param>
        /// <returns>true if <paramref name="ch"/> was handled. otherwise false.</returns>
        public bool Process(T instance, char ch, bool noRecursive = false) {
            if (_dcsProcessor != null) {
                DCSProcessCharResult dcsResult = _dcsProcessor.ProcessChar(ch);
                switch (dcsResult) {
                    case DCSProcessCharResult.Consumed:
                        return true;
                    case DCSProcessCharResult.Finished:
                        _dcsProcessor = null;
                        return true;
                    case DCSProcessCharResult.Aborted:
                        _dcsProcessor = null;
                        goto PROC_CHAR;
                    case DCSProcessCharResult.Invalid:
                        break;
                }
                _dcsProcessor = null;
                return false;
            }

        PROC_CHAR:

            IState nextState = _currentState.Accept(_context, ch);

            if (nextState == null) {
                if (Object.ReferenceEquals(_currentState, _root)) {
                    return false; // not handled
                }

                // check interrupting control characters
                if ((ch >= 0 && ch <= 0x1f) || ch == 0x7f || ch == 0xff) {
                    // execute immediately if the action was registered for the control character.
                    // _currentState and _context are retained.
                    Context context = new Context();
                    FinalState intFinal = _root.Accept(context, ch) as FinalState;
                    if (intFinal != null) {
                        CallAction(instance, context, intFinal);
                        return true; // handled
                    }
                }

                if (!_context.IsIgnoreMode) {
                    switch (_context.GetIntroducer()) {
                        case Introducer.APC:
                            // handle unsupported APC
                            _context.StartIgnoreMode();
                            nextState = _ignoreAPCState.Accept(_context, ch);
                            if (nextState != null) {
                                goto CheckFinalState;
                            }
                            break;
                        case Introducer.CSI:
                            // handle unsupported CSI
                            _context.StartIgnoreMode();
                            nextState = _ignoreCSIState.Accept(_context, ch);
                            if (nextState != null) {
                                goto CheckFinalState;
                            }
                            break;
                        case Introducer.DCS:
                            // handle unsupported DCS
                            _context.StartIgnoreMode();
                            nextState = _ignoreDCSState.Accept(_context, ch);
                            if (nextState != null) {
                                goto CheckFinalState;
                            }
                            break;
                        case Introducer.OSC:
                            // handle unsupported OSC
                            _context.StartIgnoreMode();
                            nextState = _ignoreOSCState.Accept(_context, ch);
                            if (nextState != null) {
                                goto CheckFinalState;
                            }
                            break;
                        case Introducer.PM:
                            // handle unsupported PM
                            _context.StartIgnoreMode();
                            nextState = _ignorePMState.Accept(_context, ch);
                            if (nextState != null) {
                                goto CheckFinalState;
                            }
                            break;
                        case Introducer.SOS:
                            // handle unsupported SOS
                            _context.StartIgnoreMode();
                            nextState = _ignoreSOSState.Accept(_context, ch);
                            if (nextState != null) {
                                goto CheckFinalState;
                            }
                            break;
                    }
                }

                // abort current escape sequence
                _incompleteHandler(_context);
                ResetState(); // _currentState is also reset here

                // restart
                nextState = Volatile.Read(ref _currentState).Accept(_context, ch);
                if (nextState == null) {
                    return false; // not handled
                }
            }

        CheckFinalState:
            FinalState final = nextState as FinalState;
            if (final == null) {
                _currentState = nextState;
                return true; // handled
            }

            CallAction(instance, _context, final);

            ResetState();

            if (!noRecursive && !final.IsConsuming) {
                return Process(instance, ch, true);
            }
            return true; // handled
        }

        private void CallAction(T instance, Context context, FinalState final) {
            _completedHandler(context);

            _currentThreadContext.Value = context;
            try {
                final.Action(instance, context);
            }
            catch (Exception e) {
                var ie = e as TargetInvocationException;
                _exceptionHandler((ie != null) ? ie.InnerException : e, context);
            }
            finally {
                _currentThreadContext.Value = null;
            }
        }

        /// <summary>
        /// Reset internal state.
        /// </summary>
        private void ResetState() {
            _context.Reset();
            _currentState = _root;
        }

        /// <summary>
        /// Full reset internal state.
        /// </summary>
        public void Reset() {
            _dcsProcessor = null;
            ResetState();
        }

        /// <summary>
        /// Returns whether escape sequence is reading.
        /// </summary>
        public bool IsEscapeSequenceReading {
            get {
                return _dcsProcessor != null || !Object.ReferenceEquals(_currentState, _root);
            }
        }

        /// <summary>
        /// Start DCS processor
        /// </summary>
        /// <param name="dcsProcessor">DCS processor</param>
        public void StartDCSProcessor(IDCSProcessor dcsProcessor) {
            _dcsProcessor = dcsProcessor;
        }

#if UNITTEST
        internal string[] DumpStates() {
            return _root.Dump();
        }
#endif
    }

    internal class EscapeSequenceHandlerException : Exception {

        public EscapeSequenceHandlerException(string message)
            : this(message, EscapeSequenceEngineBase.GetCurrentThreadEscapeSequence()) {
        }

        public EscapeSequenceHandlerException(string message, char[] escapeSequence)
            : base(EscapeSequenceErrorUtil.FormatMessage(message, escapeSequence)) {
        }

    }

    internal static class EscapeSequenceErrorUtil {

        public static string FormatMessage(string message, char[] escapeSequence) {
            if (escapeSequence == null) {
                return message;
            }

            StringBuilder s = new StringBuilder();
            s.Append(message).Append(" : ");

            foreach (char ch in escapeSequence) {
                string controlName = ControlCode.ToName(ch);
                if (controlName != null) {
                    s.Append('<').Append(controlName).Append('>');
                }
                else if (ch < 0x20 || ch > 0x7e) {
                    s.AppendFormat("<#{0:X2}>", (int)ch);
                }
                else {
                    s.Append(ch);
                }
            }
            return s.ToString();
        }
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

    internal enum DCSProcessCharResult {
        /// <summary>
        /// The input character has been consumed.
        /// </summary>
        Consumed,
        /// <summary>
        /// DCS has been finidhed. The input character has been consumed.
        /// </summary>
        Finished,
        /// <summary>
        /// DCS has been aborted. The input character has not been consumed. Another escape sequence may have been started.
        /// </summary>
        Aborted,
        /// <summary>
        /// The input character is invalid for DCS.
        /// </summary>
        Invalid,
    }

    internal interface IDCSProcessor {
        DCSProcessCharResult ProcessChar(char ch);
    }

    internal abstract class DCSProcessorBase : IDCSProcessor {

        protected abstract void Input(char ch);
        protected abstract void Finish();
        protected abstract void Cancel();

        private enum Status {
            Normal,
            Finished,
        }

        private enum CharType : byte {
            None = 0,
            StringChar = 1,
            Terminator = 2,
            CancelTerminator = 3,
            SemiTerminator = 4,
        }

        private static readonly CharType[] _charTable;

        static DCSProcessorBase() {
            _charTable = new CharType[256];

            for (int i = 0x08; i <= 0x0d; i++) {
                _charTable[i] = CharType.StringChar;
            }

            for (int i = 0x20; i <= 0x7e; i++) {
                _charTable[i] = CharType.StringChar;
            }

            // To accept UTF-8 text, ST and C1 controls are treated as the normal character.
            // See also IgnoreControlStringState.
            for (int i = 0x80; i <= 0xf4; i++) {
                _charTable[i] = CharType.StringChar;
            }

            _charTable[0x1b] = CharType.SemiTerminator; // ESC

            _charTable[0x18] = CharType.CancelTerminator; // CAN
            _charTable[0x1a] = CharType.CancelTerminator; // SUB
        }

        private Status _status = Status.Normal;

        public DCSProcessCharResult ProcessChar(char ch) {
            switch (_status) {
                case Status.Normal:
                    if ((int)ch < _charTable.Length) {
                        switch (_charTable[(int)ch]) {
                            case CharType.StringChar:
                                Input(ch);
                                return DCSProcessCharResult.Consumed;

                            case CharType.Terminator:
                                Finish();
                                _status = Status.Finished;
                                return DCSProcessCharResult.Finished;

                            case CharType.CancelTerminator:
                                Cancel();
                                _status = Status.Finished;
                                return DCSProcessCharResult.Finished;

                            case CharType.SemiTerminator:
                                Finish();
                                _status = Status.Finished;
                                return DCSProcessCharResult.Aborted;
                        }
                    }

                    Cancel();
                    _status = Status.Finished;
                    return DCSProcessCharResult.Invalid;

                case Status.Finished:
                default:
                    return DCSProcessCharResult.Invalid;
            }
        }
    }

    /// <summary>
    /// DCS processor that buffers subsequent data.
    /// </summary>
    /// <remarks>
    /// The callback is called only once after the control string has been successfully completed.
    /// </remarks>
    internal class BufferedDCSProcessor : DCSProcessorBase {

        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly Action<string> _callback;
        private readonly int _maxBufferLength;
        private bool _error = false;

        public BufferedDCSProcessor(Action<string> callback)
            : this(-1, callback) {
        }

        public BufferedDCSProcessor(int maxBufferLength, Action<string> callback) {
            _callback = callback;
            _maxBufferLength = maxBufferLength;
        }

        protected override void Input(char ch) {
            if (!_error) {
                _buffer.Append(ch);

                if (_maxBufferLength >= 0 && _buffer.Length > _maxBufferLength) {
                    _error = true;
                }
            }
        }

        protected override void Finish() {
            if (!_error) {
                try {
                    _callback(_buffer.ToString());
                }
                catch (Exception ex) {
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                    System.Diagnostics.Debug.WriteLine(ex.StackTrace);
                }
            }
        }

        protected override void Cancel() {
        }
    }

    /// <summary>
    /// DCS processor that chunks subsequent data and calls a callback for each chunk.
    /// </summary>
    internal class ChunkedDCSProcessor : DCSProcessorBase {

        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly Action<string> _callback;

        private const int CHUNK_SIZE = 256;

        public ChunkedDCSProcessor(Action<string> callback) {
            _callback = callback;
        }

        protected override void Input(char ch) {
            _buffer.Append(ch);

            if (_buffer.Length >= CHUNK_SIZE) {
                Flush();
            }
        }

        protected override void Finish() {
            if (_buffer.Length > 0) {
                Flush();
            }
        }

        protected override void Cancel() {
            Finish();
        }

        private void Flush() {
            try {
                _callback(_buffer.ToString());
            }
            catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.StackTrace);
            }
            _buffer.Clear();
        }
    }
}
