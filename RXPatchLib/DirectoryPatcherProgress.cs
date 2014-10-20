using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    [Serializable]
    public class DirectoryPatcherProgressReport
    {
        public DirectoryPatchPhaseProgress Analyze = new DirectoryPatchPhaseProgress();
        public DirectoryPatchPhaseProgress Load = new DirectoryPatchPhaseProgress();
        public DirectoryPatchPhaseProgress Apply = new DirectoryPatchPhaseProgress();

        public override string ToString()
        {
            var phases = new Dictionary<string, DirectoryPatchPhaseProgress> {
                { "analyze", Analyze },
                { "load", Load },
                { "apply", Apply },
            };
            return string.Join("\n", from p in phases select p.Key + ": " + p.Value.ToString());
        }
    }
}
