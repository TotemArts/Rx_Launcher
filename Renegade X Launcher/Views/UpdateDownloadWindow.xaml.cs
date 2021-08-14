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
        private string PatchUrl;

        public UpdateDownloadWindow(string patchUrl)
        {
            PatchUrl = patchUrl;
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }

        private void OnActivated(object sender, EventArgs e)
        {
            SelfUpdater.StartUpdate(this, PatchUrl);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}
