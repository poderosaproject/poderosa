/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PreferenceItemEditor.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Preferences;

namespace Poderosa.Usability {
    internal class PreferenceItemEditor : Form {
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
            this._nameLabel = new System.Windows.Forms.Label();
            this._valueLabel = new System.Windows.Forms.Label();
            this._valueBox = new System.Windows.Forms.TextBox();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._resetButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _nameLabel
            // 
            this._nameLabel.AutoSize = true;
            this._nameLabel.Location = new System.Drawing.Point(13, 13);
            this._nameLabel.Name = "_nameLabel";
            this._nameLabel.Size = new System.Drawing.Size(0, 12);
            this._nameLabel.TabIndex = 0;
            this._nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _valueLabel
            // 
            this._valueLabel.AutoSize = true;
            this._valueLabel.Location = new System.Drawing.Point(12, 35);
            this._valueLabel.Name = "_valueLabel";
            this._valueLabel.Size = new System.Drawing.Size(35, 12);
            this._valueLabel.TabIndex = 1;
            this._valueLabel.Text = "label2";
            this._valueLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _valueBox
            // 
            this._valueBox.Location = new System.Drawing.Point(60, 32);
            this._valueBox.Name = "_valueBox";
            this._valueBox.Size = new System.Drawing.Size(242, 19);
            this._valueBox.TabIndex = 2;
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(146, 57);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(75, 23);
            this._okButton.TabIndex = 3;
            this._okButton.Text = "button1";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new EventHandler(OnOK);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(227, 57);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(75, 23);
            this._cancelButton.TabIndex = 4;
            this._cancelButton.UseVisualStyleBackColor = true;
            // 
            // _resetButton
            // 
            this._resetButton.Location = new System.Drawing.Point(13, 56);
            this._resetButton.Name = "_resetButton";
            this._resetButton.Size = new System.Drawing.Size(75, 23);
            this._resetButton.TabIndex = 5;
            this._resetButton.UseVisualStyleBackColor = true;
            this._resetButton.Click += new EventHandler(OnReset);
            // 
            // PreferenceItemEditor
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(314, 90);
            this.Controls.Add(this._resetButton);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._valueBox);
            this.Controls.Add(this._valueLabel);
            this.Controls.Add(this._nameLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PreferenceItemEditor";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PreferenceItemEditor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }


        #endregion

        private System.Windows.Forms.Label _nameLabel;
        private System.Windows.Forms.Label _valueLabel;
        private System.Windows.Forms.TextBox _valueBox;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _resetButton;

        private IPreferenceItem _item;

        public PreferenceItemEditor(IPreferenceItem item) {
            InitializeComponent();
            _item = item;

            StringResource sr = UsabilityPlugin.Strings;
            this.Text = sr.GetString("Form.PreferenceItemEditor.Text");
            _nameLabel.Text = sr.GetString("Form.PreferenceItemEditor._nameLabel") + " " + item.FullQualifiedId;
            _valueLabel.Text = sr.GetString("Form.PreferenceItemEditor._valueLabel");
            _resetButton.Text = sr.GetString("Form.PreferenceItemEditor._resetButton");
            _okButton.Text = sr.GetString("Common.OK");
            _cancelButton.Text = sr.GetString("Common.Cancel");

            //int/stringどちらかの場合をサポート
            IIntPreferenceItem intitem = item.AsInt();
            IStringPreferenceItem stritem = item.AsString();
            Debug.Assert(intitem != null || stritem != null);
            _valueBox.Text = intitem != null ? intitem.Value.ToString() : stritem.Value;
        }

        private void OnReset(object sender, EventArgs e) {
            IIntPreferenceItem intitem = _item.AsInt();
            IStringPreferenceItem stritem = _item.AsString();
            Debug.Assert(intitem != null || stritem != null);
            _valueBox.Text = intitem != null ? intitem.InitialValue.ToString() : stritem.InitialValue;
        }
        private void OnOK(object sender, EventArgs e) {
            try {
                IIntPreferenceItem intitem = _item.AsInt();
                IStringPreferenceItem stritem = _item.AsString();

                if (intitem != null)
                    intitem.Value = ParseUtil.ParseInt(_valueBox.Text, intitem.InitialValue);
                else
                    stritem.Value = _valueBox.Text;
            }
            catch (Exception ex) {
                GUtil.Warning(this, ex.Message);
                this.DialogResult = DialogResult.None;
            }
        }
    }
}