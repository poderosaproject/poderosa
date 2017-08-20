/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Unicode.cs,v 1.3 2012/01/29 14:42:05 kzmi Exp $
 */
using System;

namespace Poderosa.Document {

    /// <summary>
    /// Flag bits for <see cref="UnicodeChar"/>.
    /// </summary>
    [Flags]
    public enum UnicodeCharFlags : uint {
        None = 0u,
        /// <summary>CJK character (should be displayed with the CJK font)</summary>
        CJK = 1u << 30,
        /// <summary>Wide-width character (should be displayed with two columns)</summary>
        WideWidth = 1u << 31,
    }

    /// <summary>
    /// Unicode character
    /// </summary>
    public struct UnicodeChar {

        // bit 0..20 : Unicode Code Point
        //
        // bit 30 : CJK
        // bit 31 : wide width

        public readonly uint _bits;

        private const uint CodePointMask = 0x1fffffu;

        /// <summary>
        /// An instance of SPACE (U+0020).
        /// </summary>
        public static UnicodeChar ASCII_SPACE {
            get {
                // The cost of the constructor would be zero with JIT compiler enabling optimization.
                return new UnicodeChar((uint)'\u0020', UnicodeCharFlags.None);
            }
        }

        /// <summary>
        /// An instance of NUL (U+0000).
        /// </summary>
        public static UnicodeChar ASCII_NUL {
            get {
                // The cost of the constructor would be zero with JIT compiler enabling optimization.
                return new UnicodeChar((uint)'\u0000', UnicodeCharFlags.None);
            }
        }

        /// <summary>
        /// Unicode code point
        /// </summary>
        public uint CodePoint {
            get {
                return _bits & CodePointMask;
            }
        }

        /// <summary>
        /// Raw data (Unicode code point | <see cref="UnicodeCharFlags"/>)
        /// </summary>
        internal uint RawData {
            get {
                return _bits;
            }
        }

        /// <summary>
        /// Whether this character is a wide-width character.
        /// </summary>
        public bool IsWideWidth {
            get {
                return Has(UnicodeCharFlags.WideWidth);
            }
        }

        /// <summary>
        /// Whether this character is a CJK character.
        /// </summary>
        public bool IsCJK {
            get {
                return Has(UnicodeCharFlags.CJK);
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ch">a character</param>
        /// <param name="cjk">allow cjk mode</param>
        public UnicodeChar(char ch, bool cjk) {
            uint codePoint = (uint)ch;
            UnicodeCharFlags flags = Unicode.DetermineUnicodeCharFlags(codePoint, cjk);
            _bits = codePoint | (uint)flags;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="highSurrogate">high surrogate code</param>
        /// <param name="lowSurrogate">low surrogate code</param>
        /// <param name="cjk">allow cjk mode</param>
        public UnicodeChar(char highSurrogate, char lowSurrogate, bool cjk) {
            uint codePoint = Unicode.SurrogatePairToCodePoint(highSurrogate, lowSurrogate);
            UnicodeCharFlags flags = Unicode.DetermineUnicodeCharFlags(codePoint, cjk);
            _bits = codePoint | (uint)flags;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="codePoint">Unicode code point</param>
        /// <param name="flags">flags to set</param>
        public UnicodeChar(uint codePoint, UnicodeCharFlags flags) {
            _bits = codePoint | (uint)flags;
        }

        /// <summary>
        /// Copy character into the char array in UTF-16.
        /// </summary>
        /// <param name="seq">destination array</param>
        /// <param name="index">start index of the array</param>
        /// <returns>char count written</returns>
        public int WriteTo(char[] seq, int index) {
            return Unicode.WriteCodePointTo(this._bits & CodePointMask, seq, index);
        }

        /// <summary>
        /// Checks if the specified flags were set.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns>true if all of the specified flags were set.</returns>
        public bool Has(UnicodeCharFlags flags) {
            return (this._bits & (uint)flags) == (uint)flags;
        }
    }

    /// <summary>
    /// Converter for converting <see cref="Char"/> to <see cref="UnicodeChar"/>.
    /// </summary>
    public class UnicodeCharConverter {
        private readonly bool _cjk;
        private char _highSurrogate;
        private bool _needLowSurrogate = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="cjk">allow cjk mode</param>
        public UnicodeCharConverter(bool cjk) {
            _cjk = cjk;
        }

        /// <summary>
        /// Feeds next char.
        /// </summary>
        /// <param name="c">next char</param>
        /// <param name="unicodeChar">new <see cref="UnicodeChar"/> is set if it was composed.</param>
        /// <returns>true if new <see cref="UnicodeChar"/> was composed.</returns>
        public bool Feed(char c, out UnicodeChar unicodeChar) {
            if (_needLowSurrogate) {
                _needLowSurrogate = false;
                if (Char.IsLowSurrogate(c)) {
                    unicodeChar = new UnicodeChar(_highSurrogate, c, _cjk);
                    return true;
                }

                // fall through.
                // ignore previous high surrogate.
            }

            if (Char.IsHighSurrogate(c)) {
                _highSurrogate = c;
                _needLowSurrogate = true;
                unicodeChar = UnicodeChar.ASCII_NUL;
                return false;
            }

            unicodeChar = new UnicodeChar(c, _cjk);
            return true;
        }
    }


    /// <summary>
    /// Unicode utility
    /// </summary>
    public static class Unicode {

        // Tables consist of the following values.
        //
        // 0 : Character is displayed as half-width using latin font.
        // 1 : Character is displayed as half-width using CJK font.
        // 2 : Character is displayed as full-width using CJK font.
        //
        // Symbols or letters that are contained in CJK character set (JIS X 0201/0208, GB2312, Big5, KS X 1001)
        // except ASCII characters are treated as full-width characters.

        private static readonly byte[] WIDTH_MAP_0000_00FF = {
            // C0 Controls and Basic Latin (U+0000 - U+007F)
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0000-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0010-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0020-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0030-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0040-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0050-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0060-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0070-0F
            // C1 Controls and Latin-1 Supplement (U+0080 - U+00FF)
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //0080-8F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //0090-9F
            1, 2, 1, 1, 2, 1, 1, 2, 2, 1, 2, 1, 1, 2, 2, 2, //00A0-AF
            2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, //00B0-BF
            1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, //00C0-CF
            2, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 2, 2, //00D0-DF
            1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, //00E0-EF
            2, 1, 1, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 2, 1, //00F0-FF
        };

        private static readonly byte[] WIDTH_MAP_0200_04FF = {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0200-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0210-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0220-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0230-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0240-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0250-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0260-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0270-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0280-8F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0290-9F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //02A0-AF
            // Spacing Modifier Letters (U+02B0 - U+02FF)
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //02B0-BF
            0, 0, 0, 0, 0, 0, 0, 2, 0, 2, 2, 2, 0, 2, 0, 0, //02C0-CF
            2, 0, 0, 0, 0, 0, 0, 0, 2, 2, 2, 2, 0, 2, 0, 0, //02D0-DF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //02E0-EF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //02F0-FF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0300-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0310-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0320-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0330-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0340-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0350-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0360-0F
            // Greek and Coptic (U+0370 - U+03FF)
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0370-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0380-8F
            0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //0390-9F
            2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, //03A0-AF
            0, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //03B0-BF
            2, 2, 0, 2, 2, 2, 2, 2, 2, 2, 0, 0, 0, 0, 0, 0, //03C0-BF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //03D0-BF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //03E0-EF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //03F0-FF
            // Cyrillic (U+0400 - U+04FF)
            0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0400-0F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //0410-0F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //0420-0F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //0430-0F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //0440-0F
            0, 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0450-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0460-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0470-0F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0480-8F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //0490-9F
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //04A0-AF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //04B0-BF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //04C0-BF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //04D0-BF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //04E0-EF
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, //04F0-FF
        };

        private static readonly byte[] WIDTH_MAP_2500_25FF = {
            // Box Drawing (U+2500 - U+257F)
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //2500-0F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //2510-1F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //2520-2F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //2530-3F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, //2540-4F
            2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, //2550-5F
            1, 2, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 2, 2, 2, //2560-6F
            2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2570-7F
            // Block Elements (U+2580 - U+259F)
            1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //2580-8F
            1, 1, 2, 1, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //2590-9F
            // Geometric Shapes (U+25A0 - U+25FF)
            2, 2, 1, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, //25A0-AF
            1, 1, 2, 2, 1, 1, 2, 2, 1, 1, 1, 1, 2, 2, 1, 1, //25B0-BF
            2, 2, 1, 1, 1, 1, 2, 2, 2, 1, 1, 2, 1, 1, 2, 2, //25C0-CF
            2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //25D0-DF
            1, 1, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, //25E0-EF
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1  //25F0-FF
        };

        private static readonly byte[] WIDTH_MAP_FF00_FFFF = {
            // FF01-FF5E: Fullwidth ASCII variants
            // FF5F-FF60: Fullwidth brackets
            // FF61-FF64: Halfwidth CJK punctuation
            // FF65-FF9F: Halfwidth Katakana
            // FFA0-FFDC: Halfwidth Hangul
            // FFE0-FFE6: Fullwidth symbol variants
            // FFE8-FFEE: Halfwidth symbol variants
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //FF00-0F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //FF10-1F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //FF20-2F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //FF30-3F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //FF40-4F
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, //FF50-5F
            2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FF60-6F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FF70-7F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FF80-8F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FF90-9F
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FFA0-AF
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FFB0-BF
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, //FFC0-CF
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, //FFD0-DF
            2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 2, //FFE0-EF
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2  //FFF0-FF
        };

        /// <summary>
        /// Gets <see cref="UnicodeCharFlags"/> for a Unicode code point.
        /// </summary>
        /// <param name="codePoint">Unicode code point</param>
        /// <param name="cjk">allow cjk mode</param>
        /// <returns></returns>
        public static UnicodeCharFlags DetermineUnicodeCharFlags(uint codePoint, bool cjk) {
            if (codePoint >= 0x10000u) {
                return UnicodeCharFlags.CJK | UnicodeCharFlags.WideWidth;
                // FIXME: it should not be assumed that the character is always wide-width.
            }

            if (!cjk) {
                return UnicodeCharFlags.None;
            }

            byte upperByte = (byte)(codePoint >> 8);
            byte t;
            switch (upperByte) {
                case 0x00:
                    t = WIDTH_MAP_0000_00FF[codePoint];
                    break;
                case 0x02:
                case 0x03:
                case 0x04:
                    t = WIDTH_MAP_0200_04FF[codePoint - 0x0200u];
                    break;
                case 0x20:
                    if (codePoint == 0x2017u) // for OEM850
                        t = 0;
                    else
                        t = 2;
                    break;
                case 0x25:
                    t = WIDTH_MAP_2500_25FF[codePoint - 0x2500u];
                    break;
                case 0xff:
                    t = WIDTH_MAP_FF00_FFFF[codePoint - 0xff00u];
                    break;
                default:
                    t = 2;
                    break;
            }

            switch (t) {
                case 1:
                    return UnicodeCharFlags.CJK;    // CJK Hankaku
                case 2:
                    return UnicodeCharFlags.CJK | UnicodeCharFlags.WideWidth;   // CJK Zenkaku
                default:
                    return UnicodeCharFlags.None;   // Latin Hankaku
            }
        }

        /// <summary>
        /// Gets whether a character is a control character.
        /// </summary>
        /// <param name="ch">character code.</param>
        /// <returns>True if a character is a control character.</returns>
        public static bool IsControlCharacter(char ch) {
            return ch < 0x20 || ch == 0x7f || (0x80 <= ch && ch <= 0x9f);
        }

        /// <summary>
        /// Converts surrogate pair to a Unicode code point.
        /// </summary>
        /// <param name="highSurrogate"></param>
        /// <param name="lowSurrogate"></param>
        /// <returns>Unicode code point</returns>
        public static uint SurrogatePairToCodePoint(char highSurrogate, char lowSurrogate) {
            return 0x10000u + (((uint)highSurrogate - 0xd800u) << 10) + ((uint)lowSurrogate - 0xdc00u);
        }

        /// <summary>
        /// Writes a Unicode code point into the char array in UTF-16.
        /// </summary>
        /// <param name="codePoint">Unicode code point</param>
        /// <param name="seq">destination array</param>
        /// <param name="index">start index of the array</param>
        /// <returns>char count written</returns>
        public static int WriteCodePointTo(uint codePoint, char[] seq, int index) {
            if (codePoint <= 0xffffu) {
                seq[index] = (char)codePoint;
                return 1;
            }
            else {
                uint f = codePoint - 0x10000u;
                seq[index] = (char)((f >> 10) + 0xd800u);
                seq[index + 1] = (char)((f & 0x3ff) + 0xdc00u);
                return 2;
            }
        }

    }

}
