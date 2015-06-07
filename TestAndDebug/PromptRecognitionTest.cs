#if TESTSESSION && MONOLITHIC
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Document;
using Poderosa.View;
using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Commands;
using Poderosa.Protocols;
using Poderosa.Terminal;

namespace Poderosa.Sessions {
    internal class PromptRecognitionTest : IPromptProcessor {
        private ITerminalSession _session;
        private TerminalTransmission _output;
        private bool _consoleFlag;

        private GLine _lastLine;
        private bool _isPrompt;
        private string _promptText;
        private string _commandText;

        public void Start(ITerminalSession session) {
            _session = session;
            _output = session.TerminalTransmission;
            _consoleFlag = true;

            _session.Terminal.PromptRecognizer.AddListener(this);
            //Test Body
            Test1();
            Test2();
            Test3();

            _session.Terminal.PromptRecognizer.RemoveListener(this);
        }

        private void Test1() {
            Send(">ls");
            Debug.Assert(_isPrompt);
            Debug.Assert(_commandText=="ls");

            Debug.WriteLineIf(_consoleFlag, "Test1 OK");
            Ln();
        }
        private void Test2() {
            Send(">l");
            Debug.Assert(_isPrompt);
            Debug.Assert(_commandText=="l");
            Send("s");
            Debug.Assert(_isPrompt);
            Debug.Assert(_commandText=="ls");

            Debug.WriteLineIf(_consoleFlag, "Test2 OK");
            Ln();
        }
        private void Test3() {
            Send(">ls");
            Ln();
            Debug.Assert(!_isPrompt);
            Send(">top");
            Debug.Assert(_isPrompt);
            Debug.Assert(_commandText=="top");

            Debug.WriteLineIf(_consoleFlag, "Test3 OK");
            Ln();
        }


        private void Send(string text) {
            _output.SendString(text.ToCharArray());
        }
        private void Ln() {
            _output.SendString(new char[] { '\r', '\n' });
        }

        public void OnPromptLine(GLine line, string prompt, string command) {
            _isPrompt = true;
            _lastLine = line;
            _promptText = prompt;
            _commandText = command;
        }

        public void OnNotPromptLine() {
            _isPrompt = false;
        }
    }
}
#endif
