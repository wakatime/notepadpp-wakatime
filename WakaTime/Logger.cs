using System;
using System.IO;
using System.Windows.Forms;

namespace WakaTime
{
    internal enum LogLevel
    {
        Debug = 1,
        Info,
        Warning,
        HandledException
    };

    internal static class Logger
    {        
        internal static void Debug(string msg)
        {
            if (!WakaTimePackage.Debug)
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
            var exceptionMessage = $"{msg}: {ex}";

            Log(LogLevel.HandledException, exceptionMessage);
        }

        internal static void Log(LogLevel level, string msg)
        {
            try
            {
                var writer = Setup();
                if (writer == null) return;

                writer.WriteLine("[Wakatime {0} {1:hh:mm:ss tt}] {2}", Enum.GetName(level.GetType(), level), DateTime.Now, msg);            
                writer.Flush();
                writer.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error writing to WakaTime.log", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private static StreamWriter Setup()
        {
            var configDir = Dependencies.AppDataDirectory;
            if (string.IsNullOrWhiteSpace(configDir)) return null;

            var filename = $"{configDir}\\{Constants.PluginName}.log";
            var writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
            return writer;
        }        
    }
}
