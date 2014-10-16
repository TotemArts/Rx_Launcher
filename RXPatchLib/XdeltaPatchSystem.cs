using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class XdeltaPatchSystem
    {
        public string ExecutablePath = "xdelta3-3.0.8.x86-64.exe";

        public async Task RunCommandAsync(params string[] arguments)
        {
            int exitCode = await ProcessEx.RunAsync(ExecutablePath, arguments);
            if (exitCode != 0)
            {
                throw new CommandExecutionException(ExecutablePath, arguments, exitCode);
            }
        }
    }
}
