using System;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class XdeltaPatcher
    {
        public long SourceBufferSize = 512 * 1024 * 1024;
        public long SourceWindowSize
        {
            get { return SourceBufferSize; }
            set { if (value < 1) throw new InvalidOperationException(); SourceBufferSize = value; }
        }

        private readonly XdeltaPatchSystem _patchSystem;

        public XdeltaPatcher(XdeltaPatchSystem patchSystem)
        {
            _patchSystem = patchSystem;
        }

        public async Task ApplyPatchAsync(string oldPath, string patchedPath, string patchPath)
        {
            try
            {
                await _patchSystem.RunCommandAsync(
                    "-d",
                    "-B" + SourceWindowSize.ToString(),
                    "-f",
                    "-s", oldPath,
                    patchPath,
                    patchedPath);
            }
            catch (CommandExecutionException e)
            {
                throw new PatchCreationException(e);
            }
        }

        public async Task DecompressAsync(string patchedPath, string patchPath)
        {
            try
            {
                await _patchSystem.RunCommandAsync(
                    "-d",
                    "-B" + SourceWindowSize.ToString(),
                    "-f",
                    patchPath,
                    patchedPath);
            }
            catch (CommandExecutionException e)
            {
                throw new PatchCreationException(e);
            }
        }
    }
}
