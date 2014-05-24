using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LauncherTwo
{
    public static class LaunchTools
    {
        private static string Arguments = string.Empty;
        public static readonly string INI_PATH = "-ini:UDKGame:DefaultPlayer.Name=";
        public static readonly string EXE_PATH = "\\Binaries\\Win32\\UDK.exe";

        public static void JoinGame(string anIPAdress, string Username)
        {
            Arguments = anIPAdress + " ";
            Arguments += INI_PATH + Username;

            Process UDKProcess = new Process();
            UDKProcess.StartInfo.FileName = GetPath();
            UDKProcess.StartInfo.Arguments = Arguments;

            try
            {
                UDKProcess.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }

        }

        private static string GetPath()
        {
            string FileName = string.Empty; 
            FileName += System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            FileName += EXE_PATH;
            return FileName; 
        }
    }
}
