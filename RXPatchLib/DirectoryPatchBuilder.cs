using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RXPatchLib
{
    public class DirectoryPatchBuilder : IDisposable
    {
        readonly SHA256CryptoServiceProvider _cryptoProvider = new SHA256CryptoServiceProvider();
        readonly XdeltaPatchBuilder PatchBuilder;

        public DirectoryPatchBuilder(XdeltaPatchBuilder patchBuilder)
        {
            PatchBuilder = patchBuilder;
        }

        public void Dispose()
        {
            if (_cryptoProvider != null) _cryptoProvider.Dispose();
        }

        string GetHash(string path)
        {
            if (!File.Exists(path))
                return null;

            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(_cryptoProvider.ComputeHash(stream)).Replace("-", string.Empty);
        }

        public async Task CreatePatchAsync(string oldRootPath, string newRootPath, string patchPath)
        {
            var oldPaths = DirectoryPathIterator.GetChildPathsRecursive(oldRootPath).ToArray();
            var newPaths = DirectoryPathIterator.GetChildPathsRecursive(newRootPath).ToArray();

            var instructions = new List<FilePatchInstruction>();

            RxLogger.Logger.Instance.Write($"There are {instructions.Count} instructions in this update package");

            Directory.CreateDirectory(patchPath + Path.DirectorySeparatorChar + "full");
            Directory.CreateDirectory(patchPath + Path.DirectorySeparatorChar + "delta");

            var allPaths = oldPaths.Union(newPaths).ToArray();
            foreach (var path in allPaths)
            {
                string oldPath = oldRootPath + Path.DirectorySeparatorChar + path;
                string newPath = newRootPath + Path.DirectorySeparatorChar + path;
                string oldHash = GetHash(oldPath);
                string newHash = GetHash(newPath);

                string compressedHash = null;
                string deltaHash = null;
                
                DateTime oldLastWriteTime = File.Exists(oldPath) ? new FileInfo(oldPath).LastWriteTimeUtc : DateTime.MinValue;
                DateTime newLastWriteTime = File.Exists(newPath) ? new FileInfo(newPath).LastWriteTimeUtc : DateTime.MinValue;
                long fullReplaceSize = 0;
                long deltaSize = 0;
                bool hasDelta = false;

                if (newHash != null)
                {
                    string fullPath = patchPath + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + newHash;
                    await PatchBuilder.CompressAsync(newPath, fullPath);
                    compressedHash = await SHA256.GetFileHashAsync(fullPath);
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
                            deltaHash = await SHA256.GetFileHashAsync(deltaPath);
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
                    CompressedHash = compressedHash,
                    DeltaHash = deltaHash,
                    OldLastWriteTime = oldLastWriteTime,
                    NewLastWriteTime = newLastWriteTime,
                    FullReplaceSize = fullReplaceSize,
                    DeltaSize = deltaSize,
                    HasDelta = hasDelta,
                });
            }

            string instructionsString = JsonConvert.SerializeObject(instructions);
            File.WriteAllText(patchPath + Path.DirectorySeparatorChar + "instructions.json", instructionsString);
        }
    }
}
