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
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Poderosa.Util.Collections {
    //主にMRUに使用するコレクション。
    //  * 上限個数つきリストを保持し、
    //  * 外部からもらった比較関数で要素が等しいかどうかをみて、
    //  * 前方に持ってくる。
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exclude/>
    public class OrderedCollection<T> : IEnumerable<T> {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        /// <exclude/>
        public delegate bool Equality(T t1, T t2);

        private Equality _equality;
        private List<T> _data; //[0]が先頭

        public OrderedCollection(Equality equality) {
            _equality = equality;
            _data = new List<T>();
        }

        public void Update(T element) {
            for (int i = 0; i < _data.Count; i++) {
                if (_equality(_data[i], element)) {
                    _data.RemoveAt(i);
                    break; //等しいのがみつかったらそれを除去して抜ける
                }
            }
            _data.Insert(0, element);
        }

        public void Add(T element) { //ふつうの追加
            _data.Add(element);
        }

        public void Clear() {
            _data.Clear();
        }

        public int Count {
            get {
                return _data.Count;
            }
        }
        public T this[int index] {
            get {
                return _data[index];
            }
        }

        //個数の上限を決めて切る
        public void LimitCount(int limit) {
            if (_data.Count > limit) {
                _data.RemoveRange(limit, _data.Count - limit);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return _data.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() { //本当に、IEnumerator<T>がIEnumeratorから派生してるのってクソ以外の何者でもないな
            return _data.GetEnumerator();
        }

    }
}
