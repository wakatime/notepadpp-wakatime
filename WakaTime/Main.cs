using System;
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
        static string iniFilePath = null;
        static frmMyDlg frmMyDlg = null;
        static int idMyDlg = -1;
        static Bitmap tbBmp = Properties.Resources.star;
        static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        static Icon tbIcon = null;
        public static string apiKey = null;
        public static Logger log = new Logger();
        public static string lastFile = null;
        public static DateTime lastHeartbeat = DateTime.UtcNow.AddMinutes(-3);
        public static Object threadLock = new Object();
        #endregion

        #region " StartUp/CleanUp "
        internal static void CommandMenuInit()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            iniFilePath = sbIniFilePath.ToString();
            if (!Directory.Exists(iniFilePath)) Directory.CreateDirectory(iniFilePath);
            iniFilePath = Path.Combine(iniFilePath, PluginName + ".ini");

            PluginBase.SetCommand(0, "API Key", myMenuFunction, new ShortcutKey(false, false, false, Keys.None));
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
            log.Dispose();
        }
        #endregion

        #region " Menu functions "
        internal static void myMenuFunction()
        {
            ApiKeyForm form = new ApiKeyForm();
            form.ShowDialog();
        }
        #endregion
        
        #region " Methods "
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
                                MessageBox.Show(currentFile);
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
        #endregion
    }
}
