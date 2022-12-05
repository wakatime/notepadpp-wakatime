using System;
using System.Net;
using System.Text.RegularExpressions;

namespace WakaTime
{
    public class Proxy
    {
        private readonly ConfigFile _config;
        private readonly Logger _logger;

        public Proxy(Logger logger, string configFilepath)
        {
            _logger = logger;
            _config = new ConfigFile(configFilepath);
        }

        public WebProxy Get()
        {
            WebProxy proxy = null;

            try
            {
                var proxyStr = _config.GetSetting("proxy");
                if (string.IsNullOrEmpty(proxyStr))
                {
                    _logger.Debug("No proxy will be used. It's not set.");

                    return null;
                }

                var proxyRegex = new Regex(@"\s*((?<protocol>https?|socks5):\/\/)?(([^\s:]+):([^\s:]+)@)?([^\s:]+):(\d+)\s*");
                var match = proxyRegex.Match(proxyStr);

                if (match.Success)
                {
                    var protocol = match.Groups["protocol"].Success ? match.Groups["protocol"].Value : null;
                    var address = match.Groups[5].Value;
                    var port = match.Groups[6].Value;

                    proxy = new WebProxy(
                        protocol != null ? $"{protocol}://{address}:{port}" : $"{address}:{port}",
                        true, null);

                    if (match.Groups["3"].Success)
                    {
                        var username = match.Groups[3].Value;
                        var password = match.Groups[4].Value;

                        proxy.Credentials = new NetworkCredential(username, password);
                    }

                    _logger.Debug("A proxy with authentication will be used.");

                    return proxy;
                }

                _logger.Debug("No proxy will be used. It's either not set or badly formatted.");
            }
            catch (Exception ex)
            {
                _logger.Error(
                    "Exception while parsing the proxy string from WakaTime config file. No proxy will be used.",
                    ex);
            }

            return proxy;
        }
    }
}
