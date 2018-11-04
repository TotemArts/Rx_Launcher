using System;
using RxLogger;
using RXPatchLib;

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
            // Close any other instances of the RenX-Launcher
            if (InstanceHandler.IsAnotherInstanceRunning())
                InstanceHandler.KillDuplicateInstance();

            try
            {
                var targetDir = GameInstallation.GetRootPath();
                var applicationDir = System.IO.Path.Combine(GameInstallation.GetRootPath(), "patch");
                var patchVersion = VersionCheck.GetLatestGameVersionName();
                var progress = new Progress<DirectoryPatcherProgressReport>();
                var cancellationTokenSource = new System.Threading.CancellationTokenSource();

                // Get latest data (TODO: this somehow prevents MainWindow from intializing)
                var updateTask = VersionCheck.UpdateLatestVersions();
                new Views.ApplyUpdateWindow(
                    updateTask,
                    RxPatcher.Instance,
                    progress,
                    patchVersion,
                    cancellationTokenSource,
                    Views.ApplyUpdateWindow.UpdateWindowType.Update).ShowDialog();

                // Update
                /*System.Threading.Tasks.Task task = RxPatcher.Instance.ApplyPatchFromWeb(VersionCheck.LauncherPatchUrl, targetDir, applicationDir, progress, cancellationTokenSource.Token, VersionCheck.LauncherPatchHash);
                new Views.ApplyUpdateWindow(
                    task, 
                    RxPatcher.Instance, 
                    progress, 
                    patchVersion, 
                    cancellationTokenSource, 
                    Views.ApplyUpdateWindow.UpdateWindowType.Update)
                .ShowDialog();*/

                var patchPath = VersionCheck.GamePatchPath;
                var patchUrls = VersionCheck.GamePatchUrls;

                RxLogger.Logger.Instance.Write($"Starting game update | TargetDir: {targetDir} | AppDir: {applicationDir} | PatchPath: {patchPath} | PatchVersion: {patchVersion}");
                var task = RxPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir, progress, cancellationTokenSource, VersionCheck.InstructionsHash);

                RxLogger.Logger.Instance.Write("Download complete, Showing ApplyUpdateWindow");
                var window = new Views.ApplyUpdateWindow(task, RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, Views.ApplyUpdateWindow.UpdateWindowType.Update);

                window.ShowDialog();

                VersionCheck.UpdateGameVersion();
            }
            catch { }

            context.DidTryUpdate = true;
        }
        private void UpdateGameByUrl(StartupContext context)
        {
            // Close any other instances of the RenX-Launcher
            if (InstanceHandler.IsAnotherInstanceRunning())
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
                        var progress = new Progress<DirectoryPatcherProgressReport>();
                        var cancellationTokenSource = new System.Threading.CancellationTokenSource();

                        RxPatcher.Instance.AddNewUpdateServer(patchUrl, "");
                        System.Threading.Tasks.Task task = RxPatcher.Instance.ApplyPatchFromWebDownloadTask(RXPatchLib.RxPatcher.Instance.GetNextUpdateServerEntry(), targetDir, applicationDir, progress, cancellationTokenSource, null); // no verificaiton on instructions.json, as we're bypassing standard version checking

                        var window = new Views.ApplyUpdateWindow(task, RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, Views.ApplyUpdateWindow.UpdateWindowType.Update);

                        window.ShowDialog();

                        VersionCheck.UpdateGameVersion();
                    }
                    else
                    {
                        string code = "503"; // 503: Service Unavailable
                                             //MessageBox.Show(string.Format("Failed to update the launcher because the server seems to be offline.(code {0}).\n\nPlease try again later.", code), "Update failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        Logger.Instance.Write(string.Format("Failed to update the launcher. Could not connect to server (code {0}).", code), Logger.ErrorLevel.ErrWarning);
                    }
                }
                catch { }
            }
            else
            {
                string code = "400"; // 400: Bad request
                                     //MessageBox.Show(string.Format("Failed to update the launcher because the given url is not valid(code {0}).\n\nPlease enter a valid url and try again.", code), "Update failed", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.Instance.Write(string.Format("Failed to update the launcher. Given url is not valid (code {0}).", code), Logger.ErrorLevel.ErrWarning);
            }
            context.DidTryUpdate = true;
        }
    }
}
