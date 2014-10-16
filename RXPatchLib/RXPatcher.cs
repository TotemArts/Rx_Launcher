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

        public async Task ApplyPatchFromWeb(string baseUrl, string targetPath, string workingDirPath)
        {
            var backupPath = CreateDirPath(workingDirPath, BackupSubPath);
            var downloadPath = CreateDirPath(workingDirPath, DownloadSubPath);
            var tempPath = CreateDirPath(workingDirPath, TempSubPath);

            using (var patchSource = new WebPatchSource(baseUrl, downloadPath))
            {
                var patcher = new DirectoryPatcher(new XdeltaPatcher(new XdeltaPatchSystem()), targetPath, backupPath, tempPath, patchSource);
                await patcher.ApplyPatchAsync();
            }
        }
        public async Task ApplyPatchFromFilesystem(string patchPath, string targetPath, string workingDirPath)
        {
            var backupPath = CreateDirPath(workingDirPath, BackupSubPath);
            var tempPath = CreateDirPath(workingDirPath, TempSubPath);

            var patchSource = new FileSystemPatchSource(patchPath);
            var patcher = new DirectoryPatcher(new XdeltaPatcher(new XdeltaPatchSystem()), targetPath, backupPath, tempPath, patchSource);
            await patcher.ApplyPatchAsync();
        }

        private static string CreateDirPath(string workingDirPath, string subPath)
        {
            var backupPath = Path.Combine(workingDirPath, subPath);
            Directory.CreateDirectory(backupPath);
            return backupPath;
        }
    }
}
