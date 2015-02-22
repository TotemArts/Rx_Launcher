using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace LauncherTwo
{
    public static class NetworkTools
    {
        public async static Task<PingReply> Ping(string address)
        {
            try
            {
                return await new Ping().SendPingAsync(address, 1000); 
            }
            catch (PingException)
            {
                return null;
            }
        }

        public static async Task<IPAddress> GetPublicIPAddress()
        {
            try
            {
                using (var webClient = new WebClientWithTimeout(1000))
                {
                    /*
                    string response = await webClient.DownloadStringTaskAsync("http://checkip.dyndns.org/");
                    int first = response.IndexOf("Address: ") + 9;
                    int last = response.LastIndexOf("</body>");
                    string ipString = response.Substring(first, last - first);
                    return IPAddress.Parse(ipString);
                    */
                    string response = await webClient.DownloadStringTaskAsync(RenXWebLinks.IP_PROVIDER_URL);
                    int first = response.IndexOf("<body>");
                    int last = response.LastIndexOf("</body>");
                    if (first != -1 && last != -1)
                    {
                        response = response.Substring(first + 6, last - first - 6).Trim();
                    }
                    return IPAddress.Parse(response);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
