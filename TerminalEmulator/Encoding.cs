/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Encoding.cs,v 1.7 2012/01/29 14:42:06 kzmi Exp $
 */
using System;
using System.Text;

using Poderosa.ConnectionParam;
using Poderosa.Util;
using Poderosa.Document;

namespace Poderosa.Terminal {
    //encoding関係
    internal abstract class EncodingProfile {

        private readonly Encoding _encoding;
        private readonly EncodingType _type;
        private readonly byte[] _buffer;
        private int _cursor;
        private int _byte_len;
        private readonly char[] _singleCharBuff;

        protected EncodingProfile(EncodingType t, Encoding enc) {
            _type = t;
            _encoding = enc;
            _buffer = new byte[6];
            _cursor = 0;
            _singleCharBuff = new char[1];
        }

        // Check if the byte is the first byte of a character which should be converted the character code.
        protected abstract bool IsLeadByte(byte b);

        // Determine how long byte length of a character is.
        protected abstract int GetCharLength(byte b);

        // Check whether the character should be ignored.
        protected abstract bool IsIgnoreableChar(char[] buf, int len);

        // Whether the CJK character is used.
        protected abstract bool UseCJKCharacter {
            get;
        }

        public Encoding Encoding {
            get {
                return _encoding;
            }
        }

        public EncodingType Type {
            get {
                return _type;
            }
        }

        public byte[] Buffer {
            get {
                return _buffer;
            }
        }

        public byte[] GetBytes(char[] chars) {
            return _encoding.GetBytes(chars);
        }

        //NOTE 潜在的には_tempOneCharArrayの使用でマルチスレッドでの危険がある。
        public byte[] GetBytes(char ch) {
            // FIXME: support surrogate pair
            _singleCharBuff[0] = ch;
            return _encoding.GetBytes(_singleCharBuff, 0, 1);
        }

        public bool IsInterestingByte(byte b) {
            //"b>=33"のところはもうちょっとまじめに判定するべき。
            //文字の間にエスケープシーケンスが入るケースへの対応。
            return _cursor == 0 ? IsLeadByte(b) : b >= 33;
        }

        public void Reset() {
            _cursor = 0;
            _byte_len = 0;
        }

        /// <summary>
        /// <para>Append one byte.</para>
        /// <para>If the byte sequence is representing a character in this encoding, this method returns the character.
        /// Otherwise, this method returns \0 to indicate that more bytes are required.</para>
        /// <para>A character to be returned may be converted to a character in Unicode's private-use area by <see cref="Poderosa.Document.Unicode"/>.</para>
        /// </summary>
        /// <remarks>
        /// <para>By convert the character code, informations about font-type or character's width, that are appropriate for the current encoding,
        /// can get from the character code. See <see cref="Poderosa.Document.Unicode"/>.</para>
        /// </remarks>
        /// <param name="b">new byte</param>
        /// <param name="buff">char buffer (requires length >= 2)</param>
        /// <returns>count of chars stored into the buff.</returns>
        public int PutByte(byte b, char[] buff) {
            if (_cursor == 0)
                _byte_len = GetCharLength(b);
            _buffer[_cursor++] = b;
            if (_cursor < _byte_len) {
                return 0;
            }

            int len = _encoding.GetChars(_buffer, 0, _byte_len, buff, 0);
            _cursor = 0;

            if (IsIgnoreableChar(buff, len)) {
                return 0;
            }

            return len;
        }

        /// <summary>
        /// Create a new <see cref="UnicodeCharConverter"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public UnicodeCharConverter CreateUnicodeCharConverter() {
            return new UnicodeCharConverter(UseCJKCharacter);
        }

        public static EncodingProfile Get(EncodingType et) {
            EncodingProfile p = null;
            switch (et) {
                case EncodingType.ISO8859_1:
                    p = new ISO8859_1Profile();
                    break;
                case EncodingType.EUC_JP:
                    p = new EUCJPProfile();
                    break;
                case EncodingType.SHIFT_JIS:
                    p = new ShiftJISProfile();
                    break;
                case EncodingType.UTF8:
                    p = new UTF8Profile();
                    break;
                case EncodingType.UTF8_Latin:
                    p = new UTF8_LatinProfile();
                    break;
                case EncodingType.GB2312:
                    p = new GB2312Profile();
                    break;
                case EncodingType.BIG5:
                    p = new Big5Profile();
                    break;
                case EncodingType.EUC_CN:
                    p = new EUCCNProfile();
                    break;
                case EncodingType.EUC_KR:
                    p = new EUCKRProfile();
                    break;
                case EncodingType.OEM850:
                    p = new OEM850Profile();
                    break;
            }
            return p;
        }

        //NOTE これらはメソッドのoverrideでなくdelegateでまわしたほうが効率は若干よいのかも
        private class ISO8859_1Profile : EncodingProfile {
            public ISO8859_1Profile()
                : base(EncodingType.ISO8859_1, Encoding.GetEncoding("iso-8859-1")) {
            }
            protected override int GetCharLength(byte b) {
                return 1;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0xA0;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return false;
                }
            }
        }
        private class ShiftJISProfile : EncodingProfile {
            public ShiftJISProfile()
                : base(EncodingType.SHIFT_JIS, Encoding.GetEncoding("shift_jis")) {
            }
            protected override int GetCharLength(byte b) {
                return (b >= 0xA1 && b <= 0xDF) ? 1 : 2;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0x81 && b <= 0xFC;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }
        private class EUCJPProfile : EncodingProfile {
            public EUCJPProfile()
                : base(EncodingType.EUC_JP, Encoding.GetEncoding("euc-jp")) {
            }
            protected override int GetCharLength(byte b) {
                return b == 0x8F ? 3 : b >= 0x8E ? 2 : 1;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0x8E && b <= 0xFE;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }

        private abstract class UTF8ProfileBase : EncodingProfile {
            protected UTF8ProfileBase(EncodingType encodingType)
                : base(encodingType, Encoding.UTF8) {
            }

            protected override int GetCharLength(byte b) {
                if ((b & 0x80) == 0) {
                    return 1;
                }
                if ((b & 0x40) == 0) {
                    // invalid case in UTF-8
                    return 1;
                }
                if ((b & 0x20) == 0) {
                    return 2;
                }
                if ((b & 0x10) == 0) {
                    return 3;
                }
                if ((b & 0x08) == 0) {
                    return 4;
                }
                if ((b & 0x04) == 0) {
                    return 5;
                }
                if ((b & 0x02) == 0) {
                    return 6;
                }

                // invalid case in UTF-8
                return 1;
            }

            protected override bool IsLeadByte(byte b) {
                return b >= 0x80;
            }

            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                if (len == 1) {
                    switch (charBuff[0]) {
                        case '\uFFF9':  // INTERLINEAR ANNOTATION ANCHOR
                        case '\uFFFA':  // INTERLINEAR ANNOTATION SEPARATOR
                        case '\uFFFB':  // INTERLINEAR ANNOTATION TERMINATOR
                        case '\uFFFF':  // not a character
                        case '\uFFFE':  // BOM
                        case '\uFEFF':  // BOM
                        case '\uFE00':  // Variation Selector
                        case '\uFE01':  // Variation Selector
                        case '\uFE02':  // Variation Selector
                        case '\uFE03':  // Variation Selector
                        case '\uFE04':  // Variation Selector
                        case '\uFE05':  // Variation Selector
                        case '\uFE06':  // Variation Selector
                        case '\uFE07':  // Variation Selector
                        case '\uFE08':  // Variation Selector
                        case '\uFE09':  // Variation Selector
                        case '\uFE0A':  // Variation Selector
                        case '\uFE0B':  // Variation Selector
                        case '\uFE0C':  // Variation Selector
                        case '\uFE0D':  // Variation Selector
                        case '\uFE0E':  // Variation Selector
                        case '\uFE0F':  // Variation Selector
                            return true;
                    }
                }
                else {
                    if (charBuff[0] == '\udb40' && charBuff[1] >= '\udd00' && charBuff[1] <= '\uddef') {    // Variation Selector
                        return true;
                    }
                }

                return false;
            }
        }

        private class UTF8Profile : UTF8ProfileBase {
            public UTF8Profile()
                : base(EncodingType.UTF8) {
            }

            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }

        private class UTF8_LatinProfile : UTF8ProfileBase {
            public UTF8_LatinProfile()
                : base(EncodingType.UTF8_Latin) {
            }

            protected override bool UseCJKCharacter {
                get {
                    return false;
                }
            }
        }

        private class GB2312Profile : EncodingProfile {
            public GB2312Profile()
                : base(EncodingType.GB2312, Encoding.GetEncoding("gb2312")) {
            }
            protected override int GetCharLength(byte b) {
                return 2;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0xA1 && b <= 0xF7;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }

        private class Big5Profile : EncodingProfile {
            public Big5Profile()
                : base(EncodingType.BIG5, Encoding.GetEncoding("big5")) {
            }
            protected override int GetCharLength(byte b) {
                return 2;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0x81 && b <= 0xFE;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }

        private class EUCCNProfile : EncodingProfile {
            public EUCCNProfile()
                : base(EncodingType.EUC_CN, Encoding.GetEncoding("euc-cn")) {
            }
            protected override int GetCharLength(byte b) {
                return 2;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0xA1 && b <= 0xF7;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }

        private class EUCKRProfile : EncodingProfile {
            public EUCKRProfile()
                : base(EncodingType.EUC_KR, Encoding.GetEncoding("euc-kr")) {
            }
            protected override int GetCharLength(byte b) {
                return 2;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0xA1 && b <= 0xFE;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return true;
                }
            }
        }

        private class OEM850Profile : EncodingProfile {
            public OEM850Profile()
                : base(EncodingType.OEM850, Encoding.GetEncoding(850)) {
            }
            protected override int GetCharLength(byte b) {
                return 1;
            }
            protected override bool IsLeadByte(byte b) {
                return b >= 0x80;
            }
            protected override bool IsIgnoreableChar(char[] charBuff, int len) {
                return false;
            }
            protected override bool UseCJKCharacter {
                get {
                    return false;
                }
            }
        }
    }
}
