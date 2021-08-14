using RXPatchLib;
using ManyConsole;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace RXPatch
{
    class CreateCommand : ConsoleCommand
    {
        bool _removeOutput;

        public CreateCommand()
        {
            IsCommand("create", "Create patch");
            HasOption("removeoutput", "Remove contents of output directory on startup.", value => _removeOutput = value != null);
            HasAdditionalArguments(1, "<config file path>");
        }

        public override int Run(string[] remainingArguments)
        {
            return RunAsync(remainingArguments).Result;
        }

        private async Task<int> RunAsync(string[] remainingArguments)
        {
            var configFilePath = remainingArguments[0];

            PatchInfo patchInfo = null;
            var errors = new List<string>();
            try
            {
                var configString = File.ReadAllText(configFilePath);
                patchInfo = JsonConvert.DeserializeObject<PatchInfo>(configString);
                if (patchInfo == null)
                {
                    errors.Add("No configuration was read.");
                }
            }
            catch (Exception e)
            {
                errors.Add("Failed to read configuration file. " + e.Message);
            }

            if (patchInfo != null)
            {
                if (patchInfo.OldPath == "" || !Directory.Exists(patchInfo.OldPath))
                {
                    errors.Add("OldPath was unspecified or does not exist.");
                }

                if (patchInfo.NewPath == "" || !Directory.Exists(patchInfo.NewPath))
                {
                    errors.Add("NewPath was unspecified or path does not exist.");
                }

                if (patchInfo.PatchPath == "")
                {
                    errors.Add("PatchPath path was unspecified.");
                }
            }

            if (errors.Count > 0)
            {
                var example = new PatchInfo
                {
                    OldPath = Path.Combine("path", "to", "old", "files"),
                    NewPath = Path.Combine("path", "to", "new", "files"),
                    PatchPath = Path.Combine("path", "to", "place", "output", "files"),
                };
                Console.Error.WriteLine("Invalid configuration. Please specify a configuration file that looks like the following:");
                Console.Error.WriteLine(JsonConvert.SerializeObject(example, Formatting.Indented));
                foreach (var error in errors)
                {
                    Console.Error.WriteLine("Error: " + error);
                }
                return 1;
            }

            if (_removeOutput)
            {
                DirectoryEx.DeleteContents(patchInfo.PatchPath);
            }
            else if (Directory.Exists(patchInfo.PatchPath))
            {
                Console.Error.WriteLine("Warning: Output path already exists. Use --removeoutput to erase its contents.");
            }

            await new RxPatchBuilder().CreatePatchAsync(patchInfo);
            return 0;
        }
    }
}
