using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;

namespace LauncherTwo
{
    enum EUpdateState
    {
        NotStarted = 0,
        Downloading,
        Extracting,
        ReadyToInstall,
        Done,
        Cancelled,
        Error
    }
    public static class SelfUpdater
    {
        static EUpdateState _updateState = EUpdateState.NotStarted;
        static Views.UpdateDownloadWindow _updaterWindow = null;
        static WebClient _client;
        static string _patchHash;

        static string GetTempDirectory()
        {
            return Path.GetTempPath() + @"\RxTmp\";
        }

        static string GetUpdaterPath()
        {
            return GetExtractDirectory() + @"SelfUpdateExecutor.exe";
        }

        static string GetExtractDirectory()
        {
            return GetTempDirectory() + @"launcher_update_extracted\";
        }

        static string GetSavePath()
        {
            return GetTempDirectory() + @"launcher_update.zip";
        }

        static EUpdateState GetUpdateState()
        {
            return _updateState;
        }

        public static void CancelUpdate()
        {
            _updateState = EUpdateState.Cancelled;
            // TODO: Cancel whichever state is currently happening.
        }

        public static void StartUpdate(Views.UpdateDownloadWindow aUpdaterWindow, string url, string hash)
        {
            _updaterWindow = aUpdaterWindow;
            _patchHash = hash;
            if (_updateState != EUpdateState.Downloading && _updateState != EUpdateState.Extracting && _updateState != EUpdateState.ReadyToInstall)
            {
                StartDownload(url);
            }
        }

        static void StartDownload(string url)
        {
            try
            {
                _updateState = EUpdateState.Downloading;
                _updaterWindow.StatusLabel.Content = "Starting Download...";

                if (File.Exists(GetSavePath()))
                    File.Delete(GetSavePath());
                if (!Directory.Exists(GetTempDirectory()))
                    Directory.CreateDirectory(GetTempDirectory());

                _client = new WebClient();
                _client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                _client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadCompletedCallback);

                Uri uri = new Uri(url);
                _client.DownloadFileAsync(uri, GetSavePath());
            }
            catch (Exception e)
            {
                _updaterWindow.StatusLabel.Content = "Error Downloading: " + e.Message;
            }
        }

        static void StartExtract()
        {
            try
            {
                if (Directory.Exists(GetExtractDirectory()))
                    Directory.Delete(GetExtractDirectory(), true);

                _updateState = EUpdateState.Extracting;
                _updaterWindow.StatusLabel.Content = "Extracting...";
                ZipFile.ExtractToDirectory(GetSavePath(), GetExtractDirectory());
                ReadyToInstall();
            }
            catch (Exception e)
            {
                _updaterWindow.StatusLabel.Content = "Error Extracting: " + e.Message;
            }
        }

        static void ReadyToInstall()
        {
            _updateState = EUpdateState.ReadyToInstall;
            _updaterWindow.UpdateFinished = true;
            _updaterWindow.StatusLabel.Content = "Ready to install. Press close to install and restart.";
        }

        public static void ExecuteInstall()
        {
            try
            {
                if (_updateState == EUpdateState.ReadyToInstall)
                {
                    string installLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string pidString = Process.GetCurrentProcess().Id.ToString();

                    // Build ProcessStartInfo
                    ProcessStartInfo startInfo = new ProcessStartInfo(GetUpdaterPath(), "\"--target=" + installLocation + "\" --pid=" + pidString);
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.WorkingDirectory = Path.GetTempPath();
                    startInfo.Verb = "runas";

                    // Build Process
                    Process process = new Process();
                    process.StartInfo = startInfo;

                    // Start process
                    process.Start();
                }
            }
            catch (Exception e)
            {
                _updaterWindow.StatusLabel.Content = "Error Installing: " + e.Message;
            }
        }

        private static void DownloadCompletedCallback(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _updateState = EUpdateState.Error;
                _updaterWindow.StatusLabel.Content = "Download was interrupted";
            }
            else if (e.Error != null)
            {
                _updateState = EUpdateState.Error;
                _updaterWindow.StatusLabel.Content = e.Error.Message;
            }
            else
            {
                _updaterWindow.StatusLabel.Content = "Download Finished; verifying...";

                // Verify the hash of the download
                if (_patchHash == "" || _patchHash == RXPatchLib.Sha256.GetFileHash(GetSavePath()))
                {
                    // Download valid; begin extraction
                    StartExtract();
                }
                else
                {
                    // Hash mismatch; set an error
                    _updateState = EUpdateState.Error;
                    _updaterWindow.StatusLabel.Content = "Hash mismatch";
                }
            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            //UpdaterWindow.StatusLabel.Content = (string)e.UserState + "    downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive + " bytes. " + e.ProgressPercentage + "% complete...";
            _updaterWindow.StatusLabel.Content = "Downloading...";

            double downloadedInMb = (double)(e.BytesReceived / 1024) / 1024;
            downloadedInMb = Math.Round(downloadedInMb, 2);

            if (e.TotalBytesToReceive > 0.0f)
                _updaterWindow.StatusLabel.Content += " " + (e.ProgressPercentage) + "%";
            else
                _updaterWindow.StatusLabel.Content += " " + downloadedInMb.ToString() + "mb";
        }

    }
}
