using RXPatchLib;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LauncherTwo.Views
{
    [ValueConversion(typeof(DirectoryPatchPhaseProgress), typeof(bool))]
    public class PhaseIsIndeterminateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value as DirectoryPatchPhaseProgress == null) return DependencyProperty.UnsetValue;
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
            if (value as DirectoryPatchPhaseProgress == null) return DependencyProperty.UnsetValue;
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
                var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                return string.Format("{0} / ~{1} {2}", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit);
            }
            else
            {
                var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                return string.Format("{0} / {1} {2}", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class DirectoryPatchPhaseProgressWithSpeed
    {
        private DirectoryPatchPhaseProgress _ProgressReport;
        private SpeedComputer _SpeedComputer = new SpeedComputer();

        public long BytesPerSecond
        {
            get
            {
                return _SpeedComputer.BytesPerSecond;
            }
        }

        public DirectoryPatchPhaseProgress ProgressReport
        {
            get
            {
                return _ProgressReport;
            }
            set
            {
                _ProgressReport = value;
                _SpeedComputer.AddSample(_ProgressReport.Size.Done);
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
                return string.Format("{0} / ~{1} {2} ({3} {4}/s)", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit, speedUnitAndScale.GetFormatted(progressWithSpeed.BytesPerSecond), speedUnitAndScale.Unit);
            }
            else
            {
                var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                var speedUnitAndScale = UnitAndScale.GetPreferredByteFormat(progressWithSpeed.BytesPerSecond);
                return string.Format("{0} / {1} {2} ({3} {4}/s)", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit, speedUnitAndScale.GetFormatted(progressWithSpeed.BytesPerSecond), speedUnitAndScale.Unit);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public partial class ApplyUpdateWindow : RXWindow, INotifyPropertyChanged
    {
        private bool _HasFinished;
        public bool HasFinished
        {
            get
            {
                return _HasFinished;
            }
            private set
            {
                _HasFinished = value;
                NotifyPropertyChanged("HasFinished");
            }
        }
        public bool IsCancellationPossible
        {
            get
            {
                return _ProgressReport != null ? _ProgressReport.IsCancellationPossible : false;
            }
        }
        private string _StatusMessage;
        public string StatusMessage
        {
            get
            {
                return _StatusMessage;
            }
            private set
            {
                _StatusMessage = value;
                NotifyPropertyChanged("StatusMessage");
            }
        }
        public string _TargetVersionString;
        public string TargetVersionString
        {
            get
            {
                return _TargetVersionString;
            }
            private set
            {
                _TargetVersionString = value;
                NotifyPropertyChanged("TargetVersionString");
            }
        }

        public DirectoryPatcherProgressReport _ProgressReport;
        public DirectoryPatcherProgressReport ProgressReport
        {
            get
            {
                return _ProgressReport;
            }
            private set
            {
                _ProgressReport = value;
                LoadProgressWithSpeed.ProgressReport = _ProgressReport.Load;
                NotifyPropertyChanged("ProgressReport");
                NotifyPropertyChanged("LoadProgressWithSpeed");
                NotifyPropertyChanged("IsCancellationPossible");
            }
        }

        private DirectoryPatchPhaseProgressWithSpeed _LoadProgressWithSpeed = new DirectoryPatchPhaseProgressWithSpeed();
        public DirectoryPatchPhaseProgressWithSpeed LoadProgressWithSpeed
        {
            get
            {
                return _LoadProgressWithSpeed;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private CancellationTokenSource CancellationTokenSource;

        public enum UpdateWindowType
        {
            Install,
            Update,
            Verify
        }

        /// <summary>
        /// Initializes the updatewindow
        /// </summary>
        /// <param name="patchTask">The update task</param>
        /// <param name="progress"></param>
        /// <param name="targetVersionString">The version to update to</param>
        /// <param name="cancellationTokenSource">Cancellationsource for the updatetask</param>
        /// <param name="isInstall">Is this the first install</param>
        public ApplyUpdateWindow(Task patchTask, Progress<DirectoryPatcherProgressReport> progress, string targetVersionString, CancellationTokenSource cancellationTokenSource, UpdateWindowType type)
        {
            TargetVersionString = targetVersionString;
            CancellationTokenSource = cancellationTokenSource;
            String Status = "updated";

            switch (type)
            {
                case UpdateWindowType.Install:
                    Status = "installed";
                    break;
                case UpdateWindowType.Update:
                    Status = "updated";
                    break;
                case UpdateWindowType.Verify:
                    Status = "verified";
                    break;
                default:
                    Status = "updated";
                    break;
            }

            this.StatusMessage = string.Format("Please wait while Renegade X is being {0} .", Status);
            

            InitializeComponent();

            DirectoryPatcherProgressReport lastReport = new DirectoryPatcherProgressReport();
            progress.ProgressChanged += (o, report) => lastReport = report;

            Task backgroundTask = Task.Factory.StartNew(async () =>
            {
                while (await Task.WhenAny(patchTask, Task.Delay(500)) != patchTask)
                {
                    ProgressReport = lastReport;
                }
                ProgressReport = lastReport;

                try
                {
                    await patchTask; // Collect exceptions.
                    this.StatusMessage = string.Format("Renegade X was successfully {0} to version {1}.", Status, TargetVersionString);
                    
                }
                catch (Exception exception)
                {
                    StatusMessage = string.Format("Renegade X could not be {0}. The following exception occurred:\n\n{1}", Status, exception.Message);
                }
                HasFinished = true;
            });
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
            CancellationTokenSource.Cancel();
            this.StatusMessage = "Operation cancelled by User";
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
