using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RxLogger;

namespace RXPatchLib
{
    /// <summary>
    /// Contains a server entry that eventually sits inside a List array
    /// </summary>
    public class UpdateServerEntry
    {
        public Uri Uri;
        public bool IsUsed;
        public bool HasErrored;

        public UpdateServerEntry(string url)
        {
            Uri = new Uri(url);
        }
    }

    /// <summary>
    /// Handles all Update Server URLs and Friendly Names, including getting the best latency server
    /// </summary>
    public class UpdateServerHandler
    {
        private List<UpdateServerEntry> _updateServers = new List<UpdateServerEntry>();

        public void AddUpdateServer(string url)
        {
            _updateServers.Add(new UpdateServerEntry(url));

        }

        public List<UpdateServerEntry> GetUpdateServers()
        {
            return _updateServers;
        }

        /// <summary>
        /// Selects the best patch server in the list that both is not in use, and has not errored
        /// </summary>
        /// <returns>An UpdateServerEntry of the host found, or Null if no more hosts exist</returns>
        public UpdateServerEntry SelectBestPatchServer()
        {
            lock (_updateServers)
            {
                var thisServerEntry =
                    _updateServers.DefaultIfEmpty(null).FirstOrDefault(x => !x.HasErrored && !x.IsUsed);
                if (thisServerEntry != null)
                {
                    thisServerEntry.IsUsed = true; // Mark is as used so it's not used again
                    return thisServerEntry;
                }

                return null;
            }
        }
    }
}
