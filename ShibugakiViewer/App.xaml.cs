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
        private const string settingsFileName = "appsettings.config";
        private const string placementFileName = "placement.config";


        private const string mutexId = "79509481-1f8d-44b0-a581-d0dd4fa23710";
        private const string pipeId = "1af9b56b-4195-4b99-9893-1edfb2f84cbe";
        private const string commandMarker = "?";
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
                      XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.Name)));//.IetfLanguageTag)));

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            bool createdNew = false;

            //多重起動防止
            using (var mutex = new Mutex(true, mutexId, out createdNew))
            {
                if (createdNew)
                {
                    //取得できた
                    mutex.ReleaseMutex();
                    mutex.Close();
                }
                else
                {
                    //すでに起動していると判断して終了
                    mutex.Close();
                    MessageBox.Show($"{this.Core.AppName} is already launched.");
                    this.Shutdown();
                    return;
                }
            }

            //using (var mutex = new Mutex(false, mutexId))
            //{
            //    try
            //    {
            //        if (!mutex.WaitOne(0, false))
            //        {
            //            //すでに起動していると判断して終了
            //            MessageBox.Show($"{this.Core.AppName} is already launched.");
            //            this.Shutdown();
            //            return;
            //        }
            //        mutex.ReleaseMutex();
            //        mutex.Close();
            //    }
            //    catch (AbandonedMutexException)
            //    {
            //    }
            //}

            this.isLaunched = true;

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

            if (hasItem)
            {
                this.ShowFirstClient(false, e.Args);
            }
            else
            {
                new WelcomeWindow() { ShowActivated = true }.Show();
                this.StartPipeServer();
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
            if (index > 0)
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
            if (index > 0)
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
        public void ShowClientWindow(IEnumerable<string> files)
        {
            var window = new ClientWindow() { ShowActivated = true };

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
        }

        /// <summary>
        /// ClientをCatalogPageで表示
        /// </summary>
        private void ShowClientWindowWithCatalog()
        {
            var window = new ClientWindow() { ShowActivated = true };

            var client = (window.DataContext as ClientWindowViewModel)?.Client;
            if (client != null)
            {
                client.StartNewSearch(null);
            }

            window.Show();
        }

        public void ShowFirstClient(bool withCatalog, string[] files)
        {

            //通知アイコン
            this.ShowNotifyIcon()?.AddTo(this.disposables);

            //ウィンドウ
            if (withCatalog)
            {
                this.ShowClientWindowWithCatalog();
            }
            else
            {
                this.ShowClientWindow((files.IsNullOrEmpty()) ? null : files);
            }

            this.StartPipeServer();

            this.isInitialized = true;
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
                        MessageBox.Show(ex.ToString());
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
            base.OnExit(e);

            if (!this.isLaunched)
            {
                return;
            }

            this.Core.Save();
            this.WindowPlacement.Save();
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

        ///// <summary>
        ///// アイコンを生成
        ///// </summary>
        ///// <param name="path"></param>
        ///// <returns></returns>
        //private System.Drawing.Icon CreateIcon(string path, int size)
        //{
        //    try
        //    {
        //        //const string iconUri = "pack://application:,,,/Assets/Icons/appicon.ico";

        //        Uri uri;
        //        if (!Uri.TryCreate(path, UriKind.Absolute, out uri))
        //        {
        //            return null;
        //        }

        //        var streamResourceInfo = GetResourceStream(uri);
        //        if (streamResourceInfo == null)
        //        {
        //            return null;
        //        }

        //        System.Drawing.Icon icon;

        //        using (var stream = streamResourceInfo.Stream)
        //        {
        //            icon = new System.Drawing.Icon(stream, new System.Drawing.Size(size, size));
        //        }

        //        return icon;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

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
                //var path = serverFullPath;// Path.Combine(dir, serverPath);

                //アプリ起動
                var psi = new ProcessStartInfo()
                {
                    FileName = Path.Combine(directory, "ShibugakiViewer.Backup.exe"),
                    WorkingDirectory = directory,
                    UseShellExecute = false,
                    Arguments = args.Join(" "),
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
