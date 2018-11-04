﻿using RxLogger;
using System;

namespace LauncherTwo.StartupInterpreter.Interpreters
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
            Logger.Instance.StartLogConsole();
            context.IsLogging = true;
        }
    }
}
