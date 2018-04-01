using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
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
        CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private const string TestFile = "10kb_file";
        public Queue<UpdateServerEntry> Hosts;
        private readonly List<UpdateServerSelectorObject> CurrentHostsList = new List<UpdateServerSelectorObject>();
        private const int ServerRecordsToTake = 4;

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

                // Take the top 4 servers
                var selectedServers = CurrentHostsList.Take(ServerRecordsToTake).ToList();
                if (selectedServers.Count > 0)
                {
                    // Order them by connection count and take the top one off the pile
                    var selectedServer = selectedServers.OrderBy(x => x.ConnectionCount).DefaultIfEmpty(null).FirstOrDefault();

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

            return null;
        }

        public async Task<bool> QueryHost(UpdateServerEntry hostObject)
        {
            RxLogger.Logger.Instance.Write($"Attempting to contact host {hostObject.Uri.AbsoluteUri}");

            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(hostObject.Uri.AbsoluteUri + TestFile);
            request.Method = "GET";

            //Default to "not found"
            System.Net.HttpStatusCode response = System.Net.HttpStatusCode.NotFound;
            try
            {
                response = ((System.Net.HttpWebResponse)await request.GetResponseAsync()).StatusCode;
            }
            catch
            {
                hostObject.HasErrored = true;
                RxLogger.Logger.Instance.Write($"The host {hostObject.Uri.AbsoluteUri} seems to be offline");
            }

            // Push host to queue if valid
            if (response == System.Net.HttpStatusCode.OK)
            {
                lock (Hosts)
                {
                    Hosts.Enqueue(hostObject);

                    // We can keep track of the list via this, including connection count
                    CurrentHostsList.Add(new UpdateServerSelectorObject(hostObject));
                }

                RxLogger.Logger.Instance.Write($"Added host {hostObject.Uri.AbsoluteUri} to the hosts queue");

                return true;
            }

            return false;
        }

        public async Task SelectHosts(List<UpdateServerEntry> inHosts)
        {
            // Safety check
            if (inHosts.Count == 0)
                throw new Exception("No download servers are available; please try again later.");

            // Initialize new Hosts queue
            Hosts = new Queue<UpdateServerEntry>();

            // Initialize query to each host
            List<Task<bool>> tasks = inHosts.Select(QueryHost).ToList();

            // Return when we have our best host; continue populating list in background
            while (tasks.Any())
            {
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);

                // Good mirror found; return result
                if (task.Result)
                    return;
            }

            // No host found; throw exception
            throw new Exception("Could not select a reliable download server. Please try again later.");
        }
    }
}