/*
* Copyright (c) 2005 Poderosa Project, All Rights Reserved.
* $Id: AboutBox.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace Poderosa.PortForwarding {
    /// <summary>
    /// AboutBox の概要の説明です。
    /// </summary>
    public class AboutBox : System.Windows.Forms.Form {
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Label _content;
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.Container components = null;

        public AboutBox() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();

            //
            // TODO: InitializeComponent 呼び出しの後に、コンストラクタ コードを追加してください。
            //
            _content.Text = "Poderosa SSH Portforwarding Gateway\r\n" + Env.VERSION_STRING + "\r\n\r\nCopyright(c) Poderosa Project.";
            this._okButton.Text = Env.Strings.GetString("Common.OK");
            this.Text = Env.Strings.GetString("Form.AboutBox.Text");
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

        #region Windows フォーム デザイナで生成されたコード
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this._okButton = new System.Windows.Forms.Button();
            this._content = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._okButton.Location = new System.Drawing.Point(104, 72);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 0;
            this._okButton.FlatStyle = FlatStyle.System;
            // 
            // _content
            // 
            this._content.Location = new System.Drawing.Point(8, 8);
            this._content.Name = "_content";
            this._content.Size = new System.Drawing.Size(264, 56);
            this._content.TabIndex = 1;
            // 
            // AboutBox
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.CancelButton = this._okButton;
            this.ClientSize = new System.Drawing.Size(282, 95);
            this.Controls.Add(this._content);
            this.Controls.Add(this._okButton);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutBox";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }
        #endregion
    }
}
