using System;

namespace RXPatchLib
{
    struct FilePatchInstruction
    {
        public string Path;
        public string OldHash;
        public string NewHash;
        public string CompressedHash;
        public string DeltaHash;
        public DateTime OldLastWriteTime;
        public DateTime NewLastWriteTime;
        public long FullReplaceSize;
        public long DeltaSize;
        public bool HasDelta;
        public bool isComplete;
        public bool isActive;
    }
}
