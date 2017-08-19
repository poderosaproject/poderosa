/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TextDecoration.cs,v 1.5 2012/05/20 09:17:25 kzmi Exp $
 */
using System;
using System.IO;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Text;

//using Poderosa.Util;

namespace Poderosa.Document {

    /// <summary>
    /// Color type for the background color and fore color.
    /// </summary>
    public enum ColorType {
        /// <summary>Use default color</summary>
        Default,
        /// <summary>Use 8 bit color code</summary>
        Custom8bit,
        /// <summary>Use 24 bit color</summary>
        Custom24bit,
    }

    /// <summary>
    /// Color specification
    /// </summary>
    public struct ColorSpec {
        private readonly ColorType _colorType;
        private readonly int _colorValue;

        /// <summary>
        /// Instance that indicates to use default color
        /// </summary>
        public static ColorSpec Default {
            get {
                return new ColorSpec(ColorType.Default, 0);
            }
        }

        /// <summary>
        /// Color type
        /// </summary>
        public ColorType ColorType {
            get {
                return _colorType;
            }
        }

        /// <summary>
        /// 24 bit color
        /// </summary>
        public Color Color {
            get {
                return (_colorType == ColorType.Custom24bit) ? Color.FromArgb(_colorValue) : Color.Empty;
            }
        }

        /// <summary>
        /// 8 bit color code
        /// </summary>
        public int ColorCode {
            get {
                return (_colorType == ColorType.Custom8bit) ? _colorValue : 0;
            }
        }

        /// <summary>
        /// Internal constructor
        /// </summary>
        /// <param name="colorType">color type</param>
        /// <param name="colorValue">color value (24 bit ARGB or 8 bit color code)</param>
        private ColorSpec(ColorType colorType, int colorValue) {
            _colorType = colorType;
            _colorValue = colorValue;
        }

        /// <summary>
        /// Constructor for creating an instance that indicates to use 24 bit color.
        /// </summary>
        /// <param name="color">color. empty color indicates to use default color.</param>
        public ColorSpec(Color color) {
            if (color.IsEmpty) {
                _colorType = ColorType.Default;
                _colorValue = 0;
            }
            else {
                _colorType = ColorType.Custom24bit;
                _colorValue = color.ToArgb();
            }
        }

        /// <summary>
        /// Constructor for creating an instance that indicates to use 8 bit color.
        /// </summary>
        /// <param name="color">color code</param>
        public ColorSpec(int color) {
            _colorType = ColorType.Custom8bit;
            _colorValue = color & 0xff;
        }

        public override string ToString() {
            switch (_colorType) {
                case ColorType.Default:
                    return "Default";
                case ColorType.Custom8bit:
                    return new StringBuilder()
                        .Append('{').Append(_colorValue).Append('}').ToString();
                case ColorType.Custom24bit:
                    Color c = Color.FromArgb(_colorValue);
                    return new StringBuilder()
                        .Append('{').Append(c.A).Append(',').Append(c.R).Append(',').Append(c.G).Append(',').Append(c.B).Append('}').ToString();
                default:
                    return "???";
            }
        }
    }

    /// <summary>
    /// Text decoration.
    /// </summary>
    /// <remarks>
    /// The instance is immutable.
    /// </remarks>
    /// <exclude/>
    public sealed class TextDecoration {
        private readonly ColorSpec _backColor;
        private readonly ColorSpec _foreColor;
        private readonly Attributes _attrs;

        [Flags]
        internal enum Attributes {
            None = 0,
            Blink = 1 << 0,
            Hidden = 1 << 1,
            Underlined = 1 << 2,
            Bold = 1 << 3,
            Inverted = 1 << 4,
        }

        private static readonly TextDecoration _default =
            new TextDecoration(ColorSpec.Default, ColorSpec.Default, Attributes.None);

        /// <summary>
        /// Get a default decoration.
        /// "default decoration" means that text is displayed
        /// with default text color, default background color,
        /// no underline, and no bold.
        /// </summary>
        public static TextDecoration Default {
            get {
                return _default;
            }
        }

        private TextDecoration(ColorSpec backColor, ColorSpec foreColor, Attributes attrs) {
            _backColor = backColor;
            _foreColor = foreColor;
            _attrs = attrs;
        }

        public ColorSpec ForeColor {
            get {
                return _foreColor;
            }
        }

        public ColorSpec BackColor {
            get {
                return _backColor;
            }
        }

        public bool Blink {
            get {
                return (_attrs & Attributes.Blink) != 0;
            }
        }

        public bool Hidden {
            get {
                return (_attrs & Attributes.Hidden) != 0;
            }
        }

        public bool Bold {
            get {
                return (_attrs & Attributes.Bold) != 0;
            }
        }

        public bool Underline {
            get {
                return (_attrs & Attributes.Underlined) != 0;
            }
        }

        public bool Inverted {
            get {
                return (_attrs & Attributes.Inverted) != 0;
            }
        }

        internal Attributes Attribute {
            get {
                return _attrs;
            }
        }

        public bool IsDefault {
            get {
                return _attrs == Attributes.None
                    && _backColor.ColorType == ColorType.Default
                    && _foreColor.ColorType == ColorType.Default;
            }
        }

        /// <summary>
        /// Get a new copy whose inversion flag was set.
        /// </summary>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithInverted(bool inverted) {
            return new TextDecoration(_backColor, _foreColor, GetNewAttr(Attributes.Inverted, inverted));
        }

        /// <summary>
        /// Get a new copy whose text color was set.
        /// </summary>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithForeColor(ColorSpec foreColor) {
            return new TextDecoration(_backColor, foreColor, _attrs);
        }

        /// <summary>
        /// Get a new copy whose background color was set.
        /// </summary>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithBackColor(ColorSpec backColor) {
            return new TextDecoration(backColor, _foreColor, _attrs);
        }

        /// <summary>
        /// Get a new copy whose underline status was set.
        /// </summary>
        /// <param name="underline">new underline status</param>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithUnderline(bool underline) {
            return new TextDecoration(_backColor, _foreColor, GetNewAttr(Attributes.Underlined, underline));
        }

        /// <summary>
        /// Get a new copy whose bold status was set.
        /// </summary>
        /// <param name="bold">new bold status</param>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithBold(bool bold) {
            return new TextDecoration(_backColor, _foreColor, GetNewAttr(Attributes.Bold, bold));
        }

        /// <summary>
        /// Get a new copy whose hidden status was set.
        /// </summary>
        /// <param name="hidden">new hidden status</param>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithHidden(bool hidden) {
            return new TextDecoration(_backColor, _foreColor, GetNewAttr(Attributes.Hidden, hidden));
        }

        /// <summary>
        /// Get a new copy whose blink status was set.
        /// </summary>
        /// <param name="blink">new blink status</param>
        /// <returns>new instance</returns>
        public TextDecoration GetCopyWithBlink(bool blink) {
            return new TextDecoration(_backColor, _foreColor, GetNewAttr(Attributes.Blink, blink));
        }

        private Attributes GetNewAttr(Attributes attr, bool set) {
            return set ? (_attrs | attr) : (_attrs & ~attr);
        }

        public override string ToString() {
            StringBuilder s = new StringBuilder();
            s.Append("{Back=")
                .Append(_backColor.ToString())
                .Append(",Fore=")
                .Append(_foreColor.ToString());

            if (this.Bold) {
                s.Append(",Bold");
            }
            if (this.Underline) {
                s.Append(",Underlined");
            }
            if (this.Inverted) {
                s.Append(",Inverted");
            }

            s.Append('}');

            return s.ToString();
        }
    }

}
