using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Database.Search;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using ImageLibrary.Viewer;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.Models.Utility;
using ShibugakiViewer.Views.Controls;
using ShibugakiViewer.Views.Windows;
using WpfTools;
using WpfTools.Controls;

namespace ShibugakiViewer.ViewModels
{
    public class ClientWindowViewModel : NotificationBase
    {

        private const double middleWindowWidth = 720.0;
        private const double wideWindowWidth = 970.0;


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

        //public SlideshowPageViewModel Slideshow
        //{
        //    get { return _fieldSlideshow; }
        //    set
        //    {
        //        if (_fieldSlideshow != value)
        //        {
        //            _fieldSlideshow = value;
        //            RaisePropertyChanged(nameof(Slideshow));
        //        }
        //    }
        //}
        //private SlideshowPageViewModel _fieldSlideshow;


        public ReactiveProperty<int> SelectedTab { get; }

        public ReactiveCommand BackCommand { get; }
        public ReactiveCommand ChangePageCommand { get; }
        public ReactiveCommand OpenPaneCommand { get; }
        public ReactiveCommand OpenInformationPaneCommand { get; }
        public ReactiveCommand OpenSettingPaneCommand { get; }
        public ReactiveCommand OpenSettingWindowCommand { get; }
        public ReactiveCommand FileDropCommand { get; }

        public ReactiveCommand MouseExButtonLeftCommand { get; }
        public ReactiveCommand MouseExButtonRightCommand { get; }
        private Subject<bool> MouseExButtonSubject { get; }
        public IObservable<bool> MouseExButtonPressed => this.MouseExButtonSubject.AsObservable();

        public ReactiveProperty<string> PaneSelectedPath { get; }
        public ReactiveProperty<TagInformation> PaneSelectedTag { get; }


        public ReactiveProperty<bool> IsOptionPageOpen { get; }
        public ReactiveProperty<bool> IsPaneOpen { get; }
        public ReactiveProperty<bool> IsInformationPaneOpen { get; }
        public ReactiveProperty<bool> IsPaneFixed { get; }
        public ReactiveProperty<bool> IsSettingPaneOpen { get; }

        public ReactiveProperty<double> FrameWidth { get; }

        public Subject<SplitViewDisplayMode> PageChangedSubject { get; }
        public ReactiveProperty<SplitViewDisplayMode> PaneDisplayMode { get; }

        public ReactiveProperty<double> JumpListWidth { get; }

        public ReactiveProperty<OptionPaneType> SelectedInformationPage { get; }

        //public ReactiveProperty<SolidColorBrush> InformationToggleBackgroud { get; }
        //public ReactiveProperty<SolidColorBrush> SettingToggleBackgroud { get; }

        public ReactiveCommand<string> OptionPageCommand { get; }

        private ReactiveProperty<PaneMode> DefaultPaneMode { get; }
        //public ReactiveProperty<bool> IsMenuAlwaysVisible { get; set; }

        public ReactiveProperty<Visibility> OptionPaneVisibility { get; }
        public ReactiveProperty<Visibility> PaneOpenButtonVisibility { get; }
        public ReactiveProperty<Visibility> PaneFixButtonVisibility { get; }

        public ReactiveProperty<bool> IsPopupOpen { get; }
        ////public ReactiveCommand PopupCloseCommand { get; }
        //public ReactiveProperty<FrameworkElement> PopupContent { get; }
        //public ReactiveProperty<FrameworkElement> PopupDockControl { get; }
        //public ReactiveProperty<Thickness> PopupPosition { get; }
        //public ReactiveProperty<HorizontalAlignment> PopupHorizontalAlignment { get; }
        //public ReactiveProperty<VerticalAlignment> PopupVerticalAlignment { get; }
        ////public ReactiveProperty<Brush> PopupMask { get; }
        //public ReactiveProperty<bool> IsPopupMaskEnabled { get; }

        //private Brush maskBrush;
        //private Brush transparentBrush = new SolidColorBrush(Colors.Transparent);

        public ReadOnlyReactiveProperty<Record> SelectedRecord => this.Client.SelectedRecord;
        public ReadOnlyReactiveProperty<string> WindowTitle { get; }

        public double TagSelectorScrollOffset { get; set; }

        public KeyReceiver<object> KeyReceiver { get; }

        public Window View { get; set; }
        public IPopupDialogOwner PopupOwner { get; set; }

        public Client Client { get; }
        public ApplicationCore Core { get; }
        public Library Library => this.Core.Library;
        public SelectionManager SelectedItems => this.Client.SelectedItems;

        private SplitViewDisplayMode prevPaneMode;
        private OptionPaneType prevPaneSelected;
        private bool prevPaneOpen;



        public ClientWindowViewModel()
        {
            var core = ((App)Application.Current).Core;
            var library = core.Library;

            var client = new Client(new LibraryFront(library), core).AddTo(this.Disposables);
            this.Client = client;
            this.Core = core;

            this.KeyReceiver = new KeyReceiver<object>().AddTo(this.Disposables);

            this.WindowTitle = client.SelectedPage
                .CombineLatest(client.ViewerDisplaying, (Page, Item) => new { Page, Item })
                .Select(x =>
                {
                    var file = (x.Page == PageType.Viewer || x.Page == PageType.Slideshow)
                        ? x.Item?.FileName : null;
                    return (file == null) ? core.AppName : (file + " - " + core.AppName);
                })
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.SelectedInformationPage = new ReactiveProperty<OptionPaneType>
                (core.IsViewerPageLeftBarFixed ? OptionPaneType.ItemInfo : OptionPaneType.None)
                .AddTo(this.Disposables);

            this.IsPaneOpen = new ReactiveProperty<bool>(core.IsViewerPageLeftBarFixed)
                .AddTo(this.Disposables);


            this.IsPaneFixed = core
                .ToReactivePropertyAsSynchronized(x => x.IsViewerPageLeftBarFixed)
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
                        //case PageType.Slideshow:
                        //    return 3;
                        default:
                            return 0;
                    }
                })
                .ToReactiveProperty(0)
                .AddTo(this.Disposables);


            this.SelectedTab.Subscribe(x =>
            {
                switch (x)
                {
                    case 0:
                        client.MoveToPage(PageType.Search);
                        break;
                    case 1:
                        client.MoveToPage(PageType.Catalog);
                        break;
                    case 2:
                        client.MoveToPage(PageType.Viewer);
                        break;
                    //case 3:
                    //    client.MoveToPage(PageType.Slideshow);
                    //    break;
                }
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


            this.PaneSelectedPath = new ReactiveProperty<string>((string)null)
                .AddTo(this.Disposables);
            this.PaneSelectedPath.Where(x => !string.IsNullOrWhiteSpace(x))
                .Subscribe(x => this.Client.StartNewSearch
                    (FileProperty.DirectoryPathStartsWith, x, CompareMode.Equal))
                .AddTo(this.Disposables);

            this.PaneSelectedTag = new ReactiveProperty<TagInformation>((TagInformation)null)
                .AddTo(this.Disposables);
            this.PaneSelectedTag.Where(x => x != null)
                .Subscribe(x => this.Client.StartNewSearch
                    (FileProperty.ContainsTag, x.Id, CompareMode.Equal))
                .AddTo(this.Disposables);

            this.DefaultPaneMode = client.SelectedPage
                .Select(x => (x == PageType.Slideshow) ? PaneMode.Disabled
                    : (x == PageType.Viewer) ? PaneMode.HideInClosing
                    : PaneMode.AlwaysVisible)
                .ToReactiveProperty(PaneMode.AlwaysVisible)
                .AddTo(this.Disposables);

            var compactPaneWidth = 48.0;// (double)Application.Current.Resources["CompactPaneWidth"];
            var openPaneWidth = 320.0;// (double)Application.Current.Resources["OpenPaneWidth"];




            this.IsOptionPageOpen = this.SelectedInformationPage
                .Select(x => x > 0)
                .ToReactiveProperty()
                .AddTo(this.Disposables);


            //情報
            this.IsInformationPaneOpen = this.SelectedInformationPage
                .Select(x => this.IsInformationPane(x))
                .ToReactiveProperty(false)
                .AddTo(this.Disposables);

            this.OpenInformationPaneCommand = new ReactiveCommand()
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
                }, this.Disposables);

            //this.IsInformationPaneOpen
            //    .Subscribe(x =>
            //    {
            //        if (x)
            //        {
            //            if (this.prevPaneSelected == OptionPaneType.None)
            //            {
            //                this.prevPaneSelected = OptionPaneType.NoInformation;
            //            }
            //            this.ShowInformationPane();
            //            this.IsPaneOpen.Value = true;
            //        }
            //        else
            //        {
            //            if (this.SelectedInformationPage.Value != OptionPaneType.Setting)
            //            {
            //                this.SelectedInformationPage.Value = OptionPaneType.None;
            //            }
            //        }
            //    })
            //    .AddTo(this.Disposables);


            //設定
            this.IsSettingPaneOpen = this.SelectedInformationPage
                .Select(x => x == OptionPaneType.Setting)
                .ToReactiveProperty(false)
                .AddTo(this.Disposables);

            this.OpenSettingPaneCommand = new ReactiveCommand()
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
                }, this.Disposables);

            //this.IsSettingPaneOpen
            //    .Subscribe(x =>
            //    {
            //        if (x)
            //        {
            //            this.SelectedInformationPage.Value = OptionPaneType.Setting;
            //            this.IsPaneOpen.Value = true;
            //        }
            //        else
            //        {
            //            if (this.SelectedInformationPage.Value == OptionPaneType.Setting)
            //            {
            //                this.SelectedInformationPage.Value = OptionPaneType.None;
            //            }
            //        }
            //    })
            //    .AddTo(this.Disposables);






            this.OptionPaneVisibility = this.SelectedInformationPage
                .Select(x => VisibilityHelper.Set(x > 0))
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.FrameWidth = new ReactiveProperty<double>(300).AddTo(this.Disposables);



            this.PageChangedSubject = new Subject<SplitViewDisplayMode>().AddTo(this.Disposables);

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


            this.PaneOpenButtonVisibility = new ReactiveProperty<Visibility>(Visibility.Collapsed)
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            var isWide = this.FrameWidth.Select(y => y > middleWindowWidth).Publish().RefCount();
            this.PaneFixButtonVisibility = isWide.Select(y => VisibilityHelper.Set(y))
                .ToReactiveProperty().AddTo(this.Disposables);
            isWide.Where(y => !y).Skip(2).Subscribe(y => this.IsPaneFixed.Value = false).AddTo(this.Disposables);

            this.JumpListWidth = this.IsOptionPageOpen.Select(x => x ? compactPaneWidth : openPaneWidth)
                .ToReactiveProperty().AddTo(this.Disposables);

            this.IsOptionPageOpen.Subscribe(x =>
            {
                if (x && this.SelectedInformationPage.Value != OptionPaneType.NoInformation
                    && !core.IsAutoInformationPaneDisabled)
                {
                    this.IsPaneOpen.Value = true;
                }
            }).AddTo(this.Disposables);


            //this.InformationToggleBackgroud = this.SelectedInformationPage
            //    .Select(x => (x == (int)OptionPaneType.ItemInfo || x == (int)OptionPaneType.SelectedItems)
            //        ? new SolidColorBrush(Colors.DarkGray) : new SolidColorBrush(Colors.Transparent))
            //    .ToReactiveProperty().AddTo(this.Disposables);
            //
            //this.SettingToggleBackgroud = this.SelectedInformationPage
            //    .Select(x => (x == (int)OptionPaneType.Setting)
            //        ? new SolidColorBrush(Colors.DarkGray) : new SolidColorBrush(Colors.Transparent))
            //    .ToReactiveProperty().AddTo(this.Disposables);



            this.OpenSettingWindowCommand = new ReactiveCommand()
                .WithSubscribe(_ => ((App)Application.Current).ShowSettingWindow(), this.Disposables);

            this.OptionPageCommand = new ReactiveCommand<string>().AddTo(this.Disposables);

            /*
            this.OptionPageCommand.Subscribe(x =>
            {
                int id;
                if (!int.TryParse(x, out id))
                {
                    id = NormalPane;
                }

                if (id >= 0 && this.OptionPageSelected.Value != id)
                {
                    this.OptionPageSelected.Value = id;

                    switch (id)
                    {
                        case InformationPane:
                            var page = this.MainFrame.Content as IInformationPaneUser;
                            if (page != null)
                            {
                                page.SetInformation(this.OptionFrame);
                            }
                            else
                            {
                                this.ClearInformation();
                            }
                            break;
                        case SettingPane:
                            this.OptionFrame.Navigate(typeof(SettingsListPage));
                            break;
                    }

                    this.IsPaneOpen.Value = true;
                }
                else
                {
                    if (this.IsPaneOpen.Value)
                    {
                        this.OptionPageSelected.Value = NormalPane;
                    }
                    else
                    {
                        this.IsPaneOpen.Value = true;
                    }
                }
            }).AddTo(this.Disposables);
            */






            this.IsPopupOpen = new ReactiveProperty<bool>(false).AddTo(this.Disposables);

            this.IsPopupOpen
                .Subscribe(x => this.KeyReceiver.Mode
                    = (int)(x ? KeyReceiverMode.PopupIsOpened : KeyReceiverMode.Normal))
                .AddTo(this.Disposables);


            //this.PopupContent = new ReactiveProperty<FrameworkElement>().AddTo(this.Disposables);
            //this.PopupPosition = new ReactiveProperty<Thickness>().AddTo(this.Disposables);
            //this.PopupHorizontalAlignment
            //    = new ReactiveProperty<HorizontalAlignment>(HorizontalAlignment.Center)
            //    .AddTo(this.Disposables);
            //this.PopupVerticalAlignment
            //    = new ReactiveProperty<VerticalAlignment>(VerticalAlignment.Center)
            //    .AddTo(this.Disposables);
            //this.PopupDockControl = new ReactiveProperty<FrameworkElement>().AddTo(this.Disposables);
            ////this.PopupMask = new ReactiveProperty<Brush>().AddTo(this.Disposables);
            //this.IsPopupMaskEnabled = new ReactiveProperty<bool>().AddTo(this.Disposables);
            



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

                    //this.IsPaneOpen.Value = true;


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

                    //this.SelectedInformationPage.Value = this.prevPaneSelected;

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


            this.BackCommand = client.BackHistoryCount
                .Select(x => x > 0)
                .ToReactiveCommand()
                .WithSubscribe(_ => client.Back(), this.Disposables);

            this.ChangePageCommand = new ReactiveCommand()
                .WithSubscribe(x =>
                {
                    switch (x.ToString())
                    {
                        case "0":
                            client.MoveToPage(PageType.Search);//.MoveToSearch();
                            break;
                        case "1":
                            client.MoveToPage(PageType.Catalog);//.MoveToCatalog();
                            break;
                    }
                }, this.Disposables);

            this.OpenPaneCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.TogglePane(OptionPaneType.None), this.Disposables);

            // ウインドウへのファイルのドラッグ&ドロップ
            this.FileDropCommand = new ReactiveCommand()
                .WithSubscribe(obj =>
                {
                    var files = obj as string[];
                    if (files != null)
                    {
                        this.Client.ActivateFiles(files);
                    }
                }, this.Disposables);

            this.MouseExButtonSubject = new Subject<bool>().AddTo(this.Disposables);

            this.MouseExButtonLeftCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    if (core.UseExtendedMouseButtonsToSwitchImage
                        && (client.SelectedPage.Value == PageType.Viewer
                            || client.SelectedPage.Value == PageType.Slideshow))
                    {
                        this.MouseExButtonSubject.OnNext(false);
                    }
                    else
                    {
                        client.Back();
                    }
                }, this.Disposables);

            this.MouseExButtonRightCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    if (core.UseExtendedMouseButtonsToSwitchImage
                        && (client.SelectedPage.Value == PageType.Viewer
                            || client.SelectedPage.Value == PageType.Slideshow))
                    {
                        this.MouseExButtonSubject.OnNext(true);
                    }
                    else
                    {
                        client.Forward();
                    }
                }, this.Disposables);

            //Keyboard

            //検索ページに移動
            this.KeyReceiver.Register(Key.F,
                (_, __) => client.MoveToPage(PageType.Search),
                0, modifier: ModifierKeys.Control);


            //戻る・進む
            this.KeyReceiver.Register(Key.Left,
                (_, __) => client.Back(),
                0, modifier: ModifierKeys.Alt);
            this.KeyReceiver.Register(Key.Right,
                (_, __) => client.Forward(),
                0, modifier: ModifierKeys.Alt);

            this.KeyReceiver.Register(new[] { Key.Back, Key.BrowserBack },
                (_, __) => client.Back(), 0);
            this.KeyReceiver.Register(Key.BrowserForward,
                (_, __) => client.Forward(), 0);


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


            this.KeyReceiver.Register(Key.NumPad5,
                (_, __) => { },
                0);//, modifier: ModifierKeys.Shift);


            this.Catalog = new CatalogPageViewModel(this).AddTo(this.Disposables);
            this.Viewer = new ViewerPageViewModel(this).AddTo(this.Disposables);
            this.Search = new SearchPageViewModel(this).AddTo(this.Disposables);
            //this.Slideshow = new SlideshowPageViewModel(this).AddTo(this.Disposables);
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

        /*
        public void OpenPopup(FrameworkElement content, Thickness position,
            HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment,
            FrameworkElement dock = null, bool isMaskVisible = true)
        {
            //if (this.PopupOwner == null)
            //{
            //    return;
            //}
            //this.PopupOwner.OpenPopup
            //    (content, position, horizontalAlignment, verticalAlignment, dock, isMaskVisible);


            //if (this.maskBrush == null)
            //{
            //    var color = Application.Current.Resources["PopupMaskColor"];
            //    this.maskBrush = (Brush)color;
            //}
            //
            //this.PopupMask.Value = (isMaskVisible) ? this.maskBrush : this.transparentBrush;

            this.IsPopupMaskEnabled.Value = isMaskVisible;
            this.PopupDockControl.Value = dock;
            this.PopupHorizontalAlignment.Value = horizontalAlignment;
            this.PopupVerticalAlignment.Value = verticalAlignment;
            this.PopupPosition.Value = position;
            this.PopupContent.Value = content;
            this.IsPopupOpen.Value = true;
        }*/

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

        public void StartSlideshow()
        {
            var vm = new SlideshowPageViewModel(this);
            var window = new SlideshowWindow()
            {
                DataContext = vm,
                Owner = this.View,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            window.Show();
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
    }

    public enum KeyReceiverMode
    {
        Normal = 0,
        PopupIsOpened = 1,
    }
}

