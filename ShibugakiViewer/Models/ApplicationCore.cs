using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using ImageLibrary.SearchProperty;
using ImageLibrary.Viewer;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models.ImageViewer;
using ShibugakiViewer.Models.Utility;
using WpfTools.Extensions;

namespace ShibugakiViewer.Models
{
    public class ApplicationCore : NotificationBase
    {
        public const string settingsFileName = "appsettings.config";

        public string[] FileTypeFilter { get; }
            = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".wmf", ".emf", ".bhf", ".tif", ".tiff" };

        public HashSet<string> MetaImageExtention { get; }
            = new HashSet<string>(new[] { ".wmf", ".emf" });

        private const int settingVersion = 3;

        private const string darkThemeName = "DarkTheme";
        private const string lightThemeName = "LightTheme";


        private XmlSettingManager<ApplicationSettings> SettingsXml { get; set; }
        private ApplicationSettings Settings { get; set; }

        public Library Library { get; private set; }
        public ImageBuffer ImageBuffer { get; private set; }
        private ResourceManager ResourceManager { get; set; }
        
        
        public IObservable<string> SystemNotification { get; private set; }

        public ReactiveCollection<LibraryUpdateHistoryItem> LibraryUpdateHistory { get; private set; }
        
        
        public string AppName
        {
            get
            {
                if (this._fieldAppName == null)
                {
                    this._fieldAppName = Application.Current.Resources["AppName"].ToString();
                }
                return _fieldAppName;
            }
        }
        private string _fieldAppName;


        public bool IsSVOLanguage { get; private set; }

        public bool LastSearchedFavorite
        {
            get { return this.Settings.LastSearchedFavorite; }
            set
            {
                if (this.Settings.LastSearchedFavorite != value)
                {
                    this.Settings.LastSearchedFavorite = value;
                    RaisePropertyChanged(nameof(LastSearchedFavorite));
                }
            }
        }




        public int ThumbNailSize
        {
            get
            {
                if (this.Settings.ThumbNailSize < 32)
                {
                    this.Settings.ThumbNailSize = 200;
                }
                return this.Settings.ThumbNailSize;
            }
            set
            {
                if (this.Settings.ThumbNailSize != value)
                {
                    this.Settings.ThumbNailSize = value;
                    RaisePropertyChanged(nameof(ThumbNailSize));
                }
            }
        }

        public bool IsFlipReversed
        {
            get { return this.Settings.IsFlipReversed; }
            set
            {
                if (this.Settings.IsFlipReversed != value)
                {
                    this.Settings.IsFlipReversed = value;
                    RaisePropertyChanged("IsFlipReversed");
                }
            }
        }


        public bool IsOpenNavigationWithSingleTapEnabled
        {
            get { return this.Settings.IsOpenNavigationWithSingleTapEnabled; }
            set
            {
                if (this.Settings.IsOpenNavigationWithSingleTapEnabled != value)
                {
                    this.Settings.IsOpenNavigationWithSingleTapEnabled = value;
                    RaisePropertyChanged("IsOpenNavigationWithSingleTapEnabled");
                }
            }
        }

        public bool IsFlipAnimationEnabled
        {
            get { return this.Settings.IsFlipAnimationEnabled; }
            set
            {
                if (this.Settings.IsFlipAnimationEnabled != value)
                {
                    this.Settings.IsFlipAnimationEnabled = value;
                    RaisePropertyChanged("IsFlipAnimationEnabled");
                }
            }
        }

        public bool IsCmsEnabled
        {
            get { return this.Settings.IsCmsEnabled; }
            set
            {
                if (this.Settings.IsCmsEnabled != value)
                {
                    this.Settings.IsCmsEnabled = value;
                    RaisePropertyChanged("IsCmsEnabled");
                }
            }
        }

        public bool IsAnimatedGifEnabled
        {
            get { return !this.Settings.IsGifAnimationDisabled; }
            set
            {
                if (this.Settings.IsGifAnimationDisabled == value)
                {
                    this.Settings.IsGifAnimationDisabled = !value;
                    RaisePropertyChanged(nameof(IsAnimatedGifEnabled));
                }
            }
        }

        public bool UseExtendedMouseButtonsToSwitchImage
        {
            get { return this.Settings.UseExtendedMouseButtonsToSwitchImage; }
            set
            {
                if (this.Settings.UseExtendedMouseButtonsToSwitchImage != value)
                {
                    this.Settings.UseExtendedMouseButtonsToSwitchImage = value;
                    RaisePropertyChanged("UseExtendedMouseButtonsToSwitchImage");
                }
            }
        }

        public bool RefreshLibraryOnLaunched
        {
            get { return this.Settings.RefreshLibraryOnLaunched; }
            set
            {
                if (this.Settings.RefreshLibraryOnLaunched != value)
                {
                    this.Settings.RefreshLibraryOnLaunched = value;
                    RaisePropertyChanged("RefreshLibraryOnLaunched");
                }
            }
        }


        public bool IsLibraryRefreshStatusVisible
        {
            get { return this.Settings.IsLibraryRefreshStatusVisible; }
            set
            {
                if (this.Settings.IsLibraryRefreshStatusVisible != value)
                {
                    this.Settings.IsLibraryRefreshStatusVisible = value;
                    RaisePropertyChanged("IsLibraryRefreshStatusVisible");
                }
            }
        }

        public bool IsFolderUpdatedNotificationVisible
        {
            get { return this.Settings.IsFolderUpdatedNotificationVisible; }
            set
            {
                if (this.Settings.IsFolderUpdatedNotificationVisible != value)
                {
                    this.Settings.IsFolderUpdatedNotificationVisible = value;
                    RaisePropertyChanged(nameof(IsFolderUpdatedNotificationVisible));
                }
            }
        }


        public int CursorKeyBind
        {
            get { return this.Settings.CursorKeyBind; }
            set
            {
                if (this.Settings.CursorKeyBind != value)
                {
                    this.Settings.CursorKeyBind = value;
                    RaisePropertyChanged("CursorKeyBind");
                }
            }
        }
        

        public bool IsDarkTheme
        {
            get { return this.Settings.IsDarkTheme; }
            set
            {
                if (this.Settings.IsDarkTheme != value)
                {
                    this.Settings.IsDarkTheme = value;
                    RaisePropertyChanged(nameof(IsDarkTheme));
                }
            }
        }

        public Color BackgroundColor
        {
            get { return ColorExtensions.FromCode(this.Settings.BackgroundColor); }
            set
            {
                var code = value.ToCode();
                if (this.Settings.BackgroundColor != code)
                {
                    this.Settings.BackgroundColor = code;
                    RaisePropertyChanged(nameof(BackgroundColor));
                }
            }
        }




        public bool IsProfessionalFolderSettingEnabled
        {
            get { return this.Settings.IsProfessionalFolderSettingEnabled; }
            set
            {
                if (this.Settings.IsProfessionalFolderSettingEnabled != value)
                {
                    this.Settings.IsProfessionalFolderSettingEnabled = value;
                    RaisePropertyChanged("IsProfessionalFolderSettingEnabled");
                }
            }
        }


        public int SlideshowAnimationTimeMillisec
        {
            get { return this.Settings.SlideshowAnimationTimeMillisec; }
            set
            {
                if (this.Settings.SlideshowAnimationTimeMillisec != value)
                {
                    this.Settings.SlideshowAnimationTimeMillisec = value;
                    RaisePropertyChanged(nameof(SlideshowAnimationTimeMillisec));
                }
            }
        }

        public int SlideshowFlipTimeMillisec
        {
            get { return this.Settings.SlideshowFlipTimeMillisec; }
            set
            {
                if (this.Settings.SlideshowFlipTimeMillisec != value)
                {
                    this.Settings.SlideshowFlipTimeMillisec = value;
                    RaisePropertyChanged(nameof(SlideshowFlipTimeMillisec));
                }
            }
        }



        public bool IsSlideshowResizingAlways
        {
            get { return this.Settings.IsSlideshowResizingAlways; }
            set
            {
                if (this.Settings.IsSlideshowResizingAlways != value)
                {
                    this.Settings.IsSlideshowResizingAlways = value;
                    RaisePropertyChanged(nameof(IsSlideshowResizingAlways));
                }
            }
        }


        public bool IsSlideshowResizeToFill
        {
            get { return this.Settings.IsSlideshowResizeToFill; }
            set
            {
                if (this.Settings.IsSlideshowResizeToFill != value)
                {
                    this.Settings.IsSlideshowResizeToFill = value;
                    RaisePropertyChanged(nameof(IsSlideshowResizeToFill));
                }
            }
        }


        public bool IsSlideshowRandom
        {
            get { return this.Settings.IsSlideshowRandom; }
            set
            {
                if (this.Settings.IsSlideshowRandom != value)
                {
                    this.Settings.IsSlideshowRandom = value;
                    RaisePropertyChanged(nameof(IsSlideshowRandom));
                }
            }
        }

        public bool IsSlideshowFullScreen
        {
            get { return this.Settings.IsSlideshowFullScreen; }
            set
            {
                if (this.Settings.IsSlideshowFullScreen != value)
                {
                    this.Settings.IsSlideshowFullScreen = value;
                    RaisePropertyChanged(nameof(IsSlideshowFullScreen));
                }
            }
        }


        public bool IsAutoInformationPaneDisabled
        {
            get { return this.Settings.IsAutoInformationPaneDisabled; }
            set
            {
                if (this.Settings.IsAutoInformationPaneDisabled != value)
                {
                    this.Settings.IsAutoInformationPaneDisabled = value;
                    RaisePropertyChanged(nameof(IsAutoInformationPaneDisabled));
                }
            }
        }

        public bool IsViewerMoveButtonDisabled
        {
            get { return this.Settings.IsViewerMoveButtonDisabled; }
            set
            {
                if (this.Settings.IsViewerMoveButtonDisabled != value)
                {
                    this.Settings.IsViewerMoveButtonDisabled = value;
                    RaisePropertyChanged(nameof(IsViewerMoveButtonDisabled));
                }
            }
        }


        public bool IsSettingsLoaded { get; set; }
        public bool IsLibrarySettingsLoaded { get; set; }



        public bool IsViewerPageTopBarFixed
        {
            get { return this.Settings.IsViewerPageTopBarFixed; }
            set { this.Settings.IsViewerPageTopBarFixed = value; }
        }

        public bool IsViewerPageLeftBarFixed
        {
            get { return this.Settings.IsViewerPageLeftBarFixed; }
            set { this.Settings.IsViewerPageLeftBarFixed = value; }
        }
        
        private object lockObject = new object();

        private bool isChanged;


        public void Initialize(string saveDirectory)
        {
            // Set the user interface to display in the same culture as that set in Control Panel.
            System.Threading.Thread.CurrentThread.CurrentUICulture =
                System.Threading.Thread.CurrentThread.CurrentCulture;

            //ストレージに保存する設定
            this.SettingsXml = new XmlSettingManager<ApplicationSettings>
                (Path.Combine(saveDirectory, settingsFileName));

            this.Settings = SettingsXml
                .LoadXml(XmlLoadingOptions.IgnoreAllException | XmlLoadingOptions.UseBackup)
                .Value;

            this.ImageBuffer = new ImageBuffer().AddTo(this.Disposables);
            this.ImageBuffer.MetaImageExtention = this.MetaImageExtention;

            var config = new LibraryConfiguration(saveDirectory)
            {
                Concurrency = 512,
                FileTypeFilter = new HashSet<string>(this.FileTypeFilter),
                FileSystem = new FileSystem(),
            };

            LibraryOwner.SetConfig(config);

            var library = LibraryOwner.GetCurrent();

            library.InitSettings();
            library.Load();

            library.AddTo(this.Disposables);

            this.Library = library;
            

            this.LibraryUpdateHistory = new ReactiveCollection<LibraryUpdateHistoryItem>().AddTo(this.Disposables);

            this.Library.Loaded
                .Subscribe(x => this.LibraryUpdateHistory.AddRangeOnScheduler(
                    x.AddedFiles.Select(y => new LibraryUpdateHistoryItem()
                    { Date = x.DateTime, Path = y.Key, Type = LibraryUpdateType.Add })
                    .Concat(x.RemovedFiles.Select(y => new LibraryUpdateHistoryItem()
                    { Date = x.DateTime, Path = y.Key, Type = LibraryUpdateType.Remove }))
                    .Concat(x.UpdatedFiles.Select(y => new LibraryUpdateHistoryItem()
                    { Date = x.DateTime, Path = y.Key, Type = LibraryUpdateType.Update }))))
                .AddTo(this.Disposables);

            this.SystemNotification = this.Library.Loaded
                .Select(x => this.ShowLibraryResult(x))
                .Where(x => x != null)
                .Publish().RefCount();

            //リソースから文字列を取得
            this.InitializeResourceString();

            //色テーマ
            this.ObserveProperty(x => x.IsDarkTheme)
                .Subscribe(x =>
                {
                    ((App)Application.Current).ChangeTheme(x ? darkThemeName : lightThemeName);
                })
                .AddTo(this.Disposables);

            this.ObserveProperty(x => x.BackgroundColor)
                .Subscribe(x =>
                {
                    Application.Current.Resources["BasicBackColor"] = new SolidColorBrush(x);
                })
                .AddTo(this.Disposables);

            this.isChanged = true;
            this.PropertyChangedAsObservable().Subscribe(x => this.isChanged = true).AddTo(this.Disposables);
            

            //ライブラリ更新
            if (this.RefreshLibraryOnLaunched)
            {
                this.Library.StartRefreshLibrary(false);
            }
        }
        

        /// <summary>
        /// 設定ファイルをローカルストレージに保存
        /// </summary>
        public void Save()
        {
            this.SaveLibrarySettings();
            this.SaveApplicationSettings();
        }

        private void SaveLibrarySettings()
        {
            lock (this.lockObject)
            {
                try
                {
                    this.Library.SaveSettings();
                }
                catch
                {

                }
            }
        }
        public void SaveApplicationSettings()
        {
            lock (this.lockObject)
            {
                if (!this.isChanged)
                {
                    return;
                }
                try
                {
                    this.Settings.Version = settingVersion;
                    this.SettingsXml.SaveXml(this.Settings);
                    this.isChanged = false;
                }
                catch
                {

                }
            }
        }


        public void CopySelectedItemsPath(SelectionManager manager)
        {
            if (manager == null || manager.Ids.Count <= 0)
            {
                return;
            }
            SharePathOperation.CopyPath(manager.Ids.Join("\n"));
        }

        public string GetResourceString(string key)
        {
            try
            {
                var str = Properties.Resources.ResourceManager.GetString(key);
                return str == null ? key : str;
            }
            catch
            {
                return key;
            }
        }

        private void InitializeResourceString()
        {

            FilePropertyManager.InitializeLabels(x => this.GetResourceString(x));
            CompareModeManager.InitializeLabels(x => this.GetResourceString(x));

            var language = GetResourceString("Language");
            if (language.ToLower().Contains("ja"))
            {
                this.IsSVOLanguage = false;
            }
            else
            {
                this.IsSVOLanguage = true;
            }

            App.Current.Resources["FileNameLabel"] = this.GetResourceString("FileName");
            App.Current.Resources["FileSizeLabel"] = this.GetResourceString("FileSize");
            App.Current.Resources["RatingLabel"] = this.GetResourceString("Rating");
            App.Current.Resources["DateCreatedLabel"] = this.GetResourceString("DateCreated");
            App.Current.Resources["DateModifiedLabel"] = this.GetResourceString("DateModified");
            App.Current.Resources["ImageSizeLabel"] = this.GetResourceString("ImageSize");
            App.Current.Resources["SearchLabel"] = this.GetResourceString("Search");
            App.Current.Resources["WidthLabel"] = this.GetResourceString("Width");
            App.Current.Resources["HeightLabel"] = this.GetResourceString("Height");
            
        }

        private string GetOldLibraryDirectory()
        {
            var dir = System.Environment.GetFolderPath
                (Environment.SpecialFolder.LocalApplicationData);

            var saveDirectory =
                Path.Combine(dir, @"Packages\60037Boredbone.MikanViewer_8weh06aq8rfkj\LocalState");

            return saveDirectory;
        }
        /*
        public async Task ConvertOldLibraryAsync()
        {
            using (var locking = await this.Library.LockAsync())
            {
                var saveDirectory = this.GetOldLibraryDirectory();

                var converter = new LibraryConverter.Compat.Converter();
                await converter.Start1(saveDirectory, this.Settings);

                var data = this.Library.GetDataForConvert();
                await converter.Start2(data.Item1, data.Item2, data.Item3);
            }
        }*/

        public async Task<bool> IsOldConvertableAsync()
        {
            var saveDirectory = this.GetOldLibraryDirectory();

            if (!System.IO.Directory.Exists(saveDirectory))
            {
                return false;
            }
            return !(await this.Library.HasItemsAsync());
        }

        private string ShowLibraryResult(LibraryLoadResult result)
        {

            if (!this.IsLibraryRefreshStatusVisible)
            {
                return null;
            }
            if (result.Action == LibraryLoadAction.Activation
                || ((result.Action == LibraryLoadAction.Startup || result.Action == LibraryLoadAction.FolderChanged)
                && result.AddedFiles.Count == 0
                && result.RemovedFiles.Count == 0))
            {
                return null;
            }

            if (result.Action == LibraryLoadAction.FolderChanged && !this.IsFolderUpdatedNotificationVisible)
            {
                return null;
            }

            var refreshedText = this.GetResourceString("RefreshedText");
            var addedText = this.GetResourceString("AddedText");
            var removedText = this.GetResourceString("RemovedText");
            var unitText = this.GetResourceString("UnitText");
            var updatedText = this.GetResourceString("UpdatedText");

            var resultText
                = $"{refreshedText}\n{addedText} : {result.AddedFiles.Count} {unitText}, "
                + $"{removedText} : {result.RemovedFiles.Count} {unitText}, "
                + $"{updatedText} : {result.UpdatedFiles.Count} {unitText}";

            return resultText;
        }

        public void ShowNewClient(IEnumerable<string> files)
        {
            ((App)Application.Current).ShowClientWindow(files);
        }

    }

    public class LibraryUpdateHistoryItem
    {
        public string Path { get; set; }
        public LibraryUpdateType Type { get; set; }
        public DateTimeOffset Date { get; set; }
    }
    public enum LibraryUpdateType
    {
        Add,
        Remove,
        Update,
    }
}
