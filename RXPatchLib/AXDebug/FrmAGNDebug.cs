using System;
using System.Linq;
using System.Windows.Forms;

namespace RXPatchLib.AXDebug
{
    public partial class FrmAgnDebug : Form
    {
        public FrmAgnDebug()
        {
            InitializeComponent();
        }

        /*
         * For more information on these methods, please refer to the AxDebuggerHandler.cs file
         */
        public void AddDownload(Guid guid, string filepath, string serverUri)
        {
            if ( InvokeRequired )
                Invoke(new MethodInvoker(() => AddDownload(guid, filepath, serverUri)));

            lock (lstDownloads)
            {
                string[] clm = {guid.ToString(), filepath, serverUri, "Download Pending", "0"};
                lstDownloads.Items.Add(new ListViewItem(clm) {Name = guid.ToString()});
            }
        }

        public void RemoveDownload(Guid guid)
        {
            if (InvokeRequired)
                Invoke(new MethodInvoker(() => RemoveDownload(guid)));

            lock (lstDownloads)
            {
                var clm = FindListViewItemByName(guid.ToString());
                if (clm != null)
                    lstDownloads.Items.Remove(clm);
            }
        }

        public void UpdateDownload(Guid guid, long progress, long fileSize)
        {
            if (InvokeRequired)
                Invoke(new MethodInvoker(() => UpdateDownload(guid, progress, fileSize)));

            lock (lstDownloads)
            {
                var clm = FindListViewItemByName(guid.ToString());
                if (clm != null)
                {
                    clm.SubItems[4].Text =
                        $@"{GetHumanReadableFileSize(progress)}/{GetHumanReadableFileSize(fileSize)}";
                    clm.SubItems[3].Text = "Downloading";
                }
            }
        }

        private ListViewItem FindListViewItemByName(string name)
        {
            if (InvokeRequired)
                Invoke(new MethodInvoker(() => FindListViewItemByName(name)));

            lock (lstDownloads)
            {
                try
                {
                    var retDownload = lstDownloads.Items.Find(name, true).DefaultIfEmpty(null).FirstOrDefault();
                    return retDownload;
                }
                catch
                {
                    // ignored
                }

                return null;
            }
        }

        private string GetHumanReadableFileSize(long input)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = input;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
