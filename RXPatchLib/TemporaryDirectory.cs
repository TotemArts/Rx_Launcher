using System;
using System.IO;

namespace RXPatchLib
{
    class TemporaryDirectory : IDisposable
    {
        public string Path { get; private set; }

        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            Directory.Delete(Path, true);
        }
    }
}
