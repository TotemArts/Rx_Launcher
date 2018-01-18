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
        public string Name;
        public bool IsUsed;
        public bool HasErrored;
        public string WebPatchPath;
        public long Latency;

        public UpdateServerEntry(string url, string name)
        {
            Uri = new Uri(url);
            Name = name;
        }
    }

    /// <summary>
    /// Handles all Update Server URLs and Friendly Names, including getting the best latency server
    /// </summary>
    public class UpdateServerHandler
    {
        private List<UpdateServerEntry> _updateServers = new List<UpdateServerEntry>();

        public void AddUpdateServer(string url, string friendlyName)
        {
            try
            {
                _updateServers.Add(new UpdateServerEntry(url, friendlyName));
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
            
            RxLogger.Logger.Instance.Write($"Latency for {entry.Uri.AbsoluteUri} ({entry.Name}) is {entry.Latency}");
        }

        public Task PreformLatencyTest()
        {
            // Find out the best latency server to respond with
            Logger.Instance.Write("Figuring out what server is the best for you with latency checking... wait.");
            foreach (var entry in _updateServers.Where(x => x.Latency == 0 && !x.IsUsed && !x.HasErrored))
                GetServerLatency(entry);

            _updateServers = _updateServers.OrderBy(x => x.Latency).ToList();

            Logger.Instance.Write($"Best server found, in order they are:\r\n{string.Join("\r\n", _updateServers.Select(x => $"{x.Name} | {x.Latency}"))}");

            return Task.FromResult<object>(null);
        }
    }
}
