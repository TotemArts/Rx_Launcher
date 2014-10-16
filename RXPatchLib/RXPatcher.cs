using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class RXPatcher
    {
        public async Task ApplyPatch(string baseUrl, string targetPath, string workingDirPath)
        {
            var backupPath = CreateDirPath(workingDirPath, "backup/patch");
            var downloadPath = CreateDirPath(workingDirPath, "temp/patch/download"); // Do not use the system temp dir; the user should clear this when out of space.
            var patchTempPath = CreateDirPath(workingDirPath, "temp/patch/patch"); // Do not use the system temp dir, as it may be on a different disk, making moves expensive.

            using (var patchSource = new WebPatchSource(baseUrl, downloadPath))
            {
                var patcher = new DirectoryPatcher(new XdeltaPatcher(new XdeltaPatchSystem()), targetPath, backupPath, patchTempPath, patchSource);
                await patcher.ApplyPatchAsync();
            }
        }

        private static string CreateDirPath(string workingDirPath, string subPath)
        {
            var backupPath = Path.Combine(workingDirPath, subPath);
            Directory.CreateDirectory(backupPath);
            return backupPath;
        }
    }
}
