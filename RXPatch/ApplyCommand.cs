using RXPatchLib;
using ManyConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RXPatch
{
    class ApplyCommand : ConsoleCommand
    {
        public ApplyCommand()
        {
            IsCommand("apply", "Apply patch");
            HasAdditionalArguments(3, "<patch dir> <target dir> <temporary/working dir>");
        }

        public override int Run(string[] remainingArguments)
        {
            return RunAsync(remainingArguments).Result;
        }

        private async Task<int> RunAsync(string[] remainingArguments)
        {
            var PatchDir = remainingArguments[0];
            var TargetDir = remainingArguments[1];
            var WorkingDir = remainingArguments[2];

            var errors = new List<string>();
            if (!Directory.Exists(PatchDir))
            {
                errors.Add("Patch dir does not exist.");
            }

            if (!Directory.Exists(TargetDir))
            {
                errors.Add("Target dir does not exist.");
            }

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.Error.WriteLine("Error: " + error);
                }
                return 1;
            }

            await new RXPatcher().ApplyPatchFromFilesystem(PatchDir, TargetDir, WorkingDir);

            return 0;
        }
    }
}
