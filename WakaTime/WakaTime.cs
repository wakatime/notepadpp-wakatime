using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WakaTime.Properties;

namespace WakaTime
{
    class WakaTime
    {
        #region Fields
        internal const string NativeName = "WakaTime";
        private const string CurrentWakaTimeCLIVersion = "4.0.13"; // https://github.com/wakatime/wakatime/blob/master/HISTORY.rst
        private const string _cliUrl = "https://github.com/wakatime/wakatime/archive/master.zip";
        private static string _version = CoreAssembly.Version.Major.ToString() + '.' + CoreAssembly.Version.Minor.ToString() + '.' + CoreAssembly.Version.Build.ToString();
        internal const string UserAgent = "notepadpp-wakatime";
        private const string EditorName = "notepadpp";
        private const string CurrentPythonVersion = "3.4.3";
        private static int _editorVersion;
        private static string _pythonBinaryLocation = null;

        private static string _iniFilePath;
        private static int _idMyDlg = -1;
        private static readonly Bitmap TbBmp = Resources.wakatime;
        public static string ApiKey;
        private static string _lastFile;
        private static DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static readonly object ThreadLock = new object();
        static readonly bool Is64BitProcess = (IntPtr.Size == 8);
        static readonly bool Is64BitOperatingSystem = Is64BitProcess || InternalCheckIsWow64();        
        #endregion

        #region StartUp/CleanUp
        internal static void CommandMenuInit()
        {
            _editorVersion = (int)Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETNPPVERSION, 0, 0);

            var sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            _iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(_iniFilePath)) Directory.CreateDirectory(_iniFilePath);

            try
            {
                // Make sure python is installed
                if (!IsPythonInstalled())
                {
                    var url = GetPythonDownloadUrl();
                    Downloader.DownloadPython(url, GetConfigDir());
                }

                if (!DoesCliExist() || !IsCliLatestVersion())
                {
                    try
                    {
                        Directory.Delete(GetConfigDir() + "\\wakatime-master", true);
                    }
                    catch { /* ignored */ }
                    Downloader.DownloadCli(_cliUrl, GetConfigDir());
                }

                ApiKey = Config.GetApiKey();

                if (string.IsNullOrWhiteSpace(ApiKey))
                    PromptApiKey();
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured : " + ex.Message);
            }

            // add menu item
            PluginBase.SetCommand(0, "API Key", PromptApiKey, new ShortcutKey(false, false, false, Keys.None));
            _idMyDlg = 0;
        }
        internal static void SetToolBarIcon()
        {
            var tbIcons = new toolbarIcons {hToolbarBmp = TbBmp.GetHbitmap()};
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
        #endregion

        #region Methods

        public static string GetPythonDownloadUrl()
        {
            var url = "https://www.python.org/ftp/python/" + CurrentPythonVersion + "/python-" + CurrentPythonVersion;

            if (Is64BitOperatingSystem)
                url = url + ".amd64";

            url = url + ".msi";
            return url;
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

            var thread = new Thread(
                delegate()
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
            thread.Start();
        }

        public static bool EnoughTimePassed()
        {
            return _lastHeartbeat < DateTime.UtcNow.AddMinutes(-1);
        }

        public static string GetPython()
        {
            if (_pythonBinaryLocation != null)
                return _pythonBinaryLocation;

            string[] locations = {
                "pythonw",
                "python",
                "\\Python37\\pythonw",
                "\\Python36\\pythonw",
                "\\Python35\\pythonw",
                "\\Python34\\pythonw",
                "\\Python33\\pythonw",
                "\\Python32\\pythonw",
                "\\Python31\\pythonw",
                "\\Python30\\pythonw",
                "\\Python27\\pythonw",
                "\\Python26\\pythonw",
                "\\python37\\pythonw",
                "\\python36\\pythonw",
                "\\python35\\pythonw",
                "\\python34\\pythonw",
                "\\python33\\pythonw",
                "\\python32\\pythonw",
                "\\python31\\pythonw",
                "\\python30\\pythonw",
                "\\python27\\pythonw",
                "\\python26\\pythonw",
                "\\Python37\\python",
                "\\Python36\\python",
                "\\Python35\\python",
                "\\Python34\\python",
                "\\Python33\\python",
                "\\Python32\\python",
                "\\Python31\\python",
                "\\Python30\\python",
                "\\Python27\\python",
                "\\Python26\\python",
                "\\python37\\python",
                "\\python36\\python",
                "\\python35\\python",
                "\\python34\\python",
                "\\python33\\python",
                "\\python32\\python",
                "\\python31\\python",
                "\\python30\\python",
                "\\python27\\python",
                "\\python26\\python",
            };
            foreach (string location in locations)
            {
                RunProcess process = new RunProcess(location, "--version");

                process.Run();

                if (process.OK())
                {
                    if (process.Output().StartsWith("Python "))
                    {
                        _pythonBinaryLocation = location;
                        return location;
                    }
                }
            }
            return null;
        }

        public static string GetConfigDir()
        {
            var pluginConfigDir = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, pluginConfigDir);
            return pluginConfigDir.ToString();
        }

        public static string GetCli()
        {
            return GetConfigDir() + "\\wakatime-master\\wakatime\\cli.py";
        }

        public static void SendHeartbeat(string fileName, bool isWrite)
        {
            List<String> arguments = new List<String>();

            arguments.Add(GetCli());
            arguments.Add("--key");
            arguments.Add(ApiKey);
            arguments.Add("--file");
            arguments.Add(fileName);
            arguments.Add("--plugin");
            arguments.Add(EditorName + "/" + _editorVersion + " " + UserAgent + "/" + _version);

            if (isWrite)
                arguments.Add("--write");

            RunProcess process = new RunProcess(GetPython(), arguments.ToArray());
            process.RunInBackground();
        }

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major != 5 || Environment.OSVersion.Version.Minor < 1) &&
                Environment.OSVersion.Version.Major < 6) return false;

            using (var p = Process.GetCurrentProcess())
            {
                bool retVal;
                return NativeMethods.IsWow64Process(p.Handle, out retVal) && retVal;
            }
        }

        private static bool DoesCliExist()
        {
            return File.Exists(GetCli());
        }

        private static bool IsCliLatestVersion()
        {
            RunProcess process = new RunProcess(GetPython(), GetCli(), "--version");
            process.Run();
            if (process.OK() && process.Error().Equals(CurrentWakaTimeCLIVersion))
            {
                return true;
            }

            return false;
        }

        private static bool IsPythonInstalled()
        {
            return GetPython() != null;
        }

        static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
        #endregion
    }
}
