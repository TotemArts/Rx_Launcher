using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RXPatchLib
{
    struct PatchMetadata
    {
        public string instructions_hash;
        public string version_name;
        public int version_number;
    }
    public class DirectoryPatchBuilder : IDisposable
    {
        readonly SHA256CryptoServiceProvider _cryptoProvider = new SHA256CryptoServiceProvider();
        readonly XdeltaPatchBuilder _patchBuilder;

        public DirectoryPatchBuilder(XdeltaPatchBuilder patchBuilder)
        {
            _patchBuilder = patchBuilder;
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

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);
        public Tuple<String, int> GetVersionInfo(string rootPath)
        {
            string versionFilePath = rootPath + Path.DirectorySeparatorChar + "UDKGame" + Path.DirectorySeparatorChar + "Config" + Path.DirectorySeparatorChar + "DefaultRenegadeX.ini";

            // Get version name
            var versionName = new StringBuilder();
            GetPrivateProfileString("RenX_Game.Rx_Game", "GameVersion", "", versionName, 255, versionFilePath);

            // Get version number
            var versionNumberString = new StringBuilder();
            GetPrivateProfileString("RenX_Game.Rx_Game", "GameVersionNumber", "", versionNumberString, 255, versionFilePath);
            int versionNumber = int.Parse(versionNumberString.ToString());

            return new Tuple<String, int>(versionName.ToString(), versionNumber);
        }
        public async Task CreatePatchAsync(string oldRootPath, string newRootPath, string patchPath)
        {
            var oldPaths = DirectoryPathIterator.GetChildPathsRecursive(oldRootPath).ToArray();
            var newPaths = DirectoryPathIterator.GetChildPathsRecursive(newRootPath).ToArray();

            var instructions = new List<FilePatchInstruction>();

            Console.WriteLine($"There are {instructions.Count} instructions in this update package");

            Directory.CreateDirectory(patchPath + Path.DirectorySeparatorChar + "full");
            Directory.CreateDirectory(patchPath + Path.DirectorySeparatorChar + "delta");

            var allPaths = oldPaths.Union(newPaths).ToArray();
            foreach (var path in allPaths)
            {
                // oldPath and newPath refer to files
                string oldPath = oldRootPath + Path.DirectorySeparatorChar + path;
                string newPath = newRootPath + Path.DirectorySeparatorChar + path;

                // Hashes of oldPath and newPath
                string oldHash = GetHash(oldPath);
                string newHash = GetHash(newPath);

                // Hashes of the full (compressed) and delta files
                string compressedHash = null;
                string deltaHash = null;

                long newSize = File.Exists(newPath) ? new FileInfo(newPath).Length : 0; // New file size
                DateTime oldLastWriteTime = File.Exists(oldPath) ? new FileInfo(oldPath).LastWriteTimeUtc : DateTime.MinValue; // Old last write time
                DateTime newLastWriteTime = File.Exists(newPath) ? new FileInfo(newPath).LastWriteTimeUtc : DateTime.MinValue; // New last write time
                long fullReplaceSize = 0; // Size of the full file
                long deltaSize = 0; // Size of the delta
                bool hasDelta = false; // True if there's a delta, false otherwise

                if (newHash != null)
                {
                    // Copy and compress newPath to fullPath
                    string fullPath = patchPath + Path.DirectorySeparatorChar + "full" + Path.DirectorySeparatorChar + newHash;
                    await _patchBuilder.CompressAsync(newPath, fullPath);
                    compressedHash = await Sha256.GetFileHashAsync(fullPath);
                    fullReplaceSize = new FileInfo(fullPath).Length;

                    // Write delta to deltaPath if the old file differs from the new one
                    if (oldHash != null && oldHash != newHash)
                    {
                        // Create delta
                        string deltaPath = patchPath + Path.DirectorySeparatorChar + "delta" + Path.DirectorySeparatorChar + newHash + "_from_" + oldHash;
                        await _patchBuilder.CreatePatchAsync(oldPath, newPath, deltaPath);

                        // Only keep the delta if it's smaller than the full download
                        if (new FileInfo(deltaPath).Length >= new FileInfo(fullPath).Length)
                        {
                            File.Delete(deltaPath);
                        }
                        else
                        {
                            deltaHash = await Sha256.GetFileHashAsync(deltaPath);
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

            // Write instructions
            try
            {
                Console.WriteLine($"Writing Instruction.json out to {patchPath + Path.DirectorySeparatorChar}instructions.json");
                string instructionsString = JsonConvert.SerializeObject(instructions);
                File.WriteAllText(patchPath + Path.DirectorySeparatorChar + "instructions.json", instructionsString);

                // Generate metadata
                var patchMetadata = new PatchMetadata();

                // Calculate instructions hash
                patchMetadata.instructions_hash = await Sha256.GetFileHashAsync(patchPath + Path.DirectorySeparatorChar + "instructions.json");

                // Get version info
                var version_info = GetVersionInfo(newRootPath);
                patchMetadata.version_name = version_info.Item1;
                patchMetadata.version_number = version_info.Item2;

                // Serialize metadata
                Console.WriteLine($"Writing metadata.json out to {patchPath + Path.DirectorySeparatorChar}metadata.json");
                string patchMetadataString = JsonConvert.SerializeObject(patchMetadata);
                File.WriteAllText(patchPath + Path.DirectorySeparatorChar + "metadata.json", patchMetadataString);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Exception while attempting to write instruction JSON or Hash file.\r\n{ex.Message}\r\n{ex.StackTrace}");
            }
        }
    }
}
