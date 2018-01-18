using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    class PatchSourceLoadException : Exception
    {
        private string _subPath;
        private string _hash;

        public PatchSourceLoadException(string subPath, string hash)
        {
            _subPath = subPath;
            _hash = hash;
        }
    }
}
