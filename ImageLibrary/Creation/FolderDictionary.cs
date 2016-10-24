using System;
using System.Collections;
using System.Collections.Concurrent;
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
        private ConcurrentBag<FolderInformation> Folders { get; set; }
        public bool IsEdited { get; set; }

        private Subject<FolderInformation> AddedSubject { get; }
        public IObservable<FolderInformation> Added => this.AddedSubject.AsObservable();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => this.Folders.Count;

        private FolderWatcher FolderWatcher { get; }
        public HashSet<string> FileTypeFilter { get; set; }

        private Subject<FolderUpdatedEventArgs> FolderUpdatedSubject { get; }
        public IObservable<FolderUpdatedEventArgs> FolderUpdated => this.FolderUpdatedSubject.AsObservable();

        private int maxId = -1;

        public FolderDictionary()
        {
            this.AddedSubject = new Subject<FolderInformation>().AddTo(this.Disposables);

            this.Folders = new ConcurrentBag<FolderInformation>();
            this.IsEdited = false;

            this.FolderUpdatedSubject = new Subject<FolderUpdatedEventArgs>().AddTo(this.Disposables);


            //フォルダ変更監視
            this.FolderWatcher = new FolderWatcher().AddTo(this.Disposables);

            this.FolderWatcher.FolderChanged
                .Where(x => x.ChangeType != WatcherChangeTypes.Changed)
                .BufferUntilThrottle(2000, true)
                .Subscribe(x => this.CheckFolderUpdate(x))
                .AddTo(this.Disposables);
        }

        public void SetSource(IEnumerable<FolderInformation> source)
        {
            if (source == null)
            {
                this.Folders = new ConcurrentBag<FolderInformation>();
            }
            else
            {
                this.Folders = new ConcurrentBag<FolderInformation>(source);
            }

            this.Folders.ForEach(x => this.SubscribeFolderUpdate(x));

            this.maxId = this.Folders.Select(x => x.Id).Append(0).Max();
        }


        public IEnumerable<FolderInformation> GetAvailable()
        {
            return this.Folders.Where(x => !x.Ignored);
        }

        public FolderInformation[] GetIgnored()
        {
            return this.Folders.Where(x => x.Ignored).ToArray();
        }

        public IEnumerable<FolderInformation> GetAll()
        {
            return this.Folders;
        }

        public void Add(FolderInformation item)
        {
            this.Folders.Add(item);

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
            var existingFolders = this.GetAll().ToArray();

            var registered = false;

            foreach (var folderPath in folders)
            {
                if (folderPath.IsNullOrWhiteSpace())
                {
                    continue;
                }

                var exists = existingFolders.FirstOrDefault(x => x.Path.Equals(folderPath));

                if (exists != null)
                {
                    if (exists.Ignored)
                    {
                        exists.Ignored = false;
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
                if (folder.WatchChange && !folder.Ignored)
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
            foreach (var item in items)
            {
                if (!this.Folders.Any(x => x.Path.Equals(item.Path)))
                {
                    this.Add(item);
                }

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
