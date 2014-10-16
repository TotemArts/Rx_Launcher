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

            ITorrentFileSource fileSource = new PatchTorrentFileSource(patchInfo.Name, patchDirPath);
            c.Create(fileSource, Path.Combine(patchDirPath, patchInfo.Name + ".torrent"));
        }
    }
}
