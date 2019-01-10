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

using System.Collections.Generic;
using System.Text;

namespace Poderosa.Terminal.EscapeSequenceEngine {

    /// <summary>
    /// Marker interface of the object which executes the escape-sequence.
    /// </summary>
    internal interface IEscapeSequenceExecutor {
    }

    /// <summary>
    /// Object represents a numerical parameter
    /// </summary>
    internal class NumericalParameter {
        private bool _isEmpty = true;

        private int _value = 0;

        public bool IsEmpty {
            get {
                return _isEmpty;
            }
        }

        public int? Value {
            get {
                return _isEmpty ? (int?)null : _value;
            }
        }

        public void AppendDigit(byte ch) {
            int n = ch - 0x30;
            _value = _value * 10 + ((n >= 0 && n <= 9) ? n : 0);
            _isEmpty = false;
        }
    }

    /// <summary>
    /// Collection of <see cref="NumericalParameter"/>
    /// </summary>
    internal class NumericalParameterCollection : IEnumerable<NumericalParameter> {
        private readonly List<NumericalParameter> _list = new List<NumericalParameter>();

        public NumericalParameter this[int index] {
            get {
                return (index < _list.Count) ? _list[index] : new NumericalParameter();
            }
        }

        public int Count {
            get {
                return _list.Count;
            }
        }

        public void Add(NumericalParameter param) {
            _list.Add(param);
        }

        public void Clear() {
            _list.Clear();
        }

        public IEnumerator<NumericalParameter> GetEnumerator() {
            return _list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return ((System.Collections.IEnumerable)_list).GetEnumerator();
        }
    }

    /// <summary>
    /// Object represents a text parameter
    /// </summary>
    internal class TextParameter {
        private readonly StringBuilder _str = new StringBuilder();

        public bool IsEmpty {
            get {
                return _str.Length == 0;
            }
        }

        public string Value {
            get {
                return _str.ToString();
            }
        }

        public void AppendChar(byte ch) {
            _str.Append((char)ch);
        }
    }

    /// <summary>
    /// Context object during parsing escape sequence.
    /// </summary>
    internal class EscapeSequenceContext {
        public IEscapeSequenceExecutor Executor {
            get;
            private set;
        }

        public List<byte> Matched {
            get;
            private set;
        }

        public NumericalParameterCollection NumericalParams {
            get;
            private set;
        }

        public TextParameter TextParam {
            get;
            set;
        }

        public string Pattern {
            get;
            set;
        }

        public EscapeSequenceContext(IEscapeSequenceExecutor executor) {
            this.Executor = executor;
            this.Matched = new List<byte>();
            this.NumericalParams = new NumericalParameterCollection();
            this.TextParam = null;
            this.Pattern = null;
        }

        public void Clear() {
            this.Matched.Clear();
            this.NumericalParams.Clear();
            this.TextParam = null;
            this.Pattern = null;
        }
    }

}
