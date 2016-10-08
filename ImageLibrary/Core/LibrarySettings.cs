using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using ImageLibrary.Creation;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;

namespace ImageLibrary.Core
{
    /// <summary>
    /// ストレージに保存するライブラリの設定
    /// </summary>
    [DataContract]
    [KnownType(typeof(UnitSearch))]
    [KnownType(typeof(ComplexSearch))]
    [KnownType(typeof(DateTimeOffset))]
    public class LibrarySettings
    {
        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public List<SearchInformation> SearchSettings { get; private set; }
        [DataMember]
        public List<SearchInformation> FavoriteSearch { get; private set; }
        [DataMember]
        public List<SortSetting> DefaultSort { get; private set; }
        [DataMember]
        public List<SortSetting> DefaultGroupSort { get; private set; }

        [DataMember]
        public bool IsGroupingEnabled
        {
            get { return _fieldIsGroupingEnabled; }
            set
            {
                if (_fieldIsGroupingEnabled != value)
                {
                    _fieldIsGroupingEnabled = value;
                    this.isChanged = true;
                }
            }
        }
        private bool _fieldIsGroupingEnabled;

        [DataMember]
        public bool RefreshLibraryCompletely
        {
            get { return _fieldRefreshLibraryCompletely; }
            set
            {
                if (_fieldRefreshLibraryCompletely != value)
                {
                    _fieldRefreshLibraryCompletely = value;
                    this.isChanged = true;
                }
            }
        }
        private bool _fieldRefreshLibraryCompletely;


        [DataMember]
        public bool CheckFileShellInformation
        {
            get { return _fieldCheckFileShellInformation; }
            set
            {
                if (_fieldCheckFileShellInformation != value)
                {
                    _fieldCheckFileShellInformation = value;
                    this.isChanged = true;
                }
            }
        }
        private bool _fieldCheckFileShellInformation;



        private bool isChanged;


        public LibrarySettings()
        {
            this.SetDefault();
        }

        [OnDeserializing]
        public void OnDeserializing(StreamingContext sc)
        {
            this.SetDefault();
        }

        private void SetDefault()
        {
            this.SearchSettings = new List<SearchInformation>();
            this.FavoriteSearch = new List<SearchInformation>();
            this.DefaultSort = new List<SortSetting>();
            this.DefaultGroupSort = new List<SortSetting>();

            this.IsGroupingEnabled = true;
            this.RefreshLibraryCompletely = false;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="library"></param>
        /// <param name="version"></param>
        /// <param name="manager"></param>
        public void Save(Library library, int version, XmlSettingManager<LibrarySettings> manager)
        {
            if (!library.IsLibrarySettingsLoaded)
            {
                return;
            }

            var defaultSort = library.Searcher.GetDefaultSort().ToList();
            var defaultGroupSort = library.Searcher.GetDefaultGroupSort().ToList();

            if (!this.IsChanged(library, defaultSort, defaultGroupSort))
            {
                return;
            }

            this.SearchSettings = library.Searcher.SearchHistory.Select(x => x.Clone()).ToList();

            this.FavoriteSearch = library.Searcher.FavoriteSearchList.Select(x => x.Clone()).ToList();

            this.DefaultSort = defaultSort;
            this.DefaultGroupSort = defaultGroupSort;

            this.Version = version;

            manager.SaveXml(this);

            this.isChanged = false;
        }

        /// <summary>
        /// 前回の保存以降に変化があったか確認
        /// </summary>
        /// <param name="library"></param>
        /// <param name="defaultSort"></param>
        /// <param name="defaultGroupSort"></param>
        /// <returns></returns>
        private bool IsChanged(Library library, List<SortSetting> defaultSort, List<SortSetting> defaultGroupSort)
        {
            if (this.isChanged)
            {
                return true;
            }

            if (!this.SearchSettings.SequenceEqualParallel
                (library.Searcher.SearchHistory, (a, b) => a.ValueEquals(b)))
            {
                return true;
            }
            if (!this.FavoriteSearch.SequenceEqualParallel
                (library.Searcher.FavoriteSearchList, (a, b) => a.ValueEquals(b)))
            {
                return true;
            }
            if (!this.DefaultSort.SequenceEqualParallel
                (defaultSort, (a, b) => a.ValueEquals(b)))
            {
                return true;
            }
            if (!this.DefaultGroupSort.SequenceEqualParallel
                (defaultGroupSort, (a, b) => a.ValueEquals(b)))
            {
                return true;
            }
            return false;
        }


        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="library"></param>
        public void Initialize(Library library)
        {
            if (this.DefaultSort.IsNullOrEmpty())
            {
                var sort = new List<SortSetting>();

                sort.Add(new SortSetting()
                {
                    Property = FileProperty.DateTimeModified,
                    IsDescending = true,
                });

                this.DefaultSort = sort;
            }
            library.Searcher.SetDefaultSort(this.DefaultSort);


            if (this.DefaultGroupSort.IsNullOrEmpty())
            {
                var sort = new List<SortSetting>();

                sort.Add(new SortSetting()
                {
                    Property = FileProperty.FileNameSequenceNumRight,
                    IsDescending = false,
                });

                this.DefaultGroupSort = sort;
            }

            library.Searcher.SetDefaultGroupSort(this.DefaultGroupSort);

            if (this.SearchSettings.IsNullOrEmpty())
            {
                this.SearchSettings = new List<SearchInformation>();
            }
            else
            {
                var search = this.SearchSettings.ToList();
                this.SearchSettings = search;
            }

            library.Searcher.InitializeSearchHistory(this.SearchSettings);

            if (this.FavoriteSearch != null)
            {
                library.Searcher.InitializeFovoriteSearch(this.FavoriteSearch);
            }
            else
            {
                this.FavoriteSearch = new List<SearchInformation>();
            }
        }

        /// <summary>
        /// ファイルから読み込み
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="OnErrorOccurred"></param>
        /// <returns></returns>
        public static LibrarySettings Load
            (XmlSettingManager<LibrarySettings> manager, Action<string> OnErrorOccurred)
        {

            LibrarySettings tmpLibSettings;

            var loadedLibSettings = manager.LoadXml
                    (XmlLoadingOptions.UseBackup
                    | XmlLoadingOptions.IgnoreNotFound
                    | XmlLoadingOptions.ReturnNull);

            if (loadedLibSettings.Value != null)
            {
                tmpLibSettings = loadedLibSettings.Value;
            }
            else
            {
                //オプション初期値
                tmpLibSettings = new LibrarySettings();
            }


            if (loadedLibSettings.Message != null)
            {
                OnErrorOccurred(loadedLibSettings.Message.Message);
            }

            return tmpLibSettings;
        }
    }
}
