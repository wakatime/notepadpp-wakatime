// NPP plugin platform for .Net v0.94.00 by Kasper B. Graversen etc.
using System;
using System.Runtime.InteropServices;
using Kbg.NppPluginNET.PluginInfrastructure;
using WakaTime;

namespace Kbg.NppPluginNET
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void setInfo(NppData notepadPlusData)
        {
            try
            {
                PluginBase.nppData = notepadPlusData;
                //WakaTimePackage.CommandMenuInit();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UnmanagedExports.setInfo");
            }
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static IntPtr getFuncsArray(ref int nbF)
        {
            try
            {
                WakaTimePackage.CommandMenuInit();
                nbF = PluginBase._funcItems.Items.Count;
                return PluginBase._funcItems.NativePointer;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UnmanagedExports.getFuncsArray");
                nbF = 0;
                return IntPtr.Zero;
            }
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                return 1;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UnmanagedExports.messageProc");
                return 1;
            }
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static IntPtr getName()
        {
            try
            {
                if (_ptrPluginName == IntPtr.Zero)
                    _ptrPluginName = Marshal.StringToHGlobalUni("WakaTime");
                return _ptrPluginName;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UnmanagedExports.getName");
                return IntPtr.Zero;
            }
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        public static void beNotified(IntPtr notifyCode)
        {
            try
            {
                var notification = (ScNotification)Marshal.PtrToStructure(notifyCode, typeof(ScNotification));
                WakaTimePackage.OnNppNotification(notification, _ptrPluginName);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "UnmanagedExports.beNotified");
            }
        }
    }
}
