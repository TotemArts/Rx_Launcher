﻿using System;
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
        public override string GetProcessArguments()
        {
            return "editor";
        }
    }

    public class ServerInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public override string GetProcessArguments()
        {
            return "server CNC-Walls_Flying -nosteam";
        }
    }

    public class GameInstanceStartupParameters : EngineInstanceStartupParameters
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string IpEndpoint { get; set; }
        public bool SkipIntroMovies { get; set; }

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

            //Pinpoint location of quote error
            //Arguments += " -ini:UDKGame:DefaultPlayer.Name=\"" + Username + "\"";
            //End quote error

            //Fix for quote error
            arguments += " -ini:UDKGame:DefaultPlayer.Name=" + Username + "";
            //End Fix
            return arguments;
        }

    }

    public class EngineInstance
    {
        public EngineInstanceStartupParameters StartupParameters { get; protected set; }
        public String IpEndpoint = "";
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
