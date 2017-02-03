namespace Poderosa.ConnectProfile {
    partial class ProfileEditForm {
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
            this._hostNameBox = new Poderosa.UI.WaterMarkTextBox();
            this._hostNameLabel = new System.Windows.Forms.Label();
            this._userNameLabel = new System.Windows.Forms.Label();
            this._userNameBox = new Poderosa.UI.WaterMarkTextBox();
            this._passwordLabel = new System.Windows.Forms.Label();
            this._basicGroup = new System.Windows.Forms.GroupBox();
            this._portBox = new System.Windows.Forms.NumericUpDown();
            this._protocolBox = new System.Windows.Forms.ComboBox();
            this._portLabel = new System.Windows.Forms.Label();
            this._protocolLabel = new System.Windows.Forms.Label();
            this._passwordPromptBox = new Poderosa.UI.WaterMarkTextBox();
            this._passwordPromptLabel = new System.Windows.Forms.Label();
            this._loginPromptBox = new Poderosa.UI.WaterMarkTextBox();
            this._loginPromptLabel = new System.Windows.Forms.Label();
            this._autoLoginCheck = new System.Windows.Forms.CheckBox();
            this._passwordBox = new Poderosa.UI.WaterMarkTextBox();
            this._sshGroup = new System.Windows.Forms.GroupBox();
            this._authTypeBox = new System.Windows.Forms.ComboBox();
            this._keyFileBox = new Poderosa.UI.WaterMarkTextBox();
            this._openKeyFileButton = new System.Windows.Forms.Button();
            this._keyFileLabel = new System.Windows.Forms.Label();
            this._authTypeLabel = new System.Windows.Forms.Label();
            this._okButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._suGroup = new System.Windows.Forms.GroupBox();
            this._suTypeRadio2 = new System.Windows.Forms.RadioButton();
            this._suUserNameBox = new Poderosa.UI.WaterMarkTextBox();
            this._suPasswordBox = new Poderosa.UI.WaterMarkTextBox();
            this._suTypeRadio1 = new System.Windows.Forms.RadioButton();
            this._suTypeLabel = new System.Windows.Forms.Label();
            this._suUserNameLabel = new System.Windows.Forms.Label();
            this._suPasswordLabel = new System.Windows.Forms.Label();
            this._etcGroup = new System.Windows.Forms.GroupBox();
            this._promptRecvTimeoutBox = new System.Windows.Forms.NumericUpDown();
            this._commandSendIntBox = new System.Windows.Forms.NumericUpDown();
            this._descriptionBox = new Poderosa.UI.WaterMarkTextBox();
            this._profileItemColorButton = new Poderosa.UI.ColorButton();
            this._promptRecvTimeoutLabel = new System.Windows.Forms.Label();
            this._commandSendIntLabel = new System.Windows.Forms.Label();
            this._descriptionLabel = new System.Windows.Forms.Label();
            this._profileItemColorLabel = new System.Windows.Forms.Label();
            this._terminalFontColorButton = new Poderosa.UI.ColorButton();
            this._terminalFontColorLabel = new System.Windows.Forms.Label();
            this._terminalBGColorButton = new Poderosa.UI.ColorButton();
            this._terminalBGColorLabel = new System.Windows.Forms.Label();
            this._execCommandBox = new Poderosa.UI.WaterMarkTextBox();
            this._execCommandLabel = new System.Windows.Forms.Label();
            this._charCodeLabel = new System.Windows.Forms.Label();
            this._charCodeBox = new System.Windows.Forms.ComboBox();
            this._terminalGroup = new System.Windows.Forms.GroupBox();
            this._telnetNewLineCheck = new System.Windows.Forms.CheckBox();
            this._terminalTypeBox = new System.Windows.Forms.ComboBox();
            this._newLineTypeBox = new System.Windows.Forms.ComboBox();
            this._newLineTypeLabel = new System.Windows.Forms.Label();
            this._terminalTypeLabel = new System.Windows.Forms.Label();
            this._hintLabel = new System.Windows.Forms.Label();
            this._autoLoginGroup = new System.Windows.Forms.GroupBox();
            this._accountGroup = new System.Windows.Forms.GroupBox();
            this._basicGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._portBox)).BeginInit();
            this._sshGroup.SuspendLayout();
            this._suGroup.SuspendLayout();
            this._etcGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._promptRecvTimeoutBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._commandSendIntBox)).BeginInit();
            this._terminalGroup.SuspendLayout();
            this._autoLoginGroup.SuspendLayout();
            this._accountGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _hostNameBox
            // 
            this._hostNameBox.Location = new System.Drawing.Point(109, 22);
            this._hostNameBox.Name = "_hostNameBox";
            this._hostNameBox.Size = new System.Drawing.Size(131, 19);
            this._hostNameBox.TabIndex = 0;
            this._hostNameBox.WaterMarkAlsoFocus = true;
            this._hostNameBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._hostNameBox.WaterMarkText = "";
            this._hostNameBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _hostNameLabel
            // 
            this._hostNameLabel.AutoSize = true;
            this._hostNameLabel.Location = new System.Drawing.Point(6, 25);
            this._hostNameLabel.Name = "_hostNameLabel";
            this._hostNameLabel.Size = new System.Drawing.Size(87, 12);
            this._hostNameLabel.TabIndex = 1;
            this._hostNameLabel.Text = "_hostNameLabel";
            // 
            // _userNameLabel
            // 
            this._userNameLabel.AutoSize = true;
            this._userNameLabel.Location = new System.Drawing.Point(6, 24);
            this._userNameLabel.Name = "_userNameLabel";
            this._userNameLabel.Size = new System.Drawing.Size(87, 12);
            this._userNameLabel.TabIndex = 2;
            this._userNameLabel.Text = "_userNameLabel";
            // 
            // _userNameBox
            // 
            this._userNameBox.Location = new System.Drawing.Point(109, 21);
            this._userNameBox.Name = "_userNameBox";
            this._userNameBox.Size = new System.Drawing.Size(131, 19);
            this._userNameBox.TabIndex = 2;
            this._userNameBox.WaterMarkAlsoFocus = true;
            this._userNameBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._userNameBox.WaterMarkText = "";
            this._userNameBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _passwordLabel
            // 
            this._passwordLabel.AutoSize = true;
            this._passwordLabel.Location = new System.Drawing.Point(6, 49);
            this._passwordLabel.Name = "_passwordLabel";
            this._passwordLabel.Size = new System.Drawing.Size(84, 12);
            this._passwordLabel.TabIndex = 4;
            this._passwordLabel.Text = "_passwordLabel";
            // 
            // _basicGroup
            // 
            this._basicGroup.Controls.Add(this._portBox);
            this._basicGroup.Controls.Add(this._hostNameBox);
            this._basicGroup.Controls.Add(this._protocolBox);
            this._basicGroup.Controls.Add(this._hostNameLabel);
            this._basicGroup.Controls.Add(this._portLabel);
            this._basicGroup.Controls.Add(this._protocolLabel);
            this._basicGroup.Location = new System.Drawing.Point(12, 12);
            this._basicGroup.Name = "_basicGroup";
            this._basicGroup.Size = new System.Drawing.Size(251, 104);
            this._basicGroup.TabIndex = 0;
            this._basicGroup.TabStop = false;
            this._basicGroup.Text = "_basicGroup";
            // 
            // _portBox
            // 
            this._portBox.Location = new System.Drawing.Point(109, 73);
            this._portBox.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this._portBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._portBox.Name = "_portBox";
            this._portBox.Size = new System.Drawing.Size(131, 19);
            this._portBox.TabIndex = 2;
            this._portBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._portBox.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this._portBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _protocolBox
            // 
            this._protocolBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._protocolBox.FormattingEnabled = true;
            this._protocolBox.Location = new System.Drawing.Point(109, 47);
            this._protocolBox.Name = "_protocolBox";
            this._protocolBox.Size = new System.Drawing.Size(131, 20);
            this._protocolBox.TabIndex = 1;
            this._protocolBox.Tag = "";
            this._protocolBox.SelectedIndexChanged += new System.EventHandler(this.EnableValidControls);
            this._protocolBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _portLabel
            // 
            this._portLabel.AutoSize = true;
            this._portLabel.Location = new System.Drawing.Point(6, 75);
            this._portLabel.Name = "_portLabel";
            this._portLabel.Size = new System.Drawing.Size(56, 12);
            this._portLabel.TabIndex = 4;
            this._portLabel.Text = "_portLabel";
            // 
            // _protocolLabel
            // 
            this._protocolLabel.AutoSize = true;
            this._protocolLabel.Location = new System.Drawing.Point(6, 50);
            this._protocolLabel.Name = "_protocolLabel";
            this._protocolLabel.Size = new System.Drawing.Size(77, 12);
            this._protocolLabel.TabIndex = 1;
            this._protocolLabel.Text = "_protocolLabel";
            // 
            // _passwordPromptBox
            // 
            this._passwordPromptBox.Location = new System.Drawing.Point(109, 69);
            this._passwordPromptBox.Name = "_passwordPromptBox";
            this._passwordPromptBox.Size = new System.Drawing.Size(131, 19);
            this._passwordPromptBox.TabIndex = 2;
            this._passwordPromptBox.WaterMarkAlsoFocus = true;
            this._passwordPromptBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._passwordPromptBox.WaterMarkText = "";
            this._passwordPromptBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _passwordPromptLabel
            // 
            this._passwordPromptLabel.AutoSize = true;
            this._passwordPromptLabel.Location = new System.Drawing.Point(6, 72);
            this._passwordPromptLabel.Name = "_passwordPromptLabel";
            this._passwordPromptLabel.Size = new System.Drawing.Size(120, 12);
            this._passwordPromptLabel.TabIndex = 10;
            this._passwordPromptLabel.Text = "_passwordPromptLabel";
            // 
            // _loginPromptBox
            // 
            this._loginPromptBox.Location = new System.Drawing.Point(109, 44);
            this._loginPromptBox.Name = "_loginPromptBox";
            this._loginPromptBox.Size = new System.Drawing.Size(131, 19);
            this._loginPromptBox.TabIndex = 1;
            this._loginPromptBox.WaterMarkAlsoFocus = true;
            this._loginPromptBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._loginPromptBox.WaterMarkText = "";
            this._loginPromptBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _loginPromptLabel
            // 
            this._loginPromptLabel.AutoSize = true;
            this._loginPromptLabel.Location = new System.Drawing.Point(6, 47);
            this._loginPromptLabel.Name = "_loginPromptLabel";
            this._loginPromptLabel.Size = new System.Drawing.Size(96, 12);
            this._loginPromptLabel.TabIndex = 8;
            this._loginPromptLabel.Text = "_loginPromptLabel";
            // 
            // _autoLoginCheck
            // 
            this._autoLoginCheck.Location = new System.Drawing.Point(8, 22);
            this._autoLoginCheck.Name = "_autoLoginCheck";
            this._autoLoginCheck.Size = new System.Drawing.Size(131, 16);
            this._autoLoginCheck.TabIndex = 0;
            this._autoLoginCheck.Text = "_autoLoginCheck";
            this._autoLoginCheck.UseVisualStyleBackColor = true;
            this._autoLoginCheck.CheckedChanged += new System.EventHandler(this.EnableValidControls);
            this._autoLoginCheck.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _passwordBox
            // 
            this._passwordBox.Location = new System.Drawing.Point(109, 46);
            this._passwordBox.Name = "_passwordBox";
            this._passwordBox.Size = new System.Drawing.Size(131, 19);
            this._passwordBox.TabIndex = 3;
            this._passwordBox.UseSystemPasswordChar = true;
            this._passwordBox.WaterMarkAlsoFocus = true;
            this._passwordBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._passwordBox.WaterMarkText = "";
            this._passwordBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _sshGroup
            // 
            this._sshGroup.Controls.Add(this._authTypeBox);
            this._sshGroup.Controls.Add(this._keyFileBox);
            this._sshGroup.Controls.Add(this._openKeyFileButton);
            this._sshGroup.Controls.Add(this._keyFileLabel);
            this._sshGroup.Controls.Add(this._authTypeLabel);
            this._sshGroup.Location = new System.Drawing.Point(12, 122);
            this._sshGroup.Name = "_sshGroup";
            this._sshGroup.Size = new System.Drawing.Size(251, 75);
            this._sshGroup.TabIndex = 1;
            this._sshGroup.TabStop = false;
            this._sshGroup.Text = "_sshGroup";
            // 
            // _authTypeBox
            // 
            this._authTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._authTypeBox.FormattingEnabled = true;
            this._authTypeBox.Location = new System.Drawing.Point(109, 21);
            this._authTypeBox.Name = "_authTypeBox";
            this._authTypeBox.Size = new System.Drawing.Size(131, 20);
            this._authTypeBox.TabIndex = 0;
            this._authTypeBox.SelectedIndexChanged += new System.EventHandler(this.EnableValidControls);
            this._authTypeBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _keyFileBox
            // 
            this._keyFileBox.Enabled = false;
            this._keyFileBox.Location = new System.Drawing.Point(109, 47);
            this._keyFileBox.Name = "_keyFileBox";
            this._keyFileBox.Size = new System.Drawing.Size(106, 19);
            this._keyFileBox.TabIndex = 1;
            this._keyFileBox.WaterMarkAlsoFocus = true;
            this._keyFileBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._keyFileBox.WaterMarkText = "";
            this._keyFileBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _openKeyFileButton
            // 
            this._openKeyFileButton.Enabled = false;
            this._openKeyFileButton.Location = new System.Drawing.Point(221, 47);
            this._openKeyFileButton.Name = "_openKeyFileButton";
            this._openKeyFileButton.Size = new System.Drawing.Size(19, 19);
            this._openKeyFileButton.TabIndex = 2;
            this._openKeyFileButton.Text = "...";
            this._openKeyFileButton.UseVisualStyleBackColor = true;
            this._openKeyFileButton.Click += new System.EventHandler(this._openKeyFileButton_Click);
            this._openKeyFileButton.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _keyFileLabel
            // 
            this._keyFileLabel.AutoSize = true;
            this._keyFileLabel.Location = new System.Drawing.Point(6, 50);
            this._keyFileLabel.Name = "_keyFileLabel";
            this._keyFileLabel.Size = new System.Drawing.Size(73, 12);
            this._keyFileLabel.TabIndex = 4;
            this._keyFileLabel.Text = "_keyFileLabel";
            // 
            // _authTypeLabel
            // 
            this._authTypeLabel.AutoSize = true;
            this._authTypeLabel.Location = new System.Drawing.Point(6, 24);
            this._authTypeLabel.Name = "_authTypeLabel";
            this._authTypeLabel.Size = new System.Drawing.Size(83, 12);
            this._authTypeLabel.TabIndex = 1;
            this._authTypeLabel.Text = "_authTypeLabel";
            // 
            // _okButton
            // 
            this._okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._okButton.Location = new System.Drawing.Point(269, 491);
            this._okButton.Name = "_okButton";
            this._okButton.Size = new System.Drawing.Size(116, 23);
            this._okButton.TabIndex = 7;
            this._okButton.Text = "_okButton";
            this._okButton.UseVisualStyleBackColor = true;
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            this._okButton.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _cancelButton
            // 
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.Location = new System.Drawing.Point(403, 491);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.Size = new System.Drawing.Size(116, 23);
            this._cancelButton.TabIndex = 8;
            this._cancelButton.Text = "_cancelButton";
            this._cancelButton.UseVisualStyleBackColor = true;
            this._cancelButton.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _suGroup
            // 
            this._suGroup.Controls.Add(this._suTypeRadio2);
            this._suGroup.Controls.Add(this._suUserNameBox);
            this._suGroup.Controls.Add(this._suPasswordBox);
            this._suGroup.Controls.Add(this._suTypeRadio1);
            this._suGroup.Controls.Add(this._suTypeLabel);
            this._suGroup.Controls.Add(this._suUserNameLabel);
            this._suGroup.Controls.Add(this._suPasswordLabel);
            this._suGroup.Location = new System.Drawing.Point(8, 119);
            this._suGroup.Name = "_suGroup";
            this._suGroup.Size = new System.Drawing.Size(232, 100);
            this._suGroup.TabIndex = 4;
            this._suGroup.TabStop = false;
            this._suGroup.Text = "_suGroup";
            // 
            // _suTypeRadio2
            // 
            this._suTypeRadio2.Location = new System.Drawing.Point(165, 71);
            this._suTypeRadio2.Name = "_suTypeRadio2";
            this._suTypeRadio2.Size = new System.Drawing.Size(67, 18);
            this._suTypeRadio2.TabIndex = 3;
            this._suTypeRadio2.TabStop = true;
            this._suTypeRadio2.Text = "_suTypeRadio2";
            this._suTypeRadio2.UseVisualStyleBackColor = true;
            this._suTypeRadio2.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _suUserNameBox
            // 
            this._suUserNameBox.Location = new System.Drawing.Point(101, 21);
            this._suUserNameBox.Name = "_suUserNameBox";
            this._suUserNameBox.Size = new System.Drawing.Size(125, 19);
            this._suUserNameBox.TabIndex = 0;
            this._suUserNameBox.WaterMarkAlsoFocus = true;
            this._suUserNameBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._suUserNameBox.WaterMarkText = "";
            this._suUserNameBox.TextChanged += new System.EventHandler(this.EnableValidControls);
            this._suUserNameBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _suPasswordBox
            // 
            this._suPasswordBox.Enabled = false;
            this._suPasswordBox.Location = new System.Drawing.Point(101, 46);
            this._suPasswordBox.Name = "_suPasswordBox";
            this._suPasswordBox.Size = new System.Drawing.Size(125, 19);
            this._suPasswordBox.TabIndex = 1;
            this._suPasswordBox.UseSystemPasswordChar = true;
            this._suPasswordBox.WaterMarkAlsoFocus = true;
            this._suPasswordBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._suPasswordBox.WaterMarkText = "";
            this._suPasswordBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _suTypeRadio1
            // 
            this._suTypeRadio1.Location = new System.Drawing.Point(101, 71);
            this._suTypeRadio1.Name = "_suTypeRadio1";
            this._suTypeRadio1.Size = new System.Drawing.Size(58, 18);
            this._suTypeRadio1.TabIndex = 2;
            this._suTypeRadio1.TabStop = true;
            this._suTypeRadio1.Text = "_suTypeRadio1";
            this._suTypeRadio1.UseVisualStyleBackColor = true;
            this._suTypeRadio1.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _suTypeLabel
            // 
            this._suTypeLabel.AutoSize = true;
            this._suTypeLabel.Location = new System.Drawing.Point(6, 74);
            this._suTypeLabel.Name = "_suTypeLabel";
            this._suTypeLabel.Size = new System.Drawing.Size(73, 12);
            this._suTypeLabel.TabIndex = 4;
            this._suTypeLabel.Text = "_suTypeLabel";
            // 
            // _suUserNameLabel
            // 
            this._suUserNameLabel.AutoSize = true;
            this._suUserNameLabel.Location = new System.Drawing.Point(6, 24);
            this._suUserNameLabel.Name = "_suUserNameLabel";
            this._suUserNameLabel.Size = new System.Drawing.Size(101, 12);
            this._suUserNameLabel.TabIndex = 1;
            this._suUserNameLabel.Text = "_suUserNameLabel";
            // 
            // _suPasswordLabel
            // 
            this._suPasswordLabel.AutoSize = true;
            this._suPasswordLabel.Location = new System.Drawing.Point(6, 49);
            this._suPasswordLabel.Name = "_suPasswordLabel";
            this._suPasswordLabel.Size = new System.Drawing.Size(97, 12);
            this._suPasswordLabel.TabIndex = 2;
            this._suPasswordLabel.Text = "_suPasswordLabel";
            // 
            // _etcGroup
            // 
            this._etcGroup.Controls.Add(this._promptRecvTimeoutBox);
            this._etcGroup.Controls.Add(this._commandSendIntBox);
            this._etcGroup.Controls.Add(this._descriptionBox);
            this._etcGroup.Controls.Add(this._profileItemColorButton);
            this._etcGroup.Controls.Add(this._promptRecvTimeoutLabel);
            this._etcGroup.Controls.Add(this._commandSendIntLabel);
            this._etcGroup.Controls.Add(this._descriptionLabel);
            this._etcGroup.Controls.Add(this._profileItemColorLabel);
            this._etcGroup.Location = new System.Drawing.Point(269, 203);
            this._etcGroup.Name = "_etcGroup";
            this._etcGroup.Size = new System.Drawing.Size(250, 132);
            this._etcGroup.TabIndex = 6;
            this._etcGroup.TabStop = false;
            this._etcGroup.Text = "_etcGroup";
            // 
            // _promptRecvTimeoutBox
            // 
            this._promptRecvTimeoutBox.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._promptRecvTimeoutBox.Location = new System.Drawing.Point(188, 47);
            this._promptRecvTimeoutBox.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this._promptRecvTimeoutBox.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._promptRecvTimeoutBox.Name = "_promptRecvTimeoutBox";
            this._promptRecvTimeoutBox.Size = new System.Drawing.Size(56, 19);
            this._promptRecvTimeoutBox.TabIndex = 1;
            this._promptRecvTimeoutBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._promptRecvTimeoutBox.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this._promptRecvTimeoutBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _commandSendIntBox
            // 
            this._commandSendIntBox.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this._commandSendIntBox.Location = new System.Drawing.Point(188, 22);
            this._commandSendIntBox.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this._commandSendIntBox.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this._commandSendIntBox.Name = "_commandSendIntBox";
            this._commandSendIntBox.Size = new System.Drawing.Size(56, 19);
            this._commandSendIntBox.TabIndex = 0;
            this._commandSendIntBox.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._commandSendIntBox.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this._commandSendIntBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _descriptionBox
            // 
            this._descriptionBox.Location = new System.Drawing.Point(113, 101);
            this._descriptionBox.Name = "_descriptionBox";
            this._descriptionBox.Size = new System.Drawing.Size(131, 19);
            this._descriptionBox.TabIndex = 3;
            this._descriptionBox.WaterMarkAlsoFocus = true;
            this._descriptionBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._descriptionBox.WaterMarkText = "";
            this._descriptionBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _profileItemColorButton
            // 
            this._profileItemColorButton.BackColor = System.Drawing.SystemColors.Control;
            this._profileItemColorButton.Location = new System.Drawing.Point(113, 72);
            this._profileItemColorButton.Name = "_profileItemColorButton";
            this._profileItemColorButton.SelectedColor = System.Drawing.Color.Black;
            this._profileItemColorButton.Size = new System.Drawing.Size(131, 23);
            this._profileItemColorButton.TabIndex = 2;
            this._profileItemColorButton.UseVisualStyleBackColor = false;
            this._profileItemColorButton.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _promptRecvTimeoutLabel
            // 
            this._promptRecvTimeoutLabel.AutoSize = true;
            this._promptRecvTimeoutLabel.Location = new System.Drawing.Point(6, 49);
            this._promptRecvTimeoutLabel.Name = "_promptRecvTimeoutLabel";
            this._promptRecvTimeoutLabel.Size = new System.Drawing.Size(138, 12);
            this._promptRecvTimeoutLabel.TabIndex = 28;
            this._promptRecvTimeoutLabel.Text = "_promptRecvTimeoutLabel";
            // 
            // _commandSendIntLabel
            // 
            this._commandSendIntLabel.AutoSize = true;
            this._commandSendIntLabel.Location = new System.Drawing.Point(6, 24);
            this._commandSendIntLabel.Name = "_commandSendIntLabel";
            this._commandSendIntLabel.Size = new System.Drawing.Size(122, 12);
            this._commandSendIntLabel.TabIndex = 26;
            this._commandSendIntLabel.Text = "_commandSendIntLabel";
            // 
            // _descriptionLabel
            // 
            this._descriptionLabel.AutoSize = true;
            this._descriptionLabel.Location = new System.Drawing.Point(6, 104);
            this._descriptionLabel.Name = "_descriptionLabel";
            this._descriptionLabel.Size = new System.Drawing.Size(92, 12);
            this._descriptionLabel.TabIndex = 13;
            this._descriptionLabel.Text = "_descriptionLabel";
            // 
            // _profileItemColorLabel
            // 
            this._profileItemColorLabel.AutoSize = true;
            this._profileItemColorLabel.Location = new System.Drawing.Point(6, 77);
            this._profileItemColorLabel.Name = "_profileItemColorLabel";
            this._profileItemColorLabel.Size = new System.Drawing.Size(117, 12);
            this._profileItemColorLabel.TabIndex = 11;
            this._profileItemColorLabel.Text = "_profileItemColorLabel";
            // 
            // _terminalFontColorButton
            // 
            this._terminalFontColorButton.BackColor = System.Drawing.SystemColors.Control;
            this._terminalFontColorButton.Location = new System.Drawing.Point(109, 121);
            this._terminalFontColorButton.Name = "_terminalFontColorButton";
            this._terminalFontColorButton.SelectedColor = System.Drawing.Color.Black;
            this._terminalFontColorButton.Size = new System.Drawing.Size(131, 23);
            this._terminalFontColorButton.TabIndex = 4;
            this._terminalFontColorButton.UseVisualStyleBackColor = false;
            this._terminalFontColorButton.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _terminalFontColorLabel
            // 
            this._terminalFontColorLabel.AutoSize = true;
            this._terminalFontColorLabel.Location = new System.Drawing.Point(6, 126);
            this._terminalFontColorLabel.Name = "_terminalFontColorLabel";
            this._terminalFontColorLabel.Size = new System.Drawing.Size(127, 12);
            this._terminalFontColorLabel.TabIndex = 22;
            this._terminalFontColorLabel.Text = "_terminalFontColorLabel";
            // 
            // _terminalBGColorButton
            // 
            this._terminalBGColorButton.BackColor = System.Drawing.SystemColors.Control;
            this._terminalBGColorButton.Location = new System.Drawing.Point(109, 149);
            this._terminalBGColorButton.Name = "_terminalBGColorButton";
            this._terminalBGColorButton.SelectedColor = System.Drawing.Color.Black;
            this._terminalBGColorButton.Size = new System.Drawing.Size(131, 23);
            this._terminalBGColorButton.TabIndex = 5;
            this._terminalBGColorButton.UseVisualStyleBackColor = false;
            this._terminalBGColorButton.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _terminalBGColorLabel
            // 
            this._terminalBGColorLabel.AutoSize = true;
            this._terminalBGColorLabel.Location = new System.Drawing.Point(6, 154);
            this._terminalBGColorLabel.Name = "_terminalBGColorLabel";
            this._terminalBGColorLabel.Size = new System.Drawing.Size(120, 12);
            this._terminalBGColorLabel.TabIndex = 4;
            this._terminalBGColorLabel.Text = "_terminalBGColorLabel";
            // 
            // _execCommandBox
            // 
            this._execCommandBox.Location = new System.Drawing.Point(109, 94);
            this._execCommandBox.Name = "_execCommandBox";
            this._execCommandBox.Size = new System.Drawing.Size(131, 19);
            this._execCommandBox.TabIndex = 3;
            this._execCommandBox.WaterMarkAlsoFocus = true;
            this._execCommandBox.WaterMarkColor = System.Drawing.Color.Gray;
            this._execCommandBox.WaterMarkText = "";
            this._execCommandBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _execCommandLabel
            // 
            this._execCommandLabel.AutoSize = true;
            this._execCommandLabel.Location = new System.Drawing.Point(6, 97);
            this._execCommandLabel.Name = "_execCommandLabel";
            this._execCommandLabel.Size = new System.Drawing.Size(110, 12);
            this._execCommandLabel.TabIndex = 2;
            this._execCommandLabel.Text = "_execCommandLabel";
            // 
            // _charCodeLabel
            // 
            this._charCodeLabel.AutoSize = true;
            this._charCodeLabel.Location = new System.Drawing.Point(6, 24);
            this._charCodeLabel.Name = "_charCodeLabel";
            this._charCodeLabel.Size = new System.Drawing.Size(84, 12);
            this._charCodeLabel.TabIndex = 1;
            this._charCodeLabel.Text = "_charCodeLabel";
            // 
            // _charCodeBox
            // 
            this._charCodeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._charCodeBox.FormattingEnabled = true;
            this._charCodeBox.Location = new System.Drawing.Point(109, 21);
            this._charCodeBox.Name = "_charCodeBox";
            this._charCodeBox.Size = new System.Drawing.Size(131, 20);
            this._charCodeBox.TabIndex = 0;
            this._charCodeBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _terminalGroup
            // 
            this._terminalGroup.Controls.Add(this._terminalFontColorButton);
            this._terminalGroup.Controls.Add(this._telnetNewLineCheck);
            this._terminalGroup.Controls.Add(this._charCodeBox);
            this._terminalGroup.Controls.Add(this._terminalBGColorButton);
            this._terminalGroup.Controls.Add(this._terminalTypeBox);
            this._terminalGroup.Controls.Add(this._newLineTypeBox);
            this._terminalGroup.Controls.Add(this._terminalFontColorLabel);
            this._terminalGroup.Controls.Add(this._charCodeLabel);
            this._terminalGroup.Controls.Add(this._newLineTypeLabel);
            this._terminalGroup.Controls.Add(this._terminalBGColorLabel);
            this._terminalGroup.Controls.Add(this._terminalTypeLabel);
            this._terminalGroup.Location = new System.Drawing.Point(269, 12);
            this._terminalGroup.Name = "_terminalGroup";
            this._terminalGroup.Size = new System.Drawing.Size(250, 185);
            this._terminalGroup.TabIndex = 5;
            this._terminalGroup.TabStop = false;
            this._terminalGroup.Text = "_terminalGroup";
            // 
            // _telnetNewLineCheck
            // 
            this._telnetNewLineCheck.Location = new System.Drawing.Point(109, 73);
            this._telnetNewLineCheck.Name = "_telnetNewLineCheck";
            this._telnetNewLineCheck.Size = new System.Drawing.Size(131, 16);
            this._telnetNewLineCheck.TabIndex = 2;
            this._telnetNewLineCheck.Text = "_telnetNewLineCheck";
            this._telnetNewLineCheck.UseVisualStyleBackColor = true;
            this._telnetNewLineCheck.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _terminalTypeBox
            // 
            this._terminalTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._terminalTypeBox.FormattingEnabled = true;
            this._terminalTypeBox.Location = new System.Drawing.Point(109, 95);
            this._terminalTypeBox.Name = "_terminalTypeBox";
            this._terminalTypeBox.Size = new System.Drawing.Size(131, 20);
            this._terminalTypeBox.TabIndex = 3;
            this._terminalTypeBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _newLineTypeBox
            // 
            this._newLineTypeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._newLineTypeBox.FormattingEnabled = true;
            this._newLineTypeBox.Location = new System.Drawing.Point(109, 47);
            this._newLineTypeBox.Name = "_newLineTypeBox";
            this._newLineTypeBox.Size = new System.Drawing.Size(131, 20);
            this._newLineTypeBox.TabIndex = 1;
            this._newLineTypeBox.SelectedIndexChanged += new System.EventHandler(this.EnableValidControls);
            this._newLineTypeBox.Enter += new System.EventHandler(this.ShowHint);
            // 
            // _newLineTypeLabel
            // 
            this._newLineTypeLabel.AutoSize = true;
            this._newLineTypeLabel.Location = new System.Drawing.Point(6, 50);
            this._newLineTypeLabel.Name = "_newLineTypeLabel";
            this._newLineTypeLabel.Size = new System.Drawing.Size(102, 12);
            this._newLineTypeLabel.TabIndex = 18;
            this._newLineTypeLabel.Text = "_newLineTypeLabel";
            // 
            // _terminalTypeLabel
            // 
            this._terminalTypeLabel.AutoSize = true;
            this._terminalTypeLabel.Location = new System.Drawing.Point(6, 98);
            this._terminalTypeLabel.Name = "_terminalTypeLabel";
            this._terminalTypeLabel.Size = new System.Drawing.Size(102, 12);
            this._terminalTypeLabel.TabIndex = 20;
            this._terminalTypeLabel.Text = "_terminalTypeLabel";
            // 
            // _hintLabel
            // 
            this._hintLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._hintLabel.Location = new System.Drawing.Point(269, 338);
            this._hintLabel.Name = "_hintLabel";
            this._hintLabel.Size = new System.Drawing.Size(250, 150);
            this._hintLabel.TabIndex = 28;
            this._hintLabel.Text = "_hintLabel";
            // 
            // _autoLoginGroup
            // 
            this._autoLoginGroup.Controls.Add(this._execCommandBox);
            this._autoLoginGroup.Controls.Add(this._autoLoginCheck);
            this._autoLoginGroup.Controls.Add(this._passwordPromptBox);
            this._autoLoginGroup.Controls.Add(this._loginPromptBox);
            this._autoLoginGroup.Controls.Add(this._loginPromptLabel);
            this._autoLoginGroup.Controls.Add(this._suGroup);
            this._autoLoginGroup.Controls.Add(this._execCommandLabel);
            this._autoLoginGroup.Controls.Add(this._passwordPromptLabel);
            this._autoLoginGroup.Location = new System.Drawing.Point(12, 284);
            this._autoLoginGroup.Name = "_autoLoginGroup";
            this._autoLoginGroup.Size = new System.Drawing.Size(251, 230);
            this._autoLoginGroup.TabIndex = 3;
            this._autoLoginGroup.TabStop = false;
            this._autoLoginGroup.Text = "_autoLoginGroup";
            // 
            // _accountGroup
            // 
            this._accountGroup.Controls.Add(this._passwordBox);
            this._accountGroup.Controls.Add(this._userNameBox);
            this._accountGroup.Controls.Add(this._passwordLabel);
            this._accountGroup.Controls.Add(this._userNameLabel);
            this._accountGroup.Location = new System.Drawing.Point(12, 203);
            this._accountGroup.Name = "_accountGroup";
            this._accountGroup.Size = new System.Drawing.Size(251, 75);
            this._accountGroup.TabIndex = 2;
            this._accountGroup.TabStop = false;
            this._accountGroup.Text = "_accountGroup";
            // 
            // ProfileEditForm
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(527, 522);
            this.Controls.Add(this._accountGroup);
            this.Controls.Add(this._autoLoginGroup);
            this.Controls.Add(this._hintLabel);
            this.Controls.Add(this._terminalGroup);
            this.Controls.Add(this._etcGroup);
            this.Controls.Add(this._cancelButton);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._sshGroup);
            this.Controls.Add(this._basicGroup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileEditForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "ProfileEdit";
            this._basicGroup.ResumeLayout(false);
            this._basicGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._portBox)).EndInit();
            this._sshGroup.ResumeLayout(false);
            this._sshGroup.PerformLayout();
            this._suGroup.ResumeLayout(false);
            this._suGroup.PerformLayout();
            this._etcGroup.ResumeLayout(false);
            this._etcGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._promptRecvTimeoutBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._commandSendIntBox)).EndInit();
            this._terminalGroup.ResumeLayout(false);
            this._terminalGroup.PerformLayout();
            this._autoLoginGroup.ResumeLayout(false);
            this._autoLoginGroup.PerformLayout();
            this._accountGroup.ResumeLayout(false);
            this._accountGroup.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private Poderosa.UI.WaterMarkTextBox _hostNameBox;
        private System.Windows.Forms.Label _hostNameLabel;
        private System.Windows.Forms.Label _userNameLabel;
        private Poderosa.UI.WaterMarkTextBox _userNameBox;
        private System.Windows.Forms.Label _passwordLabel;
        private System.Windows.Forms.GroupBox _basicGroup;
        private Poderosa.UI.WaterMarkTextBox _passwordBox;
        private System.Windows.Forms.ComboBox _protocolBox;
        private System.Windows.Forms.Label _portLabel;
        private System.Windows.Forms.Label _protocolLabel;
        private System.Windows.Forms.GroupBox _sshGroup;
        private Poderosa.UI.WaterMarkTextBox _keyFileBox;
        private System.Windows.Forms.Button _openKeyFileButton;
        private System.Windows.Forms.ComboBox _authTypeBox;
        private System.Windows.Forms.Label _keyFileLabel;
        private System.Windows.Forms.Label _authTypeLabel;
        private System.Windows.Forms.Button _okButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.GroupBox _suGroup;
        private System.Windows.Forms.RadioButton _suTypeRadio2;
        private System.Windows.Forms.Label _suTypeLabel;
        private System.Windows.Forms.RadioButton _suTypeRadio1;
        private Poderosa.UI.WaterMarkTextBox _suUserNameBox;
        private System.Windows.Forms.Label _suUserNameLabel;
        private Poderosa.UI.WaterMarkTextBox _suPasswordBox;
        private System.Windows.Forms.Label _suPasswordLabel;
        private Poderosa.UI.WaterMarkTextBox _descriptionBox;
        private System.Windows.Forms.Label _descriptionLabel;
        private System.Windows.Forms.Label _profileItemColorLabel;
        private System.Windows.Forms.ComboBox _charCodeBox;
        private System.Windows.Forms.Label _terminalBGColorLabel;
        private System.Windows.Forms.Label _charCodeLabel;
        private Poderosa.UI.WaterMarkTextBox _execCommandBox;
        private System.Windows.Forms.Label _execCommandLabel;
        private UI.ColorButton _profileItemColorButton;
        private UI.ColorButton _terminalBGColorButton;
        private System.Windows.Forms.GroupBox _etcGroup;
        private UI.ColorButton _terminalFontColorButton;
        private System.Windows.Forms.Label _terminalFontColorLabel;
        private System.Windows.Forms.GroupBox _terminalGroup;
        private System.Windows.Forms.ComboBox _terminalTypeBox;
        private System.Windows.Forms.Label _terminalTypeLabel;
        private System.Windows.Forms.ComboBox _newLineTypeBox;
        private System.Windows.Forms.Label _newLineTypeLabel;
        private Poderosa.UI.WaterMarkTextBox _passwordPromptBox;
        private System.Windows.Forms.Label _passwordPromptLabel;
        private Poderosa.UI.WaterMarkTextBox _loginPromptBox;
        private System.Windows.Forms.Label _loginPromptLabel;
        private System.Windows.Forms.CheckBox _autoLoginCheck;
        private System.Windows.Forms.Label _hintLabel;
        private System.Windows.Forms.NumericUpDown _portBox;
        private System.Windows.Forms.NumericUpDown _promptRecvTimeoutBox;
        private System.Windows.Forms.Label _promptRecvTimeoutLabel;
        private System.Windows.Forms.NumericUpDown _commandSendIntBox;
        private System.Windows.Forms.Label _commandSendIntLabel;
        private System.Windows.Forms.CheckBox _telnetNewLineCheck;
        private System.Windows.Forms.GroupBox _autoLoginGroup;
        private System.Windows.Forms.GroupBox _accountGroup;
    }
}