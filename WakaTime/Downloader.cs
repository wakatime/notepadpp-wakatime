using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace WakaTime
{
    public class Downloader
    {
        static public void DownloadAndInstallCli()
        {
            Logger.Debug("Downloading wakatime cli...");

            var url = WakaTimeConstants.CliUrl;
            var destinationDir = WakaTimeConstants.PluginConfigDir;

            // clean up old wakatime-master directory
            try
            {
                Directory.Delete(Path.Combine(destinationDir, "wakatime-master"), true);
            }
            catch { /* ignored */ }

            // Check for proxy setting
            var proxy = WakaTime.GetProxy();

            var localZipFile = Path.Combine(destinationDir, "wakatime-cli.zip");

            var client = new WebClient { Proxy = proxy };

            // Download wakatime cli
            client.DownloadFile(url, localZipFile);

            Logger.Debug("Finished downloading wakatime cli.");

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localZipFile, destinationDir);

            try
            {
                File.Delete(localZipFile);
            }
            catch { /* ignored */ }
        }

        static public void DownloadAndInstallPython()
        {
            Logger.Debug("Downloading python...");

            var url = PythonManager.PythonDownloadUrl;
            var destinationDir = WakaTimeConstants.PluginConfigDir;

            // Check for proxy setting
            var proxy = WakaTime.GetProxy();

            var localFile = Path.Combine(destinationDir, "python.zip");

            var client = new WebClient { Proxy = proxy };

            // Download embeddable python
            client.DownloadFile(url, localFile);

            Logger.Debug("Finished downloading python.");

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localFile, Path.Combine(destinationDir, "python"));

            Logger.Debug(string.Format("Finished extracting python: {0}", Path.Combine(destinationDir, "python")));

            try
            {
                File.Delete(localFile);
            }
            catch { /* ignored */ }
        }
    }
}