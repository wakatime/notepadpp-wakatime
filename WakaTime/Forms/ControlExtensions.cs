using System;
using System.Windows.Forms;

namespace WakaTime.Forms
{
    public static class ControlExtensions
    {
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.IsDisposed)
                return;

            try
            {
                if (control.InvokeRequired && !control.IsDisposed)
                {
                    control.Invoke(new MethodInvoker(delegate { action(); }));
                }
                else
                {
                    action();
                }
            }
            catch { }
        }
    }
}
