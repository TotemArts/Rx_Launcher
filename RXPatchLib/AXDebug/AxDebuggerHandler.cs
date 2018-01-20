using System;
using System.Linq;

namespace RXPatchLib.AXDebug
{
    class AxDebuggerHandler
    {

        // Instance Handler
        private static AxDebuggerHandler _instance;
        public static AxDebuggerHandler Instance = _instance ?? (_instance = new AxDebuggerHandler());

        // Form Container
        private readonly FrmAgnDebug _frmAgnDebug = new FrmAgnDebug();

        public AxDebuggerHandler()
        {
            if ( Environment.GetCommandLineArgs().Any(x => x.Equals("--log", StringComparison.OrdinalIgnoreCase)) )
                _frmAgnDebug.Show();
        }

        public void AddDownload(Guid guid, string filepath, string serverUri)
        {
            _frmAgnDebug.AddDownload(guid, filepath, serverUri);
        }

        public void RemoveDownload(Guid guid)
        {
            _frmAgnDebug.RemoveDownload(guid);
        }

        public void UpdateDownload(Guid guid, long progress, long fileSize)
        {
            _frmAgnDebug.UpdateDownload(guid, progress, fileSize);
        }

        public void Dispose()
        {
            _frmAgnDebug.Close();
        }
    }
}
