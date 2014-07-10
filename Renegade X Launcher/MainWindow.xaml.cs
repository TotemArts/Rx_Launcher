using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LauncherTwo.Views;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Reflection;
using System.Globalization;
using System.Net;
using System;


namespace LauncherTwo
{
    public class LockTemplateSelector : DataTemplateSelector
    {
        public DataTemplate LockedTemplate { get; set; }
        public DataTemplate UnlockedTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item,
                      DependencyObject container)
        {
            if (item == null)
                return LockedTemplate;
            return UnlockedTemplate;

            //if ((bool)item == true)
            //    return LockedTemplate;
            //else
            //    return UnlockedTemplate;
        }
    }
    public partial class MainWindow : Window
    {
        public const bool SHOW_DEBUG = true;

        public const int SERVER_REFRESH_RATE = 10000; // 10 sec
        public const int SERVER_AUTO_PING_RATE = 30000; // 30 sec
        public const int TICK_RATE = 1; // Once per second.
        private bool isOpen = false;
        public static readonly int MAX_PLAYER_COUNT = 64;
        public static MainWindow Instance { get; private set; }
        public ObservableCollection<ServerInfo> OFilteredServerList = new ObservableCollection<ServerInfo>();
        Thread TickThread = null;
        bool FoundLatestVersion = false;

        string messageText = "";

        const string MESSAGE_JOINGAME = "Establishing Battlefield Control... Standby...";
        const string MESSAGE_CANTSTARTGAME = "Error starting game executable.";
        const string MESSAGE_IDLE = "Welcome back commander.";

        #region -= Filters =-
        private int filter_MaxPlayers = 64;
        private int filter_MinPlayers = 0;
        #endregion -= Filters =-

        public MainWindow()
        {
            Instance = this;

            

            //We want the window to be in the middle of the screen. 
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            //The init step for the window.
            InitializeComponent();

            //This will poll the json site for all active servers.
            //AutoRefresh();

            //This will update the servers pings every X seconds.
            //AutoPingUpdate();

            //SD_GameVersion.Text = VersionCheck.GetGameVersion();
            //VersionCheck.StartFindVersion();

            RefreshServers();
            FilterServers();

            //This attachs all the data we pulled from the Json link to our columns.
            BindDataColumns();

            BannerTools.Setup();

            UsernameWindow usernameWindow = new UsernameWindow();

            if ( Properties.Settings.Default.Username == "")
            {
                usernameWindow.m_Username = Properties.Settings.Default.Username;

                usernameWindow.ShowDialog();

                Properties.Settings.Default.Username = usernameWindow.m_Username;

                Properties.Settings.Default.Save();

                SD_Username.Content = usernameWindow.m_Username;
            } 
            else
            {
                SD_Username.Content = Properties.Settings.Default.Username;
            }

            SetMessageboxText(MESSAGE_IDLE);

            TickThread = new Thread(new ThreadStart(TickThreadFunc));
            TickThread.Start();            

            PingStats();
           // System.ComponentModel.BackgroundWorker Background = new System.ComponentModel.BackgroundWorker();
            //Background.WorkerSupportsCancellation = true;
            //Background.WorkerReportsProgress = false;
            //Background.DoWork += new System.ComponentModel.DoWorkEventHandler(TickThreadFunc);
        }

        


        private void TickThreadFunc()
        {
            while (true)
            {
                Thread.Sleep((int)(1000.0f / TICK_RATE));
                // Have the dispatcher call tick, so we don't have to worry about cross-thread stuff.
                Dispatcher.Invoke(Tick);
            }
        }

        private void Tick ()
        {
            if (GetMessageboxText() == MESSAGE_JOINGAME && !LaunchTools.LastRunStillRunning())
            {
                OnGameExit();
            }

            if (!FoundLatestVersion && VersionCheck.GetLatestVersion() != "")
                LatestVersionFound();
        }

        void LatestVersionFound()
        {
            FoundLatestVersion = true;

            if (VersionCheck.GetGameVersion() == "")
            {
                SetMessageboxText("Could not locate installed game version. Latest is " + VersionCheck.GetLatestVersion());
                return;
            }

            if (VersionCheck.IsOutOfDate())
            {
                ShowUpdateWindow();
            }
            else
            {
                SetMessageboxText("Game is up to date! " + VersionCheck.GetGameVersion());
            }

            //if (SHOW_DEBUG)
            //{
            //    SetMessageboxText("Latest Version: " + VersionCheck.GetLatestVersion() + " ( " + VersionCheck.GetLatestVersionNumerical().ToString() + ") Game Version: " + VersionCheck.GetGameVersion() + " (" + VersionCheck.GetGameVersionNumerical().ToString() + ")");
            //}
        }

        void ShowUpdateWindow()
        {
            UpdateAvailableWindow theWindow = new UpdateAvailableWindow();
            theWindow.ShowDialog();

            if (theWindow.WantsToUpdate)
            {
                System.Diagnostics.Process.Start(VersionCheck.DOWNLOAD_URL);
                this.Close();
            }
        }

        private void OnGameExit()
        {
            SetMessageboxText(MESSAGE_IDLE);
        }


        private async void AutoPingUpdate()
        {
            while (true)
            {
                ServerInfo.PingActiveServers();
                RefreshServers();
                FilterServers();
                await Task.Delay(SERVER_AUTO_PING_RATE);
            }
        }

        private async void AutoRefresh()
        {
            while (true)
            {
                int select = ServerInfoGrid.SelectedIndex;
                RefreshServers();
                FilterServers();
                ServerInfoGrid.SelectedIndex = select;

                FocusManager.SetFocusedElement(ServerInfoGrid, null);

                await Task.Delay(SERVER_REFRESH_RATE);
            }
        }

        private void BindDataColumns()
        {
            ServerInfoGrid.ItemsSource = OFilteredServerList;

            PasswordedNameColumn.CellTemplateSelector = new LockTemplateSelector();
            //PasswordedNameColumn.ClipboardContentBinding = new Binding("ServerName");

            ServerNameColumn.Binding = new Binding("ServerName");
            MapNameColumn.Binding  = new Binding("MapName");
            PlayerCountColumn.Binding = new Binding("PlayerCountString");
            PlayerCountColumn.SortMemberPath = "PlayerCount";
            //PlayerCountColumn.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            PingColumn.Binding = new Binding("PingString");
            PlayerCountColumn.SortDirection = System.ComponentModel.ListSortDirection.Descending;
            PingColumn.SortMemberPath = "PingSort";

            //Reset our grid length
            ServerContentSplit.RowDefinitions[0].Height = new GridLength(40);

            //Since we don't use the scroll bars we will hide them
            ServerInfoGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (isOpen)
            {
                ServerContentSplit.RowDefinitions[0].Height = new GridLength(40);
                isOpen = false;
            }
            else
            {
                ServerContentSplit.RowDefinitions[0].Height = new GridLength(200);
                isOpen = true;
            }
        }

        public void FilterServers()
        {
            //If we don't have an active server list we want to return
            if (ServerInfo.ActiveServers == null)
                return;

            OFilteredServerList.Clear();

            foreach (ServerInfo info in ServerInfo.ActiveServers)
            {
                if (sv_ServerSearch.Text != "")
                {
                    if (info.ServerName.ToLower().Contains(sv_ServerSearch.Text.ToLower()) ||
                        info.IPAddress == sv_ServerSearch.Text ||
                        info.IPWithPort == sv_ServerSearch.Text)
                    {
                        OFilteredServerList.Add(info);
                    }
                }
                else
                    OFilteredServerList.Add(info);
            }

            bool SameVersionOnly = (SD_Filter_SameVersionOnly.IsChecked.HasValue) ? SD_Filter_SameVersionOnly.IsChecked.Value : false;

            for (int i = OFilteredServerList.Count - 1; i > -1; i--)
            {
                if (OFilteredServerList[i].PlayerCount < filter_MinPlayers)
                {
                    OFilteredServerList.RemoveAt(i);
                    continue;
                }

                if (OFilteredServerList[i].PlayerCount > filter_MaxPlayers)
                {
                    OFilteredServerList.RemoveAt(i);
                    continue;
                }

                if (SameVersionOnly && VersionCheck.GetGameVersion() != "" && OFilteredServerList[i].GameVersion != VersionCheck.GetGameVersion())
                {
                    OFilteredServerList.RemoveAt(i);
                    continue;
                }   
            }
        }

        private ServerInfo GetSelectedServer()
        {
            return ServerInfoGrid.SelectedValue as ServerInfo;
        }

        private void Join_Server_Btn_Click(object sender, RoutedEventArgs e)
        {
            ServerInfo SelectedServerInfo = GetSelectedServer();
            if (SelectedServerInfo != null)
            {
                if (GetSelectedServer().PasswordProtected)
                    JoinPasswordServer(GetSelectedServer().IPWithPort);
                else
                    JoinServer(GetSelectedServer().IPWithPort);
            }
        }

        void PingStats()
        {
            Uri uri = new Uri("http://www.renegade-x.com/launcher_data/launcher_ping.html", UriKind.Absolute);
            WebClient wc = new WebClient();
            wc.OpenReadAsync(uri);
        }

        void JoinServer(string IP, string Password = "")
        {
            if (LaunchTools.JoinServer(IP, Properties.Settings.Default.Username,Password))
                {
                    if (SHOW_DEBUG)
                        SetMessageboxText(IP);
                    else
                        SetMessageboxText(MESSAGE_JOINGAME);
                }
                else
                {
                    if (SHOW_DEBUG)
                        SetMessageboxText(IP);
                    else
                        SetMessageboxText(MESSAGE_CANTSTARTGAME);
                }
        }

        private void RefreshServers(bool FilterResults = false)
        {
            //This requests all the servers from the RenX Json link.
           // ServerInfo.ParseServers();
            ServerInfo.ParseJsonServers();

            //Empty the list
            OFilteredServerList.Clear();

            //Reset the list
            foreach (ServerInfo info in ServerInfo.ActiveServers)
                OFilteredServerList.Add(info);

        }

        private void SD_MaxPlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SD_MinPlayerSlider == null)
                return;

            if (filter_MaxPlayers != (int)SD_MaxPlayerSlider.Value)
            {
                filter_MaxPlayers = (int)SD_MaxPlayerSlider.Value;
                SD_MaxPlayerDile.Content = filter_MaxPlayers;
                //FilterServers(); // Do filtering on button press now.
            }            
        }

        private void SD_MinPlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SD_MinPlayerSlider == null)
                return;

            if (filter_MinPlayers != (int)SD_MinPlayerSlider.Value)
            {
                filter_MinPlayers = (int)SD_MinPlayerSlider.Value;
                SD_MinPlayerDile.Content = filter_MinPlayers;
                //FilterServers(); // Do filtering on button press now.
            }

        }

        private void ServerInfoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerInfoGrid.SelectedIndex >= OFilteredServerList.Count || ServerInfoGrid.SelectedIndex <= -1)
                return;

            ServerInfo selected = GetSelectedServer();

            sv_MapPreview.Source = MapPreviewSettings.GetMapImage(selected.MapName);

            SD_ClanHeader.Source = BannerTools.GetBanner(selected.IPAddress);

            SD_Name.Text = selected.ServerName;
            SD_IP.Text = selected.IPWithPort;
            SD_GameLength.Content = selected.TimeLimit.ToString();
            SD_MineLimit.Content = selected.MineLimit.ToString();
            SD_usesCrates.IsChecked = selected.SpawnCrates;
            SD_usesAutoBalance.IsChecked = selected.AutoBalance;
            SD_steamRequired.IsChecked = selected.SteamRequired;
            SD_PlayerLimit.Content = selected.MaxPlayers.ToString();
            SD_ServerVersion.Text = selected.GameVersion;
            SD_VehicleLimit.Content = selected.VehicleLimit;

            if (selected.VehicleLimit <= 0)
                SD_usesInfantryOnly.IsChecked = true;
            else
                SD_usesInfantryOnly.IsChecked = false;

            ServerInfoGrid.UpdateLayout();
        }

        public void SetMessageboxText(string text)
        {
            MessageBoxText.Text = text;
            messageText = text;
        }

        public string GetMessageboxText()
        {
            return messageText;
        }

        private void XBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);
            TickThread.Abort();

            Application.Current.Shutdown();
        }


        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if( e.GetPosition(this).Y < 35 )
                this.DragMove();
            }
        }

        private void SD_ClanHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ServerInfoGrid.SelectedIndex >= OFilteredServerList.Count || ServerInfoGrid.SelectedIndex <= -1)
               return;

            ServerInfo selected = GetSelectedServer();

            BannerTools.LaunchBannerLink(selected.IPAddress);
        }

        private void SD_EditUsernameBtn_Click(object sender, RoutedEventArgs e)
        {
            UsernameWindow login = new UsernameWindow();
            login.m_Username = Properties.Settings.Default.Username;
            login.SD_UsernameBox.Text = Properties.Settings.Default.Username;
            login.ShowDialog();
            SD_Username.Content = login.m_Username; 
        }

        private void MinBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        }

        

        private void SD_LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            LaunchTools.LaunchGame(Properties.Settings.Default.Username);
            SetMessageboxText("Launching Renegade-X");
        }

        void JoinPasswordServer(string IP)
        {
            PasswordWindow PassWindow = new PasswordWindow();

            PassWindow.ShowDialog();

            if (PassWindow.WantsToJoin)
            {
                JoinServer(IP, PassWindow.Password);
            }
        }

        private void SD_ConnectIP_Click(object sender, RoutedEventArgs e)
        {
            JoinIPWindow IPWindow = new JoinIPWindow();

            IPWindow.ShowDialog();

            if (IPWindow.WantsToJoin)
            {
                JoinServer(IPWindow.IP, IPWindow.Pass);
            }
        }

        private void sd_Refresh_MouseDown(object sender, RoutedEventArgs e)
        {
            RefreshServers();
            FilterServers();
        }

        private void sd_SearchClick(object sender, RoutedEventArgs e)
        {
            FilterServers();
        }

        private void SD_ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            FilterServers();
        }
    }
}