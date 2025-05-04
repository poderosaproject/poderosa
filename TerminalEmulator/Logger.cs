// Copyright 2004-2025 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading;

using Poderosa.Document;
using Poderosa.Protocols;
using Poderosa.Util;
using Poderosa.ConnectionParam;

namespace Poderosa.Terminal {

    internal abstract class LoggerBase {
        private readonly ISimpleLogSettings _logSetting;
        private int _writeNumber = 0;
        private int _prevWriteNumber = 0;

        public abstract void Flush();

        public ISimpleLogSettings LogSettings {
            get {
                return _logSetting;
            }
        }

        public LoggerBase(ISimpleLogSettings log) {
            Debug.Assert(log != null);
            _logSetting = log;
        }

        public void AutoFlush() {
            int w = _writeNumber;
            if (w != _prevWriteNumber) {
                Flush();
                _prevWriteNumber = w;
            }
        }

        protected void Wrote() {
            _writeNumber++;
        }
    }


    internal class BinaryLogger : LoggerBase, IBinaryLogger {
        private readonly Stream _strm;
        private readonly object _sync = new object();
        private bool _closed = false;

        public BinaryLogger(ISimpleLogSettings log, Stream s)
            : base(log) {
            _strm = s;
        }

        public void Write(ByteDataFragment data) {
            lock (_sync) {
                if (!_closed) {
                    _strm.Write(data.Buffer, data.Offset, data.Length);
                    Wrote();
                }
            }
        }

        public override void Flush() {
            // note that Flush() may be called by AutoFlush()
            // even if output stream has been already closed.
            lock (_sync) {
                if (!_closed) {
                    _strm.Flush();
                }
            }
        }

        public void Close() {
            lock (_sync) {
                if (!_closed) {
                    _strm.Close();
                    _closed = true;
                }
            }
        }
    }

    internal class TextLogger : LoggerBase, ITextLogger {

        private const int INVALID_LINE_ID = Int32.MinValue;

        private enum Continuity {
            None,
            ContinueNormal,
            ContinueForced,
        }

        private readonly StreamWriter _writer;
        private readonly bool _withTimestamp;
        private readonly char[] _timestampBuffer;
        private readonly object _sync = new object();
        private Continuity _continuity = Continuity.None;
        private bool _closed = false;
        private int _logLineId = INVALID_LINE_ID;
        private DateTime _logLineTimestamp = DateTime.MinValue;
        private bool _logLineContinued = false;
        private char[] _logLineBuffer = new char[0];
        private int _logLineLength = 0;

        public TextLogger(ISimpleLogSettings log, Stream stream, bool withTimestamp)
            : base(log) {
            _writer = new StreamWriter(stream, Encoding.UTF8); // BOM is inserted automatically
            _withTimestamp = withTimestamp;
            if (withTimestamp)
                _timestampBuffer = new char[26];  // "YYYY-MM-DD hh:mm:ss,nnn - "
            else
                _timestampBuffer = null;
        }

        public void WriteLine(GLine line) {
            WriteLine(line, false);
        }

        public void ForceWriteLine(GLine line) {
            WriteLine(line, true);
        }

        private void WriteLine(GLine line, bool force) {
            lock (_sync) {
                if (_closed) {
                    return;
                }

                int lineId = line.ID;
                if (force || _logLineId != lineId) {
                    EmitLogLine(force);
                    _logLineId = lineId;
                }

                _logLineTimestamp = DateTime.Now;
                _logLineContinued = line.EOLType == EOLType.Continue;
                line.WriteTo((buff, len) => {
                    EnsureLogLineBuffer(len);
                    Array.Copy(buff, _logLineBuffer, len);
                    _logLineLength = len;
                });

                if (force) {
                    EmitLogLine(force);
                }
            }
        }

        private void EnsureLogLineBuffer(int length) {
            if (length <= 0 || _logLineBuffer.Length >= length) {
                return;
            }
            int v = length - 1;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;
            _logLineBuffer = new char[v];
        }

        private void EmitLogLine(bool forced) {
            if (_logLineId == INVALID_LINE_ID) {
                return;
            }

            bool writeContinue;
            if (_continuity == Continuity.None) {
                writeContinue = false;
            }
            else {
                // reset continuity if the current log line is expected to be continued and the output type of the next log doesn't match
                if (_continuity != (forced ? Continuity.ContinueForced : Continuity.ContinueNormal)) {
                    _writer.WriteLine();
                    writeContinue = false;
                }
                else {
                    writeContinue = true;
                }
            }

            if (_withTimestamp && !writeContinue) {
                WriteTimestamp(_logLineTimestamp);
            }

            _writer.Write(_logLineBuffer, 0, _logLineLength);

            if (_logLineContinued) {
                _continuity = forced ? Continuity.ContinueForced : Continuity.ContinueNormal;
            }
            else {
                _continuity = Continuity.None;
                _writer.WriteLine();
            }

            _logLineId = INVALID_LINE_ID;
            _logLineLength = 0;

            Wrote();
        }

        public override void Flush() {
            // note that Flush() may be called by AutoFlush()
            // even if output stream has been already closed.
            lock (_sync) {
                if (!_closed) {
                    _writer.Flush();
                }
            }
        }

        public void Close() {
            lock (_sync) {
                if (!_closed) {
                    EmitLogLine(false);
                    _writer.Close();
                    _closed = true;
                }
            }
        }

        public void Comment(string comment) {
            lock (_sync) {
                if (!_closed) {
                    EmitLogLine(false);
                    if (_continuity != Continuity.None) {
                        _writer.WriteLine();
                    }
                    if (_withTimestamp) {
                        WriteTimestamp(DateTime.Now);
                    }
                    _writer.WriteLine(comment);
                    _continuity = Continuity.None;
                    Wrote();
                }
            }
        }

        private void WriteTimestamp(DateTime timestamp) {
            // Write timestamp in ISO 8601 format.
            char[] buff = _timestampBuffer;
            int offset = 0;

            offset = WriteInt(buff, offset, 4, timestamp.Year);
            buff[offset++] = '-';
            offset = WriteInt(buff, offset, 2, timestamp.Month);
            buff[offset++] = '-';
            offset = WriteInt(buff, offset, 2, timestamp.Day);
            buff[offset++] = 'T';
            offset = WriteInt(buff, offset, 2, timestamp.Hour);
            buff[offset++] = ':';
            offset = WriteInt(buff, offset, 2, timestamp.Minute);
            buff[offset++] = ':';
            offset = WriteInt(buff, offset, 2, timestamp.Second);
            buff[offset++] = '.';
            offset = WriteInt(buff, offset, 3, timestamp.Millisecond);

            // separator
            buff[offset++] = ' ';
            buff[offset++] = '-';
            buff[offset++] = ' ';

            _writer.Write(buff, 0, offset);
        }

        private static int WriteInt(char[] buff, int offset, int width, int value) {
            int limit = offset + width;
            int index = limit;
            for (int i = 0; i < width; i++) {
                buff[--index] = (char)('0' + value % 10);
                value /= 10;
            }
            return limit;
        }
    }

    internal interface INullLogger {
    }

    internal class NullBinaryLogger : INullLogger, IBinaryLogger {
        public void Write(ByteDataFragment data) {
        }

        public void Close() {
        }

        public void Flush() {
        }

        public void AutoFlush() {
        }
    }

    internal class NullTextLogger : INullLogger, ITextLogger {
        public void WriteLine(GLine line) {
        }

        public void ForceWriteLine(GLine line) {
        }

        public void Comment(string comment) {
        }

        public void Close() {
        }

        public void Flush() {
        }

        public void AutoFlush() {
        }
    }

    internal class NullXmlLogger : INullLogger, IXmlLogger {
        public void Write(char ch) {
        }

        public void EscapeSequence(char[] body) {
        }

        public void Comment(string comment) {
        }

        public void Close() {
        }

        public void Flush() {
        }

        public void AutoFlush() {
        }
    }

    internal interface ILoggerList<ILoggerT> {
        IEnumerable<ILoggerT> Loggers {
            get;
        }
    }

    internal class BinaryLoggerList : ILoggerList<IBinaryLogger>, IBinaryLogger {

        private readonly IBinaryLogger[] _loggers;

        public BinaryLoggerList(IBinaryLogger[] loggers) {
            _loggers = loggers;
        }

        public IEnumerable<IBinaryLogger> Loggers {
            get {
                return _loggers;
            }
        }

        public void Write(ByteDataFragment data) {
            foreach (IBinaryLogger logger in _loggers) {
                logger.Write(data);
            }
        }

        public void Close() {
            foreach (IBinaryLogger logger in _loggers) {
                logger.Close();
            }
        }

        public void Flush() {
            foreach (IBinaryLogger logger in _loggers) {
                logger.Flush();
            }
        }

        public void AutoFlush() {
            foreach (IBinaryLogger logger in _loggers) {
                logger.AutoFlush();
            }
        }

    }

    internal class TextLoggerList : ILoggerList<ITextLogger>, ITextLogger {

        private readonly ITextLogger[] _loggers;

        public TextLoggerList(ITextLogger[] loggers) {
            _loggers = loggers;
        }

        public IEnumerable<ITextLogger> Loggers {
            get {
                return _loggers;
            }
        }

        public void WriteLine(GLine line) {
            foreach (ITextLogger logger in _loggers) {
                logger.WriteLine(line);
            }
        }

        public void ForceWriteLine(GLine line) {
            foreach (ITextLogger logger in _loggers) {
                logger.ForceWriteLine(line);
            }
        }

        public void Comment(string comment) {
            foreach (ITextLogger logger in _loggers) {
                logger.Comment(comment);
            }
        }

        public void Close() {
            foreach (ITextLogger logger in _loggers) {
                logger.Close();
            }
        }

        public void Flush() {
            foreach (ITextLogger logger in _loggers) {
                logger.Flush();
            }
        }

        public void AutoFlush() {
            foreach (ITextLogger logger in _loggers) {
                logger.AutoFlush();
            }
        }
    }

    internal class XmlLoggerList : ILoggerList<IXmlLogger>, IXmlLogger {

        private readonly IXmlLogger[] _loggers;

        public XmlLoggerList(IXmlLogger[] loggers) {
            _loggers = loggers;
        }

        public IEnumerable<IXmlLogger> Loggers {
            get {
                return _loggers;
            }
        }

        public void Write(char ch) {
            foreach (IXmlLogger logger in _loggers) {
                logger.Write(ch);
            }
        }

        public void EscapeSequence(char[] body) {
            foreach (IXmlLogger logger in _loggers) {
                logger.EscapeSequence(body);
            }
        }

        public void Comment(string comment) {
            foreach (IXmlLogger logger in _loggers) {
                logger.Comment(comment);
            }
        }

        public void Close() {
            foreach (IXmlLogger logger in _loggers) {
                logger.Close();
            }
        }

        public void Flush() {
            foreach (IXmlLogger logger in _loggers) {
                logger.Flush();
            }
        }

        public void AutoFlush() {
            foreach (IXmlLogger logger in _loggers) {
                logger.AutoFlush();
            }
        }
    }

    //ログに関する機能のまとめクラス
    internal class LogService : ILogService {
        private IBinaryLogger _binaryLogger;
        private ITextLogger _textLogger;
        private IXmlLogger _xmlLogger;

        private Thread _autoFlushThread = null;
        private readonly object _autoFlushSync = new object();

        private const int AUTOFLUSH_CHECK_INTERVAL = 1000;

        public LogService() {
            _binaryLogger = new NullBinaryLogger();
            _textLogger = new NullTextLogger();
            _xmlLogger = new NullXmlLogger();
        }

        public void SetupDefaultLogger(ITerminalEmulatorOptions options, ITerminalParameter param, ITerminalSettings settings) {
            if (options.DefaultLogType != LogType.None) {
                ApplySimpleLogSetting(new SimpleLogSettings(options.DefaultLogType, CreateAutoLogFileName(options, param, settings)));
            }
        }

        public void AddBinaryLogger(IBinaryLogger logger) {
            AddLogger<IBinaryLogger>(ref _binaryLogger, logger, CreateLoggerList);
        }
        public void RemoveBinaryLogger(IBinaryLogger logger) {
            RemoveLogger<IBinaryLogger, NullBinaryLogger>(ref _binaryLogger, logger, CreateLoggerList);
            logger.Flush();
        }
        public void AddTextLogger(ITextLogger logger) {
            AddLogger<ITextLogger>(ref _textLogger, logger, CreateLoggerList);
        }
        public void RemoveTextLogger(ITextLogger logger) {
            RemoveLogger<ITextLogger, NullTextLogger>(ref _textLogger, logger, CreateLoggerList);
            logger.Flush();
        }
        public void AddXmlLogger(IXmlLogger logger) {
            AddLogger<IXmlLogger>(ref _xmlLogger, logger, CreateLoggerList);
        }
        public void RemoveXmlLogger(IXmlLogger logger) {
            RemoveLogger<IXmlLogger, NullXmlLogger>(ref _xmlLogger, logger, CreateLoggerList);
            logger.Flush();
        }

        private void AddLogger<ILoggerT>(ref ILoggerT target, ILoggerT logger, Func<ILoggerT[], ILoggerT> createList)
            where ILoggerT : class {

            lock (_autoFlushSync) { // pause auto flush while adding a new logger
                ILoggerT currentLogger = Volatile.Read(ref target);
                if (currentLogger == null || currentLogger is INullLogger) {
                    Volatile.Write(ref target, logger);
                }
                else if (currentLogger is ILoggerList<ILoggerT>) {
                    ILoggerT newLogger = createList(MakeLoggerArray(((ILoggerList<ILoggerT>)currentLogger).Loggers, logger));
                    Volatile.Write(ref target, newLogger);
                }
                else {
                    ILoggerT newLogger = createList(new ILoggerT[] { currentLogger, logger });
                    Volatile.Write(ref target, newLogger);
                }
            }

            StartAutoFlushThread();
        }

        private ILoggerT[] MakeLoggerArray<ILoggerT>(IEnumerable<ILoggerT> loggers, ILoggerT logger) {
            List<ILoggerT> list = new List<ILoggerT>(loggers);
            list.Add(logger);
            return list.ToArray();
        }

        private void RemoveLogger<ILoggerT, NullT>(ref ILoggerT target, ILoggerT logger, Func<ILoggerT[], ILoggerT> createList)
            where ILoggerT : class
            where NullT : INullLogger, ILoggerT, new() {

            lock (_autoFlushSync) { // pause auto flush while adding a new logger
                ILoggerT currentLogger = Volatile.Read(ref target);
                if (currentLogger is ILoggerList<ILoggerT>) {
                    ILoggerT[] loggers = ((ILoggerList<ILoggerT>)currentLogger).Loggers.Where(l => !Object.ReferenceEquals(l, logger)).ToArray();
                    ILoggerT newLogger;
                    if (loggers.Length == 0) {
                        newLogger = new NullT();
                    }
                    else if (loggers.Length == 1) {
                        newLogger = loggers[0];
                    }
                    else {
                        newLogger = createList(loggers);
                    }
                    Volatile.Write(ref target, newLogger);
                }
                else if (Object.ReferenceEquals(currentLogger, logger)) {
                    Volatile.Write(ref target, new NullT());
                }
            }

            StartAutoFlushThread();
        }

        private static BinaryLoggerList CreateLoggerList(IBinaryLogger[] loggers) {
            return new BinaryLoggerList(loggers);
        }

        private static TextLoggerList CreateLoggerList(ITextLogger[] loggers) {
            return new TextLoggerList(loggers);
        }

        private static XmlLoggerList CreateLoggerList(IXmlLogger[] loggers) {
            return new XmlLoggerList(loggers);
        }

        //以下はAbstractTerminalから
        public IBinaryLogger BinaryLogger {
            get {
                return _binaryLogger;
            }
        }
        public ITextLogger TextLogger {
            get {
                return _textLogger;
            }
        }
        public IXmlLogger XmlLogger {
            get {
                return _xmlLogger;
            }
        }

        public bool HasBinaryLogger {
            get {
                return HasLogger<IBinaryLogger>(ref _binaryLogger);
            }
        }
        public bool HasTextLogger {
            get {
                return HasLogger<ITextLogger>(ref _textLogger);
            }
        }
        public bool HasXmlLogger {
            get {
                return HasLogger<IXmlLogger>(ref _xmlLogger);
            }
        }

        private bool HasLogger<ILoggerT>(ref ILoggerT target)
            where ILoggerT : class {

            ILoggerT currentLogger = Volatile.Read(ref target);
            if (currentLogger == null || currentLogger is INullLogger) {
                return false;
            }
            return true;
        }

        public void Flush() {
            _binaryLogger.Flush();
            _textLogger.Flush();
            _xmlLogger.Flush();
        }
        public void Close(GLine lastLine) {
            _textLogger.WriteLine(lastLine); //TextLogは改行ごとであるから、Close時に最終行を書き込むようにする
            StopAutoFlushThread();
            InternalClose();
        }
        private void InternalClose() {
            IBinaryLogger binaryLogger = _binaryLogger;
            Volatile.Write(ref _binaryLogger, new NullBinaryLogger());
            binaryLogger.Close();

            ITextLogger textLogger = _textLogger;
            Volatile.Write(ref _textLogger, new NullTextLogger());
            textLogger.Close();

            IXmlLogger xmlLogger = _xmlLogger;
            Volatile.Write(ref _xmlLogger, new NullXmlLogger());
            xmlLogger.Close();
        }
        public void Comment(string comment) {
            _textLogger.Comment(comment);
            _xmlLogger.Comment(comment);
        }

        public void ApplyLogSettings(ILogSettings settings, bool clear_previous) {
            if (clear_previous)
                InternalClose();
            ApplyLogSettingsInternal(settings);
        }
        private void ApplyLogSettingsInternal(ILogSettings settings) {
            ISimpleLogSettings sl = (ISimpleLogSettings)settings.GetAdapter(typeof(ISimpleLogSettings));
            if (sl != null) {
                ApplySimpleLogSetting(sl);
                return;
            }

            IMultiLogSettings ml = (IMultiLogSettings)settings.GetAdapter(typeof(IMultiLogSettings));
            if (ml != null) {
                foreach (ILogSettings e in ml)
                    ApplyLogSettingsInternal(e);
            }
        }
        private void ApplySimpleLogSetting(ISimpleLogSettings sl) {
            if (sl.LogType == LogType.None)
                return;

            FileStream fs = new FileStream(sl.LogPath, sl.LogAppend ? FileMode.Append : FileMode.Create, FileAccess.Write, FileShare.Read);
            ISimpleLogSettings loginfo = (ISimpleLogSettings)sl.Clone();
            switch (sl.LogType) {
                case LogType.Binary:
                    AddBinaryLogger(new BinaryLogger(loginfo, fs));
                    break;
                case LogType.Default:
                case LogType.PlainTextWithTimestamp:
                    bool withTimestamp = (sl.LogType == LogType.PlainTextWithTimestamp);
                    AddTextLogger(new TextLogger(loginfo, fs, withTimestamp));
                    break;
                case LogType.Xml:
                    AddXmlLogger(new XmlLogger(loginfo, fs));
                    break;
            }
        }


        private static string CreateAutoLogFileName(ITerminalEmulatorOptions opt, ITerminalParameter param, ITerminalSettings settings) {
            IAutoLogFileFormatter[] fmts = TerminalEmulatorPlugin.Instance.AutoLogFileFormatter;
            string filebody;
            if (fmts.Length == 0) {
                DateTime now = DateTime.Now;
                filebody = String.Format("{0}\\{1}_{2}{3,2:D2}{4,2:D2}", opt.DefaultLogDirectory, ReplaceCharForLogFile(settings.Caption), now.Year, now.Month, now.Day);
            }
            else
                filebody = fmts[0].FormatFileName(opt.DefaultLogDirectory, param, settings);


            int n = 1;
            do {
                string filename;
                if (n == 1)
                    filename = String.Format("{0}.log", filebody);
                else
                    filename = String.Format("{0}_{1}.log", filebody, n);

                if (!File.Exists(filename))
                    return filename;
                else
                    n++;
            } while (true);
        }

        private static string ReplaceCharForLogFile(string src) {
            StringBuilder bld = new StringBuilder();
            foreach (char ch in src) {
                if (ch == '\\' || ch == '/' || ch == ':' || ch == ';' || ch == ',' || ch == '*' || ch == '?' || ch == '"' || ch == '<' || ch == '>' || ch == '|')
                    bld.Append('_');
                else
                    bld.Append(ch);
            }
            return bld.ToString();
        }

        private void StartAutoFlushThread() {
            if (_autoFlushThread != null)
                return;

            lock (_autoFlushSync) {
                if (_autoFlushThread == null) {
                    _autoFlushThread = new Thread(new ThreadStart(AutoFlushThread));
                    _autoFlushThread.Name = "LogService-AutoFlush";
                    _autoFlushThread.Start();
                }
            }
        }

        private void StopAutoFlushThread() {
            lock (_autoFlushSync) {
                Monitor.PulseAll(_autoFlushSync);
            }
            if (_autoFlushThread != null)
                _autoFlushThread.Join();
        }

        private void AutoFlushThread() {
            lock (_autoFlushSync) {
                while (true) {
                    _binaryLogger.AutoFlush();
                    _textLogger.AutoFlush();
                    _xmlLogger.AutoFlush();

                    bool signaled = Monitor.Wait(_autoFlushSync, AUTOFLUSH_CHECK_INTERVAL);
                    if (signaled)
                        break;
                }
            }
        }
    }

    //基本マルチログ実装
    internal class MultiLogSettings : IMultiLogSettings {
        private List<ILogSettings> _data;

        public MultiLogSettings() {
            _data = new List<ILogSettings>();
        }

        public void Add(ILogSettings log) {
            Debug.Assert(log != null);
            _data.Add(log);
        }
        public void Remove(ILogSettings log) {
            if (_data.Contains(log)) {
                _data.Remove(log);
            }
        }
        public void Reset(ILogSettings log) {
            _data.Clear();
            _data.Add(log);
        }

        public ILogSettings Clone() {
            MultiLogSettings ml = new MultiLogSettings();
            foreach (ILogSettings l in _data)
                ml.Add(l.Clone());
            return ml;
        }

        public IAdaptable GetAdapter(Type adapter) {
            return TerminalEmulatorPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        public IEnumerator<ILogSettings> GetEnumerator() {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return _data.GetEnumerator();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public enum LogFileCheckResult {
        Create,
        Append,
        Cancel,
        Error
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public static class LogUtil {
        public static string SelectLogFileByDialog(Form parent) {
            using (SaveFileDialog dlg = new SaveFileDialog()) {
                dlg.AddExtension = true;
                dlg.DefaultExt = "log";
                dlg.Title = "Select Log";
                dlg.Filter = "Log Files(*.log)|*.log|All Files(*.*)|*.*";
                if (dlg.ShowDialog(parent) == DialogResult.OK) {
                    return dlg.FileName;
                }
                return null;
            }
        }

        //既存のファイルであったり、書き込み不可能だったら警告する
        public static LogFileCheckResult CheckLogFileName(string path, Form parent) {
            try {
                StringResource sr = GEnv.Strings;
                if (path.Length == 0) {
                    GUtil.Warning(parent, sr.GetString("Message.CheckLogFileName.EmptyPath"));
                    return LogFileCheckResult.Cancel;
                }

                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir)) {
                    GUtil.Warning(parent, String.Format(sr.GetString("Message.CheckLogFileName.BadPathName"), path));
                    return LogFileCheckResult.Cancel;
                }

                if (File.Exists(path)) {
                    if ((FileAttributes.ReadOnly & File.GetAttributes(path)) != (FileAttributes)0) {
                        GUtil.Warning(parent, String.Format(sr.GetString("Message.CheckLogFileName.NotWritable"), path));
                        return LogFileCheckResult.Cancel;
                    }

                    Poderosa.Forms.ThreeButtonMessageBox mb = new Poderosa.Forms.ThreeButtonMessageBox();
                    mb.Message = String.Format(sr.GetString("Message.CheckLogFileName.AlreadyExist"), path);
                    mb.Text = sr.GetString("Util.CheckLogFileName.Caption");
                    mb.YesButtonText = sr.GetString("Util.CheckLogFileName.OverWrite");
                    mb.NoButtonText = sr.GetString("Util.CheckLogFileName.Append");
                    mb.CancelButtonText = sr.GetString("Util.CheckLogFileName.Cancel");
                    switch (mb.ShowDialog(parent)) {
                        case DialogResult.Cancel:
                            return LogFileCheckResult.Cancel;
                        case DialogResult.Yes: //上書き
                            return LogFileCheckResult.Create;
                        case DialogResult.No:  //追記
                            return LogFileCheckResult.Append;
                        default:
                            break;
                    }
                }

                return LogFileCheckResult.Create; //!!書き込み可能なディレクトリにあることを確認すればなおよし

            }
            catch (Exception ex) {
                GUtil.Warning(parent, ex.Message);
                return LogFileCheckResult.Error;
            }
        }

    }
}
