using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RXPatchLib
{
    static class ProcessEx
    {
        public static string EscapeCommandLineArgument(string argument)
        {
            // According to http://msdn.microsoft.com/en-us/library/17w5ykft%28v=vs.85%29.aspx
            return "\"" + Regex.Replace(argument, "(\\\\*)(\"|\\\\$)", "$1$1\\$2") + "\"";
        }
        public static string ToCommandLineArgumentString(string[] arguments)
        {
            return string.Join(" ", arguments.Select(EscapeCommandLineArgument));
        }
        public static async Task<int> RunAsync(string path, params string[] arguments)
        {
            string argumentsString = ToCommandLineArgumentString(arguments);
            var startInfo = new ProcessStartInfo(path, argumentsString);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;

            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
            using (Process process = new Process())
            {
                process.EnableRaisingEvents = true;
                string x = Directory.GetCurrentDirectory();
                process.Exited += (sender, e) => { tcs.SetResult(process.ExitCode); };
                process.StartInfo = startInfo;
                process.Start();
                return await tcs.Task;
            }
        }
    }
}
