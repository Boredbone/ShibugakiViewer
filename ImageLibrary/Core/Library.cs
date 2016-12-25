using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Boredbone.Utility;
using Boredbone.Utility.Extensions;
using Database.Search;
using Database.Table;
using ImageLibrary.Creation;
using ImageLibrary.File;
using ImageLibrary.Search;
using ImageLibrary.SearchProperty;
using ImageLibrary.Tag;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Boredbone.Utility.Tools;
using Boredbone.Utility.Notification;
using System.Data;
using ImageLibrary.Exif;

namespace ImageLibrary.Core
{
    /// <summary>
    /// 画像情報のデータベース
    /// </summary>
    public class Library : NotificationBase
    {
        private const int xmlVersion = 5;
        private const int databaseVersion = 5;

        private const double trackIntervalTime = 500.0;
        

        public const string databaseFileName = "library.db";
        public const string librarySettingFileName = "libsettings.config";
        private XmlSettingManager<LibrarySettings> librarySettingXml;


        public IObservable<string> Loading => this.Creator.Loading;
        public IObservable<LibraryLoadResult> Loaded => this.Creator.Loaded;
        public IObservable<int> FileEnumerated => this.Creator.FileEnumerated;
        public IObservable<int> FileLoaded => this.Creator.FileLoaded;

        private BehaviorSubject<bool> IsCreatingSubject { get; }
        public IObservable<bool> IsCreating => this.IsCreatingSubject.AsObservable();


        private Subject<DatabaseUpdatedEventArgs> DatabaseUpdatedSubject { get; }
        public IObservable<DatabaseUpdatedEventArgs> DatabaseUpdated => this.DatabaseUpdatedSubject.AsObservable();
        

        private Subject<string> MessageSubject { get; }
        public IObservable<string> Message => this.MessageSubject.AsObservable();


        public SearchSortManager Searcher { get; }

        public TagDictionary Tags { get; }
        public FolderDictionary Folders { get; }
        public ExifManager ExifManager { get; }

        public LibraryQueryHelper QueryHelper { get; }
        public RecordQuery RecordQuery { get; }
        public GroupQuery GroupQuery { get; }
        public Grouping Grouping { get; }

        private DatabaseFront Database { get; }

        private TypedTable<Record, string> Records { get; }
        private AutoTrackingTable<TagInformation, int> TagDatabase { get; }
        private AutoTrackingTable<FolderInformation, int> FolderDatabase { get; }
        private AutoTrackingTable<ExifVisibilityItem, int> ExifVisibilityDatabase { get; }

        private Tracker<Record, string> RecordTracker { get; }
        //private Tracker<TagInformation, int> TagTracker { get; }
        //private Tracker<FolderInformation, int> FolderTracker { get; }
        //private Tracker<ExifVisibilityItem, int> ExifVisibilityTracker { get; }

        private LibraryCreator Creator { get; }

        private LibrarySettings librarySettings;

        public bool IsGroupingEnabled
        {
            get { return this.librarySettings.IsGroupingEnabled; }
            set
            {
                if (this.librarySettings.IsGroupingEnabled != value)
                {
                    this.librarySettings.IsGroupingEnabled = value;
                    RaisePropertyChanged(nameof(IsGroupingEnabled));
                }
            }
        }

        public bool RefreshLibraryCompletely
        {
            get { return this.librarySettings.RefreshLibraryCompletely; }
            set
            {
                if (this.librarySettings.RefreshLibraryCompletely != value)
                {
                    this.librarySettings.RefreshLibraryCompletely = value;
                    RaisePropertyChanged(nameof(RefreshLibraryCompletely));
                }
            }
        }

        public bool CheckFileShellInformation
        {
            get { return this.librarySettings.CheckFileShellInformation; }
            set
            {
                if (this.librarySettings.CheckFileShellInformation != value)
                {
                    this.librarySettings.CheckFileShellInformation = value;
                    RaisePropertyChanged(nameof(CheckFileShellInformation));
                }
            }
        }

        private PropertiesLevel FileCheckLevel
            => this.CheckFileShellInformation ? PropertiesLevel.Shell : PropertiesLevel.Basic;



        public bool IsLibrarySettingsLoaded { get; set; }

        private static AsyncLock asyncLock = new AsyncLock();

        public bool IsLoaded { get; private set; } = false;

        public TreeNode<string> TreeRootNode { get; private set; }

        private readonly ILibraryConfiguration config;



        public Library(ILibraryConfiguration config)
        {
            this.config = config;

            this.librarySettingXml = new XmlSettingManager<LibrarySettings>
                (System.IO.Path.Combine(config.SaveDirectory, librarySettingFileName));



            this.MessageSubject = new Subject<string>().AddTo(this.Disposables);
            this.IsCreatingSubject = new BehaviorSubject<bool>(false).AddTo(this.Disposables);
            this.DatabaseUpdatedSubject = new Subject<DatabaseUpdatedEventArgs>().AddTo(this.Disposables);

            this.Searcher = new SearchSortManager();
            

            //Initialize Database

            this.Database = new DatabaseFront(System.IO.Path.Combine(config.SaveDirectory, databaseFileName))
            {
                Version = databaseVersion,
            };

            this.Records = new TypedTable<Record, string>(this.Database, nameof(Records))
            {
                IsIdAuto = false,
                Version = databaseVersion,
            };

            this.RecordTracker = new Tracker<Record, string>(this.Records).AddTo(this.Disposables);

            this.DefineMigration();



            this.Tags = new TagDictionary().AddTo(this.Disposables);
            this.TagDatabase = new AutoTrackingTable<TagInformation, int>
                (this.Database, nameof(TagDatabase), trackIntervalTime, this.Tags.Added, databaseVersion)
                .AddTo(this.Disposables);

            this.Folders = new FolderDictionary().AddTo(this.Disposables);
            this.FolderDatabase = new AutoTrackingTable<FolderInformation, int>
                (this.Database, nameof(FolderDatabase), trackIntervalTime, this.Folders.Added, databaseVersion)
                .AddTo(this.Disposables);

            this.ExifManager = new ExifManager().AddTo(this.Disposables);
            this.ExifVisibilityDatabase = new AutoTrackingTable<ExifVisibilityItem, int>
                (this.Database, nameof(ExifVisibilityDatabase), trackIntervalTime, this.ExifManager.Added, databaseVersion)
                .AddTo(this.Disposables);
            

            this.Folders.FileTypeFilter = this.config.FileTypeFilter;
            this.Folders.FolderUpdated.Subscribe(x => this.CheckFolderUpdateAsync(x).FireAndForget())
                .AddTo(this.Disposables);

            this.RecordTracker.Updated.Subscribe(this.DatabaseUpdatedSubject).AddTo(this.Disposables);


            this.RecordQuery = new RecordQuery(this.Records, this);
            this.GroupQuery = new GroupQuery(this.Records, this);
            this.Grouping = new Grouping(this.Records, this);

            this.QueryHelper = new LibraryQueryHelper(this.Records, this);
            this.QueryHelper.Updated.Subscribe(this.DatabaseUpdatedSubject).AddTo(this.Disposables);

            this.Creator = new LibraryCreator(this,this.config)
            {
                TagDictionary = this.Tags,
                Records = this.Records,
                CompletingTask = this.MakeDirectoryTree,
            }
            .AddTo(this.Disposables);

            this.Creator.Loaded.Select(_ => new DatabaseUpdatedEventArgs()
            {
                Action = DatabaseAction.Refresh,
                Sender = this.Creator,
            })
            .Subscribe(this.DatabaseUpdatedSubject)
            .AddTo(this.Disposables);



            this.IsLibrarySettingsLoaded = false;
        }

        /// <summary>
        /// 旧バージョンデータベースとの互換設定
        /// </summary>
        private void DefineMigration()
        {
            this.Records.AddColumnOption(nameof(Record.FlipDirection), "DEFAULT 0");
            this.Records.AddColumnOption(nameof(Record.IsNotFound), "DEFAULT 0");

            this.Records.Migrating += (o, e) =>
            {
                var oldRecords = e.TableInformations.First(x => x.TableName.Equals(this.Records.Name));
                if (oldRecords.Version <= 2)
                {
                    e.Converters[nameof(Record.FlipDirection)]
                        = "IsParticularFlipDirectionEnabled*(IsFlipReversed+1)";
                }
            };
        }


        /// <summary>
        /// データベースとの接続を初期化
        /// </summary>
        public async Task LoadAsync()
        {

            TagInformation[] tags;
            FolderInformation[] folders;
            ExifVisibilityItem[] exifItems;

            using (var connection = await this.Database.ConnectAsync().ConfigureAwait(false))
            {
                await this.Database.InitializeAsync(connection).ConfigureAwait(false);

                tags = await this.TagDatabase.GetAllAsTrackingAsync(connection)
                    .ConfigureAwait(false);
                folders = await this.FolderDatabase.GetAllAsTrackingAsync(connection)
                    .ConfigureAwait(false);
                exifItems = await this.ExifVisibilityDatabase.GetAllAsTrackingAsync(connection)
                    .ConfigureAwait(false);

                //loading test
                await this.Records.AsQueryable(connection).FirstOrDefaultAsync().ConfigureAwait(false);
            }
            
            this.Tags.SetSource(tags.ToDictionary(x => x.Id, x => x));
            this.Folders.SetSource(folders);
            this.ExifManager.SetSource(exifItems);

            if (folders.Length <= 0 && this.config.IsKnownFolderEnabled)
            {
                this.Folders.TryAddItems(FolderInformation.GetSpecialFolders());
            }


            this.MakeDirectoryTree();

            this.IsLoaded = true;
        }


        /// <summary>
        /// 検索
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="sort"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<Record[]> SearchMainAsync(string filter, string[] sort, long skip, long take)
        {
            Record[] records = null;

            using (var connection = await this.Database.ConnectAsync().ConfigureAwait(false))
            {
                records = await this.Records
                    .AsQueryable(connection)
                    .Where(filter)
                    .OrderBy(sort.Append(FileProperty.Id.ToSort(false)).ToArray())
                    .Skip(skip)
                    .Take(take)
                    .ToArrayAsync()
                    .ConfigureAwait(false);
            }

            foreach (var item in records)
            {
                this.RecordTracker.Track(item);
            }

            return records;
        }
        

        /// <summary>
        /// 指定検索条件下でのインデックスを調べる
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public async Task<long> FindIndexAsync(ISearchCriteria criteria, Record target)
        {
            if (criteria == null)
            {
                return 0;
            }
            using (var connection = await this.Records.Parent.ConnectAsync().ConfigureAwait(false))
            {
                return await this.FindIndexMainAsync
                    (connection, criteria, criteria.GetFilterString(this), target).ConfigureAwait(false);
            }
        }


        /// <summary>
        /// 指定検索条件下でのインデックスを調べる
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private async Task<long> FindIndexMainAsync
            (IDbConnection connection, ISearchCriteria criteria, string filterSql, Record target)
        {
            var sql = SortSetting.GetReferenceSelectorSql(criteria.GetSort());

            var reference = await this.Records
                .GetDynamicParametersAsync(connection, sql, target)
                .ConfigureAwait(false);

            if (reference == null)
            {
                return -1;
            }

            var filter = DatabaseFunction.And(filterSql,
                SortSetting.GetOrderFilterSql(criteria.GetSort()));

            return (await this.Records.CountAsync(connection, filter, reference).ConfigureAwait(false)) - 1;

        }

        /// <summary>
        /// 全てのIDを取得
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public async Task<string[]> GetAllIdsAsync(ISearchCriteria criteria)
        {
            var filter = criteria.GetFilterString(this);

            using (var connection = await this.Database.ConnectAsync().ConfigureAwait(false))
            {
                return await this.Records
                    .AsQueryable(connection)
                    .Where(filter)
                    .Select<string>(nameof(Record.Id))
                    .ToArrayAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<string[]> GetIdsAsync(ISearchCriteria criteria, long index1, long index2)
        {
            var filter = criteria.GetFilterString(this);

            using (var connection = await this.Database.ConnectAsync().ConfigureAwait(false))
            {
                return await this.GetRegionIdsMainAsync(connection, criteria, index1, index2, filter)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 二つのレコードの間にあるIDを取得(指定レコードの物を含まない)
        /// </summary>
        /// <param name="criteria"></param>
        /// <param name="record1"></param>
        /// <param name="record2"></param>
        /// <returns></returns>
        public async Task<string[]> GetRegionIdsAsync(ISearchCriteria criteria, Record record1, Record record2)
        {
            var filter = criteria.GetFilterString(this);

            using (var connection = await this.Database.ConnectAsync().ConfigureAwait(false))
            {
                var index1 = await this.FindIndexMainAsync(connection, criteria, filter, record1)
                    .ConfigureAwait(false);
                var index2 = await this.FindIndexMainAsync(connection, criteria, filter, record2)
                    .ConfigureAwait(false);

                return await this.GetRegionIdsMainAsync(connection, criteria, index1, index2, filter)
                    .ConfigureAwait(false);
            }
        }

        public async Task<string[]> GetRegionIdsMainAsync
            (IDbConnection connection, ISearchCriteria criteria, long index1, long index2, string filter)
        {

            if ((index1 < 0 && index2 < 0)
                || (index1 < 0 || index1 == index2)
                || (index2 < 0))
            {
                return null;
            }

            var startIndex = index1;
            var endIndex = index2;
            //var startRecord = record1;
            //var endRecord = record2;

            if (index2 < index1)
            {
                startIndex = index2;
                endIndex = index1;
                //startRecord = record2;
                //endRecord = record1;
            }

            if (index2 - index1 == 1)
            {
                return null;
            }

            var sort = SortSetting.GetFullSql(criteria.GetSort());

            return await this.Records
                .AsQueryable(connection)
                .Where(filter)
                .OrderBy(sort.Append(FileProperty.Id.ToSort(false)).ToArray())
                .Select<string>(nameof(Record.Id))
                .Skip(startIndex + 1)
                .Take(endIndex - startIndex - 1)
                .ToArrayAsync()
                .ConfigureAwait(false);

        }

        /// <summary>
        /// 登録されているファイルからフォルダ構造を生成
        /// </summary>
        private void MakeDirectoryTree()
        {

            string[] array;

            using (var connection = this.Database.Connect())
            {
                array = this.Records
                    .AsQueryable(connection)
                    .Select<string>(nameof(Record.Directory))
                    .Distinct()
                    .ToArray();
            }

            this.TreeRootNode = new DirectoryTreeAnalyzer().Analyze(array);

            //所有ファイル数0のフォルダはツリーに入れない
            
        }

        /// <summary>
        /// パス文字列を検索用のリストに分割
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public TreeNode<string>[] GetPathList(string path)
        {

            var list = new List<TreeNode<string>>() { this.TreeRootNode };

            var separator = System.IO.Path.DirectorySeparatorChar.ToString();

            var text = path.ToLower();

            while (text != null && text.Length > 0)
            {
                var e = list.LastOrDefault();

                if (e == null)
                {
                    break;
                }

                var children = e.GetChildren().ToArray();

                if (children.Length <= 0)
                {
                    break;
                }

                var succeeded = false;
                foreach (var dir in children.OrderByDescending(x => x.Key.Length))
                {
                    if (dir.Key.Length <= 0)
                    {
                        text = null;
                        succeeded = true;
                        break;
                    }
                    var name = dir.Key.ToLower();

                    if (text.StartsWith(name) || name.StartsWith(text + separator))
                    {
                        list.Add(dir.Value);

                        if (text.Length > dir.Key.Length)
                        {
                            text = text.Substring(dir.Key.Length);
                        }
                        else
                        {
                            text = "";
                        }
                        succeeded = true;
                        break;
                    }
                }

                if (!succeeded)
                {
                    e.AddChild(text);
                    text = "";
                }
            }

            return list.ToArray();

        }

        /// <summary>
        /// バックグラウンドでライブラリの更新を開始
        /// </summary>
        public void StartRefreshLibrary(bool notifyResult = true)
        {
            foreach (var folder in
                this.Folders.GetAvailable().Where(x => x.AutoRefreshEnable))
            {
                folder.RefreshEnable = true;
            }

            this.RefreshLibraryAsync(notifyResult).FireAndForget();
        }

        /// <summary>
        /// ストレージのファイルを検索し、ライブラリを更新する
        /// </summary>
        /// <returns></returns>
        public async Task RefreshLibraryAsync(bool notifyResult)
        {

            if (asyncLock.IsLocked)
            {
                return;
            }

            using (await asyncLock.LockAsync())
            {
                this.IsCreatingSubject.OnNext(true);


                var ignored = this.Folders.GetIgnored().Select(x => x.Path).ToArray();


                this.Creator.Folders = this.Folders
                    .GetAvailable()
                    .Where(x => !x.Ignored)
                    .ToArray();
                this.Creator.IgnoredFolders = ignored;
                this.Creator.Completely = this.RefreshLibraryCompletely;
                this.Creator.Level = this.FileCheckLevel;// PropertiesLevel.Basic;


                await this.Creator.RefreshLibraryAsync
                    (notifyResult ? LibraryLoadAction.UserOperation : LibraryLoadAction.Startup);

                this.IsCreatingSubject.OnNext(false);
            }
        }

        /// <summary>
        /// 更新待ちフォルダあり
        /// </summary>
        /// <returns></returns>
        public bool HasRefreshWaitingFolder()
            => this.Folders.GetAvailable().Any(x => x.RefreshEnable);
        

        /// <summary>
        /// 特定フォルダのファイルを列挙
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        private async Task ActivateNeighborFilesAsync(FolderInformation[] folders)
        {
            using (await asyncLock.LockAsync())
            {
                this.IsCreatingSubject.OnNext(true);

                this.Creator.Folders = folders;
                this.Creator.IgnoredFolders = new string[0];
                this.Creator.Completely = false;
                this.Creator.Level = this.FileCheckLevel;// PropertiesLevel.Basic;

                await this.Creator.RefreshLibraryAsync(LibraryLoadAction.Activation);

                this.IsCreatingSubject.OnNext(false);
            }
        }


        /// <summary>
        /// 指定ファイル・フォルダを対象にしてデータベース更新
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private async Task CheckFolderUpdateAsync(FolderUpdatedEventArgs args)
        {
            using (await asyncLock.LockAsync())
            {
                this.IsCreatingSubject.OnNext(true);

                await Task.Run(async () =>
                {
                    this.Creator.IgnoredFolders = null;
                    this.Creator.Completely = this.RefreshLibraryCompletely;
                    this.Creator.Level = this.FileCheckLevel;// PropertiesLevel.Basic;

                    if (args.AddedFiles.Length + args.RemovedFiles.Length >= 128)
                    {

                        var folderPathDictionary
                            = args.Folders.Distinct().ToDictionary(x => x, x => new FolderInformation(x)
                            {
                                IsTopDirectoryOnly = false,
                                RefreshEnable = true,
                            });


                        var parentFolders = args.AddedFiles
                            .Concat(args.RemovedFiles)
                            .AsParallel()
                            .Select(x => System.IO.Path.GetDirectoryName(x))
                            .Distinct()
                            .ToArray();


                        var existing = folderPathDictionary
                            .Select(x => PathUtility.WithPostSeparator(x.Key))
                            .ToArray();

                        foreach(var item in parentFolders)
                        {
                            var path = PathUtility.WithPostSeparator(item);
                            if (!existing.Any(x => path.StartsWith(x)))
                            {
                                folderPathDictionary.Add(item, new FolderInformation(item)
                                {
                                    RefreshEnable = true,
                                    IsTopDirectoryOnly = true,
                                });
                            }
                        }
                        
                        this.Creator.Folders = folderPathDictionary.Select(x => x.Value).ToArray();
                        
                        await this.Creator.RefreshLibraryAsync(LibraryLoadAction.FolderChanged);
                    }
                    else
                    {
                        this.Creator.Folders = args.Folders
                            .Select(x => new FolderInformation(x)
                            {
                                IsTopDirectoryOnly = false,
                                RefreshEnable = true,
                            })
                            .ToArray();

                        await this.Creator.RefreshLibraryAsync
                            (LibraryLoadAction.FolderChanged, args.AddedFiles, args.RemovedFiles);
                    }
                });
                this.IsCreatingSubject.OnNext(false);
            }
        }

        /// <summary>
        /// データベースからアイテムを削除
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="preAction"></param>
        /// <returns></returns>
        public async Task<bool> DeleteItemsAsync
            (IEnumerable<KeyValuePair<string, Record>> files)
        {
            if (files == null)
            {
                return false;
            }

            var items = files
                .Where(x => !x.Key.IsNullOrWhiteSpace())
                .ToDictionary(x => x.Key, x => x.Value);

            if (items.Count <= 0)
            {
                return false;
            }

            using (var locking = await asyncLock.LockAsync())
            {
                var ids = items.Select(x => x.Key).ToArray();
                var groups = new HashSet<string>(items.Select(x => x.Value?.GroupKey).Distinct());

                //ストレージのファイルを削除
                var result = Boredbone.Utility.Tools.ShellFileOperation.DeleteFiles(false, null, ids);

                if (result > 0)
                {
                    return false;
                }

                using (var connection = await this.Database.ConnectAsync())
                {
                    //実体のないアイテムの所属グループをデータベースに問い合わせ
                    var emptyKeys = items
                        .Where(x => x.Value == null)
                        .Select(x => x.Key)
                        .ToArray();

                    if (emptyKeys.Length > 0)
                    {
                        var values = await this.Grouping.GetGroupIds(connection, emptyKeys);
                        foreach(var item in values)
                        {
                            groups.Add(item);
                        }
                    }

                    //削除
                    await this.Records.RemoveRangeBufferedWithFilter(connection, ids,
                        DatabaseFunction.IsFalse(nameof(Record.IsGroup)));

                    //関連するグループの情報を更新
                    await this.Grouping.RefreshGroupPropertiesAsync(connection, groups.ToArray());
                }
            }
            this.DatabaseUpdatedSubject.OnNext(new DatabaseUpdatedEventArgs()
            {
                Sender = this,
                Action = DatabaseAction.Delete,
            });

            return true;
        }

        /// <summary>
        /// 設定をXMLに保存
        /// </summary>
        public void SaveSettings()
        {
            this.librarySettings.Save(this, xmlVersion, this.librarySettingXml);
        }

        /// <summary>
        /// 設定を読み出し
        /// </summary>
        public void InitSettings()
        {
            if (this.IsLibrarySettingsLoaded)
            {
                return;
            }
            
            var tmpLibSettings = LibrarySettings
                .Load(this.librarySettingXml, this.MessageSubject.OnNext);

            this.InitializeLibrarySettings(tmpLibSettings);
        }


        /// <summary>
        /// 設定データを反映
        /// </summary>
        /// <param name="savedData"></param>
        public void InitializeLibrarySettings(LibrarySettings savedData)
        {
            savedData.Initialize(this);
            this.librarySettings = savedData;
            this.IsLibrarySettingsLoaded = true;
            
        }

        
        /// <summary>
        /// 旧ライブラリからの移行用データを取得
        /// </summary>
        /// <returns></returns>
        public Tuple<LibrarySettings, Library, TypedTable<Record, string>> GetDataForConvert()
        {
            return new Tuple<LibrarySettings, Library, TypedTable<Record, string>>
                (this.librarySettings, this, this.Records);
        }

        /// <summary>
        /// 外部から更新をロック
        /// </summary>
        /// <returns></returns>
        public Task<IDisposable> LockAsync() => asyncLock.LockAsync();

        /// <summary>
        /// 指定フォルダ内のファイルを使って起動
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public async Task ActivateFolderAsync(string path)
        {

            var directory = System.IO.Path.GetDirectoryName(path);
            var info = new FolderInformation(directory)
            {
                IsTopDirectoryOnly = true,
                AutoRefreshEnable = false,
            };
            await this.ActivateNeighborFilesAsync(new[] { info });
        }

        


        /// <summary>
        /// データベースにレコードが登録されているか確認
        /// </summary>
        /// <returns></returns>
        public async Task<bool> HasItemsAsync()
        {
            using (var connection = await this.Database.ConnectAsync())
            {
                return (await this.Records
                    .AsQueryable(connection)
                    .Select<string>(nameof(Record.Id))
                    .Take(1)
                    .FirstOrDefaultAsync()) != null;
            }
        }

        /// <summary>
        /// データベースにレコードが登録されているか確認
        /// </summary>
        /// <returns></returns>
        public bool HasItems()
        {
            //return false;

            using (var connection = this.Database.Connect())
            {
                return this.Records
                    .AsQueryable(connection)
                    .Select<string>(nameof(Record.Id))
                    .Take(1)
                    .FirstOrDefault() != null;
            }
        }

        /// <summary>
        /// データべースの登録数
        /// </summary>
        /// <returns></returns>
        public long Count()
        {
            using (var connection = this.Database.Connect())
            {
                return this.Records.Count(connection);
            }
        }

        
        /// <summary>
        /// IDからレコードを取得
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<Record> GetRecordAsync(string key)
        {
            if (key == null)
            {
                return null;
            }

            using (var connection = await this.Database.ConnectAsync())
            {
                var item = await this.Records.GetRecordFromKeyAsync(connection, key);
                if (item != null)
                {
                    this.RecordTracker.Track(item);
                }
                return item;
            }
        }



        /// <summary>
        /// データベースに指定IDが登録されているか
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            if (key == null)
            {
                return false;
            }

            using (var connection = this.Database.Connect())
            {
                return this.Records.ContainsKey(connection, key);
            }
        }

        /// <summary>
        /// データベース全消去
        /// </summary>
        public async Task ClearAsync()
        {
            using (var connection = await this.Database.ConnectAsync())
            {
                this.Records.Drop(connection);
                this.TagDatabase.Table.Drop(connection);
                this.FolderDatabase.Table.Drop(connection);
            }

            await this.LoadAsync();
        }

    }
}
