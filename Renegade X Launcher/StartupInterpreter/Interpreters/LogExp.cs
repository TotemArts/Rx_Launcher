using System;

namespace LauncherTwo
{
    public class LogExp : IStartupExpression
    {
        public LogExp() { }

        public override bool CheckArgument(StartupContext context)
        {
            if (string.IsNullOrEmpty(context.Argument))
                return false;
            return context.Argument.Equals("--log", StringComparison.OrdinalIgnoreCase);
        }

        public override void Evaluate(StartupContext context)
        {
            RxLogger.Logger.Instance.StartLogConsole();
            context.IsLogging = true;
        }
    }
}
