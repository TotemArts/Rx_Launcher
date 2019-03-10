using RxLogger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LauncherTwo
{
    // This class contains useful utilities that can be accessed by everyone
    public static class Utils
    {
        /// <summary>
        /// Set the full rights permission to the usergroup of the desired folder
        /// Made by Timothée Lecomte found on http://stackoverflow.com/questions/8944765/c-sharp-set-directory-permissions-for-all-users-in-windows-7
        /// This almost made me throw out my pc
        /// </summary>
        /// <param name="path">The path of the folder you wish to get full permissions over</param>
        public static void SetFullControlPermissionsToEveryone(string path)
        {
            const FileSystemRights rights = FileSystemRights.FullControl;

            var allUsers = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);

            // Add Access Rule to the actual directory itself
            var accessRule = new FileSystemAccessRule(
                allUsers,
                rights,
                InheritanceFlags.None,
                PropagationFlags.InheritOnly,
                AccessControlType.Allow);

            var info = new DirectoryInfo(path);
            var security = info.GetAccessControl(AccessControlSections.Access);

            bool result;
            security.ModifyAccessRule(AccessControlModification.Set, accessRule, out result);

            if (!result)
            {
                throw new System.InvalidOperationException("Failed to give full-control permission to all users for path " + path);
            }

            // add inheritance
            var inheritedAccessRule = new FileSystemAccessRule(
                allUsers,
                rights,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.InheritOnly,
                AccessControlType.Allow);

            security.ModifyAccessRule(AccessControlModification.Add, inheritedAccessRule, out bool inheritedResult);

            if (!inheritedResult)
            {
                throw new System.InvalidOperationException("Failed to give full-control permission inheritance to all users for " + path);
            }

            info.SetAccessControl(security);
        }

        /// <summary>
        /// Checks if the given url is valid.
        /// </summary>
        /// <param name="uri">Url</param>
        /// <returns></returns>
        public static bool IsValidURI(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri tmp))
                return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

        /// <summary>
        /// Ping to a host to check if the server is online.
        /// </summary>
        /// <param name="nameOrAddress">Name or addres of the server (like: https://www.google.com/ or 192.168.1.1)</param>
        /// <param name="timeout">Timout of the ping in milliseconds (Default is 5 seconds)</param>
        /// <returns></returns>
        public static bool PingHost(string nameOrAddress, int timeout = 5000)
        {
            bool pingable = false;
            Ping pinger = null;

            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send(nameOrAddress, timeout);
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException ex)
            {
                // Discard PingExceptions and return false;
                Logger.Instance.Write("PingHost Error: " + ex.Message);
            }
            finally
            {
                if (pinger != null)
                {
                    pinger.Dispose();
                }
            }

            return pingable;
        }

        public static BitmapSource CreateEmtpyBitmapSource(int width, int height)
        {
            /*PixelFormat pf = pixelFormat;
            int rawStride = (width * pf.BitsPerPixel + 7) / 8;
            var rawImage = new byte[rawStride * height];
            var bitmap = BitmapSource.Create(width, height, 96, 96, pf, null, rawImage, rawStride);
            return bitmap;*/

            PixelFormat pixelFormat = PixelFormats.Indexed1;
            return BitmapSource.Create(width, height, 96, 96, pixelFormat, new BitmapPalette(new List<Color> { Colors.Transparent }), new byte[] { 0, 0, 0, 0 }, 1);
        }
        public static BitmapImage CreateEmptyBitmapImage(int width, int height)
        {
            BitmapSource bitmapSource = CreateEmtpyBitmapSource(width, height);

            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            MemoryStream memoryStream = new MemoryStream();
            BitmapImage bImg = new BitmapImage();

            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
            encoder.Save(memoryStream);

            memoryStream.Position = 0;
            bImg.BeginInit();
            bImg.StreamSource = memoryStream;
            bImg.EndInit();

            memoryStream.Close();

            return bImg;
        }

    }
}
