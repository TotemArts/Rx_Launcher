using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    struct FilePatchInstruction
    {
        public string Path;
        public string OldHash;
        public string NewHash;
        public long OldSize;
        public long NewSize;
        public long DeltaSize;
        public bool HasDelta;
    }
}
