/*
 * Copyright 2011 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SFTPToolbar.cs,v 1.1 2011/11/30 22:53:08 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

using Granados;
using Granados.SSH2;
using Granados.Poderosa.SFTP;

using Poderosa.Forms;
using Poderosa.Commands;
using Poderosa.Sessions;
using Poderosa.Util.Collections;
using Poderosa.Terminal;
using Poderosa.Protocols;
using Poderosa.SFTP.Properties;
using Granados.Poderosa.SCP;
using System.Windows.Forms;

namespace Poderosa.SFTP {

    /// <summary>
    /// SFTP Toolbar. This class also has menu group.
    /// </summary>
    internal class SFTPToolbar : IToolBarComponent, IPositionDesignation, IActiveDocumentChangeListener {

        private readonly IToolBarElement[] _toolbarElements;

        private readonly IPoderosaMenuGroup _menuGroup;

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPToolbar() {
            SFTPCommand sftpCommand = new SFTPCommand();
            SCPCommand scpCommand = new SCPCommand();

            _toolbarElements = new IToolBarElement[] {
                new SFTPToolbarCommandButton(sftpCommand),
                new SCPToolbarCommandButton(scpCommand)
            };
            _menuGroup = new PoderosaMenuGroupImpl(
                new IPoderosaMenu[] { sftpCommand, scpCommand });
        }

        #endregion

        #region Properties

        public IPoderosaMenuGroup MenuGroup {
            get {
                return _menuGroup;
            }
        }

        #endregion

        #region IToolBarComponent

        public IToolBarElement[] ToolBarElements {
            get {
                return _toolbarElements;
            }
        }

        #endregion

        #region IPositionDesignation

        public IAdaptable DesignationTarget {
            get {
                return null;
            }
        }

        public PositionType DesignationPosition {
            get {
                return PositionType.Last;
            }
        }

        #endregion

        #region IActiveDocumentChangeListener

        public void OnDocumentActivated(IPoderosaMainWindow window, IPoderosaDocument document) {
            if (window != null && window.ToolBar != null)
                window.ToolBar.RefreshComponent(this);
        }

        public void OnDocumentDeactivated(IPoderosaMainWindow window) {
            if (window != null && window.ToolBar != null)
                window.ToolBar.RefreshComponent(this);
        }

        #endregion

        #region IAdaptable

        public IAdaptable GetAdapter(Type adapter) {
            return SFTPPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion
    }

    /// <summary>
    /// SFTP toolbar button
    /// </summary>
    internal class SFTPToolbarCommandButton : ToolBarCommandButtonImpl {

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPToolbarCommandButton(SFTPCommand command)
            : base(command, Resources.IconSFTP16x16) {
        }

        #endregion

        #region Method overrides

        /// <summary>
        /// Gets the tooltip text
        /// </summary>
        public override string ToolTipText {
            get {
                return SFTPPlugin.Instance.StringResource.GetString("SFTPToolbarCommandButton.ToolTip");
            }
        }

        #endregion
    }

    /// <summary>
    /// SCP toolbar button
    /// </summary>
    internal class SCPToolbarCommandButton : ToolBarCommandButtonImpl {

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SCPToolbarCommandButton(SCPCommand command)
            : base(command, Resources.IconSCP16x16) {
        }

        #endregion

        #region Method overrides

        /// <summary>
        /// Gets the tooltip text
        /// </summary>
        public override string ToolTipText {
            get {
                return SFTPPlugin.Instance.StringResource.GetString("SCPToolbarCommandButton.ToolTip");
            }
        }

        #endregion
    }

    /// <summary>
    /// Command base class
    /// </summary>
    internal abstract class CommandBase {

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandBase() {
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Gets SSHConnection from the target.
        /// Returns null if current terminal doesn't have SSH connection.
        /// </summary>
        /// <param name="target">Target object which has been passed to the IPoderosaCommand's method.</param>
        /// <returns>A SSH connection object corresponding with current terminal. Null if it cannot be found.</returns>
        protected SSHConnection GetSSHConnection(ICommandTarget target) {
            ITerminalConnection connection = GetTerminalConnection(target);
            // Implementation classes for SSH connection are internal classes in another assembly.
            // We need to use reflection to get an instance of SSHChannel.
            if (connection != null && connection.Socket != null) {
                Type socketType = connection.Socket.GetType();
                PropertyInfo prop = socketType.GetProperty("Channel", BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty);
                if (prop != null && prop.CanRead) {
                    SSHChannel channel = prop.GetValue(connection.Socket, null) as SSHChannel;
                    if (channel != null) {
                        return channel.Connection;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets name of the current terminal.
        /// </summary>
        /// <param name="target">Target object which has been passed to the IPoderosaCommand's method.</param>
        /// <returns>Name of the current terminal. Null if it is not available.</returns>
        protected string GetTerminalName(ICommandTarget target) {
            IPoderosaDocument document = GetTerminalDocument(target);
            if (document != null)
                return document.Caption;
            else
                return null;
        }

        /// <summary>
        /// Gets System.Windows.Forms.Form of the target.
        /// </summary>
        /// <param name="target">Target object which has been passed to the IPoderosaCommand's method.</param>
        /// <returns>Form object if it is available. Otherwise null.</returns>
        protected Form GetForm(ICommandTarget target) {
            IPoderosaMainWindow window = (IPoderosaMainWindow)target.GetAdapter(typeof(IPoderosaMainWindow));
            if (window != null)
                return window.AsForm();
            else
                return null;
        }

        /// <summary>
        /// Check if current terminal is accepted.
        /// </summary>
        /// <param name="target">Target object which has been passed to the IPoderosaCommand's method.</param>
        /// <param name="acceptSSH1">Whether SSH1 connection is accepted.</param>
        /// <param name="acceptSSH2">Whether SSH2 connection is accepted.</param>
        /// <returns>True if current terminal is accepted.</returns>
        protected bool IsAcceptable(ICommandTarget target, bool acceptSSH1, bool acceptSSH2) {
            // Getting a connection object using GetSSHConnection() may be heavy.
            // We check a connection parameter.
            ITerminalConnection connection = GetTerminalConnection(target);
            if (connection != null && connection.Destination != null) {
                ISSHLoginParameter param = (ISSHLoginParameter)connection.Destination.GetAdapter(typeof(ISSHLoginParameter));
                if (param != null) {
                    switch (param.Method) {
                        case SSHProtocol.SSH1:
                            return acceptSSH1;
                        case SSHProtocol.SSH2:
                            return acceptSSH2;
                    }
                }
            }

            return false;
        }

        protected bool ConfirmToUse(Form ownerForm, string feature) {
            string messageFormat = SFTPPlugin.Instance.StringResource.GetString("Common.WarnExperimentalFormat");
            string message = String.Format(messageFormat, feature);
            DialogResult result = GUtil.AskUserYesNo(ownerForm, message, MessageBoxIcon.Warning);
            return (result == DialogResult.Yes);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Get ITerminalConnection object from the target.
        /// </summary>
        /// <param name="target">Target object which has been passed to the IPoderosaCommand's method.</param>
        /// <returns>A ITerminalConnection object corresponding with current terminal. Null if it cannot be found.</returns>
        private ITerminalConnection GetTerminalConnection(ICommandTarget target) {
            IPoderosaDocument document = GetTerminalDocument(target);
            if (document != null && document.OwnerSession != null) {
                ITerminalControlHost terminalControlHost =
                    (ITerminalControlHost)document.OwnerSession.GetAdapter(typeof(ITerminalControlHost));
                if (terminalControlHost != null) {
                    return terminalControlHost.TerminalConnection;
                }
            }

            return null;
        }

        /// <summary>
        /// Get ITerminalConnection object from the target.
        /// </summary>
        /// <param name="target">Target object which has been passed to the IPoderosaCommand's method.</param>
        /// <returns>A ITerminalConnection object corresponding with current terminal. Null if it cannot be found.</returns>
        private IPoderosaDocument GetTerminalDocument(ICommandTarget target) {
            IPoderosaMainWindow window = (IPoderosaMainWindow)target.GetAdapter(typeof(IPoderosaMainWindow));
            if (window != null && window.LastActivatedView != null)
                return window.LastActivatedView.Document;
            else
                return null;
        }

        #endregion

    }

    /// <summary>
    /// SFTP command
    /// </summary>
    internal class SFTPCommand : CommandBase, IPoderosaCommand, IPoderosaMenuItem {

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SFTPCommand() {
        }

        #endregion

        #region IPoderosaCommand

        public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {

            SSH2Connection ssh2Connection = GetSSHConnection(target) as SSH2Connection;
            if (ssh2Connection == null)
                return CommandResult.Ignored;

            string connectionName = GetTerminalName(target);
            if (connectionName == null)
                connectionName = SFTPPlugin.Instance.StringResource.GetString("Common.UnknownPeer");

            Form ownerForm = GetForm(target);

            if (!ConfirmToUse(ownerForm, "SFTP"))
                return CommandResult.Cancelled;

            SFTPClient sftp = null;
            try {
                sftp = SFTPClient.OpenSFTPChannel(ssh2Connection);

                SFTPForm form = new SFTPForm(ownerForm, sftp, connectionName);
                form.Show();    // Note: don't specify owner to avoid fixed z-order.

                return CommandResult.Succeeded;
            }
            catch (Exception e) {
                if (sftp != null) {
                    try {
                        sftp.Close();
                    }
                    catch (Exception) {
                    }
                    sftp = null;
                }

                RuntimeUtil.ReportException(e);
                return CommandResult.Failed;
            }
        }

        public bool CanExecute(ICommandTarget target) {
            return IsAcceptable(target, false, true);
        }

        #endregion

        #region IPoderosaMenuItem

        public IPoderosaCommand AssociatedCommand {
            get {
                return this;
            }
        }

        #endregion

        #region IPoderosaMenu

        public string Text {
            get {
                return SFTPPlugin.Instance.StringResource.GetString("SFTPCommand.MenuText");
            }
        }

        public bool IsEnabled(ICommandTarget target) {
            return CanExecute(target);
        }

        public bool IsChecked(ICommandTarget target) {
            return false;
        }

        #endregion

        #region IAdaptable

        public IAdaptable GetAdapter(Type adapter) {
            return SFTPPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion
    }

    /// <summary>
    /// SCP command
    /// </summary>
    internal class SCPCommand : CommandBase, IPoderosaCommand, IPoderosaMenuItem {

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public SCPCommand() {
        }

        #endregion

        #region IPoderosaCommand

        public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {

            SSHConnection sshConnection = GetSSHConnection(target);

            // Note: Currently, SCPClient supports only SSH2.
            if (!(sshConnection is SSH2Connection))
                return CommandResult.Ignored;

            string connectionName = GetTerminalName(target);
            if (connectionName == null)
                connectionName = SFTPPlugin.Instance.StringResource.GetString("Common.UnknownPeer");

            Form ownerForm = GetForm(target);

            if (!ConfirmToUse(ownerForm, "SCP"))
                return CommandResult.Cancelled;

            SCPClient scp = new SCPClient(sshConnection);

            SCPForm form = new SCPForm(ownerForm, scp, connectionName);
            form.Show();    // Note: don't specify owner to avoid fixed z-order.

            return CommandResult.Succeeded;
        }

        public bool CanExecute(ICommandTarget target) {
            // Note: Currently, SCPClient supports only SSH2.
            return IsAcceptable(target, false, true);
        }

        #endregion

        #region IPoderosaMenuItem

        public IPoderosaCommand AssociatedCommand {
            get {
                return this;
            }
        }

        #endregion

        #region IPoderosaMenu

        public string Text {
            get {
                return SFTPPlugin.Instance.StringResource.GetString("SCPCommand.MenuText");
            }
        }

        public bool IsEnabled(ICommandTarget target) {
            return CanExecute(target);
        }

        public bool IsChecked(ICommandTarget target) {
            return false;
        }

        #endregion

        #region IAdaptable

        public IAdaptable GetAdapter(Type adapter) {
            return SFTPPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion
    }
}
