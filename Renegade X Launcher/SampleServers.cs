namespace LauncherTwo
{
    public class SampleServers
    {
        public string TitleValue { get; } = "Renegade-X Test";
        public TrulyObservableCollection<ServerInfo> OFilteredServerList { get; set; }
        public TrulyObservableCollection<PlayerInfo> OFilteredServerPlayerList { get; set; }
        public bool IsLaunchingPossible { get; } = false;


        public SampleServers()
        {
            OFilteredServerList = new TrulyObservableCollection<ServerInfo>();
            OFilteredServerPlayerList = new TrulyObservableCollection<PlayerInfo>();
            CreateTestData();
        }

        private void CreateTestData()
        {
            OFilteredServerList.Clear();
            OFilteredServerList.Add(new ServerInfo()
            {
                ServerName = "Dummy Server 1",
                MapName = "CNC-Field",
                SimplifiedMapName = "Field",
                MapMode = ServerInfo.GameMode.Cnc,
                TimeLimit = 120,
                PlayerCount = 4,
                MaxPlayers = 10,
                Ping = 10,
                Ranked = false,
                PasswordProtected = false,
                SteamRequired = false,
                AllowPm = true,
                UsesBots = true,
                BotCount = 1,
                AutoBalance = true,
                SpawnCrates = true,
                MineLimit = 20,
                VehicleLimit = 12,
                Players = new TrulyObservableCollection<PlayerInfo>() {
                    new PlayerInfo{ Name = "Noob1" },
                    new PlayerInfo{ Name = "[B] Bot1" },
                    new PlayerInfo{ Name = "Noob2" },
                    new PlayerInfo{ Name = "Noob3" },
                },
                Levels = new TrulyObservableCollection<LevelInfo>() {
                    new LevelInfo{ Name = "Dummy Level 1", GUID = "000" }
                }
            });
            OFilteredServerList.Add(new ServerInfo()
            {
                ServerName = "Dummy Server 2",
                MapName = "CNC-Walls",
                SimplifiedMapName = "Walls",
                MapMode = ServerInfo.GameMode.Cnc,
                TimeLimit = 300,
                PlayerCount = 3,
                MaxPlayers = 10,
                Ping = 100,
                Ranked = false,
                PasswordProtected = false,
                SteamRequired = false,
                AllowPm = true,
                UsesBots = true,
                BotCount = 1,
                AutoBalance = true,
                SpawnCrates = true,
                MineLimit = 24,
                VehicleLimit = 12,
                Players = new TrulyObservableCollection<PlayerInfo>() {
                    new PlayerInfo{ Name = "[B] Bot1" },
                    new PlayerInfo{ Name = "[B] Bot2" },
                    new PlayerInfo{ Name = "[B] Bot3" }
                },
                Levels = new TrulyObservableCollection<LevelInfo>() {
                    new LevelInfo{ Name = "Dummy Level 1", GUID = "000" }
                }
            });
            OFilteredServerList.Add(new ServerInfo()
            {
                ServerName = "Dummy Server 3",
                MapName = "CNC-Islands",
                SimplifiedMapName = "Island",
                MapMode = ServerInfo.GameMode.Cnc,
                TimeLimit = 300,
                PlayerCount = 1,
                MaxPlayers = 10,
                Ping = 50,
                Ranked = false,
                PasswordProtected = true,
                SteamRequired = false,
                AllowPm = true,
                UsesBots = false,
                BotCount = 0,
                AutoBalance = true,
                SpawnCrates = true,
                MineLimit = 24,
                VehicleLimit = 12,
                Players = new TrulyObservableCollection<PlayerInfo>() {
                    new PlayerInfo{ Name = "The All Mighty Gabe Newell" }
                },
                Levels = new TrulyObservableCollection<LevelInfo>() {
                    new LevelInfo{ Name = "Dummy Level 1", GUID = "000" }
                }
            });

            OFilteredServerPlayerList.Clear();
            foreach (var player in OFilteredServerList[0].Players)
            {
                OFilteredServerPlayerList.Add(player);
            }
        }
        
    }
}
