﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LauncherTwo.Views
{
    /// <summary>
    /// Interaction logic for UsernameWindow.xaml
    /// </summary>
    public partial class UpdateDownloadWindow : Window
    {
        public bool UpdateFinished = false;
        private readonly string _patchUrl;
        private readonly string _patchHash;

        public UpdateDownloadWindow(string patchUrl, string patchHash)
        {
            _patchUrl = patchUrl;
            _patchHash = patchHash;
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            SelfUpdater.StartUpdate(this, _patchUrl, _patchHash);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
