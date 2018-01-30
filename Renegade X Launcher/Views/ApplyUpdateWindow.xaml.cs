using RXPatchLib;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Collections.Generic;
using System.Windows.Controls;
using FirstFloor.ModernUI.Windows.Controls;

namespace LauncherTwo.Views
{
    [ValueConversion(typeof(DirectoryPatchPhaseProgress), typeof(bool))]
    public class PhaseIsIndeterminateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DirectoryPatchPhaseProgress)) return DependencyProperty.UnsetValue;
            var progress = (DirectoryPatchPhaseProgress)value;
            return progress.State == DirectoryPatchPhaseProgress.States.Indeterminate;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    [ValueConversion(typeof(DirectoryPatchPhaseProgress), typeof(double))]
    public class PhaseProgressPercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DirectoryPatchPhaseProgress)) return DependencyProperty.UnsetValue;
            var progress = (DirectoryPatchPhaseProgress)value;

            if (progress.State == DirectoryPatchPhaseProgress.States.Unstarted)
                return 0;
            else if (progress.State == DirectoryPatchPhaseProgress.States.Indeterminate)
                return DependencyProperty.UnsetValue;
            else if (progress.State == DirectoryPatchPhaseProgress.States.Finished)
                return 100;
            else if (progress.Size.Total == 0)
                return 0;
            else
                return progress.Size.Fraction * 100.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    [ValueConversion(typeof(DirectoryPatchPhaseProgress), typeof(string))]
    public class PhaseProgressStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var progress = value as DirectoryPatchPhaseProgress;
            if (progress == null)
                return "unknown";

            if (progress.State == DirectoryPatchPhaseProgress.States.Unstarted)
                return "not started";
            else if (progress.State == DirectoryPatchPhaseProgress.States.Finished)
                return "finished";
            else if (progress.Size.Total == 0)
                return "pending";
            else if (progress.State == DirectoryPatchPhaseProgress.States.Indeterminate)
            {
                double perc = ((double)progress.Size.Done / (double)progress.Size.Total) * 100.00; ;
                return $"{perc:0.##}%";
            }
            else
            {
                double perc = ((double)progress.Size.Done / (double)progress.Size.Total) * 100.00; ;
                return $"{perc:0.##}%";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class DirectoryPatchPhaseProgressWithSpeed
    {
        private DirectoryPatchPhaseProgress _progressReport;
        private readonly SpeedComputer _speedComputer = new SpeedComputer();

        public long BytesPerSecond
        {
            get
            {
                return _speedComputer.BytesPerSecond;
            }
        }

        public DirectoryPatchPhaseProgress ProgressReport
        {
            get
            {
                return _progressReport;
            }
            set
            {
                _progressReport = value;
                _speedComputer.AddSample(_progressReport.Size.Done);
            }
        }
    }

    [ValueConversion(typeof(DirectoryPatchPhaseProgress), typeof(string))]
    public class PhaseProgressWithSpeedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var progressWithSpeed = value as DirectoryPatchPhaseProgressWithSpeed;
            if (progressWithSpeed == null)
                return "unknown";
            var progress = progressWithSpeed.ProgressReport;
            if (progress == null)
                return "unknown";
            if (progress.State == DirectoryPatchPhaseProgress.States.Unstarted)
                return "not started";
            else if (progress.State == DirectoryPatchPhaseProgress.States.Finished)
                return "finished";
            else if (progress.Size.Total == 0)
                return "pending";
            else if (progress.State == DirectoryPatchPhaseProgress.States.Indeterminate)
            {
                var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                var speedUnitAndScale = UnitAndScale.GetPreferredByteFormat(progressWithSpeed.BytesPerSecond);
                double perc = (progress.Size.Done / (double)progress.Size.Total) * 100.00;
                return string.Format("{0} / ~{1} {2} ({3} {4}/s - {5:#.##}%)", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit, speedUnitAndScale.GetFormatted(progressWithSpeed.BytesPerSecond), speedUnitAndScale.Unit, perc);
            }
            else
            {
                var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                var speedUnitAndScale = UnitAndScale.GetPreferredByteFormat(progressWithSpeed.BytesPerSecond);
                double perc = (progress.Size.Done / (double)progress.Size.Total) * 100.00;
                return string.Format("{0} / {1} {2} ({3} {4}/s - {5:#.##}%)", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit, speedUnitAndScale.GetFormatted(progressWithSpeed.BytesPerSecond), speedUnitAndScale.Unit, perc);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public partial class ApplyUpdateWindow : RxWindow, INotifyPropertyChanged
    {
        private bool _hasFinished;
        public bool HasFinished
        {
            get
            {
                return _hasFinished;
            }
            private set
            {
                _hasFinished = value;
                NotifyPropertyChanged("HasFinished");
            }
        }
        public bool IsCancellationPossible
        {
            get
            {
                return _progressReport != null ? _progressReport.IsCancellationPossible : false;
            }
        }
        private string _statusMessage;
        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            private set
            {
                _statusMessage = value;
                NotifyPropertyChanged("StatusMessage");
            }
        }
        private string _serverMessage;
        public string ServerMessage
        {
            get
            {
                return _serverMessage;
            }
            private set
            {
                _serverMessage = value;
                NotifyPropertyChanged("ServerMessage");
            }
        }
        private string _targetVersionString;
        public string TargetVersionString
        {
            get
            {
                return _targetVersionString;
            }
            private set
            {
                _targetVersionString = value;
                NotifyPropertyChanged("TargetVersionString");
            }
        }

        private DirectoryPatcherProgressReport _progressReport;
        public DirectoryPatcherProgressReport ProgressReport
        {
            get
            {
                return _progressReport;
            }
            private set
            {
                _progressReport = value;
                LoadProgressWithSpeed.ProgressReport = _progressReport.Load;
                NotifyPropertyChanged("ProgressReport");
                NotifyPropertyChanged("LoadProgressWithSpeed");
                NotifyPropertyChanged("IsCancellationPossible");
            }
        }

        private readonly DirectoryPatchPhaseProgressWithSpeed _loadProgressWithSpeed = new DirectoryPatchPhaseProgressWithSpeed();
        public DirectoryPatchPhaseProgressWithSpeed LoadProgressWithSpeed
        {
            get
            {
                return _loadProgressWithSpeed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly CancellationTokenSource _cancellationTokenSource;

        public enum UpdateWindowType
        {
            Install,
            Update,
            Reset,
            Verify
        }

        /// <summary>
        /// Initializes the updatewindow
        /// </summary>
        /// <param name="patchTask">The update task</param>
        /// <param name="patcher"></param>
        /// <param name="progress"></param>
        /// <param name="targetVersionString">The version to update to</param>
        /// <param name="cancellationTokenSource">Cancellationsource for the updatetask</param>
        /// <param name="isInstall">Is this the first install</param>
        public ApplyUpdateWindow(Task patchTask, RxPatcher patcher, Progress<DirectoryPatcherProgressReport> progress, string targetVersionString, CancellationTokenSource cancellationTokenSource, UpdateWindowType type)
        {
            TargetVersionString = targetVersionString;
            _cancellationTokenSource = cancellationTokenSource;
            string[] statusTitle = new string[]{"updated", "update"};

            Dictionary<UpdateWindowType, string[]> statusTitleDict = new Dictionary<UpdateWindowType, string[]>()
            {
                {UpdateWindowType.Install, new string[]{"installed", "installation" } },
                {UpdateWindowType.Update, new string[]{"updated", "update" } },
                {UpdateWindowType.Verify, new string[]{"verified", "verification" } },
                {UpdateWindowType.Reset, new string[]{"modified", "modification" } }
            };

            statusTitleDict.TryGetValue(type, out statusTitle);

            this.StatusMessage = string.Format("Please wait while Renegade X is being {0}.", statusTitle[0]);

            if (patcher.UpdateServer == null)
                this.ServerMessage = "pending";
            else
                this.ServerMessage = patcher.UpdateServer.Name;

            InitializeComponent();
            this.Title = string.Format("Renegade X {0} ", statusTitle[1]);

            DirectoryPatcherProgressReport lastReport = new DirectoryPatcherProgressReport();
            progress.ProgressChanged += (o, report) => lastReport = report;

            // Here we start the actual patching process, the whole thing from verification to applying.
            Task backgroundTask = Task.Factory.StartNew(async () =>
            {
                while (await Task.WhenAny(patchTask, Task.Delay(500)) != patchTask)
                {
                    if ( _cancellationTokenSource.IsCancellationRequested )
                        throw new OperationCanceledException();
                    ProgressReport = lastReport;
                }
                ProgressReport = lastReport;

                try
                {
                    await patchTask; // Collect exceptions.
                    this.StatusMessage = string.Format("Renegade X was successfully {0} to version {1}.", statusTitle[0], TargetVersionString);
                    RxLogger.Logger.Instance.Write($"Renegade X was successfully {statusTitle[0]} to version {TargetVersionString}.");
                }
                catch (Exception exception)
                {
                    StatusMessage = string.Format("Renegade X could not be {0}. The following exception occurred:\n\n{1}", statusTitle[0], exception.Message);
                    RxLogger.Logger.Instance.Write($"Renegade X could not be {statusTitle[0]}. The following exception occurred:\n\n{exception.Message}\r\n{exception.StackTrace}");
                }
                HasFinished = true;
            }, _cancellationTokenSource.Token);
        }

        public void This_Closing(object sender, CancelEventArgs e)
        {
            if (!HasFinished)
            {
                e.Cancel = true;
            }
        }

        public void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ModernDialog areYouSureDialog = new ModernDialog();
            areYouSureDialog.Title = "Stop Download - Renegade X";
            areYouSureDialog.Content = "Are you sure you want to stop this download?\r\nYou can come back to it later.";
            areYouSureDialog.Buttons = new Button[] { areYouSureDialog.OkButton, areYouSureDialog.CancelButton };
            areYouSureDialog.ShowDialog();

            if (areYouSureDialog.DialogResult.Value == true)
            {
                _cancellationTokenSource.Cancel();
                this.StatusMessage = "Operation cancelled by User";
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Buttonhandler for pause command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            //Todo-> Agents miraculous pause code that's needed in RXPatch "Help"
        }
    }
}
