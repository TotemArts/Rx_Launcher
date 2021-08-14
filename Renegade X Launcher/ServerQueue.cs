using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Windows.Controls;

namespace LauncherTwo
{
    class ServerQueue
    {
        String[] serverAddressAndPort;
        System.Windows.Threading.DispatcherTimer queueTimer;
        int maxPlayers;
        bool isQueued = false;
        public ServerQueue()
        {        
            queueTimer = new System.Windows.Threading.DispatcherTimer();
            queueTimer.Interval = new TimeSpan(0, 1, 0);
            queueTimer.Tick += (object sender, EventArgs e) => QueueCheck();
        }

        /// <summary>
        /// Enqueues in the client side queue. Only one server at a time may be queued per object.
        /// </summary>
        /// <param name="queuedServer">ServerIfo object containing the slected server</param>
        /// <returns>A bool if the enqueue is succesfull</returns>
        public bool Enqueue(ServerInfo queuedServer)
        {
            
            if(!this.isQueued)
            {
                this.maxPlayers = queuedServer.MaxPlayers;
                this.serverAddressAndPort = queuedServer.IPWithPort.Split(':');
                this.QueueCheck();
                queueTimer.Start();
                this.isQueued = true;
                return true;
            }
            else
            {
                this.queueTimer.Stop();
                this.isQueued = false;
                return false;
            }
            
        }

        /// <summary>
        /// Checks if the player can join the queued server
        /// </summary>
        private void QueueCheck()
        {
            using (System.Net.WebClient queueRequest = new System.Net.WebClient())
            {
                String queueResponse = queueRequest.DownloadString(new Uri("http://serverlist.renegade-x.com/server.jsp?ip=" + this.serverAddressAndPort[0] + "&port=" + this.serverAddressAndPort[1]));
                dynamic content = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(queueResponse);

                int playerCount = content.PlayerList.Count;//This aint working
 
                if (playerCount < this.maxPlayers)
                {
                    this.queueTimer.Stop();
                    ModernDialog t = new ModernDialog();
                    t.Title = "Time to rock and roll";
                    t.Content = "You can join the server!\nDo you want to join?";
                    t.Buttons = new Button[] { t.YesButton, t.NoButton };
                    t.Topmost = true;
                    t.ShowDialog();
                }
                
            }
            
        }

        //String[] ServerAddressAndPort = ServerAddress.Split(':');
        //new Uri("http://serverlist.renegade-x.com/server.jsp?ip=" + ServerAddressAndPort[0] + "&port=" + ServerAddressAndPort[1])
        //Content = JsonConvert.DeserializeObject<ServerContent>(GUIDJsonResponse);


    }
}
