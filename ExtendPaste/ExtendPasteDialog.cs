using System;
using System.Collections;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;


namespace Poderosa.ExtendPaste {
    /// <summary>
    /// <ja>ExtendPasteメインフォームクラス</ja>
    /// </summary>
    public partial class ExtendPasteDialog : Form {
        // メンバー変数
        private ExtendPasteOptions _options = ExtendPastePlugin.Instance.ExtendPasteOptionSupplier.OriginalOptions;
        private bool _newLine;
        private int _lineCnt;
        private string _session;
        private ArrayList _matchPosList = new ArrayList();
        private ArrayList _matchLenList = new ArrayList();
        private int _matchCnt = 0;
        private int _searchFlg = 0;
        private int _searchSelectCnt = 0;
        private bool _cancelFlg = false;

        /// <summary>
        /// <ja>コンストラクタ</ja>
        /// </summary>
        public ExtendPasteDialog(string data, bool newline, string session) {
            InitializeComponent();

            // クリップボードデータ/改行有無/セッション名/行数
            _clipBoardBox.Text = data;
            _newLine = newline;
            _session = session;
            _lineCnt = _clipBoardBox.GetLineFromCharIndex(_clipBoardBox.TextLength) + 1;

            // 強調キーワード検出
            _findPrevButton.Enabled = false;
            _findNextButton.Enabled = false;
            _matchCnt = SearchHighlightKeyword();
            if (_matchCnt > 0) {
                _findNextButton.Enabled = true;
                _keywordMatchLabel.ForeColor = Color.Red;
            }
            _clipBoardBox.DeselectAll();
            _clipBoardBox.SelectionStart = 0;
            _clipBoardBox.ScrollToCaret();

            // 確認チェックボックス
            _okButton.Enabled = true;
            _confirmedCheck.Visible = false;
            _subMessageLabel.Visible = false;
            if (_options.ShowConfirmCheck) {
                _subMessageLabel.Visible = true;
                _okButton.Enabled = false;
                _confirmedCheck.Visible = true;
                _confirmedCheck.Enabled = true;
                _confirmedCheck.Checked = false;
                _confirmedCheck.ForeColor = Color.Red;
            }

            // ダイアログスタイル
            if (_options.ChangeDialogSize) this.FormBorderStyle = FormBorderStyle.Sizable;
            else this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // オブジェクト初期化
            InitializeComponentValue();
        }

        /// <summary>
        /// <ja>オブジェクト初期化</ja>
        /// </summary>
        private void InitializeComponentValue() {
            // テキスト
            _okButton.Text = ExtendPastePlugin.Instance.Strings.GetString("Common.OK");
            _cancelButton.Text = ExtendPastePlugin.Instance.Strings.GetString("Common.Cancel");
            _messageLabel.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._messageLabel");
            _subMessageLabel.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._subMessageLabel.Check");
            _findNextButton.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._findNextButton");
            _findPrevButton.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._findPrevButton");
            _confirmedCheck.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._confirmedCheck");
            _targetSessionLabel.Text = string.Format(ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._targetSessionLabel"), _session);
            _keywordMatchLabel.Text = string.Format(ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._keywordMatchLabel"), _matchCnt);
            _lineCountLabel.Text = string.Format(ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._lineCountLabel"), _lineCnt);

            // 改行ラベル
            if (_newLine) {
                _newLineLabel.Text = string.Format(ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._newLineLabel"), ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog.NewLineExist"));
                _newLineLabel.ForeColor = Color.Red;
            } else {
                _newLineLabel.Text = string.Format(ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._newLineLabel"), ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog.NewLineNotExist"));
            }
        }

        /// <summary>
        /// <ja>強調キーワード検索</ja>
        /// </summary>
        private int SearchHighlightKeyword() {
            int cnt = 0;
            try {
                Regex RegExp = new Regex(_options.HighlightKeyword, RegexOptions.Multiline);
                _matchPosList.Clear();
                _matchLenList.Clear();
                foreach (Match Match in RegExp.Matches(_clipBoardBox.Text)) {
                    _clipBoardBox.Select(Match.Index, Match.Length);
                    _clipBoardBox.SelectionColor = Color.Red;
                    _clipBoardBox.SelectionBackColor = Color.Yellow;
                    _matchPosList.Add(Match.Index);
                    _matchLenList.Add(Match.Length);
                }
                cnt = RegExp.Matches(_clipBoardBox.Text).Count;
            } catch (Exception e) {
                GUtil.Warning(this, String.Format(ExtendPastePlugin.Instance.Strings.GetString("Message.ExtendPasteDialog.InvalidRegEx"), e.Message));
            }
            return cnt;
        }

        /// <summary>
        /// <ja>OKボタンクリックイベント</ja>
        /// </summary>
        private void _okButton_Click(object sender, EventArgs e) {
            if ((_options.AfterSpecifiedTimePaste) && (_options.PasteTime > 0)) {
                _okButton.Enabled = false;
                _subMessageLabel.Text = "";
                _subMessageLabel.Visible = true;
                for (int i = (_options.PasteTime * 10); i > 0; i--) {
                    _subMessageLabel.Text = string.Format(ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._subMessageLabel.Wait"), ((int)((i + 9) / 10)).ToString());
                    Application.DoEvents();
                    Thread.Sleep(100);
                    if (_cancelFlg) break;
                }
            }
        }

        /// <summary>
        /// <ja>キャンセルボタンクリックイベント</ja>
        /// </summary>
        private void _cancelButton_Click(object sender, EventArgs e) {
            _cancelFlg = true;
        }

        /// <summary>
        /// <ja>前を検索ボタンクリックイベント</ja>
        /// </summary>
        private void _findPrevButton_Click(object sender, EventArgs e) {
            if (_searchFlg == 0) _searchSelectCnt--;
            if (_searchSelectCnt > 0) {
                _searchFlg = 1;
                _searchSelectCnt--;
                _clipBoardBox.Select((int)_matchPosList[_searchSelectCnt], (int)_matchLenList[_searchSelectCnt]);
                if (_searchSelectCnt == 0) {
                    _findPrevButton.Enabled = false;
                    _clipBoardBox.Focus();
                    this.AcceptButton = null; // Enterキー連続押下でOKボタンを押下しない(誤操作防止)
                }
            }
            _findNextButton.Enabled = true;
        }

        /// <summary>
        /// <ja>次を検索ボタンクリックイベント</ja>
        /// </summary>
        private void _findNextButton_Click(object sender, EventArgs e) {
            if (_searchFlg == 1) _searchSelectCnt++;
            if (_searchSelectCnt < _matchCnt) {
                _searchFlg = 0;
                _clipBoardBox.Select((int)_matchPosList[_searchSelectCnt], (int)_matchLenList[_searchSelectCnt]);
                _searchSelectCnt++;
                if (_searchSelectCnt == _matchCnt) {
                    _findNextButton.Enabled = false;
                    _clipBoardBox.Focus();
                    this.AcceptButton = null; // Enterキー連続押下でOKボタンを押下しない(誤操作防止)
                }
            }
            if (_searchSelectCnt > 1) _findPrevButton.Enabled = true;
        }

        /// <summary>
        /// <ja>確認チェックボックスチェック変更イベント</ja>
        /// </summary>
        private void _confirmedCheck_CheckedChanged(object sender, EventArgs e) {
            if (_confirmedCheck.Checked) {
                _okButton.Enabled = true;
                _confirmedCheck.ForeColor = Color.Black;
                _subMessageLabel.Visible = false;
                this.AcceptButton = _okButton; // チェックON時はEnterキー押下でOKボタンを押下可能
            } else {
                _okButton.Enabled = false;
                _confirmedCheck.ForeColor = Color.Red;
                _subMessageLabel.Visible = true;
                _subMessageLabel.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteDialog._subMessageLabel.Check");
            }
        }

        /// <summary>
        /// <ja>フォームOnPaintイベント</ja>
        /// </summary>
        protected override void OnPaint(PaintEventArgs a) {
            base.OnPaint(a);
            a.Graphics.DrawIcon(SystemIcons.Question, 16, 16);
        }
    }
}
