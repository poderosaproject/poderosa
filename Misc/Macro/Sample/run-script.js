//======================================================
// Auto-exec macro sample
//
// This macro reads a shell script file line by line
// and send them to the remote.
//======================================================

import Poderosa;
import System;

var env = new Poderosa.Macro.Environment();

var conn = env.Connections.ActiveConnection;

// Path to your shell script file
var scriptFile = 'C:\\Documents and Settings\\User\\My Documents\\auto-exec-script.sh';

// You can open a trace window in macro
env.Debug.ShowTraceWindow();

// Show message in the trace window
env.Debug.Trace('Wait 5 seconds ...');

// Wait 5000 milliseconds = 5 seconds
System.Threading.Thread.Sleep(5000);

try {
  env.Debug.Trace('Open a script file');

  // Open a script file.
  // The texts are read in the default encoding of the system.
  var reader = new System.IO.StreamReader(scriptFile, System.Text.Encoding.Default);

  while(true) {
    // Read a line.
    // Returned text doesn't contain new-line characters.
    var line = reader.ReadLine();

    if (line == null)
      break; // Reached the end of the file

    if (line.match(/^\s*#/) || line.match(/^\s*$/))
      continue; // Skip comment line or empty line

    System.Threading.Thread.Sleep(300); // Wait a bit

    // Send a line
    conn.TransmitLn(line);
  }

  env.Debug.Trace('Close a script file');
  reader.Close();

  env.Debug.HideTraceWindow();
}
catch(ex) {
  env.Debug.Trace('Exception: ' + ex);
  env.Util.MessageBox(ex);
}
