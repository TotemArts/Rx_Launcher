using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    class PatchSourceLoadException : Exception
    {
        private string SubPath;
        private string Hash;

        public PatchSourceLoadException(string subPath, string hash)
        {
            SubPath = subPath;
            Hash = hash;
        }
    }
}
