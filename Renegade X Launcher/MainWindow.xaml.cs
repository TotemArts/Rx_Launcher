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
using System.Windows.Threading;


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
        public const bool SHOW_DEBUG = false;

        public const int SERVER_REFRESH_RATE = 10000; // 10 sec
        public const int SERVER_AUTO_PING_RATE = 30000; // 30 sec
        public const int TICK_RATE = 1; // Once per second.
        private bool isOpen = false;
        public static readonly int MAX_PLAYER_COUNT = 64;
        public static MainWindow Instance { get; private set; }
        public ObservableCollection<ServerInfo> OFilteredServerList = new ObservableCollection<ServerInfo>();
        Thread TickThread = null;
        bool FoundLatestGameVersion = false;
        bool FoundLatestLauncherVersion = false;
        private DispatcherTimer refreshTimer;

        string messageText = "";

        const string MESSAGE_JOINGAME = "Establishing Battlefield Control... Standby...";
        const string MESSAGE_CANTSTARTGAME = "Error starting game executable.";
        const string MESSAGE_IDLE = "Welcome back commander.";

        private BitmapImage chkBoxOnImg;
        private BitmapImage chkBoxOffImg;
        public BitmapImage GetChkBxImg (bool Value)
        {
            if (chkBoxOnImg == null)
                chkBoxOnImg = new BitmapImage(new Uri("Resources/Checkbox_ON.png", UriKind.Relative));
            if (chkBoxOffImg == null)
                chkBoxOffImg = new BitmapImage(new Uri("Resources/Checkbox_OFF.png", UriKind.Relative));

            return Value ? chkBoxOnImg : chkBoxOffImg;
        }

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

            SD_GameVersion.Text = VersionCheck.GetGameVersion();
            VersionCheck.StartFindLauncherVersion();

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = new TimeSpan(0, 0, SERVER_REFRESH_RATE);
            refreshTimer.Tick += (object sender, EventArgs e) => StartRefreshingServers();
            refreshTimer.Start();
            StartRefreshingServers();

            //This attachs all the data we pulled from the Json link to our columns.
            BindDataColumns();

            BannerTools.Setup();

            if (Properties.Settings.Default.Username == "")
                ShowUsernameBox();
            else
                SD_Username.Content = Properties.Settings.Default.Username;

            SetMessageboxText(MESSAGE_IDLE);

            TickThread = new Thread(new ThreadStart(TickThreadFunc));
            TickThread.Start();            

            PingStats();
           // System.ComponentModel.BackgroundWorker Background = new System.ComponentModel.BackgroundWorker();
            //Background.WorkerSupportsCancellation = true;
            //Background.WorkerReportsProgress = false;
            //Background.DoWork += new System.ComponentModel.DoWorkEventHandler(TickThreadFunc);

            string Title = "Renegade-X Launcher v" + VersionCheck.GetLauncherVersion();
            this.Title = Title;
            TitleText.Content = Title;
            
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

            if (!FoundLatestGameVersion && VersionCheck.GetLatestGameVersion() != "")
                LatestGameVersionFound();

            if (!FoundLatestLauncherVersion && VersionCheck.GetLatestLauncherVersion() != "")
                LatestLauncherVersionFound();
        }

        void LatestGameVersionFound()
        {
            FoundLatestGameVersion = true;

            if (VersionCheck.GetGameVersion() == "")
            {
                SetMessageboxText("Could not locate installed game version. Latest is " + VersionCheck.GetLatestGameVersion());
                return;
            }

            if (VersionCheck.IsGameOutOfDate())
            {
                ShowGameUpdateWindow();
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

        void LatestLauncherVersionFound()
        {
            FoundLatestLauncherVersion = true;

            if (VersionCheck.IsLauncherOutOfDate())
            {
                ShowLauncherUpdateWindow();
            }
            else
            {
                SetMessageboxText("Launcher is up to date!");
                // If launcher is up to date, check to see if game is up to date.
                VersionCheck.StartFindGameVersion();
            }
        }

        void ShowGameUpdateWindow()
        {
            UpdateAvailableWindow theWindow = new UpdateAvailableWindow();
            theWindow.LatestVersionText.Content = VersionCheck.GetLatestGameVersion();
            theWindow.GameVersionText.Content = VersionCheck.GetGameVersion();
            theWindow.WindowTitle.Content = "Game Update Available!";
            theWindow.ShowDialog();

            if (theWindow.WantsToUpdate)
            {
                System.Diagnostics.Process.Start(VersionCheck.GAME_DOWNLOAD_URL);
                this.Close();
            }
        }

        void DownloadLauncherUpdate()
        {
            UpdateDownloadWindow theWindow = new UpdateDownloadWindow();
            theWindow.ShowDialog();

            if (theWindow.UpdateFinished)
            {
                SelfUpdater.ExecuteInstall();
                this.Close();
            }
        }

        void ShowLauncherUpdateWindow()
        {
            UpdateAvailableWindow theWindow = new UpdateAvailableWindow();
            theWindow.LatestVersionText.Content = VersionCheck.GetLatestLauncherVersion();
            theWindow.GameVersionText.Content = VersionCheck.GetLauncherVersion();
            theWindow.WindowTitle.Content = "Launcher Update Available!";
            theWindow.ShowDialog();

            if (theWindow.WantsToUpdate)
            {
                DownloadLauncherUpdate();
            }
        }

        private void OnGameExit()
        {
            SetMessageboxText(MESSAGE_IDLE);
        }

        private void BindDataColumns()
        {
            ServerInfoGrid.ItemsSource = OFilteredServerList;

            //PasswordedNameColumn.CellTemplateSelector = new LockTemplateSelector();
            //PasswordedNameColumn.ClipboardContentBinding = new Binding("ServerName");            

            ServerNameColumn.Binding = new Binding("ServerName");
            MapNameColumn.Binding  = new Binding("MapName");
            PlayerCountColumn.Binding = new Binding("PlayerCountString");
            PlayerCountColumn.SortMemberPath = "PlayerCount";
            //PlayerCountColumn.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            PingColumn.Binding = new Binding("PingString");
            //PlayerCountColumn.SortDirection = System.ComponentModel.ListSortDirection.Descending;
            PingColumn.SortMemberPath = "PingSort";

            ServerInfoGrid.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription(PlayerCountColumn.SortMemberPath,System.ComponentModel.ListSortDirection.Descending));

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

        public void RefilterServers()
        {
            var previousSelectedServer = GetSelectedServer();

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

            if (previousSelectedServer != null)
            {
                SetSelectedServer(previousSelectedServer.IPWithPort);
            }
        }

        private ServerInfo GetSelectedServer()
        {
            return ServerInfoGrid.SelectedValue as ServerInfo;
        }

        private void SetSelectedServer(string ipWithPort)
        {
            foreach (ServerInfo item in ServerInfoGrid.Items)
            {
                if (item.IPWithPort == ipWithPort)
                {
                    ServerInfoGrid.SelectedItem = item;
                    break;
                }
            }
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
            wc.OpenReadCompleted += new OpenReadCompletedEventHandler(OpenReadComplete);
            wc.OpenReadAsync(uri);
        }

        void OpenReadComplete(object o, OpenReadCompletedEventArgs args)
        {
            WebClient wc = o as WebClient;
            if (wc != null)
            {

            }
        }

        void JoinServer(string IP, string Password = "")
        {
            if (LaunchTools.JoinServer(IP, Properties.Settings.Default.Username,Password))
                {
                    if (SHOW_DEBUG)
                        SetMessageboxText(LaunchTools.GetArguments(IP, Properties.Settings.Default.Username, Password));
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

        private async Task RefreshServersAsync()
        {
            await ServerInfo.ParseJsonServersAsync();
            await ServerInfo.PingActiveServersAsync();
            RefilterServers();
        }

        private void StartRefreshingServers()
        {
#pragma warning disable 4014
            RefreshServersAsync();
#pragma warning restore 4014
        }

        private void SD_MaxPlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SD_MinPlayerSlider == null)
                return;

            if (filter_MaxPlayers != (int)SD_MaxPlayerSlider.Value)
            {
                filter_MaxPlayers = (int)SD_MaxPlayerSlider.Value;
                SD_MaxPlayerDile.Content = filter_MaxPlayers;
                RefilterServers();
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
                RefilterServers();
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
            SD_PlayerLimit.Content = selected.MaxPlayers.ToString();
            SD_ServerVersion.Text = selected.GameVersion;
            SD_VehicleLimit.Content = selected.VehicleLimit;

            Autobalance_Checkbx.Source = GetChkBxImg(selected.AutoBalance);
            Steam_Checkbx.Source = GetChkBxImg(selected.SteamRequired);
            Crates_Checkbx.Source = GetChkBxImg(selected.SpawnCrates);
            InfantryOnly_Checkbx.Source = GetChkBxImg(selected.VehicleLimit <= 0);

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
            ShowUsernameBox();
        }

        private void ShowUsernameBox()
        {
            UsernameWindow login = new UsernameWindow();
            login.m_Username = Properties.Settings.Default.Username;
            login.SD_UsernameBox.Text = Properties.Settings.Default.Username;

            login.ShowDialog();

            Properties.Settings.Default.Username = login.m_Username;
            Properties.Settings.Default.Save();
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
            StartRefreshingServers();
        }

        private void sv_ServerSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefilterServers();
        }

        private void SD_Filter_SameVersionOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            RefilterServers();
        }

        private void SD_Filter_SameVersionOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsInitialized) return;
            RefilterServers();
        }
    }
}