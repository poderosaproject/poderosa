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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading;

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

        private static readonly UnicodeWidthAndFontTypeTable _table = new UnicodeWidthAndFontTypeTable();

        /// <summary>
        /// Initialize internal table.
        /// </summary>
        /// <exception cref="Exception">error</exception>
        public static void Initialize() {
            _table.Initialize();
        }

        /// <summary>
        /// Gets <see cref="UnicodeCharFlags"/> for a Unicode code point.
        /// </summary>
        /// <param name="codePoint">Unicode code point</param>
        /// <param name="cjk">allow cjk mode</param>
        /// <returns></returns>
        public static UnicodeCharFlags DetermineUnicodeCharFlags(uint codePoint, bool cjk) {
            int charWidth;
            bool useCjkFont;
            _table.GetWidthAndFontType(codePoint, cjk, out charWidth, out useCjkFont);

            UnicodeCharFlags f = UnicodeCharFlags.None;
            if (charWidth >= 2) {
                f |= UnicodeCharFlags.WideWidth;
            }
            if (useCjkFont) {
                f |= UnicodeCharFlags.CJK;
            }
            return f;
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

    /// <summary>
    /// Unicode width / font-type table
    /// </summary>
    internal sealed class UnicodeWidthAndFontTypeTable {
        // Character widths are managed in the two-level index table.
        // The first index table determines a second index table from the upper bits of the code point.
        // The second index table determines the character width from the lower bits of the code point.
        //
        // In the second index table, a character width is represented in 2 bits.
        //
        //  bit0, bit1 : width
        //    0 --> zero width (invisible on its own)
        //    1 --> narrow
        //    2 --> wide
        //    3 --> narrow in non-CJK mode, wide in CJK mode
        //
        //
        // Character font types are managed in the two-level index table.
        // In the second index table, a character font-type is represented in 2 bits.
        //
        //  bit0, bit1 : font-type
        //    0 --> (not used)
        //    1 --> use non-CJK font
        //    2 --> use CJK font
        //    3 --> use CJK font in CJK mode, otherwise use non-CJK font

        private const int SUB_TABLE_UINT_NUM = 128; // length of uint[]
        private const int CHARS_PER_UINT = 16; // 2 bits per char
        private const int SUB_TABLE_CHARS = SUB_TABLE_UINT_NUM * CHARS_PER_UINT;

        // interface of the second index table
        private interface ISubTable {
            // returns value in 0..3
            int GetValue(uint codePoint);
        }

        // the second index table that returns 0 for all characters in a block
        private class SubTableAllZero : ISubTable {
            public int GetValue(uint codePoint) {
                return 0;
            }
        }

        // the second index table that returns 1 for all characters in a block
        private class SubTableAllOne : ISubTable {
            public int GetValue(uint codePoint) {
                return 1;
            }
        }

        // the second index table that returns 2 for all characters in a block
        private class SubTableAllTwo : ISubTable {
            public int GetValue(uint codePoint) {
                return 2;
            }
        }

        // the second index table that returns 3 for all characters in a block
        private class SubTableAllThree : ISubTable {
            public int GetValue(uint codePoint) {
                return 3;
            }
        }

        // the second index table that has a table
        private class SubTable : ISubTable {
            private readonly uint[] _table;

            public SubTable(uint[] table) {
                if (table.Length != SUB_TABLE_UINT_NUM) {
                    throw new ArgumentException();
                }
                _table = table;
            }

            public int GetValue(uint codePoint) {
                uint charIndex = codePoint % SUB_TABLE_CHARS;
                uint tableIndex = charIndex / CHARS_PER_UINT;
                int shift = (int)((charIndex % CHARS_PER_UINT) * 2);
                return (int)((_table[tableIndex] >> shift) & 0x3u);
            }
        }

        // the first index table
        private ISubTable[] _charWidthTables = new ISubTable[0];
        private ISubTable[] _fontTypeTables = new ISubTable[0];

        private int _initialized = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public UnicodeWidthAndFontTypeTable() {
        }

        /// <summary>
        /// Get character width from a code point
        /// </summary>
        /// <param name="codePoint">code point</param>
        /// <param name="cjk">returns values for the CJK mode</param>
        /// <param name="charWidth">returns character width; 0 (invisible), 1 (narrow), or 2 (wide)</param>
        /// <param name="useCjkFont">returns whether the character should be displayed using CJK font.</param>
        public void GetWidthAndFontType(uint codePoint, bool cjk, out int charWidth, out bool useCjkFont) {
            uint tableIndex = codePoint / SUB_TABLE_CHARS;

            if (tableIndex < _charWidthTables.Length) {
                int w = _charWidthTables[tableIndex].GetValue(codePoint);
                charWidth = (w <= 2) ? w : (cjk ? 2 : 1);
            }
            else {
                charWidth = 1; // default width
            }

            if (tableIndex < _fontTypeTables.Length) {
                int w = _fontTypeTables[tableIndex].GetValue(codePoint);
                useCjkFont = w == 2 || (w > 2 && cjk);
            }
            else {
                useCjkFont = false; // default
            }
        }

        /// <summary>
        /// Initialize internal table.
        /// </summary>
        /// <exception cref="Exception">error</exception>
        public void Initialize() {
            if (Interlocked.CompareExchange(ref _initialized, 1, 0) != 0) {
                return;
            }

#if DEBUG
            long mem1 = GC.GetTotalMemory(true);
#endif

            _charWidthTables = BuildTable("charwidth");

#if DEBUG
            long mem2 = GC.GetTotalMemory(true);
#endif

            _fontTypeTables = BuildTable("charfont");

#if DEBUG
            long mem3 = GC.GetTotalMemory(true);

            System.Diagnostics.Debug.WriteLine("Width Table     : {0}", mem2 - mem1);
            System.Diagnostics.Debug.WriteLine("Font Type Table : {0}", mem3 - mem2);
#endif
        }

        private static ISubTable[] BuildTable(string fileName) {
            return Split(LoadTable(fileName), typeof(SubTableAllOne));
        }

        private static ISubTable[] Split(uint[] bigTable, Type trimmableSubTableType) {
            List<ISubTable> subTables = new List<ISubTable>();

            SubTableAllZero allZero = new SubTableAllZero();
            SubTableAllOne allOne = new SubTableAllOne();
            SubTableAllTwo allTwo = new SubTableAllTwo();
            SubTableAllThree allThree = new SubTableAllThree();

            for (int btIndex = 0; btIndex < bigTable.Length; btIndex += SUB_TABLE_UINT_NUM) {
                uint p0 = bigTable[btIndex];
                bool isUniform = true;
                for (int i = 1; i < SUB_TABLE_UINT_NUM; i++) {
                    if (bigTable[btIndex + i] != p0) {
                        isUniform = false;
                        break;
                    }
                }

                if (isUniform) {
                    switch (p0) {
                        case 0x00000000u:
                            subTables.Add(allZero);
                            continue;
                        case 0x55555555u:
                            subTables.Add(allOne);
                            continue;
                        case 0xaaaaaaaau:
                            subTables.Add(allTwo);
                            continue;
                        case 0xffffffffu:
                            subTables.Add(allThree);
                            continue;
                    }
                }

                uint[] t = new uint[SUB_TABLE_UINT_NUM];
                Array.Copy(bigTable, btIndex, t, 0, SUB_TABLE_UINT_NUM);
                subTables.Add(new SubTable(t));
            }

            while (subTables[subTables.Count - 1].GetType().Equals(trimmableSubTableType)) {
                subTables.RemoveAt(subTables.Count - 1);
            }

            return subTables.ToArray();
        }

        private static uint[] LoadTable(string fileName) {
            Assembly assy = Assembly.GetAssembly(typeof(UnicodeWidthAndFontTypeTable));
            if (assy.Location.Length == 0) {
                throw new Exception(String.Format("Cannot get the assembly location: {0}", assy.FullName));
            }

            string filePath = Path.Combine(Path.GetDirectoryName(assy.Location), fileName);
            if (!File.Exists(filePath)) {
                throw new Exception(String.Format("Table file not found: {0}", filePath));
            }

            uint[] table = new uint[0x110000 / CHARS_PER_UINT];
            // initialize table with the default value
            FillTable(table, 0x55555555u);

            using (StreamReader fin = new StreamReader(filePath, Encoding.UTF8)) {
                string line;
                while ((line = fin.ReadLine()) != null) {
                    int hashPos = line.IndexOf('#');
                    if (hashPos == 0) {
                        continue;
                    }
                    if (hashPos > 0) {
                        line = line.Substring(0, hashPos);
                    }
                    string[] s = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (s.Length == 3) {
                        uint from = Convert.ToUInt32(s[0].TrimStart('U'), 16);
                        uint to = Convert.ToUInt32(s[1].TrimStart('U'), 16);
                        int width = Convert.ToInt32(s[2], 10);
                        SetTableValueRange(table, from, to, width);
                    }
                    else if (s.Length == 2) {
                        uint cp = Convert.ToUInt32(s[0].TrimStart('U'), 16);
                        int width = Convert.ToInt32(s[1], 10);
                        SetTableValue(table, cp, width);
                    }
                }
            }

            return table;
        }

        private static void FillTable(uint[] table, uint fill) {
            for (int i = 0; i < 32; i++) {
                table[i] = fill;
            }
            int filledSize = 32;
            while (filledSize < table.Length) {
                int remSize = Math.Min(table.Length - filledSize, filledSize);
                Array.Copy(table, 0, table, filledSize, remSize);
                filledSize += remSize;
            }
        }

        private static void SetTableValue(uint[] table, uint codePoint, int value) {
            uint index = codePoint / CHARS_PER_UINT;
            int shift = (int)((codePoint % CHARS_PER_UINT) * 2);
            uint mask = 0x3u << shift;
            uint wbits = ((uint)value & 0x3u) << shift;
            table[index] = table[index] & ~mask | wbits;
        }

        private static void SetTableValueRange(uint[] table, uint from, uint to, int value) {
            uint codePoint = from;
            while (codePoint <= to) {
                if (codePoint % CHARS_PER_UINT == 0 && codePoint + CHARS_PER_UINT <= to) {
                    uint index = codePoint / CHARS_PER_UINT;
                    switch ((uint)value & 0x3u) {
                        case 0u:
                            table[index] = 0x00000000u;
                            break;
                        case 1u:
                            table[index] = 0x55555555u;
                            break;
                        case 2u:
                            table[index] = 0xaaaaaaaau;
                            break;
                        case 3u:
                            table[index] = 0xffffffffu;
                            break;
                    }
                    codePoint += CHARS_PER_UINT;
                    continue;
                }

                SetTableValue(table, codePoint, value);
                codePoint++;
            }
        }
    }

}
