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
using System.IO;
using System.Diagnostics;
using System.Resources;

using Poderosa.Protocols;

namespace Poderosa.Terminal {
    public enum CharacterSetSizeType {
        /// <summary>
        /// 94-character
        /// </summary>
        CS94,
        /// <summary>
        /// 96-character
        /// </summary>
        CS96,
        /// <summary>
        /// Other (e.g. 94x94 character)
        /// </summary>
        Other,
        /// <summary>
        /// Not designated
        /// </summary>
        NotDesignated,
    }

    public interface ICharacterSetManager {
        /// <summary>
        /// Get the character corresponding to the code in the current character set.
        /// </summary>
        /// <param name="code">code (0-255)</param>
        /// <returns>a character corresponding to the code if it exists. otherwise null.</returns>
        char? GetCharacer(byte code);

        /// <summary>
        /// Get character set size of G0, G1, G2 or G3.
        /// </summary>
        /// <param name="g">0=G0, 1=G1, 2=G2, 3=G3</param>
        /// <returns>character set size type</returns>
        CharacterSetSizeType GetCharacterSetSizeType(int g);

        /// <summary>
        /// Get character set designator in SCS (Select Character Set).
        /// </summary>
        /// <param name="g">0=G0, 1=G1, 2=G2, 3=G3</param>
        /// <returns>designator (e.g. "B") or null if the character set cannot be designated in SCS.</returns>
        string GetSCSDesignator(int g);

        /// <summary>
        /// Get character set mapping
        /// </summary>
        /// <returns>character set mapping</returns>
        CharacterSetMapping GetCharacterSetMapping();

        /// <summary>
        /// Restore character set mapping
        /// </summary>
        /// <param name="csMap">character set mapping</param>
        void RestoreCharacterSetMapping(CharacterSetMapping csMap);
    }

    internal interface ICharDecoder : ICharacterSetManager {
        void OnReception(ByteDataFragment data);
        void Reset();
        EncodingProfile CurrentEncoding {
            get;
        }
    }

    internal class ISO2022CharDecoder : ICharDecoder {

        private class ByteProcessorBuffer {
            private readonly MemoryStream _buffer = new MemoryStream(0x1000);
            public void Reset() {
                _buffer.SetLength(0);
            }
            public void Write(byte[] bytes) {
                _buffer.Write(bytes, 0, bytes.Length);
            }
            public void WriteByte(byte b) {
                _buffer.WriteByte(b);
            }
            public byte[] GetBytes() {
                return _buffer.ToArray();
            }
        }

        private interface IByteProcessor {
            void ProcessByte(byte b);
            void Init();
            void Flush();
            char? GetCharacer(byte code);
        }

        private class ASCIIByteProcessor : IByteProcessor {
            private readonly ICharProcessor _processor;
            public ASCIIByteProcessor(ICharProcessor processor) {
                _processor = processor;
            }
            public void ProcessByte(byte b) {
                _processor.ProcessChar((char)b);
            }
            public void Init() {
            }
            public void Flush() {
            }

            public char? GetCharacer(byte code) {
                return GetCharacerFallback(code);
            }

            public static char? GetCharacerFallback(byte code) {
                if ((code >= 0x20 && code <= 0x7e) || (code >= 0xa0 && code <= 0xff)) {
                    return (char)code;
                }
                else {
                    return null;
                }
            }
        }

        private class DECLineByteProcessor : IByteProcessor {

            private static readonly char[] DEC_SPECIAL_CHARACTERS = {
                '\u2666',   // 60h --> BLACK DIAMOND SUIT
                '\u2592',   // 61h --> MEDIUM SHADE
                //'\u2588',   // 61h --> FULL BLOCK
                '\u2409',   // 62h --> SYMBOL FOR HORIZONTAL TABULATION
                '\u240c',   // 63h --> SYMBOL FOR FORM FEED
                '\u240d',   // 64h --> SYMBOL FOR CARRIAGE RETURN
                '\u240a',   // 65h --> SYMBOL FOR LINE FEED
                '\u00b0',   // 66h --> DEGREE SIGN
                '\u00b1',   // 67h --> PLUS-MINUS SIGN
                '\u2424',   // 68h --> SYMBOL FOR NEWLINE
                '\u240b',   // 69h --> SYMBOL FOR VERTICAL TABULATION
                '\u2518',   // 6Ah --> BOX DRAWINGS LIGHT UP AND LEFT
                '\u2510',   // 6Bh --> BOX DRAWINGS LIGHT DOWN AND LEFT
                '\u250c',   // 6Ch --> BOX DRAWINGS LIGHT DOWN AND RIGHT
                '\u2514',   // 6Dh --> BOX DRAWINGS LIGHT UP AND RIGHT
                '\u253c',   // 6Eh --> BOX DRAWINGS LIGHT VERTICAL AND HORIZONTAL
                '\u23ba',   // 6Fh --> HORIZONTAL SCAN LINE-1
                '\u23bb',   // 70h --> HORIZONTAL SCAN LINE-3
                '\u2500',   // 71h --> BOX DRAWINGS LIGHT HORIZONTAL
                '\u23bc',   // 72h --> HORIZONTAL SCAN LINE-7
                '\u23bd',   // 73h --> HORIZONTAL SCAN LINE-9
                '\u251c',   // 74h --> BOX DRAWINGS LIGHT VERTICAL AND RIGHT
                '\u2524',   // 75h --> BOX DRAWINGS LIGHT VERTICAL AND LEFT
                '\u2534',   // 76h --> BOX DRAWINGS LIGHT UP AND HORIZONTAL
                '\u252c',   // 77h --> BOX DRAWINGS LIGHT DOWN AND HORIZONTAL
                '\u2502',   // 78h --> BOX DRAWINGS LIGHT VERTICAL
                //'\u2a7d', // 79h --> LESS-THAN OR SLANTED EQUAL TO
                '\u2264',   // 79h --> LESS-THAN OR EQUAL TO
                //'\u2a7e', // 7Ah --> GREATER-THAN OR SLANTED EQUAL TO
                '\u2265',   // 7Ah --> GREATER-THAN OR EQUAL TO
                '\u03c0',   // 7Bh --> GREEK SMALL LETTER PI
                '\u2260',   // 7Ch --> NOT EQUAL TO
                '\u00a3',   // 7Dh --> POUND SIGN
                '\u00b7',   // 7Eh --> MIDDLE DOT
                '\u2421',   // 7Fh --> SYMBOL FOR DELETE
            };

            private readonly ICharProcessor _processor;

            public DECLineByteProcessor(ICharProcessor processor) {
                _processor = processor;
            }

            public void ProcessByte(byte b) {
                char ch;
                if (0x60 <= b && b <= 0x7F)
                    ch = DEC_SPECIAL_CHARACTERS[b - 0x60];
                else
                    ch = (char)b;
                _processor.ProcessChar(ch);
            }

            public void Init() {
            }

            public void Flush() {
            }

            public char? GetCharacer(byte code) {
                if (0x60 <= code && code <= 0x7F) {
                    return DEC_SPECIAL_CHARACTERS[code - 0x60];
                }
                return ASCIIByteProcessor.GetCharacerFallback(code);
            }
        }

        private class CJKByteProcessor : IByteProcessor {
            private readonly ICharProcessor _processor;
            private readonly Encoding _encoding;
            private readonly ByteProcessorBuffer _buffer;
            private readonly byte[] _leadingBytes;
            private readonly byte[] _trailingBytes;

            public CJKByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer, Encoding encoding, byte[] leadingBytes, byte[] trailingBytes) {
                _processor = processor;
                _encoding = encoding;
                _buffer = buffer;
                _leadingBytes = leadingBytes;
                _trailingBytes = trailingBytes;
            }

            public void ProcessByte(byte b) {
                _buffer.WriteByte(b);
            }
            public void Init() {
                _buffer.Reset();
                if (_leadingBytes != null)
                    _buffer.Write(_leadingBytes);
            }
            public void Flush() {
                if (_trailingBytes != null)
                    _buffer.Write(_trailingBytes);
                string text = _encoding.GetString(_buffer.GetBytes());
                foreach (char c in text) {
                    _processor.ProcessChar(c);
                }
            }
            public char? GetCharacer(byte code) {
                return ASCIIByteProcessor.GetCharacerFallback(code);
            }
        }

        private class ISO2022JPByteProcessor : CJKByteProcessor {
            private static byte[] _jpLeadingBytes = new byte[] { (byte)0x1b, (byte)'$', (byte)'(', (byte)'D' };

            public ISO2022JPByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer)
                : base(processor, buffer, Encoding.GetEncoding("iso-2022-jp"), _jpLeadingBytes, null) {
            }
        }

        private class ISO2022JPKanaByteProcessor : CJKByteProcessor {
            private static byte[] _kanaLeadingBytes = new byte[] { (byte)0x1b, (byte)'(', (byte)'I' };

            public ISO2022JPKanaByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer)
                : base(processor, buffer, Encoding.GetEncoding("iso-2022-jp"), _kanaLeadingBytes, null) {
            }
        }

        private class ISO2022KRByteProcessor : CJKByteProcessor {
            private static byte[] _krLeadingBytes = new byte[] { (byte)0x1b, (byte)'$', (byte)')', (byte)'C', (byte)0x0e };

            public ISO2022KRByteProcessor(ICharProcessor processor, ByteProcessorBuffer buffer)
                : base(processor, buffer, Encoding.GetEncoding("iso-2022-kr"), _krLeadingBytes, null) {
            }
        }


        private class EscapeSequenceBuffer {
            private readonly byte[] _buffer = new byte[10];
            private int _len = 0;

            public EscapeSequenceBuffer() {
            }

            public byte[] Buffer {
                get {
                    return _buffer;
                }
            }
            public int Length {
                get {
                    return _len;
                }
            }

            public void Append(byte b) {
                if (_len < _buffer.Length)
                    _buffer[_len++] = b;
            }
            public void Reset() {
                _len = 0;
            }
        }

        //次の入力に繋げるための状態
        private enum State {
            Normal, //標準
            ESC,    //ESCが来たところ
            ESC_DOLLAR,    //ESC $が来たところ
            ESC_BRACKET,   //ESC (が来たところ
            ESC_ENDBRACKET,   //ESC )が来たところ
            ESC_DOLLAR_BRACKET,   //ESC $ (が来たところ
            ESC_DOLLAR_ENDBRACKET    //ESC $ )が来たところ
        }
        private State _state;

        private EscapeSequenceBuffer _escseq;

        private readonly EncodingProfile _encodingProfile;
        private readonly EncodingProfile.Decoder _decoder;

        //文字を処理するターミナル
        private ICharProcessor _processor;

        public ISO2022CharDecoder(ICharProcessor processor, EncodingProfile enc) {
            _escseq = new EscapeSequenceBuffer();
            _processor = processor;
            _state = State.Normal;
            _encodingProfile = enc;
            _decoder = enc.CreateDecoder();

            _asciiByteProcessor = new ASCIIByteProcessor(processor);
            _currentByteProcessor = _asciiByteProcessor;
            _decLineByteProcessor = new Lazy<DECLineByteProcessor>(() => new DECLineByteProcessor(_processor), false);
            _iso2022jpByteProcessor = new Lazy<ISO2022JPByteProcessor>(() => new ISO2022JPByteProcessor(_processor, _byteProcessorBuffer), false);
            _iso2022jpkanaByteProcessor = new Lazy<ISO2022JPKanaByteProcessor>(() => new ISO2022JPKanaByteProcessor(_processor, _byteProcessorBuffer), false);
            _iso2022krByteProcessor = new Lazy<ISO2022KRByteProcessor>(() => new ISO2022KRByteProcessor(_processor, _byteProcessorBuffer), false);
            _G0ByteProcessor = _asciiByteProcessor;
            _G1ByteProcessor = _asciiByteProcessor;

            _byteProcessorBuffer = new ByteProcessorBuffer();
        }

        public EncodingProfile CurrentEncoding {
            get {
                return _encodingProfile;
            }
        }

        private IByteProcessor _currentByteProcessor;
        private IByteProcessor _G0ByteProcessor; //iso2022のG0,G1
        private IByteProcessor _G1ByteProcessor;

        private readonly ASCIIByteProcessor _asciiByteProcessor;

        private readonly Lazy<DECLineByteProcessor> _decLineByteProcessor;
        private readonly Lazy<ISO2022JPByteProcessor> _iso2022jpByteProcessor;
        private readonly Lazy<ISO2022JPKanaByteProcessor> _iso2022jpkanaByteProcessor;
        private readonly Lazy<ISO2022KRByteProcessor> _iso2022krByteProcessor;

        private ByteProcessorBuffer _byteProcessorBuffer;

        public CharacterSetSizeType GetCharacterSetSizeType(int g) {
            IByteProcessor processor;
            if (g == 0) {
                processor = _G0ByteProcessor;
            }
            else if (g == 1) {
                processor = _G1ByteProcessor;
            }
            else {
                return CharacterSetSizeType.NotDesignated;
            }

            if (processor is ASCIIByteProcessor || processor is DECLineByteProcessor) {
                return CharacterSetSizeType.CS94;
            }

            return CharacterSetSizeType.Other;
        }

        public string GetSCSDesignator(int g) {
            IByteProcessor processor;
            if (g == 0) {
                processor = _G0ByteProcessor;
            }
            else if (g == 1) {
                processor = _G1ByteProcessor;
            }
            else {
                return null;
            }

            if (processor is ASCIIByteProcessor) {
                return "B";
            }

            if (processor is DECLineByteProcessor) {
                return "0";
            }

            return null;
        }

        public CharacterSetMapping GetCharacterSetMapping() {
            string byteProcessorName;
            if (_currentByteProcessor == null) {
                byteProcessorName = String.Empty;
            }
            else {
                byteProcessorName = _currentByteProcessor.GetType().Name;
            }

            return new CharacterSetMapping(
                byteProcessorName: byteProcessorName
            );
        }

        public void RestoreCharacterSetMapping(CharacterSetMapping csMap) {
            IByteProcessor byteProcessor;
            switch (csMap.ByteProcessorName) {
                case "Default":
                case "ASCIIByteProcessor":
                    byteProcessor = _asciiByteProcessor;
                    break;
                case "DECLineByteProcessor":
                    byteProcessor = _decLineByteProcessor.Value;
                    break;
                case "ISO2022JPByteProcessor":
                    byteProcessor = _iso2022jpByteProcessor.Value;
                    break;
                case "ISO2022JPKanaByteProcessor":
                    byteProcessor = _iso2022jpkanaByteProcessor.Value;
                    break;
                case "ISO2022KRByteProcessor":
                    byteProcessor = _iso2022krByteProcessor.Value;
                    break;
                default:
                    byteProcessor = null;
                    break;
            }

            if (byteProcessor != null) {
                ChangeProcessor(byteProcessor);
            }
        }

        public char? GetCharacer(byte code) {
            return _currentByteProcessor.GetCharacer(code);
        }

        public void OnReception(ByteDataFragment data) {
            //処理本体
            byte[] t = data.Buffer;
            int last = data.Offset + data.Length;
            int offset = data.Offset;
            while (offset < last) {
                ProcessByte(t[offset++]);
            }
        }

        private void ProcessByte(byte b) {
            if (_processor.IsEscapeSequenceReading)
                _processor.ProcessChar((char)b);
            else {
                if (_state == State.Normal && !IsControlChar(b) && _decoder.IsInterestingByte(b)) {
                    PutMBCSByte(b);
                }
                else {
                    switch (_state) {
                        case State.Normal:
                            if (b == 0x1B) { //ESC
                                _escseq.Reset();
                                _escseq.Append(b);
                                _state = State.ESC;
                            }
                            else if (b == 14) //SO
                                ChangeProcessor(_G1ByteProcessor);
                            else if (b == 15) //SI
                                ChangeProcessor(_G0ByteProcessor);
                            else
                                ConsumeByte(b);
                            break;
                        case State.ESC:
                            _escseq.Append(b);
                            if (b == (byte)'$')
                                _state = State.ESC_DOLLAR;
                            else if (b == (byte)'(')
                                _state = State.ESC_BRACKET;
                            else if (b == (byte)')')
                                _state = State.ESC_ENDBRACKET;
                            else {
                                ConsumeBytes(_escseq.Buffer, _escseq.Length);
                                _state = State.Normal;
                            }
                            break;
                        case State.ESC_BRACKET:
                            _escseq.Append(b);
                            if (b == (byte)'0') {
                                _G0ByteProcessor = _decLineByteProcessor.Value;
                                ChangeProcessor(_G0ByteProcessor);
                                _state = State.Normal;
                            }
                            else if (b == (byte)'B' || b == (byte)'J' || b == (byte)'~') { //!!lessでssh2architecture.txtを見ていたら来た。詳細はまだ調べていない。
                                _G0ByteProcessor = _asciiByteProcessor;
                                ChangeProcessor(_G0ByteProcessor);
                                _state = State.Normal;
                            }
                            else {
                                _processor.UnsupportedCharSetDetected((char)b);
                                ConsumeBytes(_escseq.Buffer, _escseq.Length);
                                _state = State.Normal;
                            }
                            break;
                        case State.ESC_ENDBRACKET:
                            _escseq.Append(b);
                            if (b == (byte)'0') {
                                _G1ByteProcessor = _decLineByteProcessor.Value;
                                _state = State.Normal;
                            }
                            else if (b == (byte)'B' || b == (byte)'J' || b == (byte)'~') { //!!lessでssh2architecture.txtを見ていたら来た。詳細はまだ調べていない。
                                _G1ByteProcessor = _asciiByteProcessor;
                                _state = State.Normal;
                            }
                            else {
                                ConsumeBytes(_escseq.Buffer, _escseq.Length);
                                _state = State.Normal;
                            }
                            break;
                        case State.ESC_DOLLAR:
                            _escseq.Append(b);
                            if (b == (byte)'(')
                                _state = State.ESC_DOLLAR_BRACKET;
                            else if (b == (byte)')')
                                _state = State.ESC_DOLLAR_ENDBRACKET;
                            else if (b == (byte)'B' || b == (byte)'@') {
                                _G0ByteProcessor = _iso2022jpByteProcessor.Value;
                                ChangeProcessor(_G0ByteProcessor);
                                _state = State.Normal;
                            }
                            else {
                                _processor.UnsupportedCharSetDetected((char)b);
                                ConsumeBytes(_escseq.Buffer, _escseq.Length);
                                _state = State.Normal;
                            }
                            break;
                        case State.ESC_DOLLAR_BRACKET:
                            _escseq.Append(b);
                            if (b == (byte)'C') {
                                _G0ByteProcessor = _iso2022krByteProcessor.Value;
                                ChangeProcessor(_G0ByteProcessor);
                                _state = State.Normal;
                            }
                            else if (b == (byte)'D') {
                                _G0ByteProcessor = _iso2022jpByteProcessor.Value;
                                ChangeProcessor(_G0ByteProcessor);
                                _state = State.Normal;
                            }
                            else if (b == (byte)'I') {
                                _G0ByteProcessor = _iso2022jpkanaByteProcessor.Value;
                                ChangeProcessor(_G0ByteProcessor);
                                _state = State.Normal;
                            }
                            else {
                                _processor.UnsupportedCharSetDetected((char)b);
                                ConsumeBytes(_escseq.Buffer, _escseq.Length);
                                _state = State.Normal;
                            }
                            break;
                        case State.ESC_DOLLAR_ENDBRACKET:
                            _escseq.Append(b);
                            if (b == (byte)'C') {
                                _G1ByteProcessor = _iso2022krByteProcessor.Value;
                                _state = State.Normal;
                            }
                            else {
                                ConsumeBytes(_escseq.Buffer, _escseq.Length);
                                _state = State.Normal;
                            }
                            break;
                        default:
                            Debug.Assert(false, "unexpected state transition");
                            break;
                    }
                }
            }
        }

        private void ChangeProcessor(IByteProcessor newprocessor) {
            //既存のやつがあればリセット
            if (_currentByteProcessor != null) {
                _currentByteProcessor.Flush();
            }

            if (newprocessor != null) {
                newprocessor.Init();
            }

            _currentByteProcessor = newprocessor;
            _state = State.Normal;
        }

        private void ConsumeBytes(byte[] buff, int len) {
            for (int i = 0; i < len; i++) {
                ConsumeByte(buff[i]);
            }
        }

        private void ConsumeByte(byte b) {
            _currentByteProcessor.ProcessByte(b);
        }


        public void Reset() {
            _decoder.Reset();
        }

        private static bool IsControlChar(byte b) {
            return b <= 0x1F;
        }

        private readonly char[] _encodeCharBuff = new char[2];

        private void PutMBCSByte(byte b) {
            try {
                int len = _decoder.PutByte(b, _encodeCharBuff);
                if (len > 0) {
                    _processor.ProcessChar(_encodeCharBuff[0]);
                    if (len > 1) {
                        _processor.ProcessChar(_encodeCharBuff[1]);
                    }
                }
            }
            catch (Exception) {
                _processor.InvalidCharDetected(_decoder.GetBuffer());
                _decoder.Reset();
            }
        }
    }

    /// <summary>
    /// Saved character set mapping
    /// </summary>
    public class CharacterSetMapping {
        internal readonly string ByteProcessorName;

        internal CharacterSetMapping(string byteProcessorName) {
            this.ByteProcessorName = byteProcessorName;
        }

        internal static CharacterSetMapping GetDefault() {
            return new CharacterSetMapping(byteProcessorName: "Default");
        }
    }
}
