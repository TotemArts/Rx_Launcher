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
    class WebPatchSource : IPatchSource, IDisposable
    {
        Dictionary<string, Task> LoadTasks = new Dictionary<string, Task>();
        RXPatcher Patcher;
        string DownloadPath;

        public WebPatchSource(RXPatcher patcher, string downloadPath)
        {
            Patcher = patcher;
            DownloadPath = downloadPath;
        }

        public void Dispose()
        {
            Debug.Assert(Task.WhenAll(LoadTasks.Values).IsCompleted);
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(DownloadPath, subPath);
        }

        public Task Load(string subPath, string hash, CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            Task task;
            if (!LoadTasks.TryGetValue(subPath, out task))
            {
                task = LoadNew(subPath, hash, cancellationToken, progressCallback);
                LoadTasks[subPath] = task;
            }
            return task;
        }

        public async Task LoadNew(string subPath, string hash, CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            string filePath = GetSystemPath(subPath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var webClient = new WebClient())
            {
                webClient.Proxy = null;

                webClient.DownloadProgressChanged += (o, args) =>
                {
                    progressCallback(args.BytesReceived, args.TotalBytesToReceive);
                };

                using (cancellationToken.Register(() => webClient.CancelAsync()))
                {
                    RetryStrategy retryStrategy = new RetryStrategy();

                    try
                    {
                        await retryStrategy.Run(async () =>
                        {
                            try
                            {
                                await webClient.DownloadFileTaskAsync(new Uri(Patcher.BaseURL + "/" + subPath), filePath);

                                if (hash != null && await SHA1.GetFileHashAsync(filePath) != hash)
                                    return new Exception("Download hash mismatch");

                                return null;
                            }
                            catch (WebException e)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                return e;
                            }
                        });
                    }
                    catch (TooManyRetriesException)
                    {
                        // Try the next best host
                        try
                        {
                            Patcher.PopHost();
                        }
                        catch (InvalidOperationException)
                        {
                            // No other hosts remain
                            throw new Exception("No valid mirrors are available; please check your network connection or try again later.");
                        }
                    }
                }
            }
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
             * */
        }
    }
}
