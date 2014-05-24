using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SteamWebAPI;


namespace LauncherTwo.Steam
{
    class SteamLogin
    {
        public static SteamAPISession Session { get; protected set; }
        public static SteamAPISession.LoginStatus Status { get; protected set; }
        public static SteamAPISession.User User { get; protected set; }

        private static string username;
        private static string password; 

        public static void Login(string aUsername, string aPassword)
        {
            username = aUsername;
            password = aPassword;

            Status = Session.Authenticate(aUsername, aPassword);

            if(Status == SteamAPISession.LoginStatus.LoginSuccessful )
            {
                // We are logged in and good to go
                User = Session.GetUserInfo();
                
            }

            else
            {
                //Failed to log in
            }
        }

        public static void UnlockSteamGuard(string aGaurdPassword)
        {
            Status = Session.Authenticate(username, password, aGaurdPassword); 
        }
    }
}
