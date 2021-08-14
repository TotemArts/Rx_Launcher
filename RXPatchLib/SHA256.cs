using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace RXPatchLib
{
    class SHA256
    {
        static SHA256CryptoServiceProvider CryptoProvider = new SHA256CryptoServiceProvider();

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
