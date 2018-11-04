namespace LauncherTwo
{
    public abstract class IStartupExpression
    {
        public abstract bool CheckArgument(StartupContext context);
        public abstract void Evaluate(StartupContext context);
    }
}
