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
        private static Dictionary<string, Bitmap> MapBitmaps = new Dictionary<string, Bitmap>()
        {
           { "Walls",      Properties.Resources.___map_pic_cnc_walls       },
           { "Lakeside",   Properties.Resources.___map_pic_cnc_lakeside    },
           { "Field",      Properties.Resources.___map_pic_cnc_field       },
           { "Mesa",       Properties.Resources.___map_pic_cnc_mesaii      },
           { "Goldrush",   Properties.Resources.___map_pic_cnc_goldrush    },
           { "Islands",    Properties.Resources.___map_pic_cnc_island      },
           { "Volcano",    Properties.Resources.___map_pic_cnc_volcano     },
           { "X-Mountain", Properties.Resources.___map_pic_cnc_xmountain   },
           { "Whiteout",   Properties.Resources.___map_pic_cnc_hourglassii },
           { "Canyon",     Properties.Resources.___map_pic_cnc_canyon      },
           { "Complex",    Properties.Resources.___map_pic_cnc_complex     },
           { "Under",      Properties.Resources.___map_pic_cnc_under       },
        };

        private static Dictionary<string, string> MapNames = new Dictionary<string, string>()
        {
            { "cnc-walls_flying", "Walls"      },
            { "cnc-lakeside",     "Lakeside"   },
            { "cnc-field",        "Field"      },
            { "cnc-mesa_ii",      "Mesa"       },
            { "cnc-goldrush",     "Goldrush"   },
            { "cnc-islands",      "Islands"    },
            { "cnc-volcano",      "Volcano"    },
            { "cnc-xmountain",    "X-Mountain" },
            { "cnc-whiteout",     "Whiteout"   },
            { "cnc-canyon",       "Canyon"     },
            { "cnc-complex",      "Complex"    },
            { "cnc-under",        "Under"      },
        };

        public static Bitmap GetMapBitmap(string mapName)
        {
           if (MapBitmaps.ContainsKey(mapName))
           {
                return MapBitmaps[mapName];
           }
           else
           {
               return null;
           }
        }

        public static string GetPrettyMapName(string theMap)
        {
            string theMapLower = theMap.ToLower();

            if (MapNames.ContainsKey(theMapLower))
                return MapNames[theMapLower];
            else
                return theMap;
        }
    }
}
