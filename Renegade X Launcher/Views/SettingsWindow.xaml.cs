﻿using RXPatchLib;
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
        private bool _SkipIntroMovies;
        public bool SkipIntroMovies
        {
            get
            {
                return _SkipIntroMovies;
            }
            set
            {
                _SkipIntroMovies = value;
                NotifyPropertyChanged("SkipIntroMovies");
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


    public partial class SettingsWindow : RXWindow
    {
        public Settings Settings { get; set; }

        const String RESET_MESSAGE = "Are you sure you want to reset Renegade X?\nThis will revert all settings to their default.";

        public SettingsWindow()
        {
            Settings = new Settings
            {
                SkipIntroMovies = Properties.Settings.Default.SkipIntroMovies,
            };
            InitializeComponent();
        }

        public void ApplyAndClose(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SkipIntroMovies = Settings.SkipIntroMovies;
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
            MessageBoxResult rsltMessageBox = MessageBox.Show(RESET_MESSAGE, "Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
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

            Task task = RXPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir, progress, cancellationTokenSource.Token, VersionCheck.InstructionsHash);

            var window = new ApplyUpdateWindow(task, RXPatcher.Instance, progress, patchVersion, cancellationTokenSource, type);
            window.Owner = this;
            window.ShowDialog();

            VersionCheck.UpdateGameVersion();
        }
    }
}
