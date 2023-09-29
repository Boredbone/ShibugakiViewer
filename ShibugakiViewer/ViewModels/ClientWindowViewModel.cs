using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Database.Search;
using ImageLibrary.Core;
using ImageLibrary.Exif;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using ImageLibrary.Viewer;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.Models.Utility;
using ShibugakiViewer.Views.Controls;
using WpfTools;
using WpfTools.Controls;

namespace ShibugakiViewer.ViewModels
{
    public class ClientWindowViewModel : NotificationBase
    {

        private const double middleWindowWidth = 720.0;
        private const double wideWindowWidth = 970.0;
        private const double compactPaneWidth = 48.0;// (double)Application.Current.Resources["CompactPaneWidth"];
        private const double openPaneWidth = 320.0;// (double)Application.Current.Resources["OpenPaneWidth"];


        public CatalogPageViewModel Catalog
        {
            get { return _fieldCatalog; }
            set
            {
                if (_fieldCatalog != value)
                {
                    _fieldCatalog = value;
                    RaisePropertyChanged(nameof(Catalog));
                }
            }
        }
        private CatalogPageViewModel _fieldCatalog;

        public ViewerPageViewModel Viewer
        {
            get { return _fieldViewer; }
            set
            {
                if (_fieldViewer != value)
                {
                    _fieldViewer = value;
                    RaisePropertyChanged(nameof(Viewer));
                }
            }
        }
        private ViewerPageViewModel _fieldViewer;

        public SearchPageViewModel Search
        {
            get { return _fieldSearch; }
            set
            {
                if (_fieldSearch != value)
                {
                    _fieldSearch = value;
                    RaisePropertyChanged(nameof(Search));
                }
            }
        }
        private SearchPageViewModel _fieldSearch;


        public ReactiveProperty<int> SelectedTab { get; }

        public ReactiveCommandSlim<object?> BackCommand { get; }
        public ReactiveCommandSlim<object?> MoveToSearchPageCommand { get; }
        public ReactiveCommandSlim<object?> OpenPaneCommand { get; }
        public ReactiveCommandSlim<object?> OpenInformationPaneCommand { get; }
        public ReactiveCommandSlim<object?> OpenSettingPaneCommand { get; }
        public ReactiveCommandSlim<object?> OpenSettingWindowCommand { get; }
        public ReactiveCommandSlim<object?> FileDropCommand { get; }
        public ReactiveCommandSlim<object?> OpenHelpPaneCommand { get; }

        public ReactiveCommandSlim<object?> MouseExButtonLeftCommand { get; }
        public ReactiveCommandSlim<object?> MouseExButtonRightCommand { get; }
        private Subject<bool> MouseExButtonSubject { get; }
        public IObservable<bool> MouseExButtonPressed => this.MouseExButtonSubject.AsObservable();

        public ReactivePropertySlim<string?> PaneSelectedPath { get; }
        public ReactivePropertySlim<TagInformation?> PaneSelectedTag { get; }


        public ReadOnlyReactivePropertySlim<bool> IsOptionPageOpen { get; }
        public ReactivePropertySlim<bool> IsPaneOpen { get; }
        public ReactiveProperty<bool> IsInformationPaneOpen { get; }
        public ReactivePropertySlim<bool> IsPaneFixed { get; }
        public ReactiveProperty<bool> IsSettingPaneOpen { get; }
        public ReactiveProperty<bool> IsHelpPaneOpen { get; }

        public ReactivePropertySlim<double> FrameWidth { get; }

        //public Subject<SplitViewDisplayMode> PageChangedSubject { get; }
        public ReactiveProperty<SplitViewDisplayMode> PaneDisplayMode { get; }

        public ReadOnlyReactivePropertySlim<double> JumpListWidth { get; }

        public ReactivePropertySlim<OptionPaneType> SelectedInformationPage { get; }


        public ReactiveCommandSlim<string> OptionPageCommand { get; }

        private ReadOnlyReactivePropertySlim<PaneMode> DefaultPaneMode { get; }

        public ReadOnlyReactivePropertySlim<Visibility> OptionPaneVisibility { get; }
        public ReactivePropertySlim<Visibility> PaneOpenButtonVisibility { get; }
        public ReadOnlyReactivePropertySlim<Visibility> PaneFixButtonVisibility { get; }

        public ReactivePropertySlim<bool> IsPopupOpen { get; }
        public ReactiveProperty<bool> IsFullScreen { get; }

        public ReadOnlyReactivePropertySlim<Record> SelectedRecord => this.Client.SelectedRecord;
        public ReadOnlyReactivePropertySlim<string?> WindowTitle { get; }

        public double TagSelectorScrollOffset { get; set; }
        public TagInformation TagSelectorLastSelected { get; set; }
        public ReactivePropertySlim<int> TagSelectorSortMode { get; }

        public IReadOnlyList<ExifVisibilityItem> ExifVisibilityList
            => this.Core.Library.ExifManager.TagVisibilityList;
        public ReactivePropertySlim<bool> ExifVisibilityCheck { get; }
        public ReadOnlyReactivePropertySlim<bool> IsExifEnabled { get; }

        public KeyReceiver<object> KeyReceiver { get; }
        
        public IPopupDialogOwner PopupOwner { get; set; }

        public Client Client { get; }
        public ApplicationCore Core { get; }
        public Library Library => this.Core.Library;
        public SelectionManager SelectedItems => this.Client.SelectedItems;

        private SplitViewDisplayMode prevPaneMode;
        private OptionPaneType prevPaneSelected;
        private bool prevPaneOpen;



        public ClientWindowViewModel(PageType firstPage, int pageChangeSkip)
        {
            var core = ((App)Application.Current).Core;
            var library = core.Library;

            var client = new Client(new LibraryFront(library), core, firstPage, pageChangeSkip).AddTo(this.Disposables);
            this.Client = client;
            this.Core = core;

            this.KeyReceiver = new KeyReceiver<object>().AddTo(this.Disposables);

            this.WindowTitle = client.SelectedPage
                .CombineLatest(client.ViewerDisplaying, (Page, Item) => new { Page, Item })
                .Select(x =>
                {
                    var file = (x.Page == PageType.Viewer) ? x.Item?.FileName : null;
                    return (file == null) ? core.AppName : (file + " - " + core.AppName);
                })
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.SelectedInformationPage = new ReactivePropertySlim<OptionPaneType>
                (core.IsViewerPageLeftBarFixed ? OptionPaneType.ItemInfo : OptionPaneType.None)
                .AddTo(this.Disposables);

            this.IsPaneOpen = new ReactivePropertySlim<bool>(core.IsViewerPageLeftBarFixed)
                .AddTo(this.Disposables);


            this.IsPaneFixed = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsViewerPageLeftBarFixed)
                .AddTo(this.Disposables);

            this.SelectedTab = client.SelectedPage
                .Select(x =>
                {
                    switch (x)
                    {
                        case PageType.Search:
                            return 0;
                        case PageType.Catalog:
                            return 1;
                        case PageType.Viewer:
                            return 2;
                        default:
                            return 0;
                    }
                })
                .ToReactiveProperty(0)
                .AddTo(this.Disposables);


            this.SelectedTab.Subscribe(x =>
            {
                if (this.IsPaneOpen.Value)
                {
                    if (this.IsPaneFixed.Value)
                    {
                        this.ShowInformationPane();
                    }
                    else
                    {
                        this.IsPaneOpen.Value = false;
                    }
                }
            })
            .AddTo(this.Disposables);

            this.Client.FeaturedGroupChanged.Subscribe(x =>
            {
                if (client.SelectedPage.Value == PageType.Catalog && this.IsPaneOpen.Value)
                {
                    this.ShowInformationPane();
                }
            })
            .AddTo(this.Disposables);


            this.PaneSelectedPath = new ReactivePropertySlim<string?>(null)
                .AddTo(this.Disposables);
            this.PaneSelectedPath.Where(x => !string.IsNullOrWhiteSpace(x))
                .Subscribe(x => this.StartPathOrTagSearch(FileProperty.DirectoryPathStartsWith, SearchReferences.From(x)))
                .AddTo(this.Disposables);

            this.PaneSelectedTag = new ReactivePropertySlim<TagInformation?>(null)
                .AddTo(this.Disposables);
            this.PaneSelectedTag.Where(x => x != null)
                .Subscribe(x => this.StartPathOrTagSearch(FileProperty.ContainsTag, SearchReferences.From(x.Id)))
                .AddTo(this.Disposables);

            this.DefaultPaneMode = client.SelectedPage
                .Select(x => (x == PageType.Viewer) ? PaneMode.HideInClosing
                    : PaneMode.AlwaysVisible)
                .ToReadOnlyReactivePropertySlim(PaneMode.AlwaysVisible)
                .AddTo(this.Disposables);



            this.IsOptionPageOpen = this.SelectedInformationPage
                .Select(x => x > 0)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);


            //情報
            this.IsInformationPaneOpen = this.SelectedInformationPage
                .Select(x => this.IsInformationPane(x))
                .ToReactiveProperty(false)
                .AddTo(this.Disposables);

            this.OpenInformationPaneCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    if (this.IsInformationPaneOpen.Value)
                    {
                        if (this.prevPaneSelected == OptionPaneType.None)
                        {
                            this.prevPaneSelected = OptionPaneType.NoInformation;
                        }
                        this.ShowInformationPane(true);
                    }
                    else
                    {
                        if (this.SelectedInformationPage.Value != OptionPaneType.Setting)
                        {
                            this.SelectedInformationPage.Value = OptionPaneType.None;
                        }
                    }
                })
                .AddTo(this.Disposables);



            //設定
            this.IsSettingPaneOpen = this.SelectedInformationPage
                .Select(x => x == OptionPaneType.Setting)
                .ToReactiveProperty(false)
                .AddTo(this.Disposables);

            this.OpenSettingPaneCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    if (this.IsSettingPaneOpen.Value)
                    {
                        this.SelectedInformationPage.Value = OptionPaneType.Setting;
                        this.IsPaneOpen.Value = true;
                    }
                    else
                    {
                        if (this.SelectedInformationPage.Value == OptionPaneType.Setting)
                        {
                            this.SelectedInformationPage.Value = OptionPaneType.None;
                        }
                    }
                })
                .AddTo(this.Disposables);



            //ヘルプ
            this.IsHelpPaneOpen = this.SelectedInformationPage
                .Select(x => x == OptionPaneType.Help)
                .ToReactiveProperty(false)
                .AddTo(this.Disposables);

            this.OpenHelpPaneCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    if (this.IsHelpPaneOpen.Value)
                    {
                        this.SelectedInformationPage.Value = OptionPaneType.Help;
                        this.IsPaneOpen.Value = true;
                    }
                    else
                    {
                        if (this.SelectedInformationPage.Value == OptionPaneType.Help)
                        {
                            this.SelectedInformationPage.Value = OptionPaneType.None;
                        }
                    }
                })
                .AddTo(this.Disposables);



            this.OptionPaneVisibility = this.SelectedInformationPage
                .Select(x => VisibilityHelper.Set(x > 0))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.FrameWidth = new ReactivePropertySlim<double>(300).AddTo(this.Disposables);

            this.IsFullScreen = client.SelectedPage.Select(_ => false).ToReactiveProperty().AddTo(this.Disposables);

            //this.PageChangedSubject = new Subject<SplitViewDisplayMode>().AddTo(this.Disposables);

            this.PaneDisplayMode = this.IsPaneFixed
                .CombineLatest(this.DefaultPaneMode,
                (paneFixed, defaultMode) =>
                    (defaultMode == PaneMode.Disabled) ? SplitViewDisplayMode.Overlay
                    : (paneFixed) ? SplitViewDisplayMode.CompactInline
                    : (defaultMode == PaneMode.HideInClosing) ? SplitViewDisplayMode.Overlay
                    : SplitViewDisplayMode.CompactOverlay)
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.IsPaneOpen
                .Subscribe(x =>
                {
                    if (!x)
                    {
                        this.IsPaneFixed.Value = false;
                        this.SelectedInformationPage.Value = OptionPaneType.None;
                        if (this.PaneDisplayMode.Value == SplitViewDisplayMode.CompactInline)
                        {
                            this.PaneDisplayMode.Value = SplitViewDisplayMode.CompactOverlay;
                        }
                    }
                })
                .AddTo(this.Disposables);


            this.PaneOpenButtonVisibility = new ReactivePropertySlim<Visibility>(Visibility.Collapsed)
                .AddTo(this.Disposables);

            var isWide = this.FrameWidth.Select(y => y > middleWindowWidth).Publish().RefCount();
            this.PaneFixButtonVisibility = isWide.Select(y => VisibilityHelper.Set(y))
                .ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);
            isWide.Where(y => !y).Skip(2).Subscribe(y => this.IsPaneFixed.Value = false).AddTo(this.Disposables);

            this.JumpListWidth = this.IsOptionPageOpen.Select(x => x ? compactPaneWidth : openPaneWidth)
                .ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);

            this.IsOptionPageOpen.Subscribe(x =>
            {
                if (x && this.SelectedInformationPage.Value != OptionPaneType.NoInformation
                    && !core.IsAutoInformationPaneDisabled)
                {
                    this.IsPaneOpen.Value = true;
                }
            }).AddTo(this.Disposables);




            this.OpenSettingWindowCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => ((App)Application.Current).ShowSettingWindow(-1))
                .AddTo(this.Disposables);

            this.OptionPageCommand = new ReactiveCommandSlim<string>().AddTo(this.Disposables);





            this.IsPopupOpen = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);

            this.IsPopupOpen
                .Subscribe(x => this.KeyReceiver.Mode
                    = (int)(x ? KeyReceiverMode.PopupIsOpened : KeyReceiverMode.Normal))
                .AddTo(this.Disposables);




            this.prevPaneMode = this.PaneDisplayMode.Value;
            this.prevPaneOpen = this.IsPaneOpen.Value;
            this.prevPaneSelected = (OptionPaneType)this.SelectedInformationPage.Value;

            this.SelectedItems.ObserveProperty(x => x.Count).Pairwise().Subscribe(x =>
            {
                var autoOpen = !core.IsAutoInformationPaneDisabled;

                if (x.OldItem <= 0 && x.NewItem > 0)
                {

                    this.prevPaneMode = this.PaneDisplayMode.Value;
                    this.prevPaneOpen = this.IsPaneOpen.Value;
                    this.prevPaneSelected = (OptionPaneType)this.SelectedInformationPage.Value;

                    if (autoOpen && this.FrameWidth.Value > wideWindowWidth)
                    {
                        this.PaneDisplayMode.Value = SplitViewDisplayMode.CompactInline;
                        this.IsPaneOpen.Value = true;
                    }

                    this.ShowInformationPane();
                }
                else if (x.OldItem > 0 && x.NewItem > 0
                    && (this.FrameWidth.Value > wideWindowWidth || this.IsPaneOpen.Value))
                {
                    this.ShowInformationPane();

                }
                else if (x.OldItem > 0 && x.NewItem <= 0)
                {

                    if (autoOpen)
                    {
                        this.PaneDisplayMode.Value = this.prevPaneMode;
                    }

                    this.ShowInformationPane();

                    if (autoOpen)
                    {
                        this.IsPaneOpen.Value = this.prevPaneOpen;
                    }

                }
            })
            .AddTo(this.Disposables);


            this.IsPaneFixed
                .Subscribe(x =>
                {
                    if (x)
                    {
                        this.prevPaneMode = SplitViewDisplayMode.CompactInline;
                        this.prevPaneOpen = true;
                    }
                    else
                    {
                        this.prevPaneMode = SplitViewDisplayMode.CompactOverlay;
                        this.prevPaneOpen = false;
                    }
                })
                .AddTo(this.Disposables);

            this.TagSelectorSortMode = core
                .ToReactivePropertySlimAsSynchronized(x => x.TagSelectorSortMode)
                .AddTo(this.Disposables);

            this.ExifVisibilityCheck = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);
            this.ExifVisibilityCheck
                .Skip(1)
                .Subscribe(x => core.Library.ExifManager.EnableAll(x)).AddTo(this.Disposables);

            this.IsExifEnabled = core.Library.ExifManager.HasVisibleItem
                .ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);


            this.BackCommand = client.BackHistoryCount
                .Select(x => x > 0)
                .ToReactiveCommandSlim()
                .WithSubscribe(_ => client.Back())
                .AddTo(this.Disposables);

            this.MoveToSearchPageCommand = new ReactiveCommandSlim()
                .WithSubscribe(x => client.MoveToSearch())
                .AddTo(this.Disposables);

            this.OpenPaneCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ => this.TogglePane(OptionPaneType.None))
                .AddTo(this.Disposables);

            // ウインドウへのファイルのドラッグ&ドロップ
            this.FileDropCommand = new ReactiveCommandSlim()
                .WithSubscribe(obj =>
                {
                    if (obj is string[] files)
                    {
                        this.Client.ActivateFiles(files);
                    }
                })
                .AddTo(this.Disposables);

            this.MouseExButtonSubject = new Subject<bool>().AddTo(this.Disposables);

            this.MouseExButtonLeftCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    if (core.UseExtendedMouseButtonsToSwitchImage
                        && client.SelectedPage.Value == PageType.Viewer)
                    {
                        this.MouseExButtonSubject.OnNext(false);
                    }
                    else
                    {
                        client.Back();
                    }
                })
                .AddTo(this.Disposables);

            this.MouseExButtonRightCommand = new ReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    if (core.UseExtendedMouseButtonsToSwitchImage
                        && client.SelectedPage.Value == PageType.Viewer)
                    {
                        this.MouseExButtonSubject.OnNext(true);
                    }
                    else
                    {
                        client.Forward();
                    }
                })
                .AddTo(this.Disposables);
            

            //Keyboard
            this.RegisterKeyReceiver(client);

            this.Catalog = new CatalogPageViewModel(this).AddTo(this.Disposables);
            this.Viewer = new ViewerPageViewModel(this).AddTo(this.Disposables);
            this.Search = new SearchPageViewModel(this).AddTo(this.Disposables);
        }

#if DEBUG
        ~ClientWindowViewModel()
        {
            System.Diagnostics.Debug.WriteLine("vm finalized");
        }
#endif


        /// <summary>
        /// キーボード操作を登録
        /// </summary>
        private void RegisterKeyReceiver(Client client)
        {
            var cursorFilter = this.KeyReceiver.AddPreFilter(x => !(x.FocusedControl is TextBox));

            //検索ページに移動
            this.KeyReceiver.Register(Key.F,
                (_, __) => client.MoveToSearch(),
                0, modifier: ModifierKeys.Control);


            //戻る・進む
            this.KeyReceiver.Register(Key.Left, (_, __) => client.Back(),
                0, modifier: ModifierKeys.Alt);
            this.KeyReceiver.Register(Key.Right, (_, __) => client.Forward(),
                0, modifier: ModifierKeys.Alt);

            this.KeyReceiver.Register(Key.Back, (_, __) => client.Back(), cursorFilter);
            this.KeyReceiver.Register(Key.BrowserBack, (_, __) => client.Back(), 0);
            this.KeyReceiver.Register(Key.BrowserForward, (_, __) => client.Forward(), 0);


            //ポップアップメニュー・ダイアログが開いているとき
            var isPopupOpenFilter = this.KeyReceiver
                .AddPreFilter(_ => this.IsPopupOpen.Value || this.IsPaneOpen.Value);

            //ポップアップを閉じる
            this.KeyReceiver.Register(Key.Escape, (t, key) => this.ClosePopupOrMenu(), isPopupOpenFilter);

            this.KeyReceiver.Register(Key.Escape,
                (t, key) => this.ClosePopup(), isPopupOpenFilter, (int)KeyReceiverMode.PopupIsOpened);


            this.KeyReceiver.Register(Key.Q,
                (_, __) => ((App)Application.Current).ExitAll(),
                0, modifier: ModifierKeys.Alt);

#if DEBUG
            this.KeyReceiver.Register(Key.NumPad5,
                (_, __) => { },
                0);//, modifier: ModifierKeys.Shift);
#endif

        }

        private void StartPathOrTagSearch(FileProperty property, SearchReferences reference)
        {
            if (this.Client.StartNewSearch(property, reference, CompareMode.Equal))
            {
                if (this.IsPaneOpen.Value && !this.IsPaneFixed.Value)
                {
                    this.IsPaneOpen.Value = false;
                }
            }
        }

        private void ShowInformationPane(bool open = false)
        {
            if (!this.IsPaneOpen.Value && !open)
            {
                return;
            }
            var tab = this.SelectedTab.Value;

            OptionPaneType pane;

            switch (tab)
            {
                case 0:
                    pane = OptionPaneType.NoInformation;
                    break;
                case 1:
                    pane = (this.SelectedItems.Count > 1) ? OptionPaneType.SelectedItems
                        : (this.SelectedItems.Count == 1) ? OptionPaneType.ItemInfo
                        : (this.Client.IsGroupMode.Value) ? OptionPaneType.ItemInfo
                        : (this.prevPaneSelected == OptionPaneType.None) ? OptionPaneType.None
                        : (!this.IsPaneOpen.Value && !open) ? OptionPaneType.None
                        : OptionPaneType.NoInformation;
                    break;
                case 2:
                case 3:
                    pane = OptionPaneType.ItemInfo;
                    break;
                default:
                    pane = OptionPaneType.NoInformation;
                    break;
            }

            this.SelectedInformationPage.Value = pane;

            if (open)
            {
                this.IsPaneOpen.Value = true;
            }
        }

        private bool IsInformationPane(OptionPaneType pane)
        {
            switch (pane)
            {
                case OptionPaneType.NoInformation:
                case OptionPaneType.ItemInfo:
                case OptionPaneType.SelectedItems:
                    return true;
                default:
                    return false;
            }
        }


        public void TogglePane(OptionPaneType type)
        {
            if (!this.IsPaneOpen.Value)
            {
                this.SelectedInformationPage.Value = type;
                this.IsPaneOpen.Value = true;
            }
            else
            {
                this.SelectedInformationPage.Value = OptionPaneType.None;
                this.IsPaneOpen.Value = false;
            }
        }

        public void ToggleInformationPane()
        {
            if (this.IsPaneOpen.Value && !this.IsPaneFixed.Value)
            {
                this.SelectedInformationPage.Value = OptionPaneType.None;
                this.IsPaneOpen.Value = false;
            }
            else
            {
                this.ShowInformationPane(true);
            }
        }


        private void ClosePopupOrMenu()
        {
            if (this.IsPopupOpen.Value)
            {
                this.IsPopupOpen.Value = false;
            }
            else
            {
                this.IsPaneOpen.Value = false;
            }
        }
        private void ClosePopup()
        {
            if (this.IsPopupOpen.Value)
            {
                this.IsPopupOpen.Value = false;
            }
        }


        /// <summary>
        /// タグ選択ダイアログ表示
        /// </summary>
        /// <param name="relativeControl"></param>
        public void ShowTagSelector(FrameworkElement relativeControl)
        {
            switch (this.Client.SelectedPage.Value)
            {
                case PageType.Catalog:
                    if (this.SelectedItems.Count <= 0 && this.SelectedRecord.Value == null)
                    {
                        return;
                    }
                    break;
                case PageType.Viewer:
                    if (this.SelectedRecord.Value == null)
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }

            var left
                = (relativeControl != null) ? 10.0
                : this.IsPaneOpen.Value ? (openPaneWidth + 10.0)
                : 10.0;

            var content = new TagSelector();

            if (this.Client.SelectedPage.Value == PageType.Catalog && this.SelectedItems.Count > 1)
            {
                content.TagSelectedCallBack += x => this.SelectedItems.AddTag(x);
            }
            else if (this.SelectedRecord.Value != null)
            {
                content.Target = this.SelectedRecord.Value;
            }
            else
            {
                return;
            }

            this.PopupOwner.PopupDialog.Show(content,
                new Thickness(left, double.NaN, double.NaN, double.NaN),
                relativeControl == null ? HorizontalAlignment.Left : HorizontalAlignment.Right,
                VerticalAlignment.Center, relativeControl);
        }
    }


    public enum PaneMode
    {
        AlwaysVisible,
        HideInClosing,
        Disabled
    }

    public enum OptionPaneType
    {
        None = 0,
        NoInformation = 1,
        ItemInfo = 2,
        SelectedItems = 3,
        Setting = 4,
        KeyBind = 5,
        Help = 6,
    }

    public enum KeyReceiverMode
    {
        Normal = 0,
        PopupIsOpened = 1,
    }
}

