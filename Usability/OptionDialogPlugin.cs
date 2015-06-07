/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: OptionDialogPlugin.cs,v 1.4 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Forms;
using Poderosa.Plugins;
using Poderosa.Preferences;
using Poderosa.Commands;

[assembly: PluginDeclaration(typeof(Poderosa.Usability.OptionDialogPlugin))]

namespace Poderosa.Usability {
    [PluginInfo(ID = "org.poderosa.optiondialog", Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.window;org.poderosa.core.commands")]
    internal class OptionDialogPlugin : PluginBase, ICultureChangeListener {
        public const string OPTION_PANEL_ID = "org.poderosa.optionpanel";
        private static OptionDialogPlugin _instance;
        private StringResource _stringResource;

        private ICoreServices _coreServices;
        private GeneralCommandImpl _optionDialogCommand;
        private GeneralCommandImpl _detailedPreferenceCommand;
        private IPoderosaMenuGroup _optionDialogMenuGroup;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);

            _instance = this;
            _stringResource = new StringResource("Poderosa.Usability.strings", typeof(OptionDialogPlugin).Assembly);
            poderosa.Culture.AddChangeListener(this);
            IPluginManager pm = poderosa.PluginManager;
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));

            IExtensionPoint panel_ext = pm.CreateExtensionPoint(OPTION_PANEL_ID, typeof(IOptionPanelExtension), this);

            ICommandCategory dialogs = _coreServices.CommandManager.CommandCategories.Dialogs;
            _optionDialogCommand = new GeneralCommandImpl("org.poderosa.optiondialog.open", _stringResource, "Command.OptionDialog", dialogs, new ExecuteDelegate(OptionDialogCommand.OpenOptionDialog)).SetDefaultShortcutKey(Keys.Alt | Keys.Control | Keys.T);
            _detailedPreferenceCommand = new GeneralCommandImpl("org.poderosa.preferenceeditor.open", _stringResource, "Command.PreferenceEditor", dialogs, new ExecuteDelegate(OptionDialogCommand.OpenPreferenceEditor));

            IExtensionPoint toolmenu = pm.FindExtensionPoint("org.poderosa.menu.tool");
            _optionDialogMenuGroup = new PoderosaMenuGroupImpl(new IPoderosaMenuItem[] {
                new PoderosaMenuItemImpl(_optionDialogCommand, _stringResource, "Menu.OptionDialog"),
                new PoderosaMenuItemImpl(_detailedPreferenceCommand, _stringResource, "Menu.PreferenceEditor") }).SetPosition(PositionType.Last, null);

            toolmenu.RegisterExtension(_optionDialogMenuGroup);

            //基本のオプションパネルを登録
            panel_ext.RegisterExtension(new DisplayOptionPanelExtension());
            panel_ext.RegisterExtension(new TerminalOptionPanelExtension());
            panel_ext.RegisterExtension(new PeripheralOptionPanelExtension());
            panel_ext.RegisterExtension(new CommandOptionPanelExtension());
            panel_ext.RegisterExtension(new SSHOptionPanelExtension());
            panel_ext.RegisterExtension(new ConnectionOptionPanelExtension());
            panel_ext.RegisterExtension(new GenericOptionPanelExtension());
        }

        public static OptionDialogPlugin Instance {
            get {
                return _instance;
            }
        }

        public StringResource Strings {
            get {
                return _stringResource;
            }
        }
        public ICommandManager CommandManager {
            get {
                return _coreServices.CommandManager;
            }
        }
        public IPreferences RootPreferences {
            get {
                return _coreServices.Preferences;
            }
        }
        public IPoderosaMenuGroup OptionDialogMenuGroup {
            get {
                return _optionDialogMenuGroup;
            }
        }
        public ICoreServices CoreServices {
            get {
                return _coreServices;
            }
        }

        public void OnCultureChanged(System.Globalization.CultureInfo newculture) {
            _stringResource.OnCultureChanged(newculture);
            //さらに、キャッシュしているオプションパネルをクリアする
            IOptionPanelExtension[] es = (IOptionPanelExtension[])_poderosaWorld.PluginManager.FindExtensionPoint(OPTION_PANEL_ID).GetExtensions();
            foreach (IOptionPanelExtension e in es)
                e.Dispose();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IOptionPanelExtension {
        string Caption {
            get;
        }
        Image Icon {
            get;
        }
        string[] PreferenceFolderIDsToEdit {
            get;
        }
        Control ContentPanel {
            get;
        } // BorderStyleが設定できるもの(UserControl or Panel)
        void InitiUI(IPreferenceFolder[] values); //コントロールは、Disposeされるまでは再利用可
        bool Commit(IPreferenceFolder[] values);
        void Dispose();
    }

    //基本実装
    internal abstract class OptionPanelExtensionBase : IOptionPanelExtension {
        private string _captionID;
        private int _iconIndex;

        public OptionPanelExtensionBase(string captionID, int iconIndex) {
            _captionID = captionID;
            _iconIndex = iconIndex;
        }

        public string Caption {
            get {
                return OptionDialogPlugin.Instance.Strings.GetString(_captionID);
            }
        }

        public Image Icon {
            get {
                return OptionDialog.Instance.GetPanelIcon(_iconIndex);
            }
        }

        protected Control CreateScrollablePanel(Control innerControl) {
            Panel panel = new Panel();
            panel.AutoScroll = true;
            innerControl.Location = new Point(0, 0);
            innerControl.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            panel.Controls.Add(innerControl);
            return panel;
        }

        public abstract string[] PreferenceFolderIDsToEdit {
            get;
        }
        public abstract Control ContentPanel {
            get;
        }
        public abstract void InitiUI(IPreferenceFolder[] values);
        public abstract bool Commit(IPreferenceFolder[] values);
        public abstract void Dispose();
    }


    //オプションダイアログと詳細Preferenceを開くコマンドとメニュー
    internal class OptionDialogCommand {

        public static CommandResult OpenOptionDialog(ICommandTarget target) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            OptionDialog dlg = new OptionDialog();
            if (dlg.ShowDialog(window.AsForm()) == DialogResult.OK) {
                return CommandResult.Succeeded;
            }
            else
                return CommandResult.Cancelled;
        }
        public static CommandResult OpenPreferenceEditor(ICommandTarget target) {
            IPoderosaMainWindow window = CommandTargetUtil.AsWindow(target);
            PreferenceEditor dlg = new PreferenceEditor(OptionDialogPlugin.Instance.CoreServices.Preferences);
            if (dlg.ShowDialog(window.AsForm()) == DialogResult.OK) {
                return CommandResult.Succeeded;
            }
            else
                return CommandResult.Cancelled;
        }
    }


}
