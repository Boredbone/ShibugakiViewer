﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using ImageLibrary.Core;
using ImageLibrary.File;

namespace ImageLibrary.Search
{

    public class SearchSortManager
    {
        public string AlbumName { get; set; } = "Album";
        public string GroupName { get; set; } = "Group";

        public int SearchHistoryLength { get; set; } = 16;

        private Dictionary<string, SearchInformation> SearchSettings { get; set; }

        public ObservableCollection<SearchInformation> FavoriteSearchList { get; }
        public ObservableCollection<SearchInformation> SearchHistory { get; }

        private List<SortSetting> DefaultSort { get; set; }
        private List<SortSetting> DefaultGroupSort { get; set; }



        public SearchSortManager()
        {
            this.SearchSettings = new Dictionary<string, SearchInformation>();
            this.FavoriteSearchList = new ObservableCollection<SearchInformation>();
            this.DefaultSort = new List<SortSetting>();
            this.DefaultGroupSort = new List<SortSetting>();
            this.SearchHistory = new ObservableCollection<SearchInformation>();
        }

        private void RefreshSearchHistory()
        {
            this.SearchHistory.Clear();

            this.SearchSettings
                .OrderByDescending(x => x.Value.DateLastUsed)
                .Take(this.SearchHistoryLength)
                .ForEach(x =>
                {
                    x.Value.Key = x.Key;
                    this.SearchHistory.Add(x.Value);
                });

        }

        public void InitializeSearchHistory(IEnumerable<SearchInformation> source)
        {
            this.SearchSettings = source
                .ToDictionary(x =>
                {
                    if (string.IsNullOrEmpty(x.Key))
                    {
                        x.Key = Guid.NewGuid().ToString();
                    }
                    return x.Key;
                }, x => x);
            this.RefreshSearchHistory();
        }

        public void InitializeFovoriteSearch(IEnumerable<SearchInformation> source)
        {
            this.FavoriteSearchList.Clear();

            source.Select(x =>
            {
                if (x.Key.IsNullOrEmpty())
                {
                    x.Key = Guid.NewGuid().ToString();
                }
                return x;
            })
            .ForEach(x => this.FavoriteSearchList.Add(x));

        }

        //public SearchInformation[] GetAllHistory()
        //{
        //    return this.SearchSettings.Select(x =>
        //    {
        //        x.Value.Key = x.Key;
        //        return x.Value;
        //    })
        //    .ToArray();
        //}

        public SearchInformation GetLatest()
            => this.SearchSettings
                .Select(x => x.Value)
                .OrderByDescending(x => x.DateLastUsed)
                .FirstOrDefault();


        private SearchInformation AddSearch(SearchInformation setting)
        {
            var key = Guid.NewGuid().ToString();
            setting.Key = key;
            //var value = setting;//.Clone();
            this.SearchSettings.Add(setting.Key, setting);
            //setting.Key = key;
            return setting;
        }

        private SearchInformation AddSearchToDictionaryMain(SearchInformation setting)
        {
            if (setting.Key == null || !this.SearchSettings.ContainsKey(setting.Key))
            {
                //内容の同じ検索が履歴に残っていたら置き換える
                var resembler = this.SearchSettings
                    .FirstOrDefault(x => x.Value.SettingEquals(setting));

                if (resembler.Value != null)
                {
                    var newitem = setting;//.Clone();

                    if (newitem.ThumbnailFilePath == null)
                    {
                        newitem.ThumbnailFilePath = resembler.Value.ThumbnailFilePath;
                    }
                    this.SearchSettings[resembler.Key] = newitem;
                    newitem.Key = resembler.Key;
                    return newitem;
                }

                return this.AddSearch(setting);
            }

            var existing = this.SearchSettings[setting.Key];

            if (existing.HasSameSearch(setting))
            {
                var item = setting;//.Clone();
                this.SearchSettings[setting.Key] = item;
                item.Key = setting.Key;
                return item;
            }
            else
            {
                return this.AddSearch(setting);
            }
        }

        public SearchInformation AddSearchToDictionary(SearchInformation setting)
        {
            var item = this.AddSearchToDictionaryMain(setting);
            item.SetDateToNow();
            return item;
        }

        public void RefreshList(SearchInformation setting)
        {
            this.RefreshSearchHistory();

            if (setting == null)
            {
                return;
            }

            var favoriteIndex = this.FavoriteSearchList
                .FindIndex(x => x.Key != null && x.Key.Equals(setting.Key));

            if (this.FavoriteSearchList.ContainsIndex(favoriteIndex))
            {
                this.FavoriteSearchList.RemoveAt(favoriteIndex);
                this.FavoriteSearchList.Insert(favoriteIndex, this.SearchSettings[setting.Key]);
            }
        }

        public IEnumerable<SortSetting> GetDefaultSort()
        {
            return this.DefaultSort.Select(x => x.Clone());
        }

        public IEnumerable<SortSetting> GetDefaultGroupSort()
        {
            return this.DefaultGroupSort.Select(x => x.Clone());
        }

        public void SetDefaultSort(IEnumerable<SortSetting> source)
        {
            if (source == null)
            {
                return;
            }
            var list = source.Select(x => x.Clone()).ToList();

            if (list.Count > 0 || this.DefaultSort == null)
            {
                this.DefaultSort = list;
            }
        }

        public void SetDefaultGroupSort(IEnumerable<SortSetting> source)
        {
            if (source == null)
            {
                return;
            }
            var list = source.Select(x => x.Clone()).ToList();

            if (list.Count > 0 || this.DefaultGroupSort == null)
            {
                this.DefaultGroupSort = list;
            }
        }


        //public IEnumerable<SortSetting> GetGroupSort(string groupKey)
        //{
        //    var group = LibraryOwner.GetCurrent().GetRecord(groupKey);
        //    if (group != null)
        //    {
        //        var sort = group.GetSort()?.ToArray();
        //
        //        if (sort != null && sort.Length > 0)
        //        {
        //            return group.GetSort();
        //        }
        //    }
        //    return this.DefaultGroupSort;
        //    
        //}


        public string GenerateAlbumName()
        {
            for (int i = 0; i < 1024; i++)
            {
                var name = AlbumName + i.ToString();

                if (!this.FavoriteSearchList.Any(x => name.Equals(x.Name)))
                {
                    return name;
                }
            }

            return AlbumName;
        }


        /// <summary>
        /// 検索条件をアルバムに登録
        /// </summary>
        /// <param name="item"></param>
        public void MarkSearchFavorite(SearchInformation item)
        {
            if (item == null)
            {
                return;
            }
            if (item.Key.IsNullOrEmpty())
            {
                item.Key = Guid.NewGuid().ToString();
            }
            if (!this.FavoriteSearchList.Any(x => x.Key.Equals(item.Key)))
            {
                item.Name = this.GenerateAlbumName();
                this.FavoriteSearchList.Insert(0, item);
            }
        }

        /// <summary>
        /// アルバムからアイテムを削除
        /// </summary>
        /// <param name="item"></param>
        public void MarkSearchUnfavorite(SearchInformation item)
        {
            if (item == null)
            {
                return;
            }
            var index = this.FavoriteSearchList.FindIndex(x => x.Key.Equals(item.Key));
            if (index >= 0)
            {
                this.FavoriteSearchList.RemoveAt(index);
            }
        }
    }
}
