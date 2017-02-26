using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace RXPatchLib
{
    public class SHA256
    {
        public static async Task<string> GetFileHashAsync(string path)
        {
            if (!File.Exists(path))
                return null;

            return await Task.Run(() =>
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(new SHA256CryptoServiceProvider().ComputeHash(stream)).Replace("-", string.Empty);
                }
            });
        }

        public static string Get(byte[] data)
        {
            return BitConverter.ToString(new SHA256CryptoServiceProvider().ComputeHash(data)).Replace("-", string.Empty);
        }
    }
}
