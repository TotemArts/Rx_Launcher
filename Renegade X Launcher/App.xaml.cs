using System.Windows;

namespace LauncherTwo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void StartupApp(object sender, StartupEventArgs e)
        {
            if (LauncherTwo.Properties.Settings.Default.UpgradeRequired)
            {
                LauncherTwo.Properties.Settings.Default.Upgrade();
                LauncherTwo.Properties.Settings.Default.UpgradeRequired = false;
                LauncherTwo.Properties.Settings.Default.Save();
            }

            if (!GameInstallation.IsRootPathPlausible())
            {
                var result = MessageBox.Show("The game path seems to be incorrect. Please ensure that the launcher is placed in the correct location. If you proceed, files in the following location might be affected:\n\n" + GameInstallation.GetRootPath() + "\n\nAre you sure want to proceed?", "Invalid game path", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result != MessageBoxResult.Yes)
                {
                    Shutdown();
                    return;
                }
            }

            new MainWindow().Show();
        }
    }
}
