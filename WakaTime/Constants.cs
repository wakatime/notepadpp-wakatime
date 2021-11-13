using System;
using System.Net;
using System.Text.RegularExpressions;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace WakaTime
{
    internal static class Constants
    {
        internal const string PluginName = "WakaTime";
        internal const string PluginKey = "notepadpp-wakatime";
        internal static string PluginVersion =
            $"{WakaTimePackage.CoreAssembly.Version.Major}.{WakaTimePackage.CoreAssembly.Version.Minor}.{WakaTimePackage.CoreAssembly.Version.Build}";
        internal const string EditorName = "notepadpp";
        internal static string EditorVersion {
            get
            {
                var ver = (int)Win32.SendMessage(PluginBase.nppData._nppHandle, (uint)NppMsg.NPPM_GETNPPVERSION, 0, 0);
                return ver.ToString();
            }
        }

        internal const string CliUrl = "https://github.com/wakatime/legacy-python-cli/archive/refs/heads/master.zip";
        internal const string CliFolder = @"legacy-python-cli-master\wakatime\cli.py";

        internal static Func<string> LatestWakaTimeCliVersion = () =>
        {
            var regex = new Regex(@"(__version_info__ = )(\(( ?\'[0-9]+\'\,?){3}\))");

            var client = new WebClient { Proxy = WakaTimePackage.GetProxy() };

            try
            {
                var about = client.DownloadString("https://raw.githubusercontent.com/wakatime/wakatime/master/wakatime/__about__.py");
                var match = regex.Match(about);

                if (match.Success)
                {
                    var grp1 = match.Groups[2];
                    var regexVersion = new Regex("([0-9]+)");
                    var match2 = regexVersion.Matches(grp1.Value);
                    return $"{match2[0].Value}.{match2[1].Value}.{match2[2].Value}";
                }
                else
                {
                    Logger.Warning("Couldn't auto resolve wakatime cli version");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Exception when checking current wakatime cli version: ", ex);
            }
            return string.Empty;
        };
    }
}
