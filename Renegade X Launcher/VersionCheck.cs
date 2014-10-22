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
    public static class VersionCheck
    {
        const string INI_PATH = "\\UDKGame\\Config\\DefaultRenegadeX.ini";
        const string VERSION_KEY = "GameVersion";
        const string VERSION_URL = "http://renegade-x.com/launcher_data/version.json";

        static string LatestGameVersion = "";
        static string LatestLauncherVersion = "";
        const string LauncherVersion = "0.51";
        static string GameVersion = null;

        public static string GamePatchUrl = null;

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
                UpdateGameVersion();
            return GameVersion;
        }

        public static void UpdateGameVersion()
        {
            GameVersion = ReadGameVersion();
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

        public static async Task UpdateLatestVersions()
        {
            var versionJson = await new WebClient().DownloadStringTaskAsync(VERSION_URL);
            var versionData = JsonConvert.DeserializeObject<dynamic>(versionJson);
            LatestLauncherVersion = versionData["launcher"]["version"];
            LatestGameVersion = versionData["game"]["version"];
            GamePatchUrl = versionData["game"]["patch_url"];
        }

        public static bool IsLauncherOutOfDate()
        {
            Debug.Assert(LauncherVersion != null);

            if (LatestLauncherVersion != "" && LauncherVersion != "")
            {
                float LauncherVerFloat = GetFloatVer(LauncherVersion);
                float LatestLauncherVerFloat = GetFloatVer(LatestLauncherVersion);
                if (LauncherVerFloat == -1f || LatestLauncherVerFloat == -1f)
                    return false;
                else if (LatestLauncherVerFloat > LauncherVerFloat)
                    return true;
            }
            return false;
        }

        public static bool IsGameOutOfDate()
        {
            Debug.Assert(GameVersion != null);

            if (LatestGameVersion != "" && GameVersion != "")
            {
                float GameVerFloat = GetFloatVer(GameVersion);
                float LatestGameVerFloat = GetFloatVer(LatestGameVersion);
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
