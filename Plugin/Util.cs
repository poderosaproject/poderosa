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
using System.Diagnostics;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

//using Microsoft.JScript;
using System.CodeDom.Compiler;

using Poderosa.Boot;

namespace Poderosa {
    /// <summary>
    /// <ja>
    /// 標準的な成功／失敗を示します。
    /// </ja>
    /// <en>
    /// A standard success/failure is shown. 
    /// </en>
    /// </summary>
    public enum GenericResult {
        /// <summary>
        /// <ja>成功しました</ja>
        /// <en>Succeeded</en>
        /// </summary>
        Succeeded,
        /// <summary>
        /// <ja>失敗しました</ja>
        /// <en>Failed</en>
        /// </summary>
        Failed
    }

    //Debug.WriteLineIfあたりで使用
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class DebugOpt {
#if DEBUG
        public static bool BuildToolBar = false;
        public static bool CommandPopup = false;
        public static bool DrawingPerformance = false;
        public static bool DumpDocumentRelation = false;
        public static bool IntelliSense = false;
        public static bool IntelliSenseMenu = false;
        public static bool LogViewer = false;
        public static bool Macro = false;
        public static bool MRU = false;
        public static bool PromptRecog = false;
        public static bool Socket = false;
        public static bool SSH = false;
        public static bool ViewManagement = false;
        public static bool WebBrowser = false;
#else //RELEASE
        public static bool BuildToolBar = false;
        public static bool CommandPopup = false;
        public static bool DrawingPerformance = false;
        public static bool DumpDocumentRelation = false;
        public static bool IntelliSense = false;
        public static bool IntelliSenseMenu = false;
        public static bool LogViewer = false;
        public static bool Macro = false;
        public static bool MRU = false;
        public static bool PromptRecog = false;
        public static bool Socket = false;
        public static bool SSH = false;
        public static bool ViewManagement = false;
        public static bool WebBrowser = false;
#endif
    }


    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class RuntimeUtil {

        private static readonly object _errorFileSync = new object();
        private static string _errorLogFilePath = null;

        public static void ReportException(Exception ex) {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.StackTrace);

            string errorfile = ReportExceptionToFile(ex);

            //メッセージボックスで通知。
            //だがこの中で例外が発生することがSP1ではあるらしい。しかもそうなるとアプリが強制終了だ。
            //Win32のメッセージボックスを出しても同じ。ステータスバーなら大丈夫のようだ
            try {
                string msg = String.Format(InternalPoderosaWorld.Strings.GetString("Message.Util.InternalError"), errorfile, ex.Message);
                MessageBox.Show(msg, "Poderosa", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception ex2) {
                Debug.WriteLine("(MessageBox.Show() failed) " + ex2.Message);
                Debug.WriteLine(ex2.StackTrace);
            }
        }

        public static void SilentReportException(Exception ex) {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.StackTrace);
            ReportExceptionToFile(ex);
        }

        public static void DebuggerReportException(Exception ex) {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.StackTrace);
        }

        private static string ReportExceptionToFile(Exception ex) {
            string errorfile;
            lock (_errorFileSync) {
                using (var sw = GetErrorLog(out errorfile)) {
                    sw.WriteLine(DateTime.Now.ToString());
                    sw.WriteLine(ex.Message);
                    sw.WriteLine(ex.StackTrace);
                    //inner exceptionを順次
                    Exception i = ex.InnerException;
                    while (i != null) {
                        sw.WriteLine("[inner] " + i.Message);
                        sw.WriteLine(i.StackTrace);
                        i = i.InnerException;
                    }
                }
            }
            return errorfile;
        }

        public static void SilentReportError(string message) {
            Debug.WriteLine(message);
            ReportErrorToFile(message);
        }

        private static string ReportErrorToFile(string message) {
            string errorfile;
            lock (_errorFileSync) {
                using (var sw = GetErrorLog(out errorfile)) {
                    sw.WriteLine(DateTime.Now.ToString());
                    sw.WriteLine(message);
                }
            }
            return errorfile;
        }

        private static StreamWriter GetErrorLog(out string errorfile) {
            if (_errorLogFilePath == null) {
                lock (_errorFileSync) {
                    if (_errorLogFilePath == null) {
                        string errorLogDir = PoderosaStartupContext.Instance.ProfileHomeDirectory;
                        string errorFilePath = Path.Combine(errorLogDir, "error.log");
                        if (File.Exists(errorFilePath)) {
                            // check the current encoding, and rename the log file if it is not UTF-8 with BOM
                            Encoding encoding;
                            using (FileStream fs = File.OpenRead(errorFilePath))
                            using (StreamReader reader = new StreamReader(fs, Encoding.Default, true)) {
                                reader.Read();
                                encoding = reader.CurrentEncoding;
                            }
                            if (encoding.CodePage != Encoding.UTF8.CodePage) {
                                string backupFileName = "error (backup " + DateTime.Now.ToString("yyyy-MM-dd HH_mm_ss", DateTimeFormatInfo.InvariantInfo) + ").log";
                                string backupFilePath = Path.Combine(errorLogDir, backupFileName);
                                File.Move(errorFilePath, backupFilePath);
                            }
                        }
                        _errorLogFilePath = errorFilePath;
                    }
                }
            }

            errorfile = _errorLogFilePath;
            return new StreamWriter(errorfile, true /* append */, Encoding.UTF8 /* with BOM */);
        }

        public static Font CreateFont(string name, float size) {
            try {
                return new Font(name, size);
            }
            catch (ArithmeticException) {
                //JSPagerの件で対応。msvcr71がロードできない環境もあるかもしれないので例外をもらってはじめて呼ぶようにする
                Win32.ClearFPUOverflowFlag();
                return new Font(name, size);
            }
        }

        public static string ConcatStrArray(string[] values, char delimiter) {
            StringBuilder bld = new StringBuilder();
            for (int i = 0; i < values.Length; i++) {
                if (i > 0)
                    bld.Append(delimiter);
                bld.Append(values[i]);
            }
            return bld.ToString();
        }

        //min未満はmin, max以上はmax、それ以外はvalueを返す
        public static int AdjustIntRange(int value, int min, int max) {
            Debug.Assert(min <= max);
            if (value < min)
                return min;
            else if (value > max)
                return max;
            else
                return value;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class ParseUtil {
        public static bool ParseBool(string value, bool defaultvalue) {
            try {
                if (value == null || value.Length == 0)
                    return defaultvalue;
                return Boolean.Parse(value);
            }
            catch (Exception) {
                return defaultvalue;
            }
        }
        public static byte ParseByte(string value, byte defaultvalue) {
            try {
                if (value == null || value.Length == 0)
                    return defaultvalue;
                return Byte.Parse(value);
            }
            catch (Exception) {
                return defaultvalue;
            }
        }
        public static int ParseInt(string value, int defaultvalue) {
            try {
                if (value == null || value.Length == 0)
                    return defaultvalue;
                return Int32.Parse(value);
            }
            catch (Exception) {
                return defaultvalue;
            }
        }
        public static float ParseFloat(string value, float defaultvalue) {
            try {
                if (value == null || value.Length == 0)
                    return defaultvalue;
                return Single.Parse(value);
            }
            catch (Exception) {
                return defaultvalue;
            }
        }
        public static int ParseHexInt(string value, int defaultvalue) {
            try {
                if (value == null || value.Length == 0)
                    return defaultvalue;
                return Int32.Parse(value, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception) {
                return defaultvalue;
            }
        }
        public static Color ParseColor(string t, Color defaultvalue) {
            if (t == null || t.Length == 0)
                return defaultvalue;
            else {
                if (t.Length == 8) { //16進で保存されていることもある。窮余の策でこのように
                    int v;
                    if (Int32.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out v))
                        return Color.FromArgb(v);
                }
                else if (t.Length == 6) {
                    int v;
                    if (Int32.TryParse(t, System.Globalization.NumberStyles.HexNumber, null, out v))
                        return Color.FromArgb((int)((uint)v | 0xFF000000)); //'A'要素は0xFFに
                }
                Color c = Color.FromName(t);
                return c.ToArgb() == 0 ? defaultvalue : c; //へんな名前だったとき、ARGBは全部0になるが、IsEmptyはfalse。なのでこれで判定するしかない
            }
        }

        public static T ParseEnum<T>(string value, T defaultvalue) {
            try {
                if (value == null || value.Length == 0)
                    return defaultvalue;
                else
                    return (T)Enum.Parse(typeof(T), value, false);
            }
            catch (Exception) {
                return defaultvalue;
            }
        }

        public static bool TryParseEnum<T>(string value, ref T parsed) {
            if (value == null || value.Length == 0) {
                return false;
            }

            try {
                parsed = (T)Enum.Parse(typeof(T), value, false);
                return true;
            }
            catch (Exception) {
                return false;
            }
        }

        //TODO Generics化
        public static ValueType ParseMultipleEnum(Type enumtype, string t, ValueType defaultvalue) {
            try {
                int r = 0;
                foreach (string a in t.Split(','))
                    r |= (int)Enum.Parse(enumtype, a, false);
                return r;
            }
            catch (FormatException) {
                return defaultvalue;
            }
        }
    }

    public static class BlockedFileUtil {

        /// <summary>
        /// Check if the file is blocked
        /// </summary>
        /// <param name="path">file path</param>
        /// <returns>treu if the file is blocked</returns>
        public static bool IsFileBlocked(string path) {
            return Win32.GetFileAttributes("\\\\?\\" + path + ":Zone.Identifier") != Win32.INVALID_FILE_ATTRIBUTES;
        }

    }
}
