namespace Poderosa.Forms
{
    partial class PasteConfirmDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.L_Message = new System.Windows.Forms.Label();
            this.B_Cancel = new System.Windows.Forms.Button();
            this.B_OK = new System.Windows.Forms.Button();
            this.RTB_ClipBoard = new System.Windows.Forms.RichTextBox();
            this.B_FindPrev = new System.Windows.Forms.Button();
            this.B_FindNext = new System.Windows.Forms.Button();
            this.CB_Confirmed = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.L_Info_Line = new System.Windows.Forms.Label();
            this.L_Info_NewLine = new System.Windows.Forms.Label();
            this.L_Info_HighlightKeyword = new System.Windows.Forms.Label();
            this.L_Info_Session = new System.Windows.Forms.Label();
            this.L_SubMessage = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // L_Message
            // 
            this.L_Message.AutoSize = true;
            this.L_Message.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.L_Message.Location = new System.Drawing.Point(57, 28);
            this.L_Message.Name = "L_Message";
            this.L_Message.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.L_Message.Size = new System.Drawing.Size(0, 18);
            this.L_Message.TabIndex = 6;
            // 
            // B_Cancel
            // 
            this.B_Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.B_Cancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.B_Cancel.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.B_Cancel.Location = new System.Drawing.Point(363, 69);
            this.B_Cancel.Name = "B_Cancel";
            this.B_Cancel.Size = new System.Drawing.Size(96, 29);
            this.B_Cancel.TabIndex = 1;
            this.B_Cancel.Click += new System.EventHandler(this.B_Cancel_Click);
            // 
            // B_OK
            // 
            this.B_OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.B_OK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.B_OK.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.B_OK.Location = new System.Drawing.Point(261, 69);
            this.B_OK.Name = "B_OK";
            this.B_OK.Size = new System.Drawing.Size(96, 29);
            this.B_OK.TabIndex = 0;
            this.B_OK.Click += new System.EventHandler(this.B_OK_Click);
            // 
            // RTB_ClipBoard
            // 
            this.RTB_ClipBoard.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.RTB_ClipBoard.DetectUrls = false;
            this.RTB_ClipBoard.Font = new System.Drawing.Font("ＭＳ ゴシック", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.RTB_ClipBoard.HideSelection = false;
            this.RTB_ClipBoard.Location = new System.Drawing.Point(0, 151);
            this.RTB_ClipBoard.Name = "RTB_ClipBoard";
            this.RTB_ClipBoard.ReadOnly = true;
            this.RTB_ClipBoard.Size = new System.Drawing.Size(464, 167);
            this.RTB_ClipBoard.TabIndex = 5;
            this.RTB_ClipBoard.Text = "1\n2\n3\n4\n5\n6\n7\n8\n9\n10";
            this.RTB_ClipBoard.WordWrap = false;
            // 
            // B_FindPrev
            // 
            this.B_FindPrev.Enabled = false;
            this.B_FindPrev.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.B_FindPrev.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.B_FindPrev.Location = new System.Drawing.Point(127, 69);
            this.B_FindPrev.Name = "B_FindPrev";
            this.B_FindPrev.Size = new System.Drawing.Size(63, 29);
            this.B_FindPrev.TabIndex = 4;
            this.B_FindPrev.Click += new System.EventHandler(this.B_MatchBack_Click);
            // 
            // B_FindNext
            // 
            this.B_FindNext.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.B_FindNext.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.B_FindNext.Location = new System.Drawing.Point(192, 69);
            this.B_FindNext.Name = "B_FindNext";
            this.B_FindNext.Size = new System.Drawing.Size(63, 29);
            this.B_FindNext.TabIndex = 3;
            this.B_FindNext.Click += new System.EventHandler(this.B_MatchNext_Click);
            // 
            // CB_Confirmed
            // 
            this.CB_Confirmed.AutoSize = true;
            this.CB_Confirmed.Enabled = false;
            this.CB_Confirmed.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.CB_Confirmed.ForeColor = System.Drawing.SystemColors.ControlText;
            this.CB_Confirmed.Location = new System.Drawing.Point(8, 74);
            this.CB_Confirmed.Name = "CB_Confirmed";
            this.CB_Confirmed.Size = new System.Drawing.Size(15, 14);
            this.CB_Confirmed.TabIndex = 2;
            this.CB_Confirmed.UseVisualStyleBackColor = true;
            this.CB_Confirmed.CheckedChanged += new System.EventHandler(this.CB_Confirmed_CheckedChanged);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.L_Info_Line);
            this.panel1.Controls.Add(this.L_Info_NewLine);
            this.panel1.Controls.Add(this.L_Info_HighlightKeyword);
            this.panel1.Controls.Add(this.L_Info_Session);
            this.panel1.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.panel1.Location = new System.Drawing.Point(0, 106);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(464, 45);
            this.panel1.TabIndex = 9;
            // 
            // L_Info_Line
            // 
            this.L_Info_Line.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.L_Info_Line.Location = new System.Drawing.Point(355, 21);
            this.L_Info_Line.Name = "L_Info_Line";
            this.L_Info_Line.Size = new System.Drawing.Size(99, 19);
            this.L_Info_Line.TabIndex = 3;
            // 
            // L_Info_NewLine
            // 
            this.L_Info_NewLine.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.L_Info_NewLine.Location = new System.Drawing.Point(253, 21);
            this.L_Info_NewLine.Name = "L_Info_NewLine";
            this.L_Info_NewLine.Size = new System.Drawing.Size(99, 19);
            this.L_Info_NewLine.TabIndex = 2;
            // 
            // L_Info_HighlightKeyword
            // 
            this.L_Info_HighlightKeyword.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.L_Info_HighlightKeyword.Location = new System.Drawing.Point(3, 21);
            this.L_Info_HighlightKeyword.Name = "L_Info_HighlightKeyword";
            this.L_Info_HighlightKeyword.Size = new System.Drawing.Size(234, 19);
            this.L_Info_HighlightKeyword.TabIndex = 1;
            // 
            // L_Info_Session
            // 
            this.L_Info_Session.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.L_Info_Session.Location = new System.Drawing.Point(3, 2);
            this.L_Info_Session.Name = "L_Info_Session";
            this.L_Info_Session.Size = new System.Drawing.Size(454, 21);
            this.L_Info_Session.TabIndex = 0;
            // 
            // L_SubMessage
            // 
            this.L_SubMessage.AutoSize = true;
            this.L_SubMessage.Font = new System.Drawing.Font("メイリオ", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.L_SubMessage.ForeColor = System.Drawing.Color.Red;
            this.L_SubMessage.Location = new System.Drawing.Point(57, 48);
            this.L_SubMessage.Name = "L_SubMessage";
            this.L_SubMessage.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.L_SubMessage.Size = new System.Drawing.Size(0, 18);
            this.L_SubMessage.TabIndex = 10;
            // 
            // PasteConfirmDialog
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this.B_Cancel;
            this.ClientSize = new System.Drawing.Size(464, 318);
            this.Controls.Add(this.L_SubMessage);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.CB_Confirmed);
            this.Controls.Add(this.B_FindNext);
            this.Controls.Add(this.B_FindPrev);
            this.Controls.Add(this.B_Cancel);
            this.Controls.Add(this.B_OK);
            this.Controls.Add(this.RTB_ClipBoard);
            this.Controls.Add(this.L_Message);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PasteConfirmDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Poderosa";
            this.Load += new System.EventHandler(this.PasteConfirmDialog_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label L_Message;
        private System.Windows.Forms.Button B_Cancel;
        private System.Windows.Forms.Button B_OK;
        private System.Windows.Forms.RichTextBox RTB_ClipBoard;
        private System.Windows.Forms.Button B_FindPrev;
        private System.Windows.Forms.Button B_FindNext;
        private System.Windows.Forms.CheckBox CB_Confirmed;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label L_Info_Session;
        private System.Windows.Forms.Label L_Info_HighlightKeyword;
        private System.Windows.Forms.Label L_Info_Line;
        private System.Windows.Forms.Label L_Info_NewLine;
        private System.Windows.Forms.Label L_SubMessage;
    }
}