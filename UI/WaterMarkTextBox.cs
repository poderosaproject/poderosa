using System.Drawing;
using System.Windows.Forms;

namespace Poderosa.UI {
    /// <summary>
    /// ウォーターマークテキスト付きTextBoxクラス
    /// </summary>
    public class WaterMarkTextBox : TextBox {
        /// <summary>
        /// ウォーターマークテキスト
        /// </summary>
        private string _waterMarkText = string.Empty;
        /// <summary>
        /// ウォーターマークテキストの色
        /// </summary>
        private Color _waterMarkColor = Color.Gray;
        /// <summary>
        /// フォーカス中もウォーターマークテキストを表示
        /// </summary>
        private bool _waterMarkAlsoFocus = false;

        /// <summary>
        /// Windowsメッセージを取得
        /// </summary>
        protected override void WndProc(ref Message m) {
            bool isCallAlready = false;

            if (m.Msg == 0x000F) { // WM_PAINT
                base.WndProc(ref m);
                isCallAlready = true;
                DrawWaterMarkText();
            } else if (m.Msg == 0x0008 && this.Multiline) { // WM_KILLFOCUS
                DrawWaterMarkText();
            }

            if (false == isCallAlready) base.WndProc(ref m);
        }

        /// <summary>
        /// ウォーターマークテキストを表示
        /// </summary>
        private void DrawWaterMarkText() {
            // WaterMarkAlsoFocusがfalseの場合はフォーカス中にテキストを表示しない
            if ((this.WaterMarkAlsoFocus == false) && (this.Focused == true)) return;

            if ((string.IsNullOrEmpty(this.Text)) && (string.IsNullOrEmpty(this.WaterMarkText) == false) && (this.IsHandleCreated) && (this.Visible)) {
                using (Graphics g = Graphics.FromHwnd(this.Handle)) {
                    StringFormat sf = new StringFormat();
                    float textHeight = g.MeasureString(this.WaterMarkText, this.Font, this.Width, sf).Height;
                    float textY = ((float)this.Height - textHeight) / (float)2.0;
                    RectangleF bounds = new RectangleF(0, textY, (float)this.Width, (float)this.Height - (textY * (float)2.0));
                    g.DrawString(this.WaterMarkText, this.Font, new SolidBrush(this.WaterMarkColor), bounds, sf);
                }
            }
        }

        /// <summary>
        /// ウォーターマークテキスト
        /// </summary>
        public string WaterMarkText {
            get { return _waterMarkText; }
            set { _waterMarkText = value; }
        }

        /// <summary>
        /// ウォーターマークテキストの色
        /// </summary>
        public Color WaterMarkColor {
            get { return _waterMarkColor; }
            set { _waterMarkColor = value; }
        }

        /// <summary>
        /// フォーカス中もウォーターマークテキストを表示
        /// </summary>
        public bool WaterMarkAlsoFocus {
            get { return _waterMarkAlsoFocus; }
            set { _waterMarkAlsoFocus = value; }
        }
    }
}
