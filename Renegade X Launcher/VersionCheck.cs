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


namespace LauncherTwo
{
    public class Version
    {
        public string Name { get; set; }
        public int Number { get; set; }
    }
    public static class VersionCheck
    {
        static Version LauncherVersion;
        static Version GameVersion;
        static Version LatestLauncherVersion;
        static Version LatestGameVersion;

        public static string InstructionsHash;
        public static string GamePatchPath = null;
        public static string[] GamePatchUrls = null;
        public static string LauncherPatchUrl = null;
        public static string LauncherPatchHash = null;

        static VersionCheck()
        {
            LauncherVersion = new Version
            {
                Name = "0.76-dev",
                Number = 076
            };
        }

        public static string GetLauncherVersionName()
        {
            return LauncherVersion.Name;
        }

        public static string GetGameVersionName()
        {
            if (GameVersion == null)
                UpdateGameVersion();
            return GameVersion.Name;
        }

        public static string GetLatestLauncherVersionName()
        {
            return LatestLauncherVersion.Name;
        }

        public static string GetLatestGameVersionName()
        {
            if (LatestGameVersion != null)
            {
                return LatestGameVersion.Name;
            }
            else
            {
                return "Open Beta 5.16";
            }
        }

        public static void UpdateGameVersion()
        {
            const string INI_PATH = "\\UDKGame\\Config\\DefaultRenegadeX.ini";
            const string VERSION_PREFIX = "GameVersion=";
            const string VERSION_NUMBER_PREFIX = "GameVersionNumber=";

            try
            {
                string versionName = null;
                int? versionNumber = null;
                string filename = GameInstallation.GetRootPath() + INI_PATH;
                foreach (var line in File.ReadAllLines(filename))
                {
                    if (line.StartsWith(VERSION_PREFIX))
                    {
                        versionName = line
                            .Replace(VERSION_PREFIX, "")
                            .Replace("\"", "");
                    }
                    else if (line.StartsWith(VERSION_NUMBER_PREFIX))
                    {
                        versionNumber = int.Parse(line
                            .Replace(VERSION_NUMBER_PREFIX, "")
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
                GameVersion = new Version
                {
                    Name = versionName,
                    Number = versionNumber.Value,
                };

            }
            catch
            {
                GameVersion = new Version
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
                LatestLauncherVersion = new Version
                {
                    Name = versionData["launcher"]["version_name"],
                    Number = versionData["launcher"]["version_number"],
                };
                LauncherPatchUrl = versionData["launcher"]["patch_url"];
                LauncherPatchHash = versionData["launcher"]["patch_hash"];

                // Game
                LatestGameVersion = new Version
                {
                    Name = versionData["game"]["version_name"],
                    Number = versionData["game"]["version_number"],
                };
                InstructionsHash = versionData["game"]["instructions_hash"];
                GamePatchPath = versionData["game"]["patch_path"];
                GamePatchUrls = versionData["game"]["patch_urls"].ToObject<string[]>();
            }
            catch
            {
                LatestLauncherVersion = new Version
                {
                    Name = "Unknown",
                    Number = 0,
                };
                LatestGameVersion = new Version
                {
                    Name = "Unknown",
                    Number = 0,
                };
            }
        }

        public static bool IsLauncherOutOfDate()
        {
            return LatestLauncherVersion.Number > LauncherVersion.Number;
        }

        public static bool IsGameOutOfDate()
        {
            return LatestGameVersion.Number > GameVersion.Number;
        }
    }
}
