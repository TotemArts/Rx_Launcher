
using System.IO;
namespace LauncherTwo
{
    static class GameInstallation
    {
        public static string GetRootPath()
        {
            string launcherPath;
            if (Properties.Settings.Default.GamePath != "")
            {
                launcherPath = Properties.Settings.Default.GamePath;
            }
            else
            {
                launcherPath = System.IO.Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../..");
                //launcherPath =  "C:\\Games\\Renegade X\\";
                //launcherPath = "C:\\Program Files (x86)\\Renegade X"; //For testing
                //launcherPath = "D:\\Program Files (x86)\\Renegade X"; //For Testing
            }
            return launcherPath + "\\";
        }

        public static bool IsRootPathPlausible()
        {
            return Directory.Exists(Path.Combine(GetRootPath(), "UDKGame"));
        }
    }
}
