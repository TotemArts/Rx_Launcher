using System;
using System.IO;

namespace RXPatchLib
{
    class TemporaryFile : IDisposable
    {
        public string Path { get; private set; }

        public TemporaryFile()
        {
            Path = System.IO.Path.GetTempFileName();
        }

        public void Dispose()
        {
            File.Delete(Path);
        }
    }
}
