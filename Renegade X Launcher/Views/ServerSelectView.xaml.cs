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
    /// Interaction logic for ServerSelectView.xaml
    /// </summary>
    public partial class ServerSelectView : Window
    {
        public ServerSelectView(ICollection<string> hosts)
        {
            InitializeComponent();
            this.CB_Servers.DataContext = hosts;
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            object x = this.CB_Servers.SelectedValue;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
