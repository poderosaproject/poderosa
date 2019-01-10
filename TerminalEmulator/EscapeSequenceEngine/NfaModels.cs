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
    /// <summary>
    /// Interface of a NFA state
    /// </summary>
    internal interface INfaState {
        int ID {
            get;
        }
        List<INfaTransition> Transitions {
            get;
        }
        string Description {
            get;
        }
        bool IsSameType(INfaState other);
    }

    /// <summary>
    /// Interface of a transition
    /// </summary>
    internal interface INfaTransition {
        byte[] Matches {
            get;
        }
        INfaState NextState {
            get;
        }
        string Description {
            get;
        }
        bool IsIdenticalWith(INfaTransition other);
        INfaTransition CopyWithNewNextState(INfaState nextState);
    }

    /// <summary>
    /// Base implementation of <see cref="INfaTransition"/>
    /// </summary>
    internal abstract class NfaTransitionBase : INfaTransition {
        public byte[] Matches {
            get;
            private set;
        }

        public INfaState NextState {
            get;
            private set;
        }

        public virtual string Description {
            get {
                return "[" + String.Join("", this.Matches.Select(b => {
                    if (b >= 0x21 && b <= 0x7e) {
                        return Char.ToString((char)b);
                    }
                    else {
                        return String.Format("\\x{0:x2}", b);
                    }
                })) + "]";
            }
        }

        protected NfaTransitionBase(byte[] matches, INfaState next) {
            this.Matches = matches;
            this.NextState = next;
        }

        public virtual bool IsIdenticalWith(INfaTransition other) {
            return Enumerable.SequenceEqual(this.Matches, other.Matches);
        }

        public abstract INfaTransition CopyWithNewNextState(INfaState nextState);
    }

    /// <summary>
    /// Standard transition
    /// </summary>
    internal class NfaTransition : NfaTransitionBase {
        public NfaTransition(byte[] matches, INfaState next)
            : base(matches, next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaTransition && base.IsIdenticalWith(other);
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaTransition((byte[])this.Matches.Clone(), nextState);
        }
    }

    /// <summary>
    /// Transition by digits
    /// </summary>
    internal class NfaDigitTransition : NfaTransition {
        public NfaDigitTransition(INfaState next)
            : base(new byte[] { 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, }, next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaDigitTransition; // no need to call NfaTransition.IsIdenticalWith()
        }

        public override string Description {
            get {
                return "(Digit)";
            }
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaDigitTransition(nextState);
        }
    }

    /// <summary>
    /// Transition by printable characters
    /// </summary>
    /// <remarks>
    /// "Printable characters" refer characters can be specified in APC, DCS, OSC, or PM, ending by STRING TERMINATOR (ST).
    /// In ECMA-48, they are "bit combinations in the range 00/08 to 00/13 and 02/00 to 07/14."
    /// </remarks>
    internal class NfaPrintableTransition : NfaTransition {
        public NfaPrintableTransition(INfaState next)
            : base(
                new byte[] {
                    // 0x08 - 0x0d
                    0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d,
                    // 0x20 - 0x7e
                    0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27,
                    0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d, 0x2e, 0x2f,
                    0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37,
                    0x38, 0x39, 0x3a, 0x3b, 0x3c, 0x3d, 0x3e, 0x3f,
                    0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47,
                    0x48, 0x49, 0x4a, 0x4b, 0x4c, 0x4d, 0x4e, 0x4f,
                    0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57,
                    0x58, 0x59, 0x5a, 0x5b, 0x5c, 0x5d, 0x5e, 0x5f,
                    0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67,
                    0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f,
                    0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77,
                    0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e,
                },
                next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaPrintableTransition && base.IsIdenticalWith(other);
        }

        public override string Description {
            get {
                return "(Printable)";
            }
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaPrintableTransition(nextState);
        }
    }

    /// <summary>
    /// Transition by any characters except SOS and ST.
    /// </summary>
    internal class NfaAnyCharStringTransition : NfaTransition {
        public NfaAnyCharStringTransition(INfaState next)
            : base(GetCharacters(), next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaAnyCharStringTransition && base.IsIdenticalWith(other);
        }

        public override string Description {
            get {
                return "(AnyChar)";
            }
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaAnyCharStringTransition(nextState);
        }

        private static byte[] GetCharacters() {
            byte[] table = new byte[254];
            int index = 0;
            for (int b = 0; b < 0x98 /*SOS*/; b++) {
                table[index++] = (byte)b;
            }
            for (int b = 0x99; b < 0x9c /*ST*/; b++) {
                table[index++] = (byte)b;
            }
            for (int b = 0x9d; b < 0x100; b++) {
                table[index++] = (byte)b;
            }
            return table;
        }
    }

    /// <summary>
    /// Transition by semicolon
    /// </summary>
    internal class NfaSemicolonTransition : NfaTransition {
        public NfaSemicolonTransition(INfaState next)
            : base(new byte[] { 0x3b }, next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaSemicolonTransition; // no need to call NfaTransition.IsIdenticalWith()
        }

        public override string Description {
            get {
                return base.Description;
            }
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaSemicolonTransition(nextState);
        }
    }

    /// <summary>
    /// Transition with adding a new empty numerical parameter.
    /// </summary>
    internal class NfaEmptyNumericalParamTransition : NfaTransitionBase {
        public NfaEmptyNumericalParamTransition(byte[] matches, INfaState next)
            : base(matches, next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaEmptyNumericalParamTransition && base.IsIdenticalWith(other);
        }

        public override string Description {
            get {
                return "EmptyN:" + base.Description;
            }
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaEmptyNumericalParamTransition((byte[])this.Matches.Clone(), nextState);
        }
    }

    /// <summary>
    /// Transition with adding a new empty text parameter.
    /// </summary>
    internal class NfaEmptyTextParamTransition : NfaTransitionBase {
        public NfaEmptyTextParamTransition(byte[] matches, INfaState next)
            : base(matches, next) {
        }

        public override bool IsIdenticalWith(INfaTransition other) {
            return other is NfaEmptyTextParamTransition && base.IsIdenticalWith(other);
        }

        public override string Description {
            get {
                return "EmptyT:" + base.Description;
            }
        }

        public override INfaTransition CopyWithNewNextState(INfaState nextState) {
            return new NfaEmptyTextParamTransition((byte[])this.Matches.Clone(), nextState);
        }
    }

    /// <summary>
    /// NFA state.
    /// </summary>
    internal abstract class NfaStateBase : INfaState {
        public int ID {
            get;
            private set;
        }

        public List<INfaTransition> Transitions {
            get;
            private set;
        }

        public abstract string Description {
            get;
        }

        public abstract bool IsSameType(INfaState other);

        public NfaStateBase(int id) {
            if (id == 0) {
                throw new ArgumentException("id must not be zero.", "id");
            }

            this.ID = id;
            this.Transitions = new List<INfaTransition>();
        }
    }

    /// <summary>
    /// Standard state.
    /// </summary>
    internal class NfaState : NfaStateBase {
        public NfaState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaState;
        }
    }

    /// <summary>
    /// Final state.
    /// </summary>
    internal class NfaFinalState : NfaStateBase {
        public string Pattern {
            get;
            private set;
        }

        public NfaFinalState(int id, string pattern)
            : base(id) {
            this.Pattern = pattern;
        }

        public override string Description {
            get {
                return "Final:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return Object.ReferenceEquals(this, other);
        }
    }

    /// <summary>
    /// Represents a certain number of numerical parameters. (([0-9]*)(;([0-9]*)){n-2}(;([0-9]*))?)
    /// </summary>
    internal class NfaNNumericalParamsState : NfaStateBase {
        public readonly int Number;

        public NfaNNumericalParamsState(int id, int number)
            : base(id) {
            this.Number = number;
        }

        public override string Description {
            get {
                return "P" + this.Number.ToString() + ":S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaNNumericalParamsState
                && ((NfaNNumericalParamsState)other).Number == this.Number;
        }
    }

    /// <summary>
    /// Represents zero or more numerical parameters. (([0-9]*)(;([0-9]*))*)
    /// </summary>
    internal class NfaZeroOrMoreNumericalParamsState : NfaStateBase {
        public NfaZeroOrMoreNumericalParamsState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "Pm:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaZeroOrMoreNumericalParamsState;
        }
    }

    /// <summary>
    /// Represents content of a single numerical parameter. ([0-9]+)
    /// </summary>
    internal class NfaSingleNumericalParamContentState : NfaStateBase {
        public NfaSingleNumericalParamContentState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "N:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaSingleNumericalParamContentState;
        }
    }

    /// <summary>
    /// Represents text parameter. ({Printable}*)
    /// </summary>
    internal class NfaTextParamState : NfaStateBase {
        public NfaTextParamState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "Pt:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaTextParamState;
        }
    }

    /// <summary>
    /// Represents any character string. ([^\x98\x9c]*)
    /// </summary>
    internal class NfaAnyCharStringState : NfaStateBase {
        public NfaAnyCharStringState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "Ps:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaAnyCharStringState;
        }
    }

    /// <summary>
    /// Represents content of a text parameter. ({Printable}+)
    /// </summary>
    internal class NfaSingleTextParamContentState : NfaStateBase {
        public NfaSingleTextParamContentState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "T:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaSingleTextParamContentState;
        }
    }

    /// <summary>
    /// Represents content of an any-character string. ([^\x98\x9c]+)
    /// </summary>
    internal class NfaSingleAnyCharStringContentState : NfaStateBase {
        public NfaSingleAnyCharStringContentState(int id)
            : base(id) {
        }

        public override string Description {
            get {
                return "S:S" + this.ID.ToString();
            }
        }

        public override bool IsSameType(INfaState other) {
            return other is NfaSingleAnyCharStringContentState;
        }
    }
}
