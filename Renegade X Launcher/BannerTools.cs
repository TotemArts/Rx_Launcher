using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LauncherTwo
{
    class BannerInfo
    {
        public BannerInfo()
        {

        }

        public BannerInfo(string IPAddress, string WebsiteLink, ImageSource Source )
        {
            m_IPAddress = IPAddress;
            m_WebsiteLink = WebsiteLink;
            m_BannerImageSource = Source; 
        }

        public string m_IPAddress;
        public string m_WebsiteLink;
        public ImageSource m_BannerImageSource;
    }
    class BannerTools
    {
        private static Dictionary<string, BannerInfo> Banners = new Dictionary<string, BannerInfo>()
        {
            {
                "Default", new BannerInfo( "http://www.renegade-x.com/launcher_data/images/sample_banner.png", 
                                           "http://www.renegade-x.com/",
                                            DownloadImage("http://www.renegade-x.com/launcher_data/images/sample_banner.png"))
            }
        };

        public static ImageSource GetBanner(string IPAddress)
        {
            if( Banners.ContainsKey(IPAddress))
            {
                return Banners[IPAddress].m_BannerImageSource;
            }
            else
            {
                return Banners["Default"].m_BannerImageSource;
            }
        }

        public static void LaunchBannerLink(string IPAddress )
        {
            if (Banners.ContainsKey(IPAddress))
            {
                System.Diagnostics.Process.Start( Banners[IPAddress].m_WebsiteLink );
            }
            else
            {
                System.Diagnostics.Process.Start(Banners["Default"].m_WebsiteLink);
            }
        }

        public static void Setup()
        {

            ParseBanners();

        }

        private static void ParseBanners()
        {
            //Grab the string from the RenX Website.
            string jsonText = new WebClient().DownloadString(RenXWebLinks.RENX_BANNERS_JSON_URL);

            //Turn it into a JSon object that we can parse.
            var results = JsonConvert.DeserializeObject<dynamic>(jsonText);

            foreach(var Data in results)
            {
                BannerInfo info = new BannerInfo();

                info.m_WebsiteLink = Data["Link"];

                string urlLink = Data["Banner"];

                info.m_BannerImageSource = DownloadImage(urlLink);

                string ipString = Data["IP"] ?? "-1";
                string[] ips = ipString.Split(RenXWebLinks.RENX_SERVER_SETTING_SPACE_SYMBOL);

                foreach( string ip in ips)
                {
                    Banners.Add(ip.Replace(":7777", ""), info); 
                }
            }

        }

        public static BitmapImage DownloadImage(string url)
        {
            BitmapImage image;
            Uri uri = new Uri(url);
            image = new BitmapImage(uri);

            return image; 
        }
    }
}
