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
        public ReactiveCommand StartRefreshCommand { get; }

        private Subject<bool> ExitSubject { get; }
        public IObservable<bool> Exit => this.ExitSubject.AsObservable();


        public ReactiveProperty<int> SelectedTab { get; }

        public ReactiveCommand ChangeTabCommand { get; }
        public ReactiveCommand ConvertOldLibraryCommand { get; }

        private readonly App application;

        public WelcomeWindowViewModel()
        {
            this.application = (App)Application.Current;
            var core = this.application.Core;
            var library = core.Library;

            this.ExitSubject = new Subject<bool>().AddTo(this.Disposables);

            var oldConvertable = core.IsOldConvertable();

            var firstTab = oldConvertable ? 4 : 0;

            this.SelectedTab = new ReactiveProperty<int>(firstTab).AddTo(this.Disposables);

            this.ChangeTabCommand = new ReactiveCommand()
                .WithSubscribeOfType<string>(x =>
                {
                    var index = 0;
                    if (int.TryParse(x, out index))
                    {
                        this.SelectedTab.Value = index;
                    }
                }, this.Disposables);

            library.Loaded.Subscribe(_ =>
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
                .ToReactiveCommand()
                .WithSubscribe(_ =>
                {
                    this.SelectedTab.Value = 3;
                    library.StartRefreshLibrary();
                }, this.Disposables);

            this.ConvertOldLibraryCommand = Observable
                .Return(oldConvertable)
                .ToReactiveCommand()
                .WithSubscribe(_ => application.ConvertOldLibrary(), this.Disposables);
        }

    }
}
