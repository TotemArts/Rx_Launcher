using System;
using System.Collections.Generic;
using System.Linq;

namespace RXPatchLib
{
    [Serializable]
    public class DirectoryPatcherProgressReport
    {
        private DirectoryPatchPhaseProgress _analyze = new DirectoryPatchPhaseProgress();
        private DirectoryPatchPhaseProgress _load = new DirectoryPatchPhaseProgress();
        private DirectoryPatchPhaseProgress _apply = new DirectoryPatchPhaseProgress();

        public DirectoryPatchPhaseProgress Analyze { get { return _analyze; } set { _analyze = value; } }
        public DirectoryPatchPhaseProgress Load { get { return _load; } set { _load = value; } }
        public DirectoryPatchPhaseProgress Apply { get { return _apply; } set { _apply = value; } }


        public override string ToString()
        {
            var phases = new Dictionary<string, DirectoryPatchPhaseProgress> {
                { "analyze", Analyze },
                { "load", Load },
                { "apply", Apply },
            };
            return string.Join("\n", from p in phases select p.Key + ": " + p.Value.ToString());
        }

        public bool IsCancellationPossible
        {
            get
            {
                return Apply.State == DirectoryPatchPhaseProgress.States.Unstarted;
            }
        }
    }
}
