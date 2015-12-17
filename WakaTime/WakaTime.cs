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
using System.Net;
using System.Text.RegularExpressions;

namespace WakaTime
{
    class WakaTime
    {
        #region Fields
        private static WakaTimeConfigFile _wakaTimeConfigFile;
        private static SettingsForm _settingsForm;

        private static int _idMyDlg = -1;
        private static readonly Bitmap TbBmp = Resources.wakatime;

        static readonly PythonCliParameters PythonCliParameters = new PythonCliParameters();
        private static string _lastFile;
        private static DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);

        // Settings
        public static bool Debug;
        public static string ApiKey;
        public static string Proxy;

        #endregion

        #region StartUp/CleanUp

        internal static void CommandMenuInit()
        {

            try
            {
                Logger.Info(string.Format("Initializing WakaTime v{0}", WakaTimeConstants.PluginVersion));

                _settingsForm = new SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;
                _wakaTimeConfigFile = new WakaTimeConfigFile();
                var sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
                Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
                GetSettings();

                Task.Run(() => { InitializeWakaTimeAsync(); });

                // add menu item
                PluginBase.SetCommand(0, "Wakatime Settings", SettingsPopup, new ShortcutKey(false, false, false, Keys.None));
                _idMyDlg = 0;

                // prompt for api key if not already set
                if (string.IsNullOrEmpty(ApiKey))
                    PromptApiKey();

                Logger.Info(string.Format("Finished initializing WakaTime v{0}", WakaTimeConstants.PluginVersion));
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
                Downloader.DownloadAndInstallPython();
            }

            if (!DoesCliExist() || !IsCliLatestVersion())
            {
                Downloader.DownloadAndInstallCli();
            }

            if (string.IsNullOrEmpty(ApiKey))
                PromptApiKey();
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
            Proxy = _wakaTimeConfigFile.Proxy;
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
            if (currentFile == null)
                return;

            if (!isWrite && _lastFile != null && !EnoughTimePassed() && currentFile.Equals(_lastFile))
                return;

            Task.Run(() =>
            {
                SendHeartbeat(currentFile, isWrite);
            });

            _lastFile = currentFile;
            _lastHeartbeat = DateTime.UtcNow;
        }

        public static bool EnoughTimePassed()
        {
            return _lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            PythonCliParameters.Key = ApiKey;
            PythonCliParameters.File = fileName;
            PythonCliParameters.Plugin = string.Format("{0}/{1} {2}/{3}", WakaTimeConstants.EditorName, WakaTimeConstants.EditorVersion, WakaTimeConstants.PluginName, WakaTimeConstants.PluginVersion);
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

            if (process.Success)
            {
                var currentVersion = process.Error.Trim();
                Logger.Info(string.Format("Current wakatime-cli version is {0}", currentVersion));

                Logger.Info("Checking for updates to wakatime-cli...");
                var latestVersion = WakaTimeConstants.LatestWakaTimeCliVersion();

                if (currentVersion.Equals(latestVersion))
                {
                    Logger.Info("wakatime-cli is up to date.");
                    return true;
                }
                else
                {
                    Logger.Info(string.Format("Found an updated wakatime-cli v{0}", latestVersion));
                }

            }
            return false;
        }

        public static WebProxy GetProxy()
        {
            WebProxy proxy = null;

            try
            {
                var proxyStr = Proxy;

                // Regex that matches proxy address with authentication
                var regProxyWithAuth = new Regex(@"\s*(https?:\/\/)?([^\s:]+):([^\s:]+)@([^\s:]+):(\d+)\s*");
                var match = regProxyWithAuth.Match(proxyStr);

                if (match.Success)
                {
                    var username = match.Groups[2].Value;
                    var password = match.Groups[3].Value;
                    var address = match.Groups[4].Value;
                    var port = match.Groups[5].Value;

                    var credentials = new NetworkCredential(username, password);
                    proxy = new WebProxy(string.Join(":", address, port), true, null, credentials);

                    Logger.Debug("A proxy with authentication will be used.");
                    return proxy;
                }

                // Regex that matches proxy address and port(no authentication)
                var regProxy = new Regex(@"\s*(https?:\/\/)?([^\s@]+):(\d+)\s*");
                match = regProxy.Match(proxyStr);

                if (match.Success)
                {
                    var address = match.Groups[2].Value;
                    var port = int.Parse(match.Groups[3].Value);

                    proxy = new WebProxy(address, port);

                    Logger.Debug("A proxy will be used.");
                    return proxy;
                }

                Logger.Debug("No proxy will be used. It's either not set or badly formatted.");
            }
            catch (Exception ex)
            {
                Logger.Error("Exception while parsing the proxy string from WakaTime config file. No proxy will be used.", ex);
            }

            return proxy;
        }
        #endregion

        internal static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
