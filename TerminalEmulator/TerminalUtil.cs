// Copyright 2004-2025 The Poderosa Project.
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

using Poderosa.ConnectionParam;
using Poderosa.Util;

namespace Poderosa.Terminal {

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum TerminalMode {
        Normal,
        Application
    }


    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public class TerminalUtil {
        public static char[] NewLineChars(NewLine nl) {
            switch (nl) {
                case NewLine.CR:
                    return new char[1] { '\r' };
                case NewLine.LF:
                    return new char[1] { '\n' };
                case NewLine.CRLF:
                    return new char[2] { '\r', '\n' };
                default:
                    throw new ArgumentException("Unknown NewLine " + nl);
            }
        }
        //TODO staticにしたほうがいい？ うっかり破壊が怖いが
        public static byte[] NewLineBytes(NewLine nl) {
            switch (nl) {
                case NewLine.CR:
                    return new byte[1] { (byte)'\r' };
                case NewLine.LF:
                    return new byte[1] { (byte)'\n' };
                case NewLine.CRLF:
                    return new byte[2] { (byte)'\r', (byte)'\n' };
                default:
                    throw new ArgumentException("Unknown NewLine " + nl);
            }
        }
        public static NewLine NextNewLineOption(NewLine nl) {
            switch (nl) {
                case NewLine.CR:
                    return NewLine.LF;
                case NewLine.LF:
                    return NewLine.CRLF;
                case NewLine.CRLF:
                    return NewLine.CR;
                default:
                    throw new ArgumentException("Unknown NewLine " + nl);
            }
        }


        //有効なボーレートのリスト
        public static string[] BaudRates {
            get {
                return new string[] {
                    "110", "300", "600", "1200", "2400", "4800",
                    "9600", "14400", "19200", "38400", "57600", "115200"
                };
            }
        }

        //秘密鍵ファイル選択
        public static string SelectPrivateKeyFileByDialog(Form parent) {
            using (OpenFileDialog dlg = new OpenFileDialog()) {
                dlg.CheckFileExists = true;
                dlg.Multiselect = false;
                dlg.Title = "Select Private Key File";
                dlg.Filter = "Key Files(*.bin;*)|*.bin;*";
                if (dlg.ShowDialog(parent) == DialogResult.OK) {
                    return dlg.FileName;
                }
                return null;
            }
        }

    }


    //これと同等の処理はToAscii APIを使ってもできるが、ちょっとやりづらいので逆引きマップをstaticに持っておく
    internal class KeyboardInfo {
        public static char[] _defaultGroup;
        public static char[] _shiftGroup;

        public static void Init() {
            _defaultGroup = new char[256];
            _shiftGroup = new char[256];
            for (int i = 32; i < 128; i++) {
                short v = Win32.VkKeyScan((char)i);
                bool shift = (v & 0x0100) != 0;
                short body = (short)(v & 0x00FF);
                if (shift)
                    _shiftGroup[body] = (char)i;
                else
                    _defaultGroup[body] = (char)i;
            }
        }

        public static char Scan(Keys body, bool shift) {
            if (_defaultGroup == null)
                Init();

            //制御文字のうち単品のキーで送信できるもの
            if (body == Keys.Escape)
                return (char)0x1B;
            else if (body == Keys.Tab)
                return (char)0x09;
            else if (body == Keys.Back)
                return (char)0x08;
            else if (body == Keys.Delete)
                return (char)0x7F;

            if (shift)
                return _shiftGroup[(int)body];
            else
                return _defaultGroup[(int)body];
        }
    }

    /// <summary>
    /// Mixin to add locale-independent stringification methods
    /// </summary>
    internal static class InvariantFormatMixin {

        public static string ToInvariantString(this int s) {
            return s.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static string ToInvariantString(this uint s) {
            return s.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static string ToInvariantString(this long s) {
            return s.ToString(NumberFormatInfo.InvariantInfo);
        }

        public static string ToInvariantString(this ulong s) {
            return s.ToString(NumberFormatInfo.InvariantInfo);
        }
    }
}
