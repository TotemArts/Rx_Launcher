using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RXPatchLib
{
    public class PatchTorrentInfo
    {
        public string Name = "unnamed patch";
        public string Comment = "";
        public string Publisher = "unknown";
        public bool Private = true;
        public List<List<string>> AnnounceUrls = new List<List<string>>();
    }
}
