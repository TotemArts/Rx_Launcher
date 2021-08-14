using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace RXPatchLib
{
    class SHA1
    {
        static SHA1CryptoServiceProvider CryptoProvider = new SHA1CryptoServiceProvider();

        public static async Task<string> GetFileHashAsync(string path)
        {
            if (!File.Exists(path))
                return null;

            return await Task.Run(() =>
            {
                using (var stream = File.OpenRead(path))
                {
                    return BitConverter.ToString(CryptoProvider.ComputeHash(stream)).Replace("-", string.Empty);
                }
            });
        }

        public static string Get(byte[] data)
        {
            return BitConverter.ToString(CryptoProvider.ComputeHash(data)).Replace("-", string.Empty);
        }
    }
}
