// Copyright 2004-2018 The Poderosa Project.
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

using System.Drawing;

namespace Poderosa.Document {

    /// <summary>
    /// 24 bit color information in <see cref="GLine"/>.
    /// </summary>
    internal struct GColor24 {
        // bit 0 ..7  : B
        // bit 8 ..15 : G
        // bit 16..23 : R
        private uint _foreColor;
        private uint _backColor;

        /// <summary>
        /// 24 bit fore color
        /// </summary>
        public Color ForeColor {
            get {
                return Color.FromArgb((int)(_foreColor | 0xff000000u));
            }
            set {
                _foreColor = (uint)(value.ToArgb() & 0xffffff);
            }
        }

        /// <summary>
        /// 24 bit back color
        /// </summary>
        public Color BackColor {
            get {
                return Color.FromArgb((int)(_backColor | 0xff000000u));
            }
            set {
                _backColor = (uint)(value.ToArgb() & 0xffffff);
            }
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <returns></returns>
        public static bool operator ==(GColor24 col1, GColor24 col2) {
            return col1._foreColor == col2._foreColor && col1._backColor == col2._backColor;
        }

        /// <summary>
        /// Non-equality operator
        /// </summary>
        /// <param name="col1"></param>
        /// <param name="col2"></param>
        /// <returns></returns>
        public static bool operator !=(GColor24 col1, GColor24 col2) {
            return !(col1 == col2);
        }

        public override bool Equals(object obj) {
            if (obj is GColor24) {
                return this == ((GColor24)obj);
            }
            return false;
        }

        public override int GetHashCode() {
            return (int)(this._foreColor + this._backColor);
        }
    }

}
