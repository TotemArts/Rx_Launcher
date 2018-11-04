using System.Diagnostics;
using System.IO;
using System.Linq;
using RxLogger;

namespace LauncherTwo
{
    /// <summary>
    /// InstanceHandler class
    /// This class attempts to find other running instances of the launcher running in the same directory.
    /// </summary>
    static class InstanceHandler
    {
        public static bool IsAnotherInstanceRunning()
        {
            try
            {
                var currentRunningApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                var currentRunningApplicationName = Path.GetFileName(currentRunningApplicationPath).Replace(".exe", "");
                var currentProc = Process.GetCurrentProcess();

                Logger.Instance.Write("Attempting to find other processes running which are the same as me");

                var processList = Process.GetProcessesByName(currentRunningApplicationName);
                Logger.Instance.Write($"Found {processList.Length} processes which are possibly like me");

                // If we have another process that does not have the same PID and is running from the same directory, then we have a match
                return processList.Any(process => process.MainModule.FileName == currentRunningApplicationPath && process.Id != currentProc.Id);
            } catch { 
                return false;
            }
        }

       /// <summary>
       /// Kills a "ghosted" process if the process is from the same directory as the current instance
       /// Helps with updater errors when a file lock is open on a file attempting to be updated.
       /// </summary>
        public static void KillDuplicateInstance()
        {
            try
            {
                var currentRunningApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                var currentRunningApplicationName = Path.GetFileName(currentRunningApplicationPath).Replace(".exe", "");
                var currentProc = Process.GetCurrentProcess();

                var processList = Process.GetProcessesByName(currentRunningApplicationName);

                // If we have another process that does not have the same PID and is running from the same directory, then we have a match
                var ourProcess = processList.DefaultIfEmpty(null).FirstOrDefault(x => x.MainModule.FileName == currentRunningApplicationPath && x.Id != currentProc.Id);
                ourProcess?.Kill();
            }
            catch
            {
            }
        }
    }
}
