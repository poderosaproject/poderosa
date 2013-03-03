//======================================================
// Auto-exec macro sample
//
// This macro sends some commands to the remote.
// Each command will be sent after the next command-line
// prompt was detected.
//======================================================

import Poderosa;
import System;

var env = new Poderosa.Macro.Environment();

var conn = env.Connections.ActiveConnection;

// Regular expression for detecting a command-line prompt.
// Note that '>' may match with a redirect symbol in the echo-back.
var prompt = /\n[%$>] $/;

// These are sample commands
var command1 = 'uname -a';
var command2 = 'pwd';
var command3 = 'whoami';

// You can open a trace window in macro
env.Debug.ShowTraceWindow();

// This function checks incoming data until a command line prompt has been displayed.
// If no data were received for 5 seconds, this function returns false.
function waitForPrompt() {
  env.Debug.Trace('Wait for prompt'); // Show message in the trace window
  var line = '';
  while(!line.match(prompt)) {
    var data = conn.ReceiveData(5000); // wait new data for 5 seconds
    if (data == null) {
      env.Debug.Trace('Timeout');
      return false; // timout
    }
    line += data;
  }
  return true; // command-line prompt detected
}

(function() {
  if (!waitForPrompt())
    return;
  conn.TransmitLn(command1);

  if (!waitForPrompt())
    return;
  conn.TransmitLn(command2);

  if (!waitForPrompt())
    return;
  conn.TransmitLn(command3);

  // Wait 3000 milliseconds = 3 seconds
  System.Threading.Thread.Sleep(3000);

  // Hide a trace window
  env.Debug.HideTraceWindow();
})();

