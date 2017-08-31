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
using System.Windows;

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
        /// Dictionary containing all the coordinates for the flag icons to be searched by countrycode in ISO 3166-1 alpha-2 format.
        /// </summary>
        public static readonly Dictionary<string, Rect> FlagCodes = new Dictionary<string, Rect>()
        {
            #region FlagCodes
            {"SA", new Rect( 384, 352, 32, 32)},
            {"NO", new Rect( 128, 320, 32, 32)},
            {"SG", new Rect( 64, 384, 32, 32)},
            {"TD", new Rect( 0, 416, 32, 32)},
            {"KW", new Rect( 256, 224, 32, 32)},
            {"NE", new Rect( 0, 320, 32, 32)},
            {"SM", new Rect( 192, 384, 32, 32)},
            {"CL", new Rect( 320, 64, 32, 32)},
            {"GU", new Rect( 224, 160, 32, 32)},
            {"VA", new Rect( 96, 448, 32, 32)},
            {"ID", new Rect( 0, 192, 32, 32)},
            {"LA", new Rect( 352, 224, 32, 32)},
            {"VN", new Rect( 256, 448, 32, 32)},
            {"GR", new Rect( 160, 160, 32, 32)},
            {"TV", new Rect( 320, 416, 32, 32)},
            {"PS", new Rect( 64, 352, 32, 32)},
            {"KH", new Rect( 64, 224, 32, 32)},
            {"ST", new Rect( 320, 384, 32, 32)},
            {"SN", new Rect( 224, 384, 32, 32)},
            {"GI", new Rect( 448, 128, 32, 32)},
            {"SB", new Rect( 416, 352, 32, 32)},
            {"SZ", new Rect( 416, 384, 32, 32)},
            {"IL", new Rect( 64, 192, 32, 32)},
            {"RE", new Rect( 224, 352, 32, 32)},
            {"MD", new Rect( 288, 256, 32, 32)},
            {"BI", new Rect( 224, 32, 32, 32)},
            {"SI", new Rect( 96, 384, 32, 32)},
            {"NG", new Rect( 32, 320, 32, 32)},
            {"NI", new Rect( 64, 320, 32, 32)},
            {"ER", new Rect( 448, 96, 32, 32)},
            {"BD", new Rect( 64, 32, 32, 32)},
            {"CI", new Rect( 256, 64, 32, 32)},
            {"TL", new Rect( 128, 416, 32, 32)},
            {"UG", new Rect( 448, 416, 32, 32)},
            {"PK", new Rect( 448, 320, 32, 32)},
            {"MT", new Rect( 192, 288, 32, 32)},
            {"GM", new Rect( 32, 160, 32, 32)},
            {"IN", new Rect( 160, 192, 32, 32)},
            {"VI", new Rect( 224, 448, 32, 32)},
            {"MR", new Rect( 128, 288, 32, 32)},
            {"UZ", new Rect( 64, 448, 32, 32)},
            {"CM", new Rect( 352, 64, 32, 32)},
            {"BF", new Rect( 128, 32, 32, 32)},
            {"JE", new Rect( 352, 192, 32, 32)},
            {"GQ", new Rect( 128, 160, 32, 32)},
            {"SO", new Rect( 256, 384, 32, 32)},
            {"ME", new Rect( 320, 256, 32, 32)},
            {"TW", new Rect( 352, 416, 32, 32)},
            {"RS", new Rect( 288, 352, 32, 32)},
            {"HT", new Rect( 416, 160, 32, 32)},
            {"KG", new Rect( 32, 224, 32, 32)},
            {"CV", new Rect( 32, 96, 32, 32)},
            {"NR", new Rect( 192, 320, 32, 32)},
            {"CZ", new Rect( 96, 96, 32, 32)},
            {"PL", new Rect( 0, 352, 32, 32)},
            {"MS", new Rect( 160, 288, 32, 32)},
            {"EH", new Rect( 416, 96, 32, 32)},
            {"AT", new Rect( 352, 0, 32, 32)},
            {"GD", new Rect( 288, 128, 32, 32)},
            {"BO", new Rect( 352, 32, 32, 32)},
            {"LY", new Rect( 192, 256, 32, 32)},
            {"GE", new Rect( 320, 128, 32, 32)},
            {"BB", new Rect( 32, 32, 32, 32)},
            {"IR", new Rect( 224, 192, 32, 32)},
            {"KN", new Rect( 160, 224, 32, 32)},
            {"EC", new Rect( 320, 96, 32, 32)},
            {"AR", new Rect( 288, 0, 32, 32)},
            {"VG", new Rect( 192, 448, 32, 32)},
            {"HU", new Rect( 448, 160, 32, 32)},
            {"SD", new Rect( 0, 384, 32, 32)},
            {"SV", new Rect( 352, 384, 32, 32)},
            {"TN", new Rect( 192, 416, 32, 32)},
            {"KP", new Rect( 192, 224, 32, 32)},
            {"IT", new Rect( 288, 192, 32, 32)},
            {"LI", new Rect( 448, 224, 32, 32)},
            {"LB", new Rect( 384, 224, 32, 32)},
            {"AO", new Rect( 256, 0, 32, 32)},
            {"LU", new Rect( 128, 256, 32, 32)},
            {"KE", new Rect( 0, 224, 32, 32)},
            {"KR", new Rect( 224, 224, 32, 32)},
            {"TT", new Rect( 288, 416, 32, 32)},
            {"KZ", new Rect( 320, 224, 32, 32)},
            {"GG", new Rect( 384, 128, 32, 32)},
            {"JM", new Rect( 384, 192, 32, 32)},
            {"MO", new Rect( 64, 288, 32, 32)},
            {"NZ", new Rect( 224, 320, 32, 32)},
            {"LK", new Rect( 0, 256, 32, 32)},
            {"AE", new Rect( 32, 0, 32, 32)},
            {"BM", new Rect( 288, 32, 32, 32)},
            {"BY", new Rect( 32, 64, 32, 32)},
            {"SR", new Rect( 288, 384, 32, 32)},
            {"DM", new Rect( 224, 96, 32, 32)},
            {"EG", new Rect( 384, 96, 32, 32)},
            {"UY", new Rect( 32, 448, 32, 32)},
            {"PT", new Rect( 96, 352, 32, 32)},
            {"MG", new Rect( 352, 256, 32, 32)},
            {"SC", new Rect( 448, 352, 32, 32)},
            {"MU", new Rect( 224, 288, 32, 32)},
            {"HN", new Rect( 352, 160, 32, 32)},
            {"TR", new Rect( 256, 416, 32, 32)},
            {"AG", new Rect( 96, 0, 32, 32)},
            {"IQ", new Rect( 192, 192, 32, 32)},
            {"YE", new Rect( 352, 448, 32, 32)},
            {"BH", new Rect( 192, 32, 32, 32)},
            {"AZ", new Rect( 448, 0, 32, 32)},
            {"RO", new Rect( 256, 352, 32, 32)},
            {"FM", new Rect( 128, 128, 32, 32)},
            {"MY", new Rect( 352, 288, 32, 32)},
            {"FR", new Rect( 192, 128, 32, 32)},
            {"SE", new Rect( 32, 384, 32, 32)},
            {"MK", new Rect( 416, 256, 32, 32)},
            {"CD", new Rect( 128, 64, 32, 32)},
            {"MC", new Rect( 256, 256, 32, 32)},
            {"LR", new Rect( 32, 256, 32, 32)},
            {"FO", new Rect( 160, 128, 32, 32)},
            {"MQ", new Rect( 96, 288, 32, 32)},
            {"MW", new Rect( 288, 288, 32, 32)},
            {"TO", new Rect( 224, 416, 32, 32)},
            {"GY", new Rect( 288, 160, 32, 32)},
            {"MV", new Rect( 256, 288, 32, 32)},
            {"NA", new Rect( 416, 288, 32, 32)},
            {"TM", new Rect( 160, 416, 32, 32)},
            {"AS", new Rect( 320, 0, 32, 32)},
            {"US", new Rect( 0, 448, 32, 32)},
            {"FJ", new Rect( 96, 128, 32, 32)},
            {"AD", new Rect( 0, 0, 32, 32)},
            {"VC", new Rect( 128, 448, 32, 32)},
            {"PW", new Rect( 128, 352, 32, 32)},
            {"NP", new Rect( 160, 320, 32, 32)},
            {"BZ", new Rect( 64, 64, 32, 32)},
            {"JP", new Rect( 448, 192, 32, 32)},
            {"MM", new Rect( 0, 288, 32, 32)},
            {"GP", new Rect( 96, 160, 32, 32)},
            {"AU", new Rect( 384, 0, 32, 32)},
            {"ET", new Rect( 32, 128, 32, 32)},
            {"TH", new Rect( 64, 416, 32, 32)},
            {"DZ", new Rect( 288, 96, 32, 32)},
            {"BR", new Rect( 384, 32, 32, 32)},
            {"LS", new Rect( 64, 256, 32, 32)},
            {"ZM", new Rect( 416, 448, 32, 32)},
            {"PY", new Rect( 160, 352, 32, 32)},
            {"AM", new Rect( 192, 0, 32, 32)},
            {"CK", new Rect( 288, 64, 32, 32)},
            {"SY", new Rect( 384, 384, 32, 32)},
            {"HK", new Rect( 320, 160, 32, 32)},
            {"BG", new Rect( 160, 32, 32, 32)},
            {"BT", new Rect( 448, 32, 32, 32)},
            {"CR", new Rect( 448, 64, 32, 32)},
            {"GL", new Rect( 0, 160, 32, 32)},
            {"LV", new Rect( 160, 256, 32, 32)},
            {"SL", new Rect( 160, 384, 32, 32)},
            {"RW", new Rect( 352, 352, 32, 32)},
            {"MH", new Rect( 384, 256, 32, 32)},
            {"VU", new Rect( 288, 448, 32, 32)},
            {"AI", new Rect( 128, 0, 32, 32)},
            {"MZ", new Rect( 384, 288, 32, 32)},
            {"GH", new Rect( 416, 128, 32, 32)},
            {"IE", new Rect( 32, 192, 32, 32)},
            {"CO", new Rect( 416, 64, 32, 32)},
            {"LC", new Rect( 416, 224, 32, 32)},
            {"PR", new Rect( 32, 352, 32, 32)},
            {"TG", new Rect( 32, 416, 32, 32)},
            {"ML", new Rect( 448, 256, 32, 32)},
            {"UA", new Rect( 416, 416, 32, 32)},
            {"TC", new Rect( 448, 384, 32, 32)},
            {"IS", new Rect( 256, 192, 32, 32)},
            {"DK", new Rect( 192, 96, 32, 32)},
            {"BS", new Rect( 416, 32, 32, 32)},
            {"GW", new Rect( 256, 160, 32, 32)},
            {"NC", new Rect( 448, 288, 32, 32)},
            {"ZA", new Rect( 384, 448, 32, 32)},
            {"BE", new Rect( 96, 32, 32, 32)},
            {"QA", new Rect( 192, 352, 32, 32)},
            {"KM", new Rect( 128, 224, 32, 32)},
            {"GT", new Rect( 192, 160, 32, 32)},
            {"CF", new Rect( 160, 64, 32, 32)},
            {"TJ", new Rect( 96, 416, 32, 32)},
            {"CU", new Rect( 0, 96, 32, 32)},
            {"GA", new Rect( 224, 128, 32, 32)},
            {"ES", new Rect( 0, 128, 32, 32)},
            {"CG", new Rect( 192, 64, 32, 32)},
            {"KI", new Rect( 96, 224, 32, 32)},
            {"FI", new Rect( 64, 128, 32, 32)},
            {"CA", new Rect( 96, 64, 32, 32)},
            {"RU", new Rect( 320, 352, 32, 32)},
            {"GB", new Rect( 256, 128, 32, 32)},
            {"AL", new Rect( 160, 0, 32, 32)},
            {"BN", new Rect( 320, 32, 32, 32)},
            {"EE", new Rect( 352, 96, 32, 32)},
            {"CN", new Rect( 384, 64, 32, 32)},
            {"HR", new Rect( 384, 160, 32, 32)},
            {"BA", new Rect( 0, 32, 32, 32)},
            {"MX", new Rect( 320, 288, 32, 32)},
            {"KY", new Rect( 288, 224, 32, 32)},
            {"VE", new Rect( 160, 448, 32, 32)},
            {"NL", new Rect( 96, 320, 32, 32)},
            {"DO", new Rect( 256, 96, 32, 32)},
            {"PG", new Rect( 384, 320, 32, 32)},
            {"BJ", new Rect( 256, 32, 32, 32)},
            {"DJ", new Rect( 160, 96, 32, 32)},
            {"BW", new Rect( 0, 64, 32, 32)},
            {"PH", new Rect( 416, 320, 32, 32)},
            {"ZW", new Rect( 448, 448, 32, 32)},
            {"IM", new Rect( 128, 192, 32, 32)},
            {"PA", new Rect( 288, 320, 32, 32)},
            {"PE", new Rect( 320, 320, 32, 32)},
            {"CY", new Rect( 64, 96, 32, 32)},
            {"CH", new Rect( 224, 64, 32, 32)},
            {"SK", new Rect( 128, 384, 32, 32)},
            {"AF", new Rect( 64, 0, 32, 32)},
            {"GN", new Rect( 64, 160, 32, 32)},
            {"LT", new Rect( 96, 256, 32, 32)},
            {"DE", new Rect( 128, 96, 32, 32)},
            {"TZ", new Rect( 384, 416, 32, 32)},
            {"AN", new Rect( 224, 0, 32, 32)},
            {"OM", new Rect( 256, 320, 32, 32)},
            {"PF", new Rect( 352, 320, 32, 32)},
            {"MN", new Rect( 32, 288, 32, 32)},
            {"JO", new Rect( 416, 192, 32, 32)},
            {"AW", new Rect( 416, 0, 32, 32)},
            {"MA", new Rect( 224, 256, 32, 32)},
            {"WS", new Rect( 320, 448, 32, 32)}
            #endregion
        };

        /// <summary>
        /// This is the list of all active server on RenX
        /// </summary>
        public static List<ServerInfo> ActiveServers;

        /// <summary>
        /// Contains all the previous grabbed countrycodes
        /// </summary>
        public static Dictionary<string, string[]> CountryCodeCache = new Dictionary<string, string[]>();

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

                        //SET COUNTRYINFO
                        string[] CountryInfo = await GrabCountry(NewServer.IPAddress);
                        NewServer.CountryName = CountryInfo[0];
                        NewServer.CountryCode = CountryInfo[1];

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
        static async Task<string[]> GrabCountry(string IPAddress)
        {
            //GET CountryCode
            //If the Ip is known, we can check the country.
            //Otherwise, assume CountryCode and name is missing
            if (IPAddress != "Missing")
            {
                string[] CountryCode;
                //First check the cache if we already have the current ip, this reduces the call to the api
                CountryCodeCache.TryGetValue(IPAddress, out CountryCode);
                //If the CountryCode was not found in the cache (null), grab it from the api
                //Else, use the CountryCode from the cache
                if (CountryCode == null)
                {
                    try
                    {
                        //Grab the Countrycode and name
                        string CountryJson = await new WebClient().DownloadStringTaskAsync("https://api.ip2country.info/ip?" + IPAddress);
                        var CountryResults = JsonConvert.DeserializeObject<dynamic>(CountryJson);

                        //Add Countrycode and name to cache and return
                        CountryCodeCache.Add(IPAddress, new string[2] {(string)CountryResults["countryName"], (string)CountryResults["countryCode"]});
                        return new string[2] { (string)CountryResults["countryName"], (string)CountryResults["countryCode"] };
                    }
                    catch
                    {
                        //If the api does not respond in any way, assume CountryCode is missing
                        return new string[2] { "Unknown", "Unknown" };
                    }
                }
                else
                {
                    return CountryCode;
                }
            }
            else
            {
                return new string[2] { "Unknown", "Unknown" };
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

        private BitmapImage modeImage;

        public BitmapImage ModeImage
        {
            get
            {
                if(this.modeImage == null)
                {
                    switch (this.MapMode)
                    {
                        case GameMode.DM:
                            this.modeImage = new BitmapImage(new Uri("Resources/dm_modeIcon.png", UriKind.Relative));
                            break;
                        case GameMode.CNC:
                            this.modeImage = new BitmapImage(new Uri("Resources/cnc_modeIcon.png", UriKind.Relative));
                            break;
                        case GameMode.TS:
                            this.modeImage = new BitmapImage(new Uri("Resources/ts_modeIcon.png", UriKind.Relative));
                            break;
                        default:
                            break;
                    }
                }
                return this.modeImage;
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
        public string CountryName { get;  set; }

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
