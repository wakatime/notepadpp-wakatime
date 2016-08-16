using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows.Forms;
using Kbg.NppPluginNET.PluginInfrastructure;
using Task = System.Threading.Tasks.Task;
using Timer = System.Timers.Timer;
using System.Text;

namespace WakaTime
{
    class WakaTimePackage
    {
        #region Properties

        static int idMyDlg = -1;
        static Bitmap tbBmp;
        static Timer timer;
        static WakaTimeNppPlugin _plugin = null;

        #endregion

        static WakaTimePackage()
        {
            tbBmp = Properties.Resources.wakatime;
            timer = new Timer();
        }

        internal static void CommandMenuInit()
        {
            // must add menu item in foreground thread
            PluginBase.SetCommand(0, "Wakatime Settings", SettingsPopup, new ShortcutKey(false, false, false, Keys.None));
            idMyDlg = 0;

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
                _plugin = new WakaTimeNppPlugin(default(UnmanagedExports));

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
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_ADDTOOLBARICON, PluginBase._funcItems.Items[idMyDlg]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
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
            // TODO flush heartbeats
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

        public static string GetCurrentFile()
        {
            var currentFile = new StringBuilder(Win32.MAX_PATH);
            return
                (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, 0, currentFile) != -1
                    ? currentFile.ToString()
                    : null;
        }

        public static string GetCurrentProject()
        {
            var currentFile = new StringBuilder(Win32.MAX_PATH);
            return
                (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETCURRENTDIRECTORY, 0, currentFile) != -1
                    ? currentFile.ToString()
                    : null;
        }

        private static void PromptApiKey()
        {
            Logger.Info("Please input your API key into the wakatime window.");
            _plugin.PromptApiKey();
        }

        private static void SettingsPopup()
        {
            _plugin.SettingsPopup();
        }

        #region Events

        public delegate void NotificationEventHandler(object sender, ScNotification args);
        public delegate void FileOperationEventHandler(object sender, FileOperationEventArgs args);

        private static event NotificationEventHandler _onNotified;
        public static event NotificationEventHandler Notified
        {
            add { _onNotified += value; }
            remove { _onNotified -= value; }
        }

        public static void OnNotification(ScNotification notification)
        {
            if (notification.Header.Code == (uint)NppMsg.NPPN_FILESAVED)
            {
                OnFileChanged();
                return;
            }
            else if (notification.Header.Code == (uint)SciMsg.SCN_MODIFIED && (notification.ModificationType & (int)SciMsg.SC_MOD_INSERTTEXT) == (int)SciMsg.SC_MOD_INSERTTEXT)
            {
                OnFileOpened();
                return;
            }

            var handler = _onNotified;
            if (handler != null)
                handler.Invoke(null, notification);
        }

        private static event FileOperationEventHandler _onFileChanged;
        public static event FileOperationEventHandler FileChanged
        {
            add { _onFileChanged += value; }
            remove { _onFileChanged -= value; }
        }

        public static void OnFileChanged()
        {
            var handler = _onFileChanged;
            if (handler != null)
            {
                handler.Invoke(null, new FileOperationEventArgs(GetCurrentFile(), true));
            }
        }

        private static event FileOperationEventHandler _onFileOpened;
        public static event FileOperationEventHandler FileOpened
        {
            add { _onFileOpened += value; }
            remove { _onFileOpened -= value; }
        }

        public static void OnFileOpened()
        {
            var handler = _onFileOpened;
            if (handler != null)
            {
                handler.Invoke(null, new FileOperationEventArgs(GetCurrentFile(), false));
            }
        }

        #endregion
    }
}