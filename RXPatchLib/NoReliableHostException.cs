using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    class NoReliableHostException : Exception
    {
        public NoReliableHostException()
            : base("No valid mirrors are available; please check your network connection or try again later.") { }
    }
}
