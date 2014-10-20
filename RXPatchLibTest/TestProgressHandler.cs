using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
