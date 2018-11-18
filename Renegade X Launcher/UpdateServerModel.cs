using System;
using System.Collections.Generic;

namespace LauncherTwo
{
    /// <summary>
    /// Model containing information about an updateserver
    /// </summary>
    public class UpdateServerModel
    {
        public Uri ServerUri { get; private set; }
        public string CleanServerName { get; private set; }

        /// <summary>
        /// Creates an UpdateServerModel
        /// </summary>
        /// <param name="url">An Uri containing the server</param>
        public UpdateServerModel(Uri url)
        {
            this.CleanServerName = url.Host;
            this.ServerUri = url;
        }
    }   

    /// <summary>
    /// Factory for creating UpdateServerModel objects
    /// </summary>
    public class UpdateServerModelFactory
    {
        /// <summary>
        /// Create a UpdateServerModel array to be uses within various components
        /// </summary>
        /// <param name="urls">a string array containing all the url's of the servers</param>
        /// <returns>An array of UpdateServermodels</returns>
        public static UpdateServerModel[] CreateUpdateServerModels(string[] urls)
        {
            List<UpdateServerModel> updateServers = new List<UpdateServerModel>();
            foreach (string url in urls)
            {
                updateServers.Add(new UpdateServerModel(new Uri(url)));
            }
            return updateServers.ToArray();

        }

    }
}
