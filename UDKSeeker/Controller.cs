using System;
using System.Collections.Generic;


namespace CustomContentSeeker
{
    /// <summary>
    /// Main entry for the seeker to talk to. Downloads maps, creates views.
    /// </summary>
    public class Controller
    {
        private CustomContentSeeker.UdkSeeker udkseeker;
        private List<CustomContentSeeker.Level> levels;
        
        public Controller(String MAP_REPO_ADRESS)
        {
            this.udkseeker = new CustomContentSeeker.UdkSeeker(MAP_REPO_ADRESS, "Launcher", "CustomMaps199");
            this.udkseeker.PropertyChanged += Udkseeker_PropertyChanged;

        }

        private void Udkseeker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Console.Write(this.udkseeker.getBytes());
        }

        public void SearchMaps(String selectedServerIp)
        {
            //Get the maplist of the server
            CustomContentSeeker.JSONRotationRetriever JSON = new CustomContentSeeker.JSONRotationRetriever(selectedServerIp);
            this.levels = JSON.getMaps();
            this.DownloadMaps();
        }

        private void DownloadMaps()
        {
            CustomContentSeeker.UdkSeeker.Status currentStatus = CustomContentSeeker.UdkSeeker.Status.Finished;//Status is finished untill other status gets pushed
            if (this.levels != null && levels.Count > 0)
            {

                foreach (CustomContentSeeker.Level level in levels)
                {
                    if (false)//token.IsCancellationRequested)
                    {
                        currentStatus = CustomContentSeeker.UdkSeeker.Status.Cancelled;
                        break;
                    }
                    CustomContentSeeker.UdkSeeker.Status Status = this.udkseeker.Seek(level.Name, level.GUID);//Seek a map
                    if (Status != CustomContentSeeker.UdkSeeker.Status.MapSucces)
                    {
                        currentStatus = Status;
                        Console.WriteLine("Could not download all the maps on the server. It may be possible you can't play all the maps.\nContinue downloading the other maps? (Y/N)");

                    }
                }
            }
            else//something wrong with the maplist? (No JSON) Show maplisterror
            {
                currentStatus = CustomContentSeeker.UdkSeeker.Status.MaplistError;
            }
            // return currentStatus;
        }
    }
}
