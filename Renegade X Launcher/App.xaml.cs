using System.Windows;

namespace LauncherTwo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            if (LauncherTwo.Properties.Settings.Default.UpgradeRequired)
            {
                LauncherTwo.Properties.Settings.Default.Upgrade();
                LauncherTwo.Properties.Settings.Default.UpgradeRequired = false;
                LauncherTwo.Properties.Settings.Default.Save();
            }
        }
    }
}
