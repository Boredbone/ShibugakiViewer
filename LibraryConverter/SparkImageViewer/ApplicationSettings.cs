using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using SparkImageViewer.FileSearch;
using SparkImageViewer.FileSort;

namespace SparkImageViewer.DataModel
{

    [DataContract]
    [KnownType(typeof(UnitSearchSetting))]
    [KnownType(typeof(ComplexSearchSetting))]
    [KnownType(typeof(DateTimeOffset))]
    public class ApplicationSettings
    {
        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public Dictionary<string, FolderInformation> Folders { get; set; }
        [DataMember]
        public Dictionary<int, TagInformation> RegisteredTags { get; set; }
        //[DataMember(Name = "SearchSettings")]
        //public List<ComplexSearchSetting> SearchSettingsOld { get; set; }
        [DataMember]
        public List<SearchInformation> SearchSettings { get; set; }
        [DataMember]
        public List<SearchInformation> FavoriteSearch { get; set; }
        [DataMember]
        public Dictionary<string, GroupLeaderFile> GroupLeaderDictionary { get; set; }
        [DataMember]
        public double ThumbNailSize { get; set; }
        [DataMember]
        public List<SortSetting> DefaultSort { get; set; }
        [DataMember]
        public List<SortSetting> DefaultGroupSort { get; set; }
        [DataMember]
        public bool IsFlipAnimationEnabled { get; set; }
        [DataMember]
        public bool IsViewerPageTopBarFixed { get; set; }
        [DataMember]
        public bool IsViewerPageLeftBarFixed { get; set; }
        [DataMember]
        public bool LastSearchedFavorite { get; set; }

        [DataMember]
        public bool IsGroupingEnabled { get; set; }
        [DataMember]
        public bool IsFlipReversed { get; set; }
        [DataMember]
        public bool IsOpenNavigationWithSingleTapEnabled { get; set; }
        [DataMember]
        public bool IsCmsEnabled { get; set; }
        [DataMember]
        public bool IsGifAnimationDisabled { get; set; }
        [DataMember]
        public bool UseExtendedMouseButtonsToSwitchImage { get; set; }

        [DataMember]
        public bool RefreshLibraryOnLaunched { get; set; }
        [DataMember]
        public bool RefreshLibraryCompletely { get; set; }
        [DataMember]
        public bool IsLibraryRefreshStatusVisible { get; set; }

        [DataMember]
        public int CursorKeyBind { get; set; }

        [DataMember]
        public bool IsProfessionalFolderSettingEnabled { get; set; }

        //Ver.3以降
        [DataMember]
        public int SlideshowAnimationTimeMillisec { get; set; }
        [DataMember]
        public int SlideshowFlipTimeMillisec { get; set; }
        [DataMember]
        public bool IsSlideshowResizingAlways { get; set; }
        [DataMember]
        public bool IsSlideshowResizeToFill { get; set; }
        [DataMember]
        public bool IsSlideshowRandom { get; set; }

        //Ver.4以降
        [DataMember]
        public bool IsSlideshowFullScreen { get; set; }



        public ApplicationSettings()
        {
            this.Folders = new Dictionary<string, FolderInformation>();
            this.RegisteredTags = new Dictionary<int, TagInformation>();
            this.SearchSettings = new List<SearchInformation>();
            this.GroupLeaderDictionary = new Dictionary<string, GroupLeaderFile>();
            this.DefaultSort = new List<SortSetting>();
            this.DefaultGroupSort = new List<SortSetting>();


            this.IsGroupingEnabled = true;
            this.IsOpenNavigationWithSingleTapEnabled = true;
            this.UseExtendedMouseButtonsToSwitchImage = true;
            this.IsViewerPageLeftBarFixed = false;
            this.IsViewerPageTopBarFixed = false;
            this.ThumbNailSize = 256;
            this.RefreshLibraryOnLaunched = true;
            this.RefreshLibraryCompletely = false;
            this.IsLibraryRefreshStatusVisible = true;
            this.IsProfessionalFolderSettingEnabled = false;
            this.IsCmsEnabled = false;
            this.IsGifAnimationDisabled = false;

            this.CursorKeyBind = 0;

            //Ver.3以降
            this.SlideshowAnimationTimeMillisec = 300;
            this.SlideshowFlipTimeMillisec = 5000;
            this.IsSlideshowResizingAlways = false;
            this.IsSlideshowResizeToFill = false;
            this.IsSlideshowRandom = false;


            //Ver.4以降
            this.IsSlideshowFullScreen = true;

        }

        public void ClearOldPoperties()
        {
            this.Folders = null;
            this.RegisteredTags = null;
            this.SearchSettings = null;
            this.FavoriteSearch = null;
            this.GroupLeaderDictionary = null;
            this.DefaultSort = null;
            this.DefaultGroupSort = null;
        }
    }



    [DataContract]
    [KnownType(typeof(UnitSearchSetting))]
    [KnownType(typeof(ComplexSearchSetting))]
    [KnownType(typeof(DateTimeOffset))]
    public class LibrarySettings
    {
        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public Dictionary<string, FolderInformation> Folders { get; set; }
        [DataMember]
        public Dictionary<int, TagInformation> RegisteredTags { get; set; }
        [DataMember]
        public List<SearchInformation> SearchSettings { get; set; }
        [DataMember]
        public List<SearchInformation> FavoriteSearch { get; set; }
        [DataMember]
        public Dictionary<string, GroupLeaderFile> GroupLeaderDictionary { get; set; }
        [DataMember]
        public List<SortSetting> DefaultSort { get; set; }
        [DataMember]
        public List<SortSetting> DefaultGroupSort { get; set; }



        public LibrarySettings()
        {
            this.Folders = new Dictionary<string, FolderInformation>();
            this.RegisteredTags = new Dictionary<int, TagInformation>();
            this.SearchSettings = new List<SearchInformation>();
            this.FavoriteSearch = new List<SearchInformation>();
            this.GroupLeaderDictionary = new Dictionary<string, GroupLeaderFile>();
            this.DefaultSort = new List<SortSetting>();
            this.DefaultGroupSort = new List<SortSetting>();
        }

        public void CopyFrom(ApplicationSettings source)
        {
            this.Folders = source.Folders;
            this.RegisteredTags = source.RegisteredTags;
            this.SearchSettings = source.SearchSettings;
            this.FavoriteSearch = source.FavoriteSearch;
            this.GroupLeaderDictionary = source.GroupLeaderDictionary;
            this.DefaultSort = source.DefaultSort;
            this.DefaultGroupSort = source.DefaultGroupSort;
        }
    }
}
