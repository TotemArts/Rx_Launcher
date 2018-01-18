using RXPatchLib;
using ManyConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace RXPatch
{
    class ApplyFromWebCommand : ConsoleCommand
    {
        public ApplyFromWebCommand()
        {
            IsCommand("apply_web", "Apply patch via a URL (typically HTTP or FTP)");
            HasAdditionalArguments(3, "<patch url> <destination dir> <application dir>");
        }

        public override int Run(string[] remainingArguments)
        {
            return RunAsync(remainingArguments).Result;
        }

        private async Task<int> RunAsync(string[] remainingArguments)
        {
            var patchUrl = remainingArguments[0];
            var targetDir = remainingArguments[1];
            var applicationDir = remainingArguments[2];

            var errors = new List<string>();

            if (!Directory.Exists(targetDir))
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

            RxPatcher.Instance.AddNewUpdateServer(remainingArguments[0], "");

            await ProgressReporter.AwaitWithProgressReporting<DirectoryPatcherProgressReport>(
                (progress) => RxPatcher.Instance.ApplyPatchFromWebDownloadTask(RxPatcher.Instance.GetNextUpdateServerEntry(), targetDir, applicationDir, progress, new CancellationToken(), null) // intentionally skipping instructions.json verification
            );

            return 0;
        }
    }
}
