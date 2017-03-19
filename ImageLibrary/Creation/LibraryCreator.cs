using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility.Extensions;
using Database.Table;
using ImageLibrary.Core;
using ImageLibrary.File;
using ImageLibrary.Tag;
using Reactive.Bindings.Extensions;
using ImageLibrary.SearchProperty;
using Database.Search;
using ImageLibrary.Search;
using System.Collections.ObjectModel;
using Boredbone.Utility.Notification;
using Boredbone.Utility.Tools;

namespace ImageLibrary.Creation
{
    /// <summary>
    /// ストレージを検索し、ライブラリをアップデート
    /// </summary>
    public class LibraryCreator : DisposableBase
    {
        private BehaviorSubject<string> LoadingSubject { get; }
        public IObservable<string> Loading => this.LoadingSubject.AsObservable();

        private BehaviorSubject<int> FileEnumeratedSubject { get; }
        public IObservable<int> FileEnumerated => this.FileEnumeratedSubject.AsObservable();
        private BehaviorSubject<int> FileLoadedSubject { get; }
        public IObservable<int> FileLoaded => this.FileLoadedSubject.AsObservable();


        private Subject<LibraryLoadResult> LoadedSubject { get; }
        public IObservable<LibraryLoadResult> Loaded => this.LoadedSubject.AsObservable();
        
        public ILibraryConfiguration Config { get; set; }

        private Dictionary<string, Record> addedFilesResult = null;
        private Dictionary<string, Record> removedFilesResult = null;
        private Dictionary<string, Record> updatedFilesResult = null;

        public FolderInformation[] Folders { get; set; }
        public TagDictionary TagDictionary { get; set; }
        public TypedTable<Record, string> Records { get; set; }
        public string[] IgnoredFolders { get; set; }
        public bool Completely { get; set; }
        public PropertiesLevel Level { get; set; } = PropertiesLevel.Basic;
        public Action CompletingTask { get; set; }

        private readonly Library library;


        public LibraryCreator(Library library, ILibraryConfiguration config)
        {
            this.Config = config;
            this.library = library;

            this.LoadedSubject = new Subject<LibraryLoadResult>().AddTo(this.Disposables);
            this.LoadingSubject = new BehaviorSubject<string>(null).AddTo(this.Disposables);
            this.FileEnumeratedSubject = new BehaviorSubject<int>(0).AddTo(this.Disposables);
            this.FileLoadedSubject = new BehaviorSubject<int>(0).AddTo(this.Disposables);
        }

        /// <summary>
        /// ストレージのファイルを検索し、ライブラリを更新する
        /// </summary>
        /// <returns></returns>
        public async Task RefreshLibraryAsync(LibraryLoadAction action,
            string[] addedFiles = null, string[] removedFiles = null)
        {
            this.LoadingSubject.OnNext("");

#if TEST_BUILD
            if (true)
            {
#pragma warning disable 162
                for (int x = 0; x < 30; x++)
                {
                    await Task.Delay(500);
                    this.LoadingSubject.OnNext(Guid.NewGuid().ToString());
                }
                await Task.Delay(1000);
                this.LoadingSubject.OnNext(null);
                await Task.Delay(100);

                this.LoadedSubject.OnNext(new LibraryLoadResult()
                {
                    AddedFiles = new Dictionary<string, Record>(),
                    RemovedFiles = new Dictionary<string, Record>(),
                    UpdatedFiles = new Dictionary<string, Record>(),
                    Action = action,
                    DateTime = DateTimeOffset.Now,
                });
                return;
#pragma warning restore 162
            }
#endif

            var cts = new CancellationTokenSource();


            this.addedFilesResult = null;
            this.removedFilesResult = null;
            this.updatedFilesResult = null;

            try
            {
#if DEBUG
                var sw = new Stopwatch();
                sw.Start();
#endif
                await Task.Factory.StartNew(() => this.RefreshLibraryMainAsync(addedFiles, removedFiles),
                    cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

#if DEBUG
                sw.Stop();
                Debug.WriteLine($"elapsed:{sw.ElapsedMilliseconds}");
#endif
                await Task.Delay(100);

                this.LoadedSubject.OnNext(new LibraryLoadResult()
                {
                    AddedFiles = addedFilesResult,
                    RemovedFiles = removedFilesResult,
                    UpdatedFiles = updatedFilesResult,
                    Action = action,
                    DateTime = DateTimeOffset.Now,
                });
            }
            catch
            {

            }

            this.addedFilesResult = null;
            this.removedFilesResult = null;
            this.updatedFilesResult = null;
        }


        private async Task RefreshLibraryMainAsync(string[] added, string[] removed)
        {
            //登録解除されたフォルダ内のファイルは削除扱い

            var folderIgnoredRecords = new Dictionary<string, Record>();

            if (this.IgnoredFolders != null && this.IgnoredFolders.Length > 0)
            {
                var complex = new ComplexSearch(true);

                this.IgnoredFolders.Select(x => new UnitSearch()
                {
                    Property = FileProperty.DirectoryPathStartsWith,
                    Reference = PathUtility.WithoutPostSeparator(x),
                    Mode = CompareMode.Equal,
                })
                .ForEach(x => complex.Add(x));

                var folderFilter = complex.ToSql();

                if (folderFilter != null)
                {

                    var filter = DatabaseExpression.And
                        (DatabaseExpression.IsFalse(nameof(Record.IsGroup)), folderFilter);

                    using (var connection = this.Records.Parent.Connect())
                    {
                        folderIgnoredRecords = (await this.Records
                            .AsQueryable(connection)
                            .Where(filter)
                            .ToArrayAsync())
                            .ToDictionary(x => x.Id, x => x);
                    }
                }
            }


            //列挙結果を保管する辞書
            var addedFiles = new Dictionary<string, Record>();
            var removedFiles = new Dictionary<string, Record>(folderIgnoredRecords);
            var updatedFiles = new Dictionary<string, Record>();
            var skippedFiles = new HashSet<string>();

            var prospectedTags = new ConcurrentDictionary<string, ConcurrentBag<TagManager>>();

            //登録フォルダ内の全ファイルを列挙
            //ストレージアクセスきついので直列で処理
            foreach (var folder in this.Folders.Where(x => x.RefreshEnable))
            {

                //このフォルダ配下のファイル
                Record[] array;

                using (var connection = this.Records.Parent.Connect())
                {
                    array = await this.Records.AsQueryable(connection)
                        .Where(DatabaseExpression.And(DatabaseExpression.IsFalse(nameof(Record.IsGroup)),
                            FileProperty.DirectoryPathStartsWith.ToSearch(folder.Path, CompareMode.Equal)))
                        .ToArrayAsync();
                }

                var relatedFiles = new ReadOnlyDictionary<string, Record>
                    (array.ToDictionary(x => x.Id, x => x));


                var tf = new FolderFileDetection(this.Config);

                tf.ChildFolderLoaded += x => this.LoadingSubject.OnNext(x);
                tf.FileLoaded += x => this.FileLoadedSubject.OnNext(x);
                tf.FileEnumerated += x => this.FileEnumeratedSubject.OnNext(x);

                var result = await tf.ListupFilesAsync
                    (folder, relatedFiles, this.Completely, this.Level, prospectedTags)
                    .ConfigureAwait(false);

                //見つかったファイルを登録，重複していたら上書き
                tf.AddedFiles.ForEach(x => addedFiles[x.Key] = x.Value);
                tf.RemovedFiles.ForEach(x => removedFiles[x.Key] = x.Value);
                tf.UpdatedFiles.ForEach(x => updatedFiles[x.Key] = x.Value);
                tf.SkippedFiles.ForEach(x => skippedFiles.Add(x));

            }

            this.LoadingSubject.OnNext("");
            

            //画像を読み込めなかったファイルも削除
            using (var connection = this.Records.Parent.Connect())
            {
                var notFoundRecords = await this.Records
                    .AsQueryable(connection)
                    .Where(DatabaseExpression.And(DatabaseExpression.IsTrue(nameof(Record.IsNotFound)),
                        DatabaseExpression.IsFalse(nameof(Record.IsGroup))))
                    .ToArrayAsync();

                foreach (var f in notFoundRecords)
                {
                    if (!removedFiles.ContainsKey(f.Id))
                    {
                        removedFiles[f.Id] = f;
                    }
                }
            }


            var topPathList = this.Folders
                .Where(x => !x.IsIgnored)
                .Select(x => x.Path)
                .OrderBy(x => x)
                .ToArray();
            
            if (added != null && removed != null)
            {

                ReadOnlyDictionary<string, Record> relatedFiles;

                using (var connection = this.Records.Parent.Connect())
                {
                    var items = added.Concat(removed).Distinct().ToArray();
                    var r = await this.Records.GetRecordsFromKeyAsync(connection, items);
                    
                    relatedFiles = new ReadOnlyDictionary<string, Record>
                        (r.ToDictionary(x => x.Id, x => x));
                }


                var tf = new FolderFileDetection(this.Config);

                tf.ChildFolderLoaded += x => this.LoadingSubject.OnNext(x);
                tf.FileLoaded += x => this.FileLoadedSubject.OnNext(x);
                tf.FileEnumerated += x => this.FileEnumeratedSubject.OnNext(x);

                var result = await tf.CheckFolderUpdateAsync(added, this.Level, relatedFiles, prospectedTags)
                    .ConfigureAwait(false);
                
                    //見つかったファイルを登録，重複していたら上書き
                    tf.AddedFiles.ForEach(x => addedFiles[x.Key] = x.Value);
                    tf.RemovedFiles.ForEach(x => removedFiles[x.Key] = x.Value);
                    tf.UpdatedFiles.ForEach(x => updatedFiles[x.Key] = x.Value);
                    tf.SkippedFiles.ForEach(x => skippedFiles.Add(x));
                
            }

            //ファイルにキーワードが設定されていたらタグとして設定
            foreach (var item in prospectedTags)
            {
                var id = this.TagDictionary.SetTag(new TagInformation() { Name = item.Key });
                item.Value.ForEach(x => x.Add(id));
            }

            //更新結果
            addedFilesResult = new Dictionary<string, Record>(addedFiles);
            removedFilesResult = new Dictionary<string, Record>(removedFiles);
            updatedFilesResult = new Dictionary<string, Record>(updatedFiles);
            
            var missLoadedItems = new HashSet<string>();


            //旧ライブラリにあって新ライブラリに無いファイル=削除された可能性のあるファイル
            //本当に無いのか確認
            foreach (var file in removedFiles)
            {

                //ファイル名が同じなら同じファイルが移動したと判断
                var filesWithSameName = addedFilesResult
                    .Where(x => x.Value.FileName.Equals(file.Value.FileName))
                    .ToList();

                if (filesWithSameName.Count == 1)
                {
                    this.MarkAsUpdated(file, filesWithSameName[0]);
                    continue;
                }

                //別フォルダに移動しているか
                if (!topPathList.IsNullOrEmpty())
                {
                    var relativePath = file.Value.FileName;
                    foreach (var topPath in topPathList)
                    {
                        if (file.Value.Directory.StartsWith(topPath))
                        {
                            relativePath = file.Value.FullPath.Substring(topPath.Length);
                            break;
                        }
                    }
                    var filesWithSamePath = addedFilesResult
                        .Where(x => x.Value.FullPath.EndsWith(relativePath))
                        .ToList();

                    if (filesWithSamePath.Count == 1)
                    {
                        this.MarkAsUpdated(file, filesWithSamePath[0]);
                        continue;
                    }
                }

                //プロパティが一致するものが一つだけあれば同じものと判断
                var filesWithSameProperty = addedFilesResult
                    .Where(x => x.Value.DateCreated == file.Value.DateCreated
                        && x.Value.Size == file.Value.Size
                        && x.Value.DateModified == file.Value.DateModified
                        && x.Value.Height == file.Value.Height
                        && x.Value.Width == file.Value.Width)
                    .ToList();

                if (filesWithSameProperty.Count == 1)
                {
                    this.MarkAsUpdated(file, filesWithSameProperty[0]);
                    continue;
                }

                //本当にファイルが存在しないのか確認
                if (!folderIgnoredRecords.ContainsKey(file.Key))
                {
                    if (this.Config.IsFileExists(file.Value))
                    {
                        missLoadedItems.Add(file.Key);
                        removedFilesResult.Remove(file.Key);
                    }
                }
            }
            foreach (var key in missLoadedItems)
            {
                removedFiles.Remove(key);
            }

            

            //データベース更新

            using (var connection = this.Records.Parent.ConnectAsThreadSafe())
            {
                //削除
                this.Records.RemoveRangeBuffered(connection.Value, removedFiles.Select(x => x.Value));

                //追加・更新
                var dateNow = DateTimeOffset.Now;
                foreach (var item in addedFiles)
                {
                    item.Value.DateRegistered = dateNow;
                }
                await this.Records.AddRangeBufferedAsync(connection.Value,
                    addedFiles.Select(x => x.Value).Concat(updatedFiles.Select(x => x.Value)), true);


                //関連するグループの情報を更新
                var groups = addedFiles.Select(x => x.Value.GroupKey)
                    .Union(updatedFiles.Select(x => x.Value.GroupKey))
                    .Union(removedFiles.Select(x => x.Value.GroupKey))
                    .Where(x => !x.IsNullOrWhiteSpace())
                    .ToArray();

                await library.Grouping.RefreshGroupPropertiesAsync(connection.Value, groups);
            }
            
            this.CompletingTask();

            this.LoadingSubject.OnNext(null);
        }


        private void MarkAsUpdated
            (KeyValuePair<string, Record> oldFile, KeyValuePair<string, Record> newFile)
        {
            newFile.Value.CopyAdditionalInformation(oldFile.Value);

            this.removedFilesResult.Remove(oldFile.Key);
            this.addedFilesResult.Remove(newFile.Key);
            this.updatedFilesResult[newFile.Key] = newFile.Value;
        }
    }
}

