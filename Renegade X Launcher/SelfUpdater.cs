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
    enum eUpdateState
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
        static eUpdateState UpdateState = eUpdateState.NotStarted;
        static Views.UpdateDownloadWindow UpdaterWindow = null;
        static WebClient Client;


        const string DOWNLOAD_FILENAME = "RxLauncher_Current";
        const string DOWNLOAD_EXTENSION = ".zip";
        const string DOWNLOAD_URL = "http://www.renegade-x.com/launcher_data/" + DOWNLOAD_FILENAME + DOWNLOAD_EXTENSION;


        static string GetTempDirectory()
        {
            return Path.GetTempPath() + @"\RxTmp\";
        }

        static string GetBatPath()
        {
            return GetTempDirectory() + "Install.bat";
        }

        static string GetExtractDirectory()
        {
            return GetTempDirectory() + DOWNLOAD_FILENAME + @"\";
        }

        static string GetSavePath()
        {
            return GetTempDirectory() + DOWNLOAD_FILENAME + DOWNLOAD_EXTENSION;
        }

        static eUpdateState GetUpdateState()
        {
            return UpdateState;
        }

        public static void CancelUpdate()
        {
            UpdateState = eUpdateState.Cancelled;
            // TODO: Cancel whichever state is currently happening.
        }

        public static void StartUpdate(Views.UpdateDownloadWindow aUpdaterWindow)
        {
            UpdaterWindow = aUpdaterWindow;
            if (UpdateState != eUpdateState.Downloading && UpdateState != eUpdateState.Extracting && UpdateState != eUpdateState.ReadyToInstall)
            {
                StartDownload();
            }
        }

        static void StartDownload()
        {
            try
            {
                UpdateState = eUpdateState.Downloading;
                UpdaterWindow.StatusLabel.Content = "Starting Download...";

                if (File.Exists(GetSavePath()))
                    File.Delete(GetSavePath());
                if (!Directory.Exists(GetTempDirectory()))
                    Directory.CreateDirectory(GetTempDirectory());

                Client = new WebClient();
                Client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                Client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadCompletedCallback);

                Uri uri = new Uri(DOWNLOAD_URL);
                Client.DownloadFileAsync(uri, GetSavePath());
            }
            catch (Exception e)
            {
                UpdaterWindow.StatusLabel.Content = "Error Downloading: " + e.Message;
            }
        }

        static void StartExtract()
        {
            try
            {
                if (Directory.Exists(GetExtractDirectory()))
                    Directory.Delete(GetExtractDirectory(), true);

                UpdateState = eUpdateState.Extracting;
                UpdaterWindow.StatusLabel.Content = "Extracting...";
                ZipFile.ExtractToDirectory(GetSavePath(), GetExtractDirectory());
                ReadyToInstall();
            }
            catch (Exception e)
            {
                UpdaterWindow.StatusLabel.Content = "Error Extracting: " + e.Message;
            }
        }

        static void ReadyToInstall()
        {
            UpdateState = eUpdateState.ReadyToInstall;
            UpdaterWindow.UpdateFinished = true;
            UpdaterWindow.StatusLabel.Content = "Ready to install. Press close to install and restart.";
        }

        public static void ExecuteInstall()
        {
            try
            {
                if (UpdateState == eUpdateState.ReadyToInstall)
                {
                    string InstallLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string ExecutableName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    // Write .bat file

                    string Contents = "";
                    // Give the launcher a second to exit.
                    Contents += "TIMEOUT 1 \n";
                    // Clean install location
                    Contents += "rmdir \"" + InstallLocation + "\" /s /q \n";
                    // Copy extracted files and overwrite existing launcher files.
                    Contents += "xcopy \"" + GetExtractDirectory().TrimEnd('\\') + "\" \"" + InstallLocation + "\" /v /f /e /s /r /h /y " + "\n";
                    // Restart launcher
                    Contents += "start \"" + InstallLocation + "\" \"" + ExecutableName + "\" \n";
                    // Delete temp directory.
                    Contents += "rmdir \"" + GetTempDirectory().TrimEnd('\\') + "\" /s /q \n";

                    File.WriteAllText(GetBatPath(), Contents);

                    // Execute .bat file.

                    Process InstallProc = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo(GetBatPath(), "/B");
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    InstallProc.StartInfo = startInfo;
                    InstallProc.Start();
                }
            }
            catch (Exception e)
            {
                UpdaterWindow.StatusLabel.Content = "Error Installing: " + e.Message;
            }
        }

        private static void DownloadCompletedCallback(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                UpdateState = eUpdateState.Error;
                UpdaterWindow.StatusLabel.Content = "Download was interrupted";
            }
            else if (e.Error != null)
            {
                UpdateState = eUpdateState.Error;
                UpdaterWindow.StatusLabel.Content = e.Error.Message;
            }
            else
            {
                UpdaterWindow.StatusLabel.Content = "Download Finished";
                StartExtract();
            }
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            //UpdaterWindow.StatusLabel.Content = (string)e.UserState + "    downloaded " + e.BytesReceived + " of " + e.TotalBytesToReceive + " bytes. " + e.ProgressPercentage + "% complete...";
            UpdaterWindow.StatusLabel.Content = "Downloading...";

            double DownloadedInMB = (double)(e.BytesReceived / 1024) / 1024;
            DownloadedInMB = Math.Round(DownloadedInMB, 2);

            if (e.TotalBytesToReceive > 0.0f)
                UpdaterWindow.StatusLabel.Content += " " + (e.ProgressPercentage) + "%";
            else
                UpdaterWindow.StatusLabel.Content += " " + DownloadedInMB.ToString() + "mb";
        }

    }
}
