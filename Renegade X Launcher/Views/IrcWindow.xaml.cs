using System.Windows;

namespace LauncherTwo.Views
{
    /// <summary>
    /// Interaction logic for IrcWindow.xaml
    /// </summary>
    public partial class IrcWindow : RXWindow
    {
        IrcController Controller;
        public IrcWindow(string username)
        {
            
            InitializeComponent();
            this.Controller = new IrcController("irc.CnCIRC.NET", username + "_Via_Launcher", "#renegadex");

            this.IrcChats.DataContext = this.Controller;
            this.IrcConnections.DataContext = this.Controller;
            this.Controller.Connect();

        }

        #region IRClient


        private void sd_IrcDisconnect_Click(object sender, RoutedEventArgs e)
        {
            this.Controller.Disconnect();
            this.Close();
        }
        private void sd_IrcSendMessage_Click(object sender, RoutedEventArgs e)
        {
            this.Controller.SendMsg(this.in_IrcMessageBox.Text);
            this.in_IrcMessageBox.Clear();
        }

        
        private void in_IrcMessageBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                this.Controller.SendMsg(this.in_IrcMessageBox.Text);
                this.in_IrcMessageBox.Clear();
            }
        }
        


        #endregion


    }
}
