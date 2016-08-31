using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class LibraryCreationPageViewModel : DisposableBase
    {
        public ReactiveCommand RefreshLibraryCommand { get; }

        public ReadOnlyReactiveProperty<string> LibraryRefreshStatus { get; }
        public ReadOnlyReactiveProperty<int> MaxCount { get; }
        public ReadOnlyReactiveProperty<int> CurrentCount { get; }

        public ReactiveProperty<string> Text { get; }

        //public ObservableCollection<string> Added { get; }
        //public ObservableCollection<string> Removed { get; }
        //public ObservableCollection<string> Updated { get; }

        public ReactiveCollection<LibraryUpdateHistoryItem> LibraryUpdateHistory 
            => this.core.LibraryUpdateHistory;

        private readonly ApplicationCore core;

        public LibraryCreationPageViewModel()
        {
            this.core = ((App)Application.Current).Core;
            var library = core.Library;

            this.Text = new ReactiveProperty<string>().AddTo(this.Disposables);

            //this.Added = new ObservableCollection<string>();
            //this.Removed = new ObservableCollection<string>();
            //this.Updated = new ObservableCollection<string>();

            this.MaxCount = library.FileEnumerated
                //.Buffer(TimeSpan.FromMilliseconds(500))
                //.Where(x => x.Count > 0)
                //.Select(x => x.Last())
                //.ObserveOnUIDispatcher()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.CurrentCount = library.FileLoaded
                .Buffer(TimeSpan.FromMilliseconds(500))
                .Where(x => x.Count > 0)
                .Select(x => x.Last())
                //.ObserveOnUIDispatcher()
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.LibraryRefreshStatus = library.Loading
                .ToReadOnlyReactiveProperty().AddTo(this.Disposables);


            library.Loaded
                .Where(x => x != null)
                .ObserveOnUIDispatcher()
                .Subscribe(x =>
                {
                    this.Text.Value = $"Added:{x.AddedFiles.Count}"
                    + $", Removed:{x.RemovedFiles.Count}, Updated:{x.UpdatedFiles.Count}";

                    //this.Added.Clear();
                    //x.AddedFiles.Take(200).Select(y => y.Value.FullPath).OrderBy(y => y)
                    //    .ForEach(y => this.Added.Add(y));
                    //
                    //this.Removed.Clear();
                    //x.RemovedFiles.Take(200).Select(y => y.Value.FullPath).OrderBy(y => y)
                    //    .ForEach(y => this.Removed.Add(y));
                    //
                    //this.Updated.Clear();
                    //x.UpdatedFiles.Take(200).Select(y => y.Value.FullPath).OrderBy(y => y)
                    //    .ForEach(y => this.Updated.Add(y));

                })
                .AddTo(this.Disposables);


            this.RefreshLibraryCommand = library.IsCreating
                .Select(x => !x)
                .ToReactiveCommand()
                .WithSubscribe(_ =>
                {
                    this.Text.Value = "processing";
                    library.StartRefreshLibrary();
                }, this.Disposables);
            
        }
    }
}
