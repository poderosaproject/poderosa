// Copyright 2025 The Poderosa Project.
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
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Poderosa.Boot {

    /// <summary>
    /// Read/Write preference file (options.conf)
    /// </summary>
    public static class PreferencesIO {

        /// <summary>
        /// Read preference file.
        /// </summary>
        /// <remarks>
        /// Caller should handle I/O exception.
        /// </remarks>
        /// <param name="filePath">file path</param>
        /// <returns>preferences</returns>
        public static StructuredText ReadPreferences(string filePath) {
            StructuredText pref = null;

            if (File.Exists(filePath)) {
                pref = ReadPreferencesInternal(filePath);
                // Note:
                //   if the file is empty or consists of empty lines,
                //   pref will be null.
            }

            if (pref == null) {
                pref = new StructuredText("Poderosa");
            }

            return pref;
        }

        private static StructuredText ReadPreferencesInternal(string filePath) {
            // Preferences are read in UTF-8/16/32 if any of the following conditions are met.
            // - BOM exists at the beginning of the file
            // - The first line is "# encoding: UTF-8"
            Encoding encoding = GetEncodingFromComment(filePath);
            bool detectEncodingFromByteOrderMarks = true;
            using (StreamReader r = new StreamReader(filePath, encoding, detectEncodingFromByteOrderMarks)) {
                return new TextStructuredTextReader(r).Read();
            }
        }

        private static Encoding GetEncodingFromComment(string filePath) {
            string encodingName = GetEncodingNameFromComment(filePath);
            if (encodingName != null && encodingName.ToLower() == "utf-8") {
                return Encoding.UTF8;
            }
            return Encoding.Default;
        }

        private static string GetEncodingNameFromComment(string filePath) {
            byte[] data = new byte[30];
            int dataLength;
            using (FileStream fs = File.OpenRead(filePath)) {
                dataLength = fs.Read(data, 0, data.Length);
            }

            Encoding ascii = (Encoding)Encoding.ASCII.Clone();
            ascii.DecoderFallback = DecoderFallback.ExceptionFallback;

            string s;
            try {
                s = ascii.GetString(data, 0, dataLength);
            }
            catch (DecoderFallbackException) {
                return null;
            }

            Match m = Regex.Match(s, @"\A#[ \t]*encoding[ \t]*:[ \t]*([-_a-zA-Z0-9]+)[ \t]*[\r\n]");
            if (m.Success) {
                return m.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// Write preference file.
        /// </summary>
        /// <remarks>
        /// Caller should handle I/O exception.
        /// </remarks>
        /// <param name="filePath">file path</param>
        /// <param name="prefs">preferences</param>
        public static void WritePreferences(string filePath, StructuredText prefs) {
            string tempPreferenceFilePath = filePath + ".tmp";
            string prevPreferenceFilePath = filePath + ".prev";

            WritePreferencesInternal(tempPreferenceFilePath, prefs);

            if (File.Exists(filePath)) {
                File.Delete(prevPreferenceFilePath);
                File.Move(filePath, prevPreferenceFilePath);
            }

            File.Move(tempPreferenceFilePath, filePath);
        }

        private static void WritePreferencesInternal(string filePath, StructuredText prefs) {
            using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8)) { // BOM is prepended automatically
                writer.WriteLine("# encoding: UTF-8");
                new TextStructuredTextWriter(writer).Write(prefs);
            }
        }
    }

}
