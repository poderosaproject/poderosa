// Copyright 2004-2025 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
//using System.Runtime.InteropServices;
using System.Windows.Forms;

using Poderosa.Usability;
using Poderosa.Terminal;

namespace Poderosa.Forms {
    /// <summary>
    /// GFontDialog の概要の説明です。
    /// </summary>
    internal class GFontDialog : System.Windows.Forms.Form {

        private System.Windows.Forms.ListBox _asciiFontList;
        private System.Windows.Forms.Label _lAsciiFont;
        private System.Windows.Forms.Label _lFontSize;
        private ComboBox _fontSizeList;
        private System.Windows.Forms.CheckBox _checkClearType;
        private System.Windows.Forms.CheckBox _checkBoldStyle;
        private System.Windows.Forms.CheckBox _checkForceBoldStyle;
        private System.Windows.Forms.Label _lCJKFont;
        private System.Windows.Forms.ListBox _cjkFontList;
        private ClearTypeAwareLabel _lASCIISample;
        private ClearTypeAwareLabel _lCJKSample;
        private TableLayoutPanel _tableLayout;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        private bool _ignoreEvent;


        private Font _cjkFont;
        private Font _asciiFont;
        public Font CJKFont {
            get {
                return _cjkFont;
            }
        }
        public Font ASCIIFont {
            get {
                return _asciiFont;
            }
        }
        public bool UseClearType {
            get {
                return _checkClearType.Checked;
            }
        }
        public bool EnableBoldStyle {
            get {
                return _checkBoldStyle.Checked;
            }
        }
        public bool ForceBoldStyle {
            get {
                return _checkForceBoldStyle.Checked;
            }
        }

        public void SetFont(bool cleartype, bool enable_bold, bool force_bold, Font ascii, Font cjk) {
            _ignoreEvent = true;
            _asciiFont = ascii;
            if (force_bold)
                _asciiFont = new Font(_asciiFont, _asciiFont.Style | FontStyle.Bold);
            _cjkFont = cjk;
            if (force_bold)
                _cjkFont = new Font(_cjkFont, _cjkFont.Style | FontStyle.Bold);
            _checkClearType.Checked = cleartype;
            _checkBoldStyle.Checked = enable_bold;
            _checkForceBoldStyle.Checked = force_bold;
            _lASCIISample.ClearType = cleartype;
            _lCJKSample.ClearType = cleartype;
            _fontSizeList.Text = ascii.Size.ToString();
            _asciiFontList.SelectedIndex = _asciiFontList.FindStringExact(ascii.Name);
            _cjkFontList.SelectedIndex = _cjkFontList.FindStringExact(cjk.Name);

            if (_asciiFontList.SelectedIndex == -1)
                _asciiFontList.SelectedIndex = _asciiFontList.FindStringExact("Courier New");
            if (_cjkFontList.SelectedIndex == -1)
                _cjkFontList.SelectedIndex = _cjkFontList.FindStringExact("ＭＳ ゴシック");

            _lASCIISample.Font = ascii;
            _lCJKSample.Font = cjk;
            _ignoreEvent = false;
        }

        public GFontDialog() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();
            //_language = GApp.Options.Language;
            StringResource sr = TerminalUIPlugin.Instance.Strings;

            this._lAsciiFont.Text = sr.GetString("Form.GFontDialog._lAsciiFont");
            this._lCJKFont.Text = sr.GetString("Form.GFontDialog._lCJKFont");
            this._lFontSize.Text = sr.GetString("Form.GFontDialog._lFontSize");
            this._checkClearType.Text = sr.GetString("Form.GFontDialog._checkClearType");
            this._checkBoldStyle.Text = sr.GetString("Form.GFontDialog._checkBoldStyle");
            this._checkForceBoldStyle.Text = sr.GetString("Form.GFontDialog._checkForceBoldStyle");
            this._okButton.Text = sr.GetString("Common.OK");
            this._cancelButton.Text = sr.GetString("Common.Cancel");
            this._lASCIISample.Text = sr.GetString("Common.FontSample");
            this._lCJKSample.Text = sr.GetString("Common.CJKFontSample");
            this.Text = sr.GetString("Form.GFontDialog.Text");
            InitUI();
        }

        /// <summary>
        /// 使用されているリソースに後処理を実行します。
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._asciiFontList = new System.Windows.Forms.ListBox();
            this._lAsciiFont = new System.Windows.Forms.Label();
            this._lFontSize = new System.Windows.Forms.Label();
            this._fontSizeList = new System.Windows.Forms.ComboBox();
            this._checkClearType = new System.Windows.Forms.CheckBox();
            this._checkBoldStyle = new System.Windows.Forms.CheckBox();
            this._checkForceBoldStyle = new System.Windows.Forms.CheckBox();
            this._lCJKFont = new System.Windows.Forms.Label();
            this._cjkFontList = new System.Windows.Forms.ListBox();
            this._lASCIISample = new Poderosa.Forms.ClearTypeAwareLabel();
            this._lCJKSample = new Poderosa.Forms.ClearTypeAwareLabel();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._tableLayout = new System.Windows.Forms.TableLayoutPanel();
            this._tableLayout.SuspendLayout();
            this.SuspendLayout();
            // 
            // _asciiFontList
            // 
            this._asciiFontList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._asciiFontList.ItemHeight = 12;
            this._asciiFontList.Location = new System.Drawing.Point(0, 25);
            this._asciiFontList.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this._asciiFontList.Name = "_asciiFontList";
            this._asciiFontList.Size = new System.Drawing.Size(129, 103);
            this._asciiFontList.TabIndex = 1;
            this._asciiFontList.SelectedIndexChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _lAsciiFont
            // 
            this._lAsciiFont.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lAsciiFont.Location = new System.Drawing.Point(0, 0);
            this._lAsciiFont.Margin = new System.Windows.Forms.Padding(0, 0, 4, 0);
            this._lAsciiFont.Name = "_lAsciiFont";
            this._lAsciiFont.Size = new System.Drawing.Size(129, 25);
            this._lAsciiFont.TabIndex = 0;
            this._lAsciiFont.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _lFontSize
            // 
            this._lFontSize.Location = new System.Drawing.Point(16, 12);
            this._lFontSize.Name = "_lFontSize";
            this._lFontSize.Size = new System.Drawing.Size(104, 16);
            this._lFontSize.TabIndex = 0;
            // 
            // _fontSizeList
            // 
            this._fontSizeList.Location = new System.Drawing.Point(136, 8);
            this._fontSizeList.Name = "_fontSizeList";
            this._fontSizeList.Size = new System.Drawing.Size(121, 20);
            this._fontSizeList.TabIndex = 1;
            this._fontSizeList.SelectedIndexChanged += new System.EventHandler(this.UpdateFontSample);
            this._fontSizeList.TextChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _checkClearType
            // 
            this._checkClearType.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._checkClearType.Location = new System.Drawing.Point(24, 32);
            this._checkClearType.Name = "_checkClearType";
            this._checkClearType.Size = new System.Drawing.Size(240, 15);
            this._checkClearType.TabIndex = 2;
            this._checkClearType.CheckedChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _checkBoldStyle
            // 
            this._checkBoldStyle.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._checkBoldStyle.Location = new System.Drawing.Point(24, 50);
            this._checkBoldStyle.Name = "_checkBoldStyle";
            this._checkBoldStyle.Size = new System.Drawing.Size(240, 15);
            this._checkBoldStyle.TabIndex = 3;
            this._checkBoldStyle.CheckedChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _checkForceBoldStyle
            // 
            this._checkForceBoldStyle.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._checkForceBoldStyle.Location = new System.Drawing.Point(24, 68);
            this._checkForceBoldStyle.Name = "_checkForceBoldStyle";
            this._checkForceBoldStyle.Size = new System.Drawing.Size(240, 15);
            this._checkForceBoldStyle.TabIndex = 4;
            this._checkForceBoldStyle.CheckedChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _lCJKFont
            // 
            this._lCJKFont.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lCJKFont.Location = new System.Drawing.Point(137, 0);
            this._lCJKFont.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this._lCJKFont.Name = "_lCJKFont";
            this._lCJKFont.Size = new System.Drawing.Size(129, 25);
            this._lCJKFont.TabIndex = 3;
            this._lCJKFont.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cjkFontList
            // 
            this._cjkFontList.Dock = System.Windows.Forms.DockStyle.Fill;
            this._cjkFontList.ItemHeight = 12;
            this._cjkFontList.Location = new System.Drawing.Point(137, 25);
            this._cjkFontList.Margin = new System.Windows.Forms.Padding(4, 0, 0, 0);
            this._cjkFontList.Name = "_cjkFontList";
            this._cjkFontList.Size = new System.Drawing.Size(129, 103);
            this._cjkFontList.TabIndex = 4;
            this._cjkFontList.SelectedIndexChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _lASCIISample
            // 
            this._lASCIISample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._lASCIISample.ClearType = false;
            this._lASCIISample.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lASCIISample.Location = new System.Drawing.Point(0, 130);
            this._lASCIISample.Margin = new System.Windows.Forms.Padding(0, 2, 4, 0);
            this._lASCIISample.Name = "_lASCIISample";
            this._lASCIISample.Size = new System.Drawing.Size(129, 38);
            this._lASCIISample.TabIndex = 2;
            this._lASCIISample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lCJKSample
            // 
            this._lCJKSample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._lCJKSample.ClearType = false;
            this._lCJKSample.Dock = System.Windows.Forms.DockStyle.Fill;
            this._lCJKSample.Location = new System.Drawing.Point(137, 130);
            this._lCJKSample.Margin = new System.Windows.Forms.Padding(4, 2, 0, 0);
            this._lCJKSample.Name = "_lCJKSample";
            this._lCJKSample.Size = new System.Drawing.Size(129, 38);
            this._lCJKSample.TabIndex = 5;
            this._lCJKSample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(110, 264);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 6;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(196, 264);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 7;
            this._cancelButton.Click += new System.EventHandler(this.OnCancel);
            // 
            // _tableLayout
            // 
            this._tableLayout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._tableLayout.ColumnCount = 2;
            this._tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._tableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this._tableLayout.Controls.Add(this._lAsciiFont, 0, 0);
            this._tableLayout.Controls.Add(this._asciiFontList, 0, 1);
            this._tableLayout.Controls.Add(this._lASCIISample, 0, 2);
            this._tableLayout.Controls.Add(this._lCJKFont, 1, 0);
            this._tableLayout.Controls.Add(this._cjkFontList, 1, 1);
            this._tableLayout.Controls.Add(this._lCJKSample, 1, 2);
            this._tableLayout.Location = new System.Drawing.Point(8, 88);
            this._tableLayout.Name = "_tableLayout";
            this._tableLayout.RowCount = 3;
            this._tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 25F));
            this._tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this._tableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            this._tableLayout.Size = new System.Drawing.Size(266, 168);
            this._tableLayout.TabIndex = 5;
            // 
            // GFontDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(282, 295);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._checkClearType);
            this.Controls.Add(this._checkBoldStyle);
            this.Controls.Add(this._checkForceBoldStyle);
            this.Controls.Add(this._fontSizeList);
            this.Controls.Add(this._lFontSize);
            this.Controls.Add(this._tableLayout);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(298, 334);
            this.Name = "GFontDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this._tableLayout.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion


        private void InitUI() {
            _fontSizeList.Items.Add("6");
            _fontSizeList.Items.Add("8");
            _fontSizeList.Items.Add("9");
            _fontSizeList.Items.Add("10");
            _fontSizeList.Items.Add("11");
            _fontSizeList.Items.Add("12");
            _fontSizeList.Items.Add("14");
            _fontSizeList.Items.Add("16");
            _fontSizeList.Items.Add("18");
            _fontSizeList.Items.Add("20");

            InitFontList();
        }

        private void InitFontList() {
            Graphics g = CreateGraphics();
            IntPtr hDC = g.GetHdc();

            List<string> asciiFonts = new List<string>();
            List<string> cjkFonts = new List<string>();
            Win32.EnumFontFamExProc proc = (ref Win32.ENUMLOGFONTEX lpelfe, ref Win32.NEWTEXTMETRICEX lpntme, uint fontType, IntPtr lParam) => {
                AddFont(ref lpelfe, ref lpntme, fontType, asciiFonts, cjkFonts);
                return 1; // continue enumeration
            };
            Win32.tagLOGFONT lf = new Win32.tagLOGFONT();
            lf.lfCharSet = 1; // DEFAULT_CHARSET
            Win32.EnumFontFamiliesEx(hDC, ref lf, proc, IntPtr.Zero, 0);
            g.ReleaseHdc(hDC);

            _asciiFontList.Items.AddRange(asciiFonts.OrderBy(s => s).Distinct().ToArray());
            _cjkFontList.Items.AddRange(cjkFonts.OrderBy(s => s).Distinct().ToArray());
        }

        private void AddFont(
            ref Win32.ENUMLOGFONTEX lpelfe,
            ref Win32.NEWTEXTMETRICEX lpntme,
            uint fontType,
            ICollection<string> asciiFonts,
            ICollection<string> cjkFonts
        ) {
            if (fontType == Win32.TRUETYPE_FONTTYPE
                && (lpelfe.elfLogFont.lfPitchAndFamily & Win32.FIXED_PITCH) != 0 /* monospace */
                && lpelfe.elfLogFont.lfFaceName.Length > 0
                && lpelfe.elfLogFont.lfFaceName[0] != '@'
            ) {
                switch (lpelfe.elfLogFont.lfCharSet) {
                    case 128: // SHIFTJIS_CHARSET
                    case 129: // HANGUL_CHARSET
                    case 130: // JOHAB_CHARSET
                    case 134: // GB2312_CHARSET
                    case 136: // CHINESEBIG5_CHARSET
                        cjkFonts.Add(lpelfe.elfLogFont.lfFaceName);
                        asciiFonts.Add(lpelfe.elfLogFont.lfFaceName);
                        break;
                    case 0: // ANSI_CHARSET
                        asciiFonts.Add(lpelfe.elfLogFont.lfFaceName);
                        break;
                }
            }
        }

        private void UpdateFontSample(object sender, EventArgs args) {
            if (_ignoreEvent)
                return;
            _lASCIISample.ClearType = _checkClearType.Checked;
            _lCJKSample.ClearType = _checkClearType.Checked;
            float fontSize = GetFontSize().GetValueOrDefault((float)TerminalEmulatorOptionConstants.DEFAULT_FONT_SIZE);
            UpdateCJKFont(fontSize);
            UpdateASCIIFont(fontSize);
            _lASCIISample.Invalidate();
            _lCJKSample.Invalidate();
        }

        private void UpdateCJKFont(float fontSize) {
            if (_ignoreEvent || _cjkFontList.SelectedIndex == -1) {
                return;
            }
            string fontName = (string)_cjkFontList.Items[_cjkFontList.SelectedIndex];
            _cjkFont = RuntimeUtil.CreateFont(fontName, fontSize);
            if (_checkForceBoldStyle.Checked) {
                _cjkFont = new Font(_cjkFont, _cjkFont.Style | FontStyle.Bold);
            }
            _lCJKSample.Font = _cjkFont;
        }

        private void UpdateASCIIFont(float fontSize) {
            if (_ignoreEvent || _asciiFontList.SelectedIndex == -1) {
                return;
            }
            string fontName = (string)_asciiFontList.Items[_asciiFontList.SelectedIndex];
            _asciiFont = RuntimeUtil.CreateFont(fontName, fontSize);
            if (_checkForceBoldStyle.Checked) {
                _asciiFont = new Font(_asciiFont, _asciiFont.Style | FontStyle.Bold);
            }
            _lASCIISample.Font = _asciiFont;
        }

        private void OnOK(object sender, EventArgs args) {
            this.DialogResult = DialogResult.OK;
            try {
                Close();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private void OnCancel(object sender, EventArgs args) {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private float? GetFontSize() {
            string fontSizeText = _fontSizeList.Text.Trim();
            float fontSize;
            if (Single.TryParse(fontSizeText, out fontSize)
                && fontSize >= (float)TerminalEmulatorOptionConstants.FONT_SIZE_MIN
                && fontSize <= (float)TerminalEmulatorOptionConstants.FONT_SIZE_MAX) {

                return fontSize;
            }
            return null;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    class ClearTypeAwareLabel : Label {
        private bool _clearType;
        public bool ClearType {
            get {
                return _clearType;
            }
            set {
                _clearType = value;
            }
        }
        protected override void OnPaint(PaintEventArgs args) {
            base.OnPaint(args);
            args.Graphics.TextRenderingHint = _clearType ? TextRenderingHint.ClearTypeGridFit : TextRenderingHint.SystemDefault;
        }
    }
}
