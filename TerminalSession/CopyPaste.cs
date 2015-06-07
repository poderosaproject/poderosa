/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: CopyPaste.cs,v 1.3 2011/10/27 23:21:58 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

using Poderosa.Terminal;
using Poderosa.Sessions;
using Poderosa.View;
using Poderosa.Forms;

namespace Poderosa.Commands {

    internal class PasteToTerminalCommand : IPoderosaCommand {
        private TerminalControl _control;
        public PasteToTerminalCommand(TerminalControl control) {
            _control = control;
        }
        public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            if (!CanExecute(target))
                return CommandResult.Ignored;
            TerminalTransmission output = GetSession().TerminalTransmission;

            string data = Clipboard.GetDataObject().GetData("Text") as string;

            if (data == null)
                return CommandResult.Ignored;

            ITerminalEmulatorOptions options = TerminalSessionsPlugin.Instance.TerminalEmulatorService.TerminalEmulatorOptions;
            if (options.AlertOnPasteNewLineChar) {
                // Data will be split by CR, LF, CRLF or Environment.NewLine by TextReader.ReadLine,
                // So we check the data about CR, LF and Environment.NewLine.
                if (data.IndexOfAny(new char[] { '\r', '\n' }) >= 0 || data.Contains(Environment.NewLine)) {
                    IPoderosaView view = (IPoderosaView)_control.GetAdapter(typeof(IPoderosaView));
                    IPoderosaForm form = view.ParentForm;
                    if (form != null) {
                        DialogResult res = form.AskUserYesNo(TEnv.Strings.GetString("Message.AskPasteNewLineChar"));
                        if (res != DialogResult.Yes) {
                            return CommandResult.Ignored;
                        }
                    }
                }
            }

            //TODO 長文のときにダイアログを出して中途キャンセル可能に
            StringReader reader = new StringReader(data);
            output.SendTextStream(reader, data[data.Length - 1] == '\n');
            return CommandResult.Succeeded;
        }

        public bool CanExecute(ICommandTarget target) {
            return GetSession() != null && Clipboard.GetDataObject().GetDataPresent("Text");
        }

        public IAdaptable GetAdapter(Type adapter) {
            return TerminalSessionsPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        //送信可能状態であるときのみTerminalSessionを返す
        private ITerminalSession GetSession() {
            if (!_control.EnabledEx)
                return null;

            IPoderosaView view = (IPoderosaView)_control.GetAdapter(typeof(IPoderosaView));
            ITerminalSession s = (ITerminalSession)view.Document.OwnerSession.GetAdapter(typeof(ITerminalSession));
            return s.TerminalConnection.IsClosed ? null : s;
        }
    }
}
