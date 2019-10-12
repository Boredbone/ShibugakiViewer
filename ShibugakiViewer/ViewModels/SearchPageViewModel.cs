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

        public DelegateCommand StartSearchCommand { get; }
        public DelegateCommand AddCriteriaCommand { get; }
        public DelegateCommand AddToFavoriteCommand { get; }
        public DelegateCommand SwitchModeCommand { get; }
        public DelegateCommand<ISqlSearch> ItemClickCommand { get; }
        public DelegateCommand NewSearchCommand { get; }

        public ReactivePropertySlim<bool> IsEditing { get; }
        public ReadOnlyReactivePropertySlim<bool> IsThumbnailVisible { get; }

        public ReactiveCommand SelectHistoryCommand { get; }
        public ReactiveCommand SelectFavoriteCommand { get; }
        public DelegateCommand<SearchInformation> ClickHistoryCommand { get; }
        public DelegateCommand<SearchInformation> ClickFavoriteCommand { get; }
        public DelegateCommand ShowHistoryCommand { get; }
        public DelegateCommand ShowFavoriteCommand { get; }

        public DelegateCommand UpFavoriteCommand { get; }
        public DelegateCommand DownFavoriteCommand { get; }


        public ReadOnlyReactiveCollection<SearchInformation> HistoryList { get; }
        public ReadOnlyReactiveCollection<SearchInformation> FavoriteList { get; }


        public ReactivePropertySlim<TabMode> SelectedTab { get; }
        public ReadOnlyReactivePropertySlim<int> CurrentSearchType { get; }
        public ReadOnlyReactivePropertySlim<bool> IsFavoriteSearch { get; }
                
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

            
            this.SelectHistoryCommand = new ReactiveCommand().AddTo(this.Disposables);
            this.SelectFavoriteCommand = new ReactiveCommand().AddTo(this.Disposables);

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
            

            this.ClickHistoryCommand = new DelegateCommand<SearchInformation>(x =>
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
            });

            this.ClickFavoriteCommand = new DelegateCommand<SearchInformation>(x =>
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
            });

            this.StartSearchCommand = new DelegateCommand
                (() => this.StartSearch(client, this.CurrentSearch.Value));

            this.AddCriteriaCommand = new DelegateCommand
                (() => this.EditSearch(this.CurrentSearch.Value.Root));

            this.ItemClickCommand = new DelegateCommand<ISqlSearch>(search =>
            {
                if (search != null)
                {
                    this.EditSearch(search);
                }
            });

            this.AddToFavoriteCommand = new DelegateCommand(() =>
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

                });


            this.SwitchModeCommand = new DelegateCommand(()=>
                {
                    var item = this.CurrentSearch.Value;
                    if (item == null)
                    {
                        return;
                    }

                    item.Root.IsOr = !item.Root.IsOr;
                });

            this.NewSearchCommand = new DelegateCommand
                (() => this.CurrentSearch.Value = SearchInformation.GenerateEmpty());

            this.ShowFavoriteCommand = new DelegateCommand
                (() => this.SelectedTab.Value = TabMode.Favorite);
            this.ShowHistoryCommand = new DelegateCommand
                (() => this.SelectedTab.Value = TabMode.History);

            this.UpFavoriteCommand = new DelegateCommand(() =>
            {
                if (this.IsFavoriteSearch.Value)
                {
                    searcher.MoveFavoriteItem(this.CurrentSearch.Value, -1, IsCtrlOrShiftKeyPressed());
                }
            });
            this.DownFavoriteCommand = new DelegateCommand(() =>
            {
                if (this.IsFavoriteSearch.Value)
                {
                    searcher.MoveFavoriteItem(this.CurrentSearch.Value, 1, IsCtrlOrShiftKeyPressed());
                }
            });
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
