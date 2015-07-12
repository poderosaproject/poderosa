using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;


namespace Poderosa.ConnectProfile {
    /// <summary>
    /// ConnectProfileメインフォームクラス
    /// </summary>
    public partial class ConnectProfileForm : Form {
        // メンバー変数
        private Commands _cmd = new Commands();
        private int _selectCnt = 0;
        private bool _doubleClickFlg = false;
        private bool _refreshingFlg = false;
        private MouseButtons _clickButton;
        private ListViewItemComparer _listViewItemSorter;
        private static bool _connectCancelFlg;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public ConnectProfileForm() {
            InitializeComponent();
            InitializeComponentValue();

            // 設定ファイル読み込み
            if (ConnectProfilePlugin.Instance.ConnectProfileOptionSupplier.PreferenceLoaded != true) {
                ConnectProfilePlugin.Instance.ConnectProfileOptionSupplier.LoadFromPreference();
            }
            RefreshAllProfiles();
        }

        /// <summary>
        /// オブジェクト各値設定
        /// </summary>
        private void InitializeComponentValue() {
            // テキスト
            this.Text = ConnectProfilePlugin.Strings.GetString("Caption.ConnectProfile");
            this._addProfileButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._addProfileButton");
            this._autoLoginColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._autoLoginColumn");
            this._cancelButton.Text = ConnectProfilePlugin.Strings.GetString("Form.Common._cancelButton");
            this._charCodeColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._charCodeColumn");
            this._checkAllOffButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._checkAllOffButton");
            this._copyButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._copyButton");
            this._csvExportButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._csvExportButton");
            this._csvImportButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._csvImportButton");
            this._delProfileButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._delProfileButton");
            this._descriptionColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._descriptionColumn");
            this._displaySelectedOnlyCheck.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._displaySelectedOnlyCheck");
            this._editProfileButton.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._editProfileButton");
            this._execCommandColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._execCommandColumn");
            this._filterLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._filterLabel");
            this._filterTextBox.WaterMarkText = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._filterTextBox");
            this._hintLabel.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._hintLabel");
            this._hostNameColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._hostNameColumn");
            this._newLineColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._newLineColumn");
            this._okButton.Text = ConnectProfilePlugin.Strings.GetString("Form.Common._okButton");
            this._openCSVFileDialog.Filter = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._openCSVFileDialog.Filter");
            this._openCSVFileDialog.Title = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._openCSVFileDialog.Caption");
            this._portColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._portColumn");
            this._profileCountLabel.Text = String.Format(ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._profileCountLabel"), 0);
            this._protocolColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._protocolColumn");
            this._saveCSVFileDialog.Filter = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._saveCSVFileDialog.Filter");
            this._saveCSVFileDialog.Title = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._saveCSVFileDialog.Caption");
            this._selectedProfileCountLabel.Text = String.Format(ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._selectedProfileCountLabel"), 0);
            this._suSwitchColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._suSwitchColumn");
            this._terminalBGColorColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._terminalBGColorColumn");
            this._userNameColumn.Text = ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._userNameColumn");

            // ファイル保存/開くダイアログ設定
            this._saveCSVFileDialog.DefaultExt = "csv";
            this._saveCSVFileDialog.AddExtension = true;
            this._openCSVFileDialog.DefaultExt = "csv";
            this._openCSVFileDialog.CheckFileExists = true;
            this._openCSVFileDialog.Multiselect = false;

            // プロファイルリストカラム幅
            this._hostNameColumn.Width = -2;
            this._userNameColumn.Width = -2;
            this._autoLoginColumn.Width = -2;
            this._protocolColumn.Width = -2;
            this._portColumn.Width = -2;
            this._suSwitchColumn.Width = -2;
            this._charCodeColumn.Width = -2;
            this._newLineColumn.Width = -2;
            this._execCommandColumn.Width = -2;
            this._terminalBGColorColumn.Width = -2;
            this._descriptionColumn.Width = -2;

            // プロファイルリストソートイベント作成/設定
            _listViewItemSorter = new ListViewItemComparer();
            _listViewItemSorter.ColumnModes = new ListViewItemComparer.ComparerMode[] {
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
            };
            _profileListView.ListViewItemSorter = _listViewItemSorter;
        }

        /// <summary>
        /// プロファイルリスト更新
        /// </summary>
        private void RefreshAllProfiles() {
            string str = "";
            _refreshingFlg = true;
            _profileListView.BeginUpdate();
            _profileListView.Items.Clear();

            // 各データ代入
            foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                str = string.Format("{0} {1} {2} {3} {4} {5} {6} {7} {8}", prof.HostName, prof.UserName, prof.Protocol.ToString(), prof.Port.ToString(), prof.SUUserName, prof.CharCode.ToString(), prof.NewLine, prof.ExecCommand, prof.Description);
                // フィルタ
                if (IsVisibleItem(str) == true) {
                    // チェックONのみ表示
                    if (_displaySelectedOnlyCheck.Checked == true) {
                        if (prof.Check != true) continue;
                    }

                    // リストオブジェクト作成
                    ListViewItem li = new ListViewItem();
                    li.UseItemStyleForSubItems = false; // サブアイテム書式設定用

                    // データ代入
                    li.Text = prof.HostName;
                    li.SubItems.Add(prof.UserName);
                    li.SubItems.Add((prof.AutoLogin == true) ? ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile_profileListView.Yes") : ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile_profileListView.No"));
                    li.SubItems.Add(prof.Protocol.ToString());
                    li.SubItems.Add(prof.Port.ToString());
                    li.SubItems.Add((prof.SUUserName != "") ? prof.SUUserName : "");
                    li.SubItems.Add(prof.CharCode.ToString());
                    li.SubItems.Add(prof.NewLine.ToString());
                    li.SubItems.Add((prof.ExecCommand != "") ? prof.ExecCommand : "");
                    li.SubItems.Add(""); // 背景色
                    li.SubItems.Add((prof.Description != "") ? prof.Description : "");

                    // 項目/背景色設定
                    li.ForeColor = prof.ProfileItemColor;
                    for (int i = 0; i < li.SubItems.Count; i++) {
                        li.SubItems[i].ForeColor = prof.ProfileItemColor;
                        if (i == 9) li.SubItems[i].BackColor = prof.TerminalBGColor;
                    }

                    // チェック状態復元
                    li.Checked = prof.Check;

                    // タグ追加
                    li.Tag = prof;

                    // アイテム追加
                    _profileListView.Items.Add(li);
                }
            }
            _profileListView.EndUpdate();
            _profileListView.SelectedItems.Clear();

            // プロファイル数ラベル更新
            _profileCountLabel.Text = String.Format(ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._profileCountLabel"), ConnectProfilePlugin.Profiles.Count);
            _refreshingFlg = false;
        }

        /// <summary>
        /// プロファイルリスト選択数更新
        /// </summary>
        private void RefreshSelectCount() {
            _selectCnt = 0;
            foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                if (prof.Check == true) _selectCnt++;
            }
            _selectedProfileCountLabel.Text = String.Format(ConnectProfilePlugin.Strings.GetString("Form.ConnectProfile._selectedProfileCountLabel"), _selectCnt);
        }

        /// <summary>
        /// フィルター対象抽出(大小文字区別なし, 空白区切りでAND条件)
        /// </summary>
        private bool IsVisibleItem(string targetstr) {
            string[] keys = _filterTextBox.Text.Split(' ');
            int index = 0;

            foreach (string key in keys) {
                index = targetstr.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (index < 0) break;
            }
            return (index >= 0) ? true : false;
        }

        /// <summary>
        /// 選択中のプロファイルリストを取得
        /// </summary>
        private ConnectProfileStruct GetSelectedProfile() {
            return (_profileListView.SelectedItems.Count == 0) ? null : (ConnectProfileStruct)_profileListView.SelectedItems[0].Tag;
        }

        /// <summary>
        /// 選択済みプロファイルリストのホスト名を取得
        /// </summary>
        private string GetSelectedProfileHostNames() {
            string str = "";
            foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                if (prof.Check == true) str += prof.HostName + ", ";
            }
            return str;
        }

        /// <summary>
        /// プロファイル接続
        /// </summary>
        private void Connect(ConnectProfileStruct prof) {
            if (prof != null) {
                _cmd.ConnectProfile(prof);
            }
        }

        /// <summary>
        /// プロファイル追加
        /// </summary>
        private void AddProfile() {
            _cmd.NewProfileCommand(ConnectProfilePlugin.Profiles);
            RefreshAllProfiles();
        }

        /// <summary>
        /// プロファイル編集
        /// </summary>
        private void EditProfile() {
            ConnectProfileStruct prof = GetSelectedProfile();
            if (prof != null) {
                _cmd.EditProfileCommand(ConnectProfilePlugin.Profiles, prof);
                RefreshAllProfiles();
            }
        }

        /// <summary>
        /// プロファイルコピー&編集
        /// </summary>
        private void CopyEditProfile() {
            ConnectProfileStruct prof = GetSelectedProfile();
            if (prof != null) {
                _cmd.CopyEditProfileCommand(ConnectProfilePlugin.Profiles, prof);
                RefreshAllProfiles();
            }
        }

        /// <summary>
        /// プロファイル削除
        /// </summary>
        private void DeleteProfile() {
            ConnectProfileStruct prof = GetSelectedProfile();
            if (prof != null) {
                _cmd.DeleteProfileCommand(ConnectProfilePlugin.Profiles, prof);
                RefreshAllProfiles();
                RefreshSelectCount();
            }
        }

        /// <summary>
        /// オブジェクト有効/無効化
        /// </summary>
        private void EnableControl(bool val) {
            _okButton.Enabled = val;
            _filterTextBox.Enabled = val;
            _checkAllOffButton.Enabled = val;
            _addProfileButton.Enabled = val;
            _editProfileButton.Enabled = val;
            _delProfileButton.Enabled = val;
            _copyButton.Enabled = val;
            _csvExportButton.Enabled = val;
            _csvImportButton.Enabled = val;
            _displaySelectedOnlyCheck.Enabled = val;
            _profileListView.Enabled = val;
        }

        /// <summary>
        /// フィルターテキスト変更イベント
        /// </summary>
        private void _filterTextBox_TextChanged(object sender, System.EventArgs e) {
            _filterTimer.Stop();
            _filterTimer.Start();
        }

        /// <summary>
        /// フィルターテキストキー押下イベント
        /// </summary>
        private void _filterTextBox_KeyDown(object sender, KeyEventArgs e) {
            // 上下キー押下時はリストビューにフォーカス
            if ((e.KeyCode == Keys.Up) || (e.KeyCode == Keys.Down)) _profileListView.Focus();
        }

        /// <summary>
        /// OKボタンイベント
        /// </summary>
        private void _okButton_Click(object sender, EventArgs e) {
            ArrayList hostlist = new ArrayList();
            this.DialogResult = DialogResult.None;
            _connectCancelFlg = false;
            EnableControl(false);

            if ((_selectCnt == 0) && (_profileListView.Items.Count == 1)) {
                // 選択数0 & リスト1行のみ
                _profileListView.Items[0].Selected = true;
                Connect(GetSelectedProfile());
                this.DialogResult = DialogResult.OK;
            } else if ((_selectCnt == 0) && (_profileListView.SelectedItems.Count == 1)) {
                // 選択数0 & リスト1行選択
                Connect(GetSelectedProfile());
                this.DialogResult = DialogResult.OK;
            } else if (_selectCnt > 0) {
                // 選択数1以上(複数接続)
                foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                    if (prof.Check == true) hostlist.Add(prof.HostName);
                }

                // 接続確認
                if (ConnectProfilePlugin.AskUserYesNoInvoke(String.Format(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.ConnectConfirm"), _selectCnt, String.Join(", ", (string[])hostlist.ToArray(typeof(string)))), MessageBoxIcon.Information) == DialogResult.Yes) {
                    // 接続
                    foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                        if (_connectCancelFlg != true) {
                            if (prof.Check == true) Connect(prof);
                        } else {
                            ConnectProfilePlugin.MessageBoxInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.ConnectCancel"), MessageBoxIcon.Warning);
                            this.DialogResult = DialogResult.None;
                            EnableControl(true);
                            _okButton.Focus();
                            return;
                        }
                    }
                    this.DialogResult = DialogResult.OK;
                } else {
                    // キャンセル
                    EnableControl(true);
                    _okButton.Focus();
                }
            } else {
                // リスト未選択
                ConnectProfilePlugin.AskUserYesNoInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.ProfileNotSelected"), MessageBoxIcon.Warning);
                EnableControl(true);
                _okButton.Focus();
            }
        }

        /// <summary>
        /// キャンセルボタンイベント
        /// </summary>
        private void _cancelButton_Click(object sender, EventArgs e) {
            _connectCancelFlg = true; // フォーム用
            _cmd.ConnectCancel(); // スレッド用
        }

        /// <summary>
        /// チェック全解除ボタンイベント
        /// </summary>
        private void _checkAllOffButton_Click(object sender, EventArgs e) {
            foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                prof.Check = false;
            }
            RefreshAllProfiles();
            RefreshSelectCount();
        }

        /// <summary>
        /// 追加ボタンイベント
        /// </summary>
        private void _addProfileButton_Click(object sender, EventArgs e) {
            AddProfile();
        }

        /// <summary>
        /// 編集ボタンイベント
        /// </summary>
        private void _editProfileButton_Click(object sender, EventArgs e) {
            EditProfile();
        }

        /// <summary>
        /// 削除ボタンイベント
        /// </summary>
        private void _delProfileButton_Click(object sender, EventArgs e) {
            DeleteProfile();
        }

        /// <summary>
        /// コピーボタンイベント
        /// </summary>
        private void _copyButton_Click(object sender, EventArgs e) {
            CopyEditProfile();
        }

        /// <summary>
        /// CSVエクスポートクリックイベント
        /// </summary>
        private void _csvExportButton_Click(object sender, System.EventArgs e) {
            StreamWriter sw = null;
            bool delPWFlg = false;

            if (_saveCSVFileDialog.ShowDialog() == DialogResult.OK) {
                // パスワード出力/削除確認
                if (ConnectProfilePlugin.AskUserYesNoInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVExportDetetePasssword"), MessageBoxIcon.Question) == DialogResult.Yes) {
                    delPWFlg = true;
                }

                // CSVファイル出力
                try {
                    sw = new System.IO.StreamWriter(_saveCSVFileDialog.FileName, false, System.Text.Encoding.UTF8);
                    sw.WriteLine(_cmd.CSVHeader);
                    foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                        sw.WriteLine(_cmd.ConvertCSV(prof, delPWFlg));
                    }
                    ConnectProfilePlugin.MessageBoxInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVExportComplete"), MessageBoxIcon.Information);
                } catch (Exception ex) {
                    ConnectProfilePlugin.MessageBoxInvoke(ex.Message, MessageBoxIcon.Error);
                } finally {
                    if (sw != null) sw.Close();
                }
            }
        }

        /// <summary>
        /// CSVインポートクリックイベント
        /// </summary>
        private void _csvImportButton_Click(object sender, System.EventArgs e) {
            ConnectProfileList profList = new ConnectProfileList();
            ConnectProfileStruct prof = new ConnectProfileStruct();
            StreamReader sr = null;
            bool appendFlg = false;
            int lineCnt = 1;

            if (_openCSVFileDialog.ShowDialog() == DialogResult.OK) {
                // 追加/削除確認
                if (ConnectProfilePlugin.AskUserYesNoInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportAppendProfileList"), MessageBoxIcon.Question) == DialogResult.Yes) {
                    appendFlg = true;
                }

                // 実行確認
                if (ConnectProfilePlugin.AskUserYesNoInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportConfirm"), MessageBoxIcon.Question) == DialogResult.Yes) {
                    try {
                        sr = new System.IO.StreamReader(_openCSVFileDialog.FileName, System.Text.Encoding.UTF8);
                        while (!sr.EndOfStream) {
                            string line = sr.ReadLine();

                            // 各種チェック
                            if (lineCnt == 1) {
                                // ヘッダーチェック(1行目固定)
                                if (_cmd.CheckCSVHeader(line) != true) {
                                    ConnectProfilePlugin.MessageBoxInvoke(string.Format(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportInvalidHeader"), lineCnt.ToString()), MessageBoxIcon.Error);
                                    return;
                                }
                            } else if ((line == "") || (line.IndexOf("#") == 0)) {
                                // 空白行/行頭シャープの行はスキップ
                                lineCnt++;
                                continue;
                            } else {
                                // フィールド数チェック
                                if (_cmd.CheckCSVFieldCount(line) != true) {
                                    ConnectProfilePlugin.MessageBoxInvoke(string.Format(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportInvalidFieldCount"), lineCnt.ToString()), MessageBoxIcon.Error);
                                    return;
                                }

                                // 各項目チェック
                                prof = _cmd.CheckCSVData(line);
                                if (prof == null) {
                                    ConnectProfilePlugin.MessageBoxInvoke(string.Format(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportFailed"), lineCnt.ToString()), MessageBoxIcon.Error);
                                    return;
                                }

                                // プロファイル追加
                                if (appendFlg == true) _cmd.AddProfileCommand(ConnectProfilePlugin.Profiles, prof);
                                else _cmd.AddProfileCommand(profList, prof);
                            }
                            lineCnt++;
                        }
                        // プロファイルリスト全置換
                        if (appendFlg == false) _cmd.ReplaceAllProfileCommand(ConnectProfilePlugin.Profiles, profList);

                        RefreshAllProfiles();
                        RefreshSelectCount();
                        ConnectProfilePlugin.MessageBoxInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportComplete"), MessageBoxIcon.Information);
                    } catch (Exception ex) {
                        ConnectProfilePlugin.MessageBoxInvoke(ex.Message, MessageBoxIcon.Error);
                    } finally {
                        if (sr != null) sr.Close();
                    }
                } else {
                    // キャンセル
                    ConnectProfilePlugin.MessageBoxInvoke(ConnectProfilePlugin.Strings.GetString("Message.ConnectProfile.CSVImportCancel"), MessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// 選択されているプロファイルのみ表示チェックボックスクリックイベント
        /// </summary>
        private void _displaySelectedOnlyCheck_CheckedChanged(object sender, EventArgs e) {
            if (_selectCnt == 0) _profileListView.SelectedItems.Clear();
            RefreshAllProfiles();
        }

        /// <summary>
        /// プロファイルリストカラムクリックイベント
        /// </summary>
        private void _profileListView_ColumnClick(object sender, System.Windows.Forms.ColumnClickEventArgs e) {
            _listViewItemSorter.Column = e.Column;
            _profileListView.Sort();
        }

        /// <summary>
        /// プロファイルリストマウス押下イベント
        /// </summary>
        private void _profileListView_MouseDown(object sender, MouseEventArgs e) {
            // 押下ボタン/ダブルクリックチェック
            _doubleClickFlg = (2 <= e.Clicks) ? true : false;
            _clickButton = e.Button;

            // 左ダブルクリック=接続, 中ダブルクリック=追加, 右ダブルクリック=編集
            if ((_doubleClickFlg == true) && (_clickButton == MouseButtons.Left)) {
                if (_selectCnt == 0) Connect(GetSelectedProfile());
            } else if ((_doubleClickFlg == true) && (_clickButton == MouseButtons.Middle)) {
                AddProfile();
            } else if ((_doubleClickFlg == true) && (_clickButton == MouseButtons.Right)) {
                EditProfile();
            }
        }

        /// <summary>
        /// プロファイルリストチェックステータス変更前イベント
        /// </summary>
        private void _profileListView_ItemCheck(object sender, ItemCheckEventArgs e) {
            // 何も選択されていない状態でダブルクリックした場合はチェックステータスを変更しない(左ダブルクリック接続用)
            if ((_selectCnt == 0) && (_doubleClickFlg == true)) e.NewValue = e.CurrentValue;
        }

        /// <summary>
        /// プロファイルリストチェックステータス変更後イベント
        /// </summary>
        private void _profileListView_ItemChecked(object sender, ItemCheckedEventArgs e) {
            // リスト更新実行中はスキップ(RefreshAllProfiles実行中にも度々イベントが発生してしまうため)
            if (_refreshingFlg != true) {
                e.Item.Selected = true; // チェックボックスクリック時は選択状態にならないため明示的に選択状態にする
                ConnectProfileStruct prof = GetSelectedProfile();
                if (prof != null) {
                    prof.Check = e.Item.Checked;
                    RefreshSelectCount();
                }
            }
        }

        /// <summary>
        /// フィルタータイマーイベント
        /// </summary>
        private void _filterTime_Tick(object sender, EventArgs e) {
            _filterTimer.Stop();
            RefreshAllProfiles();
        }

        /// <summary>
        /// フォームロードイベント
        /// </summary>
        private void ConnectProfileForm_Load(object sender, EventArgs e) {
            // フォーム表示時はリスト未選択状態
            _profileListView.SelectedItems.Clear();
        }

        /// <summary>
        /// フォームクローズイベント
        /// </summary>
        private void ConnectProfileForm_FormClosing(object sender, FormClosingEventArgs e) {
            // 全プロファイルのチェック状態をOFFに設定
            foreach (ConnectProfileStruct prof in ConnectProfilePlugin.Profiles) {
                prof.Check = false;
            }
        }
    }




    /// <summary>
    /// ListViewカラムソートクラス
    /// </summary>
    public class ListViewItemComparer : IComparer {
        // メンバー変数
        public enum ComparerMode { String, Integer, DateTime };
        private int _column;
        private SortOrder _order;
        private ComparerMode _mode;
        private ComparerMode[] _columnModes;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="col">ソート列番号</param>
        /// <param name="ord">ソートオーダー</param>
        /// <param name="cmod">ソート方法</param>
        public ListViewItemComparer(int col, SortOrder ord, ComparerMode cmod) {
            _column = col;
            _order = ord;
            _mode = cmod;
        }
        public ListViewItemComparer() {
            _column = 0;
            _order = SortOrder.Ascending;
            _mode = ComparerMode.String;
        }

        /// <summary>
        /// ソート
        /// </summary>
        /// <param name="x">ListViewItem</param>
        /// <param name="y">ListViewItem</param>
        public int Compare(object x, object y) {
            int result = 0;
            ListViewItem itemx = (ListViewItem)x;
            ListViewItem itemy = (ListViewItem)y;

            if (_order == SortOrder.None) return 0;

            // ソート方法決定
            if (_columnModes != null && _columnModes.Length > _column) _mode = _columnModes[_column];

            // ソート方法別にxとyを比較
            switch (_mode) {
                case ComparerMode.String:   // 文字列
                    result = string.Compare(itemx.SubItems[_column].Text, itemy.SubItems[_column].Text);
                    break;
                case ComparerMode.Integer:  // 数値
                    result = int.Parse(itemx.SubItems[_column].Text).CompareTo(int.Parse(itemy.SubItems[_column].Text));
                    break;
                case ComparerMode.DateTime: // 日付
                    result = DateTime.Compare(DateTime.Parse(itemx.SubItems[_column].Text), DateTime.Parse(itemy.SubItems[_column].Text));
                    break;
            }

            // 降順時は結果を+-逆にする
            return (_order == SortOrder.Descending) ? -result : result;
        }

        /// <summary>
        /// ソート列番号
        /// </summary>
        public int Column {
            set {
                // 同一列の時は昇順降順をスイッチ
                if (_column == value) {
                    if (_order == SortOrder.Ascending) _order = SortOrder.Descending;
                    else if (_order == SortOrder.Descending) _order = SortOrder.Ascending;
                }
                _column = value;
            }
            get { return _column; }
        }
        /// <summary>
        /// ソートオーダー
        /// </summary>
        public SortOrder Order {
            set { _order = value; }
            get { return _order; }
        }
        /// <summary>
        /// ソート方法
        /// </summary>
        public ComparerMode Mode {
            set { _mode = value; }
            get { return _mode; }
        }
        /// <summary>
        /// 列毎のソート方法
        /// </summary>
        public ComparerMode[] ColumnModes {
            set { _columnModes = value; }
        }
    }
}
