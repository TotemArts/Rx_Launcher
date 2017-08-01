using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using System.Web;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Net.NetworkInformation;
using LauncherTwo;
using System.Dynamic;
using System.Windows.Media.Imaging;

namespace LauncherTwo
{
    public class ServerInfo : INotifyPropertyChanged
    {
        public enum GameMode
        {
            None,
            Other,
            DM,
            CNC,
            TS
        }

        /// <summary>
        /// This is the list of all active server on RenX
        /// </summary>
        public static List<ServerInfo> ActiveServers;

        /// <summary>
        /// Contains all the previous grabbed countrycodes
        /// </summary>
        public static Dictionary<string, string> CountryCodeCache = new Dictionary<string, string>();

        /// <summary>
        /// This function will request all the server info on the RenXServers. This
        /// must be called to populate the server list. 
        /// </summary>
        public static async Task ParseJsonServersAsync()
        {
            var NewActiveServers = new List<ServerInfo>();

            try
            {
                //Grab the string from the RenX Website.
                string jsonText = await new WebClient().DownloadStringTaskAsync(RenXWebLinks.RENX_ACTIVE_SERVER_JSON_URL);

                //Turn it into a JSon object that we can parse.
                var results = JsonConvert.DeserializeObject<dynamic>(jsonText);


                //For each object we have to try to get its components.
                #region -= Parse JSon Collection


                foreach (var Data in results)
                {
                    try
                    {
                        ServerInfo NewServer = new ServerInfo();
                        //SET STRINGS
                        NewServer.ServerName = Data["Name"] ?? "Missing";

                        NewServer.MapName = Data["Current Map"] ?? "Missing";

                        NewServer.SimplifiedMapName = GetPrettyMapName(NewServer.MapName);

                        NewServer.MapMode = GetGameMode(NewServer.MapName);

                        NewServer.GameVersion = Data["Game Version"] ?? "Missing";

                        NewServer.IPAddress = Data["IP"] ?? "Missing";

                        //SET INTS
                        NewServer.BotCount = Data["Bots"] ?? -1;

                        NewServer.PlayerCount = Data["Players"] ?? -1;

                        NewServer.MineLimit = Data.Variables["Mine Limit"] ?? -1;

                        NewServer.MaxPlayers = Data.Variables["Player Limit"] ?? -1;

                        NewServer.VehicleLimit = Data.Variables["Vehicle Limit"] ?? -1;

                        NewServer.CrateRespawnRate = Data.Variables["CrateRespawnAfterPickup"] ?? -1;

                        NewServer.TimeLimit = Data.Variables["Time Limit"] ?? -1;

                        NewServer.Port = Data["Port"] ?? -1;

                        //SET BOOLS
                        NewServer.SteamRequired = Data.Variables["bSteamRequired"] ?? false;

                        NewServer.PasswordProtected = Data.Variables["bPassworded"] ?? false;

                        NewServer.AutoBalance = Data.Variables["bAutoBalanceTeams"] ?? false;

                        NewServer.SpawnCrates = Data.Variables["bSpawnCrates"] ?? false;

                        NewServer.CountryCode = await GrabCountry(NewServer.IPAddress);

                        //All work done, add current serverinfo to the main list
                        NewActiveServers.Add(NewServer);
                    }
                    catch
                    {
                        // If a server failed to parse, skip it.
                    }
                }
                #endregion
            }
            catch
            {
                // If a global error occurred (e.g. connectivity/JSON parse error), clear the whole list.
                NewActiveServers.Clear();
            }
            ActiveServers = NewActiveServers;
        }

        private static bool ParseBool(String str, bool def = false)
        {
            bool b;
            if (bool.TryParse(str, out b))
                return b;
            else
                return def;
        }

        private static int ParseInt(String str, int def = 0)
        {
            int i;
            if (Int32.TryParse(str, out i))
                return i;
            else
                return def;
        }

        private static double ParseDouble(String str, double def = 0)
        {
            double d;
            if (double.TryParse(str, out d))
                return d;
            else
                return def;
        }

        public static GameMode GetGameMode(string map)
        {
            string[] separated = map.Split(new char[] { '-' }, 2);

            if (separated.Length <= 0)
                return GameMode.None;

            if (separated[0].ToUpper() == "DM")
                return GameMode.DM;

            if (separated[0].ToUpper() == "CNC")
                return GameMode.CNC;

            if (separated[0].ToUpper() == "TS")
                return GameMode.TS;

            return GameMode.Other;
        }

        public static string StripGameMode(string map)
        {
            string[] separated = map.Split(new char[] { '-' }, 2);
            if (separated.Length >= 2)
                return separated[1];

            return "";
        }

        public static string GetPrettyMapName(string map)
        {
            string[] separated;

            map = StripGameMode(map);

            separated = map.Split(new char[] { '_' });

            if (separated.Length == 0)
                return "";

            map = separated[0];

            for (int index = 1; index != separated.Length; ++index)
            {
                map += " ";
                if (separated[index].ToLower() == "day")
                    map += "(Day)";
                else if (separated[index].ToLower() == "night")
                    map += "(Night)";
                else if (separated[index].ToLower() == "flying")
                    map += "(Flying)";
                else
                    map += separated[index];
            }

            return map;
        }

        public async static Task PingActiveServersAsync()
        {
            List<Task> pingTasks = new List<Task>();
            foreach (ServerInfo serverInfo in ActiveServers)
            {
                pingTasks.Add(PingAsync(serverInfo));
            }
            await Task.WhenAll(pingTasks.ToArray());
        }

        static async Task PingAsync(ServerInfo serverInfo)
        {
            try
            {
                PingReply reply = await new Ping().SendPingAsync(serverInfo.IPAddress);
                serverInfo.Ping = (int)reply.RoundtripTime;
                if (serverInfo.PropertyChanged != null)
                {
                    serverInfo.PropertyChanged(serverInfo, new PropertyChangedEventArgs("Ping"));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to ping server: " + e.ToString());
            }
        }

        /// <summary>
        /// Tries to grab the countryname based on the given IPAddress.
        /// </summary>
        /// <param name="IPAddress">The IPAddress</param>
        /// <returns>The name of the country that belongs to the IPAddress</returns>
        static async Task<string> GrabCountry(string IPAddress)
        {
            //GET CountryCode
            //If the Ip is known, we can check the country.
            //Otherwise, assume CountryCode is missing
            if (IPAddress != "Missing")
            {
                string CountryCode;
                //First check the cache if we already have the current ip, this reduces the call to the api
                CountryCodeCache.TryGetValue(IPAddress, out CountryCode);
                //If the CountryCode was not found in the cache (null), grab it from the api
                //Else, use the CountryCode from the cache
                if (CountryCode == null)
                {
                    try
                    {
                        //Grab the Countrycode
                        string CountryJson = await new WebClient().DownloadStringTaskAsync("https://api.ip2country.info/ip?" + IPAddress);
                        var CountryResults = JsonConvert.DeserializeObject<dynamic>(CountryJson);

                        //Add to CurrentServer info and cache
                        CountryCodeCache.Add(IPAddress, (string)CountryResults["countryName"]);
                        return CountryResults["countryName"];
                    }
                    catch
                    {
                        //If the api does not respond in any way, assume CountryCode is missing
                        return "Unknown";
                    }
                }
                else
                {
                    return CountryCode;
                }
            }
            else
            {
                return "Unknown";
            }
        }



        // FRONT PAGE INFO
        public string ServerName { get; set; }
        public string MapName { get; set; }
        public string SimplifiedMapName { get; set; }
        public GameMode MapMode { get; set; }
        // Raw ping value
        public int Ping { get; set; }
        // Value used to sort ping in the server list
        public int PingSort
        {
           get
            {
                if (Ping <= 0)
                    return int.MaxValue;
                else return Ping;
            }
            
        }
        // Formatted ping value for display
        public string PingString
        {
            get
            {
                if (Ping <= 0)
                    return "-";
                else return Ping.ToString();
            }
        }
        // Raw player count
        public int PlayerCount { get; set; }
        // Inverted player count for sorting purposes
        public int PlayerCountSort { get { return -PlayerCount; } }
        // Nice player count string, for display
        public string PlayerCountString
        {
            get
            {
                return PlayerCount.ToString() + "/" + MaxPlayers.ToString();
            }
        }

        private BitmapImage lockImage;
        public BitmapImage LockImage
        {
            get
            {
                if (lockImage == null)
                    lockImage = new BitmapImage(new Uri("Resources/LockIcon.png", UriKind.Relative));

                if (PasswordProtected)
                    return lockImage;
                else return null;
            }
        }
        
       
        // SIDE BAR INFO
        public string   IPAddress { get; set; }
        public int Port { get; set; }
        public string IPWithPort
        {
            get
            {
                if (Port > 0)
                    return IPAddress + ":" + Port.ToString();
                else
                    return IPAddress;
            }
        }
        public string   GameVersion { get; set; }
        public int      MineLimit { get; set; }
        public bool     SteamRequired { get; set; }
        public bool     PasswordProtected { get; set; }
        public bool     AllowPM { get; set; }
        public bool     PmTeamOnly { get; set; }
        public int      MaxPlayers { get; set; }
        public int      VehicleLimit { get; set; }
        public int      BotCount { get; set; }
        public bool     UsesBots { get; set; }
        public bool     AutoBalance { get; set; }
        public bool     SpawnCrates { get; set; }
        public int      CrateRespawnRate { get; set; }
        public int      TimeLimit { get; set; }
        public string   CountryCode { get; set; }

        public ServerInfo()
        {
            ServerName = string.Empty;
            MapName = string.Empty;
            SimplifiedMapName = string.Empty;
            PlayerCount = -1;
            Ping = -1;
            Port = -1;

            IPAddress = string.Empty;
            GameVersion = string.Empty;
            MineLimit = -1;
            SteamRequired = false;
            PasswordProtected = false;
            AllowPM = false;
            MaxPlayers = -1;
            VehicleLimit = -1;
            AutoBalance = false;
            SpawnCrates = false;
            CrateRespawnRate = -1;
            TimeLimit = -1;
            UsesBots = false;
            BotCount = -1;
            CountryCode = string.Empty;

        }

        public event PropertyChangedEventHandler PropertyChanged;

        
    }
}
