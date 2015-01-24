using RXPatchLib;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace LauncherTwo.Views
{
    public struct UnitAndScale
    {
        public string Unit;
        public double Scale;
        public int MaxSignificantDigits;

        public string GetFormatted(long value)
        {
            var scaledValue = value * Scale;
            var maxString = scaledValue.ToString("F" + MaxSignificantDigits);
            return maxString.Substring(0, MaxSignificantDigits+1);
        }

        public static UnitAndScale GetPreferredByteFormat(long value)
        {
            if (value < 1024)
            {
                return new UnitAndScale { Unit = "B", Scale = 1, MaxSignificantDigits = 0 };
            }
            else if (value < 1024 * 1024)
            {
                return new UnitAndScale { Unit = "KiB", Scale = 1.0 / 1024, MaxSignificantDigits = 3 };
            }
            else if (value < 1024 * 1024 * 1024)
            {
                return new UnitAndScale { Unit = "MiB", Scale = 1.0 / (1024 * 1024), MaxSignificantDigits = 3 };
            }
            else
            {
                return new UnitAndScale { Unit = "GiB", Scale = 1.0 / (1024 * 1024 * 1024), MaxSignificantDigits = 3 };
            }
        }
    }

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
            if (value as DirectoryPatchPhaseProgress == null) return "Unknown";
            var progress = (DirectoryPatchPhaseProgress)value;
            if (progress.State == DirectoryPatchPhaseProgress.States.Unstarted)
                return "...";
            else if (progress.State == DirectoryPatchPhaseProgress.States.Finished)
                return "Finished";
            else if (progress.Size.Total == 0)
                return "...";
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
                NotifyPropertyChanged("IsClosePossible");
                NotifyPropertyChanged("CloseLabel");
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
                NotifyPropertyChanged("ProgressReport");
                NotifyPropertyChanged("IsCancellationPossible");
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private CancellationTokenSource CancellationTokenSource;

        public ApplyUpdateWindow(Task patchTask, Progress<DirectoryPatcherProgressReport> progress, string targetVersionString, CancellationTokenSource cancellationTokenSource)
        {
            TargetVersionString = targetVersionString;
            CancellationTokenSource = cancellationTokenSource;
            StatusMessage = "Please wait while Renegade X is being updated.";

            InitializeComponent();

            DirectoryPatcherProgressReport lastReport = null;
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
                    StatusMessage = string.Format("Renegade X was successfully updated to version {0}.", TargetVersionString);
                }
                catch (Exception exception)
                {
                    StatusMessage = string.Format("Renegade X could not be updated. The following exception occurred:\n\n{0}", exception.Message);
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
