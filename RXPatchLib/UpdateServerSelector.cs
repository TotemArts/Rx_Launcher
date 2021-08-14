/*
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
    public class UpdateServerSelector
    {
        CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        int? bestHostIndex = null;
        long bestHostRtt = 0;
        object bestHostLock = new object();

        /// <summary>
        /// Check if the host is reachable by loading the patch subfolder
        /// If host is reachable, ping to determine the best connection
        /// </summary>
        /// <param name="fullHost">An Uri containing the full host path e.g.: "http://rxp-nyc.cncirc.net/Patch5282b/"</param>
        /// <param name="index">The index of this host in the main hostArray</param>
        /// <returns>A task object with no usefull data other than task info</returns>
        async Task CheckAndPingHost(Uri fullHost, int index)
        {
            //Try getting a response from the desired patch folder on the host
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(fullHost);
            request.Method = "GET";
            //Default to "not found"
            System.Net.HttpStatusCode response = System.Net.HttpStatusCode.NotFound;
            try
            {
                response = ((System.Net.HttpWebResponse)request.GetResponse()).StatusCode; //This needs to become async... For now it will do
            }
            catch
            {
                Trace.WriteLine(string.Format("<!><!><!>The host: {0} is down.", fullHost));
            }

            //If host response from the desired patch folder on the host is OK
            //Ping the host and determine the best RoundTripTime
            //Else -> Don't use this host and don't ever set this index as the desired index
            if (response == System.Net.HttpStatusCode.OK)
            {
                try
                {
                    using (var ping = new Ping())
                    {
                        var reply = await ping.SendPingAsync(fullHost.Host, 5000, CancellationTokenSource.Token);
                        Trace.WriteLine(string.Format("Ping to {0}: {1}.", fullHost, reply.RoundtripTime));
                        lock (bestHostLock)
                        {
                            if (!bestHostIndex.HasValue || reply.RoundtripTime < bestHostRtt)
                            {
                                bestHostRtt = reply.RoundtripTime;
                                bestHostIndex = index;
                            }
                        }
                    }
                }
                catch (PingException)
                {
                    Trace.WriteLine(string.Format("Ping to {0} failed.", fullHost));
                }
                catch (OperationCanceledException)
                {
                    Trace.WriteLine(string.Format("Ping to {0} canceled.", fullHost));
                }
            }

        }

        /// <summary>
        /// Select the host with the lowest ping.
        /// 
        /// Allows all hosts at least 500 ms to reply. After 500 ms (earlier if possible), the best server is selected.
        /// Allows all hosts to reply within 100 ms after the best host replied, to avoid incorrect results due to scheduling. (Somewhat pedantic.)
        /// If no pings were received within the default system timeout, a random server is selected.
        /// </summary>
        /// <param name="fullHosts"></param>
        /// <returns></returns>
        public async Task<int> SelectHostIndex(ICollection<Uri> fullHosts)
        {
            Contract.Assume(fullHosts.Count > 0);

            //If there is only one host, return index 0
            if (fullHosts.Count == 1)
            {
                return 0;
            }

            //Ping and statuscheck all Hosts and determine the best host to download from
            Task[] pingTasks = fullHosts.Select((host, index) => CheckAndPingHost(host, index)).ToArray();
            await Task.WhenAll(pingTasks).ProceedAfter(500);
            foreach (var task in pingTasks)
            {
                task
                    .ContinueWith((result) =>
                    {
                        CancellationTokenSource.CancelAfter(100);
                    }, TaskContinuationOptions.OnlyOnRanToCompletion)
                    .Forget();
            }

            await Task.WhenAll(pingTasks).ProceedIfCanceled().CancelAfter(5000);
            if (bestHostIndex == null)
            {
                throw new Exception("Could not select a reliable downloadserver. Please try again later...");
            }

            return (int)bestHostIndex;// ?? new Random().Next(hosts.Count); //This random needs to change. It can select a dead server!
        }
    }
}
*/