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

namespace Poderosa.Document {

    /// <summary>
    /// Flag bits for <see cref="GAttr"/>.
    /// </summary>
    [Flags]
    internal enum GAttrFlags : uint {
        None = 0u,
        Blink = 1u << 19,
        Hidden = 1u << 20,
        Underlined = 1u << 21,
        Bold = 1u << 22,
        UseCjkFont = 1u << 23,
        Cursor = 1u << 24,
        Selected = 1u << 25,
        Inverted = 1u << 26,
        Use24bitForeColor = 1u << 27,
        Use24bitBackColor = 1u << 28,
        Use8bitForeColor = 1u << 29,
        Use8bitBackColor = 1u << 30,
        SameToPrevious = 1u << 31,
    }

    /// <summary>
    /// Attribute information in <see cref="GLine"/>.
    /// </summary>
    /// <remarks>
    /// This object doesn't contain 24 bit colors.<br/>
    /// 24 bit colors are maintained by array of <see cref="GColor24"/>.
    /// </remarks>
    internal struct GAttr {
        // bit 0..7  : 8 bit fore color code
        // bit 8..16 : 8 bit back color code
        //
        // bit 19 : blink
        // bit 20 : hidden
        // bit 21 : underlined
        // bit 22 : bold
        // bit 23 : use cjk font
        // bit 24 : cursor
        // bit 25 : selected
        // bit 26 : inverted
        // bit 27 : use 24 bit fore color
        // bit 28 : use 24 bit back color
        // bit 29 : use 8 bit fore color
        // bit 30 : use 8 bit back color
        // bit 31 : marker to tell that this cell has the same flags/colors to previous cell.

        private readonly uint _bits;

        /// <summary>
        /// Default value
        /// </summary>
        public static GAttr Default {
            get {
                return new GAttr(); // all bits must be zero
            }
        }

        /// <summary>
        /// Back color (8 bit color code)
        /// </summary>
        public int BackColor {
            get {
                return (int)((this._bits >> 8) & 0xffu);
            }
        }

        /// <summary>
        /// Fore color (8 bit color code)
        /// </summary>
        public int ForeColor {
            get {
                return (int)(this._bits & 0xffu);
            }
        }

        /// <summary>
        /// Whether this GAttr uses 24 bit colors.
        /// </summary>
        public bool Uses24bitColor {
            get {
                return (this._bits & (uint)(GAttrFlags.Use24bitBackColor | GAttrFlags.Use24bitForeColor)) != 0u;
            }
        }

        /// <summary>
        /// Whether this GAttr represents the default settings.
        /// </summary>
        public bool IsDefault {
            get {
                return this.CoreBits == GAttr.Default.CoreBits;
            }
        }

        /// <summary>
        /// Intenal bits without "SameToPrevious" bit.
        /// </summary>
        private uint CoreBits {
            get {
                return this._bits & ~(uint)GAttrFlags.SameToPrevious;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="backColor">back color (8 bit color code)</param>
        /// <param name="foreColor">fore color (8 bit color code)</param>
        /// <param name="flags">flags</param>
        public GAttr(int backColor, int foreColor, GAttrFlags flags) {
            uint flagBits = (uint)flags;
            uint bits = flagBits & ~0xffffu;
            if ((flagBits & (uint)GAttrFlags.Use8bitForeColor) != 0u) {
                bits |= (uint)(foreColor & 0xff);
            }
            if ((flagBits & (uint)GAttrFlags.Use8bitBackColor) != 0u) {
                bits |= (uint)((backColor & 0xff) << 8);
            }
            this._bits = bits;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="bits">raw bits</param>
        private GAttr(uint bits) {
            this._bits = bits;
        }

        /// <summary>
        /// Gets a new value that is specified to use default fore color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWithDefaultForeColor() {
            return new GAttr(this._bits & ~(0xffu | (uint)GAttrFlags.Use8bitForeColor | (uint)GAttrFlags.Use24bitForeColor));
        }

        /// <summary>
        /// Gets a new value that is specified to use 8 bit fore color.
        /// </summary>
        /// <param name="color">8 bit color code</param>
        /// <returns>a new value</returns>
        public GAttr CopyWith8bitForeColor(int color) {
            return new GAttr(
                        (this._bits & ~(0xffu | (uint)GAttrFlags.Use8bitForeColor | (uint)GAttrFlags.Use24bitForeColor))
                        | (uint)(color & 0xff)
                        | (uint)GAttrFlags.Use8bitForeColor);
        }

        /// <summary>
        /// Gets a new value that is specified to use 24 bit fore color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWith24bitForeColor() {
            return new GAttr(
                        (this._bits & ~(0xffu | (uint)GAttrFlags.Use8bitForeColor | (uint)GAttrFlags.Use24bitForeColor))
                        | (uint)GAttrFlags.Use24bitForeColor);
        }

        /// <summary>
        /// Gets a new value that is specified to use default back color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWithDefaultBackColor() {
            return new GAttr(this._bits & ~(0xff00u | (uint)GAttrFlags.Use8bitBackColor | (uint)GAttrFlags.Use24bitBackColor));
        }

        /// <summary>
        /// Gets a new value that is specified to use 8 bit back color.
        /// </summary>
        /// <param name="color">8 bit color code</param>
        /// <returns>a new value</returns>
        public GAttr CopyWith8bitBackColor(int color) {
            return new GAttr(
                        (this._bits & ~(0xff00u | (uint)GAttrFlags.Use8bitBackColor | (uint)GAttrFlags.Use24bitBackColor))
                        | ((uint)(color & 0xff) << 8)
                        | (uint)GAttrFlags.Use8bitBackColor);
        }

        /// <summary>
        /// Gets a new value that is specified to use 24 bit back color.
        /// </summary>
        /// <returns>a new value</returns>
        public GAttr CopyWith24bitBackColor() {
            return new GAttr(
                        (this._bits & ~(0xff00u | (uint)GAttrFlags.Use8bitBackColor | (uint)GAttrFlags.Use24bitBackColor))
                        | (uint)GAttrFlags.Use24bitBackColor);
        }

        /// <summary>
        /// Checks if one or more of the specified flags were set.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns>true if one or more of the specified flags were set.</returns>
        public bool Has(GAttrFlags flags) {
            return (this._bits & (uint)flags) != 0u;
        }

        /// <summary>
        /// Adds flags.
        /// </summary>
        /// <param name="attr">object to be based on</param>
        /// <param name="flags">flags to set</param>
        /// <returns>new object</returns>
        public static GAttr operator +(GAttr attr, GAttrFlags flags) {
            return new GAttr(attr._bits | (uint)flags);
        }

        /// <summary>
        /// Removes flags.
        /// </summary>
        /// <param name="attr">object to be based on</param>
        /// <param name="flags">flags to remove</param>
        /// <returns>new object</returns>
        public static GAttr operator -(GAttr attr, GAttrFlags flags) {
            return new GAttr(attr._bits & ~(uint)flags);
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="attr1"></param>
        /// <param name="attr2"></param>
        /// <returns></returns>
        public static bool operator ==(GAttr attr1, GAttr attr2) {
            return attr1.CoreBits == attr2.CoreBits;
        }

        /// <summary>
        /// Non-equality operator
        /// </summary>
        /// <param name="attr1"></param>
        /// <param name="attr2"></param>
        /// <returns></returns>
        public static bool operator !=(GAttr attr1, GAttr attr2) {
            return !(attr1 == attr2);
        }

        public override bool Equals(object obj) {
            if (obj is GAttr) {
                return this == (GAttr)obj;
            }
            return false;
        }

        public override int GetHashCode() {
            return (int)this.CoreBits;
        }
    }

}
