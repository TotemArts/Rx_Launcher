using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RxLogger
{
    public class Logger
    {
        // Instancing of this class
        private static Logger _instance;
        public static Logger Instance = _instance ?? (_instance = new Logger());

        private readonly object _lockable = new object();

        // AllocConsole is needed
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        // Holds the full file path
        private readonly string _fullPath;

        // Is a debug console loaded for this session?
        private bool _hasConsole;

        public enum ErrorLevel
        {
            ErrSuccess,
            ErrInfo,
            ErrWarning,
            ErrError
        }

        /// <summary>
        /// Sets up the logger interface to use a standardised file name and path
        /// </summary>
        public Logger()
        {
            try
            {
                var filePath = $"{Environment.GetEnvironmentVariable("APPDATA")}\\Renegade-X Launcher";
                var fileName = $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}_Application.log";
                _fullPath = $"{filePath}\\{fileName}";

                if (!System.IO.Directory.Exists(filePath))
                    System.IO.Directory.CreateDirectory(filePath);
   
                InitLog();
                CleanUp(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Opens the debug console (exactly like UDK's -log parameter)
        /// </summary>
        public void StartLogConsole()
        {
            AllocConsole();
            _hasConsole = true;
        }

        /// <summary>
        /// Simple function that inits the log file by printing a header to the log
        /// </summary>
        private void InitLog()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            System.IO.File.WriteAllText(_fullPath,
                // ReSharper disable once LocalizableElement
                $"Starting Renegade-X Launcher Log\r\n\tApp Version:{version}\r\n\tBinary Path:{assembly.Location}\r\n\r\n");
        }

        /// <summary>
        /// Writes a line to the log file, including where the original call came from, the file path and the line number
        /// </summary>
        /// <param name="message">The message you want to save to the file</param>
        /// <param name="errorLevel">The error level of the current write operation (INFO by default)</param>
        /// <param name="callingMethod">This should be ignored as the compiler sets this parameter at run time</param>
        /// <param name="callingFilePath">This should be ignored as the compiler sets this parameter at run time</param>
        /// <param name="callingFileLineNumber">This should be ignored as the compiler sets this parameter at run time</param>
        public void Write(string message, ErrorLevel errorLevel = ErrorLevel.ErrInfo,
            [CallerMemberName] string callingMethod = "",
            [CallerFilePath] string callingFilePath = "",
            [CallerLineNumber] int callingFileLineNumber = 0)
        {
            lock (_lockable)
            {
                // ReSharper disable once LocalizableElement
                System.IO.File.AppendAllText(_fullPath,
                    $"[{DateTime.Now:dd-MM-yyyy_HH-mm-ss}] | [{callingMethod} @ Line {callingFileLineNumber} In {System.IO.Path.GetFileName(callingFilePath)} Thread {Thread.CurrentThread.ManagedThreadId}] | {errorLevel.ToString()} - {message}\r\n");
                if (_hasConsole)
                {
                    switch (errorLevel)
                    {
                        case ErrorLevel.ErrError:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;

                        case ErrorLevel.ErrInfo:
                        case ErrorLevel.ErrSuccess:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;

                        case ErrorLevel.ErrWarning:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                    }

                    // ReSharper disable once LocalizableElement
                    Console.WriteLine(
                        $"[{callingMethod} @ Line {callingFileLineNumber} In {System.IO.Path.GetFileName(callingFilePath)} Thread {Thread.CurrentThread.ManagedThreadId}] - {message}");
                }
            }
        }

        /// <summary>
        /// Cleans up the log folder by removing older logfiles.
        /// </summary>
        /// <param name="path">The path to cleanup</param>
        /// <param name="amountToKeep">Amount of logfiles to keep. Default is 6</param>
        private void CleanUp(string path, int amountToKeep = 6)
        {
            Write("Starting logfile cleanup.");
            try
            {
                //Grab all the files in the directory as FileInfo objects
                System.IO.FileInfo[] files = new System.IO.DirectoryInfo(path).GetFiles();

                //Delete potential bogus files by a simple extension check (Maybe a bit redundant?)
                var bogusFiles = from file in files where !file.Extension.Equals(".log") select file;

                if (bogusFiles.Any())
                {
                    foreach (System.IO.FileInfo bogusFile in bogusFiles)
                    {
                        bogusFile.Delete();
                        Write($"Deleted bogus logfile: {bogusFile.Name}.");
                    }
                    //Re-Read directory now bogusFiles are gone
                    files = new System.IO.DirectoryInfo(path).GetFiles();
                }

                //Reverse them so we have the newest at [0]
                Array.Reverse(files);
                if (files.GetLength(0) > amountToKeep)
                {
                    //Delete files > amountToKeep
                    int fileCounter = 0;
                    for (int i = amountToKeep; i < files.GetLength(0); i++, fileCounter++)
                    {
                        files[i].Delete();
                        Write($"Deleted logfile: {files[i].Name}");
                    }
                    Write($"Logfile cleanup done. Removed {fileCounter} files.");
                }
                else
                    Write($"Logfile cleanup done. No logfiles removed.");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
