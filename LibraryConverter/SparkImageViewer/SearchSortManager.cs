using System;
using System.Collections.Generic;
using System.Linq;
using Boredbone.Utility.Extensions;
using System.Collections.ObjectModel;
using SparkImageViewer.FileSearch;
using SparkImageViewer.FileSort;
using SparkImageViewer.DataModel;

namespace SparkImageViewer.DataModel
{
    public class SearchSortManager
    {


        public string AlbumName { get; set; } = "Album";
        public string GroupName { get; set; } = "Group";

        public Dictionary<string, SearchInformation> SearchSettings { get; set; }
        


        //public SearchInformation CurrentSearch
        //{
        //    get
        //    {
        //        if (this.CurrentSearchKey != null)
        //        {
        //            SearchInformation search;
        //            if (this.SearchSettings.TryGetValue(this.CurrentSearchKey, out search))
        //            {
        //                return search;
        //            }
        //        }


        //        if (this.SearchSettings.Count == 0)
        //        {
        //            return new SearchInformation(new ComplexSearchSetting(false));
        //        }
        //        else
        //        {
        //            var setting = this.SearchSettings
        //                .OrderByDescending(x => x.Value.DateLastUsed)
        //                .First();
        //            this.CurrentSearchKey = setting.Key;
        //            return setting.Value;
        //        }
        //        //if (this.SearchSettingsList.ContainsIndex(CurrentSearchIndex))
        //        //{
        //        //    return this.SearchSettingsList[CurrentSearchIndex];
        //        //}
        //        //else
        //        //{
        //        //    return this.SearchSettingsList.Last();//TODO
        //        //}
        //    }
        //}

        public ObservableCollection<SearchInformation> FavoriteSearchList { get; set; }
        public SearchInformation CurrentFavoritSearch { get; private set; }

        //private int CurrentSearchIndex { get; set; }

        //private string _fieldCurrentSearchKey;

        //private string _fieldCurrentSearchKey;
        //public string CurrentSearchKey
        //{
        //    get { return _fieldCurrentSearchKey; }
        //    set
        //    {
        //        if (_fieldCurrentSearchKey != value)
        //        {
        //            _fieldCurrentSearchKey = value;

        //            this.CurrentFavoritSearch = this.FavoriteSearchList
        //                .FirstOrDefault(x => x.Key != null && x.Key.Equals(value));
        //            //var tx = this.CurrentFavoritSearch.ToString();
        //        }
        //    }
        //}


        public List<SortSetting> DefaultSort { get; set; }
        public List<SortSetting> DefaultGroupSort { get; set; }
        public Dictionary<string, GroupLeaderFile> GroupLeaderDictionary { get; set; }


        public SearchSortManager()
        {
            this.SearchSettings = new Dictionary<string, SearchInformation>();
            this.FavoriteSearchList = new ObservableCollection<SearchInformation>();
            this.DefaultSort = new List<SortSetting>();
            this.DefaultGroupSort = new List<SortSetting>();
            this.GroupLeaderDictionary = new Dictionary<string, GroupLeaderFile>();
        }





        public string AddSearch(SearchInformation setting)
        {
            var key = Guid.NewGuid().ToString();
            setting.Key = key;
            var value = setting.Clone();
            this.SearchSettings.Add(key, value);
            return key;
            //this.SearchSettingsList.Add(new SearchInformation(setting));
            //this.CurrentSearchIndex = this.SearchSettingsList.Count - 1;
        }

        //private string AddSearchToDictionary(SearchInformation setting)
        //{
        //    if (setting.Key == null || !this.SearchSettings.ContainsKey(setting.Key))
        //    {
        //        //内容の同じ検索が履歴に残っていたら置き換える
        //        var resembler = this.SearchSettings
        //            .FirstOrNull(x => x.Value.SettingEquals(setting));

        //        if (resembler != null)
        //        {
        //            var newitem = setting.Clone();

        //            if (newitem.ThumbnailFile == null)
        //            {
        //                newitem.ThumbnailFile = resembler.Value.Value.ThumbnailFile;
        //            }
        //            this.SearchSettings[resembler.Value.Key] = newitem;
        //            return resembler.Value.Key;
        //        }

        //        return this.AddSearch(setting);
        //    }

        //    var existing = this.SearchSettings[setting.Key];

        //    if (existing.HasSameSearch(setting))
        //    {
        //        this.SearchSettings[setting.Key] = setting.Clone();
        //        return setting.Key;
        //    }
        //    else
        //    {
        //        return this.AddSearch(setting);
        //    }
        //}
        //public string AddSearchToDictionaryAndActivate(SearchInformation setting)
        //{
        //    var key = this.AddSearchToDictionary(setting);
        //    setting.Key = key;
        //    this.CurrentSearchKey = key;

        //    var favoriteIndex = this.FavoriteSearchList
        //        .FindIndex(x => x.Key != null && x.Key.Equals(key));

        //    if (this.FavoriteSearchList.ContainsIndex(favoriteIndex))
        //    {
        //        this.FavoriteSearchList.RemoveAt(favoriteIndex);
        //        this.FavoriteSearchList.Insert(favoriteIndex, this.SearchSettings[key]);
        //    }


        //    return key;
        //}
        
        //public List<SearchInformation> GetSearchList()
        //{
        //    return this.SearchSettings.Select(x =>
        //    {
        //        var setting = x.Value.Clone();
        //        setting.Key = x.Key;
        //        return setting;
        //    }).ToList();
        //}


        //public bool SetDefaultSort(IEnumerable<SortSetting> source)
        //{
        //    this.DefaultSort = source.Select(x => x.Clone()).ToList();
        //    return true;
        //}

        //public bool SetDefaultGroupSort(IEnumerable<SortSetting> source)
        //{
        //    this.DefaultGroupSort = source.Select(x => x.Clone()).ToList();
        //    return true;
        //}

        //public IEnumerable<SortSetting> GetSort()
        //{
        //    var sort = CurrentSearch.SortSettings;
        //    if (sort == null || sort.Count == 0)
        //    {
        //        return this.DefaultSort;
        //    }
        //    return sort;
        //}
        //public IEnumerable<SortSetting> GetGroupSort(string groupKey)
        //{
        //    GroupLeaderFile group;
        //    var exists = this.GroupLeaderDictionary.TryGetValue(groupKey, out group);
        //    if (!exists)
        //    {
        //        return this.DefaultGroupSort;
        //    }

        //    var sort = group.SortSettings;
        //    if (sort == null || sort.Count == 0)
        //    {
        //        return this.DefaultGroupSort;
        //    }
        //    return sort;
        //}




        //public string GenerateAlbumName()
        //{
        //    for (int i = 0; i < 1024; i++)
        //    {
        //        var name = AlbumName + i.ToString();

        //        if (!this.FavoriteSearchList.Any(x => name.Equals(x.Name)))
        //        {
        //            return name;
        //        }
        //    }

        //    return AlbumName;
        //}

        //public string GenerateGroupName()
        //{
        //    for (int i = 0; i < 1024; i++)
        //    {
        //        var name = GroupName + i.ToString();

        //        if (!this.GroupLeaderDictionary.Any(x => name.Equals(x.Value.FileName)))
        //        {
        //            return name;
        //        }
        //    }

        //    return GroupName;
        //}


    }
}
