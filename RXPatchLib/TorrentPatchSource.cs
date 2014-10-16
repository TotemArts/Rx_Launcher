using MonoTorrent.Client;
using MonoTorrent.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class TorrentPatchSource : IPatchSource
    {
        ClientEngine Engine;
        string DownloadDirPath;
        Torrent Torrent;
        TorrentManager TorrentManager;
        Dictionary<string, TaskCompletionSource<object>> CompletionSourceByPath = new Dictionary<string, TaskCompletionSource<object>>();
        CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        Task RunTask;

        public TorrentPatchSource(ClientEngine engine, string downloadDirPath)
        {
            Engine = engine;
            DownloadDirPath = downloadDirPath;
        }

        private async Task Run(byte[] torrentFileContents)
        {
            var torrentSettings = new TorrentSettings();

            Torrent = Torrent.Load(torrentFileContents);
            TorrentManager = new TorrentManager(Torrent, DownloadDirPath, torrentSettings, "");

            var tcs = new TaskCompletionSource<object>();
            TorrentManager.TorrentStateChanged += (o, args) =>
            {
                Console.WriteLine("State change: " + args.NewState);
                Trace.WriteLine("State change: " + args.NewState);
                switch (args.NewState)
                {
                    case TorrentState.Hashing: break;
                    case TorrentState.Metadata: break;
                    case TorrentState.Stopping: break;
                    case TorrentState.Paused:
                        TorrentManager.Start();
                        break;

                    case TorrentState.Stopped:
                        //Engine.Unregister(TorrentManager);
                        //Engine.Register(TorrentManager);
                        if (CancellationTokenSource.Token.IsCancellationRequested)
                        {
                            tcs.SetResult(null);
                        }
                        else
                        {
                            TorrentManager.Start();
                        }
                        break;
                    case TorrentState.Downloading: break;
                    case TorrentState.Seeding:
                        foreach (var file in Torrent.Files)
                        {
                            if (file.Priority != Priority.DoNotDownload && file.BitField.PercentComplete == 100)
                            {
                                TaskCompletionSource<object> fileTcs;
                                if (CompletionSourceByPath.TryGetValue(file.Path, out fileTcs))
                                {
                                    CompletionSourceByPath.Remove(file.Path);
                                    fileTcs.SetResult(null);
                                }
                            }
                        }
                        if (CompletionSourceByPath.Count() == 0)
                        {
                            TorrentManager.Stop();
                        }
                        break;
                    case TorrentState.Error:
                        tcs.SetException(TorrentManager.Error.Exception);
                        break;
                }
            };

            foreach (var file in Torrent.Files)
            {
                file.Priority = Priority.DoNotDownload;
            }
            Engine.Register(TorrentManager);
            TorrentManager.Start();
            await tcs.Task;
        }

        public void Start(byte[] torrentFileContents)
        {
            RunTask = Run(torrentFileContents);
        }

        public async Task Stop()
        {
            CancellationTokenSource.Cancel();
            TorrentManager.Stop();
            await RunTask;
            foreach (var fileTcs in CompletionSourceByPath)
            {
                fileTcs.Value.SetCanceled();
            }
        }

        public Task Load(string subPath)
        {
            var files = from f in Torrent.Files where f.Path == subPath select f;
            var file = files.Single();
            file.Priority = Priority.Normal;
            var tcs = new TaskCompletionSource<object>();
            CompletionSourceByPath.Add(subPath, tcs);
            if (TorrentManager.State == TorrentState.Seeding || TorrentManager.State == TorrentState.Downloading && CompletionSourceByPath.Count() == 1)
                TorrentManager.Stop(); // It will automatically restart.
            //for (; ;) Thread.Sleep(1000);
            return tcs.Task;
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(DownloadDirPath, subPath);
        }
    }
}
