using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace CustomContentSeeker
{
    public class JSONRotationRetriever
    {
        private ServerContent Content;

        /// <summary>
        /// Gets the JSON for a specific serveraddress and puts the parsed data in the class to use.
        /// </summary>
        /// <param name="ServerAddress">String containing the server address</param>
        /// <example>JSONRotationRetriever retriever = new JSONRotationRetriever("127.0.0.1:80");</example>
        public JSONRotationRetriever(String ServerAddress)
        {
            String[] ServerAddressAndPort = ServerAddress.Split(':');

            using (WebClient GUIDJsonRequest = new WebClient())
            {
                try
                {
                    String GUIDJsonResponse = GUIDJsonRequest.DownloadString(new Uri("http://serverlist.renegade-x.com/server.jsp?ip=" + ServerAddressAndPort[0] + "&port=" + ServerAddressAndPort[1]));
                    Content = JsonConvert.DeserializeObject<ServerContent>(GUIDJsonResponse);
                }
                catch
                {
                    Console.WriteLine("Error while retrieving the maplist...");
                }
            }
        }

        public List<Level> getMaps()
        {
            return this.Content.levels;
        }

        public List<Mutator> getMutators()
        {
            return this.Content.mutators;
        }
    }

    #region JSON storage structs
    /// <summary>
    /// Struct containing all the custom content of a server
    /// </summary>
    public struct ServerContent
    {
        public List<Level> levels { get; set; }
        public List<Mutator> mutators { get; set; }
    }

    /// <summary>
    /// Struct containing the Name of the level and GUID
    /// </summary>
    public struct Level
    {
        public string Name { get; set; }
        public string GUID { get; set; }
    }

    /// <summary>
    /// Struct containing the name of the mutator
    /// </summary>
    public struct Mutator
    {
        public string Name { get; set; }
    }
    #endregion
}
