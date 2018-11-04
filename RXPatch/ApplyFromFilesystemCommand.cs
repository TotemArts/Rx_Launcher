using RXPatchLib;
using ManyConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace RXPatch
{
    class ApplyFromFilesystemCommand : ConsoleCommand
    {
        public ApplyFromFilesystemCommand()
        {
            IsCommand("apply_local", "Apply patch from local filesystem");
            HasAdditionalArguments(3, "<patch dir> <destination dir> <application dir>");
        }

        public override int Run(string[] remainingArguments)
        {
            return RunAsync(remainingArguments).Result;
        }

        private async Task<int> RunAsync(string[] remainingArguments)
        {
            var patchDir = remainingArguments[0];
            var destinationDir = remainingArguments[1];
            var applicationDir = remainingArguments[2];

            var errors = new List<string>();
            if (!Directory.Exists(patchDir))
            {
                errors.Add("Patch dir does not exist.");
            }

            if (!Directory.Exists(destinationDir))
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

            await ProgressReporter.AwaitWithProgressReporting<DirectoryPatcherProgressReport>(
                (progress) => new RxPatcher().ApplyPatchFromFilesystem(patchDir, destinationDir, applicationDir, progress, new CancellationTokenSource(), null)
            );

            return 0;
        }
    }
}
