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
        private const string endMark = "?";
        private PipeServer server;

        private readonly CompositeDisposable disposables;
        private ResourceDictionary themeResourceDictionary;

        internal ApplicationCore Core { get; }


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


            //ウィンドウ配置
            this.WindowPlacement = new WindowPlace(Path.Combine(saveDirectory, placementFileName));

            this.Core.Initialize(saveDirectory);


            var args = e.Args;

            if (args != null && args.Length > 0)
            {
                this.ShowClientWindow(args);
            }
            else
            {
                this.ShowClientWindow(null);
            }

            if (this.server == null)
            {
                this.server = new PipeServer().AddTo(this.disposables);

                this.server.LineReceived
                    .Buffer(this.server.LineReceived.Where(x => x.Equals(endMark)))
                    .ObserveOnUIDispatcher()
                    .Subscribe(files => this.ShowClientWindow(files.Where(x => !x.Equals(endMark))), ex =>
                    {
                        MessageBox.Show(ex.ToString());
                        this.Shutdown();
                    })
                    .AddTo(this.disposables);

                this.server.Activate(mutexId, pipeId);

            }
        }

        public void ShowSettingWindow()
        {
            var window = this.Windows.OfType<SettingWindow>().FirstOrDefault() ?? new SettingWindow();
            //window.ShowActivated = true;
            window.Show();
            window.Activate();
        }

        public void ShowLibraryUpdateStatusWindow()
        {
            var window = this.Windows.OfType<LibraryUpdateStatusWindow>().FirstOrDefault()
                ?? new LibraryUpdateStatusWindow();
            window.Show();
            window.Activate();
        }

        public void ShowClientWindow(IEnumerable<string> files)
        {
            var window = new ClientWindow();
            window.ShowActivated = true;
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

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            this.Core.Save();
            this.WindowPlacement.Save();
            this.disposables.Dispose();
        }

        public void ExitAll()
        {
            foreach (var window in this.Windows.OfType<Window>())
            {
                window.Close();
            }
        }


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
            MessageBox.Show(exception.ToString(), "UnhandledException",
                MessageBoxButton.OK, MessageBoxImage.Stop);
            Environment.Exit(0);
        }
    }
}
