using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class HashMistmatchException : Exception
    {
        public HashMistmatchException()
            : base("Hash mismatch") { }
    }
}
