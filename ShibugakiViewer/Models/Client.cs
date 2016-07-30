using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Database.Search;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Viewer;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models.ImageViewer;

namespace ShibugakiViewer.Models
{
    public class Client : NotificationBase
    {

        private const double resizedImageLoadDelayMillisec = 200.0;
        private const double originalImageLoadDelayMillisec = 1200.0;
        private const double animationStartDelayMillisec = 500.0;

        /// <summary>
        /// ビューアがデータベースにアクセスするときに余分に読み込む数
        /// </summary>
        private const int viewerDatabaseLoadLength = 6;

        private long CatalogIndexInner
        {
            get
            {
                return (this.History.Current.CatalogIndex < this.front.Length.Value)
                    ? this.History.Current.CatalogIndex : 0;
            }
            set
            {
                if (this.History.Current.CatalogIndex != value && value < this.front.Length.Value)
                {
                    this.History.Current.CatalogIndex = value;
                    RaisePropertyChanged(nameof(CatalogIndexInner));
                }
            }
        }


        private long ViewerIndexInner
        {
            get
            {
                return (this.History.Current.ViewerIndex < this.front.Length.Value)
                    ? this.History.Current.ViewerIndex : 0;
            }
            set
            {
                if (value < this.front.Length.Value)
                {
                    this.History.Current.ViewerIndex = value;
                    RaisePropertyChanged(nameof(ViewerIndexInner));
                }
            }
        }
        
        public ReactiveProperty<long> CatalogIndex { get; }
        public ReactiveProperty<long> ViewerIndex { get; }


        public double ViewWidth { get; set; }
        public double ViewHeight { get; set; }


        public IObservable<SearchCompletedEventArgs> SearchCompleted => this.front.SearchCompleted;
        public IObservable<Unit> StateChanged { get; }

        public ReactiveProperty<int> ColumnLength { get; }
        public ReactiveProperty<int> RowLength { get; }
        public ReadOnlyReactiveProperty<int> PageSize { get; }

        public ReadOnlyReactiveProperty<long> Length { get; }
        public ReadOnlyReactiveProperty<Record> SelectedRecord { get; }

        private ReactiveProperty<Record> ViewerDisplayingInner { get; }
        public ReadOnlyReactiveProperty<Record> ViewerDisplaying { get; }

        public Record FeaturedGroup => this.front.FeaturedGroup;
        public ReactiveProperty<bool> IsGroupMode => this.front.IsGroupMode;
        public IObservable<Record> FeaturedGroupChanged => this.front.FeaturedGroupChanged;
        public IObservable<CacheUpdatedEventArgs> CacheUpdated => this.front.CacheUpdated;

        public ReadOnlyReactiveProperty<PageType> SelectedPage { get; }
        private Subject<PageType> PageChangeRequestSubject { get; }
        //public IObservable<PageType> PageChangeRequest => this.PageChangeRequestSubject.AsObservable();

        public SelectionManager SelectedItems { get; }
        public ReadOnlyReactiveProperty<int> SelectedItemsCount { get; }

        private Subject<long> ChangeToViewerSubject { get; }
        private History<ViewState> History { get; }

        private Record lastGroup = null;

        //private Action onHistoryChangedAction = null;

        public ReadOnlyReactiveProperty<int> BackHistoryCount => this.History.BackHistoryCount;
        public ReadOnlyReactiveProperty<int> ForwardHistoryCount => this.History.ForwardHistoryCount;

        private readonly ApplicationCore core;
        private readonly LibraryFront front;
        public ISearchResult SearchResult => this.front;

        private bool viewerImageChangeGate;



        public Client(LibraryFront front, ApplicationCore core)
        {
            this.core = core;
            this.front = front;
            front.AddTo(this.Disposables);

            this.SelectedItems = new SelectionManager(core.Library).AddTo(this.Disposables);

            this.SelectedItemsCount = this.SelectedItems
                .ObserveProperty(x => x.Count)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            //var initialSearch = core.Library.Searcher.GetLatest()
            //    ?? new SearchInformation(new ComplexSearch(false));


            this.History = new History<ViewState>(new ViewState()).AddTo(this.Disposables);

            this.PageChangeRequestSubject = new Subject<PageType>().AddTo(this.Disposables);
            this.SelectedPage = this.PageChangeRequestSubject
                .ToReadOnlyReactiveProperty(PageType.Search)
                .AddTo(this.Disposables);
            //this.PageChangeRequestSubject = new Subject<PageType>().AddTo(this.Disposables);



            this.ViewerDisplayingInner = new ReactiveProperty<Record>().AddTo(this.Disposables);
            this.ViewerDisplaying = this.ViewerDisplayingInner
                .ToReadOnlyReactiveProperty().AddTo(this.Disposables);



            this.SelectedPage.Subscribe(x =>
            {
                if (x != PageType.None)
                {
                    this.History.Current.Type = x;
                }
                if (x == PageType.Viewer)
                {
                    this.SetRecord(false);
                }
            })
            .AddTo(this.Disposables);

            this.Length = this.front.Length
                .ObserveOnUIDispatcher()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);



            this.StateChanged = this.front.Length.Select(_ => Unit.Default)
                .Merge(this.front.CacheCleared.Select(_ => Unit.Default))
                .Publish().RefCount();

            this.front.CacheCleared.Subscribe(x =>
            {
                if (x.Action == CacheClearAction.SearchChanged || x.Action == CacheClearAction.GroupUpdated)
                {
                    this.SelectedItems.Clear();
                }

                if (x.Action == CacheClearAction.SortChanged)
                {
                    this.History.Current.ViewerIndex = 0;
                    this.History.Current.CatalogIndex = 0;
                }

                if (x.Action != CacheClearAction.SortChanged)
                {
                    this.SelectedItems.ClearCache();
                }
            })
            .AddTo(this.Disposables);

            this.ColumnLength = new ReactiveProperty<int>(-1).AddTo(this.Disposables);
            this.RowLength = new ReactiveProperty<int>(-1).AddTo(this.Disposables);

            this.CatalogIndex = this.ToReactivePropertyAsSynchronized(x => x.CatalogIndexInner)
                .AddTo(this.Disposables);
            this.ViewerIndex = this.ToReactivePropertyAsSynchronized(x => x.ViewerIndexInner)
                .AddTo(this.Disposables);




            this.SearchCompleted
                .Where(x => (this.SelectedPage.Value == PageType.Viewer && x.Takes > 0))
                .ObserveOnUIDispatcher()
                .Subscribe(_ => this.SetRecord(true))
                .AddTo(this.Disposables);


            var viewerDisplaying = this.ViewerDisplayingInner
                .CombineLatest(this.SelectedPage, (r, p) => p == PageType.Viewer ? r : null)
                .Where(x => x != null);
            //.Publish().RefCount();

            var catalogDisplaying = this.SelectedItems.SelectedItemChanged
                .CombineLatest(this.front.FeaturedGroupChanged, (s, g) => s ?? g)
                .Where(_ => this.SelectedPage.Value == PageType.Catalog)
                .CombineLatest(this.SelectedPage.Where(x => x == PageType.Catalog), (x, p) => x);

            this.SelectedRecord = viewerDisplaying
                .Merge(catalogDisplaying)
                //.Select(x => x ?? front.FeaturedGroup)
                //.Merge(front.CacheCleared.Select(_ => (Record)null))
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);


            this.PageSize = this.ColumnLength.Where(x => x > 0)
                .CombineLatest(this.RowLength.Where(x => x > 0), (c, r) => c * r)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.PageSize
                .Subscribe(x => core.ImageBuffer.ThumbnailRequestCapacity = Math.Max(x * 2, 32))
                .AddTo(this.Disposables);

            //long prevCatalogIndex = 0;

            //TODO 移動方向アイテムを多めに先読み
            //var databaseUpdated = front.SearchCompleted
            //    .Where(x => this.SelectedPage.Value == PageType.Catalog);
            this.CatalogIndex
                .Restrict(TimeSpan.FromMilliseconds(100))
                .Merge(this.front.CacheCleared
                    .Where(x => x.Action != CacheClearAction.SearchChanged
                        && this.SelectedPage.Value == PageType.Catalog)
                    .Select(_ => this.CatalogIndex.Value))
                .Pairwise(0)
                .CombineLatest(this.PageSize, (Index, ViewSize) => new { ViewSize, Index })
                .Subscribe(x =>
                {
                    if (x.ViewSize <= 0)
                    {
                        return;
                    }

                    var offset = x.Index.NewItem - (x.ViewSize / 2);
                    var takes = x.ViewSize * 2;

                    if (offset <= 0)
                    {
                        offset = 0;
                    }

                    var direction = (x.Index.NewItem == 0) ? 1 : (int)(x.Index.NewItem - x.Index.OldItem);
                    //prevCatalogIndex = x.Index;

                    //Debug.WriteLine($"load-{offset}-{offset+takes}");
                    this.front.GetRecords(offset, takes, direction, true);

                    //if (offset <= 0)
                    //{
                    //    this.front.GetRecords(-2, 2, direction, true);
                    //}
                })
                .AddTo(this.Disposables);

            //this.CatalogIndex.Subscribe(x => Debug.WriteLine($"index-{x}")).AddTo(this.Disposables);

            //TODO 余裕があれば離れたところも読み込むようにキューイング
            //TODO 移動方向アイテムを多めに先読み

            //Viewerインデックス変更時のデータベース問い合わせ
            var viewerImage = this.ViewerIndex
                .Skip(1)
                .Where(_ => this.viewerImageChangeGate && this.SelectedPage.Value == PageType.Viewer)
                .Pairwise(0)
                .Merge(this.SelectedPage.Where(x => x == PageType.Viewer)
                    .Select(_ => new OldNewPair<long>(this.ViewerIndex.Value, this.ViewerIndex.Value)))
                .Publish()
                .RefCount();

            viewerImage
                .Subscribe(x =>
                {
                    this.SearchForViewer(x, false);
                    this.SetRecord(false);
                })
                .AddTo(this.Disposables);

            viewerImage
                //.Restrict(TimeSpan.FromMilliseconds(200))
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(x =>
                {
                    this.SearchForViewer(x, true);
                    //this.front.GetRecords(x - 1, 3, true);
                    //this.SetRecord(false);
                })
                .AddTo(this.Disposables);

            //キャッシュクリア時のViewer再読み込み

            var viewerImageLast = new BehaviorSubject<OldNewPair<long>>(new OldNewPair<long>(0, 0))
                .AddTo(this.Disposables);
            viewerImage.Subscribe(viewerImageLast).AddTo(this.Disposables);

            var viewerCacheClearedTrigger = this.front.CacheCleared
                .Where(x => x.Action != CacheClearAction.SearchChanged
                    && this.SelectedPage.Value == PageType.Viewer);
            //.Select(_ => Unit.Default);
            //.Merge(this.SelectedPage.Where(x => x == PageType.Viewer).Select(_ => Unit.Default));


            viewerCacheClearedTrigger
                .Select(_ => viewerImageLast.Value)
                //viewerImage
                //.Sample(viewerCacheClearedTrigger)
                .Subscribe(x =>
                {
                    this.SearchForViewer(x, true);
                })
                .AddTo(this.Disposables);

            front.DatabaseAddedOrRemoved
                //.ObserveOnUIDispatcher()
                .Subscribe(async _ =>
                {
                    //return;
                    var criteria = front.GetActiveSearch();
                    var record = this.ViewerDisplayingInner.Value;
                    if (criteria != null && record != null)
                    {
                        var index = await core.Library.FindIndexAsync(criteria, record);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            this.viewerImageChangeGate = false;
                            this.ViewerIndexInner = index;
                            this.viewerImageChangeGate = true;
                        });
                    }
                })
                .AddTo(this.Disposables);


            //画像読み込みリクエスト

            this.ChangeToViewerSubject = new Subject<long>().AddTo(this.Disposables);


            var cacheUpdated = front.CacheUpdated
                .Where(x => this.ViewerIndex.Value >= x.Start && this.ViewerIndex.Value < x.Start + x.Length)
                .Select(_ => this.ViewerIndex.Value);


            var viewerIndexChanged = ChangeToViewerSubject
                .Merge(this.ViewerIndex.Where(_ => this.SelectedPage.Value == PageType.Viewer))
                .Merge(cacheUpdated)
                .Publish()
                .RefCount();

            viewerIndexChanged
                .Subscribe(x => LoadImagesMain(x, ImageQuality.LowQuality,
                    ListOrderFlags.Current | ListOrderFlags.Next | ListOrderFlags.Previous))
                .AddTo(this.Disposables);

            viewerIndexChanged
                .Throttle(TimeSpan.FromMilliseconds(resizedImageLoadDelayMillisec))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => LoadImagesMain(x, ImageQuality.Resized,
                    ListOrderFlags.Current | ListOrderFlags.Next | ListOrderFlags.Previous))
                .AddTo(this.Disposables);

            viewerIndexChanged
                .Throttle(TimeSpan.FromMilliseconds(originalImageLoadDelayMillisec))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => LoadImagesMain(x, ImageQuality.OriginalSize,
                    ListOrderFlags.Current))
                .AddTo(this.Disposables);//前後の画像は最高画質ロードしない



            //検索条件が変化したらライブラリに設定して最初の方を読み込む
            this.History.StateChanged
                .Where(x => x != null)
                .Subscribe(state =>
                {
                    var refreshList = false;

                    if (state.Type != PageType.Search)
                    {
                        if (state.GroupKey != null)
                        {
                            if (this.lastGroup != null && this.lastGroup.Id.Equals(state.GroupKey))
                            {
                                this.front.SetGroupSearch(this.lastGroup);
                                this.lastGroup = null;
                            }
                            else
                            {
                                this.front.SetGroupSearchAsync(state.GroupKey).FireAndForget();
                            }
                        }
                        else
                        {
                            this.front.SetSearch(state.Search, false);
                            refreshList = true;
                        }
                    }

                    if (state.Type == PageType.Viewer)
                    {
                        this.ChangeToViewerSubject.OnNext(state.ViewerIndex);
                        this.ViewerIndex.Value = state.ViewerIndex;
                    }

                    this.CatalogIndex.Value = state.CatalogIndex;

                    if (state.GroupKey != null || state.Search != null)
                    {
                        core.ImageBuffer.ClearThumbNailRequests();
                        core.ImageBuffer.ClearRequests();

                        var direction = (state.CatalogIndex == 0) ? 1 : 0;
                        this.front.GetRecords(state.CatalogIndex, this.PageSize.Value * 2, direction, true);
                    }

                    if (state.Type != PageType.None)
                    {
                        this.PageChangeRequestSubject.OnNext(state.Type);
                    }
                    if (refreshList)
                    {
                        this.front.RefreshSearchList();
                    }
                })
                .AddTo(this.Disposables);

            //破棄時に設定を保存
            Disposable.Create(() => core.Save()).AddTo(this.Disposables);

            this.History.Clear();
            this.SetNewSearch(null);
            this.History.Current.Type = PageType.Search;

            this.viewerImageChangeGate = true;
        }

        /// <summary>
        /// ビューア用データベースアクセス
        /// </summary>
        /// <param name="index"></param>
        private void SearchForViewer(OldNewPair<long> index, bool wait)
        {
            var direction = (index.NewItem == 0) ? 0
                : (index.OldItem == 0 && index.NewItem == this.front.Length.Value - 1) ? -1
                : (int)(index.NewItem - index.OldItem);

            this.front.GetRecords(index.NewItem - viewerDatabaseLoadLength / 2,
                viewerDatabaseLoadLength, direction, wait);
        }

        /// <summary>
        /// 新しい検索を開始
        /// </summary>
        /// <param name="search"></param>
        public void SetNewSearch(SearchInformation search)
        {
            if (this.History.Current != null
                && this.History.Current.GroupKey == null
                && this.History.Current.Search != null
                && this.History.Current.Search.SettingEquals(search))
            {
                return;
            }

            this.History.MoveNew(new ViewState()
            {
                Search = search,
                GroupKey = null,
                CatalogIndex = 0,
                ViewerIndex = 0,
                Type = PageType.None,
            });
        }

        public void StartNewSearch(FileProperty property, object reference, CompareMode mode)
        {

            var search = new SearchInformation(new ComplexSearch(false));
            search.Root.Add(new UnitSearch()
            {
                Property = property,
                Reference = reference,
                Mode = mode,
            });

            this.SetNewSearch(search);
            this.ChangePage(PageType.Catalog);
        }

        /// <summary>
        /// グループの内容を表示
        /// </summary>
        /// <param name="group"></param>
        public void SetNewGroupSearch(Record group)
        {
            this.History.MoveNew(new ViewState()
            {
                Search = null,
                GroupKey = group.Id,
                CatalogIndex = 0,
                ViewerIndex = 0,
                Type = PageType.None,
            });
            this.lastGroup = group;
        }

        /// <summary>
        /// ソート条件を変更
        /// </summary>
        /// <param name="sort"></param>
        public void SetSort(IEnumerable<SortSetting> sort)
        {
            this.front.SetSort(sort);
            //this.History.Current.ViewerIndex = 0;
            //this.History.Current.CatalogIndex = 0;
        }

        /// <summary>
        /// 現在のソート設定を取得
        /// </summary>
        /// <returns></returns>
        public SortSetting[] GetSort() => this.front.GetSort();

        ///// <summary>
        ///// 現在の条件での検索結果のうち指定した範囲内のレコードを取得
        ///// </summary>
        ///// <param name="offset"></param>
        ///// <param name="takes"></param>
        ///// <returns></returns>
        //public Record[] GetRecords(long offset, int takes)
        //    => this.front.GetRecords(offset, takes, false);

        /// <summary>
        /// 指定レコードの現在の検索条件でのインデックスをデータベースに問い合わせ
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Task<long> FindIndexFromDatabaseAsync(Record value)
            => this.front.FindIndexFromDatabaseAsync(value);

        /// <summary>
        /// 検索条件を満たすアイテムを全て選択
        /// </summary>
        /// <returns></returns>
        public async Task SelectAllAsync()
        {
            var ids = await this.front.GetAllIdsAsync();
            this.SelectedItems.AddRange(ids);
        }

        /// <summary>
        /// 範囲選択
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task SelectRegionAsync(Record item)
        {
            if (item == null)
            {
                return;
            }
            if (this.SelectedItems.LastSelectedItem == null)
            {
                this.SelectedItems.AddOrReplace(item);
                return;
            }
            if (item.Id.Equals(this.SelectedItems.LastSelectedItem))
            {
                this.SelectedItems.Toggle(item);
                return;
            }

            var ids = await this.front.GetRegionIdsAsync(this.SelectedItems.LastSelectedItem, item);
            this.SelectedItems.AddRange(ids);
            this.SelectedItems.AddOrReplace(item);
        }

        /// <summary>
        /// グループ化
        /// </summary>
        public void Grouping()
        {
            var items = this.SelectedItems.GetAll().Select(x => x.Key).ToArray();
            this.front.GroupAsync(items).FireAndForget();
        }

        /// <summary>
        /// グループから退去
        /// </summary>
        public void RemoveFromGroup()
        {
            var items = this.SelectedItems.GetAll().Select(x => x.Key).ToArray();
            this.front.RemoveFromGroupAsync(items).FireAndForget();
        }

        /// <summary>
        /// 選択されたレコードをグループの代表に設定
        /// </summary>
        /// <param name="leader"></param>
        public void SetGroupLeader()
        {
            var leader = this.SelectedItems
                .GetAll()
                .Select(x => x.Value)
                .Where(x => x != null)
                .FirstOrDefault();

            if (leader != null)
            {
                this.front.SetGroupLeader(leader);
            }
        }



        /// <summary>
        /// 指定レコードがファイルなら画像を表示、グループならグループ内容を問い合わせ
        /// </summary>
        /// <param name="record"></param>
        /// <param name="index"></param>
        public void MoveToViewerOrGroupDetail(Record record)
        {
            if (record == null)
            {
                return;
            }

            if (record.IsGroup)
            {
                this.SetNewGroupSearch(record);
                core.ImageBuffer.ClearThumbNailRequests();
                this.ChangePage(PageType.Catalog);

                return;
            }

            var key = this.front.FindIndex(record);

            if (key >= 0)
            {
                //var oldIndex = this.ViewerIndex.Value;

                //this.ChangeToViewerSubject.OnNext(key);

                this.ViewerIndex.Value = key;
                core.ImageBuffer.ClearThumbNailRequests();
                this.ChangePage(PageType.Viewer);
            }
        }

        /// <summary>
        /// グループ内容をViewerで表示
        /// </summary>
        /// <param name="record"></param>
        /// <param name="index"></param>
        public void DisplayGroup(int index)
        {
            var record = this.ViewerDisplaying.Value;

            if (record == null || !record.IsGroup)
            {
                return;
            }

            this.SetNewGroupSearch(record);
            this.ViewerIndex.Value = index;
            //core.ImageBuffer.ClearThumbNailRequests();
            this.ChangePage(PageType.Viewer);
        }




        /// <summary>
        /// 選択された画像および隣接する画像をロード
        /// </summary>
        /// <param name="information"></param>
        /// <param name="quality"></param>
        /// <param name="order"></param>
        private void LoadImagesMain(long index, ImageQuality quality, ListOrderFlags order)
        {
            var length = this.front.Length.Value;
            if (length <= 0)
            {
                return;
            }

            //Debug.WriteLine($"Load Image At({index}), {quality}, {order}");

            var option = new ImageLoadingOptions()
            {
                FrameHeight = this.ViewHeight,
                FrameWidth = this.ViewWidth,
                Quality = quality,
                CmsEnable = this.core.IsCmsEnabled,
            };


            if (order.HasFlag(ListOrderFlags.Current))
            {
                //現在選択されている画像をロード
                var record = this.front[index];

                if (record != null)
                {
                    this.core.ImageBuffer.RequestLoading
                        (record, option, Observer.Create<int>(_ => { }),
                        true, default(CancellationToken));
                }

            }

            if (order.HasFlag(ListOrderFlags.Next) && length > 1)
            {
                //次の画像をロード

                var nextIndex = (index < length - 1) ? index + 1 : 0;

                var record = this.front[nextIndex];

                if (record != null)
                {
                    this.core.ImageBuffer.RequestLoading
                        (record, option, Observer.Create<int>(_ => { }),
                        false, default(CancellationToken));


                    //Debug.WriteLine($"Load Next Image At({nextIndex}), {quality}, {order}");
                }
            }

            if (order.HasFlag(ListOrderFlags.Previous) && length > 1)
            {
                //前の画像をロード

                var prevIndex = (index > 0) ? index - 1 : length - 1;

                var record = this.front[prevIndex];

                if (record != null)
                {
                    this.core.ImageBuffer.RequestLoading
                        (record, option, Observer.Create<int>(_ => { }),
                        false, default(CancellationToken));


                    //Debug.WriteLine($"Load Prev Image At({prevIndex}), {quality}, {order}");
                }

            }

        }


        /// <summary>
        /// ビューア用画像を設定
        /// </summary>
        /// <param name="client"></param>
        private void SetRecord(bool ignoreIfDifferent)
        {
            var index = this.ViewerIndex.Value;

            var result = this.front.GetRecords(index, 1, 0, false);// this.GetRecords(index, 1);

            if (result.Length > 0)
            {
                //this.ViewerDisplayingInner.Value = result[0];

                var current = this.ViewerDisplayingInner.Value;

                if (!ignoreIfDifferent
                    || current == null
                    || current == Record.Empty)
                {
                    this.ViewerDisplayingInner.Value = result[0];
                }
                else
                {
                    if (current?.Id == result[0]?.Id)
                    {
                        if (current != result[0])
                        {
                            Debug.WriteLine($"database updated but ignored");
                        }
                    }
                    else
                    {
                        Debug.WriteLine
                            ($"different file {current?.Id ?? "null"}, {result[0]?.Id ?? "null"}");
                    }
                }
            }
            else
            {
                this.ViewerDisplayingInner.Value = Record.Empty;
            }

        }

        public void MoveToPage(PageType type, bool force = false)
        {
            if (this.SelectedPage.Value == type && !force)
            {
                return;
            }
            switch (type)
            {
                case PageType.None:
                    break;
                case PageType.Search:
                case PageType.Catalog:
                case PageType.Viewer:
                    this.ChangePage(type);
                    break;
                case PageType.Slideshow:
                    break;
                default:
                    break;
            }
        }

        private void ChangePage(PageType type)
        {
            if (this.History.BackHistoryCount.Value > 0
                && (this.History.Current.Type == PageType.None
                || this.History.Current.Type == PageType.Search))
            {
                this.History.Current.Type = type;
            }
            else if (this.History.Current.Type != type)
            {
                var state = this.History.Current.Clone();
                state.Type = type;
                this.History.MoveNew(state);
            }

            this.PageChangeRequestSubject.OnNext(type);
        }

        public void Back() => this.History.MoveBack();
        public void Forward() => this.History.MoveForward();


        /// <summary>
        /// アプリ外から渡されたファイルリストを使用して起動
        /// </summary>
        /// <param name="list"></param>
        public void ActivateFiles(IEnumerable<string> list)
        {
            var files = list?.Where(x => x.HasText()).ToArray();

            if (files.IsNullOrEmpty())
            {
                return;
            }

            var records = this.front.ActivateFiles(files);

            this.History.MoveNew(new ViewState()
            {
                Search = null,
                GroupKey = null,
                CatalogIndex = 0,
                ViewerIndex = 0,
                Type = PageType.Catalog,
            });


            this.ChangePage(PageType.Viewer);

            if (records.Length == 1)
            {
                //return;
                Task.Run(async () =>
                {
                    //await Task.Delay(3000);
                    var record = records[0];
                    await this.front.ActivateFolderAsync(record.FullPath);

                    var search = new SearchInformation(new ComplexSearch(false));
                    search.Root.Add(new UnitSearch()
                    {
                        Property = FileProperty.DirectoryPathStartsWith,
                        Reference = record.Directory,
                        Mode = CompareMode.Equal,
                    });

                    var index = await core.Library.FindIndexAsync(search, record);

                    //this.History.InsertToBack(new ViewState()
                    //{
                    //    Search = search,
                    //    GroupKey = null,
                    //    CatalogIndex = 0,
                    //    ViewerIndex = 0,
                    //    Type = PageType.Catalog,
                    //});

                    this.front.Length
                        .Where(x => x > index)
                        .Take(1)
                        .Subscribe(x =>
                        {
                            this.viewerImageChangeGate = false;
                            this.ViewerIndexInner = index;
                            this.viewerImageChangeGate = true;
                        });

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        this.front.SetSearch(search, true);
                        //this.viewerImageChangeGate = false;
                        //this.ViewerIndexInner = index;
                        //this.viewerImageChangeGate = true;
                    });

                    //this.SetNewSearch(search);
                })
                .FireAndForget();
            }
        }

        public void Refresh()
        {
            this.core.ImageBuffer.ClearAll();
            this.front.Refresh();
        }


        [Flags]
        private enum ListOrderFlags
        {
            None = 0x00,
            Current = 0x01,
            Previous = 0x02,
            Next = 0x04,
        };
    }

}
