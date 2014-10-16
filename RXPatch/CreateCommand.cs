using RXPatchLib;
using ManyConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatch
{
    class CreateCommand : ConsoleCommand
    {
        bool RemoveOutput = false;
        public CreateCommand()
        {
            IsCommand("create", "Create patch");
            HasOption("removeoutput", "Remove contents of output directory on startup.", value => RemoveOutput = value != null);
            HasAdditionalArguments(3, "<old path> <new path> <output path>");
        }

        public override int Run(string[] remainingArguments)
        {
            return RunAsync(remainingArguments).Result;
        }

        private async Task<int> RunAsync(string[] remainingArguments)
        {
            var oldPath = remainingArguments[0];
            var newPath = remainingArguments[1];
            var patchPath = remainingArguments[2];
            var patchSystem = new XdeltaPatchSystem();
            var patchBuilder = new XdeltaPatchBuilder(patchSystem);
            var directoryPatchBuilder = new DirectoryPatchBuilder(patchBuilder);

            if (!Directory.Exists(oldPath))
            {
                Console.Error.WriteLine("Error: Old path does not exist.");
                return 1;
            }

            if (!Directory.Exists(newPath))
            {
                Console.Error.WriteLine("Error: New path does not exist.");
                return 1;
            }

            if (RemoveOutput)
            {
                DirectoryEx.DeleteContents(patchPath);
            }
            else  if (Directory.Exists(patchPath))
            {
                Console.Error.WriteLine("Warning: Output path already exists. Use --removeoutput to erase its contents.");
            }

            await directoryPatchBuilder.CreatePatchAsync(oldPath, newPath, patchPath);
            return 0;
        }
    }
}
