//======================================================
// Macro sample
//
// This macro reads connection parameters from
// the current terminal, and display them.
//======================================================
import System;
import Poderosa.Macro;

var env = new Poderosa.Macro.Environment();
var conn = env.Connections.ActiveConnection;

var names = conn.ConnectionParameters.Names;
var msg = '';
for(var i = 0; i < names.Count; i++) {
    var param = conn.ConnectionParameters[names[i]];
    if (param instanceof System.Array) {
        msg += names[i] + ' = Array\r\n';
        for(var k = 0; k < param.Length; k++) {
            msg += '    [' + k + '] ' + param[k] + '\r\n';
        }
    } else {
        msg += names[i] + ' = ' + conn.ConnectionParameters[names[i]] + '\r\n';
    }
}

env.Util.MessageBox(msg);

