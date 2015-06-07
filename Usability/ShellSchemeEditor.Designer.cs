using System;
using System.Windows.Forms;

namespace Poderosa.Usability {
    partial class ShellSchemeEditor {
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
            this._schemeCollectionGroup = new System.Windows.Forms.GroupBox();
            this._deleteSchemeButton = new System.Windows.Forms.Button();
            this._newSchemeButton = new System.Windows.Forms.Button();
            this._schemeComboBox = new System.Windows.Forms.ComboBox();
            this._schemeLabel = new System.Windows.Forms.Label();
            this._currentSchemeGroup = new System.Windows.Forms.GroupBox();
            this._promptBox = new System.Windows.Forms.TextBox();
            this._promptLabel = new System.Windows.Forms.Label();
            this._deleteCommandsButton = new System.Windows.Forms.Button();
            this._alphabeticalSort = new System.Windows.Forms.CheckBox();
            this._commandListBox = new System.Windows.Forms.ListBox();
            this._itemLabel = new System.Windows.Forms.Label();
            this._deleteCharBox = new System.Windows.Forms.ComboBox();
            this._deleteCharLabel = new System.Windows.Forms.Label();
            this._nameBox = new System.Windows.Forms.TextBox();
            this._nameLabel = new System.Windows.Forms.Label();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._schemeCollectionGroup.SuspendLayout();
            this._currentSchemeGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _schemeCollectionGroup
            // 
            this._schemeCollectionGroup.Controls.Add(this._deleteSchemeButton);
            this._schemeCollectionGroup.Controls.Add(this._newSchemeButton);
            this._schemeCollectionGroup.Controls.Add(this._schemeComboBox);
            this._schemeCollectionGroup.Controls.Add(this._schemeLabel);
            this._schemeCollectionGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._schemeCollectionGroup.Location = new System.Drawing.Point(12, 12);
            this._schemeCollectionGroup.Name = "_schemeCollectionGroup";
            this._schemeCollectionGroup.Size = new System.Drawing.Size(333, 74);
            this._schemeCollectionGroup.TabIndex = 0;
            this._schemeCollectionGroup.TabStop = false;
            // 
            // _deleteSchemeButton
            // 
            this._deleteSchemeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deleteSchemeButton.Location = new System.Drawing.Point(237, 17);
            this._deleteSchemeButton.Name = "_deleteSchemeButton";
            this._deleteSchemeButton.Size = new System.Drawing.Size(75, 23);
            this._deleteSchemeButton.TabIndex = 3;
            this._deleteSchemeButton.UseVisualStyleBackColor = true;
            this._deleteSchemeButton.Click += new System.EventHandler(this.OnDeleteScheme);
            // 
            // _newSchemeButton
            // 
            this._newSchemeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._newSchemeButton.Location = new System.Drawing.Point(237, 45);
            this._newSchemeButton.Name = "_newSchemeButton";
            this._newSchemeButton.Size = new System.Drawing.Size(75, 23);
            this._newSchemeButton.TabIndex = 2;
            this._newSchemeButton.UseVisualStyleBackColor = true;
            this._newSchemeButton.Click += new System.EventHandler(this.OnNewScheme);
            // 
            // _schemeComboBox
            // 
            this._schemeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._schemeComboBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._schemeComboBox.FormattingEnabled = true;
            this._schemeComboBox.Location = new System.Drawing.Point(94, 19);
            this._schemeComboBox.Name = "_schemeComboBox";
            this._schemeComboBox.Size = new System.Drawing.Size(137, 20);
            this._schemeComboBox.TabIndex = 1;
            this._schemeComboBox.SelectedIndexChanged += new System.EventHandler(this.OnSchemeChanged);
            // 
            // _schemeLabel
            // 
            this._schemeLabel.AutoSize = true;
            this._schemeLabel.Location = new System.Drawing.Point(6, 22);
            this._schemeLabel.Name = "_schemeLabel";
            this._schemeLabel.Size = new System.Drawing.Size(0, 12);
            this._schemeLabel.TabIndex = 0;
            this._schemeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _currentSchemeGroup
            // 
            this._currentSchemeGroup.Controls.Add(this._promptBox);
            this._currentSchemeGroup.Controls.Add(this._promptLabel);
            this._currentSchemeGroup.Controls.Add(this._deleteCommandsButton);
            this._currentSchemeGroup.Controls.Add(this._alphabeticalSort);
            this._currentSchemeGroup.Controls.Add(this._commandListBox);
            this._currentSchemeGroup.Controls.Add(this._itemLabel);
            this._currentSchemeGroup.Controls.Add(this._deleteCharBox);
            this._currentSchemeGroup.Controls.Add(this._deleteCharLabel);
            this._currentSchemeGroup.Controls.Add(this._nameBox);
            this._currentSchemeGroup.Controls.Add(this._nameLabel);
            this._currentSchemeGroup.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._currentSchemeGroup.Location = new System.Drawing.Point(12, 104);
            this._currentSchemeGroup.Name = "_currentSchemeGroup";
            this._currentSchemeGroup.Size = new System.Drawing.Size(333, 278);
            this._currentSchemeGroup.TabIndex = 1;
            this._currentSchemeGroup.TabStop = false;
            // 
            // _promptBox
            // 
            this._promptBox.Location = new System.Drawing.Point(167, 43);
            this._promptBox.Name = "_promptBox";
            this._promptBox.Size = new System.Drawing.Size(158, 19);
            this._promptBox.TabIndex = 3;
            this._promptBox.Validating += new System.ComponentModel.CancelEventHandler(this.OnValidatePrompt);
            // 
            // _promptLabel
            // 
            this._promptLabel.AutoSize = true;
            this._promptLabel.Location = new System.Drawing.Point(9, 47);
            this._promptLabel.Name = "_promptLabel";
            this._promptLabel.Size = new System.Drawing.Size(0, 12);
            this._promptLabel.TabIndex = 2;
            this._promptLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _deleteCommandsButton
            // 
            this._deleteCommandsButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deleteCommandsButton.Location = new System.Drawing.Point(226, 249);
            this._deleteCommandsButton.Name = "_deleteCommandsButton";
            this._deleteCommandsButton.Size = new System.Drawing.Size(99, 23);
            this._deleteCommandsButton.TabIndex = 9;
            this._deleteCommandsButton.UseVisualStyleBackColor = true;
            this._deleteCommandsButton.Click += new System.EventHandler(this.OnDeleteCommands);
            // 
            // _alphabeticalSort
            // 
            this._alphabeticalSort.AutoSize = true;
            this._alphabeticalSort.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._alphabeticalSort.Location = new System.Drawing.Point(13, 248);
            this._alphabeticalSort.Name = "_alphabeticalSort";
            this._alphabeticalSort.Size = new System.Drawing.Size(60, 17);
            this._alphabeticalSort.TabIndex = 7;
            this._alphabeticalSort.Text = "SORT";
            this._alphabeticalSort.UseVisualStyleBackColor = true;
            this._alphabeticalSort.CheckedChanged += new System.EventHandler(this.OnSortChange);
            // 
            // _commandListBox
            // 
            this._commandListBox.FormattingEnabled = true;
            this._commandListBox.ItemHeight = 12;
            this._commandListBox.Location = new System.Drawing.Point(13, 130);
            this._commandListBox.Name = "_commandListBox";
            this._commandListBox.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
            this._commandListBox.Size = new System.Drawing.Size(312, 112);
            this._commandListBox.TabIndex = 8;
            this._commandListBox.SelectedIndexChanged += new System.EventHandler(this.OnSelectedIndicesChanged);
            // 
            // _itemLabel
            // 
            this._itemLabel.AutoSize = true;
            this._itemLabel.Location = new System.Drawing.Point(11, 114);
            this._itemLabel.Name = "_itemLabel";
            this._itemLabel.Size = new System.Drawing.Size(0, 12);
            this._itemLabel.TabIndex = 6;
            // 
            // _deleteCharBox
            // 
            this._deleteCharBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._deleteCharBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._deleteCharBox.FormattingEnabled = true;
            this._deleteCharBox.Location = new System.Drawing.Point(167, 69);
            this._deleteCharBox.Name = "_deleteCharBox";
            this._deleteCharBox.Size = new System.Drawing.Size(158, 20);
            this._deleteCharBox.TabIndex = 5;
            this._deleteCharBox.SelectedIndexChanged += new System.EventHandler(this.OnDeleteCharBoxChanged);
            // 
            // _deleteCharLabel
            // 
            this._deleteCharLabel.AutoSize = true;
            this._deleteCharLabel.Location = new System.Drawing.Point(9, 72);
            this._deleteCharLabel.Name = "_deleteCharLabel";
            this._deleteCharLabel.Size = new System.Drawing.Size(0, 12);
            this._deleteCharLabel.TabIndex = 4;
            // 
            // _nameBox
            // 
            this._nameBox.Location = new System.Drawing.Point(167, 18);
            this._nameBox.Name = "_nameBox";
            this._nameBox.Size = new System.Drawing.Size(158, 19);
            this._nameBox.TabIndex = 1;
            this._nameBox.Validating += new System.ComponentModel.CancelEventHandler(this.OnValidateSchemeName);
            // 
            // _nameLabel
            // 
            this._nameLabel.AutoSize = true;
            this._nameLabel.Location = new System.Drawing.Point(9, 22);
            this._nameLabel.Name = "_nameLabel";
            this._nameLabel.Size = new System.Drawing.Size(0, 12);
            this._nameLabel.TabIndex = 0;
            this._nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(168, 388);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 2;
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this.OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(262, 388);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 3;
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // ShellSchemeEditor
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(352, 418);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._currentSchemeGroup);
            this.Controls.Add(this._schemeCollectionGroup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ShellSchemeEditor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this._schemeCollectionGroup.ResumeLayout(false);
            this._schemeCollectionGroup.PerformLayout();
            this._currentSchemeGroup.ResumeLayout(false);
            this._currentSchemeGroup.PerformLayout();
            this.ResumeLayout(false);

        }


        #endregion

        private System.Windows.Forms.GroupBox _schemeCollectionGroup;
        private System.Windows.Forms.Button _deleteSchemeButton;
        private System.Windows.Forms.Button _newSchemeButton;
        private System.Windows.Forms.ComboBox _schemeComboBox;
        private System.Windows.Forms.Label _schemeLabel;
        private System.Windows.Forms.GroupBox _currentSchemeGroup;
        private System.Windows.Forms.ComboBox _deleteCharBox;
        private System.Windows.Forms.Label _deleteCharLabel;
        private System.Windows.Forms.TextBox _nameBox;
        private System.Windows.Forms.Label _nameLabel;
        private System.Windows.Forms.TextBox _promptBox;
        private System.Windows.Forms.Label _promptLabel;
        private System.Windows.Forms.Button _deleteCommandsButton;
        private System.Windows.Forms.CheckBox _alphabeticalSort;
        private System.Windows.Forms.ListBox _commandListBox;
        private System.Windows.Forms.Label _itemLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
    }
}