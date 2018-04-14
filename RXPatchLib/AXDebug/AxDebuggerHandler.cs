using System;
using System.Linq;

namespace RXPatchLib.AXDebug
{
    /// <summary>
    /// The Concurrent Download Debug Handler
    ///     This class handles the instanced version of the Concurrent Downloading Debug window (FrmAGNDebug)
    ///     This must be instanced, otherwise a new form would load for every download that starts, and that would suck
    /// </summary>
    class AxDebuggerHandler
    {

        // Instance Handler
        private static AxDebuggerHandler _instance;
        public static AxDebuggerHandler Instance = _instance ?? (_instance = new AxDebuggerHandler());

        // Form Container
        private readonly FrmAgnDebug _frmAgnDebug = new FrmAgnDebug();
        private readonly bool _isFormLoaded;

        /// <summary>
        /// This will only show the download debug form if --log is in the startup parameter of the application
        /// </summary>
        public AxDebuggerHandler()
        {
            if (Environment.GetCommandLineArgs().Any(x => x.Equals("--log", StringComparison.OrdinalIgnoreCase)))
            {
                _frmAgnDebug.Show();
                _isFormLoaded = true;
            }
        }

        /// <summary>
        /// Adds a new download to the debug dialog
        /// </summary>
        /// <param name="guid">A unique reference for this download, to be created by you before calling this</param>
        /// <param name="filepath">The file path that is dispalyed on the debug window</param>
        /// <param name="serverUri">The server URL that it's downloading from</param>
        public void AddDownload(Guid guid, string filepath, string serverUri)
        {
            if ( _isFormLoaded) _frmAgnDebug.AddDownload(guid, filepath, serverUri);
        }

        /// <summary>
        /// Removes a download from the debug window
        /// </summary>
        /// <param name="guid">The GUID you passed in during AddDownload</param>
        public void RemoveDownload(Guid guid)
        {
            if (_isFormLoaded) _frmAgnDebug.RemoveDownload(guid);
        }

        /// <summary>
        /// Updates a download in the debug window
        /// </summary>
        /// <param name="guid">The GUID you passed in during AddDownload</param>
        /// <param name="progress">The current progress from the WebClient OnDownloadProgressed event</param>
        /// <param name="fileSize">The current total file size from the WebClient OnDownloadProgressed event</param>
        public void UpdateDownload(Guid guid, long progress, long fileSize)
        {
            if (_isFormLoaded) _frmAgnDebug.UpdateDownload(guid, progress, fileSize);
        }

        /// <summary>
        /// Disposes of the debug window
        /// </summary>
        public void Dispose()
        {
            if (_isFormLoaded) _frmAgnDebug.Close();
        }
    }
}
