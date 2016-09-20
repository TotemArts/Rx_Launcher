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

namespace LauncherTwo
{

    /// <summary>
    /// Interaction logic for Installer.xaml
    /// </summary>
    public partial class Installer : Window
    {
        const string MESSAGE_INSTALL = "It looks like this is the first time you're running Renegade X or your installation is corrupted.\nDo you wish to install the game?";
        const string MESSAGE_NOT_INSTALLED = "You will not be able to play the game until the installation is finished!\nThis message will continue to appear untill installation is succesfull.";
        const string MESSAGE_REDIST_INSTALL = "You will now be prompted to install the Unreal Engine dependancies.\nThis is needed for the successfull installation of Renegade X.";

        public Installer()
        {
            InitializeComponent();
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
            var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
            var patchUrls = VersionCheck.GamePatchUrls;
            var patchVersion = VersionCheck.GetLatestGameVersionName();

            //Create an empty var containing the progress report from the patcher
            var progress = new Progress<DirectoryPatcherProgressReport>();
            var cancellationTokenSource = new System.Threading.CancellationTokenSource();
            Task task = new RXPatcher().ApplyPatchFromWeb(patchUrls, targetDir, applicationDir, progress, cancellationTokenSource.Token);


            //Create the update window
            int index = await new UpdateServerSelector().SelectHostIndex(patchUrls);
            var window = new ApplyUpdateWindow(task, progress, patchVersion, cancellationTokenSource, ApplyUpdateWindow.UpdateWindowType.Install, new Uri(patchUrls[index]).Host);
            //window.Owner = this;
            //Show the dialog and wait for completion
            window.ShowDialog();
            
            if (task.IsCompleted == true)
            {
                VersionCheck.UpdateGameVersion();
                //Create the UE3 redist dialog
                ModernDialog UERedistDialog = new ModernDialog();
                UERedistDialog.Title = "UE3 Redistributable";
                UERedistDialog.Content = MESSAGE_REDIST_INSTALL;
                UERedistDialog.Buttons = new Button[] { UERedistDialog.OkButton, UERedistDialog.CancelButton };
                UERedistDialog.ShowDialog();

                if (UERedistDialog.DialogResult.Value == true)
                {
                    //Determine which server has best ping
                    String[] PatchUrls = VersionCheck.GamePatchUrls;
                    RXPatchLib.UpdateServerSelector Selector = new RXPatchLib.UpdateServerSelector();//Need to solve a import issue regarding duplicate classes
                    Task<int> SelectorTask = Selector.SelectHostIndex(VersionCheck.GamePatchUrls); //NEed to suppress the ui from showing here
                    await SelectorTask;
                    Uri Redistserver = new Uri(PatchUrls[SelectorTask.Result]);
                    //Create new URL based on the patch url (Without the patch part)
                    String RedistUrl = "http://" + Redistserver.Host + "/redists/UE3Redist.exe";
                    string SystemUrl = GameInstallation.GetRootPath() + "Launcher\\Redist\\UE3Redist.exe";

                    //Create canceltokens to stop the downloaderthread if neccesary
                    CancellationTokenSource downloaderTokenSource = new CancellationTokenSource();
                    CancellationToken downloaderToken = downloaderTokenSource.Token;

                    //Task for downloading the redist from patch server
                    Task RedistDownloader = new Task(() => {
                        System.IO.Directory.CreateDirectory(GameInstallation.GetRootPath() + "Launcher\\Redist");
                        System.Net.WebClient RedistRequest = new WebClient();
                        RedistRequest.DownloadFileAsync(new Uri(RedistUrl), SystemUrl);
                        while (RedistRequest.IsBusy && !downloaderToken.IsCancellationRequested)
                        {
                            if (downloaderToken.IsCancellationRequested)
                            {
                                RedistRequest.CancelAsync();
                            }
                            //Thread.Sleep(1000);
                        }

                    }, downloaderToken);

                    //Redist downloader statuswindow
                    GeneralDownloadWindow RedistWindow = new GeneralDownloadWindow(downloaderTokenSource, "UE3Redist download");
                    RedistWindow.Show();
                    //Task to keep the status of the UE3Redist download
                    Task RedistDownloadStatus = new Task(() =>
                    {
                        WebRequest req = System.Net.HttpWebRequest.Create(RedistUrl);
                        req.Method = "HEAD";
                        int ContentLength;
                        using (WebResponse resp = req.GetResponse())
                        {
                            int.TryParse(resp.Headers.Get("Content-Length"), out ContentLength);
                        }

                        RedistWindow.initProgressBar(ContentLength);
                        while (RedistDownloader.Status == TaskStatus.Running)
                        {
                            RedistWindow.Status = "Downloading UE3Redist";
                            FileInfo inf = new FileInfo(SystemUrl);
                            RedistWindow.updateProgressBar(inf.Length);
                            //Thread.Sleep(1000);
                        }
                    });

                    //Start downloading
                    RedistDownloader.Start();
                    RedistDownloadStatus.Start();
                    await RedistDownloader;
                    RedistWindow.Close();

                    //When done, execute the UE3Redist here
                    try
                    {
                        using (Process UE3Redist = Process.Start(GameInstallation.GetRootPath() + "Launcher\\Redist\\UE3Redist.exe"))
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
                                    System.IO.File.Delete(GameInstallation.GetRootPath() + "Launcher\\Redist\\UE3Redist.exe");
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
                        
                        //Set the directory ownership
                        DirectoryInfo dirInfo = new DirectoryInfo(GameInstallation.GetRootPath());
                        var sid = new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.AuthenticatedUserSid, null);
                        DirectorySecurity dirRights = dirInfo.GetAccessControl();
                        dirRights.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));                  
                        dirInfo.SetAccessControl(dirRights);         
                        //Restart launcher
                        System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
                        Application.Current.Shutdown();
                    }

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
