using System;
using System.IO;
using System.Windows.Forms;

namespace WakaTime
{
    class LoggerNpp : ILogService
    {
        internal void Debug(string msg)
        {
            if (!WakaTimeConfigFile.Debug)
                return;

            Log(LogLevel.Debug, msg);
        }

        internal void Info(string msg)
        {
            Log(LogLevel.Info, msg);
        }

        internal void Warning(string msg)
        {
            Log(LogLevel.Warning, msg);
        }

        internal void Error(string msg, Exception ex = null)
        {
            var exceptionMessage = string.Format("{0}: {1}", msg, ex);

            Log(LogLevel.HandledException, exceptionMessage);
        }

        internal void Log(LogLevel level, string msg)
        {
            Log(string.Format("[Wakatime {0} {1}] {2}", Enum.GetName(level.GetType(), level), DateTime.Now.ToString("hh:mm:ss tt"), msg));
        }

        public void Log(string msg)
        {
            try
            {
                var writer = Setup();
                if (writer == null) return;

                writer.WriteLine(msg);
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
            var configDir = AppDataDirectory;
            if (string.IsNullOrWhiteSpace(configDir)) return null;

            var filename = string.Format("{0}\\{1}.log", configDir, Constants.PluginName);
            var writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
            return writer;
        }

        private static string AppDataDirectory
        {
            get
            {
                string roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appFolder = Path.Combine(roamingFolder, "WakaTime");

                // Create folder if it does not exist
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                return appFolder;
            }
        }
    }
}
