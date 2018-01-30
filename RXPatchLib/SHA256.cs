using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace RXPatchLib
{
    public class Sha256
    {
        public static string GetFileHash(string path)
        {
            if (!File.Exists(path))
                return null;

            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(new SHA256CryptoServiceProvider().ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

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
