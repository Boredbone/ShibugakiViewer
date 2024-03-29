﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ImageLibrary.Viewer
{
    /// <summary>
    /// 各クライアントからライブラリにアクセスするための窓口
    /// </summary>
    public class LibraryFront : DisposableBase, ISearchResult
    {
        private const double updatedEventThrottleTime = 500.0;


        private Subject<CacheClearedEventArgs> CacheClearedSubject { get; }
        public IObservable<CacheClearedEventArgs> CacheCleared => this.CacheClearedSubject.AsObservable();

        private Subject<CacheUpdatedEventArgs> CacheUpdatedSubject { get; }
        public IObservable<CacheUpdatedEventArgs> CacheUpdated => this.CacheUpdatedSubject.AsObservable();

        private Subject<SearchCompletedEventArgs> SearchCompletedSubject { get; }
        public IObservable<SearchCompletedEventArgs> SearchCompleted => this.SearchCompletedSubject.AsObservable();

        private readonly ReactivePropertySlim<long> lengthPrivate;
        public ReadOnlyReactivePropertySlim<long> Length { get; }
        public int PageSize { get; set; }

        private Dictionary<long, Record> Cache { get; }

        public SearchInformation SearchInformation { get; private set; }

        public Record FeaturedGroup
        {
            get { return _fieldFeaturedGroup; }
            private set
            {
                if (_fieldFeaturedGroup != value)
                {
                    _fieldFeaturedGroup = value;
                    this.isGroupModePrivate.Value = (value != null);
                    this.FeaturedGroupChangedSubject.OnNext(value);
                }
            }
        }
        private Record _fieldFeaturedGroup = null;
        private BehaviorSubject<Record> FeaturedGroupChangedSubject { get; }
        public IObservable<Record> FeaturedGroupChanged => this.FeaturedGroupChangedSubject.AsObservable();

        public readonly ReactivePropertySlim<bool> isGroupModePrivate;
        public ReadOnlyReactivePropertySlim<bool> IsGroupMode { get; }

        public bool HasSearch => this.FeaturedGroup != null || this.SearchInformation != null;

        public IObservable<LibraryLoadResult> DatabaseAddedOrRemoved => this.library.Loaded;

        private bool isReset = false;

        private readonly Library library;
        private readonly AsyncLock asyncLock = new AsyncLock();
        private static readonly Record dummyRecord = new Record();


        public LibraryFront(Library library)
        {
            this.library = library;
            this.Cache = new Dictionary<long, Record>();

            this.CacheClearedSubject = new Subject<CacheClearedEventArgs>().AddTo(this.Disposables);
            this.CacheUpdatedSubject = new Subject<CacheUpdatedEventArgs>().AddTo(this.Disposables);
            this.SearchCompletedSubject = new Subject<SearchCompletedEventArgs>().AddTo(this.Disposables);

            this.isGroupModePrivate = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.IsGroupMode = this.isGroupModePrivate.ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);
            this.lengthPrivate = new ReactivePropertySlim<long>().AddTo(this.Disposables);
            this.Length = this.lengthPrivate.ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);

            this.FeaturedGroupChangedSubject = new BehaviorSubject<Record>(null).AddTo(this.Disposables);

            this.Length.Pairwise().Subscribe(x =>
            {

                if (x.NewItem < 0)
                {
                    Debug.WriteLine(x.NewItem);
                }
                NotifyCollectionChangedEventArgs eventArgs;

                if (x.NewItem == 0)
                {
                    eventArgs = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
                }
                else
                {
                    var action
                        = (x.NewItem > x.OldItem) ? NotifyCollectionChangedAction.Add
                        : NotifyCollectionChangedAction.Remove;

                    eventArgs = new NotifyCollectionChangedEventArgs(action, new List<Record>());
                }
                this.CollectionChanged?.Invoke(this, eventArgs);
            })
            .AddTo(this.Disposables);


            library.DatabaseUpdated.Throttle(TimeSpan.FromMilliseconds(updatedEventThrottleTime))
                .Subscribe(_ => this.Clear(CacheClearAction.DatabaseUpdated))
                .AddTo(this.Disposables);


            this.SearchInformation = null;

            this.isReset = true;
        }



        /// <summary>
        /// 検索条件を設定、グループ検索を無効化
        /// </summary>
        /// <param name="criteria"></param>
        public void SetSearch(SearchInformation criteria, bool refreshList)
        {
            var clearFlag = false;

            if (this.FeaturedGroup != null)
            {
                this.FeaturedGroup = null;
                clearFlag = true;
            }

            if (criteria != null)
            {
                if (!criteria.CheckSimilarity(this.SearchInformation))
                {
                    clearFlag = true;
                }

                this.SearchInformation = library.Searcher.AddSearchToDictionary(criteria);

                this.SearchInformation.Root.DownEdited();

                //履歴・アルバムのリストを更新
                if (refreshList)
                {
                    this.RefreshSearchList();
                }
            }
            else
            {
                this.SearchInformation = null;
            }

            if (clearFlag)
            {
                this.Clear(CacheClearAction.SearchChanged);
            }

        }

        public void RefreshSearchList()
        {
            if (this.SearchInformation != null)
            {
                this.library.Searcher.RefreshList(this.SearchInformation);
            }
        }

        /// <summary>
        /// グループ検索を有効化
        /// </summary>
        /// <param name="group"></param>
        public void SetGroupSearch(Record group)
        {
            if (group == null || !group.IsGroup)
            {
                throw new ArgumentException("no group");
            }

            if (this.FeaturedGroup == null || this.FeaturedGroup.Id != group.Id)
            {
                this.FeaturedGroup = group;
                this.Clear(CacheClearAction.SearchChanged);
            }
        }
        public async Task SetGroupSearchAsync(string key)
        {
            var group = await this.library.GetRecordAsync(key);
            if (group != null)
            {
                this.SetGroupSearch(group);
            }
        }

        /// <summary>
        /// 検索またはグループのソート条件を変更
        /// </summary>
        /// <param name="sort"></param>
        public void SetSort(IEnumerable<SortSetting> sort)
        {
            if (this.GetActiveSearch()?.SetSort(sort, true) ?? false)
            {
                this.Clear(CacheClearAction.SortChanged);
            }
        }

        /// <summary>
        /// 現在のソート設定を取得
        /// </summary>
        /// <returns></returns>
        public SortSetting[] GetSort()
            => this.GetActiveSearch()?.GetSort()?.ToArray() ?? new SortSetting[0];



        /// <summary>
        /// 検索
        /// </summary>
        /// <returns></returns>
        public async Task SearchAsync(long offset, int takes, bool wait)
        {
            if (!wait && this.asyncLock.IsLocked)
            {
                return;
            }

            await Task.Run(async () =>
            {
                using (var locking = await this.asyncLock.LockAsync().ConfigureAwait(false))
                {
                    if (takes > 0)
                    {
                        var existing = this.CheckCache(offset, takes);

                        if (existing.Start <= existing.End)
                        {
                            offset += existing.Start;
                            takes = existing.End - existing.Start + 1;
                        }
                        else
                        {
                            return;
                        }
                    }
                    if (takes >= 0)
                    {
                        this.SearchMain(offset, takes);
                        //await this.SearchMain(offset, takes).ConfigureAwait(false);
                    }
                    if (this.SearchCompletedSubject.HasObservers)
                    {
                        this.SearchCompletedSubject.OnNext(new SearchCompletedEventArgs()
                        {
                            Start = offset,
                            Takes = takes,
                            Length = 0,
                        });
                    }
                }
                //sw.Stop();
                //System.Diagnostics.Debug.WriteLine($"{sw.ElapsedMilliseconds}[ms],offset={offset},takes={takes}");
            });
        }


        private void SearchMain(long offset, int takes)
        {
            var criteria = this.GetActiveSearch();

            if (criteria == null)
            {
                return;
            }

            var length = this.lengthPrivate.Value;

            if (this.isReset)
            {
                length = criteria.Count(library);
                //length = await criteria.CountAsync(library).ConfigureAwait(false);

                this.lengthPrivate.Value = length;
                this.isReset = false;
            }

            if (takes <= 0)
            {
                return;
            }

            if (offset < 0)
            {
                if (takes < length)
                {
                    offset = length + offset;

                    if (offset < 0)
                    {
                        offset = 0;
                    }
                }
                else
                {
                    offset = 0;
                }
            }

            var result = this.RequestSearch(criteria, offset, takes);
            //var result = await this.RequestSearchAsync(criteria, offset, takes).ConfigureAwait(false);

            if (result.Length < takes && result.Length > 0 && this.lengthPrivate.Value != offset + result.Length)
            {
                this.lengthPrivate.Value = offset + result.Length;
            }


            if (result.Length < takes && takes < length && takes + offset > length)
            {
                if (result.Length > 0)
                {
                    //とりあえず少しだけ取得したことを通知
                    this.SearchCompletedSubject.OnNext(new SearchCompletedEventArgs()
                    {
                        Start = offset,
                        Takes = takes,
                        Length = result.Length,
                    });
                }

                //最後まで検索
                takes -= result.Length;
                if (takes > offset)
                {
                    takes = (int)offset;
                }
                offset = 0;

                lock (this.Cache)
                {
                    while (this.Cache.ContainsKey(offset))
                    {
                        offset++;
                    }
                    while (this.Cache.ContainsKey(offset + takes - 1) && takes > 0)
                    {
                        takes--;
                    }
                }

                if (takes > 0 && offset < length)
                {
                    this.RequestSearch(criteria, offset, takes);
                    //await this.RequestSearchAsync(criteria, offset, takes).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        /// ライブラリに検索を要求、結果をキャッシュ
        /// </summary>
        /// <returns></returns>
        private async Task<Record[]> RequestSearchAsync
            (ISearchCriteria criteria, long offset, long takes)
        {
            if (criteria == null || takes <= 0)
            {
                throw new ArgumentException();
            }

            Record skipUntil = null;
            /*
            if (offset > 0)
            {
                lock (this.Cache)
                {
                    if (!this.Cache.TryGetValue(offset - 1, out skipUntil))
                    {
                        skipUntil = null;
                    }
                }
            }*/

            var result = await criteria.SearchAsync(library, offset, takes, skipUntil).ConfigureAwait(false);
            PostRequestSearch(result, offset, takes);
            return result;
        }
        private Record[] RequestSearch
            (ISearchCriteria criteria, long offset, long takes)
        {
            if (criteria == null || takes <= 0)
            {
                throw new ArgumentException();
            }
            var result = criteria.Search(library, offset, takes, null);
            PostRequestSearch(result, offset, takes);
            return result;
        }
        private void PostRequestSearch(Record[] records, long offset, long takes)
        {
            lock (this.Cache)
            {
                for (int i = 0; i < takes; i++)
                {
                    if (i < records.Length)
                    {
                        this.Cache[offset + i] = records[i];
                    }
                    else
                    {
                        this.Cache[offset + i] = dummyRecord;
                    }
                }
            }

            if (records.Length > 0)
            {
                NotifyCollectionChangedEventArgs eventArgs;

                if (offset + records.Length > this.lengthPrivate.Value)
                {
                    eventArgs = new NotifyCollectionChangedEventArgs
                        (NotifyCollectionChangedAction.Add, records.ToList());
                }
                else
                {
                    eventArgs = new NotifyCollectionChangedEventArgs
                        (NotifyCollectionChangedAction.Replace, records.ToList(), new List<Record>());
                }
                this.CollectionChanged?.Invoke(this, eventArgs);

                if (this.CacheUpdatedSubject.HasObservers)
                {
                    this.CacheUpdatedSubject.OnNext(new CacheUpdatedEventArgs()
                    {
                        Start = offset,
                        Length = records.Length,
                    });
                }
            }
        }

        /// <summary>
        /// レコードを取得、キャッシュに無ければ検索開始
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="takes"></param>
        /// <returns></returns>
        public Record[] GetRecords(long offset, int takes, int direction, bool wait)
        {

            var searchLengthMax = takes;

            //キャッシュを確認
            var result = this.CheckCache(offset, takes);

            //キャッシュに無いレコードがあるなら検索開始
            if (result.Start <= result.End)
            {
                if (this.HasSearch)
                {
                    var startSearch = offset + result.Start;
                    var searchLength = result.End - result.Start + 1;

                    if (searchLength < searchLengthMax && startSearch < this.lengthPrivate.Value)
                    {
                        var margin = searchLengthMax - searchLength;
                        if (direction == 0)
                        {
                            margin = (searchLengthMax - searchLength) / 2;
                            startSearch = startSearch - margin;
                        }
                        else if (direction < 0)
                        {
                            startSearch = startSearch - margin;
                        }
                        searchLength += margin;
                    }

                    this.SearchAsync(startSearch, searchLength, wait).FireAndForget();
                }
                else
                {
                    Debug.WriteLine("no search front");
                }
            }

            return result.Result.Where(x => x != dummyRecord).ToArray();
        }

        /// <summary>
        /// キャッシュ内に既にレコードがあるかチェック
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="takes"></param>
        /// <returns></returns>
        private CacheCheckResult CheckCache(long offset, int takes)
        {
            Record[] result;

            //指定インデックスのアイテムがあるかチェック
            lock (this.Cache)
            {
                result = Enumerable
                    .Range(0, takes)
                    .Select(i => this.GetItemFromIndexWithoutLock(i + offset, true))
                    .ToArray();
            }

            var start = 0;
            var end = result.Length - 1;

            //キャッシュ内にレコードがあるなら開始位置を進める
            while (start < result.Length && result[start] != null)
            {
                start++;
            }

            //キャッシュ内にレコードがあるなら終了位置を減らす
            while (end >= 0 && start <= end && result[end] != null)
            {
                end--;
            }

            return new CacheCheckResult()
            {
                Start = start,
                End = end,
                Result = result,
            };
        }

        /// <summary>
        /// キャッシュチェックの結果
        /// </summary>
        private class CacheCheckResult
        {
            /// <summary>
            /// キャッシュ確認結果のうち、存在しない最初のレコードの位置
            /// </summary>
            public int Start { get; set; }

            /// <summary>
            /// キャッシュ確認結果のうち、存在しない最後のレコードの位置
            /// </summary>
            public int End { get; set; }

            /// <summary>
            /// キャッシュ確認結果
            /// </summary>
            public Record[] Result { get; set; }
        }

        public void Refresh()
        {
            this.Clear(CacheClearAction.Refresh);
        }

        /// <summary>
        /// キャッシュクリア
        /// </summary>
        private void Clear(CacheClearAction action)
        {
            if (action == CacheClearAction.SearchChanged)
            {
                this.lengthPrivate.Value = 0;
            }

            lock (this.Cache)
            {
                this.Cache.Clear();
            }

            if (action != CacheClearAction.SortChanged)
            {
                this.isReset = true;
                this.SearchAsync(0, 0, true).FireAndForget();
            }

            if (!this.CacheClearedSubject.IsDisposed)
            {
                this.CacheClearedSubject.OnNext
                    (new CacheClearedEventArgs() { Action = action, });
            }
        }

        /// <summary>
        /// レコードがキャッシュ内にあればそのキーを返却
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public long FindIndex(Record value)
        {
            var item = this.GetFromCacheById(value.Id);
            return (item.Value != null) ? item.Key : -1;
        }


        public Task<long> FindIndexFromDatabaseAsync(Record value)
            => this.library.FindIndexAsync(this.GetActiveSearch(), value);
        

        public ISearchCriteria GetActiveSearch()
            => (ISearchCriteria)this.FeaturedGroup ?? this.SearchInformation;


        /// <summary>
        /// 渡されたファイルを検索結果として利用
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public Record[] ActivateFiles(string[] files)
        {
            var records = files.Select(x => new Record(x)).ToArray();

            this.SetSearch(null, false);

            lock (this.Cache)
            {
                this.Cache.Clear();

                for (long c = 0; c < records.LongLength; c++)
                {
                    this.Cache[c] = records[c];
                }
            }
            
            this.lengthPrivate.Value = records.LongLength;

            return records;
        }

        /// <summary>
        /// 与えられたファイルと同じフォルダ内のファイルを列挙してライブラリに登録
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Task ActivateFolderAsync(string directory)
            => this.library.ActivateFolderAsync(directory);
        


        /// <summary>
        /// 全て選択
        /// </summary>
        /// <returns></returns>
        public Task<string[]?> GetAllIdsAsync()
        {
            return Task.Run(async () =>
            {
                using (var locking = await this.asyncLock.LockAsync().ConfigureAwait(false))
                {
                    var criteria = this.GetActiveSearch();

                    if (criteria == null)
                    {
                        return null;
                    }

                    return await this.library.GetAllIdsAsync(criteria).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// 範囲選択
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public Task<string[]?> GetRegionIdsAsync(Record start, Record end)
        {
            return Task.Run(async () =>
            {
                using (var locking = await this.asyncLock.LockAsync().ConfigureAwait(false))
                {
                    var criteria = this.GetActiveSearch();

                    if (criteria == null)
                    {
                        return null;
                    }

                    var index1 = this.FindIndex(start);
                    var index2 = this.FindIndex(end);

                    return await this.library.GetIdsAsync(criteria, index1, index2).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// グループ化
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task<string> GroupAsync(string[] items)
        {
            var key = await Task.Run(() => this.library.Grouping.GroupAsync(items)).ConfigureAwait(false);
            this.Clear(CacheClearAction.GroupUpdated);
            return key;
        }

        /// <summary>
        /// グループから退去
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public async Task RemoveFromGroupAsync(string[] items)
        {
            var group = this.FeaturedGroup;
            if (group == null)
            {
                return;
            }

            await Task.Run(() => this.library.Grouping.RemoveFromGroupAsync(group, items)).ConfigureAwait(false);
            this.Clear(CacheClearAction.GroupUpdated);
        }

        /// <summary>
        /// 指定Recordをグループの代表に設定
        /// </summary>
        /// <param name="leader"></param>
        public void SetGroupLeader(Record leader)
        {
            if (this.FeaturedGroup != null)
            {
                this.FeaturedGroup.SetGroupLeader(leader);
            }
        }

        public async Task<bool> DeleteItemsAsync
            (IEnumerable<KeyValuePair<string, Record>> items, bool notDeleteFile)
        {
            var result = await this.library.DeleteItemsAsync(items, notDeleteFile).ConfigureAwait(false);
            if (result)
            {
                this.Refresh();
            }
            return result;
        }

        public async Task<Record> GetItemByIdAsync(string id)
        {
            var item = this.GetFromCacheById(id).Value;

            if (item != null)
            {
                return item;
            }
            return await this.library.GetRecordAsync(id).ConfigureAwait(false);
        }

        private KeyValuePair<long, Record> GetFromCacheById(string id)
        {
            lock (this.Cache)
            {
                return this.Cache
                    .FirstOrDefault(x => x.Value?.Id != null && x.Value.Id.Equals(id));
            }
        }


        /// <summary>
        /// インデックスからレコードを取得
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Record GetItemFromIndexWithoutLock(long index, bool acceptDummy = false)
        {
            if (index < 0)
            {
                index = index + this.lengthPrivate.Value;
            }

            if (this.Cache.TryGetValue(index, out Record value) && (acceptDummy || value != dummyRecord))
            {
                return value;
            }

            return null;
        }

        private Record GetItemFromIndex(long index, bool acceptDummy = false)
        {
            lock (this.Cache)
            {
                return this.GetItemFromIndexWithoutLock(index, acceptDummy);
            }
        }

        public Record this[long index] => this.GetItemFromIndex(index, false);


        int IReadOnlyCollection<Record>.Count
            => (this.lengthPrivate.Value < int.MaxValue) ? (int)this.lengthPrivate.Value : int.MaxValue;

        Record IReadOnlyList<Record>.this[int index]
            => this.GetItemFromIndex(index);

        IEnumerator<Record> IEnumerable<Record>.GetEnumerator()
            => Enumerable.Range(0, ((IReadOnlyCollection<Record>)this).Count)
                .Select(x => this.GetItemFromIndex(x))
                .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<Record>)this).GetEnumerator();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

    }

    public interface ISearchResult : IReadOnlyList<Record>, INotifyCollectionChanged
    {

    }

    public class CacheUpdatedEventArgs
    {
        public long Start { get; set; }
        public int Length { get; set; }
    }

    public class CacheClearedEventArgs
    {
        public CacheClearAction Action { get; set; }
    }

    public enum CacheClearAction
    {
        SearchChanged,
        DatabaseUpdated,
        Refresh,
        SortChanged,
        FileActivated,
        GroupUpdated,
    }


    public class SearchCompletedEventArgs
    {
        public long Start { get; set; }
        public int Takes { get; set; }
        public int Length { get; set; }
    }

}
