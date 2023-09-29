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
        public ReactiveCommandSlim<object?> RefreshLibraryCommand { get; }

        public ReadOnlyReactivePropertySlim<string?> LibraryRefreshStatus { get; }
        public ReadOnlyReactivePropertySlim<int> MaxCount { get; }
        public ReadOnlyReactivePropertySlim<int> CurrentCount { get; }

        public ReactivePropertySlim<string> Text { get; }
        

        public ReactiveCollection<LibraryUpdateHistoryItem> LibraryUpdateHistory 
            => this.core.LibraryUpdateHistory;

        private readonly ApplicationCore core;

        public LibraryCreationPageViewModel()
        {
            this.core = ((App)Application.Current).Core;
            var library = core.Library;

            this.Text = new ReactivePropertySlim<string>().AddTo(this.Disposables);
            

            this.MaxCount = library.FileEnumerated
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.CurrentCount = library.FileLoaded
                .Buffer(TimeSpan.FromMilliseconds(500))
                .Where(x => x.Count > 0)
                .Select(x => x.Last())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.LibraryRefreshStatus = library.Loading
                .ToReadOnlyReactivePropertySlim().AddTo(this.Disposables);


            library.Loaded
                .Where(x => x != null)
                .ObserveOnUIDispatcher()
                .Subscribe(x =>
                {
                    this.Text.Value = $"Added:{x.AddedFiles.Count}"
                    + $", Removed:{x.RemovedFiles.Count}, Updated:{x.UpdatedFiles.Count}";
                })
                .AddTo(this.Disposables);


            this.RefreshLibraryCommand = library.IsCreating
                .Select(x => !x)
                .ToReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    this.Text.Value = "processing";
                    library.StartRefreshLibrary();
                })
                .AddTo(this.Disposables);
            
        }
    }
}
