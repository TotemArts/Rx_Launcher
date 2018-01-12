using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{

    /// <summary>
    /// Contains a server entry that eventually sits inside a List array
    /// </summary>
    public class UpdateServerEntry
    {
        public Uri Uri;
        public string FriendlyName;
        public bool IsUsed;
        public bool HasErrored;
        public string WebPatchPath;

        public UpdateServerEntry(string Url, string FriendlyName)
        {
            Uri = new Uri(Url);
            this.FriendlyName = FriendlyName;
        }
    }

    /// <summary>
    /// Handles all Update Server URLs and Friendly Names, including getting the best latency server
    /// </summary>
    internal class UpdateServerHandler
    {
        private readonly List<UpdateServerEntry> _updateServers = new List<UpdateServerEntry>();

        public void AddUpdateServer(string Url, string FriendlyName)
        {
            try
            {
                _updateServers.Add(new UpdateServerEntry(Url, FriendlyName));
            }
            catch
            {
                // ignored
            }
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
            return _updateServers.DefaultIfEmpty(null).FirstOrDefault(x => !x.HasErrored && !x.IsUsed);
        }
    }
}
