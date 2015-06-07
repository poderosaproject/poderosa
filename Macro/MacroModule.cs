/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MacroModule.cs,v 1.3 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;

using Poderosa.Preferences;
using Poderosa.Forms;
using Poderosa.Sessions;

namespace Poderosa.MacroInternal {
    internal enum MacroType {
        Unknown,
        JavaScript,
        Assembly
    }

    internal interface IMacroEventListener {
        void IndicateMacroStarted();
        void IndicateMacroFinished();
    }

    internal class MacroModule : ICloneable, IAdaptable {
        private MacroType _type;
        private string _path;
        private string _title;
        private string[] _additionalAssemblies;
        private bool _debugMode;
        private int _index;

        public MacroModule(int index, string path, string title) {
            _index = index;
            this.Path = path;
            _title = title;
            _additionalAssemblies = new string[0];
        }
        public int Index {
            get {
                return _index;
            }
            set {
                _index = value;
            }
        }

        public object Clone() {
            MacroModule m = new MacroModule(_index, _path, _title);
            m._type = _type;
            m._additionalAssemblies = _additionalAssemblies;
            m._debugMode = _debugMode;
            return m;
        }

        public MacroType Type {
            get {
                return _type;
            }
        }
        public string Path {
            get {
                return _path;
            }
            set {
                _path = value;
                string t = System.IO.Path.GetExtension(_path).ToLower();
                if (t.EndsWith("js"))
                    _type = MacroType.JavaScript;
                else if (t.EndsWith("exe") || t.EndsWith("dll"))
                    _type = MacroType.Assembly;
                else
                    _type = MacroType.Unknown;
            }
        }
        public string Title {
            get {
                return _title;
            }
            set {
                _title = value;
            }
        }

        public string[] AdditionalAssemblies {
            get {
                return _additionalAssemblies;
            }
            set {
                _additionalAssemblies = value;
            }
        }
        public bool DebugMode {
            get {
                return _debugMode;
            }
            set {
                _debugMode = value;
            }
        }

        public string FormatAdditionalAssemblies() {
            StringBuilder b = new StringBuilder();
            foreach (string t in _additionalAssemblies) {
                if (b.Length > 0)
                    b.Append(';');
                b.Append(t);
            }
            return b.ToString();
        }

        public IAdaptable GetAdapter(Type adapter) {
            return MacroPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }

    internal class MacroManager : IPreferenceSupplier, IMacroEventListener {
        private List<MacroModule> _entries;
        private bool _touchedPreference;
        private MacroExecutor _runningMacro;


        private IMacroEventListener _macroListener;

        public MacroManager() {
            _entries = new List<MacroModule>();
            //_environmentVariables = new Hashtable();
        }
        public IEnumerable<MacroModule> Modules {
            get {
                if (!_touchedPreference)
                    LoadFromPreference(); //遅延評価
                return _entries;
            }
        }
        public int ModuleCount {
            get {
                if (!_touchedPreference)
                    LoadFromPreference(); //遅延評価
                return _entries.Count;
            }
        }

        /*
        public IDictionaryEnumerator EnvironmentVariables {
            get {
                return _environmentVariables.GetEnumerator();
            }
        }
        public string GetVariable(string name, string defaultvalue) {
            object t = _environmentVariables[name];
            return t == null ? defaultvalue : (string)t;
        }
        public void ResetEnvironmentVariables(Hashtable newmap) {
            _environmentVariables = newmap;
        }
         */

        public bool MacroIsRunning {
            get {
                return _runningMacro != null;
            }
        }
        public MacroExecutor CurrentExecutor {
            get {
                return _runningMacro;
            }
        }
        public void SetMacroEventListener(IMacroEventListener f) {
            _macroListener = f;
        }

        public void Execute(Form parent, MacroModule mod) {
            StringResource sr = MacroPlugin.Instance.Strings;
            if (_runningMacro != null) {
                GUtil.Warning(parent, sr.GetString("Message.MacroModule.AlreadyRunning"));
                return;
            }

            if (mod.Type == MacroType.Unknown) {
                GUtil.Warning(parent, sr.GetString("Message.MacroModule.UnknownModuleType"));
                return;
            }
            else {
                _runningMacro = MacroPlugin.Instance.RunMacroModule(mod, null, this);
            }
        }

        public void StopMacro() {
            if (_runningMacro == null)
                return;
            _runningMacro.Abort();
        }

        public MacroModule GetModule(int index) {
            return (MacroModule)_entries[index];
        }

        public void AddModule(MacroModule mod) {
            _entries.Add(mod);
        }
        public void RemoveModule(MacroModule mod) {
            _entries.Remove(mod);
        }
        public void RemoveAt(int n) {
            _entries.RemoveAt(n);
        }
        public void InsertModule(int n, MacroModule mod) {
            _entries.Insert(n, mod);
        }
        public void ReplaceModule(MacroModule old, MacroModule module) {
            int i = _entries.IndexOf(old);
            Debug.Assert(i != -1);
            _entries[i] = module;
        }

        #region IMacroEventListener

        public void IndicateMacroStarted() {
            if (_macroListener != null)
                _macroListener.IndicateMacroStarted();
        }

        public void IndicateMacroFinished() {
            _runningMacro = null;
            if (_macroListener != null)
                _macroListener.IndicateMacroFinished();
        }

        #endregion

        public void LoadFromPreference() {
            IPreferenceFolderArray ma = _rootPreference.FindChildFolderArray(_moduleDefinitionTemplate.Id);
            if (ma == null || ma.Folders.Length == 0)
                InitSample();
            else {
                _entries.Clear();
                foreach (IPreferenceFolder f in ma.Folders) {
                    MacroModule m = new MacroModule(_entries.Count,
                        ma.ConvertItem(f, _pathPreferenceTemplate).AsString().Value,
                        ma.ConvertItem(f, _titlePreferenceTemplate).AsString().Value);
                    m.AdditionalAssemblies = ma.ConvertItem(f, _additionalAssembliesPreferenceTemplate).AsString().Value.Split(';');
                    m.DebugMode = ma.ConvertItem(f, _tracePreferenceTemplate).AsBool().Value;
                    _entries.Add(m);
                }
            }

            _touchedPreference = true;
        }
        public void SaveToPreference() {
            IPreferenceFolderArray ma = _rootPreference.FindChildFolderArray(_moduleDefinitionTemplate.Id);
            ma.Clear();
            foreach (MacroModule m in _entries) {
                IPreferenceFolder f = ma.CreateNewFolder();
                ma.ConvertItem(f, _pathPreferenceTemplate).AsString().Value = m.Path;
                ma.ConvertItem(f, _titlePreferenceTemplate).AsString().Value = m.Title;
                ma.ConvertItem(f, _additionalAssembliesPreferenceTemplate).AsString().Value = m.FormatAdditionalAssemblies();
                ma.ConvertItem(f, _tracePreferenceTemplate).AsBool().Value = m.DebugMode;
            }
        }

        private void InitSample() {
            StringResource sr = MacroPlugin.Instance.Strings;
            string b = MacroPlugin.Instance.PoderosaApplication.HomeDirectory + "Macro\\Sample\\";

            _entries.Clear();

            MacroModule hello = new MacroModule(0, b + "helloworld.js", sr.GetString("Caption.MacroModule.SampleTitleHelloWorld"));
            _entries.Add(hello);

            MacroModule telnet = new MacroModule(1, b + "telnet.js", sr.GetString("Caption.MacroModule.SampleTitleAutoTelnet"));
            _entries.Add(telnet);

            MacroModule bashrc = new MacroModule(2, b + "bashrc.js", sr.GetString("Caption.MacroModule.SampleTitleOpenBashrc"));
            _entries.Add(bashrc);
        }

        public void ReloadLanguage() {
            /*
            string b = AppDomain.CurrentDomain.BaseDirectory + "macrosample\\";
            string helloworld = b + "helloworld.js";
            string autotelnet = b + "telnet.js";
            string openbashrc = b + "bashrc.js";
            foreach (MacroModule mod in _entries) {
                if (mod.Path == helloworld)
                    mod.Title = GApp.Strings.GetString("Caption.MacroModule.SampleTitleHelloWorld");
                else if (mod.Path == autotelnet)
                    mod.Title = GApp.Strings.GetString("Caption.MacroModule.SampleTitleAutoTelnet");
                else if (mod.Path == openbashrc)
                    mod.Title = GApp.Strings.GetString("Caption.MacroModule.SampleTitleOpenBashrc");
            }
             */
        }

        #region IPreferenceSupplier

        private IPreferenceFolder _rootPreference;

        private IPreferenceFolder _moduleDefinitionTemplate;
        private IStringPreferenceItem _pathPreferenceTemplate;
        private IStringPreferenceItem _titlePreferenceTemplate;
        private IStringPreferenceItem _additionalAssembliesPreferenceTemplate;
        private IBoolPreferenceItem _tracePreferenceTemplate;

        public string PreferenceID {
            get {
                return MacroPlugin.PLUGIN_ID;
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _rootPreference = folder;

            _moduleDefinitionTemplate = builder.DefineFolderArray(folder, this, "modules");
            _pathPreferenceTemplate = builder.DefineStringValue(_moduleDefinitionTemplate, "path", "", null);
            _titlePreferenceTemplate = builder.DefineStringValue(_moduleDefinitionTemplate, "title", "", null);
            _additionalAssembliesPreferenceTemplate = builder.DefineStringValue(_moduleDefinitionTemplate, "additionalAssemblies", "", null);
            _tracePreferenceTemplate = builder.DefineBoolValue(_moduleDefinitionTemplate, "trace", false, null);
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public string GetDescription(IPreferenceItem item) {
            return "";
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }

        #endregion

    }

}
