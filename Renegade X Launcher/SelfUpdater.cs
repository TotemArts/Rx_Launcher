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
                    string executableName = System.Reflection.Assembly.GetExecutingAssembly().Location;

                    string pidString = Process.GetCurrentProcess().Id.ToString();
                    string contents = string.Join("\r\n", new string[]
                    {
                        "cd /D \"" + Path.GetTempPath() + "\"",

                        // Wait for the launcher to close.
                        //":wait_for_close",
                        //"tasklist /FI \"PID eq " + pidString + "\" /FO csv /NH | find \"\"\"" + pidString + "\"\"\" > nul",
                        //"if not errorlevel 1 (",
                        //"    timeout /t 1 > nul",
                        //"    goto :wait_for_close",
                        //")",

                        // Kill the process, why did we wait for it in the first place? - this is easier and less error prone
                        "taskkill /PID " + pidString + " /F",

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
