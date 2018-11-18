using System;

namespace LauncherTwo
{
    public class UpdateGameExp : IStartupExpression
    {
        public UpdateGameExp() { }

        public override bool CheckArgument(StartupContext context)
        {
            if (string.IsNullOrEmpty(context.Argument))
                return false;

            return context.Argument.StartsWith("--UpdateGame");
        }

        public override void Evaluate(StartupContext context)
        {
            // Check if an url is provided
            if (context.Argument.Contains("="))
                UpdateGameByUrl(context);
            else
                UpdateGame(context);
        }

        private void UpdateGame(StartupContext context)
        {
            // --------------------- Launcher needs revamp ----------------------------
            // ---------------- This needs to be command-line only --------------------

            // Close any other instances of the RenX-Launcher
            /*if (InstanceHandler.IsAnotherInstanceRunning())
                InstanceHandler.KillDuplicateInstance();

            try
            {
                // Get latest data from server
                await VersionCheck.UpdateLatestVersions();
                
                // Check if game needs an update!
                string gameVersion = VersionCheck.GetGameVersionName();
                if (gameVersion.ToLower().Equals("unknown")) {
                    // Could not locate game version
                    RxLogger.Logger.Instance.Write(string.Format("Could not locate installed game version. Latest version is {0}", VersionCheck.GetLatestGameVersionName()), RxLogger.Logger.ErrorLevel.ErrWarning);
                }
                else if (!VersionCheck.IsGameOutOfDate())
                {
                    // Game is already up to date! Nothing to do here...
                    RxLogger.Logger.Instance.Write(string.Format("Game is up to date!. Version {0}", VersionCheck.GetLatestGameVersionName()), RxLogger.Logger.ErrorLevel.ErrSuccess);
                } else {
                    // Game is out of date: Update Time!
                    RxLogger.Logger.Instance.Write(string.Format("Upgrading game... Version {0}", VersionCheck.GetLatestGameVersionName()), RxLogger.Logger.ErrorLevel.ErrSuccess);

                    // TODO
                }


                var targetDir = GameInstallation.GetRootPath();
                var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
                var patchVersion = VersionCheck.GetLatestGameVersionName();
                var progress = new Progress<RXPatchLib.DirectoryPatcherProgressReport>();
                var cancellationTokenSource = new System.Threading.CancellationTokenSource();

                // Get latest data (TODO: this somehow prevents MainWindow from intializing)
                var updateTask = VersionCheck.UpdateLatestVersions();

                // Update
                System.Threading.Tasks.Task task = RXPatchLib.RxPatcher.Instance.ApplyPatchFromWeb(VersionCheck.LauncherPatchUrl, targetDir, applicationDir, progress, cancellationTokenSource, VersionCheck.LauncherPatchHash);
                new Views.ApplyUpdateWindow(
                    task,
                    RXPatchLib.RxPatcher.Instance, 
                    progress, 
                    patchVersion, 
                    cancellationTokenSource, 
                    Views.ApplyUpdateWindow.UpdateWindowType.Update)
                .ShowDialog();

                var patchPath = VersionCheck.GamePatchPath;
                var patchUrls = VersionCheck.GamePatchUrls;

                RxLogger.Logger.Instance.Write($"Starting game update | TargetDir: {targetDir} | AppDir: {applicationDir} | PatchPath: {patchPath} | PatchVersion: {patchVersion}");
                var patch = RXPatchLib.RxPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir, progress, cancellationTokenSource, VersionCheck.InstructionsHash);

                RxLogger.Logger.Instance.Write("Download complete, Showing ApplyUpdateWindow");
                var window = new Views.ApplyUpdateWindow(patch, RXPatchLib.RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, Views.ApplyUpdateWindow.UpdateWindowType.Update);

                window.Show();

                VersionCheck.UpdateGameVersion();
            }
            catch { }

            context.DidTryUpdate = true;*/
        }
        private void UpdateGameByUrl(StartupContext context)
        {
            // --------------------- Launcher needs revamp ----------------------------
            // ---------------- This needs to be command-line only --------------------

            // Close any other instances of the RenX-Launcher
            /*if (InstanceHandler.IsAnotherInstanceRunning())
                InstanceHandler.KillDuplicateInstance();

            var targetDir = GameInstallation.GetRootPath();
            var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
            string patchUrl = context.Argument.Substring("--UpdateGame=".Length);
            var patchVersion = VersionCheck.GetLatestGameVersionName();

            if (patchUrl != string.Empty && Utils.IsValidURI(patchUrl))
            {
                try
                {
                    Uri patchUri = new Uri(patchUrl);
                    if (Utils.PingHost(patchUri.Host))
                    {
                        var progress = new Progress<RXPatchLib.DirectoryPatcherProgressReport>();
                        var cancellationTokenSource = new System.Threading.CancellationTokenSource();

                        RXPatchLib.RxPatcher.Instance.AddNewUpdateServer(patchUrl, "");
                        System.Threading.Tasks.Task task = RXPatchLib.RxPatcher.Instance.ApplyPatchFromWebDownloadTask(RXPatchLib.RxPatcher.Instance.GetNextUpdateServerEntry(), targetDir, applicationDir, progress, cancellationTokenSource, null); // no verificaiton on instructions.json, as we're bypassing standard version checking

                        var window = new Views.ApplyUpdateWindow(task, RXPatchLib.RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, Views.ApplyUpdateWindow.UpdateWindowType.Update);

                        window.ShowDialog();

                        VersionCheck.UpdateGameVersion();
                    }
                    else
                    {
                        string code = "503"; // 503: Service Unavailable
                                             //MessageBox.Show(string.Format("Failed to update the launcher because the server seems to be offline.(code {0}).\n\nPlease try again later.", code), "Update failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        RxLogger.Logger.Instance.Write(string.Format("Failed to update the launcher. Could not connect to server (code {0}).", code), RxLogger.Logger.ErrorLevel.ErrWarning);
                    }
                }
                catch { }
            }
            else
            {
                string code = "400"; // 400: Bad request
                                     //MessageBox.Show(string.Format("Failed to update the launcher because the given url is not valid(code {0}).\n\nPlease enter a valid url and try again.", code), "Update failed", MessageBoxButton.OK, MessageBoxImage.Error);
                RxLogger.Logger.Instance.Write(string.Format("Failed to update the launcher. Given url is not valid (code {0}).", code), RxLogger.Logger.ErrorLevel.ErrWarning);
            }
            context.DidTryUpdate = true;*/
        }
    }
}
