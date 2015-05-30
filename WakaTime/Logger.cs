using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WakaTime
{
    class Logger
    {

        public static int DEBUG = 3;
        public static int INFO = 2;
        public static int WARN = 1;
        public static int ERROR = 0;
        public static string[] LEVELS = new string[] { "ERROR", "WARN", "INFO", "DEBUG" };
        private static int currentLevel = 2;

        public Logger()
        {
        }

        public static void Debug(String msg)
        {
            Log(Logger.DEBUG, msg);
        }

        public static void Info(String msg)
        {
            Log(Logger.INFO, msg);
        }

        public static void Warning(String msg)
        {
            Log(Logger.WARN, msg);
        }

        public static void Error(String msg)
        {
            Log(Logger.ERROR, msg);
        }

        public static void Log(int level, String msg)
        {
            if (level <= currentLevel && level >= 0)
            {
                StreamWriter writer = Setup();
                if (writer != null)
                {
                    writer.WriteLine(DateTime.Now.ToLongTimeString() + " : " + WakaTime.NativeName + " : " + Logger.LEVELS[level] + " : " + msg);
                    writer.Flush();
                    writer.Close();
                }
            }
        }

        private static StreamWriter Setup()
        {
            StreamWriter writer = null;
            string configDir = WakaTime.GetConfigDir();
            if (string.IsNullOrWhiteSpace(configDir) == false)
            {
                string filename = configDir + "\\" + WakaTime.NativeName + ".log";
                writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
            }
            return writer;
        }

        public static void setLevel(int level)
        {
            if (level < 0 || level >= LEVELS.Length)
            {
                level = 2;
            }
            currentLevel = level;
        }

    }
}
