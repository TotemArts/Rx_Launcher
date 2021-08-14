using System;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class XdeltaPatchBuilder
    {
        public long SourceBufferSize = 512 * 1024 * 1024;
        public long SourceWindowSize {
            get { return SourceBufferSize; }
            set { if (value < 1) throw new InvalidOperationException(); SourceBufferSize = value; }
        }

        private int _compressionLevel = 9;
        public int CompressionLevel {
            get { return _compressionLevel; }
            set { if (value < 0 || value > 9) throw new InvalidOperationException(); _compressionLevel = value; }
        }

        private readonly string _secondaryLevelCompression = "lzma";

        private readonly XdeltaPatchSystem _patchSystem;

        public XdeltaPatchBuilder(XdeltaPatchSystem patchSystem)
        {
            _patchSystem = patchSystem;
        }

        public async Task CreatePatchAsync(string oldPath, string newPath, string patchPath)
        {
            try
            {
                await _patchSystem.RunCommandAsync(
                    "-e",
                    "-B" + SourceWindowSize.ToString(),
                    "-" + CompressionLevel,
                    "-S", _secondaryLevelCompression,
                    "-s", oldPath,
                    "-f",
                    "-A", "",
                    newPath,
                    patchPath);
            }
            catch (CommandExecutionException e)
            {
                throw new PatchCreationException(e);
            }
        }

        public async Task CompressAsync(string newPath, string patchPath)
        {
            try
            {
                await _patchSystem.RunCommandAsync(
                    "-e",
                    "-B" + SourceWindowSize.ToString(),
                    "-" + CompressionLevel,
                    "-S", _secondaryLevelCompression,
                    "-f",
                    "-A", "",
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
