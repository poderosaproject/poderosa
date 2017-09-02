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
using Poderosa.Util.Collections;

namespace Poderosa.Terminal {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IShellSchemeCollection : IAdaptable {
        IShellScheme FindShellSchemeOrDefault(string name);
        IShellScheme CreateEmptyScheme(string name);

        IEnumerable<IShellScheme> Items {
            get;
        }
        int IndexOf(IShellScheme scheme);
        IShellScheme GetAt(int index);

        void UpdateAll(IShellScheme[] values, TypedHashtable<IShellScheme, IShellScheme> table);
        void AddDynamicChangeListener(IShellSchemeDynamicChangeListener listener);
        void RemoveDynamicChangeListener(IShellSchemeDynamicChangeListener listener);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IShellSchemeDynamicChangeListener {
        void OnShellSchemeCollectionChanged(IShellScheme[] values, TypedHashtable<IShellScheme, IShellScheme> table);
    }

    //NOTE: 取得専用と設定可能とでインタフェース分けてもいい
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IShellScheme : IAdaptable {
        bool IsGeneric {
            get;
        }
        string Name {
            get;
            set;
        }
        string PromptExpression {
            get;
            set;
        }
        char BackSpaceChar {
            get;
            set;
        }

        string[] ParseCommandInput(string src);
        string ParseFirstCommand(string src, out bool is_partial);

        bool IsDelimiter(char ch);
        char DefaultDelimiter {
            get;
        }
        IIntelliSenseItemCollection CommandHistory {
            get;
        }

        IShellScheme Clone();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IIntelliSenseItemCollection : IAdaptable {
        void UpdateItem(string[] command);
        IIntelliSenseItem FindItem(string[] command);
        void RemoveAt(int index);
        IEnumerable<IIntelliSenseItem> Items {
            get;
        }
        int Count {
            get;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IIntelliSenseItem : IAdaptable, IComparable {
        string Format(char delimiter);
        IIntelliSenseItem Clone();
    }

    //項目拡張ExtensionPointがらみ
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IIntelliSenseCandidateList : IEnumerable<IIntelliSenseItem>, IAdaptable {
        int Count {
            get;
        }
        void AddItem(IIntelliSenseItem item);
        void RemoveItem(IIntelliSenseItem item);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IIntelliSenseCandidateExtension {
        void AdjustItem(AbstractTerminal terminal, IIntelliSenseCandidateList list, string[] input);
    }
}
