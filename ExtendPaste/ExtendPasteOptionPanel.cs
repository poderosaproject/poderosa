using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Poderosa.Preferences;
using Poderosa.Terminal;
using Poderosa.Usability;
using Poderosa.Util;


namespace Poderosa.ExtendPaste {
    /// <summary>
    /// <ja>オプションパネルフォームクラス</ja>
    /// </summary>
    internal class ExtendPasteOptionPanel : UserControl {
        // メンバー変数
        private CheckBox _afterSpecifiedTimePasteCheck;
        private CheckBox _changeDialogSize;
        private CheckBox _showConfirmCheck;
        private ComboBox _useActionBox;
        private GroupBox _extendPasteOptionGroup;
        private Label _descriptionLabel;
        private Label _highlightKeywordLabel;
        private Label _useActionLabel;
        private NumericUpDown _pasteTimeBox;
        private TextBox _highlightKeywordBox;

        /// <summary>
        /// <ja>コンストラクタ</ja>
        /// </summary>
        public ExtendPasteOptionPanel() {
            InitializeComponent();
            FillText();
        }

        /// <summary>
        /// <ja>InitializeComponent</ja>
        /// </summary>
        private void InitializeComponent() {
            this._descriptionLabel = new System.Windows.Forms.Label();
            this._useActionLabel = new System.Windows.Forms.Label();
            this._useActionBox = new System.Windows.Forms.ComboBox();
            this._showConfirmCheck = new System.Windows.Forms.CheckBox();
            this._afterSpecifiedTimePasteCheck = new System.Windows.Forms.CheckBox();
            this._changeDialogSize = new System.Windows.Forms.CheckBox();
            this._extendPasteOptionGroup = new System.Windows.Forms.GroupBox();
            this._pasteTimeBox = new System.Windows.Forms.NumericUpDown();
            this._highlightKeywordBox = new System.Windows.Forms.TextBox();
            this._highlightKeywordLabel = new System.Windows.Forms.Label();
            this._extendPasteOptionGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._pasteTimeBox)).BeginInit();
            this.SuspendLayout();
            //
            // _descriptionLabel
            //
            this._descriptionLabel.Location = new System.Drawing.Point(11, 210);
            this._descriptionLabel.Name = "_descriptionLabel";
            this._descriptionLabel.Size = new System.Drawing.Size(419, 155);
            this._descriptionLabel.TabIndex = 0;
            this._descriptionLabel.Text = "_descriptionLabel";
            //
            // _useActionLabel
            //
            this._useActionLabel.AutoSize = true;
            this._useActionLabel.Location = new System.Drawing.Point(15, 24);
            this._useActionLabel.Name = "_useActionLabel";
            this._useActionLabel.Size = new System.Drawing.Size(87, 12);
            this._useActionLabel.TabIndex = 1;
            this._useActionLabel.Text = "_useActionLabel";
            //
            // _useActionBox
            //
            this._useActionBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._useActionBox.FormattingEnabled = true;
            this._useActionBox.Location = new System.Drawing.Point(172, 21);
            this._useActionBox.Name = "_useActionBox";
            this._useActionBox.Size = new System.Drawing.Size(239, 20);
            this._useActionBox.TabIndex = 2;
            this._useActionBox.SelectedIndexChanged += new System.EventHandler(this._useActionBox_SelectedIndexChanged);
            //
            // _showConfirmCheck
            //
            this._showConfirmCheck.AutoSize = true;
            this._showConfirmCheck.Location = new System.Drawing.Point(17, 87);
            this._showConfirmCheck.Name = "_showConfirmCheck";
            this._showConfirmCheck.Size = new System.Drawing.Size(126, 16);
            this._showConfirmCheck.TabIndex = 6;
            this._showConfirmCheck.Text = "_showConfirmCheck";
            this._showConfirmCheck.UseVisualStyleBackColor = true;
            //
            // _afterSpecifiedTimePasteCheck
            //
            this._afterSpecifiedTimePasteCheck.AutoSize = true;
            this._afterSpecifiedTimePasteCheck.Location = new System.Drawing.Point(17, 118);
            this._afterSpecifiedTimePasteCheck.Name = "_afterSpecifiedTimePasteCheck";
            this._afterSpecifiedTimePasteCheck.Size = new System.Drawing.Size(185, 16);
            this._afterSpecifiedTimePasteCheck.TabIndex = 7;
            this._afterSpecifiedTimePasteCheck.Text = "_afterSpecifiedTimePasteCheck";
            this._afterSpecifiedTimePasteCheck.UseVisualStyleBackColor = true;
            this._afterSpecifiedTimePasteCheck.CheckedChanged += new System.EventHandler(this._afterSpecifiedTimePasteCheck_CheckedChanged);
            //
            // _changeDialogSize
            //
            this._changeDialogSize.AutoSize = true;
            this._changeDialogSize.Location = new System.Drawing.Point(17, 149);
            this._changeDialogSize.Name = "_changeDialogSize";
            this._changeDialogSize.Size = new System.Drawing.Size(117, 16);
            this._changeDialogSize.TabIndex = 8;
            this._changeDialogSize.Text = "_changeDialogSize";
            this._changeDialogSize.UseVisualStyleBackColor = true;
            //
            // _extendPasteOptionGroup
            //
            this._extendPasteOptionGroup.Controls.Add(this._pasteTimeBox);
            this._extendPasteOptionGroup.Controls.Add(this._highlightKeywordBox);
            this._extendPasteOptionGroup.Controls.Add(this._highlightKeywordLabel);
            this._extendPasteOptionGroup.Controls.Add(this._useActionLabel);
            this._extendPasteOptionGroup.Controls.Add(this._changeDialogSize);
            this._extendPasteOptionGroup.Controls.Add(this._useActionBox);
            this._extendPasteOptionGroup.Controls.Add(this._afterSpecifiedTimePasteCheck);
            this._extendPasteOptionGroup.Controls.Add(this._showConfirmCheck);
            this._extendPasteOptionGroup.Location = new System.Drawing.Point(13, 13);
            this._extendPasteOptionGroup.Name = "_extendPasteOptionGroup";
            this._extendPasteOptionGroup.Size = new System.Drawing.Size(417, 185);
            this._extendPasteOptionGroup.TabIndex = 0;
            this._extendPasteOptionGroup.TabStop = false;
            this._extendPasteOptionGroup.Text = "_extendPasteOptionGroup";
            //
            // _pasteTimeBox
            //
            this._pasteTimeBox.Location = new System.Drawing.Point(366, 118);
            this._pasteTimeBox.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this._pasteTimeBox.Name = "_pasteTimeBox";
            this._pasteTimeBox.Size = new System.Drawing.Size(45, 19);
            this._pasteTimeBox.TabIndex = 11;
            //
            // _highlightKeywordBox
            //
            this._highlightKeywordBox.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this._highlightKeywordBox.Location = new System.Drawing.Point(172, 54);
            this._highlightKeywordBox.Name = "_highlightKeywordBox";
            this._highlightKeywordBox.Size = new System.Drawing.Size(239, 19);
            this._highlightKeywordBox.TabIndex = 10;
            //
            // _highlightKeywordLabel
            //
            this._highlightKeywordLabel.AutoSize = true;
            this._highlightKeywordLabel.Location = new System.Drawing.Point(15, 57);
            this._highlightKeywordLabel.Name = "_highlightKeywordLabel";
            this._highlightKeywordLabel.Size = new System.Drawing.Size(122, 12);
            this._highlightKeywordLabel.TabIndex = 9;
            this._highlightKeywordLabel.Text = "_highlightKeywordLabel";
            //
            // ExtendPasteOptionPanel
            //
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this._extendPasteOptionGroup);
            this.Controls.Add(this._descriptionLabel);
            this.Name = "ExtendPasteOptionPanel";
            this.Size = new System.Drawing.Size(448, 380);
            this._extendPasteOptionGroup.ResumeLayout(false);
            this._extendPasteOptionGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._pasteTimeBox)).EndInit();
            this.ResumeLayout(false);

        }

        /// <summary>
        /// <ja>オブジェクトテキスト初期化</ja>
        /// </summary>
        private void FillText() {
            // テキスト
            this._extendPasteOptionGroup.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._extendPasteOptionGroup");
            this._useActionLabel.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._useActionLabel");
            this._highlightKeywordLabel.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._highlightKeywordLabel");
            this._showConfirmCheck.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._showConfirmCheck");
            this._afterSpecifiedTimePasteCheck.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._afterSpecifiedTimePasteCheck");
            this._changeDialogSize.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._changeDialogSize");
            this._descriptionLabel.Text = ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel._descriptionLabel");

            // コンボボックス
            this._useActionBox.Items.AddRange(EnumListItem<UseAction>.GetListItems());
        }

        /// <summary>
        /// <ja>オプション値をオブジェクトに反映(パネルを開く度に実行される)</ja>
        /// </summary>
        public void InitUI(IExtendPasteOptions options) {
            this._useActionBox.SelectedItem = options.UseAction;
            this._highlightKeywordBox.Text = options.HighlightKeyword;
            this._showConfirmCheck.Checked = options.ShowConfirmCheck;
            this._afterSpecifiedTimePasteCheck.Checked = options.AfterSpecifiedTimePaste;
            this._pasteTimeBox.Value = options.PasteTime;
            this._changeDialogSize.Checked = options.ChangeDialogSize;
        }

        /// <summary>
        /// <ja>コミット(パネルを切り替える度に実行される)</ja>
        /// </summary>
        public bool Commit(IExtendPasteOptions options) {
            StringResource sr = ExtendPastePlugin.Instance.Strings;
            bool successful = false;
            string itemName = null;

            try {
                options.UseAction = ((EnumListItem<UseAction>)_useActionBox.SelectedItem).Value;
                options.HighlightKeyword = _highlightKeywordBox.Text;
                options.ShowConfirmCheck = _showConfirmCheck.Checked;
                options.AfterSpecifiedTimePaste = _afterSpecifiedTimePasteCheck.Checked;
                options.ChangeDialogSize = _changeDialogSize.Checked;

                // 強調キーワード正規表現パターンチェック
                itemName = sr.GetString("Message.ExtendPasteOptionPanel.RegExpPettern");
                Regex RegExp = new Regex(options.HighlightKeyword, RegexOptions.Multiline);

                // ペースト秒数チェック
                itemName = sr.GetString("Message.ExtendPasteOptionPanel.AfterSpecifiedTimePaste");
                if ((_afterSpecifiedTimePasteCheck.Checked) && (_pasteTimeBox.Value > 0)) {
                    options.PasteTime = (int)_pasteTimeBox.Value;
                } else {
                    options.PasteTime = 0;
                    options.AfterSpecifiedTimePaste = false;
                }

                successful = true;
            } catch (InvalidOptionException ex) {
                GUtil.Warning(this, ex.Message);
            } catch (Exception) {
                GUtil.Warning(this, String.Format(sr.GetString("Message.ExtendPasteOptionPanel.InvalidItem"), itemName));
            }

            return successful;
        }

        /// <summary>
        /// <ja>使用アクションコンボボックス選択変更イベント</ja>
        /// </summary>
        private void _useActionBox_SelectedIndexChanged(object sender, EventArgs e) {
            bool value = (((EnumListItem<UseAction>)_useActionBox.SelectedItem).Value != UseAction.NotUse) ? true : false;
            _highlightKeywordBox.Enabled = value;
            _showConfirmCheck.Enabled = value;
            _afterSpecifiedTimePasteCheck.Enabled = value;
            _pasteTimeBox.Enabled = (value && _afterSpecifiedTimePasteCheck.Checked);
            _changeDialogSize.Enabled = value;
        }

        /// <summary>
        /// <ja>指定秒数後ペーストチェックボックス切替イベント</ja>
        /// </summary>
        private void _afterSpecifiedTimePasteCheck_CheckedChanged(object sender, EventArgs e) {
            if ((((EnumListItem<UseAction>)_useActionBox.SelectedItem).Value != UseAction.NotUse) && (_afterSpecifiedTimePasteCheck.Checked == true)) {
                _pasteTimeBox.Enabled = true;
            } else {
                _pasteTimeBox.Enabled = false;
            }
        }
    }




    /// <summary>
    /// <ja>オプションパネル定義クラス</ja>
    /// </summary>
    internal class ExtendPastePanelExtension : IOptionPanelExtension {
        // メンバー変数
        private ExtendPasteOptionPanel _panel;

        /// <summary>
        /// <ja>オプションパネル初期化</ja>
        /// </summary>
        public void InitiUI(IPreferenceFolder[] values) {
            if (_panel == null) _panel = new ExtendPasteOptionPanel();
            _panel.InitUI((IExtendPasteOptions)values[0].QueryAdapter(typeof(IExtendPasteOptions)));
        }

        /// <summary>
        /// <ja>コミット</ja>
        /// </summary>
        public bool Commit(IPreferenceFolder[] values) {
            return _panel.Commit((IExtendPasteOptions)values[0].QueryAdapter(typeof(IExtendPasteOptions)));
        }

        /// <summary>
        /// <ja>Dispose</ja>
        /// </summary>
        public void Dispose() {
            if (_panel != null) {
                _panel.Dispose();
                _panel = null;
            }
        }

        /// <summary>
        /// <ja>項目タイトル</ja>
        /// </summary>
        public string Caption {
            get { return ExtendPastePlugin.Instance.Strings.GetString("Form.ExtendPasteOptionPanel.Caption"); }
        }

        /// <summary>
        /// <ja>項目アイコン</ja>
        /// </summary>
        public Image Icon {
            get { return Properties.Resources.OptionDialogIcon; }
        }

        /// <summary>
        /// <ja>オプションパネル</ja>
        /// </summary>
        public Control ContentPanel {
            get { return _panel; }
        }

        /// <summary>
        /// <ja>識別子</ja>
        /// </summary>
        public string[] PreferenceFolderIDsToEdit {
            get { return new string[] { ExtendPastePlugin.PLUGIN_ID }; }
        }
    }
}
