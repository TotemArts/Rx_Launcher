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
        static Thread UpdateThread;
        static float statePercentComplete;
        static Views.UpdateDownloadWindow UpdaterWindow = null;
        static WebClient Client;

        const string TEMP_DIRECTORY = @"C:\RxTmp\";
        const string DOWNLOAD_FILENAME = "RxLauncher_Current";
        const string DOWNLOAD_EXTENSION = ".zip";
        const string BAT_PATH = TEMP_DIRECTORY + "Install.bat";
        const string EXTRACT_DIRECTORY = TEMP_DIRECTORY + DOWNLOAD_FILENAME + @"\";
        const string SAVE_PATH = TEMP_DIRECTORY + DOWNLOAD_FILENAME + DOWNLOAD_EXTENSION;
        const string DOWNLOAD_URL = "http://www.renegade-x.com/launcher_data/" + DOWNLOAD_FILENAME + DOWNLOAD_EXTENSION;


        static eUpdateState GetUpdateState()
        {
            return UpdateState;
        }
        
        static float GetStatePercentComplete()
        {
            return statePercentComplete;
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
                //if (UpdateThread == null)
                //    UpdateThread = new Thread(new ThreadStart(StartDownload));

                //UpdateThread.Start();
                StartDownload();
            }
        }

        static void StartDownload()
        {
            UpdateState = eUpdateState.Downloading;
            UpdaterWindow.StatusLabel.Content = "Starting Download...";
            
            

            if (File.Exists(SAVE_PATH))
                File.Delete(SAVE_PATH);
            if (!Directory.Exists(TEMP_DIRECTORY))
                Directory.CreateDirectory(TEMP_DIRECTORY);


            
            Client = new WebClient ();
            Client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            Client.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(DownloadCompletedCallback);

            Uri uri = new Uri(DOWNLOAD_URL);
            Client.DownloadFileAsync(uri, SAVE_PATH);
        }

        static void StartExtract()
        {
            if (Directory.Exists(EXTRACT_DIRECTORY))
                Directory.Delete(EXTRACT_DIRECTORY, true);

            UpdateState = eUpdateState.Extracting;
            UpdaterWindow.StatusLabel.Content = "Extracting...";
            ZipFile.ExtractToDirectory(SAVE_PATH,EXTRACT_DIRECTORY);
            ReadyToInstall();
        }

        static void ReadyToInstall()
        {
            UpdateState = eUpdateState.ReadyToInstall;
            UpdaterWindow.UpdateFinished = true;
            UpdaterWindow.StatusLabel.Content = "Ready to install. Press close to install and restart.";
        }

        public static void ExecuteInstall()
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
                Contents += "xcopy \"" + EXTRACT_DIRECTORY.TrimEnd('\\') + "\" \"" + InstallLocation + "\" /v /f /e /s /r /h /y " + "\n";
                // Restart launcher
                Contents += "start \"" + InstallLocation + "\" \"" + ExecutableName + "\" \n";
                // Delete temp directory.
                Contents += "rmdir \"" + TEMP_DIRECTORY.TrimEnd('\\') + "\" /s /q \n";
                

                File.WriteAllText(BAT_PATH, Contents);


                // Execute .bat file.

                Process InstallProc = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo(BAT_PATH,"/B");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                InstallProc.StartInfo = startInfo;
                InstallProc.Start();
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
