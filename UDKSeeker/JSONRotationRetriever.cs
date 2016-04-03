using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;

namespace UDKSeeker
{
    public class JSONRotationRetriever
    {
        private readonly static String repositoryAdress = "ftp://renx.constructivetyranny.com/";
        private readonly static String ftpUsername = "Launcher";
        private readonly static String ftpPassword = "CustomMaps199";
        private List<MapItem> Maps = new List<MapItem>();

        public JSONRotationRetriever(String ServerAddress)
        {
            String[] ServerAddressAndPort = ServerAddress.Split(':');
            String GUIDJsonResponse;

            

            using (WebClient GUIDJsonRequest = new WebClient())
            {
                try
                {
                    GUIDJsonResponse = GUIDJsonRequest.DownloadString(new Uri("http://renegadexgs.appspot.com/serverGUIDs.jsp?ip=" + ServerAddressAndPort[0] + "&port=" + ServerAddressAndPort[1]));
                    Maps = JsonConvert.DeserializeObject<List<MapItem>>(GUIDJsonResponse);
                }
                catch
                {
                    Console.WriteLine("Error while retrieving the maplist...\nDo you wish to join the server anyway?(Y/N)");
                }
            }
        }

        public List<MapItem> getMaps()
        {
            return this.Maps;
        }
    }
}
