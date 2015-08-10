using System;
using System.IO;

namespace WakaTime
{
    class Logger
    {        
        internal static void Debug(string msg)
        {
            if (!WakaTime.Debug)
                return;

            Log("Debug", msg);
        }

        internal static void Info(string msg)
        {
            Log("Info", msg);
        }

        internal static void Warning(string msg)
        {
            Log("Warning", msg);
        }

        internal static void Error(string msg, Exception ex = null)
        {
            var exceptionMessage = string.Format("{0}: {1}", msg, ex);

            Log("Handled Exception", exceptionMessage);
        }

        internal static void Log(string level, string msg)
        {
            var writer = Setup();
            if (writer == null) return;

            writer.WriteLine("[Wakatime {0} {1}] {2}", level, DateTime.Now.ToString("hh:mm:ss tt"), msg);            
            writer.Flush();
            writer.Close();
        }

        private static StreamWriter Setup()
        {
            var configDir = WakaTime.GetConfigDir();
            if (string.IsNullOrWhiteSpace(configDir)) return null;

            var filename = string.Format("{0}\\{1}.log", configDir, WakaTimeConstants.NativeName);
            var writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
            return writer;
        }        
    }
}
