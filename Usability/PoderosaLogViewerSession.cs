/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PoderosaLogViewerSession.cs,v 1.6 2011/12/10 10:36:42 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;

using Poderosa.Forms;
using Poderosa.Sessions;
using Poderosa.View;
using Poderosa.Document;
using Poderosa.Plugins;
using Poderosa.Commands;

namespace Poderosa.LogViewer {
    internal class PoderosaLogViewerSession : ISession {
        private PoderosaLogViewControl _view;
        private PoderosaLogDocument _document;
        private ISessionHost _host;

        public PoderosaLogViewerSession() {
            _document = new PoderosaLogDocument(this);
            IPoderosaLog log = ((IPoderosaApplication)PoderosaLogViewerPlugin.Instance.PoderosaWorld.GetAdapter(typeof(IPoderosaApplication))).PoderosaLog;
            log.AddChangeListener(_document);
        }

        public string Caption {
            get {
                return _document.Caption;
            }
        }

        public Image Icon {
            get {
                return _document.Icon;
            }
        }

        public void InternalStart(ISessionHost host) {
            _host = host;
            _host.RegisterDocument(_document);
        }

        public void InternalTerminate() {
        }

        public PrepareCloseResult PrepareCloseDocument(IPoderosaDocument document) {
            return PrepareCloseResult.TerminateSession;
        }

        public PrepareCloseResult PrepareCloseSession() {
            return PrepareCloseResult.TerminateSession;
        }

        public void InternalAttachView(IPoderosaDocument document, IPoderosaView view) {
            _view = (PoderosaLogViewControl)view.GetAdapter(typeof(PoderosaLogViewControl));
            Debug.Assert(_view != null);
            _view.SetParent(this);
        }

        public void InternalDetachView(IPoderosaDocument document, IPoderosaView view) {
            Debug.WriteLineIf(DebugOpt.LogViewer, "LogView InternalDetach");
            _view = null;
        }

        public void InternalCloseDocument(IPoderosaDocument document) {
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PoderosaLogViewerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public PoderosaLogViewControl CurrentView {
            get {
                return _view;
            }
        }
        public bool IsWindowVisible {
            get {
                return _view != null;
            }
        }

        public PoderosaLogDocument Document {
            get {
                return _document;
            }
        }
    }

    //ViewClass
    internal class PoderosaLogViewControl : CharacterDocumentViewer, IPoderosaView, IGeneralViewCommands {
        private IPoderosaForm _form;
        private PoderosaLogViewerSession _session;


        public PoderosaLogViewControl(IPoderosaForm form) {
            _form = form;
            _caret.Enabled = false;
            _caret.Blink = false;
        }

        public void SetParent(PoderosaLogViewerSession session) {
            _session = session;
            this.SetPrivateRenderProfile(CreateLogRenderProfile());
            this.SetContent(_session.Document);
        }

        public IPoderosaDocument Document {
            get {
                return _session == null ? null : _session.Document;
            }
        }

        public ISelection CurrentSelection {
            get {
                return this.ITextSelection;
            }
        }

        public IPoderosaForm ParentForm {
            get {
                return this.FindForm() as IPoderosaForm;
            }
        }


        //更新 最終行が見えるように
        public void UpdateDocument() {
            PoderosaLogDocument doc = _session.Document;
            int newtop = RuntimeUtil.AdjustIntRange(doc.Size - this.GetHeightInLines(), 0, doc.Size - 1);
            AdjustScrollBar();
            if (_VScrollBar.Enabled)
                _VScrollBar.Value = newtop;
            else
                Invalidate();
        }
        //UpdateDocumentのInvoke実行
        private delegate void UpdateDocumentDelegate_();
        private UpdateDocumentDelegate_ _updateDocumentDelegate;
        public Delegate UpdateDocumentDelegate {
            get {
                if (_updateDocumentDelegate == null)
                    _updateDocumentDelegate = new UpdateDocumentDelegate_(UpdateDocument);
                return _updateDocumentDelegate;
            }
        }

        //Command
        public IPoderosaCommand Copy {
            get {
                return PoderosaLogViewerPlugin.Instance.CoreServices.WindowManager.SelectionService.DefaultCopyCommand;
            }
        }

        public IPoderosaCommand Paste {
            get {
                return null; //ペースト不可
            }
        }

        private static RenderProfile CreateLogRenderProfile() {
            RenderProfile rp = new RenderProfile();
            rp.FontName = "Courier New";
            rp.FontSize = 9;
            rp.BackColor = SystemColors.Window;
            rp.ForeColor = SystemColors.WindowText;
            return rp;
        }

        //標準幅
        public static int DefaultWidth {
            get {
                return (int)(CreateLogRenderProfile().Pitch.Width * PoderosaLogDocument.DefaultWidth);
            }
        }

    }

    //DocClass
    internal class PoderosaLogDocument : CharacterDocument, IPoderosaLogListener {
        private PoderosaLogViewerSession _session;

        private bool _nextLineIsFirstLine;

        public PoderosaLogDocument(PoderosaLogViewerSession session) {
            _caption = "Poderosa Event Log";
            _session = session;
            //１行はないとだめな制約があるので
            this.AddLine(GLine.CreateSimpleGLine("", TextDecoration.Default));
            _nextLineIsFirstLine = true;
        }

        //初期状態の１行の文字数
        public static int DefaultWidth {
            get {
                return 80; //可変にしてもよい
            }
        }

        public void OnNewItem(IPoderosaLogItem item) {
            //カテゴリ分けなどあるかもしれないが...
            String text = String.Format("[{0}] {1}", item.Category.Name, item.Text);
            int width = PoderosaLogDocument.DefaultWidth;

            //width文字ごとに切り取り。日本語文字があるケースは未サポート
            int offset = 0;
            while (offset < text.Length) {
                int next = RuntimeUtil.AdjustIntRange(offset + width, 0, text.Length);
                GLine line = GLine.CreateSimpleGLine(text.Substring(offset, next - offset), TextDecoration.Default);
                line.EOLType = next < text.Length ? EOLType.Continue : EOLType.CRLF;
                Append(line);
                offset = next;
            }

            PoderosaLogViewControl vc = _session.CurrentView;
            if (vc != null) {
                if (vc.InvokeRequired)
                    vc.Invoke(vc.UpdateDocumentDelegate);
                else
                    vc.UpdateDocument();
            }
        }

        //ちょっと見苦しいが、行の追加
        private void Append(GLine line) {
            if (_nextLineIsFirstLine) {
                _nextLineIsFirstLine = false;
                _firstLine = line;
                _lastLine = line;
                line.ID = 0;
            }
            else
                this.AddLine(line);
        }
    }

    //ViewFactory
    internal class LogViewerFactory : IViewFactory {
        public IPoderosaView CreateNew(IPoderosaForm parent) {
            return new PoderosaLogViewControl(parent);
        }

        public Type GetViewType() {
            return typeof(PoderosaLogViewControl);
        }

        public Type GetDocumentType() {
            return typeof(PoderosaLogDocument);
        }

        public IAdaptable GetAdapter(Type adapter) {
            return PoderosaLogViewerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }
}
