using Kbg.NppPluginNET.PluginInfrastructure;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using WakaTime.Forms;

namespace WakaTime
{
    internal class WakaTimePackage
    {
        private static WakaTime _wakaTime;
        private static SettingsForm _settingsForm;

        private static void Initialize()
        {
            var metadata = new Metadata
            {
                EditorName = "notepadpp",
                PluginName = "notepadpp-wakatime",
                EditorVersion = EditorVersion,
                PluginVersion = PluginVersion
            };

            _wakaTime = new WakaTime(metadata, new Logger(Dependencies.GetConfigFilePath()));
            _wakaTime.Initialize();

            _settingsForm = new SettingsForm(_wakaTime.Config, _wakaTime.Logger);

            if (string.IsNullOrEmpty(_wakaTime.Config.GetSetting("api_key")))
                PromptApiKey();
        }

        internal static void CommandMenuInit()
        {
            PluginBase.SetCommand(0, "Settings", SettingsPopup, new ShortcutKey(false, false, false, Keys.None));
        }

        private static void SettingsPopup()
        {
            _settingsForm.ShowDialog();
        }

        private static void PromptApiKey()
        {
            var form = new ApiKeyForm(_wakaTime.Config, _wakaTime.Logger);
            form.ShowDialog();
        }

        private static string GetCurrentFile()
        {
            var currentFile = new StringBuilder(Win32.MAX_PATH);
            return
                (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETFULLCURRENTPATH, 0, currentFile) != -1
                    ? currentFile.ToString()
                    : null;
        }

        public static void OnNppNotification(ScNotification nc, IntPtr ptrPluginName)
        {
            switch (nc.Header.Code)
            {
                case (uint)NppMsg.NPPN_TBMODIFICATION:
                    PluginBase._funcItems.RefreshItems();
                    SetToolBarIcon();
                    return;
                case (uint)NppMsg.NPPN_READY:
                    Initialize();
                    return;
                case (uint)NppMsg.NPPN_FILESAVED:
                    _wakaTime.HandleActivity(GetCurrentFile(), true);
                    return;
                case (uint)SciMsg.SCN_MODIFIED when (nc.ModificationType & (int)SciMsg.SC_MOD_INSERTTEXT) == (int)SciMsg.SC_MOD_INSERTTEXT:
                    _wakaTime.HandleActivity(GetCurrentFile(), false);
                    return;
                case (uint)NppMsg.NPPN_SHUTDOWN:
                    ShutDown();
                    Marshal.FreeHGlobal(ptrPluginName);
                    return;
            }
        }

        internal static void SetToolBarIcon()
        {
            var tbIcons = new toolbarIcons { hToolbarBmp = ImageResources.WakaTime.GetHbitmap() };
            var pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(
                PluginBase.nppData._nppHandle,
                (uint)NppMsg.NPPM_ADDTOOLBARICON,
                PluginBase._funcItems.Items[0]._cmdID,
                pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        private static string EditorVersion
        {
            get
            {
                var msgPtr = Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETNPPVERSION, 0, 0);

                try
                {
                    var num = Convert.ToInt32(msgPtr.ToString());
                    var high = num >> 16;
                    var low = num & 0xFFFF;

                    return $"{high}.{string.Join(".", low.ToString().ToCharArray())}";
                }
                catch
                {
                    return "0";
                }
            }
        }

        private static void ShutDown()
        {
            _wakaTime?.Dispose();
        }

        private static string PluginVersion
        {
            get
            {
                var version = CoreAssembly.Version;

                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }


        internal static class CoreAssembly
        {
            private static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
