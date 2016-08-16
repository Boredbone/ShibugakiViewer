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
using ImageLibrary.Core;
using ImageLibrary.Creation;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using WpfTools;

namespace ShibugakiViewer.ViewModels.SettingPages
{
    class FolderSettingPageViewModel : DisposableBase
    {

        public FolderDictionary Folders => this.library.Folders;

        public ReactiveProperty<bool> IsProfessionalFolderSettingEnabled { get; }
        public ReactiveProperty<bool> IsEditable { get; }
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


            this.IsProfessionalFolderSettingEnabled = core
                .ToReactivePropertyAsSynchronized(x => x.IsProfessionalFolderSettingEnabled)
                .AddTo(this.Disposables);

            this.IsProfessionalFolderSettingEnabledView = this.IsEditable
                .CombineLatest(this.IsProfessionalFolderSettingEnabled, (a, b) => a && b)
                .ToReadOnlyReactiveProperty()
                .AddTo(this.Disposables);

            this.IgnoreCommand = this.IsEditable
                .ToReactiveCommand()
                .WithSubscribe(x =>
                {
                    var folder = x as FolderInformation;
                    if (folder != null)
                    {
                        folder.Ignored = true;
                        this.Folders.Reset();
                    }
                }, this.Disposables);

            this.RefreshCommand = this.IsEditable
                .ToReactiveCommand()
                .WithSubscribe(x =>
                {
                    var folder = x as FolderInformation;
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
                    string folderPath = null;
                    using (var fbd = new FolderSelectDialog())
                    {
                        if (System.IO.Directory.Exists(defaultPath))
                        {
                            fbd.DefaultDirectory = defaultPath;
                        }

                        if (fbd.ShowDialog() == true)
                        {
                            folderPath = fbd.SelectedPath;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(folderPath))
                    {
                        return;
                    }

                    this.previousSelectedDirectory = folderPath;

                    var exists = this.library.Folders.GetAll()
                        .FirstOrDefault(x => x.Path.Equals(folderPath));

                    if (exists != null)
                    {
                        exists.Ignored = false;
                        return;
                    }
                    else
                    {
                        this.library.Folders.Add(new FolderInformation(folderPath));
                        this.library.RefreshLibraryAsync(true).FireAndForget();
                    }

                }, this.Disposables);
        }
    }
}
