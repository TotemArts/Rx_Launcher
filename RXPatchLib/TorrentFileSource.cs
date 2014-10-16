using MonoTorrent.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class PatchTorrentFileSource : ITorrentFileSource
    {
        public IEnumerable<FileMapping> Files
        {
            get
            {
                string instructionsString = File.ReadAllText(Path.Combine(PatchDirPath, "instructions.json"));
                var instructions = JsonConvert.DeserializeObject<List<FilePatchInstruction>>(instructionsString);

                var paths = new List<string>();
                paths.Add("instructions.json");
                foreach (var instruction in instructions)
                {
                    if (instruction.NewHash != null)
                    {
                        paths.Add(Path.Combine("full", instruction.NewHash));
                        if (instruction.HasDelta)
                        {
                            string deltaFileName = instruction.NewHash + "_from_" + instruction.OldHash;
                            paths.Add(Path.Combine("delta", deltaFileName));
                        }
                    }
                }
                return from path in paths.Distinct() select new FileMapping(Path.Combine(PatchDirPath, path), path);
            }
        }

        public bool IgnoreHidden { get { return false; } }
        public string TorrentName { get; internal set; }
        public string PatchDirPath { get; internal set; }

        public PatchTorrentFileSource(string torrentName, string patchDirPath)
        {
            TorrentName = torrentName;
            PatchDirPath = patchDirPath;
        }
    }
}
