// Copyright 2023 The Poderosa Project.
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

namespace Poderosa.Sessions {
    partial class ShortcutFileOptionsDialog {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this._savePasswordCheckBox = new System.Windows.Forms.CheckBox();
            this._bottomPanel = new System.Windows.Forms.Panel();
            this._cancelButton = new System.Windows.Forms.Button();
            this._saveButton = new System.Windows.Forms.Button();
            this._encryptPasswordRadioButton = new System.Windows.Forms.RadioButton();
            this._plaintextPasswordRadioButton = new System.Windows.Forms.RadioButton();
            this._passwordOptionsPanel = new System.Windows.Forms.Panel();
            this._bottomPanel.SuspendLayout();
            this._passwordOptionsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // _savePasswordCheckBox
            // 
            this._savePasswordCheckBox.AutoSize = true;
            this._savePasswordCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._savePasswordCheckBox.Location = new System.Drawing.Point(30, 12);
            this._savePasswordCheckBox.Name = "_savePasswordCheckBox";
            this._savePasswordCheckBox.Size = new System.Drawing.Size(356, 17);
            this._savePasswordCheckBox.TabIndex = 0;
            this._savePasswordCheckBox.UseVisualStyleBackColor = true;
            this._savePasswordCheckBox.CheckedChanged += new System.EventHandler(this._savePasswordCheckBox_CheckedChanged);
            // 
            // _bottomPanel
            // 
            this._bottomPanel.Controls.Add(this._cancelButton);
            this._bottomPanel.Controls.Add(this._saveButton);
            this._bottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._bottomPanel.Location = new System.Drawing.Point(0, 95);
            this._bottomPanel.Margin = new System.Windows.Forms.Padding(0);
            this._bottomPanel.Name = "_bottomPanel";
            this._bottomPanel.Size = new System.Drawing.Size(424, 38);
            this._bottomPanel.TabIndex = 3;
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(343, 9);
            this._cancelButton.Margin = new System.Windows.Forms.Padding(6);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 1;
            this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
            // 
            // _saveButton
            // 
            this._saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._saveButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._saveButton.Location = new System.Drawing.Point(243, 9);
            this._saveButton.Name = "_saveButton";
            this._saveButton.Size = new System.Drawing.Size(75, 23);
            this._saveButton.TabIndex = 0;
            this._saveButton.UseVisualStyleBackColor = true;
            this._saveButton.Click += new System.EventHandler(this._saveButton_Click);
            // 
            // _encryptPasswordRadioButton
            // 
            this._encryptPasswordRadioButton.AutoSize = true;
            this._encryptPasswordRadioButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._encryptPasswordRadioButton.Location = new System.Drawing.Point(0, 0);
            this._encryptPasswordRadioButton.Name = "_encryptPasswordRadioButton";
            this._encryptPasswordRadioButton.Size = new System.Drawing.Size(138, 17);
            this._encryptPasswordRadioButton.TabIndex = 1;
            this._encryptPasswordRadioButton.TabStop = true;
            this._encryptPasswordRadioButton.UseVisualStyleBackColor = true;
            this._encryptPasswordRadioButton.CheckedChanged += new System.EventHandler(this._encryptPasswordRadioButton_CheckedChanged);
            // 
            // _plaintextPasswordRadioButton
            // 
            this._plaintextPasswordRadioButton.AutoSize = true;
            this._plaintextPasswordRadioButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._plaintextPasswordRadioButton.Location = new System.Drawing.Point(0, 24);
            this._plaintextPasswordRadioButton.Name = "_plaintextPasswordRadioButton";
            this._plaintextPasswordRadioButton.Size = new System.Drawing.Size(183, 17);
            this._plaintextPasswordRadioButton.TabIndex = 2;
            this._plaintextPasswordRadioButton.TabStop = true;
            this._plaintextPasswordRadioButton.UseVisualStyleBackColor = true;
            this._plaintextPasswordRadioButton.CheckedChanged += new System.EventHandler(this._plaintextPasswordRadioButton_CheckedChanged);
            // 
            // _passwordOptionsPanel
            // 
            this._passwordOptionsPanel.AutoSize = true;
            this._passwordOptionsPanel.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this._passwordOptionsPanel.Controls.Add(this._plaintextPasswordRadioButton);
            this._passwordOptionsPanel.Controls.Add(this._encryptPasswordRadioButton);
            this._passwordOptionsPanel.Location = new System.Drawing.Point(50, 36);
            this._passwordOptionsPanel.Name = "_passwordOptionsPanel";
            this._passwordOptionsPanel.Size = new System.Drawing.Size(186, 44);
            this._passwordOptionsPanel.TabIndex = 4;
            // 
            // ShortcutFileOptionsDialog
            // 
            this.AcceptButton = this._saveButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(424, 133);
            this.Controls.Add(this._passwordOptionsPanel);
            this.Controls.Add(this._bottomPanel);
            this.Controls.Add(this._savePasswordCheckBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShortcutFileOptionsDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Load += new System.EventHandler(this.SaveShortcutFileDialog_Load);
            this._bottomPanel.ResumeLayout(false);
            this._passwordOptionsPanel.ResumeLayout(false);
            this._passwordOptionsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox _savePasswordCheckBox;
        private System.Windows.Forms.Panel _bottomPanel;
        protected System.Windows.Forms.Button _cancelButton;
        protected System.Windows.Forms.Button _saveButton;
        private System.Windows.Forms.RadioButton _encryptPasswordRadioButton;
        private System.Windows.Forms.RadioButton _plaintextPasswordRadioButton;
        private System.Windows.Forms.Panel _passwordOptionsPanel;

    }
}