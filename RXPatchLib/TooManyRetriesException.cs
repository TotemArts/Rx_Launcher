using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    class TooManyRetriesException : Exception
    {
        public List<Exception> Exceptions { get; private set; }

        public TooManyRetriesException(IEnumerable<Exception> exceptions) :
            base("Too many retries.")
        {
            Exceptions = new List<Exception>(exceptions);
        }
    }
}
