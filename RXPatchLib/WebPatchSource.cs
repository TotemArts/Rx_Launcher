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
        readonly Dictionary<string, Task> _loadTasks = new Dictionary<string, Task>();
        readonly RxPatcher _patcher;
        readonly string _downloadPath;
        bool _isDownloading = false;
        readonly object _isDownloadingLock = new object();
        private byte _downloadsRunning = 0;

        public WebPatchSource(RxPatcher patcher, string downloadPath)
        {
            _patcher = patcher;
            _downloadPath = downloadPath;
        }

        public void Dispose()
        {
            Debug.Assert(Task.WhenAll(_loadTasks.Values).IsCompleted);
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

        private async Task LockDownload()
        {
            while (true)
            {
                lock (_isDownloadingLock)
                {
                    //if (!_isDownloading)
                    if (_downloadsRunning < 128)
                    {
                        _isDownloading = true;
                        _downloadsRunning++;
                        Debug.Print($"{Thread.CurrentThread.ManagedThreadId} | DLRUN: {_downloadsRunning} | ACCEPTING DOWNLOAD");
                        break;
                    }
                }

                await Task.Delay(1000);
            }
        }

        private void UnlockDownload()
        {
            lock (_isDownloadingLock)
            {
                _downloadsRunning--;
                Debug.Print($"{Thread.CurrentThread.ManagedThreadId} | DLRUN: {_downloadsRunning} | DOWNLOAD COMPLETE");
                _isDownloading = false;
            }
        }

        public async Task LoadNew(string subPath, string hash, CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            string filePath = GetSystemPath(subPath);

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // If the file exists and is correct, return without redownloading.
                if (hash != null && await Sha256.GetFileHashAsync(filePath) == hash)
                {
                    // Update progress (probably unncessary)
                    long fileSize = new FileInfo(filePath).Length;
                    progressCallback(fileSize, fileSize, _downloadsRunning);

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
                    progressCallback(args.BytesReceived, args.TotalBytesToReceive, _downloadsRunning);
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
                                var rnd = new Random();
                                var xyz = _patcher.UpdateServerSelector.Hosts.ToArray();
                                var thisPatchServer = xyz[rnd.Next(0, (xyz.Length < 4 ? xyz.Length : 4))];
                                if (thisPatchServer == null)
                                    throw new Exception("Unable to find a suitable update server");

                                thisPatchServer.IsUsed = true;

                                // Download file and wait until finished
                                RxLogger.Logger.Instance.Write($"Starting file transfer: {_patcher.UpdateServer.Uri.AbsoluteUri}/{_patcher.WebPatchPath}/{subPath}");
                                await webClient.DownloadFileTaskAsync(new Uri($"{_patcher.UpdateServer.Uri.AbsoluteUri}/{_patcher.WebPatchPath}/{subPath}"), filePath);
                                RxLogger.Logger.Instance.Write("  > File Transfer Complete");

                                thisPatchServer.IsUsed = false;

                                // File finished downoading successfully; allow next download to start and check hash
                                UnlockDownload();

                                if (hash != null && await Sha256.GetFileHashAsync(filePath) != hash)
                                    throw new HashMistmatchException(); // Hash mismatch; throw exception

                                return null;
                            }
                            catch (WebException e)
                            {
                                RxLogger.Logger.Instance.Write(
                                    $"Error while attempting to transfer the file.\r\n{e.Message}\r\n{e.StackTrace}");
                                cancellationToken.ThrowIfCancellationRequested();
                                return e;
                            }
                        });

                        // Download successfully completed
                    }
                    catch (TooManyRetriesException)
                    {
                        // Try the next best host; throw an exception if there is none
                        if (_patcher.PopHost() == null)
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
                        RxLogger.Logger.Instance.Write($"Invalid file hash for {subPath} - Expected hash {hash}, requeuing download");

                        // Try the next best host; throw an exception if there is none
                        if (_patcher.PopHost() == null)
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
