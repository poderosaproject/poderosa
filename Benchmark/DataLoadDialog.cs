/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: DataLoadDialog.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Poderosa.Benchmark {
        
    internal partial class DataLoadDialog : Form {

        private string _dataFileToLoad = null;
        private int _repeat = 1;

        public string DataFileToLoad {
            get {
                return _dataFileToLoad;
            }
        }

        public int Repeat {
            get {
                return _repeat;
            }
        }

        public DataLoadDialog() {
            InitializeComponent();
        }

        private void buttonBrowseDataFile_Click(object sender, EventArgs e) {
            using (OpenFileDialog dialog = new OpenFileDialog()) {
                dialog.FileName = this.textDataFile.Text;
                dialog.RestoreDirectory = true;
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;
                DialogResult result = dialog.ShowDialog(this);
                if (result == DialogResult.OK)
                    this.textDataFile.Text = dialog.FileName;
            }
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this._dataFileToLoad = this.textDataFile.Text;
            if (this._dataFileToLoad.Length == 0) {
                MessageBox.Show(this, "No data file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Int32.TryParse(this.textRepeat.Text, out this._repeat)) {
                MessageBox.Show(this, "No repeat count", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this._repeat <= 0) {
                MessageBox.Show(this, "Invalid repeat count", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}