using System.Collections.Generic;

namespace LauncherTwo
{
    public class SampleServers
    {
        public TrulyObservableCollection<ServerInfo> OFilteredServerList { get; set; }
        
        public SampleServers()
        {
            OFilteredServerList = new TrulyObservableCollection<ServerInfo>();

            CreateTestData();
        }

        private void CreateTestData()
        {
            OFilteredServerList.Add(new ServerInfo()
            {
                ServerName = "Dummy Server 1",
                MapName = "dummy map",
                SimplifiedMapName = "dummy map",
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
                Players = new List<PlayerInfo>() {
                    new PlayerInfo{ Name = "Noob1" },
                    new PlayerInfo{ Name = "Noob2" },
                    new PlayerInfo{ Name = "Noob3" },
                    new PlayerInfo{ Name = "Bot1" }
                },
                Levels = new List<LevelInfo>() {
                    new LevelInfo{ Name = "Dummy Level 1", GUID = "000" }
                }
            });
            OFilteredServerList.Add(new ServerInfo()
            {
                ServerName = "Dummy Server 2",
                MapName = "dummy map",
                SimplifiedMapName = "dummy map",
                MapMode = ServerInfo.GameMode.Dm,
                TimeLimit = 300,
                PlayerCount = 1,
                MaxPlayers = 10,
                Ping = 100,
                Ranked = false,
                PasswordProtected = true,
                SteamRequired = false,
                AllowPm = true,
                UsesBots = true,
                BotCount = 1,
                AutoBalance = true,
                SpawnCrates = true,
                MineLimit = 24,
                VehicleLimit = 12,
                Players = new List<PlayerInfo>() {
                    new PlayerInfo{ Name = "Bot1" }
                },
                Levels = new List<LevelInfo>() {
                    new LevelInfo{ Name = "Dummy Level 1", GUID = "000" }
                }
            });
            OFilteredServerList.Add(new ServerInfo()
            {
                ServerName = "Dummy Server 3",
                MapName = "dummy map",
                SimplifiedMapName = "dummy map",
                MapMode = ServerInfo.GameMode.Cnc,
                TimeLimit = 300,
                PlayerCount = 1,
                MaxPlayers = 10,
                Ping = 50,
                Ranked = false,
                PasswordProtected = true,
                SteamRequired = false,
                AllowPm = true,
                UsesBots = true,
                BotCount = 1,
                AutoBalance = true,
                SpawnCrates = true,
                MineLimit = 24,
                VehicleLimit = 12,
                Players = new List<PlayerInfo>() {
                    new PlayerInfo{ Name = "Bot1" }
                },
                Levels = new List<LevelInfo>() {
                    new LevelInfo{ Name = "Dummy Level 1", GUID = "000" }
                }
            });
        }
        
    }
}
