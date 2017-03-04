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

        public ReactiveProperty<bool> IsProfessionalFolderSettingEnabled { get; }
        public ReactiveProperty<bool> IsEditable { get; }
        public ReactiveProperty<bool> IsInitializeMode { get; }
        public ReadOnlyReactiveProperty<bool> IsProfessionalFolderSettingEnabledView { get; }

        public ReactiveCommand IgnoreCommand { get; }
        public ReactiveCommand RefreshCommand { get; }
        public ReactiveCommand AddCommand { get; }

        private string previousSelectedDirectory;

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
                .ToReactiveProperty()
                .AddTo(this.Disposables);

            this.IsInitializeMode = new ReactiveProperty<bool>(false);//.AddTo(this.Disposables);

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
                .ToReactivePropertyAsSynchronized(x => x.IsProfessionalFolderSettingEnabled)
                .AddTo(this.Disposables);

            this.IsProfessionalFolderSettingEnabledView = this.IsProfessionalFolderSettingEnabled
                .CombineLatest(this.IsInitializeMode, (a, b) => a && !b)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.IgnoreCommand = this.IsEditable
                .ToReactiveCommand()
                .WithSubscribeOfType<FolderInformation>(folder => folder?.Ignore(), this.Disposables);

            this.RefreshCommand = this.IsEditable
                .CombineLatest(this.IsInitializeMode, (a, b) => a && !b)
                .ToReactiveCommand()
                .WithSubscribeOfType<FolderInformation>(folder =>
                {
                    if (folder != null)
                    {
                        folder.RefreshEnable = true;
                        this.library.RefreshLibraryAsync(true).FireAndForget();
                    }
                }, this.Disposables);

            this.AddCommand = this.IsEditable
                .ToReactiveCommand()
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

                }, this.Disposables);
        }
    }
}
