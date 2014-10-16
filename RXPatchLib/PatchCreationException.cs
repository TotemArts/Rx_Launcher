using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    class PatchCreationException : Exception
    {
        public CommandExecutionException CommandExecutionException { get; private set; }

        public PatchCreationException(CommandExecutionException commandExecutionException)
        {
            CommandExecutionException = commandExecutionException;
        }

        public override string ToString()
        {
            return "Patch creation failed: " + CommandExecutionException.ToString();
        }
    }
}
