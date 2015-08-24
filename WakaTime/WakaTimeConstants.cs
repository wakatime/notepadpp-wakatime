using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WakaTime
{
    internal static class WakaTimeConstants
    {        
        internal const string CliUrl = "https://github.com/wakatime/wakatime/archive/master.zip";
        internal const string NativeName = "WakaTime";
        internal const string PluginName = "notepadpp-wakatime";
        internal const string EditorName = "notepadpp";
        internal const string CliFolder = @"wakatime-master\wakatime\cli.py";
        internal static string UserConfigDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        internal static Func<string> PluginDir = () =>
        {
            var pluginConfigDir = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(PluginBase.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, pluginConfigDir);
            return pluginConfigDir.ToString();
        };

        internal static Func<string> CurrentWakaTimeCliVersion = () =>
        {
            var regex = new Regex(@"(__version_info__ = )(\(( ?\'[0-9]\'\,?){3}\))");
            var client = new WebClient();
            var about = client.DownloadString("https://raw.githubusercontent.com/wakatime/wakatime/master/wakatime/__about__.py");
            var match = regex.Match(about);

            if (!match.Success)
            {
                Logger.Warning("Couldn't auto resolve wakatime cli version");
                return string.Empty;
            }

            var grp1 = match.Groups[2];
            var regexVersion = new Regex("([0-9])");
            var match2 = regexVersion.Matches(grp1.Value);

            return string.Format("{0}.{1}.{2}", match2[0].Value, match2[1].Value, match2[2].Value);
        };
    }
}