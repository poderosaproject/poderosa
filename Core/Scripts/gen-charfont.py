# Generate character font-type table based on:
#    unicode CJK blocks
#    unicode-org/icu-data    charset/data/ucm/*.ucm

import pathlib
import re
import sys

def usage():
    print('Usage: gen-charfont.py <icu-data-dir>', file=sys.stderr)

# char_font_type
#   0 --> (not used)
#   1 --> use non-CJK font
#   2 --> use CJK font
#   3 --> use non-CJK font in non-CJK mode, use CJK font in CJK mode
char_font_type = [1 for _ in range(0x110000)]

def set_base_cjk_chars():
    """Set base character font-type."""
    global char_font_type

    def set_cjk(cfrom, cto):
        for c in range(cfrom, cto):
            char_font_type[c] = 2

    # Based on the Unicode block described in
    # "Unicode Standard Version 15.0 Core Specification, Chapter 18 East Asia"

    # Hangul Jamo (U+1100 – U+11FF)
    set_cjk(0x1100, 0x11FF)

    # CJK Radicals Supplement (U+2E80 - U+2EFF)
    # Kangxi Radicals (U+2F00 – U+2FDF)
    set_cjk(0x2E80, 0x2FDF)

    # Ideographic Description Characters (U+2FF0 – U+2FFF)
    # CJK Symbols and Punctuation (U+3000 - U+303F)
    # Hiragana (U+3040 - U+309F)
    # Katakana (U+30A0 - U+30FF)
    # Bopomofo (U+3100 – U+312F)
    # Hangul Compatibility Jamo (U+3130 – U+318F)
    # Kanbun (U+3190 – U+319F)
    # Bopomofo Extended (U+31A0 – U+31BF)
    # CJK Strokes (U+31C0 – U+31EF)
    # Katakana Phonetic Extensions (U+31F0 – U+31FF)
    # Enclosed CJK Letters and Months (U+3200 – U+32FF)
    # CJK Compatibility (U+3300 - U+33FF)
    # CJK Unified Ideographs Extension A (U+3400 – U+4DBF)
    set_cjk(0x2FF0, 0x4DBF)

    # CJK Unified Ideographs (U+4E00 – U+9FFF)
    # Yi (U+A000 – U+A4CF)
    # Lisu (U+A4D0 – U+A4FF)
    set_cjk(0x4E00, 0xA4FF)

    # Hangul Jamo Extended-A (U+A960 – U+A97F)
    set_cjk(0xA960, 0xA97F)

    # Hangul Syllables (U+AC00 – U+D7AF)
    # Hangul Jamo Extended-B (U+D7B0 – U+D7FF)
    set_cjk(0xAC00, 0xD7FF)

    # CJK Compatibility Ideographs (U+F900 – U+FAFF)
    set_cjk(0xF900, 0xFAFF)

    # Halfwidth and Fullwidth Forms (U+FF00 – U+FFEF)
    set_cjk(0xFF00, 0xFFEF)

    # Miao (U+16F00 – U+16F9F)
    set_cjk(0x16F00, 0x16F9F)

    # Ideographic Symbols and Punctuation (U+16FE0 – U+16FFF)
    # Tangut (U+17000 – U+187FF)
    # Tangut Components (U+18800 - U+18AFF)
    # Khitan Small Script (U+18B00 – U+18CFF)
    # Tangut Supplement (U+18D00 – U+18D8F)
    set_cjk(0x16FE0, 0x18D8F)

    # Kana Extended-B (U+1AFF0 - U+1AFFF)
    set_cjk(0x1AFF0, 0x1AFFF)

    # Kana Supplement (U+1B000 – U+1B0FF)
    # Kana Extended-A (U+1B100 – U+1B12F)
    # Small Kana Extension (U+1B130 - U+1B16F)
    # Nüshu (U+1B170 – U+1B2FF)
    set_cjk(0x1B000, 0x1B2FF)

    # Enclosed Ideographic Supplement (U+1F200 - U+1F2FF)
    set_cjk(0x1F200, 0x1F2FF)

    # CJK Unified Ideographs Extension B (U+20000 – U+2A6DF)
    set_cjk(0x20000, 0x2A6DF)

    # CJK Unified Ideographs Extension C (U+2A700 – U+2B73F)
    # CJK Unified Ideographs Extension D (U+2B740 – U+2B81F)
    # CJK Unified Ideographs Extension E (U+2B820 – U+2CEAF)
    # CJK Unified Ideographs Extension F (U+2CEB0 – U+2EBEF)
    set_cjk(0x2A700, 0x2EBEF)

    # CJK Compatibility Ideographs Supplement (U+2F800 – U+2FA1F)
    set_cjk(0x2F800, 0x2FA1F)

    # CJK Unified Ideographs Extension G (U+30000 – U+3134F)
    # CJK Unified Ideographs Extension H (U+31350 – U+323AF)
    set_cjk(0x30000, 0x323AF)

def read_icu_data_ucms(icu_data_dir):
    """Set special case (use non-CJK font in non-CJK mode, use CJK font in CJK mode)
    base on the CP932/CP936/CP949/CP950 conversion table.
    """
    global char_font_type
    base = pathlib.Path(icu_data_dir)
    for ucm in [
        'windows-932-2000.ucm',
        'windows-936-2000.ucm',
        'windows-949-2000.ucm',
        'windows-950_hkscs-2001.ucm',
        'windows-950-2000.ucm',
    ]:
        with open(base / 'charset' / 'data' / 'ucm' / ucm, 'r') as fin:
            for line in fin:
                line = line.strip()
                if line == 'CHARMAP':
                    break
            for line in fin:
                line = line.strip()
                if line == 'END CHARMAP':
                    break
                # extract characters that are mapped to one or more bytes in MBCS.
                m = re.match(r'<U([0-9A-F]+)>\s+\\x([0-9A-F]+)', line)
                if m:
                    c = int(m.group(1), 16)
                    #b = int(m.group(2), 16)
                    if c < 0x00A0:
                        continue
                    if c >= 0xE000 and c <= 0xF8FF: # Private Use Area
                        continue
                    if char_font_type[c] == 1:
                        char_font_type[c] = 3

def output_ranges(file):
    """Output character font-type table"""
    global char_font_type
    ranges = []
    cur_range = None
    for c, t in enumerate(char_font_type):
        if cur_range is not None:
            if t == cur_range[2]:
                cur_range[1] = c
                continue
        cur_range = [c, c, t]
        ranges.append(cur_range)

    with open(file, 'w', encoding='ASCII') as fout:
        print('# Generated by Core/Scripts/gen-charfont.py', file=fout)
        print('#', file=fout)
        print('# Unicode character font-type table', file=fout)
        print(f'# Based on Unicode CJK blocks and icu-data', file=fout)
        print('# TYPE can be', file=fout)
        print('#  1 : use non-CJK font (default; omitted in this file)', file=fout)
        print('#  2 : use CJK font', file=fout)
        print('#  3 : use CJK font in CJK mode, otherwise use non-CJK font', file=fout)
        print('#', file=fout)
        print('# CODEPOINT     TYPE', file=fout)
        print('# (SINGLE/RANGE)', file=fout)

        for r in ranges:
            if r[2] == 1:
                continue
            if r[0] == r[1]:
                print(f'U{r[0]:06X}         {r[2]}', file=fout)
            else:
                print(f'U{r[0]:06X} U{r[1]:06X} {r[2]}', file=fout)

if __name__ == '__main__':
    if len(sys.argv) < 2:
        usage()
        sys.exit(1)

    set_base_cjk_chars()
    read_icu_data_ucms(sys.argv[1])

    output_ranges(pathlib.Path(__file__).parent.parent / 'charfont')
