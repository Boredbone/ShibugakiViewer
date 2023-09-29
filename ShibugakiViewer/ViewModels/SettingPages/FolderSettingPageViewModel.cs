using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using ImageLibrary.Core;
using ImageLibrary.Creation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using ShibugakiViewer.Models;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class FolderSettingPageViewModel : DisposableBase
    {

        public FolderDictionary Folders => this.library.Folders;

        public ReactivePropertySlim<bool> IsProfessionalFolderSettingEnabled { get; }
        public ReadOnlyReactivePropertySlim<bool> IsEditable { get; }
        public ReactivePropertySlim<bool> IsInitializeMode { get; }
        public ReadOnlyReactivePropertySlim<bool> IsProfessionalFolderSettingEnabledView { get; }

        public ReactiveCommandSlim<object?> IgnoreCommand { get; }
        public ReactiveCommandSlim<object?> RefreshCommand { get; }
        public ReactiveCommandSlim<object?> AddCommand { get; }

        private string? previousSelectedDirectory;

        private readonly Library library;

        public FolderSettingPageViewModel()
        {
            var core = ((App)Application.Current).Core;
            this.library = core.Library;

            this.previousSelectedDirectory = this.library.Folders.GetAvailable()
                .OrderByDescending(x => x.Id)
                .Select(x => x.Path)
                .FirstOrDefault(x => System.IO.Directory.Exists(x));


            this.IsEditable = this.library.IsCreating
                .Select(x => !x)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IsInitializeMode = new ReactivePropertySlim<bool>(false);//.AddTo(this.Disposables);

            Disposable.Create(() =>
            {
                if (!this.IsInitializeMode.Value && this.library.HasRefreshWaitingFolder())
                {
                    this.library.RefreshLibraryAsync(true).FireAndForget();
                }

                this.IsInitializeMode.Dispose();
            })
            .AddTo(this.Disposables);


            this.IsProfessionalFolderSettingEnabled = core
                .ToReactivePropertySlimAsSynchronized(x => x.IsProfessionalFolderSettingEnabled)
                .AddTo(this.Disposables);

            this.IsProfessionalFolderSettingEnabledView = this.IsProfessionalFolderSettingEnabled
                .CombineLatest(this.IsInitializeMode, (a, b) => a && !b)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(this.Disposables);

            this.IgnoreCommand = this.IsEditable
                .ToReactiveCommandSlim()
                .AddTo(this.Disposables);

            this.IgnoreCommand
                .OfType<FolderInformation>()
                .Subscribe(folder => folder?.Ignore())
                .AddTo(this.Disposables);

            this.RefreshCommand = this.IsEditable
                .CombineLatest(this.IsInitializeMode, (a, b) => a && !b)
                .ToReactiveCommandSlim()
                .AddTo(this.Disposables);

            this.RefreshCommand
                .OfType<FolderInformation>()
                .Subscribe(folder =>
                {
                    if (folder != null)
                    {
                        folder.RefreshEnable = true;
                        this.library.RefreshLibraryAsync(true).FireAndForget();
                    }
                })
                .AddTo(this.Disposables);

            this.AddCommand = this.IsEditable
                .ToReactiveCommandSlim()
                .WithSubscribe(_ =>
                {
                    var dir = this.previousSelectedDirectory?.Split(System.IO.Path.DirectorySeparatorChar);
                    var defaultPath = "";
                    if (dir != null && dir.Length > 1)
                    {
                        defaultPath = dir.Take(dir.Length - 1).Join(System.IO.Path.DirectorySeparatorChar.ToString());
                    }

                    string folderPath;

                    var result = core.AddFolder(defaultPath, out folderPath);

                    if (!folderPath.IsNullOrWhiteSpace())
                    {
                        this.previousSelectedDirectory = folderPath;
                    }

                })
                .AddTo(this.Disposables);
        }
    }
}
