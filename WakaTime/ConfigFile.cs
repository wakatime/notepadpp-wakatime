using System.Runtime.InteropServices;
using System.Text;

namespace WakaTime
{
    public class ConfigFile
    {
        [DllImport("kernel32", EntryPoint = "WritePrivateProfileStringW", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool WritePrivateProfileString(
            string section,
            string key,
            string val,
            string filePath);

        [DllImport("kernel32", EntryPoint = "GetPrivateProfileStringW", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern uint GetPrivateProfileString(
            string section,
            string key,
            string def,
            StringBuilder retVal, 
            uint size,
            string filePath);

        public readonly string ConfigFilepath;

        public ConfigFile(string configFilepath)
        {
            ConfigFilepath = configFilepath;
        }

        public string GetSetting(string key, string section = "settings")
        {
            var ret = new StringBuilder(255);

            _ = GetPrivateProfileString(section, key, "", ret, (uint)ret.Capacity, ConfigFilepath);
            
            return ret.ToString();
        }

        public bool GetSettingAsBoolean(string key, bool @default = false, string section = "settings")
        {
            var ret = GetSetting(key, section);

            if (bool.TryParse(ret.ToString(), out var parsed))
                return parsed;

            return @default;
        }

        public void SaveSetting(string section, string key, string value)
        {
            _ = WritePrivateProfileString(section, key, value, ConfigFilepath);
        }
    }
}
