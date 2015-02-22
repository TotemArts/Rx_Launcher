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
using RXPatchLib;
using System.IO;
using System.Linq;
using System.Net.Sockets;


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
        private EngineInstance _GameInstance;
        public EngineInstance GameInstance
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

        public string TitleValue { get { return "Renegade-X Launcher v" + VersionCheck.GetLauncherVersionName(); } }
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
            ServerInfoGrid.Items.SortDescriptions.Add(new SortDescription(PlayerCountColumn.SortMemberPath, ListSortDirection.Ascending));

            SD_GameVersion.Text = VersionCheck.GetGameVersionName();

            BannerTools.Setup();
            SD_ClanHeader.Cursor = BannerTools.GetBannerLink(null) != "" ? Cursors.Hand : null;

            SourceInitialized += (s, a) =>
            {
                StartCheckingVersions();

                refreshTimer = new DispatcherTimer();
                refreshTimer.Interval = new TimeSpan(0, 0, SERVER_REFRESH_RATE);
                refreshTimer.Tick += (object sender, EventArgs e) => StartRefreshingServers();
                refreshTimer.Start();
                StartRefreshingServers();

                if (Properties.Settings.Default.Username == "")
                    ShowUsernameBox();
                else
                    SD_Username.Content = Properties.Settings.Default.Username;
            };
        }

        private async Task CheckVersionsAsync()
        {
            Task updateTask = VersionCheck.UpdateLatestVersions();
            await updateTask;

            if (VersionCheck.IsLauncherOutOfDate())
            {
                ShowLauncherUpdateWindow();
            }
            else
            {
                SetMessageboxText("Launcher is up to date!");
            }

            if (VersionCheck.GetGameVersionName() == "")
            {
                SetMessageboxText("Could not locate installed game version. Latest is " + VersionCheck.GetLatestGameVersionName());
            }
            else if (VersionCheck.IsGameOutOfDate())
            {
                ShowGameUpdateWindow();
                SD_GameVersion.Text = VersionCheck.GetGameVersionName();
                SetMessageboxText("Game was updated! " + VersionCheck.GetGameVersionName());
            }
            else
            {
                SetMessageboxText("Game is up to date! " + VersionCheck.GetGameVersionName());
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
            theWindow.LatestVersionText.Content = VersionCheck.GetLatestGameVersionName();
            theWindow.GameVersionText.Content = VersionCheck.GetGameVersionName();
            theWindow.WindowTitle.Content = "Game update available!";
            theWindow.Owner = this;
            theWindow.ShowDialog();

            if (theWindow.WantsToUpdate)
            {
                var targetDir = GameInstallation.GetRootPath();
                var applicationDir = Path.Combine(GameInstallation.GetRootPath(), "patch");
                var patchUrl = VersionCheck.GamePatchUrl;
                var patchVersion = VersionCheck.GetLatestGameVersionName();

                var progress = new Progress<DirectoryPatcherProgressReport>();
                var cancellationTokenSource = new CancellationTokenSource();
                Task task = new RXPatcher().ApplyPatchFromWeb(patchUrl, targetDir, applicationDir, progress, cancellationTokenSource.Token);

                var window = new ApplyUpdateWindow(task, progress, patchVersion, cancellationTokenSource);
                window.Owner = this;
                window.ShowDialog();

                VersionCheck.UpdateGameVersion();
            }
        }

        void DownloadLauncherUpdate()
        {
            UpdateDownloadWindow theWindow = new UpdateDownloadWindow(VersionCheck.LauncherPatchUrl);
            theWindow.Owner = this;
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
            theWindow.LatestVersionText.Content = VersionCheck.GetLatestLauncherVersionName();
            theWindow.GameVersionText.Content = VersionCheck.GetLauncherVersionName();
            theWindow.WindowTitle.Content = "Launcher update available!";
            theWindow.Owner = this;
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

                if (SameVersionOnly && VersionCheck.GetGameVersionName() != "" && OFilteredServerList[i].GameVersion != VersionCheck.GetGameVersionName())
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
                    PassWindow.Owner = this;
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

            sv_MapPreview.Source = BitmapToImageSourceConverter.Convert(MapPreviewSettings.GetMapBitmap(selected.MapName));

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
            login.Username = Properties.Settings.Default.Username;
            login.Owner = this;
            bool? result = login.ShowDialog();

            if (result.HasValue && result.Value)
            {
                Properties.Settings.Default.Username = login.Username;
                Properties.Settings.Default.Save();
                SD_Username.Content = login.Username;
            }
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
                startupParameters.SkipIntroMovies = false; // Properties.Settings.Default.SkipIntroMovies;
                GameInstance = EngineInstance.Start(startupParameters);

                await GameInstance.Task;

                SetMessageboxText(MESSAGE_IDLE);
            }
            catch
            {
                SetMessageboxText(MESSAGE_CANTSTARTGAME);
            }
            finally
            {
                GameInstance = null;
            }
        }

        private void StartEditorInstance()
        {
            try
            {
                EditorInstanceStartupParameters startupParameters = new EditorInstanceStartupParameters();
                EngineInstance.Start(startupParameters);
                SetMessageboxText("The editor was started.");
            }
            catch
            {
                SetMessageboxText("Error starting editor.");
            }
            finally
            {
                GameInstance = null;
            }
        }

        private void StartServerInstance()
        {
            try
            {
                ServerInstanceStartupParameters startupParameters = new ServerInstanceStartupParameters();
                EngineInstance.Start(startupParameters);
                SetMessageboxText("The server was started.");

                IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                var localIps = host.AddressList.Where((a) => a.AddressFamily == AddressFamily.InterNetwork);
                string localIPString = string.Join("\n", from ip in localIps select ip.ToString());
                MessageBox.Show(String.Format("The server was started and will continue to run in the background. You can connect to it via LAN by pressing \"Join IP\" in the launcher, and then entering one of the IP addresses below.\n\n{0}\n\nIf you want to play over the internet, you can use the server list in the launcher or in game. Note that you likely need to forward port 7777 in your router and/or firewall to make internet games work.\n\nNote that launching the server via the launcher is intended for LAN servers, and some online functionality (such as leaderboard statistics) is disabled.", localIPString));
            }
            catch
            {
                SetMessageboxText("Error starting server.");
            }
            finally
            {
                GameInstance = null;
            }
        }

        private void SD_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow();
            window.ShowDialog();
        }

        private async void SD_LaunchGame_Click(object sender, RoutedEventArgs e)
        {
            await StartGameInstance(null, null);
        }

        private void SD_LaunchEditor_Click(object sender, RoutedEventArgs e)
        {
            StartEditorInstance();
        }

        private void SD_LaunchServer_Click(object sender, RoutedEventArgs e)
        {
            StartServerInstance();
        }

        private async void SD_ConnectIP_Click(object sender, RoutedEventArgs e)
        {
            JoinIPWindow IPWindow = new JoinIPWindow();
            IPWindow.Owner = this;
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