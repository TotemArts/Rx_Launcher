using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;

namespace LauncherTwo
{
    public class PingResult
    {
        public readonly String Address;
        public readonly PingReply Reply;

        public PingResult(String address, PingReply reply)
        {
            Address = address;
            Reply = reply;
        }
    }
}
