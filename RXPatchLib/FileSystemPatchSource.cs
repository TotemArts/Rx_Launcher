using System.IO;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class FileSystemPatchSource : IPatchSource
    {
        string RootPath;

        public FileSystemPatchSource(string rootPath)
        {
            RootPath = rootPath;
        }

        public string GetSystemPath(string subPath)
        {
            return Path.Combine(RootPath, subPath);
        }

        public Task Load(string subPath)
        {
            return TaskExtensions.CompletedTask;
        }
    }
}
