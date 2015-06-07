/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: KeyAgent.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

using Granados;
using Granados.SSH2;
using Poderosa.Plugins;
using Poderosa.Protocols;
using Poderosa.Preferences;
using Poderosa.Forms;
using Poderosa.Util.Collections;

namespace Poderosa.Usability {
    //各鍵の状態
    internal enum PrivateKeyStatus {
        OK, //有効な鍵
        PassphraseRequired, //パスフレーズが求められる
        FileError, //ファイルが見つからない、有効な形式でない
        Initial //未処理
    }

    //個々の鍵
    internal class AgentPrivateKey {
        private string _filename;
        private PrivateKeyStatus _status;
        private SSH2UserAuthKey _key; //有効化するまではnull

        public AgentPrivateKey(string filename) {
            _filename = filename;
            _status = PrivateKeyStatus.Initial;
            _key = null;
        }

        public string FileName {
            get {
                return _filename;
            }
        }
        public PrivateKeyStatus Status {
            get {
                return _status;
            }
        }
        public SSH2UserAuthKey Key {
            get {
                return _key;
            }
        }

        //状態更新 パスフレーズ入力ダイアログから
        public void SetStatus(PrivateKeyStatus st, SSH2UserAuthKey key) {
            _status = st;
            _key = key;
        }

        //パスフレーズ入力まではいけそうかどうかを判定し、statusも更新する
        public bool GuessValidKeyFileOrWarn(IPoderosaForm form) { //別スレッドから呼ぶ版
            return InternalGuessValidKeyFileOrWarn(form, null);
        }
        public bool GuessValidKeyFileOrWarn(Form form) { //直接のWinForm版
            return InternalGuessValidKeyFileOrWarn(null, form);
        }
        private bool InternalGuessValidKeyFileOrWarn(IPoderosaForm pf, Form wf) {
            if (_status == PrivateKeyStatus.OK || _status == PrivateKeyStatus.PassphraseRequired)
                return true;

            StringResource sr = SSHUtilPlugin.Instance.Strings;
            try {
                if (!File.Exists(_filename)) {
                    Warning(pf, wf, String.Format(sr.GetString("Message.KeyAgent.FileNotExist"), _filename));
                    _status = PrivateKeyStatus.FileError;
                    return false;
                }
            }
            catch (Exception ex) {
                Warning(pf, wf, ex.Message);
                _status = PrivateKeyStatus.FileError;
                return false;
            }

            _status = PrivateKeyStatus.PassphraseRequired;
            return true;
        }

        private void Warning(IPoderosaForm pf, Form wf, string msg) {
            if (pf != null)
                pf.Warning(msg);
            else
                GUtil.Warning(wf, msg);
        }
    }


    //鍵の管理をする。プラグインの区分としてはSSHUtilPlugin内
    internal class KeyAgent : IPreferenceSupplier, IAgentForward, IConnectionResultEventHandler {
        private KeyAgentOptions _originalOptions;
        private List<AgentPrivateKey> _keys;
        private IPreferenceFolder _originalFolder;
        private bool _loadRequiredFlag; //ファイル名リストからもってくる必要の有無

        public KeyAgent() {
            _keys = new List<AgentPrivateKey>();
            _loadRequiredFlag = true;
        }

        public List<AgentPrivateKey> GetCurrentKeys() {
            if (_loadRequiredFlag)
                LoadKeys();
            return new List<AgentPrivateKey>(_keys); //ディープコピーではない、注意
        }
        //鍵リスト管理ダイアログが呼ぶ
        public void SetKeyList(List<AgentPrivateKey> keys) {
            _keys = keys;
            _loadRequiredFlag = false;
            //preferenceに反映
            StringBuilder bld = new StringBuilder();
            foreach (AgentPrivateKey k in keys) {
                if (bld.Length > 0)
                    bld.Append(',');
                bld.Append(k.FileName);
            }
            _originalOptions.PrivateKeyFileNames = bld.ToString();
        }

        #region IPreferenceSupplier
        public string PreferenceID {
            get {
                return "org.poderosa.usability.ssh-keyagent";
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _originalFolder = folder;
            _originalOptions = new KeyAgentOptions(folder);
            _originalOptions.DefineItems(builder);
            _loadRequiredFlag = true;
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            Debug.Assert(folder.Id == _originalFolder.Id);
            if (type == typeof(IKeyAgentOptions))
                return folder == _originalFolder ? _originalOptions : new KeyAgentOptions(folder).Import(_originalOptions);
            else
                return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }
        #endregion

        private delegate void InputPassphraseDelegate(IPoderosaForm form, AgentPrivateKey key);

        #region IAgentForward
        public SSH2UserAuthKey[] GetAvailableSSH2UserAuthKeys() {
            if (_loadRequiredFlag)
                LoadKeys();

            IPoderosaForm form = UsabilityPlugin.Instance.WindowManager.ActiveWindow;
            List<SSH2UserAuthKey> keys = new List<SSH2UserAuthKey>();
            foreach (AgentPrivateKey key in _keys) {
                if (key.Status == PrivateKeyStatus.OK)
                    keys.Add(key.Key); //有効であれば追加
                else if (key.Status != PrivateKeyStatus.FileError) { //ファイルエラーだったら再試行はしない
                    if (key.GuessValidKeyFileOrWarn(form)) {
                        form.AsForm().Invoke(new InputPassphraseDelegate(OpenInputPassphraseDialog), form, key);
                        if (key.Status == PrivateKeyStatus.OK)
                            keys.Add(key.Key);
                    }
                }
            }
            return keys.ToArray();
        }

        public bool CanAcceptForwarding() {
            return _originalOptions.EnableKeyAgent;
        }

        public void Close() {
        }

        public void NotifyPublicKeyDidNotMatch() {
            //ここでログに出してもいいかも
        }

        public void OnError(Exception ex) {
        }
        #endregion

        private void OpenInputPassphraseDialog(IPoderosaForm form, AgentPrivateKey key) {
            Debug.Assert(!form.AsForm().InvokeRequired);
            InputPassphraseDialog dlg = new InputPassphraseDialog(key);
            dlg.ShowDialog(form.AsForm());
        }


        #region IConnectionResultEventHandler
        public void BeforeAsyncConnect(ITerminalParameter tp) {
            ISSHLoginParameter ssh = (ISSHLoginParameter)tp.GetAdapter(typeof(ISSHLoginParameter));
            if (ssh == null)
                return; //SSH以外は興味なし

            if (ssh.Method == SSHProtocol.SSH2 && _originalOptions.EnableKeyAgent) {
                ssh.AgentForward = this; //自分をハンドルするように設定
            }
        }

        public void OnSucceeded(ITerminalParameter param) {
        }

        public void OnFailed(ITerminalParameter param, string msg) {
        }
        #endregion

        private void LoadKeys() {
            _keys.Clear();
            string filenames = _originalOptions.PrivateKeyFileNames;
            foreach (string t in filenames.Split(','))
                if (t.Length > 0)
                    _keys.Add(new AgentPrivateKey(t));
            _loadRequiredFlag = false;
        }
    }

    //Preference用インタフェース
    internal interface IKeyAgentOptions {
        bool EnableKeyAgent {
            get;
            set;
        }
        string PrivateKeyFileNames {
            get;
        } //取得のみ
    }

    internal class KeyAgentOptions : SnapshotAwarePreferenceBase, IKeyAgentOptions {
        private IBoolPreferenceItem _enabled;
        private IStringPreferenceItem _privateKeyFileNames;

        public KeyAgentOptions(IPreferenceFolder folder)
            : base(folder) {
        }

        public override void DefineItems(IPreferenceBuilder builder) {
            _enabled = builder.DefineBoolValue(_folder, "enabled", true, null);
            _privateKeyFileNames = builder.DefineStringValue(_folder, "filenames", "", null);
        }

        public KeyAgentOptions Import(KeyAgentOptions src) {
            _enabled = ConvertItem(src._enabled);
            _privateKeyFileNames = ConvertItem(src._privateKeyFileNames);
            return this;
        }

        public bool EnableKeyAgent {
            get {
                return _enabled.Value;
            }
            set {
                _enabled.Value = value;
            }
        }
        public string PrivateKeyFileNames {
            get {
                return _privateKeyFileNames.Value;
            }
            set {
                _privateKeyFileNames.Value = value;
            }
        }
    }
}
