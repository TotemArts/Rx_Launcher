using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.IO.Compression;

namespace CustomContentSeeker
{
    /// <summary>
    /// UdkSeeker wil seek Renegade-x maps from the given RenegadeX repository
    /// Created by Rob Smit for Renegade-x With help from Jessica James (GUID extraction)
    /// </summary>
    public class UdkSeeker
    {
        /// <summary>
        /// All credentials and addresses
        /// </summary>
        private String ftpAddress { get; set; }
        private String username { get; set; }
        private String password { get; set; }
        private String renXDir { get; set; }

        public String currMap { get; private set; }

        public long DownloadedBytes { get; private set; }
        public long TotalAmountOfBytes { get; private set; }

    public Status status { get; private set; }

        public enum Status{
            GeneralError, //General error
            MaplistError, //No maplist error
            DownloadError, //Error while downloading
            MapNotFoundError, //Map not present on repository
            ExtractError, //Extraction failed
            MapSucces, //Map downloaded and extracted succesfully
            Downloading, //Now downloading
            Cancelled, //Cancelled
            Finished //All done, seeking succes
        };

        /// <summary>
        /// Constuctor for the UdkSeeker
        /// </summary>
        /// <param name="ftpAddress">The repository to use (FTP)</param>
        /// <param name="username">The username to acces the FTP server</param>
        /// <param name="password">The passwordt to access the FTP server</param>
        public UdkSeeker(String ftpAddress, String username, String password)
        {
            this.ftpAddress = ftpAddress;
            this.username = username;
            this.password = password;
            //this.renXDir = "D:\\Program Files (x86)\\Renegade X\\UDKGame\\CookedPC\\Maps\\RenX\\"; //For Testing
            this.renXDir = System.IO.Path.GetFullPath(System.Reflection.Assembly.GetExecutingAssembly().Location + "/../.." + "\\UDKGame\\CookedPC\\Maps\\RenX\\");
        }

        public long getBytes()
        {
            return DownloadedBytes;
        }

        /// <summary>
        /// Seeks for all maps on a server
        /// </summary>
        /// <param name="serverAddress">The address of the server</param>
        /// <returns></returns>
        public Status SeekAll(String serverAddress)
        {
            JSONRotationRetriever JSONretriever = new CustomContentSeeker.JSONRotationRetriever(serverAddress); //<-TestIP
            var Maps = JSONretriever.getMaps();
            if (Maps != null)
            {
                foreach (var Map in Maps)
                {
                    currMap = Map.Name;
                    if (Seek(Map.Name, Map.GUID) != Status.Finished)
                    {
                        //Console.WriteLine("Could not download all the maps on the server. It may be possible you can't play all the maps.\nContinue downloading the other maps? (Y/N)");
                        //StatusLabelContent.Content = "Could not download all maps, continue anyway?";
                        return UdkSeeker.Status.MapNotFoundError;
                    }
                }
                return Status.Finished;
            }
            else
            {
                return Status.MaplistError;
            }
            
        }


        /// <summary>
        /// Seek the map that is provided
        /// </summary>
        /// <param name="Map">The mapname to seek</param>
        /// <param name="ServerGUID">The GUID of the map on the server that will be joined</param>
        public Status Seek(String Map, String ServerGUID)
        {
            currMap = Map;
            //Add the map + udk extension (As the serverlist shows them without udk extension)
            String dir = this.renXDir + Map + ".udk";

            //Get the GUID of the map on the system. NULL wil be the answer if map is not found/
            String localGUID = this.GetGUID(dir);

            //Compare the GUID of the map on the system to the one on the server. 
            //If false -> search and download map from repository; If true -> map already present on the system so the game can launch
            if (localGUID == ServerGUID && localGUID != null)
            {
                Console.WriteLine("The map {0} is identical to the server's...", Map);
                return Status.MapSucces;
            }
            else
            {
                Console.WriteLine("Seeking correct map");
                //Trying to search for the map on the repository (According to the server GUID)
                try
                {
                    //Return whatever the getMap functionreturns. If Status.MapSucces, all is good.
                    return this.GetMap(Map, ServerGUID); 

                }
                catch(Exception ex)
                {
                    //If any failure gets detected, exit and print message
                    return Status.GeneralError;

                }
            }
        }

        /// <summary>
        /// Get the GUID of a UDK file
        /// </summary>
        /// <param name="FilePath">Path to the UDK file to get the GUID from</param>
        /// <returns>A string with the GUId or null if no udk file was found</returns>
        private String GetGUID(String FilePath)
        {
            if (!File.Exists(FilePath))
                return null;

            FileStream fs = new FileStream(FilePath, FileMode.Open);

            /*///EXPERIMENTAL, ONLY NEED TO REVERSE
            BinaryReader temp1 = new BinaryReader(fs);
            temp1.BaseStream.Position = 0x45;
            byte[] itemSection = temp1.ReadBytes(16);
            UInt16 hex2 = new UInt16();
            String hex3 = "";
            for (int index = 0; index < 16; index++)
            {
                 hex2 = BitConverter.ToUInt16(itemSection, index);
                hex3 = BitConverter.ToString(itemSection, index, 4);
                string output = new string(hex3.ToCharArray().Reverse().ToArray());
            }
            ////EXPERIMENTAL, ONLY NEED TO REVERSE*/


            int hexIn;

            List<string> q = new List<string>();
            List<string> x = new List<string>();
            List<string> y = new List<string>();
            List<string> z = new List<string>();

            fs.Seek(0x45, SeekOrigin.Begin);

            for (int i = 0; (hexIn = fs.ReadByte()) != -1; i++)
            {
                /*if(i >= 0 && i < 16)
                    hex += string.Format("{0:X2}", hexIn);*/
                if (i >= 0 && i < 4)
                {
                    q.Add(string.Format("{0:X2}", hexIn));
                }
                if (i >= 4 && i < 8)
                {
                    x.Add(string.Format("{0:X2}", hexIn));
                }
                if (i >= 8 && i < 12)
                {
                    y.Add(string.Format("{0:X2}", hexIn));
                }
                if (i >= 12 && i < 16)
                {
                    z.Add(string.Format("{0:X2}", hexIn));
                }
                if (i >= 15)
                {
                    break;
                }
            }



            q.Reverse();
            x.Reverse();
            y.Reverse();
            z.Reverse();

            String total = "";
            foreach (string part in q)
            {
                total += part;
            }
            foreach (string part in x)
            {
                total += part;
            }
            foreach (string part in y)
            {
                total += part;
            }
            foreach (string part in z)
            {
                total += part;
            }
            fs.Close();
            return total;
        }


        /// <summary>
        /// Download the map from the maps repository
        /// </summary>
        /// <param name="Map">The name of the map, only used for feedback to user</param>
        /// <param name="ServerGuid">The GUID of the map to download</param>
        private Status GetMap(string Map, string ServerGuid)
        {
            String MapToDownload = null;
            Console.WriteLine("Searching for map {0} on {1}", Map, this.ftpAddress);
            List<string> dirs = this.ShowDir(new Uri(this.ftpAddress));


            //Searcher for all dirs and uses them to search for maps
            foreach (string dir in dirs)
            {
                if (dir != ".htaccess")
                {
                    MapToDownload = this.SearchMap(ServerGuid, Map, new Uri(this.ftpAddress + dir));
                }
                if (MapToDownload != null)
                {
                    break;
                }
            }

            if (MapToDownload != null)
            {
                FtpWebRequest request = FtpWebRequest.Create(this.ftpAddress + MapToDownload) as FtpWebRequest;
                request.Credentials = new NetworkCredential(this.username, this.password);

                request.Method = WebRequestMethods.Ftp.GetFileSize;

                this.TotalAmountOfBytes = request.GetResponse().ContentLength;

                request = null;
                request = FtpWebRequest.Create(new Uri(this.ftpAddress + MapToDownload)) as FtpWebRequest;
                request.Credentials = new NetworkCredential(this.username, this.password);
                request.UseBinary = true;
                request.UsePassive = false;
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                

                WebResponse response = request.GetResponse();
                

                Stream responseStream = response.GetResponseStream();

                //Temporary directory creation
                if (!Directory.Exists(this.renXDir + "..//..//..//..//" + "UDKSeekerTemp//"))
                    Directory.CreateDirectory(this.renXDir + "..//..//..//..//" + "UDKSeekerTemp//");

                //Create a temp file to save the map in
                FileStream newFile = new FileStream(this.renXDir + "..//..//..//..//" + "UDKSeekerTemp//tmp.rxmap", FileMode.Create);
                DownloadedBytes = 0;

                int bytesRead = 0;
                byte[] buffer = new byte[4096];

                this.status = Status.Downloading;
                //Download the map
                while (true)
                {
                    bytesRead = responseStream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                        break;

                    newFile.Write(buffer, 0, bytesRead);
                    DownloadedBytes += bytesRead;
                }
                //Download complete, close the file.
                newFile.Close();
                this.status = Status.Finished;
                //close the connection to the repository
                response.Close();

                //Extract the map
                if (this.Extract(this.renXDir + "..//..//..//..//" + "UDKSeekerTemp//tmp.rxmap"))
                {
                    //Succes
                    return Status.MapSucces;
                }
                else
                {
                    //Error while extracting
                    return Status.ExtractError;
                }
            }
            else
            {
                //Map not found 
                this.status = Status.MapNotFoundError;
                return Status.MapNotFoundError;
            }
        }

        /// <summary>
        /// Show all the directories and files on a give repository path
        /// </summary>
        /// <param name="FtpDir">The directory to search (e.g.: ftp://maps.com/John OR ftp://maps.com/)</param>
        /// <returns>A list with all the directories and files</returns>
        private List<string> ShowDir(Uri FtpDir)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FtpDir);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = new NetworkCredential(this.username, this.password);
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            List<string> dirs = new List<string>(); //All directories
            while (reader.Peek() >= 0)
            {
                dirs.Add(reader.ReadLine());
            }

            reader.Close();
            response.Close();

            return dirs;
        }

        /// <summary>
        /// Searches a repository for the desired map 
        /// </summary>
        /// <param name="ServerGuid">The GUID of the map to search</param>
        /// <param name="Map">Name of the map</param>
        /// <param name="address">Repository address</param>
        /// <returns>String with the maplocation on the repository</returns>
        private String SearchMap(string ServerGuid, string Map, Uri address)
        {
            List<string> dirs = this.ShowDir(address);
            String returnDir = null;

            IEnumerable<string> linq = from dir in dirs
                                       where dir.Contains(ServerGuid + ".zip")
                                       select dir;


            //Searcher for all dirs and uses them to search for maps
            //Search deeper into filesystem! TODO
            //Searches only 1 folder deep
            foreach (string dir in linq)
            {
                Console.WriteLine("Succes! Map {0} Found...", Map);
                return dir;
            }
            return returnDir;
        }


        /// <summary>
        /// WIP NOT WORKING: Searches the whole repository (All maps)
        /// </summary>
        /// <param name="ServerGuid">The GUID of the map to search</param>
        /// <param name="Map">Name of the map</param>
        /// <param name="address">Repository address</param>
        /// <returns>String with the maplocation on the repository</returns>
        private String SearchMapRecursive(string ServerGuid, string Map, Uri address)
        {
            List<string> dirs = this.ShowDir(address);
            String returnDir = null;

            //Searcher for all dirs and uses them to search for maps
            //Search deeper into filesystem! TODO
            //Searches only 1 folder deep
            foreach (string dir in dirs)
            {
                string[] tmpDir = dir.Split('/');

                if (dir.Contains(ServerGuid + ".zip"))
                {
                    Console.WriteLine("Succes! Map {0} Found...", Map);
                    return dir;
                }
                else if (!dir.Contains("."))
                {

                    string newDir = tmpDir[tmpDir.GetLength(0) - 1];

                    returnDir = this.SearchMapRecursive(ServerGuid, Map, new Uri(address + "/" + newDir));
                    if (returnDir != null)
                    {
                        return returnDir;
                    }
                }



            }
            return returnDir;
        }







        /// <summary>
        /// Extracts the archive to the temp folder
        /// </summary>
        /// <param name="MapLocation"></param>
        /// <returns></returns>
        private Boolean Extract(String MapLocation)
        {

            string zipPath = MapLocation;

            if (!Directory.Exists(this.renXDir + "..//..//..//..//" + "UDKSeekerTempExtracted//"))
                Directory.CreateDirectory(this.renXDir + "..//..//..//..//" + "UDKSeekerTempExtracted//");


            string extractPath = this.renXDir + "..//..//..//..//" + "UDKSeekerTempExtracted//";

            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);              
                this.XCopy(this.renXDir + "..//..//..//..//" + "UDKSeekerTempExtracted//UDKGame", this.renXDir + "..//..//..//", true); //<-changed from root RenXFolder to UDKGame CHECK IF THIS WORKS!
                Directory.Delete(this.renXDir + "..//..//..//..//" + "UDKSeekerTempExtracted//", true);
                Directory.Delete(this.renXDir + "..//..//..//..//" + "UDKSeekerTemp//", true);
                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Copy's files from a folder to another keeping the tree intact.
        /// http://urenjoy.blogspot.nl/2008/11/copy-directory-or-move-folder-in-net.html
        /// </summary>
        /// <param name="src">The src folder</param>
        /// <param name="dest">The destination folderparam>
        /// <param name="isOverwrite">Overwrite existing files boolean</param>
        private void XCopy(String src, String dest, Boolean isOverwrite)
        {
            DirectoryInfo currentDirectory;
            currentDirectory = new DirectoryInfo(src);
            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);
            foreach (FileInfo filein in currentDirectory.GetFiles())
            {
                filein.CopyTo(System.IO.Path.Combine(dest, filein.Name), true);
                // To move files uncomment following line
                filein.Delete();
            }
            foreach (DirectoryInfo dr in currentDirectory.GetDirectories())
            {
                XCopy(dr.FullName, Path.Combine(dest, dr.Name), isOverwrite);
            }
        }


    }
}
