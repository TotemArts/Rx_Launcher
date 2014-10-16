using System;
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
    public partial class UsernameWindow : Window
    {
        public string Username { get; set; }

        public UsernameWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (Username.Contains("\"") || Username.Contains("#"))
            {
                MessageBox.Show("Your name may not contain any of the following characters: \" #", "Invalid name", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                DialogResult = true;
            }
        }
    }
}
