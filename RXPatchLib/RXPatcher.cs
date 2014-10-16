using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class RXPatcher
    {
        // Do not use the system temp dir because it may be on a different volume.
        const string BackupSubPath = "patch/backup";
        const string DownloadSubPath = "temp/patch/download";
        const string TempSubPath = "temp/patch";

        public async Task ApplyPatchFromWeb(string baseUrl, string targetPath, string applicationDirPath)
        {
            var backupPath = CreateDirPath(applicationDirPath, BackupSubPath);
            var downloadPath = CreateDirPath(applicationDirPath, DownloadSubPath);
            var tempPath = CreateDirPath(applicationDirPath, TempSubPath);

            using (var patchSource = new WebPatchSource(baseUrl, downloadPath))
            {
                var patcher = new DirectoryPatcher(new XdeltaPatcher(new XdeltaPatchSystem()), targetPath, backupPath, tempPath, patchSource);
                await patcher.ApplyPatchAsync();
            }
        }
        public async Task ApplyPatchFromFilesystem(string patchPath, string targetPath, string applicationDirPath)
        {
            var backupPath = CreateDirPath(applicationDirPath, BackupSubPath);
            var tempPath = CreateDirPath(applicationDirPath, TempSubPath);

            var patchSource = new FileSystemPatchSource(patchPath);
            var patcher = new DirectoryPatcher(new XdeltaPatcher(new XdeltaPatchSystem()), targetPath, backupPath, tempPath, patchSource);
            await patcher.ApplyPatchAsync();
        }

        private static string CreateDirPath(string applicationDirPath, string subPath)
        {
            var backupPath = Path.Combine(applicationDirPath, subPath);
            Directory.CreateDirectory(backupPath);
            return backupPath;
        }
    }
}
