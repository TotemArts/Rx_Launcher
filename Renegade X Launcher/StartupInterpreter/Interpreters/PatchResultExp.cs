using LauncherTwo.Tools;
using RxLogger;
using System;
using System.Windows;

namespace LauncherTwo.StartupInterpreter.Interpreters
{
    public class PatchResultExp : IStartupExpression
    {
        public PatchResultExp() { }

        public override bool CheckArgument(StartupContext context)
        {
            if (string.IsNullOrEmpty(context.Argument))
                return false;

            return context.Argument.StartsWith("--patch-result=");
        }

        public override void Evaluate(StartupContext context)
        {
            context.DidTryUpdate = true;
            string code = context.Argument.Substring("--patch-result=".Length);
            Logger.Instance.Write($"Startup Parameter 'patch-result' found - contents: {code}");
            
            //If the code !=0 -> there is something wrong with the patching of the launcher
            if (code != "0" && code != "Success") {
                MessageBox.Show(string.Format("Failed to update the launcher (code {0}).\n\nPlease close any applications related to Renegade-X and try again.", code), "Patch failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else // Otherwise -> change folderpermissions and afterwards launch the launcher
            {
                try
                {
                    Utils.SetFullControlPermissionsToEveryone(GameInstallation.GetRootPath());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}
