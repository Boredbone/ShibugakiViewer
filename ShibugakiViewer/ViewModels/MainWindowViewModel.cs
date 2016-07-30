using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Views.Windows;

namespace ShibugakiViewer.ViewModels
{
    class MainWindowViewModel : DisposableBase
    {
        public ReactiveCommand RefreshLibraryCommand { get; }
        public ReactiveCommand GenerateNewClientCommand { get; }
        public ReactiveCommand ConvertCommand { get; }


        public ReadOnlyReactiveProperty<string> LibraryRefreshStatus { get; }
        public ReactiveProperty<string> Text { get; }

        public ObservableCollection<string> Added { get; }
        public ObservableCollection<string> Removed { get; }
        public ObservableCollection<string> Updated { get; }

        public MainWindowViewModel()
        {
            var core = ((App)Application.Current).Core;
            var library = core.Library;

            this.Text = new ReactiveProperty<string>().AddTo(this.Disposables);

            this.Added = new ObservableCollection<string>();
            this.Removed = new ObservableCollection<string>();
            this.Updated = new ObservableCollection<string>();

            this.LibraryRefreshStatus = library.Loading
                .ToReadOnlyReactiveProperty().AddTo(this.Disposables);


            library.Loaded.Subscribe(x =>
            {
                this.Text.Value = $"Added:{x.AddedFiles.Count}"
                + $", Removed:{x.RemovedFiles.Count}, Updated:{x.UpdatedFiles.Count}";

                this.Added.Clear();
                x.AddedFiles.Select(y => y.Value.FullPath).OrderBy(y => y)
                    .ForEach(y => this.Added.Add(y));

                this.Removed.Clear();
                x.RemovedFiles.Select(y => y.Value.FullPath).OrderBy(y => y)
                    .ForEach(y => this.Removed.Add(y));

                this.Updated.Clear();
                x.UpdatedFiles.Select(y => y.Value.FullPath).OrderBy(y => y)
                    .ForEach(y => this.Updated.Add(y));

            })
            .AddTo(this.Disposables);


            this.RefreshLibraryCommand = new ReactiveCommand()
                .WithSubscribe(_ =>
                {
                    this.Text.Value = "processing";
                    library.StartRefreshLibrary();
                }, this.Disposables);

            this.GenerateNewClientCommand = new ReactiveCommand()
                .WithSubscribe(_ => core.ShowNewClient(null), this.Disposables);

            this.ConvertCommand = new ReactiveCommand()
                .WithSubscribe(_ => core.ConvertOldLibrary().FireAndForget(), this.Disposables);
        }
    }
}
