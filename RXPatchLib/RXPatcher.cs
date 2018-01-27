﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class RXPatcher
    {
        // Do not use the system temp dir because it may be on a different volume.
        const string BackupSubPath = "backup";
        const string DownloadSubPath = "download"; // Note that this directory will be automatically emptied after patching.
        const string TempSubPath = "apply"; // Note that this directory will be automatically emptied after patching.

        //public UpdateServerSelector Selector = null;
        public UpdateServerEntry UpdateServer = null;
        public string WebPatchPath = null;

        private static RXPatcher _instance;
        public static RXPatcher Instance => _instance ?? (_instance = new RXPatcher());

<<<<<<< b2e30413944ea970b03cce03f62fbd53027b45a6
        public readonly UpdateServerHandler UpdateServerHandler = new UpdateServerHandler();

        public void AddNewUpdateServer(string url, string friendlyName)
        {
            UpdateServerHandler.AddUpdateServer(url, friendlyName);
        }

        public UpdateServerEntry GetNextUpdateServerEntry()
        {
            return UpdateServerHandler.SelectBestPatchServer();
=======
        private readonly UpdateServerHandler _updateServerHandler = new UpdateServerHandler();

        public void AddNewUpdateServer(string url, string friendlyName)
        {
            _updateServerHandler.AddUpdateServer(url, friendlyName);
>>>>>>> Instancing RxPatcher + Added UpdateServerHandler for URLs
        }

        public IEnumerable<UpdateServerEntry> GetCurrentlyUsedUpdateServerEntries()
        {
<<<<<<< b2e30413944ea970b03cce03f62fbd53027b45a6
            return UpdateServerHandler.GetUpdateServers().Where(x => x.IsUsed);
        }

        public async Task ApplyPatchFromWebDownloadTask(UpdateServerEntry baseURL, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructions_hash)
=======
            return _updateServerHandler.GetUpdateServers().Where(x => x.IsUsed);
        }

        public async Task ApplyPatchFromWeb(string baseUrl, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructions_hash)
>>>>>>> Instancing RxPatcher + Added UpdateServerHandler for URLs
        {
            UpdateServer = baseURL;

            var backupPath = CreateBackupPath(applicationDirPath);
            var downloadPath = CreateDownloadPath(applicationDirPath);
            var tempPath = CreateTempPath(applicationDirPath);

            using (var patchSource = new WebPatchSource(this, downloadPath))
            {
                var patcher = new DirectoryPatcher(new XdeltaPatcher(XdeltaPatchSystemFactory.Preferred), targetPath, backupPath, tempPath, patchSource);
                await patcher.ApplyPatchAsync(progress, cancellationToken, instructions_hash);
                DirectoryEx.DeleteContents(downloadPath);
                DirectoryEx.DeleteContents(tempPath);

                // delete backup?
            }
        }

        public async Task ApplyPatchFromWeb(string patchPath, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructions_hash)
        {
            Contract.Assert(UpdateServerHandler.GetUpdateServers().Count > 0);
            WebPatchPath = patchPath;

            var Selector = new UpdateServerSelector();
            await Selector.SelectHosts(UpdateServerHandler.GetUpdateServers());

            var bestHost = Selector.Hosts.Dequeue();

            Console.WriteLine("#######HOST: {0} ({1})", bestHost.Uri, bestHost.Name);
            await ApplyPatchFromWebDownloadTask(bestHost, targetPath, applicationDirPath, progress, cancellationToken, instructions_hash);
        }

        public async Task ApplyPatchFromFilesystem(string patchPath, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructions_hash)
        {
            var backupPath = CreateBackupPath(applicationDirPath);
            var tempPath = CreateTempPath(applicationDirPath);

            var patchSource = new FileSystemPatchSource(patchPath);
            var patcher = new DirectoryPatcher(new XdeltaPatcher(XdeltaPatchSystemFactory.Preferred), targetPath, backupPath, tempPath, patchSource);
            await patcher.ApplyPatchAsync(progress, cancellationToken, instructions_hash);
        }

        
        public UpdateServerEntry PopHost()
        {
            UpdateServer.HasErrored = true;
            UpdateServer = UpdateServerHandler.SelectBestPatchServer();
            return UpdateServer;
        }
        
        private static string CreateBackupPath(string applicationDirPath)
        {
            string dirName = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            return CreateDirPath(Path.Combine(applicationDirPath, BackupSubPath, dirName));
        }

        private static string CreateDownloadPath(string applicationDirPath)
        {
            return CreateDirPath(Path.Combine(applicationDirPath, DownloadSubPath));
        }

        private static string CreateTempPath(string applicationDirPath)
        {
            return CreateDirPath(Path.Combine(applicationDirPath, TempSubPath));
        }

        private static string CreateDirPath(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
