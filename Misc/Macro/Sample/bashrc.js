import Poderosa.Macro;
import System.IO;
var env = new Environment();
bashrc();

function bashrc() {
	var filename = Path.GetTempFileName()+".txt";
	var con = env.Connections.ActiveConnection;
	if(con==null) {
		env.Util.MessageBox("This macro requires an established connection to shell.");
		return;
	}

	var output = new StreamWriter(filename);
	//'start_mark' is for distinguishing the input to the shell and the output of the 'cat' command
	var start_mark = "start";
	var end_mark = "a long line to distinguish the end of the command";
	con.TransmitLn("echo "+start_mark+";cat ~/.bashrc;echo "+end_mark);

	var l = con.ReceiveLine();
	var started = false;
	while(l!=end_mark) {
		if(started) output.WriteLine(l);
		else if(l==start_mark) started = true;
		l = con.ReceiveLine();
	}
	output.Close();

	env.Util.ShellExecute("open", filename);
}