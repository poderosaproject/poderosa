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

namespace Poderosa.Serializing {

    /// <summary>
    /// Type of password serialization
    /// </summary>
    public enum PasswordSerialization {
        /// <summary>Don't serialize passwords</summary>
        None,
        /// <summary>Serialize passwords as plaintext</summary>
        Plaintext,
        /// <summary>Serialize passwords using simple encryption</summary>
        Encrypted,
    }

    /// <summary>
    /// Serialization options
    /// </summary>
    public class SerializationOptions {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="passwordSerialization">Type of password serialization</param>
        public SerializationOptions(PasswordSerialization passwordSerialization = PasswordSerialization.None) {
            PasswordSerialization = passwordSerialization;
        }

        public PasswordSerialization PasswordSerialization {
            get;
            private set;
        }
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ISerializeService {
        StructuredText Serialize(object obj, SerializationOptions options);
        StructuredText Serialize(Type type, object obj, SerializationOptions options);
        object Deserialize(StructuredText node);
    }

    //ExtensionPointに接続するインタフェース。
    //ConcreteTypeに対応するオブジェクトに対して使用する。
    //扱うStructuredTextの形は
    // <ConcreteType.FullName> {
    //   ...
    // }
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ISerializeServiceElement {
        Type ConcreteType {
            get;
        }
        StructuredText Serialize(object obj, SerializationOptions options);
        object Deserialize(StructuredText node);
    }
}
