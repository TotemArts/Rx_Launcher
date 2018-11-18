using System.Collections.Generic;
using System.Windows;
using RxLogger;

namespace LauncherTwo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public void StartupApp(object sender, StartupEventArgs e)
        {
            // Application startup; Evaluating command-line arguments...
            bool didTryUpdate = false;
            bool isLogging = false;
            bool interrupt = false;

            // Use Interpreter design pattern to solve our command line options
            List<IStartupExpression> expressions = new List<IStartupExpression>
            {
                new LogExp(),
                new FirstInstallExp(),
                new PatchResultExp(),
                new UpdateGameExp()
            };
            
            foreach (string a in e.Args)
            {
                // Create a context foreach argument
                StartupContext context = new StartupContext
                {
                    DidTryUpdate = false,
                    IsLogging = false,
                    Interrupt = false,
                    Argument = a
                };
                foreach (IStartupExpression exp in expressions)
                {
                    if (exp.CheckArgument(context))
                        exp.Evaluate(context);

                    if (context.DidTryUpdate)
                        didTryUpdate = true;

                    if (context.IsLogging)
                        isLogging = true;

                    if (context.Interrupt) {
                        interrupt = true;
                        break;
                    }
                }
                if (interrupt) { break; }
            }

            // Not all expressions want to show the MainWindow afterwards
            if (interrupt) {
                return;
            }
            
            if (LauncherTwo.Properties.Settings.Default.UpgradeRequired)
            {
                Logger.Instance.Write("Upgrading properties...");
                LauncherTwo.Properties.Settings.Default.Upgrade();
                LauncherTwo.Properties.Settings.Default.UpgradeRequired = false;
                LauncherTwo.Properties.Settings.Default.Save();
                Logger.Instance.Write("Properties upgraded");
            }

            // If no args are present, or a permissionChange update was executed -> normally start the launcher
            // didTryUpdate - If we tried an update, we have args, so we need to check this as well to make the main window load.
            if (e.Args.Length == 0 || didTryUpdate || isLogging)
            {
                if (InstanceHandler.IsAnotherInstanceRunning() && !didTryUpdate)
                {
                    MessageBox.Show("Error:\nUnable to start Renegade-X Launcher: Another instance is already running!",
                        "Renegade-X Launcher", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    //Current.Shutdown();
                    return; // No need to force shutdown
                }

                Logger.Instance.Write("Initial application startup complete, Creating new MainWindow...", Logger.ErrorLevel.ErrSuccess);
                new MainWindow(isLogging).Show();
            }

            Logger.Instance.Write("Exiting application...");
        }
        
    }
}
