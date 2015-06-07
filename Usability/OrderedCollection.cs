/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OrderedCollection.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
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
        public void LimitCount(int value) {
            if (_data.Count > value) {
                _data.RemoveRange(_data.Count - 1, _data.Count - value);
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
