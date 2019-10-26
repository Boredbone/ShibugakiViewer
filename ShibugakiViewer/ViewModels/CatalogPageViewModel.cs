using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Boredbone.Utility.Tools;
using Boredbone.XamlTools;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.Viewer;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels.Controls;
using ShibugakiViewer.Views.Controls;
using WpfTools;

namespace ShibugakiViewer.ViewModels
{
    public class CatalogPageViewModel : NotificationBase
    {

        public DelegateCommand<object> ItemClickCommand { get; }
        public DelegateCommand<object> ItemSelectCommand { get; }
        public DelegateCommand<FrameworkElement> EditSortCommand { get; }
        public DelegateCommand SelectAllCommand { get; }
        public ReactiveCommand SelectionClearCommand { get; }
        public ReactiveCommand GroupingCommand { get; }
        public ReactiveCommand RemoveFromGroupCommand { get; }
        public ReactiveCommand SetToLeaderCommand { get; }
        public DelegateCommand RefreshCommand { get; }

        public ReadOnlyReactiveProperty<long> Length { get; }
        public ReactiveProperty<int> DisplayIndex { get; }
        public ReactiveProperty<int> StartIndex { get; }

        public ReactivePropertySlim<int> RowLength => this.client.RowLength;
        public ReactivePropertySlim<int> ColumnLength => this.client.ColumnLength;


        public ReactiveProperty<double> ThumbnailSize { get; }
        //public ReactiveProperty<Thickness> ThumbnailMargin { get; }
        public ReadOnlyReactivePropertySlim<Size> ThumbnailViewSize { get; }
        public ReadOnlyReactivePropertySlim<Visibility> ImagePropertiesVisibility { get; }
        public ReadOnlyReactivePropertySlim<bool> IsInSelecting { get; }
        public ReadOnlyReactiveProperty<bool> IsRefreshEnabled { get; }
        public ReadOnlyReactiveProperty<bool> IsRenderingEnabled { get; }

        public Action<Vector> RequestScrollAction { get; set; }
        public Action<int> ScrollToIndexAction { get; set; }

        public bool RefreshTrigger
        {
            get { return _fieldRefreshTrigger; }
            set
            {
                if (_fieldRefreshTrigger != value)
                {
                    _fieldRefreshTrigger = value;
                    RaisePropertyChanged(nameof(RefreshTrigger));
                }
            }
        }
        private bool _fieldRefreshTrigger;

        public ISearchResult SearchResult => this.client.SearchResult;
        public SelectionManager SelectedItems => this.client.SelectedItems;

        private readonly ClientWindowViewModel parent;
        private readonly Client client;
        private readonly Library library;


        public CatalogPageViewModel(ClientWindowViewModel parent)
        {
            this.parent = parent;
            this.client = parent.Client;
            this.library = parent.Library;
            var core = parent.Core;

            this.Length = client.Length.ToReadOnlyReactiveProperty().AddTo(this.Disposables);

            this.IsInSelecting = this.client.SelectedItemsCount
                .Select(x => x > 0)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            //表示中のインデックス
            this.StartIndex = this.client
                .CatalogIndex
                .Select(x => (x < int.MaxValue) ? (int)x : int.MaxValue)
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.StartIndex
                .Subscribe(x => this.client.CatalogIndex.Value = x)
                .AddTo(this.Disposables);

            this.DisplayIndex = this.StartIndex.Select(x => x + 1).ToReactiveProperty().AddTo(this.Disposables);
            this.DisplayIndex.Subscribe(x => this.StartIndex.Value = x - 1).AddTo(this.Disposables);

            this.ThumbnailSize = core.ObserveProperty(x => x.ThumbNailSize)
                .Select(x => (double)x)
                .ToReactiveProperty().AddTo(this.Disposables);

            this.ThumbnailViewSize = this.ThumbnailSize
                .Select(x => new Size(x, x))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.ImagePropertiesVisibility = this.ThumbnailSize
                .Select(x => VisibilityHelper.Set(x > 128)).ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);

            //this.ThumbnailMargin = this.ThumbnailSize
            //    .Select(x => new Thickness((x < 128) ? 1 : (x < 256) ? 2 : 4))
            //    .ToReactiveProperty().AddTo(this.Disposables);

            this.IsRefreshEnabled = this.client.IsStateChanging
                .Select(x => !x)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.IsRenderingEnabled = this.client.IsCatalogRenderingEnabled
                .CombineLatest(this.client.SelectedPage, (e, p) => e && p == PageType.Catalog)
                .ToReadOnlyReactiveProperty(true)
                .AddTo(this.Disposables);


            //戻ってきたときにサムネイル再読み込み
            client.BackHistoryCount
                .Pairwise()
                .CombineLatest(client.SelectedPage,
                    (history, page) => history.OldItem > history.NewItem && page == PageType.Catalog)
                .Where(x => x)
                .Subscribe(_ => this.RefreshTrigger = !this.RefreshTrigger)
                .AddTo(this.Disposables);

            client.CacheUpdated
                 .SkipUntil(client.StateChanged)
                 .Take(1)
                 .Repeat()
                 .ObserveOnUIDispatcher()
                 .Subscribe(_ => this.RefreshTrigger = !this.RefreshTrigger)
                 .AddTo(this.Disposables);


            //スクロールが落ち着いたら再読み込み
            this.StartIndex
                .Throttle(TimeSpan.FromMilliseconds(3000))
                .ObserveOnUIDispatcher()
                .Subscribe(_ => this.RefreshTrigger = !this.RefreshTrigger)
                .AddTo(this.Disposables);

            //スクロール位置復元
            this.client.CatalogScrollIndex
                .Subscribe(x => this.ScrollToIndexAction?.Invoke((int)x))
                .AddTo(this.Disposables);

            //サムネイルクリック
            this.ItemClickCommand = new DelegateCommand<object>(context => this.SelectOrShow(context, true));

            //選択
            this.ItemSelectCommand = new DelegateCommand<object>(context => this.SelectOrShow(context, false));

            //ソート条件編集
            this.EditSortCommand = new DelegateCommand<FrameworkElement>(control =>
            {
                var content = new SortEditor()
                {
                    ItemsSource = this.client.GetSort(),
                };

                content.IsEnabledChanged += (o, e) =>
                {
                    var value = e.NewValue as bool?;
                    if (value.HasValue && !value.Value)
                    {
                        this.client.SetSort(content.SortSettings);
                    }
                };

                this.parent.PopupOwner.PopupDialog.Show(content,
                    new Thickness(double.NaN, 10.0, 0.0, double.NaN),
                    HorizontalAlignment.Right, VerticalAlignment.Bottom, control);

            });

            //すべて選択
            this.SelectAllCommand = new DelegateCommand(async () => await this.client.SelectAllAsync());

            //選択をクリア
            this.SelectionClearCommand = this.IsInSelecting
                .ToReactiveCommand()
                .WithSubscribe(_ => this.SelectedItems.Clear(), this.Disposables);

            //グループ化
            this.GroupingCommand = this.client.SelectedItemsCount
                .CombineLatest(this.client.IsGroupMode, (c, g) => c > 1 && !g)
                .ToReactiveCommand()
                .WithSubscribe(_ => this.client.Grouping(), this.Disposables);

            //グループから退去
            this.RemoveFromGroupCommand = this.client.SelectedItemsCount
                .CombineLatest(this.client.IsGroupMode, (c, g) => c >= 1 && g)
                .ToReactiveCommand()
                .WithSubscribe(_ => this.client.RemoveFromGroup(), this.Disposables);

            this.SetToLeaderCommand = this.client.SelectedItemsCount
                .CombineLatest(this.client.IsGroupMode, (c, g) => c == 1 && g)
                .ToReactiveCommand()
                .WithSubscribe(_ => this.client.SetGroupLeader(), this.Disposables);

            this.RefreshCommand = new DelegateCommand(() => this.client.Refresh());

            this.RegisterKeyReceiver(parent);
        }

        /// <summary>
        /// クリックされたアイテムの選択または表示
        /// </summary>
        /// <param name="context"></param>
        /// <param name="show"></param>
        private void SelectOrShow(object context, bool show)
        {

            var pair = context as Indexed<object>;
            var record = pair?.Value as Record;
            if (record == null)
            {
                return;
            }
            var index = pair.Index;

            if (!show || this.IsInSelecting.Value)
            {
                this.SelectItem(record, index);
            }
            else
            {
                client.MoveToViewerOrGroupDetail(record, index);
            }
        }

        /// <summary>
        /// 選択
        /// </summary>
        /// <param name="record"></param>
        private void SelectItem(Record record, long index)
        {
            if (this.parent.KeyReceiver.IsKeyPressed(ModifierKeys.Shift))
            {
                this.parent.Client.SelectRegionAsync(record).FireAndForget();
            }
            else
            {
                this.SelectedItems.Toggle(record);
            }
        }

        /// <summary>
        /// 選択されたアイテムにタグを設定
        /// </summary>
        /// <param name="code"></param>
        private void SetTag(string code)
        {
            var res = this.library.Tags.GetTag(code).Value;

            if (res != null)
            {
                if (this.SelectedItems.Count <= 0)
                {
                    return;
                }
                else if (this.SelectedItems.Count == 1 && this.client.SelectedRecord.Value != null)
                {
                    this.client.SelectedRecord.Value.TagSet.Toggle(res);
                }
                else
                {
                    this.SelectedItems.AddTag(res);
                }
            }
        }


        /// <summary>
        /// キーボード操作を登録
        /// </summary>
        /// <param name="keyReceiver"></param>
        /// <param name="client"></param>
        private void RegisterKeyReceiver(ClientWindowViewModel parent)
        {
            var keyReceiver = parent.KeyReceiver;

            var pageFilter = keyReceiver.AddPreFilter(x =>
                (client.SelectedPage.Value == PageType.Catalog));

            var cursorFilter = keyReceiver.AddPreFilter(x =>
            {
                if (client.SelectedPage.Value != PageType.Catalog)
                {
                    return false;
                }
                return !(x.FocusedControl is TextBox);
            });

            var buttonFilter = keyReceiver.AddPreFilter(x =>
            {
                if (client.SelectedPage.Value != PageType.Catalog)
                {
                    return false;
                }
                return !(x.FocusedControl is ButtonBase) && !(x.FocusedControl is TextBox);
            });


            keyReceiver.Register(Key.PageUp, (t, key)
                => this.RequestScrollAction?.Invoke(new Vector(0, -0.8)),
                pageFilter, isPreview: true);
            keyReceiver.Register(Key.PageDown, (t, key)
                => this.RequestScrollAction?.Invoke(new Vector(0, 0.8)),
                pageFilter, isPreview: true);

            keyReceiver.Register(Key.Up, (t, key)
                => this.RequestScrollAction?.Invoke(new Vector(0, -0.2)),
                pageFilter, isPreview: true);
            keyReceiver.Register(Key.Down, (t, key)
                => this.RequestScrollAction?.Invoke(new Vector(0, 0.2)),
                pageFilter, isPreview: true);

            keyReceiver.Register(Key.Home, (t, key)
                => this.RequestScrollAction?.Invoke(new Vector(0, double.NegativeInfinity)),
                pageFilter, isPreview: true);
            keyReceiver.Register(Key.End, (t, key)
                => this.RequestScrollAction?.Invoke(new Vector(0, double.PositiveInfinity)),
                pageFilter, isPreview: true);



            keyReceiver.Register(Key.A, (t, key) =>
            {
                if (this.SelectedItems.Count != this.client.Length.Value)
                {
                    this.client.SelectAllAsync().FireAndForget();
                }
                else
                {
                    this.SelectedItems.Clear();
                }
            }, pageFilter, modifier: ModifierKeys.Control);


            keyReceiver.Register(Key.N, (t, key) => this.SelectedItems.Clear(),
                pageFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.C,
                (t, key) => this.parent.Core.CopySelectedItemsPath(this.SelectedItems),
                pageFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.T, (t, key) => this.parent.ShowTagSelector(null),
                cursorFilter, modifier: ModifierKeys.Control);


            keyReceiver.Register(Key.Apps, (t, key) =>
            {
                if (!parent.IsPaneFixed.Value)
                {
                    parent.ToggleInformationPane();
                }
            }, cursorFilter);

            keyReceiver.Register(k => k >= Key.A && k <= Key.Z,
                (t, key) => this.SetTag(((char)(key - Key.A + 'a')).ToString()),
                cursorFilter);


            keyReceiver.Register(Key.Delete,
                async (t, key) => await this.client.DeleteSelectedFiles(false),
                cursorFilter);

            keyReceiver.Register(Key.Delete,
                async (t, key) => await this.client.DeleteSelectedFiles(true),
                cursorFilter, modifier: ModifierKeys.Control);

            keyReceiver.Register(Key.F5, (t, key) => this.client.Refresh(), cursorFilter);
        }
    }
}
