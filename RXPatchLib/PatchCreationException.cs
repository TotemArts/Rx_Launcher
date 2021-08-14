using System;

namespace RXPatchLib
{
    class PatchCreationException : Exception
    {
        public CommandExecutionException CommandExecutionException { get; private set; }

        public PatchCreationException(CommandExecutionException commandExecutionException)
        {
            CommandExecutionException = commandExecutionException;
        }

        public override string Message
        {
            get
            {
                return "Patch creation failed: " + CommandExecutionException.Message;
            }
        }
    }
}
