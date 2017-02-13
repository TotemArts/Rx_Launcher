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

        private const string TestFile = "10kb_file";
        public Queue<Uri> Hosts;

        public async Task<bool> QueryHost(Uri InHost)
        {
            // Send GET request to host
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(InHost + TestFile);
            request.Method = "GET";

            //Default to "not found"
            System.Net.HttpStatusCode response = System.Net.HttpStatusCode.NotFound;
            try
            {
                response = ((System.Net.HttpWebResponse) await request.GetResponseAsync()).StatusCode;
            }
            catch
            {
                Trace.WriteLine(string.Format("<!><!><!>The host: {0} is down.", InHost));
            }

            // Push host to queue if valid
            if (response == System.Net.HttpStatusCode.OK)
            {
                lock (Hosts)
                    Hosts.Enqueue(InHost);

                Trace.WriteLine("Added: " + InHost);

                return true;
            }

            return false;
        }

        public async Task SelectHosts(ICollection<Uri> InHosts)
        {
            // Safety check
            if (InHosts.Count == 0)
                throw new Exception("No download servers are available; please try again later.");

            // Initialize new Hosts queue
            Hosts = new Queue<Uri>();

            // Initialize query to each host
            List<Task<bool>> tasks = InHosts.Select(host => QueryHost(host)).ToList();

            // Return when we have our best host; continue populating list in background
            while (tasks.Any())
            {
                var task = await Task.WhenAny(tasks);

                // Good mirror found; return result
                if (task.Result)
                    return;

                tasks.Remove(task);
            }

            // No host found; throw exception
            throw new Exception("Could not select a reliable download server. Please try again later.");
        }
    }
}
