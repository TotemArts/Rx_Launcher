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

    public partial class GeneralDownloadWindow : RxWindow
    {
        
        private readonly CancellationTokenSource _token;
        private long _sizeOfFile;

        internal string Status
        {
            get { return StatusLabelContent.Content.ToString(); }
            set { Dispatcher.Invoke(new Action(() => { StatusLabelContent.Content = value; })); }
        }

        
        public GeneralDownloadWindow(CancellationTokenSource token, String windowTitle)
        {
            InitializeComponent();
            this._token = token;
            this.Title = windowTitle;
        }

        public void SetStatusLabel(String status)
        {
            this.StatusLabelContent.Content = status;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this._token.Cancel();
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

        public void InitProgressBar(long sizeOfFile)
        {
            this._sizeOfFile = sizeOfFile;

            Dispatcher.Invoke(new Action(() => {
                this.ProgressPercentage.Content = "0%";
                this.ProgressBar.Maximum = sizeOfFile;
            }));
        }

        public void UpdateProgressBar(long currentAmount)
        {
            Dispatcher.Invoke(new Action(() => {
                this.ProgressBar.Value = currentAmount; 
                if (this._sizeOfFile != 0)
                {
                    this.ProgressPercentage.Content = (int)currentAmount / (this._sizeOfFile / 100) + "%";
                }
                else
                {
                    this.ProgressPercentage.Content = "0%";
                }
            }));
        }

        
    }
}
