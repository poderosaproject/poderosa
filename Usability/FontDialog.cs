/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: FontDialog.cs,v 1.4 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Drawing.Text;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Poderosa.Usability;

namespace Poderosa.Forms {
    /// <summary>
    /// GFontDialog の概要の説明です。
    /// </summary>
    internal class GFontDialog : System.Windows.Forms.Form {

        //このダイアログは言語によって様子が違ってくる
        //private Language _language;

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
            int s = (int)ascii.Size;
            _fontSizeList.SelectedIndex = _fontSizeList.FindStringExact(s.ToString());
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
            this._fontSizeList = new ComboBox();
            this._checkClearType = new System.Windows.Forms.CheckBox();
            this._checkBoldStyle = new CheckBox();
            this._checkForceBoldStyle = new CheckBox();
            this._lCJKFont = new System.Windows.Forms.Label();
            this._cjkFontList = new System.Windows.Forms.ListBox();
            this._lASCIISample = new Poderosa.Forms.ClearTypeAwareLabel();
            this._lCJKSample = new Poderosa.Forms.ClearTypeAwareLabel();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _asciiFontList
            // 
            this._asciiFontList.ItemHeight = 12;
            this._asciiFontList.Location = new System.Drawing.Point(8, 112);
            this._asciiFontList.Name = "_asciiFontList";
            this._asciiFontList.Size = new System.Drawing.Size(128, 100);
            this._asciiFontList.TabIndex = 5;
            this._asciiFontList.SelectedIndexChanged += new System.EventHandler(this.OnASCIIFontChange);
            // 
            // _lAsciiFont
            // 
            this._lAsciiFont.Location = new System.Drawing.Point(8, 88);
            this._lAsciiFont.Name = "_lAsciiFont";
            this._lAsciiFont.Size = new System.Drawing.Size(120, 23);
            this._lAsciiFont.TabIndex = 3;
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
            this._fontSizeList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._fontSizeList.Location = new System.Drawing.Point(136, 8);
            this._fontSizeList.Name = "_fontSizeList";
            this._fontSizeList.Size = new System.Drawing.Size(121, 20);
            this._fontSizeList.TabIndex = 1;
            this._fontSizeList.SelectedIndexChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _checkClearType
            // 
            this._checkClearType.Location = new System.Drawing.Point(24, 32);
            this._checkClearType.Name = "_checkClearType";
            this._checkClearType.FlatStyle = FlatStyle.System;
            this._checkClearType.Size = new System.Drawing.Size(240, 15);
            this._checkClearType.TabIndex = 2;
            this._checkClearType.CheckedChanged += new System.EventHandler(this.UpdateFontSample);
            // 
            // _checkBoldStyle
            // 
            this._checkBoldStyle.Location = new System.Drawing.Point(24, 50);
            this._checkBoldStyle.Name = "_checkBoldStyle";
            this._checkBoldStyle.FlatStyle = FlatStyle.System;
            this._checkBoldStyle.Size = new System.Drawing.Size(240, 15);
            this._checkBoldStyle.TabIndex = 3;
            this._checkBoldStyle.CheckedChanged += new System.EventHandler(this.UpdateFontSample);
            //
            // _checkForceBoldStyle
            //
            this._checkForceBoldStyle.Location = new Point(24, 68);
            this._checkForceBoldStyle.Name = "_checkForceBoldStyle";
            this._checkForceBoldStyle.FlatStyle = FlatStyle.System;
            this._checkForceBoldStyle.Size = new Size(240, 15);
            this._checkForceBoldStyle.TabIndex = 4;
            this._checkForceBoldStyle.Checked = false;
            this._checkForceBoldStyle.CheckedChanged += new EventHandler(this.UpdateFontSample);
            // 
            // _lCJKFont
            // 
            this._lCJKFont.Location = new System.Drawing.Point(144, 88);
            this._lCJKFont.Name = "_lCJKFont";
            this._lCJKFont.Size = new System.Drawing.Size(128, 23);
            this._lCJKFont.TabIndex = 6;
            this._lCJKFont.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _cjkFontList
            // 
            this._cjkFontList.ItemHeight = 12;
            this._cjkFontList.Location = new System.Drawing.Point(144, 112);
            this._cjkFontList.Name = "_cjkFontList";
            this._cjkFontList.Size = new System.Drawing.Size(128, 100);
            this._cjkFontList.TabIndex = 7;
            this._cjkFontList.SelectedIndexChanged += new System.EventHandler(this.OnCJKFontChange);
            // 
            // _lASCIISample
            // 
            this._lASCIISample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._lASCIISample.ClearType = false;
            this._lASCIISample.Location = new System.Drawing.Point(8, 216);
            this._lASCIISample.Name = "_lASCIISample";
            this._lASCIISample.Size = new System.Drawing.Size(128, 40);
            this._lASCIISample.TabIndex = 8;
            this._lASCIISample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _lCJKSample
            // 
            this._lCJKSample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._lCJKSample.ClearType = false;
            this._lCJKSample.Location = new System.Drawing.Point(144, 216);
            this._lCJKSample.Name = "_lCJKSample";
            this._lCJKSample.Size = new System.Drawing.Size(128, 40);
            this._lCJKSample.TabIndex = 9;
            this._lCJKSample.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(112, 260);
            this._okButton.Name = "_okButton";
            this._okButton.FlatStyle = FlatStyle.System;
            this._okButton.TabIndex = 10;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(200, 260);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.FlatStyle = FlatStyle.System;
            this._cancelButton.TabIndex = 11;
            this._cancelButton.Click += new System.EventHandler(this.OnCancel);
            // 
            // GFontDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(282, 295);
            this.Controls.AddRange(new System.Windows.Forms.Control[] {
                this._cancelButton,
                this._okButton,
                this._lCJKSample,
                this._lASCIISample,
                this._cjkFontList,
                this._lCJKFont,
                this._checkClearType,
                this._checkBoldStyle,
                this._checkForceBoldStyle,
                this._fontSizeList,
                this._lFontSize,
                this._lAsciiFont,
                this._asciiFontList});
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "GFontDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
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
            /*
            foreach(FontFamily f in FontFamily.Families) {
                if(!f.IsStyleAvailable(FontStyle.Regular|FontStyle.Underline|FontStyle.Bold)) continue;
                Win32.LOGFONT lf = new Win32.LOGFONT();
                new Font(f, 10).ToLogFont(lf);
                //if((lf.lfPitchAndFamily & 0x01)==0) continue; //fixed pitchのみ認める
                Debug.WriteLine(lf.lfFaceName+" " + lf.lfCharSet + " " + lf.lfPitchAndFamily);
                if(lf.lfCharSet==128)
                    _japaneseFontList.Items.Add(f.GetName(0));
                if(lf.lfCharSet!=2) //Symbol用は除く
                    _asciiFontList.Items.Add(f.GetName(0));
            }
            */
        }

        private void InitFontList() {
            Win32.tagLOGFONT lf = new Win32.tagLOGFONT();
            Graphics g = CreateGraphics();
            IntPtr hDC = g.GetHdc();

            Win32.EnumFontFamExProc proc = new Win32.EnumFontFamExProc(FontProc);
            IntPtr lParam = new IntPtr(0);
            lf.lfCharSet = 1; //default
            Win32.EnumFontFamiliesEx(hDC, ref lf, proc, lParam, 0);
            //lf.lfCharSet = 128; //日本語
            //lParam = new IntPtr(128);
            //Win32.EnumFontFamiliesEx(hDC, ref lf, proc, lParam, 0);
            g.ReleaseHdc(hDC);
        }

        private int FontProc(ref Win32.ENUMLOGFONTEX lpelfe, ref Win32.NEWTEXTMETRICEX lpntme, uint FontType, IntPtr lParam) {
            //(lpelfe.lfPitchAndFamily & 2)==0)
            bool interesting = FontType == 4 && (lpntme.ntmTm.tmPitchAndFamily & 1) == 0 && lpelfe.lfFaceName[0] != '@';
            //Terminalは依然ダメ
            //if(!interesting)
            //	if(lpelfe.lfFaceName=="FixedSys" || lpelfe.lfFaceName=="Terminal") interesting = true; //この２つだけはTrueTypeでなくともリストにいれる

            if (interesting) { //縦書きでないことはこれでしか判定できないのか？
                //さぼり
                if (/*_language==Language.Japanese && */lpntme.ntmTm.tmCharSet == 128/*SHIFTJIS_CHARSET*/
                    || lpntme.ntmTm.tmCharSet == 129/*HANGUL_CHARSET*/
                    || lpntme.ntmTm.tmCharSet == 130/*JOHAB_CHARSET*/
                    || lpntme.ntmTm.tmCharSet == 134/*GB2312_CHARSET*/
                    || lpntme.ntmTm.tmCharSet == 136/*CHINESEBIG5_CHARSET*/) {
                    _cjkFontList.Items.Add(lpelfe.lfFaceName);
                    //日本語フォントでもASCIIは必ず表示できるはず
                    if (_asciiFontList.FindStringExact(lpelfe.lfFaceName) == -1)
                        _asciiFontList.Items.Add(lpelfe.lfFaceName);
                }
                else if (lpntme.ntmTm.tmCharSet == 0) {
                    if (_asciiFontList.FindStringExact(lpelfe.lfFaceName) == -1)
                        _asciiFontList.Items.Add(lpelfe.lfFaceName);
                }
            }
            return 1;
        }

        private void UpdateFontSample(object sender, EventArgs args) {
            if (_ignoreEvent)
                return;
            _lASCIISample.ClearType = _checkClearType.Checked;
            _lCJKSample.ClearType = _checkClearType.Checked;
            OnCJKFontChange(sender, args);
            OnASCIIFontChange(sender, args);
            _lASCIISample.Invalidate();
            _lCJKSample.Invalidate();
        }
        private void OnCJKFontChange(object sender, EventArgs args) {
            if (_ignoreEvent || _cjkFontList.SelectedIndex == -1)
                return;
            string fontname = (string)_cjkFontList.Items[_cjkFontList.SelectedIndex];
            _cjkFont = RuntimeUtil.CreateFont(fontname, GetFontSize());
            if (_checkForceBoldStyle.Checked)
                _cjkFont = new Font(_cjkFont, _cjkFont.Style | FontStyle.Bold);
            _lCJKSample.Font = _cjkFont;
        }
        private void OnASCIIFontChange(object sender, EventArgs args) {
            if (_ignoreEvent || _asciiFontList.SelectedIndex == -1)
                return;
            string fontname = (string)_asciiFontList.Items[_asciiFontList.SelectedIndex];
            _asciiFont = RuntimeUtil.CreateFont(fontname, GetFontSize());
            if (_checkForceBoldStyle.Checked)
                _asciiFont = new Font(_asciiFont, _asciiFont.Style | FontStyle.Bold);
            _lASCIISample.Font = _asciiFont;
        }
        private void OnOK(object sender, EventArgs args) {
            if (!CheckFixedSizeFont("FixedSys", 14) || !CheckFixedSizeFont("Terminal", 6, 10, 14, 17, 20))
                this.DialogResult = DialogResult.None;
            else {
                this.DialogResult = DialogResult.OK;
                try {
                    Close();
                }
                catch (Exception ex) {
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine(ex.StackTrace);
                }
            }
        }
        private void OnCancel(object sender, EventArgs args) {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        //固定長フォントを使っているとき、認められていないサイズを指定していたら警告してfalseを返す。
        //allowed_sizesはサイズ指定のリストに含まれているものを使用すること！
        private bool CheckFixedSizeFont(string name, params float[] allowed_sizes) {
            if (_asciiFont.Name == name || _cjkFont.Name == name) {
                float sz = GetFontSize();
                bool contained = false;
                float diff = Single.MaxValue;
                float nearest = 0;
                foreach (float t in allowed_sizes) {
                    if (t == sz) {
                        contained = true;
                        break;
                    }
                    else {
                        if (diff > Math.Abs(sz - t)) {
                            diff = Math.Abs(sz - t);
                            nearest = t;
                        }
                    }
                }

                if (!contained) {
                    GUtil.Warning(this, String.Format(TerminalUIPlugin.Instance.Strings.GetString("Message.GFontDialog.NotTrueTypeWarning"), name, nearest));
                    _fontSizeList.SelectedIndex = _fontSizeList.FindStringExact(nearest.ToString());
                    return false;
                }
                else
                    return true;
            }
            else
                return true;
        }

        private float GetFontSize() {
            return Single.Parse((string)_fontSizeList.Items[_fontSizeList.SelectedIndex]);
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
