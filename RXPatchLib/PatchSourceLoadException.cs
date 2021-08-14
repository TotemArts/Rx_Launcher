using System;

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
