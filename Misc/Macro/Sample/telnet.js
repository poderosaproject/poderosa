import Poderosa;
import Poderosa.ConnectionParam;
import Poderosa.Terminal;
import Poderosa.Macro;
import Poderosa.View;
import System.Drawing;

var env = new Environment();

/*
	Please modify the following values before you run this macro!
*/
var host = "myhost.mydomain";
var account = "hidetoshi";
var password = "nakata";

telnettest();

function telnettest() {
	if(host=="myhost.mydomain") {
		env.Util.MessageBox(String.Format("This telnet sample requires to set the target host.\nPlease modify the 'host','account',and 'password' variables in {0} and try again.", env.MacroFileName));
		return;
	}

	/*
	//if you want to connect using SSH, create SSHTerminalParam instead of TelnetTerminalParam
	var param = new SSHTerminalParam(ConnectionMethod.SSH2, host, account, password);
	*/

	var param = new TelnetTerminalParam(host);
	
	var prof = new RenderProfile();
	prof.FontSize = 10;
	prof.FontName = "Courier New";
	prof.SetBackColor(Color.Black);
	prof.SetForeColor(Color.White);
	param.RenderProfile = prof;

	//Telnet negotiation
	var c = env.Connections.Open(param);
	var r = c.ReceiveData();
	while(r.indexOf("login:")==-1) r = c.ReceiveData(); //waiting prompt for account
	c.TransmitLn(account);
	r = c.ReceiveData();
	while(r.indexOf("Password:")==-1) r = c.ReceiveData(); //waiting prompt for password
	c.TransmitLn(password);

}
