using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using LauncherTwo.Views;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Reflection;
using System.Net;
using System;
using System.Windows.Threading;
using System.ComponentModel;
using FirstFloor.ModernUI.Windows.Controls;
using RXPatchLib;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Diagnostics;

namespace LauncherTwo
{
    public partial class MainWindow : RxWindow, INotifyPropertyChanged
    {
        public const bool ShowDebug = false;
        public bool VersionMismatch = false;
        private bool IsLogOpen { get; } = false;

        /// <summary>
        /// Boolean that holds the state of the default movie.
        /// </summary>
        private Boolean _defaultMoviePlays = false;

        public const int ServerRefreshRate = 0; // 60 sec
        public static readonly int MaxPlayerCount = 64;
        public TrulyObservableCollection<ServerInfo> OFilteredServerList { get; set; }
        private DispatcherTimer _refreshTimer;
        private EngineInstance _gameInstance;
        public EngineInstance GameInstance
        {
            get { return _gameInstance; }
            set
            {
                _gameInstance = value;
                NotifyPropertyChanged("GameInstance");
                NotifyPropertyChanged("IsLaunchingPossible");
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public string TitleValue { get { return "Renegade-X Launcher v" + VersionCheck.GetLauncherVersionName() + " | Players online: " + this.TotalPlayersOnline; } }

        private int _totalPlayersOnline = 0;
        public int TotalPlayersOnline {get { return this._totalPlayersOnline; } private set
            {
                this._totalPlayersOnline = value;
                this.NotifyPropertyChanged("TitleValue");       
            } }
        public bool IsLaunchingPossible { get { return GameInstance == null && VersionMismatch == false; } }

        const string MessageJoingame = "Establishing Battlefield Control... Standby...";
        const string MessageCantstartgame = "Error starting game executable.";
        const string MessageIdle = "Welcome back commander.";

        const string MessageInstall = "It looks like this is the first time you're running Renegade X or your installation is corrupted.\nDo you wish to install the game?";
        const string MessageNotInstalled = "You will not be able to play the game until the installation is finished!\nThis message will continue to appear untill installation is succesfull.";
        const string MessageRedistInstall = "You will now be prompted to install the Unreal Engine dependancies.\nThis is needed for the successfull installation of Renegade X.";


        private BitmapImage _chkBoxOnImg;
        private BitmapImage _chkBoxOffImg;
        public BitmapImage GetChkBxImg (bool value)
        {
            if (_chkBoxOnImg == null)
                _chkBoxOnImg = new BitmapImage(new Uri("Resources/Checkbox_ON.png", UriKind.Relative));
            if (_chkBoxOffImg == null)
                _chkBoxOffImg = new BitmapImage(new Uri("Resources/Checkbox_OFF.png", UriKind.Relative));

            return value ? _chkBoxOnImg : _chkBoxOffImg;
        }

        #region -= Filters =-
        private int _filterMaxPlayers = MaxPlayerCount; // Default to max
        private int _filterMinPlayers = 0;
        #endregion -= Filters =-

        public MainWindow(bool isLogOpen = false)
        {
            IsLogOpen = isLogOpen;
            RxLogger.Logger.Instance.Write("Initializing MainWindow...");
            OFilteredServerList = new TrulyObservableCollection<ServerInfo>();

            SourceInitialized += (s, a) =>
            {
                StartCheckingVersions();

                if (ServerRefreshRate > 0)
                {
                    _refreshTimer = new DispatcherTimer();
                    _refreshTimer.Interval = new TimeSpan(0, 0, ServerRefreshRate);
                    _refreshTimer.Tick += (object sender, EventArgs e) => StartRefreshingServers();
                    _refreshTimer.Start();
                }
                StartRefreshingServers();

                if (VersionCheck.GetGameVersionName() == "Unknown")
                {
                    Properties.Settings.Default.Installed = false;
                    Properties.Settings.Default.Save();

                    #region PrimaryStartupInstallation
                    //Show the dialog that asks to install the game
                    this.InitFirstInstall();
                    #endregion PrimaryStartupInstallation
                }
                else
                {
                    Properties.Settings.Default.Installed = true;
                    Properties.Settings.Default.Save();
                    if (!string.IsNullOrEmpty(Properties.Settings.Default.Username))
                    {
                        SD_Username.Content = Properties.Settings.Default.Username;
                    }
                    else
                    {
                        ShowUsernameBox();
                    }
                }
            };
            InitializeComponent();

            //SetMessageboxText(MESSAGE_IDLE); // This must be set before any asynchronous code runs, as it might otherwise be overridden.
            ServerInfoGrid.Items.SortDescriptions.Add(new SortDescription(PlayerCountColumn.SortMemberPath, ListSortDirection.Ascending));

            SD_GameVersion.Content = VersionCheck.GetGameVersionName();
            SD_ClanHeader.Cursor = BannerTools.GetBannerLink(null) != "" ? Cursors.Hand : null;

            //due to the mediaelement being set to manual control, we need to start the previewvid in the constructor
            this.sv_MapPreviewVid.Play();
        }

        private async Task CheckVersionsAsync()
        {
            await VersionCheck.UpdateLatestVersions();

            if (!VersionCheck.IsLauncherOutOfDate())
            {
                SetMessageboxText("Launcher is up to date!");
            }
            else
            {
                SetMessageboxText("Launcher is out of date!");

                bool updateInstallPending;
                ShowLauncherUpdateWindow(out updateInstallPending);
                if (updateInstallPending)
                {
                    Close();
                }
                else
                {
                    // Get banners; consider moving this later
                    BannerTools.Setup();
                }
                return;
            }

            if (VersionCheck.GetGameVersionName() == "Unknown")
            {
                SetMessageboxText("Could not locate installed game version. Latest is " + VersionCheck.GetLatestGameVersionName());
            }
            else if (!VersionCheck.IsGameOutOfDate())
            {
                SetMessageboxText("Game is up to date! " + VersionCheck.GetGameVersionName());
            }
            else
            {
                SetMessageboxText("Game is out of date!");
                
                ShowGameUpdateWindow(out bool wasUpdated);
                if (wasUpdated)
                {
                    SetMessageboxText("Game was updated! " + VersionCheck.GetGameVersionName());
                }
                SD_GameVersion.Content = VersionCheck.GetGameVersionName();
            }

            // Get banners; consider moving this later
            BannerTools.Setup();
        }

        private void StartCheckingVersions()
        {
#pragma warning disable 4014
            CheckVersionsAsync();
#pragma warning restore 4014
        }

        void ShowGameUpdateWindow(out bool wasUpdated)
        {
            RxLogger.Logger.Instance.Write("Showing game update window");
            UpdateAvailableWindow theWindow = new UpdateAvailableWindow();
            theWindow.LatestVersionText.Content = VersionCheck.GetLatestGameVersionName();
            theWindow.GameVersionText.Content = VersionCheck.GetGameVersionName();
            theWindow.WindowTitle.Content = "Game update available!";
            theWindow.Owner = this;
            theWindow.ShowDialog();

            if (!theWindow.WantsToUpdate)
            {
                wasUpdated = false;
            }
            else
            {
                //Get the previous vid that plays and store it. Nullify the playing vid so no handle is open
                Uri previousVid = sv_MapPreviewVid.Source;
                sv_MapPreviewVid.Source = null;

                // Close any other instances of the RenX-Launcher
                if (InstanceHandler.IsAnotherInstanceRunning())
                    InstanceHandler.KillDuplicateInstance();

                var targetDir = GameInstallation.GetRootPath();
                var applicationDir = Path.Combine(GameInstallation.GetRootPath(), "patch");
                var patchPath = VersionCheck.GamePatchPath;
                var patchUrls = VersionCheck.GamePatchUrls;
                var patchVersion = VersionCheck.GetLatestGameVersionName();

                var progress = new Progress<DirectoryPatcherProgressReport>();
                var cancellationTokenSource = new CancellationTokenSource();

                RxLogger.Logger.Instance.Write($"Starting game update | TargetDir: {targetDir} | AppDir: {applicationDir} | PatchPath: {patchPath},| PatchVersion: {patchVersion}");
                Task task = RxPatcher.Instance.ApplyPatchFromWeb(patchPath, targetDir, applicationDir, progress, cancellationTokenSource, VersionCheck.InstructionsHash);

                RxLogger.Logger.Instance.Write("Download complete, Showing ApplyUpdateWindow");
                var window = new ApplyUpdateWindow(task, RxPatcher.Instance, progress, patchVersion, cancellationTokenSource, ApplyUpdateWindow.UpdateWindowType.Update);
                window.Owner = this;
                window.ShowDialog();

                VersionCheck.UpdateGameVersion();
                wasUpdated = true;

                //Resume playback of vid
                sv_MapPreviewVid.Source = previousVid;
                sv_MapPreviewVid.Play();

                // Refresh server list
                StartRefreshingServers();
            }
        }

        void DownloadLauncherUpdate(out bool updateInstallPending)
        {
            UpdateDownloadWindow theWindow = new UpdateDownloadWindow(VersionCheck.LauncherPatchUrl, VersionCheck.LauncherPatchHash);
            theWindow.Owner = this;
            theWindow.ShowDialog();

            if (theWindow.UpdateFinished)
            {
                SelfUpdater.ExecuteInstall();
                updateInstallPending = true;
            }
            else
            {
                updateInstallPending = false;
            }
        }

        void ShowLauncherUpdateWindow(out bool updateInstallPending)
        {
            UpdateAvailableWindow theWindow = new UpdateAvailableWindow();
            theWindow.LatestVersionText.Content = VersionCheck.GetLatestLauncherVersionName();
            theWindow.GameVersionText.Content = VersionCheck.GetLauncherVersionName();
            theWindow.WindowTitle.Content = "Launcher update available!";
            theWindow.Owner = this;
            theWindow.ShowDialog();

            if (theWindow.WantsToUpdate)
            {
                DownloadLauncherUpdate(out updateInstallPending);
            }
            else
            {
                updateInstallPending = false;
            }
        }

        public void RefilterServers()
        {
            //If we don't have an active server list we want to return
            if (ServerInfo.ActiveServers == null)
                return;

            var previousSelectedServer = GetSelectedServer();

            OFilteredServerList.Clear();
            this.TotalPlayersOnline = 0;
            foreach (ServerInfo info in ServerInfo.ActiveServers)
            {
                this.TotalPlayersOnline += info.PlayerCount;
                if (sv_ServerSearch.Text != "")
                {
                    if (info.ServerName.ToLower().Contains(sv_ServerSearch.Text.ToLower()) ||
                        info.IpAddress == sv_ServerSearch.Text ||
                        info.IpWithPort == sv_ServerSearch.Text)
                    {
                        OFilteredServerList.Add(info);
                    }
                }
                else
                    OFilteredServerList.Add(info);
            }

            bool sameVersionOnly = (SD_Filter_SameVersionOnly.IsChecked.HasValue) ? SD_Filter_SameVersionOnly.IsChecked.Value : false;

            for (int i = OFilteredServerList.Count - 1; i > -1; i--)
            {
                if (OFilteredServerList[i].PlayerCount < _filterMinPlayers)
                {
                    OFilteredServerList.RemoveAt(i);
                    continue;
                }

                if (OFilteredServerList[i].PlayerCount > _filterMaxPlayers)
                {
                    OFilteredServerList.RemoveAt(i);
                    continue;
                }

                if (sameVersionOnly && VersionCheck.GetGameVersionName() != "" && OFilteredServerList[i].GameVersion != VersionCheck.GetGameVersionName())
                {
                    OFilteredServerList.RemoveAt(i);
                    continue;
                }
            }

            if (previousSelectedServer != null)
            {
                SetSelectedServer(previousSelectedServer.IpWithPort);
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
                if (item.IpWithPort == ipWithPort)
                {
                    ServerInfoGrid.SelectedItem = item;
                    break;
                }
            }
        }

        private void Join_Server_Btn_Click(object sender, RoutedEventArgs e)
        {
            JoinSelectedServer();
        }

        private async Task RefreshServersAsync()
        {
            await ServerInfo.ParseJsonServersAsync();
            RxLogger.Logger.Instance.Write($"{ServerInfo.ActiveServers.Count} servers loaded");
            RefilterServers();
            await ServerInfo.PingActiveServersAsync();
        }

        private void StartRefreshingServers()
        {
#pragma warning disable 4014
            RxLogger.Logger.Instance.Write("Refreshing server list");
            RefreshServersAsync();
#pragma warning restore 4014
        }

        private void SD_MaxPlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SD_MinPlayerSlider == null)
                return;

            if (_filterMaxPlayers != (int)SD_MaxPlayerSlider.Value)
            {
                _filterMaxPlayers = (int)SD_MaxPlayerSlider.Value;
                SD_MaxPlayerDile.Content = _filterMaxPlayers;
                RefilterServers();
            }
        }

        private void SD_MinPlayerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SD_MinPlayerSlider == null)
                return;

            if (_filterMinPlayers != (int)SD_MinPlayerSlider.Value)
            {
                _filterMinPlayers = (int)SD_MinPlayerSlider.Value;
                SD_MinPlayerDile.Content = _filterMinPlayers;
                RefilterServers();
            }

        }

        private void ServerInfoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerInfoGrid.SelectedIndex >= OFilteredServerList.Count || ServerInfoGrid.SelectedIndex <= -1)
                return;

            ServerInfo selected = GetSelectedServer();

            //Original mappreview code
            //sv_MapPreview.Source = BitmapToImageSourceConverter.Convert(MapPreviewSettings.GetMapBitmap(selected.MapName));

            //Movie mappreview code
            if (File.Exists(GameInstallation.GetRootPath() + "\\PreviewVids\\" + selected.MapName + ".mp4"))
            {
                this._defaultMoviePlays = false;
                sv_MapPreviewVid.Source = new Uri(GameInstallation.GetRootPath() + "\\PreviewVids\\" + selected.MapName + ".mp4");
                sv_MapPreviewVid.Play();
            }
            else if (!this._defaultMoviePlays)
            {
                sv_MapPreviewVid.Source = new Uri(GameInstallation.GetRootPath() + "\\PreviewVids\\Default.mp4");
                this._defaultMoviePlays = true;
                sv_MapPreviewVid.Play();
            }

            SD_ClanHeader.Source = BannerTools.GetBanner(selected.IpWithPort);
            SD_ClanHeader.Cursor = BannerTools.GetBannerLink(selected.IpWithPort) != "" ? Cursors.Hand : null;

            SD_Name.Content = selected.ServerName;
            SD_GameLength.Content = selected.TimeLimit.ToString();
            SD_MineLimit.Content = selected.MineLimit.ToString();
            SD_GameMode.Content = selected.MapMode.ToString();
            SD_PlayerLimit.Content = selected.MaxPlayers.ToString();
            SD_ServerVersion.Content = selected.GameVersion;
            SD_VehicleLimit.Content = selected.VehicleLimit;
            SD_CN.Content = selected.CountryName;

            Rect r;
            ServerInfo.FlagCodes.TryGetValue(selected.CountryCode, out r);
            this.SD_CFI.Viewbox = r;
            
            Autobalance_Checkbx.Source = GetChkBxImg(selected.AutoBalance);
            Steam_Checkbx.Source = GetChkBxImg(selected.SteamRequired);
            Crates_Checkbx.Source = GetChkBxImg(selected.SpawnCrates);
            InfantryOnly_Checkbx.Source = GetChkBxImg(selected.VehicleLimit <= 0);
            Ranked_Checkbx.Source = GetChkBxImg(selected.Ranked);

            // Set version mismatch message visibility and join button opacity
            if (VersionCheck.GetGameVersionName() == selected.GameVersion)
            {
                VersionMismatch = false;
                SD_VersionMismatch.Visibility = Visibility.Hidden;
                this.Join_Server_Btn.Background.Opacity = 1.0;
                this.Join_Server_Btn.Content = "Join Server";
            }
            else
            {
                VersionMismatch = true;
                SD_VersionMismatch.Visibility = Visibility.Visible;
                this.Join_Server_Btn.Background.Opacity = 0.5;
                this.Join_Server_Btn.Content = "Server Version Mismatch";
            }
            
            // Ax: This just doesnt work, i assume it was meant to make the Join Game button actually "glow", however it just makes it fade out and back in when a user picks a new server in the list
            // Schmitz - i assume this was something you were working on?
            //System.Windows.Media.Animation.Storyboard sb = this.FindResource("JoinButtonGlow") as System.Windows.Media.Animation.Storyboard;
            //System.Windows.Media.Animation.Storyboard.SetTarget(sb, this.Join_Server_Btn);
            //sb.Begin();

            ServerInfoGrid.UpdateLayout();
        }

        public void SetMessageboxText(string text)
        {
            MessageBoxText.Text = text;
        }

        private void SD_ClanHeader_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ServerInfo selected = GetSelectedServer();
            BannerTools.LaunchBannerLink(selected != null ? selected.IpWithPort : null);
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
                startupParameters.IpEndpoint = ipEndpoint;
                startupParameters.Password = password;
                startupParameters.SkipIntroMovies = Properties.Settings.Default.SkipIntroMovies; // <-Dynamic skipMovies bool

                GameInstance = EngineInstance.Start(startupParameters);

                await GameInstance.Task;

                SetMessageboxText(MessageIdle);
            }
            catch
            {
                SetMessageboxText(MessageCantstartgame);
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
                string localIpString = string.Join("\n", from ip in localIps select ip.ToString());
                MessageBox.Show(string.Format("The server was started and will continue to run in the background. You can connect to it via LAN by pressing \"Join IP\" in the launcher, and then entering one of the IP addresses below.\n\n{0}\n\nIf you want to play over the internet, you can use the server list in the launcher or in game. Note that you likely need to forward port 7777 in your router and/or firewall to make internet games work.\n\nNote that launching the server via the launcher is intended for LAN servers, and some online functionality (such as leaderboard statistics) is disabled.", localIpString));
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
            JoinIpWindow ipWindow = new JoinIpWindow() { Owner = this };
            ipWindow.ShowDialog();
            if (ipWindow.WantsToJoin)
            {
                await StartGameInstance(ipWindow.Ip, ipWindow.Pass);
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


        /// <summary>
        /// Event handler for rewinding previewmovies on movie end
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void MediaEndedHandler(object sender, RoutedEventArgs args)
        {
            var previewMovie = (sender as MediaElement);
            previewMovie.Position = System.TimeSpan.Zero;
        }

        private void SD_UpdateGame_Click(object sender, RoutedEventArgs e)
        {
            SetMessageboxText("Game is out of date!");

            ShowGameUpdateWindow(out bool wasUpdated);
            if (wasUpdated)
            {
                SetMessageboxText("Game was updated! " + VersionCheck.GetGameVersionName());
            }
        }

        /// <summary>
        /// Opens the settings menu dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SD_OpenSettingWindow(object sender, RoutedEventArgs e)
        {
            //Get the previous vid that plays and store it. Nullify the playing vid so no handle is open
            Uri previousVid = sv_MapPreviewVid.Source;
            sv_MapPreviewVid.Source = null;
            //Open the settings as a dialog
            new SettingsWindow().ShowDialog();
            //Resume playback of vid
            sv_MapPreviewVid.Source = previousVid;
            sv_MapPreviewVid.Play();
        }

        /// <summary>
        /// Method that is responsible for initializing the first install message. If yes -> restart application as admin with "--firstInstall" parameter.
        /// </summary>
        private void InitFirstInstall()
        {
            //Show the dialog that asks to install the game
            ModernDialog firstInstallDialog = new ModernDialog
            {
                Title = "Installation",
                Content = MessageInstall
            };
            firstInstallDialog.Buttons = new Button[] { firstInstallDialog.YesButton, firstInstallDialog.NoButton };
            firstInstallDialog.ShowDialog();
            //Check if the user wants to install
            if (firstInstallDialog.DialogResult.Value == true)
            {
                Uri path = new Uri(Assembly.GetExecutingAssembly().CodeBase);
                string args = (IsLogOpen) ? "--log " : "";
                args += "--firstInstall";
                ProcessStartInfo startInfo = new ProcessStartInfo(path.AbsoluteUri, args) {
                    Verb = "runas" // Run as admin
                };
                try {
                    Process.Start(startInfo);
                } catch (Win32Exception) {
                    // User could deny the Permission Prompt window.
                    // In that case, the Process.Start will throw an 'Win32Exception' error.
                    MessageBox.Show(string.Format("Permission denied. Installation cancelled."), "Renegade X: Installation", MessageBoxButton.OK);
                }
                catch (Exception) {
                    MessageBox.Show(string.Format("Something went wrong. Please try again later."), "Renegade X: Installation", MessageBoxButton.OK);
                }
                //Application.Current.Shutdown(); // No need to force shutdown
                this.Close(); // Close MainWindow
            }
            else
            {
                //Show dialog that the game is not playable untill installation is completed
                ModernDialog notInstalledDialog = new ModernDialog
                {
                    Title = "Installation",
                    Content = MessageNotInstalled
                };
                notInstalledDialog.Buttons = new Button[] { notInstalledDialog.OkButton };
                notInstalledDialog.ShowDialog();
            }
        }

        private async void JoinSelectedServer()
        {
            ServerInfo selectedServerInfo = GetSelectedServer();
            if (selectedServerInfo != null)
            {
                string password = null;
                if (GetSelectedServer().PasswordProtected)
                {
                    PasswordWindow passWindow = new PasswordWindow();
                    passWindow.Owner = this;
                    passWindow.ShowDialog();
                    if (!passWindow.WantsToJoin)
                    {
                        return;
                    }
                    password = passWindow.Password;
                }

                this.WindowState = WindowState.Minimized;
                await StartGameInstance(GetSelectedServer().IpWithPort, password); //<-Start 
                this.WindowState = WindowState.Normal;
            }
        }

        private void ServerInfoGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            JoinSelectedServer();
        }
    }
}