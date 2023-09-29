using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace ShibugakiViewer.ViewModels
{
    class WelcomeWindowViewModel : DisposableBase
    {
        public ReactiveCommandSlim<object?> StartRefreshCommand { get; }

        private Subject<bool> ExitSubject { get; }
        public IObservable<bool> Exit => this.ExitSubject.AsObservable();

        public ReactivePropertySlim<bool> IsCheckFileShellInformation { get; }

        public ReactivePropertySlim<int> SelectedTab { get; }

        public bool WaitTextVisibility { get; set; } = true;

        public ReactiveCommandSlim ChangeTabCommand { get; }
        public ReactiveCommandSlim<object?> ConvertOldLibraryCommand { get; }

        private readonly App application;

        public WelcomeWindowViewModel()
        {
            this.application = (App)Application.Current;
            var core = this.application.Core;
            var library = core.Library;

            this.ExitSubject = new Subject<bool>().AddTo(this.Disposables);

            this.IsCheckFileShellInformation = library
                .ToReactivePropertySlimAsSynchronized(x => x.CheckFileShellInformation)
                .AddTo(this.Disposables);

            var oldConvertable = core.IsOldConvertable();

            var firstTab = oldConvertable ? 4 : 0;

            this.SelectedTab = new ReactivePropertySlim<int>(firstTab).AddTo(this.Disposables);

            this.ChangeTabCommand = new ReactiveCommandSlim().AddTo(this.Disposables);
            this.ChangeTabCommand
                .OfType<string>()
                .Subscribe(x =>
                {
                    var index = 0;
                    if (int.TryParse(x, out index))
                    {
                        this.SelectedTab.Value = index;
                    }
                })
                .AddTo(this.Disposables);

            library.Loaded.ObserveOnUIDispatcher().Subscribe(_ =>
            {
                if (library.HasItems())
                {
                    this.ExitSubject.OnNext(true);
                    this.application.ShowFirstClient(true, null);
                }
                else
                {
                    this.SelectedTab.Value = 2;
                }
            })
            .AddTo(this.Disposables);

            this.StartRefreshCommand = library.IsCreating
                .Select(x => !x)
                .ToReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    this.SelectedTab.Value = 3;
                    library.StartRefreshLibrary();
                })
                .AddTo(this.Disposables);

            this.ConvertOldLibraryCommand = Observable
                .Return(oldConvertable)
                .ToReactiveCommandSlim()
                .WithSubscribe(_ => application.ConvertOldLibrary())
                .AddTo(this.Disposables);
        }

    }
}
