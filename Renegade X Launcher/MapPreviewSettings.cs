using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace LauncherTwo
{
    class MapPreviewSettings
    {
        public static string GetGameMode(string map)
        {
            string[] separated = map.Split(new char[] { '-' }, 2);
            if (separated.Length >= 1)
                return separated[0];

            return "";
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
            string tmp;
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
                else
                    map += separated[index];
            }

            return map;
        }
    }
}
