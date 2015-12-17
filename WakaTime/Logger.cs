using System;
using System.IO;

namespace WakaTime
{
    internal enum LogLevel
    {
        Debug = 1,
        Info,
        Warning,
        HandledException
    };

    static class Logger
    {        
        internal static void Debug(string msg)
        {
            if (!WakaTime.Debug)
                return;

            Log(LogLevel.Debug, msg);
        }

        internal static void Info(string msg)
        {
            Log(LogLevel.Info, msg);
        }

        internal static void Warning(string msg)
        {
            Log(LogLevel.Warning, msg);
        }

        internal static void Error(string msg, Exception ex = null)
        {
            var exceptionMessage = string.Format("{0}: {1}", msg, ex);

            Log(LogLevel.HandledException, exceptionMessage);
        }

        internal static void Log(LogLevel level, string msg)
        {
            var writer = Setup();
            if (writer == null) return;

            writer.WriteLine("[Wakatime {0} {1}] {2}", Enum.GetName(level.GetType(), level), DateTime.Now.ToString("hh:mm:ss tt"), msg);            
            writer.Flush();
            writer.Close();
        }

        private static StreamWriter Setup()
        {
            var configDir = WakaTimeConstants.PluginConfigDir;
            if (string.IsNullOrWhiteSpace(configDir)) return null;

            var filename = string.Format("{0}\\{1}.log", configDir, WakaTimeConstants.NativeName);
            var writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
            return writer;
        }        
    }
}
