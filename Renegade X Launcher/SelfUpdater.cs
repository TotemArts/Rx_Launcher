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
        static string PatchHash;

        static string GetTempDirectory()
        {
            return Path.GetTempPath() + @"\RxTmp\";
        }

        static string GetBatPath()
        {
            return GetTempDirectory() + "install.bat";
        }

        static string GetExtractDirectory()
        {
            return GetTempDirectory() + @"launcher_update_extracted\";
        }

        static string GetSavePath()
        {
            return GetTempDirectory() + @"launcher_update.zip";
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

        public static void StartUpdate(Views.UpdateDownloadWindow aUpdaterWindow, string url, string hash)
        {
            UpdaterWindow = aUpdaterWindow;
            PatchHash = hash;
            if (UpdateState != eUpdateState.Downloading && UpdateState != eUpdateState.Extracting && UpdateState != eUpdateState.ReadyToInstall)
            {
                StartDownload(url);
            }
        }

        static void StartDownload(string url)
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

                Uri uri = new Uri(url);
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
                if (UpdateState == eUpdateState.ReadyToInstall || true)
                {
                    string installLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    string executableName = System.Reflection.Assembly.GetExecutingAssembly().Location;

                    string pidString = Process.GetCurrentProcess().Id.ToString();
                    string contents = string.Join("\r\n", new string[]
                    {
                        "cd /D \"" + Path.GetTempPath() + "\"",

                        // Wait for the launcher to close.
                        ":wait_for_close",
                        "tasklist /FI \"PID eq " + pidString + "\" /FO csv /NH | find \"\"\"" + pidString + "\"\"\" > nul",
                        "if not errorlevel 1 (",
                        "    timeout /t 1 > nul",
                        "    goto :wait_for_close",
                        ")",

                        // Clean up possible left behind files from previous installation attempt. (If it fails, abort update.)
                        "set patch_result=1",
                        "if exist \"" + installLocation + "_removeme\" (",
                        "    rmdir \"" + installLocation + "_removeme\" /s /q || goto :restart",
                        ")",

                        // Move away old version. (If it fails, abort update.)
                        "set patch_result=2",
                        "move \"" + installLocation + "\" \"" + installLocation + "_removeme\" || goto :restart",

                        // Copy new version and remove old version. (These are sufficiently unlikely to fail to ignore failures.)
                        "set patch_result=0",
                        "xcopy \"" + GetExtractDirectory().TrimEnd('\\') + "\" \"" + installLocation + "\" /v /f /e /s /r /h /y /i",
                        "rmdir \"" + installLocation + "_removeme\" /s /q",

                        // Restart launcher.
                        ":restart",
                        "start \"\" \"" + executableName + "\" --patch-result=%patch_result%",

                        // Clean up. (This also removes this batch file!)
                        "rmdir \"" + GetTempDirectory().TrimEnd('\\') + "\" /s /q",
                    });
                    if (!Directory.Exists(GetTempDirectory()))
                    {
                        throw new Exception("Temp directory failure, can not initialize bat file.");
                    }
                    File.WriteAllText(GetBatPath(), contents);

                    ProcessStartInfo startInfo = new ProcessStartInfo(GetBatPath(), "/B");
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    Process process = new Process();
                    process.StartInfo = startInfo;
                    process.StartInfo.Verb = "runas";
                    process.Start();
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
                UpdaterWindow.StatusLabel.Content = "Download Finished; verifying...";

                // Generate SHA256 hash of the download
                Task<string> hash_task = RXPatchLib.SHA256.GetFileHashAsync(GetSavePath());
                hash_task.Wait();

                // Verify the hash of the download
                if (hash_task.Result == PatchHash || PatchHash == "")
                {
                    // Download valid; begin extraction
                    StartExtract();
                }
                else
                {
                    // Hash mismatch; set an error
                    UpdateState = eUpdateState.Error;
                    UpdaterWindow.StatusLabel.Content = "Hash mismatch";
                }
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
