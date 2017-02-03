using System.Collections;
using System.Drawing;

using Poderosa.ConnectionParam;


namespace Poderosa.ConnectProfile {
    /// <summary>
    /// プロファイルリスト管理クラス
    /// </summary>
    internal class ConnectProfileList : IEnumerable {
        // メンバー変数
        private ArrayList _data;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConnectProfileList() {
            _data = new ArrayList();
        }

        /// <summary>
        /// GetEnumerator
        /// </summary>
        public IEnumerator GetEnumerator() {
            return _data.GetEnumerator();
        }

        /// <summary>
        /// プロファイルを追加
        /// </summary>
        /// <param name="p">プロファイル</param>
        public void AddProfile(ConnectProfileStruct p) {
            _data.Add(p);
        }

        /// <summary>
        /// プロファイルを削除
        /// </summary>
        /// <param name="p">プロファイル</param>
        public void DeleteProfile(ConnectProfileStruct p) {
            _data.Remove(p);
        }

        /// <summary>
        /// プロファイルを置換
        /// </summary>
        /// <param name="p1">置換前プロファイル</param>
        /// <param name="p2">置換後プロファイル</param>
        public void ReplaceProfile(ConnectProfileStruct p1, ConnectProfileStruct p2) {
            _data[_data.IndexOf(p1)] = p2;
        }

        /// <summary>
        /// 全プロファイルリストを削除
        /// </summary>
        public void DeleteAllProfile() {
            _data.Clear();
        }

        /// <summary>
        /// 全プロファイルリストを置換
        /// </summary>
        /// <param name="pl">置換後プロファイルリスト</param>
        public void ReplaceAllProfile(ConnectProfileList pl) {
            DeleteAllProfile();
            _data = pl._data;
        }

        /// <summary>
        /// プロファイル定義数
        /// </summary>
        public int Count {
            get { return _data.Count; }
        }
    }




    /// <summary>
    /// プロファイル情報構成クラス
    /// </summary>
    public class ConnectProfileStruct {
        // メンバー変数
        public const int DEFAULT_TELNET_PORT = 23;
        public const int DEFAULT_SSH_PORT = 22;
        public const int DEFAULT_CMD_SEND_INTERVAL = 200;
        public const int DEFAULT_PROMPT_RECV_TIMEOUT = 5000;
        public const string FMT_CSV = "{00},{01},{02},{03},{04},{05},{06},{07},{08},{09},{10},{11},{12},{13},{14},{15},{16},{17},{18},{19},{20},{21},{22},{23}";
        public const int CSV_FIELD_CNT = 24;
        private string _hostName;           // ホスト名
        private ConnectionMethod _protocol; // プロトコル
        private int _port;                  // ポート
        private AuthType _authType;         // SSH認証方法
        private string _keyFile;            // 秘密鍵ファイル
        private string _userName;           // ユーザ名
        private string _password;           // パスワード
        private bool _autoLogin;            // 自動ログイン
        private string _loginPrompt;        // ログインプロンプト
        private string _passwordPrompt;     // パスワードプロンプト
        private string _execCommand;        // 実行コマンド
        private string _suUserName;         // SUユーザ名
        private string _suPassword;         // SUパスワード
        private string _suType;             // SUコマンド種類
        private EncodingType _charCode;     // 文字コード
        private NewLine _newLine;           // 改行コード
        private bool _telnetNewLine;        // TelnetNewLine
        private TerminalType _terminalType; // ターミナル種類
        private Color _terminalFontColor;   // フォント色
        private Color _terminalBGColor;     // 背景色
        private int _commandSendInterval;   // コマンド発行間隔
        private int _promptRecvTimeout;     // プロンプト受信タイムアウト
        private Color _profileItemColor;    // プロファイル項目色
        private string _description;        // 説明
        private bool _checkState = false;   // リストチェック状態(ListView用)

        /// <summary>
        /// ホスト名
        /// </summary>
        public string HostName {
            get { return _hostName; }
            set { _hostName = value; }
        }

        /// <summary>
        /// プロトコル
        /// </summary>
        public ConnectionMethod Protocol {
            get { return _protocol; }
            set { _protocol = value; }
        }

        /// <summary>
        /// ポート
        /// </summary>
        public int Port {
            get { return _port; }
            set { _port = value; }
        }

        /// <summary>
        /// SSH認証方法
        /// </summary>
        public AuthType AuthType {
            get { return _authType; }
            set { _authType = value; }
        }

        /// <summary>
        /// 秘密鍵ファイル
        /// </summary>
        public string KeyFile {
            get { return _keyFile; }
            set { _keyFile = value; }
        }

        /// <summary>
        /// ユーザ名
        /// </summary>
        public string UserName {
            get { return _userName; }
            set { _userName = value; }
        }

        /// <summary>
        /// パスワード
        /// </summary>
        public string Password {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// 自動ログイン
        /// </summary>
        public bool AutoLogin {
            get { return _autoLogin; }
            set { _autoLogin = value; }
        }

        /// <summary>
        /// ログインプロンプト
        /// </summary>
        public string LoginPrompt {
            get { return _loginPrompt; }
            set { _loginPrompt = value; }
        }

        /// <summary>
        /// パスワードプロンプト
        /// </summary>
        public string PasswordPrompt {
            get { return _passwordPrompt; }
            set { _passwordPrompt = value; }
        }

        /// <summary>
        /// 実行コマンド
        /// </summary>
        public string ExecCommand {
            get { return _execCommand; }
            set { _execCommand = value; }
        }

        /// <summary>
        /// SUユーザ名
        /// </summary>
        public string SUUserName {
            get { return _suUserName; }
            set { _suUserName = value; }
        }

        /// <summary>
        /// SUパスワード
        /// </summary>
        public string SUPassword {
            get { return _suPassword; }
            set { _suPassword = value; }
        }

        /// <summary>
        /// SUコマンド種類
        /// </summary>
        public string SUType {
            get { return _suType; }
            set { _suType = value; }
        }

        /// <summary>
        /// 文字コード
        /// </summary>
        public EncodingType CharCode {
            get { return _charCode; }
            set { _charCode = value; }
        }

        /// <summary>
        /// 改行コード
        /// </summary>
        public NewLine NewLine {
            get { return _newLine; }
            set { _newLine = value; }
        }

        /// <summary>
        /// TelnetNewLine
        /// </summary>
        public bool TelnetNewLine {
            get { return _telnetNewLine; }
            set { _telnetNewLine = value; }
        }

        /// <summary>
        /// ターミナル種類
        /// </summary>
        public TerminalType TerminalType {
            get { return _terminalType; }
            set { _terminalType = value; }
        }

        /// <summary>
        /// フォント色
        /// </summary>
        public Color TerminalFontColor {
            get { return _terminalFontColor; }
            set { _terminalFontColor = value; }
        }

        /// <summary>
        /// 背景色
        /// </summary>
        public Color TerminalBGColor {
            get { return _terminalBGColor; }
            set { _terminalBGColor = value; }
        }

        /// <summary>
        /// コマンド発行間隔(ms)
        /// </summary>
        public int CommandSendInterval {
            get { return _commandSendInterval; }
            set { _commandSendInterval = value; }
        }

        /// <summary>
        /// プロンプト受信タイムアウト(ms)
        /// </summary>
        public int PromptRecvTimeout {
            get { return _promptRecvTimeout; }
            set { _promptRecvTimeout = value; }
        }

        /// <summary>
        /// プロファイル項目色
        /// </summary>
        public Color ProfileItemColor {
            get { return _profileItemColor; }
            set { _profileItemColor = value; }
        }

        /// <summary>
        /// 説明
        /// </summary>
        public string Description {
            get { return _description; }
            set { _description = value; }
        }

        /// <summary>
        /// リストチェック状態
        /// </summary>
        public bool Check {
            get { return _checkState; }
            set { _checkState = value; }
        }
    }
}
