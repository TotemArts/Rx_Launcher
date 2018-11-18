using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
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
        protected override WebRequest GetWebRequest(Uri address)
        {
            var w = base.GetWebRequest(address);
            w.Timeout = 10 * 1000;
            return w;
        }
    }

    class WebPatchSource : IPatchSource, IDisposable
    {
        readonly Dictionary<string, Task> _loadTasks = new Dictionary<string, Task>();
        readonly RxPatcher _patcher;
        readonly string _downloadPath;
        readonly object _isDownloadingLock = new object();
        private byte _downloadsRunning;
        private const int MaxDownloadThreads = 12;

        public WebPatchSource(RxPatcher patcher, string downloadPath)
        {
            _patcher = patcher;
            _downloadPath = downloadPath;
        }

        public void Dispose()
        {
            //Debug.Assert(Task.WhenAll(_loadTasks.Values).IsCompleted);
            //_loadTasks.Clear();
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(_downloadPath, subPath);
        }

        public Task Load(string subPath, string hash, CancellationTokenSource cancellationTokenSource, Action<long, long, byte> progressCallback)
        {
            if (!_loadTasks.TryGetValue(subPath, out Task task))
            {
                task = LoadNew(subPath, hash, cancellationTokenSource, progressCallback);
                _loadTasks[subPath] = task;
            }
            return task;
        }

        /// <summary>
        /// Locks a download thread so that it may not attempt to download again
        /// This controls concurrent downloading
        /// </summary>
        /// <returns>The return value can be ignored</returns>
        private async Task LockDownload(CancellationTokenSource cancellationTokenSource)
        {
            while (true)
            {
                if (cancellationTokenSource.IsCancellationRequested) {
                    break;
                }

                lock (_isDownloadingLock)
                {
                    // This is where the threading of concurrent downloads happens, currently hardcoded to 12 but easily changed.
                    if (_downloadsRunning < MaxDownloadThreads)
                    {
                        _downloadsRunning++;
                        Debug.Print($"{Thread.CurrentThread.ManagedThreadId} | DLRUN: {_downloadsRunning} | ACCEPTING DOWNLOAD");
                        break;
                    }
                }

                await Task.Delay(1000);
            }
        }

        /// <summary>
        /// Unlocks a concurrent download thread to allow another one to start
        /// </summary>
        private void UnlockDownload()
        {
            lock (_isDownloadingLock)
            {
                _downloadsRunning--;
                Debug.Print($"{Thread.CurrentThread.ManagedThreadId} | DLRUN: {_downloadsRunning} | DOWNLOAD COMPLETE");
            }
        }

        public async Task LoadNew(string subPath, string hash, CancellationTokenSource cancellationTokenSource, Action<long, long, byte> progressCallback)
        {
            if (cancellationTokenSource.IsCancellationRequested) {
                return;
            }

            string filePath = GetSystemPath(subPath);
            var guid = Guid.NewGuid();

            // Check if the file exists
            if (File.Exists(filePath))
            {
                // If the file exists and is correct, return without redownloading.
                if (hash != null && await Sha256.GetFileHashAsync(filePath) == hash)
                {
                    // Update progress (probably unncessary)
                    long fileSize = new FileInfo(filePath).Length;
                    lock (_isDownloadingLock)
                    {
                        progressCallback(fileSize, fileSize, _downloadsRunning);
                    }

                    return;
                }

                // The hash didn't match; delete it.
                File.Delete(filePath);
            }

            // Ensure the necessary directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            // Since I can't await within a lock...
            await LockDownload(cancellationTokenSource);

            using (MyWebClient webClient = new MyWebClient())
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    RxLogger.Logger.Instance.Write($"Web client for {subPath} starting to shutdown due to Cancellation Requested");
                    return;
                }

                webClient.DownloadProgressChanged += (o, args) =>
                {
                    // Notify the RenX Updater window of a downloads progress
                    lock (_isDownloadingLock)
                    {
                        progressCallback(args.BytesReceived, args.TotalBytesToReceive, _downloadsRunning);
                    }

                    // Notify our debug window of a downloads progress
                    AXDebug.AxDebuggerHandler.Instance.UpdateDownload(guid, args.BytesReceived, args.TotalBytesToReceive);
                };
                

                bool nextMirror = true;
                while (nextMirror)
                {
                    nextMirror = false; //in any case: stop loop
                    using (cancellationTokenSource.Token.Register(() => webClient.CancelAsync()))
                    {
                        RetryStrategy retryStrategy = new RetryStrategy();
                        UpdateServerEntry thisPatchServer = null;
                        try
                        {
                            await retryStrategy.Run(async () =>
                            {
                                try
                                {
                                    thisPatchServer = _patcher.UpdateServerSelector.GetNextAvailableServerEntry();
                                    if (thisPatchServer == null)
                                        throw new NoReliableHostException();

                                    // Mark this patch server as currently used (is active)
                                    thisPatchServer.IsUsed = true;

                                    // Download file and wait until finished
                                    RxLogger.Logger.Instance.Write($"Starting file transfer: {thisPatchServer.Uri.AbsoluteUri}/{_patcher.WebPatchPath}/{subPath}");

                                    await webClient.DownloadFileTaskAsync(new Uri($"{thisPatchServer.Uri.AbsoluteUri}/{_patcher.WebPatchPath}/{subPath}"), filePath);
                                    RxLogger.Logger.Instance.Write(" > File Transfer Complete");
                                    thisPatchServer.IsUsed = false;

                                    // File finished downoading successfully; allow next download to start and check hash
                                    UnlockDownload();

                                    // Check our hash, if it's not the same we re-queue
                                    // todo: add a retry count to the file instruction, this is needed because if the servers file is actually broken you'll be in an infiniate download loop
                                    if (hash != null && await Sha256.GetFileHashAsync(filePath) != hash)
                                        throw new HashMistmatchException(); // Hash mismatch; throw exception

                                    return null;
                                }
                                catch (WebException e)
                                {
                                    RxLogger.Logger.Instance.Write(
                                        $"Error while attempting to transfer the file.\r\n{e.Message}");
                                    
                                    if (e.Status == WebExceptionStatus.RequestCanceled)
                                    {
                                        RxLogger.Logger.Instance.Write("User cancelled operation.", RxLogger.Logger.ErrorLevel.ErrInfo);

                                        // Mark current mirror failed
                                        if (thisPatchServer != null)
                                        {
                                            thisPatchServer.HasErrored = true;
                                            thisPatchServer.IsUsed = false;
                                        }

                                        nextMirror = false;

                                        // Cancel
                                        cancellationTokenSource.Cancel();

                                        // Unlock download
                                        UnlockDownload();

                                        return null;
                                    }

                                    HttpWebResponse errorResponse = e.Response as HttpWebResponse;
                                    if (errorResponse?.StatusCode >= (HttpStatusCode)400 && errorResponse?.StatusCode < (HttpStatusCode)500)
                                    {
                                        // 400 class errors will never resolve; do not retry
                                        throw new TooManyRetriesException(new List<Exception> { e });
                                    }

                                    return e;
                                }
                            });

                            // Download successfully completed
                        }
                        catch (TooManyRetriesException tooManyRetriesException)
                        {
                            RxLogger.Logger.Instance.Write("Too many retries; caught exceptions: ");
                            foreach (Exception ex in tooManyRetriesException.Exceptions)
                            {
                                RxLogger.Logger.Instance.Write(ex.Message + "\r\n" + ex.StackTrace);
                            }

                            // Mark current mirror failed
                            if (thisPatchServer != null)
                                thisPatchServer.HasErrored = true;

                            // Try the next best host; throw an exception if there is none
                            if (_patcher.PopHost() == null)
                            {
                                // Unlock download to leave in clean state.
                                UnlockDownload();
                                throw new NoReliableHostException();
                            }

                            // Proceed execution with next mirror (retry loop)
                            nextMirror = true;
                            continue;
                        }
                        catch (HashMistmatchException)
                        {
                            RxLogger.Logger.Instance.Write($"Invalid file hash for {subPath} - Expected hash {hash}, requeuing download");

                            // Mark current mirror failed
                            if (thisPatchServer != null)
                                thisPatchServer.HasErrored = true;

                            // Try the next best host; throw an exception if there is none
                            if (_patcher.PopHost() == null)
                            {
                                UnlockDownload();
                                throw new NoReliableHostException();
                            }

                            // Reset progress and requeue download
                            //await LoadNew(subPath, hash, cancellationTokenSource, progressCallback);
                            nextMirror = true;
                            continue;
                        }
                    }
                }
            }
        }
    }
}
