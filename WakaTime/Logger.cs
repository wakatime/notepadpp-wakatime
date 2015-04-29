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

        private StreamWriter writer;

        public Logger()
        {
            string userHomeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userHomeDir) == false)
            {
                string filename = userHomeDir + "\\" + "notepadpp-wakatime.log";
                this.writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write));
            }
        }

        public void Info(String msg)
        {
            this.Log("INFO", msg);
        }

        public void Warning(String msg)
        {
            this.Log("WARN", msg);
        }

        public void Debug(String msg)
        {
            this.Log("DEBUG", msg);
        }

        public void Error(String msg)
        {
            this.Log("ERROR", msg);
        }

        public void Log(String level, String msg)
        {
            writer.WriteLine(level + " -- " + DateTime.Now.ToLongTimeString() + " -- " + msg);
            writer.Flush();
        }

        public void Dispose()
        {
            writer.Close();
        }
    }
}
