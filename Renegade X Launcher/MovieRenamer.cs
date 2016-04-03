using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LauncherTwo
{
    class MovieRenamer
    {
        /// <summary>
        /// Renames the movies in the Renegade X folder so they dont play
        /// </summary>
        public static bool MovieRenamerMethod(bool SkipMovie)
        {
            
            if (GameInstallation.IsRootPathPlausible())
            {
                try {

                    if (File.Exists(GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bak") && File.Exists(GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bik"))
                    {
                        switch (Properties.Settings.Default.SkipIntroMovies)
                        {
                            case true:
                                File.Delete(GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bik");
                                break;
                            case false:
                                File.Delete(GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bak");
                                break;
                        }
                    }

                    if (SkipMovie)
                    {
                        
                        System.IO.File.Move(GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bik", GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bak");
                    }
                    else
                    {
                        System.IO.File.Move(GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bak", GameInstallation.GetRootPath() + "UDKGame\\Movies\\UE3_logo.bik");
                    }
                }
                catch
                {
                    System.Windows.MessageBox.Show("Error while changing intromovies!");
                    return false;
                }
            }
            return true;
            
            
        }
    }
}
