using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class XdeltaPatchBuilder
    {
        public int _SourceBufferSize = 2*1024*1024;
        public int SourceBufferSize {
            get { return _SourceBufferSize; }
            set { if (value < 1) throw new InvalidOperationException(); _SourceBufferSize = value; }
        }

        private int _CompressionLevel = 9;
        public int CompressionLevel {
            get { return _CompressionLevel; }
            set { if (value < 1 || value > 9) throw new InvalidOperationException(); _CompressionLevel = value; }
        }

        private string SecondLevelCompression = "lzma";

        private XdeltaPatchSystem PatchSystem;

        public XdeltaPatchBuilder(XdeltaPatchSystem patchSystem)
        {
            PatchSystem = patchSystem;
        }

        public async Task CreatePatchAsync(string oldPath, string newPath, string patchPath)
        {
            try
            {
                await PatchSystem.RunCommandAsync(
                    "-e",
                    "-B" + SourceBufferSize.ToString(),
                    "-" + CompressionLevel,
                    "-S", SecondLevelCompression,
                    "-s", oldPath,
                    "-f",
                    newPath,
                    patchPath);
            }
            catch (CommandExecutionException e)
            {
                throw new PatchCreationException(e);
            }
        }
    }
}
