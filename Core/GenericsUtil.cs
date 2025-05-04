// Copyright 2004-2025 The Poderosa Project.
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

namespace Poderosa.Util.Generics {
    //ToString, Parse, Equalsの３つを備える。PreferenceItem用に導入された。
    //bool, int, stringについてはここに列挙だが、Enum用には各自で。
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exclude/>
    public interface IPrimitiveAdapter<T> {
        string ToString(T value);
        T Parse(string value);
        bool Equals(T v1, T v2);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    /// 
    public class BoolPrimitiveAdapter : IPrimitiveAdapter<bool> {
        public string ToString(bool value) {
            return value.ToString();
        }

        public bool Parse(string value) {
            return Boolean.Parse(value);
        }

        public bool Equals(bool v1, bool v2) {
            return v1 == v2;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class IntPrimitiveAdapter : IPrimitiveAdapter<int> {
        public string ToString(int value) {
            return value.ToString();
        }

        public int Parse(string value) {
            return Int32.Parse(value);
        }

        public bool Equals(int v1, int v2) {
            return v1 == v2;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class DecimalPrimitiveAdapter : IPrimitiveAdapter<decimal> {
        public string ToString(decimal value) {
            return value.ToString();
        }

        public decimal Parse(string value) {
            return Decimal.Parse(value);
        }

        public bool Equals(decimal v1, decimal v2) {
            return v1 == v2;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class StringPrimitiveAdapter : IPrimitiveAdapter<string> {
        public string ToString(string value) {
            return value;
        }

        public string Parse(string value) {
            return value;
        }

        public bool Equals(string v1, string v2) {
            return v1 == v2;
        }
    }
}
