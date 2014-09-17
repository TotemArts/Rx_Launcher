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
        private static Dictionary<string, ImageSource> MapImages = new Dictionary<string, ImageSource>()
        {
           { "Walls",    ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_walls       ) },
           { "Lakeside", ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_lakeside    ) },
           { "Field",    ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_field       ) },
           { "Mesa",     ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_mesaii      ) },
           { "Goldrush", ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_goldrush    ) },
           { "Islands",  ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_island      ) },
           { "Volcano",  ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_volcano      ) },
           { "Whiteout", ConvertBitmapToSouce ( Properties.Resources.___map_pic_cnc_hourglassii ) }
        };

        private static Dictionary<string, string> MapNames = new Dictionary<string, string>()
        {
            { "cnc-walls_flying", "Walls"    },
            { "cnc-lakeside",     "Lakeside" },
            { "cnc-field",        "Field"    },
            { "cnc-mesa_ii",      "Mesa"     },
            { "cnc-goldrush",     "Goldrush" },
            { "cnc-islands",      "Islands"  },
            { "cnc-whiteout",     "Whiteout" }
        };


        public static ImageSource ConvertBitmapToSouce(Bitmap bit)
        {
            var memoryStream = new MemoryStream();
            bit.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();

            return bitmapImage; 
        }

        public static ImageSource GetMapImage(string mapName)
        {
           if( MapImages.ContainsKey( mapName ) )
           {
                return MapImages[mapName];
           }
           else
           {

               return null;
           }
        }

        public static string GetPrettyMapName(string theMap)
        {

            if (MapNames.ContainsKey(theMap))
                return MapNames[theMap];
            else
                return "Default";

            
        }
    }
}
