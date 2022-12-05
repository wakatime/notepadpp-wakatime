using System;
using System.IO;
using System.Windows.Forms;

namespace WakaTime
{
    public class Logger
    {
        private readonly bool _isDebugEnabled;
        private readonly StreamWriter _writer;

        public Logger(string configFilepath)
        {
            var configFile = new ConfigFile(configFilepath);

            _isDebugEnabled = configFile.GetSettingAsBoolean("debug");

            var filename = $"{AppDataDirectory}\\notepadpp-wakatime.log";

            _writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
        }

        private static string AppDataDirectory
        {
            get
            {
                var roamingFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(roamingFolder, "WakaTime");

                // Create folder if it does not exist
                if (!Directory.Exists(appFolder))
                    Directory.CreateDirectory(appFolder);

                return appFolder;
            }
        }

        public void Debug(string msg)
        {
            if (!_isDebugEnabled)
                return;

            Log(LogLevel.Debug, msg);
        }

        public void Info(string msg)
        {
            Log(LogLevel.Info, msg);
        }

        public void Warning(string msg)
        {
            Log(LogLevel.Warning, msg);
        }

        public void Error(string msg, Exception ex = null)
        {
            var exceptionMessage = $"{msg}: {ex}";

            Log(LogLevel.HandledException, exceptionMessage);
        }

        private void Log(LogLevel level, string msg)
        {
            try
            {
                _writer.WriteLine("[Wakatime {0} {1:hh:mm:ss tt}] {2}", Enum.GetName(level.GetType(), level), DateTime.Now, msg);
                _writer.Flush();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error writing to notepadpp-wakatime.log", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        public void Close()
        {
            _writer?.Close();
        }
    }
}
