using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Net;
using Task = System.Threading.Tasks.Task;
using System.Collections.Concurrent;
using System.Collections;
using System.Timers;
using System.Web.Script.Serialization;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace WakaTime
{
    internal class WakaTimePackage
    {
        #region Properties
        internal const string PluginName = Constants.PluginName;

        private static int _idMyDlg = -1;
        private static readonly Bitmap TbBmp = Properties.Resources.wakatime;

        private static readonly PythonCliParameters PythonCliParameters = new PythonCliParameters();
        internal static ConfigFile Config;
        private static Forms.SettingsForm _settingsForm;
        private static string _lastFile;
        private static DateTime _lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        private const int HeartbeatFrequency = 2; // minutes
        
        private static readonly ConcurrentQueue<Heartbeat> HeartbeatQueue = new ConcurrentQueue<Heartbeat>();
        private static System.Timers.Timer _timer = new System.Timers.Timer();
        #endregion

        internal static void CommandMenuInit()
        {
            // must add menu item in foreground thread
            PluginBase.SetCommand(0, "Wakatime Settings", SettingsPopup, new ShortcutKey(false, false, false, Keys.None));
            _idMyDlg = 0;

            // finish initializing in background thread
            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        private static void InitializeAsync()
        {
            try
            {
                Logger.Info($"Initializing WakaTime v{Constants.PluginVersion}");

                // Settings Form
                _settingsForm = new Forms.SettingsForm();
                _settingsForm.ConfigSaved += SettingsFormOnConfigSaved;

                // Load config file
                Config = new ConfigFile();                

                // Prompt for api key if not already set
                if (string.IsNullOrEmpty(Config.ApiKey))
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
                _timer.Interval = 1000 * 8;
                _timer.Elapsed += ProcessHeartbeats;
                _timer.Start();

                Logger.Info($"Finished initializing WakaTime v{Constants.PluginVersion}");
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
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON,
                PluginBase._funcItems.Items[_idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        public static void OnNotification(ScNotification notification)
        {
            switch (notification.Header.Code)
            {
                case (uint)NppMsg.NPPN_FILESAVED:
                    HandleActivity(GetCurrentFile(), true);
                    break;
                case (uint)SciMsg.SCN_MODIFIED when (notification.ModificationType & (int)SciMsg.SC_MOD_INSERTTEXT) == (int)SciMsg.SC_MOD_INSERTTEXT:
                    HandleActivity(GetCurrentFile(), false);
                    break;
            }
        }

        public static void HandleActivity(string currentFile, bool isWrite)
        {
            try
            {
                if (currentFile == null)
                    return;

                var now = DateTime.UtcNow;

                if (!isWrite && _lastFile != null && !EnoughTimePassed(now) && currentFile.Equals(_lastFile))
                    return;

                _lastFile = currentFile;
                _lastHeartbeat = now;

                AppendHeartbeat(currentFile, isWrite, now);
            }
            catch (Exception ex)
            {
                Logger.Error("Error appending heartbeat", ex);
            }
        }

        private static void AppendHeartbeat(string fileName, bool isWrite, DateTime time)
        {
            Task.Run(() =>
            {
                try
                {
                    var h = new Heartbeat
                    {
                        entity = fileName,
                        timestamp = ToUnixEpoch(time),
                        is_write = isWrite
                    };
                    HeartbeatQueue.Enqueue(h);
                }
                catch (Exception ex)
                {
                    Logger.Error("Error appending heartbeat", ex);
                }
            });
        }

        private static void ProcessHeartbeats(object sender, ElapsedEventArgs e)
        {
            Task.Run(() =>
            {
                try
                {
                    ProcessHeartbeats();
                }
                catch (Exception ex)
                {
                    Logger.Error("Error processing heartbeats", ex);
                }
            });
        }

        private static void ProcessHeartbeats()
        {
            var pythonBinary = Dependencies.GetPython();
            Heartbeat h;
            if (pythonBinary != null)
            {
                // get first heartbeat from queue
                var gotOne = HeartbeatQueue.TryDequeue(out var heartbeat);
                if (!gotOne)
                    return;

                // remove all extra heartbeats from queue
                var extraHeartbeats = new ArrayList();
                while (HeartbeatQueue.TryDequeue(out h))
                    extraHeartbeats.Add(new Heartbeat(h));
                var hasExtraHeartbeats = extraHeartbeats.Count > 0;

                PythonCliParameters.Key = Config.ApiKey;
                PythonCliParameters.Plugin =
                    $"{Constants.EditorName}/{Constants.EditorVersion} {Constants.PluginKey}/{Constants.PluginVersion}";
                PythonCliParameters.File = heartbeat.entity;
                PythonCliParameters.Time = heartbeat.timestamp;
                PythonCliParameters.IsWrite = heartbeat.is_write;
                PythonCliParameters.HasExtraHeartbeats = hasExtraHeartbeats;

                string extraHeartbeatsJson = null;
                if (hasExtraHeartbeats)
                    extraHeartbeatsJson = new JavaScriptSerializer().Serialize(extraHeartbeats);

                var process = new RunProcess(pythonBinary, PythonCliParameters.ToArray());
                if (Config.Debug)
                {
                    Logger.Debug(
                        $"[\"{pythonBinary}\", \"{string.Join("\", \"", PythonCliParameters.ToArray(true))}\"]");
                    process.Run(extraHeartbeatsJson);

                    if (!string.IsNullOrEmpty(process.Output))
                        Logger.Debug(process.Output);

                    if (!string.IsNullOrEmpty(process.Error))
                        Logger.Debug(process.Error);
                }
                else
                    process.RunInBackground(extraHeartbeatsJson);

                if (!process.Success)
                {
                    Logger.Error("Could not send heartbeat.");
                    if (!string.IsNullOrEmpty(process.Output))
                        Logger.Error(process.Output);

                    if (!string.IsNullOrEmpty(process.Error))
                        Logger.Error(process.Error);
                }
            }
            else
                Logger.Error("Could not send heartbeat because python is not installed");
        }

        public static string GetCurrentFile()
        {
            var currentFile = new StringBuilder(Win32.MAX_PATH);
            return
                (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, 0, currentFile) != -1
                    ? currentFile.ToString()
                    : null;
        }

        public static bool EnoughTimePassed(DateTime now)
        {
            return _lastHeartbeat < now.AddMinutes(-1 * HeartbeatFrequency);
        }

        private static void SettingsFormOnConfigSaved(object sender, EventArgs eventArgs)
        {
            Config.Read();
        }        

        private static void PromptApiKey()
        {
            Logger.Info("Please input your api key into the wakatime window.");
            var form = new Forms.ApiKeyForm();
            form.ShowDialog();
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }

        private static string ToUnixEpoch(DateTime date)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var timestamp = date - epoch;
            var seconds = Convert.ToInt64(Math.Floor(timestamp.TotalSeconds));
            var milliseconds = timestamp.ToString("ffffff");
            return $"{seconds}.{milliseconds}";
        }

        public static WebProxy GetProxy()
        {
            WebProxy proxy = null;

            try
            {
                var proxyStr = Config.Proxy;

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
                    proxy = new WebProxy(string.Join(":", new string[] { address, port }), true, null, credentials);

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
            private static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }

        internal static void PluginCleanUp()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= ProcessHeartbeats;
                _timer.Dispose();
                _timer = null;

                // make sure the queue is empty
                ProcessHeartbeats();
            }
        }
    }
}