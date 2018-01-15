using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RXPatchLib
{
    /// <summary>
    /// Contains a server entry that eventually sits inside a List array
    /// </summary>
    public class UpdateServerEntry
    {
        public Uri Uri;
        public string Name;
        public bool IsUsed;
        public bool HasErrored;

        public UpdateServerEntry(string Url, string name)
        {
            Uri = new Uri(Url);
            Name = name;
        }
    }

    /// <summary>
    /// Handles all Update Server URLs and Friendly Names, including getting the best latency server
    /// </summary>
    public class UpdateServerHandler
    {
        private readonly List<UpdateServerEntry> _updateServers = new List<UpdateServerEntry>();
        private UpdateServerEntry _lastBestServerEntry;

        public void AddUpdateServer(string Url, string FriendlyName)
        {
            _updateServers.Add(new UpdateServerEntry(Url, FriendlyName));
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
            if (_lastBestServerEntry != null)
                return _lastBestServerEntry;

            _lastBestServerEntry = _updateServers.DefaultIfEmpty(null).FirstOrDefault(x => !x.HasErrored && !x.IsUsed);
            return _lastBestServerEntry;
        }
    }
}
