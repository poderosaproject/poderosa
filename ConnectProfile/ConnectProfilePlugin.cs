using System;
using System.Drawing;
using System.Windows.Forms;

using Poderosa.Commands;
using Poderosa.ConnectionParam;
using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Preferences;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.Terminal;


/********* アセンブリ情報 *********/
[assembly: PluginDeclaration(typeof(Poderosa.ConnectProfile.ConnectProfilePlugin))]
/**********************************/


namespace Poderosa.ConnectProfile {
    /********* プラグイン情報 *********/
    [PluginInfo(
        ID           = PLUGIN_ID,
        Version      = VersionInfo.PODEROSA_VERSION,
        Author       = VersionInfo.PROJECT_NAME,
        Dependencies = "org.poderosa.core.window"
    )]
    /**********************************/




    /// <summary>
    /// ConnectProfileプラグインメインクラス
    /// </summary>
    internal class ConnectProfilePlugin : PluginBase {
        // メンバー変数
        public const string PLUGIN_ID = "org.poderosa.connectprofile";
        private static ConnectProfilePlugin _instance;
        private static ICoreServices _coreServices;
        private static ConnectProfileCommand _connectProfileCommand;
        private static StringResource _stringResource;
        private static ConnectProfileList _profiles;
        private static ConnectProfileOptionsSupplier _connectProfileOptionSupplier;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;

            // 文字列リソース読み込み
            _stringResource = new StringResource("Poderosa.ConnectProfile.strings", typeof(ConnectProfilePlugin).Assembly);
            ConnectProfilePlugin.Instance.PoderosaWorld.Culture.AddChangeListener(_stringResource);

            // メニュー登録
            IPluginManager pm = poderosa.PluginManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _connectProfileCommand = new ConnectProfileCommand();
            _coreServices.CommandManager.Register(_connectProfileCommand);
            IExtensionPoint toolmenu = pm.FindExtensionPoint("org.poderosa.menu.tool");
            toolmenu.RegisterExtension(new PoderosaMenuGroupImpl(new IPoderosaMenu[] { new PoderosaMenuItemImpl(_connectProfileCommand, ConnectProfilePlugin.Strings, "Menu.ConnectProfile") }, false));

            // 設定ファイル連携
            _connectProfileOptionSupplier = new ConnectProfileOptionsSupplier();
            _coreServices.PreferenceExtensionPoint.RegisterExtension(_connectProfileOptionSupplier);

            // 接続プロファイル
            _profiles = new ConnectProfileList();
        }

        /// <summary>
        /// プラグイン終了
        /// </summary>
        public override void TerminatePlugin() {
            base.TerminatePlugin();
            _connectProfileOptionSupplier.SaveToPreference();
        }

        /// <summary>
        /// メッセージ表示Delegate
        /// </summary>
        private delegate void MessageBoxDelegate(IWin32Window window, string msg, MessageBoxIcon icon);
        /// <summary>
        /// メッセージ表示Invoke
        /// </summary>
        /// <param name="msg">メッセージ文字列</param>
        /// <param name="icon">アイコン</param>
        public static void MessageBoxInvoke(string msg, MessageBoxIcon icon) {
            Form f = ConnectProfilePlugin.Instance.WindowManager.ActiveWindow.AsForm();
            f.Invoke(new MessageBoxDelegate(GUtil.Warning), f, msg, icon);
        }

        /// <summary>
        /// 選択メッセージ表示Delegate
        /// </summary>
        private delegate DialogResult AskUserYesNoDelegate(IWin32Window window, string msg, MessageBoxIcon icon);
        /// <summary>
        /// 選択メッセージ表示Invoke
        /// </summary>
        /// <param name="msg">メッセージ文字列</param>
        /// <param name="icon">アイコン</param>
        public static DialogResult AskUserYesNoInvoke(string msg, MessageBoxIcon icon) {
            Form f = ConnectProfilePlugin.Instance.WindowManager.ActiveWindow.AsForm();
            return (DialogResult)f.Invoke(new AskUserYesNoDelegate(GUtil.AskUserYesNo), f, msg, icon);
        }

        /// <summary>
        /// インスタンス
        /// </summary>
        public static ConnectProfilePlugin Instance {
            get { return _instance; }
        }

        /// <summary>
        /// 文字列リソース
        /// </summary>
        public static StringResource Strings {
            get { return _stringResource; }
        }

        /// <summary>
        /// プロファイルリスト
        /// </summary>
        public static ConnectProfileList Profiles {
            get { return _profiles; }
        }

        /// <summary>
        /// 設定ファイル
        /// </summary>
        public ConnectProfileOptionsSupplier ConnectProfileOptionSupplier {
            get { return _connectProfileOptionSupplier; }
        }

        /// <summary>
        /// CommandManager
        /// </summary>
        public ICommandManager CommandManager {
            get { return _coreServices.CommandManager; }
        }

        /// <summary>
        /// WindowManager
        /// </summary>
        public IWindowManager WindowManager {
            get { return _coreServices.WindowManager; }
        }

        /// <summary>
        /// TerminalEmulatorService
        /// </summary>
        public ITerminalEmulatorService TerminalEmulatorService {
            get { return (ITerminalEmulatorService)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.terminalemulator", typeof(ITerminalEmulatorService)); }
        }

        /// <summary>
        /// TerminalSessionsService
        /// </summary>
        public ITerminalSessionsService TerminalSessionsService {
            get { return (ITerminalSessionsService)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService)); }
        }

        /// <summary>
        /// ProtocolService
        /// </summary>
        public IProtocolService ProtocolService {
            get { return (IProtocolService)_poderosaWorld.PluginManager.FindPlugin("org.poderosa.protocols", typeof(IProtocolService)); }
        }
    }




    /// <summary>
    /// プラグイン実行クラス
    /// </summary>
    internal class ConnectProfileCommand : GeneralCommandImpl {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConnectProfileCommand()
            : base(ConnectProfilePlugin.PLUGIN_ID, ConnectProfilePlugin.Strings, "Command.ConnectProfile", ConnectProfilePlugin.Instance.CommandManager.CommandCategories.Dialogs) {
            return;
        }

        /// <summary>
        /// プラグイン実行
        /// </summary>
        public override CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            ConnectProfileForm Form = new ConnectProfileForm();
            if (Form.ShowDialog(window.AsForm()) == DialogResult.OK) {
                return CommandResult.Succeeded;
            } else {
                return CommandResult.Cancelled;
            }
        }
    }




    /// <summary>
    /// 設定ファイル保存/読み込みクラス
    /// </summary>
    internal class ConnectProfileOptionsSupplier : IPreferenceSupplier {
        // メンバー変数
        private IPreferenceFolder _rootPreference;
        private IPreferenceFolder _profileDefinition;
        private IStringPreferenceItem _hostName;
        private IStringPreferenceItem _protocol;
        private IIntPreferenceItem _port;
        private IStringPreferenceItem _authType;
        private IStringPreferenceItem _keyFile;
        private IStringPreferenceItem _userName;
        private IStringPreferenceItem _password;
        private IBoolPreferenceItem _autoLogin;
        private IStringPreferenceItem _loginPrompt;
        private IStringPreferenceItem _passwordPrompt;
        private IStringPreferenceItem _execCommand;
        private IStringPreferenceItem _suUserName;
        private IStringPreferenceItem _suPassword;
        private IStringPreferenceItem _suType;
        private IStringPreferenceItem _charCode;
        private IStringPreferenceItem _newLine;
        private IBoolPreferenceItem _telnetNewLine;
        private IStringPreferenceItem _terminalType;
        private ColorPreferenceItem _terminalFontColor;
        private ColorPreferenceItem _terminalBGColor;
        private IIntPreferenceItem _commandSendInterval;
        private IIntPreferenceItem _promptRecvTimeout;
        private ColorPreferenceItem _profileItemColor;
        private IStringPreferenceItem _description;
        private bool _preferenceLoaded = false;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _rootPreference = folder;
            _profileDefinition = builder.DefineFolderArray(folder, this, "profile");
            _hostName = builder.DefineStringValue(_profileDefinition, "hostName", "", null);
            _protocol = builder.DefineStringValue(_profileDefinition, "protocol", "", null);
            _port = builder.DefineIntValue(_profileDefinition, "port", 0, null);
            _authType = builder.DefineStringValue(_profileDefinition, "authType", "", null);
            _keyFile = builder.DefineStringValue(_profileDefinition, "keyFile", "", null);
            _userName = builder.DefineStringValue(_profileDefinition, "userName", "", null);
            _password = builder.DefineStringValue(_profileDefinition, "password", "", null);
            _autoLogin = builder.DefineBoolValue(_profileDefinition, "autoLogin", false, null);
            _loginPrompt = builder.DefineStringValue(_profileDefinition, "loginPrompt", "", null);
            _passwordPrompt = builder.DefineStringValue(_profileDefinition, "passwordPrompt", "", null);
            _execCommand = builder.DefineStringValue(_profileDefinition, "execCommand", "", null);
            _suUserName = builder.DefineStringValue(_profileDefinition, "suUserName", "", null);
            _suPassword = builder.DefineStringValue(_profileDefinition, "suPassword", "", null);
            _suType = builder.DefineStringValue(_profileDefinition, "suType", "", null);
            _charCode = builder.DefineStringValue(_profileDefinition, "charCode", "", null);
            _newLine = builder.DefineStringValue(_profileDefinition, "newLine", "", null);
            _telnetNewLine = builder.DefineBoolValue(_profileDefinition, "telnetNewLine", true, null);
            _terminalType = builder.DefineStringValue(_profileDefinition, "terminalType", "", null);
            _terminalFontColor = new ColorPreferenceItem(builder.DefineStringValue(_profileDefinition, "terminalFontColor", "White", null), KnownColor.White);
            _terminalBGColor = new ColorPreferenceItem(builder.DefineStringValue(_profileDefinition, "terminalBGColor", "Black", null), KnownColor.Black);
            _commandSendInterval = builder.DefineIntValue(_profileDefinition, "commandSendInterval", ConnectProfileStruct.DEFAULT_CMD_SEND_INTERVAL, null);
            _promptRecvTimeout = builder.DefineIntValue(_profileDefinition, "promptRecvTimeout", ConnectProfileStruct.DEFAULT_PROMPT_RECV_TIMEOUT, null);
            _profileItemColor = new ColorPreferenceItem(builder.DefineStringValue(_profileDefinition, "profileItemColor", "Black", null), KnownColor.Black);
            _description = builder.DefineStringValue(_profileDefinition, "description", "", null);
        }

        /// <summary>
        /// 保存
        /// </summary>
        public void SaveToPreference() {
            // 一度も読み込まれていない場合は読み込む(フォームが一度も表示されてない場合に設定が消滅してしまう)
            if (this.PreferenceLoaded != true) this.LoadFromPreference();

            IPreferenceFolderArray fa = _rootPreference.FindChildFolderArray(_profileDefinition.Id);
            fa.Clear();

            foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                IPreferenceFolder f = fa.CreateNewFolder();

                // パスワード暗号化
                string pw = "";
                string supw = "";
                if (prof.Password != "") pw = new SimpleStringEncrypt().EncryptString(prof.Password);
                if (prof.SUPassword != "") supw = new SimpleStringEncrypt().EncryptString(prof.SUPassword);

                // 値代入
                fa.ConvertItem(f, _hostName).AsString().Value = prof.HostName;
                fa.ConvertItem(f, _protocol).AsString().Value = prof.Protocol.ToString();
                fa.ConvertItem(f, _port).AsInt().Value = prof.Port;
                fa.ConvertItem(f, _authType).AsString().Value = prof.AuthType.ToString();
                fa.ConvertItem(f, _keyFile).AsString().Value = prof.KeyFile;
                fa.ConvertItem(f, _userName).AsString().Value = prof.UserName;
                fa.ConvertItem(f, _password).AsString().Value = (pw != null) ? pw : "";
                fa.ConvertItem(f, _autoLogin).AsBool().Value = prof.AutoLogin;
                fa.ConvertItem(f, _loginPrompt).AsString().Value = prof.LoginPrompt;
                fa.ConvertItem(f, _passwordPrompt).AsString().Value = prof.PasswordPrompt;
                fa.ConvertItem(f, _execCommand).AsString().Value = prof.ExecCommand;
                fa.ConvertItem(f, _suUserName).AsString().Value = prof.SUUserName;
                fa.ConvertItem(f, _suPassword).AsString().Value = (supw != null) ? supw : "";
                fa.ConvertItem(f, _suType).AsString().Value = prof.SUType;
                fa.ConvertItem(f, _charCode).AsString().Value = prof.CharCode.ToString();
                fa.ConvertItem(f, _newLine).AsString().Value = prof.NewLine.ToString();
                fa.ConvertItem(f, _telnetNewLine).AsBool().Value = prof.TelnetNewLine;
                fa.ConvertItem(f, _terminalType).AsString().Value = prof.TerminalType.ToString();
                fa.ConvertItem(f, _terminalFontColor.PreferenceItem).AsString().Value = Convert.ToString(prof.TerminalFontColor.ToArgb(), 16);
                fa.ConvertItem(f, _terminalBGColor.PreferenceItem).AsString().Value = Convert.ToString(prof.TerminalBGColor.ToArgb(), 16);
                fa.ConvertItem(f, _commandSendInterval).AsInt().Value = prof.CommandSendInterval;
                fa.ConvertItem(f, _promptRecvTimeout).AsInt().Value = prof.PromptRecvTimeout;
                fa.ConvertItem(f, _profileItemColor.PreferenceItem).AsString().Value = Convert.ToString(prof.ProfileItemColor.ToArgb(), 16);
                fa.ConvertItem(f, _description).AsString().Value = prof.Description;
            }
        }

        /// <summary>
        /// 読み込み
        /// </summary>
        public void LoadFromPreference() {
            IPreferenceFolderArray fa = _rootPreference.FindChildFolderArray(_profileDefinition.Id);

            foreach (IPreferenceFolder f in fa.Folders) {
                ConnectProfileStruct prof = new ConnectProfileStruct();
                prof.HostName = fa.ConvertItem(f, _hostName).AsString().Value;
                if (fa.ConvertItem(f, _protocol).AsString().Value == "Telnet") prof.Protocol = ConnectionMethod.Telnet;
                else if (fa.ConvertItem(f, _protocol).AsString().Value == "SSH1") prof.Protocol = ConnectionMethod.SSH1;
                else if (fa.ConvertItem(f, _protocol).AsString().Value == "SSH2") prof.Protocol = ConnectionMethod.SSH2;
                prof.Port = fa.ConvertItem(f, _port).AsInt().Value;
                if (fa.ConvertItem(f, _authType).AsString().Value == "Password") prof.AuthType = AuthType.Password;
                else if (fa.ConvertItem(f, _authType).AsString().Value == "PublicKey") prof.AuthType = AuthType.PublicKey;
                else if (fa.ConvertItem(f, _authType).AsString().Value == "KeyboardInteractive") prof.AuthType = AuthType.KeyboardInteractive;
                prof.KeyFile = fa.ConvertItem(f, _keyFile).AsString().Value;
                prof.UserName = fa.ConvertItem(f, _userName).AsString().Value;
                prof.Password = fa.ConvertItem(f, _password).AsString().Value;
                prof.AutoLogin = fa.ConvertItem(f, _autoLogin).AsBool().Value;
                prof.LoginPrompt = fa.ConvertItem(f, _loginPrompt).AsString().Value;
                prof.PasswordPrompt = fa.ConvertItem(f, _passwordPrompt).AsString().Value;
                prof.ExecCommand = fa.ConvertItem(f, _execCommand).AsString().Value;
                prof.SUUserName = fa.ConvertItem(f, _suUserName).AsString().Value;
                prof.SUPassword = fa.ConvertItem(f, _suPassword).AsString().Value;
                prof.SUType = fa.ConvertItem(f, _suType).AsString().Value;
                if (fa.ConvertItem(f, _charCode).AsString().Value == "ISO8859_1") prof.CharCode = EncodingType.ISO8859_1;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "UTF8") prof.CharCode = EncodingType.UTF8;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "EUC_JP") prof.CharCode = EncodingType.EUC_JP;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "SHIFT_JIS") prof.CharCode = EncodingType.SHIFT_JIS;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "GB2312") prof.CharCode = EncodingType.GB2312;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "BIG5") prof.CharCode = EncodingType.BIG5;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "EUC_CN") prof.CharCode = EncodingType.EUC_CN;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "EUC_KR") prof.CharCode = EncodingType.EUC_KR;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "UTF8_Latin") prof.CharCode = EncodingType.UTF8_Latin;
                else if (fa.ConvertItem(f, _charCode).AsString().Value == "OEM850") prof.CharCode = EncodingType.OEM850;
                if (fa.ConvertItem(f, _newLine).AsString().Value == "CR") prof.NewLine = NewLine.CR;
                else if (fa.ConvertItem(f, _newLine).AsString().Value == "LF") prof.NewLine = NewLine.LF;
                else if (fa.ConvertItem(f, _newLine).AsString().Value == "CRLF") prof.NewLine = NewLine.CRLF;
                prof.TelnetNewLine = fa.ConvertItem(f, _telnetNewLine).AsBool().Value;
                if (fa.ConvertItem(f, _terminalType).AsString().Value == "KTerm") prof.TerminalType = TerminalType.KTerm;
                else if (fa.ConvertItem(f, _terminalType).AsString().Value == "VT100") prof.TerminalType = TerminalType.VT100;
                else if (fa.ConvertItem(f, _terminalType).AsString().Value == "XTerm") prof.TerminalType = TerminalType.XTerm;
                prof.TerminalFontColor = ParseUtil.ParseColor(fa.ConvertItem(f, _terminalFontColor.PreferenceItem).AsString().Value, Color.White);
                prof.TerminalBGColor = ParseUtil.ParseColor(fa.ConvertItem(f, _terminalBGColor.PreferenceItem).AsString().Value, Color.Black);
                prof.CommandSendInterval = fa.ConvertItem(f, _commandSendInterval).AsInt().Value;
                prof.PromptRecvTimeout = fa.ConvertItem(f, _promptRecvTimeout).AsInt().Value;
                prof.ProfileItemColor = ParseUtil.ParseColor(fa.ConvertItem(f, _profileItemColor.PreferenceItem).AsString().Value, Color.Black);
                prof.Description = fa.ConvertItem(f, _description).AsString().Value;

                // パスワード複合化
                if (prof.Password != "") prof.Password = new SimpleStringEncrypt().DecryptString(prof.Password);
                if (prof.SUPassword != "") prof.SUPassword = new SimpleStringEncrypt().DecryptString(prof.SUPassword);

                ConnectProfilePlugin.Profiles.AddProfile(prof);
            }

            _preferenceLoaded = true;
        }

        /// <summary>
        /// ValidateFolder
        /// </summary>
        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
            return;
        }

        /// <summary>
        /// QueryAdapter
        /// </summary>
        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        /// <summary>
        /// プラグインID
        /// </summary>
        public string PreferenceID {
            get { return ConnectProfilePlugin.PLUGIN_ID; }
        }

        /// <summary>
        /// 読み込み完了フラグ
        /// </summary>
        public bool PreferenceLoaded {
            get { return _preferenceLoaded; }
        }
    }
}
