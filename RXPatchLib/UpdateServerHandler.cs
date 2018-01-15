using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public long Latency;

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
        private List<UpdateServerEntry> _updateServers = new List<UpdateServerEntry>();

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
            // Find out the best latency server to respond with
            foreach (var entry in _updateServers.Where(x => x.Latency == 0 && !x.IsUsed && !x.HasErrored))
            {
                GetServerLatency(entry);
            }

            _updateServers = _updateServers.OrderBy(x => x.Latency).ToList();

            return _updateServers.DefaultIfEmpty(null).FirstOrDefault(x => !x.HasErrored && !x.IsUsed);
        }

        /// <summary>
        /// Tests the latency of the remote server for the user
        /// </summary>
        /// <param name="entry">The UpdateServerEntry object to test against</param>
        /// <returns>Returns the latency as a float</returns>
        private void GetServerLatency(UpdateServerEntry entry)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create($"{entry.Uri.AbsoluteUri}/10kb_file");
            request.Method = "GET";
            request.Timeout = 1500;

            //Default to "not found"
            System.Net.HttpStatusCode response = System.Net.HttpStatusCode.NotFound;
            try
            {
                response = ((System.Net.HttpWebResponse) request.GetResponse()).StatusCode;
            }
            catch(Exception ex)
            {
                entry.HasErrored = true;
            }

            // Push host to queue if valid
            if (response != System.Net.HttpStatusCode.OK) return;

            stopWatch.Stop();
            entry.Latency = stopWatch.ElapsedMilliseconds;
            Console.WriteLine($"Latency for {entry.Uri.AbsoluteUri} ({entry.FriendlyName}) is {entry.Latency}");
        }
    }
}
