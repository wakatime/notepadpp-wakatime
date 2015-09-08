using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace WakaTime
{
    class Downloader
    {
        static public void DownloadCli(string url, string dir)
        {
            var client = new WebClient();
            var localZipFile = dir + "\\wakatime-cli.zip";

            // Download wakatime cli
            client.DownloadFile(url, localZipFile);

            // Extract wakatime cli zip file
            ZipFile.ExtractToDirectory(localZipFile, dir);

            try
            {
                File.Delete(localZipFile);
            }
            catch { /* ignored */ }
        }

        static public void DownloadPython(string url, string dir)
        {
            var localFile = dir + "\\python.msi";

            var client = new WebClient();
            client.DownloadFile(url, localFile);

            var arguments = "/i \"" + localFile + "\"";
            arguments = arguments + " /norestart /qb!";

            var procInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardError = true,
                FileName = "msiexec",
                CreateNoWindow = true,
                Arguments = arguments
            };

            Process.Start(procInfo);

            try
            {
                File.Delete(localFile);
            }
            catch { /* ignored */ }
        }
    }
}