/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: ChoosePatternDialog.cs,v 1.1 2011/12/25 03:12:09 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Poderosa.Benchmark {

    internal partial class ChoosePatternDialog : Form {

        private XTermBenchmarkPattern _pattern;

        public XTermBenchmarkPattern Pattern {
            get {
                return _pattern;
            }
        }

        public ChoosePatternDialog() {
            InitializeComponent();

            this.comboBoxPattern.Items.Clear();
            foreach (XTermBenchmarkPattern pattern in Enum.GetValues(typeof(XTermBenchmarkPattern))) {
                this.comboBoxPattern.Items.Add(pattern);
            }
            this.comboBoxPattern.SelectedIndex = 0;
        }

        private void buttonOK_Click(object sender, EventArgs e) {
            this._pattern = (XTermBenchmarkPattern)this.comboBoxPattern.SelectedItem;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}