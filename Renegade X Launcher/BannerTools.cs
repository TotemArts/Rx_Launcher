using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public BannerInfo(string WebsiteLink, ImageSource Source )
        {
            m_WebsiteLink = WebsiteLink;
            m_BannerImageSource = Source; 
        }

        public string m_WebsiteLink;
        public ImageSource m_BannerImageSource;
    }
    class BannerTools
    {
        private static Dictionary<string, BannerInfo> Banners = new Dictionary<string, BannerInfo>()
        {
            { "Default", new BannerInfo("https://renegade-x.com/", new BitmapImage(new Uri("Resources/defaultBanner.png", UriKind.Relative))) }
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

        public static void LaunchBannerLink(string IPAddress)
        {
            System.Diagnostics.Process.Start(GetBannerLink(IPAddress));
        }

        public static string GetBannerLink(string IPAddress)
        {
            string Link;
            if (IPAddress != null && Banners.ContainsKey(IPAddress))
            {
                Link = Banners[IPAddress].m_WebsiteLink;
            }
            else
            {
                Link = Banners["Default"].m_WebsiteLink;
            }
            return Link;
        }

        public static void Setup()
        {

            ParseBanners();

        }

        private static void ParseBanners()
        {
            try
            {
                //Grab the string from the RenX Website.
                string jsonText = new WebClient().DownloadString(RenXWebLinks.RENX_BANNERS_JSON_URL);

                //Turn it into a JSon object that we can parse.
                var results = JsonConvert.DeserializeObject<dynamic>(jsonText);

                foreach(var Data in results)
                {
                    BannerInfo info = new BannerInfo
                    {
                        m_WebsiteLink = Data["Link"],
                    };

                    string bannerURL = Data["Banner"];
                    info.m_BannerImageSource = DownloadImage(bannerURL);

                    string ipString = Data["IP"] ?? "-1";
                    string[] ips = ipString.Split(RenXWebLinks.RENX_SERVER_SETTING_SPACE_SYMBOL);

                    foreach( string ip in ips)
                    {
                        /*
                         * AX: No longer are we ripping the port off of the IP Address here for banners,
                         *  Reason: Game server providers use the same server IP Address for multiple instances of the same game, this fixes banners for these server types.
                         */

                        Banners.Add(ip, info); 
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.Print(ex.Message);
                // Swallow any exceptions (usually connectivity or parse errors).
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
