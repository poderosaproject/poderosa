/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PeripheralOptionPanel.cs,v 1.6 2012/07/03 14:36:37 kzmi Exp $
 */
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text;

using Poderosa.Util;
using Poderosa.Usability;
using Poderosa.Preferences;
using Poderosa.UI;
using Poderosa.Terminal;

namespace Poderosa.Forms {
    internal class PeripheralOptionPanel : UserControl {
        private bool _constructing = false; // temporarily disable updating key settings

        private GroupBox _mouseGroup;
        private GroupBox _keyboardGroup;

        private System.Windows.Forms.Label _leftAltKeyLabel;
        private ComboBox _leftAltKeyAction;
        private System.Windows.Forms.Label _rightAltKeyLabel;
        private ComboBox _rightAltKeyAction;
        private System.Windows.Forms.Label _rightButtonActionLabel;
        private ComboBox _rightButtonAction;
        private System.Windows.Forms.Label _middleButtonActionLabel;
        private ComboBox _middleButtonAction;
        private CheckBox _autoCopyByLeftButton;
        private System.Windows.Forms.Label _wheelAmountLabel;
        private TextBox _wheelAmount;
        private Label _viewSplitModifierLabel;
        private ComboBox _viewSplitModifierBox;
        private CheckBox _send0x7FByDel;
        private CheckBox _send0x7FByBack;
        private Label _zone0x1FLabel;
        private ComboBox _zone0x1FBox;
        private Label _autoKeySettingsLabel;
        private TextBox _autoKeySettingsBox;
        private Label _customKeySettingsLabel;
        private TextBox _customKeySettingsBox;

        public PeripheralOptionPanel() {
            InitializeComponent();
            FillText();
        }
        private void InitializeComponent() {
            _mouseGroup = new GroupBox();
            _keyboardGroup = new GroupBox();


            this._leftAltKeyLabel = new System.Windows.Forms.Label();
            this._leftAltKeyAction = new ComboBox();
            this._rightAltKeyLabel = new System.Windows.Forms.Label();
            this._rightAltKeyAction = new ComboBox();
            this._send0x7FByDel = new CheckBox();
            this._send0x7FByBack = new CheckBox();
            this._zone0x1FLabel = new Label();
            this._zone0x1FBox = new ComboBox();
            this._autoKeySettingsLabel = new Label();
            this._autoKeySettingsBox = new TextBox();
            this._customKeySettingsLabel = new Label();
            this._customKeySettingsBox = new TextBox();
            this._autoCopyByLeftButton = new CheckBox();
            this._rightButtonActionLabel = new Label();
            this._rightButtonAction = new ComboBox();
            this._middleButtonActionLabel = new Label();
            this._middleButtonAction = new ComboBox();
            this._wheelAmountLabel = new Label();
            this._wheelAmount = new TextBox();
            this._viewSplitModifierLabel = new Label();
            this._viewSplitModifierBox = new ComboBox();

            this.Controls.AddRange(new Control[] {
                _mouseGroup, _keyboardGroup
            });
            _mouseGroup.Controls.AddRange(new Control[] {
                _rightButtonActionLabel, _rightButtonAction,
                _middleButtonActionLabel, _middleButtonAction,
                _wheelAmountLabel, _wheelAmount,
                _autoCopyByLeftButton,
                _viewSplitModifierLabel, _viewSplitModifierBox
            });
            _keyboardGroup.Controls.AddRange(new Control[] {
                _leftAltKeyLabel, _leftAltKeyAction,
                _rightAltKeyLabel, _rightAltKeyAction,
                _send0x7FByDel, _send0x7FByBack,
                _zone0x1FLabel, _zone0x1FBox,
                _autoKeySettingsLabel, _autoKeySettingsBox,
                _customKeySettingsLabel, _customKeySettingsBox
            });

            _mouseGroup.Location = new Point(8, 8);
            _mouseGroup.Size = new Size(408, 138);
            _mouseGroup.TabIndex = 0;
            _mouseGroup.FlatStyle = FlatStyle.System;

            _keyboardGroup.Location = new Point(8, 152);
            _keyboardGroup.Size = new Size(408, 162);
            _keyboardGroup.TabIndex = 1;
            _keyboardGroup.FlatStyle = FlatStyle.System;

            // 
            // _rightButtonActionLabel
            // 
            this._rightButtonActionLabel.Location = new System.Drawing.Point(8, 18);
            this._rightButtonActionLabel.Name = "_rightButtonActionLabel";
            this._rightButtonActionLabel.Size = new System.Drawing.Size(160, 23);
            this._rightButtonActionLabel.TabIndex = 0;
            this._rightButtonActionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _rightButtonAction
            // 
            this._rightButtonAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._rightButtonAction.Location = new System.Drawing.Point(240, 18);
            this._rightButtonAction.Name = "_rightButtonAction";
            this._rightButtonAction.Size = new System.Drawing.Size(152, 19);
            this._rightButtonAction.TabIndex = 1;
            // 
            // _middleButtonActionLabel
            // 
            this._middleButtonActionLabel.Location = new System.Drawing.Point(8, 42);
            this._middleButtonActionLabel.Name = "_middleButtonActionLabel";
            this._middleButtonActionLabel.Size = new System.Drawing.Size(160, 23);
            this._middleButtonActionLabel.TabIndex = 2;
            this._middleButtonActionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _middleButtonAction
            // 
            this._middleButtonAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._middleButtonAction.Location = new System.Drawing.Point(240, 42);
            this._middleButtonAction.Name = "_middleButtonAction";
            this._middleButtonAction.Size = new System.Drawing.Size(152, 19);
            this._middleButtonAction.TabIndex = 3;
            // 
            // _wheelAmountLabel
            // 
            this._wheelAmountLabel.Location = new System.Drawing.Point(8, 66);
            this._wheelAmountLabel.Name = "_wheelAmountLabel";
            this._wheelAmountLabel.Size = new System.Drawing.Size(176, 23);
            this._wheelAmountLabel.TabIndex = 4;
            this._wheelAmountLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _wheelAmount
            // 
            this._wheelAmount.Location = new System.Drawing.Point(240, 66);
            this._wheelAmount.Name = "_wheelAmount";
            this._wheelAmount.Size = new System.Drawing.Size(152, 19);
            this._wheelAmount.TabIndex = 5;
            this._wheelAmount.MaxLength = 2;
            // 
            // _autoCopyByLeftButton
            // 
            this._autoCopyByLeftButton.Location = new System.Drawing.Point(8, 90);
            this._autoCopyByLeftButton.Name = "_autoCopyByLeftButton";
            this._autoCopyByLeftButton.FlatStyle = FlatStyle.System;
            this._autoCopyByLeftButton.Size = new System.Drawing.Size(288, 20);
            this._autoCopyByLeftButton.TabIndex = 6;
            // 
            // _viewSplitModifierLabel
            // 
            this._viewSplitModifierLabel.Location = new System.Drawing.Point(8, 114);
            this._viewSplitModifierLabel.Name = "_viewSplitModifierLabel";
            this._viewSplitModifierLabel.Size = new System.Drawing.Size(176, 23);
            this._viewSplitModifierLabel.TabIndex = 7;
            this._viewSplitModifierLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _viewSplitModifierBox
            // 
            this._viewSplitModifierBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._viewSplitModifierBox.Location = new System.Drawing.Point(240, 114);
            this._viewSplitModifierBox.Name = "_viewSplitModifierBox";
            this._viewSplitModifierBox.Size = new System.Drawing.Size(152, 19);
            this._viewSplitModifierBox.TabIndex = 8;

            // 
            // _leftAltKeyLabel
            // 
            this._leftAltKeyLabel.Location = new System.Drawing.Point(8, 18);
            this._leftAltKeyLabel.Name = "_leftAltKeyLabel";
            this._leftAltKeyLabel.Size = new System.Drawing.Size(160, 23);
            this._leftAltKeyLabel.TabIndex = 0;
            this._leftAltKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _leftAltKey
            // 
            this._leftAltKeyAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._leftAltKeyAction.Location = new System.Drawing.Point(240, 18);
            this._leftAltKeyAction.Name = "_leftAltKey";
            this._leftAltKeyAction.Size = new System.Drawing.Size(152, 19);
            this._leftAltKeyAction.TabIndex = 1;
            // 
            // _rightAltKeyLabel
            // 
            this._rightAltKeyLabel.Location = new System.Drawing.Point(8, 42);
            this._rightAltKeyLabel.Name = "_rightAltKeyLabel";
            this._rightAltKeyLabel.Size = new System.Drawing.Size(160, 23);
            this._rightAltKeyLabel.TabIndex = 2;
            this._rightAltKeyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _rightAltKey
            // 
            this._rightAltKeyAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._rightAltKeyAction.Location = new System.Drawing.Point(240, 42);
            this._rightAltKeyAction.Name = "_rightAltKey";
            this._rightAltKeyAction.Size = new System.Drawing.Size(152, 19);
            this._rightAltKeyAction.TabIndex = 3;
            // 
            // _send0x7FByDel
            // 
            this._send0x7FByDel.Location = new System.Drawing.Point(8, 66);
            this._send0x7FByDel.Name = "_send0x7FByDel";
            this._send0x7FByDel.FlatStyle = FlatStyle.System;
            this._send0x7FByDel.Size = new System.Drawing.Size(192, 20);
            this._send0x7FByDel.TabIndex = 4;
            this._send0x7FByDel.CheckedChanged += new EventHandler(AdjustAutoKeySettings);
            //
            // _send0x7FByBack
            //
            this._send0x7FByBack.Location = new System.Drawing.Point(200, 66);
            this._send0x7FByBack.Name = "_send0x7FByBack";
            this._send0x7FByBack.FlatStyle = FlatStyle.System;
            this._send0x7FByBack.Size = new System.Drawing.Size(192, 20);
            this._send0x7FByBack.TabIndex = 5;
            this._send0x7FByBack.CheckedChanged += new EventHandler(AdjustAutoKeySettings);
            // 
            // _zone0x1FLabel
            // 
            this._zone0x1FLabel.Location = new System.Drawing.Point(8, 90);
            this._zone0x1FLabel.Name = "_zone0x1FLabel";
            this._zone0x1FLabel.Size = new System.Drawing.Size(176, 23);
            this._zone0x1FLabel.TabIndex = 6;
            this._zone0x1FLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _zone0x1FBox
            // 
            this._zone0x1FBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._zone0x1FBox.Location = new System.Drawing.Point(240, 90);
            this._zone0x1FBox.Name = "_zone0x1FBox";
            this._zone0x1FBox.Size = new System.Drawing.Size(152, 19);
            this._zone0x1FBox.TabIndex = 7;
            this._zone0x1FBox.SelectedIndexChanged += new EventHandler(AdjustAutoKeySettings);
            // 
            // _autoKeySettingsLabel
            // 
            this._autoKeySettingsLabel.Location = new System.Drawing.Point(8, 114);
            this._autoKeySettingsLabel.Name = "_autoKeySettingsLabel";
            this._autoKeySettingsLabel.Size = new System.Drawing.Size(120, 23);
            this._autoKeySettingsLabel.TabIndex = 8;
            this._autoKeySettingsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _autoKeySettingsBox
            // 
            this._autoKeySettingsBox.Location = new System.Drawing.Point(144, 114);
            this._autoKeySettingsBox.Size = new System.Drawing.Size(248, 23);
            this._autoKeySettingsBox.TabIndex = 9;
            this._autoKeySettingsBox.Enabled = false;
            // 
            // _customKeySettingsLabel
            // 
            this._customKeySettingsLabel.Location = new System.Drawing.Point(8, 138);
            this._customKeySettingsLabel.Name = "_autoKeySettingsLabel";
            this._customKeySettingsLabel.Size = new System.Drawing.Size(120, 23);
            this._customKeySettingsLabel.TabIndex = 10;
            this._customKeySettingsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _customKeySettingsBox
            // 
            this._customKeySettingsBox.Location = new System.Drawing.Point(144, 138);
            this._customKeySettingsBox.Size = new System.Drawing.Size(248, 50);
            this._customKeySettingsBox.TabIndex = 11;
            this._customKeySettingsBox.WordWrap = true;

            this.BackColor = SystemColors.Window;
        }
        private void FillText() {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            this._mouseGroup.Text = sr.GetString("Form.OptionDialog._mouseGroup");
            this._keyboardGroup.Text = sr.GetString("Form.OptionDialog._keyboardGroup");
            this._leftAltKeyLabel.Text = sr.GetString("Form.OptionDialog._leftAltKeyLabel");
            this._rightAltKeyLabel.Text = sr.GetString("Form.OptionDialog._rightAltKeyLabel");
            this._send0x7FByDel.Text = sr.GetString("Form.OptionDialog._send0x7FByDel");
            this._send0x7FByBack.Text = sr.GetString("Form.OptionDialog._send0x7FByBack");
            this._autoCopyByLeftButton.Text = sr.GetString("Form.OptionDialog._autoCopyByLeftButton");
            this._viewSplitModifierLabel.Text = sr.GetString("Form.OptionDialog._viewSplitModifierLabel");
            this._rightButtonActionLabel.Text = sr.GetString("Form.OptionDialog._rightButtonActionLabel");
            this._middleButtonActionLabel.Text = sr.GetString("Form.OptionDialog._middleButtonActionLabel");
            this._wheelAmountLabel.Text = sr.GetString("Form.OptionDialog._wheelAmountLabel");
            this._zone0x1FLabel.Text = sr.GetString("Form.OptionDialog._zone0x1FLabel");
            this._autoKeySettingsLabel.Text = sr.GetString("Form.OptionDialog._autoKeySettingsLabel");
            this._customKeySettingsLabel.Text = sr.GetString("Form.OptionDialog._customKeySettingsLabel");

            _leftAltKeyAction.Items.AddRange(EnumListItem<AltKeyAction>.GetListItems());
            _rightAltKeyAction.Items.AddRange(EnumListItem<AltKeyAction>.GetListItems());
            _rightButtonAction.Items.AddRange(EnumListItem<MouseButtonAction>.GetListItems());
            _middleButtonAction.Items.AddRange(EnumListItem<MouseButtonAction>.GetListItems());
            _zone0x1FBox.Items.AddRange(EnumListItem<KeyboardStyle>.GetListItems());
            _viewSplitModifierBox.Items.AddRange(
                new object[] {
                    new ListItem<Keys>(Keys.None, sr.GetString("Caption.KeysNone")),
                    new ListItem<Keys>(Keys.Control, "Ctrl"),
                    new ListItem<Keys>(Keys.Shift, "Shift"),
                    new ListItem<Keys>(Keys.Alt, "Alt"),
                });
        }

        private Keys FixupViewSplitModifier(Keys key) {
            switch (key) {
                case Keys.Control:
                case Keys.Shift:
                case Keys.Alt:
                    return key;
                case Keys.Menu:
                    return Keys.Alt;
                default:
                    return Keys.None;
            }
        }

        public void InitUI(ITerminalEmulatorOptions options, ICoreServicePreference window_options) {
            _constructing = true;

            _leftAltKeyAction.SelectedItem = options.LeftAltKey;            // select EnumListItem<T> by T
            _rightAltKeyAction.SelectedItem = options.RightAltKey;          // select EnumListItem<T> by T
            _send0x7FByDel.Checked = options.Send0x7FByDel;
            _send0x7FByBack.Checked = options.Send0x7FByBack;
            _autoCopyByLeftButton.Checked = window_options.AutoCopyByLeftButton;
            _rightButtonAction.SelectedItem = options.RightButtonAction;    // select EnumListItem<T> by T
            _middleButtonAction.SelectedItem = options.MiddleButtonAction;  // select EnumListItem<T> by T
            _wheelAmount.Text = options.WheelAmount.ToString();
            _zone0x1FBox.SelectedItem = options.Zone0x1F;                   // select EnumListItem<T> by T
            _viewSplitModifierBox.SelectedItem = FixupViewSplitModifier(window_options.ViewSplitModifier);  // select EnumListItem<T> by T
            _customKeySettingsBox.Text = options.CustomKeySettings;

            _constructing = false;

            AdjustAutoKeySettings(null, null);
        }
        public bool Commit(ITerminalEmulatorOptions options, ICoreServicePreference window_options) {
            StringResource sr = OptionDialogPlugin.Instance.Strings;
            bool successful = false;
            string itemname = null;
            try {
                //Win9xでは、左右のAltの区別ができないので別々の設定にすることを禁止する
                if (System.Environment.OSVersion.Platform == PlatformID.Win32Windows &&
                        ((EnumListItem<AltKeyAction>)_leftAltKeyAction.SelectedItem).Value
                            != ((EnumListItem<AltKeyAction>)_rightAltKeyAction.SelectedItem).Value) {
                    GUtil.Warning(this, sr.GetString("Message.OptionDialog.AltKeyOnWin9x"));
                    return false;
                }

                options.LeftAltKey = ((EnumListItem<AltKeyAction>)_leftAltKeyAction.SelectedItem).Value;
                options.RightAltKey = ((EnumListItem<AltKeyAction>)_rightAltKeyAction.SelectedItem).Value;
                options.Send0x7FByDel = _send0x7FByDel.Checked;
                options.Send0x7FByBack = _send0x7FByBack.Checked;
                options.Zone0x1F = ((EnumListItem<KeyboardStyle>)_zone0x1FBox.SelectedItem).Value;
                itemname = "Custom Key Setting";
                KeyFunction.Parse(_customKeySettingsBox.Text); //パースできればOK
                options.CustomKeySettings = _customKeySettingsBox.Text;
                window_options.AutoCopyByLeftButton = _autoCopyByLeftButton.Checked;
                options.RightButtonAction = ((EnumListItem<MouseButtonAction>)_rightButtonAction.SelectedItem).Value;
                options.MiddleButtonAction = ((EnumListItem<MouseButtonAction>)_middleButtonAction.SelectedItem).Value;

                itemname = sr.GetString("Caption.OptionDialog.MousewheelAmount");
                options.WheelAmount = Int32.Parse(_wheelAmount.Text);

                window_options.ViewSplitModifier = ((ListItem<Keys>)_viewSplitModifierBox.SelectedItem).Value;

                successful = true;
            }
            catch (FormatException) {
                GUtil.Warning(this, String.Format(sr.GetString("Message.OptionDialog.InvalidItem"), itemname));
            }
            catch (InvalidOptionException ex) {
                GUtil.Warning(this, ex.Message);
            }
            return successful;
        }

        private void AdjustAutoKeySettings(object sender, EventArgs args) {
            if (_constructing)
                return;

            StringBuilder bld = new StringBuilder();
            if (_send0x7FByDel.Checked)
                bld.Append("Delete=0x7F");
            if (_send0x7FByBack.Checked) {
                if (bld.Length > 0)
                    bld.Append(", ");
                bld.Append("Back=0x7F");
            }

            KeyboardStyle ks = ((EnumListItem<KeyboardStyle>)_zone0x1FBox.SelectedItem).Value;
            if (ks != KeyboardStyle.None) {
                string s;
                if (ks == KeyboardStyle.Default)
                    s = "Ctrl+D6=0x1E, Ctrl+Minus=0x1F";
                else //Japanese
                    s = "Ctrl+BackSlash=0x1F";
                //一応パース
                //KeyFunction.Parse(s);
                if (bld.Length > 0)
                    bld.Append(", ");
                bld.Append(s);
            }
            _autoKeySettingsBox.Text = bld.ToString();
        }

    }


    internal class PeripheralOptionPanelExtension : OptionPanelExtensionBase {
        private PeripheralOptionPanel _panel;

        public PeripheralOptionPanelExtension()
            : base("Form.OptionDialog._peripheralPanel", 2) {
        }

        public override string[] PreferenceFolderIDsToEdit {
            get {
                return new string[] { "org.poderosa.terminalemulator", "org.poderosa.core.window" };
            }
        }
        public override Control ContentPanel {
            get {
                return _panel;
            }
        }

        public override void InitiUI(IPreferenceFolder[] values) {
            if (_panel == null)
                _panel = new PeripheralOptionPanel();
            _panel.InitUI((ITerminalEmulatorOptions)values[0].QueryAdapter(typeof(ITerminalEmulatorOptions)),
                (ICoreServicePreference)values[1].QueryAdapter(typeof(ICoreServicePreference)));
        }

        public override bool Commit(IPreferenceFolder[] values) {
            Debug.Assert(_panel != null);
            return _panel.Commit((ITerminalEmulatorOptions)values[0].QueryAdapter(typeof(ITerminalEmulatorOptions)),
                (ICoreServicePreference)values[1].QueryAdapter(typeof(ICoreServicePreference)));
        }

        public override void Dispose() {
            if (_panel != null) {
                _panel.Dispose();
                _panel = null;
            }
        }
    }
}
