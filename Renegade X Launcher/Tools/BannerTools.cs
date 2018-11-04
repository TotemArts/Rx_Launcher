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

        public BannerInfo(string websiteLink, ImageSource source )
        {
            MWebsiteLink = websiteLink;
            MBannerImageSource = source; 
        }

        public string MWebsiteLink;
        public ImageSource MBannerImageSource;
    }
    class BannerTools
    {
        private static readonly Dictionary<string, BannerInfo> _banners = new Dictionary<string, BannerInfo>()
        {
            { "Default", new BannerInfo("https://renegade-x.com/", new BitmapImage(new Uri("Resources/defaultBanner.png", UriKind.Relative))) }
        };

        public static ImageSource GetBanner(string ipAddress)
        {
            if( _banners.ContainsKey(ipAddress))
            {
                return _banners[ipAddress].MBannerImageSource;
            }
            else
            {
                return _banners["Default"].MBannerImageSource;
            }
        }

        public static void LaunchBannerLink(string ipAddress)
        {
            System.Diagnostics.Process.Start(GetBannerLink(ipAddress));
        }

        public static string GetBannerLink(string ipAddress)
        {
            string link;
            if (ipAddress != null && _banners.ContainsKey(ipAddress))
            {
                link = _banners[ipAddress].MWebsiteLink;
            }
            else
            {
                link = _banners["Default"].MWebsiteLink;
            }
            return link;
        }

        public static void Setup()
        {
            ParseBanners();
        }

        private static void ParseBanners()
        {
            if (VersionCheck.BannersUrl == null)
            {
                Debug.Print("Error: BannersUrl is null in ParseBanners");
            }

            try
            {
                //Grab the string from the RenX Website.
                string jsonText = new WebClient().DownloadString(VersionCheck.BannersUrl);

                //Turn it into a JSon object that we can parse.
                var results = JsonConvert.DeserializeObject<dynamic>(jsonText);

                foreach(var data in results)
                {
                    BannerInfo info = new BannerInfo
                    {
                        MWebsiteLink = data["Link"],
                    };

                    string bannerUrl = data["Banner"];
                    info.MBannerImageSource = DownloadImage(bannerUrl);

                    string ipString = data["IP"] ?? "-1";
                    string[] ips = ipString.Split(RenXWebLinks.RenxServerSettingSpaceSymbol);

                    foreach( string ip in ips)
                    {
                        /*
                         * AX: No longer are we ripping the port off of the IP Address here for banners,
                         *  Reason: Game server providers use the same server IP Address for multiple instances of the same game, this fixes banners for these server types.
                         */

                        _banners.Add(ip, info); 
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
