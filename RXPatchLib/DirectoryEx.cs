using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RXPatchLib
{
    public class DirectoryEx
    {
        public static void DeleteContents(string path)
        {
            var dir = new DirectoryInfo(path);

            foreach (var file in dir.GetFiles())
            {
                file.Delete();
            }
            foreach (var subDir in dir.GetDirectories())
            {
                subDir.Delete(true);
            }
        }
    }
}
