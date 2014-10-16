
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
            }
            return launcherPath + "\\";
        }
    }
}
