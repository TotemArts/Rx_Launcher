using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RXPatchLib
{

    public class UpdateServerSelectorObject
    {
        public UpdateServerEntry UpdateServer; // stores the update server object for the server in question
        public int ConnectionCount; //Stores the amount of times this server was used, it's used for selecting the next best server

        public UpdateServerSelectorObject(UpdateServerEntry updateServerEntry)
        {
            UpdateServer = updateServerEntry;
        }
    }

    public class UpdateServerSelector
    {
        private const string TestFile = "10kb_file";
        public readonly Queue<UpdateServerEntry> Hosts = new Queue<UpdateServerEntry>();
        private readonly List<UpdateServerSelectorObject> CurrentHostsList = new List<UpdateServerSelectorObject>();
        private List<HttpWebRequest> CurrentConnections = new List<HttpWebRequest>();

        /// <summary>
        /// Gets the next UpdateServerEntry that has the least amount of connections to it
        /// </summary>
        /// <returns></returns>
        public UpdateServerEntry GetNextAvailableServerEntry()
        {
            lock (CurrentHostsList)
            {
                if (CurrentHostsList.Count == 0)
                    return null;

                // Remove any server that has errored
                CurrentHostsList.RemoveAll(server => server.UpdateServer.HasErrored);

                // If we have ran out of hosts, then return null.
                if (CurrentHostsList.Count == 0)
                    return null;

                // Order them by connection count and take the top one off the pile
                UpdateServerSelectorObject selectedServer = CurrentHostsList.OrderByDescending(x => x.ConnectionCount).FirstOrDefault();
                
                // If we didnt get null, ++ connection count and return it, otherwise return null
                if (selectedServer != null)
                {
                    selectedServer.ConnectionCount++;

                    RxLogger.Logger.Instance.Write(
                        $"I have picked the server {selectedServer.UpdateServer.Uri.AbsoluteUri} as it has only {selectedServer.ConnectionCount} connections against it");

                    return selectedServer.UpdateServer;
                }

                // Server selection failed, return null
                return null;
            }
        }

        private async Task<bool> QueryHost(UpdateServerEntry hostObject)
        {
            RxLogger.Logger.Instance.Write($"Attempting to contact host {hostObject.Uri.AbsoluteUri}.");
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(hostObject.Uri.AbsoluteUri + TestFile);
            request.Method = "GET";
            request.Timeout = 20000; // max wait time = 20 sec

            // Default to "not found"
            HttpStatusCode responseCode = HttpStatusCode.NotFound;
            CurrentConnections.Add(request);
            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                responseCode = response.StatusCode;
            }
            catch
            {
                hostObject.HasErrored = true;
                RxLogger.Logger.Instance.Write($"The host {hostObject.Uri.AbsoluteUri} seems to be offline.");
            }
            CurrentConnections.Remove(request);

            // Push host to queue if valid
            if (responseCode == HttpStatusCode.OK)
            {
                lock (Hosts)
                {
                    Hosts.Enqueue(hostObject);

                    // We can keep track of the list via this, including connection count
                    CurrentHostsList.Add(new UpdateServerSelectorObject(hostObject));
                }

                RxLogger.Logger.Instance.Write($"Added host {hostObject.Uri.AbsoluteUri} to the hosts queue.");

                return true;
            }

            return false;
        }

        public async Task SelectHosts(List<UpdateServerEntry> inHosts)
        {
            // Safety check
            if (inHosts.Count == 0)
                throw new Exception("No download servers are available; please try again later.");
            
            // Initialize query to each host
            List<Task<bool>> tasks = inHosts.Select(QueryHost).ToList();

            // Return when we have our best host; continue populating list in background
            while (tasks.Any())
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);

                // Good mirror found; return result
                // Leave other tasks to finish on their own
                if (task.Result) {
                    return;
                }
            }

            // No host found; throw exception
            throw new Exception("Could not select a reliable download server. Please try again later.");
        }

        public void Dispose()
        {
            Hosts.Clear();
            CurrentHostsList.Clear();

            if (CurrentConnections.Count > 0) {
                for (int i=0; i < CurrentConnections.Count; i++) {
                    RxLogger.Logger.Instance.Write($"Aborting connection to host {CurrentConnections[i].Host}...");
                    CurrentConnections[i].Abort();
                }
                CurrentConnections.Clear();
            }
        }
    }
}