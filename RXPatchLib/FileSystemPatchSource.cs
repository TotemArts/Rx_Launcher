using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class FileSystemPatchSource : IPatchSource
    {
        readonly string _rootPath;

        public FileSystemPatchSource(string rootPath)
        {
            _rootPath = rootPath;
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(_rootPath, subPath);
        }

        public async Task Load(string subPath, string hash, CancellationToken cancellationToken, Action<long, long, byte> progressCallback)
        {
            if (hash != null && await Sha256.GetFileHashAsync(GetSystemPath(subPath)) != hash)
                throw new PatchSourceLoadException(subPath, hash);
        }
    }
}
