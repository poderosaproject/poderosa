/*
 Copyright (c) 2005 Poderosa Project, All Rights Reserved.

 $Id: Main.cs,v 1.8 2012/03/18 12:05:53 kzmi Exp $
*/
using System;
using System.Text;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using Microsoft.Win32;

using Poderosa.Toolkit;
using System.Drawing;

namespace Poderosa.PortForwarding {
    internal class Env {

        public const string VERSION_STRING = "Version 4.3b";

        [STAThread]
        public static void Main(string[] args) {
            if (ActivateAnotherInstance())
                return;
            LoadEnv();
            Run();
            SaveEnv();
        }

        private static ChannelProfileCollection _channels;
        private static Options _options;
        private static MainForm _form;
        private static Commands _commands;
        private static ConnectionManager _manager;
        private static ConnectionLog _log;
        private static StringResources _strings;
        private static IntPtr _globalMutex;

        private static void LoadEnv() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.VisualStyleState = System.Windows.Forms.VisualStyles.VisualStyleState.ClientAndNonClientAreasEnabled;

            string error_msg = null;
            ReloadStringResource();
            _channels = new ChannelProfileCollection();
            _options = new Options();

            _globalMutex = Win32.CreateMutex(IntPtr.Zero, 0, "PoderosaPFGlobalMutex");
            bool already_exists = (Win32.GetLastError() == Win32.ERROR_ALREADY_EXISTS);
            if (_globalMutex == IntPtr.Zero)
                throw new Exception("Global mutex could not open");
            if (Win32.WaitForSingleObject(_globalMutex, 10000) != Win32.WAIT_OBJECT_0)
                throw new Exception("Global mutex lock error");

            try {
                OptionPreservePlace place = GetOptionPreservePlace();
                _options.OptionPreservePlace = place;
                string dir = GetOptionDirectory(place);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string configfile = dir + "portforwarding.conf";
                bool options_loaded = false;
                try {
                    if (File.Exists(configfile)) {
                        Encoding encoding = DetermineConfigFileEncoding(configfile);
                        using (TextReader reader = new StreamReader(File.Open(configfile, FileMode.Open, FileAccess.Read), encoding)) {
                            ConfigNode parent = new ConfigNode("root", reader).FindChildConfigNode("poderosa-portforwarding");
                            if (parent != null) {
                                _channels.Load(parent);
                                _options.Load(parent);
                                options_loaded = true;
                            }
                        }
                    }
                }
                catch (Exception ex) {
                    error_msg = ex.Message;
                }
                finally {
                    if (!options_loaded)
                        _options.Init();
                }

                //ここまできたら言語設定をチェックし、必要なら読み直し
                if (Util.CurrentLanguage != _options.Language) {
                    System.Threading.Thread.CurrentThread.CurrentUICulture = _options.Language == Language.Japanese ? new CultureInfo("ja") : CultureInfo.InvariantCulture;
                }

                _log = new ConnectionLog(dir + "portforwarding.log");
            }
            finally {
                Win32.ReleaseMutex(_globalMutex);
            }
        }

        private static Encoding DetermineConfigFileEncoding(string filename) {
            byte[] data = File.ReadAllBytes(filename);
            try {
                // check if whole content is valid as default encoding
                Encoding enc = Encoding.GetEncoding(0, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                enc.GetString(data);
                return Encoding.Default;
            }
            catch (DecoderFallbackException) {
                // saved by old version ?
                return Encoding.UTF8;
            }
        }

        private static void SaveEnv() {
            if (IsRegistryWritable) {
                RegistryKey g = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH);
                g.SetValue("option-place", _options.OptionPreservePlace.ToString());
            }

            if (Win32.WaitForSingleObject(_globalMutex, 10000) != Win32.WAIT_OBJECT_0)
                throw new Exception("Global mutex lock error");

            try {
                string dir = GetOptionDirectory(_options.OptionPreservePlace);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                string configfile = dir + "portforwarding.conf";
                string configfileTemp = configfile + ".tmp";
                string configfilePrev = configfile + ".prev";
                using (TextWriter wr = new StreamWriter(configfileTemp, false, Encoding.Default)) {
                    ConfigNode root = new ConfigNode("poderosa-portforwarding");
                    _channels.Save(root);
                    _options.Save(root);
                    root.WriteTo(wr);
                }
                if (File.Exists(configfile)) {
                    File.Delete(configfilePrev);
                    File.Move(configfile, configfilePrev);
                }
                File.Move(configfileTemp, configfile);
                _log.Close();
            }
            finally {
                Win32.ReleaseMutex(_globalMutex);
            }
            Win32.CloseHandle(_globalMutex);
        }

        private static void Run() {
            _commands = new Commands();
            _manager = new ConnectionManager();
            _form = new MainForm();
            _form.ShowInTaskbar = _options.ShowInTaskBar;
            Rectangle formRect = AdjustWindowRect(_options.FramePosition);
            _form.Location = formRect.Location;
            _form.Size = formRect.Size;
            _form.WindowState = _options.FrameState;
            _form.RefreshAllProfiles();
            System.Windows.Forms.Application.Run(_form);
        }

        private static Rectangle AdjustWindowRect(Rectangle location) {
            const int MARGIN = 3;
            Rectangle titlebarRect =
                new Rectangle(location.X + MARGIN, location.Y + MARGIN,
                                Math.Max(location.Width - MARGIN * 2, 1),
                                Math.Max(SystemInformation.CaptionHeight - MARGIN * 2, 1));
            bool visible = false;
            foreach (Screen s in Screen.AllScreens) {
                if (s.WorkingArea.IntersectsWith(titlebarRect))
                    visible = true;
            }

            if (!visible) {
                Screen baseScreen = null;
                foreach (Screen s in Screen.AllScreens) {
                    if (s.Bounds.IntersectsWith(location)) {
                        baseScreen = s;
                        break;
                    }
                }
                if (baseScreen == null)
                    baseScreen = Screen.PrimaryScreen;

                Rectangle sb = baseScreen.WorkingArea;
                if (location.Width > sb.Width)
                    location.Width = sb.Width;
                if (location.Height > sb.Height)
                    location.Height = sb.Height;
                location.X = sb.X + (sb.Width - location.Width) / 2;
                location.Y = sb.Y + (sb.Height - location.Height) / 2;
            }

            return location;
        }

        public static void UpdateOptions(Options opt) {
            _form.ShowInTaskbar = opt.ShowInTaskBar;
            if (_options.Language != opt.Language) { //言語のリロードが必要なとき
                System.Threading.Thread.CurrentThread.CurrentUICulture = opt.Language == Language.Japanese ? new CultureInfo("ja") : CultureInfo.InvariantCulture;
                ReloadStringResource();
                _form.ReloadLanguage();
            }
            _options = opt;
        }
        public static void ReloadStringResource() {
            _strings = new StringResources("Portforwarding.strings", typeof(Env).Assembly);
        }

        public static Options Options {
            get {
                return _options;
            }
        }
        public static StringResources Strings {
            get {
                return _strings;
            }
        }

        public static ChannelProfileCollection Profiles {
            get {
                return _channels;
            }
        }
        public static MainForm MainForm {
            get {
                return _form;
            }
        }
        public static Commands Commands {
            get {
                return _commands;
            }
        }
        public static ConnectionManager Connections {
            get {
                return _manager;
            }
        }
        public static ConnectionLog Log {
            get {
                return _log;
            }
        }

        public static string GetOptionDirectory(OptionPreservePlace p) {
            if (p == OptionPreservePlace.InstalledDir) {
                string t = AppDomain.CurrentDomain.BaseDirectory;
                if (Environment.UserName.Length > 0)
                    t += Environment.UserName + "\\";
                return t;
            }
            else
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Poderosa\\";
        }


        private const string REGISTRY_PATH = "Software\\Poderosa Networks\\Portforwarding";

        public static bool IsRegistryWritable {
            get {
                try {
                    RegistryKey g = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH);
                    if (g == null)
                        return false;
                    else
                        return true;
                }
                catch (Exception) {
                    return false;
                }
            }
        }

        private static OptionPreservePlace GetOptionPreservePlace() {
            const OptionPreservePlace DEFAULT_PLACE = OptionPreservePlace.AppData;

            RegistryKey g = Registry.CurrentUser.OpenSubKey(REGISTRY_PATH, false);
            if (g == null)
                return DEFAULT_PLACE;

            string regVal = g.GetValue("option-place", null) as string;
            if (regVal == null)
                return DEFAULT_PLACE;

            try {
                return (OptionPreservePlace)Enum.Parse(typeof(OptionPreservePlace), regVal);
            }
            catch (Exception) {
                return DEFAULT_PLACE;
            }
        }

        public const string WindowTitle = "SSH PortForwarding Gateway";

        private static bool ActivateAnotherInstance() {
            //find target
            unsafe {
                IntPtr hwnd = Win32.FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, null);
                while (hwnd != IntPtr.Zero) {
                    char* buf = stackalloc char[256];
                    Win32.GetWindowText(hwnd, buf, 256);
                    string name = new string(buf);
                    if (name == WindowTitle) {
                        Win32.SetForegroundWindow(hwnd);
                        Win32.ShowWindow(hwnd, Win32.SW_RESTORE);
                        return true;
                    }
                    hwnd = Win32.FindWindowEx(IntPtr.Zero, hwnd, null, null);
                }
            }
            return false;
        }
    }
}
