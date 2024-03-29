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

        #region UseSeeker Setting
        private bool _UseSeeker;
        public bool UseSeeker
        {
            get
            {
                return _UseSeeker;
            }
            set
            {
                _UseSeeker = value;
                NotifyPropertyChanged("UseSeeker");
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
                UseSeeker = Properties.Settings.Default.UseSeeker,
            };
            InitializeComponent();
        }

        public void ApplyAndClose(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SkipIntroMovies = Settings.SkipIntroMovies;
            Properties.Settings.Default.UseSeeker = Settings.UseSeeker;
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
            var patchUrls = VersionCheck.GamePatchUrls;
            var patchVersion = VersionCheck.GetLatestGameVersionName();

            var progress = new Progress<DirectoryPatcherProgressReport>();
            var cancellationTokenSource = new System.Threading.CancellationTokenSource();
            Task task = new RXPatcher().ApplyPatchFromWeb(patchUrls, targetDir, applicationDir, progress, cancellationTokenSource.Token);

            var window = new ApplyUpdateWindow(task, progress, patchVersion, cancellationTokenSource, type);
            window.Owner = this;
            window.ShowDialog();

            VersionCheck.UpdateGameVersion();
        }
    }
}
