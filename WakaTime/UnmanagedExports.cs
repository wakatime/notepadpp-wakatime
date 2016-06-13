using System;
using System.Runtime.InteropServices;
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
            PluginBase.NppData = notepadPlusData;
            WakaTimePackage.CommandMenuInit();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = PluginBase.FuncItems.Items.Count;
            return PluginBase.FuncItems.NativePointer;
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
                _ptrPluginName = Marshal.StringToHGlobalUni(Constants.NativeName);
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));
            if (nc.nmhdr.code == (uint)NppMsg.NPPN_TBMODIFICATION)
            {
                PluginBase.FuncItems.RefreshItems();
                WakaTimePackage.SetToolBarIcon();
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_SHUTDOWN)
            {
                WakaTimePackage.PluginCleanUp();
                Marshal.FreeHGlobal(_ptrPluginName);
            }
            else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILESAVED)
            {
                WakaTimePackage.HandleActivity(WakaTimePackage.GetCurrentFile(), true);
            }
            else if (nc.nmhdr.code == (uint)SciMsg.SCI_ADDTEXT)
            {
                WakaTimePackage.HandleActivity(WakaTimePackage.GetCurrentFile(), false);
            }
            else if (nc.nmhdr.code == (uint)SciMsg.SCI_INSERTTEXT)
            {
                WakaTimePackage.HandleActivity(WakaTimePackage.GetCurrentFile(), false);
            }
        }
    }
}
