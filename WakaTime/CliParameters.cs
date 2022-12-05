using System.Collections.ObjectModel;
using System.Linq;

namespace WakaTime
{
    public class CliParameters
    {
        public string Key { get; set; }
        public string File { get; set; }
        public string Lines { get; set; }
        public string LineNumber { get; set; }
        public string Time { get; set; }
        public string Plugin { get; set; }
        public bool IsWrite { get; set; }
        public bool HasExtraHeartbeats { get; set; }

        public string[] ToArray(bool obfuscate = false)
        {
            var parameters = new Collection<string>
            {
                "--key",
                obfuscate ? $"XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXX{Key.Substring(Key.Length - 4)}" : Key,
                "--entity",
                File,
                "--lines-in-file",
                Lines,
                "--lineno",
                LineNumber,
                "--time",
                Time,
                "--plugin",
                Plugin
            };

            if (IsWrite)
                parameters.Add("--write");

            if (HasExtraHeartbeats)
                parameters.Add("--extra-heartbeats");

            return parameters.ToArray();
        }
    }
}
