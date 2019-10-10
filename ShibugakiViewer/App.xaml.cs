using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using RestoreWindowPlace;
using System.Reflection;
using System.Reactive.Disposables;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models.Utility;
using ShibugakiViewer.Views;
using System.IO;
using ShibugakiViewer.Models;
using ShibugakiViewer.Views.Windows;
using System.Windows.Markup;
using System.Globalization;
using System.Windows.Navigation;
using System.Windows.Input;
using System.Reactive.Linq;
using ShibugakiViewer.ViewModels;
using System.Threading;
using System.Diagnostics;
using System.Text;
using Boredbone.Utility.Extensions;
using ShibugakiViewer.Views.Controls;
using System.IO.Pipes;

namespace ShibugakiViewer
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public WindowPlace WindowPlacement { get; private set; }

        private const string companyName = @"Boredbone";
        private const string productName = @"ShibugakiViewer";

        private const string placementFileName = "placement.config";


        private const string mutexId = "79509481-1f8d-44b0-a581-d0dd4fa23710";
        private const string pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";
        private const string commandMarker = "?";
        private static Mutex processMutex = null;
        private PipeServer server;

        private string SaveDirectory;

        private readonly CompositeDisposable disposables;
        private ResourceDictionary themeResourceDictionary;

        internal ApplicationCore Core { get; }

        private bool isLaunched = false;
        private bool isInitialized = false;

        private System.Windows.Forms.NotifyIcon notifyIcon;


        public App()
        {
            this.disposables = new CompositeDisposable();
            this.Core = new ApplicationCore().AddTo(this.disposables);


            AppDomain.CurrentDomain.UnhandledException
                += CurrentDomain_UnhandledException;

            // Ensure the current culture passed into bindings is the OS culture.
            // By default, WPF uses en-US as the culture, regardless of the system settings.
            FrameworkElement.LanguageProperty.OverrideMetadata(
                  typeof(FrameworkElement),
                  new FrameworkPropertyMetadata(
                      XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)));
        }

        private static void ExchangeProcessMutex(Mutex newVal)
        {
            // MUST called from UI thread
            if (processMutex != null)
            {
                processMutex.ReleaseMutex();
                processMutex.Close();
                processMutex.Dispose();
                System.Diagnostics.Debug.WriteLine("mutex disposed");
            }
            processMutex = newVal;
        }
        private static void ReleaseProcessMutex()
        {
            try
            {
                ExchangeProcessMutex(null);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!ConfirmSingleInstance(e.Args))
            {
                this.Shutdown();
                return;
            }
            base.OnStartup(e);

#if DEBUG
            this.Resources["IsThumbnailTooltipEnabled"] = false;
#endif

            this.isLaunched = true;

            Disposable.Create(() => ReleaseProcessMutex()).AddTo(this.disposables);

            //Thread.CurrentThread.CurrentCulture = new CultureInfo("");
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("");

            //ブラウザ用のキーボードショートカットを無効化
            NavigationCommands.BrowseBack.InputGestures.Clear();
            NavigationCommands.BrowseForward.InputGestures.Clear();


            //設定ファイルの保存先ディレクトリ
            var dir = System.Environment.GetFolderPath
                (Environment.SpecialFolder.LocalApplicationData);

            var saveDirectory = Path.Combine(dir, companyName, productName);

            if (!System.IO.Directory.Exists(saveDirectory))
            {
                System.IO.Directory.CreateDirectory(saveDirectory);
            }

            this.SaveDirectory = saveDirectory;


            //ウィンドウ配置
            this.WindowPlacement = new WindowPlace(Path.Combine(saveDirectory, placementFileName));

            //Model初期化
            var hasItem = this.Core.Initialize(saveDirectory);

            Window window = null;
            var exitApp = false;

            if (hasItem)
            {
                window = this.ShowFirstClient(false, e.Args);
            }
            else
            {
                window = new WelcomeWindow() { ShowActivated = true };
                window.Show();
                this.StartPipeServer();
                exitApp = true;
            }

#if DEBUG
            //new VersionCheckWindow()
            //{
            //    WindowStartupLocation = WindowStartupLocation.CenterOwner,
            //    Owner = window,
            //}.ShowDialog();

#endif

            if (this.Core.IsVersionCheckEnabled())
            {
                this.CheckNewVersionAsync(window, exitApp).FireAndForget();
            }
        }

        private bool ConfirmSingleInstance(string[] args)
        {
            try
            {
                var mutex = new Mutex(true, mutexId, out var createdNew);

                if (createdNew)
                {
                    // Got mutex
                    ExchangeProcessMutex(mutex);

                    // This is the first instance
                    return true;
                }
                // Another instance is already running...

                // Connect pipe
                bool pipeSucceeded = false;

                try
                {
                    using (var pipeClient =
                        new NamedPipeClientStream(".", pipeId, PipeDirection.Out))
                    {
                        pipeClient.Connect(10000);

                        using (var sw = new StreamWriter(pipeClient) { AutoFlush = true })
                        {
                            if (args != null)
                            {
                                foreach (var line in args)
                                {
                                    sw.WriteLine(line);
                                }
                            }
                            sw.WriteLine(commandMarker);
                        }
                    }
                    pipeSucceeded = true;
                }
                catch (TimeoutException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }


                // Close this app
                mutex.Close();
                mutex.Dispose();

                if (!pipeSucceeded)
                {
                    MessageBox.Show($"already launched.");
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                return true;
            }
        }

        private async Task CheckNewVersionAsync(Window window, bool exitApp)
        {

            var available = await this.Core.CheckNewVersionAsync();

            if (!available)
            {
                return;
            }

            bool? dialogResult = null;

            var dialog = new VersionCheckWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = window,
            };

            dialogResult = dialog.ShowDialog();
            //if (window != null)
            //{
            //    window.PopupDialog.Show(new VersionCheckPopup(), new Thickness(0),
            //        HorizontalAlignment.Stretch, VerticalAlignment.Center, null, true);
            //}

            if (dialogResult == true)
            {
                Process.Start(this.Core.ProjectHomeUrl);
                if (exitApp)
                {
                    await this.ExitAllAfterLibraryUnLockedAsync();
                }
            }
        }


        /// <summary>
        /// コマンド受信
        /// </summary>
        /// <param name="args"></param>
        private void ExecutePipeCommand(IList<string> args)
        {
            if (args == null || args.Count <= 0)
            {
                return;
            }

            var command = args[args.Count - 1].Replace("\0", "");

            switch (command)
            {
                case "?exit":
                    this.ExitAll();
                    break;
                default:
                    if (this.isInitialized)
                    {
                        this.ShowClientWindow(args.Where(x => !x.Equals(commandMarker)));
                    }
                    break;
            }
        }

        /// <summary>
        /// 設定ウインドウ
        /// </summary>
        /// <param name="index"></param>
        public void ShowSettingWindow(int index)
        {
            if (index >= 0)
            {
                this.Core.SettingPageIndex = index;
            }
            this.ShowUniqueWindow<SettingWindow>();
        }

        /// <summary>
        /// ツールウインドウ
        /// </summary>
        /// <param name="index"></param>
        public void ShowToolWindow(int index)
        {
            if (index >= 0)
            {
                this.Core.ToolPageIndex = index;
            }
            this.ShowUniqueWindow<ToolWindow>();
        }

        /// <summary>
        /// フォルダ設定ウインドウ
        /// </summary>
        public void ShowFolderWindow() => this.ShowUniqueWindow<FolderWindow>();

        /// <summary>
        /// 指定されたウインドウがあれば最前面に表示，なければ生成
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private void ShowUniqueWindow<T>() where T : Window, new()
        {
            var window = this.Windows.OfType<T>().FirstOrDefault()
                ?? new T();
            window.Show();
            window.Activate();
        }

        /// <summary>
        /// Client表示
        /// </summary>
        /// <param name="files"></param>
        private ClientWindow ShowClientWindow(IEnumerable<string> files)
        {
            ClientWindow window;// = new ClientWindow(PageType.Viewer, 1) { ShowActivated = true };

            if (files != null && files.Count(x => x.HasText()) == 1)
            {
                window = new ClientWindow(PageType.Viewer, 2) { ShowActivated = true };
            }
            else
            {
                window = new ClientWindow() { ShowActivated = true };
            }
            if (files != null)
            {
                var client = (window.DataContext as ClientWindowViewModel)?.Client;
                if (client != null)
                {
                    client.ActivateFiles(files);
                }
                //window.ViewModel.LoadFiles(new[] { file });
            }

            window.Show();
            return window;
        }

        /// <summary>
        /// ClientをCatalogPageで表示
        /// </summary>
        private ClientWindow ShowClientWindowWithCatalog()
        {
            var window = new ClientWindow() { ShowActivated = true };

            var client = (window.DataContext as ClientWindowViewModel)?.Client;
            if (client != null)
            {
                client.StartNewSearch(null);
            }

            window.Show();
            return window;
        }

        public ClientWindow ShowFirstClient(bool withCatalog, string[] files)
        {

            //通知アイコン
            this.ShowNotifyIcon()?.AddTo(this.disposables);

            ClientWindow window;

            //ウィンドウ
            if (withCatalog)
            {
                window = this.ShowClientWindowWithCatalog();
            }
            else
            {
                window = this.ShowClientWindow((files.IsNullOrEmpty()) ? null : files);
            }

            this.StartPipeServer();

            this.isInitialized = true;
            return window;
        }

        private void StartPipeServer()
        {

            //パイプサーバ
            if (this.server == null)
            {
                this.server = new PipeServer().AddTo(this.disposables);

                this.server.LineReceived
                    .Buffer(this.server.LineReceived.Where(x => x.StartsWith(commandMarker)))
                    .ObserveOnUIDispatcher()
                    .Subscribe(x => this.ExecutePipeCommand(x), ex =>
                    {
                        ReleaseProcessMutex();
                        MessageBox.Show($"{this.Core.AppName} Error : {ex.Message}");
                        this.Shutdown();
                    })
                    .AddTo(this.disposables);

                this.server.Activate(mutexId, pipeId);
            }
        }


        /// <summary>
        /// アプリケーション終了時処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            ReleaseProcessMutex();

            base.OnExit(e);

            if (!this.isLaunched)
            {
                return;
            }

            this.Core.Save();
            try { this.WindowPlacement.Save(); } catch { }
            this.disposables.Dispose();
        }

        /// <summary>
        /// 全てのウィンドウを閉じてアプリケーションを終了させる
        /// </summary>
        public void ExitAll()
        {
            foreach (var window in this.Windows.OfType<Window>())
            {
                window.Close();
            }
            this.Shutdown();
        }

        public async Task ExitAllAfterLibraryUnLockedAsync()
        {
            foreach (var window in this.Windows.OfType<Window>())
            {
                window.Close();
            }

            (await this.Core.Library.LockAsync()).Dispose();
            this.ExitAll();
        }

        /// <summary>
        /// Viewのテーマを変更
        /// </summary>
        /// <param name="themeName"></param>
        public void ChangeTheme(string themeName)
        {
            if (this.themeResourceDictionary == null)
            {
                // 新しいリソース・ディクショナリを追加
                this.themeResourceDictionary = new ResourceDictionary();
                this.Resources.MergedDictionaries.Add(this.themeResourceDictionary);
            }

            // WPFテーマをリソース・ディクショナリのソースに指定
            string themeUri = $"pack://application:,,,/Views/Resources/{themeName}.xaml";
            this.themeResourceDictionary.Source = new Uri(themeUri);
        }

        /// <summary>
        /// 例外
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ReleaseProcessMutex();
            var exception = e.ExceptionObject as Exception;
            if (exception == null)
            {
                MessageBox.Show("Unknown Exception");
                return;
            }

            //var errorMember = exception.TargetSite.Name;
            //var errorMessage = exception.Message;
            //var message = string.Format(exception.ToString());

#if DEBUG
            var text = exception.ToString();
#else
            var text = exception.Message;
#endif

            MessageBox.Show(text, "UnhandledException",
                MessageBoxButton.OK, MessageBoxImage.Stop);
            Environment.Exit(0);
        }
        

        /// <summary>
        /// 通知アイコン表示
        /// </summary>
        /// <returns></returns>
        private IDisposable ShowNotifyIcon()
        {
            var icon = IconHelper.CreateIcon("pack://application:,,,/Assets/Icons/appicon.ico", 16);
            if (icon == null)
            {
                return null;
            }

            var menu = new[]
            {
                new System.Windows.Forms.MenuItem(this.Core.GetResourceString("OpenNewWindow"),//."&Settings (S)",
                    (o, e) => this.ShowClientWindow(null)),
                new System.Windows.Forms.MenuItem(this.Core.GetResourceString("Exit"),
                    (o, e) => this.ExitAll())
            };

            var subscription = new CompositeDisposable();

            this.notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Text = this.Core.AppName,
                Icon = icon,
                Visible = true,
                ContextMenu = new System.Windows.Forms.ContextMenu(menu),
            }.AddTo(subscription);

            this.Core.SystemNotification
                .Subscribe(x => this.notifyIcon.ShowBalloonTip
                    (3000, this.Core.AppName, x, System.Windows.Forms.ToolTipIcon.Info))
                .AddTo(subscription);

            this.notifyIcon.DoubleClick += (o, e) => this.ShowClientWindow(null);
            this.notifyIcon.BalloonTipClicked += (o, e) => this.ShowToolWindow(0);

            var monitoringText = this.Core.GetResourceString("FolderUpdateChecking");
            var updatingText = this.Core.GetResourceString("LibraryUpadating");

            this.Core.Library.IsCreating.Subscribe(x =>
            {
                this.notifyIcon.Text = (x ? updatingText : monitoringText) + " - " + this.Core.AppName;
            })
            .AddTo(subscription);

            return subscription;

        }

        /// <summary>
        /// 保存データのインポート・エクスポート
        /// </summary>
        /// <param name="isImport"></param>
        public void ImportOrExportLibrary(bool isImport, bool showDialog)
        {
            if (showDialog)
            {
                var result = MessageBox.Show(this.Core.GetResourceString("ExportImportLibraryMessage"),
                    this.Core.AppName, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                if (result != MessageBoxResult.OK && result != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var myAssembly = Assembly.GetEntryAssembly();
            var path = myAssembly.Location;
            var dir = Path.GetDirectoryName(path);

            var args = new[]
            {
                (isImport ? "/r" : "/w"),
                path,
                this.SaveDirectory,
                mutexId,
                pipeId,
                ImageLibrary.Core.Library.databaseFileName,
                ImageLibrary.Core.Library.librarySettingFileName,
                ApplicationCore.settingsFileName,
            };

            this.StartBackupApp(dir, args);
        }

        /// <summary>
        /// 旧ライブラリのインポート
        /// </summary>
        public void ConvertOldLibrary()
        {
            var myAssembly = Assembly.GetEntryAssembly();
            var path = myAssembly.Location;
            var dir = Path.GetDirectoryName(path);

            var args = new[]
            {
                "/c",
                path,
                this.SaveDirectory,
                mutexId,
                pipeId,
                ApplicationCore.settingsFileName,
            }
            .Concat(this.Core.GetConvertArgs())
            .ToArray();

            this.StartBackupApp(dir, args);
        }

        /// <summary>
        /// バックアップアプリ起動
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="args"></param>
        private void StartBackupApp(string directory, string[] args)
        {
            try
            {
                //アプリ起動
                var psi = new ProcessStartInfo()
                {
                    FileName = Path.Combine(directory, "ShibugakiViewer.Backup.exe"),
                    WorkingDirectory = directory,
                    UseShellExecute = false,
                    Arguments = args.Select(x => $"\"{x}\"").Join(" "),
                };

                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception e)
            {
                var tx = e.ToString();
            }
        }
    }
}
