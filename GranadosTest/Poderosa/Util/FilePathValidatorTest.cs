// Copyright 2011-2019 The Poderosa Project.
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
#if UNITTEST
using NUnit.Framework;
using System;

namespace Granados.Poderosa.Util {

    [TestFixture]
    class FilePathValidatorExceptionTest {

        [Test]
        public void ValidatePathException_Null() {
            var exception = new FilePathValidatorException(null, false);
            Console.Out.WriteLine("Message : " + exception.Message);
            Assert.IsTrue(exception.Message.Contains("null"));
        }

        [Test]
        public void ValidatePathException_InhibitedChar() {
            var exception = new FilePathValidatorException("<>:\"|?*\u0000\u001f", false);
            Console.Out.WriteLine("Message : " + exception.Message);
            Assert.IsTrue(exception.Message.Contains("<>:\"|?*<0000><001F>"));
        }

        [Test]
        public void ValidatePathException_File() {
            var exception = new FilePathValidatorException("abc", false);
            Console.Out.WriteLine("Message : " + exception.Message);
            Assert.IsTrue(exception.Message.Contains("file"));
        }

        [Test]
        public void ValidatePathException_Directory() {
            var exception = new FilePathValidatorException("abc", true);
            Console.Out.WriteLine("Message : " + exception.Message);
            Assert.IsTrue(exception.Message.Contains("directory"));
        }
    }

    [TestFixture]
    class FilePathValidatorTest {

        private readonly FilePathValidator validator = new FilePathValidator();

        //------------------------------------------------------
        // ValidateRelativeUnixPath
        //------------------------------------------------------

        [Test]
        public void ValidateRelativeUnixPath_Null() {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(null, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(null, true)
            );
        }

        [TestCase("")]
        [TestCase("..")]
        [TestCase("...")]
        [TestCase("a.")]
        [TestCase("a ")]
        [TestCase(" ")]
        [TestCase("CoN")]
        [TestCase("PrN")]
        [TestCase("aUX")]
        [TestCase("nUL")]
        [TestCase("COm1")]
        [TestCase("CoM2")]
        [TestCase("cOM3")]
        [TestCase("COm4")]
        [TestCase("CoM5")]
        [TestCase("cOM6")]
        [TestCase("COm7")]
        [TestCase("CoM8")]
        [TestCase("cOM9")]
        [TestCase("lPT1")]
        [TestCase("LpT2")]
        [TestCase("LPt3")]
        [TestCase("lPT4")]
        [TestCase("LpT5")]
        [TestCase("LPt6")]
        [TestCase("lPT7")]
        [TestCase("LpT8")]
        [TestCase("LPt9")]
        [TestCase("a<b")]
        [TestCase("a>b")]
        [TestCase("a:b")]
        [TestCase("a\"b")]
        [TestCase("a|b")]
        [TestCase("a?b")]
        [TestCase("a*b")]
        [TestCase("a\\b")]
        public void ValidateRelativeUnixPath_InhibitedFileNames(string name) {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(name, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(name, true)
            );
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(0x1f)]
        public void ValidateRelativeUnixPath_InhibitedFileNames_Controls(int invalidCharCode) {
            string name = "a" + Char.ToString((char)invalidCharCode) + "b";
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(name, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(name, true)
            );
        }

        [Test]
        public void ValidateRelativeUnixPath_SingleDotAsFileName() {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(".", false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath("aaa/.", false)
            );
        }

        [Test]
        public void ValidateRelativeUnixPath_SingleDotAsDirectoryName() {
            validator.ValidateRelativeUnixPath(".", true);
            validator.ValidateRelativeUnixPath("aaa/.", true);
            validator.ValidateRelativeUnixPath("./aaa", false);
            validator.ValidateRelativeUnixPath("./aaa", true);
            // no exception
        }

        [TestCase("/abc")]
        [TestCase("abc/")]
        [TestCase("abc//abc")]
        [TestCase("../abc")]
        [TestCase("abc/..")]
        [TestCase("a./abc")]
        [TestCase("abc/a.")]
        [TestCase("abc /abc")]
        [TestCase("abc/abc ")]
        [TestCase("abc/a\\b")]
        [TestCase("abc/a\\/b")]
        [TestCase("abc/a\\..\\b")]
        [TestCase("NUL/abc")]
        [TestCase("abc/NUL")]
        [TestCase("a<b/abc")]
        [TestCase("abc/a<b")]
        public void ValidateRelativeUnixPath_PathContainsInhibitedFileName(string name) {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(name, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(name, true)
            );
        }

        [TestCase("abc/a", 0, "b")]
        [TestCase("abc/a", 1, "b")]
        [TestCase("abc/a", 0x1f, "b")]
        [TestCase("a", 0, "b/abc")]
        [TestCase("a", 1, "b/abc")]
        [TestCase("a", 0x1f, "b/abc")]
        public void ValidateRelativeUnixPath_PathContainsInhibitedFileName_Controls(string leading, int invalidCharCode, string trailing) {
            string s = leading + Char.ToString((char)invalidCharCode) + trailing;
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(s, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateRelativeUnixPath(s, true)
            );
        }

        [TestCase(".a")]
        [TestCase("a.b")]
        [TestCase(" a")]
        [TestCase("CONX")]
        [TestCase("XPRN")]
        [TestCase("COM")]
        [TestCase("COM0")]
        [TestCase("COM10")]
        [TestCase("LPT")]
        [TestCase("LPT0")]
        [TestCase("LPT10")]
        public void ValidateRelativeUnixPath_AllowedFileName(string name) {
            validator.ValidateRelativeUnixPath(name, false);
            validator.ValidateRelativeUnixPath(name, true);
            // no exception
        }

        [TestCase("a/b/c")]
        [TestCase("a.b.c/a.b.c/a.b.c")]
        public void ValidateRelativeUnixPath_AllowedPath(string path) {
            validator.ValidateRelativeUnixPath(path, false);
            validator.ValidateRelativeUnixPath(path, true);
            // no exception
        }

        //------------------------------------------------------
        // ValidateFileName
        //------------------------------------------------------

        [Test]
        public void ValidateFileName_Null() {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateFileName(null, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateFileName(null, true)
            );
        }

        [TestCase("")]
        [TestCase(".")]
        [TestCase("..")]
        [TestCase("...")]
        [TestCase("a.")]
        [TestCase("a ")]
        [TestCase(" ")]
        [TestCase("CoN")]
        [TestCase("PrN")]
        [TestCase("aUX")]
        [TestCase("nUL")]
        [TestCase("COm1")]
        [TestCase("CoM2")]
        [TestCase("cOM3")]
        [TestCase("COm4")]
        [TestCase("CoM5")]
        [TestCase("cOM6")]
        [TestCase("COm7")]
        [TestCase("CoM8")]
        [TestCase("cOM9")]
        [TestCase("lPT1")]
        [TestCase("LpT2")]
        [TestCase("LPt3")]
        [TestCase("lPT4")]
        [TestCase("LpT5")]
        [TestCase("LPt6")]
        [TestCase("lPT7")]
        [TestCase("LpT8")]
        [TestCase("LPt9")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        [TestCase("a<b")]
        [TestCase("a>b")]
        [TestCase("a:b")]
        [TestCase("a\"b")]
        [TestCase("a|b")]
        [TestCase("a?b")]
        [TestCase("a*b")]
        public void ValidateFileName_InhibitedFileNames(string name) {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateFileName(name, false)
            );
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateFileName(name, true)
            );
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(0x1f)]
        public void ValidateFileName_InhibitedFileNames_Controls(int invalidCharCode) {
            Assert.Catch<FilePathValidatorException>(
                () => validator.ValidateFileName("a" + Char.ToString((char)invalidCharCode) + "b", false)
            );
        }

        [TestCase(".a")]
        [TestCase("a.b")]
        [TestCase(" a")]
        [TestCase("CONX")]
        [TestCase("XPRN")]
        [TestCase("COM")]
        [TestCase("COM0")]
        [TestCase("COM10")]
        [TestCase("LPT")]
        [TestCase("LPT0")]
        [TestCase("LPT10")]
        public void ValidateFileName_AllowedFileName(string name) {
            validator.ValidateFileName(name, false);
            // no exception
        }
    }
}
#endif
