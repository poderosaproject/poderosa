/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: EnvironmentVariablesDialog.cs,v 1.2 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Poderosa.Pipe {

    /// <summary>
    /// Edit environment variables
    /// </summary>
    internal partial class EnvironmentVariablesDialog : Form {

        private PipeTerminalParameter.EnvironmentVariable[] _environmentVariables;

        public PipeTerminalParameter.EnvironmentVariable[] EnvironmentVariables {
            get {
                return _environmentVariables;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public EnvironmentVariablesDialog() {
            InitializeComponent();

            SetupControls();
        }

        /// <summary>
        /// Set parameters
        /// </summary>
        /// <param name="env">environment variables</param>
        public void ApplyParams(PipeTerminalParameter.EnvironmentVariable[] env) {
            _listViewVariables.Items.Clear();

            if (env != null && env.Length > 0) {
                List<PipeTerminalParameter.EnvironmentVariable> varList = new List<PipeTerminalParameter.EnvironmentVariable>();
                varList.AddRange(env);
                SortItemList(varList);

                for (int i = 0; i < varList.Count; i++) {
                    ListViewItem item = new ListViewItem(
                                            new string[] {
                                            varList[i].Name,
                                            varList[i].Value
                                        });
                    _listViewVariables.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Set i18n text
        /// </summary>
        private void SetupControls() {
            StringResource res = PipePlugin.Instance.Strings;

            this.Text = res.GetString("Form.EnvironmentVariablesDialog.Title");

            _labelDescription.Text = res.GetString("Form.EnvironmentVariablesDialog._labelDescription");

            _columnName.Text = res.GetString("Form.EnvironmentVariablesDialog._columnName");
            _columnValue.Text = res.GetString("Form.EnvironmentVariablesDialog._columnValue");

            _buttonAdd.Text = res.GetString("Form.EnvironmentVariablesDialog._buttonAdd");
            _buttonEdit.Text = res.GetString("Form.EnvironmentVariablesDialog._buttonEdit");
            _buttonDelete.Text = res.GetString("Form.EnvironmentVariablesDialog._buttonDelete");

            _buttonOK.Text = res.GetString("Common.OK");
            _buttonCancel.Text = res.GetString("Common.Cancel");
        }

        private void SortItemList(List<PipeTerminalParameter.EnvironmentVariable> itemList) {
            itemList.Sort(
                delegate(
                    PipeTerminalParameter.EnvironmentVariable a,
                    PipeTerminalParameter.EnvironmentVariable b
                ) {
                    return String.CompareOrdinal(a.Name, b.Name);
                });
        }

        private void DeleteItem(string name) {
            for (int i = 0; i < _listViewVariables.Items.Count; ) {
                if (String.Compare(_listViewVariables.Items[i].Text, name, true) == 0)
                    _listViewVariables.Items.RemoveAt(i);
                else
                    i++;
            }
        }

        private void AddItem(string name, string value) {
            int index = 0;
            for (; index < _listViewVariables.Items.Count; index++) {
                if (String.Compare(
                        _listViewVariables.Items[index].Text,
                        name,
                        StringComparison.OrdinalIgnoreCase) > 0) {

                    break;
                }
            }

            ListViewItem item = new ListViewItem(
                                        new string[] {
                                            name,
                                            value
                                        });
            _listViewVariables.Items.Insert(index, item);
        }

        private void _buttonAdd_Click(object sender, EventArgs e) {
            using (EditVariableDialog dialog = new EditVariableDialog()) {
                dialog.ApplyParams("", "");
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    string name = dialog.VariableName;
                    string value = dialog.VariableValue;
                    DeleteItem(name);
                    AddItem(name, value);
                }
            }
        }

        private void _buttonEdit_Click(object sender, EventArgs e) {
            if (_listViewVariables.SelectedItems.Count == 0)
                return;

            ListViewItem selectedItem = _listViewVariables.SelectedItems[0];
            string oldName = selectedItem.SubItems[0].Text;
            string oldValue = selectedItem.SubItems[1].Text;

            using (EditVariableDialog dialog = new EditVariableDialog()) {
                dialog.ApplyParams(oldName, oldValue);
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    string name = dialog.VariableName;
                    string value = dialog.VariableValue;
                    DeleteItem(oldName);
                    DeleteItem(name);
                    AddItem(name, value);
                }
            }
        }

        private void _buttonDelete_Click(object sender, EventArgs e) {
            if (_listViewVariables.SelectedItems.Count == 0)
                return;

            ListViewItem selectedItem = _listViewVariables.SelectedItems[0];
            _listViewVariables.Items.Remove(selectedItem);
        }

        private void _buttonCancel_Click(object sender, EventArgs e) {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void _buttonOK_Click(object sender, EventArgs e) {
            _environmentVariables = new PipeTerminalParameter.EnvironmentVariable[_listViewVariables.Items.Count];

            for (int i = 0; i < _listViewVariables.Items.Count; i++) {
                ListViewItem item = _listViewVariables.Items[i];
                string name = item.SubItems[0].Text;
                string value = item.SubItems[1].Text;
                _environmentVariables[i] = new PipeTerminalParameter.EnvironmentVariable(name, value);
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }


    }
}