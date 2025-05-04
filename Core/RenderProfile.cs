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
using System.IO;
using System.Collections;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Forms;

#if !MACRODOC
using Poderosa.Util;
using Poderosa.Util.Drawing;
using Poderosa.Document;
using System.Globalization;
#endif

namespace Poderosa.View {

#if MACRODOC
    /// <summary>
    /// <ja>背景画像の位置を指定します。</ja>
    /// <en>Specifies the position of the background image.</en>
    /// </summary>
    public enum ImageStyle {
        /// <summary>
        /// <ja>中央</ja>
        /// <en>Center</en>
        /// </summary>
        Center,
        /// <summary>
        /// <ja>左上</ja>
        /// <en>Upper left corner</en>
        /// </summary>
        TopLeft,
        /// <summary>
        /// <ja>右上</ja>
        /// <en>Upper right corner</en>
        /// </summary>
        TopRight,
        /// <summary>
        /// <ja>左下</ja>
        /// <en>Lower left corner</en>
        /// </summary>
        BottomLeft,
        /// <summary>
        /// <ja>右下</ja>
        /// <en>Lower right corner</en>
        /// </summary>
        BottomRight,
        /// <summary>
        /// <ja>伸縮して全体に表示</ja>
        /// <en>The image covers the whole area of the console by expansion</en>
        /// </summary>
        Scaled
    }
#else
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum ImageStyle {
        [EnumValue(Description = "Enum.ImageStyle.Center")]
        Center,
        [EnumValue(Description = "Enum.ImageStyle.TopLeft")]
        TopLeft,
        [EnumValue(Description = "Enum.ImageStyle.TopRight")]
        TopRight,
        [EnumValue(Description = "Enum.ImageStyle.BottomLeft")]
        BottomLeft,
        [EnumValue(Description = "Enum.ImageStyle.BottomRight")]
        BottomRight,
        [EnumValue(Description = "Enum.ImageStyle.Scaled")]
        Scaled,
        [EnumValue(Description = "Enum.ImageStyle.HorizontalFit")]
        HorizontalFit,
        [EnumValue(Description = "Enum.ImageStyle.VerticalFit")]
        VerticalFit
    }
#endif

    internal class FontHandle {
        private readonly Font _font;
        private readonly bool _clearType;
        private readonly int? _charWidth;
        private IntPtr _hFont;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="f">reference Font object</param>
        /// <param name="clearType">true if use ClearType</param>
        /// <param name="charWidth">value of LOGFONT.lfWidth</param>
        public FontHandle(Font f, bool clearType, int? charWidth) {
            _font = f;
            _clearType = clearType;
            _charWidth = charWidth;
            _hFont = IntPtr.Zero;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="f">reference Font object</param>
        /// <param name="clearType">true if use ClearType</param>
        public FontHandle(Font f, bool clearType)
            : this(f, clearType, null) {
        }

        public Font Font {
            get {
                return _font;
            }
        }

        public IntPtr HFONT {
            get {
                if (_hFont == IntPtr.Zero) {
                    CreateFont();
                }
                return _hFont;
            }
        }

        private void CreateFont() {
            lock (this) {
                if (_hFont == IntPtr.Zero) {
                    if (_clearType) {
                        Win32.LOGFONT lf = new Win32.LOGFONT();
                        _font.ToLogFont(lf);
                        Version osVer = Environment.OSVersion.Version;
                        int major = osVer.Major;
                        int minor = osVer.Minor;
                        if (major > 5 || (major == 5 && minor >= 1)) {
                            lf.lfQuality = Win32.CLEARTYPE_NATURAL_QUALITY;
                        }
                        else {
                            lf.lfQuality = Win32.CLEARTYPE_QUALITY;
                        }
                        if (_charWidth.HasValue) {
                            lf.lfWidth = _charWidth.Value;
                        }
                        _hFont = Win32.CreateFontIndirect(lf);
                    }
                    else {
                        _hFont = _font.ToHfont();
                    }
                }
            }
        }

        public void Dispose() {
            if (_hFont != IntPtr.Zero)
                Win32.DeleteObject(_hFont);
            _hFont = IntPtr.Zero;
            _font.Dispose();
        }
    }

    /// <summary>
    /// <ja>コンソールの表示方法を指定するオブジェクトです。接続前にTerminalParamのRenderProfileプロパティにセットすることで、マクロから色・フォント・背景画像を指定できます。</ja>
    /// <en>Implements the parameters for displaying the console. By setting this object to the RenderProfile property of the TerminalParam object, the macro can control colors, fonts, and background images.</en>
    /// </summary>
    public class RenderProfile : ICloneable {

        private class FontSet {
            public readonly FontHandle NormalFont;
            public readonly FontHandle BoldFont;

            public FontSet(
                FontHandle normalFont,
                FontHandle boldFont
            ) {
                NormalFont = normalFont;
                BoldFont = boldFont;
            }

            public void Dispose() {
                NormalFont.Dispose();
                BoldFont.Dispose();
            }
        }

        private class FontSizes {
            public FontSet Single = null;
            public FontSet Double = null;
            public FontSet Quad = null;

            public void Dispose() {
                if (Single != null) {
                    Single.Dispose();
                    Single = null;
                }
                if (Double != null) {
                    Double.Dispose();
                    Double = null;
                }
                if (Quad != null) {
                    Quad.Dispose();
                    Quad = null;
                }
            }
        }

        private class Fonts {
            public readonly FontSizes Latin = new FontSizes();
            public readonly FontSizes CJK = new FontSizes();

            public void Dispose() {
                Latin.Dispose();
                CJK.Dispose();
            }
        }

        private readonly Fonts _fonts = new Fonts();

        private string _fontName;
        private string _cjkFontName;
        private float _fontSize;
        private bool _useClearType;
        private bool _enableBoldStyle;
        private bool _forceBoldStyle;
#if !MACRODOC
        private EscapesequenceColorSet _esColorSet;
#endif
        private bool _darkenEsColorForBackground;

        private Color _forecolor;
        private Color _bgcolor;

        private Brush _brush;
        private Brush _bgbrush;

        private string _backgroundImageFileName;
        private Image _backgroundImage;
        private bool _imageLoadIsAttempted;
        private ImageStyle _imageStyle;

        private SizeF _pitch;
        private int _lineSpacing;
        private float _charGap; //文字列を表示するときに左右につく余白

        /// <summary>
        /// <ja>通常の文字を表示するためのフォント名です。</ja>
        /// <en>Gets or sets the font name for normal characters.</en>
        /// </summary>
        public string FontName {
            get {
                return _fontName;
            }
            set {
                _fontName = value;
                ClearFont();
            }
        }
        /// <summary>
        /// <ja>CJK文字を表示するためのフォント名です。</ja>
        /// <en>Gets or sets the font name for CJK characters.</en>
        /// </summary>
        public string CJKFontName {
            get {
                return _cjkFontName;
            }
            set {
                _cjkFontName = value;
                ClearFont();
            }
        }
        /// <summary>
        /// <ja>フォントサイズです。</ja>
        /// <en>Gets or sets the font size.</en>
        /// </summary>
        public float FontSize {
            get {
                return _fontSize;
            }
            set {
                _fontSize = value;
                ClearFont();
            }
        }
        /// <summary>
        /// <ja>trueにセットすると、フォントとOSでサポートされていれば、ClearTypeを使用して文字が描画されます。</ja>
        /// <en>If this property is true, the characters are drew by the ClearType when the font and the OS supports it.</en>
        /// </summary>
        public bool UseClearType {
            get {
                return _useClearType;
            }
            set {
                _useClearType = value;
            }
        }

        /// <summary>
        /// <ja>falseにするとエスケープシーケンスでボールドフォントが指定されていても通常フォントで描画します</ja>
        /// <en>If this property is false, bold fonts are replaced by normal fonts even if the escape sequence indicates bold.</en>
        /// </summary>
        public bool EnableBoldStyle {
            get {
                return _enableBoldStyle;
            }
            set {
                _enableBoldStyle = value;
            }
        }

        /// <summary>
        /// </summary>
        public bool ForceBoldStyle {
            get {
                return _forceBoldStyle;
            }
            set {
                _forceBoldStyle = value;
            }
        }

        /// <summary>
        /// <ja>文字色です。</ja>
        /// <en>Gets or sets the color of characters.</en>
        /// </summary>
        public Color ForeColor {
            get {
                return _forecolor;
            }
            set {
                _forecolor = value;
                ClearBrush();
            }
        }
        /// <summary>
        /// <ja>JScriptではColor構造体が使用できないので、ForeColorプロパティを設定するかわりにこのメソッドを使ってください。</ja>
        /// <en>Because JScript cannot handle the Color structure, please use this method instead of the ForeColor property.</en>
        /// </summary>
        public void SetForeColor(object value) {
            _forecolor = (Color)value;
            ClearBrush();
        }
        /// <summary>
        /// <ja>背景色です。</ja>
        /// <en>Gets or sets the background color.</en>
        /// </summary>
        public Color BackColor {
            get {
                return _bgcolor;
            }
            set {
                _bgcolor = value;
                ClearBrush();
            }
        }
        /// <summary>
        /// <ja>JScriptでは構造体が使用できないので、BackColorプロパティを設定するかわりにこのメソッドを使ってください。</ja>
        /// <en>Because JScript cannot handle the Color structure, please use this method instead of the BackColor property.</en>
        /// </summary>
        public void SetBackColor(object value) {
            _bgcolor = (Color)value;
            ClearBrush();
        }

        /// <summary>
        /// <ja>背景色を色テーブルから選択するときに、暗い色にするかどうかを設定または取得します。</ja>
        /// <en>Gets or sets whether the color is darken when the background color is chosen from the color table.</en>
        /// </summary>
        public bool DarkenEsColorForBackground {
            get {
                return _darkenEsColorForBackground;
            }
            set {
                _darkenEsColorForBackground = value;
            }
        }

        /// <summary>
        /// <ja>背景画像のファイル名です。</ja>
        /// <en>Gets or set the file name of the background image.</en>
        /// </summary>
        public string BackgroundImageFileName {
            get {
                return _backgroundImageFileName;
            }
            set {
                _backgroundImageFileName = value;
                _backgroundImage = null;
            }
        }
        /// <summary>
        /// <ja>背景画像の位置です。</ja>
        /// <en>Gets or sets the position of the background image.</en>
        /// </summary>
        public ImageStyle ImageStyle {
            get {
                return _imageStyle;
            }
            set {
                _imageStyle = value;
            }
        }
#if !MACRODOC

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public EscapesequenceColorSet ESColorSet {
            get {
                return _esColorSet;
            }
            set {
                Debug.Assert(value != null);
                _esColorSet = value;
            }
        }
#endif
        /// <summary>
        /// <ja>コピーして作成します。</ja>
        /// <en>Initializes with another instance.</en>
        /// </summary>
        public RenderProfile(RenderProfile src) {
            _fontName = src._fontName;
            _cjkFontName = src._cjkFontName;
            _fontSize = src._fontSize;
            _lineSpacing = src._lineSpacing;
            _useClearType = src._useClearType;
            _enableBoldStyle = src._enableBoldStyle;
            _forceBoldStyle = src._forceBoldStyle;

            _forecolor = src._forecolor;
            _bgcolor = src._bgcolor;
#if !MACRODOC
            _esColorSet = (EscapesequenceColorSet)src._esColorSet.Clone();
#endif
            _bgbrush = _brush = null;

            _backgroundImageFileName = src._backgroundImageFileName;
            _imageLoadIsAttempted = false;
            _imageStyle = src.ImageStyle;
        }

        public RenderProfile() {
            //do nothing. properties must be filled
            _backgroundImageFileName = "";
#if !MACRODOC
            _esColorSet = new EscapesequenceColorSet();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exclude/>
        public object Clone() {
            return new RenderProfile(this);
        }

        private void ClearFont() {
            _fonts.Dispose();
        }

        private void ClearBrush() {
            if (_brush != null)
                _brush.Dispose();
            if (_bgbrush != null)
                _bgbrush.Dispose();
            _brush = null;
            _bgbrush = null;
        }

#if !MACRODOC
        private void CreateSingleFonts() {
            if (_fonts.Latin.Single != null) {
                return;
            }

            Graphics g = Graphics.FromHwnd(Win32.GetDesktopWindow());
            IntPtr hdc = g.GetHdc();

            Font font = RuntimeUtil.CreateFont(_fontName, _fontSize);
            FontHandle normalFont = new FontHandle(font, _useClearType);
            FontHandle boldFont = new FontHandle(new Font(font, font.Style | FontStyle.Bold), _useClearType);
            _fonts.Latin.Single = new FontSet(
                normalFont: normalFont,
                boldFont: boldFont
            );

            Font cjkFont = RuntimeUtil.CreateFont(_cjkFontName, _fontSize);
            FontHandle cjkNormalFont = new FontHandle(cjkFont, _useClearType);
            FontHandle cjkBoldFont = new FontHandle(new Font(cjkFont, cjkFont.Style | FontStyle.Bold), _useClearType);
            _fonts.CJK.Single = new FontSet(
                normalFont: cjkNormalFont,
                boldFont: cjkBoldFont
            );

            Win32.SelectObject(hdc, normalFont.HFONT);
            Win32.SIZE charSize1, charSize2;
            Win32.GetTextExtentPoint32(hdc, "A", 1, out charSize1);
            Win32.GetTextExtentPoint32(hdc, "AAA", 3, out charSize2);

            _pitch = new SizeF((charSize2.width - charSize1.width) / 2, charSize1.height);
            _charGap = (charSize1.width - _pitch.Width) / 2;

            g.ReleaseHdc(hdc);
            g.Dispose();
        }

        private FontSet CreateDoubleFonts(FontSet baseFontSet) {
            Graphics g = Graphics.FromHwnd(Win32.GetDesktopWindow());
            IntPtr hdc = g.GetHdc();

            Win32.SelectObject(hdc, baseFontSet.NormalFont.HFONT);
            Win32.TEXTMETRICW textMetric = new Win32.TEXTMETRICW();
            Win32.GetTextMetrics(hdc, out textMetric);

            int charWidth = textMetric.tmAveCharWidth * 2;

            Font font = baseFontSet.NormalFont.Font;
            FontHandle normalFont = new FontHandle(font, _useClearType, charWidth);
            FontHandle boldFont = new FontHandle(new Font(font, font.Style | FontStyle.Bold), _useClearType, charWidth);

            g.ReleaseHdc(hdc);
            g.Dispose();

            return new FontSet(
                normalFont: normalFont,
                boldFont: boldFont
            );
        }

        private FontSet CreateQuadFonts(FontSet baseFontSet) {
            Font font = new Font(baseFontSet.NormalFont.Font.FontFamily, baseFontSet.NormalFont.Font.Size * 2);
            FontHandle normalFont = new FontHandle(font, _useClearType);
            FontHandle boldFont = new FontHandle(new Font(font, font.Style | FontStyle.Bold), _useClearType);

            return new FontSet(
                normalFont: normalFont,
                boldFont: boldFont
            );
        }

        private void CreateBrushes() {
            _brush = new SolidBrush(_forecolor);
            _bgbrush = new SolidBrush(_bgcolor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public Brush Brush {
            get {
                if (_brush == null)
                    CreateBrushes();
                return _brush;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public Brush BgBrush {
            get {
                if (_bgbrush == null)
                    CreateBrushes();
                return _bgbrush;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public SizeF Pitch {
            get {
                CreateSingleFonts();
                return _pitch;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public int LineSpacing {
            get {
                return _lineSpacing;
            }
            set {
                _lineSpacing = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public Font DefaultFont {
            get {
                CreateSingleFonts();
                return _fonts.Latin.Single.NormalFont.Font;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exclude/>
        public Image GetImage() {
            try {
                if (!_imageLoadIsAttempted) {
                    _imageLoadIsAttempted = true;
                    _backgroundImage = null;
                    if (_backgroundImageFileName != null && _backgroundImageFileName.Length > 0) {
                        try {
                            _backgroundImage = Image.FromFile(_backgroundImageFileName);
                        }
                        catch (Exception) {
                            MessageBox.Show("Can't find the background image!", "Poderosa error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                        }
                    }
                }

                return _backgroundImage;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public float CharGap {
            get {
                CreateSingleFonts();
                return _charGap;
            }
        }

        public Color GetBackColor(ColorSpec backColorSpec) {
            switch (backColorSpec.ColorType) {
                case ColorType.Custom24bit:
                    return backColorSpec.Color;

                case ColorType.Custom8bit:
                    return GetESBackColor(backColorSpec.ColorCode);

                case ColorType.Default:
                default:
                    return this.BackColor;
            }
        }

        private Color GetESBackColor(int colorCode) {
            ESColor c = this.ESColorSet[colorCode];
            if (this.DarkenEsColorForBackground && !c.IsExactColor) {
                return DrawUtil.DarkColor(c.Color);
            }
            return c.Color;
        }

        public Color GetForeColor(ColorSpec foreColorSpec) {
            switch (foreColorSpec.ColorType) {
                case ColorType.Custom24bit:
                    return foreColorSpec.Color;

                case ColorType.Custom8bit:
                    return GetESForeColor(foreColorSpec.ColorCode);

                case ColorType.Default:
                default:
                    return this.ForeColor;
            }
        }

        private Color GetESForeColor(int colorCode) {
            return this.ESColorSet[colorCode].Color;
        }

        public Font GetIMECompositionFont() {
            return CalcFontInternal(true, false, LineRenderingType.Normal).Font;
        }

        internal IntPtr CalcHFONT(GAttr attr, LineRenderingType renderingType) {
            return CalcFontInternal(
                    attr.Has(GAttrFlags.UseCjkFont),
                    DetermineBold(attr),
                    renderingType
            ).HFONT;
        }

        private FontHandle CalcFontInternal(bool useCjkFont, bool bold, LineRenderingType renderingType) {
            CreateSingleFonts();

            FontSizes fss = (useCjkFont) ? _fonts.CJK : _fonts.Latin;

            FontSet fs;

            if ((renderingType & LineRenderingType.DoubleWidth) == 0) {
                fs = fss.Single;
            }
            else if ((renderingType & LineRenderingType.DoubleHeight) == 0) {
                fs = fss.Double;
                if (fs == null) {
                    fs = CreateDoubleFonts(fss.Single);
                    fss.Double = fs;
                }
            }
            else {
                fs = fss.Quad;
                if (fs == null) {
                    fs = CreateQuadFonts(fss.Single);
                    fss.Quad = fs;
                }
            }

            if (bold) {
                return fs.BoldFont;
            }
            return fs.NormalFont;
        }

        internal bool DetermineBold(GAttr attr) {
            return _forceBoldStyle || (_enableBoldStyle && attr.Has(GAttrFlags.Bold));
        }

        internal void DetermineColors(GAttr attr, GColor24 color24, Caret caret, Color baseBackColor, out Color backColor, out Color foreColor) {
            if (_brush == null) {
                CreateBrushes();
            }

            bool inverted = attr.Has(GAttrFlags.Inverted) ^ attr.Has(GAttrFlags.Selected);

            bool blinkStatus = caret.IsActiveTick;

            if (attr.Has(GAttrFlags.Cursor) && (!caret.Blink || blinkStatus)) {
                if (inverted) {
                    // paint as normal
                    backColor = DetermineActualBackColor(caret.Color.IsEmpty ? DetermineNormalBackColor(attr, color24) : caret.Color, baseBackColor);
                    foreColor = DetermineNormalForeColor(attr, color24);
                }
                else {
                    // paint as inverted
                    backColor = DetermineActualBackColor(caret.Color.IsEmpty ? DetermineNormalForeColor(attr, color24) : caret.Color, baseBackColor);
                    foreColor = DetermineNormalBackColor(attr, color24);
                }
            }
            else {
                bool isHidden = attr.Has(GAttrFlags.Hidden) || (attr.Has(GAttrFlags.Blink) && !blinkStatus);

                if (inverted) {
                    backColor = DetermineActualBackColor(DetermineNormalForeColor(attr, color24), baseBackColor);
                    foreColor = isHidden ? Color.Transparent : DetermineNormalBackColor(attr, color24);
                }
                else {
                    backColor = DetermineActualBackColor(DetermineNormalBackColor(attr, color24), baseBackColor);
                    foreColor = isHidden ? Color.Transparent : DetermineNormalForeColor(attr, color24);
                }
            }
        }

        private Color DetermineNormalBackColor(GAttr attr, GColor24 color24) {
            if (attr.Has(GAttrFlags.Use8bitBackColor)) {
                return GetESBackColor(attr.BackColor);
            }
            if (attr.Has(GAttrFlags.Use24bitBackColor)) {
                return color24.BackColor;
            }
            return this.BackColor;
        }

        private Color DetermineNormalForeColor(GAttr attr, GColor24 color24) {
            if (attr.Has(GAttrFlags.Use8bitForeColor)) {
                return GetESForeColor(attr.ForeColor);
            }
            if (attr.Has(GAttrFlags.Use24bitForeColor)) {
                return color24.ForeColor;
            }
            return this.ForeColor;
        }

        private Color DetermineActualBackColor(Color candidate, Color baseBackColor) {
            return (candidate.ToArgb() == baseBackColor.ToArgb()) ? Color.Transparent : candidate;
        }

#endif
    }

#if !MACRODOC

    /// <summary>
    /// Color palette element
    /// </summary>
    public struct ESColor {
        private readonly Color _color;
        private readonly bool _isExactColor;

        /// <summary>
        /// Gets a color value.
        /// </summary>
        public Color Color {
            get {
                return _color;
            }
        }

        /// <summary>
        /// Gets if this color must be displayed exectly.
        /// </summary>
        public bool IsExactColor {
            get {
                return _isExactColor;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="color">Color</param>
        /// <param name="isExactColor">True if this color imust be displayed exectly.</param>
        public ESColor(Color color, bool isExactColor) {
            _color = color;
            _isExactColor = isExactColor;
        }

        public override bool Equals(object obj) {
            if (obj == null)
                return false;
            if (!(obj is ESColor))
                return false;

            ESColor c = (ESColor)obj;
            return (_color.ToArgb() == c._color.ToArgb()) && (_isExactColor == c._isExactColor);
        }

        public override int GetHashCode() {
            return _color.GetHashCode();
        }

        public static bool operator ==(ESColor c1, ESColor c2) {
            return (c1._color.ToArgb() == c2._color.ToArgb()) && (c1._isExactColor == c2._isExactColor);
        }

        public static bool operator !=(ESColor c1, ESColor c2) {
            return !(c1 == c2);
        }
    }

    /// <summary>
    /// Color palette changeable by escape sequence.
    /// </summary>
    /// <exclude/>
    public class EscapesequenceColorSet : ICloneable {

        private bool _isDefault;
        private readonly ESColor[] _colors = new ESColor[256];

        public EscapesequenceColorSet() {
            ResetToDefault();
        }

        private EscapesequenceColorSet(EscapesequenceColorSet a) {
            _isDefault = a._isDefault;
            for (int i = 0; i < _colors.Length; i++) {
                _colors[i] = a._colors[i];
            }
        }

        public object Clone() {
            return new EscapesequenceColorSet(this);
        }

        public bool IsDefault {
            get {
                return _isDefault;
            }
        }

        public ESColor this[int index] {
            get {
                return _colors[index];
            }
            set {
                _colors[index] = value;
                if (_isDefault)
                    _isDefault = GetDefaultColor(index) == value;
            }
        }

        public void ResetToDefault() {
            for (int i = 0; i < _colors.Length; i++) {
                _colors[i] = GetDefaultColor(i);
            }
            _isDefault = true;
        }

        public string Format() {
            if (_isDefault)
                return String.Empty;
            StringBuilder bld = new StringBuilder();
            for (int i = 0; i < _colors.Length; i++) {
                if (i > 0)
                    bld.Append(',');

                ESColor color = _colors[i];
                if (color.IsExactColor)
                    bld.Append('!');
                bld.Append(color.Color.Name);
                // Note: Color.Name returns hex'ed ARGB value if it was not a named color.
            }
            return bld.ToString();
        }

        public void Load(string value) {
            if (!_isDefault)
                ResetToDefault();

            if (value == null)
                return; // use default colors

            string[] cols = value.Split(',');
            int overrides = 0;
            for (int i = 0; i < cols.Length; i++) {
                string w = cols[i].Trim();

                bool isExactColor;
                if (w.Length > 0 && w[0] == '!') {
                    isExactColor = true;
                    w = w.Substring(1);
                }
                else {
                    isExactColor = false;
                }

                if (w.Length == 0)
                    continue;   // use default color

                Color color = ParseUtil.ParseColor(w, Color.Empty);
                if (!color.IsEmpty) {
                    _colors[i] = new ESColor(color, isExactColor);
                    overrides++;
                }
            }
            if (overrides > 0)
                _isDefault = false;
        }

        public static EscapesequenceColorSet Parse(string s) {
            EscapesequenceColorSet r = new EscapesequenceColorSet();
            r.Load(s);
            return r;
        }

        public static ESColor GetDefaultColor(int index) {
            return new ESColor(GetDefaultColorValue(index), false);
        }

        private static Color GetDefaultColorValue(int index) {
            int r, g, b;
            switch (index) {
                case 0:
                    return Color.Black;
                case 1:
                    return Color.Red;
                case 2:
                    return Color.Green;
                case 3:
                    return Color.Yellow;
                case 4:
                    return Color.Blue;
                case 5:
                    return Color.Magenta;
                case 6:
                    return Color.Cyan;
                case 7:
                    return Color.White;
                case 8:
                    return Color.FromArgb(64, 64, 64);
                case 9:
                    return Color.FromArgb(255, 64, 64);
                case 10:
                    return Color.FromArgb(64, 255, 64);
                case 11:
                    return Color.FromArgb(255, 255, 64);
                case 12:
                    return Color.FromArgb(64, 64, 255);
                case 13:
                    return Color.FromArgb(255, 64, 255);
                case 14:
                    return Color.FromArgb(64, 255, 255);
                case 15:
                    return Color.White;
                default:
                    if (index >= 16 && index <= 231) {
                        r = (index - 16) / 36 % 6;
                        g = (index - 16) / 6 % 6;
                        b = (index - 16) % 6;
                        return Color.FromArgb((r == 0) ? 0 : r * 40 + 55, (g == 0) ? 0 : g * 40 + 55, (b == 0) ? 0 : b * 40 + 55);
                    }
                    else if (index >= 232 && index <= 255) {
                        r = (index - 232) * 10 + 8;
                        return Color.FromArgb(r, r, r);
                    }
                    else {
                        return Color.Empty;
                    }
            }
        }
    }
#endif
}
