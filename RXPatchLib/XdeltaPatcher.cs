using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class XdeltaPatcher
    {
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
