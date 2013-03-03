//======================================================
// Macro sample
//
// This macro launches WinSCP with settings of the current terminal.
//======================================================
import System;
import Poderosa.Macro;

var winscp = 'C:\\ CHANGE THIS \\WinSCP.exe';

var env = new Poderosa.Macro.Environment();
var conn = env.Connections.ActiveConnection;

var method = conn.ConnectionParameters['Method'];

if (method != 'SSH2') {
  env.Util.MessageBox('Not a SSH2 connection');
}
else {
  var account = conn.ConnectionParameters['Account'];
  var host = conn.ConnectionParameters['Destination'];
  var port = conn.ConnectionParameters['Port'];
  var authtype = conn.ConnectionParameters['AuthenticationType'];
  var keyfile = conn.ConnectionParameters['IdentityFileName'];

  if (!(account && host && port)) {
    env.Util.MessageBox('Missing account, host or port');
  }
  else {
    var args = '"sftp://' + account + '@' + host + ':' + port + '"';
    if (authtype == 'PublicKey' && keyfile)
      args += ' "/privatekey=' + keyfile + '"';

    env.Util.Exec('"' + winscp + '" ' + args);
  }

}

