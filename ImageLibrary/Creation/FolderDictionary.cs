using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Boredbone.Utility.Notification;
using Boredbone.Utility.Tools;
using Reactive.Bindings.Extensions;

namespace ImageLibrary.Creation
{
    public class FolderDictionary : DisposableBase,
        INotifyCollectionChanged, IEnumerable<FolderInformation>
    {
        private List<FolderInformation> registeredFolders;
        public bool IsEdited { get; set; }

        private Subject<FolderInformation> AddedSubject { get; }
        public IObservable<FolderInformation> Added => this.AddedSubject.AsObservable();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => this.registeredFolders.Count;

        private FolderWatcher FolderWatcher { get; }
        public HashSet<string> FileTypeFilter { get; set; }

        private Subject<FolderUpdatedEventArgs> FolderUpdatedSubject { get; }
        public IObservable<FolderUpdatedEventArgs> FolderUpdated => this.FolderUpdatedSubject.AsObservable();

        private int maxId = -1;

        private object gate = new object();

        public FolderDictionary()
        {
            this.AddedSubject = new Subject<FolderInformation>().AddTo(this.Disposables);

            this.registeredFolders = new List<FolderInformation>();
            this.IsEdited = false;

            this.FolderUpdatedSubject = new Subject<FolderUpdatedEventArgs>().AddTo(this.Disposables);


            //フォルダ変更監視
            this.FolderWatcher = new FolderWatcher().AddTo(this.Disposables);

            this.FolderWatcher.FolderChanged
                .Where(x => x.ChangeType != WatcherChangeTypes.Changed)
                .BufferUntilThrottle(TimeSpan.FromSeconds(4), true)
                .Subscribe(x => this.CheckFolderUpdate(x))
                .AddTo(this.Disposables);
        }

        public void SetSource(IEnumerable<FolderInformation> source)
        {
            lock (this.gate)
            {
                if (source == null)
                {
                    this.registeredFolders = new List<FolderInformation>();
                }
                else
                {
                    this.registeredFolders = new List<FolderInformation>(source.Distinct(x => x.Path));
                }

                this.registeredFolders.ForEach(x => this.SubscribeFolderUpdate(x));

                this.maxId = this.registeredFolders.Select(x => x.Id).Append(0).Max();
            }
        }


        public FolderInformation[] GetAvailable()
        {
            lock (this.gate)
            {
                return this.registeredFolders.Where(x => !x.IsIgnored).ToArray();
            }
        }

        public FolderInformation[] GetIgnored()
        {
            lock (this.gate)
            {
                return this.registeredFolders.Where(x => x.IsIgnored).ToArray();
            }
        }


        public void Add(FolderInformation item)
        {
            lock (this.gate)
            {
                this.registeredFolders.Add(item);
            }

            item.Id = ++this.maxId;

            this.SubscribeFolderUpdate(item);

            this.Reset();
            this.AddedSubject.OnNext(item);
        }

        /// <summary>
        /// フォルダを登録
        /// </summary>
        /// <param name="folders"></param>
        /// <returns>登録された場合はtrue</returns>
        public bool RegisterFolders(params string[] folders)
        {
            var registered = false;

            foreach (var folderPath in folders)
            {
                if (folderPath.IsNullOrWhiteSpace())
                {
                    continue;
                }
                FolderInformation exists;
                lock (this.gate)
                {
                    exists = this.registeredFolders.FirstOrDefault(x => x.Path.Equals(folderPath));
                }

                if (exists != null)
                {
                    if (exists.IsIgnored)
                    {
                        exists.CancelIgnore();
                        exists.RefreshEnable = true;
                        registered = true;
                    }
                }
                else
                {
                    this.Add(new FolderInformation(folderPath));
                    registered = true;
                }

            }

            return registered;
        }

        /// <summary>
        /// フォルダ監視
        /// </summary>
        /// <param name="folder"></param>
        private void SubscribeFolderUpdate(FolderInformation folder)
        {
            folder.PropertyChangedAsObservable()
                .Subscribe(x =>
                {
                    if (!x.PropertyName.Equals(nameof(FolderInformation.WatchChange))
                        && !x.PropertyName.Equals(nameof(FolderInformation.IsTopDirectoryOnly))
                        && !x.PropertyName.Equals(nameof(FolderInformation.Ignored)))
                    {
                        return;
                    }
                    this.WatchFolderUpdate(folder);

                    if (x.PropertyName.Equals(nameof(FolderInformation.Ignored)))
                    {
                        this.Reset();
                    }
                })
                .AddTo(this.Disposables);

            this.WatchFolderUpdate(folder);
        }
        private void WatchFolderUpdate(FolderInformation folder)
        {
            try
            {
                if (folder.WatchChange && !folder.IsIgnored)
                {
                    this.FolderWatcher.Add(folder.Path, !folder.IsTopDirectoryOnly);
                }
                else
                {
                    this.FolderWatcher.Remove(folder.Path);
                }
            }
            catch
            {
                //フォルダを登録できない場合はスキップ
            }
        }


        public void TryAddItems(IEnumerable<FolderInformation> items)
        {
            List<FolderInformation> newItems = new List<FolderInformation>();

            lock (this.gate)
            {
                foreach (var item in items.Distinct(x => x.Path))
                {
                    if (!this.registeredFolders.Any(x => x.Path.Equals(item.Path)))
                    {
                        newItems.Add(item);
                    }
                }
            }

            foreach (var item in newItems)
            {
                this.Add(item);
            }
        }

        public void Reset()
        {
            this.CollectionChanged?.Invoke
                (this, new NotifyCollectionChangedEventArgs
                    (NotifyCollectionChangedAction.Reset));
        }


        /// <summary>
        /// フォルダの変更を監視
        /// </summary>
        /// <param name="x"></param>
        /// <param name="config"></param>
        private void CheckFolderUpdate(IList<ExtendedFileSystemEventArgs> files)
        {

            var added = new List<string>();
            var removed = new List<string>();
            var folders = new List<string>();

            foreach (var item in files)
            {
                if (this.FileTypeFilter.Contains(System.IO.Path.GetExtension(item.FullPath).ToLower()))
                {
                    switch (item.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                            added.Add(item.FullPath);
                            break;
                        case WatcherChangeTypes.Deleted:
                            removed.Add(item.FullPath);
                            break;
                        case WatcherChangeTypes.Changed:
                            break;
                        case WatcherChangeTypes.Renamed:
                            added.Add(item.FullPath);
                            removed.Add(item.OldFullPath);
                            break;
                        case WatcherChangeTypes.All:
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    folders.Add(item.FullPath);
                    if (item.ChangeType == WatcherChangeTypes.Renamed)
                    {
                        folders.Add(item.OldFullPath);
                    }
                }
            }

            this.FolderUpdatedSubject.OnNext(new FolderUpdatedEventArgs()
            {
                AddedFiles = added.Distinct().ToArray(),
                RemovedFiles = removed.Distinct().ToArray(),
                Folders = folders.Distinct().ToArray()
            });
        }

        public IEnumerator<FolderInformation> GetEnumerator()
            => this.GetAvailable().OrderBy(x => x.Path).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable<FolderInformation>)this).GetEnumerator();
    }

    public class FolderUpdatedEventArgs
    {
        public string[] AddedFiles { get; set; }
        public string[] RemovedFiles { get; set; }
        public string[] Folders { get; set; }
    }
}
