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



    public class ServerInfo
    {
        public enum SERVER_INFO_POSITIONS
        {
            Server_Name = 0,
            Server_IP = 1,
            Bots = 2,
            Password_Required = 3,
            Map_Name = 4,
            Server_Settings = 5,
            Player_Count = 6,
            Player_Limit = 7,

        }


        public enum SERVER_SETTINGS_POSTIONS
        {
            MaxPlayers = 0,
            VehicleLimit = 1,
            MineLimit = 2,
            SpawnCreate = 3,
            CrateRespawnTime = 4,
            AutoBalance = 5,
            TimeLimit = 6,
            AllowPm = 7,
            PmTeamOnly = 8,
            SteamRequired = 9,
            Version = 10
        }

        /// <summary>
        /// This is the list of all active server on RenX
        /// </summary>
        public static List<ServerInfo> ActiveServers;

        /// <summary>
        /// This function will request all the server info on the RenXServers. This
        /// must be called to populate the server list. 
        /// </summary>
        public static async Task ParseJsonServersAsync()
        {
            var NewActiveServers = new List<ServerInfo>();

            //Grab the string from the RenX Website.
            string jsonText = await new WebClient().DownloadStringTaskAsync(RenXWebLinks.RENX_ACTIVE_SERVER_JSON_URL);

            //Turn it into a JSon object that we can parse.
            var results = JsonConvert.DeserializeObject<dynamic>(jsonText);


            //For each object we have to try to get its components.
            #region -= Parse JSon Collection


            foreach (var Data in results)
            {

                ServerInfo NewServer = new ServerInfo();
                //SET STRINGS
                NewServer.ServerName =  Data["Name"] ?? "Missing";

                NewServer.MapName =     Data["Current Map"] ?? "Missing";

                NewServer.MapName = MapPreviewSettings.GetPrettyMapName(NewServer.MapName);

                NewServer.GameVersion = Data["Game Version"] ?? "Missing";

                NewServer.IPAddress =   Data["IP"] ?? "Missing";

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

                NewActiveServers.Add(NewServer);
            }
            #endregion

            ActiveServers = NewActiveServers;
        }

        public static void ParseServers()
        {
            //Empty the list of our active servesr
            ActiveServers = new List<ServerInfo>();

            //Grab the string from the RenX Website.
            string serverText = new WebClient().DownloadString(RenXWebLinks.RENX_ACTIVE_SERVERS_LIST_URL);

            //Turn it into a JSon object that we can parse.
            string[] results = serverText.Split(RenXWebLinks.RENX_SERVER_INFO_BREAK_SYMBOL, StringSplitOptions.RemoveEmptyEntries);


            //We start at 2 to skip the dev notes. 
            //We end one early to skip the html tags.
            for(int i = 2; i < results.Length - 1; i++ )
            {
                string[] info = results[i].Split(RenXWebLinks.RENX_SERVER_INFO_SPACER_SYMBOL);

                ServerInfo newServer = new ServerInfo();

                // SERVER NAME
                newServer.ServerName = info[(int)SERVER_INFO_POSITIONS.Server_Name];

                // SERVER IP & PORT
                string[] address = info[ (int)SERVER_INFO_POSITIONS.Server_IP ].Split(':');
                newServer.IPAddress = address[0];
                newServer.Port = ParseInt(address[1]);

                //BOT COUNT
                newServer.BotCount = ParseInt( info[(int)SERVER_INFO_POSITIONS.Bots] ) ;

                //PASSWORD REQUIRED
                newServer.PasswordProtected = ParseBool( info[(int)SERVER_INFO_POSITIONS.Password_Required] );

                //MAP
                newServer.MapName = MapPreviewSettings.GetPrettyMapName(info[(int)SERVER_INFO_POSITIONS.Map_Name]);

                //SERVER SETTINGS
                {
                    //Break down the settings
                    string[] serverSettings = info[(int)SERVER_INFO_POSITIONS.Server_Settings].Split(RenXWebLinks.RENX_SERVER_SETTING_SPACE_SYMBOL);

                    newServer.MaxPlayers          = ParseInt( serverSettings[ (int)SERVER_SETTINGS_POSTIONS.VehicleLimit      ] );
                    newServer.VehicleLimit        = ParseInt( serverSettings[ (int)SERVER_SETTINGS_POSTIONS.MaxPlayers        ] );
                    newServer.MineLimit           = ParseInt( serverSettings[ (int)SERVER_SETTINGS_POSTIONS.MineLimit         ] );
                    newServer.SpawnCrates         = ParseBool(serverSettings[ (int)SERVER_SETTINGS_POSTIONS.SpawnCreate       ] );
                    newServer.CrateRespawnRate    = ParseInt( serverSettings[ (int)SERVER_SETTINGS_POSTIONS.CrateRespawnTime  ] );
                    newServer.AutoBalance         = ParseBool(serverSettings[ (int)SERVER_SETTINGS_POSTIONS.AutoBalance       ] );
                    newServer.TimeLimit           = ParseInt( serverSettings[ (int)SERVER_SETTINGS_POSTIONS.TimeLimit         ] );
                    newServer.AllowPM             = ParseBool(serverSettings[ (int)SERVER_SETTINGS_POSTIONS.AllowPm           ] );
                    newServer.PmTeamOnly          = ParseBool(serverSettings[ (int)SERVER_SETTINGS_POSTIONS.PmTeamOnly        ] );
                    newServer.SteamRequired       = ParseBool(serverSettings[ (int)SERVER_SETTINGS_POSTIONS.SteamRequired     ] );
                    newServer.GameVersion         =           serverSettings[ (int)SERVER_SETTINGS_POSTIONS.Version];
                }

                // PLAYER COUNT
                newServer.PlayerCount = ParseInt(info[(int)SERVER_INFO_POSITIONS.Player_Count]);

                ActiveServers.Add(newServer);
            }
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

 

        public async static Task PingActiveServersAsync()
        {
            try
            {

                List<Task<PingReply>> pingTasks = new List<Task<PingReply>>();
                foreach (ServerInfo address in ActiveServers)
                {
                    pingTasks.Add(PingAsync(address.IPAddress));
                }

                await Task.WhenAll(pingTasks.ToArray());

                for (int i = 0; i < ActiveServers.Count; i++)
                {
                    if (pingTasks[i] != null)
                        if (pingTasks[i].Result != null)
                            ActiveServers[i].Ping = (int)pingTasks[i].Result.RoundtripTime;
                }
            }
            catch(Exception)
            {
                Console.WriteLine("Failed to call servers");
            }
        }

        static Task<PingReply> PingAsync(string address)
        {
            var tcs = new TaskCompletionSource<PingReply>();
            Ping ping = new Ping();
            ping.PingCompleted += (obj, sender) =>
            {
                tcs.SetResult(sender.Reply);
            };
            ping.SendAsync(address, new object());
            return tcs.Task;
        }

        // FRONT PAGE INFO
        public string ServerName { get; set; }
        public string MapName { get; set; }
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

        public ServerInfo()
        {
            ServerName = string.Empty;
            MapName = string.Empty;
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

        }

 }
        
