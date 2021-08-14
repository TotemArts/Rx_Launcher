using System;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class XdeltaPatchSystem
    {
        public string ExecutablePath;

        public XdeltaPatchSystem(string executablePath)
        {
            ExecutablePath = executablePath;
        }

        public async Task RunCommandAsync(params string[] arguments)
        {
            int exitCode = await ProcessEx.RunAsync(ExecutablePath, arguments);
            if (exitCode != 0)
            {
                throw new CommandExecutionException(ExecutablePath, arguments, exitCode);
            }
        }
    }

    public class XdeltaPatchSystemFactory
    {
        public static XdeltaPatchSystem X32 = new XdeltaPatchSystem("xdelta3-3.1.0-i686");
        public static XdeltaPatchSystem X64 = new XdeltaPatchSystem("xdelta3-3.1.0-x86_64.exe");
        public static XdeltaPatchSystem Preferred = Environment.Is64BitOperatingSystem ? X64 : X32;
    }
}
