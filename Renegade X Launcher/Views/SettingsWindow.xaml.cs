using RXPatchLib;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LauncherTwo.Views
{
    public class Settings : INotifyPropertyChanged
    {
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
    }
}
