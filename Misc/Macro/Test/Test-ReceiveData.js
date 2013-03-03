//
// Connection.ReceiveData() / ReceiveData(int)
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
  proc();
  var time2 = new Date().getTime();
  var elapsed = (time2 - time1) / 1000;
  env.Debug.Trace('  elapsed: ' + elapsed);
  if (System.Math.Round(double(elapsed)) == timeExpected)
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
    var data = '';
    while(true) {
      data += conn.ReceiveData();
      env.Debug.Trace('  data: ' + data);
      if (data.match(/ABCDEF/))
        break;
    }
  },
  3
);

//--------------------------------------------
// Test 2
//--------------------------------------------

RunTest(
  'Test 2',
  "echo -n 'ABC'; sleep 4; echo 'DEF'",
  function() {
    var data = '';
    while(true) {
      var rcv = conn.ReceiveData(2000);
      if (rcv == null)
        break;
      data += rcv;
      env.Debug.Trace('  data: ' + data);
      if (data.match(/ABCDEF/))
        break;
    }
  },
  2
);
System.Threading.Thread.Sleep(4000);


//--------------------------------------------
// Test 3
//--------------------------------------------

RunTest(
  'Test 3',
  "echo -n 'ABC'; sleep 3; echo 'DEF'",
  function() {
    var data = '';
    while(true) {
      var rcv = conn.ReceiveData(0);
      if (rcv == null)
        break;
      data += rcv;
      env.Debug.Trace('  data: ' + data);
      if (data.match(/ABCDEF/))
        break;
    }
  },
  0
);
System.Threading.Thread.Sleep(4000);


//--------------------------------------------
// Test 4
//--------------------------------------------

RunTest(
  'Test 4',
  "echo -n 'ABC'; sleep 1; logout",
  function() {
    var data = '';
    while(true) {
      var rcv = conn.ReceiveData(4000);
      if (rcv == null)
        break;
      data += rcv;
      env.Debug.Trace('  data: ' + data);
      if (data.match(/ABCDEF/))
        break;
    }
  },
  4
);


