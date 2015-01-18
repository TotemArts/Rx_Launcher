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
    public abstract class EngineInstanceStartupParameters
    {
        public string GetProcessPath ()
        {
            return GameInstallation.GetRootPath() + "Binaries\\Win32\\UDK.exe";
        }

        public abstract string GetProcessArguments ();

    }

    public class EditorInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public override string GetProcessArguments ()
        {
            return " editor";
        }
    }

    public class GameInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IPEndpoint { get; set; }
        public bool SkipIntroMovies { get; set; }

        public override string GetProcessArguments ()
        {
            string Arguments = "";
            if (IPEndpoint != null)
            {
                Arguments += IPEndpoint;
                if (Password != null)
                {
                    Arguments += "?PASSWORD=" + Password;
                }
            }
            if (SkipIntroMovies)
            {
                Arguments += " -nomovies";
            }
            Arguments += " -ini:UDKGame:DefaultPlayer.Name=\"" + Username + "\"";
            return Arguments;
        }
    }

    public class EngineInstance
    {
        public EngineInstanceStartupParameters StartupParameters { get; protected set; }
        public Task Task { get; protected set; }

        public static EngineInstance Start<T> (T StartupParameters) where T : EngineInstanceStartupParameters
        {
            var instance = new EngineInstance();
            instance.StartupParameters = StartupParameters;
            instance.Task = instance.StartAsync();
            return instance;
        }

        public async Task StartAsync ()
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

        protected Process Process;
    }
}
