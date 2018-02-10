using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using RxLogger;
using RXPatchLib;

namespace LauncherTwo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        

        public void StartupApp(object sender, StartupEventArgs e)
        {
            //Determine if the permissionChange is succesfull after launcher update
            bool didTryUpdate = false;
            bool isLogging = false;

            Logger.Instance.Write("Application starting up...");

            foreach (string a in e.Args)
            {
                if (a.Equals("--log", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Instance.StartLogConsole();
                    isLogging = true;
                }
                if (a.StartsWith("--patch-result="))
                {
                    didTryUpdate = true;
                    string code = a.Substring("--patch-result=".Length);
                    Logger.Instance.Write($"Startup Parameter 'patch-result' found - contents: {code}");
                    //If the code !=0 -> there is something wrong with the patching of the launcher
                    if (code != "0" && code != "Success")
                    {
                        MessageBox.Show(string.Format("Failed to update the launcher (code {0}).\n\nPlease close any applications related to Renegade-X and try again.", code), "Patch failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else // Otherwise -> change folderpermissions and afterwards launch the launcher
                    {
                        try {
                            SetFullControlPermissionsToEveryone(GameInstallation.GetRootPath());
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message);
                        }
                    }
                }
                else if (a.StartsWith("--firstInstall")) //Init the first install
                {
                    Logger.Instance.Write("Startup parameters 'firstInstall' found - Starting RenX Installer");
                    Installer x = new Installer();
                    x.Show();
                    x.FirstInstall();
                    return;
                }
                else if(a.StartsWith("--UpdateGame="))//Manually opdate the game to a given URL.
                {
                    // Close any other instances of the RenX-Launcher
                    if ( InstanceHandler.IsAnotherInstanceRunning() )
                        InstanceHandler.KillDuplicateInstance();

                    var targetDir = GameInstallation.GetRootPath();
                    var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
                    String patchUrl = a.Substring("--UpdateGame=".Length);
                    var patchVersion = VersionCheck.GetLatestGameVersionName();

                    var progress = new Progress<DirectoryPatcherProgressReport>();
                    var cancellationTokenSource = new System.Threading.CancellationTokenSource();

                    RxPatcher.Instance.AddNewUpdateServer(patchUrl, "");
                    System.Threading.Tasks.Task task = RxPatcher.Instance.ApplyPatchFromWebDownloadTask(RXPatchLib.RxPatcher.Instance.GetNextUpdateServerEntry(), targetDir, applicationDir, progress, cancellationTokenSource.Token, null); // no verificaiton on instructions.json, as we're bypassing standard version checking

                    var window = new Views.ApplyUpdateWindow(task, RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, Views.ApplyUpdateWindow.UpdateWindowType.Update);

                    window.ShowDialog();

                    VersionCheck.UpdateGameVersion();
                    return;
                }
            }

            if (LauncherTwo.Properties.Settings.Default.UpgradeRequired)
            {
                LauncherTwo.Properties.Settings.Default.Upgrade();
                LauncherTwo.Properties.Settings.Default.UpgradeRequired = false;
                LauncherTwo.Properties.Settings.Default.Save();
            }

            /* Commented out untill I found a better way to intergrate it in the installation
            if (!GameInstallation.IsRootPathPlausible())
            {
                var result = MessageBox.Show("The game path seems to be incorrect. Please ensure that the launcher is placed in the correct location. If you proceed, files in the following location might be affected:\n\n" + GameInstallation.GetRootPath() + "\n\nAre you sure want to proceed?", "Invalid game path", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
                if (result != MessageBoxResult.Yes)
                {
                    Shutdown();
                    return;
                }
            }
            */
            //If no args are present, or a permissionChange update was executed -> normally start the launcher
            // didTryUpdate - If we tried an update, we have args, so we need to check this as well to make the main window load.
            if (e.Args.Length == 0 || didTryUpdate || isLogging)
            {
                if (InstanceHandler.IsAnotherInstanceRunning() && !didTryUpdate)
                {
                    MessageBox.Show("Error:\nUnable to start Renegade-X Launcher: Another instance is already running!",
                        "Renegade-X Launcher", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    Current.Shutdown();
                }

                Logger.Instance.Write("Initial application startup complete, Creating new MainWindow");
                new MainWindow().Show();
            }
            /*else
            {
                Application.Current.Shutdown();
            }*/

            
        }

        /// <summary>
        /// Set the full rights permission to the usergroup of the desired folder
        /// Made by Timothée Lecomte found on http://stackoverflow.com/questions/8944765/c-sharp-set-directory-permissions-for-all-users-in-windows-7
        /// This almost made me throw out my pc
        /// </summary>
        /// <param name="path">The path of the folder you wish to get full permissions over</param>
        static void SetFullControlPermissionsToEveryone(string path)
        {
            const FileSystemRights rights = FileSystemRights.FullControl;

            var allUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            // Add Access Rule to the actual directory itself
            var accessRule = new FileSystemAccessRule(
                allUsers,
                rights,
                InheritanceFlags.None,
                PropagationFlags.InheritOnly,
                AccessControlType.Allow);

            var info = new DirectoryInfo(path);
            var security = info.GetAccessControl(AccessControlSections.Access);

            bool result;
            security.ModifyAccessRule(AccessControlModification.Set, accessRule, out result);

            if (!result)
            {
                throw new System.InvalidOperationException("Failed to give full-control permission to all users for path " + path);
            }

            // add inheritance
            var inheritedAccessRule = new FileSystemAccessRule(
                allUsers,
                rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.InheritOnly,
                AccessControlType.Allow);

            bool inheritedResult;
            security.ModifyAccessRule(AccessControlModification.Add, inheritedAccessRule, out inheritedResult);

            if (!inheritedResult)
            {
                throw new System.InvalidOperationException("Failed to give full-control permission inheritance to all users for " + path);
            }

            info.SetAccessControl(security);
        }
    }
}
