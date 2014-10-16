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
    class DirectoryPatchBuilder : IDisposable
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
                return BitConverter.ToString(CryptoProvider.ComputeHash(stream));
        }

        public async Task CreatePatchAsync(string oldRootPath, string newRootPath, string patchPath)
        {
            var oldPaths = DirectoryPathIterator.GetChildPathsRecursive(oldRootPath).ToArray();
            var newPaths = DirectoryPathIterator.GetChildPathsRecursive(newRootPath).ToArray();

            var instructions = new List<FilePatchInstruction>();

            var allPaths = oldPaths.Union(newPaths).ToArray();
            foreach (var path in allPaths)
            {
                string oldPath = oldRootPath + Path.DirectorySeparatorChar + path;
                string newPath = newRootPath + Path.DirectorySeparatorChar + path;
                string oldHash = GetHash(oldPath);
                string newHash = GetHash(newPath);
                bool hasDelta = false;

                if (newHash != null)
                {
                    File.Copy(newPath, patchPath + Path.DirectorySeparatorChar + newHash, true);

                    if (oldHash != null && oldHash != newHash)
                    {
                        string deltaPath = patchPath + Path.DirectorySeparatorChar + oldHash + "_to_" + newHash;
                        await PatchBuilder.CreatePatchAsync(oldPath, newPath, deltaPath);
                        if (new FileInfo(deltaPath).Length > new FileInfo(newPath).Length)
                        {
                            File.Delete(deltaPath);
                        }
                        else
                        {
                            hasDelta = true;
                        }
                    }
                }

                instructions.Add(new FilePatchInstruction
                {
                    Path = path,
                    OldHash = oldHash,
                    NewHash = newHash,
                    HasDelta = hasDelta,
                });
            }

            string instructionsString = JsonConvert.SerializeObject(instructions);
            File.WriteAllText(patchPath + Path.DirectorySeparatorChar + "instructions.json", instructionsString);
        }
    }
}
