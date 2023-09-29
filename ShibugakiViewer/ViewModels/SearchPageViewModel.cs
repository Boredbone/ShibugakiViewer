using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Boredbone.XamlTools;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.Search;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;
using ShibugakiViewer.ViewModels.Controls;
using ShibugakiViewer.Views.Controls;
using ShibugakiViewer.Views.Windows;
using WpfTools;

namespace ShibugakiViewer.ViewModels
{
    public class SearchPageViewModel : DisposableBase
    {
        public ReactiveProperty<SearchInformation> CurrentSearch { get; }

        public ReactiveCommandSlim StartSearchCommand { get; }
        public ReactiveCommandSlim AddCriteriaCommand { get; }
        public ReactiveCommandSlim AddToFavoriteCommand { get; }
        public ReactiveCommandSlim SwitchModeCommand { get; }
        public ReactiveCommandSlim<ISqlSearch> ItemClickCommand { get; }
        public ReactiveCommandSlim NewSearchCommand { get; }

        public ReactivePropertySlim<bool> IsEditing { get; }
        public ReadOnlyReactivePropertySlim<bool> IsThumbnailVisible { get; }

        public ReactiveCommandSlim SelectHistoryCommand { get; }
        public ReactiveCommandSlim SelectFavoriteCommand { get; }
        public ReactiveCommandSlim<SearchInformation> ClickHistoryCommand { get; }
        public ReactiveCommandSlim<SearchInformation> ClickFavoriteCommand { get; }
        public ReactiveCommandSlim ShowHistoryCommand { get; }
        public ReactiveCommandSlim ShowFavoriteCommand { get; }

        public ReactiveCommandSlim UpFavoriteCommand { get; }
        public ReactiveCommandSlim DownFavoriteCommand { get; }


        public ReadOnlyReactiveCollection<SearchInformation> HistoryList { get; }
        public ReadOnlyReactiveCollection<SearchInformation> FavoriteList { get; }


        public ReactivePropertySlim<TabMode> SelectedTab { get; }
        public ReadOnlyReactivePropertySlim<int> CurrentSearchType { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFavoriteSearch { get; }
        public ReadOnlyReactivePropertySlim<Visibility> IsFavoriteSearchSortVisible { get; }

        private readonly ClientWindowViewModel parent;


        public SearchPageViewModel(ClientWindowViewModel parent)
        {
            this.parent = parent;
            var client = parent.Client;
            var library = parent.Library;
            var core = parent.Core;

            var searcher = library.Searcher;

            this.HistoryList = searcher.SearchHistory.ToReadOnlyReactiveCollection().AddTo(this.Disposables);
            this.FavoriteList = searcher.FavoriteSearchList.ToReadOnlyReactiveCollection().AddTo(this.Disposables);

            this.SelectedTab = new ReactivePropertySlim<TabMode>
                ((searcher.FavoriteSearchList.Count > 0 && core.LastSearchedFavorite)
                    ? TabMode.Favorite : TabMode.History)
                .AddTo(this.Disposables);

            this.SelectedTab
                .Subscribe(x => core.LastSearchedFavorite = (x == TabMode.Favorite))
                .AddTo(this.Disposables);

            this.IsEditing = new ReactivePropertySlim<bool>(false).AddTo(this.Disposables);


            this.SelectHistoryCommand = new ReactiveCommandSlim().AddTo(this.Disposables);
            this.SelectFavoriteCommand = new ReactiveCommandSlim().AddTo(this.Disposables);

            var hitoryItem = this.SelectHistoryCommand
                .OfType<SearchInformation>()
                .Select(x => x.Clone());

            var favoriteItem = this.SelectFavoriteCommand
                .OfType<SearchInformation>();

            this.CurrentSearch = Observable
                .Merge(hitoryItem, favoriteItem)//, newItem)
                .ToReactiveProperty(SearchInformation.GenerateEmpty())
                .AddTo(this.Disposables);

            this.CurrentSearch
                .Subscribe(x => this.IsEditing.Value = (x != null && !x.Key.IsNullOrEmpty()))
                .AddTo(this.Disposables);

            this.IsThumbnailVisible = this.CurrentSearch
                .Select(x => x.DateLastUsed > default(DateTimeOffset))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            searcher.FavoriteSearchList
                .ObserveAddChanged()
                .Subscribe(x =>
                {
                    if (this.CurrentSearch.Value == x)
                    {
                        this.SelectedTab.Value = TabMode.Favorite;
                    }
                })
                .AddTo(this.Disposables);

            searcher.FavoriteSearchList
                .ObserveRemoveChanged()
                .Subscribe(x =>
                {
                    if (this.CurrentSearch.Value == x && this.SelectedTab.Value == TabMode.Favorite)
                    {
                        this.CurrentSearch.Value = SearchInformation.GenerateEmpty();
                    }
                })
                .AddTo(this.Disposables);

            this.CurrentSearchType = this.HistoryList
                .CollectionChangedAsObservable()
                .Merge(this.FavoriteList.CollectionChangedAsObservable())
                .Select(_ => this.CurrentSearch.Value)
                .Merge(this.CurrentSearch)
                .Select(x => this.HasFavoriteSearch(x) ? 1
                    : this.HasHistorySearch(x) ? 0 : 2)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IsFavoriteSearch = this.CurrentSearchType
                .Select(x => x == 1)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IsFavoriteSearchSortVisible = this.IsFavoriteSearch
                .CombineLatest(this.SelectedTab, (a, b) => a && b == TabMode.Favorite)
                .Select(x => VisibilityHelper.Set(x))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);



            this.ClickHistoryCommand = new ReactiveCommandSlim<SearchInformation>()
                .WithSubscribe(x =>
                {
                    if (x == null)
                    {
                        return;
                    }
                    if (!this.IsEditing.Value
                        || this.CurrentSearch.Value == null
                        || this.CurrentSearch.Value.Key.IsNullOrEmpty()
                        || this.CurrentSearch.Value.Key.Equals(x.Key))
                    {
                        this.StartSearch(client, x);
                    }
                    else
                    {
                        this.SelectHistoryCommand.Execute(x);
                    }
                })
                .AddTo(this.Disposables);

            this.ClickFavoriteCommand = new ReactiveCommandSlim<SearchInformation>()
                .WithSubscribe(x =>
                {
                    if (x == null)
                    {
                        return;
                    }
                    if (!this.IsEditing.Value
                        || this.CurrentSearch.Value == null
                        || this.CurrentSearch.Value.Key.IsNullOrEmpty()
                        || this.CurrentSearch.Value.Key.Equals(x.Key))
                    {
                        this.StartSearch(client, x);
                    }
                    else
                    {
                        this.SelectFavoriteCommand.Execute(x);
                    }
                })
                .AddTo(this.Disposables);

            this.StartSearchCommand = new ReactiveCommandSlim()
                .WithSubscribe(() => this.StartSearch(client, this.CurrentSearch.Value))
                .AddTo(this.Disposables);

            this.AddCriteriaCommand = new ReactiveCommandSlim()
                .WithSubscribe(() => this.EditSearch(this.CurrentSearch.Value.Root))
                .AddTo(this.Disposables);

            this.ItemClickCommand = new ReactiveCommandSlim<ISqlSearch>()
                .WithSubscribe(search =>
                {
                    if (search != null)
                    {
                        this.EditSearch(search);
                    }
                })
                .AddTo(this.Disposables);

            this.AddToFavoriteCommand = new ReactiveCommandSlim()
                .WithSubscribe(() =>
                {
                    var item = this.CurrentSearch.Value;
                    if (item == null)
                    {
                        return;
                    }

                    if (this.HasFavoriteSearch(item))
                    {
                        searcher.MarkSearchUnfavorite(item);
                    }
                    else
                    {
                        searcher.MarkSearchFavorite(item);
                    }

                })
                .AddTo(this.Disposables);


            this.SwitchModeCommand = new ReactiveCommandSlim()
                .WithSubscribe(() =>
                {
                    var item = this.CurrentSearch.Value;
                    if (item == null)
                    {
                        return;
                    }

                    item.Root.IsOr = !item.Root.IsOr;
                })
                .AddTo(this.Disposables);

            this.NewSearchCommand = new ReactiveCommandSlim()
                .WithSubscribe
                (() => this.CurrentSearch.Value = SearchInformation.GenerateEmpty())
                .AddTo(this.Disposables);

            this.ShowFavoriteCommand = new ReactiveCommandSlim()
                .WithSubscribe
                (() => this.SelectedTab.Value = TabMode.Favorite)
                .AddTo(this.Disposables);
            this.ShowHistoryCommand = new ReactiveCommandSlim()
                .WithSubscribe
                (() => this.SelectedTab.Value = TabMode.History)
                .AddTo(this.Disposables);

            this.UpFavoriteCommand = new ReactiveCommandSlim()
                .WithSubscribe(() =>
                {
                    if (this.IsFavoriteSearch.Value)
                    {
                        searcher.MoveFavoriteItem(this.CurrentSearch.Value, -1, IsCtrlOrShiftKeyPressed());
                    }
                })
                .AddTo(this.Disposables);
            this.DownFavoriteCommand = new ReactiveCommandSlim()
                .WithSubscribe(() =>
                {
                    if (this.IsFavoriteSearch.Value)
                    {
                        searcher.MoveFavoriteItem(this.CurrentSearch.Value, 1, IsCtrlOrShiftKeyPressed());
                    }
                })
                .AddTo(this.Disposables);
        }

        private bool HasFavoriteSearch(SearchInformation search)
            => (search != null && search.Key != null && this.FavoriteList != null
                && this.FavoriteList.Any(y => search.Key.Equals(y?.Key)));

        private bool HasHistorySearch(SearchInformation search)
            => (search != null && search.Key != null && this.HistoryList != null
                && this.HistoryList.Any(y => search.Key.Equals(y?.Key)));

        private void EditSearch(ISqlSearch search)
        {
            var editor = new SearchSettingEditor()
            {
                DataContext = new EditSearchViewModel(search),
            };

            this.parent.PopupOwner.PopupDialog.Show(editor, default(Thickness),
                HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private void StartSearch(Client client, SearchInformation info)
        {
            client.SetNewSearch(info);
            client.MoveToCatalog();

            this.CurrentSearch.Value = SearchInformation.GenerateEmpty();
            if (!this.HasFavoriteSearch(info))
            {
                this.SelectedTab.Value = TabMode.History;
            }
        }
        private static bool IsCtrlOrShiftKeyPressed()
        {
            return (Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) == KeyStates.Down ||
                 (Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) == KeyStates.Down ||
                 (Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) == KeyStates.Down ||
                 (Keyboard.GetKeyStates(Key.RightCtrl) & KeyStates.Down) == KeyStates.Down;
        }

    }
    public enum TabMode
    {
        History = 0,
        Favorite = 1,
    }
}
