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
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
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

        public ReactiveCommand StartSearchCommand { get; }
        public ReactiveCommand AddCriteriaCommand { get; }
        public ReactiveCommand AddToFavoriteCommand { get; }
        public ReactiveCommand SwitchModeCommand { get; }
        public ReactiveCommand ItemClickCommand { get; }
        public ReactiveCommand NewSearchCommand { get; }

        public ReactiveProperty<bool> IsEditing { get; }
        public ReadOnlyReactiveProperty<bool> IsThumbnailVisible { get; }

        public ReactiveCommand SelectHistoryCommand { get; }
        public ReactiveCommand SelectFavoriteCommand { get; }
        public ReactiveCommand ClickHistoryCommand { get; }
        public ReactiveCommand ClickFavoriteCommand { get; }
        public ReactiveCommand ShowHistoryCommand { get; }
        public ReactiveCommand ShowFavoriteCommand { get; }

        public ReadOnlyReactiveCollection<SearchInformation> HistoryList { get; }
        public ReadOnlyReactiveCollection<SearchInformation> FavoriteList { get; }


        public ReactiveProperty<TabMode> SelectedTab { get; }
        public ReadOnlyReactiveProperty<int> CurrentSearchType { get; }
        public ReadOnlyReactiveProperty<bool> IsFavoriteSearch { get; }

        //private Subject<EditSearchViewModel> EditSearchRequestSubject { get; }
        //public IObservable<EditSearchViewModel> EditSearchRequest => this.EditSearchRequestSubject.AsObservable();

        public Window View { get; set; }

        //private SearchSortManager Searcher { get; }

        private readonly ClientWindowViewModel parent;
        private readonly Client client;
        private readonly Library library;


        public SearchPageViewModel(ClientWindowViewModel parent)
        {
            this.parent = parent;
            this.client = parent.Client;
            this.library = parent.Library;
            var core = parent.Core;

            var searcher = library.Searcher;

            this.HistoryList = searcher.SearchHistory.ToReadOnlyReactiveCollection().AddTo(this.Disposables);
            this.FavoriteList = searcher.FavoriteSearchList.ToReadOnlyReactiveCollection().AddTo(this.Disposables);

            this.SelectedTab = new ReactiveProperty<TabMode>
                ((searcher.FavoriteSearchList.Count > 0 && core.LastSearchedFavorite)
                    ? TabMode.Favorite : TabMode.History)
                .AddTo(this.Disposables);

            this.SelectedTab
                .Subscribe(x => core.LastSearchedFavorite = (x == TabMode.Favorite))
                .AddTo(this.Disposables);

            this.IsEditing = new ReactiveProperty<bool>(false).AddTo(this.Disposables);


            //this.SelectedTab.Where(x => x == TabMode.New)
            //    .Subscribe(_ => this.IsEditing.Value = true).AddTo(this.Disposables);

            this.SelectHistoryCommand = new ReactiveCommand().AddTo(this.Disposables);
            this.SelectFavoriteCommand = new ReactiveCommand().AddTo(this.Disposables);

            var hitoryItem = this.SelectHistoryCommand
                .OfType<SearchInformation>()
                .Select(x => x.Clone());

            var favoriteItem = this.SelectFavoriteCommand
                .OfType<SearchInformation>();

            //var newItem = this.SelectedTab
            //    .Where(x => x == TabMode.New)
            //    .Select(_ => SearchInformation.GenerateEmpty());

            this.CurrentSearch = Observable
                .Merge(hitoryItem, favoriteItem)//, newItem)
                .ToReactiveProperty(SearchInformation.GenerateEmpty())
                .AddTo(this.Disposables);

            this.CurrentSearch
                .Subscribe(x => this.IsEditing.Value = (x != null && !x.Key.IsNullOrEmpty()))
                .AddTo(this.Disposables);

            this.IsThumbnailVisible = this.CurrentSearch
                .Select(x => x.DateLastUsed > default(DateTimeOffset))
                .ToReadOnlyReactiveProperty()
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
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.IsFavoriteSearch = this.CurrentSearchType
                .Select(x => x == 1)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            //this.IsFavoriteSearch = this.HistoryList
            //    .CollectionChangedAsObservable()
            //    .Merge(this.FavoriteList.CollectionChangedAsObservable())
            //    .Select(_ => Unit.Default)
            //    .CombineLatest(this.CurrentSearch, (l, c) => c)
            //    .Select(x => this.HasFavoriteSearch(x))
            //    .ToReadOnlyReactiveProperty()
            //    .AddTo(this.Disposables);

            /*
            this.IsFavoritePage = this.SelectedTab
                .Select(x => x == (int)TabMode.Favorite)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);*/


            this.ClickHistoryCommand = new ReactiveCommand().AddTo(this.Disposables);
            this.ClickFavoriteCommand = new ReactiveCommand().AddTo(this.Disposables);

            this.ClickHistoryCommand
                .Select(x => x as SearchInformation)
                .Where(x => x != null)
                .Subscribe(x =>
                {
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

            this.ClickFavoriteCommand
                .Select(x => x as SearchInformation)
                .Where(x => x != null)
                .Subscribe(x =>
                {
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

            //this.CurrentSearch = client.HistoryChanged
            //    .Where(x => x != null && x.Search != null)
            //    .Select(x => x.Search.Clone())
            //    .ToReactiveProperty(client.CurrentState.Search.Clone())
            //    .AddTo(this.Disposables);

            this.StartSearchCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.StartSearch(client, this.CurrentSearch.Value), this.Disposables);

            this.AddCriteriaCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.EditSearch(this.CurrentSearch.Value.Root), this.Disposables);

            this.ItemClickCommand = new ReactiveCommand()
                .WithSubscribe(item =>
                {
                    var search = item as ISqlSearch;

                    if (search != null)
                    {
                        this.EditSearch(search);
                    }

                }, this.Disposables);

            this.AddToFavoriteCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    var item = this.CurrentSearch.Value;// x as SearchInformation;
                    if (item == null)
                    {
                        return;
                    }

                    if (this.HasFavoriteSearch(item))//(TabMode)this.SelectedTab.Value == TabMode.Favorite)
                    {
                        searcher.MarkSearchUnfavorite(item);
                    }
                    else
                    {
                        searcher.MarkSearchFavorite(item);
                    }

                }, this.Disposables);


            this.SwitchModeCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    var item = this.CurrentSearch.Value;// x as SearchInformation;
                    if (item == null)
                    {
                        return;
                    }

                    item.Root.IsOr = !item.Root.IsOr;
                }, this.Disposables);

            this.NewSearchCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.CurrentSearch.Value = SearchInformation.GenerateEmpty(),
                    this.Disposables);

            this.ShowFavoriteCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.SelectedTab.Value = TabMode.Favorite, this.Disposables);
            this.ShowHistoryCommand = new ReactiveCommand()
                .WithSubscribe(_ => this.SelectedTab.Value = TabMode.History, this.Disposables);
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
            //this.library.Searcher.FavoriteSearchList.Insert(0, info);
            //return;

            client.SetNewSearch(info);
            client.MoveToCatalog();

            this.CurrentSearch.Value = SearchInformation.GenerateEmpty();
            if (!this.HasFavoriteSearch(info))//(this.SelectedTab.Value == TabMode.New)
            {
                this.SelectedTab.Value = TabMode.History;
            }
        }

    }
    public enum TabMode
    {
        History = 0,
        Favorite = 1,
        //New = 2,
    }
}
