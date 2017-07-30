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

        /// <summary>
        /// Decoder
        /// </summary>
        internal class Decoder {
            private readonly EncodingProfile _profile;
            private readonly byte[] _buffer;
            private int _bytes;
            private int _bytesNeeded;

            public Decoder(EncodingProfile profile) {
                _profile = profile;
                _buffer = new byte[6];
                _bytes = 0;
                _bytesNeeded = 0;
            }

            /// <summary>
            /// Reset status
            /// </summary>
            public void Reset() {
                _bytes = 0;
                _bytesNeeded = 0;
            }

            /// <summary>
            /// Check whether the next byte should be processed with this decoder.
            /// </summary>
            /// <param name="b">the next byte</param>
            /// <returns>true if the next byte hould be processed with this decoder.</returns>
            public bool IsInterestingByte(byte b) {
                // FIXME:
                // "b>=33" is an adhoc check for the case that the escape sequence was interleaved.
                return _bytes == 0 ? _profile.IsLeadByte(b) : b >= 33;
            }

            /// <summary>
            /// <para>Append one byte.</para>
            /// <para>If the byte sequence is representing a character in this encoding, this method returns the character.</para>
            /// </summary>
            /// <param name="b">new byte</param>
            /// <param name="buff">char buffer (requires length >= 2)</param>
            /// <returns>count of chars stored into the buff.</returns>
            public int PutByte(byte b, char[] buff) {
                if (_bytes == 0) {
                    _bytesNeeded = _profile.GetCharLength(b);
                }
                _buffer[_bytes++] = b;
                if (_bytes < _bytesNeeded) {
                    return 0;
                }

                int len = _profile.Encoding.GetChars(_buffer, 0, _bytes, buff, 0);
                _bytes = 0;

                if (_profile.IsIgnoreableChar(buff, len)) {
                    return 0;
                }

                return len;
            }

            /// <summary>
            /// Gets copy of the internal buffer.
            /// </summary>
            /// <returns>copy of the internal buffer</returns>
            public byte[] GetBuffer() {
                byte[] buff = new byte[_bytes];
                Buffer.BlockCopy(_buffer, 0, buff, 0, _bytes);
                return buff;
            }
        }

        /// <summary>
        /// Encoder
        /// </summary>
        internal class Encoder {
            private readonly EncodingProfile _profile;
            private readonly char[] _buff;
            private bool _needLowSurrogate;

            public Encoder(EncodingProfile profile) {
                _profile = profile;
                _buff = new char[2];
                _needLowSurrogate = false;
            }

            /// <summary>
            /// Reset status
            /// </summary>
            public void Reset() {
                _needLowSurrogate = false;
            }

            /// <summary>
            /// Encode into the bytes
            /// </summary>
            /// <param name="ch">next character</param>
            /// <param name="bytes">encoded result is set if a character was encoded.</param>
            /// <returns>true if a character was encoded.</returns>
            public bool GetBytes(char ch, out byte[] bytes) {
                if (_needLowSurrogate) {
                    _needLowSurrogate = false;
                    if (Char.IsLowSurrogate(ch)) {
                        _buff[1] = ch;
                        bytes = _profile.Encoding.GetBytes(_buff, 0, 2);
                        return true;
                    }

                    // fall through.
                    // ignore high surrogate.
                }

                if (Char.IsHighSurrogate(ch)) {
                    _buff[0] = ch;
                    _needLowSurrogate = true;
                    bytes = null;
                    return false;
                }

                _buff[0] = ch;
                bytes = _profile.Encoding.GetBytes(_buff, 0, 1);
                return true;
            }

            /// <summary>
            /// Encode into the bytes
            /// </summary>
            /// <param name="chs">characters</param>
            /// <param name="bytes">encoded result is set if a character was encoded.</param>
            /// <returns>true if a character was encoded.</returns>
            public bool GetBytes(char[] chs, out byte[] bytes) {
                if (chs.Length == 0) {
                    bytes = null;
                    return false;
                }

                char[] src = chs;
                int srcLen = chs.Length;
                if (_needLowSurrogate) {
                    _needLowSurrogate = false;
                    if (Char.IsLowSurrogate(chs[0])) {
                        src = new char[chs.Length + 1];
                        src[0] = _buff[0];
                        Buffer.BlockCopy(chs, 0, src, 1, chs.Length * sizeof(char));
                        srcLen = chs.Length + 1;
                    }
                }

                if (Char.IsHighSurrogate(src[src.Length - 1])) {
                    _buff[0] = src[src.Length - 1];
                    _needLowSurrogate = true;
                    srcLen--;
                }

                if (srcLen <= 0) {
                    bytes = null;
                    return false;
                }

                bytes = _profile.Encoding.GetBytes(src, 0, srcLen);
                return true;
            }
        }

        private readonly Encoding _encoding;
        private readonly EncodingType _type;

        protected EncodingProfile(EncodingType t, Encoding enc) {
            _type = t;
            _encoding = enc;
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

        /// <summary>
        /// Create a new <see cref="UnicodeCharConverter"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public UnicodeCharConverter CreateUnicodeCharConverter() {
            return new UnicodeCharConverter(UseCJKCharacter);
        }

        /// <summary>
        /// Create a new <see cref="Encoder"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public Encoder CreateEncoder() {
            return new Encoder(this);
        }

        /// <summary>
        /// Create a new <see cref="Decoder"/>.
        /// </summary>
        /// <returns>new instance</returns>
        public Decoder CreateDecoder() {
            return new Decoder(this);
        }

        /// <summary>
        /// Create a new instance.
        /// </summary>
        /// <param name="et">encoding type</param>
        /// <returns>new instance</returns>
        public static EncodingProfile Create(EncodingType et) {
            switch (et) {
                case EncodingType.ISO8859_1:
                    return new ISO8859_1Profile();
                case EncodingType.EUC_JP:
                    return new EUCJPProfile();
                case EncodingType.SHIFT_JIS:
                    return new ShiftJISProfile();
                case EncodingType.UTF8:
                    return new UTF8Profile();
                case EncodingType.UTF8_Latin:
                    return new UTF8_LatinProfile();
                case EncodingType.GB2312:
                    return new GB2312Profile();
                case EncodingType.BIG5:
                    return new Big5Profile();
                case EncodingType.EUC_CN:
                    return new EUCCNProfile();
                case EncodingType.EUC_KR:
                    return new EUCKRProfile();
                case EncodingType.OEM850:
                    return new OEM850Profile();
                default:
                    return null;
            }
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

    /// <summary>
    /// A cache bound with <see cref="EncodingType"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class CacheByEncodingType<T> where T : class {

        private readonly Func<EncodingProfile, T> _creator;
        private EncodingProfile _encodingProfile;
        private T _instance = null;

        public CacheByEncodingType(Func<EncodingProfile, T> creator) {
            _creator = creator;
        }

        public EncodingProfile EncodingProfile {
            get {
                return _encodingProfile;
            }
        }

        public T Get(EncodingType encodingType) {
            if (_instance == null || _encodingProfile == null || _encodingProfile.Type != encodingType) {
                _encodingProfile = EncodingProfile.Create(encodingType);
                _instance = _creator(_encodingProfile);
            }
            return _instance;
        }
    }

}
