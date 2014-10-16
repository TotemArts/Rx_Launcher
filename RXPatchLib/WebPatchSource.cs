using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class WebPatchSource : IPatchSource, IDisposable
    {
        Dictionary<string, WebClient> WebClientsBySubPath = new Dictionary<string, WebClient>();
        string BaseUrl;
        string DownloadPath;

        public WebPatchSource(string baseUrl, string downloadPath)
        {
            BaseUrl = baseUrl;
            DownloadPath = downloadPath;
        }

        public void Dispose()
        {
            foreach (var webClient in WebClientsBySubPath)
                webClient.Value.Dispose();
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(DownloadPath, subPath);
        }

        public async Task Load(string subPath, string hash)
        {
            string filePath = GetSystemPath(subPath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var webClient = new WebClient();
            WebClientsBySubPath[subPath] = webClient;
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            await webClient.DownloadFileTaskAsync(new Uri(BaseUrl + "/" + subPath), filePath);
            /* TODO
            if (UseProxy)
            {
                request.Proxy = new WebProxy(ProxyServer + ":" + ProxyPort.ToString());
                if (ProxyUsername.Length > 0)
                    request.Proxy.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
            }

            WebResponse response = request.GetResponse();
            //result.MimeType = res.ContentType;
            //result.LastModified = response.LastModified;
            if (!resuming)//(Size == 0)
            {
                //resuming = false;
                Size = (int)response.ContentLength;
                SizeInKB = (int)Size / 1024;
            }
            acceptRanges = String.Compare(response.Headers["Accept-Ranges"], "bytes", true) == 0;

            //create network stream
            ns = response.GetResponseStream();

            return WebClient.DownloadFileTaskAsync(subPath, GetSystemPath(subPath));
             * */
        }
    }
}
