using System;

namespace RXPatchLib
{
    class CommandExecutionException : Exception
    {
        public string Path { get; private set; }
        public string[] Arguments { get; private set; }
        public int ExitCode { get; private set; }

        public CommandExecutionException(string path, string[] arguments, int exitCode)
        {
            Path = path;
            Arguments = arguments;
            ExitCode = exitCode;
        }
        public override string Message
        {
            get
            {
                return "Command execution failed: " + Path + " " + ProcessEx.ToCommandLineArgumentString(Arguments) + " exited with code " + ExitCode.ToString();
            }
        }
    }
}
