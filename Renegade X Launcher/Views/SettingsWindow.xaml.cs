using RXPatchLib;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LauncherTwo.Views
{
    public class Settings : INotifyPropertyChanged
    {
        #region SkipIntroMovies Setting
        private bool _skipIntroMovies;
        public bool SkipIntroMovies
        {
            get
            {
                return _skipIntroMovies;
            }
            set
            {
                _skipIntroMovies = value;
                NotifyPropertyChanged("SkipIntroMovies");
            }
        }
        #endregion

        #region Use64Bit Setting
        private bool _use64Bit;
        public bool Use64Bit
        {
            get
            {
                return _use64Bit;
            }
            set
            {
                _use64Bit = value;
                NotifyPropertyChanged("Use64Bit");
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }


    public partial class SettingsWindow : RxWindow
    {
        public Settings Settings { get; set; }

        const String ResetMessage = "Are you sure you want to reset Renegade X?\nThis will revert all settings to their default.";

        public SettingsWindow()
        {
            Settings = new Settings
            {
                SkipIntroMovies = Properties.Settings.Default.SkipIntroMovies,
                Use64Bit = Properties.Settings.Default.Binaries64,
            };
            InitializeComponent();
        }

        public void ApplyAndClose(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SkipIntroMovies = Settings.SkipIntroMovies;
            Properties.Settings.Default.Binaries64 = Settings.Use64Bit;
            Properties.Settings.Default.Save();
            Close();
        }

        public void Cancel(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void Verify_Click(object sender, RoutedEventArgs e)
        {
            this.ApplyResetOrVerify(ApplyUpdateWindow.UpdateWindowType.Verify);
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult rsltMessageBox = MessageBox.Show(ResetMessage, "Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                String configFolder = System.IO.Path.Combine(GameInstallation.GetRootPath(), "UDKGame\\Config");
                System.IO.Directory.Delete(configFolder, true);
                this.ApplyResetOrVerify(ApplyUpdateWindow.UpdateWindowType.Reset);
            }
        }

        private void ApplyResetOrVerify(ApplyUpdateWindow.UpdateWindowType type)
        {
            var targetDir = GameInstallation.GetRootPath();
            var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
            var patchPath = VersionCheck.GamePatchPath;
            var patchUrls = VersionCheck.GamePatchUrls;
            var patchVersion = VersionCheck.GetLatestGameVersionName();

            var progress = new Progress<DirectoryPatcherProgressReport>();
            var cancellationTokenSource = new System.Threading.CancellationTokenSource();

            Task task = RxPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir, progress, cancellationTokenSource.Token, VersionCheck.InstructionsHash);

            var window = new ApplyUpdateWindow(task, RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, type);
            window.Owner = this;
            window.ShowDialog();

            VersionCheck.UpdateGameVersion();
        }
    }
}
