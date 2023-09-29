using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
//using Microsoft.WindowsAPICodePack.Shell;
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
        //public const string projectHomeUrl = @"https://boredbone.github.io/ShibugakiViewer/";
        public const string projectReleaseRssUrl = @"https://github.com/Boredbone/ShibugakiViewer/releases.atom";


        public string[] FileTypeFilter { get; }
            = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".wmf", ".emf", ".bhf", ".tif", ".tiff", ".webp" };

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

        private readonly VersionCheck versionCheck = new VersionCheck();
        public Version LatestReleasedVersion => this.versionCheck.LatestVersion;
        public Version AppCurrentVersion => this.versionCheck.CurrentVersion;

        public static bool ExpandTagShortcut => false;


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

        public string ProjectHomeUrl
        {
            get 
            {
                if (_fieldProjectHomeUrl == null)
                {
                    _fieldProjectHomeUrl = this.GetResourceString("ProjectHomeUrl");
                }
                return _fieldProjectHomeUrl;
            }
        }
        private string _fieldProjectHomeUrl;



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

        public bool IsExifOrientationDisabled
        {
            get { return this.Settings.IsExifOrientationDisabled; }
            set
            {
                if (this.Settings.IsExifOrientationDisabled != value)
                {
                    this.Settings.IsExifOrientationDisabled = value;
                    RaisePropertyChanged(nameof(IsExifOrientationDisabled));
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
            get { return ColorHelper.FromCode(this.Settings.BackgroundColor); }
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

        public int SettingPageIndex
        {
            get { return this.Settings.SettingPageIndex; }
            set
            {
                if (this.Settings.SettingPageIndex != value)
                {
                    this.Settings.SettingPageIndex = value;
                    RaisePropertyChanged(nameof(SettingPageIndex));
                }
            }
        }

        public int ToolPageIndex
        {
            get { return this.Settings.ToolPageIndex; }
            set
            {
                if (this.Settings.ToolPageIndex != value)
                {
                    this.Settings.ToolPageIndex = value;
                    RaisePropertyChanged(nameof(ToolPageIndex));
                }
            }
        }

        public int TagSelectorSortMode
        {
            get { return this.Settings.TagSelectorSortMode; }
            set
            {
                if (this.Settings.TagSelectorSortMode != value)
                {
                    this.Settings.TagSelectorSortMode = value;
                    RaisePropertyChanged(nameof(TagSelectorSortMode));
                }
            }
        }


        public bool UseLogicalPixel
        {
            get { return this.Settings.UseLogicalPixel; }
            set
            {
                if (this.Settings.UseLogicalPixel != value)
                {
                    this.Settings.UseLogicalPixel = value;
                    RaisePropertyChanged(nameof(UseLogicalPixel));
                }
            }
        }

        public bool LoadingOriginalQualityQuick
        {
            get { return this.Settings.LoadingOriginalQualityQuick; }
            set
            {
                if (this.Settings.LoadingOriginalQualityQuick != value)
                {
                    this.Settings.LoadingOriginalQualityQuick = value;
                    RaisePropertyChanged(nameof(LoadingOriginalQualityQuick));
                }
            }
        }

        public int ScalingMode
        {
            get { return this.Settings.ScalingMode; }
            set
            {
                if (this.Settings.ScalingMode != value)
                {
                    this.Settings.ScalingMode = value;
                    RaisePropertyChanged(nameof(ScalingMode));
                }
            }
        }

        public bool SkipVersionCheck
        {
            get { return this.Settings.SkipVersionCheck; }
            set
            {
                if (this.Settings.SkipVersionCheck != value)
                {
                    this.Settings.SkipVersionCheck = value;
                    RaisePropertyChanged(nameof(SkipVersionCheck));
                }
            }
        }
        public DateTimeOffset LastVersionCheckedDate
        {
            get { return this.Settings.LastVersionCheckedDate; }
            set
            {
                if (this.Settings.LastVersionCheckedDate != value)
                {
                    this.Settings.LastVersionCheckedDate = value;
                    RaisePropertyChanged(nameof(LastVersionCheckedDate));
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

        private static object lockObject = new object();

        private bool isChanged;


        public bool Initialize(string saveDirectory)
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
            library.LoadAsync().Wait();

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

            this.Library.CreateThumbnailFunc = (s, d) => ImageResize.Resize(s, d, 128);

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


            var libraryHasItem = this.Library.HasItems();

            //ライブラリ更新
            if (libraryHasItem && this.RefreshLibraryOnLaunched)
            {
                this.Library.StartRefreshLibrary(false);
            }

            return libraryHasItem;
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
            lock (lockObject)
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
            lock (lockObject)
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
                return Properties.Resources.ResourceManager.GetString(key) ?? key;
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
        }

        private string oldLibraryDirectory = null;

        /// <summary>
        /// 旧ライブラリの保存ディレクトリを取得
        /// </summary>
        /// <returns></returns>
        private string GetOldLibraryDirectory()
        {
            try
            {
                if (this.oldLibraryDirectory == null)
                {
                    var dir = System.Environment.GetFolderPath
                        (Environment.SpecialFolder.LocalApplicationData);

                    //var saveDirectory =
                    //    Path.Combine(dir, @"Packages\60037Boredbone.MikanViewer_8weh06aq8rfkj\LocalState");

                    var folders = System.IO.Directory.GetDirectories
                        (Path.Combine(dir, "Packages"), "*Boredbone.MikanViewer*",
                        System.IO.SearchOption.TopDirectoryOnly);

                    if (folders == null || folders.Length == 0)
                    {
                        this.oldLibraryDirectory = "";
                        return null;
                    }


                    string folder = null;
                    if (folders.Length == 1)
                    {
                        folder = folders[0];
                    }
                    else
                    {
                        folder = folders.FirstOrDefault(x => System.IO.Path.GetFileName(x).StartsWith("60037"))
                            ?? folders[0];
                    }

                    if (folder != null)
                    {
                        this.oldLibraryDirectory = Path.Combine(folder, "LocalState");
                    }
                    else
                    {
                        this.oldLibraryDirectory = "";
                        return null;
                    }
                }


                return this.oldLibraryDirectory;
            }
            catch
            {
                this.oldLibraryDirectory = "";
                return null;
            }
        }

        /// <summary>
        /// 旧ライブラリ変換用のパラメータを取得
        /// </summary>
        /// <returns></returns>
        public string[] GetConvertArgs()
            => new[] { this.GetOldLibraryDirectory(), settingVersion.ToString(), };


        /// <summary>
        /// 旧ライブラリのディレクトリが存在するか
        /// </summary>
        /// <returns></returns>
        private bool IsOldLibraryDirectoryExists()
        {
            var dir = this.GetOldLibraryDirectory();
            if (dir.IsNullOrWhiteSpace())
            {
                return false;
            }
            return System.IO.Directory.Exists(dir);
        }


        public async Task<bool> IsOldConvertableAsync()
            => this.IsOldLibraryDirectoryExists() && !(await this.Library.HasItemsAsync());

        public bool IsOldConvertable()
            => this.IsOldLibraryDirectoryExists() && !this.Library.HasItems();

        /// <summary>
        /// ライブラリ更新結果を文字列化
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
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

        ///// <summary>
        ///// 新しいClientWindowを表示
        ///// </summary>
        ///// <param name="files"></param>
        //public void ShowNewClient(IEnumerable<string> files)
        //{
        //    ((App)Application.Current).ShowClientWindow(files);
        //}

        /// <summary>
        /// フォルダを登録
        /// </summary>
        /// <param name="defaultPath"></param>
        /// <param name="lastSelectedPath"></param>
        /// <returns></returns>
        public bool AddFolder(string defaultPath, out string lastSelectedPath)
        {
#if true
            lastSelectedPath = null;
            try
            {
                var selector = new FolderSelector();
                if (!selector.ShowDialog(defaultPath))
                {
                    return false;
                }
                lastSelectedPath = selector.LastSelectedPath;

                return this.Library.Folders.RegisterFolders(selector.SelectedItems.ToArray());
            }
            catch
            {
                //TODO
            }
            return false;
#else

            string folderPath = null;
            using (var fbd = new FolderSelectDialog())
            {
                if (System.IO.Directory.Exists(defaultPath))
                {
                    fbd.DefaultDirectory = defaultPath;
                }

                if (fbd.ShowDialog() == true)
                {
                    folderPath = fbd.SelectedPath;
                }
            }

            if (string.IsNullOrWhiteSpace(folderPath))
            {
                return false;
            }

            var folders = new List<string>();

            try
            {
#if !DEBUG
                if ((".library-ms").Equals(Path.GetExtension(folderPath)))
                {
                    //Windowsライブラリの場合

                    var libraryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"Microsoft\Windows\Libraries\");
                    var libraryName = Path.GetFileNameWithoutExtension
                        (folderPath.Split(Path.DirectorySeparatorChar).Last());

                    using (var shellLibrary = ShellLibrary.Load(libraryName, libraryPath, true))
                    {
                        foreach (var folder in shellLibrary)
                        {
                            folders.Add(folder.Path);
                            lastSelectedPath = folder.Path;
                        }
                    }
                }
                else
#endif
                {
                    //通常フォルダ
                    folders.Add(folderPath);
                    lastSelectedPath = folderPath;
                }
            }
            catch
            {

            }

            return this.Library.Folders.RegisterFolders(folders.ToArray());
#endif
        }

        public bool IsVersionCheckEnabled()
        {
#if DEBUG
            //return true;
#endif
            return !this.SkipVersionCheck && (DateTimeOffset.Now - this.LastVersionCheckedDate > TimeSpan.FromDays(3));
        }

        public async Task<bool> CheckNewVersionAsync()
        {
            this.LastVersionCheckedDate = DateTimeOffset.Now;
#if DEBUG
            //return false;
#endif
            return await this.versionCheck.CheckAsync(projectReleaseRssUrl);
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
