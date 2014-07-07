using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LauncherTwo
{
    public static class LaunchTools
    {
        private static string Arguments = string.Empty;
        public static readonly string INI_PATH = "-ini:UDKGame:DefaultPlayer.Name=";
        public static readonly string EXE_PATH = "\\Binaries\\Win32\\UDK.exe";
        static Process LastRunprocess;

        public static bool LaunchGame(string Username)
        {
            Arguments = INI_PATH + Username;

            Process UDKProcess = new Process();
            UDKProcess.StartInfo.FileName = GetPath();
            UDKProcess.StartInfo.Arguments = Arguments;

            try
            {
                UDKProcess.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            LastRunprocess = UDKProcess;
            return true;
        }

        public static bool JoinServer(string anIPAdress, string Username)
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
                return false;
            }
            LastRunprocess = UDKProcess;
            return true;

        }

        public static bool LastRunStillRunning()
        {
            if (LastRunprocess != null && !LastRunprocess.HasExited)
                return true;
            else return false;
        }

        private static string GetPath()
        {
            string FileName = string.Empty; 
            // Exe location
            FileName += System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            // Go up one directory.
            FileName = Directory.GetParent(FileName).FullName;
            // Now into UDK.exe location.
            FileName += EXE_PATH;

            return FileName; 
        }
    }
}
