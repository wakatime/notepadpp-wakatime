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
    class WakaTimePackage
    {
        #region Fields
        private static ConfigFile _wakaTimeConfigFile;
        private static SettingsForm _settingsForm;

        private static int _idMyDlg = -1;
        private static readonly Bitmap TbBmp = Resources.wakatime;

        static readonly PythonCliParameters PythonCliParameters = new PythonCliParameters();
        private static string _lastFile;
        private static DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private static int heartbeatFrequency = 2; // minutes

        // Settings
        public static bool Debug;
        public static string ApiKey;
        public static string Proxy;

        private static ConcurrentQueue<Heartbeat> heartbeatQueue = new ConcurrentQueue<Heartbeat>();
        private static Timer timer = new Timer();

        #endregion

        #region StartUp/CleanUp

        internal static void CommandMenuInit()
        {
            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        public void InitializeAsync()
        {

            try
            {
                Logger.Info(string.Format("Initializing WakaTime v{0}", Constants.PluginVersion));

                // add menu item
                PluginBase.SetCommand(0, "Wakatime Settings", SettingsPopup, new ShortcutKey(false, false, false, Keys.None));
                _idMyDlg = 0;

                // Settings Form
                _settingsForm = new SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;
                
                // Load config file
                _wakaTimeConfigFile = new ConfigFile();
                GetSettings();

                // Prompt for api key if not already set
                if (string.IsNullOrEmpty(ApiKey))
                    PromptApiKey();

                try
                {
                    // Make sure python is installed
                    if (!Dependencies.IsPythonInstalled())
                    {
                        Dependencies.DownloadAndInstallPython();
                    }

                    if (!Dependencies.DoesCliExist() || !Dependencies.IsCliUpToDate())
                    {
                        Dependencies.DownloadAndInstallCli();
                    }
                }
                catch (WebException ex)
                {
                    Logger.Error("Are you behind a proxy? Try setting a proxy in WakaTime Settings with format https://user:pass@host:port. Exception Traceback:", ex);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error detecting dependencies. Exception Traceback:", ex);
                }                

                // setup timer to process queued heartbeats every 8 seconds
                timer.Interval = 1000 * 8;
                timer.Elapsed += ProcessHeartbeats;
                timer.Start();

                Logger.Info(string.Format("Finished initializing WakaTime v{0}", Constants.PluginVersion));
            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing WakaTime", ex);
            }
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
            if (timer != null)
            {
                timer.Stop();
                timer.Elapsed -= ProcessHeartbeats;
                timer.Dispose();
                timer = null;

                // make sure the queue is empty
                ProcessHeartbeats();
            }
        }
        #endregion

        #region Methods
        public static string GetCurrentFile()
        {
            var currentFile = new StringBuilder(Win32.MAX_PATH);
            return
                (int)Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, currentFile) != -1
                    ? currentFile.ToString()
                    : null;
        }

        private void HandleActivity(string currentFile, bool isWrite)
        {
            if (currentFile == null)
                return;

            DateTime now = DateTime.UtcNow;

            if (!isWrite && _lastFile != null && !EnoughTimePassed(now) && currentFile.Equals(_lastFile))
                return;

            _lastFile = currentFile;
            _lastHeartbeat = now;
            
            AppendHeartbeat(currentFile, isWrite, now);
        }

        public static void AppendHeartbeat(string fileName, bool isWrite, DateTime time)
        {
            Task.Run(() =>
            {
                Heartbeat h = new Heartbeat();
                h.entity = fileName;
                h.timestamp = ToUnixEpoch(time);
                h.is_write = isWrite;
                h.project = GetProjectName();
                heartbeatQueue.Enqueue(h);
            });
        }

        private void ProcessHeartbeats(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                ProcessHeartbeats();
            });
        }

        private void ProcessHeartbeats()
        {
            var pythonBinary = Dependencies.GetPython();
            if (pythonBinary != null)
            {
                // get first heartbeat from queue
                Heartbeat heartbeat;
                bool gotOne = heartbeatQueue.TryDequeue(out heartbeat);
                if (!gotOne)
                    return;

                // remove all extra heartbeats from queue
                ArrayList extraHeartbeats = new ArrayList();
                Heartbeat h;
                while (heartbeatQueue.TryDequeue(out h))
                    extraHeartbeats.Add(new Heartbeat(h));
                bool hasExtraHeartbeats = extraHeartbeats.Count > 0;

                PythonCliParameters.Key = ApiKey;
                PythonCliParameters.Plugin = string.Format("{0}/{1} {2}/{3}", Constants.EditorName, Constants.EditorVersion, Constants.PluginName, Constants.PluginVersion);
                PythonCliParameters.File = heartbeat.entity;
                PythonCliParameters.Time = heartbeat.timestamp;
                PythonCliParameters.IsWrite = heartbeat.is_write;
                PythonCliParameters.Project = heartbeat.project;
                PythonCliParameters.HasExtraHeartbeats = hasExtraHeartbeats;

                string extraHeartbeatsJSON = null;
                if (hasExtraHeartbeats)
                    extraHeartbeatsJSON = new JavaScriptSerializer().Serialize(extraHeartbeats);

                var process = new RunProcess(pythonBinary, PythonCliParameters.ToArray());
                if (Debug)
                {
                    Logger.Debug(string.Format("[\"{0}\", \"{1}\"]", pythonBinary, string.Join("\", \"", PythonCliParameters.ToArray(true))));
                    process.Run(extraHeartbeatsJSON);
                    if (process.Output != null && process.Output != "")
                        Logger.Debug(process.Output);
                    if (process.Error != null && process.Error != "")
                        Logger.Debug(process.Error);
                }
                else
                    process.RunInBackground(extraHeartbeatsJSON);

                if (!process.Success)
                {
                    Logger.Error("Could not send heartbeat.");
                    if (process.Output != null && process.Output != "")
                        Logger.Error(process.Output);
                    if (process.Error != null && process.Error != "")
                        Logger.Error(process.Error);
                }
            }
            else
                Logger.Error("Could not send heartbeat because python is not installed");
        }

        private bool EnoughTimePassed(DateTime now)
        {
            return _lastHeartbeat < now.AddMinutes(-1 * heartbeatFrequency);
        }

        private static void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            GetSettings();
        }

        private static void GetSettings()
        {
            _wakaTimeConfigFile.Read();
            ApiKey = _wakaTimeConfigFile.ApiKey;
            Debug = _wakaTimeConfigFile.Debug;
            Proxy = _wakaTimeConfigFile.Proxy;
        }

        private static void PromptApiKey()
        {
            Logger.Info("Please input your api key into the wakatime window.");
            var form = new ApiKeyForm();
            form.ShowDialog();            
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }

        private static string GetProjectName()
        {
            return !string.IsNullOrEmpty(_solutionName)
                ? Path.GetFileNameWithoutExtension(_solutionName)
                : (ObjDte.Solution != null && !string.IsNullOrEmpty(ObjDte.Solution.FullName))
                    ? Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName)
                    : string.Empty;
        }

        private static string ToUnixEpoch(DateTime date)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timestamp = date - epoch;
            long seconds = Convert.ToInt64(Math.Floor(timestamp.TotalSeconds));
            string milliseconds = timestamp.ToString("ffffff");
            return string.Format("{0}.{1}", seconds, milliseconds);
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

        internal static class CoreAssembly
        {
            static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
        #endregion
    }
}
