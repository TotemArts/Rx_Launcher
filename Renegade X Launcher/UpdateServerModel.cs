using System;
using System.Collections.Generic;

namespace LauncherTwo
{
    /// <summary>
    /// Model containing infor about an updateserver
    /// </summary>
    public class UpdateServerModel
    {
        public Uri ServerUri { get; private set; }
        public string CleanServerName { get; private set; }

        /// <summary>
        /// Creates an UpdateServerModel
        /// </summary>
        /// <param name="Url">An Uri containing the server</param>
        public UpdateServerModel(Uri Url)
        {
            this.CleanServerName = Url.Host;
            this.ServerUri = Url;
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
        /// <param name="Urls">a string array containing all the url's of the servers</param>
        /// <returns>An array of UpdateServermodels</returns>
        public static UpdateServerModel[] CreateUpdateServerModels(string[] Urls)
        {
            List<UpdateServerModel> UpdateServers = new List<UpdateServerModel>();
            foreach (string Url in Urls)
            {
                UpdateServers.Add(new UpdateServerModel(new Uri(Url)));
            }
            return UpdateServers.ToArray();

        }

    }
}
