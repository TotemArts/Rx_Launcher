using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

    public partial class GeneralDownloadWindow : RXWindow
    {
        
        private CancellationTokenSource token;
        private long sizeOfFile;

        internal string Status
        {
            get { return StatusLabelContent.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { StatusLabelContent.Content = value; })); }
        }

        
        public GeneralDownloadWindow(CancellationTokenSource token, String windowTitle)
        {
            InitializeComponent();
            this.token = token;
            this.Title = windowTitle;
        }

        public void setStatusLabel(String status)
        {
            this.StatusLabelContent.Content = status;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.token.Cancel();
        }

        public void ToggleProgressBar()
        {
            switch (ProgressBar.Visibility)
            {
                case Visibility.Collapsed:
                    Dispatcher.Invoke(new Action(() => { ProgressBar.Visibility = Visibility.Visible; }));
                    break;

                case Visibility.Visible:
                    Dispatcher.Invoke(new Action(() => { ProgressBar.Visibility = Visibility.Collapsed; }));
                    break;
            }
           
        }

        public void ToggleContinueButton()
        {
            switch (ContinueBtn.Visibility)
            {
                case Visibility.Collapsed:
                    Dispatcher.Invoke(new Action(() => { ContinueBtn.Visibility = Visibility.Visible; }));
                    break;

                case Visibility.Visible:
                    Dispatcher.Invoke(new Action(() => { ContinueBtn.Visibility = Visibility.Collapsed; }));
                    break;
            }
        }

        public void initProgressBar(long sizeOfFile)
        {
            this.sizeOfFile = sizeOfFile;
            Dispatcher.Invoke(new Action(() => {
                this.ProgressPercentage.Content = "0%";
                this.ProgressBar.Maximum = sizeOfFile;
            }));
        }

        public void updateProgressBar(long currentAmount)
        {
            Dispatcher.Invoke(new Action(() => {
                this.ProgressBar.Value = currentAmount; 
                if (this.sizeOfFile != 0)
                {
                    this.ProgressPercentage.Content = (int)currentAmount / (this.sizeOfFile / 100) + "%";
                }
                else
                {
                    this.ProgressPercentage.Content = "0%";
                }
            }));
        }

        
    }
}
