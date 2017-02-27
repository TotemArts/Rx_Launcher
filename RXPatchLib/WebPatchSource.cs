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
        bool IsDownloading = false;
        object IsDownloadingLock = new object();

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

        private async Task LockDownload()
        {
            while (true)
            {
                lock (IsDownloadingLock)
                {
                    if (!IsDownloading)
                    {
                        IsDownloading = true;
                        break;
                    }
                }

                await Task.Delay(100);
            }
        }

        private void UnlockDownload()
        {
            lock (IsDownloadingLock)
                IsDownloading = false;
        }

        public async Task LoadNew(string subPath, string hash, CancellationToken cancellationToken, Action<long, long> progressCallback)
        {
            string filePath = GetSystemPath(subPath);

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // If the file exists and is correct, return without redownloading.
                if (hash != null && await SHA256.GetFileHashAsync(filePath) == hash)
                {
                    // Update progress (probably unncessary)
                    long FileSize = new FileInfo(filePath).Length;
                    progressCallback(FileSize, FileSize);

                    return;
                }

                // The hash didn't match; delete it.
                File.Delete(filePath);
            }

            // Ensure the necessary directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Since I can't await within a lock...
            await LockDownload();

            using (var webClient = new WebClient())
            {
                webClient.Proxy = null;

                webClient.DownloadProgressChanged += (o, args) =>
                {
                    progressCallback(args.BytesReceived, args.TotalBytesToReceive);
                };

                new_host_selected:

                using (cancellationToken.Register(() => webClient.CancelAsync()))
                {
                    RetryStrategy retryStrategy = new RetryStrategy();

                    try
                    {
                        await retryStrategy.Run(async () =>
                        {
                            try
                            {
                                // Download file and wait until finished
                                await webClient.DownloadFileTaskAsync(new Uri(Patcher.BaseURL + "/" + subPath), filePath);

                                // File finished downoading successfully; allow next download to start and check hash
                                UnlockDownload();

                                if (hash != null && await SHA256.GetFileHashAsync(filePath) != hash)
                                    throw new HashMistmatchException(); // Hash mismatch; throw exception

                                return null;
                            }
                            catch (WebException e)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                return e;
                            }
                        });

                        // Download successfully completed
                    }
                    catch (TooManyRetriesException)
                    {
                        // Try the next best host; throw an exception if there is none
                        if (Patcher.PopHost() == null)
                        {
                            // Unlock download to leave in clean state.
                            UnlockDownload();

                            throw new NoReliableHostException();
                        }

                        // Proceed execution with next mirror
                        goto new_host_selected;
                    }
                    catch (HashMistmatchException)
                    {
                        // Try the next best host; throw an exception if there is none
                        if (Patcher.PopHost() == null)
                            throw new NoReliableHostException();

                        // Reset progress and requeue download
                        await LoadNew(subPath, hash, cancellationToken, progressCallback);
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
