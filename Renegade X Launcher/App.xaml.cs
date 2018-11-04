using System.Collections.Generic;
using System.Windows;
using LauncherTwo.StartupInterpreter;
using LauncherTwo.StartupInterpreter.Interpreters;
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
            //Determine if the permissionChange is successful after launcher update
            bool didTryUpdate = false;
            bool isLogging = false;
            bool stopChecking = false;

            Logger.Instance.Write("Application starting up; checking command line options...");
            
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
                Logger.Instance.Write("Parsing option: " + a);
                // Create a context for each argument
                StartupContext context = new StartupContext
                {
                    DidTryUpdate = false,
                    IsLogging = false,
                    StopChecking = false,
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

                    if (context.StopChecking) {
                        stopChecking = true;
                        break;
                    }
                }
            }

            if (stopChecking) {
                return;
            }

            Logger.Instance.Write("Done checking command line options");

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
                    Current.Shutdown();
                }

                Logger.Instance.Write("Initial application startup complete, Creating new MainWindow");
                new MainWindow().Show();
            }

            Logger.Instance.Write("Exiting StartupApp...");
        }
        
    }
}
