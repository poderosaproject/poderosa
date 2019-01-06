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
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

using Poderosa.Util;

namespace Poderosa.Terminal {

    //繰り上げて実装することにした、キーの割当のためのクラス。
    //典型的には、例えば 0x1Fの送信は Ctrl+_ だが、英語キーボードでは実際には Ctrl+Shift+- が必要であり、押しづらい。このあたりを解決する。
    //ついでに、文字列に対してバインドを可能にすれば、"ls -la"キーみたいなのを定義できる。
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class KeyFunction {
        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public class Entry {
            private Keys _key;
            private string _data;

            public Keys Key {
                get {
                    return _key;
                }
            }
            public string Data {
                get {
                    return _data;
                }
            }

            public Entry(Keys key, string data) {
                _key = key;
                _data = data;
            }

            //0x形式も含めて扱えるように
            public string FormatData() {
                StringBuilder bld = new StringBuilder();
                foreach (char ch in _data) {
                    if (ch < ' ' || (int)ch == 0x7F) { //制御文字とdel
                        bld.Append("0x");
                        bld.Append(((int)ch).ToString("X2"));
                    }
                    else
                        bld.Append(ch);
                }
                return bld.ToString();
            }

            public static string ParseData(string s) {
                StringBuilder bld = new StringBuilder();
                int c = 0;
                while (c < s.Length) {
                    char ch = s[c];
                    if (ch == '0' && c + 3 <= s.Length && s[c + 1] == 'x') { //0x00形式。
                        int t;
                        if (Int32.TryParse(s.Substring(c + 2, 2), NumberStyles.HexNumber, null, out t)) {
                            bld.Append((char)t);
                        }
                        c += 4;
                    }
                    else {
                        bld.Append(ch);
                        c++;
                    }
                }

                return bld.ToString();
            }

        }

        private List<Entry> _elements;

        public KeyFunction() {
            _elements = new List<Entry>();
        }

        internal FixedStyleKeyFunction ToFixedStyle() {
            Keys[] keys = new Keys[_elements.Count];
            char[][] datas = new char[_elements.Count][];
            for (int i = 0; i < _elements.Count; i++) {
                keys[i] = _elements[i].Key;
                datas[i] = _elements[i].Data.ToCharArray();
            }

            FixedStyleKeyFunction r = new FixedStyleKeyFunction(keys, datas);
            return r;
        }

        public string Format() {
            StringBuilder bld = new StringBuilder();
            foreach (Entry e in _elements) {
                if (bld.Length > 0)
                    bld.Append(", ");
                bld.Append(Poderosa.UI.GMenuItem.FormatShortcut(e.Key));
                bld.Append("=");
                bld.Append(e.FormatData());
            }
            return bld.ToString();
        }

        public static KeyFunction Parse(string format) {
            string[] elements = format.Split(',');
            KeyFunction f = new KeyFunction();
            foreach (string e in elements) {
                int eq = e.IndexOf('=');
                if (eq != -1) {
                    string keypart = e.Substring(0, eq).Trim();
                    f._elements.Add(new Entry(WinFormsUtil.ParseKey(keypart.Split('+')), Entry.ParseData(e.Substring(eq + 1))));
                }
            }
            return f;
        }
    }

    internal class FixedStyleKeyFunction {
        public Keys[] _keys;
        public char[][] _datas;

        public FixedStyleKeyFunction(Keys[] keys, char[][] data) {
            _keys = keys;
            _datas = data;
        }
    }

}
