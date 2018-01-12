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
    }
}
