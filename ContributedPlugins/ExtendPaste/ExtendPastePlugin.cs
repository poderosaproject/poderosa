/*
 * Copyright 2015 yoshikixxxx.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

using Poderosa.Commands;
using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Preferences;
using Poderosa.Sessions;
using Poderosa.Terminal;
using Poderosa.Util;


/********* アセンブリ情報 *********/
[assembly: PluginDeclaration(typeof(Poderosa.ExtendPaste.ExtendPastePlugin))]
/**********************************/


namespace Poderosa.ExtendPaste {
    /********* プラグイン情報 *********/
    [PluginInfo(
        ID = PLUGIN_ID,
        Version = VersionInfo.PODEROSA_VERSION,
        Author = VersionInfo.PROJECT_NAME,
        //Dependencies = "org.poderosa.terminalsessions;org.poderosa.usability;org.poderosa.optiondialog"
        Dependencies = "org.poderosa.terminalsessions;org.poderosa.usability;org.poderosa.optiondialog"
    )]
    /**********************************/




    /// <summary>
    /// <ja>ExtendPasteプラグインメインクラス</ja>
    /// </summary>
    internal class ExtendPastePlugin : PluginBase {
        // メンバー変数
        public const string PLUGIN_ID = "org.poderosa.extendpaste";
        private static ExtendPastePlugin _instance;
        private static ICoreServices _coreServices;
        private static StringResource _stringResource;
        private static ExtendPasteOptionsSupplier _optionSupplier;

        /// <summary>
        /// <ja>初期化</ja>
        /// </summary>
        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);
            _instance = this;

            // 文字列リソース読み込み
            _stringResource = new StringResource("Poderosa.ExtendPaste.strings", typeof(ExtendPastePlugin).Assembly);
            ExtendPastePlugin.Instance.PoderosaWorld.Culture.AddChangeListener(_stringResource);

            // コマンド登録
            IPluginManager pm = poderosa.PluginManager;
            IExtensionPoint extp_cmd = pm.FindExtensionPoint("org.poderosa.terminalsessions.pasteCommand");
            extp_cmd.RegisterExtension(new ExtendPasteCommand());

            // オプションパネル登録
            IExtensionPoint extp_opt = pm.FindExtensionPoint("org.poderosa.optionpanel");
            extp_opt.RegisterExtension(new ExtendPastePanelExtension());

            // オプションクラス登録
            _optionSupplier = new ExtendPasteOptionsSupplier();
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _coreServices.PreferenceExtensionPoint.RegisterExtension(_optionSupplier);
        }

        /// <summary>
        /// <ja>プラグイン終了</ja>
        /// </summary>
        public override void TerminatePlugin() {
            base.TerminatePlugin();
        }

        /// <summary>
        /// <ja>インスタンス</ja>
        /// </summary>
        public static ExtendPastePlugin Instance {
            get { return _instance; }
        }

        /// <summary>
        /// <ja>文字列リソース</ja>
        /// </summary>
        public StringResource Strings {
            get { return _stringResource; }
        }

        /// <summary>
        /// <ja>オプション</ja>
        /// </summary>
        public ExtendPasteOptionsSupplier ExtendPasteOptionSupplier {
            get { return _optionSupplier; }
        }
    }




    /// <summary>
    /// <ja>プラグイン実行クラス</ja>
    /// </summary>
    internal class ExtendPasteCommand : IPoderosaCommand {
        /// <summary>
        /// <ja>プラグイン実行</ja>
        /// </summary>
        public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            IPoderosaView view;
            ITerminalSession session;

            // ビュー/セッション取得
            if (!GetViewAndSession(target, out view, out session)) return CommandResult.Ignored;

            // クリップボードデータ取得
            var clipboardData = Clipboard.GetDataObject();
            if (!clipboardData.GetDataPresent("Text")) return CommandResult.Ignored;
            string data = (string)clipboardData.GetData("Text");
            if (data == null) return CommandResult.Ignored;

            // オプション取得
            ExtendPasteOptions opt = ExtendPastePlugin.Instance.ExtendPasteOptionSupplier.OriginalOptions;

            // 改行存在チェック
            bool newLineFlg = ((data.IndexOfAny(new char[] { '\r', '\n' }) >= 0) || (data.Contains(Environment.NewLine))) ? true : false;

            // セッション名取得
            //ITerminalSession sessionName = (ITerminalSession)view.Document.OwnerSession.GetAdapter(typeof(ITerminalSession));

            // 確認ダイアログ表示
            if (((opt.UseAction == UseAction.NewLine) && (newLineFlg)) || (opt.UseAction == UseAction.Always)) {
                IPoderosaForm poderosaForm = view.ParentForm;
                ExtendPasteDialog Form = new ExtendPasteDialog(data, newLineFlg, session.Caption);
                if (Form.ShowDialog(poderosaForm.AsForm()) != DialogResult.OK) return CommandResult.Ignored;
            }

            // クリップボードデータ送信
            StringReader reader = new StringReader(data);
            TerminalTransmission output = session.TerminalTransmission;
            output.SendTextStream(reader, data[data.Length - 1] == '\n');
            return CommandResult.Succeeded;
        }

        /// <summary>
        /// <ja>CanExecute</ja>
        /// </summary>
        public bool CanExecute(ICommandTarget target) {
            IPoderosaView view;
            ITerminalSession session;

            if (!GetViewAndSession(target, out view, out session)) return false;
            var clipboardData = Clipboard.GetDataObject();
            if (!clipboardData.GetDataPresent("Text")) return false;

            return true;
        }

        /// <summary>
        /// <ja>GetAdapter</ja>
        /// </summary>
        public IAdaptable GetAdapter(Type adapter) {
            return ExtendPastePlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        /// <summary>
        /// <ja>GetViewAndSession</ja>
        /// </summary>
        private bool GetViewAndSession(ICommandTarget target, out IPoderosaView view, out ITerminalSession session) {
            view = (IPoderosaView)target.GetAdapter(typeof(IPoderosaView));
            if ((view != null) && (view.Document != null)) {
                session = (ITerminalSession)view.Document.OwnerSession.GetAdapter(typeof(ITerminalSession));
                if (!session.TerminalConnection.IsClosed) return true;
            } else {
                session = null;
            }
            return false;
        }
    }




    /// <summary>
    /// <ja>オプション本体定義クラス</ja>
    /// </summary>
    internal class ExtendPasteOptionsSupplier : IPreferenceSupplier {
        // メンバー変数
        private IPreferenceFolder _originalFolder;
        private ExtendPasteOptions _originalOptions;

        /// <summary>
        /// <ja>初期化</ja>
        /// </summary>
        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _originalFolder = folder;
            _originalOptions = new ExtendPasteOptions(_originalFolder);
            _originalOptions.DefineItems(builder);
        }

        /// <summary>
        /// <ja>QueryAdapter</ja>
        /// </summary>
        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            Debug.Assert(_originalFolder.Id == folder.Id);
            if (type == typeof(IExtendPasteOptions)) {
                return _originalFolder == folder ? _originalOptions : new ExtendPasteOptions(folder).Import(_originalOptions);
            } else {
                return null;
            }
        }

        /// <summary>
        /// <ja>ValidateFolder</ja>
        /// </summary>
        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }

        /// <summary>
        /// <ja>識別子</ja>
        /// </summary>
        public string PreferenceID {
            get { return ExtendPastePlugin.PLUGIN_ID; }
        }

        /// <summary>
        /// <ja>オプション値参照</ja>
        /// </summary>
        public ExtendPasteOptions OriginalOptions {
            get { return _originalOptions; }
        }
    }




    /// <summary>
    /// <ja>オプション項目定義クラス</ja>
    /// </summary>
    internal class ExtendPasteOptions : SnapshotAwarePreferenceBase, IExtendPasteOptions {
        // メンバー変数
        private const string DEFAULT_HIGHLIGHT_KEYWORD = "rm|kill|killall|shutdown|reboot|halt";
        private EnumPreferenceItem<UseAction> _useAction;
        private IStringPreferenceItem _highlightKeyword;
        private IBoolPreferenceItem _showConfirmCheck;
        private IBoolPreferenceItem _afterSpecifiedTimePaste;
        private IIntPreferenceItem _pasteTime;
        private IBoolPreferenceItem _ChangeDialogSize;

        /// <summary>
        /// <ja>コンストラクタ</ja>
        /// </summary>
        public ExtendPasteOptions(IPreferenceFolder folder)
            : base(folder) {
        }

        /// <summary>
        /// <ja>オプション項目定義</ja>
        /// </summary>
        public override void DefineItems(IPreferenceBuilder builder) {
            _useAction = new EnumPreferenceItem<UseAction>(builder.DefineStringValue(_folder, "useAction", "WhenNewLine", null), UseAction.NewLine);
            _highlightKeyword = builder.DefineStringValue(_folder, "highlightKeyword", DEFAULT_HIGHLIGHT_KEYWORD, null);
            _showConfirmCheck = builder.DefineBoolValue(_folder, "showConfirmed", false, null);
            _afterSpecifiedTimePaste = builder.DefineBoolValue(_folder, "afterSpecifiedTimePaste", false, null);
            _pasteTime = builder.DefineIntValue(_folder, "pasteTime", 0, null);
            _ChangeDialogSize = builder.DefineBoolValue(_folder, "allowChangeDialogSize", false, null);
        }

        /// <summary>
        /// <ja>設定ファイルからインポート</ja>
        /// </summary>
        public ExtendPasteOptions Import(ExtendPasteOptions src) {
            _useAction = ConvertItem(src._useAction);
            _highlightKeyword = ConvertItem(src._highlightKeyword);
            _showConfirmCheck = ConvertItem(src._showConfirmCheck);
            _afterSpecifiedTimePaste = ConvertItem(src._afterSpecifiedTimePaste);
            _pasteTime = ConvertItem(src._pasteTime);
            _ChangeDialogSize = ConvertItem(src._ChangeDialogSize);
            return this;
        }

        /// <summary>
        /// <ja>使用アクション</ja>
        /// </summary>
        public UseAction UseAction {
            get { return _useAction.Value; }
            set { _useAction.Value = value; }
        }

        /// <summary>
        /// <ja>強調キーワード</ja>
        /// </summary>
        public string HighlightKeyword {
            get { return _highlightKeyword.Value; }
            set { _highlightKeyword.Value = value; }
        }

        /// <summary>
        /// <ja>確認チェックボックスを表示</ja>
        /// </summary>
        public bool ShowConfirmCheck {
            get { return _showConfirmCheck.Value; }
            set { _showConfirmCheck.Value = value; }
        }

        /// <summary>
        /// <ja>指定秒数経過後にペーストを実行</ja>
        /// </summary>
        public bool AfterSpecifiedTimePaste {
            get { return _afterSpecifiedTimePaste.Value; }
            set { _afterSpecifiedTimePaste.Value = value; }
        }

        /// <summary>
        /// <ja>ペーストを実行する秒数</ja>
        /// </summary>
        public int PasteTime {
            get { return _pasteTime.Value; }
            set { _pasteTime.Value = value; }
        }

        /// <summary>
        /// <ja>ダイアログサイズの変更を許可</ja>
        /// </summary>
        public bool ChangeDialogSize {
            get { return _ChangeDialogSize.Value; }
            set { _ChangeDialogSize.Value = value; }
        }
    }




    /// <summary>
    /// <ja>オプション項目定義インターフェイス</ja>
    /// </summary>
    internal interface IExtendPasteOptions {
        /// <summary>
        /// <ja>使用アクション</ja>
        /// </summary>
        UseAction UseAction {
            get;
            set;
        }

        /// <summary>
        /// <ja>強調キーワード</ja>
        /// </summary>
        string HighlightKeyword {
            get;
            set;
        }

        /// <summary>
        /// <ja>確認チェックボックスを表示</ja>
        /// </summary>
        bool ShowConfirmCheck {
            get;
            set;
        }

        /// <summary>
        /// <ja>指定秒数経過後にペーストを実行</ja>
        /// </summary>
        bool AfterSpecifiedTimePaste {
            get;
            set;
        }

        /// <summary>
        /// <ja>ペーストを実行する秒数</ja>
        /// </summary>
        int PasteTime {
            get;
            set;
        }

        /// <summary>
        /// <ja>ダイアログサイズの変更を許可</ja>
        /// </summary>
        bool ChangeDialogSize {
            get;
            set;
        }
    }




    /// <summary>
    /// <ja>使用アクション定義リスト</ja>
    /// </summary>
    internal enum UseAction {
        /// <summary>
        /// <ja>改行を含む場合に使用</ja>
        /// </summary>
        [EnumValue(Description = "Enum.UseAction.NewLine")]
        NewLine,
        /// <summary>
        /// <ja>常に使用</ja>
        /// </summary>
        [EnumValue(Description = "Enum.UseAction.Always")]
        Always,
        /// <summary>
        /// <ja>使用しない</ja>
        /// </summary>
        [EnumValue(Description = "Enum.UseAction.NotUse")]
        NotUse
    }
}
