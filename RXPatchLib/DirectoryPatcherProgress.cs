using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    [Serializable]
    public class DirectoryPatcherProgressReport
    {
        private DirectoryPatchPhaseProgress _Analyze = new DirectoryPatchPhaseProgress();
        private DirectoryPatchPhaseProgress _Load = new DirectoryPatchPhaseProgress();
        private DirectoryPatchPhaseProgress _Apply = new DirectoryPatchPhaseProgress();

        public DirectoryPatchPhaseProgress Analyze { get { return _Analyze; } set { _Analyze = value; } }
        public DirectoryPatchPhaseProgress Load { get { return _Load; } set { _Load = value; } }
        public DirectoryPatchPhaseProgress Apply { get { return _Apply; } set { _Apply = value; } }


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
