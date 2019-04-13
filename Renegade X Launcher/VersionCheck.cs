using System;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Windows;
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
        public static UpdateServerModel[] GamePatchUrls = null;
        public static string LauncherPatchUrl = null;
        public static string LauncherPatchHash = null;
        public static string BannersUrl = null;

        static VersionCheck()
        {
            _launcherVersion = new Version
            {
                Name = "0.00",
                Number = 00
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
                return "Open Beta 5.370";
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

                if (!File.Exists(filename))
                    throw new Exception("Default config not found.");

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
            string versionJson = "";
            dynamic versionData = null;

            using (WebClient client = new WebClient())
            {
                try
                {
                    versionJson = await client.DownloadStringTaskAsync(Properties.Settings.Default.VersionUrl);
                }
                catch (WebException ex)
                {
                    // WHY?! There is no Singleplayer!! The update process needs to be finished before anyone can access the game
                    MessageBox.Show("An error occurred while downloading version information, you can still play the game in singleplayer but multiplayer might be unavailable.",
                        "RenegadeX Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                    RxLogger.Logger.Instance.Write("Error while downloading launcher startup configuration, you can still play the game in singleplayer but multiplayer might be unavailable.\n" +
                                                    "Error: " + ex.Message, RxLogger.Logger.ErrorLevel.ErrError);
                } catch(Exception ex) {
                    RxLogger.Logger.Instance.Write("Something went wrong during latest version check... \nError: " + ex.Message, RxLogger.Logger.ErrorLevel.ErrError);
                }
            }

            // If we dont have any versionJson, we cannot continue anyway!
            if (string.IsNullOrEmpty(versionJson))
                return;

            // Json parsing
            try
            {
                versionData = JsonConvert.DeserializeObject<dynamic>(versionJson);
            }
            catch (Exception)
            {
                MessageBox.Show("Unable to load the RenegadeX Launcher, unable to parse JSON Version Information",
                    "RenegadeX Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                RxLogger.Logger.Instance.Write($"Unable to load the RenegadeX Launcher, unable to parse JSON Version Information", RxLogger.Logger.ErrorLevel.ErrError);
            }

            // Launcher parsing
            try
            {
                _latestLauncherVersion = new Version
                {
                    Name = versionData["launcher"]["version_name"],
                    Number = versionData["launcher"]["version_number"],
                };

                LauncherPatchUrl = versionData["launcher"]["patch_url"];
                LauncherPatchHash = versionData["launcher"]["patch_hash"];
                BannersUrl = versionData["launcher"]["banners_url"];
            }
            catch (Exception)
            {
                MessageBox.Show("An error occurred while loading the launcher version information.\r\nIt's recommended that you download the launcher again from www.renegade-x.com/download",
                    "RenegadeX Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                RxLogger.Logger.Instance.Write($"Error loading the launcher version information.\r\nIt's recommended that you download the launcher again from www.renegade-x.com", RxLogger.Logger.ErrorLevel.ErrError);
            }

            // Game parsing
            try
            { 
                _latestGameVersion = new Version
                {
                    Name = versionData["game"]["version_name"],
                    Number = versionData["game"]["version_number"],
                };
                InstructionsHash = versionData["game"]["instructions_hash"];
                GamePatchPath = versionData["game"]["patch_path"];

                try
                {
                    // Server URL's list & Friendly Names
                    foreach (var x in versionData["game"]["mirrors"].ToObject<dynamic>())
                        RxPatcher.Instance.AddNewUpdateServer(x["url"].ToString(), x["name"].ToString());
                }
                catch (Exception)
                {
                    // If the launcher is out of date, we dont care that the game is wrong
                    if (!IsLauncherOutOfDate())
                    {
                        MessageBox.Show(
                            "An error occurred while while parsing the Content Delivery Network JSON, you will not be able to update the game.\r\nPlease report this issue to a developer over at www.renegade-x.com",
                            "RenegadeX Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    // Log this event to the file still however.
                    RxLogger.Logger.Instance.Write($"Error while parsing the Content Delivery Network JSON, you will not be able to update the game.\r\nPlease report this issue to a developer over at www.renegade-x.com", RxLogger.Logger.ErrorLevel.ErrError);
                }
            }
            catch(Exception)
            {
                MessageBox.Show("An error occurred while loading the game version information.\r\nPlease report this issue to a developer over at www.renegade-x.com",
                    "RenegadeX Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
                RxLogger.Logger.Instance.Write($"Error loading the game version information.\r\nPlease report this issue to a developer over at www.renegade-x.com", RxLogger.Logger.ErrorLevel.ErrError);
            }
        }

        public static bool IsLauncherOutOfDate()
        {
            return _launcherVersion.Number != 0 // Suppress for development builds
                && _latestLauncherVersion.Number > _launcherVersion.Number;
        }

        public static bool IsGameOutOfDate()
        {
            return _latestGameVersion.Number > _gameVersion.Number;
        }
    }
}
