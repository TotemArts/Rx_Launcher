using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LauncherTwo
{
    public abstract class EngineInstanceStartupParameters
    {
        public virtual string GetProcessPath()
        {
            return GameInstallation.GetRootPath() + "Binaries\\UDKLift.exe";
        }

        public abstract string GetProcessArguments();
    }

    public class EditorInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public override string GetProcessArguments()
        {
            return "editor";
        }
    }

    public class ServerInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public override string GetProcessArguments()
        {
            return "server CNC-Walls -nosteam";
        }
    }

    public class GameInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IpEndpoint { get; set; }
        public bool SkipIntroMovies { get; set; }
        public bool Use64bit { get; set; }

        public override string GetProcessArguments ()
        {
            string arguments = "";
            if (IpEndpoint != null)
            {
                arguments += IpEndpoint;
                if (Password != null)
                {
                    arguments += "?PASSWORD=" + Password;
                }
            }
            if (SkipIntroMovies)
            {
                arguments += " -nomoviestartup"; 
            }

            arguments += " -ini:UDKGame:DefaultPlayer.Name=" + Username.Replace(' ', '\u00A0');
            return arguments;
        }

        public override string GetProcessPath()
        {
            if (Use64bit)
                return GameInstallation.GetRootPath() + "Binaries\\Win64\\UDK.exe";
            else
                return GameInstallation.GetRootPath() + "Binaries\\Win32\\UDK.exe";
        }

    }

    public class EngineInstance
    {
        public EngineInstanceStartupParameters StartupParameters { get; protected set; }
        public string IpEndpoint = "";
        public Task Task { get; protected set; }

        public static EngineInstance Start(EngineInstanceStartupParameters startupParameters)
        {
            var instance = new EngineInstance();

            try
            {
                var ipHack = (GameInstanceStartupParameters)startupParameters;
                instance.IpEndpoint = ipHack.IpEndpoint;
                ipHack = null;
            }
            catch { };

            instance.StartupParameters = startupParameters;
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