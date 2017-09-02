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
using System.Diagnostics;
using Poderosa.Util.Collections;

namespace Poderosa {
    //普通はアプリで唯一。各AdapterFactoryを管理する
    internal class AdapterManager : IAdapterManager {

        //変換するオブジェクトのTypeから対応するFactoryのコレクションへのマッピング
        //双方向のどちら側なのかは毎回型を尋ねる、注意
        private TypedHashtable<Type, List<IDualDirectionalAdapterFactory>> _classToFactoryList;

        public AdapterManager() {
            _classToFactoryList = new TypedHashtable<Type, List<IDualDirectionalAdapterFactory>>();
        }

        #region IAdapterManager
        public void RegisterFactory(IDualDirectionalAdapterFactory factory) {
            Debug.Assert(factory.SourceType.IsClass, "source type must be a class");
            RegisterFactory(factory.SourceType, factory);
            Debug.Assert(factory.AdapterType.IsClass, "adapter type must be a class");
            RegisterFactory(factory.AdapterType, factory);
        }
        private void RegisterFactory(Type type, IDualDirectionalAdapterFactory factory) {
            List<IDualDirectionalAdapterFactory> l = FindFactoryList(type);
            if (l == null) {
                l = new List<IDualDirectionalAdapterFactory>();
                _classToFactoryList.Add(type, l);
            }
            l.Add(factory);
        }

        public void RemoveFactory(IDualDirectionalAdapterFactory factory) {
            RemoveFactory(factory.SourceType, factory);
            RemoveFactory(factory.AdapterType, factory);
        }
        private void RemoveFactory(Type type, IDualDirectionalAdapterFactory factory) {
            List<IDualDirectionalAdapterFactory> l = FindFactoryList(type);
            if (l != null) {
                l.Remove(factory);
                if (l.Count == 0)
                    _classToFactoryList.Remove(type);
            }
        }

        public IAdaptable GetAdapter(IAdaptable obj, Type adapter) {
            //ショートカット: 直接型がある場合はAdapterFactoryの存在に関係なく変換可能
            if (adapter.IsInstanceOfType(obj))
                return obj;

            //探してみる
            List<IDualDirectionalAdapterFactory> l = FindFactoryList(obj.GetType());
            if (l == null)
                return null;

            foreach (IDualDirectionalAdapterFactory f in l) {
                IAdaptable r = ChallengeUsingAdapterFactory(f, obj, adapter);
                if (r != null)
                    return r;
            }

            return null;
        }

        private IAdaptable ChallengeUsingAdapterFactory(IDualDirectionalAdapterFactory factory, IAdaptable obj, Type adapter) {
            Type dest = factory.SourceType == obj.GetType() ? factory.AdapterType : factory.SourceType; //変換先クラス

            if (adapter.IsAssignableFrom(dest)) { //これならビンゴといっていい。現在はこのケースしかないはず
                IAdaptable t = factory.SourceType == obj.GetType() ? factory.GetAdapter(obj) : factory.GetSource(obj);
                Debug.Assert(adapter.IsInstanceOfType(t));
                return t;
            }

            //複雑なケース。
            //変換後のオブジェクトのGetAdapterを読んでみる（再帰呼び出し対策必要）、
            //２ステップ以上のFactoryを使用する、などが必要。
            //TODO 現在未サポート
            return null;
        }

        //上のGeneric版
        T IAdapterManager.GetAdapter<T>(IAdaptable obj) {
            return (T)GetAdapter(obj, typeof(T));
        }
        #endregion

        private List<IDualDirectionalAdapterFactory> FindFactoryList(Type adapter) {
            return _classToFactoryList[adapter];
        }

    }
}
