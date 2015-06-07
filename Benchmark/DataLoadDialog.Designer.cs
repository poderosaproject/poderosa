namespace Poderosa.Benchmark {
    partial class DataLoadDialog {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this.textDataFile = new System.Windows.Forms.TextBox();
            this.buttonBrowseDataFile = new System.Windows.Forms.Button();
            this.textRepeat = new System.Windows.Forms.TextBox();
            this.labelRepeat = new System.Windows.Forms.Label();
            this.labelRepeatTimes = new System.Windows.Forms.Label();
            this.labelDataFile = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textDataFile
            // 
            this.textDataFile.Location = new System.Drawing.Point(12, 27);
            this.textDataFile.Name = "textDataFile";
            this.textDataFile.Size = new System.Drawing.Size(315, 19);
            this.textDataFile.TabIndex = 1;
            // 
            // buttonBrowseDataFile
            // 
            this.buttonBrowseDataFile.Location = new System.Drawing.Point(333, 25);
            this.buttonBrowseDataFile.Name = "buttonBrowseDataFile";
            this.buttonBrowseDataFile.Size = new System.Drawing.Size(28, 23);
            this.buttonBrowseDataFile.TabIndex = 2;
            this.buttonBrowseDataFile.Text = "...";
            this.buttonBrowseDataFile.UseVisualStyleBackColor = true;
            this.buttonBrowseDataFile.Click += new System.EventHandler(this.buttonBrowseDataFile_Click);
            // 
            // textRepeat
            // 
            this.textRepeat.Location = new System.Drawing.Point(67, 69);
            this.textRepeat.Name = "textRepeat";
            this.textRepeat.Size = new System.Drawing.Size(63, 19);
            this.textRepeat.TabIndex = 4;
            // 
            // labelRepeat
            // 
            this.labelRepeat.AutoSize = true;
            this.labelRepeat.Location = new System.Drawing.Point(10, 72);
            this.labelRepeat.Name = "labelRepeat";
            this.labelRepeat.Size = new System.Drawing.Size(41, 12);
            this.labelRepeat.TabIndex = 3;
            this.labelRepeat.Text = "Repeat";
            // 
            // labelRepeatTimes
            // 
            this.labelRepeatTimes.AutoSize = true;
            this.labelRepeatTimes.Location = new System.Drawing.Point(136, 72);
            this.labelRepeatTimes.Name = "labelRepeatTimes";
            this.labelRepeatTimes.Size = new System.Drawing.Size(33, 12);
            this.labelRepeatTimes.TabIndex = 5;
            this.labelRepeatTimes.Text = "times";
            // 
            // labelDataFile
            // 
            this.labelDataFile.AutoSize = true;
            this.labelDataFile.Location = new System.Drawing.Point(10, 9);
            this.labelDataFile.Name = "labelDataFile";
            this.labelDataFile.Size = new System.Drawing.Size(88, 12);
            this.labelDataFile.TabIndex = 0;
            this.labelDataFile.Text = "Data file to load";
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.Location = new System.Drawing.Point(205, 112);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 23);
            this.buttonOK.TabIndex = 6;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(286, 112);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // DataLoadDialog
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(373, 147);
            this.ControlBox = false;
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.labelRepeatTimes);
            this.Controls.Add(this.labelDataFile);
            this.Controls.Add(this.labelRepeat);
            this.Controls.Add(this.textRepeat);
            this.Controls.Add(this.buttonBrowseDataFile);
            this.Controls.Add(this.textDataFile);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "DataLoadDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Benchmark Data";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textDataFile;
        private System.Windows.Forms.Button buttonBrowseDataFile;
        private System.Windows.Forms.TextBox textRepeat;
        private System.Windows.Forms.Label labelRepeat;
        private System.Windows.Forms.Label labelRepeatTimes;
        private System.Windows.Forms.Label labelDataFile;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
    }
}