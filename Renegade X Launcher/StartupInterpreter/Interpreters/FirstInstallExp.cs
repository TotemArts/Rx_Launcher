using System;

namespace LauncherTwo
{
    public class FirstInstallExp : IStartupExpression
    {
        public FirstInstallExp() { }

        public override bool CheckArgument(StartupContext context)
        {
            if (string.IsNullOrEmpty(context.Argument))
                return false;

            return context.Argument.StartsWith("--firstInstall");
        }

        public override void Evaluate(StartupContext context)
        {
            RxLogger.Logger.Instance.Write("Startup parameters 'firstInstall' found - Starting RenX Installer");
            try
            {
                Installer x = new Installer();
                x.Show();
                x.FirstInstall();
                context.Interrupt = true;
            } catch (Exception) { }
        }
    }
}
