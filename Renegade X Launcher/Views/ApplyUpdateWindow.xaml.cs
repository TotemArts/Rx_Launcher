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
                //var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                //return string.Format("{0} / ~{1} {2}", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit);
                double perc = ((double)progress.Size.Done / (double)progress.Size.Total) * 100.00; ;
                return string.Format("{0}%", perc.ToString("0.##"));
            }
            else
            {
                //var unitAndScale = UnitAndScale.GetPreferredByteFormat(progress.Size.Total);
                //return string.Format("{0} / {1} {2}", unitAndScale.GetFormatted(progress.Size.Done), unitAndScale.GetFormatted(progress.Size.Total), unitAndScale.Unit);
                double perc = ((double)progress.Size.Done / (double)progress.Size.Total) * 100.00; ;
                return string.Format("{0}%", perc.ToString("0.##"));
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
        private string _ServerMessage;
        public string ServerMessage
        {
            get
            {
                return _ServerMessage;
            }
            private set
            {
                _ServerMessage = value;
                NotifyPropertyChanged("ServerMessage");
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
            Reset,
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
        
        
        public ApplyUpdateWindow(Task patchTask, RXPatcher patcher, Progress<DirectoryPatcherProgressReport> progress, string targetVersionString, CancellationTokenSource cancellationTokenSource, UpdateWindowType type)
        {
            TargetVersionString = targetVersionString;
            CancellationTokenSource = cancellationTokenSource;
            string[] StatusTitle = new string[]{"updated", "update"};

            Dictionary<UpdateWindowType, string[]> StatusTitleDict = new Dictionary<UpdateWindowType, string[]>()
            {
                {UpdateWindowType.Install, new string[]{"installed", "installation" } },
                {UpdateWindowType.Update, new string[]{"updated", "update" } },
                {UpdateWindowType.Verify, new string[]{"verified", "verification" } },
                {UpdateWindowType.Reset, new string[]{"modified", "modification" } }
            };

            StatusTitleDict.TryGetValue(type, out StatusTitle);

            this.StatusMessage = string.Format("Please wait while Renegade X is being {0}.", StatusTitle[0]);

            if (string.IsNullOrEmpty(patcher.BaseURL))
                this.ServerMessage = "pending";
            else
                this.ServerMessage = patcher.BaseURL;

            InitializeComponent();
            this.Title = string.Format("Renegade X {0} ", StatusTitle[1]);

            DirectoryPatcherProgressReport lastReport = new DirectoryPatcherProgressReport();
            progress.ProgressChanged += (o, report) => lastReport = report;

            Task backgroundTask = Task.Factory.StartNew(async () =>
            {
                while (await Task.WhenAny(patchTask, Task.Delay(500)) != patchTask)
                {
                    // URL could theoretically change at any point
                    if (this.ServerMessage != patcher.BaseURL)
                    {
                        if (patcher.BaseURL == null)
                            this.ServerMessage = "pending";
                        else
                            this.ServerMessage = patcher.BaseURL;
                    }

                    ProgressReport = lastReport;
                }
                ProgressReport = lastReport;

                try
                {
                    await patchTask; // Collect exceptions.
                    this.StatusMessage = string.Format("Renegade X was successfully {0} to version {1}.", StatusTitle[0], TargetVersionString);
                    
                }
                catch (Exception exception)
                {
                    StatusMessage = string.Format("Renegade X could not be {0}. The following exception occurred:\n\n{1}", StatusTitle[0], exception.Message);
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
