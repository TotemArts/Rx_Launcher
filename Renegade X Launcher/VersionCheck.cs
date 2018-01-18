using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using Newtonsoft.Json;
using RXPatchLib;


namespace LauncherTwo
{
    public class Version
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }
    public static class VersionCheck
    {
        static readonly Version _launcherVersion;
        static Version _gameVersion;
        static Version _latestLauncherVersion;
        static Version _latestGameVersion;

        public static string InstructionsHash;
        public static string GamePatchPath = null;
        //public static string[] GamePatchUrls = null;
        public static UpdateServerModel[] GamePatchUrls = null;
        public static string LauncherPatchUrl = null;
        public static string LauncherPatchHash = null;

        static VersionCheck()
        {
            _launcherVersion = new Version
            {
                Name = "0.77-dev",
                Number = 077
            };
        }

        public static string GetLauncherVersionName()
        {
            return _launcherVersion.Name;
        }

        public static string GetGameVersionName()
        {
            if (_gameVersion == null)
                UpdateGameVersion();
            return _gameVersion.Name;
        }

        public static string GetLatestLauncherVersionName()
        {
            return _latestLauncherVersion.Name;
        }

        public static string GetLatestGameVersionName()
        {
            if (_latestGameVersion != null)
            {
                return _latestGameVersion.Name;
            }
            else
            {
                return "Open Beta 5.16";
            }
        }

        public static void UpdateGameVersion()
        {
            const string iniPath = "\\UDKGame\\Config\\DefaultRenegadeX.ini";
            const string versionPrefix = "GameVersion=";
            const string versionNumberPrefix = "GameVersionNumber=";

            try
            {
                string versionName = null;
                int? versionNumber = null;
                string filename = GameInstallation.GetRootPath() + iniPath;
                foreach (var line in File.ReadAllLines(filename))
                {
                    if (line.StartsWith(versionPrefix))
                    {
                        versionName = line
                            .Replace(versionPrefix, "")
                            .Replace("\"", "");
                    }
                    else if (line.StartsWith(versionNumberPrefix))
                    {
                        versionNumber = int.Parse(line
                            .Replace(versionNumberPrefix, "")
                            .Replace("\"", ""));
                    }
                }

                if (versionName == "Open Beta 2" ||
                    versionName == "Open Beta 3")
                {
                    versionNumber = 0;
                }

                if (versionName == null) throw new Exception("No version number found.");
                if (versionNumber == null) throw new Exception("No version number found.");
                _gameVersion = new Version
                {
                    Name = versionName,
                    Number = versionNumber.Value,
                };

            }
            catch
            {
                _gameVersion = new Version
                {
                    Name = "Unknown",
                    Number = 0,
                };
            }
        }

        public static async Task UpdateLatestVersions()
        {
            try
            {
                var versionJson = await new WebClient().DownloadStringTaskAsync(Properties.Settings.Default.VersionUrl);
                var versionData = JsonConvert.DeserializeObject<dynamic>(versionJson);

                // Launcher
                _latestLauncherVersion = new Version
                {
                    Name = versionData["launcher"]["version_name"],
                    Number = versionData["launcher"]["version_number"],
                };
                LauncherPatchUrl = versionData["launcher"]["patch_url"];
                LauncherPatchHash = versionData["launcher"]["patch_hash"];

                // Game
                _latestGameVersion = new Version
                {
                    Name = versionData["game"]["version_name"],
                    Number = versionData["game"]["version_number"],
                };
                InstructionsHash = versionData["game"]["instructions_hash"];
                GamePatchPath = versionData["game"]["patch_path"];

                // Server URL's list & Friendly Names
                foreach (var x in versionData["game"]["server_urls"].ToObject<dynamic>())
                    RxPatcher.Instance.AddNewUpdateServer(x["url"].ToString(), x["friendly_name"].ToString());
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
                _latestLauncherVersion = new Version
                {
                    Name = "Unknown",
                    Number = 0,
                };
                _latestGameVersion = new Version
                {
                    Name = "Unknown",
                    Number = 0,
                };
            }
        }

        public static bool IsLauncherOutOfDate()
        {
            return _latestLauncherVersion.Number > _launcherVersion.Number;
        }

        public static bool IsGameOutOfDate()
        {
            return _latestGameVersion.Number > _gameVersion.Number;
        }
    }
}
