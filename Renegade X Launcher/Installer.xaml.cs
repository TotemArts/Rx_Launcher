using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LauncherTwo.Views;
using System.Net;
using System;
using FirstFloor.ModernUI.Windows.Controls;
using RXPatchLib;
using System.IO;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Linq;

namespace LauncherTwo
{

    /// <summary>
    /// Interaction logic for Installer.xaml
    /// </summary>
    public partial class Installer : Window
    {
        const string MESSAGE_INSTALL = "It looks like this is the first time you're running Renegade X.\nDo you wish to install the game?";
        const string MESSAGE_NOT_INSTALLED = "You will not be able to play the game until the installation is finished!\nThis message will continue to appear untill installation is succesfull.";
        const string MESSAGE_REDIST_INSTALL = "You will now be prompted to install the Unreal Engine dependancies.\nThis is needed for the successfull installation of Renegade X.";

        public Installer()
        {
            InitializeComponent();
        }

        public async Task<bool> DownloadRedist(string source, string target, CancellationToken cancelToken, Action<long, long> progressCallback)
        {
            // Initialize directory and WebClient
            System.IO.Directory.CreateDirectory(GameInstallation.GetRootPath() + "Launcher\\Redist");
            System.Net.WebClient RedistRequest = new WebClient();

            // Report progress
            RedistRequest.DownloadProgressChanged += (o, args) =>
            {
                // Listen for cancellation
                if (cancelToken.IsCancellationRequested)
                    RedistRequest.CancelAsync();

                // Update progress
                progressCallback(args.BytesReceived, args.TotalBytesToReceive);
            };

            // Download file
            await RedistRequest.DownloadFileTaskAsync(new Uri(source), target);

            // Verify (UE3Redist isn't expected to ever change, so we're just dumping the hash here).
            return (await RXPatchLib.SHA256.GetFileHashAsync(target) == "A1A49F3C2E6830BAE084259650DFADF3AD97A30F59391930639D59220CC0B01F");
        }

        /// <summary>
        /// Function to control the first launch install.
        /// </summary>
        public async void FirstInstall()
        {
            VersionCheck.GetLatestGameVersionName();
            await VersionCheck.UpdateLatestVersions();

            // Preform a latency test
            // DISABLE THIS FOR NOW, REVERTING BACK TO QUEUE POP
            /*
            var LatencyTestDialog = new ModernDialog
            {
                Content = "Please Wait while we find the best possible patch server for you.\r\nThis should not take too long.",
                Title = "Renegade-X Server Selector",
                Width = 497,
                Height = 234
            };
            LatencyTestDialog.Show();
            await Task.Delay(1000);
            await RXPatcher.Instance.UpdateServerHandler.PreformLatencyTest();
            LatencyTestDialog.Close();
            */

            //Get the current root path and prepare the installation
            var targetDir = GameInstallation.GetRootPath();
            var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
            var patchPath = VersionCheck.GamePatchPath;
            var patchUrls = VersionCheck.GamePatchUrls;
            var patchVersion = VersionCheck.GetLatestGameVersionName();

            //Create an empty var containing the progress report from the patcher
            var progress = new Progress<DirectoryPatcherProgressReport>();
            var cancellationTokenSource = new System.Threading.CancellationTokenSource();

            Task task = RXPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir, progress, cancellationTokenSource.Token, VersionCheck.InstructionsHash);

            //Create the update window
            var window = new ApplyUpdateWindow(task, RXPatcher.Instance, progress, patchVersion, cancellationTokenSource, ApplyUpdateWindow.UpdateWindowType.Install);
            window.Owner = this;

            //Show the dialog and wait for completion
            window.ShowDialog();

            RxLogger.Logger.Instance.Write($"Install complete, task state isCompleted = {task.IsCompleted}");
            
            if (task.IsCompleted == true)
            {
                VersionCheck.UpdateGameVersion();
                //Create the UE3 redist dialog

                RxLogger.Logger.Instance.Write("Creating the UE3 Redist package dialog");
                ModernDialog UERedistDialog = new ModernDialog();
                UERedistDialog.Title = "UE3 Redistributable";
                UERedistDialog.Content = MESSAGE_REDIST_INSTALL;
                UERedistDialog.Buttons = new Button[] { UERedistDialog.OkButton, UERedistDialog.CancelButton };
                UERedistDialog.ShowDialog();

                RxLogger.Logger.Instance.Write($"Did the user want to install the UE3 Redist? - {UERedistDialog.DialogResult.Value}");

                if (UERedistDialog.DialogResult.Value == true)
                {
                    bool downloadSuccess = false;

                    var bestPatchServer = RXPatcher.Instance.GetUpdateServerHandler().SelectBestPatchServer();

                    Uri RedistServer = bestPatchServer.Uri;

                    //Create new URL based on the patch url (Without the patch part)
                    String RedistUrl = RedistServer + "redists/UE3Redist.exe";
                    string SystemPath = GameInstallation.GetRootPath() + "Launcher\\Redist\\UE3Redist.exe";

                    //Create canceltokens to stop the downloaderthread if neccesary
                    CancellationTokenSource downloaderTokenSource = new CancellationTokenSource();
                    CancellationToken downloaderToken = downloaderTokenSource.Token;

                    //Redist downloader statuswindow
                    GeneralDownloadWindow RedistWindow = new GeneralDownloadWindow(downloaderTokenSource, "UE3Redist download");
                    RedistWindow.Show();

                    //Start downloading redist
                    RxLogger.Logger.Instance.Write($"Downloading UE3 Redist from {RedistServer.AbsoluteUri}");
                    downloadSuccess = await DownloadRedist(RedistUrl, SystemPath, downloaderToken, (received, size) =>
                    {
                        RedistWindow.updateProgressBar(received, size);
                    });
                    RxLogger.Logger.Instance.Write("UE3 Redist Download Complete");

                    RedistWindow.Close();

                    if (downloadSuccess)
                    {
                        //When done, execute the UE3Redist here
                        try
                        {
                            using (Process UE3Redist = Process.Start(SystemPath))
                            {
                                UE3Redist.WaitForExit();
                                if (UE3Redist.ExitCode != 0)//If redist install fails, notify the user
                                {
                                    MessageBox.Show("Error while installing the UE3 Redist.");
                                }
                                else//Everything done! save installed flag and restart
                                {
                                    Properties.Settings.Default.Installed = true;
                                    Properties.Settings.Default.Save();
                                    try
                                    {
                                        System.IO.File.Delete(SystemPath);
                                        System.IO.Directory.Delete(GameInstallation.GetRootPath() + "Launcher\\Redist\\");
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
                            //Restart launcher
                            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                            Application.Current.Shutdown();
                        }
                    }

                    if (downloadSuccess == false)
                        MessageBox.Show("Unable to download the UE3 Redist (corrupt download)");
                }
                else
                {
                    ModernDialog notInstalledDialog = new ModernDialog();
                    notInstalledDialog.Title = "UE3 Redistributable";
                    notInstalledDialog.Content = MESSAGE_NOT_INSTALLED;
                    notInstalledDialog.Buttons = new Button[] { notInstalledDialog.OkButton };
                    notInstalledDialog.ShowDialog();
                    //Shutdown launcher
                    Application.Current.Shutdown();

                }

            }
        }
    }
}
