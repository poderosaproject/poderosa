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

namespace Poderosa.Util {
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exclude/>
    public class ListenerList<T> : IEnumerable<T> {
        private LinkedList<T> _list;

        public ListenerList() {
        }
        public void Add(T listener) {
            Precheck();
            _list.AddLast(listener);
        }
        public void Remove(T listener) {
            Precheck();
            _list.Remove(listener);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            Precheck();
            return _list.GetEnumerator();
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            Precheck();
            return _list.GetEnumerator();
        }

        public bool IsEmpty {
            get {
                return _list != null && _list.Count == 0;
            }
        }

        public void Clear() {
            if (_list != null)
                _list.Clear();
        }

        //多くは一つもListenerが登録されない。遅延作成する
        private void Precheck() {
            if (_list == null)
                _list = new LinkedList<T>();
        }

    }

    //リスナの登録・削除インタフェース
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <exclude/>
    public interface IListenerRegistration<T> {
        void AddListener(T listener);
        void RemoveListener(T listener);
    }
}
