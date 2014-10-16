using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class XdeltaPatcher
    {
        public long _SourceBufferSize = 2L * 1024 * 1024 * 1024;
        public long SourceWindowSize
        {
            get { return _SourceBufferSize; }
            set { if (value < 1) throw new InvalidOperationException(); _SourceBufferSize = value; }
        }

        private XdeltaPatchSystem PatchSystem;

        public XdeltaPatcher(XdeltaPatchSystem patchSystem)
        {
            PatchSystem = patchSystem;
        }

        public async Task ApplyPatchAsync(string oldPath, string patchedPath, string patchPath)
        {
            try
            {
                await PatchSystem.RunCommandAsync(
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
    }
}
