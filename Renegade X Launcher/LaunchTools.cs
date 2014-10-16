using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;

namespace LauncherTwo
{
    public class GameInstanceStartupParameters
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IPEndpoint { get; set; }

        public string GetProcessPath()
        {
            return GameInstallation.GetRootPath() +"Binaries\\Win32\\UDK.exe";
        }

        public string GetProcessArguments()
        {
            string Arguments = "";
            if (Username != null)
            {
                Arguments += IPEndpoint;
                if (Password != null)
                {
                    Arguments += "?PASSWORD=" + Password;
                }
            }
            Arguments += " -ini:UDKGame:DefaultPlayer.Name=\"" + Username + "\"";
            return Arguments;
        }
    }

    public class GameInstance
    {
        public GameInstanceStartupParameters StartupParameters { get; private set; }
        public Task Task { get; private set; }

        Process Process;

        public static GameInstance Start(GameInstanceStartupParameters StartupParameters)
        {
            var instance = new GameInstance();
            instance.StartupParameters = StartupParameters;
            instance.Task = instance.StartAsync();
            return instance;
        }

        public async Task StartAsync()
        {
            try
            {
                Process = new Process();
                Process.StartInfo.FileName = StartupParameters.GetProcessPath();
                Process.StartInfo.Arguments = StartupParameters.GetProcessArguments();
                TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();
                Process.EnableRaisingEvents = true;
                Process.Exited += (sender, e) => { tcs.SetResult(Process.ExitCode); };
                Process.Start();
                await tcs.Task;
            }
            finally
            {
                Process.Dispose();
                Process = null;
            }
        }
    }
}
