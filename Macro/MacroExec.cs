/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MacroExec.cs,v 1.11 2012/03/18 02:52:23 kzmi Exp $
 */
using System;
using System.Threading;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Globalization;
using System.Drawing;
using System.IO;
using System.Collections.Specialized;
using Microsoft.JScript;

using Poderosa.Plugins;
using Poderosa.ConnectionParam;
using Poderosa.Terminal;
using Poderosa.Sessions;
using Poderosa.Protocols;
using Poderosa.Commands;
using Poderosa.View;

using MacroEnvironment = Poderosa.Macro.Environment;

namespace Poderosa.MacroInternal {
    internal class MacroUtil {
        public static Assembly LoadMacroAssembly(MacroModule mod) {
            if (mod.Type == MacroType.Assembly) {
                return Assembly.LoadFrom(mod.Path);
            }
            else if (mod.Type == MacroType.JavaScript) {
                JScriptCodeProvider compiler = new JScriptCodeProvider();
                CompilerParameters param = new CompilerParameters();
                param.IncludeDebugInformation = true;
                param.GenerateInMemory = false; //これがプラグインをロードできるかどうかの決め手になった。周辺をすべて理解したわけではないが、とりあえずこれでよしとする。深入りしている時間はあまりないし
                param.GenerateExecutable = true;

                StringCollection sc = param.ReferencedAssemblies;

                bool[] assyAdded = new bool[9];
                foreach (Assembly assy in AppDomain.CurrentDomain.GetAssemblies()) {
                    try {
                        string assyFilePath = new Uri(assy.CodeBase).LocalPath;
                        string assyFileName = Path.GetFileName(assyFilePath).ToLower(CultureInfo.InvariantCulture);
                        switch (assyFileName) {
                            case "system.drawing.dll":
                                assyAdded[0] = true;
                                break;
                            case "system.windows.forms.dll":
                                assyAdded[1] = true;
                                break;
                            case "poderosa.plugin.dll":
                                assyAdded[2] = true;
                                break;
                            case "poderosa.core.dll":
                                assyAdded[3] = true;
                                break;
                            case "granados.dll":
                                assyAdded[4] = true;
                                break;
                            case "poderosa.protocols.dll":
                                assyAdded[5] = true;
                                break;
                            case "poderosa.terminalemulator.dll":
                                assyAdded[6] = true;
                                break;
                            case "poderosa.terminalsession.dll":
                                assyAdded[7] = true;
                                break;
                            case "poderosa.macro.dll":
                                assyAdded[8] = true;
                                break;
                            case "poderosa.monolithic.exe":
                                // FIXME: it is better to use the name of the entry assembly.
                                //        but how can we know the entry assembly is the monolithic type ?
                                assyAdded[2] =
                                assyAdded[3] =
                                assyAdded[4] =
                                assyAdded[5] =
                                assyAdded[6] =
                                assyAdded[7] =
                                assyAdded[8] = true;
                                break;
                            default:
                                continue;
                        }
                        Debug.WriteLine("(LoadMacroAssembly) add to ReferencedAssemblies: " + assyFilePath);
                        sc.Add(assyFilePath);
                    }
                    catch (Exception) {
                    }
                }

                foreach (bool flag in assyAdded) {
                    if (!flag) {
                        throw new Exception(MacroPlugin.Instance.Strings.GetString("Message.MacroExec.MissingAssemblies"));
                    }
                }

                foreach (string x in mod.AdditionalAssemblies)
                    if (x.Length > 0)
                        sc.Add(x);

                CompilerResults result = compiler.CompileAssemblyFromFile(param, mod.Path);
                if (result.Errors.Count > 0) {
                    StringBuilder bld = new StringBuilder();
                    bld.Append(MacroPlugin.Instance.Strings.GetString("Message.MacroExec.FailedToCompileScript"));
                    foreach (CompilerError err in result.Errors) {
                        bld.Append(String.Format("Line {0} Column {1} : {2}\n", err.Line, err.Column, err.ErrorText));
                    }
                    throw new Exception(bld.ToString());
                }

                Debug.WriteLineIf(DebugOpt.Macro, "Compiled:" + result.PathToAssembly + " FullName:" + result.CompiledAssembly.FullName);
                //AppDomain.CurrentDomain.Load(result.CompiledAssembly.FullName, result.Evidence);

                return result.CompiledAssembly;
            }
            else
                throw new Exception("Unsupported macro module type " + mod.Type.ToString() + " is specified.");
        }
        private static string GetMacroPath() {
            string t = typeof(MacroUtil).Assembly.CodeBase;
            int c1 = t.IndexOf(':'); //先頭はfile://...とくる
            int c2 = t.IndexOf(':', c1 + 1); //これがドライブ名直後のコロン
            t = t.Substring(c2 - 1);
            return t.Replace('/', '\\');
        }
        private static string GetCorePath() {
            string t = typeof(Poderosa.View.RenderProfile).Assembly.CodeBase;
            int c1 = t.IndexOf(':'); //先頭はfile://...とくる
            int c2 = t.IndexOf(':', c1 + 1); //これがドライブ名直後のコロン
            t = t.Substring(c2 - 1);
            return t.Replace('/', '\\');
        }

        //Invoke系
        public static void InvokeMessageBox(string msg) {
            Form f = MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm();
            f.Invoke(new MessageBoxDelegate(GUtil.Warning), f, msg);
        }
        public static ITerminalSession InvokeOpenSessionOrNull(ICommandTarget target, TerminalParam param) {
            ITerminalParameter tp = param.ConvertToTerminalParameter();
            ITerminalSettings ts = CreateTerminalSettings(param);

            IViewManager pm = CommandTargetUtil.AsWindow(target).ViewManager;
            //独立ウィンドウにポップアップさせるようなことは考えていない
            IContentReplaceableView rv = (IContentReplaceableView)pm.GetCandidateViewForNewDocument().GetAdapter(typeof(IContentReplaceableView));
            TerminalControl tc = (TerminalControl)rv.GetCurrentContent().GetAdapter(typeof(TerminalControl));
            if (tc != null) { //ターミナルコントロールがないときは無理に設定しにいかない
                RenderProfile rp = ts.UsingDefaultRenderProfile ? MacroPlugin.Instance.TerminalEmulatorService.TerminalEmulatorOptions.CreateRenderProfile() : ts.RenderProfile;
                Size sz = tc.CalcTerminalSize(rp);
                tp.SetTerminalSize(sz.Width, sz.Height);
            }


            return (ITerminalSession)MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm().Invoke(new OpenSessionDelegate(OpenSessionOrNull), tp, ts);
        }
        private static ITerminalSession OpenSessionOrNull(ITerminalParameter tp, ITerminalSettings ts) {
            try {
                ITerminalSessionsService ss = MacroPlugin.Instance.TerminalSessionsService;
                ITerminalSession newsession = ss.TerminalSessionStartCommand.StartTerminalSession(MacroPlugin.Instance.WindowManager.ActiveWindow, tp, ts);
                if (newsession == null)
                    return null;

                MacroPlugin.Instance.MacroManager.CurrentExecutor.AddRuntimeSession(newsession);
                return newsession;
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
                return null;
            }
        }
        private static ITerminalSettings CreateTerminalSettings(TerminalParam param) {
            ITerminalSettings ts = MacroPlugin.Instance.TerminalEmulatorService.CreateDefaultTerminalSettings(param.Caption, null);
            ts.BeginUpdate();
            ts.RenderProfile = param.RenderProfile;
            ts.TransmitNL = param.TransmitNL;
            ts.LocalEcho = param.LocalEcho;
            ts.Encoding = param.Encoding;
            if (param.LogType != LogType.None)
                ts.LogSettings.Reset(CreateSimpleLogSettings(param));
            ts.EndUpdate();
            return ts;
        }
        private static ISimpleLogSettings CreateSimpleLogSettings(TerminalParam param) {
            ISimpleLogSettings logsettings = MacroPlugin.Instance.TerminalEmulatorService.CreateDefaultSimpleLogSettings();
            logsettings.LogPath = param.LogPath;
            logsettings.LogType = param.LogType;
            logsettings.LogAppend = param.LogAppend;
            return logsettings;
        }

        public static void InvokeActivate(IPoderosaDocument document) {
            MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm().Invoke(new DocDelegate(ActivateDoc), document);
        }
        private static void ActivateDoc(IPoderosaDocument document) {
            MacroPlugin.Instance.SessionManager.ActivateDocument(document, ActivateReason.InternalAction);
        }
        public static void InvokeClose(IPoderosaDocument document) {
            MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm().Invoke(new DocDelegate(CloseDoc), document);
        }
        private static void CloseDoc(IPoderosaDocument document) {
            MacroPlugin.Instance.SessionManager.CloseDocument(document);
        }


        private delegate void MessageBoxDelegate(IWin32Window window, string msg);
        private delegate ITerminalSession OpenSessionDelegate(ITerminalParameter tp, ITerminalSettings ts);
        private delegate void DocDelegate(IPoderosaDocument document);
    }

    internal delegate void TraceWindowOperator(MacroTraceWindow window);

    internal interface ITraceWindowManager {

        void ShowTraceWindow();

        void HideTraceWindow();

        void OperateTraceWindow(TraceWindowOperator op);
    }

    internal class MacroExecutor : ITraceWindowManager {

        private MacroModule _module;
        private Assembly _assembly;
        private MacroTraceWindow _traceWindow;
        private readonly object _traceWindowLock = new object();
        private Thread _macroThread;
        private List<ReceptionDataPool> _receptionDataPool;

        public MacroExecutor(MacroModule mod, Assembly asm) {
            _module = mod;
            _assembly = asm;
            _receptionDataPool = new List<ReceptionDataPool>();
            if (mod.DebugMode) {
                ShowTraceWindow();
            }
        }
        public MacroModule Module {
            get {
                return _module;
            }
        }


        public void AsyncExec(IMacroEventListener listener) {
            InitReceptionPool();
            _macroThread = new Thread((ThreadStart)delegate() {
                MacroMain(listener);
            });
            _macroThread.Name = "Macro - " + _module.Title;
            _macroThread.Start();
        }

        private void MacroMain(IMacroEventListener listener) {
            try {
                InitEnv();
                //AppDomain.CurrentDomain.Load(_assembly.FullName);
                MethodInfo mi = _assembly.EntryPoint;
                if (DebugOpt.Macro) {
                    Type[] ts = _assembly.GetTypes();
                    foreach (Type t in ts) {
                        Debug.WriteLine("Found Type " + t.FullName);

                        mi = _assembly.GetType("JScript Main").GetMethod("Main");
                        Debug.WriteLine("Method Found " + mi.Name);
                    }

                }

                if (mi == null)
                    MacroUtil.InvokeMessageBox(MacroPlugin.Instance.Strings.GetString("Message.MacroModule.NoEntryPoint"));
                else
                    mi.Invoke(null, BindingFlags.InvokeMethod, null, new object[1] { new string[0] }, CultureInfo.CurrentUICulture);
            }
            catch (TargetInvocationException tex) {
                Exception inner = tex.InnerException;
                lock (_traceWindowLock) {
                    if (_traceWindow == null) {
                        MacroUtil.InvokeMessageBox(String.Format(MacroPlugin.Instance.Strings.GetString("Message.MacroExec.ExceptionWithoutTraceWindow"), inner.Message));
                        Debug.WriteLine("TargetInvocationException");
                        Debug.WriteLine(inner.GetType().Name);
                        Debug.WriteLine(inner.Message);
                        Debug.WriteLine(inner.StackTrace);
                    }
                    else {
                        _traceWindow.AddLine(MacroPlugin.Instance.Strings.GetString("Message.MacroExec.ExceptionInMacro"));
                        _traceWindow.AddLine(String.Format("{0} : {1}", inner.GetType().FullName, inner.Message));
                        _traceWindow.AddLine(inner.StackTrace);
                    }
                }
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            finally {
                CloseAll();
                if (listener != null)
                    listener.IndicateMacroFinished();

                lock (_traceWindowLock) {
                    if (_traceWindow != null) {
                        if (_traceWindow.Visible)
                            _traceWindow.HideOnCloseButton = false;
                        else {
                            MacroTraceWindow traceWindow = _traceWindow;
                            _traceWindow.BeginInvoke(
                                (System.Windows.Forms.MethodInvoker)delegate() {
                                traceWindow.Dispose();
                            });
                        }
                        _traceWindow = null;
                    }
                }
            }
        }

        private void InitEnv() {
            MacroEnvironment.Init(new ConnectionListImpl(), new UtilImpl(), new DebugServiceImpl(this));
        }

        public void Abort() {
            _macroThread.Abort();
        }

        //実行するセッションにタスク登録。マクロ実行時に動いていたセッションだけでなく、マクロによって開設されるセッションも同様
        public void AddRuntimeSession(ITerminalSession session) {
            ReceptionDataPool pool = new ReceptionDataPool();
            _receptionDataPool.Add(pool);
            session.Terminal.StartModalTerminalTask(pool);
        }

        private void InitReceptionPool() {
            foreach (ISession session in MacroPlugin.Instance.SessionManager.AllSessions) {
                ITerminalSession ts = (ITerminalSession)session.GetAdapter(typeof(ITerminalSession));
                if (ts != null)
                    AddRuntimeSession(ts);
            }
        }
        private void CloseAll() {
            foreach (ReceptionDataPool pool in _receptionDataPool)
                pool.Close();
        }

        #region TraceWindowManager

        public void ShowTraceWindow() {
            Form ownerForm = MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm();
            if (ownerForm.InvokeRequired) {
                ownerForm.Invoke(new System.Windows.Forms.MethodInvoker(ShowTraceWindow));
            }
            else {
                lock (_traceWindowLock) {
                    if (_traceWindow == null) {
                        _traceWindow = new MacroTraceWindow();
                        _traceWindow.AdjustTitle(_module);
                        _traceWindow.Owner = MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm();
                        _traceWindow.HideOnCloseButton = true;
                    }
                    _traceWindow.Show();
                }
            }
        }

        public void HideTraceWindow() {
            Form ownerForm = MacroPlugin.Instance.WindowManager.ActiveWindow.AsForm();
            if (ownerForm.InvokeRequired) {
                ownerForm.Invoke(new System.Windows.Forms.MethodInvoker(HideTraceWindow));
            }
            else {
                lock (_traceWindowLock) {
                    if (_traceWindow != null) {
                        _traceWindow.Hide();
                    }
                }
            }
        }

        public void OperateTraceWindow(TraceWindowOperator op) {
            lock (_traceWindowLock) {
                if (_traceWindow != null)
                    op(_traceWindow);
            }
        }

        #endregion
    }

    //受信データをためておく
    internal class ReceptionDataPool : IModalCharacterTask {
        private IModalTerminalTaskSite _site;
        private StringBuilder _bufferForMacro;
        private readonly object _signalForMacro = new object();
        private bool _closed;

        public void InitializeModelTerminalTask(IModalTerminalTaskSite site, IByteAsyncInputStream default_handler, ITerminalConnection connection) {
            _site = site;
            _bufferForMacro = new StringBuilder();
            _closed = false;
        }

        public string Caption {
            get {
                return "MACRO";
            }
        }

        public bool ShowInputInTerminal {
            get {
                return true;
            }
        }

        public IAdaptable GetAdapter(Type adapter) {
            return MacroPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public void OnReception(ByteDataFragment data) {
        }

        public void OnNormalTermination() {
            lock (_signalForMacro) {    // for sync state of _closed and _site
                if (!_closed)
                    _site.Cancel(null);
            }
        }

        public void OnAbnormalTermination(string message) {
            lock (_signalForMacro) {    // for sync state of _closed and _site
                if (!_closed)
                    _site.Cancel(null);
            }
        }

        public void NotifyEndOfPacket() {
            lock (_signalForMacro) {
                if (!_closed) {
                    Monitor.PulseAll(_signalForMacro);
                }
            }
        }

        public void Close() {
            lock (_signalForMacro) {
                if (!_closed) {
                    _closed = true;
                    Monitor.PulseAll(_signalForMacro);
                    _site.Complete();
                }
            }
        }

        //マクロ実行スレッドから呼ばれる１行読み出しメソッド

        public string ReadLineFromMacroBuffer() {
            return ReadLineFromMacroBuffer(false, 0);
        }

        public string ReadLineFromMacroBuffer(int timeoutMillisecs) {
            if (timeoutMillisecs < 0)
                throw new ArgumentException("Invalid timeout", "timeoutMillisecs");
            return ReadLineFromMacroBuffer(true, timeoutMillisecs);
        }

        private string ReadLineFromMacroBuffer(bool hasTimeout, int timeoutMillisecs) {
            DateTime timeout;
            if (hasTimeout)
                timeout = DateTime.UtcNow.AddMilliseconds(timeoutMillisecs);
            else
                timeout = DateTime.MaxValue;

            lock (_signalForMacro) {
                while (!_closed) {
                    int l = _bufferForMacro.Length;
                    int i = 0;
                    for (i = 0; i < l; i++) {
                        if (_bufferForMacro[i] == '\n')
                            break;
                    }

                    if (l > 0 && i < l) { //めでたく行末がみつかった
                        int j = i;
                        if (i > 0 && _bufferForMacro[i - 1] == '\r')
                            j = i - 1; //CRLFのときは除いてやる
                        string r;
                        lock (_bufferForMacro) {
                            r = _bufferForMacro.ToString(0, j);
                            _bufferForMacro.Remove(0, i + 1);
                        }
                        return r;
                    }

                    if (hasTimeout) {
                        int timeLeft = (int)(timeout.Subtract(DateTime.UtcNow)).TotalMilliseconds;
                        if (timeLeft <= 0 || timeoutMillisecs == 0)
                            break;  // Timeout
                        if (_closed) {
                            break;  // already closed
                        }
                        else {
                            bool signaled = Monitor.Wait(_signalForMacro, timeLeft);
                            if (!signaled)
                                break;  // Timeout
                        }
                    }
                    else {
                        if (_closed) {
                            break;  // already closed
                        }
                        else {
                            Monitor.Wait(_signalForMacro);
                        }
                    }
                }
            }

            // timeout or closed
            return hasTimeout ? null : String.Empty;
        }


        //マクロ実行スレッドから呼ばれる、「何かデータがあれば全部もっていく」メソッド

        public string ReadAllFromMacroBuffer() {
            return ReadAllFromMacroBuffer(false, 0);
        }

        public string ReadAllFromMacroBuffer(int timeoutMillisecs) {
            if (timeoutMillisecs < 0)
                throw new ArgumentException("Invalid timeout", "timeoutMillisecs");
            return ReadAllFromMacroBuffer(true, timeoutMillisecs);
        }

        private string ReadAllFromMacroBuffer(bool hasTimeout, int timeoutMillisecs) {
            lock (_signalForMacro) {
                if (_bufferForMacro.Length == 0) {
                    if (_closed) {
                        if (hasTimeout) {
                            return null;    // Timeout, no data
                        }
                    }
                    else {
                        if (hasTimeout) {
                            bool signaled = Monitor.Wait(_signalForMacro, timeoutMillisecs);
                            if (!signaled && _bufferForMacro.Length == 0)
                                return null;    // Timeout, no data
                        }
                        else {
                            Monitor.Wait(_signalForMacro);
                        }
                    }
                }
            }

            string r;
            lock (_bufferForMacro) {
                r = _bufferForMacro.ToString();
                _bufferForMacro.Remove(0, _bufferForMacro.Length);
            }
            return r;
        }
        public void ProcessChar(char ch) {
            if (Char.IsControl(ch) && ch != '\n')
                return;

            lock (_bufferForMacro) {
                _bufferForMacro.Append(ch); //!!長さに上限をつけたほうが安全かも
            }
        }

    }
}
