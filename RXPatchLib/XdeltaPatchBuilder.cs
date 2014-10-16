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
    public class XdeltaPatchBuilder
    {
        public long _SourceBufferSize = 512 * 1024 * 1024;
        public long SourceWindowSize {
            get { return _SourceBufferSize; }
            set { if (value < 1) throw new InvalidOperationException(); _SourceBufferSize = value; }
        }

        private int _CompressionLevel = 9;
        public int CompressionLevel {
            get { return _CompressionLevel; }
            set { if (value < 0 || value > 9) throw new InvalidOperationException(); _CompressionLevel = value; }
        }

        private string SecondaryLevelCompression = "lzma";

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
                    "-B" + SourceWindowSize.ToString(),
                    "-" + CompressionLevel,
                    "-S", SecondaryLevelCompression,
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
