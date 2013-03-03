//
// Connection.ReceiveLine() / ReceiveLine(int)
// Test cases
//

import Poderosa;
import System;

var env = new Poderosa.Macro.Environment();

var conn = env.Connections.ActiveConnection;

env.Debug.ShowTraceWindow();

function RunTest(name, command, proc, timeExpected) {
  conn.TransmitLn("echo 'Clear buffer'");
  System.Threading.Thread.Sleep(1000);
  conn.ReceiveData();

  env.Debug.Trace(name + ' Start');
  conn.TransmitLn(command);
  var time1 = new Date().getTime();
  var result = proc();
  var time2 = new Date().getTime();
  var elapsed = (time2 - time1) / 1000;
  env.Debug.Trace('  elapsed: ' + elapsed);
  if (result && System.Math.Round(double(elapsed)) == timeExpected)
    env.Debug.Trace(name + ' End -- PASS');
  else
    env.Debug.Trace(name + ' End -- FAILED');
}


//--------------------------------------------
// Test 1
//--------------------------------------------
RunTest(
  'Test 1',
  "echo -n 'ABC'; sleep 3; echo 'DEF'",
  function() {
    while(true) {
      var line = conn.ReceiveLine();
      env.Debug.Trace('  line: ' + line);
      if (line.match(/ABCDEF$/))
        return true;
    }
  },
  3
);


//--------------------------------------------
// Test 2
//--------------------------------------------
RunTest(
  'Test 2',
  "echo 'ABC'; echo 'DEF'; echo 'GHI'; echo 'JKL'",
  function() {
    var line = conn.ReceiveLine();  // read echo back line
    env.Debug.Trace('  line: ' + line);

    line = conn.ReceiveLine();
    env.Debug.Trace('  line: ' + line);
    if (line != 'ABC')
      return false;

    line = conn.ReceiveLine();
    env.Debug.Trace('  line: ' + line);
    if (line != 'DEF')
      return false;

    line = conn.ReceiveLine();
    env.Debug.Trace('  line: ' + line);
    if (line != 'GHI')
      return false;

    line = conn.ReceiveLine();
    env.Debug.Trace('  line: ' + line);
    if (line != 'JKL')
      return false;

    return true;
  },
  0
);


//--------------------------------------------
// Test 3
//--------------------------------------------

RunTest(
  'Test 3',
  "echo -n 'ABC'; sleep 4; echo 'DEF'",
  function() {
    var line = conn.ReceiveLine();  // read echo back line
    env.Debug.Trace('  line: ' + line);

    line = conn.ReceiveLine(2000);
    env.Debug.Trace('  line: ' + line);
    if (line == null)
      return true;

    return false;
  },
  2
);
System.Threading.Thread.Sleep(4000);


//--------------------------------------------
// Test 4
//--------------------------------------------

RunTest(
  'Test 4',
  "echo -n 'ABC'; sleep 3; echo 'DEF'",
  function() {
    var line = conn.ReceiveLine();  // read echo back line
    env.Debug.Trace('  line: ' + line);

    line = conn.ReceiveLine(0);
    env.Debug.Trace('  line: ' + line);
    if (line == null)
      return true;

    return false;
  },
  0
);
System.Threading.Thread.Sleep(4000);


//--------------------------------------------
// Test 5
//--------------------------------------------

RunTest(
  'Test 5',
  "echo -n 'ABC'; sleep 1; logout",
  function() {
    var line = conn.ReceiveLine();  // read echo back line
    env.Debug.Trace('  line: ' + line);

    line = conn.ReceiveLine(4000);
    env.Debug.Trace('  line: ' + line);
    if (line == null)
      return true;

    return false;
  },
  4
);


