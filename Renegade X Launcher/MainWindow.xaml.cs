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
using System.ComponentModel;
using FirstFloor.ModernUI.Windows.Controls;
using System.Collections.Generic;


namespace LauncherTwo
{
    public partial class MainWindow : RXWindow, INotifyPropertyChanged
    {
        public const bool SHOW_DEBUG = false;

        public const int SERVER_REFRESH_RATE = 10000; // 10 sec
        public const int SERVER_AUTO_PING_RATE = 30000; // 30 sec
        public static readonly int MAX_PLAYER_COUNT = 64;
        public TrulyObservableCollection<ServerInfo> OFilteredServerList { get; set; }
        private DispatcherTimer refreshTimer;
        private GameInstance _GameInstance;
        public GameInstance GameInstance
        {
            get { return _GameInstance; }
            set
            {
                _GameInstance = value;
                NotifyPropertyChanged("GameInstance");
                NotifyPropertyChanged("IsLaunchingPossible");
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public string TitleValue { get { return "Renegade-X Launcher v" + VersionCheck.GetLauncherVersion(); } }
        public bool IsLaunchingPossible { get { return GameInstance == null; } }

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
            OFilteredServerList = new TrulyObservableCollection<ServerInfo>();

            InitializeComponent();

            SetMessageboxText(MESSAGE_IDLE); // This must be set before any asynchronous code runs, as it might otherwise be overridden.

            SD_GameVersion.Text = VersionCheck.GetGameVersion();
            StartCheckingVersions();

            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = new TimeSpan(0, 0, SERVER_REFRESH_RATE);
            refreshTimer.Tick += (object sender, EventArgs e) => StartRefreshingServers();
            refreshTimer.Start();
            StartRefreshingServers();

            ServerInfoGrid.Items.SortDescriptions.Add(new SortDescription(PlayerCountColumn.SortMemberPath, ListSortDirection.Ascending));

            BannerTools.Setup();
            SD_ClanHeader.Cursor = BannerTools.GetBannerLink(null) != "" ? Cursors.Hand : null;

            if (Properties.Settings.Default.Username == "")
                ShowUsernameBox();
            else
                SD_Username.Content = Properties.Settings.Default.Username;
        }

        private async Task CheckVersionsAsync()
        {
            Task launcherTask = VersionCheck.FindLauncherVersionAsync();
            Task gameTask = VersionCheck.FindGameVersionAsync();

            await launcherTask;
            if (VersionCheck.IsLauncherOutOfDate())
            {
                ShowLauncherUpdateWindow();
            }
            else
            {
                SetMessageboxText("Launcher is up to date!");
            }

            await gameTask;
            if (VersionCheck.GetGameVersion() == "")
            {
                SetMessageboxText("Could not locate installed game version. Latest is " + VersionCheck.GetLatestGameVersion());
            }
            else if (VersionCheck.IsGameOutOfDate())
            {
                ShowGameUpdateWindow();
            }
            else
            {
                SetMessageboxText("Game is up to date! " + VersionCheck.GetGameVersion());
            }
        }

        private void StartCheckingVersions()
        {
#pragma warning disable 4014
            CheckVersionsAsync();
#pragma warning restore 4014
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

        private async void Join_Server_Btn_Click(object sender, RoutedEventArgs e)
        {
            ServerInfo SelectedServerInfo = GetSelectedServer();
            if (SelectedServerInfo != null)
            {
                string password = null;
                if (GetSelectedServer().PasswordProtected)
                {
                    PasswordWindow PassWindow = new PasswordWindow();
                    PassWindow.ShowDialog();
                    if (!PassWindow.WantsToJoin)
                    {
                        return;
                    }
                    password = PassWindow.Password;
                }
                await StartGameInstance(GetSelectedServer().IPWithPort, password);
            }
        }

        private async Task RefreshServersAsync()
        {
            await ServerInfo.ParseJsonServersAsync();
            RefilterServers();
            await ServerInfo.PingActiveServersAsync();
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
            SD_ClanHeader.Cursor = BannerTools.GetBannerLink(selected.IPAddress) != "" ? Cursors.Hand : null;

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
        }

        private void SD_ClanHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ServerInfo selected = GetSelectedServer();
            BannerTools.LaunchBannerLink(selected != null ? selected.IPAddress : null);
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

        private async Task StartGameInstance(string ipEndpoint, string password)
        {
            try
            {
                SetMessageboxText("The game is running.");

                GameInstanceStartupParameters startupParameters = new GameInstanceStartupParameters();
                startupParameters.Username = Properties.Settings.Default.Username;
                startupParameters.IPEndpoint = ipEndpoint;
                startupParameters.Password = password;
                GameInstance = GameInstance.Start(startupParameters);

                await GameInstance.Task;

                SetMessageboxText(MESSAGE_IDLE);
            }
            catch (Exception)
            {
                SetMessageboxText(MESSAGE_CANTSTARTGAME);
            }
            finally
            {
                GameInstance = null;
            }
        }

        private async void SD_LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            await StartGameInstance(null, null);
        }

        private async void SD_ConnectIP_Click(object sender, RoutedEventArgs e)
        {
            JoinIPWindow IPWindow = new JoinIPWindow();
            IPWindow.ShowDialog();
            if (IPWindow.WantsToJoin)
            {
                await StartGameInstance(IPWindow.IP, IPWindow.Pass);
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                StartRefreshingServers();
            }
        }
    }
}