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
    }
}
