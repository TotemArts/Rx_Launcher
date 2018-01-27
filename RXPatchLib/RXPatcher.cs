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
    public class RxPatcher
    {
        // Do not use the system temp dir because it may be on a different volume.
        const string BackupSubPath = "backup";
        const string DownloadSubPath = "download"; // Note that this directory will be automatically emptied after patching.
        const string TempSubPath = "apply"; // Note that this directory will be automatically emptied after patching.

        //public UpdateServerSelector Selector = null;

        public UpdateServerEntry UpdateServer = null;
        public string WebPatchPath = null;

        private static RxPatcher _instance;
        public static RxPatcher Instance => _instance ?? (_instance = new RxPatcher());

        public readonly UpdateServerHandler UpdateServerHandler = new UpdateServerHandler();
        public UpdateServerSelector UpdateServerSelector = new UpdateServerSelector();

        public RxPatcher()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 50;
        }

        public void AddNewUpdateServer(string url, string friendlyName)
        {
            UpdateServerHandler.AddUpdateServer(url, friendlyName);
        }

        public UpdateServerEntry GetNextUpdateServerEntry()
        {
            return UpdateServerHandler.SelectBestPatchServer();
        }

        public IEnumerable<UpdateServerEntry> GetCurrentlyUsedUpdateServerEntries()
        {

            return UpdateServerHandler.GetUpdateServers().Where(x => x.IsUsed);
        }

        public async Task ApplyPatchFromWebDownloadTask(UpdateServerEntry baseUrl, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructionsHash)
        {
            UpdateServer = baseUrl;

            var backupPath = CreateBackupPath(applicationDirPath);
            var downloadPath = CreateDownloadPath(applicationDirPath);
            var tempPath = CreateTempPath(applicationDirPath);

            using (var patchSource = new WebPatchSource(this, downloadPath))
            {
                var patcher = new DirectoryPatcher(new XdeltaPatcher(XdeltaPatchSystemFactory.Preferred), targetPath, backupPath, tempPath, patchSource);
                await patcher.ApplyPatchAsync(progress, cancellationToken, instructionsHash);
                DirectoryEx.DeleteContents(downloadPath);
                DirectoryEx.DeleteContents(tempPath);

                // delete backup?
            }
        }

        public async Task ApplyPatchFromWeb(string patchPath, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructionsHash)
        {
            Contract.Assert(UpdateServerHandler.GetUpdateServers().Count > 0);
            WebPatchPath = patchPath;

            await UpdateServerSelector.SelectHosts(UpdateServerHandler.GetUpdateServers());

            var bestHost = UpdateServerSelector.Hosts.Dequeue();

            Console.WriteLine("#######HOST: {0} ({1})", bestHost.Uri, bestHost.Name);
            await ApplyPatchFromWebDownloadTask(bestHost, targetPath, applicationDirPath, progress, cancellationToken, instructionsHash);
        }

        public async Task ApplyPatchFromFilesystem(string patchPath, string targetPath, string applicationDirPath, IProgress<DirectoryPatcherProgressReport> progress, CancellationToken cancellationToken, string instructionsHash)
        {
            var backupPath = CreateBackupPath(applicationDirPath);
            var tempPath = CreateTempPath(applicationDirPath);

            var patchSource = new FileSystemPatchSource(patchPath);
            var patcher = new DirectoryPatcher(new XdeltaPatcher(XdeltaPatchSystemFactory.Preferred), targetPath, backupPath, tempPath, patchSource);
            await patcher.ApplyPatchAsync(progress, cancellationToken, instructionsHash);
        }

        public UpdateServerEntry PopHost()
        {
            // Lock Hosts queue and dequeue next host to BaseURL.
            if (UpdateServerSelector != null && UpdateServerSelector.Hosts.Count != 0)
                lock (UpdateServerSelector.Hosts)
                    UpdateServer = UpdateServerSelector.Hosts.Dequeue();

            // Lock Hosts queue and dequeue next host to BaseURL.
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
