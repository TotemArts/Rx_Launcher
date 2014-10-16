using MonoTorrent;
using MonoTorrent.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class PatchTorrentBuilder
    {
        public void CreatePatchTorrent(string patchDirPath, PatchTorrentInfo patchInfo)
        {
            TorrentCreator c = new TorrentCreator();
            c.Comment = patchInfo.Comment;
            c.CreatedBy = Assembly.GetExecutingAssembly().GetName().Name;
            c.Publisher = patchInfo.Publisher;
            c.Private = patchInfo.Private;

            foreach (var tierUrls in patchInfo.AnnounceUrls)
            {
                c.Announces.Add(new RawTrackerTier(tierUrls));
            }
            if (c.Announces.Count > 0 && c.Announces[0].Count > 0)
            {
                c.Announce = c.Announces[0][0];
            }

            c.PieceLength = 0;

            ITorrentFileSource fileSource = new PatchTorrentFileSource(patchInfo.Name, patchDirPath);
            c.Create(fileSource, Path.Combine(patchDirPath, patchInfo.Name + ".torrent"));
        }
    }
}
