using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irc;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace LauncherTwo
{
    class IrcController : INotifyPropertyChanged
    {
        private const String ERROR = "SOMETHING WENT WRONG, PLEASE TRY AGAIN";

        /// <summary>
        /// PropertyChanged event handler
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Port number
        /// </summary>
        private int port;

        /// <summary>
        /// IrcClient object that handles all the calls
        /// </summary>
        private IrcClient Client { get; set; }

        /// <summary>
        /// String that contains the username/Nickname
        /// </summary>
        public String Username { get; set; }
        /// <summary>
        /// String that contains the password(Optional)
        /// </summary>
        private String Password { get; set; }

        /// <summary>
        /// String that contains an errormessage if there is an error
        /// </summary>
        public String ErrorMessage { get; private set; }

        /// <summary>
        /// String that contains the serveradress
        /// </summary>
        public String Server { get; private set; }

        /// <summary>
        /// String that contains the channel
        /// </summary>
        public String Channel { get; private set; }


        //public List<string> messages = new List<string>();

        private String _Messages { get; set; }

        public String Messages
        {
            get
            {
                return this._Messages;
            }
            set
            {
                if (!this._Messages.Equals(value))
                {
                    this._Messages = value;
                    //Notify all subscribers that "Messages" has changed;
                    NotifyPropertyChanged("Messages");
                }
            }
        }

        /// <summary>
        /// Collection that contains all the servers.
        /// </summary>
        private System.Collections.ObjectModel.ObservableCollection<String> _ConnectionList;

        /// <summary>
        /// Event serverlist
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<String> ConnectionList
        {
            get
            {
                return this._ConnectionList;
            }
            set
            {
                if (!this._ConnectionList.Equals(value))
                {
                    this._ConnectionList = value;
                    //Notify all subscribers that "ConnectionList" has changed;
                    NotifyPropertyChanged("ConnectionList");
                }
            }
        }


        /// <summary>
        /// Default constructor with default data (Connects to irc.renegade-x.com)
        /// </summary>
        public IrcController()
        {
            this.Client = new IrcClient("irc.renegade-x.com");
            this.Server = this.Client.Server;
            this.Events();
            this.Username = "RenegadeX_launcher_test";
            this.Password = "huehuehue";
            this.Client.Nick = this.Username;
            this._Messages = "Connecting...";
        }

        /// <summary>
        /// Creates an instance of the Irc class
        /// </summary>
        /// <param name="adress">The adress to connect to</param>
        /// <param name="Username">The desired username</param>
        public IrcController(String adress, String Username, String Channel)
        {
            this.Client = new IrcClient(adress);
            this.Server = adress;

            if (!Channel.Contains("#"))
            {
                this.Channel = "#" + Channel;
            }
            else
            {
                this.Channel = Channel;
            }

            this.Events();
            this.Username = Username;
            this.Client.Nick = this.Username;
            this._Messages = "Connecting...";
            this._ConnectionList = new ObservableCollection<string>();
        }

        /// <summary>
        /// Creates an instance of the Irc class
        /// </summary>
        /// <param name="adress">The adress to connect to</param>
        /// <param name="Username">The desired username</param>
        /// <param name="Password">The required password</param>
        public IrcController(String adress, String Username, String Channel, String Password)
        {
            this.Client = new IrcClient(adress);
            this.Server = adress;

            if (!Channel.Contains("#"))
            {
                this.Channel = "#" + Channel;
            }
            else
            {
                this.Channel = Channel;
            }

            this.Events();
            this.Username = Username;
            this.Password = Password;
            this.Client.Nick = this.Username;
            this._Messages = "Connecting...";

        }

        /// <summary>
        /// Function that creates all events
        /// </summary>
        private void Events()
        {
            this.Client.ChannelMessage += Client_ChannelMessage;
            //client.ExceptionThrown += client_ExceptionThrown;
            this.Client.NoticeMessage += Client_NoticeMessage;
            this.Client.OnConnect += Client_OnConnect;
            this.Client.PrivateMessage += Client_PrivateMessage;
            //this.Client.ServerMessage += Client_ServerMessage;
            this.Client.UpdateUsers += Client_UpdateUsers;
            this.Client.UserJoined += Client_UserJoined;
            this.Client.UserLeft += Client_UserLeft;
            //client.UserNickChange += client_UserNickChange;
        }

        /// <summary>
        /// Connects to the server given in the constructor
        /// </summary>
        /// <returns>Boolean if connection is established</returns>
        public Boolean Connect()
        {
            try
            {
                this.Client.Connect();
                return true;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = ex.Message;
            }
            return false;
        }

        /// <summary>
        /// Disconnect from current server
        /// </summary>
        /// <returns>Boolean if the disconnection has been succesfull</returns>
        public Boolean Disconnect()
        {
            try
            {
                this.Client.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// When a user leaves, notify the current users and remove the user from the connectionlist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_UserLeft(object sender, UserLeftEventArgs e)
        {
            this.Messages = this.Messages + "\n" + e.User + " has left. ";
            this.ConnectionList.Remove(e.User);
        }

        /// <summary>
        /// When the connection has made, switch to channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_OnConnect(object sender, EventArgs e)
        {
            this.Client.JoinChannel(this.Channel);
        }

        /// <summary>
        /// Send a message to the IRC channel
        /// </summary>
        /// <param name="message">The message to be sent</param>
        public void SendMsg(String message)
        {
            try
            {
                this.Client.SendMessage(this.Channel, message);
                this.Messages = this.Messages + "\n" + this.Username + ": " + message;
            }
            catch (Exception)
            {
                this.Messages = this.Messages + "\n" + this.Username + ": " + message;
            }

        }

        /// <summary>
        /// Capture the servermessages and display them to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_NoticeMessage(object sender, EventArgs e)
        {
            sender.ToString();
            NoticeMessageEventArgs Message = (Irc.NoticeMessageEventArgs)e;
            this.Messages = this.Messages + "\n" + Message.Message;
        }

        /// <summary>
        /// Capture and display any channel messages and display them to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_ChannelMessage(object sender, ChannelMessageEventArgs e)
        {
            ChannelMessageEventArgs ChannelMessage = e;
            this.Messages = this.Messages + "\n" + e.From + ": " + ChannelMessage.Message;
        }

        /// <summary>
        /// Capture any private messages and display them to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_PrivateMessage(object sender, PrivateMessageEventArgs e)
        {
            PrivateMessageEventArgs PrivateMessage = e;
            this.Messages = this.Messages + "\nPrivate message from " + e.From + ": " + PrivateMessage.Message;
        }


        /// <summary>
        /// Capture and display if an user has joined the channel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_UserJoined(object sender, UserJoinedEventArgs e)
        {
            this.Messages = this.Messages + "\n" + e.User + " has joined. ";
            this.ConnectionList.Add(e.User);
        }

        /// <summary>
        /// Capture all users and display them in the ConnectionList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Client_UpdateUsers(object sender, UpdateUsersEventArgs e)
        {
            this.ConnectionList.Clear();
            foreach (String connection in e.UserList)
            {
                this.ConnectionList.Add(connection);
            }

        }

        /// <summary>
        /// Notifies all the subscribers that a property has changed
        /// </summary>
        /// <param name="propertyName">The name of the property that has changed, leave blank for an empty property</param>
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
