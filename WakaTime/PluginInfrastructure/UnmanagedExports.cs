// NPP plugin platform for .Net v0.92.83 by Kasper B. Graversen etc.
using System;
using System.Runtime.InteropServices;
using Kbg.NppPluginNET.PluginInfrastructure;
using NppPlugin.DllExport;

namespace WakaTime
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention=CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            PluginBase.nppData = notepadPlusData;
            WakaTimePackage.CommandMenuInit();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = PluginBase._funcItems.Items.Count;
            return PluginBase._funcItems.NativePointer;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getName()
        {
            if (_ptrPluginName == IntPtr.Zero)
                _ptrPluginName = Marshal.StringToHGlobalUni(WakaTimePackage.PluginName);
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            ScNotification notification = (ScNotification)Marshal.PtrToStructure(notifyCode, typeof(ScNotification));
            if (notification.Header.Code == (uint)NppMsg.NPPN_TBMODIFICATION)
            {
                PluginBase._funcItems.RefreshItems();
                WakaTimePackage.SetToolBarIcon();
            }
            else if (notification.Header.Code == (uint)NppMsg.NPPN_SHUTDOWN)
            {
                WakaTimePackage.PluginCleanUp();
                Marshal.FreeHGlobal(_ptrPluginName);
            }
            {
                WakaTimePackage.OnNotification(notification);
            }
        }
    }
}
