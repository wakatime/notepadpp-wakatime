using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using NppPluginNET;

namespace WakaTime
{
    class Main
    {
        #region " Fields "
        internal const string PluginName = "WakaTime";
        public static string VERSION = "1.0.0";
        public static string PLUGIN_NAME = "notepadpp-wakatime";
        public static string EDITOR_NAME = "notepadpp";
        public static int EDITOR_VERSION = 0;
        public static string CURRENT_PYTHON_VERSION = "3.4.3";
        static string iniFilePath = null;
        static frmMyDlg frmMyDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;
        public static string apiKey = null;
        public static string lastFile = null;
        public static DateTime lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        public static Object threadLock = new Object();
        static bool is64BitProcess = (IntPtr.Size == 8);
        static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            EDITOR_VERSION = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETNPPVERSION, 0, 0);

            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);

            try
            {
                // Make sure python is installed
                if (!isPythonInstalled())
                {
                    string url = getPythonDownloadUrl();
                    Downloader.downloadPython(url, getConfigDir());
                }

                if (!doesCLIExist())
                {
                    string url = "https://github.com/wakatime/wakatime/archive/master.zip";
                    Downloader.downloadCLI(url, getConfigDir());
                }

                apiKey = Config.getApiKey();

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    promptApiKey();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("An exception occured : " + ex.Message);
            }

            // add menu item
            PluginBase.SetCommand(0, "API Key", promptApiKey, new ShortcutKey(false, false, false, Keys.None));
            idMyDlg = 0;
        }
        internal static void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }
        internal static void PluginCleanUp()
        {
        }
        #endregion

        #region " Menu functions "
        internal static void promptApiKey()
        {
            ApiKeyForm form = new ApiKeyForm();
            form.ShowDialog();
        }
        #endregion

        #region " Methods "

        public static string getPythonDownloadUrl()
        {
            string url = "https://www.python.org/ftp/python/" + CURRENT_PYTHON_VERSION + "/python-" + CURRENT_PYTHON_VERSION;
            if (is64BitOperatingSystem)
            {
                url = url + ".amd64";
            }
            url = url + ".msi";
            return url;
        }

        public static string getCurrentFile()
        {
            StringBuilder currentFile = new StringBuilder(Win32.MAX_PATH);
            if ((int)Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, currentFile) != -1)
            {
                return currentFile.ToString();
            }
            return null;
        }

        public static void handleActivity(bool isWrite)
        {
            string currentFile = Main.getCurrentFile();
            if (currentFile != null)
            {
                Thread thread = new Thread(
                    delegate()
                    {
                        lock (Main.threadLock)
                        {

                            if (isWrite || Main.lastFile == null || Main.enoughTimePassed() || !currentFile.Equals(Main.lastFile))
                            {
                                sendHeartbeat(currentFile, isWrite);
                                Main.lastFile = currentFile;
                                Main.lastHeartbeat = DateTime.UtcNow;
                            }

                        }
                    }
                );
                thread.Start();
            }
        }

        public static bool enoughTimePassed()
        {
            if (Main.lastHeartbeat == null || Main.lastHeartbeat < DateTime.UtcNow.AddMinutes(-1))
            {
                return true;
            }
            return false;
        }

        public static string getPython() {
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
            foreach (string location in locations) {
                try {
                    ProcessStartInfo procInfo = new ProcessStartInfo();
                    procInfo.UseShellExecute = false;
                    procInfo.RedirectStandardError = true;
                    procInfo.FileName = location;
                    procInfo.CreateNoWindow = true;
                    procInfo.Arguments = "--version";
                    var proc = Process.Start(procInfo);
                    string errors = proc.StandardError.ReadToEnd();
                    if (errors == null || errors == "") {
                        return location;
                    }
                } catch (Exception ex) { }
            }
            return null;
        }

        public static string getConfigDir()
        {
            StringBuilder pluginConfigDir = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, pluginConfigDir);
            return pluginConfigDir.ToString();
        }

        public static string getCLI()
        {
            return getConfigDir() + "\\wakatime-master\\wakatime\\cli.py";
        }

        public static void sendHeartbeat(string fileName, bool isWrite)
        {
            string arguments = "\"" + getCLI() + "\" --key=\"" + apiKey + "\""
                                + " --file=\"" + fileName + "\""
                                + " --plugin=\"" + EDITOR_NAME + "/" + EDITOR_VERSION.ToString() + " " + PLUGIN_NAME + "/" + Main.VERSION + "\"";
            
            if (isWrite)
                arguments = arguments + " --write";

            ProcessStartInfo procInfo = new ProcessStartInfo();
            procInfo.UseShellExecute = false;
            procInfo.FileName = getPython();
            procInfo.CreateNoWindow = true;
            procInfo.Arguments = arguments;

            try {
                var proc = Process.Start(procInfo);
            } catch (InvalidOperationException ex) {
                Logger.Error("Could not send heartbeat : " + getPython() + " " + arguments);
                Logger.Error("Could not send heartbeat : " + ex.Message);
            } catch (Exception ex) {
                Logger.Error("Could not send heartbeat : " + getPython() + " " + arguments);
                Logger.Error("Could not send heartbeat : " + ex.Message);
            }
        }

        public static bool InternalCheckIsWow64() {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) || Environment.OSVersion.Version.Major >= 6) {
                using (Process p = Process.GetCurrentProcess()) {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal)) {
                        return false;
                    }
                    return retVal;
                }
            } else {
                return false;
            }
        }

        private static bool doesCLIExist()
        {
            if (File.Exists(getCLI())) {
                return true;
            }
            return false;
        }

        private static bool isPythonInstalled()
        {
            if (getPython() != null) {
                return true;
            }
            return false;
        }

        #endregion
    }
}
