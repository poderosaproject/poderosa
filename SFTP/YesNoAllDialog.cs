/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: YesNoAllDialog.cs,v 1.1 2011/11/30 22:53:08 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Poderosa.SFTP {

    /// <summary>
    /// Confitmation dialog with Yes/No/Yes to all/Cancel buttons.
    /// </summary>
    public partial class YesNoAllDialog : Form {

        /// <summary>
        /// DialogResult value for "Yes to all"
        /// </summary>
        public const DialogResult YesToAll = DialogResult.Retry;

        private readonly string _text;
        private readonly string _caption;

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        public YesNoAllDialog()
            : this(null, null) {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="caption">Caption</param>
        public YesNoAllDialog(string text, string caption) {
            InitializeComponent();
            this._text = text;
            this._caption = caption;

            StringResource res = SFTPPlugin.Instance.StringResource;
            this.buttonYes.Text = res.GetString("YesNoAllDialog.buttonYes");
            this.buttonNo.Text = res.GetString("YesNoAllDialog.buttonNo");
            this.buttonYesToAll.Text = res.GetString("YesNoAllDialog.buttonYesToAll");
            this.buttonCancel.Text = res.GetString("YesNoAllDialog.buttonCancel");
        }

        #endregion

        #region Event handlers

        private void YesNoAllDialog_Load(object sender, EventArgs e) {
            this.Text = _caption;
            this.labelText.Text = _text;

            int contentWidth = Math.Max(this.labelText.Width, this.panelButtons.Width) + this.Padding.Left + this.Padding.Right;
            int contentHeight = this.labelText.Height + panelButtons.Height + this.Padding.Top + this.Padding.Bottom;

            this.ClientSize = new Size(contentWidth, contentHeight);

            this.labelText.Location = new Point((contentWidth - this.labelText.Width) / 2, this.Padding.Top);
            this.panelButtons.Location = new Point((contentWidth - this.panelButtons.Width) / 2, this.Padding.Top + this.labelText.Height);
        }

        private void buttonYes_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        private void buttonNo_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.No;
            this.Close();
        }

        private void buttonYesToAll_Click(object sender, EventArgs e) {
            this.DialogResult = YesToAll;
            this.Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        #endregion
    }
}