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
        public DateTime OldLastWriteTime;
        public DateTime NewLastWriteTime;
        public long FullReplaceSize;
        public long DeltaSize;
        public bool HasDelta;
        public bool isComplete;
        public bool isActive;
    }
}
