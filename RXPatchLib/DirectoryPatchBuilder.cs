using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class DirectoryPatchBuilder : IDisposable
    {
        SHA1CryptoServiceProvider CryptoProvider = new SHA1CryptoServiceProvider();
        XdeltaPatchBuilder PatchBuilder;

        public DirectoryPatchBuilder(XdeltaPatchBuilder patchBuilder)
        {
            PatchBuilder = patchBuilder;
        }

        public void Dispose()
        {
            if (CryptoProvider != null) CryptoProvider.Dispose();
        }

        string GetHash(string path)
        {
            if (!File.Exists(path))
                return null;

            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(CryptoProvider.ComputeHash(stream)).Replace("-", string.Empty);
        }

        public async Task CreatePatchAsync(string oldRootPath, string newRootPath, string patchPath)
        {
            var oldPaths = DirectoryPathIterator.GetChildPathsRecursive(oldRootPath).ToArray();
            var newPaths = DirectoryPathIterator.GetChildPathsRecursive(newRootPath).ToArray();

            var instructions = new List<FilePatchInstruction>();

            Directory.CreateDirectory(patchPath + Path.DirectorySeparatorChar + "full");
            Directory.CreateDirectory(patchPath + Path.DirectorySeparatorChar + "delta");

            var allPaths = oldPaths.Union(newPaths).ToArray();
            foreach (var path in allPaths)
            {
                string oldPath = oldRootPath + Path.DirectorySeparatorChar + path;
                string newPath = newRootPath + Path.DirectorySeparatorChar + path;
                string oldHash = GetHash(oldPath);
                string newHash = GetHash(newPath);
                long newSize = File.Exists(newPath) ? new FileInfo(newPath).Length : 0;
                DateTime oldLastWriteTime = File.Exists(oldPath) ? new FileInfo(oldPath).LastWriteTimeUtc : DateTime.MinValue;
                DateTime newLastWriteTime = File.Exists(newPath) ? new FileInfo(newPath).LastWriteTimeUtc : DateTime.MinValue;
                long fullReplaceSize = 0;
                long deltaSize = 0;
                bool hasDelta = false;

                if (newHash != null)
                {
                    string fullPath = patchPath + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + newHash;
                    await PatchBuilder.CompressAsync(newPath, fullPath);
                    fullReplaceSize = new FileInfo(fullPath).Length;

                    if (oldHash != null && oldHash != newHash)
                    {
                        string deltaPath = patchPath + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + newHash + "_from_" + oldHash;
                        await PatchBuilder.CreatePatchAsync(oldPath, newPath, deltaPath);
                        if (new FileInfo(deltaPath).Length >= new FileInfo(fullPath).Length)
                        {
                            File.Delete(deltaPath);
                        }
                        else
                        {
                            deltaSize = new FileInfo(deltaPath).Length;
                            hasDelta = true;
                        }
                    }
                }

                instructions.Add(new FilePatchInstruction
                {
                    Path = path,
                    OldHash = oldHash,
                    NewHash = newHash,
                    OldLastWriteTime = oldLastWriteTime,
                    NewLastWriteTime = newLastWriteTime,
                    FullReplaceSize = fullReplaceSize,
                    DeltaSize = deltaSize,
                    HasDelta = hasDelta,
                });
            }

            // Write instructions
            string instructionsString = JsonConvert.SerializeObject(instructions);
            File.WriteAllText(patchPath + Path.DirectorySeparatorChar + "instructions.json", instructionsString);

            // Write SHA1 hash of instructions.json to instructions_hash.txt
            File.WriteAllText(patchPath + Path.DirectorySeparatorChar + "instructions_hash.txt", await SHA1.GetFileHashAsync(patchPath + Path.DirectorySeparatorChar + "instructions.json"));
        }
    }
}
