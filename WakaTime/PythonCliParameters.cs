using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace WakaTime
{
    internal class PythonCliParameters
    {
        public string Cli
        {
            get
            {
                var pluginConfigDir = new StringBuilder(Win32.MAX_PATH);
                Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, pluginConfigDir);
                return Path.Combine(pluginConfigDir.ToString(), WakaTimeConstants.CliFolder);                               
            }
        }
        public string Key { get; set; }
        public string File { get; set; }
        public string Plugin { get; set; }
        public bool IsWrite { get; set; }
        public string Project { get; set; }        

        public string[] ToArray(bool obfuscate = false)
        {
            var parameters = new Collection<string>
            {
                Cli,
                "--key",
                obfuscate ? string.Format("********-****-****-****-********{0}", Key.Substring(Key.Length - 4)) : Key,
                "--file",
                File,
                "--plugin",
                Plugin
            };

            if (IsWrite)
                parameters.Add("--write");

            // ReSharper disable once InvertIf
            if (!string.IsNullOrEmpty(Project))
            {
                parameters.Add("--project");
                parameters.Add(Project);
            }

            return parameters.ToArray();
        }
    }
}