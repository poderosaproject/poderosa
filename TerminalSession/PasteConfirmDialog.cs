using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Collections;
using System.Threading;

using Poderosa.Terminal;
using Poderosa.Sessions;

namespace Poderosa.Forms {
    public partial class PasteConfirmDialog : Form {
        private ITerminalEmulatorOptions Options = TerminalSessionsPlugin.Instance.TerminalEmulatorService.TerminalEmulatorOptions;
        private ArrayList MatchPosList = new ArrayList();
        private ArrayList MatchLenList = new ArrayList();
        private string _SessionName = "";
        private int MatchCnt = 0;
        private bool NewLine = false;
        private int LineCnt = 0;
        private int SearchFlg = 0;
        private int SearchSelCnt = 0;
        private bool CancelFlg = false;

        public string ClipBoardData {
            get { return RTB_ClipBoard.Text; }
            set { RTB_ClipBoard.Text = value; }
        }
        public string SessionName {
            get { return _SessionName; }
            set { _SessionName = value; }
        }


        public PasteConfirmDialog() {
            InitializeComponent();
        }

        // アイコン読み込み
        private static Icon _questionIcon;
        private static void LoadQuestionIcon() {
            _questionIcon = SystemIcons.Question;
        }

        // 強調キーワード正規表現検索
        private int HighLightString() {
            try {
                Regex RegExp = new Regex(Options.HighlightKeyword, RegexOptions.Multiline);
                MatchPosList.Clear();
                MatchLenList.Clear();
                foreach (Match Match in RegExp.Matches(RTB_ClipBoard.Text)) {
                    RTB_ClipBoard.Select(Match.Index, Match.Length);
                    RTB_ClipBoard.SelectionColor = Color.Red;
                    RTB_ClipBoard.SelectionBackColor = Color.Yellow;
                    MatchPosList.Add(Match.Index);
                    MatchLenList.Add(Match.Length);
                }
                return RegExp.Matches(RTB_ClipBoard.Text).Count;
            } catch (Exception e) {
                GUtil.Warning(this, String.Format(TEnv.Strings.GetString("Message.PasteConfirm.RegExpError"), e.Message));
                return 0;
            }
        }


        // (ダイアログ) Load
        private void PasteConfirmDialog_Load(object sender, EventArgs e) {
            B_FindPrev.Enabled = false;
            B_FindNext.Enabled = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            L_SubMessage.Visible = false;

            // 強調キーワード検出
            MatchCnt = HighLightString();
            if (MatchCnt > 0) {
                B_FindNext.Enabled = true;
                L_Info_HighlightKeyword.ForeColor = Color.Red;
            }
            RTB_ClipBoard.DeselectAll();
            RTB_ClipBoard.SelectionStart = 0;
            //RTB_ClipBoard.Focus();
            RTB_ClipBoard.ScrollToCaret();

            // 改行文字存在チェック
            NewLine = (RTB_ClipBoard.Text.IndexOfAny(new char[] { '\r', '\n' }) >= 0) || (RTB_ClipBoard.Text.Contains(Environment.NewLine));

            // クリップボード行数取得
            LineCnt = RTB_ClipBoard.GetLineFromCharIndex(RTB_ClipBoard.TextLength) + 1;

            // ダイアログスタイル
            if (Options.EnableChangeDialogSize) this.FormBorderStyle = FormBorderStyle.Sizable;

            // 確認しましたボタン有効/無効
            if (Options.ShowConfirmedCheckBox) {
                B_OK.Enabled = false;
                CB_Confirmed.Enabled = true;
                CB_Confirmed.Checked = false;
                CB_Confirmed.ForeColor = Color.Red;
            } else {
                B_OK.Enabled = true;
                CB_Confirmed.Enabled = false;
                CB_Confirmed.Checked = true;
            }

            // 各オブジェクト文字列
            B_OK.Text = TEnv.Strings.GetString("Common.OK");
            B_Cancel.Text = TEnv.Strings.GetString("Common.Cancel");
            L_Message.Text = TEnv.Strings.GetString("Form.PasteConfirm.MainLabel");
            CB_Confirmed.Text = TEnv.Strings.GetString("Form.PasteConfirm.Confirmed");
            B_FindNext.Text = TEnv.Strings.GetString("Form.PasteConfirm.FindNext");
            B_FindPrev.Text = TEnv.Strings.GetString("Form.PasteConfirm.FindPrev");
            L_Info_Session.Text = string.Format(TEnv.Strings.GetString("Form.PasteConfirm.SessionInfo"), SessionName);
            L_Info_Line.Text = string.Format(TEnv.Strings.GetString("Form.PasteConfirm.LineInfo"), LineCnt);
            L_Info_HighlightKeyword.Text = string.Format(TEnv.Strings.GetString("Form.PasteConfirm.HighlightInfo"), MatchCnt);

            if (Options.ShowConfirmedCheckBox) {
                L_SubMessage.Text = TEnv.Strings.GetString("Form.PasteConfirm.CheckBoxMessage");
                L_SubMessage.Visible = true;
            }
            
            if (NewLine) {
                L_Info_NewLine.Text = string.Format(TEnv.Strings.GetString("Form.PasteConfirm.NewLineInfo"), TEnv.Strings.GetString("Form.PasteConfirm.NewLineInfo_Exist"));
                L_Info_NewLine.ForeColor = Color.Red;
            } else {
                L_Info_NewLine.Text = string.Format(TEnv.Strings.GetString("Form.PasteConfirm.NewLineInfo"), TEnv.Strings.GetString("Form.PasteConfirm.NewLineInfo_NotExist"));
            }
        }

        // (ダイアログ) OnPaint
        protected override void OnPaint(PaintEventArgs a) {
            base.OnPaint(a);
            if (_questionIcon == null) LoadQuestionIcon();
            a.Graphics.DrawIcon(_questionIcon, 16, 20);
        }


        // (ボタン) 前を検索
        private void B_MatchBack_Click(object sender, EventArgs e) {
            if (SearchFlg == 0) SearchSelCnt--;
            if (SearchSelCnt > 0) {
                SearchFlg = 1;
                SearchSelCnt--;
                RTB_ClipBoard.Select((int)MatchPosList[SearchSelCnt], (int)MatchLenList[SearchSelCnt]);
                if (SearchSelCnt == 0) {
                    B_FindPrev.Enabled = false;
                    RTB_ClipBoard.Focus();
                }
            }
            B_FindNext.Enabled = true;
        }

        // (ボタン) 次を検索
        private void B_MatchNext_Click(object sender, EventArgs e) {
            if (SearchFlg == 1) SearchSelCnt++;
            if (SearchSelCnt < MatchCnt) {
                SearchFlg = 0;
                RTB_ClipBoard.Select((int)MatchPosList[SearchSelCnt], (int)MatchLenList[SearchSelCnt]);
                SearchSelCnt++;
                if (SearchSelCnt == MatchCnt) {
                    B_FindNext.Enabled = false;
                    RTB_ClipBoard.Focus();
                }
            }
            if (SearchSelCnt > 1) B_FindPrev.Enabled = true;
        }

        // (チェックボックス) 確認済みチェック
        private void CB_Confirmed_CheckedChanged(object sender, EventArgs e) {
            if (CB_Confirmed.Checked) {
                B_OK.Enabled = true;
                CB_Confirmed.ForeColor = Color.Black;
                L_SubMessage.Visible = false;
            } else {
                B_OK.Enabled = false;
                CB_Confirmed.ForeColor = Color.Red;
                L_SubMessage.Text = TEnv.Strings.GetString("Form.PasteConfirm.CheckBoxMessage");
                L_SubMessage.Visible = true;
            }
        }

        // (ボタン) OK
        private void B_OK_Click(object sender, EventArgs e) {
            if ((Options.PasteAfterSpecifiedTime == true) && (Options.PasteAfterSpecifiedTimeValue > 0)) {
                B_OK.Enabled = false;
                L_SubMessage.Text = "";
                L_SubMessage.Visible = true;
                for (int i = (Options.PasteAfterSpecifiedTimeValue * 10); i > 0; i--) {
                    L_SubMessage.Text = string.Format(TEnv.Strings.GetString("Form.PasteConfirm.WaitMessage"), ((int)((i + 9) / 10)).ToString());
                    Application.DoEvents();
                    Thread.Sleep(100);
                    if (CancelFlg) break;
                }
            }
        }

        // (ボタン) キャンセル
        private void B_Cancel_Click(object sender, EventArgs e) {
            CancelFlg = true;
        }

    }
}
