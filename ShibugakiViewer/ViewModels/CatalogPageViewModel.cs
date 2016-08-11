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

        public ReactiveCommand ItemClickCommand { get; }
        public ReactiveCommand ItemSelectCommand { get; }
        public ReactiveCommand EditSortCommand { get; }
        public ReactiveCommand SelectAllCommand { get; }
        public ReactiveCommand SelectionClearCommand { get; }
        public ReactiveCommand GroupingCommand { get; }
        public ReactiveCommand RemoveFromGroupCommand { get; }
        public ReactiveCommand SetToLeaderCommand { get; }
        public ReactiveCommand RefreshCommand { get; }

        public ReadOnlyReactiveProperty<long> Length { get; }
        public ReactiveProperty<int> DisplayIndex { get; }
        public ReactiveProperty<int> StartIndex { get; }

        public ReactiveProperty<int> RowLength => this.client.RowLength;
        public ReactiveProperty<int> ColumnLength => this.client.ColumnLength;


        public ReactiveProperty<double> ThumbnailSize { get; }
        public ReactiveProperty<Thickness> ThumbnailMargin { get; }
        public ReadOnlyReactiveProperty<Size> ThumbnailViewSize { get; }
        public ReactiveProperty<Visibility> ImagePropertiesVisibility { get; }
        public ReadOnlyReactiveProperty<bool> IsInSelecting { get; }
        public ReadOnlyReactiveProperty<bool> IsRefreshEnabled { get; }
        
        public Action<Vector> RequestScrollAction { get; set; }

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
                .ToReadOnlyReactiveProperty()
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
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.ImagePropertiesVisibility = this.ThumbnailSize
                .Select(x => VisibilityHelper.Set(x > 128)).ToReactiveProperty().AddTo(this.Disposables);

            this.ThumbnailMargin = this.ThumbnailSize
                .Select(x => new Thickness((x < 128) ? 1 : (x < 256) ? 2 : 4))
                .ToReactiveProperty().AddTo(this.Disposables);

            this.IsRefreshEnabled = this.client.IsStateChanging
                .Select(x => !x)
                .ObserveOnUIDispatcher()
                .ToReadOnlyReactiveProperty()
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
                 .Subscribe(_ => this.RefreshTrigger = !this.RefreshTrigger)
                 .AddTo(this.Disposables);
            

            //スクロールが落ち着いたら再読み込み
            this.StartIndex
                .Throttle(TimeSpan.FromMilliseconds(3000))
                .ObserveOnUIDispatcher()
                .Subscribe(_ => this.RefreshTrigger = !this.RefreshTrigger)
                .AddTo(this.Disposables);

            //サムネイルクリック
            this.ItemClickCommand = new ReactiveCommand()
                .WithSubscribeOfType<Record>(record =>
                {
                    if (this.IsInSelecting.Value)
                    {
                        this.SelectItem(record);
                    }
                    else
                    {
                        client.MoveToViewerOrGroupDetail(record);
                    }
                }, this.Disposables);

            //選択
            this.ItemSelectCommand = new ReactiveCommand()
                .WithSubscribeOfType<Record>(record => this.SelectItem(record), this.Disposables);

            //ソート条件編集
            this.EditSortCommand = new ReactiveCommand()
                .WithSubscribe(x =>
                {
                    var control = x as FrameworkElement;
                    
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

                }, this.Disposables);

            //すべて選択
            this.SelectAllCommand = new ReactiveCommand()
                .WithSubscribe(async _ => await this.client.SelectAllAsync(), this.Disposables);

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

            this.RefreshCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.client.Refresh(), this.Disposables);

            this.RegisterKeyReceiver(parent);
        }

        /// <summary>
        /// 選択
        /// </summary>
        /// <param name="record"></param>
        private void SelectItem(Record record)
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

            keyReceiver.Register(Key.C,
                (t, key) => this.parent.Core.CopySelectedItemsPath(this.SelectedItems),
                pageFilter, modifier: ModifierKeys.Control);
        }
    }
}
