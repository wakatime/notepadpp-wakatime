using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WakaTime.Forms;
using WakaTime.Properties;

namespace WakaTime
{
    class WakaTime
    {
        #region Fields
        private static string _version = string.Empty;
        private static int _editorVersion;
        private static WakaTimeConfigFile _wakaTimeConfigFile;
        private static SettingsForm _settingsForm;

        private static string _iniFilePath;
        private static int _idMyDlg = -1;
        private static readonly Bitmap TbBmp = Resources.wakatime;

        public static bool Debug;
        public static string ApiKey;
        static readonly PythonCliParameters PythonCliParameters = new PythonCliParameters();
        private static string _lastFile;
        private static DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static readonly object ThreadLock = new object();
        #endregion

        #region StartUp/CleanUp

        internal static void CommandMenuInit()
        {
            _version = string.Format("{0}.{1}.{2}", CoreAssembly.Version.Major, CoreAssembly.Version.Minor, CoreAssembly.Version.Build);

            try
            {
                Logger.Info(string.Format("Initializing WakaTime v{0}", _version));

                _editorVersion = (int)Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETNPPVERSION, 0, 0);
                _settingsForm = new SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;
                _wakaTimeConfigFile = new WakaTimeConfigFile();
                var sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
                Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
                _iniFilePath = sbIniFilePath.ToString();
                if (!Directory.Exists(_iniFilePath)) Directory.CreateDirectory(_iniFilePath);

                Task.Run(() => { InitializeWakaTimeAsync(); });

                GetSettings();

                if (string.IsNullOrEmpty(ApiKey))
                    PromptApiKey();

                // add menu item
                PluginBase.SetCommand(0, "Wakatime Settings", SettingsPopup, new ShortcutKey(false, false, false, Keys.None));
                _idMyDlg = 0;

                Logger.Info(string.Format("Finished initializing WakaTime v{0}", _version));
            }
            catch (Exception ex)
            {
                Logger.Error("Error initializing Wakatime", ex);
            }
        }

        private static void InitializeWakaTimeAsync()
        {
            // Make sure python is installed
            if (!PythonManager.IsPythonInstalled())            
            {
                var dialogResult = MessageBox.Show(@"Let's download and install Python now?", @"WakaTime requires Python", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dialogResult == DialogResult.Yes)
                {
                    var url = PythonManager.PythonDownloadUrl;
                    Downloader.DownloadPython(url, WakaTimeConstants.UserConfigDir);
                }
                else
                    MessageBox.Show(
                        @"Please install Python (https://www.python.org/downloads/) and restart Visual Studio to enable the WakaTime plugin.",
                        @"WakaTime", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            if (DoesCliExist() && IsCliLatestVersion()) return;

            try
            {
                Directory.Delete(string.Format("{0}\\wakatime-master", WakaTimeConstants.UserConfigDir), true);
            }
            catch { /* ignored */ }

            Downloader.DownloadCli(WakaTimeConstants.CliUrl, WakaTimeConstants.UserConfigDir);
        }

        internal static void SetToolBarIcon()
        {
            var tbIcons = new toolbarIcons { hToolbarBmp = TbBmp.GetHbitmap() };
            var pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase.FuncItems.Items[_idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        internal static void PluginCleanUp()
        {
        }
        #endregion

        #region Menu functions
        internal static void PromptApiKey()
        {
            var form = new ApiKeyForm();
            form.ShowDialog();
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }
        #endregion

        #region Methods
        private static void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            _wakaTimeConfigFile.Read();
            GetSettings();
        }

        private static void GetSettings()
        {
            ApiKey = _wakaTimeConfigFile.ApiKey;
            Debug = _wakaTimeConfigFile.Debug;
        }

        public static string GetCurrentFile()
        {
            var currentFile = new StringBuilder(Win32.MAX_PATH);
            return
                (int)Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, currentFile) != -1
                    ? currentFile.ToString()
                    : null;
        }

        public static void HandleActivity(bool isWrite)
        {
            var currentFile = GetCurrentFile();
            if (currentFile == null) return;

            Task.Run(() =>
            {
                lock (ThreadLock)
                {
                    if (!isWrite && _lastFile != null && !EnoughTimePassed() && currentFile.Equals(_lastFile))
                        return;

                    SendHeartbeat(currentFile, isWrite);
                    _lastFile = currentFile;
                    _lastHeartbeat = DateTime.UtcNow;
                }
            });
        }

        public static bool EnoughTimePassed()
        {
            return _lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            PythonCliParameters.Key = ApiKey;
            PythonCliParameters.File = fileName;
            PythonCliParameters.Plugin = string.Format("{0}/{1} {2}/{3}", WakaTimeConstants.EditorName, _editorVersion, WakaTimeConstants.PluginName, _version);
            PythonCliParameters.IsWrite = isWrite;

            var pythonBinary = PythonManager.GetPython();
            if (pythonBinary != null)
            {
                var process = new RunProcess(pythonBinary, PythonCliParameters.ToArray());
                if (Debug)
                {
                    Logger.Debug(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", PythonCliParameters.ToArray(true))));
                    process.Run();
                    Logger.Debug(string.Format("CLI STDOUT: {0}", process.Output));
                    Logger.Debug(string.Format("CLI STDERR: {0}", process.Error));
                }
                else
                    process.RunInBackground();

                if (!process.Success)
                    Logger.Error(string.Format("Could not send heartbeat: {0}", process.Error));
            }
            else
                Logger.Error("Could not send heartbeat because python is not installed");
        }

        private static bool DoesCliExist()
        {
            return File.Exists(PythonCliParameters.Cli);
        }

        private static bool IsCliLatestVersion()
        {
            var process = new RunProcess(PythonManager.GetPython(), PythonCliParameters.Cli, "--version");
            process.Run();

            var wakatimeVersion = WakaTimeConstants.CurrentWakaTimeCliVersion();

            return process.Success && process.Error.Equals(wakatimeVersion);
        }
        #endregion

        static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
