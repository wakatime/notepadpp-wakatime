using System;
using System.Collections.Generic;
using System.Linq;
namespace WakaTime
{
    public struct Constants
    {
        public const string PluginName = "WakaTime";
        public const string PluginKey = "notepadpp-wakatime"; // TODO use
        public static readonly Version Version = typeof(WakaTimeNppPlugin).Assembly.GetName().Version;
        public static readonly Version PluginVersion = new Version(Version.Major, Version.Minor, Version.Build);
    }
}
