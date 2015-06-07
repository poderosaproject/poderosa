/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.

 $Id: MainForm.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
*/
using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Net.Sockets;

namespace Poderosa.PortForwarding {
    /// <summary>
    /// MainForm の概要の説明です。
    /// </summary>
    internal class MainForm : System.Windows.Forms.Form {
        private Hashtable _menuMap;

        private System.Windows.Forms.ListView _list;
        private System.Windows.Forms.ColumnHeader _sshHostColumn;
        private System.Windows.Forms.ColumnHeader _accountColumn;
        private System.Windows.Forms.ColumnHeader _typeColumn;
        private System.Windows.Forms.ColumnHeader _destinationHostColumn;
        private System.Windows.Forms.ColumnHeader _destinationPortColumn;
        private System.Windows.Forms.ColumnHeader _listenPortColumn;
        private System.Windows.Forms.ColumnHeader _statusColumn;
        private System.Windows.Forms.MenuStrip _mainMenu;

        private ToolStripMenuItem _menuFile;
        private ToolStripMenuItem _menuNewProfile;
        private ToolStripMenuItem _menuTaskTray;
        private ToolStripMenuItem _menuExit;
        private ToolStripMenuItem _menuProfile;
        private ToolStripMenuItem _menuProfileProperty;
        private ToolStripMenuItem _menuProfileRemove;
        private ToolStripMenuItem _menuProfileUp;
        private ToolStripMenuItem _menuProfileDown;
        private ToolStripMenuItem _menuProfileConnect;
        private ToolStripMenuItem _menuProfileDisconnect;
        private ToolStripMenuItem _menuAllProfile;
        private ToolStripMenuItem _menuAllProfileConnect;
        private ToolStripMenuItem _menuAllProfileDisconnect;
        private ToolStripMenuItem _menuTool;
        private ToolStripMenuItem _menuOption;
        private ToolStripMenuItem _menuHelp;
        private ToolStripMenuItem _menuAboutBox;
        private NotifyIcon _taskTrayIcon;
        private ContextMenuStrip _listViewContextMenu;
        private ContextMenuStrip _iconContextMenu;
        private System.ComponentModel.IContainer components;

        public MainForm() {
            //
            // Windows フォーム デザイナ サポートに必要です。
            //
            InitializeComponent();
            InitializeText();

            //
            // TODO: InitializeComponent 呼び出しの後に、コンストラクタ コードを追加してください。
            //
            InitMenuShortcut();

            InitContextMenu();
        }

        /// <summary>
        /// 使用されているリソースに後処理を実行します。
        /// </summary>
        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (components != null) {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード
        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MainForm));
            this._list = new System.Windows.Forms.ListView();
            this._sshHostColumn = new System.Windows.Forms.ColumnHeader();
            this._accountColumn = new System.Windows.Forms.ColumnHeader();
            this._typeColumn = new System.Windows.Forms.ColumnHeader();
            this._listenPortColumn = new System.Windows.Forms.ColumnHeader();
            this._destinationHostColumn = new System.Windows.Forms.ColumnHeader();
            this._destinationPortColumn = new System.Windows.Forms.ColumnHeader();
            this._statusColumn = new System.Windows.Forms.ColumnHeader();
            this._mainMenu = new MenuStrip();
            this._menuFile = new ToolStripMenuItem();
            this._menuNewProfile = new ToolStripMenuItem();
            this._menuTaskTray = new ToolStripMenuItem();
            this._menuExit = new ToolStripMenuItem();
            this._menuProfile = new ToolStripMenuItem();
            this._menuProfileProperty = new ToolStripMenuItem();
            this._menuProfileRemove = new ToolStripMenuItem();
            this._menuProfileUp = new ToolStripMenuItem();
            this._menuProfileDown = new ToolStripMenuItem();
            this._menuProfileConnect = new ToolStripMenuItem();
            this._menuProfileDisconnect = new ToolStripMenuItem();
            this._menuAllProfile = new ToolStripMenuItem();
            this._menuAllProfileConnect = new ToolStripMenuItem();
            this._menuAllProfileDisconnect = new ToolStripMenuItem();
            this._menuTool = new ToolStripMenuItem();
            this._menuOption = new ToolStripMenuItem();
            this._menuHelp = new ToolStripMenuItem();
            this._menuAboutBox = new ToolStripMenuItem();
            this._taskTrayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.SuspendLayout();
            // 
            // _list
            // 
            this._list.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                this._sshHostColumn,
                this._accountColumn,
                this._typeColumn,
                this._listenPortColumn,
                this._destinationHostColumn,
                this._destinationPortColumn,
                this._statusColumn});
            this._list.Dock = System.Windows.Forms.DockStyle.Fill;
            this._list.FullRowSelect = true;
            this._list.GridLines = true;
            this._list.Location = new System.Drawing.Point(0, 0);
            this._list.MultiSelect = false;
            this._list.Name = "_list";
            this._list.Size = new System.Drawing.Size(504, 345);
            this._list.TabIndex = 0;
            this._list.View = System.Windows.Forms.View.Details;
            this._list.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnListViewKeyPress);
            this._list.DoubleClick += new System.EventHandler(this.OnListViewDoubleClicked);
            this._list.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnListViewMouseUp);
            this._list.SelectedIndexChanged += new System.EventHandler(this.OnSelectedIndexChanged);
            // 
            // _sshHostColumn
            // 
            this._sshHostColumn.Width = 69;
            // 
            // _accountColumn
            // 
            // 
            // _typeColumn
            // 
            // 
            // _listenPortColumn
            // 
            this._listenPortColumn.Width = 75;
            // 
            // _destinationHostColumn
            // 
            this._destinationHostColumn.Width = 100;
            // 
            // _destinationPortColumn
            // 
            this._destinationPortColumn.Width = 75;
            // 
            // _statusColumn
            // 
            // 
            // _mainMenu
            // 
            this._mainMenu.Items.AddRange(new ToolStripMenuItem[] {
                this._menuFile,
                this._menuProfile,
                this._menuAllProfile,
                this._menuTool,
                this._menuHelp});
            // 
            // _menuFile
            // 
            this._menuFile.DropDownItems.AddRange(new ToolStripItem[] {
                this._menuNewProfile,
                new ToolStripSeparator(),
                this._menuTaskTray,
                this._menuExit});
            // 
            // _menuNewProfile
            // 
            this._menuNewProfile.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuTaskTray
            // 
            this._menuTaskTray.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuExit
            // 
            this._menuExit.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuProfile
            // 
            this._menuProfile.DropDownItems.AddRange(new ToolStripItem[] {
                this._menuProfileProperty,
                this._menuProfileRemove,
                new ToolStripSeparator(),
                this._menuProfileUp,
                this._menuProfileDown,
                new ToolStripSeparator(),
                this._menuProfileConnect,
                this._menuProfileDisconnect});
            this._menuProfile.DropDownOpening += new System.EventHandler(this.OnProfileMenuClicked);
            // 
            // _menuProfileProperty
            // 
            this._menuProfileProperty.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuProfileRemove
            // 
            this._menuProfileRemove.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuProfileUp
            // 
            this._menuProfileUp.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuProfileDown
            // 
            this._menuProfileDown.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuProfileConnect
            // 
            this._menuProfileConnect.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuProfileDisconnect
            // 
            this._menuProfileDisconnect.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuAllProfile
            // 
            this._menuAllProfile.DropDownItems.AddRange(new ToolStripMenuItem[] {
                this._menuAllProfileConnect,
                this._menuAllProfileDisconnect});
            // 
            // _menuAllProfileConnect
            // 
            this._menuAllProfileConnect.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuAllProfileDisconnect
            // 
            this._menuAllProfileDisconnect.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuTool
            // 
            this._menuTool.DropDownItems.AddRange(new ToolStripMenuItem[] {
                this._menuOption});
            // 
            // _menuOption
            // 
            this._menuOption.Click += new System.EventHandler(this.OnMenu);
            // 
            // _menuHelp
            // 
            this._menuHelp.DropDownItems.AddRange(new ToolStripMenuItem[] {
                this._menuAboutBox});
            // 
            // _menuAboutBox
            // 
            this._menuAboutBox.Click += new System.EventHandler(this.OnMenu);
            // 
            // _taskTrayIcon
            // 
            this._taskTrayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("_taskTrayIcon.Icon")));
            this._taskTrayIcon.Text = "Poderosa SSH Portforwarding Gateway";
            this._taskTrayIcon.Visible = true;
            this._taskTrayIcon.DoubleClick += new System.EventHandler(this.OnTaskTrayIconDoubleClicked);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 12);
            this.ClientSize = new System.Drawing.Size(504, 345);
            this.Controls.Add(this._list);
            this.Controls.Add(_mainMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MainMenuStrip = this._mainMenu;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = Env.WindowTitle;
            this.ResumeLayout(false);

        }
        #endregion

        private void InitializeText() {
            this._sshHostColumn.Text = Env.Strings.GetString("Form.MainForm._sshHostColumn");
            this._accountColumn.Text = Env.Strings.GetString("Form.MainForm._accountColumn");
            this._typeColumn.Text = Env.Strings.GetString("Form.MainForm._typeColumn");
            this._listenPortColumn.Text = Env.Strings.GetString("Form.MainForm._listenPortColumn");
            this._destinationHostColumn.Text = Env.Strings.GetString("Form.MainForm._destinationHostColumn");
            this._destinationPortColumn.Text = Env.Strings.GetString("Form.MainForm._destinationPortColumn");
            this._statusColumn.Text = Env.Strings.GetString("Form.MainForm._statusColumn");
            this._menuFile.Text = Env.Strings.GetString("Menu._menuFile");
            this._menuNewProfile.Text = Env.Strings.GetString("Menu._menuNewProfile");
            this._menuTaskTray.Text = Env.Strings.GetString("Menu._menuTaskTray");
            this._menuExit.Text = Env.Strings.GetString("Menu._menuExit");
            this._menuProfile.Text = Env.Strings.GetString("Menu._menuProfile");
            this._menuProfileProperty.Text = Env.Strings.GetString("Menu._menuProfileProperty");
            this._menuProfileRemove.Text = Env.Strings.GetString("Menu._menuProfileRemove");
            this._menuProfileUp.Text = Env.Strings.GetString("Menu._menuProfileUp");
            this._menuProfileDown.Text = Env.Strings.GetString("Menu._menuProfileDown");
            this._menuProfileConnect.Text = Env.Strings.GetString("Menu._menuProfileConnect");
            this._menuProfileDisconnect.Text = Env.Strings.GetString("Menu._menuProfileDisconnect");
            this._menuAllProfile.Text = Env.Strings.GetString("Menu._menuAllProfile");
            this._menuAllProfileConnect.Text = Env.Strings.GetString("Menu._menuAllProfileConnect");
            this._menuAllProfileDisconnect.Text = Env.Strings.GetString("Menu._menuAllProfileDisconnect");
            this._menuTool.Text = Env.Strings.GetString("Menu._menuTool");
            this._menuOption.Text = Env.Strings.GetString("Menu._menuOption");
            this._menuHelp.Text = Env.Strings.GetString("Menu._menuHelp");
            this._menuAboutBox.Text = Env.Strings.GetString("Menu._menuAboutBox");
        }
        public void ReloadLanguage() {
            InitializeText();
            /*
            //こうすることでメニュー幅が調整される
            MainMenu mm = new MainMenu();
            while (_mainMenu.Items.Count > 0) {
                mm.Items.Add(_mainMenu.Items[0]);
            }
            _mainMenu = mm;
            this.Menu = mm;
             */
            InitContextMenu();
            RefreshAllProfiles();
        }

        private void InitMenuShortcut() {
            _menuNewProfile.ShortcutKeys = (Keys.Control | Keys.N);
            _menuProfileUp.ShortcutKeys = (Keys.Control | Keys.K);
            _menuProfileDown.ShortcutKeys = (Keys.Control | Keys.J);
            _menuAllProfileConnect.ShortcutKeys = (Keys.Control | Keys.A);
            _menuAllProfileDisconnect.ShortcutKeys = (Keys.Control | Keys.D);
            _menuOption.ShortcutKeys = (Keys.Control | Keys.T);
        }

        private void InitContextMenu() {
            _menuMap = new Hashtable();
            _listViewContextMenu = new ContextMenuStrip();
            foreach (ToolStripItem item in _menuProfile.DropDownItems) {
                ToolStripMenuItem mi = item as ToolStripMenuItem;
                if (mi != null) {
                    ToolStripMenuItem cloned = new ToolStripMenuItem();
                    cloned.Text = mi.Text;
                    cloned.Click += new EventHandler(OnMenu);
                    _menuMap.Add(cloned, mi);
                    _listViewContextMenu.Items.Add(cloned);
                }
                else if (item is ToolStripSeparator)
                    _listViewContextMenu.Items.Add(new ToolStripSeparator());
            }

            _iconContextMenu = new ContextMenuStrip();
            AddIconContextMenu(_menuAllProfileConnect, Env.Strings.GetString("Menu._menuAllProfileConnect"));
            AddIconContextMenu(_menuAllProfileDisconnect, Env.Strings.GetString("Menu._menuAllProfileDisconnect"));
            _iconContextMenu.Items.Add(new ToolStripSeparator());
            AddIconContextMenu(_menuExit, Env.Strings.GetString("Menu._menuExit"));
            _taskTrayIcon.ContextMenuStrip = _iconContextMenu;
        }
        private void AddIconContextMenu(ToolStripMenuItem basemenu, string text) {
            ToolStripMenuItem mi = new ToolStripMenuItem();
            mi.Text = text;
            mi.Click += new EventHandler(OnMenu);
            _iconContextMenu.Items.Add(mi);
            _menuMap.Add(mi, basemenu);
        }

        private void OnTaskTrayIconDoubleClicked(object sender, EventArgs args) {
            if (this.WindowState == FormWindowState.Minimized) {
                this.Visible = true;
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = Env.Options.ShowInTaskBar;
            }
            this.Activate();
        }
        private void OnListViewDoubleClicked(object sender, EventArgs args) {
            ChannelProfile prof = GetSelectedProfile();
            if (prof == null)
                return;

            Env.Commands.ConnectProfile(prof);
        }
        private void OnSelectedIndexChanged(object sender, EventArgs args) {
            if (GetSelectedProfile() != null)
                _menuProfile.Enabled = true;
        }
        private void OnListViewKeyPress(object sender, KeyPressEventArgs args) {
            if (args.KeyChar == '\r') {
                ChannelProfile prof = GetSelectedProfile();
                if (prof != null) {
                    if (Control.ModifierKeys == Keys.Control)
                        Env.Commands.EditProfile(prof);
                    else if (!Env.Connections.IsConnected(prof))
                        Env.Commands.ConnectProfile(prof);
                }
            }
        }
        private void OnListViewMouseUp(object sender, MouseEventArgs args) {
            ChannelProfile prof = GetSelectedProfile();
            if (args.Button != MouseButtons.Right || prof == null)
                return;

            AdjustMenu(_listViewContextMenu.Items, prof);
            _listViewContextMenu.Show(this, new Point(args.X, args.Y));
        }
        private void OnProfileMenuClicked(object sender, EventArgs args) {
            AdjustMenu(_menuProfile.DropDownItems, GetSelectedProfile());
        }


        private void OnMenu(object sender, EventArgs args) {
            //MenuMapにあれば変換。これでContext Menuを処理
            object t = _menuMap[sender];
            if (t != null)
                sender = t;

            Commands cmd = Env.Commands;
            if (sender == _menuNewProfile)
                cmd.CreateNewProfile();
            else if (sender == _menuTaskTray) {
                this.WindowState = FormWindowState.Minimized;
                this.Visible = false;
                this.ShowInTaskbar = false;
            }
            else if (sender == _menuExit)
                this.Close();
            else if (sender == _menuProfileProperty)
                cmd.EditProfile(GetSelectedProfile());
            else if (sender == _menuProfileRemove)
                cmd.RemoveProfile(GetSelectedProfile());
            else if (sender == _menuProfileUp)
                cmd.MoveProfileUp(GetSelectedProfile());
            else if (sender == _menuProfileDown)
                cmd.MoveProfileDown(GetSelectedProfile());
            else if (sender == _menuProfileConnect)
                cmd.ConnectProfile(GetSelectedProfile());
            else if (sender == _menuProfileDisconnect)
                cmd.DisconnectProfile(GetSelectedProfile());
            else if (sender == _menuAllProfileConnect)
                cmd.ConnectAllProfiles();
            else if (sender == _menuAllProfileDisconnect)
                cmd.DisconnectAllProfiles();
            else if (sender == _menuOption)
                cmd.ShowOptionDialog();
            else if (sender == _menuAboutBox)
                cmd.ShowAboutBox();
            else
                Debug.WriteLine("not implemented!");
        }

        protected override void OnClosing(CancelEventArgs e) {
            if (Env.Connections.HasConnection && Env.Options.WarningOnExit && Util.AskUserYesNo(this, Env.Strings.GetString("Message.MainForm.AskDisconnect")) == DialogResult.No)
                e.Cancel = true;
            else {
                Env.Connections.CloseAll();
                Env.Options.FrameState = this.WindowState;
                Env.Options.FramePosition = this.DesktopBounds;
            }
            base.OnClosing(e);
        }



        public void RefreshAllProfiles() {
            _list.Items.Clear();
            foreach (ChannelProfile prof in Env.Profiles) {
                string port_postfix = prof.ProtocolType == ProtocolType.Udp ? "(UDP)" : "";
                ListViewItem li = new ListViewItem();
                li.Text = prof.SSHHost;
                li.SubItems.Add(prof.SSHAccount);
                li.SubItems.Add(Util.GetProfileTypeString(prof));
                li.SubItems.Add(prof.ListenPort.ToString() + port_postfix);
                li.SubItems.Add(prof.DestinationHost);
                li.SubItems.Add(prof.DestinationPort.ToString() + port_postfix);
                li.SubItems.Add(Util.GetProfileStatusString(prof));

                li.Tag = prof;
                _list.Items.Add(li);
            }

            _menuAllProfile.Enabled = _list.Items.Count > 0;
            _menuProfile.Enabled = _list.Items.Count > 0;

        }

        public void RefreshProfileStatus(ChannelProfile prof) {
            ListViewItem li = FindItem(prof);
            Debug.Assert(li != null);
            li.SubItems[6].Text = Util.GetProfileStatusString(prof);
        }

        private ListViewItem FindItem(ChannelProfile prof) {
            foreach (ListViewItem li in _list.Items) {
                if (li.Tag == prof)
                    return li;
            }
            return null;
        }

        //コンテキストメニューと双方で使用する
        private void AdjustMenu(ToolStripItemCollection items, ChannelProfile prof) {
            bool connected = prof != null && Env.Connections.IsConnected(prof);

            ConvertMenuItem(items, _menuProfileConnect).Enabled = prof != null && !connected;
            ConvertMenuItem(items, _menuProfileDisconnect).Enabled = prof != null && connected;
            ConvertMenuItem(items, _menuProfileRemove).Enabled = prof != null && !connected;
            ConvertMenuItem(items, _menuProfileProperty).Enabled = prof != null && !connected;

            int index = prof == null ? -1 : Env.Profiles.IndexOf(prof);
            ConvertMenuItem(items, _menuProfileUp).Enabled = prof != null && index > 0;
            ConvertMenuItem(items, _menuProfileDown).Enabled = prof != null && index < Env.Profiles.Count - 1;
        }
        private ToolStripMenuItem ConvertMenuItem(ToolStripItemCollection col, ToolStripMenuItem baseitem) {
            return (ToolStripMenuItem)col[_menuProfile.DropDownItems.IndexOf(baseitem)];
        }


        public ChannelProfile GetSelectedProfile() {
            if (_list.SelectedItems.Count == 0)
                return null;
            return (ChannelProfile)_list.SelectedItems[0].Tag;
        }
        public int GetSelectedIndex() {
            ListView.SelectedIndexCollection indices = _list.SelectedIndices;
            return indices.Count > 0 ? indices[0] : -1;
        }
        public void SetSelectedIndex(int index) {
            _list.Items[index].Selected = true;
        }

        protected override void OnActivated(EventArgs e) {
            base.OnActivated(e);
            //外部からShowWindowでアクティブにするとなぜかリストビューがおかしくなる
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = Env.Options.ShowInTaskBar;
        }

        //別スレッドからの警告
        public void ShowError(string msg) {
            Util.Warning(this, msg);
        }

    }
}
