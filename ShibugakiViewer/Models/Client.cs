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

        private Subject<long> PrepareNextSubject { get; }

        public Record FeaturedGroup => this.front.FeaturedGroup;
        public ReactiveProperty<bool> IsGroupMode => this.front.IsGroupMode;
        public IObservable<Record> FeaturedGroupChanged => this.front.FeaturedGroupChanged;
        public IObservable<CacheUpdatedEventArgs> CacheUpdated => this.front.CacheUpdated;

        private BehaviorSubject<bool> IsStateChangingSubject { get; }
        public IObservable<bool> IsStateChanging => this.IsStateChangingSubject.AsObservable();


        private Subject<bool> IsCatalogRenderingEnabledSubject { get; }
        public IObservable<bool> IsCatalogRenderingEnabled => this.IsCatalogRenderingEnabledSubject.AsObservable();


        private Subject<long> CatalogScrollIndexSubject { get; }
        public IObservable<long> CatalogScrollIndex => this.CatalogScrollIndexSubject.AsObservable();

        public IObservable<CacheClearedEventArgs> ViewerCacheClearedTrigger { get; }


        public ReadOnlyReactiveProperty<PageType> SelectedPage { get; }
        private Subject<PageType> PageChangeRequestSubject { get; }

        public SelectionManager SelectedItems { get; }
        public ReadOnlyReactiveProperty<int> SelectedItemsCount { get; }

        private Subject<long> ChangeToViewerSubject { get; }
        private History<ViewState> History { get; }

        private OldNewPair<long> lastViewerImageIndex;
        private Record lastGroup = null;

        public ReadOnlyReactiveProperty<int> BackHistoryCount => this.History.BackHistoryCount;
        public ReadOnlyReactiveProperty<int> ForwardHistoryCount => this.History.ForwardHistoryCount;

        private readonly ApplicationCore core;
        private readonly LibraryFront front;
        public ISearchResult SearchResult => this.front;

        private bool viewerImageChangeGate;
        private bool isCatalogLoaded = false;



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


            this.History = new History<ViewState>(new ViewState()).AddTo(this.Disposables);

            this.PageChangeRequestSubject = new Subject<PageType>().AddTo(this.Disposables);
            this.SelectedPage = this.PageChangeRequestSubject
                .ToReadOnlyReactiveProperty(PageType.Search)
                .AddTo(this.Disposables);

            this.IsStateChangingSubject = new BehaviorSubject<bool>(false).AddTo(this.Disposables);

            this.IsCatalogRenderingEnabledSubject = new Subject<bool>().AddTo(this.Disposables);
            this.CatalogScrollIndexSubject = new Subject<long>().AddTo(this.Disposables);

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
                    //this.History.Current.ViewerIndex = 0;
                    //this.History.Current.CatalogIndex = 0;
                }
                else
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

            //Viewerで表示中のRecord
            var viewerDisplaying = this.ViewerDisplayingInner
                .CombineLatest(this.SelectedPage, (r, p) => p == PageType.Viewer ? r : null)
                .Where(x => x != null);

            //Catalogで選択数が1になったとき、Recordの実体がわからない
            var selectedChangedTrigger = this.SelectedItemsCount
                .Pairwise()
                .Where(x => x.OldItem > 1 && x.NewItem == 1 && this.SelectedRecord.Value == null
                    && this.SelectedPage.Value == PageType.Catalog)
                .Select(_ => this.SelectedItems.GetAll().FirstOrDefault())
                .Where(x => x.Key != null)
                .Publish().RefCount();

            //DBに問い合わせ
            var selectedChanged = selectedChangedTrigger
                .Where(x => x.Value == null)
                .SelectMany(x => core.Library.GetRecordAsync(x.Key))
                .Merge(selectedChangedTrigger.Where(x => x.Value != null).Select(x => x.Value));


            //Catalogで選択中のRecord
            var catalogDisplaying = this.SelectedItems.SelectedItemChanged
                .CombineLatest(this.front.FeaturedGroupChanged, (s, g) => s ?? g)
                .Merge(selectedChanged)
                .Where(_ => this.SelectedPage.Value == PageType.Catalog)
                .CombineLatest(this.SelectedPage.Where(x => x == PageType.Catalog), (x, p) => x);

            //情報を表示すべきRecord
            this.SelectedRecord = viewerDisplaying
                .Merge(catalogDisplaying)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            //Exif
            this.SelectedRecord
                .CombineLatest(this.core.Library.ExifManager.HasVisibleItem, (r, h) => h ? r : null)
                .Throttle(TimeSpan.FromMilliseconds(300))
                .Where(x => x != null && !x.IsGroup && x.Exif == null)
                .Select(x => new { R = x, E = this.core.Library.ExifManager.LoadExif(x.FullPath) })
                .ObserveOnUIDispatcher()
                .Subscribe(x => x.R.Exif = x.E)
                .AddTo(this.Disposables);


            this.PageSize = this.ColumnLength.Where(x => x > 0)
                .CombineLatest(this.RowLength.Where(x => x > 0), (c, r) => c * r)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.PageSize
                .Subscribe(x => core.ImageBuffer.ThumbnailRequestCapacity = Math.Max(x * 2, 32))
                .AddTo(this.Disposables);

            //データベース更新
            var databaseUpdatedForCatalog = this.front.CacheCleared
                .Where(x => x.Action == CacheClearAction.DatabaseUpdated)
                .Buffer(this.SelectedPage.Where(x => x == PageType.Catalog))
                .Where(x => x.Count > 0)
                .Select(_ => this.CatalogIndex.Value);

            //キャッシュクリア
            var catalogReset = this.front.CacheCleared
                .Where(x => this.SelectedPage.Value == PageType.Catalog)
                .Select(_ => this.CatalogIndex.Value);


            //Catalog用DB問い合わせ
            this.CatalogIndex
                .Restrict(TimeSpan.FromMilliseconds(100))
                .Merge(catalogReset)
                .Merge(databaseUpdatedForCatalog)
                .Pairwise(0)
                .CombineLatest(this.PageSize, (Index, ViewSize) => new { ViewSize, Index })
                .Subscribe(x =>
                {
                    if (x.ViewSize > 0 && !this.IsStateChangingSubject.Value)
                    {
                        this.SearchForCatalog(x.Index.NewItem, x.ViewSize,
                            (int)(x.Index.NewItem - x.Index.OldItem));
                    }
                })
                .AddTo(this.Disposables);


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
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(x => this.SearchForViewer(x, true))
                .AddTo(this.Disposables);

            //キャッシュクリア時のViewer再読み込み

            this.lastViewerImageIndex = new OldNewPair<long>(0, 0);
            viewerImage.Subscribe(x => this.lastViewerImageIndex = x).AddTo(this.Disposables);

            this.ViewerCacheClearedTrigger = this.front.CacheCleared
                .Where(x => x.Action != CacheClearAction.SearchChanged
                    && this.SelectedPage.Value == PageType.Viewer)
                .Publish().RefCount();

            this.ViewerCacheClearedTrigger
                .Select(_ => this.lastViewerImageIndex)
                .Subscribe(x =>
                {
                    this.SearchForViewer(x, true);
                })
                .AddTo(this.Disposables);

            front.DatabaseAddedOrRemoved
                .Subscribe(async _ =>
                {
                    var criteria = front.GetActiveSearch();
                    var record = this.ViewerDisplayingInner.Value;
                    if (criteria != null && record != null)
                    {
                        var index = await core.Library.FindIndexAsync(criteria, record);

                        await Application.Current.Dispatcher.InvokeAsync(() =>
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

            //表示中の画像のみ最高画質ロード

            var viewerIndexChangedFiltered = core.LoadingOriginalQualityQuick
                ? viewerIndexChanged.Restrict(TimeSpan.FromMilliseconds(originalImageLoadDelayMillisec))
                : viewerIndexChanged.Throttle(TimeSpan.FromMilliseconds(originalImageLoadDelayMillisec));

            viewerIndexChangedFiltered
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x => LoadImagesMain(x, ImageQuality.OriginalSize,
                    ListOrderFlags.Current))
                .AddTo(this.Disposables);


            this.PrepareNextSubject = new Subject<long>().AddTo(this.Disposables);
            this.PrepareNextSubject
                .Throttle(TimeSpan.FromMilliseconds(resizedImageLoadDelayMillisec))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(async x =>
                {
                    await this.front.SearchAsync(x, 1, true);
                    LoadImagesMain(x, ImageQuality.Resized, ListOrderFlags.Current);
                })
                .AddTo(this.Disposables);

            //検索条件が変化したらライブラリに設定して最初の方を読み込む
            this.History.StateChanged
                .Where(x => x != null)
                .Subscribe(this.OnStateChanged)
                .AddTo(this.Disposables);

            //破棄時に設定を保存
            Disposable.Create(() => core.Save()).AddTo(this.Disposables);

            this.History.Clear();
            this.SetNewSearch(null);
            this.History.Current.Type = PageType.Search;

            this.viewerImageChangeGate = true;
        }

        /// <summary>
        /// ページ・状態変更時処理
        /// </summary>
        /// <param name="state"></param>
        private void OnStateChanged(ViewState state)
        {
            this.IsStateChangingSubject.OnNext(true);

            //Catalogの描画を止める
            this.IsCatalogRenderingEnabledSubject.OnNext(false);

            var refreshList = false;

            //リクエストされたインデックスを保持
            var viewerIndex = state.ViewerIndex;
            var catalogIndex = state.CatalogIndex;

            if (state.Type != PageType.Search)
            {
                //検索条件を設定
                if (state.GroupKey != null)
                {
                    if (this.lastGroup != null && this.lastGroup.Id.Equals(state.GroupKey))
                    {
                        this.front.SetGroupSearch(this.lastGroup);
                        this.lastGroup = null;
                    }
                    else if (!state.GroupKey.Equals(this.front.FeaturedGroup?.Id))
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
                //Viewerの画像を更新
                this.ChangeToViewerSubject.OnNext(viewerIndex);
                this.ViewerIndex.Value = viewerIndex;
            }

            //Catalogのスクロール位置を復元
            this.CatalogIndex.Value = catalogIndex;
            if (state.Type == PageType.Catalog)
            {
                this.CatalogScrollIndexSubject.OnNext(catalogIndex);
            }
            //Catalog再描画
            this.IsCatalogRenderingEnabledSubject.OnNext(true);




            if ((state.GroupKey != null || state.Search != null) && state.Type != PageType.Search)
            {
                //画像読み込みをクリア
                core.ImageBuffer.ClearThumbNailRequests();
                core.ImageBuffer.ClearRequests();

                //DB問い合わせ
                if (state.Type == PageType.Viewer)
                {
                    this.front.GetRecords(viewerIndex, 1, 0, true);
                }
                else
                {
                    this.SearchForCatalog(catalogIndex, this.PageSize.Value, 0);
                }
            }

            if (state.Type != PageType.None)
            {
                this.PageChangeRequestSubject.OnNext(state.Type);
            }
            if (refreshList)
            {
                //検索履歴を更新
                this.front.RefreshSearchList();
            }

            //状態遷移完了
            this.IsStateChangingSubject.OnNext(false);
        }

        /// <summary>
        /// 検索結果一覧用データベースアクセス
        /// </summary>
        /// <param name="index"></param>
        /// <param name="viewSize"></param>
        /// <param name="baseDirection"></param>
        private void SearchForCatalog(long index, int viewSize, int baseDirection)
        {
            if (viewSize <= 0 && !isCatalogLoaded)
            {
                isCatalogLoaded = true;
                return;
            }
            var offset = Math.Max(index - (viewSize / 2), 0);
            var takes = viewSize * 2;
            var direction = (index == 0) ? 1 : baseDirection;

            var length = this.front.Length.Value;

            if (length > 0 && offset > 0 && viewSize > 0 && offset + viewSize > length)
            {
                offset = length - viewSize * 3 / 2;
            }

            this.front.GetRecords(offset, takes > 0 ? takes : 1, direction, true);
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
            this.History.MoveNew(new ViewState()
            {
                Search = search,
                GroupKey = null,
                CatalogIndex = 0,
                ViewerIndex = 0,
                Type = PageType.None,
            });
        }

        /// <summary>
        /// 単一条件の検索を開始
        /// </summary>
        /// <param name="property"></param>
        /// <param name="reference"></param>
        /// <param name="mode"></param>
        public bool StartNewSearch(FileProperty property, object reference, CompareMode mode)
        {
            return this.StartNewSearch(
                new UnitSearch()
                {
                    Property = property,
                    Reference = reference,
                    Mode = mode,
                }
            );
        }

        /// <summary>
        /// 新しい検索を開始してページを移動
        /// </summary>
        /// <param name="criteria"></param>
        public bool StartNewSearch(ISqlSearch criteria)
        {
            var search = new SearchInformation(new ComplexSearch(false));

            if (criteria != null)
            {
                search.Root.Add(criteria);
            }

            //// 同じ内容の検索は無視
            //if (this.front.FeaturedGroup == null && this.front.SearchInformation != null)
            //{
            //    if (this.SelectedPage.Value == PageType.Catalog
            //        && this.front.SearchInformation.SettingEquals(search))
            //    {
            //        Debug.WriteLine("same search");
            //        return false;
            //    }
            //}

            this.SetNewSearch(search);
            this.ChangePage(PageType.Catalog, null, 0);

            return true;
        }


        /// <summary>
        /// グループの内容を表示
        /// </summary>
        /// <param name="group"></param>
        public void SetNewGroupSearch(Record group)
        {
            this.lastGroup = group;
            this.History.MoveNew(new ViewState()
            {
                Search = null,
                GroupKey = group.Id,
                CatalogIndex = 0,
                ViewerIndex = 0,
                Type = PageType.None,
            });
        }

        /// <summary>
        /// ソート条件を変更
        /// </summary>
        /// <param name="sort"></param>
        public void SetSort(IEnumerable<SortSetting> sort) => this.front.SetSort(sort);

        /// <summary>
        /// 現在のソート設定を取得
        /// </summary>
        /// <returns></returns>
        public SortSetting[] GetSort() => this.front.GetSort();


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
        public void MoveToViewerOrGroupDetail(Record record, long index)
        {
            if (record == null)
            {
                return;
            }

            if (record.IsGroup)
            {
                this.SetNewGroupSearch(record);
                core.ImageBuffer.ClearThumbNailRequests();
                this.ChangePage(PageType.Catalog, null, 0);
            }
            else if (index >= 0)
            {
                core.ImageBuffer.ClearThumbNailRequests();
                this.ChangePage(PageType.Viewer, index, null);
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

            if (record != null && record.IsGroup)
            {
                this.SetNewGroupSearch(record);
                this.ChangePage(PageType.Viewer, index, null);
            }
        }


        private ImageLoadingOptions GenerateImageLoadOption(ImageQuality quality)
        {
            return new ImageLoadingOptions(
                this.ViewWidth,
                this.ViewHeight,
                this.core.IsSlideshowResizeToFill,
                quality,
                this.core.IsCmsEnabled);
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

            var option = this.GenerateImageLoadOption(quality);

            if (order.HasFlag(ListOrderFlags.Current))
            {
                //現在選択されている画像をロード
                this.core.ImageBuffer.RequestLoading
                    (this.front[index], option, null, true, default(CancellationToken));
            }

            if (order.HasFlag(ListOrderFlags.Next) && length > 1)
            {
                //次の画像をロード
                var nextIndex = (index < length - 1) ? index + 1 : 0;

                this.core.ImageBuffer.RequestLoading
                    (this.front[nextIndex], option, null, false, default(CancellationToken));
            }

            if (order.HasFlag(ListOrderFlags.Previous) && length > 1)
            {
                //前の画像をロード
                var prevIndex = (index > 0) ? index - 1 : length - 1;

                this.core.ImageBuffer.RequestLoading
                    (this.front[prevIndex], option, null, false, default(CancellationToken));

            }
        }


        /// <summary>
        /// ビューア用画像を設定
        /// </summary>
        /// <param name="client"></param>
        private void SetRecord(bool ignoreIfDifferent)
        {
            var length = this.front.Length.Value;

            if (length <= 0)
            {
                if (!ignoreIfDifferent)
                {
                    this.ViewerDisplayingInner.Value = Record.Empty;
                }
                return;
            }

            if (length == 1)
            {
                ignoreIfDifferent = false;
            }

            var index = this.ViewerIndex.Value;

            if (length > 0 && index >= length && index > 0 && !ignoreIfDifferent)
            {
                this.ViewerIndex.Value = index - 1;
                return;
            }

            var result = this.front.GetRecords(index, 1, 0, false);

            if (result.Length > 0)
            {
                var current = this.ViewerDisplayingInner.Value;

                if (!ignoreIfDifferent
                    || current == null
                    || current == Record.Empty)
                {
                    if (result[0] != null)
                    {
                        this.ViewerDisplayingInner.Value = result[0];
                    }
                    else if (this.front.HasSearch)
                    {
                        this.ViewerDisplayingInner.Value = Record.Empty;
                    }
                    else
                    {
                        Debug.WriteLine($"no search client");
                    }
                    //this.ViewerDisplayingInner.Value = result[0] ?? Record.Empty;
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
            else if (!ignoreIfDifferent)
            {
                this.ViewerDisplayingInner.Value = Record.Empty;
            }

        }

        /// <summary>
        /// 次のレコードを先読み(ランダム用)
        /// </summary>
        /// <param name="index"></param>
        public void PrepareNext(long index) => this.PrepareNextSubject.OnNext(index);

        /// <summary>
        /// 検索ページに移動
        /// </summary>
        public void MoveToSearch() => this.MoveToPage(PageType.Search, false);

        /// <summary>
        /// 一覧ページに移動
        /// </summary>
        public void MoveToCatalog() => this.MoveToPage(PageType.Catalog, false);


        /// <summary>
        /// ページを移動
        /// </summary>
        /// <param name="type"></param>
        /// <param name="force"></param>
        private void MoveToPage(PageType type, bool force = false)
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
                    this.ChangePage(type, 0, 0);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// ページを移動
        /// </summary>
        /// <param name="type"></param>
        /// <param name="viewerIndex"></param>
        /// <param name="catalogIndex"></param>
        private void ChangePage(PageType type, long? viewerIndex, long? catalogIndex)
        {
            if (this.History.BackHistoryCount.Value > 0
                && (this.History.Current.Type == PageType.None
                || this.History.Current.Type == PageType.Search))
            {
                if (viewerIndex.HasValue)
                {
                    this.ViewerIndex.Value = viewerIndex.Value;
                }
                if (catalogIndex.HasValue)
                {
                    this.CatalogIndex.Value = catalogIndex.Value;
                }
                this.History.Current.Type = type;
            }
            else if (this.History.Current.Type != type)
            {
                var state = new ViewState()
                {
                    Search = this.front.SearchInformation,
                    GroupKey = this.front.FeaturedGroup?.Id,
                    ViewerIndex = viewerIndex ?? this.ViewerIndex.Value,
                    CatalogIndex = catalogIndex ?? this.CatalogIndex.Value,
                    Type = type,
                };

                this.History.MoveNew(state);
            }

            this.PageChangeRequestSubject.OnNext(type);

            if (type == PageType.Catalog && catalogIndex.HasValue)
            {
                this.CatalogScrollIndexSubject.OnNext(catalogIndex.Value);
            }
        }

        /// <summary>
        /// 戻る
        /// </summary>
        public void Back()
        {
            var prevState = this.History.Current;
            var prevSearch = prevState?.Search;
            var prevSearchSnapShot = prevSearch?.Clone();

            this.History.MoveBack();

            var currentSearch = this.History.Current?.Search;

            if (currentSearch != null && prevState != null && prevSearchSnapShot != null
                && object.ReferenceEquals(currentSearch, prevSearch))
            {
                prevSearchSnapShot.SetNewKey();
                prevState.Search = prevSearchSnapShot;
            }
        }

        /// <summary>
        /// 進む
        /// </summary>
        public void Forward() => this.History.MoveForward();

        /// <summary>
        /// ビューアで表示中のファイルを削除
        /// </summary>
        /// <returns></returns>
        public Task<bool> DeleteDisplayingFile(bool notDeleteFile)
            => this.DeleteFilesAsync(this.ToSingleDictionary(this.ViewerDisplaying.Value), notDeleteFile);

        /// <summary>
        /// プロパティ表示中のファイルを削除
        /// </summary>
        /// <returns></returns>
        public Task<bool> DeleteSelectedSingleFile(bool notDeleteFile)
            => this.DeleteFilesAsync(this.ToSingleDictionary(this.SelectedRecord.Value), notDeleteFile);

        private KeyValuePair<string, Record>[] ToSingleDictionary(Record value)
            => new[] { new KeyValuePair<string, Record>(value?.Id, value) };


        /// <summary>
        /// 選択されたファイルをすべて削除
        /// </summary>
        /// <returns></returns>
        public Task<bool> DeleteSelectedFiles(bool notDeleteFile)
            => this.DeleteFilesAsync(this.SelectedItems.GetAll().ToArray(), notDeleteFile);

        /// <summary>
        /// ファイルの削除
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        private async Task<bool> DeleteFilesAsync
            (IEnumerable<KeyValuePair<string, Record>> files, bool notDeleteFile)
        {
            if (files == null)
            {
                return false;
            }

            var items = files.Where(x => !x.Key.IsNullOrWhiteSpace()).ToArray();

            if (items.IsNullOrEmpty())
            {
                return false;
            }

            if (items.Any(x => x.Value?.IsGroup ?? false))
            {
                MessageBox.Show((items.Length == 1)
                    ? this.core.GetResourceString("DeleteFileText6")
                    : this.core.GetResourceString("DeleteFileText7"));
                return false;
            }

            string messageBoxTitle;
            string messaBoxText;

            if (!notDeleteFile)
            {
                if (items.Length == 1)
                {
                    messageBoxTitle = this.core.GetResourceString("DeleteFileText1");
                    messaBoxText = this.core.GetResourceString("DeleteFileText2")
                        + "\n" + items[0].Key;
                }
                else
                {
                    messageBoxTitle = this.core.GetResourceString("DeleteFileText3");
                    messaBoxText = this.core.GetResourceString("DeleteFileText4")
                        + items.Length.ToString() + this.core.GetResourceString("DeleteFileText5");
                }
            }
            else
            {

                if (items.Length == 1)
                {
                    messageBoxTitle = this.core.GetResourceString("DeleteFileText1");
                    messaBoxText = this.core.GetResourceString("DeleteFileText8")
                        + "\n" + this.core.GetResourceString("DeleteFileText10")
                        + "\n" + items[0].Key;
                }
                else
                {
                    messageBoxTitle = this.core.GetResourceString("DeleteFileText3");
                    messaBoxText = this.core.GetResourceString("DeleteFileText9")
                        + items.Length.ToString() + this.core.GetResourceString("DeleteFileText11")
                        + "\n" + this.core.GetResourceString("DeleteFileText10");
                }
            }

            var userOperation = MessageBox.Show(messaBoxText, messageBoxTitle,
                MessageBoxButton.OKCancel,
                MessageBoxImage.Question);

            if (userOperation != MessageBoxResult.Yes && userOperation != MessageBoxResult.OK)
            {
                return false;
            }

            var result = await this.front.DeleteItemsAsync(items, notDeleteFile);

            if (result)
            {
                this.SelectedItems.Clear();

                if (this.SelectedPage.Value == PageType.Viewer)
                {
                    this.SetRecord(false);
                }
            }
            return result;
        }


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

            var state = new ViewState()
            {
                Search = null,
                GroupKey = null,
                CatalogIndex = 0,
                ViewerIndex = 0,
                Type = PageType.Catalog,
            };

            this.History.MoveNew(state);

            this.ChangePage(PageType.Viewer, 0, 0);

            var record = records[0];

            //画像ロード
            this.core.ImageBuffer.RequestLoading
                (record, this.GenerateImageLoadOption(ImageQuality.Resized),
                null, true, default(CancellationToken));

            if (records.Length != 1)
            {
                return;
            }

            string directory;
            string tmpDir;

            try
            {
                directory = System.IO.Path.GetDirectoryName(record.FullPath);
                tmpDir = System.IO.Path.GetDirectoryName(System.IO.Path.GetTempPath());
            }
            catch
            {
                return;
            }

            //一時フォルダ内のファイル
            if (directory.ToLower().StartsWith(tmpDir.ToLower()))
            {
                return;
            }

            //ファイルが一つだけ、かつ一時フォルダでない

            Task.Run(async () =>
            {
                //同じフォルダ内のファイルをライブラリに登録
                await this.front.ActivateFolderAsync(directory).ConfigureAwait(false);

                var search = new SearchInformation(new ComplexSearch(false));
                search.Root.Add(new UnitSearch()
                {
                    Property = FileProperty.DirectoryPathStartsWith,
                    Reference = record.Directory,
                    Mode = CompareMode.Equal,
                });

                search.SetSort(new[]
                {
                    new SortSetting() { Property = FileProperty.FileName, IsDescending = false }
                });

                var index = await core.Library.FindIndexAsync(search, record).ConfigureAwait(false);

                this.lastViewerImageIndex = new OldNewPair<long>(index, index);

                this.front.Length
                    .Where(x => x > index)
                    .Take(1)
                    .Subscribe(x =>
                    {
                        this.viewerImageChangeGate = false;
                        this.ViewerIndexInner = index;
                        this.viewerImageChangeGate = true;
                    });


                state.Search = search;
                this.History.Current.Search = search;
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.front.SetSearch(search, true);
                });

            })
            .FireAndForget();
        }

        /// <summary>
        /// 最新の情報に更新
        /// </summary>
        public void Refresh()
        {
            this.core.ImageBuffer.ClearAll();
            this.front.Refresh();
            if (this.SelectedPage.Value == PageType.Viewer)
            {
                this.SetRecord(false);
            }
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
