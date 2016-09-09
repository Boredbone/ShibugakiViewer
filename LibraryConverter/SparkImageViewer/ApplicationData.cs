using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using System.ComponentModel;
using System.Collections.ObjectModel;
using SparkImageViewer.FileSort;
using SparkImageViewer.FileSearch;
using Boredbone.Utility.Notification;

namespace SparkImageViewer.DataModel
{
    public class ApplicationCoreData : NotificationBase
    {

        private const string settingFileName = "settings.xml";
        private XmlSettingManager<ApplicationSettings> settingXml
            = new XmlSettingManager<ApplicationSettings>(settingFileName);
        private const string librarySettingFileName = "libsettings.xml";
        private XmlSettingManager<LibrarySettings> librarySettingXml
            = new XmlSettingManager<LibrarySettings>(librarySettingFileName);
        private const int xmlVersion = 4;

        public ApplicationSettings coreSettings;
        public LibrarySettings librarySettings;
        //private ImageLibrary library;



        public double LeftBarWidth { get { return 290; } }

        private int SearchHistoryLength { get { return 16; } }

        public double ThumbNailSize
        {
            get
            {
                if (this.coreSettings.ThumbNailSize < 32)
                {
                    this.coreSettings.ThumbNailSize = 256.0;
                }
                return this.coreSettings.ThumbNailSize;
            }
            set
            {
                this.coreSettings.ThumbNailSize = value;
                RaisePropertyChanged("ThumbNailSize");
            }
        }
        
        public bool IsGroupingEnabled
        {
            get { return this.coreSettings.IsGroupingEnabled; }
            set
            {
                if (this.coreSettings.IsGroupingEnabled != value)
                {
                    this.coreSettings.IsGroupingEnabled = value;
                    RaisePropertyChanged("IsGroupingEnabled");
                }
            }
        }
        
        public bool IsFlipReversed
        {
            get { return this.coreSettings.IsFlipReversed; }
            set
            {
                if (this.coreSettings.IsFlipReversed != value)
                {
                    this.coreSettings.IsFlipReversed = value;
                    RaisePropertyChanged("IsFlipReversed");
                }
            }
        }
        
        public bool IsOpenNavigationWithSingleTapEnabled
        {
            get { return this.coreSettings.IsOpenNavigationWithSingleTapEnabled; }
            set
            {
                if (this.coreSettings.IsOpenNavigationWithSingleTapEnabled != value)
                {
                    this.coreSettings.IsOpenNavigationWithSingleTapEnabled = value;
                    RaisePropertyChanged("IsOpenNavigationWithSingleTapEnabled");
                }
            }
        }

        public bool IsFlipAnimationEnabled
        {
            get { return this.coreSettings.IsFlipAnimationEnabled; }
            set
            {
                if (this.coreSettings.IsFlipAnimationEnabled != value)
                {
                    this.coreSettings.IsFlipAnimationEnabled = value;
                    RaisePropertyChanged("IsFlipAnimationEnabled");
                }
            }
        }

        public bool IsCmsEnabled
        {
            get { return this.coreSettings.IsCmsEnabled; }
            set
            {
                if (this.coreSettings.IsCmsEnabled != value)
                {
                    this.coreSettings.IsCmsEnabled = value;
                    RaisePropertyChanged("IsCmsEnabled");
                }
            }
        }

        public bool IsAnimatedGifEnabled
        {
            get { return !this.coreSettings.IsGifAnimationDisabled; }
            set
            {
                if (this.coreSettings.IsGifAnimationDisabled == value)
                {
                    this.coreSettings.IsGifAnimationDisabled = !value;
                    RaisePropertyChanged(nameof(IsAnimatedGifEnabled));
                }
            }
        }

        public bool UseExtendedMouseButtonsToSwitchImage
        {
            get { return this.coreSettings.UseExtendedMouseButtonsToSwitchImage; }
            set
            {
                if (this.coreSettings.UseExtendedMouseButtonsToSwitchImage != value)
                {
                    this.coreSettings.UseExtendedMouseButtonsToSwitchImage = value;
                    RaisePropertyChanged("UseExtendedMouseButtonsToSwitchImage");
                }
            }
        }

        public bool RefreshLibraryOnLaunched
        {
            get { return this.coreSettings.RefreshLibraryOnLaunched; }
            set
            {
                if (this.coreSettings.RefreshLibraryOnLaunched != value)
                {
                    this.coreSettings.RefreshLibraryOnLaunched = value;
                    RaisePropertyChanged("RefreshLibraryOnLaunched");
                }
            }
        }

        public bool RefreshLibraryCompletely
        {
            get { return this.coreSettings.RefreshLibraryCompletely; }
            set
            {
                if (this.coreSettings.RefreshLibraryCompletely != value)
                {
                    this.coreSettings.RefreshLibraryCompletely = value;
                    RaisePropertyChanged("RefreshLibraryCompletely");
                }
            }
        }

        public bool IsLibraryRefreshStatusVisible
        {
            get { return this.coreSettings.IsLibraryRefreshStatusVisible; }
            set
            {
                if (this.coreSettings.IsLibraryRefreshStatusVisible != value)
                {
                    this.coreSettings.IsLibraryRefreshStatusVisible = value;
                    RaisePropertyChanged("IsLibraryRefreshStatusVisible");
                }
            }
        }

        public int CursorKeyBind
        {
            get { return this.coreSettings.CursorKeyBind; }
            set
            {
                if (this.coreSettings.CursorKeyBind != value)
                {
                    this.coreSettings.CursorKeyBind = value;
                    RaisePropertyChanged("CursorKeyBind");
                }
            }
        }



        public bool IsProfessionalFolderSettingEnabled
        {
            get { return this.coreSettings.IsProfessionalFolderSettingEnabled; }
            set
            {
                if (this.coreSettings.IsProfessionalFolderSettingEnabled != value)
                {
                    this.coreSettings.IsProfessionalFolderSettingEnabled = value;
                    RaisePropertyChanged("IsProfessionalFolderSettingEnabled");
                }
            }
        }


        public int SlideshowAnimationTimeMillisec
        {
            get { return this.coreSettings.SlideshowAnimationTimeMillisec; }
            set
            {
                if (this.coreSettings.SlideshowAnimationTimeMillisec != value)
                {
                    this.coreSettings.SlideshowAnimationTimeMillisec = value;
                    RaisePropertyChanged(nameof(SlideshowAnimationTimeMillisec));
                }
            }
        }

        public int SlideshowFlipTimeMillisec
        {
            get { return this.coreSettings.SlideshowFlipTimeMillisec; }
            set
            {
                if (this.coreSettings.SlideshowFlipTimeMillisec != value)
                {
                    this.coreSettings.SlideshowFlipTimeMillisec = value;
                    RaisePropertyChanged(nameof(SlideshowFlipTimeMillisec));
                }
            }
        }



        public bool IsSlideshowResizingAlways
        {
            get { return this.coreSettings.IsSlideshowResizingAlways; }
            set
            {
                if (this.coreSettings.IsSlideshowResizingAlways != value)
                {
                    this.coreSettings.IsSlideshowResizingAlways = value;
                    RaisePropertyChanged(nameof(IsSlideshowResizingAlways));
                }
            }
        }


        public bool IsSlideshowResizeToFill
        {
            get { return this.coreSettings.IsSlideshowResizeToFill; }
            set
            {
                if (this.coreSettings.IsSlideshowResizeToFill != value)
                {
                    this.coreSettings.IsSlideshowResizeToFill = value;
                    RaisePropertyChanged(nameof(IsSlideshowResizeToFill));
                }
            }
        }


        public bool IsSlideshowRandom
        {
            get { return this.coreSettings.IsSlideshowRandom; }
            set
            {
                if (this.coreSettings.IsSlideshowRandom != value)
                {
                    this.coreSettings.IsSlideshowRandom = value;
                    RaisePropertyChanged(nameof(IsSlideshowRandom));
                }
            }
        }

        public bool IsSlideshowFullScreen
        {
            get { return this.coreSettings.IsSlideshowFullScreen; }
            set
            {
                if (this.coreSettings.IsSlideshowFullScreen != value)
                {
                    this.coreSettings.IsSlideshowFullScreen = value;
                    RaisePropertyChanged(nameof(IsSlideshowFullScreen));
                }
            }
        }

        public bool IsCoreSettingsLoaded { get; set; }
        public bool IsLibrarySettingsLoaded { get; set; }

        public bool IsSVOLanguage { get; private set; }


        public bool IsViewerPageTopBarFixed
        {
            get { return this.coreSettings.IsViewerPageTopBarFixed; }
            set { this.coreSettings.IsViewerPageTopBarFixed = value; }
        }

        public bool IsViewerPageLeftBarFixed
        {
            get { return this.coreSettings.IsViewerPageLeftBarFixed; }
            set { this.coreSettings.IsViewerPageLeftBarFixed = value; }
        }

        public bool LastSearchedFavorite
        {
            get { return this.coreSettings.LastSearchedFavorite; }
            set { this.coreSettings.LastSearchedFavorite = value; }
        }




        public Dictionary<string, FolderInformation> Folders
        {
            get
            {
                if (this.librarySettings.Folders == null)
                {
                    this.librarySettings.Folders = new Dictionary<string, FolderInformation>();
                }
                return this.librarySettings.Folders;
            }
        }

        //private ResourceLoader ResourceLoader { get; set; }

        public bool TagEdited => this.Tags.IsEdited;

        public TagDictionary Tags { get; }

        public List<string> XmlLoadingMessages { get; private set; }


        public SearchSortManager Searcher { get; }
        //public KnownFoldersManager KnownFoldersManager { get; }

        private string directory;

        public ApplicationCoreData(string directory)
        {
            this.directory = directory;

            this.IsCoreSettingsLoaded = false;
            this.IsLibrarySettingsLoaded = false;

            settingXml = new XmlSettingManager<ApplicationSettings>
                (System.IO.Path.Combine(directory, settingFileName));

            librarySettingXml = new XmlSettingManager<LibrarySettings>
                (System.IO.Path.Combine(directory, librarySettingFileName));

            this.coreSettings = new ApplicationSettings();

            this.Tags = new TagDictionary();

            this.XmlLoadingMessages = new List<string>();

            this.Searcher = new SearchSortManager();
            //this.KnownFoldersManager = new KnownFoldersManager();
        }








        private static AsyncLock storageAccessLock = new AsyncLock();


        private async Task<bool> LoadSettingsAsync()
        {

            using (await storageAccessLock.LockAsync())
            {
                this.XmlLoadingMessages.Clear();

                var loaded = await this.settingXml.LoadXmlAsync(XmlLoadingOptions.ReturnNull);

                if (loaded.Value != null)
                {
                    this.coreSettings = loaded.Value;
                }
                else
                {
                    return false;
                    //オプション初期値
                    //this.coreSettings = new ApplicationSettings();
                }


                if (loaded.Message != null)
                {
                    this.XmlLoadingMessages.Add(loaded.Message.Message);
                }

                //coreSettings.SearchSettings = coreSettings.SearchSettingsOld.Select(x => new SearchInformation(x)).ToList();


                if (this.coreSettings.Version < 3)
                {
                    //Ver.3以降
                    this.coreSettings.SlideshowAnimationTimeMillisec = 300;
                    this.coreSettings.SlideshowFlipTimeMillisec = 5000;
                    this.coreSettings.IsSlideshowResizingAlways = false;
                    this.coreSettings.IsSlideshowResizeToFill = false;
                    this.coreSettings.IsSlideshowRandom = false;
                }
                if (this.coreSettings.Version < 3)
                {
                    //Ver.4以降
                    this.coreSettings.IsSlideshowFullScreen = true;
                }


                this.IsCoreSettingsLoaded = true;


                //LibrarySettings tmpLibSettings;
                if (this.coreSettings.Version < 4)
                {
                    //Ver.4以降
                    var tmpLibSettings = new LibrarySettings();
                    tmpLibSettings.CopyFrom(this.coreSettings);


                    this.InitializeLibrarySettings(tmpLibSettings);
                }

                this.coreSettings.ClearOldPoperties();

                return true;
            }
        }

        private async Task<bool> LoadLibrarySettingsAsync()
        {
            using (await storageAccessLock.LockAsync())
            {
                LibrarySettings tmpLibSettings;

                var loadedLibSettings = await this.librarySettingXml.LoadXmlAsync
                        (XmlLoadingOptions.IgnoreNotFound | XmlLoadingOptions.ReturnNull);

                if (loadedLibSettings.Value != null)
                {
                    tmpLibSettings = loadedLibSettings.Value;
                }
                else
                {
                    return false;
                    //オプション初期値
                    //tmpLibSettings = new LibrarySettings();
                }


                if (loadedLibSettings.Message != null)
                {
                    this.XmlLoadingMessages.Add(loadedLibSettings.Message.Message);
                }

                this.InitializeLibrarySettings(tmpLibSettings);

                return true;
            }
        }

        private void InitializeLibrarySettings(LibrarySettings savedData)
        {

            this.Tags.SetSource(savedData.RegisteredTags);


            if (savedData.DefaultSort == null
                    || savedData.DefaultSort.Count == 0)
            {
                var sort = new List<SortSetting>();

                sort.Add(new SortSetting()
                {
                    Property = FileProperty.DateTimeModified,
                    IsDescending = true,
                });

                savedData.DefaultSort = sort;
            }
            this.Searcher.DefaultSort = savedData.DefaultSort;


            if (savedData.DefaultGroupSort == null
                    || savedData.DefaultGroupSort.Count == 0)
            {
                var sort = new List<SortSetting>();

                sort.Add(new SortSetting()
                {
                    Property = FileProperty.RelativePathSequenceNum,
                    IsDescending = false,
                });

                savedData.DefaultGroupSort = sort;
            }
            this.Searcher.DefaultGroupSort = savedData.DefaultGroupSort;


            if (savedData.GroupLeaderDictionary == null)
            {
                savedData.GroupLeaderDictionary = new Dictionary<string, GroupLeaderFile>();
            }
            this.Searcher.GroupLeaderDictionary = savedData.GroupLeaderDictionary;



            if (savedData.SearchSettings == null || savedData.SearchSettings.Count == 0)
            {
                savedData.SearchSettings = new List<SearchInformation>();
                //savedData.SearchSettings.Add(new SearchInformation(new ComplexSearchSetting(false)));
            }
            else
            {
                var search = savedData.SearchSettings.ToList();
                savedData.SearchSettings = search;
            }

            savedData.SearchSettings.ForEach(x => this.Searcher.AddSearch(x));

            if (savedData.FavoriteSearch != null)
            {
                this.Searcher.FavoriteSearchList
                    = new ObservableCollection<SearchInformation>
                        (savedData.FavoriteSearch.Select(x => x.Clone()));
            }


                if (savedData.Folders == null)
                {
                    savedData.Folders = new Dictionary<string, FolderInformation>();
                }
                



            foreach (var item in this.Searcher.GroupLeaderDictionary)
            {
                item.Value.UniqueKey = item.Key;
            }


            this.librarySettings = savedData;
            this.IsLibrarySettingsLoaded = true;


        }

        public async Task<bool> InitSettingsAsync()
        {
            if (!this.IsCoreSettingsLoaded)
            {
                var result = await this.LoadSettingsAsync();
                if (!result)
                {
                    return false;
                }
            }
            if (!this.IsLibrarySettingsLoaded)
            {
                var result = await this.LoadLibrarySettingsAsync();
                if (!result)
                {
                    return false;
                }
            }
            return true;
        }



    }

    /*
    public static class StorageExtensions
    {
        public static string GetUniqueKey(this StorageFolder folder)
        {
            return folder.FolderRelativeId.Replace("\\", ">"); ;
        }

        /// <summary>
        /// ファイルの識別子を取得
        /// </summary>
        public static string GetFileId(this StorageFile file)
        {
            var path = file.Path;
            if (path == null || path.Length < 3)
            {
                path = file.FolderRelativeId;
            }
            return path;
        }
    }*/
}
