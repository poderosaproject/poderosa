/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: AgentKeyListDialog.Designer.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
namespace Poderosa.Forms {
    partial class AgentKeyListDialog {
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
            this._list = new System.Windows.Forms.ListView();
            this._filenameHeader = new System.Windows.Forms.ColumnHeader();
            this._statusHeader = new System.Windows.Forms.ColumnHeader();
            this._commentHeader = new System.Windows.Forms.ColumnHeader();
            this._addButton = new System.Windows.Forms.Button();
            this._removeButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _list
            // 
            this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._filenameHeader,
            this._statusHeader,
            this._commentHeader});
            this._list.FullRowSelect = true;
            this._list.GridLines = true;
            this._list.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._list.Location = new System.Drawing.Point(-2, -2);
            this._list.MultiSelect = false;
            this._list.Name = "_list";
            this._list.Size = new System.Drawing.Size(436, 251);
            this._list.TabIndex = 0;
            this._list.UseCompatibleStateImageBehavior = false;
            this._list.View = System.Windows.Forms.View.Details;
            this._list.SelectedIndexChanged += new System.EventHandler(OnSelectedIndexChanged);
            this._list.DoubleClick += new System.EventHandler(OnListDoubleClick);
            // 
            // _filenameHeader
            // 
            this._filenameHeader.Width = 170;
            // 
            // _statusHeader
            // 
            this._statusHeader.Width = 44;
            // 
            // _commentHeader
            // 
            this._commentHeader.Width = 188;
            // 
            // _addButton
            // 
            this._addButton.Location = new System.Drawing.Point(441, 13);
            this._addButton.Name = "_addButton";
            this._addButton.Size = new System.Drawing.Size(75, 23);
            this._addButton.TabIndex = 1;
            this._addButton.UseVisualStyleBackColor = true;
            this._addButton.Click += new System.EventHandler(OnAddButton);
            // 
            // _removeButton
            // 
            this._removeButton.Enabled = false;
            this._removeButton.Location = new System.Drawing.Point(441, 43);
            this._removeButton.Name = "_removeButton";
            this._removeButton.Size = new System.Drawing.Size(75, 23);
            this._removeButton.TabIndex = 2;
            this._removeButton.UseVisualStyleBackColor = true;
            this._removeButton.Click += new System.EventHandler(OnRemoveButton);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(441, 215);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 3;
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(441, 186);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 4;
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(OnOK);
            // 
            // AgentKeyListDialog
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(525, 250);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._removeButton);
            this.Controls.Add(this._addButton);
            this.Controls.Add(this._list);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AgentKeyListDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView _list;
        private System.Windows.Forms.ColumnHeader _filenameHeader;
        private System.Windows.Forms.ColumnHeader _statusHeader;
        private System.Windows.Forms.ColumnHeader _commentHeader;
        private System.Windows.Forms.Button _addButton;
        private System.Windows.Forms.Button _removeButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _okButton;
    }
}