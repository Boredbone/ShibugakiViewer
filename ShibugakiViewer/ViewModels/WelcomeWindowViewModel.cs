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

namespace ShibugakiViewer.ViewModels
{
    class WelcomeWindowViewModel : DisposableBase
    {
        public ReactiveCommand StartRefreshCommand { get; }

        private Subject<bool> ExitSubject { get; }
        public IObservable<bool> Exit => this.ExitSubject.AsObservable();

        private readonly App application;

        public WelcomeWindowViewModel()
        {
            this.application = (App)Application.Current;
            var core = this.application.Core;
            var library = core.Library;

            this.ExitSubject = new Subject<bool>().AddTo(this.Disposables);

            library.Loaded.Subscribe(_ =>
            {
                if (library.HasItems())
                {
                    this.ExitSubject.OnNext(true);
                    this.application.ShowClientWindowWithCatalog();
                }
            })
            .AddTo(this.Disposables);

            this.StartRefreshCommand = library.IsCreating
                .Select(x => !x)
                .ToReactiveCommand()
                .WithSubscribe(_ =>
                {
                    library.StartRefreshLibrary();
                }, this.Disposables);
        }

    }
}
