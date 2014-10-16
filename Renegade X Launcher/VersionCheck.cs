using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;


namespace LauncherTwo
{
    public static class VersionCheck
    {
        // Read from DefaultRenegadeX.ini
        const string INI_PATH = "\\UDKGame\\Config\\DefaultRenegadeX.ini";
        const string VERSION_KEY = "GameVersion";
        const string LATEST_GAMEVERSION_URL = "http://renegade-x.com/launcher_data/gameversion";
        const string LATEST_LAUNCHERVERSION_URL = "http://renegade-x.com/launcher_data/launcherversion";
        static string DownloadingURL = "";

        public const string GAME_DOWNLOAD_URL = "http://renegade-x.com/download";
        public const string LAUNCHER_DOWNLOAD_URL = "http://renegade-x.com/download";
        static string LatestGameVersion = "";
        static string LatestLauncherVersion = "";
        const string LauncherVersion = "0.51";
        static string GameVersion = null;

        public static float GetLatestGameVersionNumerical()
        {
            return GetFloatVer(GetLatestGameVersion());
        }
        public static float GetGameVersionNumerical()
        {
            return GetFloatVer(GetGameVersion());
        }

        public static string GetLatestGameVersion()
        {
            return LatestGameVersion;
        }

        public static string GetGameVersion()
        {
            if (GameVersion == null)
                GameVersion = ReadGameVersion();
            return GameVersion;
        }

        public static float GetLatestLauncherVersionNumerical()
        {
            return GetFloatVer(GetLatestLauncherVersion());
        }
        public static float GetLauncherVersionNumerical()
        {
            return GetFloatVer(GetLauncherVersion());
        }

        public static string GetLatestLauncherVersion()
        {
            return LatestLauncherVersion;
        }

        public static string GetLauncherVersion()
        {
            return LauncherVersion;
        }

        static string ReadGameVersion()
        {
            string FileName = GameInstallation.GetRootPath() + INI_PATH;

            try
            {
                string[] IniLines = File.ReadAllLines(FileName);
                for (int i = 0; i < IniLines.Length; i++)
                {
                    if (IniLines[i].StartsWith(VERSION_KEY))
                    {
                        string VersionString = IniLines[i];
                        VersionString = VersionString.Replace(VERSION_KEY, "");
                        VersionString = VersionString.Replace("=", "");
                        VersionString = VersionString.Replace("\"", "");
                        return VersionString;
                    }
                }
                return "";

            }
            catch
            {
                return "";
            }
        }

        public static async Task FindGameVersionAsync()
        {
            LatestGameVersion = await new WebClient().DownloadStringTaskAsync(LATEST_GAMEVERSION_URL);
        }

        public static async Task FindLauncherVersionAsync()
        {
            LatestLauncherVersion = await new WebClient().DownloadStringTaskAsync(LATEST_GAMEVERSION_URL);
        }

        public static string GetDownloadingURL()
        {
            return DownloadingURL;
        }

        public static bool IsLauncherOutOfDate()
        {
            float LauncherVerFloat;
            float LatestLauncherVerFloat;

            if (LatestLauncherVersion != "" && LauncherVersion != "")
            {
                LauncherVerFloat = GetFloatVer(LauncherVersion);
                LatestLauncherVerFloat = GetFloatVer(LatestLauncherVersion);
                if (LauncherVerFloat == -1f || LatestLauncherVerFloat == -1f)
                    return false;
                else if (LatestLauncherVerFloat > LauncherVerFloat)
                    return true;
            }
            return false;
        }

        public static bool IsGameOutOfDate()
        {
            GetGameVersion(); // Ensure that GameVersion is set.

            float GameVerFloat;
            float LatestGameVerFloat;

            if (LatestGameVersion != "" && GameVersion != "")
            {
                GameVerFloat = GetFloatVer(GameVersion);
                LatestGameVerFloat = GetFloatVer(LatestGameVersion);
                if (GameVerFloat == -1f || LatestGameVerFloat == -1f)
                    return false;
                else if (LatestGameVerFloat > GameVerFloat)
                    return true;
            }
            return false;
        }

        static bool IsPreReleaseBeta(string StringVer)
        {
            if (StringVer.ToUpper().Contains("BETA"))
                return true;
            else return false;
        }

        static bool IsReleaseCandidate(string StringVer)
        {
            if (StringVer.ToUpper().Contains("RC"))
                return true;
            else return false;
        }

        static bool IsOpenBeta(string StringVer)
        {
            if (StringVer.ToUpper().Contains("OPEN"))
                return true;
            else return false;
        }

        static float GetFloatVer(string StringVer)
        {
            float valmod;
            valmod = 0;
            StringVer = StringVer.ToUpper();

            // Release candidates are always under non-RC of the same version.
            if (IsReleaseCandidate(StringVer))
            {
                valmod -= 0.00001f;
            }

            // Open beta is after pre-release, but before non-beta
            if (IsOpenBeta(StringVer))
            {
                valmod -= 500f;
            }
            // Pre-release betas are always under everything.
            else if (IsPreReleaseBeta(StringVer))
            {
                valmod -= 100f;
            }

            StringVer = StringVer.Replace("BETA", "");
            StringVer = StringVer.Replace("OPEN", "");
            StringVer = StringVer.Replace("ALPHA", "");
            StringVer = StringVer.Replace("RC", "");
            StringVer = StringVer.Replace(" ", "");  

            try
            {      
                return float.Parse(StringVer) + valmod;
            }
            catch
            {
                return -1f;
            }
        }
    }
}
