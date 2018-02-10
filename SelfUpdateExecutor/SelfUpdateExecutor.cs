using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;
using System.IO;

namespace SelfUpdateExecutor
{
    class SelfUpdateExecutor
    {
        /** Constants */
        const int MillisecondsToWait = 10000; // 10 seconds
        const string LauncherExeFilename = "\\Renegade X Launcher.exe";
        const string TargetPathSwitch = "--target=";
        const string ProcessIDSwitch = "--pid=";

        /**
         * SelfUpdateExecutor.exe entry point
         * 
         * @param args Command-line arguments passed to SelfUpdateExecutor.exe by the Renegade X Launcher.
         */
        static void Main(string[] args)
        {
            // Setup default values
            string sourcePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string targetPath = "";
            int processId = 0;

            // Process command line arguments
            foreach (string arg in args)
            {
                if (arg.StartsWith(TargetPathSwitch))
                {
                    // Process targetPath
                    targetPath = arg.Substring(TargetPathSwitch.Length);
                    if (targetPath.EndsWith("\\") || targetPath.EndsWith("/"))
                    {
                        targetPath = targetPath.Substring(0, targetPath.Length - 1);
                    }
                }
                else if (arg.StartsWith(ProcessIDSwitch))
                {
                    // Process processId
                    processId = Int32.Parse(arg.Substring(ProcessIDSwitch.Length));
                }
                else
                {
                    Error("Unknown argument: " + arg);
                }
            }

            // Execute update
            if (sourcePath != "" && targetPath != "" && processId != 0)
            {
                execute(sourcePath, targetPath, processId);
            }
        }

        /**
         * Represents update success/failure
         */
        enum SelfUpdateStatus
        {
            Success = 0,
            InvalidArguments = 1,
            KillFailure = 2,
            DeletePermissionFailure = 3,
            MovePermissionFailure = 4,
            DirectoryMissingFailure = 5,
            UnhandledException = 6,
            UnknownError = 7
        }

        /**
         * Logs a log message to stdout
         * 
         * @param message Log message to write to stdout
         */
        static void Log(string message)
        {
            Console.Out.WriteLine(message);
        }

        /**
         * Logs an error to stderr
         * 
         * @param message Error message to write to stderr
         */
        static void Error(string message)
        {
            Console.Error.WriteLine(message);
        }

        /**
         * Executes a launcher update by attempting to apply a launcher update, then restarting the launcher
         * 
         * @param sourcePath Path of the new launcher binaries
         * @param targetPath Path of the current launcher binaries
         * @param processId Process ID of the launcher which blocks us from updating
         */
        static void execute(string sourcePath, string targetPath, int processId)
        {
            string backupPath = targetPath + "_removeme";

            // Apply launcher self-update
            SelfUpdateStatus status = SelfUpdateStatus.UnhandledException;
            try
            {
                status = apply(sourcePath, targetPath, backupPath, processId);

                if (status == SelfUpdateStatus.Success)
                {
                    // Patch applied successfully; delete backup
                    deleteDirectory(backupPath);
                }
                else if (status == SelfUpdateStatus.MovePermissionFailure) {
                    // Patch failed to apply; try to move backup back (if it exists)
                    SelfUpdateStatus restoreBackupResult = moveDirectory(backupPath, targetPath);
                    if (restoreBackupResult != SelfUpdateStatus.Success // Backup failed to restore
                        && restoreBackupResult != SelfUpdateStatus.DirectoryMissingFailure) // Backup existed
                    {
                        // The launcher is stuck in the backup directory. We're up shit creek, and won't be able to restart the launcher. This shouldn't ever happen.
                        throw new Exception("Backup failed to restore; this should never happen");
                    }
                }
            }
            catch (Exception e)
            {
                Error("Unhandled exception: " + e.ToString());
                return;
            }

            // Startup new launcher; failure is irresolvable
            Process.Start(targetPath + LauncherExeFilename, "--patch-result=" + status);
        }

        /**
         * Applies a launcher update
         * 
         * @param sourcePath Path of the new launcher binaries
         * @param targetPath Path of the current launcher binaries
         * @param backupPath Path to backup current launcher binaries to
         * @param processId Process ID of the launcher which blocks us from updating
         * @return A status code indicating Success, or some error
         */
        static SelfUpdateStatus apply(string sourcePath, string targetPath, string backupPath, int processId)
        {
            SelfUpdateStatus result;

            // Wait for launcher to close (failure is fatal)
            result = waitForProcess(processId);
            if (result != SelfUpdateStatus.Success)
            {
                return result;
            }

            // Clean up possible left behind files from previous installation attempt (failure is fatal)
            result = deleteDirectory(backupPath);
            if (result != SelfUpdateStatus.Success)
            {
                return result;
            }

            // Move away old version (failure is fatal)
            result = moveDirectory(targetPath, backupPath);
            if (result != SelfUpdateStatus.Success)
            {
                return result;
            }

            // Move new launcher to target (failure is fatal; this shouldn't ever fail)
            result = moveDirectory(sourcePath, targetPath);
            if (result != SelfUpdateStatus.Success)
            {
                return result;
            }

            // Delete old launcher (failure is non-fatal; this shouldn't ever fail)
            deleteDirectory(backupPath);

            return SelfUpdateStatus.Success;
        }

        /**
         * Waits 10 seconds for a process to close gracefully, and then kills the process afterwards if it never closed.
         * 
         * @param processId Process ID of the task to wait on
         * @return KillFailure if the task never ends and we are unable to kill it, Success otherwise
         */
        static SelfUpdateStatus waitForProcess(int processId)
        {
            // Wait for launcher to close (failure is fatal)
            try
            {
                Log("Waiting for launcher to close...");
                var process = Process.GetProcessById(processId);
                if (!process.WaitForExit(MillisecondsToWait))
                {
                    Log("Launcher hasn't closed gracefully; killing launcher process...");
                    // Process failed to exit gracefully; murder it
                    process.Kill();
                }
            }
            catch (ArgumentException) { } // Process doesn't exist; already closed
            catch (InvalidOperationException) { } // Process doesn't exist; already closed
            catch (Win32Exception e) // Process couldn't be killed; update failed
            {
                Error("Unable to kill launcher process: " + e.ToString());
                return SelfUpdateStatus.KillFailure;
            }

            // Process has ended successfully
            Log("Launcher closed; applying update...");
            return SelfUpdateStatus.Success;
        }

        /**
         * Deletes a directory, and all of the files in it
         * 
         * @param directory Directory to delete
         * @return DeletePermissionFailure on permission failure, Success otherwise
         */
        static SelfUpdateStatus deleteDirectory(string directory)
        {
            try
            {
                // Delete the directory
                Directory.Delete(directory, true);
            }
            catch (DirectoryNotFoundException) { } // Directory does not exist
            catch (UnauthorizedAccessException) // We don't have permission to delete the directory
            {
                return SelfUpdateStatus.DeletePermissionFailure;
            }
            catch (IOException) // Some other error
            {
                try
                {
                    File.Delete(directory); // Try deleting it as a file
                }
                catch
                {
                    return SelfUpdateStatus.DeletePermissionFailure; // We failed due to some obscure permissions issue
                }
            }

            // Directory deleted successfully
            return SelfUpdateStatus.Success;
        }

        /**
         * Moves a directory from one place to another
         * 
         * @param source Directory to move
         * @param target Target to move directory to
         * @return MovePermissionFailure if a permission issue occurs, DirectoryMissingFailure if the directory doesn't exist, Success otherwise
         */
        static SelfUpdateStatus moveDirectory(string source, string target)
        {
            try
            {
                // Move the directory
                Directory.Move(source, target);
            }
            catch (UnauthorizedAccessException) // We don't have permission to move the directory
            {
                return SelfUpdateStatus.MovePermissionFailure;
            }
            catch (DirectoryNotFoundException) // We're trying to move something that doesn't exist
            {
                return SelfUpdateStatus.DirectoryMissingFailure;
            }

            return SelfUpdateStatus.Success;
        }
    }
}
