using System;

namespace RXPatchLib
{
    static class TestProgressHandlerFactory
    {
        public static IProgress<DirectoryPatcherProgressReport> Create()
        {
            return new Progress<DirectoryPatcherProgressReport>(report => Console.WriteLine(report));
        }
    }
}
