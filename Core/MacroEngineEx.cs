// Copyright 2011-2017 The Poderosa Project.
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
using System.Windows.Forms;
using Poderosa.Sessions;

namespace Poderosa.MacroEngine {

    /// <summary>
    /// <ja>
    /// マクロ実行サポートのためのインターフェイスです。
    /// </ja>
    /// <en>
    /// Interface for supporting executing macro.
    /// </en>
    /// </summary>
    public interface IMacroEngine {

        /// <summary>
        /// <ja>
        /// セッションを指定してマクロを実行する。
        /// </ja>
        /// <en>
        /// Run a macro with specifying session.
        /// </en>
        /// </summary>
        /// <param name="path">
        /// <ja>マクロのパス</ja>
        /// <en>Path of a macro to execute.</en>
        /// </param>
        /// <param name="session">
        /// <ja>セッション</ja>
        /// <en>Session.</en>
        /// </param>
        void RunMacro(string path, ISession session);

        /// <summary>
        /// <ja>
        /// マクロ選択ダイアログを表示する。
        /// </ja>
        /// <en>
        /// Show a dialog for selecting macro.
        /// </en>
        /// </summary>
        /// <param name="owner">
        /// <ja>オーナーフォーム</ja>
        /// <en>Owner form</en>
        /// </param>
        /// <returns>
        /// <ja>選択したマクロのパス。選択していなければnull。</ja>
        /// <en>Path of the selected macro. Null if no macro was selected.</en>
        /// </returns>
        string SelectMacro(Form owner);

    }

    /// <summary>
    /// <ja>
    /// プロパティ値がマクロ環境で接続パラメータとして取得できることを示します。
    /// </ja>
    /// <en>
    /// Represents the property value can be obtained as a connection parameter in the macro environment. 
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// この属性は<see cref="Poderosa.Protocols.ITerminalParameter"/>、または<see cref="Poderosa.Terminal.ITerminalSettings"/>実装クラスのプロパティに指定します。
    /// </ja>
    /// <en>
    /// This attribute must be specified to a property of a class which implements <see cref="Poderosa.Protocols.ITerminalParameter"/> or <see cref="Poderosa.Terminal.ITerminalSettings"/>.
    /// </en>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class MacroConnectionParameterAttribute : Attribute {
    }

}
