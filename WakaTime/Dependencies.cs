using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;

namespace WakaTime
{
    public class Dependencies
    {
        private readonly Logger _logger;
        private readonly ConfigFile _configFile;

        public Dependencies(Logger logger, ConfigFile configFile)
        {
            _logger = logger;
            _configFile = configFile;
        }

        public static string HomeLocation
        {
            get
            {
                var wakaHome = Environment.GetEnvironmentVariable("WAKATIME_HOME");

                if (!string.IsNullOrEmpty(wakaHome) && Directory.Exists(wakaHome))
                    return wakaHome;

                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
        }

        public static string ResourcesLocation
        {
            get
            {
                var path = Path.Combine(HomeLocation, ".wakatime");

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                return path;
            }
        }

        public void CheckAndInstallCli()
        {
            if (!IsCliInstalled() || !IsCliLatest())
                InstallCli();
        }

        private void InstallCli()
        {
            var version = GetLatestCliVersion();

            _logger.Debug($"Downloading wakatime-cli {version}...");

            var arch = ProcessorArchitectureHelper.Is64BitOperatingSystem ? "amd64" : "386";
            var url = $"{Constants.GithubDownloadPrefix}/{version}/wakatime-cli-windows-{arch}.zip";
            var localZipFile = Path.Combine(ResourcesLocation, $"wakatime-cli-{version}.zip");

            // Download wakatime-cli
            DownloadFile(url, localZipFile);
            _logger.Debug($"Finished downloading wakatime-cli {version}");

            // Extract wakatime-cli zip file
            _logger.Debug($"Extracting wakatime-cli to: {ResourcesLocation}");
            using (var archive = ZipFile.OpenRead(localZipFile))
            {
                foreach (var entry in archive.Entries)
                {
                    entry.ExtractToFile(Path.Combine(ResourcesLocation, entry.FullName), true);
                }
            }

            _logger.Debug("Finished extracting wakatime-cli");

            try
            {
                File.Delete(localZipFile);
            }
            catch
            { /* ignored */ }
        }

        private string GetLatestCliVersion()
        {
            try
            {
                var isAlpha = _configFile.GetSettingAsBoolean("alpha");
                var url = isAlpha
                    ? Constants.GithubReleasesAlphaUrl
                    : Constants.GithubReleasesStableUrl;

                var client = GetHttpClient();
                var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.TryAddWithoutValidation("User-Agent", "github.com/wakatime/notepadpp-wakatime");

                var cliVersionLastModified = _configFile.GetSetting("cli_version_last_modified", "internal");
                if (!string.IsNullOrEmpty(cliVersionLastModified))
                    req.Headers.TryAddWithoutValidation("If-Modified-Since", cliVersionLastModified);

                var res = client.SendAsync(req).GetAwaiter().GetResult();

                _logger.Debug($"GitHub API Response {res?.StatusCode}");

                if (res.StatusCode == HttpStatusCode.NotModified)
                {
                    return _configFile.GetSetting("cli_version", "internal");
                }

                var resBody = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                string version;

                if (isAlpha)
                    version = JSONSerializer.DeSerialize<IList<GithubReleaseApiResponse>>(resBody)[0].TagName;
                else
                    version = JSONSerializer.DeSerialize<GithubReleaseApiResponse>(resBody).TagName;

                _logger.Debug($"Latest wakatime-cli version from GitHub: {version}");

                res.Headers.TryGetValues("Last-Modified", out var lastModifiedList);
                var lastModified = lastModifiedList?.FirstOrDefault();
                if (!string.IsNullOrEmpty(lastModified))
                {
                    _configFile.SaveSetting("internal", "cli_version", version);
                    _configFile.SaveSetting("internal", "cli_version_last_modified", lastModified);
                }

                return version;

            }
            catch (Exception ex)
            {
                _logger.Error("Error getting latest wakatime cli version from GitHub", ex);
            }

            return null;
        }

        private bool IsCliLatest()
        {
            var process = new RunProcess(GetCliLocation(), "--version");

            process.Run();

            if (!process.Success)
            {
                _logger.Error(process.Error);

                return false;
            }

            var currentVersion = process.Output?.Trim();

            _logger.Debug($"Current wakatime-cli version is {currentVersion}");
            _logger.Debug("Checking for updates to wakatime-cli...");

            var latestVersion = GetLatestCliVersion();

            if (currentVersion.Equals(latestVersion))
            {
                _logger.Info("wakatime-cli is up to date");
                return true;
            }

            _logger.Info($"Found an updated wakatime-cli {latestVersion}");

            return false;
        }

        public static string GetConfigFilePath()
        {
            return Path.Combine(HomeLocation, ".wakatime.cfg");
        }

        internal static string GetCliLocation()
        {
            var ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "";

            var arch = ProcessorArchitectureHelper.Is64BitOperatingSystem ? "amd64" : "386";

            return Path.Combine(ResourcesLocation, $"wakatime-cli-windows-{arch}{ext}");
        }

        private static bool IsCliInstalled()
        {
            return File.Exists(GetCliLocation());
        }

        private HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler();

            if (!_configFile.GetSettingAsBoolean("no_ssl_verify") &&
                !ServicePointManager.SecurityProtocol.HasFlag(SecurityProtocolType.Tls12))
            {
                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            }

            var proxy = _configFile.GetSetting("proxy");
            if (!string.IsNullOrEmpty(proxy))
            {
                handler.UseProxy = true;
                handler.Proxy = new Proxy(_logger, _configFile.ConfigFilepath).Get();
            }

            return new HttpClient(handler);
        }

        private void DownloadFile(string url, string saveAs)
        {
            var client = GetHttpClient();

            var res = client.GetAsync(url).GetAwaiter().GetResult();
            var stream = res.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

            using (var fileStream = File.Create(saveAs))
                stream.CopyTo(fileStream);
        }
    }
}
