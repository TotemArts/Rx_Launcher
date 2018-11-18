using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LauncherTwo.Views;
using System.Net;
using System;
using FirstFloor.ModernUI.Windows.Controls;
using System.IO;
using System.Diagnostics;

namespace LauncherTwo
{

    /// <summary>
    /// Interaction logic for Installer.xaml
    /// </summary>
    public partial class Installer : Window
    {
        const string MessageInstall = "It looks like this is the first time you're running Renegade X.\nDo you wish to install the game?";
        const string MessageNotInstalled = "You will not be able to play the game until the installation is finished!\nThis message will continue to appear until installation is successful.";
        const string MessageRedistInstall = "You will now be prompted to install the Unreal Engine dependencies.\nThis is needed for the successful installation of Renegade X.";

        public Installer()
        {
            InitializeComponent();
        }

        public async Task<bool> DownloadRedist(string source, string target, CancellationToken cancelToken, Action<long, long> progressCallback)
        {
            // Initialize directory and WebClient
            Directory.CreateDirectory(GameInstallation.GetRootPath() + "Launcher\\Redist");
            using (WebClient redistRequest = new WebClient())
            {
                // Report progress
                redistRequest.DownloadProgressChanged += (o, args) =>
                {
                    // Listen for cancellation
                    if (cancelToken.IsCancellationRequested)
                        redistRequest.CancelAsync();

                    // Update progress
                    progressCallback(args.BytesReceived, args.TotalBytesToReceive);
                };

                // Download file
                await redistRequest.DownloadFileTaskAsync(new Uri(source), target);
            }

            // Verify (UE3Redist isn't expected to ever change, so we're just dumping the hash here).
            return (await RXPatchLib.Sha256.GetFileHashAsync(target) == "A1A49F3C2E6830BAE084259650DFADF3AD97A30F59391930639D59220CC0B01F");
        }

        /// <summary>
        /// Function to control the first launch install.
        /// </summary>
        public async void FirstInstall()
        {
            VersionCheck.GetLatestGameVersionName();
            await VersionCheck.UpdateLatestVersions();

            //Get the current root path and prepare the installation
            var targetDir = GameInstallation.GetRootPath();
            var applicationDir = Path.Combine(GameInstallation.GetRootPath(), "patch");
            var patchPath = VersionCheck.GamePatchPath;
            var patchVersion = VersionCheck.GetLatestGameVersionName();

            //Create an empty var containing the progress report from the patcher
            var progress = new Progress<RXPatchLib.DirectoryPatcherProgressReport>();
            var cancellationTokenSource = new CancellationTokenSource();

            bool didSucceed = false;

            Task task = RXPatchLib.RxPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir,
                            progress, cancellationTokenSource, VersionCheck.InstructionsHash);
            
            //Create the update window
            var window = new ApplyUpdateWindow(task, RXPatchLib.RxPatcher.Instance, progress, patchVersion,
                cancellationTokenSource, ApplyUpdateWindow.UpdateWindowType.Install)
            { Owner = this };
                
            //Show the dialog and wait for completion
            window.Show();
            
            // Wait for ApplyUpdateWindow window to be finished (by either finishing or cancelled by user)
            window.Closed += async (s, e) => {
                didSucceed = !cancellationTokenSource.IsCancellationRequested;
                RxLogger.Logger.Instance.Write($"Install finished, task state isCompleted = {task.IsCompleted}, didSucceed = {didSucceed}");
                
                if (didSucceed)
                {
                    // Install UE3 Redist
                    await InstallRedist();
                }

                Close();
            };

            try
            {
                while (!task.IsCompleted)
                {
                    await Task.Delay(1000);
                }
            } catch (OperationCanceledException) { // user cancelled operation
                //Application.Current.Shutdown(); // This will work... for now (not the right way tho)
            }
        }

        private async Task InstallRedist()
        {
            VersionCheck.UpdateGameVersion();
            //Create the UE3 redist dialog
            RxLogger.Logger.Instance.Write("Creating the UE3 Redist package dialog");
            ModernDialog ueRedistDialog = new ModernDialog
            {
                Title = "UE3 Redistributable",
                Content = MessageRedistInstall
            };
            ueRedistDialog.Buttons = new Button[] { ueRedistDialog.OkButton, ueRedistDialog.CancelButton };
            ueRedistDialog.ShowDialog();

            RxLogger.Logger.Instance.Write($"Did the user want to install the UE3 Redist? - {ueRedistDialog.DialogResult.Value}");

            if (ueRedistDialog.DialogResult.Value == true)
            {
                bool downloadSuccess = false;

                var bestPatchServer = RXPatchLib.RxPatcher.Instance.UpdateServerHandler.SelectBestPatchServer();
                Uri RedistServer = bestPatchServer.Uri;

                //Create new URL based on the patch url (Without the patch part)
                string redistUrl = RedistServer + "redists/UE3Redist.exe";
                string systemPath = GameInstallation.GetRootPath() + "Launcher\\Redist\\UE3Redist.exe";

                //Create canceltokens to stop the downloaderthread if neccesary
                CancellationTokenSource downloaderTokenSource = new CancellationTokenSource();
                CancellationToken downloaderToken = downloaderTokenSource.Token;

                //Redist downloader statuswindow
                GeneralDownloadWindow redistWindow = new GeneralDownloadWindow(downloaderTokenSource, "UE3Redist download");
                redistWindow.Show();

                //Start downloading redist
                RxLogger.Logger.Instance.Write($"Downloading UE3 Redist from {RedistServer.AbsoluteUri}");
                downloadSuccess = await DownloadRedist(redistUrl, systemPath, downloaderToken, (received, size) =>
                {
                    redistWindow.UpdateProgressBar(received, size);
                });
                RxLogger.Logger.Instance.Write("UE3 Redist Download Complete");

                redistWindow.Close();

                if (downloadSuccess)
                {
                    //When done, execute the UE3Redist here
                    try
                    {
                        using (Process ue3Redist = Process.Start(systemPath))
                        {
                            ue3Redist.WaitForExit();
                            if (ue3Redist.ExitCode != 0)//If redist install fails, notify the user
                            {
                                MessageBox.Show("Error while installing the UE3 Redist.");
                            }
                            else//Everything done! save installed flag and restart
                            {
                                Properties.Settings.Default.Installed = true;
                                Properties.Settings.Default.Save();
                                try
                                {
                                    File.Delete(systemPath);
                                    Directory.Delete(GameInstallation.GetRootPath() + "Launcher\\Redist\\");
                                }
                                catch (Exception)
                                {
                                    MessageBox.Show("Could not cleanup the redist file. This won't hinder the game.");
                                }
                            }
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Error while executing the UE3 Redist.");
                    }
                    finally
                    {
                        // Restart launcher
                        Process.Start(Application.ResourceAssembly.Location);
                        //Application.Current.Shutdown();
                    }
                }

                if (downloadSuccess == false)
                    MessageBox.Show("Unable to download the UE3 Redist (corrupt download)");
            }
            else
            {
                ModernDialog notInstalledDialog = new ModernDialog();
                notInstalledDialog.Title = "UE3 Redistributable";
                notInstalledDialog.Content = MessageNotInstalled;
                notInstalledDialog.Buttons = new Button[] { notInstalledDialog.OkButton };
                notInstalledDialog.ShowDialog();
                        
                //Shutdown launcher
                //Application.Current.Shutdown();
            }
        }
    }
}
