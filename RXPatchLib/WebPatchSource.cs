using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    /// <summary>
    /// Overwrites for the WebClient to specify our own timeout when the downloader attempts to grab files
    /// </summary>
    [System.ComponentModel.DesignerCategory("Code")]
    class MyWebClient : WebClient
    {
        Dictionary<string, Task> LoadTasks = new Dictionary<string, Task>();
        string BaseUrl;
        string DownloadPath;

        public WebPatchSource(string baseUrl, string downloadPath)
        {
            BaseUrl = baseUrl;
            DownloadPath = downloadPath;
        }

        public void Dispose()
        {
            //Debug.Assert(Task.WhenAll(_loadTasks.Values).IsCompleted);
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(_downloadPath, subPath);
        }

        public Task Load(string subPath, string hash, CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            Task task;
            if (!_loadTasks.TryGetValue(subPath, out task))
            {
                task = LoadNew(subPath, hash, cancellationToken, progressCallback);
                _loadTasks[subPath] = task;
            }
            return task;
        }

        public async Task LoadNew(string subPath, string hash, CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            string filePath = GetSystemPath(subPath);
            var guid = Guid.NewGuid();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var webClient = new WebClient())
            {
                webClient.Proxy = null; // Were not using a proxy server, let's ensure that.

                webClient.DownloadProgressChanged += (o, args) =>
                {
                    progressCallback(args.BytesReceived, args.TotalBytesToReceive);
                };

                using (cancellationToken.Register(() => webClient.CancelAsync()))
                {
                    RetryStrategy retryStrategy = new RetryStrategy();
                    await retryStrategy.Run(async () =>
                    {
                        try
                        {
                            await webClient.DownloadFileTaskAsync(new Uri(BaseUrl + "/" + subPath), filePath);
                            return null;
                        }
                        catch (WebException e)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            return e;
                        }
                    });
                }
            }
            /* TODO
            if (UseProxy)
            {
                request.Proxy = new WebProxy(ProxyServer + ":" + ProxyPort.ToString());
                if (ProxyUsername.Length > 0)
                    request.Proxy.Credentials = new NetworkCredential(ProxyUsername, ProxyPassword);
            }

                        // Proceed execution with next mirror
                        goto new_host_selected;
                    }
                }
            }
        }
    }
}
