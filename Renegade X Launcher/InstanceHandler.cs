// AX
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LauncherTwo
{
    /*
     * InstanceHandler class
     * What Does It Do
     *  This class attempts to find other running instances of the launcher running in the same directory.
     */
    static class InstanceHandler
    {
        public static bool IsAnotherInstanceRunning()
        {
            try
            {
                var CurrentRunningApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                var CurrentRunningApplicationName = Path.GetFileName(CurrentRunningApplicationPath).Replace(".exe", "");
                var CurrentProc = Process.GetCurrentProcess();

                var processList = Process.GetProcessesByName(CurrentRunningApplicationName);

                // If we have another process that does not have the same PID and is running from the same directory, then we have a match
                return processList.Any(process => process.MainModule.FileName == CurrentRunningApplicationPath && process.Id != CurrentProc.Id);
            } catch { 
                return false;
            }
        }

        /*
         * Kills a "ghosted" process if the process is from the same directory as the current instance
         *  Helps with updater errors when a file lock is open on a file attempting to be updated.
         */
        public static void KillDuplicateInstance()
        {
            try
            {
                var CurrentRunningApplicationPath = System.Reflection.Assembly.GetEntryAssembly().Location;
                var CurrentRunningApplicationName = Path.GetFileName(CurrentRunningApplicationPath).Replace(".exe", "");
                var CurrentProc = Process.GetCurrentProcess();

                var processList = Process.GetProcessesByName(CurrentRunningApplicationName);

                // If we have another process that does not have the same PID and is running from the same directory, then we have a match
                var ourProcess = processList.DefaultIfEmpty(null).FirstOrDefault(x => x.MainModule.FileName == CurrentRunningApplicationPath && x.Id != CurrentProc.Id);
                ourProcess?.Kill();
            }
            catch
            {
            }
        }
    }
}
