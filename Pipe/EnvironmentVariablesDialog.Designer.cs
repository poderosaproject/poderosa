/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: EnvironmentVariablesDialog.Designer.cs,v 1.1 2011/08/04 15:28:58 kzmi Exp $
 */
namespace Poderosa.Pipe {
    partial class EnvironmentVariablesDialog {
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
            this._listViewVariables = new System.Windows.Forms.ListView();
            this._columnName = new System.Windows.Forms.ColumnHeader();
            this._columnValue = new System.Windows.Forms.ColumnHeader();
            this._buttonOK = new System.Windows.Forms.Button();
            this._buttonCancel = new System.Windows.Forms.Button();
            this._buttonAdd = new System.Windows.Forms.Button();
            this._buttonEdit = new System.Windows.Forms.Button();
            this._buttonDelete = new System.Windows.Forms.Button();
            this._labelDescription = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _listViewVariables
            // 
            this._listViewVariables.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._listViewVariables.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._columnName,
            this._columnValue});
            this._listViewVariables.FullRowSelect = true;
            this._listViewVariables.GridLines = true;
            this._listViewVariables.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._listViewVariables.HideSelection = false;
            this._listViewVariables.Location = new System.Drawing.Point(6, 41);
            this._listViewVariables.MultiSelect = false;
            this._listViewVariables.Name = "_listViewVariables";
            this._listViewVariables.ShowItemToolTips = true;
            this._listViewVariables.Size = new System.Drawing.Size(390, 137);
            this._listViewVariables.TabIndex = 1;
            this._listViewVariables.UseCompatibleStateImageBehavior = false;
            this._listViewVariables.View = System.Windows.Forms.View.Details;
            // 
            // _columnName
            // 
            this._columnName.Text = "";
            this._columnName.Width = 100;
            // 
            // _columnValue
            // 
            this._columnValue.Text = "";
            this._columnValue.Width = 260;
            // 
            // _buttonOK
            // 
            this._buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonOK.Location = new System.Drawing.Point(234, 217);
            this._buttonOK.Name = "_buttonOK";
            this._buttonOK.Size = new System.Drawing.Size(75, 23);
            this._buttonOK.TabIndex = 5;
            this._buttonOK.UseVisualStyleBackColor = true;
            this._buttonOK.Click += new System.EventHandler(this._buttonOK_Click);
            // 
            // _buttonCancel
            // 
            this._buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonCancel.Location = new System.Drawing.Point(315, 217);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(75, 23);
            this._buttonCancel.TabIndex = 6;
            this._buttonCancel.UseVisualStyleBackColor = true;
            this._buttonCancel.Click += new System.EventHandler(this._buttonCancel_Click);
            // 
            // _buttonAdd
            // 
            this._buttonAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonAdd.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonAdd.Location = new System.Drawing.Point(159, 184);
            this._buttonAdd.Name = "_buttonAdd";
            this._buttonAdd.Size = new System.Drawing.Size(75, 23);
            this._buttonAdd.TabIndex = 2;
            this._buttonAdd.UseVisualStyleBackColor = true;
            this._buttonAdd.Click += new System.EventHandler(this._buttonAdd_Click);
            // 
            // _buttonEdit
            // 
            this._buttonEdit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonEdit.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonEdit.Location = new System.Drawing.Point(240, 184);
            this._buttonEdit.Name = "_buttonEdit";
            this._buttonEdit.Size = new System.Drawing.Size(75, 23);
            this._buttonEdit.TabIndex = 3;
            this._buttonEdit.UseVisualStyleBackColor = true;
            this._buttonEdit.Click += new System.EventHandler(this._buttonEdit_Click);
            // 
            // _buttonDelete
            // 
            this._buttonDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._buttonDelete.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._buttonDelete.Location = new System.Drawing.Point(321, 184);
            this._buttonDelete.Name = "_buttonDelete";
            this._buttonDelete.Size = new System.Drawing.Size(75, 23);
            this._buttonDelete.TabIndex = 4;
            this._buttonDelete.UseVisualStyleBackColor = true;
            this._buttonDelete.Click += new System.EventHandler(this._buttonDelete_Click);
            // 
            // _labelDescription
            // 
            this._labelDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this._labelDescription.Location = new System.Drawing.Point(6, 6);
            this._labelDescription.Name = "_labelDescription";
            this._labelDescription.Size = new System.Drawing.Size(390, 32);
            this._labelDescription.TabIndex = 0;
            // 
            // EnvironmentVariablesDialog
            // 
            this.AcceptButton = this._buttonOK;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this._buttonCancel;
            this.ClientSize = new System.Drawing.Size(402, 246);
            this.Controls.Add(this._labelDescription);
            this.Controls.Add(this._buttonCancel);
            this.Controls.Add(this._buttonDelete);
            this.Controls.Add(this._buttonEdit);
            this.Controls.Add(this._buttonAdd);
            this.Controls.Add(this._buttonOK);
            this.Controls.Add(this._listViewVariables);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(270, 200);
            this.Name = "EnvironmentVariablesDialog";
            this.Padding = new System.Windows.Forms.Padding(3);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "EditEnvironmentVariables";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView _listViewVariables;
        private System.Windows.Forms.Button _buttonOK;
        private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.Button _buttonAdd;
        private System.Windows.Forms.Button _buttonEdit;
        private System.Windows.Forms.Button _buttonDelete;
        private System.Windows.Forms.Label _labelDescription;
        private System.Windows.Forms.ColumnHeader _columnName;
        private System.Windows.Forms.ColumnHeader _columnValue;
    }
}