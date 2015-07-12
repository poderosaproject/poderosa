namespace Poderosa.ConnectProfile {
    partial class ConnectProfileForm {
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem6 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem7 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem8 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem9 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem10 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem11 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem12 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem13 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem14 = new System.Windows.Forms.ListViewItem("");
            System.Windows.Forms.ListViewItem listViewItem15 = new System.Windows.Forms.ListViewItem("");
            this._profileListView = new System.Windows.Forms.ListView();
            this._hostNameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._userNameColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._autoLoginColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._protocolColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._portColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._suSwitchColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._charCodeColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._newLineColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._execCommandColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._terminalBGColorColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._descriptionColumn = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this._filterLabel = new System.Windows.Forms.Label();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._addProfileButton = new System.Windows.Forms.Button();
            this._delProfileButton = new System.Windows.Forms.Button();
            this._csvExportButton = new System.Windows.Forms.Button();
            this._csvImportButton = new System.Windows.Forms.Button();
            this._profileCountLabel = new System.Windows.Forms.Label();
            this._editProfileButton = new System.Windows.Forms.Button();
            this._checkAllOffButton = new System.Windows.Forms.Button();
            this._filterTimer = new System.Windows.Forms.Timer(this.components);
            this._selectedProfileCountLabel = new System.Windows.Forms.Label();
            this._displaySelectedOnlyCheck = new System.Windows.Forms.CheckBox();
            this._saveCSVFileDialog = new System.Windows.Forms.SaveFileDialog();
            this._openCSVFileDialog = new System.Windows.Forms.OpenFileDialog();
            this._hintLabel = new System.Windows.Forms.Label();
            this._copyButton = new System.Windows.Forms.Button();
            this._filterTextBox = new Poderosa.UI.WaterMarkTextBox();
            this.SuspendLayout();
            // 
            // _profileListView
            // 
            this._profileListView.AllowColumnReorder = true;
            this._profileListView.CheckBoxes = true;
            this._profileListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._hostNameColumn,
            this._userNameColumn,
            this._autoLoginColumn,
            this._protocolColumn,
            this._portColumn,
            this._suSwitchColumn,
            this._charCodeColumn,
            this._newLineColumn,
            this._execCommandColumn,
            this._terminalBGColorColumn,
            this._descriptionColumn});
            this._profileListView.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._profileListView.FullRowSelect = true;
            this._profileListView.GridLines = true;
            this._profileListView.HideSelection = false;
            listViewItem1.StateImageIndex = 0;
            listViewItem2.StateImageIndex = 0;
            listViewItem3.StateImageIndex = 0;
            listViewItem4.StateImageIndex = 0;
            listViewItem5.StateImageIndex = 0;
            listViewItem6.StateImageIndex = 0;
            listViewItem7.StateImageIndex = 0;
            listViewItem8.StateImageIndex = 0;
            listViewItem9.StateImageIndex = 0;
            listViewItem10.StateImageIndex = 0;
            listViewItem11.StateImageIndex = 0;
            listViewItem12.StateImageIndex = 0;
            listViewItem13.StateImageIndex = 0;
            listViewItem14.StateImageIndex = 0;
            listViewItem15.StateImageIndex = 0;
            this._profileListView.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5,
            listViewItem6,
            listViewItem7,
            listViewItem8,
            listViewItem9,
            listViewItem10,
            listViewItem11,
            listViewItem12,
            listViewItem13,
            listViewItem14,
            listViewItem15});
            this._profileListView.Location = new System.Drawing.Point(0, 79);
            this._profileListView.MultiSelect = false;
            this._profileListView.Name = "_profileListView";
            this._profileListView.ShowItemToolTips = true;
            this._profileListView.Size = new System.Drawing.Size(780, 271);
            this._profileListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
            this._profileListView.TabIndex = 1;
            this._profileListView.UseCompatibleStateImageBehavior = false;
            this._profileListView.View = System.Windows.Forms.View.Details;
            this._profileListView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this._profileListView_ColumnClick);
            this._profileListView.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this._profileListView_ItemCheck);
            this._profileListView.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this._profileListView_ItemChecked);
            this._profileListView.MouseDown += new System.Windows.Forms.MouseEventHandler(this._profileListView_MouseDown);
            // 
            // _hostNameColumn
            // 
            this._hostNameColumn.Text = "_hostNameColumn";
            this._hostNameColumn.Width = 105;
            // 
            // _userNameColumn
            // 
            this._userNameColumn.Text = "_userNameColumn";
            this._userNameColumn.Width = 105;
            // 
            // _autoLoginColumn
            // 
            this._autoLoginColumn.Text = "_autoLoginColumn";
            this._autoLoginColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this._autoLoginColumn.Width = 103;
            // 
            // _protocolColumn
            // 
            this._protocolColumn.Text = "_protocolColumn";
            this._protocolColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this._protocolColumn.Width = 95;
            // 
            // _portColumn
            // 
            this._portColumn.Text = "_portColumn";
            this._portColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._portColumn.Width = 74;
            // 
            // _suSwitchColumn
            // 
            this._suSwitchColumn.Text = "_suSwitchColumn";
            this._suSwitchColumn.Width = 100;
            // 
            // _charCodeColumn
            // 
            this._charCodeColumn.Text = "_charCodeColumn";
            this._charCodeColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this._charCodeColumn.Width = 102;
            // 
            // _newLineColumn
            // 
            this._newLineColumn.Text = "_newLineColumn";
            this._newLineColumn.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this._newLineColumn.Width = 95;
            // 
            // _execCommandColumn
            // 
            this._execCommandColumn.Text = "_execCommandColumn";
            this._execCommandColumn.Width = 128;
            // 
            // _terminalBGColorColumn
            // 
            this._terminalBGColorColumn.Text = "_terminalBGColorColumn";
            this._terminalBGColorColumn.Width = 138;
            // 
            // _descriptionColumn
            // 
            this._descriptionColumn.Text = "_descriptionColumn";
            this._descriptionColumn.Width = 95;
            // 
            // _filterLabel
            // 
            this._filterLabel.AutoSize = true;
            this._filterLabel.Location = new System.Drawing.Point(9, 15);
            this._filterLabel.Name = "_filterLabel";
            this._filterLabel.Size = new System.Drawing.Size(60, 12);
            this._filterLabel.TabIndex = 1;
            this._filterLabel.Text = "_filterLabel";
            // 
            // _okButton
            // 
            this._okButton.Location = new System.Drawing.Point(241, 10);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 2;
            this._okButton.Text = "_okButton";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(322, 10);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 3;
            this._cancelButton.Text = "_cancelButton";
            this._cancelButton.UseVisualStyleBackColor = true;
            this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
            // 
            // _addProfileButton
            // 
            this._addProfileButton.Location = new System.Drawing.Point(534, 10);
            this._addProfileButton.Name = "_addProfileButton";
            this._addProfileButton.Size = new System.Drawing.Size(75, 23);
            this._addProfileButton.TabIndex = 5;
            this._addProfileButton.Text = "_addProfileButton";
            this._addProfileButton.UseVisualStyleBackColor = true;
            this._addProfileButton.Click += new System.EventHandler(this._addProfileButton_Click);
            // 
            // _delProfileButton
            // 
            this._delProfileButton.Location = new System.Drawing.Point(696, 10);
            this._delProfileButton.Name = "_delProfileButton";
            this._delProfileButton.Size = new System.Drawing.Size(75, 23);
            this._delProfileButton.TabIndex = 7;
            this._delProfileButton.Text = "_delProfileButton";
            this._delProfileButton.UseVisualStyleBackColor = true;
            this._delProfileButton.Click += new System.EventHandler(this._delProfileButton_Click);
            // 
            // _csvExportButton
            // 
            this._csvExportButton.Location = new System.Drawing.Point(615, 39);
            this._csvExportButton.Name = "_csvExportButton";
            this._csvExportButton.Size = new System.Drawing.Size(75, 23);
            this._csvExportButton.TabIndex = 9;
            this._csvExportButton.Text = "_csvExportButton";
            this._csvExportButton.UseVisualStyleBackColor = true;
            this._csvExportButton.Click += new System.EventHandler(this._csvExportButton_Click);
            // 
            // _csvImportButton
            // 
            this._csvImportButton.Location = new System.Drawing.Point(696, 39);
            this._csvImportButton.Name = "_csvImportButton";
            this._csvImportButton.Size = new System.Drawing.Size(75, 23);
            this._csvImportButton.TabIndex = 10;
            this._csvImportButton.Text = "_csvImportButton";
            this._csvImportButton.UseVisualStyleBackColor = true;
            this._csvImportButton.Click += new System.EventHandler(this._csvImportButton_Click);
            // 
            // _profileCountLabel
            // 
            this._profileCountLabel.AutoSize = true;
            this._profileCountLabel.Location = new System.Drawing.Point(9, 44);
            this._profileCountLabel.Name = "_profileCountLabel";
            this._profileCountLabel.Size = new System.Drawing.Size(98, 12);
            this._profileCountLabel.TabIndex = 10;
            this._profileCountLabel.Text = "_profileCountLabel";
            // 
            // _editProfileButton
            // 
            this._editProfileButton.Location = new System.Drawing.Point(615, 10);
            this._editProfileButton.Name = "_editProfileButton";
            this._editProfileButton.Size = new System.Drawing.Size(75, 23);
            this._editProfileButton.TabIndex = 6;
            this._editProfileButton.Text = "_editProfileButton";
            this._editProfileButton.UseVisualStyleBackColor = true;
            this._editProfileButton.Click += new System.EventHandler(this._editProfileButton_Click);
            // 
            // _checkAllOffButton
            // 
            this._checkAllOffButton.Location = new System.Drawing.Point(413, 10);
            this._checkAllOffButton.Name = "_checkAllOffButton";
            this._checkAllOffButton.Size = new System.Drawing.Size(106, 23);
            this._checkAllOffButton.TabIndex = 4;
            this._checkAllOffButton.Text = "_checkAllOffButton";
            this._checkAllOffButton.UseVisualStyleBackColor = true;
            this._checkAllOffButton.Click += new System.EventHandler(this._checkAllOffButton_Click);
            // 
            // _filterTimer
            // 
            this._filterTimer.Tick += new System.EventHandler(this._filterTime_Tick);
            // 
            // _selectedProfileCountLabel
            // 
            this._selectedProfileCountLabel.AutoSize = true;
            this._selectedProfileCountLabel.Location = new System.Drawing.Point(130, 44);
            this._selectedProfileCountLabel.Name = "_selectedProfileCountLabel";
            this._selectedProfileCountLabel.Size = new System.Drawing.Size(142, 12);
            this._selectedProfileCountLabel.TabIndex = 16;
            this._selectedProfileCountLabel.Text = "_selectedProfileCountLabel";
            // 
            // _displaySelectedOnlyCheck
            // 
            this._displaySelectedOnlyCheck.AutoSize = true;
            this._displaySelectedOnlyCheck.Location = new System.Drawing.Point(241, 43);
            this._displaySelectedOnlyCheck.Name = "_displaySelectedOnlyCheck";
            this._displaySelectedOnlyCheck.Size = new System.Drawing.Size(163, 16);
            this._displaySelectedOnlyCheck.TabIndex = 11;
            this._displaySelectedOnlyCheck.Text = "_displaySelectedOnlyCheck";
            this._displaySelectedOnlyCheck.UseVisualStyleBackColor = true;
            this._displaySelectedOnlyCheck.CheckedChanged += new System.EventHandler(this._displaySelectedOnlyCheck_CheckedChanged);
            // 
            // _saveCSVFileDialog
            // 
            this._saveCSVFileDialog.DefaultExt = "csv";
            // 
            // _openCSVFileDialog
            // 
            this._openCSVFileDialog.DefaultExt = "csv";
            // 
            // _hintLabel
            // 
            this._hintLabel.AutoSize = true;
            this._hintLabel.Location = new System.Drawing.Point(9, 62);
            this._hintLabel.Name = "_hintLabel";
            this._hintLabel.Size = new System.Drawing.Size(55, 12);
            this._hintLabel.TabIndex = 18;
            this._hintLabel.Text = "_hintLabel";
            // 
            // _copyButton
            // 
            this._copyButton.Location = new System.Drawing.Point(534, 39);
            this._copyButton.Name = "_copyButton";
            this._copyButton.Size = new System.Drawing.Size(75, 23);
            this._copyButton.TabIndex = 8;
            this._copyButton.Text = "_copyButton";
            this._copyButton.UseVisualStyleBackColor = true;
            this._copyButton.Click += new System.EventHandler(this._copyButton_Click);
            // 
            // _filterTextBox
            // 
            this._filterTextBox.Location = new System.Drawing.Point(52, 12);
            this._filterTextBox.Name = "_filterTextBox";
            this._filterTextBox.Size = new System.Drawing.Size(183, 19);
            this._filterTextBox.TabIndex = 0;
            this._filterTextBox.WaterMarkAlsoFocus = true;
            this._filterTextBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._filterTextBox.WaterMarkText = "";
            this._filterTextBox.TextChanged += new System.EventHandler(this._filterTextBox_TextChanged);
            this._filterTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this._filterTextBox_KeyDown);
            // 
            // ConnectProfileForm
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(780, 350);
            this.Controls.Add(this._copyButton);
            this.Controls.Add(this._hintLabel);
            this.Controls.Add(this._filterTextBox);
            this.Controls.Add(this._displaySelectedOnlyCheck);
            this.Controls.Add(this._selectedProfileCountLabel);
            this.Controls.Add(this._csvExportButton);
            this.Controls.Add(this._csvImportButton);
            this.Controls.Add(this._checkAllOffButton);
            this.Controls.Add(this._addProfileButton);
            this.Controls.Add(this._editProfileButton);
            this.Controls.Add(this._delProfileButton);
            this.Controls.Add(this._profileCountLabel);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._filterLabel);
            this.Controls.Add(this._profileListView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConnectProfileForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ConnectProfileForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ConnectProfileForm_FormClosing);
            this.Load += new System.EventHandler(this.ConnectProfileForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListView _profileListView;
        private System.Windows.Forms.Label _filterLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.ColumnHeader _hostNameColumn;
        private System.Windows.Forms.ColumnHeader _userNameColumn;
        private System.Windows.Forms.ColumnHeader _autoLoginColumn;
        private System.Windows.Forms.ColumnHeader _protocolColumn;
        private System.Windows.Forms.ColumnHeader _portColumn;
        private System.Windows.Forms.ColumnHeader _suSwitchColumn;
        private System.Windows.Forms.ColumnHeader _charCodeColumn;
        private System.Windows.Forms.ColumnHeader _newLineColumn;
        private System.Windows.Forms.ColumnHeader _execCommandColumn;
        private System.Windows.Forms.Button _addProfileButton;
        private System.Windows.Forms.Button _delProfileButton;
        private System.Windows.Forms.Button _csvExportButton;
        private System.Windows.Forms.Button _csvImportButton;
        private System.Windows.Forms.ColumnHeader _terminalBGColorColumn;
        private System.Windows.Forms.ColumnHeader _descriptionColumn;
        private System.Windows.Forms.Label _profileCountLabel;
        private System.Windows.Forms.Button _editProfileButton;
        private System.Windows.Forms.Button _checkAllOffButton;
        private System.Windows.Forms.Timer _filterTimer;
        private System.Windows.Forms.Label _selectedProfileCountLabel;
        private System.Windows.Forms.CheckBox _displaySelectedOnlyCheck;
        private Poderosa.UI.WaterMarkTextBox _filterTextBox;
        private System.Windows.Forms.SaveFileDialog _saveCSVFileDialog;
        private System.Windows.Forms.OpenFileDialog _openCSVFileDialog;
        private System.Windows.Forms.Label _hintLabel;
        private System.Windows.Forms.Button _copyButton;
    }

}